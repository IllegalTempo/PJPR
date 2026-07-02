using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Item", menuName = "Game/Item")]
public class ItemDefinition : ScriptableObject
{
    public string itemName;
    public string itemDescription;
    public Sprite itemIcon;
    public GameObject itemPrefab;
    public string prefabID;
    public int maxStackSize = 64;

    // Transform state to apply when item is picked up and held by player
    // Uses LOCAL coordinate space (relative to HandTransform parent)
    public ItemSnapshot holdState = new ItemSnapshot
    {
        position = Vector3.zero,
        rotation = Quaternion.identity,  // Identity rotation works in both world and local space
        scale = Vector3.one
    };

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (itemPrefab == null)
        {
            return;
        }

        NetworkPrefabIdentity identity = itemPrefab.GetComponent<NetworkPrefabIdentity>();
        if (identity != null && identity.PrefabID != prefabID)
        {
            identity.PrefabID = prefabID;
            EditorUtility.SetDirty(identity);
            EditorUtility.SetDirty(itemPrefab);
        }

        Item item = itemPrefab.GetComponent<Item>();
        if (item != null && item.AbstractItem != this)
        {
            item.AbstractItem = this;
            EditorUtility.SetDirty(item);
            EditorUtility.SetDirty(itemPrefab);
        }
    }
#endif
}
