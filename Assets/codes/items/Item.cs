using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody))]

public class Item : Selectable
{
    public string ItemName;
    public string ItemDescription;
    public NetworkObject netObj;
    private Rigidbody rb;
    private Collider itemCollider;

    protected new void OnEnable()
    {
        base.OnEnable();
        netObj = GetComponent<NetworkObject>();
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

    private void PickUpItem() //Only Run by local
    {

        rb.linearVelocity = Vector3.zero;
        outline.OutlineColor = Color.aquamarine;

        if (itemCollider != null)
        {
            itemCollider.isTrigger = true;
        }
        if (NetworkSystem.instance.IsServer)
        {
            netObj.Owner = GameCore.instance.localNetworkPlayer.NetworkID;
            ServerSend.DistributePickUpItem(netObj.Identifier, netObj.Owner);
        }
        else
        {
            ClientSend.PickUpItem(netObj.Identifier, netObj.Owner);
        }
        GameCore.instance.localPlayer.OnPickUpItem(this);


    }
    protected override void Update()
    {
        base.Update();
        if (GameCore.instance.IsLocal(netObj.Owner))
        {
            // Update position to follow camera
            GameCore.instance.localPlayer.HoldingItem(this);

        }
    }
    public void Drop(Vector3 dropPosition) //Only Run by local
    {


        // Remove from inventory
        outline.OutlineColor = Color.white;

        this.transform.position = dropPosition;

        itemCollider.isTrigger = false;
        netObj.Owner = -1;

        if (NetworkSystem.instance.IsServer)
        {
            ServerSend.DistributePickUpItem(netObj.Identifier, netObj.Owner);
        }
        else
        {
            ClientSend.PickUpItem(netObj.Identifier, netObj.Owner);
        }
        GameCore.instance.localPlayer.OnDropItem(this);

    }
    public void Drop()
    {
        Drop(transform.position);
    }

}
