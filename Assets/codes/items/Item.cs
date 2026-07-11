using Assets.codes.Network.Messages;
using Assets.codes.Network.SyncedIdentity;
using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
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
    Processable = 1 << 2,

    All = Generic | SpaceshipModule | Processable,
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
[RequireComponent(typeof(NetworkPrefabIdentity), typeof(Rigidbody))]

public class Item : Selectable //Item is any that is pickable
{

    public ItemDefinition AbstractItem;
    //[SerializeField] protected bool isRepairTool;
    [SerializeField]
    protected NetworkGameObject netObj;
    protected Rigidbody rb;
    protected Collider[] colliders;

    [SerializeField]
    public bool lockRelativeRotation = false;

    public bool IsPickable = true;
    public bool IsLocked = false;

    public ItemType itemType = ItemType.Generic;
    public Slot AttachedSlot;









    public Slot BindSlot = null; //use for visual dont mind this

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
            netObj = GetComponent<NetworkGameObject>();
        }

        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
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
    public bool FitIn(Slot slot)
    {
        if (!slot.IsEmpty()) return false;
        if (slot.AllowedItemType == ItemType.All) return true;
        return HasItemType(slot.AllowedItemType);
    }
    public bool HasItemType(ItemType type)
    {
        return (itemType & type) != 0;
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
            netObj = GetComponent<NetworkGameObject>();
            Debug.LogWarning($"{name} has no NetworkObject, cannot be picked up.");
            return;
        }

    }
    public void ChangeItemOwner(ulong newowner)
    {
        NMS_Both_PickUpItem message = new NMS_Both_PickUpItem(netObj.Identity.Identifier, newowner);
        message.SendMessageAsServerOrClient();
    }
    public void Network_onPickUPorDrop(ulong newowner)
    {
        bool isDropAction = newowner == 0;
        if(!isDropAction && netObj.Identity.Sovereignty != 0)
        {
            Debug.LogWarning($" {name} is already picked up, ignoring pickup action.");
            return;
        }
        netObj.Sync_Transform = isDropAction; //Only sync transform if dropped, not when picked up, because the player will be moving it.
        if (isDropAction)
        {
            PlayerMain who = NetworkSystem.Instance.PlayerList[netObj.Identity.Sovereignty].playerControl;
            gotDropped(who,transform.position);
            
        }
        else
        {
            PlayerMain who = NetworkSystem.Instance.PlayerList[newowner].playerControl;
            gotPickedup(who);
        }
        netObj.Identity.ChangeSovereignty(newowner);

    }
    private void SetColliders(bool enabled)
    {
        if (colliders != null)
        {
            foreach (Collider collider in colliders)
            {
                collider.enabled = enabled;
            }
        }
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
        outline.OutlineColor = Color.aquamarine;
        SetColliders(false);
    }
    private void gotDropped(PlayerMain who,Vector3 dropPosition)
    {

        Debug.Log($"{name} dropped by {who.name}");
        who.holdingItem = null;

        if (who.Equals(GameCore.Instance.Local_Player))
        {
            UIManager.Instance.HideInteraction(0);

        }
        transform.parent = null;
        transform.localScale = snapshot_start.scale;

        //ApplySnapshot(snapshot_start);
        outline.OutlineColor = Color.white;
        EnableRB();
        rb.constraints = RigidbodyConstraints.None;

        rb.AddForce(who.head.transform.forward * 10f, ForceMode.VelocityChange);
        rb.linearVelocity = Vector3.zero;

        transform.position = dropPosition;
        SetColliders(true);
    }
    public void AttachToSlot(Slot slot,Quaternion rot) //Dont use this directly, use slot.Attach(item) instead, this is just for internal use
    {
        AttachedSlot = slot;
        DisableRB();
        transform.localScale = snapshot_start.scale;
        transform.SetParent(slot.transform);

        transform.localPosition = Vector3.zero;
        transform.localRotation = rot;
        netObj.Sync_Transform = false;
    }




    public NetworkGameObject GetNetworkObject()
    {
        return netObj;
    }

    public void Bind(Slot slot)
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

    }
}
