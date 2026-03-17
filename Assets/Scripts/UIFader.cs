using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen fade overlay for transitions between game states.
/// Provides static FadeIn/FadeOut methods. Persists across scenes.
/// </summary>
public class UIFader : MonoBehaviour
{
    public static UIFader Instance { get; private set; }

    public Color fadeColor = new Color(0.02f, 0.02f, 0.1f, 1f);
    public float defaultFadeDuration = 0.4f;

    private Canvas fadeCanvas;
    private Image fadeImage;
    private CanvasGroup fadeGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateFadeOverlay();
    }

    private void CreateFadeOverlay()
    {
        GameObject canvasGo = new GameObject("FadeCanvas");
        canvasGo.transform.SetParent(transform);
        fadeCanvas = canvasGo.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 540);

        fadeGroup = canvasGo.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable = false;

        GameObject imgGo = new GameObject("FadeImage");
        imgGo.transform.SetParent(canvasGo.transform);
        RectTransform rect = imgGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeImage = imgGo.AddComponent<Image>();
        fadeImage.color = fadeColor;
        fadeImage.raycastTarget = false;
    }

    /// <summary>Fade from transparent to opaque.</summary>
    public Coroutine FadeOut(float duration = -1f)
    {
        if (duration < 0f) duration = defaultFadeDuration;
        return StartCoroutine(FadeCoroutine(0f, 1f, duration));
    }

    /// <summary>Fade from opaque to transparent.</summary>
    public Coroutine FadeIn(float duration = -1f)
    {
        if (duration < 0f) duration = defaultFadeDuration;
        return StartCoroutine(FadeCoroutine(1f, 0f, duration));
    }

    /// <summary>Fade out, execute action, then fade in.</summary>
    public void FadeOutIn(System.Action midAction, float outDuration = -1f, float inDuration = -1f)
    {
        if (outDuration < 0f) outDuration = defaultFadeDuration;
        if (inDuration < 0f) inDuration = defaultFadeDuration;
        StartCoroutine(FadeOutInCoroutine(midAction, outDuration, inDuration));
    }

    private IEnumerator FadeOutInCoroutine(System.Action midAction,
        float outDuration, float inDuration)
    {
        yield return FadeCoroutine(0f, 1f, outDuration);
        midAction?.Invoke();
        yield return FadeCoroutine(1f, 0f, inDuration);
    }

    private IEnumerator FadeCoroutine(float from, float to, float duration)
    {
        fadeGroup.blocksRaycasts = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        fadeGroup.alpha = to;

        if (to <= 0f)
        {
            fadeGroup.blocksRaycasts = false;
        }
    }
}
