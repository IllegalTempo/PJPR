using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MeteoriteFragment : Meteorite
{
    [Header("Fragment Properties")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private bool fadeBeforeDestroy = true;
    
    private Renderer fragmentRenderer;
    private Material fragmentMaterial;
    private float spawnTime;
    private Color originalColor;

    protected override void Awake()
    {
        base.Awake();
        SetHarmfulObjectType(HarmfulObjectType.MeteoriteFragment);
    }

    void Start()
    {
        spawnTime = Time.time;
        fragmentRenderer = GetComponent<Renderer>();
        
        if (fragmentRenderer != null)
        {
            fragmentMaterial = fragmentRenderer.material;
            
            if (fragmentMaterial.HasProperty("_Color"))
            {
                originalColor = fragmentMaterial.color;
            }
        }

        // self-destruct after lifetime
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (fadeBeforeDestroy && fragmentMaterial != null)
        {
            float age = Time.time - spawnTime;
            float fadeStartTime = lifetime - fadeDuration;
            
            if (age >= fadeStartTime)
            {
                float fadeProgress = (age - fadeStartTime) / fadeDuration;
                Color currentColor = originalColor;
                currentColor.a = Mathf.Lerp(1f, 0f, fadeProgress);
                fragmentMaterial.color = currentColor;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Optional: Add small damage to spaceship if fragment hits
        if (collision.gameObject.CompareTag("Spaceship"))
        {
            // Fragments deal minimal damage
            // You can implement this when spaceship damage system is ready
        }
    }
}
