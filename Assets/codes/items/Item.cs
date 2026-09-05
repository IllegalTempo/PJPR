using Assets.codes.Network.Messages;
using Assets.codes.Network.SyncedIdentity;
using System;
using System.Collections.Generic;
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
[RequireComponent(typeof(NetworkGameObject), typeof(Rigidbody), typeof(Selectable))]

public class Item : MonoBehaviour//Item is any that is pickable
{

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
    [HideInInspector]
    public Slot AttachedSlot;

    private readonly Dictionary<Rigidbody, int> movementReferenceContacts = new Dictionary<Rigidbody, int>();

    public PlayerMain PickedUpBy;
    private Rigidbody activeMovementReferenceRigidbody;
    private Vector3 previousMovementReferenceVelocity;




    [HideInInspector]

    public Slot BindSlot = null; //use for visual dont mind this

    //public virtual bool IsRepairTool => isRepairTool;

    /// <summary>
    /// Snapshot of the item's initial transform state (before being picked up).
    /// Captured in OnEnable() and restored when item is dropped.
    /// Uses LOCAL coordinate space.
    /// </summary>
    //[SerializeField]
    private ItemSnapshot snapshot_start;
    private Quaternion originalWorldRotation;
    public Quaternion OriginalRotation => originalWorldRotation;

    /// <summary>
    /// Snapshot of the item's transform state when bound to a slot.
    /// Captured in Bind() and restored when item is unbound.
    /// Uses LOCAL coordinate space.
    /// </summary>
    //[SerializeField]
    private ItemSnapshot snapshot_bind;
    private Transform pre_bind_parent;
    private LayerMask[] colliderExcludeLayersBeforeAttach;

    void OnEnable()
    {

        if (netObj == null)
        {
            netObj = GetComponent<NetworkGameObject>();
        }

        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        snapshot_start = GetSnapshot();
        originalWorldRotation = transform.rotation;
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
    private void FixedUpdate()
    {
        ApplyMovementReferenceVelocity();
    }

    private void ApplyMovementReferenceVelocity()
    {
        if (PickedUpBy != null || AttachedSlot != null || rb == null || rb.isKinematic)
        {
            previousMovementReferenceVelocity = Vector3.zero;
            return;
        }

        Vector3 movementReferenceVelocity = GetMovementReferenceVelocity();
        Vector3 relativeVelocity = rb.linearVelocity - previousMovementReferenceVelocity;
        rb.linearVelocity = relativeVelocity + movementReferenceVelocity;
        previousMovementReferenceVelocity = movementReferenceVelocity;
    }

    private Vector3 GetMovementReferenceVelocity()
    {
        if (movementReferenceContacts.Count <= 0)
        {
            return Vector3.zero;
        }

        Rigidbody movementReferenceRigidbody = activeMovementReferenceRigidbody;
        if (movementReferenceRigidbody == null || !movementReferenceContacts.ContainsKey(movementReferenceRigidbody))
        {
            movementReferenceRigidbody = ChooseMovementReferenceRigidbody();
            SetActiveMovementReferenceRigidbody(movementReferenceRigidbody);
        }

        return movementReferenceRigidbody != null ? movementReferenceRigidbody.GetPointVelocity(rb.position) : Vector3.zero;
    }

    private Rigidbody ChooseMovementReferenceRigidbody()
    {
        foreach (Rigidbody movementReferenceRigidbody in movementReferenceContacts.Keys)
        {
            if (movementReferenceRigidbody != null)
            {
                return movementReferenceRigidbody;
            }
        }

        return null;
    }

    private void SetActiveMovementReferenceRigidbody(Rigidbody movementReferenceRigidbody)
    {
        if (activeMovementReferenceRigidbody == movementReferenceRigidbody)
        {
            return;
        }

        activeMovementReferenceRigidbody = movementReferenceRigidbody;
        previousMovementReferenceVelocity = Vector3.zero;
    }

    private Rigidbody GetMovementReferenceRigidbody(Rigidbody candidateRigidbody, Transform candidateTransform)
    {
        if (MainSpaceship.Instance == null)
        {
            return null;
        }

        Transform referenceRootTransform = MainSpaceship.Instance.transform;
        if (IsMovementReferenceRigidbody(candidateRigidbody, referenceRootTransform))
        {
            return candidateRigidbody;
        }

        if (candidateTransform != null &&
            (candidateTransform == referenceRootTransform || candidateTransform.IsChildOf(referenceRootTransform)))
        {
            Rigidbody transformRigidbody = candidateTransform.GetComponentInParent<Rigidbody>();
            return IsMovementReferenceRigidbody(transformRigidbody, referenceRootTransform)
                ? transformRigidbody
                : MainSpaceship.Instance.GetComponent<Rigidbody>();
        }

        return null;
    }

    private bool IsMovementReferenceRigidbody(Rigidbody candidateRigidbody, Transform referenceRootTransform)
    {
        if (candidateRigidbody == null || candidateRigidbody == rb)
        {
            return false;
        }

        if (candidateRigidbody.GetComponentInParent<Item>() != null)
        {
            return false;
        }

        return candidateRigidbody.transform == referenceRootTransform ||
            candidateRigidbody.transform.IsChildOf(referenceRootTransform);
    }

    private void EnterMovementReference(Rigidbody movementReferenceRigidbody)
    {
        if (movementReferenceRigidbody == null)
        {
            return;
        }

        if (movementReferenceContacts.TryGetValue(movementReferenceRigidbody, out int contactCount))
        {
            movementReferenceContacts[movementReferenceRigidbody] = contactCount + 1;
        }
        else
        {
            movementReferenceContacts.Add(movementReferenceRigidbody, 1);
        }

        if (activeMovementReferenceRigidbody == null)
        {
            SetActiveMovementReferenceRigidbody(movementReferenceRigidbody);
        }
    }

    private void ExitMovementReference(Rigidbody movementReferenceRigidbody)
    {
        if (movementReferenceRigidbody == null ||
            !movementReferenceContacts.TryGetValue(movementReferenceRigidbody, out int contactCount))
        {
            return;
        }

        if (contactCount <= 1)
        {
            movementReferenceContacts.Remove(movementReferenceRigidbody);
        }
        else
        {
            movementReferenceContacts[movementReferenceRigidbody] = contactCount - 1;
        }

        if (activeMovementReferenceRigidbody == movementReferenceRigidbody &&
            !movementReferenceContacts.ContainsKey(movementReferenceRigidbody))
        {
            SetActiveMovementReferenceRigidbody(ChooseMovementReferenceRigidbody());
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (PickedUpBy != null)
        {
            return;
        }

        EnterMovementReference(GetMovementReferenceRigidbody(collision.rigidbody, collision.transform));
    }
    private void OnCollisionExit(Collision collision)
    {
        if (PickedUpBy != null)
        {
            return;
        }

        Rigidbody movementReferenceRigidbody = GetMovementReferenceRigidbody(collision.rigidbody, collision.transform);
        if (movementReferenceRigidbody != null)
        {
            ExitMovementReference(movementReferenceRigidbody);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (PickedUpBy != null)
        {
            return;
        }

        EnterMovementReference(GetMovementReferenceRigidbody(other.attachedRigidbody, other.transform));
    }
    private void OnTriggerExit(Collider other)
    {
        if (PickedUpBy != null)
        {
            return;
        }

        Rigidbody movementReferenceRigidbody = GetMovementReferenceRigidbody(other.attachedRigidbody, other.transform);
        if (movementReferenceRigidbody != null)
        {
            ExitMovementReference(movementReferenceRigidbody);
        }
    }
    public void ChangeItemOwner(ulong newowner)
    {
        NMS_Both_PickUpItem message = new NMS_Both_PickUpItem(netObj.Identity.Identifier, newowner);
        message.SendMessageAsServerOrClient();
    }
    public void Network_onPickUPorDrop(ulong newowner)
    {
        Network_onPickUPorDrop(newowner, transform.position, OriginalRotation, Vector3.zero, 0f);
    }

    public void Network_onPickUPorDrop(ulong newowner, Vector3 dropPosition, Quaternion dropRotation, Vector3 throwDirection, float throwForce)
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
            NetworkPlayerObject player = NetworkSystem.Instance.GetPlayer(netObj.Identity.Sovereignty);
            if (player == null) return;

            PlayerMain who = player.playerControl;
            gotDropped(who, dropPosition, dropRotation, throwDirection, throwForce);
            
        }
        else
        {
            NetworkPlayerObject player = NetworkSystem.Instance.GetPlayer(newowner);
            if (player == null) return;

            PlayerMain who = player.playerControl;
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
    private void ExcludeSlotLayerFromColliders(Slot slot)
    {
        RestoreColliderExcludeLayers();

        if (slot == null)
        {
            return;
        }

        if (colliders == null)
        {
            colliders = GetComponentsInChildren<Collider>();
        }

        int slotLayerMask = 1 << slot.gameObject.layer;
        colliderExcludeLayersBeforeAttach = new LayerMask[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null)
            {
                continue;
            }

            LayerMask excludeLayers = collider.excludeLayers;
            colliderExcludeLayersBeforeAttach[i] = excludeLayers;
            excludeLayers.value |= slotLayerMask;
            collider.excludeLayers = excludeLayers;
        }
    }
    private void RestoreColliderExcludeLayers()
    {
        if (colliders == null || colliderExcludeLayersBeforeAttach == null)
        {
            colliderExcludeLayersBeforeAttach = null;
            return;
        }

        int colliderCount = Mathf.Min(colliders.Length, colliderExcludeLayersBeforeAttach.Length);
        for (int i = 0; i < colliderCount; i++)
        {
            Collider collider = colliders[i];
            if (collider != null)
            {
                collider.excludeLayers = colliderExcludeLayersBeforeAttach[i];
            }
        }

        colliderExcludeLayersBeforeAttach = null;
    }
    private void gotPickedup(PlayerMain who)


    {
        Debug.Log($"{name} picked up by {who.name}");
        who.PickUp(this);
        PickedUpBy = who;

        if (who.Equals(GameCore.Instance.Local_Player))
        {
            UIManager.Instance.ShowInteraction("Drop", who.control.Player.pickup.GetBindingDisplayString(), 0);
        }
        transform.SetParent(who.HandTransform);
        rb.constraints = RigidbodyConstraints.FreezeAll;

        if (netObj.AbstractObject != null)
        {
            ApplySnapshot(netObj.AbstractObject.holdState);
        }
        else
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }



        rb.linearVelocity = Vector3.zero;
        SetColliders(false);
    }
    private void gotDropped(PlayerMain who, Vector3 dropPosition, Quaternion dropRotation, Vector3 throwDirection, float throwForce)
    {

        Debug.Log($"{name} dropped by {who.name}");
        who.Drop(this);
        PickedUpBy = null;
        if (who.Equals(GameCore.Instance.Local_Player))
        {
            UIManager.Instance.HideInteraction(0);

        }
        transform.parent = null;
        transform.localScale = snapshot_start.scale;

        //ApplySnapshot(snapshot_start);
        EnableRB();
        rb.constraints = RigidbodyConstraints.None;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = dropPosition;
        transform.rotation = dropRotation;
        if (throwForce > 0f && throwDirection.sqrMagnitude > 0.001f)
        {
            rb.AddForce(throwDirection.normalized * throwForce, ForceMode.VelocityChange);
        }
        SetColliders(true);
    }
    public void AttachToSlot(Slot slot, Quaternion rot) //Dont use this directly, use slot.Attach(item) instead, this is just for internal use
    {
        AttachedSlot = slot;
        ExcludeSlotLayerFromColliders(slot);
        DisableRB();
        transform.localScale = snapshot_start.scale;
        transform.SetParent(slot.transform);

        transform.localPosition = Vector3.zero;
        transform.localRotation = rot;
        netObj.Sync_Transform = false;
    }
    public void DetachFromSlot()
    {
        RestoreColliderExcludeLayers();
        AttachedSlot = null;
        EnableRB();
        transform.SetParent(null);
    }




    public NetworkGameObject GetNetworkObject()
    {
        // Lazily resolve if the cached reference is stale (e.g. the NetworkGameObject was
        // added after this Item's OnEnable had already run, or it lives on a child).
        if (netObj == null)
        {
            netObj = GetComponentInChildren<NetworkGameObject>();
        }
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



}
