using UnityEngine;
using UnityEngine.Events;

public class SpaceshipPart : MonoBehaviour
{ 

    [Header("Identity")]
    [SerializeField] private string partName = "Hull";

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private bool destroyWhenBroken = false;

    [Header("Collision Damage")]
    [SerializeField] private bool enableCollisionDamage = true;
    [SerializeField] private float meteoriteDamage = 25f;
	[SerializeField] private float fragmentDamage = 8f;
	[SerializeField] private float projectileDamage = 15f;
	[SerializeField] private float defaultCollisionDamage = 5f;

	[Header("Events")]
	public UnityEvent<float, float> OnHealthChanged;
	public UnityEvent OnBroken;

    public bool IsBroken => currentHealth <= 0f;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public string PartName => partName;


    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    public void OnDamage(float amount)
    {
        OnDamage(amount, "Unknown");
    }

    public void OnDamage(float amount, string source)
    {
        if (IsBroken)
        {
            return;
        }
        if (amount <= 0f)
        {
            return;
        }
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"{partName} took {amount} damage from {source}. Current health: {currentHealth}/{maxHealth}");
        if (currentHealth <= 0f)
        {
            HandleBroken();
        }
    }

    public void Repair(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"{partName} repaired by {amount}. Current health: {currentHealth}/{maxHealth}");
    }

    public void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        // Debug.Log($"{partName} health set to {currentHealth}/{maxHealth}");
        if (currentHealth <= 0f)
        {
            HandleBroken();
        }

    }

    private void HandleBroken()
    { 
        OnBroken?.Invoke();
        Debug.Log($"{partName} is broken!");
        if (destroyWhenBroken)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enableCollisionDamage || IsBroken)
        {
            return;
        }
        float damage = GetDamageFromCollision(collision.gameObject);
        if (damage > 0f)
        {
            OnDamage(damage, collision.gameObject.tag);
        }
    }

    private float GetDamageFromCollision(GameObject other)
    {
        HarmfulObject harmfulObject = other.GetComponent<HarmfulObject>();
        if (harmfulObject != null)
        {
            if (harmfulObject.Type == HarmfulObjectType.Meteorite)
            {
                return meteoriteDamage;
            }

            if (harmfulObject.Type == HarmfulObjectType.MeteoriteFragment)
            {
                return fragmentDamage;
            }

            return harmfulObject.DamageToShipPart;
        }

        if (other.CompareTag("Projectile"))
        {
            return projectileDamage;
        }
        
        return defaultCollisionDamage;
    }

    [ContextMenu("Debug/Apply 10 dmg")]
    private void DebugApplyDamage()
    {
        OnDamage(10f, "Debug");
    }

    [ContextMenu("Debug/Full Repair")]
    private void DebugFullRepair()
    {
        Repair(maxHealth);
    }

    [ContextMenu("Debug/Test Meteorite Hit")]
    private void DebugTestMeteoriteHit()
    {
        OnDamage(meteoriteDamage, "Meteorite(Debug)");
    }
}