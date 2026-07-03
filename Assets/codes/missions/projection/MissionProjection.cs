using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Assets.codes.Network.Messages;

[RequireComponent(typeof(BoxCollider))]
public class MissionProjection : Selectable, IUsable
{
    [Header("Display Components")]
    [SerializeField] private TextMeshProUGUI missionNameText;
    [SerializeField] private TextMeshProUGUI missionDescriptionText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private TextMeshProUGUI durationText;
    [SerializeField] private TextMeshProUGUI voteCountText;

    [Header("Vote Bar")]
    [SerializeField] private RectTransform voteBarContainer;
    [SerializeField] private Color voteFilledColor = Color.green;
    [SerializeField] private float boxSize = 8f;
    [SerializeField] private float boxSpacing = 3f;

    private int currentBoxCount = 0;

    [Header("Visual Settings")]
    [SerializeField] private Material projectionMaterial;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Color winnerColor = Color.green;
    [SerializeField] private Color loserColor = Color.red;

    private Mission mission;
    private int missionIndex;

    protected override int Layer => 6;

    protected override void OnEnable()
    {
        base.OnEnable();

        // Ensure there's a collider for raycast detection
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    public void Initialize(Mission mission, int index)
    {
        this.mission = mission;
        this.missionIndex = index;

        UpdateDisplay();
        ShowVoteCount(0); // Hide all vote boxes initially
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

    public void OnInteract(PlayerMain who)
    {
        if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline && !NetworkSystem.Instance.IsServer)
        {
            var msg = new NMS_Client_CastVote(missionIndex);
            NetworkRouter.Instance.SendMessageToServer(msg);
        }
        else
        {
            ulong steamId = who != null && who.networkinfo != null ? who.networkinfo.steamID : 0;
            MissionManager.Instance.CastVote(steamId, missionIndex);
        }

        // Tell the timer display which mission the local player voted for
        MissionProjectionDisplay.LocalPlayerVotedMissionName = mission.missionName;

        Debug.Log($"[MissionProjection] Voted for mission index {missionIndex}: {mission.missionName}");
    }

    public void ShowVoteCount(int count)
    {
        if (voteBarContainer == null)
            return;

        // Add boxes if needed
        while (currentBoxCount < count)
        {
            GameObject box = new GameObject("VoteBox", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            box.transform.SetParent(voteBarContainer, false);

            RectTransform rt = box.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(boxSize, boxSize);
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(currentBoxCount * (boxSize + boxSpacing), 0);

            box.GetComponent<UnityEngine.UI.Image>().color = voteFilledColor;
            currentBoxCount++;
        }

        // Remove excess boxes
        while (currentBoxCount > count)
        {
            Transform last = voteBarContainer.GetChild(voteBarContainer.childCount - 1);
            if (last != null)
                Destroy(last.gameObject);
            currentBoxCount--;
        }

        if (voteCountText != null)
            voteCountText.text = count > 0 ? $"{count} vote{(count != 1 ? "s" : "")}" : "";
    }

    public void ShowAsWinner()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        if (meshRenderer != null && meshRenderer.material != null)
            meshRenderer.material.color = winnerColor;
    }

    public void ShowAsLoser()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0.5f;
        if (meshRenderer != null && meshRenderer.material != null)
            meshRenderer.material.color = loserColor;
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
