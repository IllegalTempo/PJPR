using UnityEngine;

public class SplashController : machine
{
    [SerializeField]
    private LiquidDefinition[] LiquidDefinitionArray;

    [SerializeField]
    private float splashCooldown = 0.1f;

    private float lastSplashTime = 0f;
    private LiquidDefinition currentLiquid;

    private AudioSource audioSource;

    [SerializeField]
    private Animator animator;

    private void Awake()
    {
        currentLiquid = LiquidDefinitionArray[0];
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void Splash()
    {
        if (Time.time - lastSplashTime < splashCooldown)
        {
            Debug.Log("Splash on cooldown");
            return;
        }

        //if (settings.SplashParticle == null)
        //{
        //    Debug.LogWarning($"No particle system assigned for liquid type: {liquidType}");
        //}

        PerformSplash(currentLiquid);
        lastSplashTime = Time.time;
    }

    private void PerformSplash(LiquidDefinition settings)
    {
        //if (settings.SplashParticle != null)
        //{
        //    settings.SplashParticle.Play();
        //}
        animator.Play("Splash");

        // Play sound
        if (settings.SplashSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(settings.SplashSound);
        }
        //ApplySplashPhysics(settings);

        //Debug.Log($"Splashing {settings.liquidType}!");
    }

    //private void ApplySplashPhysics(LiquidDefinition settings)
    //{
    //    Collider[] hitColliders = Physics.OverlapSphere(
    //        splashOrigin != null ? splashOrigin.position : transform.position,
    //        5f
    //    );

    //    foreach (var collider in hitColliders)
    //    {
    //        if (collider.TryGetComponent<Rigidbody>(out var rb) && rb != gameObject.GetComponent<Rigidbody>())
    //        {
    //            Vector3 forceDirection = (collider.transform.position - transform.position).normalized;
    //            rb.AddForce(forceDirection * settings.SplashForce, ForceMode.Impulse);
    //        }
    //    }
    //}

    public void SetCurrentLiquid(LiquidDefinition liquidType)
    {
        currentLiquid = liquidType;
    }

    public override void OnInteract(PlayerMain who)
    {
        base.OnInteract(who);
        Splash();
    }
}
