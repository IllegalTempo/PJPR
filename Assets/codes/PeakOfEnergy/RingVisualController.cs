using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the physical ring's visuals from the manager's UnityEvents (works on server
/// AND client mirrors, since the manager fires the same events everywhere).
///
/// Color states (from the mission spec):
///   Blue           : currentRPM &lt; targetRPM_Min        (too slow - pull harder)
///   Green          : in range AND stable                (sweet spot - ready to charge, pulsing glow)
///   Red/Orange     : currentRPM &gt; targetRPM_Max        (too fast - slow down)
///   Flashing Yellow: isStable == false                  (wobble - Rust Eater interference, overrides all)
///
/// The spin is done PURELY with Unity physics: the Rigidbody drives rotation via its
/// angularVelocity. Use the Rigidbody's constraints in Unity to freeze position XYZ and
/// rotation X/Y, leaving Z rotation free, so the ring spins in place around its local Z.
/// </summary>
public class RingVisualController : MonoBehaviour
{
    [Header("Ring Rendering")]
    [Tooltip("The whole ring group (parent GameObject containing the ring pieces). All its renderers are colored together. Its Rigidbody drives the spin.")]
    [SerializeField] private Transform ringVisualsRoot;
    [Tooltip("MeshRenderer of a single ring piece (used only when Ring Visuals Root is empty).")]
    [SerializeField] private Renderer ringRenderer;
    [Tooltip("Optional light on the ring for the pulsing glow.")]
    [SerializeField] private Light ringLight;
    [Tooltip("Scale the spin speed (1 = ring rotates at currentRPM).")]
    [SerializeField] private float spinVisualMultiplier = 1f;
    [Tooltip("Rigidbody that drives the ring's spin (auto-found on Ring Visuals Root if empty). It must have rotation X/Y frozen and Z free in its constraints.")]
    [SerializeField] private Rigidbody spinRigidbody;

    [Header("Center Button")]
    [SerializeField] private Renderer buttonRenderer;
    [SerializeField] private Light buttonLight;

    [Header("Colors")]
    [SerializeField] private Color tooSlowColor = new Color(0.15f, 0.4f, 1f);      // blue
    [SerializeField] private Color sweetSpotColor = new Color(0.2f, 1f, 0.4f);     // green
    [SerializeField] private Color tooFastColor = new Color(1f, 0.35f, 0.1f);      // orange/red
    [SerializeField] private Color unstableColor = new Color(1f, 0.95f, 0.3f);     // flashing yellow
    [SerializeField] private Color victoryColor = new Color(0.3f, 1f, 0.8f);       // teal-gold on victory
    [SerializeField] private Color gameOverColor = new Color(0.5f, 0.1f, 0.1f);    // dark red on game over
    [SerializeField] private Color idleColor = new Color(0.5f, 0.5f, 0.5f);        // gray while idle

    [Header("Effects")]
    [Tooltip("Pulsing speed of the green sweet-spot glow.")]
    [SerializeField] private float sweetSpotPulseSpeed = 3f;
    [Tooltip("Minimum glow intensity (0-1).")]
    [SerializeField] private float sweetSpotPulseMin = 0.5f;
    [Tooltip("Maximum glow intensity (0-1).")]
    [SerializeField] private float sweetSpotPulseMax = 1f;
    [Tooltip("Seconds the red 'failed' flash stays visible.")]
    [SerializeField] private float failFlashDuration = 0.4f;
    [Tooltip("Particle effect played on the ring when a charge completes (electric burst).")]
    [SerializeField] private GameObject chargeBurstEffect;

    private PeakOfEnergyManager manager;
    private bool isReadyToCharge;
    private bool isCharging;
    private Color currentColor;
    private Coroutine failFlashRoutine;
    private Renderer[] ringRenderers = new Renderer[0];

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

    private void Awake()
    {
        if (ringVisualsRoot != null)
        {
            // Whole-group mode: color every renderer under the root.
            ringRenderers = ringVisualsRoot.GetComponentsInChildren<Renderer>();
        }
        else
        {
            // Single-piece mode (fallback).
            ringRenderers = ringRenderer != null ? new[] { ringRenderer } : new Renderer[0];
        }

        // Find the Rigidbody that spins the ring.
        if (spinRigidbody == null)
            spinRigidbody = ringVisualsRoot != null
                ? ringVisualsRoot.GetComponent<Rigidbody>()
                : GetComponent<Rigidbody>();

        currentColor = idleColor;
        ApplyColor(currentColor);
    }

    private void OnEnable()
    {
        if (Manager != null)
        {
            Manager.OnRPMChanged.AddListener(OnRPMChanged);
            Manager.OnStabilityChanged.AddListener(OnStabilityChanged);
            Manager.OnStateChanged.AddListener(OnStateChanged);
            Manager.OnChargeStarted.AddListener(OnChargeStarted);
            Manager.OnChargeCompleted.AddListener(OnChargeCompleted);
            Manager.OnChargeFailed.AddListener(OnChargeFailed);
        }
    }

    private void OnDisable()
    {
        if (manager != null)
        {
            manager.OnRPMChanged.RemoveListener(OnRPMChanged);
            manager.OnStabilityChanged.RemoveListener(OnStabilityChanged);
            manager.OnStateChanged.RemoveListener(OnStateChanged);
            manager.OnChargeStarted.RemoveListener(OnChargeStarted);
            manager.OnChargeCompleted.RemoveListener(OnChargeCompleted);
            manager.OnChargeFailed.RemoveListener(OnChargeFailed);
        }
    }

    private void OnDestroy()
    {
        if (failFlashRoutine != null)
            StopCoroutine(failFlashRoutine);
    }

    // ---- Manager events ----

    private void OnRPMChanged(float rpm) => RefreshColor();

    private void OnStabilityChanged(bool stable) => RefreshColor();

    private void OnStateChanged(PeakOfEnergyManager.MissionState state) => RefreshColor();

    private void OnChargeStarted()
    {
        isCharging = true;
        RefreshColor();
    }

    private void OnChargeCompleted(int index)
    {
        isCharging = false;
        RefreshColor();
        PlayChargeBurst();
    }

    private void OnChargeFailed()
    {
        isCharging = false;
        RefreshColor();
        if (failFlashRoutine != null)
            StopCoroutine(failFlashRoutine);
        failFlashRoutine = StartCoroutine(FailFlash());
    }

    // ---- Color logic ----

    private void RefreshColor()
    {
        if (Manager == null)
            return;

        PeakOfEnergyManager.MissionState state = Manager.State;
        if (state == PeakOfEnergyManager.MissionState.Idle)
        {
            currentColor = idleColor;
            isReadyToCharge = false;
            ApplyColor(currentColor);
            ApplyButton(false);
            return;
        }

        if (state == PeakOfEnergyManager.MissionState.Victory)
        {
            currentColor = victoryColor;
            ApplyColor(currentColor);
            ApplyButton(false);
            return;
        }

        if (state == PeakOfEnergyManager.MissionState.GameOver)
        {
            currentColor = gameOverColor;
            ApplyColor(currentColor);
            ApplyButton(false);
            return;
        }

        if (state == PeakOfEnergyManager.MissionState.Cooldown)
        {
            // Keep showing the sweet-spot color so players remember the next target.
            currentColor = Manager.IsRpmInTargetRange() ? sweetSpotColor : PickRangeColor();
            ApplyColor(currentColor);
            ApplyButton(false);
            return;
        }

        // Spinning / Stabilizing: the 4-state color logic from the spec.
        if (!Manager.IsStable)
        {
            currentColor = unstableColor; // overrides all
            isReadyToCharge = false;
        }
        else if (Manager.IsRpmInTargetRange())
        {
            currentColor = sweetSpotColor;
            isReadyToCharge = !isCharging;
        }
        else
        {
            currentColor = PickRangeColor();
            isReadyToCharge = false;
        }

        ApplyColor(currentColor);
        ApplyButton(isReadyToCharge || isCharging);
    }

    private Color PickRangeColor()
    {
        return Manager.CurrentRPM < Manager.GetTargetMin() ? tooSlowColor : tooFastColor;
    }

    private void ApplyColor(Color color)
    {
        foreach (Renderer renderer in ringRenderers)
        {
            if (renderer != null)
                renderer.material.color = color;
        }

        if (ringLight != null)
            ringLight.color = color;
    }

    private void ApplyButton(bool glowing)
    {
        Color buttonColor = glowing ? sweetSpotColor : new Color(0.3f, 0.3f, 0.3f);
        if (buttonRenderer != null)
            buttonRenderer.material.color = buttonColor;
        if (buttonLight != null)
        {
            buttonLight.color = buttonColor;
            buttonLight.intensity = glowing ? 3f : 0.5f;
        }
    }

    private void PlayChargeBurst()
    {
        if (chargeBurstEffect == null)
            return;
        chargeBurstEffect.SetActive(false);
        chargeBurstEffect.SetActive(true);
    }

    private IEnumerator FailFlash()
    {
        float elapsed = 0f;
        while (elapsed < failFlashDuration)
        {
            elapsed += Time.deltaTime;
            float flash = (Mathf.Sin(elapsed * 40f) > 0f) ? 1f : 0f;
            Color flashColor = Color.Lerp(currentColor, Color.white, flash);
            foreach (Renderer renderer in ringRenderers)
            {
                if (renderer != null)
                    renderer.material.color = flashColor;
            }
            yield return null;
        }
        RefreshColor();
        failFlashRoutine = null;
    }

    // ---- Per-frame visuals ----

    private void Update()
    {
        if (Manager == null)
            return;

        // Green pulsing glow in the sweet spot.
        if (isReadyToCharge || isCharging)
        {
            float pulse = Mathf.Lerp(sweetSpotPulseMin, sweetSpotPulseMax,
                (Mathf.Sin(Time.time * sweetSpotPulseSpeed) + 1f) * 0.5f);
            if (ringLight != null)
                ringLight.intensity = pulse * (isCharging ? 2f : 1f);
        }
    }

    /// <summary>
    /// Drives the ring's spin PURELY with Unity physics: sets the Rigidbody's
    /// angularVelocity around the ring's local Z axis. Unity's physics then integrates
    /// the rotation. The Rigidbody's constraints (freeze position XYZ + rotation X/Y,
    /// leave Z free) keep it spinning in place - position never drifts and X/Y rotation
    /// stays locked, so a tilted ring just turns around its local Z without tumbling.
    /// </summary>
    private void FixedUpdate()
    {
        if (Manager == null || spinRigidbody == null)
            return;

        // Revolutions per minute -> radians per second.
        float radiansPerSecond = Manager.CurrentRPM / 60f * Mathf.PI * 2f * spinVisualMultiplier;

        // Local Z in world space (the free rotation axis the constraints leave unlocked).
        Vector3 axisWorld = spinRigidbody.transform.TransformDirection(Vector3.forward);
        spinRigidbody.angularVelocity = axisWorld * radiansPerSecond;
    }
}
