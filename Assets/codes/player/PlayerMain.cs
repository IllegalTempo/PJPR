using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMain : MonoBehaviour
{
    public PlayerInputAction control;
    public float moveSpeed = 0.5f;
    public float lookSpeed = 2f;
    public float maxSpeed = 3f; // Maximum allowed speed
    public float jumpHeight = 1.6f;
    public float gravity = -25f;
    public float groundedStickVelocity = -2f;
    
    private float yaw = 0f;
    private float pitch = 0f;
    private float verticalVelocity = 0f;
    private bool jumpQueued = false;
    private CharacterController controller;
    private Vector3 movement = Vector3.zero;
    private Vector2 moveinput = Vector2.zero;
    private Vector2 lookinput = Vector2.zero;
    public Selectable seenObject = null;
    public Selectable clickedObject = null;
    private interactable seenInteractable = null;
    private interactable clickedInteractable = null;

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
        control = new PlayerInputAction();
        string rebinds = PlayerPrefs.GetString("inputRebinds", string.Empty);
        control.LoadBindingOverridesFromJson(rebinds);
        control.Enable();
        control.Player.Move.performed += ctx => moveinput = ctx.ReadValue<Vector2>();
        control.Player.Move.canceled += ctx => moveinput = Vector2.zero;
        control.Player.Look.performed += ctx => lookinput = ctx.ReadValue<Vector2>();
        control.Player.Look.canceled += ctx => lookinput = Vector2.zero;
        control.Player.Interact.performed += ctx => OnPrimaryInteract();

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
            control.Disable();
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
    private void Move()
    {
        bool grounded = controller.isGrounded;
        if (grounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedStickVelocity;
        }

        if (jumpQueued)
        {
            if (grounded)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            jumpQueued = false;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = (transform.forward * moveinput.y + transform.right * moveinput.x);
        move.y = 0f;
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        Vector3 horizontal = move * moveSpeed * maxSpeed;
        movement = horizontal;

        Vector3 velocity = horizontal + (Vector3.up * verticalVelocity);
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
    private void OnPrimaryInteract()
    {
        if (holdingItem != null)
        {
            if (IsHoldingRepairToolAndLookingAtRepairablePart())
            {
                return;
            }

            OnDrop();
            return;
        }

        if (seenObject == null)
        {
            return;
        }

        if (seenObject.IsFunctionKeyOnly())
        {
            return;
        }

        clickedObject = seenObject;
        onSelectObject(clickedObject);
    }

    private bool IsHoldingRepairToolAndLookingAtRepairablePart()
    {
        if (holdingItem == null || !holdingItem.IsRepairTool)
        {
            return false;
        }

        if (cam == null)
        {
            return false;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            return false;
        }

        SpaceshipPart part = hit.collider.GetComponentInParent<SpaceshipPart>();
        return part != null && part.CanStartRepairWithHeldItem(holdingItem);
    }

    private void OnFunctionInteract()
    {
        if (holdingItem != null || seenInteractable == null)
        {
            return;
        }

        if (!seenInteractable.IsFunctionKeyOnly())
        {
            return;
        }

        clickedInteractable = seenInteractable;
        onSelectObject(clickedInteractable);
    }
    private void OnDrop()
    {
        holdingItem.Drop();

    }
    private void PlayerControl()
    {
        if (settingsMenu != null && settingsMenu.IsMenuOpen)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            jumpQueued = true;
        }

        if (IsFunctionInteractPressedThisFrame())
        {
            OnFunctionInteract();
        }

        Move();
        Look();
        

        // Outline logic
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;
        if(holdingItem == null)
        {
            bool foundSelectable = false;

            LayerMask selectableMask = GameCore.instance != null && GameCore.instance.Masks != null
                ? GameCore.instance.Masks.SelectableItems
                : 0;

            if (selectableMask.value != 0 && Physics.Raycast(ray, out hit, 100f, selectableMask))
            {
                seenObject = hit.collider.GetComponentInParent<Selectable>();
                seenInteractable = hit.collider.GetComponentInParent<interactable>();
                if (seenObject != null)
                {
                    seenObject.LookedAt = true;
                    foundSelectable = true;
                }
            }

            if (!foundSelectable && Physics.Raycast(ray, out hit, 100f))
            {
                seenObject = hit.collider.GetComponentInParent<Selectable>();
                seenInteractable = hit.collider.GetComponentInParent<interactable>();
                if (seenObject != null)
                {
                    seenObject.LookedAt = true;
                    foundSelectable = true;
                }
            }

            if (!foundSelectable)
            {
                seenObject = null;
                seenInteractable = null;
            }
        }
        else
        {
            seenObject = null;
            seenInteractable = null;
        } 
        
    }
    void Update()
    {
        if (networkinfo.IsLocal)
        {
            PlayerControl();
        }

    }

    private bool IsFunctionInteractPressedThisFrame()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        var keyControl = Keyboard.current[(Key)PlayerSettingsMenu.GetFunctionInteractKey()];
        return keyControl != null && keyControl.wasPressedThisFrame;
    }
}
