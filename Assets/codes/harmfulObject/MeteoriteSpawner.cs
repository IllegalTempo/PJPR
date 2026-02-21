using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class MeteoriteSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] meteoritePrefabs;
    [SerializeField] private string[] meteoritePrefabIDs;
    [SerializeField] private Transform spaceshipTarget;
    [SerializeField] private float spawnRadius = 50f;
    [SerializeField] private float minDistanceFromTarget = 20f;
    
    [Header("Spawn Rate")]
    [SerializeField] private float baseSpawnInterval = 3f;
    [SerializeField] private float spawnIntervalVariation = 1f;
    [SerializeField] private int maxMeteorites = 20;
    
    [Header("Wave System")]
    [SerializeField] private bool useWaveSystem = true;
    [SerializeField] private float waveDuration = 30f;
    [SerializeField] private float waveBreakTime = 10f;
    [SerializeField] private int meteoritesPerWave = 15;
    
    [Header("Meteorite Properties")]
    [SerializeField] private Vector2 sizeRange = new Vector2(0.5f, 2f);
    [SerializeField] private Vector2 speedRange = new Vector2(5f, 15f);
    [SerializeField] private float aimTowardTarget = 0.7f; // 0 = random 1 = always toward target
    
    [Header("Spawn Area Shape")]
    [SerializeField] private SpawnAreaType spawnAreaType = SpawnAreaType.Sphere;
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(50f, 30f, 50f);

    private List<GameObject> activeMeteorites = new List<GameObject>();
    private float nextSpawnTime;
    private bool isWaveActive = false;
    private int currentWaveMeteorites = 0;

    private bool IsServerAuthority
    {
        get
        {
            return NetworkSystem.INSTANCE == null || NetworkSystem.INSTANCE.IsServer;
        }
    }
    
    public enum SpawnAreaType
    {
        Sphere,
        Box,
        Ring
    }

    void Start()
    {
        if (!IsServerAuthority)
        {
            enabled = false;
            return;
        }

        // find spaceship if not assigned eh 
        if (spaceshipTarget == null)
        {
            GameObject spaceship = GameObject.FindGameObjectWithTag("Spaceship");
            if (spaceship != null)
            {
                spaceshipTarget = spaceship.transform;
            }
            else
            {
                spaceshipTarget = transform; // fallback
            }
        }

        nextSpawnTime = Time.time + baseSpawnInterval;

        if (useWaveSystem)
        {
            StartCoroutine(WaveController());
        }
    }

    void Update()
    {
        if (!IsServerAuthority)
        {
            return;
        }

        // clean
        activeMeteorites.RemoveAll(item => item == null);

        // keep spawning if not using wave system
        if (!useWaveSystem && Time.time >= nextSpawnTime && activeMeteorites.Count < maxMeteorites)
        {
            SpawnMeteorite().Forget();
            nextSpawnTime = Time.time + baseSpawnInterval + Random.Range(-spawnIntervalVariation, spawnIntervalVariation);
        }
    }

    IEnumerator WaveController()
    {
        while (true)
        {
            // start wave
            isWaveActive = true;
            currentWaveMeteorites = 0;
            Debug.Log("Meteorite wave starting!");

            float waveEndTime = Time.time + waveDuration;
            
            while (Time.time < waveEndTime && currentWaveMeteorites < meteoritesPerWave)
            {
                if (activeMeteorites.Count < maxMeteorites)
                {
                    SpawnMeteorite().Forget();
                    currentWaveMeteorites++;
                    
                    float waveSpawnInterval = waveDuration / meteoritesPerWave;
                    yield return new WaitForSeconds(waveSpawnInterval + Random.Range(-0.5f, 0.5f));
                }
                else
                {
                    yield return new WaitForSeconds(1f);
                }
            }

            // Wave break
            isWaveActive = false;
            Debug.Log($"Wave complete! Break for {waveBreakTime} seconds.");
            yield return new WaitForSeconds(waveBreakTime);
        }
    }

    async UniTask SpawnMeteorite()
    {
        if (!NetworkSystem.INSTANCE.IsServer) return;
        string prefabID = GetRandomMeteoritePrefabID();
        if (string.IsNullOrEmpty(prefabID))
        {
            Debug.LogWarning("No meteorite prefab IDs configured for network spawn!");
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition();
        NetworkObject networkObject = await NetworkSystem.INSTANCE.Server.CreateNetworkObject(prefabID, spawnPosition, Random.rotation,0);
        if (networkObject == null)
        {
            return;
        }

        GameObject meteorite = networkObject.gameObject;
        float scale = Random.Range(sizeRange.x, sizeRange.y);
        meteorite.transform.localScale = Vector3.one * scale;
        Rigidbody rb = meteorite.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 directionToTarget = (spaceshipTarget.position - spawnPosition).normalized;
            Vector3 randomDirection = Random.onUnitSphere;
            Vector3 finalDirection = Vector3.Lerp(randomDirection, directionToTarget, aimTowardTarget).normalized;
            
            float speed = Random.Range(speedRange.x, speedRange.y);
            rb.linearVelocity = finalDirection * speed;
            rb.angularVelocity = Random.insideUnitSphere * 2f;
        }

        activeMeteorites.Add(meteorite);
        // clean when too far
        StartCoroutine(CheckMeteoriteDistance(meteorite));
    }

    string GetRandomMeteoritePrefabID()
    {
        if (meteoritePrefabIDs != null && meteoritePrefabIDs.Length > 0)
        {
            List<string> validIDs = new List<string>();
            foreach (string prefabID in meteoritePrefabIDs)
            {
                if (!string.IsNullOrWhiteSpace(prefabID))
                {
                    validIDs.Add(prefabID);
                }
            }

            if (validIDs.Count > 0)
            {
                return validIDs[Random.Range(0, validIDs.Count)];
            }
        }

        if (meteoritePrefabs != null && meteoritePrefabs.Length > 0)
        {
            GameObject meteoritePrefab = meteoritePrefabs[Random.Range(0, meteoritePrefabs.Length)];
            if (meteoritePrefab != null)
            {
                foreach (KeyValuePair<string, string> prefabEntry in GameCore.INSTANCE.GetPrefabWithID)
                {
                    if (prefabEntry.Key == meteoritePrefab.name || prefabEntry.Value == meteoritePrefab.name)
                    {
                        return prefabEntry.Key;
                    }
                }
            }
        }

        if (GameCore.INSTANCE != null)
        {
            if (GameCore.INSTANCE.GetPrefabWithID.ContainsKey("Meteorite_Test"))
            {
                return "Meteorite_Test";
            }

            if (GameCore.INSTANCE.GetPrefabWithID.ContainsKey("Meteorite_Fragment"))
            {
                return "Meteorite_Fragment";
            }
        }

        return string.Empty;
    }

    Vector3 GetSpawnPosition()
    {
        Vector3 spawnPos = Vector3.zero;
        int maxAttempts = 10;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            switch (spawnAreaType)
            {
                case SpawnAreaType.Sphere:
                    spawnPos = spaceshipTarget.position + Random.onUnitSphere * spawnRadius;
                    break;
                    
                case SpawnAreaType.Box:
                    spawnPos = spaceshipTarget.position + new Vector3(
                        Random.Range(-spawnAreaSize.x, spawnAreaSize.x),
                        Random.Range(-spawnAreaSize.y, spawnAreaSize.y),
                        Random.Range(-spawnAreaSize.z, spawnAreaSize.z)
                    );
                    break;
                    
                case SpawnAreaType.Ring:
                    Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
                    spawnPos = spaceshipTarget.position + new Vector3(randomCircle.x, Random.Range(-10f, 10f), randomCircle.y);
                    break;
            }

            if (Vector3.Distance(spawnPos, spaceshipTarget.position) >= minDistanceFromTarget)
            {
                return spawnPos;
            }
        }
        
        return spawnPos;
    }

    IEnumerator CheckMeteoriteDistance(GameObject meteorite)
    {
        while (meteorite != null)
        {
            float distance = Vector3.Distance(meteorite.transform.position, spaceshipTarget.position);
            
            if (distance > spawnRadius * 3f)
            {
                activeMeteorites.Remove(meteorite);
                Destroy(meteorite);
                yield break;
            }
            
            yield return new WaitForSeconds(2f);
        }
    }

    public void StartSpawning()
    {
        enabled = true;
    }

    public void StopSpawning()
    {
        enabled = false;
    }

    public void ClearAllMeteorites()
    {
        foreach (GameObject meteorite in activeMeteorites)
        {
            if (meteorite != null)
            {
                Destroy(meteorite);
            }
        }
        activeMeteorites.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Transform target = spaceshipTarget != null ? spaceshipTarget : transform;

        Gizmos.color = Color.yellow;
        switch (spawnAreaType)
        {
            case SpawnAreaType.Sphere:
                Gizmos.DrawWireSphere(target.position, spawnRadius);
                break;
            case SpawnAreaType.Box:
                Gizmos.DrawWireCube(target.position, spawnAreaSize * 2f);
                break;
            case SpawnAreaType.Ring:
                int segments = 32;
                for (int i = 0; i < segments; i++)
                {
                    float angle1 = (i / (float)segments) * Mathf.PI * 2f;
                    float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
                    Vector3 p1 = target.position + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * spawnRadius;
                    Vector3 p2 = target.position + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * spawnRadius;
                    Gizmos.DrawLine(p1, p2);
                }
                break;
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(target.position, minDistanceFromTarget);
    }
}
