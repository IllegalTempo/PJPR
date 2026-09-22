using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaypointIndicator : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string label = "Waypoint";

    [Header("Layout")]
    [SerializeField, Min(0f)] private float edgePadding = 64f;
    [SerializeField, Min(0f)] private float iconSize = 34f;
    [SerializeField, Min(0f)] private float targetHeightOffset = 0f;

    [Header("Style")]
    [SerializeField] private Color onScreenColor = new Color(0.22f, 0.86f, 1f, 1f);
    [SerializeField] private Color offScreenColor = new Color(1f, 0.74f, 0.22f, 1f);
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private Image icon;
    [SerializeField] private RectTransform markerRoot;
    [SerializeField] private RectTransform arrowRoot;

    private Canvas canvas;
    private RectTransform canvasRect;
    private Camera cachedCamera;
    private bool hasFixedPosition;
    private Vector3 fixedPosition;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        EnsureVisuals();
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (!TryGetTargetPosition(out Vector3 targetPosition))
        {
            SetVisible(false);
            return;
        }

        Camera playerCamera = GetPlayerCamera();
        if (playerCamera == null || canvasRect == null)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        UpdateMarker(playerCamera, targetPosition + Vector3.up * targetHeightOffset);
    }

    public void SetTarget(Transform newTarget, string newLabel = "Waypoint")
    {
        target = newTarget;
        label = string.IsNullOrWhiteSpace(newLabel) ? "Waypoint" : newLabel;
        hasFixedPosition = false;
        UpdateLabel();
    }

    public void SetPosition(Vector3 worldPosition, string newLabel = "Waypoint")
    {
        target = null;
        fixedPosition = worldPosition;
        label = string.IsNullOrWhiteSpace(newLabel) ? "Waypoint" : newLabel;
        hasFixedPosition = true;
        UpdateLabel();
    }

    public void Clear()
    {
        target = null;
        hasFixedPosition = false;
        SetVisible(false);
    }

    private bool TryGetTargetPosition(out Vector3 targetPosition)
    {
        if (target != null)
        {
            targetPosition = target.position;
            return true;
        }

        targetPosition = fixedPosition;
        return hasFixedPosition;
    }

    private void UpdateMarker(Camera playerCamera, Vector3 targetPosition)
    {
        Vector3 viewportPosition = playerCamera.WorldToViewportPoint(targetPosition);
        bool isBehindCamera = viewportPosition.z < 0f;
        bool isOnScreen = !isBehindCamera
            && viewportPosition.x >= 0f
            && viewportPosition.x <= 1f
            && viewportPosition.y >= 0f
            && viewportPosition.y <= 1f;

        Vector2 screenPoint;
        Vector2 directionFromCenter = new Vector2(viewportPosition.x - 0.5f, viewportPosition.y - 0.5f);
        if (isBehindCamera)
        {
            directionFromCenter = -directionFromCenter;
        }

        if (isOnScreen)
        {
            screenPoint = new Vector2(
                viewportPosition.x * playerCamera.pixelWidth,
                viewportPosition.y * playerCamera.pixelHeight);
        }
        else
        {
            screenPoint = GetEdgeScreenPoint(playerCamera, directionFromCenter);
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : playerCamera,
            out Vector2 localPoint);

        markerRoot.anchoredPosition = localPoint;
        SetStyle(isOnScreen, directionFromCenter);
        UpdateDistance(targetPosition);
    }

    private Vector2 GetEdgeScreenPoint(Camera playerCamera, Vector2 directionFromCenter)
    {
        if (directionFromCenter.sqrMagnitude <= Mathf.Epsilon)
        {
            directionFromCenter = Vector2.up;
        }

        directionFromCenter.Normalize();

        float halfWidth = Mathf.Max(0f, playerCamera.pixelWidth * 0.5f - edgePadding);
        float halfHeight = Mathf.Max(0f, playerCamera.pixelHeight * 0.5f - edgePadding);
        float xScale = Mathf.Abs(directionFromCenter.x) > Mathf.Epsilon
            ? halfWidth / Mathf.Abs(directionFromCenter.x)
            : float.PositiveInfinity;
        float yScale = Mathf.Abs(directionFromCenter.y) > Mathf.Epsilon
            ? halfHeight / Mathf.Abs(directionFromCenter.y)
            : float.PositiveInfinity;
        float scale = Mathf.Min(xScale, yScale);

        Vector2 screenCenter = new Vector2(playerCamera.pixelWidth * 0.5f, playerCamera.pixelHeight * 0.5f);
        return screenCenter + directionFromCenter * scale;
    }

    private void SetStyle(bool isOnScreen, Vector2 directionFromCenter)
    {
        Color color = isOnScreen ? onScreenColor : offScreenColor;
        if (icon != null)
        {
            icon.color = color;
        }

        if (labelText != null)
        {
            labelText.color = color;
        }

        if (distanceText != null)
        {
            distanceText.color = Color.white;
        }

        if (arrowRoot == null)
        {
            return;
        }

        if (isOnScreen)
        {
            arrowRoot.localRotation = Quaternion.identity;
            return;
        }

        if (directionFromCenter.sqrMagnitude <= Mathf.Epsilon)
        {
            directionFromCenter = Vector2.up;
        }

        float angle = Mathf.Atan2(directionFromCenter.y, directionFromCenter.x) * Mathf.Rad2Deg - 90f;
        arrowRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void UpdateLabel()
    {
        if (labelText != null)
        {
            labelText.text = label;
        }
    }

    private void UpdateDistance(Vector3 targetPosition)
    {
        if (distanceText == null)
        {
            return;
        }

        Transform origin = null;
        if (GameCore.Instance != null && GameCore.Instance.Local_Player != null)
        {
            origin = GameCore.Instance.Local_Player.transform;
        }
        else if (cachedCamera != null)
        {
            origin = cachedCamera.transform;
        }

        distanceText.text = origin != null
            ? $"{Vector3.Distance(origin.position, targetPosition):F0}m"
            : string.Empty;
    }

    private Camera GetPlayerCamera()
    {
        if (GameCore.Instance != null
            && GameCore.Instance.Local_Player != null
            && GameCore.Instance.Local_Player.cam != null)
        {
            Camera localPlayerCamera = GameCore.Instance.Local_Player.cam.GetComponent<Camera>()
                ?? GameCore.Instance.Local_Player.cam.GetComponentInChildren<Camera>();
            if (localPlayerCamera != null)
            {
                cachedCamera = localPlayerCamera;
                return cachedCamera;
            }
        }

        if (cachedCamera != null && cachedCamera.isActiveAndEnabled)
        {
            return cachedCamera;
        }

        cachedCamera = Camera.main;
        return cachedCamera;
    }

    private void EnsureVisuals()
    {
        if (markerRoot == null)
        {
            markerRoot = CreateRect("WaypointMarker", transform, new Vector2(130f, 72f));
        }

        if (arrowRoot == null)
        {
            arrowRoot = CreateRect("Arrow", markerRoot, new Vector2(iconSize, iconSize));
            arrowRoot.anchoredPosition = new Vector2(0f, 18f);
        }

        if (icon == null)
        {
            icon = arrowRoot.gameObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.sprite = CreateArrowSprite();
        }

        if (labelText == null)
        {
            labelText = CreateText("Label", markerRoot, new Vector2(0f, -10f), 18f, FontStyles.Bold);
        }

        if (distanceText == null)
        {
            distanceText = CreateText("Distance", markerRoot, new Vector2(0f, -31f), 15f, FontStyles.Normal);
        }

        UpdateLabel();
    }

    private RectTransform CreateRect(string objectName, Transform parent, Vector2 size)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = rectObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        return rect;
    }

    private TMP_Text CreateText(string objectName, Transform parent, Vector2 position, float fontSize, FontStyles fontStyle)
    {
        RectTransform textRect = CreateRect(objectName, parent, new Vector2(160f, 22f));
        textRect.anchoredPosition = position;

        TextMeshProUGUI text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMax = fontSize;
        text.fontSizeMin = 9f;
        text.fontStyle = fontStyle;
        text.raycastTarget = false;
        text.text = string.Empty;
        return text;
    }

    private Sprite CreateArrowSprite()
    {
        const int textureSize = 32;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color white = Color.white;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                bool inHead = y >= 12 && Mathf.Abs(x - 16) <= (textureSize - y) * 0.45f;
                bool inStem = y >= 3 && y < 18 && x >= 13 && x <= 18;
                texture.SetPixel(x, y, inHead || inStem ? white : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
    }

    private void SetVisible(bool visible)
    {
        if (markerRoot != null && markerRoot.gameObject.activeSelf != visible)
        {
            markerRoot.gameObject.SetActive(visible);
        }
    }
}
