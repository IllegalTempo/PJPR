using UnityEngine;

[CreateAssetMenu(fileName = "Mission", menuName = "Missions/Mission")]
public class MissionData : ScriptableObject
{
    public string missionName = "Unnamed Mission";
    public string missionDescription = "No description provided";
    public int rewardCredits = 100;
    [Range(0, 10)] public float difficulty = 5f;
    public int estimatedDuration = 10; // minutes

    public Mission ToMission()
    {
        return new Mission(missionName, missionDescription, rewardCredits, difficulty, estimatedDuration);
    }
}
