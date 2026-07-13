using UnityEngine;

[CreateAssetMenu(fileName = "MeteoriteType", menuName = "Escape Blackhole/Meteorite Type")]
public class MeteoriteTypeDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name, e.g. 'Small', 'Medium', 'Large'")]
    public string typeName = "Small";

    [Tooltip("Enum used for SpaceshipPart collision-damage lookup")]
    public MeteoriteSize size = MeteoriteSize.Small;

    [Header("Stats")]
    [Tooltip("Maximum health before the meteorite breaks")]
    public float maxHealth = 50f;

    [Tooltip("Multiplier applied to the spawner's base speed (1.0 = base speed)")]
    public float speedMultiplier = 1f;

    [Tooltip("Damage dealt to spaceship parts on collision")]
    public float damage = 10f;

    [Header("Visuals")]
    [Tooltip("Random scale range (x = min, y = max)")]
    public Vector2 scaleRange = new Vector2(0.5f, 1.2f);

    [Header("Loot")]
    [Tooltip("Loot table for this meteorite type (null = no loot)")]
    public LootTableDefinition lootTable;

    [Header("Pooling")]
    [Tooltip("How many of this type to pre-instantiate in the object pool")]
    public int poolSize = 15;

    /// <summary>Returns a random scale within the configured range.</summary>
    public float GetRandomScale()
    {
        return Random.Range(scaleRange.x, scaleRange.y);
    }
}

public enum MeteoriteSize
{
    Small,
    Medium,
    Large
}
