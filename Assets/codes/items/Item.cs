using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody))]

public class Item : Selectable //Item is any that is pickable
{
    public string ItemName;
    public string ItemDescription;
    //[SerializeField] protected bool isRepairTool;
    public NetworkObject netObj;
    protected Rigidbody rb;
    protected Collider itemCollider;

    //public virtual bool IsRepairTool => isRepairTool;

    protected new void OnEnable()
    {
        base.OnEnable();

        if (netObj == null)
        {
            netObj = GetComponent<NetworkObject>();
        }

        rb = GetComponent<Rigidbody>();
        itemCollider = GetComponent<Collider>();
    }




    public override void OnClicked()
    {
        base.OnClicked();
        if (netObj == null)
        {
            netObj = GetComponent<NetworkObject>();
            Debug.LogWarning($"{name} has no NetworkObject, cannot be picked up.");
            return;
        }

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
            itemCollider.enabled = false;
        }
    }
    private void gotDropped(Vector3 dropPosition)
    {
        outline.OutlineColor = Color.white;
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = true;
        this.transform.position = dropPosition;

        itemCollider.enabled = true;
    }
    protected virtual void PickUpItem() //Only Run by local
    {

        gotPickedup();
        GameCore.INSTANCE.Local_Player.OnPickUpItem(this);

        ulong localOwner = 1;
        if (GameCore.INSTANCE != null && GameCore.INSTANCE.Local_NetworkPlayer != null)
        {
            localOwner = GameCore.INSTANCE.Local_NetworkPlayer.steamID;
        }

        netObj.Owner = localOwner;

        if (NetworkSystem.INSTANCE != null && NetworkSystem.INSTANCE.IsOnline)
        {
            if (NetworkSystem.INSTANCE.IsServer)
            {
                ServerSend.DistributePickUpItem(netObj.Identifier, netObj.Owner);
            }
            else
            {
                ClientSend.PickUpItem(netObj.Identifier, netObj.Owner);
            }
        }


    }
    protected override void Update()
    {
        base.Update();

        if (GameCore.INSTANCE != null && GameCore.INSTANCE.Local_Player != null && GameCore.INSTANCE.Local_Player.holdingItem == this)
        {
            GameCore.INSTANCE.Local_Player.HoldingItem(this);
            return;
        }

        if (NetworkSystem.INSTANCE == null || !NetworkSystem.INSTANCE.IsOnline || netObj == null || GameCore.INSTANCE == null || GameCore.INSTANCE.Local_NetworkPlayer == null || GameCore.INSTANCE.Local_Player == null)
        {
            return;
        }

        if (GameCore.INSTANCE.IsLocal(netObj.Owner))
        {
            GameCore.INSTANCE.Local_Player.HoldingItem(this);
        }
    }

        
    
    protected virtual void Drop(Vector3 dropPosition) //Only Run by local
    {


        gotDropped(dropPosition);
        netObj.Owner = 0;
        if (NetworkSystem.INSTANCE != null && NetworkSystem.INSTANCE.IsOnline)
        {
            if (NetworkSystem.INSTANCE.IsServer)
            {
                ServerSend.DistributePickUpItem(netObj.Identifier,0);
            }
            else
            {
                ClientSend.PickUpItem(netObj.Identifier, 0);
            }
        }
        GameCore.INSTANCE.Local_Player.OnDropItem(this);

    }
    public void Drop()
{
    Drop(transform.position);
}

}
