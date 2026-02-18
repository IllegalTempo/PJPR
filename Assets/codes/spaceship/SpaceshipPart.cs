using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class SpaceshipPart : MonoBehaviour
{ 

    [Header("Identity")]
    [SerializeField] private string partName = "Hull";

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private bool destroyWhenBroken = false;
    [SerializeField] private bool permanentlyDestroyGameObjectOnBreak = false;

    [Header("Collision Damage")]
    [SerializeField] private bool enableCollisionDamage = true;
    [SerializeField] private float meteoriteDamage = 25f;
	[SerializeField] private float fragmentDamage = 8f;
	[SerializeField] private float projectileDamage = 15f;
	[SerializeField] private float defaultCollisionDamage = 5f;

	[Header("Events")]
	public UnityEvent<float, float> OnHealthChanged;
	public UnityEvent OnBroken;

    [Header("Repair")]
    [SerializeField] private bool enableRepair = true;
    [SerializeField] private bool requireRepairTool = true;
    [SerializeField, Range(0f, 1f)] private float repairThresholdNormalized = 0.25f;
    [SerializeField, Range(0f, 1f)] private float repairPercentMaxHealthPerSecond = 0.06f;
    [SerializeField] private float repairRayDistance = 100f;

    [Header("Repair Prompt")]
    [SerializeField] private bool showRepairPrompt = true;
    [SerializeField] private string repairPromptText = "{mouse}: Repair {part} ({hp}%)";
    [SerializeField] private string missingToolPromptText = "Need Hammer to Repair {part}";
    [SerializeField] private Vector2 repairPromptSize = new Vector2(380f, 28f);
    [SerializeField] private Vector2 repairPromptOffset = new Vector2(0f, 105f);

    [Header("Broken Visibility")]
    [SerializeField, Range(0f, 1f)] private float brokenHiddenOpacity = 0f;
    [SerializeField, Range(0f, 1f)] private float brokenLookedAtOpacity = 0.35f;

    public bool IsBroken => currentHealth <= 0f;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public string PartName => partName;

    private bool brokenStateApplied;
    private bool isLookedAtNow;
    private bool repairSessionActive;
    private GUIStyle promptStyle;

    private Renderer[] cachedRenderers = new Renderer[0];
    private ColorBinding[] colorBindings = new ColorBinding[0];

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private struct ColorBinding
    {
        public Renderer renderer;
        public int materialIndex;
        public int colorPropertyId;
        public Color baseColor;
    }


    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        CacheRendererBindings();
        EnsurePromptStyle();

        if (IsBroken)
        {
            HandleBroken();
        }
    }

    private void Update()
    {
        isLookedAtNow = IsLookedAtByLocalPlayer();
        Item heldItem = GetHeldItem();
        bool hasRequiredTool = HasRequiredRepairTool(heldItem);

        if (brokenStateApplied)
        {
            float targetOpacity = isLookedAtNow ? brokenLookedAtOpacity : brokenHiddenOpacity;
            ApplyBrokenOpacity(targetOpacity);
        }

        bool inputHeld = IsRepairInputHeld();
        if (!inputHeld || !isLookedAtNow || !hasRequiredTool || !CanRepairContinueByHealth())
        {
            repairSessionActive = false;
            return;
        }

        if (!repairSessionActive)
        {
            repairSessionActive = CanStartRepairByHealth();
        }

        if (repairSessionActive)
        {
            float amount = maxHealth * Mathf.Max(0f, repairPercentMaxHealthPerSecond) * Time.deltaTime;
            if (amount > 0f)
            {
                Repair(amount);
            }
        }
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

        if (!CanRepairContinueByHealth())
        {
            return;
        }

        bool wasBroken = IsBroken;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"{partName} repaired by {amount}. Current health: {currentHealth}/{maxHealth}");

        if (wasBroken && !IsBroken)
        {
            ExitBrokenState();
        }
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
        else
        {
            ExitBrokenState();
        }

    }

    private void HandleBroken()
    { 
        if (brokenStateApplied)
        {
            return;
        }

        brokenStateApplied = true;
        OnBroken?.Invoke();
        Debug.Log($"{partName} is broken!");

        if (destroyWhenBroken)
        {
            ApplyBrokenOpacity(brokenHiddenOpacity);

            if (permanentlyDestroyGameObjectOnBreak)
            {
                Destroy(gameObject);
            }
        }
    }

    private void ExitBrokenState()
    {
        if (!brokenStateApplied)
        {
            return;
        }

        brokenStateApplied = false;
        RestoreDefaultOpacity();
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
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        ExitBrokenState();
    }

    [ContextMenu("Debug/Test Meteorite Hit")]
    private void DebugTestMeteoriteHit()
    {
        OnDamage(meteoriteDamage, "Meteorite(Debug)");
    }

    private bool CanRepairNow()
    {
        if (!enableRepair || maxHealth <= 0f)
        {
            return false;
        }

        return CanRepairContinueByHealth() && CanStartRepairByHealth();
    }

    private bool CanRepairContinueByHealth()
    {
        if (!enableRepair || maxHealth <= 0f)
        {
            return false;
        }

        return currentHealth < maxHealth;
    }

    private bool CanStartRepairByHealth()
    {
        if (!CanRepairContinueByHealth())
        {
            return false;
        }

        if (IsBroken)
        {
            return true;
        }

        return (currentHealth / maxHealth) <= Mathf.Clamp01(repairThresholdNormalized);
    }

    private static bool IsRepairInputHeld()
    {
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
    }

    private bool IsLookedAtByLocalPlayer()
    {
        Transform cameraTransform = GetLocalCameraTransform();
        if (cameraTransform == null)
        {
            return false;
        }

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, repairRayDistance))
        {
            return false;
        }

        SpaceshipPart lookedPart = hit.collider.GetComponentInParent<SpaceshipPart>();
        return lookedPart == this;
    }

    public bool CanStartRepairWithHeldItem(Item heldItem)
    {
        return HasRequiredRepairTool(heldItem) && CanStartRepairByHealth();
    }

    private bool HasRequiredRepairTool(Item heldItem)
    {
        if (!requireRepairTool)
        {
            return true;
        }

        return heldItem != null && heldItem.IsRepairTool;
    }

    private Item GetHeldItem()
    {
        if (GameCore.instance == null || GameCore.instance.localPlayer == null)
        {
            return null;
        }

        return GameCore.instance.localPlayer.holdingItem;
    }

    private Transform GetLocalCameraTransform()
    {
        if (GameCore.instance != null && GameCore.instance.localPlayer != null && GameCore.instance.localPlayer.cam != null)
        {
            return GameCore.instance.localPlayer.cam.transform;
        }

        return Camera.main != null ? Camera.main.transform : null;
    }

    private void CacheRendererBindings()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);

        var bindings = new System.Collections.Generic.List<ColorBinding>();
        foreach (Renderer renderer in cachedRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                {
                    continue;
                }

                int propertyId = 0;
                if (material.HasProperty(BaseColorId))
                {
                    propertyId = BaseColorId;
                }
                else if (material.HasProperty(ColorId))
                {
                    propertyId = ColorId;
                }

                if (propertyId == 0)
                {
                    continue;
                }

                bindings.Add(new ColorBinding
                {
                    renderer = renderer,
                    materialIndex = i,
                    colorPropertyId = propertyId,
                    baseColor = material.GetColor(propertyId)
                });
            }
        }

        colorBindings = bindings.ToArray();
    }

    private void ApplyBrokenOpacity(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        for (int i = 0; i < colorBindings.Length; i++)
        {
            ColorBinding binding = colorBindings[i];
            if (binding.renderer == null)
            {
                continue;
            }

            var block = new MaterialPropertyBlock();
            binding.renderer.GetPropertyBlock(block, binding.materialIndex);
            Color color = binding.baseColor;
            color.a = alpha;
            block.SetColor(binding.colorPropertyId, color);
            binding.renderer.SetPropertyBlock(block, binding.materialIndex);
        }
    }

    private void RestoreDefaultOpacity()
    {
        for (int i = 0; i < colorBindings.Length; i++)
        {
            ColorBinding binding = colorBindings[i];
            if (binding.renderer == null)
            {
                continue;
            }

            var block = new MaterialPropertyBlock();
            binding.renderer.GetPropertyBlock(block, binding.materialIndex);
            block.SetColor(binding.colorPropertyId, binding.baseColor);
            binding.renderer.SetPropertyBlock(block, binding.materialIndex);
        }
    }

    private void EnsurePromptStyle()
    {
        if (promptStyle != null)
        {
            return;
        }

        promptStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14
        };
    }

    private void OnGUI()
    {
        if (!showRepairPrompt || !isLookedAtNow || !CanStartRepairByHealth())
        {
            return;
        }

        EnsurePromptStyle();

        Rect rect = new Rect(
            (Screen.width * 0.5f) - (repairPromptSize.x * 0.5f) + repairPromptOffset.x,
            (Screen.height * 0.5f) + repairPromptOffset.y,
            repairPromptSize.x,
            repairPromptSize.y
        );

        bool hasRequiredTool = HasRequiredRepairTool(GetHeldItem());
        GUI.Box(rect, BuildRepairPromptText(hasRequiredTool), promptStyle);
    }

    private string BuildRepairPromptText(bool hasRequiredTool)
    {
        int healthPercent = Mathf.RoundToInt((currentHealth / maxHealth) * 100f);
        string template = hasRequiredTool
            ? (string.IsNullOrEmpty(repairPromptText) ? "{mouse}: Repair {part} ({hp}%)" : repairPromptText)
            : (string.IsNullOrEmpty(missingToolPromptText) ? "Need Hammer to Repair {part}" : missingToolPromptText);

        string resolved = template;

        resolved = resolved.Replace("{mouse}", "LMB");
        resolved = resolved.Replace("{part}", partName);
        resolved = resolved.Replace("{hp}", healthPercent.ToString());

        return resolved;
    }
}