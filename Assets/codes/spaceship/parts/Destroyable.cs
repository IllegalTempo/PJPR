using UnityEngine;
using UnityEngine.Events;

public class Destroyable : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] protected float currentHealth = 100f;
    [SerializeField] private bool destroyWhenBroken = false;
    [SerializeField] private bool permanentlyDestroyGameObjectOnBreak = false;

    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged;
    public UnityEvent OnBroken;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsBroken => currentHealth <= 0f;

    private bool brokenStateApplied;

    protected virtual string DestroyableName => gameObject.name;

    protected virtual void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (IsBroken)
        {
            HandleBroken();
        }
    }

    public void OnDamage(float amount)
    {
        OnDamage(amount, "Unknown");
    }

    public void OnDamage(float amount, string source)
    {
        if (IsBroken || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamaged(amount, source);

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

        bool wasBroken = IsBroken;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnRepaired(amount);

        if (wasBroken && !IsBroken)
        {
            ExitBrokenState();
        }
    }

    public void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            HandleBroken();
        }
        else
        {
            ExitBrokenState();
        }
    }

    protected virtual void OnDamaged(float amount, string source)
    {
        Debug.Log($"{DestroyableName} took {amount} damage from {source}. Current health: {currentHealth}/{maxHealth}");
    }

    protected virtual void OnRepaired(float amount)
    {
        Debug.Log($"{DestroyableName} repaired by {amount}. Current health: {currentHealth}/{maxHealth}");
    }

    protected virtual void OnBrokenStateEntered()
    {
        Debug.Log($"{DestroyableName} is broken!");
    }

    protected virtual void OnBrokenStateExited()
    {
    }

    private void HandleBroken()
    {
        if (brokenStateApplied)
        {
            return;
        }

        brokenStateApplied = true;
        OnBroken?.Invoke();
        OnBrokenStateEntered();

        if (destroyWhenBroken && permanentlyDestroyGameObjectOnBreak)
        {
            Destroy(gameObject);
        }
    }

    private void ExitBrokenState()
    {
        if (!brokenStateApplied)
        {
            return;
        }

        brokenStateApplied = false;
        OnBrokenStateExited();
    }
}
