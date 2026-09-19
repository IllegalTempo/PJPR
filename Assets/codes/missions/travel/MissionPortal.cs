using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(NetworkPrefabIdentity))]
public sealed class MissionPortal : MonoBehaviour
{
    private bool triggerAccepted;

    public string NetworkId
    {
        get
        {
            NetworkPrefabIdentity identity = GetComponent<NetworkPrefabIdentity>();
            return identity != null ? identity.Identifier : string.Empty;
        }
    }

    private void Start()
    {
        ShowWaypointWhenReadyAsync().Forget();
    }

    private async UniTaskVoid ShowWaypointWhenReadyAsync()
    {
        await UniTask.Yield();
        MissionTravelPhase phase = MissionManager.Instance != null
            ? MissionManager.Instance.TravelSnapshot.Phase
            : MissionTravelPhase.OutboundPortal;
        ShowWaypoint(phase == MissionTravelPhase.MissionActive ? "Return to Main" : "Mission Portal");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerAccepted || NetworkSystem.Instance == null || !NetworkSystem.Instance.IsWorldManager)
            return;
        if (!IsSpaceshipCollider(other, MainSpaceship.Instance))
            return;

        if (MissionManager.Instance != null && MissionManager.Instance.TryUsePortal(NetworkId, other))
            triggerAccepted = true;
    }

    public void SetUsable(bool usable)
    {
        triggerAccepted = !usable;
        Collider portalCollider = GetComponent<Collider>();
        if (portalCollider != null)
            portalCollider.enabled = usable;
    }

    public void ShowWaypoint(string label)
    {
        UIManager.Instance?.SetWaypoint(transform, label);
    }

    public void HideWaypoint()
    {
        UIManager.Instance?.HideWaypoint();
    }

    public static bool IsSpaceshipCollider(Collider collider, MainSpaceship ship = null)
    {
        ship ??= MainSpaceship.Instance;
        return collider != null && ship != null &&
               (collider.transform == ship.transform || collider.transform.IsChildOf(ship.transform));
    }
}
