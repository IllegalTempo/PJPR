using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Assets.codes.Network.Messages;

/// <summary>
/// Peak Of Energy mission brain.
///
/// SERVER-AUTHORITATIVE: all simulation (RPM, stability, charging, cooldown, health,
/// state machine) runs only on the world manager (host or offline). Clients receive a
/// mirrored state via <see cref="NMS_Server_PeakOfEnergyState"/> and fire the same
/// UnityEvents, so visuals/UI behave identically everywhere.
///
/// The physical pulling mechanic is intentionally NOT here. Hook the future spaceship
/// thrust into <see cref="AddRPM"/> / <see cref="ApplyBrake"/> / <see cref="UpdateRPM"/>.
/// </summary>
public class PeakOfEnergyManager : MonoBehaviour
{
    public enum MissionState
    {
        Idle = 0,       // Mission start / waiting
        Spinning = 1,   // Players actively pulling to reach the target RPM
        Stabilizing = 2,// RPM in range + stable; charge channel is running
        Cooldown = 3,   // Charge succeeded; waiting for cooldown (clear enemies)
        GameOver = 4,   // Ring destroyed
        Victory = 5,    // 5 charges completed
    }

    public static PeakOfEnergyManager Instance { get; private set; }

    [Header("RPM Targets (one entry per charge tier, 5 total)")]
    [Tooltip("Minimum RPM required for each charge tier (index 0 = charge 1 ... index 4 = charge 5).")]
    [SerializeField] private float[] targetRPM_Min = { 100f, 140f, 180f, 220f, 260f };

    [Tooltip("Maximum RPM allowed for each charge tier. Must be > the matching min.")]
    [SerializeField] private float[] targetRPM_Max = { 140f, 180f, 220f, 260f, 300f };

    [Header("Charging")]
    [Tooltip("Seconds the player must hold the center button to complete a charge.")]
    [SerializeField] private float chargeHoldDuration = 2.5f;

    [Tooltip("Max allowable RPM fluctuation (standard deviation over the stability window) to count as Stable. e.g. 5 = ±5 RPM.")]
    [SerializeField] private float rpmFluctuationTolerance = 5f;

    [Tooltip("How long the stability window is (seconds). RPM variance is measured over this window.")]
    [SerializeField] private float stabilityWindowSeconds = 0.5f;

    [Tooltip("Seconds the team must wait between charges (enemies keep spawning so they can clear the board).")]
    [SerializeField] private float cooldownDuration = 15f;

    [Tooltip("RPM lost per second while in the Spinning state, so pulling matters. 0 = no decay.")]
    [SerializeField] private float rpmDecayPerSecond = 2f;

    [Header("Ring Health")]
    [SerializeField] private int ringMaxHealth = 100;

    [Tooltip("Flat damage a meteorite deals to the ring on impact.")]
    [SerializeField] private int meteoriteDamage = 10;

    [Header("Network Sync")]
    [Tooltip("How many authoritative state snapshots are sent to clients per second.")]
    [SerializeField] private float stateSyncRate = 10f;

    [Header("Test Input (until the real pulling mechanic exists)")]
    [Tooltip("Hold this key to spin the ring up. This is a test-only shortcut so you can try the mission without the spaceship pulling mechanic.")]
    [SerializeField] private bool testSpinKeyEnabled = true;
    [Tooltip("The key to hold. Defaults to Left Shift.")]
    [SerializeField] private KeyCode testSpinKey = KeyCode.LeftShift;
    [Tooltip("RPM gained per second while the test key is held.")]
    [SerializeField] private float testSpinRatePerSecond = 30f;

    [Header("Events")]
    public UnityEvent<float> OnRPMChanged;            // (currentRPM)
    public UnityEvent<bool> OnStabilityChanged;       // (isStable)
    public UnityEvent OnChargeStarted;
    public UnityEvent<int> OnChargeCompleted;         // (new charge index 0-4)
    public UnityEvent OnChargeFailed;
    public UnityEvent<float> OnChargeProgress;        // (0..1 while holding)
    public UnityEvent<int, int> OnHealthChanged;      // (currentHealth, maxHealth)
    public UnityEvent<float> OnCooldownTick;          // (cooldown remaining)
    public UnityEvent<MissionState> OnStateChanged;   // (state)
    public UnityEvent OnGameOver;
    public UnityEvent OnVictory;

    // ---- Public mission state ----
    public float CurrentRPM { get; private set; }
    public bool IsStable { get; private set; }
    public MissionState State { get; private set; } = MissionState.Idle;
    public int RingCurrentHealth { get; private set; }
    public int CurrentChargeIndex { get; private set; }
    public float CooldownRemaining { get; private set; }
    public bool IsCharging { get; private set; }
    public float ChargeProgress { get; private set; }
    public float TotalWobble { get; private set; }

    public float[] TargetRPM_Min => targetRPM_Min;
    public float[] TargetRPM_Max => targetRPM_Max;
    public float ChargeHoldDuration => chargeHoldDuration;
    public float RpmFluctuationTolerance => rpmFluctuationTolerance;
    public int RingMaxHealth => ringMaxHealth;
    public float CooldownDuration => cooldownDuration;
    public int MeteoriteDamage => meteoriteDamage;

    /// <summary>True when this machine must NOT run the simulation (online client mirror).</summary>
    private bool IsClientMirror =>
        NetworkSystem.Instance != null && NetworkSystem.Instance.IsOnline && !NetworkSystem.Instance.IsServer;

    private struct WobbleEntry
    {
        public float intensity;
        public float frequency;
        public float phase;
    }

    private readonly List<WobbleEntry> wobbleEntries = new List<WobbleEntry>();
    private int nextWobbleId = 1;
    private readonly Dictionary<int, WobbleEntry> wobbleById = new Dictionary<int, WobbleEntry>();

    // Rolling RPM samples for the stability window: (sampleTime, sampledRpm)
    private readonly List<KeyValuePair<float, float>> rpmSamples = new List<KeyValuePair<float, float>>();

    private float chargeStartedAt;
    private float nextStateSyncTime;
    private bool chargeEventPending; // force an immediate state broadcast after an event

    // Authoritative target range received from the server (used on client mirrors when
    // the host's config differs from the local arrays).
    private float syncedTargetMin = -1f;
    private float syncedTargetMax = -1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (targetRPM_Min == null || targetRPM_Min.Length == 0) targetRPM_Min = new float[5];
        if (targetRPM_Max == null || targetRPM_Max.Length == 0) targetRPM_Max = new float[5];
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // =====================================================================
    // Mission lifecycle (called by PeakOfEnergyMission)
    // =====================================================================

    public void StartMission()
    {
        CurrentRPM = 0f;
        RingCurrentHealth = ringMaxHealth;
        CurrentChargeIndex = 0;
        CooldownRemaining = 0f;
        IsCharging = false;
        ChargeProgress = 0f;
        rpmSamples.Clear();
        wobbleEntries.Clear();
        wobbleById.Clear();
        TotalWobble = 0f;
        chargeEventPending = false;

        SetState(MissionState.Spinning);
        OnHealthChanged?.Invoke(RingCurrentHealth, ringMaxHealth);
        OnRPMChanged?.Invoke(CurrentRPM);
        OnCooldownTick?.Invoke(CooldownRemaining);
        BroadcastState();
        Debug.Log("[PeakOfEnergyManager] Mission started. Charge 1 of 5. Target RPM: " + GetTargetMin() + " - " + GetTargetMax());
    }

    public void ResetToIdle()
    {
        IsCharging = false;
        ChargeProgress = 0f;
        SetState(MissionState.Idle);
        BroadcastState();
    }

    // =====================================================================
    // RPM hooks (public API for the future pulling / braking mechanic)
    // =====================================================================

    /// <summary>Integrate a delta into the ring RPM. Positive = spin up, negative = spin down.</summary>
    public void UpdateRPM(float delta)
    {
        if (IsClientMirror) return;
        if (State != MissionState.Spinning && State != MissionState.Stabilizing) return;

        CurrentRPM = Mathf.Max(0f, CurrentRPM + delta);
        OnRPMChanged?.Invoke(CurrentRPM);
    }

    /// <summary>Increase RPM. Hook the spaceship thrust/pull mechanic here.</summary>
    public void AddRPM(float amount)
    {
        UpdateRPM(amount);
    }

    /// <summary>Decrease RPM. Hook braking/reverse-pull here.</summary>
    public void ApplyBrake(float amount)
    {
        UpdateRPM(-amount);
    }

    /// <summary>
    /// Adds a wobble source (called by RustEater while latched).
    /// Wobble does NOT change the average RPM; it only inflates the fluctuation
    /// measured by the stability check, making isStable flip to false.
    /// </summary>
    /// <returns>An id token that must be passed back to <see cref="RemoveWobble"/> when unlatched.</returns>
    public int ApplyWobble(float intensity)
    {
        if (IsClientMirror) return -1;
        if (intensity <= 0f) return -1;

        WobbleEntry entry = new WobbleEntry
        {
            intensity = intensity,
            frequency = Random.Range(0.8f, 2.5f),
            phase = Random.Range(0f, Mathf.PI * 2f),
        };
        int id = nextWobbleId++;
        wobbleEntries.Add(entry);
        wobbleById[id] = entry;
        RecomputeTotalWobble();
        return id;
    }

    public void RemoveWobble(int id)
    {
        if (IsClientMirror || id < 0) return;
        if (wobbleById.Remove(id, out WobbleEntry entry))
        {
            wobbleEntries.Remove(entry);
            RecomputeTotalWobble();
        }
    }

    private void RecomputeTotalWobble()
    {
        float total = 0f;
        foreach (WobbleEntry entry in wobbleEntries)
            total += entry.intensity;
        TotalWobble = total;
    }

    /// <summary>Instantaneous asymmetric drag/wobble noise. Zero-mean so average RPM is untouched.</summary>
    private float SampleWobbleNoise(float time)
    {
        float noise = 0f;
        foreach (WobbleEntry entry in wobbleEntries)
        {
            float wave = Mathf.Sin(time * entry.frequency * Mathf.PI * 2f + entry.phase);
            float jitter = Random.Range(-0.3f, 0.3f);
            noise += entry.intensity * (0.7f * wave + 0.3f * jitter);
        }
        return noise;
    }

    // =====================================================================
    // Damage
    // =====================================================================

    /// <summary>Called by mission meteorites when they hit the ring. Server-authoritative.</summary>
    public void TakeDamage(int amount)
    {
        if (IsClientMirror) return;
        if (State == MissionState.Idle || State == MissionState.GameOver || State == MissionState.Victory) return;
        if (amount <= 0) return;

        RingCurrentHealth = Mathf.Max(0, RingCurrentHealth - amount);
        OnHealthChanged?.Invoke(RingCurrentHealth, ringMaxHealth);
        Debug.Log($"[PeakOfEnergyManager] Ring took {amount} damage. Health {RingCurrentHealth}/{ringMaxHealth}");

        if (RingCurrentHealth <= 0)
        {
            IsCharging = false;
            ChargeProgress = 0f;
            SetState(MissionState.GameOver);
            OnGameOver?.Invoke();
        }

        BroadcastState();
    }

    // =====================================================================
    // Charging
    // =====================================================================

    /// <summary>Player pressed the center button. Validates RPM range + stability, then starts the channel.</summary>
    public void RequestCharge()
    {
        if (IsClientMirror) return;
        if (IsCharging) return;
        if (State != MissionState.Spinning) return;
        if (!IsRpmInTargetRange() || !IsStable) return;

        IsCharging = true;
        ChargeProgress = 0f;
        chargeStartedAt = Time.time;
        SetState(MissionState.Stabilizing);
        OnChargeStarted?.Invoke();
        BroadcastChargeEvent(NMS_Server_PeakOfEnergyChargeEvent.ChargeEventType.Started, CurrentChargeIndex);
        BroadcastState();
        Debug.Log($"[PeakOfEnergyManager] Charge started. Hold for {chargeHoldDuration}s. RPM must stay in {GetTargetMin():F0}-{GetTargetMax():F0} and stable.");
    }

    /// <summary>Player released the button before finishing. Cancels quietly and resets progress.</summary>
    public void ReleaseCharge()
    {
        if (IsClientMirror) return;
        if (!IsCharging) return;

        CancelCharge(false);
    }

    private void CancelCharge(bool failed)
    {
        if (!IsCharging) return;

        IsCharging = false;
        ChargeProgress = 0f;
        SetState(MissionState.Spinning);
        OnChargeProgress?.Invoke(0f);

        if (failed)
        {
            OnChargeFailed?.Invoke();
            BroadcastChargeEvent(NMS_Server_PeakOfEnergyChargeEvent.ChargeEventType.Failed, CurrentChargeIndex);
            Debug.Log("[PeakOfEnergyManager] Charge FAILED - RPM left the target range or became unstable.");
        }

        BroadcastState();
    }

    private void CompleteCharge()
    {
        IsCharging = false;
        ChargeProgress = 0f;
        CurrentChargeIndex++;
        OnChargeCompleted?.Invoke(CurrentChargeIndex);
        BroadcastChargeEvent(NMS_Server_PeakOfEnergyChargeEvent.ChargeEventType.Completed, CurrentChargeIndex);
        Debug.Log($"[PeakOfEnergyManager] Charge {CurrentChargeIndex}/5 completed!");

        if (CurrentChargeIndex >= targetRPM_Min.Length)
        {
            SetState(MissionState.Victory);
            OnVictory?.Invoke();
            BroadcastState();
            return;
        }

        CooldownRemaining = cooldownDuration;
        OnCooldownTick?.Invoke(CooldownRemaining);
        SetState(MissionState.Cooldown);
        BroadcastState();
        Debug.Log($"[PeakOfEnergyManager] Cooldown started ({cooldownDuration}s). Next target: {GetTargetMin():F0}-{GetTargetMax():F0} RPM.");
    }

    // =====================================================================
    // Simulation (server / offline only)
    // =====================================================================

    private void Update()
    {
        if (IsClientMirror)
        {
            UpdateClientMirrorProgress();
            return;
        }

        // Test-only spin key: lets you raise RPM from anywhere (even while Idle) so the
        // ring responds immediately when you press Play, before the mission officially starts.
        // Uses the NEW Input System (this project uses Input System package; legacy
        // UnityEngine.Input is disabled and would silently do nothing).
        if (testSpinKeyEnabled && testSpinKey != KeyCode.None && IsTestKeyHeld())
        {
            CurrentRPM = Mathf.Max(0f, CurrentRPM + testSpinRatePerSecond * Time.deltaTime);
            OnRPMChanged?.Invoke(CurrentRPM);
        }

        if (State == MissionState.Idle || State == MissionState.GameOver || State == MissionState.Victory)
            return;

        float now = Time.time;

        // Natural decay while spinning (so players must keep pulling)
        if (State == MissionState.Spinning && rpmDecayPerSecond > 0f)
        {
            CurrentRPM = Mathf.Max(0f, CurrentRPM - rpmDecayPerSecond * Time.deltaTime);
            OnRPMChanged?.Invoke(CurrentRPM);
        }

        SampleStability(now);
        TickCooldown(now);
        TickCharge(now);
        BroadcastStateIfDue(now);
    }

    /// <summary>True while the configured test key is held (new Input System keyboard API).</summary>
    private bool IsTestKeyHeld()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        Key key = ToNewKey(testSpinKey);
        return key != Key.None && keyboard[key].isPressed;
    }

    /// <summary>Maps a legacy UnityEngine.KeyCode to a UnityEngine.InputSystem.Key.</summary>
    private static Key ToNewKey(KeyCode code)
    {
        switch (code)
        {
            case KeyCode.LeftShift: return Key.LeftShift;
            case KeyCode.RightShift: return Key.RightShift;
            case KeyCode.LeftControl: return Key.LeftCtrl;
            case KeyCode.RightControl: return Key.RightCtrl;
            case KeyCode.LeftAlt: return Key.LeftAlt;
            case KeyCode.RightAlt: return Key.RightAlt;
            case KeyCode.Space: return Key.Space;
            case KeyCode.W: return Key.W;
            case KeyCode.A: return Key.A;
            case KeyCode.S: return Key.S;
            case KeyCode.D: return Key.D;
            case KeyCode.E: return Key.E;
            case KeyCode.F: return Key.F;
            case KeyCode.R: return Key.R;
            default: return Key.None;
        }
    }

    private void TickCooldown(float now)
    {
        if (State != MissionState.Cooldown) return;

        CooldownRemaining = Mathf.Max(0f, CooldownRemaining - Time.deltaTime);
        OnCooldownTick?.Invoke(CooldownRemaining);

        if (CooldownRemaining <= 0f)
        {
            if (CurrentChargeIndex >= targetRPM_Min.Length)
            {
                SetState(MissionState.Victory);
                OnVictory?.Invoke();
            }
            else
            {
                SetState(MissionState.Spinning);
                Debug.Log($"[PeakOfEnergyManager] Cooldown over. Charge {CurrentChargeIndex + 1}/5 - target {GetTargetMin():F0}-{GetTargetMax():F0} RPM.");
            }
            BroadcastState();
        }
    }

    private void TickCharge(float now)
    {
        if (State != MissionState.Stabilizing || !IsCharging) return;

        // Continuous hold validation: RPM must stay inside the range AND stable.
        if (!IsRpmInTargetRange() || !IsStable)
        {
            CancelCharge(true);
            return;
        }

        ChargeProgress = Mathf.Clamp01((now - chargeStartedAt) / chargeHoldDuration);
        OnChargeProgress?.Invoke(ChargeProgress);

        if (ChargeProgress >= 1f)
        {
            CompleteCharge();
        }
    }

    /// <summary>Push one RPM sample into the rolling window and recompute isStable.</summary>
    private void SampleStability(float now)
    {
        float wobbleNoise = SampleWobbleNoise(now);
        rpmSamples.Add(new KeyValuePair<float, float>(now, CurrentRPM + wobbleNoise));

        float minAge = now - stabilityWindowSeconds;
        rpmSamples.RemoveAll(sample => sample.Key < minAge);

        bool stable = rpmSamples.Count < 5; // not enough data yet -> treat as stable
        if (rpmSamples.Count >= 5)
        {
            float mean = 0f;
            foreach (KeyValuePair<float, float> sample in rpmSamples)
                mean += sample.Value;
            mean /= rpmSamples.Count;

            float variance = 0f;
            foreach (KeyValuePair<float, float> sample in rpmSamples)
                variance += (sample.Value - mean) * (sample.Value - mean);
            variance /= rpmSamples.Count;

            stable = Mathf.Sqrt(variance) <= rpmFluctuationTolerance;
        }

        if (stable != IsStable)
        {
            IsStable = stable;
            OnStabilityChanged?.Invoke(IsStable);
        }
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    public bool IsRpmInTargetRange()
    {
        return CurrentRPM >= GetTargetMin() && CurrentRPM <= GetTargetMax();
    }

    public float GetTargetMin()
    {
        if (IsClientMirror && syncedTargetMin >= 0f)
            return syncedTargetMin;
        int index = Mathf.Clamp(CurrentChargeIndex, 0, Mathf.Max(0, targetRPM_Min.Length - 1));
        return targetRPM_Min[index];
    }

    public float GetTargetMax()
    {
        if (IsClientMirror && syncedTargetMax >= 0f)
            return syncedTargetMax;
        int index = Mathf.Clamp(CurrentChargeIndex, 0, Mathf.Max(0, targetRPM_Max.Length - 1));
        return targetRPM_Max[index];
    }

    private void SetState(MissionState newState)
    {
        if (State == newState) return;
        State = newState;
        OnStateChanged?.Invoke(State);
    }

    // =====================================================================
    // Network (server -> clients)
    // =====================================================================

    private void BroadcastStateIfDue(float now)
    {
        if (now < nextStateSyncTime && !chargeEventPending) return;
        if (NetworkSystem.Instance == null || NetworkRouter.Instance == null) return;
        if (!NetworkSystem.Instance.IsOnline || !NetworkSystem.Instance.IsServer) return;

        nextStateSyncTime = now + 1f / Mathf.Max(0.1f, stateSyncRate);
        chargeEventPending = false;
        BroadcastState();
    }

    private void BroadcastState()
    {
        if (NetworkSystem.Instance == null || NetworkRouter.Instance == null) return;
        if (!NetworkSystem.Instance.IsOnline || !NetworkSystem.Instance.IsServer) return;

        NetworkRouter.Instance.DistributeMessageToReady(
            new NMS_Server_PeakOfEnergyState(
                CurrentRPM, IsStable, (int)State, RingCurrentHealth, CurrentChargeIndex,
                CooldownRemaining, IsCharging, ChargeProgress, TotalWobble,
                GetTargetMin(), GetTargetMax()),
            sendType: NetworkSendProfiles.State);
    }

    private void BroadcastChargeEvent(NMS_Server_PeakOfEnergyChargeEvent.ChargeEventType eventType, int chargeIndex)
    {
        chargeEventPending = true;
        if (NetworkSystem.Instance == null || NetworkRouter.Instance == null) return;
        if (!NetworkSystem.Instance.IsOnline || !NetworkSystem.Instance.IsServer) return;

        NetworkRouter.Instance.DistributeMessageToReady(
            new NMS_Server_PeakOfEnergyChargeEvent((int)eventType, chargeIndex),
            sendType: NetworkSendProfiles.Critical);
    }

    // =====================================================================
    // Client mirror
    // =====================================================================

    public void ApplyNetworkState(
        float rpm, bool isStable, MissionState state, int health, int chargeIndex,
        float cooldownRemaining, bool isCharging, float chargeProgress,
        float wobble, float targetMin, float targetMax)
    {
        bool stateChanged = state != State;
        bool stabilityChanged = isStable != IsStable;
        bool healthChanged = health != RingCurrentHealth;

        CurrentRPM = rpm;
        IsStable = isStable;
        State = state;
        RingCurrentHealth = health;
        CurrentChargeIndex = chargeIndex;
        CooldownRemaining = cooldownRemaining;
        IsCharging = isCharging;
        ChargeProgress = chargeProgress;
        TotalWobble = wobble;
        syncedTargetMin = targetMin;
        syncedTargetMax = targetMax;

        OnRPMChanged?.Invoke(CurrentRPM);
        if (stabilityChanged) OnStabilityChanged?.Invoke(IsStable);
        if (stateChanged) OnStateChanged?.Invoke(State);
        if (healthChanged) OnHealthChanged?.Invoke(RingCurrentHealth, ringMaxHealth);
        OnCooldownTick?.Invoke(CooldownRemaining);
    }

    /// <summary>Smooth the progress bar locally between authoritative ticks while charging.</summary>
    private void UpdateClientMirrorProgress()
    {
        if (!IsCharging || State != MissionState.Stabilizing) return;

        float next = ChargeProgress + Time.deltaTime / Mathf.Max(0.01f, chargeHoldDuration);
        if (next > 1f) next = 1f;
        ChargeProgress = next;
        OnChargeProgress?.Invoke(ChargeProgress);
    }

    public void HandleChargeEvent(NMS_Server_PeakOfEnergyChargeEvent.ChargeEventType eventType, int chargeIndex)
    {
        switch (eventType)
        {
            case NMS_Server_PeakOfEnergyChargeEvent.ChargeEventType.Started:
                IsCharging = true;
                ChargeProgress = 0f;
                OnChargeStarted?.Invoke();
                break;
            case NMS_Server_PeakOfEnergyChargeEvent.ChargeEventType.Completed:
                IsCharging = false;
                ChargeProgress = 0f;
                OnChargeCompleted?.Invoke(chargeIndex);
                break;
            case NMS_Server_PeakOfEnergyChargeEvent.ChargeEventType.Failed:
                IsCharging = false;
                ChargeProgress = 0f;
                OnChargeFailed?.Invoke();
                break;
        }
    }
}
