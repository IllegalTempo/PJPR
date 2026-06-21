using UnityEngine;
[CreateAssetMenu(fileName = "New Item", menuName = "Game/Item")]
public class ItemDefinition : ScriptableObject
{
    public string itemName;
    public string itemDescription;
    public Sprite itemIcon;
    public GameObject itemPrefab;
    public int maxStackSize = 64;

    // Transform state to apply when item is picked up and held by player
    // Uses LOCAL coordinate space (relative to HandTransform parent)
    public ItemSnapshot holdState = new ItemSnapshot
    {
        position = Vector3.zero,
        rotation = Quaternion.identity,  // Identity rotation works in both world and local space
        scale = Vector3.one
    };

}
