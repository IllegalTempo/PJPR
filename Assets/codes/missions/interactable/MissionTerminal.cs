using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class MissionTerminal : Selectable
{
    [SerializeField] private MissionProjectionDisplay projectionDisplay;
    [SerializeField] private int missionsToShow = 3;
    private NetworkObject networkObject;
    private bool projectionsActive = false;

    protected override int Layer => 6; // Selectable layer

    protected override void OnEnable()
    {
        base.OnEnable();
        networkObject = GetComponent<NetworkObject>();
    }

    public override void OnClicked()
    {
        base.OnClicked();
        
        if (!projectionsActive)
        {
            ShowMissions();
        }
        else
        {
            HideMissions();
        }
    }

    private void ShowMissions()
    {
        if (projectionDisplay == null)
        {
            Debug.LogError("MissionTerminal: No projectionDisplay assigned!");
            return;
        }

        // Get random missions from manager
        Mission[] missions = MissionManager.Instance.GetRandomMissions(missionsToShow);
        
        if (missions.Length == 0)
        {
            Debug.LogError("MissionTerminal: No missions available!");
            return;
        }

        projectionDisplay.ShowMissions(missions);
        projectionsActive = true;

        // TODO: If networked, broadcast to all players
        // BroadcastMissionDisplay(missions);
    }

    private void HideMissions()
    {
        if (projectionDisplay != null)
            projectionDisplay.ClearMissions();

        projectionsActive = false;
    }

    /// <summary>
    /// Accept the selected mission (call this from UI button or after selection)
    /// </summary>
    public void AcceptSelectedMission()
    {
        Mission selectedMission = projectionDisplay.GetSelectedMission();
        if (selectedMission != null)
        {
            MissionManager.Instance.AcceptMission(selectedMission);
            HideMissions();

            // TODO: If networked, send to server
            // SendMissionAcceptanceToServer(projectionDisplay.GetSelectedMissionIndex());
        }
        else
        {
            Debug.LogWarning("No mission selected!");
        }
    }

    private void OnDestroy()
    {
        if (projectionsActive && projectionDisplay != null)
            projectionDisplay.ClearMissions();
    }
}
