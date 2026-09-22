using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Assets.codes.Network.Messages;

[RequireComponent(typeof(BoxCollider))]
public class MissionProjection : Interactable
{
    [Header("Display Components")]
    [SerializeField] private TextMeshProUGUI missionNameText;
    [SerializeField] private TextMeshProUGUI missionDescriptionText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private TextMeshProUGUI durationText;
    [SerializeField] private TextMeshProUGUI voteCountText;

    [Header("Vote Progress")]
    [SerializeField] private Slider voteSlider;

    private Mission mission;
    private int missionIndex;


    void OnEnable()
    {

        // Ensure there's a collider for raycast detection
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        Selectable mySelectable = GetComponent<Selectable>();
        if (mySelectable == null)
        {
            mySelectable = gameObject.AddComponent<Selectable>();
        }
        if (mySelectable.usableOverride == null)
        {
            mySelectable.usableOverride = this;
        }

    }

    public void Initialize(Mission mission, int index)
    {
        this.mission = mission;
        this.missionIndex = index;

        UpdateDisplay();
        ShowVoteCount(0, 0);
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

    public override void OnInteract_press(PlayerMain who)
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

    public void ShowVoteCount(int count, int totalPlayers)
    {
        if (voteCountText != null)
        {
            int displayTotal = Mathf.Max(totalPlayers, 0);
            voteCountText.text = $"{count}/{displayTotal}";
        }

        if (voteSlider != null)
        {
            voteSlider.minValue = 0f;
            voteSlider.maxValue = 1f;
            voteSlider.value = totalPlayers > 0 ? Mathf.Clamp01((float)count / totalPlayers) : 0f;
        }
    }

    public void ShowAsWinner()
    {
    }

    public void ShowAsLoser()
    {
    }

    public Mission GetMission()
    {
        return mission;
    }

    public int GetMissionIndex()
    {
        return missionIndex;
    }

    public void ClearDisplay()
    {
        mission = null;
        missionIndex = -1;

        if (missionNameText != null)
            missionNameText.text = "";
        if (missionDescriptionText != null)
            missionDescriptionText.text = "";
        if (rewardText != null)
            rewardText.text = "";
        if (difficultyText != null)
            difficultyText.text = "";
        if (durationText != null)
            durationText.text = "";

        ShowVoteCount(0, 0);
    }
}
