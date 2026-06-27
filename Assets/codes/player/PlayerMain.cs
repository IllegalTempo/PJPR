
using Assets.codes.Network.Messages;
using Steamworks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

/// <summary>
/// This is the brain of a player, most action of the player is done here, such as movement, looking around, picking up items, interacting with objects, and voice chat control. It also handles the player's camera and what they are currently looking at or holding. This script is attached to the player GameObject and requires a Rigidbody component for physics-based movement.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public partial class PlayerMain : MonoBehaviour
{


    public float GroundCheckDistance = 0.3f;

    private float yaw = 0f;
    private float pitch = 0f;
    private Rigidbody rb;
    [SerializeField]
    private AudioSource audioSource;

    private bool usingvc = false;
    public Selectable seenObject = null;
    public Selectable clickedObject = null;
    public GameObject cam;
    public GameObject head;
    public NetworkPlayerObject networkinfo;

    public Item holdingItem = null;

    public Transform HandTransform;

    public PlayerInputAction control;
    private IUsable activeUsable;

    [SerializeField]
    private GameObject[] LocalInvisible;
    void Start()
    {

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        if (networkinfo.IsLocal)
        {
            Initialize_local();
        }
        else
        {
            Initialize_remote();

        }
    }

    private void Initialize_local()
    {
        foreach (GameObject obj in LocalInvisible)
        {
            obj.SetActive(false);
        }
        cam.SetActive(true);
        control = GameCore.Instance.PlayerControl;

        control.Player.Move.performed += ctx => moveinput = ctx.ReadValue<Vector2>();
        control.Player.Move.canceled += ctx => moveinput = Vector2.zero;
        control.Player.Look.performed += ctx => lookinput = ctx.ReadValue<Vector2>();
        control.Player.Look.canceled += ctx => lookinput = Vector2.zero;
        control.Player.pickup.performed += ctx => OnClickPickUp();
        control.Player.Interact.performed += ctx => OnInteract();
        control.Player.voice.performed += ctx => OnClickVC();
        control.Player.rotate.performed += ctx => OnClickSlotRotate();


    }

    private void OnDisable()
    {


        if (control != null)
        {
            control.Player.Move.performed -= ctx => moveinput = ctx.ReadValue<Vector2>();
            control.Player.Move.canceled -= ctx => moveinput = Vector2.zero;
            control.Player.Look.performed -= ctx => lookinput = ctx.ReadValue<Vector2>();
            control.Player.Look.canceled -= ctx => lookinput = Vector2.zero;
            control.Player.pickup.performed -= ctx => OnClickPickUp();
            control.Player.Interact.performed -= ctx => OnInteract();
            control.Player.voice.performed -= ctx => OnClickVC();
            control.Player.rotate.performed -= ctx => OnClickSlotRotate();
        }
    }
    private void OnClickVC()
    {
        usingvc = !usingvc;
        if (usingvc)
        {
            GameCore.Instance.vc.StartVoice();
        }
        else
        {
            GameCore.Instance.vc.StopVoice();
        }


    }
    private void OnInteract()
    {
        IUsable usable = activeUsable
                 ?? holdingItem as IUsable
                 ?? seenObject as IUsable;

        if (usable != null)
        {
            usable.OnInteract(this);
            if (NetworkSystem.Instance == null || !NetworkSystem.Instance.IsOnline) return;
            NetworkPrefab netObj = (usable as MonoBehaviour).GetComponent<NetworkPrefab>();
            if (netObj == null) return;
            NMS_Both_Interact msg = new NMS_Both_Interact(networkinfo.steamID, netObj.Identifier);
            if (NetworkSystem.Instance.IsServer)
            {
                NetworkRouter.Instance.DistributeMessageToReady(msg);

            }
            else
            {
                NetworkRouter.Instance.SendMessageToServer(msg);

            }
        }
    }

    public void SetActiveUsable(IUsable usable)
    {
        activeUsable = usable;
    }

    public void ClearActiveUsable(IUsable usable)
    {
        if (activeUsable == usable)
        {
            activeUsable = null;
        }
    }

    private void Initialize_remote()
    {
        cam.SetActive(false);
        rb.isKinematic = true;
    }

    //



    //


    private void onSelectObject(Selectable item)
    {
        item.OnClicked();

    }
    private void SendDrop()
    {
        holdingItem.Network_Send_ChangeOwnerShip(0);
    }
    private void SendPickUP()
    {
        if (seenObject is Item it)
        {
            it.Network_Send_ChangeOwnerShip(networkinfo.steamID);
        }
    }
    private void OnClickSlotRotate()
    {
        if (holdingItem != null && holdingItem.BindSlot != null)
        {
            // Rotate the item 90 degrees around the slot's local Y-axis (up axis)
            // This allows the player to orient items in different rotations while bound to a slot
            Slot boundSlot = holdingItem.BindSlot;

            // Calculate 90 degree rotation around the slot's local Y-axis
            Quaternion rotationIncrement = Quaternion.AngleAxis(90f, boundSlot.transform.up);

            // Apply rotation relative to slot's current rotation
            holdingItem.transform.rotation = rotationIncrement * holdingItem.transform.rotation;

            // Network sync: The item's NetworkObject has Sync_Transform enabled,
            // so the rotation change will be automatically synchronized to other players
            if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline)
            {
                NetworkPrefab netObj = holdingItem.GetNetworkObject();
                if (netObj != null)
                {
                    // Rotation sync happens automatically through NetworkObject
                    // Update NetworkRot to reflect the new rotation for consistency
                    netObj.NetworkRot = holdingItem.transform.rotation;
                }
            }

            Debug.Log($"Item rotated 90 degrees around {boundSlot.name}'s Y-axis");
        }
    }
    private void OnClickPickUp()
    {
        if (holdingItem != null)
        {
            Item it = holdingItem;
            SendDrop();
            if (seenObject is Slot s)
            {

                if (it.FitIn(s))
                    s.Attach(it);
                return;
            }

            return;
        }
        SendPickUP();
        if (seenObject == null)
        {
            return;
        }


        clickedObject = seenObject;
        onSelectObject(clickedObject);
    }

    //private bool IsHoldingRepairToolAndLookingAtRepairablePart()
    //{
    //    if (holdingItem == null || !holdingItem.IsRepairTool)
    //    {
    //        return false;
    //    }

    //    if (cam == null)
    //    {
    //        return false;
    //    }

    //    Ray ray = new Ray(cam.transform.position, cam.transform.forward);
    //    if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
    //    {
    //        return false;
    //    }

    //    SpaceshipPart part = hit.collider.GetComponentInParent<SpaceshipPart>();
    //    return part != null && part.CanStartRepairWithHeldItem(holdingItem);
    //}

    //private void OnFunctionInteract()
    //{
    //    if (holdingItem != null || seenInteractable == null)
    //    {
    //        return;
    //    }

    //    //if (!seenInteractable.IsFunctionKeyOnly())
    //    //{
    //    //    return;
    //    //}

    //    clickedInteractable = seenInteractable;
    //    onSelectObject(clickedInteractable);
    //}

    private void UpdateSeenObject(Selectable @new, Selectable before)
    {
        HandleSlotUnbinding(@new);

        // Handle looking away from previous object
        if (before != null)
        {
            before.onLookedAway();
            UIManager.Instance.HideAllInteraction();
        }
        // Handle looking at a new object
        if (@new != null)
        {
            @new.onLookedAt();
            HandleNewObjectUI(@new);
            HandleSlotBinding(@new);
        }

        // Handle slot unbinding if we're not looking at a compatible slot
        
    }

    private void HandleNewObjectUI(Selectable @new)
    {
        if (@new is IUsable)
        {
            UIManager.Instance.ShowInteraction("Use", control.Player.Interact.GetBindingDisplayString(), 1);
        }

        if (@new is Item)
        {
            UIManager.Instance.ShowInteraction("Pick Up", control.Player.pickup.GetBindingDisplayString(), 0);
        }

        if (@new is Slot s && holdingItem != null && holdingItem.FitIn(s))
        {
            UIManager.Instance.ShowInteraction("Install", control.Player.pickup.GetBindingDisplayString(), 0);
            UIManager.Instance.ShowInteraction("Rotate", control.Player.rotate.GetBindingDisplayString(), 1);
        }
        else if (@new is Slot)
        {
            UIManager.Instance.ShowInteraction("X", "", 0);
        }
    }

    private void HandleSlotBinding(Selectable @new)
    {
        if (@new is Slot s && holdingItem != null && holdingItem.FitIn(s))
        {
            holdingItem.Bind(s);
        }
    }

    private void HandleSlotUnbinding(Selectable @new)
    {
        if (holdingItem == null || holdingItem.BindSlot == null) return;
        //if (!isCompatibleSlot && holdingItem != null && holdingItem.BindSlot != null)
        if(@new is not Slot || (@new is Slot s  && !holdingItem.FitIn(s)))
        {
            holdingItem.Unbind();
        }
    }
    void Update()
    {
        if (networkinfo.IsLocal)
        {
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
    private void OnTriggerEnter(Collider other)
    {
        if ((GameCore.Instance.Masks.MoveWith.value & (1 << other.gameObject.layer)) != 0)
        {
            transform.SetParent(other.transform);
        }
    }
    private void OnTriggerExit(Collider collision)
    {
        if ((GameCore.Instance.Masks.MoveWith.value & (1 << collision.gameObject.layer)) != 0)
        {
            transform.SetParent(null);
        }
    }
    public void ReceiveVoice(byte[] bytesArray)
    {
        if (bytesArray == null || bytesArray.Length == 0)
            return;

        // ¢w¢w Step 1: Convert 16-bit signed PCM bytes ¡÷ float[-1..1] ¢w¢w
        float[] floatSamples = new float[bytesArray.Length / 2];   // 2 bytes per sample

        for (int i = 0; i < floatSamples.Length; i++)
        {
            // Read two bytes ¡÷ little-endian signed 16-bit integer
            short pcmValue = (short)(
                (bytesArray[i * 2 + 1] << 8) |           // high byte
                (bytesArray[i * 2] & 0xFF)               // low byte (mask to prevent sign extension)
            );

            // Normalize to float range [-1.0 .. 1.0]
            floatSamples[i] = pcmValue / 32767f;        // 32767 = short.MaxValue
        }

        // ¢w¢w Step 2: Create or update the clip ¢w¢w
        // Important: For streaming voice, it's better NOT to create a new clip every packet!
        //            Create once (when first voice arrives), then keep SetData() on it.

        // Option A: Simple version (new clip every packet) ¡V works but causes small gaps/clicks
        AudioClip remoteClip = AudioClip.Create(
            "remoteVoice",
            floatSamples.Length,               // number of samples this packet contains
            1,                                 // mono
            recording.SAMPLE_RATE,
            stream: false                      // stream:true is only useful with repeated SetData()
        );

        remoteClip.SetData(floatSamples, 0);

        audioSource.clip = remoteClip;
        audioSource.Play();

        // ¢w¢w Option B: Better for real voice chat (recommended) ¢w¢w
        // Use one persistent clip + rolling buffer + SetData(offset)
        // See explanation below
    }
}
