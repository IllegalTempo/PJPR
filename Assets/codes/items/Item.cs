using Assets.codes.Network.Messages;
using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.UI.GridLayoutGroup;


///
/// Item is any object that can be picked up
///
[Flags]
public enum ItemType
{
    None = 0,
    Generic = 1 << 0,
    SpaceshipModule = 1 << 1,
    All = SpaceshipModule,
}
/// <summary>
/// Stores item transform state in LOCAL coordinate space.
/// All values (position, rotation, scale) are relative to the item's parent transform.
/// This ensures consistent behavior when the item is reparented (e.g., HandTransform when picked up).
/// </summary>
[System.Serializable]
public struct ItemSnapshot
{
    /// <summary>Local position relative to parent transform</summary>
    public Vector3 position;

    /// <summary>Local rotation relative to parent transform</summary>
    public Quaternion rotation;

    /// <summary>Local scale relative to parent transform</summary>
    public Vector3 scale;
}
[RequireComponent(typeof(NetworkObject), typeof(Rigidbody))]

public class Item : Selectable //Item is any that is pickable
{

    public ItemDefinition AbstractItem;
    //[SerializeField] protected bool isRepairTool;
    [SerializeField]
    protected NetworkObject netObj;
    protected Rigidbody rb;
    protected Collider itemCollider;

    [SerializeField]
    public bool lockRelativeRotation = false;

    public bool IsPickable = true;
    public bool IsLocked = false;
    public ItemType itemType = ItemType.Generic;

    public slot BindSlot = null;

    //public virtual bool IsRepairTool => isRepairTool;

    /// <summary>
    /// Snapshot of the item's initial transform state (before being picked up).
    /// Captured in OnEnable() and restored when item is dropped.
    /// Uses LOCAL coordinate space.
    /// </summary>
    [SerializeField]
    private ItemSnapshot snapshot_start;

    /// <summary>
    /// Snapshot of the item's transform state when bound to a slot.
    /// Captured in Bind() and restored when item is unbound.
    /// Uses LOCAL coordinate space.
    /// </summary>
    [SerializeField]
    private ItemSnapshot snapshot_bind;
    private Transform pre_bind_parent;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (netObj == null)
        {
            netObj = GetComponent<NetworkObject>();
        }

        rb = GetComponent<Rigidbody>();
        itemCollider = GetComponent<Collider>();
        snapshot_start = GetSnapshot();
    }
    private ItemSnapshot GetSnapshot()
    {
        // Capture transform state in LOCAL coordinate space
        // This ensures consistent behavior when parent transforms change
        return new ItemSnapshot
        {
            position = transform.localPosition,
            rotation = transform.localRotation,  // Use localRotation instead of world rotation
            scale = transform.localScale,
        };
    }
    private void ApplySnapshot(ItemSnapshot snapshot)
    {
        // Restore transform state using LOCAL coordinate space
        // Consistent with GetSnapshot() for predictable behavior
        transform.localPosition = snapshot.position;
        transform.localRotation = snapshot.rotation;  // Use localRotation instead of world rotation
        transform.localScale = snapshot.scale;
    }
    public bool FitIn(slot slot)
    {
        if (slot == null) return false;
        if (slot.GetAttachedItem() != null) return false;
        if (slot.AllowedItemType == ItemType.All) return true;
        return (itemType & slot.AllowedItemType) != 0;
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
            Network_onPickUPorDrop(newowner);

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
        netObj.Sync_Transform = newowner == 0; //Only sync transform if dropped, not when picked up, because the player will be moving it.
        if (newowner == 0)
        {
            PlayerMain who = NetworkSystem.Instance.PlayerList[netObj.Owner].playerControl;
            who.holdingItem = null;
            gotDropped(who,transform.position);
            
        }
        else
        {
            PlayerMain who = NetworkSystem.Instance.PlayerList[newowner].playerControl;
            gotPickedup(who);
        }
        netObj.ChangeOwner(newowner);

    }
    private void gotPickedup(PlayerMain who)


    {
        Debug.Log($"{name} picked up by {who.name}");
        who.holdingItem = this;

        if (who.Equals(GameCore.Instance.Local_Player))
        {
            UIManager.Instance.ShowInteraction("Drop", who.control.Player.pickup.GetBindingDisplayString(), 0);
        }
        transform.SetParent(who.HandTransform);
        rb.constraints = RigidbodyConstraints.FreezeAll;

        if (AbstractItem != null)
        {
            ApplySnapshot(AbstractItem.holdState);
        }
        else
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
    private void gotDropped(PlayerMain who,Vector3 dropPosition)
    {
        Debug.Log($"{name} dropped by {who.name}");
        if (who.Equals(GameCore.Instance.Local_Player))
        {
            UIManager.Instance.HideInteraction(0);

        }
        transform.parent = null;

        //ApplySnapshot(snapshot_start);
        outline.OutlineColor = Color.white;
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        transform.position = dropPosition;
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

    public void Bind(slot slot)
    {
        pre_bind_parent = transform.parent;
        snapshot_bind = GetSnapshot();
        transform.parent = null;
        transform.localScale = snapshot_start.scale;

        transform.parent = slot.transform;
        transform.localPosition = Vector3.zero;
        transform.rotation = slot.transform.rotation;
        BindSlot = slot;
    }
    public void Unbind()
    {
        
        BindSlot = null;
        transform.parent = pre_bind_parent;
        ApplySnapshot(snapshot_bind);
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
