
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

    public float MoveSpeed = 1f;
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

    public Transform HandTransform;

    PlayerInputAction control;

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
            GameCore.Instance.vc.StartVoice();
        }
        else
        {
            GameCore.Instance.vc.StopVoice();
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
                ServerSend.DistributeInteract(networkinfo.steamID, netObj.Identifier);

            }
            else
            {
                ClientSend.SendInteract(netObj.Identifier);

            }
        }
        
        
    }
    private void Initialize_remote()
    {
        cam.SetActive(false);
        rb.isKinematic = true;
    }
    public void OnPickUpItem(Item Item,NetworkObject netObj)
    {
        holdingItem = Item;
        if(networkinfo.IsLocal)
        {
            LOCAL_OnPickUpItem(Item,netObj);
        }


    }
    private void LOCAL_OnPickUpItem(Item Item,NetworkObject netObj)
    {
        UIManager.Instance.ShowInteraction("Drop", control.Player.pickup.GetBindingDisplayString(), 0);
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
    public void OnDropItem(Item Item,NetworkObject netObj)
    {
        holdingItem = null;

        if (networkinfo.IsLocal)
        {
            UIManager.Instance.HideInteraction(0);
            LOCAL_OnDropItem(Item, netObj);

        }

    }
    private void LOCAL_OnDropItem(Item Item,NetworkObject netObj)
    {
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
    private void onSelectObject(Selectable item)
    {
        item.OnClicked();

    }
    public void HoldingItem(Item item)
    {
        //if(!item.lockRelativeRotation)
        //{
        //    item.transform.rotation = cam.transform.rotation;

        //}
        
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
        float sens = GameCore.Instance.Option.mouseSensitivity;
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
            Item it = holdingItem;
            OnDrop();
            if (seenObject is slot s)
            {
                s.AttachItem(it);
                return;
            }
            
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
        OnDropItem(holdingItem,holdingItem.GetNetworkObject());

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
        Selectable before = seenObject;
        if (Physics.Raycast(ray, out hit, 100f, GameCore.Instance.Masks.SelectableItems))
        {

            seenObject = hit.collider.GetComponent<Selectable>();
            if (seenObject == null) return;

        }
        else
        {
            seenObject = null;
        }
        UpdateSeenObject(before, before == seenObject);

    }
    private void UpdateSeenObject(Selectable before,bool lookedat)
    {
        if(before != null)
        {
            if (lookedat)
            {

                before.onLookedAt();
                if(before is IUsable)
                {
                    UIManager.Instance.ShowInteraction("Use", control.Player.Interact.GetBindingDisplayString(),1);
                }
                if(before is Item)
                {
                    UIManager.Instance.ShowInteraction("Pick Up", control.Player.pickup.GetBindingDisplayString(), 0);

                }
            }
            else
            {
                before.onLookedAway();
                UIManager.Instance.HideAllInteraction();
            }
        }
        
    }
    void Update()
    {
        if (networkinfo.IsLocal)
        {
            PlayerControl();
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
