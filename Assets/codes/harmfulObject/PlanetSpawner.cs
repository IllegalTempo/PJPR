using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class PlanetSpawner : HazardSpawnerBase
{
    [Header("Planet Prefabs")]
    [SerializeField] private GameObject[] planetPrefabs;
    [SerializeField] private string[] planetPrefabIDs;

    [Header("Spawn Settings")]
    [SerializeField] private int planetCount = 3;
    [SerializeField] private Vector2 scaleRange = new Vector2(8f, 20f);
    [SerializeField] private bool spawnOnStart = true;

    private readonly List<GameObject> activePlanets = new List<GameObject>();

    protected override void Start()
    {
        base.Start();
        if (!enabled) return; 

        if (spawnOnStart)
            SpawnAll();
    }

    private void Update()
    {
        activePlanets.RemoveAll(g => g == null);
    }

    private void SpawnAll()
    {
        for (int i = 0; i < planetCount; i++)
            SpawnOne().Forget();
    }

    private async UniTaskVoid SpawnOne()
    {
        Vector3 pos = GetSpawnPosition(minSpawnDistance, spawnRadius);
        Quaternion rot = Random.rotation;

        GameObject planetObj = await SpawnHazardObject(planetPrefabs, planetPrefabIDs, pos, rot);
        if (planetObj == null) return;

        planetObj.transform.localScale = Vector3.one * Random.Range(scaleRange.x, scaleRange.y);
        activePlanets.Add(planetObj);

        Debug.Log($"[PlanetSpawner] Spawned planet at {pos}");
    }

    public void ClearAll()
    {
        foreach (var g in activePlanets) { if (g != null) Destroy(g); }
        activePlanets.Clear();
    }

    public void Respawn()
    {
        ClearAll();
        SpawnAll();
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
        Vector3 origin = spaceshipTarget != null ? spaceshipTarget.position : transform.position;
        foreach (var g in activePlanets)
        {
            if (g != null)
            {
                Gizmos.DrawWireSphere(g.transform.position, g.transform.localScale.x * 0.5f);
            }
        }
    }
}
