using System.Collections;
using UnityEngine;
using Assets.codes.Network.Messages;
using Assets.codes.Network.SyncedIdentity;

/// <summary>
/// Mission meteorite for Peak Of Energy. Spawns at a distance and flies toward the ring.
///
///   * Reaches the ring -> Manager.TakeDamage(flat damage) + self-destruct (server resolves).
///   * Hits a player    -> player is bounced far backward + briefly slowed. NO player damage.
///   * Hammer (EVA)     -> instant destruction.
///   * Cannon (future)  -> projectile TakeDamage.
///   * Lifetime expired -> self-destructs (never hangs in the air).
///
/// SERVER-AUTHORITATIVE + KINEMATIC: only the world manager simulates movement (by
/// translating the kinematic Rigidbody toward the target). Clients just follow the synced
/// transform, and the ring impact is detected by DISTANCE as well as collision, so a
/// meteorite always "keeps moving or disappears" - it never stops dead in the middle.
/// The Rigidbody on the prefab must be KINEMATIC.
/// </summary>
[RequireComponent(typeof(NetworkPrefabIdentity))]
[RequireComponent(typeof(Rigidbody))]
public class PeakOfEnergyMeteorite : Selectable
{
    [Header("Stats")]
    [Tooltip("Ring damage dealt on impact. 0 = use PeakOfEnergyManager.meteoriteDamage.")]
    [SerializeField] private int damage = 0;
    [Tooltip("Health; hammer destroys instantly, future cannon projectiles chip it down.")]
    [SerializeField] private float health = 50f;
    [Tooltip("Impulse strength applied to a player on collision.")]
    [SerializeField] private float bounceForce = 12f;
    [Tooltip("How long the player is slowed after being hit (seconds).")]
    [SerializeField] private float slowDuration = 2f;
    [Tooltip("Movement speed multiplier while slowed.")]
    [SerializeField] private float slowFactor = 0.3f;
    [Tooltip("If the meteorite hasn't reached the ring in this many seconds, it self-destructs.")]
    [SerializeField] private float maxLifetime = 12f;

    [Header("Breaking")]
    [SerializeField] private GameObject fragmentPrefab;
    [SerializeField] private int fragmentCount = 5;
    [SerializeField] private float fragmentForce = 5f;
    [SerializeField] private float fragmentLifetime = 5f;
    [SerializeField] private GameObject breakEffectPrefab;

    private bool isDestroyed = false;
    private bool hasImpacted = false;
    private NetworkGameObject netObj;
    private Rigidbody rb;

    private Vector3 moveDirection;
    private float moveSpeed;
    private float timeoutAt;
    private PeakOfEnergyManager ringManager;
    private float ringRadius = 12f;
    private Vector3 ringCenter;
    private bool initialized;

    protected override int Layer => 6; // Selectable layer so the hammer can target this meteorite

    private PeakOfEnergyManager Manager => PeakOfEnergyManager.Instance;

    private bool IsWorldManagerSide => NetworkSystem.Instance == null || NetworkSystem.Instance.IsWorldManager;

    private void Awake()
    {
        netObj = GetComponent<NetworkGameObject>();
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // server drives movement by transform; physics must not interfere
            rb.useGravity = false;
        }
    }

    /// <summary>Called by the spawner right after creation (server-side only).</summary>
    public void Initialize(Vector3 targetPosition, float speed)
    {
        if (!IsWorldManagerSide)
            return; // only the server simulates movement; clients follow the synced transform

        ringManager = RingManagerAt(targetPosition);
        if (ringManager != null)
        {
            ringCenter = ringManager.transform.position;
            // Match the spawner's manually-set Ring Radius so the meteorite's impact
            // distance agrees with where enemies/ring actually are. Fall back to 12 if
            // no spawner config is available.
            ringRadius = GetConfiguredRingRadius(ringManager);
        }
        else
        {
            ringCenter = targetPosition;
            ringRadius = 0f; // no ring known -> rely on collision + lifetime only
        }

        Vector3 start = transform.position;
        Vector3 delta = targetPosition - start;
        moveDirection = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.forward;
        moveSpeed = speed;

        // Fire immediately toward the ring and keep flying without re-aiming each frame.
        timeoutAt = Time.time + Mathf.Max(1f, maxLifetime);
        initialized = true;

        // Face the direction of travel (visual only).
        transform.rotation = Quaternion.LookRotation(moveDirection);
        Debug.Log($"[PeakOfEnergyMeteorite] launched toward {targetPosition} at {speed}u/s");
    }

    private PeakOfEnergyManager RingManagerAt(Vector3 target)
    {
        // Prefer the singleton; if none, try to find one whose transform is near the target.
        PeakOfEnergyManager m = PeakOfEnergyManager.Instance;
        if (m != null)
            return m;
        PeakOfEnergyManager[] all = FindObjectsByType<PeakOfEnergyManager>(FindObjectsSortMode.None);
        if (all == null || all.Length == 0)
            return null;
        return all[0];
    }

    /// <summary>Uses the PeakOfEnergySpawner's manually-configured Ring Radius so impact
    /// distance matches the actual ring. Falls back to the manager's rough default if no
    /// spawner is present.</summary>
    private float GetConfiguredRingRadius(PeakOfEnergyManager m)
    {
        // Walk up from the manager to find the spawner that configured the ring radius.
        Transform current = m != null ? m.transform : transform;
        while (current != null)
        {
            PeakOfEnergySpawner spawner = current.GetComponent<PeakOfEnergySpawner>();
            if (spawner != null && spawner.RingRadius > 0f)
                return spawner.RingRadius;
            current = current.parent;
        }

        // No spawner config found - fall back to a sensible default.
        return 12f;
    }

    protected override void Update()
    {
        base.Update(); // keep the Selectable outline working

        if (isDestroyed || !IsWorldManagerSide || !initialized)
            return;

        // Move toward the stored target every frame (server-authoritative).
        transform.position += moveDirection * (moveSpeed * Time.deltaTime);

        // Guaranteed ring impact when close enough, regardless of collider chain.
        if (ringRadius > 0f)
        {
            Vector3 toRing = ringCenter - transform.position;
            if (toRing.magnitude <= ringRadius + 0.5f && !hasImpacted)
            {
                ImpactRing();
                return;
            }
        }

        // Lifetime fallback: never leave a meteorite hanging in the air forever.
        if (Time.time >= timeoutAt)
        {
            Debug.Log("[PeakOfEnergyMeteorite] Lifetime expired - self-destructing.");
            DestroyMeteorite();
        }
    }

    private void ImpactRing()
    {
        if (hasImpacted) return;
        if (ringManager == null) ringManager = Manager;

        int ringDamage = damage > 0 ? damage : (ringManager != null ? ringManager.MeteoriteDamage : 10);
        ringManager?.TakeDamage(ringDamage);
        Debug.Log($"[PeakOfEnergyMeteorite] Hit the ring for {ringDamage} damage.");
        DestroyMeteorite();
    }

    // ---- Hammer (EVA melee) ----

    public void HammerHit()
    {
        if (!IsWorldManagerSide)
        {
            NetworkRouter.Instance.SendMessageToServer(
                new NMS_Client_PeakOfEnergyHammerHit(netObj != null ? netObj.Identity.Identifier : string.Empty));
            return;
        }

        DestroyFromHammer();
    }

    /// <summary>Server-side resolution of a hammer hit.</summary>
    public void DestroyFromHammer()
    {
        Debug.Log("[PeakOfEnergyMeteorite] Destroyed by hammer!");
        DestroyMeteorite();
    }

    /// <summary>Projectile damage (future cannon).</summary>
    public void TakeDamage(float amount)
    {
        if (isDestroyed || amount <= 0f) return;

        health -= amount;
        if (health <= 0f)
            DestroyMeteorite();
    }

    // ---- Collisions (player bounce + belt-and-suspenders ring damage) ----

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    private void HandleHit(GameObject other)
    {
        if (isDestroyed || hasImpacted || other == null) return;

        // Hit a ring part -> flat damage + self destruct (server resolves).
        PeakOfEnergyManager ringManagerHit = other.GetComponentInParent<PeakOfEnergyManager>();
        if (ringManagerHit != null)
        {
            if (IsWorldManagerSide)
            {
                ringManager = ringManagerHit;
                ImpactRing();
            }
            return;
        }

        // Hit a player -> bounce + slow (no damage). Applied locally to the local player.
        PlayerMain player = other.GetComponentInParent<PlayerMain>();
        if (player != null)
        {
            HandlePlayerHit(player);
        }
    }

    private void HandlePlayerHit(PlayerMain player)
    {
        if (player.networkinfo == null || !player.networkinfo.IsLocal)
            return; // each machine only bounces its own local player

        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb == null) return;

        Vector3 away = (player.transform.position - transform.position).normalized;
        away.y = Mathf.Max(0.15f, away.y); // always knock upward-ish so they don't clip the ring
        playerRb.AddForce(away * bounceForce, ForceMode.Impulse);
        StartCoroutine(SlowPlayerTemporarily(player));
    }

    private IEnumerator SlowPlayerTemporarily(PlayerMain player)
    {
        float original = player.MoveSpeed;
        player.MoveSpeed = Mathf.Max(0.5f, original * slowFactor);
        yield return new WaitForSeconds(slowDuration);
        if (player != null)
            player.MoveSpeed = original;
    }

    // ---- Destruction ----

    public void DestroyMeteorite()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        hasImpacted = true;

        if (breakEffectPrefab != null)
        {
            GameObject effect = Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        if (fragmentPrefab != null)
        {
            for (int i = 0; i < fragmentCount; i++)
            {
                Vector3 randomOffset = Random.insideUnitSphere * 0.5f;
                GameObject fragment = Instantiate(fragmentPrefab, transform.position + randomOffset, Random.rotation);
                Rigidbody fragmentRb = fragment.GetComponent<Rigidbody>();
                if (fragmentRb != null)
                {
                    fragmentRb.AddForce(Random.insideUnitSphere * fragmentForce, ForceMode.Impulse);
                    fragmentRb.AddTorque(Random.insideUnitSphere * fragmentForce, ForceMode.Impulse);
                }
                Destroy(fragment, fragmentLifetime);
            }
        }

        if (netObj != null && GameCore.Instance != null)
            GameCore.Instance.DestroyNetworkObject(netObj.Identity.Identifier);
        else
            Destroy(gameObject);
    }
}
