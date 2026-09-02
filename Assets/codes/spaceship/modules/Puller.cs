using UnityEngine;

public class Puller : Module<bool>
{
    public GameObject PullerSegment;
    public GameObject PullerHand;

    [SerializeField] private float pullerRayDistance = 100f;
    [SerializeField] private float pullerRayRadius = 0.5f;
    [SerializeField] private LayerMask pullerRayMask = Physics.DefaultRaycastLayers;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    private const string StartAnchorName = "start";
    private const string EndAnchorName = "end";

    private GameObject activeHand;
    private GameObject activeSegment;

    protected override void OnDataChanged(bool newData)
    {
        base.OnDataChanged(newData);

        if (newData)
        {
            ActivatePuller();
            return;
        }

        ClearPuller();
    }

    private void ActivatePuller()
    {
        ClearPuller();

        if (PullerHand == null || PullerSegment == null)
        {
            Debug.LogWarning($"{name} cannot activate puller because PullerHand or PullerSegment is not assigned.");
            return;
        }

        Vector3 pullDirection = transform.up;
        if (!Physics.SphereCast(transform.position, pullerRayRadius, pullDirection, out RaycastHit hit, pullerRayDistance, pullerRayMask, triggerInteraction))
        {
            Debug.LogWarning($"{name} puller did not hit anything along transform.up.");
            return;
        }

        Transform pullerStart = FindChildRecursive(transform, StartAnchorName);
        if (pullerStart == null)
        {
            Debug.LogWarning($"{name} cannot activate puller because it has no child named '{StartAnchorName}'.");
            return;
        }

        activeHand = Instantiate(PullerHand, hit.point, transform.rotation);

        Transform handEnd = FindChildRecursive(activeHand.transform, EndAnchorName);
        if (handEnd == null)
        {
            Debug.LogWarning($"{activeHand.name} cannot connect puller because it has no child named '{EndAnchorName}'.");
            ClearPuller();
            return;
        }

        activeHand.transform.position += hit.point - handEnd.position;
        activeSegment = Instantiate(PullerSegment, pullerStart.position, transform.rotation);

        Transform segmentStart = FindChildRecursive(activeSegment.transform, StartAnchorName);
        Transform segmentEnd = FindChildRecursive(activeSegment.transform, EndAnchorName);
        if (segmentStart == null || segmentEnd == null)
        {
            Debug.LogWarning($"{activeSegment.name} cannot connect puller because it needs child anchors named '{StartAnchorName}' and '{EndAnchorName}'.");
            ClearPuller();
            return;
        }

        AlignSegment(activeSegment.transform, segmentStart, segmentEnd, pullerStart.position, handEnd.position);
        ConnectSegmentHinge(activeSegment);
        ConnectHandHinge(activeHand, activeSegment);
        ConnectHandToSurface(activeHand, handEnd.position, hit.rigidbody);
    }

    private void ClearPuller()
    {
        if (activeSegment != null)
        {
            Destroy(activeSegment);
            activeSegment = null;
        }

        if (activeHand != null)
        {
            Destroy(activeHand);
            activeHand = null;
        }
    }

    private void AlignSegment(Transform segmentRoot, Transform segmentStart, Transform segmentEnd, Vector3 targetStart, Vector3 targetEnd)
    {
        Vector3 targetDelta = targetEnd - targetStart;
        Vector3 segmentDelta = segmentEnd.position - segmentStart.position;

        if (targetDelta.sqrMagnitude <= Mathf.Epsilon || segmentDelta.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        segmentRoot.rotation = Quaternion.FromToRotation(segmentDelta, targetDelta) * segmentRoot.rotation;

        float currentLength = Vector3.Distance(segmentStart.position, segmentEnd.position);
        if (currentLength > Mathf.Epsilon)
        {
            Vector3 scale = segmentRoot.localScale;
            int lengthAxis = GetSegmentLengthAxis(segmentRoot, segmentStart, segmentEnd);
            float scaleFactor = targetDelta.magnitude / currentLength;

            if (lengthAxis == 0)
            {
                scale.x *= scaleFactor;
            }
            else if (lengthAxis == 1)
            {
                scale.y *= scaleFactor;
            }
            else
            {
                scale.z *= scaleFactor;
            }

            segmentRoot.localScale = scale;
        }

        segmentRoot.position += targetStart - segmentStart.position;
    }

    private int GetSegmentLengthAxis(Transform segmentRoot, Transform segmentStart, Transform segmentEnd)
    {
        Vector3 localDelta = segmentRoot.InverseTransformPoint(segmentEnd.position) - segmentRoot.InverseTransformPoint(segmentStart.position);
        Vector3 absoluteDelta = new Vector3(Mathf.Abs(localDelta.x), Mathf.Abs(localDelta.y), Mathf.Abs(localDelta.z));

        if (absoluteDelta.x >= absoluteDelta.y && absoluteDelta.x >= absoluteDelta.z)
        {
            return 0;
        }

        if (absoluteDelta.y >= absoluteDelta.z)
        {
            return 1;
        }

        return 2;
    }

    private void ConnectSegmentHinge(GameObject segment)
    {
        HingeJoint hinge = segment.GetComponentInChildren<HingeJoint>();
        if (hinge == null)
        {
            Debug.LogWarning($"{segment.name} cannot connect puller because it has no HingeJoint.");
            return;
        }

        Rigidbody pullerRigidbody = GetComponent<Rigidbody>();
        if (pullerRigidbody == null)
        {
            Debug.LogWarning($"{name} cannot connect puller hinge because it has no Rigidbody.");
            return;
        }

        hinge.connectedBody = pullerRigidbody;
    }

    private void ConnectHandHinge(GameObject hand, GameObject segment)
    {
        HingeJoint hinge = hand.GetComponentInChildren<HingeJoint>();
        if (hinge == null)
        {
            Debug.LogWarning($"{hand.name} cannot connect puller because it has no HingeJoint.");
            return;
        }

        Rigidbody segmentRigidbody = segment.GetComponent<Rigidbody>();
        if (segmentRigidbody == null)
        {
            Debug.LogWarning($"{segment.name} cannot connect puller hand hinge because it has no Rigidbody.");
            return;
        }

        hinge.connectedBody = segmentRigidbody;
    }

    private void ConnectHandToSurface(GameObject hand, Vector3 anchorWorldPosition, Rigidbody surfaceRigidbody)
    {
        Rigidbody handRigidbody = hand.GetComponent<Rigidbody>();
        if (handRigidbody == null)
        {
            Debug.LogWarning($"{hand.name} cannot stick to surface because it has no Rigidbody.");
            return;
        }

        if (surfaceRigidbody == handRigidbody)
        {
            Debug.LogWarning($"{hand.name} cannot stick to itself.");
            return;
        }

        FixedJoint surfaceJoint = hand.AddComponent<FixedJoint>();
        surfaceJoint.connectedBody = surfaceRigidbody;
        surfaceJoint.autoConfigureConnectedAnchor = false;
        surfaceJoint.anchor = hand.transform.InverseTransformPoint(anchorWorldPosition);
        surfaceJoint.connectedAnchor = surfaceRigidbody != null
            ? surfaceRigidbody.transform.InverseTransformPoint(anchorWorldPosition)
            : anchorWorldPosition;
        surfaceJoint.enableCollision = false;
        surfaceJoint.breakForce = Mathf.Infinity;
        surfaceJoint.breakTorque = Mathf.Infinity;
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }

            Transform nestedChild = FindChildRecursive(child, childName);
            if (nestedChild != null)
            {
                return nestedChild;
            }
        }

        return null;
    }
}
