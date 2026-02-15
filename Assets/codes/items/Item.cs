using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody))]

public class Item : Selectable
{
    public string ItemName;
    public string ItemDescription;
    public NetworkObject netObj;
    protected Rigidbody rb;
    protected Collider itemCollider;

    protected new void OnEnable()
    {
        base.OnEnable();

        rb = GetComponent<Rigidbody>();
        itemCollider = GetComponent<Collider>();
    }




    public override void OnClicked()
    {
        base.OnClicked();
        if (netObj.Owner != 0) return;
        // Item is not in inventory, so pick it up
        PickUpItem();

    }
    public void Network_onChangeOwnership(ulong newowner)
    {
        netObj.Network_ChangeOwner(newowner);
        if (newowner == 0)
        {
            gotDropped(transform.position);

        }
        else
        {
            gotPickedup();
        }
    }
    private void gotPickedup()
    {
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = false;
        outline.OutlineColor = Color.aquamarine;
        if (itemCollider != null)
        {
            itemCollider.isTrigger = true;
        }
    }
    private void gotDropped(Vector3 dropPosition)
    {
        // Remove from inventory
        outline.OutlineColor = Color.white;
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = true;
        this.transform.position = dropPosition;

        itemCollider.isTrigger = false;
    }
    protected virtual void PickUpItem() //Only Run by local
    {

        gotPickedup();
        GameCore.instance.localPlayer.OnPickUpItem(this);


        netObj.Owner = GameCore.instance.localNetworkPlayer.steamID;
        if (NetworkSystem.instance.IsServer)
        {
            ServerSend.DistributePickUpItem(netObj.Identifier, netObj.Owner);
        }
        else
        {
            ClientSend.PickUpItem(netObj.Identifier, netObj.Owner);
        }


    }
    protected override void Update()
    {
        base.Update();
        if (!NetworkSystem.instance.IsOnline) return;
        if (GameCore.instance.IsLocal(netObj.Owner))
        {
            // Update position to follow camera
            GameCore.instance.localPlayer.HoldingItem(this);

        }
    }

        
    
    protected virtual void Drop(Vector3 dropPosition) //Only Run by local
    {


        gotDropped(dropPosition);
        netObj.Owner = 0;
        if (NetworkSystem.instance.IsServer)
        {
            ServerSend.DistributePickUpItem(netObj.Identifier,0);
        }
        else
        {
            ClientSend.PickUpItem(netObj.Identifier, 0);
        }
        GameCore.instance.localPlayer.OnDropItem(this);

    }
    public void Drop()
{
    Drop(transform.position);
}

}
