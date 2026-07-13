using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public class MeteoriteFragment : Meteorite, IPoolable
{
    [Header("Fragment Properties")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private bool fadeBeforeDestroy = true;

    private Renderer fragmentRenderer;
    private Material fragmentMaterial;
    private float spawnTime;
    private Color originalColor;
    private bool hasFaded;

    protected override void Awake()
    {
        base.Awake();
        SetHarmfulObjectType(HarmfulObjectType.MeteoriteFragment);
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        spawnTime = Time.time;
        hasFaded = false;

        fragmentRenderer = GetComponent<Renderer>();
        if (fragmentRenderer != null)
        {
            fragmentMaterial = fragmentRenderer.material;
            if (fragmentMaterial.HasProperty("_Color"))
                originalColor = fragmentMaterial.color;
        }
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
        hasFaded = false;
        fragmentRenderer = null;
        fragmentMaterial = null;
    }

    void Update()
    {
        if (fadeBeforeDestroy && fragmentMaterial != null && !hasFaded)
        {
            float age = Time.time - spawnTime;
            float fadeStartTime = lifetime - fadeDuration;

            if (age >= fadeStartTime)
            {
                float fadeProgress = (age - fadeStartTime) / fadeDuration;
                Color currentColor = originalColor;
                currentColor.a = Mathf.Lerp(1f, 0f, Mathf.Clamp01(fadeProgress));
                fragmentMaterial.color = currentColor;

                if (fadeProgress >= 1f)
                    hasFaded = true;
            }
        }

        if (Time.time - spawnTime >= lifetime)
        {
            if (onReturnToPool != null)
                onReturnToPool(this);
            else
                Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        SpaceshipPart part = collision.gameObject.GetComponent<SpaceshipPart>();
        if (part == null)
            part = collision.gameObject.GetComponentInParent<SpaceshipPart>();

        if (part != null)
        {
            // SpaceshipPart.OnCollisionEnter handles this via HarmfulObjectType check
        }
    }
}
