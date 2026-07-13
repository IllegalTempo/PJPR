using UnityEngine;

/// <summary>
/// Tracks elapsed mission time and evaluates difficulty curves from MeteoriteSpawnConfig.
/// Provides scaled spawn parameters based on current difficulty.
/// </summary>
public class DifficultyScaler : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private MeteoriteSpawnConfig spawnConfig;

    /// <summary>Elapsed time since mission started (seconds).</summary>
    public float ElapsedTime { get; private set; }

    /// <summary>Normalized mission time (0-1), based on missionTimeLimit.</summary>
    public float NormalizedTime
    {
        get
        {
            if (spawnConfig == null || spawnConfig.missionTimeLimit <= 0f)
                return 0f;
            return Mathf.Clamp01(ElapsedTime / spawnConfig.missionTimeLimit);
        }
    }

    /// <summary>Current difficulty multiplier from the difficulty curve.</summary>
    public float CurrentDifficultyMultiplier
    {
        get
        {
            if (spawnConfig == null) return 1f;
            return Mathf.Max(0.1f, spawnConfig.difficultyCurve.Evaluate(ElapsedTime));
        }
    }

    /// <summary>Current spawn interval, scaled by difficulty.</summary>
    public float CurrentSpawnInterval
    {
        get
        {
            if (spawnConfig == null) return 8f;
            return spawnConfig.baseSpawnInterval / CurrentDifficultyMultiplier;
        }
    }

    /// <summary>Current speed multiplier for meteorite velocity.</summary>
    public float CurrentSpeedMultiplier => CurrentDifficultyMultiplier;

    /// <summary>Whether the mission time limit has been reached.</summary>
    public bool IsTimeUp
    {
        get
        {
            if (spawnConfig == null || spawnConfig.missionTimeLimit <= 0f)
                return false;
            return ElapsedTime >= spawnConfig.missionTimeLimit;
        }
    }

    private bool isRunning = false;

    private void Update()
    {
        if (isRunning)
        {
            ElapsedTime += Time.deltaTime;
        }
    }

    /// <summary>Start tracking time.</summary>
    public void StartScaling()
    {
        isRunning = true;
        ElapsedTime = 0f;
    }

    /// <summary>Stop tracking time.</summary>
    public void StopScaling()
    {
        isRunning = false;
    }

    /// <summary>Reset to initial state.</summary>
    public void Reset()
    {
        isRunning = false;
        ElapsedTime = 0f;
    }

    /// <summary>
    /// Selects a MeteoriteTypeDefinition based on current type probability curves.
    /// Uses weighted random selection from the three curves evaluated at normalized time.
    /// </summary>
    public MeteoriteTypeDefinition SelectMeteoriteType(MeteoriteTypeDefinition smallDef, MeteoriteTypeDefinition mediumDef, MeteoriteTypeDefinition largeDef)
    {
        if (spawnConfig == null)
            return smallDef;

        float t = NormalizedTime;

        float smallWeight = spawnConfig.smallTypeWeight.Evaluate(t);
        float mediumWeight = spawnConfig.mediumTypeWeight.Evaluate(t);
        float largeWeight = spawnConfig.largeTypeWeight.Evaluate(t);

        float totalWeight = smallWeight + mediumWeight + largeWeight;

        if (totalWeight <= 0f)
            return smallDef;

        float roll = Random.Range(0f, totalWeight);

        if (roll < smallWeight)
            return smallDef;
        if (roll < smallWeight + mediumWeight)
            return mediumDef;
        return largeDef;
    }

    /// <summary>
    /// Returns a scaled speed from the base speed range.
    /// </summary>
    public float GetScaledSpeed()
    {
        if (spawnConfig == null) return 10f;
        float baseSpeed = Random.Range(spawnConfig.baseSpeedRange.x, spawnConfig.baseSpeedRange.y);
        return baseSpeed * CurrentSpeedMultiplier;
    }
}
