using UnityEngine;
using Assets.codes.Network.SyncedIdentity;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Item", menuName = "Game/Item")]
public class PrefabDefinition : ScriptableObject
{
    public string itemName;
    public string itemDescription;
    public Sprite itemIcon;
    public GameObject itemPrefab;
    public int maxStackSize = 64;


    [Header("Pool Setting")]
    public bool IsPoolPrefab = false;
    public int poolSize = 10;

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

        NetworkGameObject nobj = itemPrefab.GetComponent<NetworkGameObject>();
        if (nobj != null && nobj.AbstractObject != this)
        {
            nobj.AbstractObject = this;
            EditorUtility.SetDirty(nobj);
            EditorUtility.SetDirty(itemPrefab);
        }
    }
#endif
}
