using UnityEngine;

public class DifficultyScaler : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private MeteoriteSpawnConfig spawnConfig;

    public float ElapsedTime { get; private set; }

    public float NormalizedTime
    {
        get
        {
            if (spawnConfig == null || spawnConfig.missionTimeLimit <= 0f)
                return 0f;
            return Mathf.Clamp01(ElapsedTime / spawnConfig.missionTimeLimit);
        }
    }

    public float CurrentDifficultyMultiplier
    {
        get
        {
            if (spawnConfig == null) return 1f;
            return Mathf.Max(0.1f, spawnConfig.difficultyCurve.Evaluate(ElapsedTime));
        }
    }

    public float CurrentSpawnInterval
    {
        get
        {
            if (spawnConfig == null) return 8f;
            return spawnConfig.baseSpawnInterval / CurrentDifficultyMultiplier;
        }
    }

    public float CurrentSpeedMultiplier => CurrentDifficultyMultiplier;

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

    public void StartScaling()
    {
        isRunning = true;
        ElapsedTime = 0f;
    }

    public void StopScaling()
    {
        isRunning = false;
    }

    public void Reset()
    {
        isRunning = false;
        ElapsedTime = 0f;
    }

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

    public float GetScaledSpeed()
    {
        if (spawnConfig == null) return 10f;
        float baseSpeed = Random.Range(spawnConfig.baseSpeedRange.x, spawnConfig.baseSpeedRange.y);
        return baseSpeed * CurrentSpeedMultiplier;
    }
}
