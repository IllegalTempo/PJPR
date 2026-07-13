using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Assets.codes.Network.Messages;

public class MissionManager : MonoBehaviour
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

    private void Update()
    {
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
            var msg = new NMS_Server_StartVotingSession(CurrentVotingMissions, VotingTimer);
            NetworkRouter.Instance.DistributeMessageToReady(msg);
            // Also apply locally
            MissionProjectionDisplay.Instance?.ShowVotingMissions(CurrentVotingMissions, VotingTimer);
        }
        else
        {
            // Offline / single-player
            MissionProjectionDisplay.Instance?.ShowVotingMissions(CurrentVotingMissions, VotingTimer);
        }
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

        if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline && NetworkSystem.Instance.IsServer)
        {
            var msg = new NMS_Server_VoteUpdate(counts);
            NetworkRouter.Instance.DistributeMessageToReady(msg);
            MissionProjectionDisplay.Instance?.UpdateVoteCounts(counts);
        }
        else
        {
            MissionProjectionDisplay.Instance?.UpdateVoteCounts(counts);
        }
    }

    public void TallyVotes()
    {
        IsVotingActive = false;

        if (playerVotes.Count == 0 || CurrentVotingMissions == null || CurrentVotingMissions.Length == 0)
        {
            Debug.Log("[MissionManager] No votes cast. No mission selected.");
            WinningMission = null;

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

        EscapeBlackholeMission.OnMissionVoteWon(WinningMission.missionName);

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
}
