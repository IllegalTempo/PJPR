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

    private float yaw = 0f;
    private float pitch = 0f;
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
        control.Player.Interact.performed += ctx => OnClickPickUp();
    }
    private void OnDisable()
    {
        control.Disable();
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
        Vector3 move = (transform.forward * moveinput.y + transform.right * moveinput.x);
        move.y = 0;
        if (move.sqrMagnitude > 0f)
        {
            movement = move.normalized * moveSpeed * maxSpeed;
            controller.Move(movement * Time.deltaTime);
        }
        else
        {
            movement = Vector3.zero;
            controller.Move(Vector3.zero); // Stop instantly
        }
    }
    private void Look()
    {
        yaw += lookSpeed * lookinput.x;
        pitch -= lookSpeed * lookinput.y;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        head.transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        transform.eulerAngles = new Vector3(0, yaw, 0f);

    }
    private void OnClickPickUp()
    {
        if(holdingItem == null)
        {
            if (seenObject != null)
            {
                clickedObject = seenObject;
                onSelectObject(clickedObject);

            }
        } else
        {
            OnDrop();
        }
       
    }
    private void OnDrop()
    {
        holdingItem.Drop();

    }
    private void PlayerControl()
    {
        
        controller.Move(Vector3.down * Time.deltaTime * 5);
        Move();
        Look();
        

        // Outline logic
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;
        if(holdingItem == null)
        {
            if (Physics.Raycast(ray, out hit, 100f, GameCore.instance.Masks.SelectableItems))
            {

                seenObject = hit.collider.GetComponent<Selectable>();
                if (seenObject == null) return;
                seenObject.LookedAt = true;
                
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
}
