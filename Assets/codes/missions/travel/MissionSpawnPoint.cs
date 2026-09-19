using UnityEngine;

public sealed class MissionSpawnPoint : MonoBehaviour
{
    [SerializeField] private Vector3 returnPortalLocalOffset = new Vector3(15f, 0f, 0f);

    public Vector3 ShipPosition => transform.position;
    public Quaternion ShipRotation => transform.rotation;
    public Vector3 ReturnPortalPosition => transform.TransformPoint(returnPortalLocalOffset);
}
