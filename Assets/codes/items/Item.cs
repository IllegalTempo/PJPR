using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody))]

///
/// Item is any object that can be picked up
///
public class Item : Selectable //Item is any that is pickable
{
    public string ItemName;
    public string ItemDescription;
    //[SerializeField] protected bool isRepairTool;
    [SerializeField]
    protected NetworkObject netObj;
    protected Rigidbody rb;
    protected Collider itemCollider;

    [SerializeField]
    private Vector3 HoldOffset;
    [SerializeField]
    private Vector3 HoldScale;
    [SerializeField]
    private Vector3 HoldRotation;

    private Transform originalParent;

    [SerializeField]
    public bool lockRelativeRotation = false;
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
        originalParent = transform.parent;
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

        // Item is not in inventory, so pick it up
        PickUpItem();

    }
    public void Network_onPickUPorDrop(ulong newowner)
    {
        netObj.Network_ChangeOwner(newowner);
        PlayerMain who = NetworkSystem.Instance.PlayerList[newowner].playerControl;
        if (newowner == 0)
        {
            gotDropped(transform.position);

        }
        else
        {
            gotPickedup(who);
        }
    }
    public void whenPickUp()
    {
        transform.localPosition = HoldOffset;
        transform.localRotation = Quaternion.Euler(HoldRotation);
        transform.localScale = HoldScale;
    }
    private void gotPickedup(PlayerMain who)

    {
        transform.parent = who.HandTransform;
        


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
        transform.parent = originalParent;
        outline.OutlineColor = Color.white;
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = true;
        this.transform.position = dropPosition;

        itemCollider.enabled = true;
    }
    protected virtual void PickUpItem() //Only Run by local
    {

        gotPickedup(GameCore.Instance.Local_Player);
        GameCore.Instance.Local_Player.OnPickUpItem(this);

        ulong localOwner = 1;
        if (GameCore.Instance != null && GameCore.Instance.Local_NetworkPlayer != null)
        {
            localOwner = GameCore.Instance.Local_NetworkPlayer.steamID;
        }
            netObj.Owner = localOwner;


        if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline)
        {
            if (NetworkSystem.Instance.IsServer)
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

        if (GameCore.Instance != null && GameCore.Instance.Local_Player != null && GameCore.Instance.Local_Player.holdingItem == this)
        {
            GameCore.Instance.Local_Player.HoldingItem(this);
            return;
        }

        if (NetworkSystem.Instance == null || !NetworkSystem.Instance.IsOnline || netObj == null || GameCore.Instance == null || GameCore.Instance.Local_NetworkPlayer == null || GameCore.Instance.Local_Player == null)
        {
            return;
        }

        if (GameCore.Instance.IsLocal(netObj.Owner))
        {
            GameCore.Instance.Local_Player.HoldingItem(this);
        }
    }

        
    
    protected virtual void Drop(Vector3 dropPosition) //Only Run by local
    {

        gotDropped(dropPosition);
            netObj.Owner = 0;

        if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline)
        {
            if (NetworkSystem.Instance.IsServer)
            {
                ServerSend.DistributePickUpItem(netObj.Identifier,0);
            }
            else
            {
                ClientSend.PickUpItem(netObj.Identifier, 0);
            }
        }

    }
    public void Drop()
    {
        Drop(transform.position);
    }

}
