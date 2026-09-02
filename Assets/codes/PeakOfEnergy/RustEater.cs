using System.Collections;
using UnityEngine;
using Assets.codes.Network.Messages;
using Assets.codes.Network.SyncedIdentity;

/// <summary>
/// The "stability breaker" enemy. Spawns at the ring edge, crawls to a latch point and
/// latches onto the ring. While latched it applies an asymmetrical wobble to the manager
/// via <see cref="PeakOfEnergyManager.ApplyWobble"/> - this does NOT change the average RPM,
/// it only makes isStable flip to false. Multiple Rust Eaters stack the effect.
///
/// SERVER-AUTHORITATIVE: only the world manager (host/offline) simulates the crawl and
/// latches. Clients simply follow the synced transform (NetworkGameObject), so there is no
/// double-movement and the eater never "flies away". The Rigidbody must be KINEMATIC on
/// the prefab so physics don't fight the server's transform writes.
///
/// Removal: EVA + Hammer (instant). Projectiles (future cannon) can chip it down first.
/// </summary>
[RequireComponent(typeof(NetworkPrefabIdentity))]
public class RustEater : Selectable
{
    [Header("Stats")]
    [Tooltip("Wobble intensity while latched. One eater should push past rpmFluctuationTolerance.")]
    [SerializeField] private float wobbleIntensity = 8f;
    [Tooltip("Seconds to crawl from the spawn point to the ring edge.")]
    [SerializeField] private float crawlDuration = 2f;
    [Tooltip("Projectile damage needed to destroy it (future cannon). 0 = only the hammer can kill it.")]
    [SerializeField] private float health = 0f;

    [Header("VFX")]
    [Tooltip("Spawned at the latch point when destroyed.")]
    [SerializeField] private GameObject deathEffectPrefab;

    private int wobbleId = -1;
    private Vector3 latchTarget;
    private bool isLatched = false;
    private bool isDestroyed = false;
    private NetworkGameObject netObj;
    private Coroutine crawlRoutine;

    protected override int Layer => 6; // Selectable layer so the hammer can target this enemy

    private PeakOfEnergyManager Manager => PeakOfEnergyManager.Instance;

    private bool IsWorldManagerSide => NetworkSystem.Instance == null || NetworkSystem.Instance.IsWorldManager;

    private void Awake()
    {
        netObj = GetComponent<NetworkGameObject>();
    }

    private void OnDisable()
    {
        RemoveWobble();
    }

    /// <summary>Called by the spawner right after the network object is created (server-side only).</summary>
    public void Initialize(Vector3 latchTarget)
    {
        if (!IsWorldManagerSide)
            return; // only the server simulates the crawl & latch

        this.latchTarget = latchTarget;
        // Knock the eater onto the ring's latch point instantly - no long slow crawl that
        // lets it drift. It visibly "attaches" to the ring edge.
        transform.position = latchTarget;
        AttachAndApplyWobble();
    }

    private void AttachAndApplyWobble()
    {
        if (isDestroyed) return;

        isLatched = true;
        if (Manager != null)
            wobbleId = Manager.ApplyWobble(wobbleIntensity);

        Debug.Log("[RustEater] Latched onto the ring - stability broken!");
    }

    /// <summary>
    /// Hammer melee entry point (client or server). Online clients ask the server;
    /// the server/offline path destroys directly.
    /// </summary>
    public void HammerHit()
    {
        if (!IsWorldManagerSide)
        {
            NetworkRouter.Instance.SendMessageToServer(
                new NMS_Client_PeakOfEnergyHammerHit(netObj != null ? netObj.Identity.Identifier : string.Empty));
            return;
        }

        DestroyWithHammer();
    }

    public void TakeDamage(float amount)
    {
        if (isDestroyed || health <= 0f) return;

        health -= amount;
        if (health <= 0f)
            DestroyWithHammer();
    }

    /// <summary>Destroys the eater instantly, removes its wobble and plays the death VFX. Server-side.</summary>
    public void DestroyWithHammer()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        RemoveWobble();

        if (crawlRoutine != null)
            StopCoroutine(crawlRoutine);

        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        Debug.Log("[RustEater] Destroyed by hammer! Wobble removed.");

        if (netObj != null && GameCore.Instance != null)
            GameCore.Instance.DestroyNetworkObject(netObj.Identity.Identifier);
        else
            Destroy(gameObject);
    }

    private void RemoveWobble()
    {
        if (wobbleId >= 0 && Manager != null)
        {
            Manager.RemoveWobble(wobbleId);
            wobbleId = -1;
        }
    }
}
