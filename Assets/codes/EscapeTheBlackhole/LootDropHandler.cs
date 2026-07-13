using UnityEngine;

/// <summary>
/// Stub loot drop handler — rolls on LootTableDefinition and logs results.
/// Physical item spawning is deferred.
/// </summary>
public class LootDropHandler : MonoBehaviour
{
    public static LootDropHandler Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("If true, loot rolls and logs to console. If false, does nothing.")]
    [SerializeField] private bool enableLootDrops = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Process a loot drop for a meteorite type at a given position.
    /// Called from Meteorite.BreakMeteorite() or MeteoriteSpawner.
    /// </summary>
    public void ProcessLootDrop(MeteoriteTypeDefinition typeDef, Vector3 position)
    {
        if (!enableLootDrops) return;
        if (typeDef == null) return;
        if (typeDef.lootTable == null) return;

        string[] drops = typeDef.lootTable.RollLoot();

        if (drops.Length > 0)
        {
            Debug.Log($"[LootDrop] {typeDef.typeName} meteorite at {position} dropped: {string.Join(", ", drops)}");
        }

        // TODO: Future — spawn physical Item prefabs at position
        // foreach (string itemName in drops)
        // {
        //     NetworkSystem.Instance.CreateWorldReferenceNetworkObject(itemName, position, Random.rotation, 0);
        // }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
