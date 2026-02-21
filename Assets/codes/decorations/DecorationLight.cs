using Assets.codes.items;
using UnityEngine;

public class DecorationLight : Decoration,IUsable
{
    [Header("Light Target")]
    [SerializeField] private Light targetLight;



    public void OnInteract(PlayerMain who)
    {
        if (targetLight != null)
        {
            targetLight.enabled = !targetLight.enabled;
        }
    }

    //private void EnsurePromptStyle()
    //{
    //    if (promptStyle != null)
    //    {
    //        return;
    //    }

    //    promptStyle = new GUIStyle(GUI.skin.box)
    //    {
    //        alignment = TextAnchor.MiddleCenter,
    //        fontSize = 14
    //    };
    //}

    //private void OnGUI()
    //{
    //    if (!showPrompt || !isLookedAtNow)
    //    {
    //        return;
    //    }

    //    EnsurePromptStyle();

    //    Rect rect = new Rect(
    //        (Screen.width * 0.5f) - (promptSize.x * 0.5f) + promptOffset.x,
    //        (Screen.height * 0.5f) + promptOffset.y,
    //        promptSize.x,
    //        promptSize.y
    //    );

    //    GUI.Box(rect, BuildPromptText(), promptStyle);
    //}

    //private string BuildPromptText()
    //{
    //    string keyLabel = PlayerSettingsMenu.GetFunctionInteractKeyLabel();
    //    if (string.IsNullOrEmpty(promptText))
    //    {
    //        return $"{keyLabel}: Interact";
    //    }

    //    if (promptText.Contains("{key}"))
    //    {
    //        return promptText.Replace("{key}", keyLabel);
    //    }

    //    int colonIndex = promptText.IndexOf(':');
    //    if (colonIndex >= 0 && colonIndex < promptText.Length - 1)
    //    {
    //        string actionText = promptText.Substring(colonIndex + 1).Trim();
    //        return $"{keyLabel}: {actionText}";
    //    }

    //    return $"{keyLabel}: {promptText}";
    //}

    //private bool IsLookedAt()
    //{
    //    Transform cameraTransform = GetCameraTransform();
    //    if (cameraTransform == null)
    //    {
    //        return false;
    //    }

    //    Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
    //    if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
    //    {
    //        return false;
    //    }

    //    interactable lookedInteractable = hit.collider.GetComponentInParent<interactable>();
    //    return lookedInteractable == this;
    //}

    //private Transform GetCameraTransform()
    //{
    //    if (GameCore.instance != null && GameCore.instance.localPlayer != null && GameCore.instance.localPlayer.cam != null)
    //    {
    //        return GameCore.instance.localPlayer.cam.transform;
    //    }

    //    return Camera.main != null ? Camera.main.transform : null;
    //}
}