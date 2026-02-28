using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;


public class BlackHoleSpawner : HazardSpawnerBase
{
    [Header("Black Hole Prefab")]
    [SerializeField] private GameObject blackHolePrefab;
    [SerializeField] private string blackHolePrefabID;

    [Header("Spawn Timing")]
    [SerializeField] private float firstSpawnDelay = 60f;
    [SerializeField] private float spawnInterval = 120f;
    [SerializeField] private float spawnIntervalVariation = 30f;

    [Header("Despawn")]
    [SerializeField] private float blackHoleLifetime = 45f;
    [SerializeField] private float despawnShrinkTime = 2f;

    [Header("Scale")]
    [SerializeField] private Vector2 scaleRange = new Vector2(1.5f, 3f);

    private readonly List<GameObject> activeBlackHoles = new List<GameObject>();

    protected override void Start()
    {
        base.Start();
        if (!enabled) return;

        StartCoroutine(SpawnCycle());
    }

    private void Update()
    {
        activeBlackHoles.RemoveAll(g => g == null);
    }

    private IEnumerator SpawnCycle()
    {
        yield return new WaitForSeconds(firstSpawnDelay);

        while (true)
        {
            SpawnOne().Forget();

            float wait = spawnInterval + Random.Range(-spawnIntervalVariation, spawnIntervalVariation);
            yield return new WaitForSeconds(Mathf.Max(10f, wait));
        }
    }

    private async UniTaskVoid SpawnOne()
    {
        Vector3 pos = GetSpawnPosition(minSpawnDistance * 1.5f, spawnRadius * 0.8f);

        GameObject[] prefabs = blackHolePrefab != null ? new[] { blackHolePrefab } : null;
        string[] ids = !string.IsNullOrEmpty(blackHolePrefabID) ? new[] { blackHolePrefabID } : null;

        GameObject bh = await SpawnHazardObject(prefabs, ids, pos, Quaternion.identity);
        if (bh == null) return;

        bh.transform.localScale = Vector3.one * Random.Range(scaleRange.x, scaleRange.y);
        activeBlackHoles.Add(bh);

        Vector3 targetPos = spaceshipTarget != null ? spaceshipTarget.position : Vector3.zero;
        Vector3 direction = (targetPos - pos).normalized; // moves towards the spaceship 
        
        BlackHole bhScript = bh.GetComponent<BlackHole>();
        if (bhScript != null)
        {
            bhScript.InitializeMovement(direction, Random.Range(1f, 3f)); // low speed
        }

        Debug.Log($"[BlackHoleSpawner] Spawned moving black hole at {pos}, lifetime={blackHoleLifetime}s");

        StartCoroutine(DespawnAfter(bh, blackHoleLifetime, despawnShrinkTime));
    }

    public void SpawnNow() => SpawnOne().Forget();

    public void ClearAll()
    {
        foreach (var g in activeBlackHoles) { if (g != null) Destroy(g); }
        activeBlackHoles.Clear();
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = new Color(0.6f, 0f, 1f, 0.35f);
        foreach (var bh in activeBlackHoles)
        {
            if (bh != null)
                Gizmos.DrawWireSphere(bh.transform.position, bh.transform.localScale.x * 4f);
        }
    }
}
