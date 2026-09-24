using UnityEngine;

public sealed class MissionSpawnPoint : MonoBehaviour
{
    private const float ReturnPortalDistance = 3000f;

    public Vector3 ShipPosition => transform.position;
    public Quaternion ShipRotation => transform.rotation;

    public Vector3 GetReturnPortalPosition(int sessionId)
    {
        UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(sessionId);
        Vector3 direction = UnityEngine.Random.onUnitSphere;
        UnityEngine.Random.state = previousRandomState;
        return ShipPosition + direction * ReturnPortalDistance;
    }
}
