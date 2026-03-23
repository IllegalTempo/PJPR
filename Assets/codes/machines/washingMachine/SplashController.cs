using UnityEngine;

public class SplashController : machine
{
    [System.Serializable]
    public class LiquidSettings
    {
        public LiquidType liquidType;
        public Color liquidColor = Color.white;
        public float splashForce = 5f;
        public float splashDuration = 1f;
        public ParticleSystem splashParticles; // ill try particle for now
        public AudioClip splashSound; 
    }

    [SerializeField]
    private LiquidSettings[] liquidSettingsArray;

    [SerializeField]
    private Transform splashOrigin; // Position from which splash starts

    [SerializeField]
    private float splashCooldown = 0.1f;

    private float lastSplashTime = 0f;
    private LiquidType currentLiquid = LiquidType.Water;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void Splash(LiquidType liquidType)
    {
        Debug.Log($"Splash called with liquid type: {liquidType}");
        if (Time.time - lastSplashTime < splashCooldown)
        {
            Debug.Log("Splash on cooldown");
            return;
        }

        currentLiquid = liquidType;
        LiquidSettings settings = GetLiquidSettings(liquidType);

        if (settings == null)
        {
            Debug.LogWarning($"No settings found for liquid type: {liquidType}");
            return;
        }

        if (settings.splashParticles == null)
        {
            Debug.LogWarning($"No particle system assigned for liquid type: {liquidType}");
        }

        PerformSplash(settings);
        lastSplashTime = Time.time;
    }

    private LiquidSettings GetLiquidSettings(LiquidType liquidType)
    {
        foreach (var settings in liquidSettingsArray)
        {
            if (settings.liquidType == liquidType)
                return settings;
        }
        return null;
    }

    private void PerformSplash(LiquidSettings settings)
    {
        if (settings.splashParticles != null)
        {
            settings.splashParticles.Play();
        }

        // Play sound
        if (settings.splashSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(settings.splashSound);
        }
        ApplySplashPhysics(settings);

        Debug.Log($"Splashing {settings.liquidType}!");
    }

    private void ApplySplashPhysics(LiquidSettings settings)
    {
        Collider[] hitColliders = Physics.OverlapSphere(
            splashOrigin != null ? splashOrigin.position : transform.position,
            5f
        );

        foreach (var collider in hitColliders)
        {
            if (collider.TryGetComponent<Rigidbody>(out var rb) && rb != gameObject.GetComponent<Rigidbody>())
            {
                Vector3 forceDirection = (collider.transform.position - transform.position).normalized;
                rb.AddForce(forceDirection * settings.splashForce, ForceMode.Impulse);
            }
        }
    }

    public void SetCurrentLiquid(LiquidType liquidType)
    {
        currentLiquid = liquidType;
    }

    public LiquidType GetCurrentLiquid()
    {
        return currentLiquid;
    }

    public override void OnInteract(PlayerMain who)
    {
        base.OnInteract(who);
        Splash(currentLiquid);
    }
}
