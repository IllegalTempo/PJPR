using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public abstract class HazardSpawnerBase : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] protected Transform spaceshipTarget;
    [SerializeField] protected float spawnRadius = 150f;
    [SerializeField] protected float minSpawnDistance = 50f;

    protected bool IsServerAuthority =>
        NetworkSystem.Instance == null || NetworkSystem.Instance.IsServer;


    protected virtual void Start()
    {
        if (!IsServerAuthority)
        {
            enabled = false;
            return;
        }
        if (spaceshipTarget == null)
        {
            GameObject ss = GameObject.FindGameObjectWithTag("Spaceship");
            spaceshipTarget = ss != null ? ss.transform : transform;
        }
    }


    protected async UniTask<GameObject> SpawnHazardObject(
        GameObject[] prefabs,
        string[] prefabIDs,
        Vector3 position,
        Quaternion rotation)
    {
        if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsServer)
        {
            string id = GetPrefabID(prefabs, prefabIDs);
            if (!string.IsNullOrEmpty(id))
            {
                NetworkObject netObj = await NetworkSystem.Instance.CreateWorldReferenceNetworkObject(id, position, rotation, 0);
                return netObj?.gameObject;
            }
        }
        if (prefabs != null && prefabs.Length > 0)
        {
            List<GameObject> valid = new List<GameObject>();
            foreach (var p in prefabs) { if (p != null) valid.Add(p); }
            if (valid.Count > 0)
                return Instantiate(valid[Random.Range(0, valid.Count)], position, rotation);
        }
        Debug.LogWarning($"[{GetType().Name}] No prefab available for spawn!");
        return null;
    }

    protected string GetPrefabID(GameObject[] prefabs, string[] prefabIDs)
    {
        if (prefabIDs != null)
        {
            foreach (string id in prefabIDs)
                if (!string.IsNullOrWhiteSpace(id)) return id;
        }

        if (prefabs != null && GameCore.Instance != null)
        {
            foreach (GameObject p in prefabs)
            {
                if (p == null) continue;
                foreach (var kv in GameCore.Instance.GetPrefabWithID)
                    if (kv.Key == p.name || kv.Value == p.name) return kv.Key;
            }
        }

        return string.Empty;
    }

    protected Vector3 GetSpawnPosition(float minDist, float maxDist)
    {
        Vector3 origin = spaceshipTarget != null ? spaceshipTarget.position : Vector3.zero;
        for (int attempt = 0; attempt < 20; attempt++)
        {
            Vector3 candidate = origin + Random.onUnitSphere * Random.Range(minDist, maxDist);
            if (Vector3.Distance(candidate, origin) >= minDist)
                return candidate;
        }
        return origin + Random.onUnitSphere * maxDist;
    }

    protected IEnumerator DespawnAfter(GameObject obj, float delay, float shrinkTime = 2f)
    {
        yield return new WaitForSeconds(delay);
        if (obj == null) yield break;

        float elapsed = 0f;
        Vector3 originalScale = obj.transform.localScale;
        while (elapsed < shrinkTime && obj != null)
        {
            obj.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsed / shrinkTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Vector3 origin = spaceshipTarget != null ? spaceshipTarget.position : transform.position;
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(origin, spawnRadius);
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.25f);
        Gizmos.DrawWireSphere(origin, minSpawnDistance);
    }
}
