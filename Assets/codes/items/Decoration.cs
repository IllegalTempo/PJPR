using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Decoration can be picked up by ONLY the owner of its spaceship, netobj.owner is always the spaceship owner.
/// </summary>
public class Decoration : Item
{
    public string DecorationID;
    public string DecorationName;
    public string DecorationDescription;

    public override void OnClicked()
    {
        base.OnClicked();
        // Item is not in inventory, so pick it up
        if(GameCore.instance.IsLocal(netObj.Owner))
        PickUpItem();

    }
    public void OnCreate()
    {
        GameCore.instance.localSpaceship.GetDecorationByUUID_onShip.Add(netObj.Identifier, this);
    }
    protected override void PickUpItem() //Only Run by local
    {

        rb.linearVelocity = Vector3.zero;
        outline.OutlineColor = Color.aquamarine;

        if (itemCollider != null)
        {
            itemCollider.isTrigger = true;
        }
        //netObj.Owner = GameCore.instance.localNetworkPlayer.NetworkID;
        //if (NetworkSystem.instance.IsServer) Decorations cant be pickup by other
        //{
        //    ServerSend.DistributePickUpItem(netObj.Identifier, netObj.Owner);
        //}
        //else
        //{
        //    ClientSend.PickUpItem(netObj.Identifier, netObj.Owner);
        //}
        GameCore.instance.localPlayer.OnPickUpItem(this);


    }
    protected override void Drop(Vector3 dropPosition) //Only Run by local
    {


        // Remove from inventory
        outline.OutlineColor = Color.white;
        rb.linearVelocity = Vector3.zero;

        this.transform.position = dropPosition;
        itemCollider.isTrigger = false;
        //netObj.Owner = -1;
        //if (NetworkSystem.instance.IsServer)
        //{
        //    ServerSend.DistributePickUpItem(netObj.Identifier, -1);
        //}
        //else
        //{
        //    ClientSend.PickUpItem(netObj.Identifier, -1);
        //}
        GameCore.instance.localPlayer.OnDropItem(this);

    }
}
