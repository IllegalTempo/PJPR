using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mission HUD for Peak Of Energy. Works with any Canvas (Screen Space Overlay or
/// World Space above the ring). Listens to the manager's UnityEvents, which fire on
/// both the server and client mirrors, so the HUD behaves identically everywhere.
///
/// Elements wired in the inspector (all optional, null-safe):
///   RPM gauge text, target range text, stability icon, charge radial, charge counter,
///   ring health bar, cooldown radial, status text.
/// </summary>
public class PeakOfEnergyUI : MonoBehaviour
{
    [Header("RPM Gauge")]
    [SerializeField] private TMP_Text rpmValueText;
    [SerializeField] private TMP_Text targetRangeText;

    [Header("Stability Indicator")]
    [SerializeField] private GameObject stableIcon;   // checkmark (green)
    [SerializeField] private GameObject unstableIcon; // X (red)

    [Header("Charge Progress")]
    [SerializeField] private Image chargeRadialFill;  // radial fill circle
    [SerializeField] private GameObject chargeGroup;  // parent shown while charging

    [Header("Charge Counter")]
    [SerializeField] private TMP_Text chargeCounterText; // "Charge X / 5"

    [Header("Ring Health")]
    [SerializeField] private Image healthBarFill;     // horizontal fill
    [SerializeField] private TMP_Text healthText;

    [Header("Cooldown")]
    [SerializeField] private Image cooldownRadialFill; // circular countdown
    [SerializeField] private GameObject cooldownGroup; // parent shown during cooldown

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;     // optional "SPIN THE RING!" etc.

    private PeakOfEnergyManager manager;

    private void Awake()
    {
        manager = GetComponentInParent<PeakOfEnergyManager>();
        if (manager == null)
            manager = FindFirstObjectByType<PeakOfEnergyManager>();
    }

    private void OnEnable()
    {
        if (manager != null)
        {
            manager.OnRPMChanged.AddListener(OnRPMChanged);
            manager.OnStabilityChanged.AddListener(OnStabilityChanged);
            manager.OnChargeStarted.AddListener(OnChargeStarted);
            manager.OnChargeProgress.AddListener(OnChargeProgress);
            manager.OnChargeCompleted.AddListener(OnChargeCompleted);
            manager.OnChargeFailed.AddListener(OnChargeFailed);
            manager.OnHealthChanged.AddListener(OnHealthChanged);
            manager.OnCooldownTick.AddListener(OnCooldownTick);
            manager.OnStateChanged.AddListener(OnStateChanged);
        }
    }

    private void OnDisable()
    {
        if (manager != null)
        {
            manager.OnRPMChanged.RemoveListener(OnRPMChanged);
            manager.OnStabilityChanged.RemoveListener(OnStabilityChanged);
            manager.OnChargeStarted.RemoveListener(OnChargeStarted);
            manager.OnChargeProgress.RemoveListener(OnChargeProgress);
            manager.OnChargeCompleted.RemoveListener(OnChargeCompleted);
            manager.OnChargeFailed.RemoveListener(OnChargeFailed);
            manager.OnHealthChanged.RemoveListener(OnHealthChanged);
            manager.OnCooldownTick.RemoveListener(OnCooldownTick);
            manager.OnStateChanged.RemoveListener(OnStateChanged);
        }
    }

    // ---- RPM gauge ----

    private void OnRPMChanged(float rpm)
    {
        if (rpmValueText != null)
            rpmValueText.text = $"{rpm:F0} RPM";

        if (targetRangeText != null && manager != null)
            targetRangeText.text = $"{manager.GetTargetMin():F0} - {manager.GetTargetMax():F0} RPM";
    }

    // ---- Stability ----

    private void OnStabilityChanged(bool stable)
    {
        if (stableIcon != null)
            stableIcon.SetActive(stable);
        if (unstableIcon != null)
            unstableIcon.SetActive(!stable);
    }

    // ---- Charge progress ----

    private void OnChargeStarted()
    {
        SetChargeGroupVisible(true);
        OnChargeProgress(0f);
    }

    private void OnChargeProgress(float progress)
    {
        if (chargeRadialFill != null)
            chargeRadialFill.fillAmount = Mathf.Clamp01(progress);
    }

    private void OnChargeCompleted(int index)
    {
        SetChargeGroupVisible(false);
        UpdateChargeCounter();
    }

    private void OnChargeFailed()
    {
        SetChargeGroupVisible(false);
        if (statusText != null)
            statusText.text = "CHARGE FAILED!";
    }

    private void SetChargeGroupVisible(bool visible)
    {
        if (chargeGroup != null)
            chargeGroup.SetActive(visible);
    }

    private void UpdateChargeCounter()
    {
        if (chargeCounterText != null && manager != null)
            chargeCounterText.text = $"Charge {manager.CurrentChargeIndex} / {manager.TargetRPM_Min.Length}";
    }

    // ---- Health ----

    private void OnHealthChanged(int current, int max)
    {
        if (healthBarFill != null)
            healthBarFill.fillAmount = max > 0 ? current / (float)max : 0f;
        if (healthText != null)
            healthText.text = $"{current} / {max}";
    }

    // ---- Cooldown ----

    private void OnCooldownTick(float remaining)
    {
        if (cooldownGroup != null)
            cooldownGroup.SetActive(manager != null && manager.State == PeakOfEnergyManager.MissionState.Cooldown);

        if (cooldownRadialFill != null && manager != null && manager.CooldownDuration > 0f)
            cooldownRadialFill.fillAmount = Mathf.Clamp01(remaining / manager.CooldownDuration);
    }

    // ---- State ----

    private void OnStateChanged(PeakOfEnergyManager.MissionState state)
    {
        if (statusText == null)
            return;

        switch (state)
        {
            case PeakOfEnergyManager.MissionState.Idle:
                statusText.text = "AWAITING MISSION...";
                break;
            case PeakOfEnergyManager.MissionState.Spinning:
                statusText.text = "SPIN THE RING!";
                break;
            case PeakOfEnergyManager.MissionState.Stabilizing:
                statusText.text = "HOLD THE CHARGE!";
                break;
            case PeakOfEnergyManager.MissionState.Cooldown:
                statusText.text = "COOLDOWN - CLEAR THE FIELD!";
                break;
            case PeakOfEnergyManager.MissionState.GameOver:
                statusText.text = "GAME OVER - RING DESTROYED";
                break;
            case PeakOfEnergyManager.MissionState.Victory:
                statusText.text = "VICTORY! PEAK OF ENERGY CHARGED!";
                break;
        }
    }

    private void Start()
    {
        UpdateChargeCounter();
        OnHealthChanged(manager != null ? manager.RingCurrentHealth : 0, manager != null ? manager.RingMaxHealth : 1);
        OnRPMChanged(manager != null ? manager.CurrentRPM : 0f);
        OnStateChanged(manager != null ? manager.State : PeakOfEnergyManager.MissionState.Idle);
    }
}
