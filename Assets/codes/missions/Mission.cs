using UnityEngine;

[System.Serializable]
public class Mission
{
    public string missionName;
    public string missionDescription;
    public int rewardCredits;
    public float difficulty; // 0-10 scale
    public int estimatedDuration; // in minutes

    public Mission(string name, string description, int reward, float diff, int duration)
    {
        missionName = name;
        missionDescription = description;
        rewardCredits = reward;
        difficulty = Mathf.Clamp01(diff / 10f); // Normalize to 0-1
        estimatedDuration = duration;
    }
}
