using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Assets.codes.Network.Messages;

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

            MeteoriteTypeDefinition typeDef = difficultyScaler.SelectMeteoriteType(
                smallMeteoriteDef, mediumMeteoriteDef, largeMeteoriteDef);

            if (typeDef == null) continue;

            Vector3 directionToOrigin = (-pos).normalized;

            activeWarnings.Add(new ActiveWarning
            {
                spawnPosition = pos,
                direction = directionToOrigin,
                remainingTime = spawnConfig.warningTime,
                meteoriteTypeKey = typeDef.typeName
            });

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

            float scale = typeDef.GetRandomScale();
            obj.transform.localScale = Vector3.one * scale;

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Random.insideUnitSphere * 2f;
            }
        }

        activeMeteorites[obj] = poolKey;
        TotalSpawned++;

        BroadcastMeteoriteSpawn(obj, position, direction, typeDef);

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

            BroadcastMeteoriteDestroy(obj);

            meteoritePool.Return(obj, poolKey);
        }
    }


    private Vector3 GetClusterCenter()
    {
        float distance = Random.Range(spawnConfig.spawnDistanceMin, spawnConfig.spawnDistanceMax);
        Vector3 direction = Random.onUnitSphere;
        return direction * distance; // Relative to origin (ship at 0,0,0)
    }

    private List<Vector3> GenerateClusterPositions(Vector3 center, int count, float radius, float gapRadius)
    {
        List<Vector3> positions = new List<Vector3>(count);
        Vector3 towardOrigin = (-center).normalized; 

        int attempts = 0;
        int maxAttempts = count * 3;

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
            Vector3 projected = towardOrigin * alongAxis;
            float perpendicularDist = (toCandidate - projected).magnitude;

            if (alongAxis > 0f && perpendicularDist < gapRadius)
                continue;

            positions.Add(candidate);
        }

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

    private float GaussianRandom()
    {
        float u1 = 1f - Random.value; // Avoid log(0)
        float u2 = Random.value;
        return Mathf.Sqrt(-2f * Mathf.Log(Mathf.Max(u1, 0.0001f))) * Mathf.Sin(2f * Mathf.PI * u2);
    }


    private IEnumerator CheckMeteoriteDistance(GameObject meteorite)
    {
        while (meteorite != null && activeMeteorites.ContainsKey(meteorite))
        {
            float distance = meteorite.transform.position.magnitude; // Distance from origin

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

    public void BroadcastWarning(Vector3 direction, float duration, int warningID)
    {
        if (!NetworkSystem.Instance.IsOnline || !NetworkSystem.Instance.IsServer) return;

        var msg = new Assets.codes.Network.Messages.NMS_Server_MeteoriteWarning(direction, duration, warningID);
        NetworkRouter.Instance.DistributeMessageToReady(msg);
    }


    private void OnDrawGizmosSelected()
    {
        if (spawnConfig == null) return;

        Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
        Gizmos.DrawWireSphere(Vector3.zero, spawnConfig.spawnDistanceMin);
        Gizmos.DrawWireSphere(Vector3.zero, spawnConfig.spawnDistanceMax);

        Vector3 sampleCenter = Vector3.forward * ((spawnConfig.spawnDistanceMin + spawnConfig.spawnDistanceMax) * 0.5f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(sampleCenter, spawnConfig.clusterRadius);

        Gizmos.color = Color.green;
        Vector3 towardOrigin = (-sampleCenter).normalized;
        Gizmos.DrawRay(sampleCenter, towardOrigin * spawnConfig.clusterRadius);
        Gizmos.DrawWireSphere(sampleCenter + towardOrigin * (spawnConfig.clusterRadius * 0.5f), spawnConfig.dodgeGapRadius);
    }
}

