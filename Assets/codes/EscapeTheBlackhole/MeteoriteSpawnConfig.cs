using UnityEngine;

[CreateAssetMenu(fileName = "MeteoriteSpawnConfig", menuName = "Escape Blackhole/Spawn Config")]
public class MeteoriteSpawnConfig : ScriptableObject
{
    [Header("Spawn Distances")]
    [Tooltip("Minimum distance from origin (ship) to spawn meteorites")]
    [Min(10f)]
    public float spawnDistanceMin = 80f;

    [Tooltip("Maximum distance from origin (ship) to spawn meteorites")]
    [Min(20f)]
    public float spawnDistanceMax = 150f;

    [Header("Difficulty Scaling")]
    [Tooltip("X = elapsed seconds since mission start | Y = difficulty multiplier applied to speed/rate")]
    public AnimationCurve difficultyCurve = AnimationCurve.Linear(0f, 1f, 300f, 3f);

    [Tooltip("X = normalized mission time (0-1) | Y = probability weight for Small meteorites")]
    public AnimationCurve smallTypeWeight = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);

    [Tooltip("X = normalized mission time (0-1) | Y = probability weight for Medium meteorites")]
    public AnimationCurve mediumTypeWeight = AnimationCurve.Linear(0f, 0f, 1f, 0.5f);

    [Tooltip("X = normalized mission time (0-1) | Y = probability weight for Large meteorites")]
    public AnimationCurve largeTypeWeight = AnimationCurve.Linear(0f, 0f, 1f, 0.3f);

    [Header("Cluster Settings")]
    [Tooltip("How many clusters to spawn per wave")]
    [Min(1)]
    public int clustersPerWave = 3;

    [Tooltip("How many meteorites per cluster")]
    [Min(1)]
    public int meteoritesPerCluster = 8;

    [Tooltip("Radius of each cluster (meteorites spread around cluster center within this radius)")]
    [Min(1f)]
    public float clusterRadius = 15f;

    [Tooltip("Radius of the intentional gap/hole in each cluster for the ship to dodge through")]
    [Min(0f)]
    public float dodgeGapRadius = 8f;

    [Tooltip("Minimum fraction of cluster meteorites that must have valid positions (0-1). If too many are removed by the gap, the gap is reduced.")]
    [Range(0.3f, 0.95f)]
    public float minValidPositionFraction = 0.6f;

    [Header("Timing")]
    [Tooltip("Base interval between spawn waves (seconds) — scaled by difficulty curve")]
    [Min(1f)]
    public float baseSpawnInterval = 8f;

    [Tooltip("How long before meteorites arrive that the warning indicator appears (seconds)")]
    [Min(0.5f)]
    public float warningTime = 3f;

    [Header("Limits")]
    [Tooltip("Maximum concurrent active meteorites")]
    [Min(5)]
    public int maxActiveMeteorites = 50;

    [Tooltip("Maximum total mission time in seconds (0 = no limit)")]
    [Min(0f)]
    public float missionTimeLimit = 180f;

    [Header("Speed")]
    [Tooltip("Base speed range for meteorites moving toward origin (x = min, y = max)")]
    public Vector2 baseSpeedRange = new Vector2(8f, 16f);

    [Header("Wave System")]
    [Tooltip("Distance ahead of the ship to spawn each wave")]
    [Min(5f)]
    public float waveSpawnDistanceAhead = 100f;

    [Tooltip("How far past the ship a wave must be (ship Z > wave Z + threshold) to despawn")]
    [Min(1f)]
    public float wavePassedThreshold = 20f;

    [Tooltip("Speed at which waves slowly drift toward the ship (-Z, no X/Y)")]
    [Min(0f)]
    public float waveDriftSpeed = 2f;

    [Tooltip("Minimum Z distance between consecutive wave spawns")]
    [Min(10f)]
    public float minWaveSeparation = 100f;

    [Tooltip("Maximum number of waves for this mission (0 = unlimited, spawns until timer ends)")]
    [Min(0)]
    public int maxWaves = 0;
}
