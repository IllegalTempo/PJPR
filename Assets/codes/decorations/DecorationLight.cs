using UnityEngine;

public class DecorationLight : Selectable
{
    [Header("Light Target")]
    [SerializeField] private Light targetLight;

    [Header("Prompt")]
    [SerializeField] private bool showPrompt = true;
    [SerializeField] private string promptText = "F: Turn Light On/Off";
    [SerializeField] private Vector2 promptSize = new Vector2(260f, 28f);
    [SerializeField] private Vector2 promptOffset = new Vector2(0f, 70f);

    private bool isLookedAtNow;
    private GUIStyle promptStyle;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (targetLight == null)
        {
            targetLight = GetComponentInChildren<Light>();
        }
    }

    protected override void Update()
    {
        base.Update();
        isLookedAtNow = outline != null && outline.enabled;
    }

    public override void OnClicked()
    {
        base.OnClicked();

        if (targetLight != null)
        {
            targetLight.enabled = !targetLight.enabled;
        }
    }

    public override bool IsFunctionKeyOnly()
    {
        return true;
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
        if (!showPrompt || !isLookedAtNow)
        {
            return;
        }

        EnsurePromptStyle();

        Rect rect = new Rect(
            (Screen.width * 0.5f) - (promptSize.x * 0.5f) + promptOffset.x,
            (Screen.height * 0.5f) + promptOffset.y,
            promptSize.x,
            promptSize.y
        );

        GUI.Box(rect, promptText, promptStyle);
    }
}