using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Assets.codes.Network.Messages;
using Cysharp.Threading.Tasks;

public partial class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [SerializeField] private MissionData[] availableMissions;
    [SerializeField] private int missionsPerVote = 3;
    [SerializeField] private float secondsPerMission = 15f;

    private List<Mission> activeMissions = new List<Mission>();

    public bool IsVotingActive { get; private set; }
    public float VotingTimer { get; private set; }
    public Mission[] CurrentVotingMissions { get; private set; }
    public Mission WinningMission { get; private set; }

    private Dictionary<ulong, int> playerVotes = new Dictionary<ulong, int>();
    private GameObject activeMissionScene;
    [SerializeField]
    private GameObject PortalPrefab;
    
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public int[] GetCurrentVoteCounts()
    {
        if (CurrentVotingMissions == null)
            return new int[0];

        int[] counts = new int[CurrentVotingMissions.Length];
        foreach (var kvp in playerVotes)
        {
            if (kvp.Value >= 0 && kvp.Value < counts.Length)
                counts[kvp.Value]++;
        }
        return counts;
    }

    public int GetVotingPlayerCount()
    {
        if (NetworkSystem.Instance == null || !NetworkSystem.Instance.IsOnline)
            return 1;

        int playerCount = NetworkSystem.Instance.CurrentNetworkInstance.PlayerCount;
        return Mathf.Max(playerCount, 1);
    }

    private void Update()
    {
        PruneDisconnectedLoadingPeers();

        if (!IsVotingActive)
            return;

        VotingTimer -= Time.deltaTime;
        if (VotingTimer <= 0f)
        {
            VotingTimer = 0f;
            TallyVotes();
        }
    }

    public Mission[] GetRandomMissions(int count = 3)
    {
        if (availableMissions == null || availableMissions.Length == 0)
        {
            Debug.LogError("No available missions configured in MissionManager!");
            return new Mission[0];
        }

        Mission[] selected = new Mission[Mathf.Min(count, availableMissions.Length)];
        
        // Simple random selection without replacement
        List<int> indices = new List<int>();
        for (int i = 0; i < availableMissions.Length; i++)
            indices.Add(i);

        for (int i = 0; i < selected.Length; i++)
        {
            int randomIdx = Random.Range(0, indices.Count);
            selected[i] = availableMissions[indices[randomIdx]].ToMission();
            indices.RemoveAt(randomIdx);
        }

        return selected;
    }

    public void StartVotingSession(int missionCount)
    {
        TryStartVotingSession(missionCount);
    }

    public bool TryStartVotingSession(int missionCount)
    {
        if (!travelState.TryBeginVote())
        {
            Debug.LogWarning($"[MissionManager] Vote rejected while travel is {travelState.Phase}.");
            return false;
        }

        if (missionCount <= 0)
            missionCount = missionsPerVote;

        missionCount = Mathf.Clamp(missionCount, 1, availableMissions.Length);
        CurrentVotingMissions = GetRandomMissions(missionCount);
        playerVotes.Clear();
        IsVotingActive = true;
        VotingTimer = missionCount * secondsPerMission;
        WinningMission = null;

        Debug.Log($"[MissionManager] Voting session started with {missionCount} missions. Timer: {VotingTimer}s");

        // Broadcast to all clients
        if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline && NetworkSystem.Instance.IsServer)
        {
            var msg = new NMS_Server_StartVotingSession(CurrentVotingMissions, VotingTimer, GetVotingPlayerCount());
            NetworkRouter.Instance.DistributeMessageToReady(msg);
            // Also apply locally
            MissionProjectionDisplay.Instance?.ShowVotingMissions(CurrentVotingMissions, VotingTimer, GetVotingPlayerCount());
        }
        else
        {
            // Offline / single-player
            MissionProjectionDisplay.Instance?.ShowVotingMissions(CurrentVotingMissions, VotingTimer, GetVotingPlayerCount());
        }

        return true;
    }

    public void CastVote(ulong steamId, int missionIndex)
    {
        if (!IsVotingActive)
        {
            Debug.LogWarning($"[MissionManager] Vote rejected: no active voting session.");
            return;
        }

        if (missionIndex < 0 || CurrentVotingMissions == null || missionIndex >= CurrentVotingMissions.Length)
        {
            Debug.LogWarning($"[MissionManager] Vote rejected: invalid mission index {missionIndex}.");
            return;
        }

        playerVotes[steamId] = missionIndex;
        Debug.Log($"[MissionManager] Player {steamId} voted for mission {missionIndex} ({CurrentVotingMissions[missionIndex].missionName}). Total votes: {playerVotes.Count}");

        // Broadcast live vote counts to all clients
        BroadcastVoteUpdate();
    }

    private void BroadcastVoteUpdate()
    {
        int[] counts = GetCurrentVoteCounts();
        int totalPlayers = GetVotingPlayerCount();

        if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline && NetworkSystem.Instance.IsServer)
        {
            var msg = new NMS_Server_VoteUpdate(counts, totalPlayers);
            NetworkRouter.Instance.DistributeMessageToReady(msg);
            MissionProjectionDisplay.Instance?.UpdateVoteCounts(counts, totalPlayers);
        }
        else
        {
            MissionProjectionDisplay.Instance?.UpdateVoteCounts(counts, totalPlayers);
        }
    }

    public void TallyVotes()
    {
        IsVotingActive = false;

        if (playerVotes.Count == 0 || CurrentVotingMissions == null || CurrentVotingMissions.Length == 0)
        {
            Debug.Log("[MissionManager] No votes cast. No mission selected.");
            WinningMission = null;
            travelState.Reset();

            if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline && NetworkSystem.Instance.IsServer)
            {
                var msg = new NMS_Server_VoteResult(-1, "");
                NetworkRouter.Instance.DistributeMessageToReady(msg);
                MissionProjectionDisplay.Instance?.ShowVoteResult(-1, null);
            }
            else
            {
                MissionProjectionDisplay.Instance?.ShowVoteResult(-1, null);
            }
            return;
        }

        // Group votes by mission index
        var voteCounts = new Dictionary<int, int>();
        foreach (var kvp in playerVotes)
        {
            if (!voteCounts.ContainsKey(kvp.Value))
                voteCounts[kvp.Value] = 0;
            voteCounts[kvp.Value]++;
        }

        // Find max votes
        int maxVotes = voteCounts.Values.Max();
        var topIndices = voteCounts.Where(kvp => kvp.Value == maxVotes).Select(kvp => kvp.Key).ToList();

        // Random tiebreak
        int winningIndex = topIndices[Random.Range(0, topIndices.Count)];
        WinningMission = CurrentVotingMissions[winningIndex];

        Debug.Log($"[MissionManager] Voting ended. Winner: {WinningMission.missionName} (index {winningIndex}) with {maxVotes} vote(s).");

        PrepareWinningMissionAsync(GetMissionData(WinningMission.missionName)).Forget();

        // Broadcast result
        if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline && NetworkSystem.Instance.IsServer)
        {
            var msg = new NMS_Server_VoteResult(winningIndex, WinningMission.missionName);
            NetworkRouter.Instance.DistributeMessageToReady(msg);
            MissionProjectionDisplay.Instance?.ShowVoteResult(winningIndex, WinningMission.missionName);
        }
        else
        {
            MissionProjectionDisplay.Instance?.ShowVoteResult(winningIndex, WinningMission.missionName);
        }
    }

    public void AcceptMission(Mission mission)
    {
        activeMissions.Add(mission);
        Debug.Log($"Mission accepted: {mission.missionName}");
    }

    public List<Mission> GetActiveMissions()
    {
        return activeMissions;
    }

    public void SpawnMissionScene(string missionName)
    {
        if (string.IsNullOrEmpty(missionName))
            return;

        MissionData missionData = GetMissionData(missionName);
        if (missionData == null)
        {
            Debug.LogWarning($"[MissionManager] Cannot spawn mission scene for '{missionName}': no matching MissionData found.");
            return;
        }

        if (missionData.missionScene == null)
        {
            Debug.LogWarning($"[MissionManager] Cannot spawn mission scene for '{missionName}': MissionData has no missionScene assigned.");
            return;
        }

        if (activeMissionScene != null)
            Destroy(activeMissionScene);

        activeMissionScene = Instantiate(missionData.missionScene);
        Debug.Log($"[MissionManager] Spawned mission scene prefab for '{missionName}'.");
    }

    public MissionData GetMissionData(string missionName)
    {
        if (availableMissions == null)
            return null;

        for (int i = 0; i < availableMissions.Length; i++)
        {
            MissionData missionData = availableMissions[i];
            if (missionData != null && missionData.missionName == missionName)
                return missionData;
        }

        return null;
    }
}
