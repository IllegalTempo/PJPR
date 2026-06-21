using System;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

public class slot : Selectable //slot is the place where items are put in to be used by machines, attach this to a gameobject that is a child of a machine, when item is dropped look at it, it will attach to this slot
{
    Item item;
    [SerializeField]
    public ItemType AllowedItemType = ItemType.All;
    public virtual void AttachItem(Item item)
    {
        this.item = item;
        item.DisableRB();
        item.transform.position = this.transform.position;
    }
    public Item GetAttachedItem()
    {
        return this.item; 
    }
}
