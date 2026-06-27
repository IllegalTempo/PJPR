using System;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;

[RequireComponent(typeof(NetworkIdentity))]
public class Slot : Selectable //slot is the place where items are put in to be used by machines, attach this to a gameobject that is a child of a machine, when item is dropped look at it, it will attach to this slot
{
    Item item;
    [SerializeField]
    public ItemType AllowedItemType = ItemType.All;
    [SerializeField]
    public NetworkIdentity Identity;
    public virtual void Attach(Item item)
    {
        this.item = item;
        item.DisableRB();
        item.transform.position = this.transform.position;
    }
    public void Attach(string itemId)
    {
        Item item = NetworkSystem.Instance.GetComponentOfIdentity<Item>(itemId);
        Attach(item);
        
    }
    public Item GetAttachedItem()
    {
        return this.item; 
    }
    private void Start()
    {
        if(NetworkSystem.Instance.Slots.ContainsKey(Identity.Identifier))
        {
            Debug.LogError($"Slot with identifier {Identity.Identifier} already exists in NetworkSystem. Please ensure unique identifiers for each slot.");
            return;
        } else
        {
            NetworkSystem.Instance.Slots.Add(Identity.Identifier, this);

        }
    }
}
