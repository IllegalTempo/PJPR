using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MovementReferenceTracker
{
    private readonly Rigidbody ownerRigidbody;
    private readonly Func<Rigidbody, bool> canUseReference;
    private readonly Dictionary<Rigidbody, int> contacts = new Dictionary<Rigidbody, int>();

    private Rigidbody activeRigidbody;
    private Quaternion previousReferenceRotation;
    private bool hasPreviousReferenceRotation;
    private Vector3 localAnchorPosition;
    private bool hasLocalAnchor;
    private float defaultLinearDamping;
    private bool hasDefaultLinearDamping;
    private int activeVersion;

    public MovementReferenceTracker(Rigidbody ownerRigidbody, Func<Rigidbody, bool> canUseReference = null)
    {
        this.ownerRigidbody = ownerRigidbody;
        this.canUseReference = canUseReference;
    }

    public bool HasReference => contacts.Count > 0;

    public Rigidbody ActiveRigidbody => GetActiveRigidbody();

    public int ActiveVersion => activeVersion;

    public void CacheDefaultLinearDamping()
    {
        if (ownerRigidbody == null)
        {
            return;
        }

        defaultLinearDamping = ownerRigidbody.linearDamping;
        hasDefaultLinearDamping = true;
    }

    public void ApplyReferenceDamping()
    {
        if (ownerRigidbody == null)
        {
            return;
        }

        if (!hasDefaultLinearDamping)
        {
            CacheDefaultLinearDamping();
        }

        ownerRigidbody.linearDamping = HasReference ? 0f : defaultLinearDamping;
    }

    public void RestoreDefaultDamping()
    {
        if (ownerRigidbody == null || !hasDefaultLinearDamping)
        {
            return;
        }

        ownerRigidbody.linearDamping = defaultLinearDamping;
    }

    public bool Enter(Collision collision)
    {
        if (collision == null)
        {
            return false;
        }

        return Enter(GetReferenceRigidbody(collision.rigidbody, collision.transform));
    }

    public bool Exit(Collision collision)
    {
        if (collision == null)
        {
            return false;
        }

        return Exit(GetReferenceRigidbody(collision.rigidbody, collision.transform));
    }

    public bool Enter(Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        return Enter(GetReferenceRigidbody(collider.attachedRigidbody, collider.transform));
    }

    public bool Exit(Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        return Exit(GetReferenceRigidbody(collider.attachedRigidbody, collider.transform));
    }

    public Vector3 GetVelocity(Vector3 position)
    {
        Rigidbody referenceRigidbody = ActiveRigidbody;
        return referenceRigidbody != null ? referenceRigidbody.GetPointVelocity(position) : Vector3.zero;
    }

    public void ApplyYawDelta(Transform ownerTransform, ref float yaw)
    {
        Rigidbody referenceRigidbody = ActiveRigidbody;
        if (referenceRigidbody == null || ownerTransform == null)
        {
            hasPreviousReferenceRotation = false;
            return;
        }

        Quaternion referenceRotation = referenceRigidbody.rotation;
        if (!hasPreviousReferenceRotation)
        {
            previousReferenceRotation = referenceRotation;
            hasPreviousReferenceRotation = true;
            return;
        }

        Quaternion deltaRotation = referenceRotation * Quaternion.Inverse(previousReferenceRotation);
        Vector3 previousForward = Vector3.ProjectOnPlane(ownerTransform.forward, Vector3.up);
        Vector3 rotatedForward = Vector3.ProjectOnPlane(deltaRotation * ownerTransform.forward, Vector3.up);
        if (previousForward.sqrMagnitude <= Mathf.Epsilon || rotatedForward.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        yaw += Vector3.SignedAngle(previousForward, rotatedForward, Vector3.up);
    }

    public void UpdateRotationTracking()
    {
        Rigidbody referenceRigidbody = ActiveRigidbody;
        if (referenceRigidbody == null)
        {
            hasPreviousReferenceRotation = false;
            return;
        }

        previousReferenceRotation = referenceRigidbody.rotation;
    }

    public void ApplyLocalAnchor(bool lockToReference)
    {
        Rigidbody referenceRigidbody = ActiveRigidbody;
        if (!lockToReference || referenceRigidbody == null || ownerRigidbody == null)
        {
            hasLocalAnchor = false;
            return;
        }

        if (!hasLocalAnchor)
        {
            localAnchorPosition = referenceRigidbody.transform.InverseTransformPoint(ownerRigidbody.position);
            hasLocalAnchor = true;
        }

        ownerRigidbody.MovePosition(referenceRigidbody.transform.TransformPoint(localAnchorPosition));
    }

    private bool Enter(Rigidbody referenceRigidbody)
    {
        if (referenceRigidbody == null)
        {
            return false;
        }

        if (contacts.TryGetValue(referenceRigidbody, out int contactCount))
        {
            contacts[referenceRigidbody] = contactCount + 1;
        }
        else
        {
            contacts.Add(referenceRigidbody, 1);
        }

        if (activeRigidbody == null)
        {
            SetActiveRigidbody(referenceRigidbody);
        }

        return true;
    }

    private bool Exit(Rigidbody referenceRigidbody)
    {
        if (referenceRigidbody == null || !contacts.TryGetValue(referenceRigidbody, out int contactCount))
        {
            return false;
        }

        if (contactCount <= 1)
        {
            contacts.Remove(referenceRigidbody);
        }
        else
        {
            contacts[referenceRigidbody] = contactCount - 1;
        }

        if (activeRigidbody == referenceRigidbody && !contacts.ContainsKey(referenceRigidbody))
        {
            SetActiveRigidbody(ChooseRigidbody());
        }

        if (!HasReference)
        {
            hasPreviousReferenceRotation = false;
            hasLocalAnchor = false;
        }

        return true;
    }

    private Rigidbody GetActiveRigidbody()
    {
        if (!HasReference)
        {
            return null;
        }

        if (activeRigidbody == null || !contacts.ContainsKey(activeRigidbody))
        {
            SetActiveRigidbody(ChooseRigidbody());
        }

        return activeRigidbody;
    }

    private Rigidbody ChooseRigidbody()
    {
        foreach (Rigidbody referenceRigidbody in contacts.Keys)
        {
            if (referenceRigidbody != null)
            {
                return referenceRigidbody;
            }
        }

        return null;
    }

    private void SetActiveRigidbody(Rigidbody referenceRigidbody)
    {
        if (activeRigidbody == referenceRigidbody)
        {
            return;
        }

        activeRigidbody = referenceRigidbody;
        activeVersion++;
        hasPreviousReferenceRotation = false;
        hasLocalAnchor = false;
    }

    private Rigidbody GetReferenceRigidbody(Rigidbody candidateRigidbody, Transform candidateTransform)
    {
        if (MainSpaceship.Instance == null)
        {
            return null;
        }

        Transform referenceRoot = MainSpaceship.Instance.transform;
        if (IsReferenceRigidbody(candidateRigidbody, referenceRoot))
        {
            return candidateRigidbody;
        }

        if (candidateTransform != null &&
            (candidateTransform == referenceRoot || candidateTransform.IsChildOf(referenceRoot)))
        {
            Rigidbody transformRigidbody = candidateTransform.GetComponentInParent<Rigidbody>();
            return IsReferenceRigidbody(transformRigidbody, referenceRoot)
                ? transformRigidbody
                : MainSpaceship.Instance.GetComponent<Rigidbody>();
        }

        return null;
    }

    private bool IsReferenceRigidbody(Rigidbody candidateRigidbody, Transform referenceRoot)
    {
        if (candidateRigidbody == null || candidateRigidbody == ownerRigidbody)
        {
            return false;
        }

        if (canUseReference != null && !canUseReference(candidateRigidbody))
        {
            return false;
        }

        return candidateRigidbody.transform == referenceRoot ||
            candidateRigidbody.transform.IsChildOf(referenceRoot);
    }
}
