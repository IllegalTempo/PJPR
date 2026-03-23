using UnityEngine;

public class ItemDefinition : ScriptableObject
{
    public string itemName;
    public string itemDescription;
    public Sprite itemIcon;
    public GameObject itemPrefab;
    public int maxStackSize = 64;
}
