using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Type-keyed object pool for meteorites, fragments, and warning indicators.
/// Pre-warms pools based on MeteoriteTypeDefinition.poolSize.
/// Attach to a persistent GameObject in the scene.
/// </summary>
public class MeteoritePool : MonoBehaviour
{
    public static MeteoritePool Instance { get; private set; }

    [Header("Pool Configuration")]
    [Tooltip("Parent transform to hold all pooled objects (created automatically if null)")]
    [SerializeField] private Transform poolRoot;

    /// <summary>Keyed by unique pool key (e.g. "Small", "Medium", "Large", "Fragment", "Warning")</summary>
    private readonly Dictionary<string, Queue<GameObject>> pools = new();

    /// <summary>Maps pool key → prefab used to instantiate more when pool is exhausted.</summary>
    private readonly Dictionary<string, GameObject> prefabMap = new();

    /// <summary>Maps pool key → pool size for pre-warming.</summary>
    private readonly Dictionary<string, int> poolSizes = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (poolRoot == null)
        {
            var rootGO = new GameObject("_MeteoritePool");
            rootGO.transform.SetParent(transform);
            poolRoot = rootGO.transform;
        }
    }

    /// <summary>
    /// Register a prefab for a pool key and pre-warm the pool.
    /// </summary>
    public void RegisterPool(string poolKey, GameObject prefab, int prewarmSize)
    {
        if (string.IsNullOrEmpty(poolKey) || prefab == null)
        {
            Debug.LogWarning($"[MeteoritePool] Cannot register pool: key='{poolKey}' prefab={prefab}");
            return;
        }

        prefabMap[poolKey] = prefab;
        poolSizes[poolKey] = prewarmSize;

        if (!pools.ContainsKey(poolKey))
        {
            pools[poolKey] = new Queue<GameObject>();
        }

        PreWarm(poolKey, prewarmSize);
    }

    /// <summary>
    /// Register a MeteoriteTypeDefinition as a pool.
    /// </summary>
    public void RegisterPool(MeteoriteTypeDefinition typeDef, GameObject prefab)
    {
        if (typeDef == null || prefab == null)
        {
            Debug.LogWarning("[MeteoritePool] Cannot register pool from null definition or prefab.");
            return;
        }

        RegisterPool(typeDef.typeName, prefab, typeDef.poolSize);
    }

    private void PreWarm(string poolKey, int count)
    {
        if (!prefabMap.TryGetValue(poolKey, out GameObject prefab))
        {
            Debug.LogWarning($"[MeteoritePool] Cannot pre-warm '{poolKey}': no prefab registered.");
            return;
        }

        Queue<GameObject> queue = pools[poolKey];

        for (int i = 0; i < count; i++)
        {
            GameObject obj = InstantiateNew(poolKey, prefab);
            queue.Enqueue(obj);
        }

        Debug.Log($"[MeteoritePool] Pre-warmed {count} objects for '{poolKey}'.");
    }

    private GameObject InstantiateNew(string poolKey, GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, Vector3.zero, Quaternion.identity, poolRoot);
        obj.name = $"{poolKey}_Pooled_{pools[poolKey].Count}";

        // Disable NetworkIdentity immediately so its Start() never fires.
        // Pooled meteorites are synced via NMS messages, not the Identity dictionary.
        NetworkPrefabIdentity netId = obj.GetComponent<NetworkPrefabIdentity>();
        if (netId != null)
        {
            // If a previous pooled copy already registered (should not happen now, but safety)
            if (!string.IsNullOrEmpty(netId.Identifier)
                && NetworkSystem.Instance.FindNetworkIdentity.ContainsKey(netId.Identifier))
            {
                NetworkSystem.Instance.FindNetworkIdentity.Remove(netId.Identifier);
            }

            netId.Identifier = "";
            netId.enabled = false; // Prevents NetworkIdentity.Start() from logging the warning
        }

        obj.SetActive(false);
        return obj;
    }

    /// <summary>
    /// Get an object from the pool. Expands pool if empty.
    /// </summary>
    public GameObject Get(string poolKey, Vector3 position, Quaternion rotation)
    {
        if (!pools.TryGetValue(poolKey, out Queue<GameObject> queue))
        {
            Debug.LogError($"[MeteoritePool] No pool registered for key '{poolKey}'.");
            return null;
        }

        GameObject obj;

        if (queue.Count > 0)
        {
            obj = queue.Dequeue();
        }
        else
        {
            // Expand pool
            if (!prefabMap.TryGetValue(poolKey, out GameObject prefab))
            {
                Debug.LogError($"[MeteoritePool] Cannot expand pool '{poolKey}': no prefab.");
                return null;
            }

            obj = InstantiateNew(poolKey, prefab);
            Debug.Log($"[MeteoritePool] Expanded pool '{poolKey}' (now ~{queue.Count + 1} total).");
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        // Notify IPoolable components
        var poolables = obj.GetComponentsInChildren<IPoolable>(includeInactive: true);
        foreach (var p in poolables)
        {
            p.OnSpawn();
        }

        return obj;
    }

    /// <summary>
    /// Return an object to the pool.
    /// </summary>
    public void Return(GameObject obj, string poolKey)
    {
        if (obj == null) return;

        if (!pools.TryGetValue(poolKey, out Queue<GameObject> queue))
        {
            Debug.LogWarning($"[MeteoritePool] No pool '{poolKey}' for returning object. Destroying instead.");
            Destroy(obj);
            return;
        }

        // Notify IPoolable components before deactivating
        var poolables = obj.GetComponentsInChildren<IPoolable>(includeInactive: false);
        foreach (var p in poolables)
        {
            p.OnDespawn();
        }

        obj.SetActive(false);
        obj.transform.SetParent(poolRoot);
        queue.Enqueue(obj);
    }

    /// <summary>
    /// Clear all pools and destroy all pooled objects.
    /// </summary>
    public void ClearAll()
    {
        foreach (var kvp in pools)
        {
            while (kvp.Value.Count > 0)
            {
                GameObject obj = kvp.Value.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }

        pools.Clear();
        prefabMap.Clear();
        poolSizes.Clear();

        Debug.Log("[MeteoritePool] All pools cleared.");
    }

    /// <summary>
    /// Returns the active count for a pool (total instantiated minus currently queued).
    /// </summary>
    public int GetActiveCount(string poolKey)
    {
        if (!pools.TryGetValue(poolKey, out Queue<GameObject> queue))
            return 0;

        if (!poolSizes.TryGetValue(poolKey, out int total))
            return 0;

        return total - queue.Count;
    }

    private void OnDestroy()
    {
        ClearAll();
        if (Instance == this)
            Instance = null;
    }
}
