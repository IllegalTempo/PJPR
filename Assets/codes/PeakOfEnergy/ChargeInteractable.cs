using UnityEngine;
using Assets.codes.Network.Messages;

/// <summary>
/// The "Core Button" at the center of the ring. Hold to channel a charge.
///
/// Press/release is routed to the server (or handled locally when offline/host), and the
/// <see cref="PeakOfEnergyManager"/> drives the actual hold timer + per-frame validation
/// (RPM in range AND stable, otherwise the channel cancels instantly).
/// </summary>
[RequireComponent(typeof(StaticOutline))]
public class ChargeInteractable : Interactable
{
    [Header("References")]
    [Tooltip("Optional explicit manager reference; auto-found from the scene if empty.")]
    [SerializeField] private PeakOfEnergyManager manager;

    private PeakOfEnergyManager Manager
    {
        get
        {
            if (manager == null)
            {
                manager = GetComponentInParent<PeakOfEnergyManager>();
                if (manager == null)
                    manager = FindFirstObjectByType<PeakOfEnergyManager>();
            }
            return manager;
        }
    }

    public override void OnInteract_press(PlayerMain who)
    {
        if (Manager == null)
        {
            Debug.LogWarning("[ChargeInteractable] No PeakOfEnergyManager found.");
            return;
        }

        if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline && !NetworkSystem.Instance.IsServer)
        {
            NetworkRouter.Instance.SendMessageToServer(new NMS_Client_PeakOfEnergyChargeRequest(true));
        }
        else
        {
            Manager.RequestCharge();
        }
    }

    public override void OnInteract_release(PlayerMain who)
    {
        if (Manager == null)
            return;

        if (NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline && !NetworkSystem.Instance.IsServer)
        {
            NetworkRouter.Instance.SendMessageToServer(new NMS_Client_PeakOfEnergyChargeRequest(false));
        }
        else
        {
            Manager.ReleaseCharge();
        }
    }
}
