using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkPrefabIdentity))]
public class Meteorite : HarmfulObject, IPoolable
{
    [Header("Meteorite Type Reference")]
    [Tooltip("The ScriptableObject that defines this meteorite's stats. Assigned at spawn time.")]
    [SerializeField] private MeteoriteTypeDefinition typeDefinition;

    [Header("Meteorite Properties")]
    [SerializeField] private float health = 100f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private bool damageOnAnyCollision = false;
    [SerializeField] private float anyCollisionDamage = 999f;

    [Header("Breaking Settings")]
    [SerializeField] private GameObject fragmentPrefab;
    [SerializeField] private int fragmentCount = 5;
    [SerializeField] private float fragmentForce = 5f;
    [SerializeField] private float fragmentLifetime = 5f;
    [SerializeField] private GameObject breakEffect;
    [SerializeField] private bool detachFragmentsBeforeDestroy = true;

    [Header("Visual Effects")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private Material damagedMaterial;

    // Pooling
    [NonSerialized] public string poolKey;
    [NonSerialized] public Action<Meteorite> onReturnToPool;

    private Rigidbody rb;
    private Renderer meshRenderer;
    private Material originalMaterial;
    private Vector3 randomRotation;
    private bool isBreaking = false;
    private bool isInitialized = false;

    public MeteoriteTypeDefinition TypeDefinition => typeDefinition;
    public bool IsBreaking => isBreaking;
    public float CurrentHealth => health;

    protected virtual void Awake()
    {
        SetHarmfulObjectType(HarmfulObjectType.Meteorite);
        rb = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<Renderer>();
        if (meshRenderer != null)
            originalMaterial = meshRenderer.material;
    }

    void Start()
    {
        if (!isInitialized)
            InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        if (isInitialized) return;
        isInitialized = true;

        if (rb == null) rb = GetComponent<Rigidbody>();
        if (meshRenderer == null) meshRenderer = GetComponent<Renderer>();
        if (meshRenderer != null && originalMaterial == null)
            originalMaterial = meshRenderer.material;

        randomRotation = new Vector3(
            UnityEngine.Random.Range(-rotationSpeed, rotationSpeed),
            UnityEngine.Random.Range(-rotationSpeed, rotationSpeed),
            UnityEngine.Random.Range(-rotationSpeed, rotationSpeed)
        );
    }

    public virtual void OnSpawn()
    {
        isBreaking = false;
        isInitialized = false;

        if (typeDefinition != null)
        {
            maxHealth = typeDefinition.maxHealth;
            damage = typeDefinition.damage;
        }

        health = maxHealth;

        if (meshRenderer != null && originalMaterial != null)
            meshRenderer.material = originalMaterial;

        InitializeDefaults();
    }

    public virtual void OnDespawn()
    {
        isBreaking = false;
        isInitialized = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.EndsWith("_Fragments") || child.name.StartsWith("Fragment_"))
                Destroy(child.gameObject);
        }
    }

    void FixedUpdate()
    {
        if (rb != null && !isBreaking)
        {
            rb.AddTorque(randomRotation * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (isBreaking) return;

        health -= damageAmount;

        if (health < maxHealth * 0.5f && meshRenderer != null && damagedMaterial != null)
            meshRenderer.material = damagedMaterial;

        if (hitEffect != null)
            Instantiate(hitEffect, transform.position, Quaternion.identity);

        if (health <= 0)
            BreakMeteorite();
    }

    public void BreakMeteorite()
    {
        if (isBreaking) return;
        isBreaking = true;

        Transform fragmentRoot = null;
        if (fragmentPrefab != null)
        {
            GameObject rootObject = new GameObject($"{name}_Fragments");
            fragmentRoot = rootObject.transform;
            fragmentRoot.SetParent(transform);
            fragmentRoot.localPosition = Vector3.zero;
            fragmentRoot.localRotation = Quaternion.identity;
        }

        if (breakEffect != null)
        {
            Transform worldRef = GameCore.Instance != null ? GameCore.Instance.GetWorldReferenceTransform() : null;
            GameObject effect = Instantiate(breakEffect, transform.position, Quaternion.identity, worldRef);
            Destroy(effect, 3f);
        }

        if (fragmentPrefab != null)
        {
            for (int i = 0; i < fragmentCount; i++)
            {
                Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * 0.5f;
                GameObject fragment = Instantiate(fragmentPrefab, transform.position + randomOffset, UnityEngine.Random.rotation, fragmentRoot);

                Rigidbody fragmentRb = fragment.GetComponent<Rigidbody>();
                if (fragmentRb != null)
                {
                    Vector3 randomDirection = UnityEngine.Random.insideUnitSphere;
                    fragmentRb.AddForce(randomDirection * fragmentForce, ForceMode.Impulse);
                    fragmentRb.AddTorque(UnityEngine.Random.insideUnitSphere * fragmentForce, ForceMode.Impulse);
                }

                Destroy(fragment, fragmentLifetime);
            }

            if (fragmentRoot != null && detachFragmentsBeforeDestroy)
            {
                fragmentRoot.SetParent(null);
                Destroy(fragmentRoot.gameObject, fragmentLifetime + 0.5f);
            }
        }

        // Loot drop
        if (LootDropHandler.Instance != null && typeDefinition != null)
        {
            LootDropHandler.Instance.ProcessLootDrop(typeDefinition, transform.position);
        }

        if (onReturnToPool != null)
            onReturnToPool(this);
        else
            Destroy(gameObject);
    }

    public void ConfigureFromDefinition(MeteoriteTypeDefinition def)
    {
        typeDefinition = def;
        if (def != null)
        {
            maxHealth = def.maxHealth;
            damage = def.damage;
            health = def.maxHealth;
        }
    }

    [ContextMenu("Debug/Break Now")]
    private void DebugBreakNow()
    {
        BreakMeteorite();
    }

    [ContextMenu("Debug/Apply 1 Damage")]
    private void DebugApplyOneDamage()
    {
        TakeDamage(1f);
    }

    private void HandleCollisionBreak(GameObject other)
    {
        if (damageOnAnyCollision)
        {
            TakeDamage(anyCollisionDamage);
            return;
        }

        // Damage from projectiles — check for a SpaceshipPart on the other object
        // (projectiles carry this or a similar damage-dealing component).
        // SpaceshipPart handles meteorite collision damage itself via HarmfulObjectType.
        // If the other object has a Rigidbody and is moving fast, treat it as a damaging impact.
        Rigidbody otherRb = other.GetComponent<Rigidbody>();
        if (otherRb != null && otherRb.linearVelocity.magnitude > 5f)
        {
            TakeDamage(otherRb.linearVelocity.magnitude * 2f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleCollisionBreak(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCollisionBreak(other.gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}
