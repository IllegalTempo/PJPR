using UnityEngine;
using System.Collections;

public partial class PlayerMain: MonoBehaviour
{
    public float MoveSpeed = 1f;
    public float LookSpeed = 2f;
    public float MaxSpeed = 3f; // Maximum allowed speed
    public float JetPackForce = 2f;
    public float Gravity = 2f;
    private Vector2 moveinput = Vector2.zero;
    private Vector2 lookinput = Vector2.zero;

    public float maxVerticalVelocity = 100f;
    private void Jetpack()
    {
        rb.AddForce(Vector3.up * JetPackForce, ForceMode.Acceleration);

    }
    private void Move()
    {


        Vector3 move = (cam.transform.forward * moveinput.y + cam.transform.right * moveinput.x);
        move.y = 0f;
        move.Normalize();
        

        Vector3 targetVelocity = move * MoveSpeed * MaxSpeed;
        targetVelocity.y = Mathf.Clamp(rb.linearVelocity.y, -maxVerticalVelocity, maxVerticalVelocity);
        rb.AddForce(Vector3.down * Gravity, ForceMode.Acceleration);
        rb.linearVelocity = targetVelocity;
        if (control.Player.jump.IsPressed())
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
}
