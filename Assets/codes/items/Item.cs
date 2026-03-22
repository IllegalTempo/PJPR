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
    private void gotPickedup(PlayerMain who)

    {
        who.OnPickUpItem(this, netObj);
        transform.parent = who.HandTransform;
        

        rb.constraints = RigidbodyConstraints.FreezeAll;
        
        transform.localPosition = HoldOffset;
        transform.localRotation = Quaternion.Euler(HoldRotation);

        

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
        rb.constraints = RigidbodyConstraints.None;
        this.transform.position = dropPosition;

        itemCollider.enabled = true;
    }
    protected virtual void PickUpItem() //Only Run by local
    {

        gotPickedup(GameCore.Instance.Local_Player);




    }
    public NetworkObject GetNetworkObject()
    {
        if (netObj == null)
        {
            netObj = GetComponent<NetworkObject>();
        }
        return netObj;
    }



    protected virtual void Drop(Vector3 dropPosition) //Only Run by local
    {

        gotDropped(dropPosition);
        netObj.Owner = 0;

        if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline)
        {
            if (NetworkSystem.Instance.IsServer)
            {
                ServerSend.DistributePickUpItem(netObj.Identifier, 0);
            }
            else
            {
                ClientSend.PickUpItem(netObj.Identifier, 0);
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
    public void Drop()
    {
        Drop(transform.position);
    }

}
