using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Assets.codes.Network.Messages;

/// <summary>
/// Revamped meteorite spawner for "Escape the Blackhole" mission.
/// Uses MeteoriteSpawnConfig SO, DifficultyScaler, MeteoritePool, and
/// cluster-based spawning with dodge gaps.
/// Server-authoritative via IsWorldManager guard.
/// </summary>
public class MeteoriteSpawner : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private MeteoriteSpawnConfig spawnConfig;
    [SerializeField] private MeteoriteTypeDefinition smallMeteoriteDef;
    [SerializeField] private MeteoriteTypeDefinition mediumMeteoriteDef;
    [SerializeField] private MeteoriteTypeDefinition largeMeteoriteDef;
    [SerializeField] private GameObject smallMeteoritePrefab;
    [SerializeField] private GameObject mediumMeteoritePrefab;
    [SerializeField] private GameObject largeMeteoritePrefab;

    [Header("Dependencies")]
    [SerializeField] private MeteoritePool meteoritePool;
    [SerializeField] private DifficultyScaler difficultyScaler;

    /// <summary>Tracked active meteorites with their pool keys.</summary>
    private readonly Dictionary<GameObject, string> activeMeteorites = new();
    /// <summary>Currently active spawn warnings.</summary>
    private readonly List<ActiveWarning> activeWarnings = new();
    private Coroutine spawnLoopCoroutine;
    private bool isMissionActive = false;

    /// <summary>Total meteorites spawned this mission (for stats).</summary>
    public int TotalSpawned { get; private set; }

    private struct ActiveWarning
    {
        public Vector3 spawnPosition;
        public Vector3 direction;
        public float remainingTime;
        public string meteoriteTypeKey;
    }

    void Start()
    {
        if (!NetworkSystem.Instance.IsWorldManager)
        {
            enabled = false;
            return;
        }

        // Validate dependencies
        if (spawnConfig == null)
            Debug.LogError("[MeteoriteSpawner] No MeteoriteSpawnConfig assigned!");
        if (meteoritePool == null)
            Debug.LogError("[MeteoriteSpawner] No MeteoritePool assigned!");
        if (difficultyScaler == null)
            Debug.LogError("[MeteoriteSpawner] No DifficultyScaler assigned!");
    }

    /// <summary>
    /// Register pools and start the mission spawn loop.
    /// Called by EscapeBlackholeMission.
    /// </summary>
    public void StartMission()
    {
        if (!NetworkSystem.Instance.IsWorldManager) return;

        StopMission(); // Ensure clean state

        RegisterPools();
        difficultyScaler.StartScaling();
        isMissionActive = true;
        TotalSpawned = 0;

        spawnLoopCoroutine = StartCoroutine(SpawnLoop());
        Debug.Log("[MeteoriteSpawner] Mission started.");
    }

    /// <summary>
    /// Stop the spawn loop and return all active meteorites to pool.
    /// Called by EscapeBlackholeMission.
    /// </summary>
    public void StopMission()
    {
        isMissionActive = false;

        if (spawnLoopCoroutine != null)
        {
            StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = null;
        }

        difficultyScaler?.StopScaling();
        ReturnAllToPool();
        activeWarnings.Clear();
        Debug.Log("[MeteoriteSpawner] Mission stopped.");
    }

    private void RegisterPools()
    {
        if (meteoritePool == null) return;

        if (smallMeteoriteDef != null && smallMeteoritePrefab != null)
            meteoritePool.RegisterPool(smallMeteoriteDef.typeName, smallMeteoritePrefab, smallMeteoriteDef.poolSize);

        if (mediumMeteoriteDef != null && mediumMeteoritePrefab != null)
            meteoritePool.RegisterPool(mediumMeteoriteDef.typeName, mediumMeteoritePrefab, mediumMeteoriteDef.poolSize);

        if (largeMeteoriteDef != null && largeMeteoritePrefab != null)
            meteoritePool.RegisterPool(largeMeteoriteDef.typeName, largeMeteoritePrefab, largeMeteoriteDef.poolSize);
    }

    private IEnumerator SpawnLoop()
    {
        while (isMissionActive)
        {
            if (difficultyScaler.IsTimeUp)
            {
                Debug.Log("[MeteoriteSpawner] Time limit reached. Ending spawn loop.");
                yield break;
            }

            float interval = difficultyScaler.CurrentSpawnInterval;
            yield return new WaitForSeconds(interval);

            if (!isMissionActive) yield break;

            // Spawn clusters
            int clusters = spawnConfig.clustersPerWave;
            for (int c = 0; c < clusters; c++)
            {
                if (!isMissionActive) yield break;
                if (activeMeteorites.Count >= spawnConfig.maxActiveMeteorites)
                {
                    Debug.Log("[MeteoriteSpawner] Max active meteorites reached. Skipping cluster.");
                    break;
                }

                SpawnCluster();
            }
        }
    }

    private void SpawnCluster()
    {
        Vector3 clusterCenter = GetClusterCenter();
        int count = spawnConfig.meteoritesPerCluster;
        float radius = spawnConfig.clusterRadius;
        float gapRadius = spawnConfig.dodgeGapRadius;

        List<Vector3> positions = GenerateClusterPositions(clusterCenter, count, radius, gapRadius);

        foreach (Vector3 pos in positions)
        {
            if (activeMeteorites.Count >= spawnConfig.maxActiveMeteorites)
                break;

            // Select type based on difficulty
            MeteoriteTypeDefinition typeDef = difficultyScaler.SelectMeteoriteType(
                smallMeteoriteDef, mediumMeteoriteDef, largeMeteoriteDef);

            if (typeDef == null) continue;

            // Calculate direction toward origin (ship position)
            Vector3 directionToOrigin = (-pos).normalized;

            // Queue warning (3s before spawn)
            activeWarnings.Add(new ActiveWarning
            {
                spawnPosition = pos,
                direction = directionToOrigin,
                remainingTime = spawnConfig.warningTime,
                meteoriteTypeKey = typeDef.typeName
            });

            // Broadcast warning to clients
            BroadcastWarning(directionToOrigin, spawnConfig.warningTime, activeWarnings.Count);

            // Spawn after warning delay
            StartCoroutine(SpawnAfterWarning(pos, directionToOrigin, typeDef));
        }
    }

    private IEnumerator SpawnAfterWarning(Vector3 position, Vector3 direction, MeteoriteTypeDefinition typeDef)
    {
        yield return new WaitForSeconds(spawnConfig.warningTime);

        if (!isMissionActive) yield break;

        SpawnSingleMeteorite(position, direction, typeDef);
    }

    private void SpawnSingleMeteorite(Vector3 position, Vector3 direction, MeteoriteTypeDefinition typeDef)
    {
        if (meteoritePool == null || typeDef == null) return;

        string poolKey = typeDef.typeName;
        Quaternion rotation = Quaternion.LookRotation(direction);

        GameObject obj = meteoritePool.Get(poolKey, position, rotation);
        if (obj == null)
        {
            Debug.LogWarning($"[MeteoriteSpawner] Failed to get '{poolKey}' from pool.");
            return;
        }

        // Configure meteorite
        Meteorite meteorite = obj.GetComponent<Meteorite>();
        if (meteorite != null)
        {
            meteorite.ConfigureFromDefinition(typeDef);
            meteorite.poolKey = poolKey;
            meteorite.onReturnToPool = OnMeteoriteReturnToPool;

            // Apply scale
            float scale = typeDef.GetRandomScale();
            obj.transform.localScale = Vector3.one * scale;

            // Meteorites stay at their spawn positions (static obstacles).
            // Only apply tumbling rotation — no linear velocity.
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Random.insideUnitSphere * 2f;
            }
        }

        activeMeteorites[obj] = poolKey;
        TotalSpawned++;

        // Network: if online, broadcast spawn to clients (Phase 5)
        BroadcastMeteoriteSpawn(obj, position, direction, typeDef);

        // Start distance checker
        StartCoroutine(CheckMeteoriteDistance(obj));
    }

    private void OnMeteoriteReturnToPool(Meteorite meteorite)
    {
        if (meteorite == null) return;
        GameObject obj = meteorite.gameObject;

        if (activeMeteorites.TryGetValue(obj, out string poolKey))
        {
            activeMeteorites.Remove(obj);
            meteorite.onReturnToPool = null;

            // Network: broadcast destroy to clients (Phase 5)
            BroadcastMeteoriteDestroy(obj);

            meteoritePool.Return(obj, poolKey);
        }
    }

    // ---- Cluster position generation ----

    private Vector3 GetClusterCenter()
    {
        float distance = Random.Range(spawnConfig.spawnDistanceMin, spawnConfig.spawnDistanceMax);
        Vector3 direction = Random.onUnitSphere;
        return direction * distance; // Relative to origin (ship at 0,0,0)
    }

    /// <summary>
    /// Generates meteorite positions in a 3D Gaussian cluster around the center,
    /// with a cylindrical gap oriented toward the origin (ship) for dodging.
    /// </summary>
    private List<Vector3> GenerateClusterPositions(Vector3 center, int count, float radius, float gapRadius)
    {
        List<Vector3> positions = new List<Vector3>(count);
        Vector3 towardOrigin = (-center).normalized; // Direction from cluster center to ship

        int attempts = 0;
        int maxAttempts = count * 3;

        while (positions.Count < count && attempts < maxAttempts)
        {
            attempts++;

            // 3D Gaussian-like distribution (Box-Muller in 3D)
            Vector3 offset = new Vector3(
                GaussianRandom() * radius,
                GaussianRandom() * radius,
                GaussianRandom() * radius
            );

            Vector3 candidate = center + offset;

            // Check distance from origin
            float distFromOrigin = candidate.magnitude;
            if (distFromOrigin < spawnConfig.spawnDistanceMin * 0.5f)
                continue;

            // Check gap: measure perpendicular distance from candidate to the toward-origin axis
            Vector3 toCandidate = candidate - center;
            float alongAxis = Vector3.Dot(toCandidate, towardOrigin);
            Vector3 projected = towardOrigin * alongAxis;
            float perpendicularDist = (toCandidate - projected).magnitude;

            // If the point is in front of the center AND within the gap cylinder, skip
            if (alongAxis > 0f && perpendicularDist < gapRadius)
                continue;

            positions.Add(candidate);
        }

        // If we didn't get enough valid positions, reduce gap and retry
        if (positions.Count < Mathf.CeilToInt(count * spawnConfig.minValidPositionFraction))
        {
            float reducedGap = gapRadius * 0.5f;
            attempts = 0;
            while (positions.Count < count && attempts < maxAttempts)
            {
                attempts++;
                Vector3 offset = new Vector3(
                    GaussianRandom() * radius,
                    GaussianRandom() * radius,
                    GaussianRandom() * radius
                );
                Vector3 candidate = center + offset;

                float distFromOrigin = candidate.magnitude;
                if (distFromOrigin < spawnConfig.spawnDistanceMin * 0.5f)
                    continue;

                Vector3 toCandidate = candidate - center;
                float alongAxis = Vector3.Dot(toCandidate, towardOrigin);
                float perpendicularDist = (toCandidate - towardOrigin * alongAxis).magnitude;

                if (alongAxis > 0f && perpendicularDist < reducedGap)
                    continue;

                positions.Add(candidate);
            }
        }

        return positions;
    }

    /// <summary>Box-Muller Gaussian random (mean 0, stddev 1).</summary>
    private float GaussianRandom()
    {
        float u1 = 1f - Random.value; // Avoid log(0)
        float u2 = Random.value;
        return Mathf.Sqrt(-2f * Mathf.Log(Mathf.Max(u1, 0.0001f))) * Mathf.Sin(2f * Mathf.PI * u2);
    }

    // ---- Distance cleanup ----

    private IEnumerator CheckMeteoriteDistance(GameObject meteorite)
    {
        while (meteorite != null && activeMeteorites.ContainsKey(meteorite))
        {
            float distance = meteorite.transform.position.magnitude; // Distance from origin

            // Despawn once the ship has passed the meteorite (it's now close behind the ship).
            // Since meteorites are static and the world scrolls via WorldReference, a meteorite
            // that started far away will eventually reach the origin as the world moves.
            if (distance < spawnConfig.spawnDistanceMin * 0.15f)
            {
                Meteorite m = meteorite.GetComponent<Meteorite>();
                if (m != null)
                {
                    OnMeteoriteReturnToPool(m);
                }
                else
                {
                    if (activeMeteorites.TryGetValue(meteorite, out string pk))
                    {
                        activeMeteorites.Remove(meteorite);
                        meteoritePool.Return(meteorite, pk);
                    }
                }
                yield break;
            }

            yield return new WaitForSeconds(2f);
        }
    }

    private void ReturnAllToPool()
    {
        foreach (var kvp in new Dictionary<GameObject, string>(activeMeteorites))
        {
            GameObject obj = kvp.Key;
            string poolKey = kvp.Value;

            if (obj != null)
            {
                Meteorite m = obj.GetComponent<Meteorite>();
                if (m != null)
                    m.onReturnToPool = null;

                BroadcastMeteoriteDestroy(obj);
                meteoritePool.Return(obj, poolKey);
            }
        }

        activeMeteorites.Clear();
    }

    // ---- Network (Phase 5) ----

    private int nextSpawnID = 0;

    private void BroadcastMeteoriteSpawn(GameObject obj, Vector3 position, Vector3 direction, MeteoriteTypeDefinition typeDef)
    {
        if (!NetworkSystem.Instance.IsOnline || !NetworkSystem.Instance.IsServer) return;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        Vector3 velocity = Vector3.zero; // Meteorites are static — no linear movement
        Vector3 angularVelocity = rb != null ? rb.angularVelocity : Vector3.zero;
        float scale = obj.transform.localScale.x;
        int spawnID = ++nextSpawnID;

        var msg = new Assets.codes.Network.Messages.NMS_Server_SpawnMeteorite(
            typeDef.typeName, position, velocity, angularVelocity, scale, spawnID);
        NetworkRouter.Instance.DistributeMessageToReady(msg);
    }

    private void BroadcastMeteoriteDestroy(GameObject obj)
    {
        if (!NetworkSystem.Instance.IsOnline || !NetworkSystem.Instance.IsServer) return;

        Meteorite m = obj.GetComponent<Meteorite>();
        string poolKey = m != null ? m.poolKey : "Small";

        var msg = new Assets.codes.Network.Messages.NMS_Server_DestroyMeteorite(poolKey, 0);
        NetworkRouter.Instance.DistributeMessageToReady(msg);
    }

    /// <summary>
    /// Broadcast a warning indicator to all clients.
    /// </summary>
    public void BroadcastWarning(Vector3 direction, float duration, int warningID)
    {
        if (!NetworkSystem.Instance.IsOnline || !NetworkSystem.Instance.IsServer) return;

        var msg = new Assets.codes.Network.Messages.NMS_Server_MeteoriteWarning(direction, duration, warningID);
        NetworkRouter.Instance.DistributeMessageToReady(msg);
    }

    // ---- Editor visualization ----

    private void OnDrawGizmosSelected()
    {
        if (spawnConfig == null) return;

        // Draw spawn sphere bounds
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
        Gizmos.DrawWireSphere(Vector3.zero, spawnConfig.spawnDistanceMin);
        Gizmos.DrawWireSphere(Vector3.zero, spawnConfig.spawnDistanceMax);

        // Draw a sample cluster
        Vector3 sampleCenter = Vector3.forward * ((spawnConfig.spawnDistanceMin + spawnConfig.spawnDistanceMax) * 0.5f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(sampleCenter, spawnConfig.clusterRadius);

        // Draw gap cylinder direction
        Gizmos.color = Color.green;
        Vector3 towardOrigin = (-sampleCenter).normalized;
        Gizmos.DrawRay(sampleCenter, towardOrigin * spawnConfig.clusterRadius);
        Gizmos.DrawWireSphere(sampleCenter + towardOrigin * (spawnConfig.clusterRadius * 0.5f), spawnConfig.dodgeGapRadius);
    }
}

