using System.Collections.Generic;
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

    private readonly Dictionary<Rigidbody, int> movementReferenceContacts = new Dictionary<Rigidbody, int>();
    private Rigidbody activeMovementReferenceRigidbody;
    private Quaternion previousMovementReferenceRotation;
    private bool hasPreviousMovementReferenceRotation;
    private Vector3 movementReferenceLocalAnchorPosition;
    private bool hasMovementReferenceLocalAnchor;
    private float defaultLinearDamping;
    private bool hasDefaultLinearDamping;

    private void Move()
    {
        ApplyMovementReferenceYawDelta();
        Vector2 input = control.Player.enabled ? Vector2.ClampMagnitude(moveinput, 1f) : Vector2.zero;
        // Build the horizontal basis from yaw so looking vertically never changes walking direction.
        Vector3 move = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y);
        bool hasMoveInput = input.sqrMagnitude > 0f;
        bool jumpPressed = control.Player.jump.IsPressed();
        bool moveDownPressed = control.Player.enabled && IsMoveDownPressed();
        Vector3 movementReferenceVelocity = GetMovementReferenceVelocity();
        Vector3 currentRelativeVelocity = rb.linearVelocity - movementReferenceVelocity;
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentRelativeVelocity, Vector3.up);
        float acceleration = hasMoveInput ? MoveAcceleration : MoveDeceleration;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, move * Mathf.Max(0f, MoveSpeed),
            Mathf.Max(0.01f, acceleration) * Time.fixedDeltaTime);

        bool lockToMovementReference = ShouldLockToMovementReference(hasMoveInput, jumpPressed, moveDownPressed)
            && horizontalVelocity.sqrMagnitude <= 0.0001f;
        ApplyMovementReferenceLocalAnchor(lockToMovementReference);

        float verticalInput = (jumpPressed ? 1f : 0f) - (moveDownPressed ? 1f : 0f);
        float verticalLimit = Mathf.Max(0f, maxVerticalVelocity);
        float verticalVelocity = lockToMovementReference ? 0f : Mathf.Clamp(
            currentRelativeVelocity.y + verticalInput * Mathf.Max(0f, JetPackForce) * Time.fixedDeltaTime,
            -verticalLimit, verticalLimit);
        Vector3 targetVelocity = movementReferenceVelocity + horizontalVelocity + Vector3.up * verticalVelocity;
        rb.AddForce(targetVelocity - rb.linearVelocity, ForceMode.VelocityChange);

        animator.SetBool("jetpack", verticalInput > 0f);
        float targetAnimatorSpeed = MoveSpeed > 0f ? Mathf.Clamp01(horizontalVelocity.magnitude / MoveSpeed) : 0f;
        animator.SetFloat("speed", Mathf.Lerp(animator.GetFloat("speed"), targetAnimatorSpeed,
            1f - Mathf.Exp(-10f * Time.fixedDeltaTime)));

        UpdateMovementReferenceRotationTracking();
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
        Rigidbody movementReferenceRigidbody = GetActiveMovementReferenceRigidbody();
        if (!lockToMovementReference || movementReferenceRigidbody == null)
        {
            hasMovementReferenceLocalAnchor = false;
            return;
        }

        if (!hasMovementReferenceLocalAnchor)
        {
            movementReferenceLocalAnchorPosition = movementReferenceRigidbody.transform.InverseTransformPoint(rb.position);
            hasMovementReferenceLocalAnchor = true;
        }

        rb.MovePosition(movementReferenceRigidbody.transform.TransformPoint(movementReferenceLocalAnchorPosition));
    }

    private void ApplyMovementReferenceYawDelta()
    {
        Rigidbody movementReferenceRigidbody = GetActiveMovementReferenceRigidbody();
        if (movementReferenceRigidbody == null)
        {
            hasPreviousMovementReferenceRotation = false;
            return;
        }

        Quaternion movementReferenceRotation = movementReferenceRigidbody.rotation;
        if (!hasPreviousMovementReferenceRotation)
        {
            previousMovementReferenceRotation = movementReferenceRotation;
            hasPreviousMovementReferenceRotation = true;
            return;
        }

        Quaternion deltaRotation = movementReferenceRotation * Quaternion.Inverse(previousMovementReferenceRotation);
        ApplyReferenceYawDelta(deltaRotation);
    }

    private void ApplyReferenceYawDelta(Quaternion deltaRotation)
    {
        Vector3 previousForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        Vector3 rotatedForward = Vector3.ProjectOnPlane(deltaRotation * transform.forward, Vector3.up);
        if (previousForward.sqrMagnitude <= Mathf.Epsilon || rotatedForward.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        yaw += Vector3.SignedAngle(previousForward, rotatedForward, Vector3.up);
    }

    private void UpdateMovementReferenceRotationTracking()
    {
        Rigidbody movementReferenceRigidbody = GetActiveMovementReferenceRigidbody();
        if (movementReferenceRigidbody == null)
        {
            hasPreviousMovementReferenceRotation = false;
            return;
        }

        previousMovementReferenceRotation = movementReferenceRigidbody.rotation;
    }

    private Vector3 GetMovementReferenceVelocity()
    {
        Rigidbody movementReferenceRigidbody = GetActiveMovementReferenceRigidbody();
        return movementReferenceRigidbody != null ? movementReferenceRigidbody.GetPointVelocity(rb.position) : Vector3.zero;
    }

    private Rigidbody GetActiveMovementReferenceRigidbody()
    {
        if (!HasMovementReference)
        {
            return null;
        }
        
        Rigidbody movementReferenceRigidbody = activeMovementReferenceRigidbody;
        if (movementReferenceRigidbody == null || !movementReferenceContacts.ContainsKey(movementReferenceRigidbody))
        {
            movementReferenceRigidbody = ChooseMovementReferenceRigidbody();
            SetActiveMovementReferenceRigidbody(movementReferenceRigidbody);
        }

        return movementReferenceRigidbody;
    }

    private void InitializeMovementReferencePhysics()
    {
        if (rb == null)
        {
            return;
        }

        defaultLinearDamping = rb.linearDamping;
        hasDefaultLinearDamping = true;
    }

    private void ApplyMovementReferenceDamping()
    {
        if (rb == null)
        {
            return;
        }

        if (!hasDefaultLinearDamping)
        {
            InitializeMovementReferencePhysics();
        }

        rb.linearDamping = HasMovementReference ? 0f : defaultLinearDamping;
    }

    private void RestoreMovementReferencePhysics()
    {
        if (rb == null || !hasDefaultLinearDamping)
        {
            return;
        }

        rb.linearDamping = defaultLinearDamping;
    }

    private Rigidbody ChooseMovementReferenceRigidbody()
    {
        foreach (Rigidbody movementReferenceRigidbody in movementReferenceContacts.Keys)
        {
            if (movementReferenceRigidbody != null)
            {
                return movementReferenceRigidbody;
            }
        }

        return null;
    }

    private void SetActiveMovementReferenceRigidbody(Rigidbody movementReferenceRigidbody)
    {
        if (activeMovementReferenceRigidbody == movementReferenceRigidbody)
        {
            return;
        }

        activeMovementReferenceRigidbody = movementReferenceRigidbody;
        hasPreviousMovementReferenceRotation = false;
        hasMovementReferenceLocalAnchor = false;
    }

    private Rigidbody GetMovementReferenceRigidbody(Rigidbody candidateRigidbody, Transform candidateTransform)
    {
        if (MainSpaceship.Instance == null)
        {
            return null;
        }

        Transform referenceRootTransform = MainSpaceship.Instance.transform;
        if (IsMovementReferenceRigidbody(candidateRigidbody, referenceRootTransform))
        {
            return candidateRigidbody;
        }

        if (candidateTransform != null &&
            (candidateTransform == referenceRootTransform || candidateTransform.IsChildOf(referenceRootTransform)))
        {
            Rigidbody transformRigidbody = candidateTransform.GetComponentInParent<Rigidbody>();
            return IsMovementReferenceRigidbody(transformRigidbody, referenceRootTransform)
                ? transformRigidbody
                : MainSpaceship.Instance.GetComponent<Rigidbody>();
        }

        return null;
    }

    private bool IsMovementReferenceRigidbody(Rigidbody candidateRigidbody, Transform referenceRootTransform)
    {
        if (candidateRigidbody == null || candidateRigidbody == rb)
        {
            return false;
        }

        if (candidateRigidbody.GetComponentInParent<Item>() != null)
        {
            return false;
        }

        return candidateRigidbody.transform == referenceRootTransform ||
            candidateRigidbody.transform.IsChildOf(referenceRootTransform);
    }

    private void EnterMovementReference(Rigidbody movementReferenceRigidbody)
    {
        if (movementReferenceRigidbody == null)
        {
            return;
        }

        if (movementReferenceContacts.TryGetValue(movementReferenceRigidbody, out int contactCount))
        {
            movementReferenceContacts[movementReferenceRigidbody] = contactCount + 1;
        }
        else
        {
            movementReferenceContacts.Add(movementReferenceRigidbody, 1);
        }

        if (activeMovementReferenceRigidbody == null)
        {
            SetActiveMovementReferenceRigidbody(movementReferenceRigidbody);
        }

        ApplyMovementReferenceDamping();
        transform.parent = null;
    }

    private void ExitMovementReference(Rigidbody movementReferenceRigidbody)
    {
        if (movementReferenceRigidbody == null ||
            !movementReferenceContacts.TryGetValue(movementReferenceRigidbody, out int contactCount))
        {
            return;
        }

        if (contactCount <= 1)
        {
            movementReferenceContacts.Remove(movementReferenceRigidbody);
        }
        else
        {
            movementReferenceContacts[movementReferenceRigidbody] = contactCount - 1;
        }

        if (activeMovementReferenceRigidbody == movementReferenceRigidbody &&
            !movementReferenceContacts.ContainsKey(movementReferenceRigidbody))
        {
            SetActiveMovementReferenceRigidbody(ChooseMovementReferenceRigidbody());
        }

        if (!HasMovementReference)
        {
            ApplyMovementReferenceDamping();
            transform.parent = null;
            hasPreviousMovementReferenceRotation = false;
            hasMovementReferenceLocalAnchor = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        EnterMovementReference(GetMovementReferenceRigidbody(collision.rigidbody, collision.transform));
    }

    private void OnCollisionExit(Collision collision)
    {
        Rigidbody movementReferenceRigidbody = GetMovementReferenceRigidbody(collision.rigidbody, collision.transform);
        if (movementReferenceRigidbody != null)
        {
            ExitMovementReference(movementReferenceRigidbody);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        EnterMovementReference(GetMovementReferenceRigidbody(other.attachedRigidbody, other.transform));
    }

    private void OnTriggerExit(Collider collision)
    {
        Rigidbody movementReferenceRigidbody = GetMovementReferenceRigidbody(collision.attachedRigidbody, collision.transform);
        if (movementReferenceRigidbody != null)
        {
            ExitMovementReference(movementReferenceRigidbody);
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
