using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class MeteoriteWarningIndicator : MonoBehaviour, IPoolable
{
    [Header("Settings")]
    [Tooltip("Distance from origin to place the indicator")]
    [SerializeField] private float indicatorDistance = 30f;

    [Tooltip("Fade-in duration when shown")]
    [SerializeField] private float fadeInDuration = 0.3f;

    [Header("Colors")]
    [SerializeField] private Color safeColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;

    private Image arrowImage;
    private float warningDuration;
    private float elapsedTime;
    private bool isActive;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        arrowImage = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
    }

    public void Show(Vector3 direction, float duration)
    {
        warningDuration = duration;
        elapsedTime = 0f;
        isActive = true;

        transform.position = direction.normalized * indicatorDistance;

        transform.rotation = Quaternion.LookRotation(direction);

        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    public void Hide()
    {
        isActive = false;
        StopAllCoroutines();
        StartCoroutine(FadeOutAndReturn());
    }

    private System.Collections.IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < fadeInDuration && isActive)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeInDuration);
            yield return null;
        }

        if (isActive)
            canvasGroup.alpha = 1f;
    }

    private System.Collections.IEnumerator FadeOutAndReturn()
    {
        float t = 0f;
        float startAlpha = canvasGroup.alpha;

        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;

        if (MeteoritePool.Instance != null)
        {
            MeteoritePool.Instance.Return(gameObject, "Warning");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!isActive) return;

        elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsedTime / warningDuration);

        if (progress < 0.5f)
        {
            arrowImage.color = Color.Lerp(safeColor, warningColor, progress * 2f);
        }
        else
        {
            arrowImage.color = Color.Lerp(warningColor, dangerColor, (progress - 0.5f) * 2f);
        }

        // Pulse effect as it gets closer
        float pulse = 1f + Mathf.Sin(elapsedTime * 10f) * 0.1f * progress;
        transform.localScale = Vector3.one * pulse;

        // Auto-hide when duration expires
        if (elapsedTime >= warningDuration)
        {
            Hide();
        }
    }

    public void OnSpawn()
    {
        isActive = false;
        elapsedTime = 0f;
        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.one;
    }

    public void OnDespawn()
    {
        isActive = false;
        elapsedTime = 0f;
        canvasGroup.alpha = 0f;
        StopAllCoroutines();
    }
}
