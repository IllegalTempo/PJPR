using Assets.codes.items;
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
public class PlayerMain : MonoBehaviour
{
    public float MoveSpeed = 0.5f;
    public float LookSpeed = 2f;
    public float MaxSpeed = 3f; // Maximum allowed speed
    public float JetPackForce = 0.5f;
    public float GroundCheckDistance = 0.3f;

    private float yaw = 0f;
    private float pitch = 0f;
    private Rigidbody rb;
    [SerializeField]
    private AudioSource audioSource;
    private Vector2 moveinput = Vector2.zero;
    private Vector2 lookinput = Vector2.zero;
    private bool usingvc = false;
    public Selectable seenObject = null;
    public Selectable clickedObject = null;
    public GameObject cam;
    public GameObject head;
    public NetworkPlayerObject networkinfo;

    public Item holdingItem = null;

    public Vector3 itemHoldOffset = new Vector3(0, 2f, 15f); // Position in front of the camera for held items

    private PlayerSettingsMenu settingsMenu;
    PlayerInputAction control;
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
        cam.SetActive(true);
        control = GameCore.INSTANCE.PlayerControl;

        control.Player.Move.performed += ctx => moveinput = ctx.ReadValue<Vector2>();
        control.Player.Move.canceled += ctx => moveinput = Vector2.zero;
        control.Player.Look.performed += ctx => lookinput = ctx.ReadValue<Vector2>();
        control.Player.Look.canceled += ctx => lookinput = Vector2.zero;
        control.Player.pickup.performed += ctx => OnClickPickUp();
        control.Player.Interact.performed += ctx => OnInteract();
        control.Player.voice.performed += ctx => OnClickVC();


        settingsMenu = GetComponent<PlayerSettingsMenu>();
        if (settingsMenu == null)
        {
            settingsMenu = gameObject.AddComponent<PlayerSettingsMenu>();
        }
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
        }
    }
    private void OnClickVC()
    {
        usingvc = !usingvc;
        if (usingvc)
        {
            GameCore.INSTANCE.vc.StartVoice();
        }
        else
        {
            GameCore.INSTANCE.vc.StopVoice();
        }


    }
    private void OnInteract()
    {
        IUsable usable = holdingItem as IUsable
                 ?? seenObject as IUsable;

        if (usable != null)
        {
            usable.OnInteract(this);
            if (NetworkSystem.Instance == null || !NetworkSystem.Instance.IsOnline) return;
            NetworkObject netObj = (usable as MonoBehaviour).GetComponent<NetworkObject>();
            if (netObj == null) return;
            if (NetworkSystem.Instance.IsServer)
            {
                ServerSend.DistributeDecorationInteract(networkinfo.steamID, netObj.Identifier);

            }
            else
            {
                ClientSend.SendDecorationInteract(netObj.Identifier);

            }
        }
        
        
    }
    private void Initialize_remote()
    {
        cam.SetActive(false);
        rb.isKinematic = true;
    }
    public void OnPickUpItem(Item Item)
    {
        holdingItem = Item;

    }

    public void OnDropItem(Item Item)
    {

        holdingItem = null;

    }
    private void onSelectObject(Selectable item)
    {
        item.OnClicked();

    }
    public void HoldingItem(Item item)
    {
        item.transform.position = cam.transform.position + cam.transform.rotation * itemHoldOffset;
        item.transform.rotation = cam.transform.rotation;

    }


    private void Jetpack()
    {
        rb.AddForce(Vector3.up * JetPackForce, ForceMode.Impulse);

    }
    private void Move()
    {


        Vector3 move = (cam.transform.forward * moveinput.y + cam.transform.right * moveinput.x);
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        Vector3 targetVelocity = move * MoveSpeed * MaxSpeed;

        rb.linearVelocity = targetVelocity;
        if(control.Player.jump.IsPressed())
        {
            Jetpack();
        }
    }
    private void Look()
    {
        float sens = GameCore.INSTANCE.Option.mouseSensitivity;
        yaw += LookSpeed * lookinput.x * sens;
        pitch -= LookSpeed * lookinput.y * sens;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        head.transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        transform.eulerAngles = new Vector3(0, yaw, 0f);

    }
    private void OnClickPickUp()
    {
        if (holdingItem != null)
        {
            OnDrop();
            return;
        }

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
    private void OnDrop()
    {
        holdingItem.Drop();

    }
    private void PlayerControl()
    {
        //if (settingsMenu != null && settingsMenu.IsMenuOpen)
        //{
        //    return;
        //}


        //if (IsFunctionInteractPressedThisFrame())
        //{
        //    OnFunctionInteract();
        //}

        Move();
        Look();


        // Outline logic
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, GameCore.INSTANCE.Masks.SelectableItems))
        {

            seenObject = hit.collider.GetComponent<Selectable>();
            if (seenObject == null) return;
            seenObject.LookedAt = true;

        }
        else
        {
            seenObject = null;
        }


    }
    void Update()
    {
        if (networkinfo.IsLocal)
        {
            PlayerControl();
        }

    }
    private void OnCollisionEnter(Collision collision)
    {
        if ((GameCore.INSTANCE.Masks.MoveWith.value & (1 << collision.gameObject.layer)) != 0)
        {
            //transform.SetParent(collision.transform);
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if ((GameCore.INSTANCE.Masks.MoveWith.value & (1 << collision.gameObject.layer)) != 0)
        {
            //transform.SetParent(null);
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
