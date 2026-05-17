using UnityEngine;

public class MissionProjectionDisplay : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private GameObject missionProjectionPrefab;

    [Header("Layout Settings")]
    [SerializeField] private float horizontalSpacing = 3f;
    [SerializeField] private float verticalOffset = 2f;
    [SerializeField] private float projectionScale = 1f;

    private MissionProjection[] activeProjections = new MissionProjection[3];
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

    /// <summary>
    /// Display missions as 3D projections in the world
    /// </summary>
    public void ShowMissions(Mission[] missions)
    {
        // Clear previous projections
        ClearMissions();

        if (missions == null || missions.Length == 0)
        {
            Debug.LogWarning("No missions provided to display");
            return;
        }

        int missionCount = Mathf.Min(missions.Length, 3);

        // Layout cards in an arc/row formation
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
                projection.Initialize(missions[i], i, OnMissionSelected);
                activeProjections[i] = projection;
            }
        }
    }

    private void OnMissionSelected(int missionIndex)
    {
        selectedMissionIndex = missionIndex;

        // Update highlight states
        for (int i = 0; i < activeProjections.Length; i++)
        {
            if (activeProjections[i] != null)
                activeProjections[i].SetHighlight(i == missionIndex);
        }

        Debug.Log($"Mission selected: Index {missionIndex}");
    }

    public void ClearMissions()
    {
        foreach (var projection in activeProjections)
        {
            if (projection != null)
                Destroy(projection.gameObject);
        }

        System.Array.Clear(activeProjections, 0, activeProjections.Length);
        selectedMissionIndex = -1;
    }

    public int GetSelectedMissionIndex()
    {
        return selectedMissionIndex;
    }

    public Mission GetSelectedMission()
    {
        if (selectedMissionIndex >= 0 && selectedMissionIndex < activeProjections.Length)
        {
            if (activeProjections[selectedMissionIndex] != null)
                return activeProjections[selectedMissionIndex].GetMission();
        }
        return null;
    }
}
