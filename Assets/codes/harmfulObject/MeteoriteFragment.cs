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
        SpaceshipPart part = collision.gameObject.GetComponent<SpaceshipPart>();
        if (part == null)
            part = collision.gameObject.GetComponentInParent<SpaceshipPart>();

        if (part != null)
        {
            // SpaceshipPart.OnCollisionEnter handles this via HarmfulObjectType check,
            // but fallback here in case the part does not have collision damage enabled.
            // Damage is already driven by MeteoriteFragment's HarmfulObjectType.
        }
    }
}
