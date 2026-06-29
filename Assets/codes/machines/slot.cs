using Assets.codes.Network.Messages;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public class Slot : Selectable //slot is the place where items are put in to be used by machines, attach this to a gameobject that is a child of a machine, when item is dropped look at it, it will attach to this slot
{
    protected Item item;
    [SerializeField]
    public ItemType AllowedItemType = ItemType.All;
    [SerializeField]
    public NetworkIdentity Identity;
    public virtual void Attach(Item item, Quaternion rot)
    {
        this.item = item;
        item.AttachToSlot(this,rot);
    }
    public virtual void Detach()
    {
        if (item != null)
        {
            item.AttachedSlot = null;
            item.EnableRB();
            item.transform.SetParent(null);
            item = null;
        }
    }
    public void SendAttach(Item item)
    {
        NMS_Both_SlotAttach message = new NMS_Both_SlotAttach(Identity.Identifier,item.GetNetworkObject().Identity.Identifier,item.transform.rotation);
        message.SendMessageAsServerOrClient();
    }
    public void SendDetach()
    {
        NMS_Both_SlotDetach message = new NMS_Both_SlotDetach(Identity.Identifier);
        message.SendMessageAsServerOrClient();
    }
    public void Attach(string itemId,Quaternion rot)
    {
        Item item = NetworkSystem.Instance.GetComponentOfIdentity<Item>(itemId);
        Attach(item,rot);

    }
    public Item GetAttachedItem()
    {
        return this.item;
    }
}
