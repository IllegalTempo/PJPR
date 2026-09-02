using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.codes.Network.SyncedIdentity;
using Cysharp.Threading.Tasks;

/// <summary>
/// Ring-centric enemy spawner for Peak Of Energy (server / offline only).
///
/// Spawns mission meteorites (fly toward the ring) and Rust Eaters (latch on and wobble)
/// during the Spinning, Stabilizing and Cooldown states. The spawn rate scales up with
/// the current charge index. On a successful charge the ring bursts, so all active
/// enemies are cleared.
///
/// The ring radius is AUTO-MEASURED from the ring's renderers at runtime, and all spawn /
/// latch positions are computed in the ring's own plane (Ring Visuals Root's local XZ),
/// so you do NOT need to guess the radius and the ring can be tilted in the scene.
/// Place this component on the ring device (same object as PeakOfEnergyManager).
/// </summary>
public class PeakOfEnergySpawner : MonoBehaviour
{
    [Header("Network Prefab IDs (must match NetworkPrefabRegistry entries)")]
    [Tooltip("PrefabId of the mission meteorite prefab.")]
    [SerializeField] private string meteoritePrefabId = "PeakMeteorite";
    [Tooltip("PrefabId of the Rust Eater prefab.")]
    [SerializeField] private string rustEaterPrefabId = "RustEater";

    [Header("Ring Measurement")]
    [Tooltip("Drag the OUTER_RING object here ONLY if you want this spawner to reuse its geometry. Leave empty to use this spawner object's position as the ring center.")]
    [SerializeField] private Transform ringVisualsRoot;
    [Tooltip("Ring radius in world units - the distance from the ring's center to its outer edge. THIS is the source of truth for where enemies spawn and where Rust Eaters latch. Set one round number that matches your ring (e.g. 25).")]
    [SerializeField] private float ringRadius = 12f;

    [Header("Natural Targeting")]
    [Tooltip("Chance (0-1) that a meteorite or Rust Eater homes in on one of the actual ring COMPONENTS (the individual OUTER_RING pieces with their own positions) rather than a generic point on the ring circle. The rest scatter naturally around the ring.")]
    [Range(0f, 1f)]
    [SerializeField] private float componentAimChance = 0.5f;
    [Tooltip("Random offset applied around the chosen component/ring target so spawns don't all meet at the exact same pixel (natural scatter).")]
    [SerializeField] private float targetScatterRadius = 2f;

    [Header("Arena Layout")]
    [Tooltip("Extra distance BEYOND the ring radius where meteorites spawn, so they always come from outside the ring and never spawn inside it.")]
    [SerializeField] private float spawnDistanceMargin = 30f;
    [Tooltip("Vertical band around the ring plane where meteorites spawn.")]
    [SerializeField] private float spawnHeightRange = 8f;

    [Header("Meteorites")]
    [SerializeField] private float meteoriteSpeed = 12f;
    [SerializeField] private float meteoriteBaseInterval = 6f;
    [SerializeField] private float meteoriteMinInterval = 1.5f;

    [Header("Rust Eaters")]
    [SerializeField] private float rustEaterBaseInterval = 25f;
    [SerializeField] private float rustEaterMinInterval = 9f;
    [SerializeField] private int maxRustEaters = 4;

    [Header("Limits")]
    [SerializeField] private int maxActiveMeteorites = 24;

    private readonly HashSet<GameObject> activeMeteorites = new HashSet<GameObject>();
    private readonly HashSet<GameObject> activeRustEaters = new HashSet<GameObject>();
    private Coroutine meteoriteLoop;
    private Coroutine rustEaterLoop;
    private bool isActive;

    // Measured ring geometry (world space), centered on the manager/ring device.
    private Vector3 measuredCenter = Vector3.zero;
    private Vector3 measuredPlaneNormal = Vector3.up;
    private Vector3 planeRight = Vector3.right;
    private Vector3 planeForward = Vector3.forward;

    // The individual ring pieces (OUTER_RING's children) that enemies can aim at.
    private readonly List<Transform> ringComponents = new List<Transform>();

    private PeakOfEnergyManager manager;

    private void Awake()
    {
        manager = GetComponentInParent<PeakOfEnergyManager>();
        if (manager == null)
            manager = FindFirstObjectByType<PeakOfEnergyManager>();
    }

    private void Start()
    {
        if (!NetworkSystem.Instance.IsWorldManager)
        {
            enabled = false;
            return;
        }

        EnsureGeometry();
    }

    private void OnEnable()
    {
        if (manager != null)
        {
            manager.OnStateChanged.AddListener(HandleStateChanged);
            manager.OnChargeCompleted.AddListener(HandleChargeCompleted);
            manager.OnGameOver.AddListener(HandleMissionEnded);
            manager.OnVictory.AddListener(HandleMissionEnded);
        }
    }

    private void OnDisable()
    {
        SetSpawningActive(false);
        if (manager != null)
        {
            manager.OnStateChanged.RemoveListener(HandleStateChanged);
            manager.OnChargeCompleted.RemoveListener(HandleChargeCompleted);
            manager.OnGameOver.RemoveListener(HandleMissionEnded);
            manager.OnVictory.RemoveListener(HandleMissionEnded);
        }
    }

    // =====================================================================
    // Ring geometry - centered on the manager (ring device), manual radius.
    // The ring center is this object's position (the spawner sits on the ring
    // device alongside PeakOfEnergyManager). All enemies are placed relative to
    // that center on the device's plane, using the manual Ring Radius you set.
    // =====================================================================

    private float EffectiveRingRadius => ringRadius;

    /// <summary>Read-only: the manually-configured ring radius (used by meteorites for impact distance).</summary>
    public float RingRadius => ringRadius;

    private void EnsureGeometry()
    {
        if (manager == null)
            manager = GetComponentInParent<PeakOfEnergyManager>();
        if (manager == null)
            manager = FindFirstObjectByType<PeakOfEnergyManager>();

        Transform planeRoot = manager != null ? manager.transform
                             : (ringVisualsRoot != null ? ringVisualsRoot : transform);
        measuredCenter = planeRoot.position;
        measuredPlaneNormal = planeRoot.up.normalized;

        // Build a right-handed basis in the ring plane (normalized).
        Vector3 right = Vector3.Cross(measuredPlaneNormal, Vector3.up);
        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.Cross(measuredPlaneNormal, Vector3.right);
        right.Normalize();
        Vector3 forward = Vector3.Cross(right, measuredPlaneNormal).normalized;
        planeRight = right;
        planeForward = forward;

        CollectRingComponents();

        Debug.Log($"[PeakOfEnergySpawner] Ring geometry: center={measuredCenter}, plane={measuredPlaneNormal}, radius={EffectiveRingRadius}.");
    }

    /// <summary>
    /// Collects the individual ring pieces (the direct children under Ring Visuals Root,
    /// e.g. the 16 OUTER_RING components) so enemies can aim at/hang on individual parts.
    /// Components with a Renderer are kept; nested visual-only groups without renderers
    /// are skipped so we aim at the actual visible pieces.
    /// </summary>
    private void CollectRingComponents()
    {
        ringComponents.Clear();
        if (ringVisualsRoot == null)
            return;

        for (int i = 0; i < ringVisualsRoot.childCount; i++)
        {
            Transform child = ringVisualsRoot.GetChild(i);
            if (child == null)
                continue;

            // A usable component is either a renderer itself or has one in children.
            bool hasRenderer = child.GetComponentInChildren<Renderer>() != null;
            if (hasRenderer)
                ringComponents.Add(child);
        }

        Debug.Log($"[PeakOfEnergySpawner] Collected {ringComponents.Count} ring component(s) for natural aiming.");
    }

    /// <summary>World position on the ring's plane at the given angle and distance from the ring center.</summary>
    private Vector3 RingPoint(float angle, float distance)
    {
        return measuredCenter
            + (Mathf.Cos(angle) * planeRight + Mathf.Sin(angle) * planeForward) * distance
            + measuredPlaneNormal * 0.5f;
    }

    /// <summary>A point on the ring body itself (between the inner half and the outer edge).</summary>
    private Vector3 RandomRingPoint(float minRadiusFraction, float maxRadiusFraction)
    {
        float radius = ringRadius * Random.Range(minRadiusFraction, maxRadiusFraction);
        return RingPoint(Random.Range(0f, Mathf.PI * 2f), radius);
    }

    /// <summary>
    /// Picks a natural target for an enemy: sometimes one of the actual ring components
    /// (hitting/latching a specific OUTER_RING piece), otherwise a generic scatter point on
    /// the ring circle. A random offset is added so enemies don't all converge on one spot.
    /// </summary>
    private Vector3 PickNaturalTarget()
    {
        Vector3 basePoint;
        if (ringComponents.Count > 0 && Random.value < componentAimChance)
        {
            // Home in on one of the ring's individual components.
            basePoint = ringComponents[Random.Range(0, ringComponents.Count)].position;
        }
        else
        {
            // Generic point on/near the ring body.
            basePoint = RandomRingPoint(0.4f, 1.0f);
        }

        // Natural scatter so spawns feel organic rather than pixel-perfect.
        basePoint += Random.insideUnitSphere * targetScatterRadius;
        basePoint += measuredPlaneNormal * Random.Range(-0.5f, 0.5f);
        return basePoint;
    }

    // =====================================================================
    // Manager events
    // =====================================================================

    private void HandleStateChanged(PeakOfEnergyManager.MissionState state)
    {
        bool shouldSpawn = state == PeakOfEnergyManager.MissionState.Spinning
                        || state == PeakOfEnergyManager.MissionState.Stabilizing
                        || state == PeakOfEnergyManager.MissionState.Cooldown;
        SetSpawningActive(shouldSpawn);

        if (!shouldSpawn)
            ClearEnemies();
    }

    private void HandleChargeCompleted(int chargeIndex)
    {
        // Electric burst kills all active minor enemies.
        ClearEnemies();
    }

    private void HandleMissionEnded()
    {
        SetSpawningActive(false);
        ClearEnemies();
    }

    private void SetSpawningActive(bool active)
    {
        isActive = active;

        if (meteoriteLoop != null)
        {
            StopCoroutine(meteoriteLoop);
            meteoriteLoop = null;
        }
        if (rustEaterLoop != null)
        {
            StopCoroutine(rustEaterLoop);
            rustEaterLoop = null;
        }

        if (active)
        {
            meteoriteLoop = StartCoroutine(MeteoriteSpawnLoop());
            rustEaterLoop = StartCoroutine(RustEaterSpawnLoop());
        }
    }

    // =====================================================================
    // Spawn loops
    // =====================================================================

    private float CurrentChargeScale01
    {
        get
        {
            int tierCount = manager != null && manager.TargetRPM_Min != null
                ? Mathf.Max(1, manager.TargetRPM_Min.Length - 1)
                : 4;
            return Mathf.Clamp01(manager != null ? manager.CurrentChargeIndex / (float)tierCount : 0f);
        }
    }

    private float ScaledInterval(float baseInterval, float minInterval)
    {
        return Mathf.Lerp(baseInterval, minInterval, CurrentChargeScale01);
    }

    private IEnumerator MeteoriteSpawnLoop()
    {
        while (isActive)
        {
            yield return new WaitForSeconds(ScaledInterval(meteoriteBaseInterval, meteoriteMinInterval));
            if (!isActive) yield break;

            if (activeMeteorites.Count < maxActiveMeteorites)
                SpawnMeteorite().Forget();
        }
    }

    private IEnumerator RustEaterSpawnLoop()
    {
        while (isActive)
        {
            yield return new WaitForSeconds(ScaledInterval(rustEaterBaseInterval, rustEaterMinInterval));
            if (!isActive) yield break;

            if (activeRustEaters.Count < maxRustEaters)
                SpawnRustEater().Forget();
        }
    }

    // =====================================================================
    // Spawning
    // =====================================================================

    private async UniTask SpawnMeteorite()
    {
        // Spawn outside the ring, then aim at a NATURALLY-CHOSEN target: sometimes a
        // specific ring component, otherwise a scatter point on the ring body.
        Vector3 spawnPos = RandomSpawnPosition();
        Vector3 target = PickNaturalTarget();

        NetworkGameObject nobj = await NetworkSystem.Instance.CreateNetworkObject(meteoritePrefabId, spawnPos, Random.rotation, 0);
        if (nobj == null)
        {
            Debug.LogWarning($"[PeakOfEnergySpawner] Failed to create '{meteoritePrefabId}'. Is it registered in NetworkPrefabRegistry?");
            return;
        }

        PeakOfEnergyMeteorite meteorite = nobj.gameObject.GetComponent<PeakOfEnergyMeteorite>();
        if (meteorite != null)
            meteorite.Initialize(target, meteoriteSpeed);

        float scale = Random.Range(0.8f, 1.4f);
        nobj.gameObject.transform.localScale = Vector3.one * scale;

        activeMeteorites.Add(nobj.gameObject);
        StartCoroutine(WatchForSelfDestroy(nobj.gameObject, activeMeteorites));
    }

    private async UniTask SpawnRustEater()
    {
        // Latch onto a naturally-chosen point: sometimes a specific ring component,
        // otherwise a scatter point near the ring's outer edge.
        Vector3 latchTarget = PickNaturalTarget();

        // Spawn just outside the ring, offset from the latch point.
        float spawnAngle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 spawnPos = RingPoint(spawnAngle, EffectiveRingRadius + Random.Range(4f, 10f)) + measuredPlaneNormal * Random.Range(-2f, 2f);

        NetworkGameObject nobj = await NetworkSystem.Instance.CreateNetworkObject(rustEaterPrefabId, spawnPos, Random.rotation, 0);
        if (nobj == null)
        {
            Debug.LogWarning($"[PeakOfEnergySpawner] Failed to create '{rustEaterPrefabId}'. Is it registered in NetworkPrefabRegistry?");
            return;
        }

        RustEater eater = nobj.gameObject.GetComponent<RustEater>();
        if (eater != null)
            eater.Initialize(latchTarget);

        activeRustEaters.Add(nobj.gameObject);
        StartCoroutine(WatchForSelfDestroy(nobj.gameObject, activeRustEaters));
    }

    private Vector3 RandomSpawnPosition()
    {
        // Spawn OUTSIDE the ring (radius + margin) so meteorites come from far away and
        // never spawn inside the ring where they'd instantly hit it.
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = ringRadius + spawnDistanceMargin;
        Vector3 position = RingPoint(angle, distance);
        position += measuredPlaneNormal * Random.Range(-spawnHeightRange, spawnHeightRange);
        return position;
    }

    private IEnumerator WatchForSelfDestroy(GameObject obj, HashSet<GameObject> set)
    {
        while (obj != null && set.Contains(obj))
        {
            yield return new WaitForSeconds(2f);
        }
        set.Remove(obj);
    }

    // =====================================================================
    // Clearing
    // =====================================================================

    private void ClearEnemies()
    {
        DestroyAllIn(activeMeteorites);
        DestroyAllIn(activeRustEaters);
    }

    private void DestroyAllIn(HashSet<GameObject> set)
    {
        foreach (GameObject obj in new List<GameObject>(set))
        {
            if (obj == null) continue;
            NetworkGameObject nobj = obj.GetComponent<NetworkGameObject>();
            if (nobj != null && GameCore.Instance != null)
                GameCore.Instance.DestroyNetworkObject(nobj.Identity.Identifier);
        }
        set.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if (manager == null) return;
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, ringRadius);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, ringRadius + spawnDistanceMargin);
    }
}
