using UnityEngine;
using TMPro;

public class MissionProjectionDisplay : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private GameObject missionProjectionPrefab;

    [Header("Layout Settings")]
    [SerializeField] private float horizontalSpacing = 3f;
    [SerializeField] private float verticalOffset = 2f;
    [SerializeField] private float projectionScale = 1f;

    [Header("Timer Display")]
    [SerializeField] private TextMeshProUGUI timerText;

    private MissionProjection[] activeProjections;
    private float votingTimer;
    private bool isVotingActive;
    private int selectedMissionIndex = -1;

    public static MissionProjectionDisplay Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
        timerText.text = $"Voting: {mins:00}:{secs:00}";
    }

    public void ShowVotingMissions(Mission[] missions, float totalTime)
    {
        ClearMissions();

        if (missions == null || missions.Length == 0)
        {
            Debug.LogWarning("[MissionProjectionDisplay] No missions provided to display.");
            return;
        }

        int missionCount = missions.Length;
        activeProjections = new MissionProjection[missionCount];
        isVotingActive = true;
        votingTimer = totalTime;

        // Layout cards in a horizontal row
        float totalWidth = (missionCount - 1) * horizontalSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < missionCount; i++)
        {
            GameObject projectionObj = Instantiate(
                missionProjectionPrefab,
                transform.position + new Vector3(startX + i * horizontalSpacing, verticalOffset, 0),
                Quaternion.identity,
                transform
            );

            projectionObj.transform.localScale = Vector3.one * projectionScale;

            MissionProjection projection = projectionObj.GetComponent<MissionProjection>();
            if (projection != null)
            {
                projection.Initialize(missions[i], i);
                activeProjections[i] = projection;
            }
        }

        UpdateTimerDisplay();
        Debug.Log($"[MissionProjectionDisplay] Displaying {missionCount} missions. Timer: {totalTime}s");
    }

    public void ShowVoteResult(int winningIndex)
    {
        isVotingActive = false;

        if (timerText != null)
            timerText.text = winningIndex >= 0 ? "Voting Ended!" : "No Winner";

        if (activeProjections == null)
            return;

        for (int i = 0; i < activeProjections.Length; i++)
        {
            if (activeProjections[i] == null)
                continue;

            if (i == winningIndex)
                activeProjections[i].ShowAsWinner();
            else
                activeProjections[i].ShowAsLoser();
        }

        selectedMissionIndex = winningIndex;
        Debug.Log($"[MissionProjectionDisplay] Vote result: winning index = {winningIndex}");

        // Clear after 5 seconds
        Invoke(nameof(ClearMissions), 5f);
    }

    public void UpdateVoteCounts(int[] counts)
    {
        if (activeProjections == null || counts == null)
            return;

        for (int i = 0; i < activeProjections.Length && i < counts.Length; i++)
        {
            if (activeProjections[i] != null)
                activeProjections[i].ShowVoteCount(counts[i]);
        }
    }

    public void ClearMissions()
    {
        if (activeProjections != null)
        {
            foreach (var projection in activeProjections)
            {
                if (projection != null)
                    Destroy(projection.gameObject);
            }
        }

        activeProjections = null;
        selectedMissionIndex = -1;
        isVotingActive = false;
        votingTimer = 0f;

        if (timerText != null)
            timerText.text = "";
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
