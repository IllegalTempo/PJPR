using Assets.codes.items;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMain : MonoBehaviour
{
    public float moveSpeed = 0.5f;
    public float lookSpeed = 2f;
    public float maxSpeed = 3f; // Maximum allowed speed
    public float jumpHeight = 1.6f;
    public float gravity = -25f;
    public float groundedStickVelocity = -2f;

    private float yaw = 0f;
    private float pitch = 0f;
    private float verticalVelocity = 0f;
    private CharacterController controller;
    private Vector3 movement = Vector3.zero;
    private Vector2 moveinput = Vector2.zero;
    private Vector2 lookinput = Vector2.zero;
    public Selectable seenObject = null;
    public Selectable clickedObject = null;
    public GameObject cam;
    public GameObject head;
    public NetworkPlayerObject networkinfo;

    public Item holdingItem = null;

    public Vector3 itemHoldOffset = new Vector3(0, 2f, 15f); // Position in front of the camera for held items

    private PlayerSettingsMenu settingsMenu;

    void Start()
    {

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        controller = GetComponent<CharacterController>();
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
        PlayerInputAction control = GameCore.instance.control;

        control.Player.Move.performed += ctx => moveinput = ctx.ReadValue<Vector2>();
        control.Player.Move.canceled += ctx => moveinput = Vector2.zero;
        control.Player.Look.performed += ctx => lookinput = ctx.ReadValue<Vector2>();
        control.Player.Look.canceled += ctx => lookinput = Vector2.zero;
        control.Player.pickup.performed += ctx => OnPickUp();
        control.Player.jump.performed += ctx => Jump();
        control.Player.Interact.performed += ctx => OnInteract();


        settingsMenu = GetComponent<PlayerSettingsMenu>();
        if (settingsMenu == null)
        {
            settingsMenu = gameObject.AddComponent<PlayerSettingsMenu>();
        }
    }
    private void OnDisable()
    {
        PlayerInputAction control = GameCore.instance.control;

        if (control != null)
        {
            control.Player.Move.performed -= ctx => moveinput = ctx.ReadValue<Vector2>();
            control.Player.Move.canceled -= ctx => moveinput = Vector2.zero;
            control.Player.Look.performed -= ctx => lookinput = ctx.ReadValue<Vector2>();
            control.Player.Look.canceled -= ctx => lookinput = Vector2.zero;
            control.Player.pickup.performed -= ctx => OnPickUp();
            control.Player.jump.performed -= ctx => Jump();
            control.Player.Interact.performed -= ctx => OnInteract();
        }
    }
    private void OnInteract()
    {
        if (holdingItem != null && holdingItem is usableItem usi)
        {
            usi.OnInteract();
        }
        if(seenObject is InteractableDecoration sid)
        {
            sid.OnInteract();
        }
    }
    private void Initialize_remote()
    {
        cam.SetActive(false);
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
    private void Jump()
    {
        bool grounded = controller.isGrounded;
        if (grounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedStickVelocity;
        }


        verticalVelocity += gravity * Time.deltaTime;
        if (grounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        }
    }
    private void Move()
    {


        Vector3 move = (transform.forward * moveinput.y + transform.right * moveinput.x);
        move.y = 0f;
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        Vector3 horizontal = move * moveSpeed * maxSpeed;
        movement = horizontal;

        Vector3 velocity = horizontal + (Vector3.up * gravity);
        controller.Move(velocity * Time.deltaTime);

        if (horizontal.sqrMagnitude <= 0.0001f)
        {
            movement = Vector3.zero;
        }
    }
    private void Look()
    {
        float sens = GameCore.instance.option.mouseSensitivity;
        yaw += lookSpeed * lookinput.x * sens;
        pitch -= lookSpeed * lookinput.y * sens;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        head.transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        transform.eulerAngles = new Vector3(0, yaw, 0f);

    }
    private void OnPickUp()
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
        if (Physics.Raycast(ray, out hit, 100f, GameCore.instance.Masks.SelectableItems))
        {

            seenObject = hit.collider.GetComponent<Selectable>();
            if (seenObject == null) return;
            seenObject.LookedAt = true;

        } else
        {
            seenObject= null;
        }


    }
    void Update()
    {
        if (networkinfo.IsLocal)
        {
            PlayerControl();
        }

    }

    
}
