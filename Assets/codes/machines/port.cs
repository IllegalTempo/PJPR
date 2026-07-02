using Assets.codes.Network.Messages;
using UnityEngine;

public enum portType
{
    input,
    output,
    both
}
public abstract class Port : Slot //port are slots that will unrealize the item that is attached to it
{
    public portType type;
    //public override void Attach(Item item)
    //{
    //    if (LinkedStorage == null || LinkedStorage.IsFull()) return;

    //}
    public override void Attach(Item item, Quaternion rot)
    {

    }
    public override void ServerActionOnAttach(Item item, Quaternion rot)
    {
        base.ServerActionOnAttach(item, rot);
        GameCore.Instance.ServerDestroyNetworkItem(item);
    }
    public override void Detach()
    {
        
    }

}
