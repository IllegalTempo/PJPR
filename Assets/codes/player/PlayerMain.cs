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

    public Item seenOutline = null;
    public Item clickedOutline = null;

    public GameObject cam;
    public NetworkPlayerObject networkinfo;

    public Item holdingItem = null;


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
    private void onSelectItem(Item item)
    {
        item.OnClicked();
        holdingItem = item;

    }
    private void PlayerControl()
    {
        controller.Move(Vector3.down * Time.deltaTime * 5);
        yaw += lookSpeed * Input.GetAxis("Mouse X");
        pitch -= lookSpeed * Input.GetAxis("Mouse Y");
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        transform.eulerAngles = new Vector3(pitch, yaw, 0f);

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
        if (Physics.Raycast(ray, out hit, 100f, GameCore.instance.Masks.SelectableItems))
        {

            seenOutline = hit.collider.GetComponent<Item>();
            if (seenOutline == null) return;
            seenOutline.LookedAt = true;
            if (seenOutline != null && Input.GetMouseButtonDown(0))
            {
                clickedOutline = seenOutline;
                onSelectItem(clickedOutline);



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
