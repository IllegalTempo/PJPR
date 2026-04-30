using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StorageSlot : MonoBehaviour
{
    [SerializeField]
    private Image itemIcon;
    public void InitSlot(Sprite itemIcon)
    {
        this.itemIcon.sprite = itemIcon;
    }

}
