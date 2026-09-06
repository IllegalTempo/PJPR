using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Peak Of Energy mission lifecycle wrapper (mirrors EscapeBlackholeMission).
///
/// Holds the mission identity (matched against MissionData.missionName during voting),
/// starts/stops the <see cref="PeakOfEnergyManager"/>, and ends the mission on
/// Victory / GameOver events.
///
/// The enemy spawner is intentionally NOT referenced here: it subscribes to the
/// manager's UnityEvents itself, so this wrapper stays decoupled.
/// </summary>
public class PeakOfEnergyMission : MonoBehaviour
{
    public static PeakOfEnergyMission Instance { get; private set; }

    [Header("Mission Identity")]
    [Tooltip("Must match the missionName in your MissionData asset, e.g. 'Peak of Energy'.")]
    [SerializeField] private string missionName = "Peak of Energy";

    [Header("Dependencies")]
    [Tooltip("The PeakOfEnergyManager on the mission's ring device. Auto-found if left empty.")]
    [SerializeField] private PeakOfEnergyManager missionManager;

    public bool IsMissionActive { get; private set; }
    public bool HasWon { get; private set; }

    private PeakOfEnergyManager Manager =>
        missionManager != null ? missionManager : (missionManager = FindFirstObjectByType<PeakOfEnergyManager>());

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (Manager != null)
        {
            Manager.OnVictory.AddListener(HandleVictory);
            Manager.OnGameOver.AddListener(HandleGameOver);
        }
    }

    private void OnDisable()
    {
        if (missionManager != null)
        {
            missionManager.OnVictory.RemoveListener(HandleVictory);
            missionManager.OnGameOver.RemoveListener(HandleGameOver);
        }
    }

    [ContextMenu("Start Mission")]
    public void StartMission()
    {
        if (IsMissionActive)
        {
            Debug.LogWarning("[PeakOfEnergyMission] Mission is already active.");
            return;
        }

        if (Manager == null)
        {
            Debug.LogError("[PeakOfEnergyMission] No PeakOfEnergyManager found. Add the mission ring prefab to the scene.");
            return;
        }

        IsMissionActive = true;
        HasWon = false;
        Manager.StartMission();
        Debug.Log($"[PeakOfEnergyMission] '{missionName}' started! Spin the ring to {Manager.GetTargetMin():F0}-{Manager.GetTargetMax():F0} RPM, then hold the center button.");
    }

    [ContextMenu("End Mission (Win)")]
    public void DebugEndMissionWin()
    {
        EndMission(true);
    }

    [ContextMenu("End Mission (Lose)")]
    public void DebugEndMissionLose()
    {
        EndMission(false);
    }

    public void EndMission(bool won)
    {
        if (!IsMissionActive) return;

        IsMissionActive = false;
        HasWon = won;
        Manager?.ResetToIdle();

        if (won)
        {
            Debug.Log($"[PeakOfEnergyMission] '{missionName}' COMPLETED! The Peak of Energy is charged!");
        }
        else
        {
            Debug.Log($"[PeakOfEnergyMission] '{missionName}' FAILED! The ring was destroyed.");
        }

        // TODO: Notify MissionManager / reward system, same as EscapeBlackholeMission.
    }

    private void HandleVictory()
    {
        EndMission(true);
    }

    private void HandleGameOver()
    {
        EndMission(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f6Key.wasPressedThisFrame && !IsMissionActive)
        {
            StartMission();
        }
    }

    /// <summary>Called by MissionManager when "Peak of Energy" wins the vote. Matched by mission name.</summary>
    public static void OnMissionVoteWon(string winningMissionName)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"[PeakOfEnergyMission] '{winningMissionName}' won the vote, but there is NO PeakOfEnergyMission object in the scene, so the mission cannot start. Add an empty GameObject with the PeakOfEnergyMission component (guide Step 4).");
            return;
        }

        if (Instance.missionName != winningMissionName)
        {
            Debug.LogWarning($"[PeakOfEnergyMission] The vote was won by '{winningMissionName}', but this mission object is named '{Instance.missionName}'. Open your MissionData asset and set its 'Mission Name' field to exactly '{Instance.missionName}' (case-sensitive), then vote again.");
            return;
        }

        Instance.StartMission();
    }

    private void OnDestroy()
    {
        if (IsMissionActive)
        {
            EndMission(false);
        }

        if (Instance == this)
            Instance = null;
    }
}
