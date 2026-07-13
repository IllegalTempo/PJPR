using UnityEngine;
using UnityEngine.InputSystem;
using Assets.codes.Network.Messages;

/// <summary>
/// Mission controller for "Escape the Blackhole".
/// Hooks into MissionManager to start/stop the meteorite spawning system.
/// Attach this to a GameObject in the scene and wire references.
/// </summary>
public class EscapeBlackholeMission : MonoBehaviour
{
    public static EscapeBlackholeMission Instance { get; private set; }

    [Header("Mission Identity")]
    [Tooltip("Must match the missionName in your MissionData asset")]
    [SerializeField] private string missionName = "Escape the Blackhole";

    [Header("Dependencies")]
    [SerializeField] private MeteoriteSpawner meteoriteSpawner;
    [SerializeField] private DifficultyScaler difficultyScaler;
    [SerializeField] private MeteoriteSpawnConfig spawnConfig;
    [SerializeField] private MeteoritePool meteoritePool;
    [SerializeField] private LootDropHandler lootDropHandler;
    [SerializeField] private GameObject warningIndicatorPrefab;

    [Header("Win Condition")]
    [Tooltip("Survive this many seconds to win (0 = use spawnConfig.missionTimeLimit)")]
    [SerializeField] private float survivalTime = 0f;

    /// <summary>Whether the mission is currently active.</summary>
    public bool IsMissionActive { get; private set; }

    /// <summary>Elapsed time since mission started.</summary>
    public float ElapsedTime => difficultyScaler != null ? difficultyScaler.ElapsedTime : 0f;

    /// <summary>Whether the mission has been won (survived the time limit).</summary>
    public bool HasWon { get; private set; }

    /// <summary>Total meteorites spawned this session.</summary>
    public int TotalMeteoritesSpawned => meteoriteSpawner != null ? meteoriteSpawner.TotalSpawned : 0;

    private float effectiveTimeLimit;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Register warning indicator pool
        if (meteoritePool != null && warningIndicatorPrefab != null)
        {
            meteoritePool.RegisterPool("Warning", warningIndicatorPrefab, 10);
        }
    }

    /// <summary>
    /// Start the Escape the Blackhole mission.
    /// Called when this mission wins the vote (or manually for testing).
    /// Right-click this component in the Inspector → "Start Mission" to test in Play Mode.
    /// </summary>
    [ContextMenu("Start Mission")]
    public void StartMission()
    {
        if (IsMissionActive)
        {
            Debug.LogWarning("[EscapeBlackholeMission] Mission is already active.");
            return;
        }

        effectiveTimeLimit = survivalTime > 0f ? survivalTime : (spawnConfig != null ? spawnConfig.missionTimeLimit : 180f);

        IsMissionActive = true;
        HasWon = false;

        difficultyScaler?.Reset();
        difficultyScaler?.StartScaling();

        meteoriteSpawner?.StartMission();

        Debug.Log($"[EscapeBlackholeMission] '{missionName}' started! Survive {effectiveTimeLimit}s.");
    }

    /// <summary>
    /// End the mission (win or lose).
    /// Right-click this component in the Inspector → "End Mission (Win)" or "End Mission (Lose)".
    /// </summary>
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

        meteoriteSpawner?.StopMission();
        difficultyScaler?.StopScaling();

        if (won)
        {
            Debug.Log($"[EscapeBlackholeMission] '{missionName}' COMPLETED! You survived!");
        }
        else
        {
            Debug.Log($"[EscapeBlackholeMission] '{missionName}' FAILED!");
        }

        // TODO: Notify MissionManager, give rewards, etc.
    }

    private void Update()
    {
        // Debug shortcut: press F5 to start the mission in Play Mode
        if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame && !IsMissionActive)
        {
            StartMission();
        }

        if (!IsMissionActive) return;
        if (!NetworkSystem.Instance.IsWorldManager) return;

        // Check win condition
        if (ElapsedTime >= effectiveTimeLimit)
        {
            EndMission(true);
        }

        // Check if time is up via difficulty scaler
        if (difficultyScaler != null && difficultyScaler.IsTimeUp)
        {
            EndMission(true);
        }
    }

    /// <summary>
    /// Called by MissionManager when "Escape the Blackhole" wins the vote.
    /// Match by mission name.
    /// </summary>
    public static void OnMissionVoteWon(string winningMissionName)
    {
        if (Instance != null && Instance.missionName == winningMissionName)
        {
            Instance.StartMission();
        }
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
