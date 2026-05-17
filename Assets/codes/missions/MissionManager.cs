using UnityEngine;
using System.Collections.Generic;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [SerializeField] private MissionData[] availableMissions;
    private List<Mission> activeMissions = new List<Mission>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
