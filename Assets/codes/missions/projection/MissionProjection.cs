using UnityEngine;
using TMPro;

public class MissionProjection : MonoBehaviour
{
    [Header("Display Components")]
    [SerializeField] private TextMeshProUGUI missionNameText;
    [SerializeField] private TextMeshProUGUI missionDescriptionText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private TextMeshProUGUI durationText;

    [Header("Visual Settings")]
    [SerializeField] private Material projectionMaterial;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private CanvasGroup canvasGroup;

    private Mission mission;
    private int missionIndex;
    private System.Action<int> onSelectedCallback;

    public void Initialize(Mission mission, int index, System.Action<int> onSelected)
    {
        this.mission = mission;
        this.missionIndex = index;
        this.onSelectedCallback = onSelected;

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (mission == null) return;

        if (missionNameText != null)
            missionNameText.text = mission.missionName;

        if (missionDescriptionText != null)
            missionDescriptionText.text = mission.missionDescription;

        if (rewardText != null)
            rewardText.text = $"Reward: {mission.rewardCredits} Credits";

        if (difficultyText != null)
            difficultyText.text = $"Difficulty: {mission.difficulty * 10:F1}/10";

        if (durationText != null)
            durationText.text = $"Duration: ~{mission.estimatedDuration} min";
    }

    public void OnProjectionClicked()
    {
        // Highlight this projection
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        onSelectedCallback?.Invoke(missionIndex);
    }

    public void SetHighlight(bool highlighted)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = highlighted ? 1f : 0.7f;
    }

    public Mission GetMission()
    {
        return mission;
    }

    public int GetMissionIndex()
    {
        return missionIndex;
    }
}
