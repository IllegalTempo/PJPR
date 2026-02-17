using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Meteorite : HarmfulObject
{
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
    [SerializeField] private GameObject breakEffect;
    [SerializeField] private bool detachFragmentsBeforeDestroy = true;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private Material damagedMaterial;
    
    private Rigidbody rb;
    private Renderer meshRenderer;
    private Material originalMaterial;
    private Vector3 randomRotation;
    private bool isBreaking = false;

    protected virtual void Awake()
    {
        SetHarmfulObjectType(HarmfulObjectType.Meteorite);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<Renderer>();
        
        if (meshRenderer != null)
        {
            originalMaterial = meshRenderer.material;
        }

        // random roatation
        randomRotation = new Vector3(
            Random.Range(-rotationSpeed, rotationSpeed),
            Random.Range(-rotationSpeed, rotationSpeed),
            Random.Range(-rotationSpeed, rotationSpeed)
        );
    }

    void FixedUpdate()
    {
        // constant rotation
        if (rb != null && !isBreaking)
        {
            rb.AddTorque(randomRotation * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (isBreaking) return;

        health -= damageAmount;

        // visual feedback for damage
        if (health < maxHealth * 0.5f && meshRenderer != null && damagedMaterial != null)
        {
            meshRenderer.material = damagedMaterial;
        }

        // hit effect show
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        if (health <= 0)
        {
            BreakMeteorite();
        }
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

        // spawning break effect
        if (breakEffect != null)
        {
            GameObject effect = Instantiate(breakEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        // create fragments
        if (fragmentPrefab != null)
        {
            for (int i = 0; i < fragmentCount; i++)
            {
                Vector3 randomOffset = Random.insideUnitSphere * 0.5f;
                GameObject fragment = Instantiate(fragmentPrefab, transform.position + randomOffset, Random.rotation, fragmentRoot);
                
                // phy to fragments
                Rigidbody fragmentRb = fragment.GetComponent<Rigidbody>();
                if (fragmentRb != null)
                {
                    Vector3 randomDirection = Random.insideUnitSphere;
                    fragmentRb.AddForce(randomDirection * fragmentForce, ForceMode.Impulse);
                    fragmentRb.AddTorque(Random.insideUnitSphere * fragmentForce, ForceMode.Impulse);
                }

                // Auto-destroy fragments
                Destroy(fragment, 5f);
            }

            if (fragmentRoot != null && detachFragmentsBeforeDestroy)
            {
                fragmentRoot.SetParent(null);
                Destroy(fragmentRoot.gameObject, 5.5f);
            }
        }

        Destroy(gameObject);
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

    private void OnCollisionEnter(Collision collision)
    {
        if (damageOnAnyCollision)
        {
            TakeDamage(anyCollisionDamage);
            return;
        }

        // check-if hit spaceship idk
        if (collision.gameObject.CompareTag("Spaceship") || collision.gameObject.CompareTag("Player"))
        {
            // apply damgae
            Spaceship_movement spaceship = collision.gameObject.GetComponent<Spaceship_movement>();
            if (spaceship != null)
            {
                // You can add spaceship damage method here
                // spaceship.TakeDamage(damage);
            }

            // meteorite takes damage from impact
            TakeDamage(health * 0.3f);
        }
        // hit by weapon/projectile (bullet?)
        else if (collision.gameObject.CompareTag("Projectile"))
        {
            TakeDamage(30f);
            Destroy(collision.gameObject);
        }
    }
// idk
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}
