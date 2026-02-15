using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMain : MonoBehaviour
{
    public float moveSpeed = 0.5f;
    public float lookSpeed = 2f;
    public float maxSpeed = 3f; // Maximum allowed speed

    private float yaw = 0f;
    private float pitch = 0f;
    private CharacterController controller;
    private Vector3 moveInput = Vector3.zero;

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
    private void PlayerControl()
    {
        
        controller.Move(Vector3.down * Time.deltaTime * 5);
        yaw += lookSpeed * Input.GetAxis("Mouse X");
        pitch -= lookSpeed * Input.GetAxis("Mouse Y");
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        head.transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        transform.eulerAngles = new Vector3(0, yaw, 0f);

        // WASD movement (direct, instant, with collision)
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        float moveX = Input.GetAxisRaw("Horizontal"); // A/D
        float moveZ = Input.GetAxisRaw("Vertical");   // W/S

        Vector3 move = (forward * moveZ + right * moveX);
        move.y = 0;
        if (move.sqrMagnitude > 0f)
        {
            moveInput = move.normalized * moveSpeed * maxSpeed;
            controller.Move(moveInput * Time.deltaTime);
        }
        else
        {
            moveInput = Vector3.zero;
            controller.Move(Vector3.zero); // Stop instantly
        }

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
                if (seenObject != null && Input.GetMouseButtonDown(0))
                {
                    clickedObject = seenObject;
                    onSelectObject(clickedObject);



                }
            }
        } else
        {
            if (Input.GetMouseButtonDown(0))
            {
                holdingItem.Drop();
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
