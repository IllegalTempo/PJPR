using UnityEngine;

/// <summary>
/// Defines loot entries and their drop probabilities for a meteorite type.
/// Create via: Assets → Create → Escape Blackhole → Loot Table
/// </summary>
[CreateAssetMenu(fileName = "LootTable", menuName = "Escape Blackhole/Loot Table")]
public class LootTableDefinition : ScriptableObject
{
    [Header("Loot Entries")]
    [Tooltip("List of possible loot drops with probabilities")]
    public LootEntry[] entries;

    /// <summary>
    /// Rolls the loot table and returns the names of items that dropped.
    /// Logs results to console (physical drop spawning is deferred).
    /// </summary>
    public string[] RollLoot()
    {
        if (entries == null || entries.Length == 0)
            return System.Array.Empty<string>();

        var results = new System.Collections.Generic.List<string>();

        for (int i = 0; i < entries.Length; i++)
        {
            LootEntry entry = entries[i];
            if (string.IsNullOrEmpty(entry.itemName))
                continue;

            float roll = Random.value;
            if (roll <= entry.probability)
            {
                int count = Random.Range(entry.minCount, entry.maxCount + 1);
                for (int c = 0; c < count; c++)
                {
                    results.Add(entry.itemName);
                }
            }
        }

        if (results.Count > 0)
        {
            Debug.Log($"[LootDrop] Dropped: {string.Join(", ", results)}");
        }

        return results.ToArray();
    }
}

/// <summary>
/// A single loot entry with probability and count range.
/// </summary>
[System.Serializable]
public struct LootEntry
{
    [Tooltip("Name of the item (for future physical drop)")]
    public string itemName;

    [Tooltip("Probability of this item dropping (0-1)")]
    [Range(0f, 1f)]
    public float probability;

    [Tooltip("Minimum quantity dropped when successful")]
    [Min(1)]
    public int minCount;

    [Tooltip("Maximum quantity dropped when successful")]
    [Min(1)]
    public int maxCount;
}
