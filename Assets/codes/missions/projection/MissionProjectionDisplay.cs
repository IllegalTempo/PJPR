using UnityEngine;
using TMPro;

public class MissionProjectionDisplay : MonoBehaviour
{
    [SerializeField] private MissionProjection[] prespawnedProjections;


    [Header("Timer Display")]
    [SerializeField] private TextMeshProUGUI timerText;

    private MissionProjection[] activeProjections;
    private float votingTimer;
    private bool isVotingActive;
    private int selectedMissionIndex = -1;
    private int totalVotingPlayers;

    /// <summary>
    /// Set by MissionProjection.OnInteract when the local player votes.
    /// Display uses this to show "You have voted for X".
    /// </summary>
    public static string LocalPlayerVotedMissionName { get; set; }

    public static MissionProjectionDisplay Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        HidePrespawnedProjections();
    }

    private void Update()
    {
        if (!isVotingActive)
            return;

        votingTimer -= Time.deltaTime;
        if (votingTimer < 0f)
            votingTimer = 0f;

        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        if (timerText == null)
            return;

        int seconds = Mathf.CeilToInt(votingTimer);
        int mins = seconds / 60;
        int secs = seconds % 60;
        string timeStr = $"{mins:00}:{secs:00}";

        if (!string.IsNullOrEmpty(LocalPlayerVotedMissionName))
            timerText.text = $"You have voted for {LocalPlayerVotedMissionName} - {timeStr}";
        else
            timerText.text = $"Vote for a mission: {timeStr}";
    }

    public void ShowVotingMissions(Mission[] missions, float totalTime, int totalPlayers = 0)
    {
        CancelInvoke(nameof(ClearMissions));
        ClearMissions();

        if (missions == null || missions.Length == 0)
        {
            Debug.LogWarning("[MissionProjectionDisplay] No missions provided to display.");
            return;
        }

        if (prespawnedProjections == null || prespawnedProjections.Length == 0)
        {
            Debug.LogWarning("[MissionProjectionDisplay] No prespawned projections are assigned.");
            return;
        }

        int missionCount = Mathf.Min(missions.Length, prespawnedProjections.Length);
        if (missionCount < missions.Length)
            Debug.LogWarning($"[MissionProjectionDisplay] Only {missionCount} prespawned projections are available for {missions.Length} missions.");

        activeProjections = new MissionProjection[missionCount];
        isVotingActive = true;
        votingTimer = totalTime;
        totalVotingPlayers = Mathf.Max(totalPlayers, 0);

        // Layout cards in a horizontal row

        for (int i = 0; i < missionCount; i++)
        {
            MissionProjection projection = prespawnedProjections[i];
            if (projection != null)
            {
                projection.gameObject.SetActive(true);
                projection.Initialize(missions[i], i);
                projection.ShowVoteCount(0, totalVotingPlayers);
                activeProjections[i] = projection;
            }
        }

        UpdateTimerDisplay();
        Debug.Log($"[MissionProjectionDisplay] Displaying {missionCount} missions. Timer: {totalTime}s");
    }

    public void ShowVoteResult(int winningIndex, string winningName = null)
    {
        isVotingActive = false;
        LocalPlayerVotedMissionName = null; // Reset for next session

        if (timerText != null)
        {
            if (winningIndex >= 0 && !string.IsNullOrEmpty(winningName))
                timerText.text = $"Winner: {winningName}";
            else if (winningIndex >= 0)
                timerText.text = "Voting Ended!";
            else
                timerText.text = "No Winner";
        }

        if (activeProjections == null)
            return;

        for (int i = 0; i < activeProjections.Length; i++)
        {
            if (activeProjections[i] == null)
                continue;

            if (i == winningIndex)
            {
                activeProjections[i].gameObject.SetActive(true);
                activeProjections[i].ShowAsWinner();
            }
            else
            {
                activeProjections[i].ShowAsLoser();
                activeProjections[i].gameObject.SetActive(false);
            }
        }

        selectedMissionIndex = winningIndex;
        Debug.Log($"[MissionProjectionDisplay] Vote result: winning index = {winningIndex}");

        // Clear after 5 seconds
        Invoke(nameof(ClearMissions), 5f);
    }

    public void UpdateVoteCounts(int[] counts, int totalPlayers = 0)
    {
        if (activeProjections == null || counts == null)
            return;

        if (totalPlayers > 0)
            totalVotingPlayers = totalPlayers;

        for (int i = 0; i < activeProjections.Length && i < counts.Length; i++)
        {
            if (activeProjections[i] != null)
                activeProjections[i].ShowVoteCount(counts[i], totalVotingPlayers);
        }
    }

    public void ClearMissions()
    {
        if (activeProjections != null)
        {
            foreach (var projection in activeProjections)
            {
                if (projection != null)
                {
                    projection.ClearDisplay();
                    projection.gameObject.SetActive(false);
                }
            }
        }

        activeProjections = null;
        selectedMissionIndex = -1;
        isVotingActive = false;
        votingTimer = 0f;
        totalVotingPlayers = 0;
        LocalPlayerVotedMissionName = null;

        if (timerText != null)
            timerText.text = "";
    }

    private void HidePrespawnedProjections()
    {
        if (prespawnedProjections == null)
            return;

        foreach (var projection in prespawnedProjections)
        {
            if (projection == null)
                continue;

            projection.ClearDisplay();
            projection.gameObject.SetActive(false);
        }
    }

    public int GetSelectedMissionIndex()
    {
        return selectedMissionIndex;
    }

    public Mission GetSelectedMission()
    {
        if (selectedMissionIndex >= 0 && activeProjections != null && selectedMissionIndex < activeProjections.Length)
        {
            if (activeProjections[selectedMissionIndex] != null)
                return activeProjections[selectedMissionIndex].GetMission();
        }
        return null;
    }
}
