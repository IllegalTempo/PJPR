using UnityEngine;
using UnityEngine.UI;
using UIEffectOutline = UnityEngine.UI.Outline;

public class UIOutline : SelectionOutline
{
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private UIEffectOutline outline;
    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField] private float outlineWidth = 5f;

    public override float OutlineWidth
    {
        get => outlineWidth;
        set
        {
            outlineWidth = value;
            Apply();
        }
    }

    public override bool OutlineVisible
    {
        get => outline != null && outline.enabled;
        set
        {
            CacheOutline();
            if (outline != null)
                outline.enabled = value;
        }
    }

    private void Awake()
    {
        CacheOutline();
        Apply();
    }

    private void OnValidate()
    {
        CacheOutline();
        Apply();
    }

    private void CacheOutline()
    {
        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();

        if (targetGraphic == null)
            targetGraphic = GetPreferredChildGraphic();

        if (outline != null && outline.GetComponent<Graphic>() == null)
            outline = null;

        if (outline == null && targetGraphic != null)
            outline = targetGraphic.GetComponent<UIEffectOutline>();

        if (outline == null && targetGraphic != null && Application.isPlaying)
            outline = targetGraphic.gameObject.AddComponent<UIEffectOutline>();
    }

    private void Apply()
    {
        CacheOutline();
        if (outline == null)
            return;

        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(outlineWidth, -outlineWidth);
    }

    private Graphic GetPreferredChildGraphic()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i].name.Contains("Background"))
                return graphics[i];
        }

        return graphics.Length > 0 ? graphics[0] : null;
    }
}
