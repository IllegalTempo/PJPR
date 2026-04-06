using UnityEngine;
[CreateAssetMenu(fileName = "New Item", menuName = "Game/Item")]
public class ItemDefinition : ScriptableObject
{
    public string itemName;
    public string itemDescription;
    public Sprite itemIcon;
    public GameObject itemPrefab;
    public int maxStackSize = 64;

    public Vector3 HoldScale = Vector3.one;
    public Quaternion HoldRotation = Quaternion.identity;
    public Vector3 HoldOffset = Vector3.zero;

}
