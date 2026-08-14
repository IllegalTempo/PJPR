using System.Collections.Generic;
using UnityEngine;

public partial class PlayerMain : MonoBehaviour
{
    public float MoveSpeed = 1f;
    public float LookSpeed = 2f;
    public float MaxSpeed = 3f; // Maximum allowed speed
    public float JetPackForce = 2f;
    public float Gravity = 2f;
    private Vector2 moveinput = Vector2.zero;
    public Vector2 lookinput = Vector2.zero;

    public float maxVerticalVelocity = 100f;
    private readonly Dictionary<Collider, Rigidbody> triggerFollowColliders = new Dictionary<Collider, Rigidbody>();
    private Rigidbody triggerFollowRigidbody;
    private Rigidbody touchedFollowRigidbody;
    private Rigidbody activeFollowRigidbody;
    private Vector3 touchedFollowLocalPoint;
    private Vector3 previousFollowVelocity;
    private int touchedFollowFixedTick = -1;
    private int movementFixedTick;

    private void Jetpack()
    {
        rb.AddForce(Vector3.up * JetPackForce, ForceMode.Acceleration);

    }
    private void Move()
    {
        movementFixedTick++;

        Vector3 move = (GetFacing() * moveinput.y + cam.transform.right * moveinput.x);
        move.y = 0f;
        move.Normalize();

        Vector3 currentVelocity = rb.GetPointVelocity(rb.worldCenterOfMass);
        Vector3 followVelocity = GetFollowVelocity();
        followVelocity = StopFollowVelocityWhenBlocked(followVelocity);
        Vector3 currentRelativeVelocity = currentVelocity - previousFollowVelocity;
        Vector3 relativeVelocity = move * MoveSpeed * MaxSpeed;
        relativeVelocity.y = Mathf.Clamp(currentRelativeVelocity.y, -maxVerticalVelocity, maxVerticalVelocity);
        rb.AddForce(Vector3.down * Gravity, ForceMode.Acceleration);
        Vector3 targetVelocity = relativeVelocity + followVelocity;
        rb.AddForce(targetVelocity - currentVelocity, ForceMode.VelocityChange);
        previousFollowVelocity = followVelocity;
        if (control.Player.jump.IsPressed())
        {
            Jetpack();
        }

    }

    private void AddTriggerFollowRigidbody(Collider other)
    {
        Rigidbody followRigidbody = other != null ? other.attachedRigidbody : null;
        if (followRigidbody == null || followRigidbody == rb)
        {
            return;
        }

        triggerFollowColliders[other] = followRigidbody;
        triggerFollowRigidbody = followRigidbody;
    }

    private void RemoveTriggerFollowRigidbody(Collider other)
    {
        if (other == null || !triggerFollowColliders.Remove(other))
        {
            return;
        }

        triggerFollowRigidbody = null;
        foreach (Rigidbody followRigidbody in triggerFollowColliders.Values)
        {
            triggerFollowRigidbody = followRigidbody;
        }
    }

    private Vector3 GetFollowVelocity()
    {
        activeFollowRigidbody = null;

        if (triggerFollowRigidbody != null)
        {
            activeFollowRigidbody = triggerFollowRigidbody;
            return triggerFollowRigidbody.GetPointVelocity(rb.position);
        }

        Vector3 touchedFollowVelocity = GetTouchedFollowVelocity();
        if (touchedFollowRigidbody != null)
        {
            activeFollowRigidbody = touchedFollowRigidbody;
            return touchedFollowVelocity;
        }

        previousFollowVelocity = Vector3.zero;
        return Vector3.zero;
    }

    private Vector3 StopFollowVelocityWhenBlocked(Vector3 followVelocity)
    {
        float followSpeed = followVelocity.magnitude;
        if (followSpeed <= Mathf.Epsilon)
        {
            return Vector3.zero;
        }

        float followDistance = followSpeed * Time.fixedDeltaTime;
        RaycastHit[] hits = rb.SweepTestAll(followVelocity / followSpeed, followDistance, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (IsFollowMovementBlocker(hit.collider))
            {
                return Vector3.zero;
            }
        }

        return followVelocity;
    }

    private bool IsFollowMovementBlocker(Collider collider)
    {
        if (collider == null || collider.isTrigger)
        {
            return false;
        }

        if (collider.attachedRigidbody == rb || collider.attachedRigidbody == activeFollowRigidbody)
        {
            return false;
        }

        if (collider.transform.IsChildOf(transform))
        {
            return false;
        }

        return true;
    }
    private void UpdateTouchedFollowRigidbody(Collision collision)
    {
        if (collision == null || collision.rigidbody == null || collision.rigidbody == rb)
        {
            return;
        }

        touchedFollowRigidbody = collision.rigidbody;
        Vector3 contactPoint = collision.contactCount > 0 ? collision.GetContact(0).point : rb.position;
        touchedFollowLocalPoint = touchedFollowRigidbody.transform.InverseTransformPoint(contactPoint);
        touchedFollowFixedTick = movementFixedTick;
    }

    private Vector3 GetTouchedFollowVelocity()
    {
        if (touchedFollowRigidbody == null || movementFixedTick - touchedFollowFixedTick > 1)
        {
            ClearTouchedFollowRigidbody();
            return Vector3.zero;
        }

        Vector3 followPoint = touchedFollowRigidbody.transform.TransformPoint(touchedFollowLocalPoint);
        return touchedFollowRigidbody.GetPointVelocity(followPoint);
    }

    private void ClearTouchedFollowRigidbody()
    {
        touchedFollowRigidbody = null;
        touchedFollowLocalPoint = Vector3.zero;
        touchedFollowFixedTick = -1;
    }

    private void OnCollisionEnter(Collision collision)
    {
        UpdateTouchedFollowRigidbody(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        UpdateTouchedFollowRigidbody(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision != null && collision.rigidbody == touchedFollowRigidbody)
        {
            ClearTouchedFollowRigidbody();
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
