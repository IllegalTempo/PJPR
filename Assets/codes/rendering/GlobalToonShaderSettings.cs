using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("PJPR/Rendering/Global Toon Shader Settings")]
public class GlobalToonShaderSettings : MonoBehaviour
{
    public Color baseColor = Color.white;
    public Color shadeColor = new Color(0.45f, 0.5f, 0.65f, 1f);
    public Color thirdColor = new Color(0.18f, 0.22f, 0.35f, 1f);

    [Range(0f, 2f)] public float normalStrength = 1f;
    [Range(0f, 1f)] public float occlusionStrength = 1f;
    [Range(0f, 1f)] public float shadowBoundary = 0.48f;
    [Range(0.001f, 0.5f)] public float shadowSoftness = 0.035f;
    [Range(0f, 1f)] public float thirdColorStrength = 0.65f;
    [Range(0.001f, 1f)] public float thirdColorSize = 0.45f;
    [Range(0f, 2f)] public float ambientStrength = 0.35f;
    [Range(0f, 2f)] public float additionalLightStrength = 1f;
    [Range(0f, 2f)] public float ambientOcclusionIntensity = 1f;

    private static readonly int BaseColorId = Shader.PropertyToID("_PJPRToonBaseColor");
    private static readonly int ShadeColorId = Shader.PropertyToID("_PJPRToonShadeColor");
    private static readonly int ThirdColorId = Shader.PropertyToID("_PJPRToonThirdColor");
    private static readonly int SettingsActiveId = Shader.PropertyToID("_PJPRToonSettingsActive");
    private static readonly int NormalStrengthId = Shader.PropertyToID("_PJPRToonNormalStrength");
    private static readonly int OcclusionStrengthId = Shader.PropertyToID("_PJPRToonOcclusionStrength");
    private static readonly int ShadowBoundaryId = Shader.PropertyToID("_PJPRToonShadowBoundary");
    private static readonly int ShadowSoftnessId = Shader.PropertyToID("_PJPRToonShadowSoftness");
    private static readonly int ThirdColorStrengthId = Shader.PropertyToID("_PJPRToonThirdColorStrength");
    private static readonly int ThirdColorSizeId = Shader.PropertyToID("_PJPRToonThirdColorSize");
    private static readonly int AmbientStrengthId = Shader.PropertyToID("_PJPRToonAmbientStrength");
    private static readonly int AdditionalLightStrengthId = Shader.PropertyToID("_PJPRToonAdditionalLightStrength");
    private static readonly int AmbientOcclusionIntensityId = Shader.PropertyToID("_PJPRToonAOIntensity");

    private void OnEnable()
    {
        Apply();
    }

    private void Update()
    {
        Apply();
    }

    private void OnDisable()
    {
        Shader.SetGlobalFloat(SettingsActiveId, 0f);
    }

    private void OnValidate()
    {
        Apply();
    }

    [ContextMenu("Apply Global Toon Settings")]
    public void Apply()
    {
        Shader.SetGlobalFloat(SettingsActiveId, 1f);
        Shader.SetGlobalColor(BaseColorId, baseColor);
        Shader.SetGlobalColor(ShadeColorId, shadeColor);
        Shader.SetGlobalColor(ThirdColorId, thirdColor);
        Shader.SetGlobalFloat(NormalStrengthId, Mathf.Max(0f, normalStrength));
        Shader.SetGlobalFloat(OcclusionStrengthId, Mathf.Clamp01(occlusionStrength));
        Shader.SetGlobalFloat(ShadowBoundaryId, Mathf.Clamp01(shadowBoundary));
        Shader.SetGlobalFloat(ShadowSoftnessId, Mathf.Max(0.001f, shadowSoftness));
        Shader.SetGlobalFloat(ThirdColorStrengthId, Mathf.Clamp01(thirdColorStrength));
        Shader.SetGlobalFloat(ThirdColorSizeId, Mathf.Max(0.001f, thirdColorSize));
        Shader.SetGlobalFloat(AmbientStrengthId, Mathf.Max(0f, ambientStrength));
        Shader.SetGlobalFloat(AdditionalLightStrengthId, Mathf.Max(0f, additionalLightStrength));
        Shader.SetGlobalFloat(AmbientOcclusionIntensityId, Mathf.Max(0f, ambientOcclusionIntensity));
    }
}
