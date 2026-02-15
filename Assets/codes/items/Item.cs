using UnityEngine;

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
        if (netObj.Owner != -1) return;
        // Item is not in inventory, so pick it up
        PickUpItem();

    }

    protected virtual void PickUpItem() //Only Run by local
    {

        rb.linearVelocity = Vector3.zero;
        outline.OutlineColor = Color.aquamarine;
        if (itemCollider != null)
        {
            itemCollider.isTrigger = true;
        }
        GameCore.instance.localPlayer.OnPickUpItem(this);


        netObj.Owner = GameCore.instance.localNetworkPlayer.NetworkID;
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


        // Remove from inventory
        outline.OutlineColor = Color.white;
        rb.linearVelocity = Vector3.zero;

        this.transform.position = dropPosition;

        itemCollider.isTrigger = false;
        netObj.Owner = -1;
        if (NetworkSystem.instance.IsServer)
        {
            ServerSend.DistributePickUpItem(netObj.Identifier, -1);
        }
        else
        {
            ClientSend.PickUpItem(netObj.Identifier, -1);
        }
        GameCore.instance.localPlayer.OnDropItem(this);

    }
    public void Drop()
{
    Drop(transform.position);
}

}
