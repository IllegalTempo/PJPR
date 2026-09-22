using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerMain : MonoBehaviour
{
    public float MoveSpeed = 1f;
    public float LookSpeed = 2f;
    public float MaxSpeed = 5f; // Maximum allowed speed
    public float JetPackForce = 2f;
    [Min(0.01f)] public float MoveAcceleration = 40f;
    [Min(0.01f)] public float MoveDeceleration = 60f;
    private Vector2 moveinput = Vector2.zero;
    public Vector2 lookinput = Vector2.zero;

    public float maxVerticalVelocity = 100f;

    private MovementReferenceTracker movementReferenceTracker;

    private void Move()
    {
        ApplyMovementReferenceYawDelta();
        Vector2 input = control.Player.enabled ? Vector2.ClampMagnitude(moveinput, 1f) : Vector2.zero;
        Vector3 move = GetLookRelativeMove(input);
        bool hasMoveInput = input.sqrMagnitude > 0f;
        bool jumpPressed = control.Player.jump.IsPressed();
        bool moveDownPressed = control.Player.enabled && IsMoveDownPressed();
        Vector3 movementReferenceVelocity = GetMovementReferenceVelocity();
        Vector3 currentRelativeVelocity = rb.linearVelocity - movementReferenceVelocity;
        float acceleration = hasMoveInput ? MoveAcceleration : MoveDeceleration;
        Vector3 movementVelocity = Vector3.MoveTowards(currentRelativeVelocity, move * Mathf.Max(0f, MoveSpeed),
            Mathf.Max(0.01f, acceleration) * Time.fixedDeltaTime);

        bool lockToMovementReference = ShouldLockToMovementReference(hasMoveInput, jumpPressed, moveDownPressed)
            && movementVelocity.sqrMagnitude <= 0.0001f;
        ApplyMovementReferenceLocalAnchor(lockToMovementReference);

        float verticalInput = (jumpPressed ? 1f : 0f) - (moveDownPressed ? 1f : 0f);
        float verticalLimit = Mathf.Max(0f, maxVerticalVelocity);
        if (lockToMovementReference)
        {
            movementVelocity = Vector3.zero;
        }
        else
        {
            movementVelocity.y = Mathf.Clamp(
                movementVelocity.y + verticalInput * Mathf.Max(0f, JetPackForce) * Time.fixedDeltaTime,
                -verticalLimit, verticalLimit);
        }

        Vector3 targetVelocity = movementReferenceVelocity + movementVelocity;
        rb.AddForce(targetVelocity - rb.linearVelocity, ForceMode.VelocityChange);

        animator.SetBool("jetpack", verticalInput > 0f);
        float targetAnimatorSpeed = MoveSpeed > 0f ? Mathf.Clamp01(movementVelocity.magnitude / MoveSpeed) : 0f;
        animator.SetFloat("speed", Mathf.Lerp(animator.GetFloat("speed"), targetAnimatorSpeed,
            1f - Mathf.Exp(-10f * Time.fixedDeltaTime)));

        UpdateMovementReferenceRotationTracking();
    }

    private Vector3 GetLookRelativeMove(Vector2 input)
    {
        Vector3 forward = head != null ? head.transform.forward : transform.forward;
        if (forward.sqrMagnitude <= Mathf.Epsilon)
        {
            forward = Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
        }

        Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        Vector3 move = right * input.x + forward.normalized * input.y;
        return move.sqrMagnitude > 1f ? move.normalized : move;
    }

    private bool IsMoveDownPressed()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null &&
            ((keyboard.leftCtrlKey != null && keyboard.leftCtrlKey.isPressed) ||
             (keyboard.rightCtrlKey != null && keyboard.rightCtrlKey.isPressed));
    }

    private bool ShouldLockToMovementReference(bool hasMoveInput, bool jumpPressed, bool moveDownPressed)
    {
        return HasMovementReference && !hasMoveInput && !jumpPressed && !moveDownPressed;
    }

    private void ApplyMovementReferenceLocalAnchor(bool lockToMovementReference)
    {
        movementReferenceTracker?.ApplyLocalAnchor(lockToMovementReference);
    }

    private void ApplyMovementReferenceYawDelta()
    {
        movementReferenceTracker?.ApplyYawDelta(transform, ref yaw);
    }

    private void UpdateMovementReferenceRotationTracking()
    {
        movementReferenceTracker?.UpdateRotationTracking();
    }

    private Vector3 GetMovementReferenceVelocity()
    {
        return movementReferenceTracker != null ? movementReferenceTracker.GetVelocity(rb.position) : Vector3.zero;
    }

    private void InitializeMovementReferencePhysics()
    {
        if (rb == null)
        {
            return;
        }

        movementReferenceTracker = new MovementReferenceTracker(
            rb,
            candidateRigidbody => candidateRigidbody.GetComponentInParent<Item>() == null);
        movementReferenceTracker.CacheDefaultLinearDamping();
    }

    private void ApplyMovementReferenceDamping()
    {
        movementReferenceTracker?.ApplyReferenceDamping();
    }

    private void RestoreMovementReferencePhysics()
    {
        movementReferenceTracker?.RestoreDefaultDamping();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (movementReferenceTracker != null && movementReferenceTracker.Enter(collision))
        {
            ApplyMovementReferenceDamping();
            transform.parent = null;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (movementReferenceTracker != null && movementReferenceTracker.Exit(collision))
        {
            ApplyMovementReferenceDamping();
            if (!HasMovementReference)
            {
                transform.parent = null;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (movementReferenceTracker != null && movementReferenceTracker.Enter(other))
        {
            ApplyMovementReferenceDamping();
            transform.parent = null;
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (movementReferenceTracker != null && movementReferenceTracker.Exit(collision))
        {
            ApplyMovementReferenceDamping();
            if (!HasMovementReference)
            {
                transform.parent = null;
            }
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
