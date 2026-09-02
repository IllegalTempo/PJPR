using Assets.codes.Network.Messages;
using Assets.codes.Network.SyncedIdentity;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// This is the brain of a player, most action of the player is done here, such as movement, looking around, picking up items, interacting with objects, and voice chat control. It also handles the player's camera and what they are currently looking at or holding. This script is attached to the player GameObject and requires a Rigidbody component for physics-based movement.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public partial class PlayerMain : MonoBehaviour
{
    private const int PrimaryInteractionIndex = 0;
    private const int SecondaryInteractionIndex = 1;

    private float yaw = 0f;
    private float pitch = 0f;
    private Rigidbody rb;
    public bool InSpaceship => collisionCount > 0;
    [SerializeField]
    private AudioSource audioSource;

    private bool usingVoiceChat = false;
    public Selectable seenObject = null;
    public GameObject cam;
    public GameObject head;
    public NetworkPlayerObject networkinfo;

    public Item holdingItem = null;

    public Transform HandTransform;

    [SerializeField]
    private float tapDropPlaceDistance = 1.2f;
    [SerializeField]
    private float dropCollisionRadius = 0.35f;
    [SerializeField]
    private float dropCollisionPadding = 0.05f;
    [SerializeField]
    private LayerMask dropCollisionMask = Physics.DefaultRaycastLayers;
    [SerializeField]
    private float throwHoldThreshold = 0.25f;
    [SerializeField]
    private float throwFullChargeTime = 1.5f;
    [SerializeField]
    private float maxThrowForce = 10f;
    [SerializeField]
    private float throwChargeFieldOfView = 45f;
    [SerializeField]
    private float throwCameraZoomTransitionSpeed = 60f;

    public PlayerInputAction control;

    [SerializeField]
    private GameObject[] LocalInvisible;


    private Interactable pressedUsable = null;
    private bool isChargingDrop;
    private float dropChargeStartedAt;
    private Item chargingDropItem;
    private Camera localCamera;
    private float normalCameraFieldOfView;
    private float targetCameraFieldOfView;
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        if (networkinfo.IsLocal)
        {
            InitializeLocal();
        }
        else
        {
            InitializeRemote();
        }
    }
    public Vector3 GetFacing()
    {
        return head.transform.forward;
    }
    public Quaternion GetHeadRotation()
    {
        return head.transform.rotation;     
    }
    private void InitializeLocal()
    {
        PlayerMain[] players = FindObjectsByType<PlayerMain>(FindObjectsSortMode.None);
        foreach (PlayerMain player in players)
        {
            if (player != this && player.networkinfo != null && player.networkinfo.IsLocal && player.cam != null)
            {
                player.cam.SetActive(false);
            }
        }

        foreach (GameObject obj in LocalInvisible)
        {
            obj.SetActive(false);
        }
        if (cam != null)
        {
            cam.SetActive(true);
            localCamera = cam.GetComponent<Camera>() ?? cam.GetComponentInChildren<Camera>();
            if (localCamera != null)
            {
                normalCameraFieldOfView = localCamera.fieldOfView;
                targetCameraFieldOfView = normalCameraFieldOfView;
            }
        }

        control = GameCore.Instance.PlayerControl;

        control.Player.Move.performed += OnMovePerformed;
        control.Player.Move.canceled += OnMoveCanceled;
        control.Player.Look.performed += OnLookPerformed;
        control.Player.Look.canceled += OnLookCanceled;
        control.Player.pickup.started += OnPickupStarted;
        control.Player.pickup.canceled += OnPickupCanceled;
        control.Player.Interact.performed += OnInteractPerformed;
        control.Player.Interact.canceled += OnInteractCanceled;
        control.Player.voice.performed += OnVoicePerformed;
        control.Player.rotate.performed += OnRotatePerformed;
    }

    private void OnDisable()
    {
        ResetThrowCameraZoom();

        if (control != null)
        {
            control.Player.Move.performed -= OnMovePerformed;
            control.Player.Move.canceled -= OnMoveCanceled;
            control.Player.Look.performed -= OnLookPerformed;
            control.Player.Look.canceled -= OnLookCanceled;
            control.Player.pickup.started -= OnPickupStarted;
            control.Player.pickup.canceled -= OnPickupCanceled;
            control.Player.Interact.performed -= OnInteractPerformed;
            control.Player.Interact.canceled -= OnInteractCanceled;
            control.Player.voice.performed -= OnVoicePerformed;
            control.Player.rotate.performed -= OnRotatePerformed;
        }
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        moveinput = ctx.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        moveinput = Vector2.zero;
    }

    private void OnLookPerformed(InputAction.CallbackContext ctx)
    {
        lookinput = ctx.ReadValue<Vector2>();
    }

    private void OnLookCanceled(InputAction.CallbackContext ctx)
    {
        lookinput = Vector2.zero;
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        OnInteractPressed();
    }

    private void OnInteractCanceled(InputAction.CallbackContext ctx)
    {
        OnInteractReleased();
    }

    private void OnVoicePerformed(InputAction.CallbackContext ctx)
    {
        ToggleVoiceChat();
    }

    private void OnRotatePerformed(InputAction.CallbackContext ctx)
    {
        RotateHeldItemInSlot();
    }

    private void ToggleVoiceChat()
    {
        usingVoiceChat = !usingVoiceChat;
        if (usingVoiceChat)
        {
            GameCore.Instance.vc.StartVoice();
        }
        else
        {
            GameCore.Instance.vc.StopVoice();
        }


    }
    private void OnInteractPressed()
    {
        if(seenObject == null)
        {
            return;
        }   
        Interactable usable = seenObject.usableOverride;

        if (usable != null)
        {
            usable.OnInteract_press(this);
            pressedUsable = usable;
        } else
        {
            if (holdingItem != null && holdingItem is Tool t)
            {
                t.OnUsingInteract(seenObject);
            }
        }

    }
    private void OnInteractReleased()
    {
        if (pressedUsable != null)
        {
            pressedUsable.OnInteract_release(this);
        }
    }


    private void InitializeRemote()
    {
        if (cam != null)
        {
            cam.SetActive(false);
        }

        rb.isKinematic = true;
    }

    //



    //


    private void SelectObject(Selectable item)
    {
        item.OnClicked();

    }

    private Item SendDropRequest(Item item, float throwForce)
    {
        Vector3 dropPosition = GetSafeDropPosition();
        new NMS_Both_PickUpItem(
            item.GetNetworkObject().Identity.Identifier,
            0,
            dropPosition,
            item.OriginalRotation,
            head.transform.forward,
            throwForce).SendMessageAsServerOrClient();
        return item;

    }
    private Vector3 GetSafeDropPosition()
    {
        Vector3 origin = head.transform.position;
        Vector3 direction = head.transform.forward;
        float radius = Mathf.Max(0.01f, dropCollisionRadius);
        float padding = Mathf.Max(0f, dropCollisionPadding);
        float maxDistance = tapDropPlaceDistance + radius + padding;
        float safeDistance = tapDropPlaceDistance;

        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, direction, maxDistance, dropCollisionMask, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (!IsDropPlacementBlocker(hit.collider))
            {
                continue;
            }

            safeDistance = Mathf.Min(safeDistance, Mathf.Max(0f, hit.distance - padding));
        }

        return origin + direction * safeDistance;
    }

    private bool IsDropPlacementBlocker(Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (collider.transform.IsChildOf(transform))
        {
            return false;
        }

        if (holdingItem != null && collider.transform.IsChildOf(holdingItem.transform))
        {
            return false;
        }

        return true;
    }
    private void SendPickupRequest(Item item)
    {
        NetworkGameObject nobj = item != null ? item.GetNetworkObject() : null;
        if (nobj == null || nobj.Identity == null || string.IsNullOrEmpty(nobj.Identity.Identifier))
        {
            Debug.LogWarning($"[PlayerMain] Cannot pick up '{(item != null ? item.name : "null")}': it has no NetworkObject / identifier. Add NetworkGameObject + NetworkPrefabIdentity to the item prefab.");
            return;
        }

        new NMS_Both_PickUpItem(nobj.Identity.Identifier, networkinfo.steamID).SendMessageAsServerOrClient();
    }

    private void RotateHeldItemInSlot()
    {
        if (holdingItem != null && holdingItem.BindSlot != null)
        {
            Slot boundSlot = holdingItem.BindSlot;
            Quaternion rotationIncrement = Quaternion.AngleAxis(90f, boundSlot.transform.up);
            holdingItem.transform.rotation = rotationIncrement * holdingItem.transform.rotation;

            Debug.Log($"Item rotated 90 degrees around {boundSlot.name}'s Y-axis");
        }
    }
    private void OnPickupStarted(InputAction.CallbackContext ctx)
    {
        if (holdingItem == null)
        {
            HandlePickupButton(0f);
            return;
        }

        isChargingDrop = true;
        dropChargeStartedAt = Time.time;
        chargingDropItem = holdingItem;
        UIManager.Instance.ShowThrowForce(0f);
        UpdateThrowCameraZoom(0f);
    }

    private void OnPickupCanceled(InputAction.CallbackContext ctx)
    {
        if (!isChargingDrop || holdingItem == null || holdingItem != chargingDropItem)
        {
            isChargingDrop = false;
            chargingDropItem = null;
            UIManager.Instance.HideThrowForce();
            ResetThrowCameraZoom();
            return;
        }

        float charge = CalculateThrowCharge01();
        float throwForce = charge * maxThrowForce;

        isChargingDrop = false;
        chargingDropItem = null;
        UIManager.Instance.HideThrowForce();
        ResetThrowCameraZoom();
        HandlePickupButton(throwForce);
    }

    private float CalculateThrowCharge01()
    {
        float heldTime = Time.time - dropChargeStartedAt;
        if (heldTime < throwHoldThreshold)
        {
            return 0f;
        }

        return Mathf.InverseLerp(throwHoldThreshold, throwFullChargeTime, heldTime);
    }

    private float CalculateThrowCameraZoom01()
    {
        float heldTime = Time.time - dropChargeStartedAt;
        return Mathf.InverseLerp(0f, throwFullChargeTime, heldTime);
    }

    private void UpdateThrowForceUI()
    {
        if (!isChargingDrop)
        {
            return;
        }

        float charge = CalculateThrowCharge01();
        UIManager.Instance.ShowThrowForce(charge);
        UpdateThrowCameraZoom(CalculateThrowCameraZoom01());
    }

    private void UpdateThrowCameraZoom(float charge)
    {
        if (localCamera == null)
        {
            return;
        }

        float zoomedFieldOfView = Mathf.Min(normalCameraFieldOfView, throwChargeFieldOfView);
        targetCameraFieldOfView = Mathf.Lerp(normalCameraFieldOfView, zoomedFieldOfView, Mathf.Clamp01(charge));
    }

    private void ResetThrowCameraZoom()
    {
        if (localCamera == null)
        {
            return;
        }

        targetCameraFieldOfView = normalCameraFieldOfView;
    }

    private void UpdateThrowCameraTransition()
    {
        if (localCamera == null)
        {
            return;
        }

        localCamera.fieldOfView = Mathf.MoveTowards(
            localCamera.fieldOfView,
            targetCameraFieldOfView,
            throwCameraZoomTransitionSpeed * Time.deltaTime);
    }

    private void HandlePickupButton(float throwForce)
    {
        if (holdingItem != null)
        {
            DropHeldItem(throwForce);
        }
        else
        {
            TryPickupSeenItem();
        }

        if (seenObject != null)
        {
            SelectObject(seenObject);
        }
    }

    private void DropHeldItem(float throwForce)
    {
        Item droppedItem = holdingItem;
        Quaternion heldRotation = droppedItem.transform.rotation;

        SendDropRequest(droppedItem, throwForce);
        if (seenObject == null)
        {
            return;
        }
        TryCombineDroppedItem(droppedItem);
        TryAttachDroppedItemToSlot(droppedItem, heldRotation);
    }

    private void TryCombineDroppedItem(Item droppedItem)
    {
        
        Item seenItem = seenObject.itemOverride;
        if (seenItem == null)
        {
            return;
        }

        if (!droppedItem.HasItemType(ItemType.Processable) || !seenItem.HasItemType(ItemType.Processable))
        {
            return;
        }

        NMS_Both_SendCombineItem combineMessage = new NMS_Both_SendCombineItem(
            droppedItem.GetNetworkObject().Identity.Identifier,
            seenItem.GetNetworkObject().Identity.Identifier);
        combineMessage.SendMessageAsServerOrClient();
    }

    private void TryAttachDroppedItemToSlot(Item droppedItem, Quaternion heldRotation)
    {

        Slot slot = seenObject.slotOverride;
        if (slot != null && droppedItem.FitIn(slot))
        {
            slot.SendAttach(droppedItem, heldRotation);
        }
    }

    private void TryPickupSeenItem()
    {
        if(seenObject == null)
        {
            return;
        }
        Item item = seenObject.itemOverride;
        if (item == null)
        {
            return;
        }

        if (item.AttachedSlot != null)
        {
            item.AttachedSlot.SendDetach();
        }

        SendPickupRequest(item);
    }

    private void UpdateSeenObject(Selectable current, Selectable previous)
    {
        UnbindHeldItemIfNeeded(current);

        if (previous != null)
        {
            previous.onLookedAway();
            UIManager.Instance.HideAllInteraction();
            UIManager.Instance.HideGameObjectName();
        }

        if (current != null)
        {
            current.onLookedAt();
            ShowSeenObjectUI(current);
            BindHeldItemToSlot(current);
        }
    }

    private void ShowSeenObjectUI(Selectable selectable)
    {
        if (selectable.usableOverride != null)
        {
            ShowSecondaryInteraction("Use", control.Player.Interact.GetBindingDisplayString());
        }

        ShowPrimaryObjectInteraction(selectable);
        UIManager.Instance.DisplayGameObjectName(GetSelectableDisplayName(selectable));
    }

    private string GetSelectableDisplayName(Selectable selectable)
    {
        NetworkGameObject networkObject = selectable.GetComponent<NetworkGameObject>();
        if (networkObject != null && networkObject.AbstractObject != null)
        {
            return networkObject.AbstractObject.itemName;
        }

        return selectable.gameObject.name;
    }

    private void ShowPrimaryObjectInteraction(Selectable selectable)
    {
        Item item = selectable.itemOverride;
        Slot slot = selectable.slotOverride;

        if (holdingItem == null)
        {
            if (item != null)
            {
                ShowPrimaryInteraction("Pick Up", control.Player.pickup.GetBindingDisplayString());
            }

            return;
        }

        if (slot != null)
        {
            ShowSlotInteraction(slot);
            return;
        }

        if (item != null && holdingItem.HasItemType(ItemType.Processable) && item.HasItemType(ItemType.Processable))
        {
            ShowPrimaryInteraction("Combine", control.Player.pickup.GetBindingDisplayString());
            return;
        }

        ShowPrimaryInteraction("Drop", control.Player.pickup.GetBindingDisplayString());
    }

    private void ShowSlotInteraction(Slot slot)
    {
        if (!holdingItem.FitIn(slot))
        {
            ShowPrimaryInteraction("Not Available", "");
            return;
        }

        if (slot is Port)
        {
            ShowPrimaryInteraction("Put", control.Player.pickup.GetBindingDisplayString());
            return;
        }

        ShowPrimaryInteraction("Install", control.Player.pickup.GetBindingDisplayString());
        ShowSecondaryInteraction("Rotate", control.Player.rotate.GetBindingDisplayString());
    }

    private void ShowPrimaryInteraction(string name, string key)
    {
        UIManager.Instance.ShowInteraction(name, key, PrimaryInteractionIndex);
    }

    private void ShowSecondaryInteraction(string name, string key)
    {
        UIManager.Instance.ShowInteraction(name, key, SecondaryInteractionIndex);
    }

    private void BindHeldItemToSlot(Selectable selectable)
    {
        Slot slot = selectable.slotOverride;
        if (slot != null && holdingItem != null && holdingItem.FitIn(slot) && slot is not Port)
        {
            holdingItem.Bind(slot);
        }
    }

    private void UnbindHeldItemIfNeeded(Selectable selectable)
    {
        if (holdingItem == null || holdingItem.BindSlot == null)
        {
            return;
        }
        if(selectable == null)
        {
            holdingItem.Unbind();
            return;
        }
        Slot slot = selectable.slotOverride;
        if (slot == null || !holdingItem.FitIn(slot))
        {
            holdingItem.Unbind();
        }
    }
    private void Update()
    {
        if (networkinfo.IsLocal)
        {
            UpdateThrowForceUI();
            UpdateThrowCameraTransition();
            PlayerControl();
        }

    }
    private void FixedUpdate()
    {
        if (networkinfo.IsLocal)
        {
            Move();
        }
    }
   
    public void ReceiveVoice(byte[] bytesArray)
    {
        if (bytesArray == null || bytesArray.Length == 0)
        {
            return;
        }

        float[] floatSamples = new float[bytesArray.Length / 2];
        for (int i = 0; i < floatSamples.Length; i++)
        {
            short pcmValue = (short)(
                (bytesArray[i * 2 + 1] << 8) |
                (bytesArray[i * 2] & 0xFF)
            );

            floatSamples[i] = pcmValue / 32767f;
        }

        AudioClip remoteClip = AudioClip.Create(
            "remoteVoice",
            floatSamples.Length,
            1,
            recording.SAMPLE_RATE,
            stream: false
        );

        remoteClip.SetData(floatSamples, 0);
        audioSource.clip = remoteClip;
        audioSource.Play();
    }
}

