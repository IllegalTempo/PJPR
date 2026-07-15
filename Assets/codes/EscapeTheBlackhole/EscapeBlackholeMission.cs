using UnityEngine;
using UnityEngine.InputSystem;
using Assets.codes.Network.Messages;

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

    public bool IsMissionActive { get; private set; }
    public float ElapsedTime => difficultyScaler != null ? difficultyScaler.ElapsedTime : 0f;

    public bool HasWon { get; private set; }

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
        if (meteoritePool != null && warningIndicatorPrefab != null)
        {
            meteoritePool.RegisterPool("Warning", warningIndicatorPrefab, 10);
        }
    }

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

        if (meteoriteSpawner != null) meteoriteSpawner.StopMission();
        if (difficultyScaler != null) difficultyScaler.StopScaling();

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
        if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame && !IsMissionActive)
        {
            StartMission();
        }

        if (!IsMissionActive) return;
        if (!NetworkSystem.Instance.IsWorldManager) return;

        if (ElapsedTime >= effectiveTimeLimit)
        {
            EndMission(true);
        }

        if (difficultyScaler != null && difficultyScaler.IsTimeUp)
        {
            EndMission(true);
        }

        // Win by surviving all waves (when maxWaves > 0)
        if (meteoriteSpawner != null && meteoriteSpawner.AllWavesCompleted)
        {
            EndMission(true);
        }
    }

    /// Called by MissionManager when "Escape the Blackhole" wins the vote.
    /// Match by mission name. (change after ig)
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
