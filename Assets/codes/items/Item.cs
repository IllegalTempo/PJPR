using Assets.codes.Network.Messages;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.UI.GridLayoutGroup;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody))]

///
/// Item is any object that can be picked up
///
public class Item : Selectable //Item is any that is pickable
{
    public ItemDefinition AbstractItem;
    //[SerializeField] protected bool isRepairTool;
    [SerializeField]
    protected NetworkObject netObj;
    protected Rigidbody rb;
    protected Collider itemCollider;

    private Transform originalParent;
    private Vector3 originalScale;
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
        originalScale = transform.localScale;
    }
    public void DisableRB()
    {
        rb.isKinematic = true;
    }
    public void EnableRB()
    {
        rb.isKinematic = false;
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

    }
    public void Network_Send_ChangeOwnerShip(ulong newowner)
    {
        if (NetworkSystem.Instance == null || !NetworkSystem.Instance.IsOnline)
        {
            return;
        }
        NMS_Both_PickUpItem message = new NMS_Both_PickUpItem(netObj.Identifier, newowner);
        if (NetworkSystem.Instance.IsServer)
        {
            NetworkRouter.Instance.DistributeMessageToReady(message);
            Network_onPickUPorDrop(newowner);
            //Because the server doesnt receive distribution messages, we call it directly.
        }
        else
        {
            NetworkRouter.Instance.SendMessageToServer(message);
        }
    }
    public void Network_onPickUPorDrop(ulong newowner)
    {
        PlayerMain who = NetworkSystem.Instance.PlayerList[newowner].playerControl;
        bool doneByLocal = newowner == NetworkSystem.Instance.SteamID || (newowner == 0 && netObj.Owner == NetworkSystem.Instance.SteamID);
        if (newowner == 0)
        {
            gotDropped(transform.position,doneByLocal);

        }
        else
        {
            gotPickedup(who,doneByLocal);
        }
        netObj.ChangeOwner(newowner);

    }
    private void gotPickedup(PlayerMain who,bool local)

    {
        if(local)
        {
            UIManager.Instance.ShowInteraction("Drop", who.control.Player.pickup.GetBindingDisplayString(), 0);
        }
        transform.SetParent(who.HandTransform);
        rb.constraints = RigidbodyConstraints.FreezeAll;
        
        if(AbstractItem != null)
        {
            transform.localPosition = AbstractItem.HoldOffset;
            transform.localRotation = AbstractItem.HoldRotation;
            transform.localScale = AbstractItem.HoldScale;
        } else
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        


        rb.linearVelocity = Vector3.zero;
        rb.useGravity = false;
        outline.OutlineColor = Color.aquamarine;
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }
    }
    private void gotDropped(Vector3 dropPosition,bool local)
    {
        if(local)
        {
            UIManager.Instance.HideInteraction(0);

        }
        transform.SetParent(originalParent);
        outline.OutlineColor = Color.white;
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        this.transform.position = dropPosition;
        transform.localScale = originalScale;
        itemCollider.enabled = true;
    }





    public NetworkObject GetNetworkObject()
    {
        if (netObj == null)
        {
            netObj = GetComponent<NetworkObject>();
        }
        return netObj;
    }



    
    protected override void Update()
    {
        base.Update();

        if (NetworkSystem.Instance == null || !NetworkSystem.Instance.IsOnline || netObj == null || GameCore.Instance == null || GameCore.Instance.Local_NetworkPlayer == null || GameCore.Instance.Local_Player == null)
        {
            return;
        }

    }
}
