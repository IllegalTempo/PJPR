using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerMain : MonoBehaviour
{
    public float MoveSpeed = 1f;
    public float LookSpeed = 2f;
    public float MaxSpeed = 5f; // Maximum allowed speed
    public float JetPackForce = 2f;
    private Vector2 moveinput = Vector2.zero;
    public Vector2 lookinput = Vector2.zero;

    public float maxVerticalVelocity = 100f;

    private int collisionCount = 0;
    private Rigidbody activeShipRigidbody;
    private Quaternion previousShipRotation;
    private bool hasPreviousShipRotation;

    private void Jetpack()
    {
        rb.AddForce(Vector3.up * JetPackForce, ForceMode.Acceleration);
        animator.SetBool("jetpack", true);
    }

    private void Move()
    {
        Vector3 move = (GetFacing() * moveinput.y + cam.transform.right * moveinput.x);
        move.y = 0f;
        float targetAnimatorSpeed = Mathf.Clamp01(move.magnitude);
        animator.SetFloat("speed", Mathf.Lerp(animator.GetFloat("speed"), targetAnimatorSpeed, Time.deltaTime * 10f));

        move.Normalize();

        ApplySpaceshipRotationDelta();
        Vector3 inputVelocity = move * MoveSpeed;
        Vector3 shipVelocity = GetSpaceshipVelocity();
        Vector3 currentRelativeVelocity = rb.linearVelocity - shipVelocity;
        Vector3 targetVelocity = shipVelocity + inputVelocity;
        targetVelocity.y = shipVelocity.y + Mathf.Clamp(currentRelativeVelocity.y, -maxVerticalVelocity, maxVerticalVelocity);
        rb.AddForce(targetVelocity - rb.linearVelocity, ForceMode.VelocityChange);
        //if (rb.linearVelocity.magnitude > MaxSpeed)
        //{
        //    rb.linearVelocity = rb.linearVelocity.normalized * MaxSpeed;

        //}

        if (control.Player.jump.IsPressed())
        {
            Jetpack();
        }

        if (IsMoveDownPressed())
        {
            rb.AddForce(Vector3.down * JetPackForce, ForceMode.Acceleration);
        }
        

        UpdateSpaceshipRotationTracking();
    }

    private bool IsMoveDownPressed()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null &&
            ((keyboard.leftCtrlKey != null && keyboard.leftCtrlKey.isPressed) ||
             (keyboard.rightCtrlKey != null && keyboard.rightCtrlKey.isPressed));
    }

    private void ApplySpaceshipRotationDelta()
    {
        Rigidbody shipRigidbody = GetActiveSpaceshipRigidbody();
        if (shipRigidbody == null)
        {
            hasPreviousShipRotation = false;
            return;
        }

        Quaternion shipRotation = shipRigidbody.rotation;
        if (!hasPreviousShipRotation)
        {
            previousShipRotation = shipRotation;
            hasPreviousShipRotation = true;
            return;
        }

        Quaternion deltaRotation = shipRotation * Quaternion.Inverse(previousShipRotation);
        Vector3 pivotToPlayer = rb.position - shipRigidbody.position;
        Vector3 rotatedPosition = shipRigidbody.position + deltaRotation * pivotToPlayer;

        rb.MovePosition(rotatedPosition);
        ApplyShipYawDelta(deltaRotation);
    }

    private void ApplyShipYawDelta(Quaternion deltaRotation)
    {
        Vector3 previousForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        Vector3 rotatedForward = Vector3.ProjectOnPlane(deltaRotation * transform.forward, Vector3.up);
        if (previousForward.sqrMagnitude <= Mathf.Epsilon || rotatedForward.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        yaw += Vector3.SignedAngle(previousForward, rotatedForward, Vector3.up);
    }

    private void UpdateSpaceshipRotationTracking()
    {
        Rigidbody shipRigidbody = GetActiveSpaceshipRigidbody();
        if (shipRigidbody == null)
        {
            hasPreviousShipRotation = false;
            return;
        }

        previousShipRotation = shipRigidbody.rotation;
    }

    private Vector3 GetSpaceshipVelocity()
    {
        Rigidbody shipRigidbody = GetActiveSpaceshipRigidbody();
        return shipRigidbody != null ? shipRigidbody.GetPointVelocity(rb.position) : Vector3.zero;
    }

    private Rigidbody GetActiveSpaceshipRigidbody()
    {
        if (!InSpaceship)
        {
            return null;
        }
        
        Rigidbody shipRigidbody = activeShipRigidbody;
        if (shipRigidbody == null && MainSpaceship.Instance != null)
        {
            shipRigidbody = MainSpaceship.Instance.GetComponent<Rigidbody>();
        }

        return shipRigidbody;
    }

    private Rigidbody GetSpaceshipRigidbody(Rigidbody candidateRigidbody, Transform candidateTransform)
    {
        if (MainSpaceship.Instance == null)
        {
            return null;
        }

        Transform shipTransform = MainSpaceship.Instance.transform;
        Rigidbody shipRigidbody = MainSpaceship.Instance.GetComponent<Rigidbody>();
        if (candidateRigidbody != null &&
            (candidateRigidbody.transform == shipTransform || candidateRigidbody.transform.IsChildOf(shipTransform)))
        {
            return shipRigidbody;
        }

        if (candidateTransform != null &&
            (candidateTransform == shipTransform || candidateTransform.IsChildOf(shipTransform)))
        {
            return shipRigidbody;
        }

        return null;
    }

    private void EnterSpaceship(Rigidbody shipRigidbody)
    {
        if (shipRigidbody == null)
        {
            return;
        }

        collisionCount++;
        activeShipRigidbody = shipRigidbody;
        transform.parent = null;
    }

    private void ExitSpaceship()
    {
        collisionCount = Mathf.Max(0, collisionCount - 1);
        if (collisionCount <= 0)
        {
            activeShipRigidbody = null;
            transform.parent = null;
            hasPreviousShipRotation = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        EnterSpaceship(GetSpaceshipRigidbody(collision.rigidbody, collision.transform));
    }

    private void OnCollisionExit(Collision collision)
    {
        if (GetSpaceshipRigidbody(collision.rigidbody, collision.transform) != null)
        {
            ExitSpaceship();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        EnterSpaceship(GetSpaceshipRigidbody(other.attachedRigidbody, other.transform));
    }

    private void OnTriggerExit(Collider collision)
    {
        if (GetSpaceshipRigidbody(collision.attachedRigidbody, collision.transform) != null)
        {
            ExitSpaceship();
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
        Look();
        RefreshSeenObject();
    }

    private void RefreshSeenObject()
    {
        Selectable previousSeenObject = seenObject;
        seenObject = FindSeenSelectable();

        if (previousSeenObject != seenObject)
        {
            UpdateSeenObject(seenObject, previousSeenObject);
        }
    }

    private Selectable FindSeenSelectable()
    {
        Ray ray = new Ray(cam.transform.position, GetFacing());
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            return null;
        }

        Debug.DrawLine(ray.origin, hit.point, Color.red);
        Selectable selectable = hit.collider.GetComponentInParent<Selectable>();
        GameObject layerObject = selectable != null ? selectable.gameObject : hit.transform.gameObject;
        bool isSelectableLayer = (GameCore.Instance.Masks.SelectableItems.value & (1 << layerObject.layer)) != 0;
        if (!isSelectableLayer)
        {
            return null;
        }

        return selectable;
    }
}
