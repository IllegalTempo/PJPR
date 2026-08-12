using UnityEngine;

public class SpaceshipFollowTracker
{
    private int inSpaceshipTriggerCount;
    private bool hasPreviousSpaceshipPoint;
    private Vector3 previousSpaceshipLocalPosition;
    private Vector3 previousSpaceshipPoint;
    private Vector3 previousSpaceshipVelocity;

    public bool InSpaceship { get; private set; }

    public void OnTriggerEnter(Collider other)
    {
        if (!IsInSpaceshipDetector(other))
        {
            return;
        }

        inSpaceshipTriggerCount++;
        InSpaceship = true;
    }

    public void OnTriggerExit(Collider other)
    {
        if (!IsInSpaceshipDetector(other))
        {
            return;
        }

        inSpaceshipTriggerCount = Mathf.Max(0, inSpaceshipTriggerCount - 1);
        InSpaceship = inSpaceshipTriggerCount > 0;
        if (!InSpaceship)
        {
            ResetVelocityTracking();
        }
    }

    public Vector3 GetSpaceshipVelocity(Vector3 worldPosition)
    {
        if (!InSpaceship || MainSpaceship.Instance == null)
        {
            ResetVelocityTracking();
            return Vector3.zero;
        }

        Transform spaceshipTransform = MainSpaceship.Instance.transform;
        if (!hasPreviousSpaceshipPoint)
        {
            previousSpaceshipLocalPosition = spaceshipTransform.InverseTransformPoint(worldPosition);
            previousSpaceshipPoint = spaceshipTransform.TransformPoint(previousSpaceshipLocalPosition);
            hasPreviousSpaceshipPoint = true;
            return Vector3.zero;
        }

        Vector3 currentSpaceshipPoint = spaceshipTransform.TransformPoint(previousSpaceshipLocalPosition);
        Vector3 spaceshipVelocity = Time.fixedDeltaTime > Mathf.Epsilon
            ? (currentSpaceshipPoint - previousSpaceshipPoint) / Time.fixedDeltaTime
            : Vector3.zero;

        previousSpaceshipLocalPosition = spaceshipTransform.InverseTransformPoint(worldPosition);
        previousSpaceshipPoint = spaceshipTransform.TransformPoint(previousSpaceshipLocalPosition);
        return spaceshipVelocity;
    }

    public Vector3 GetRelativeVelocity(Rigidbody rb)
    {
        if (rb == null)
        {
            return Vector3.zero;
        }

        return rb.linearVelocity - previousSpaceshipVelocity;
    }

    public void StoreAppliedSpaceshipVelocity(Vector3 spaceshipVelocity)
    {
        previousSpaceshipVelocity = spaceshipVelocity;
    }

    public void ApplyTo(Rigidbody rb)
    {
        if (rb == null || rb.isKinematic)
        {
            ResetVelocityTracking();
            return;
        }

        Vector3 spaceshipVelocity = GetSpaceshipVelocity(rb.position);
        Vector3 relativeVelocity = GetRelativeVelocity(rb);
        rb.linearVelocity = relativeVelocity + spaceshipVelocity;
        StoreAppliedSpaceshipVelocity(spaceshipVelocity);
    }

    public void ResetVelocityTracking()
    {
        previousSpaceshipVelocity = Vector3.zero;
        hasPreviousSpaceshipPoint = false;
        previousSpaceshipLocalPosition = Vector3.zero;
        previousSpaceshipPoint = Vector3.zero;
    }

    private bool IsInSpaceshipDetector(Collider other)
    {
        if (GameCore.Instance == null || GameCore.Instance.Masks == null)
        {
            return false;
        }

        return (GameCore.Instance.Masks.InSpaceshipDetect.value & (1 << other.gameObject.layer)) != 0;
    }
}
