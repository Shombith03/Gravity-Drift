using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Death screen overlay: final score, personal best comparison with NEW RECORD
/// animation if beaten, Retry button, and Share button (copies score to clipboard).
/// Fades in over the game view.
/// </summary>
public class DeathScreenUI : MonoBehaviour
{
    [Header("Colors")]
    public Color overlayColor = new Color(0.02f, 0.02f, 0.1f, 0.85f);
    public Color titleColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    public Color scoreColor = Color.white;
    public Color bestColor = new Color(0.6f, 0.6f, 0.7f, 0.9f);
    public Color newRecordColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color buttonColor = new Color(0.3f, 1f, 0.9f, 1f);
    public Color buttonHoverColor = new Color(0.4f, 1f, 1f, 1f);
    public Color shareButtonColor = new Color(0.6f, 0.6f, 0.7f, 1f);

    [Header("Timing")]
    public float fadeInDuration = 0.5f;
    public float scoreCountDuration = 1f;
    public float newRecordDelay = 0.3f;

    public static DeathScreenUI Instance { get; private set; }

    private Canvas deathCanvas;
    private CanvasGroup canvasGroup;
    private GameObject panel;
    private TextMeshProUGUI finalScoreText;
    private TextMeshProUGUI bestCompareText;
    private TextMeshProUGUI newRecordText;
    private Button retryButton;
    private Button shareButton;

    private bool isShowing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateDeathScreen();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver += ShowDeathScreen;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver -= ShowDeathScreen;
    }

    private void CreateDeathScreen()
    {
        // Canvas
        GameObject canvasGo = new GameObject("DeathCanvas");
        canvasGo.transform.SetParent(transform);
        deathCanvas = canvasGo.AddComponent<Canvas>();
        deathCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        deathCanvas.sortingOrder = 20;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 540);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        canvasGroup = canvasGo.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Overlay background
        panel = new GameObject("Overlay");
        panel.transform.SetParent(canvasGo.transform);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = overlayColor;

        // "GAME OVER" title
        CreateText("GameOverTitle", panel.transform,
            new Vector2(0.5f, 0.82f), new Vector2(400f, 60f),
            42, titleColor, "GAME OVER", FontStyles.Bold);

        // Final score (large)
        finalScoreText = CreateText("FinalScore", panel.transform,
            new Vector2(0.5f, 0.62f), new Vector2(500f, 90f),
            72, scoreColor, "0", FontStyles.Bold);

        // Best comparison
        bestCompareText = CreateText("BestCompare", panel.transform,
            new Vector2(0.5f, 0.50f), new Vector2(400f, 40f),
            28, bestColor, "", FontStyles.Normal);

        // NEW RECORD text (hidden by default)
        newRecordText = CreateText("NewRecord", panel.transform,
            new Vector2(0.5f, 0.43f), new Vector2(400f, 50f),
            36, newRecordColor, "NEW RECORD!", FontStyles.Bold);
        newRecordText.gameObject.SetActive(false);

        // Retry button
        retryButton = CreateButton("RetryBtn", panel.transform,
            new Vector2(0.5f, 0.25f), new Vector2(220f, 55f),
            "RETRY", 30, buttonColor);
        retryButton.onClick.AddListener(OnRetryClicked);

        // Share button
        shareButton = CreateButton("ShareBtn", panel.transform,
            new Vector2(0.5f, 0.14f), new Vector2(180f, 45f),
            "SHARE", 24, shareButtonColor);
        shareButton.onClick.AddListener(OnShareClicked);

        deathCanvas.gameObject.SetActive(false);
    }

    public void ShowDeathScreen()
    {
        if (isShowing) return;
        isShowing = true;
        deathCanvas.gameObject.SetActive(true);
        StartCoroutine(ShowSequence());
    }

    private IEnumerator ShowSequence()
    {
        int finalScore = (GameManager.Instance != null) ? GameManager.Instance.Score : 0;
        int personalBest = (GameManager.Instance != null) ? GameManager.Instance.PersonalBest : 0;
        bool isNewRecord = finalScore >= personalBest && finalScore > 0;

        newRecordText.gameObject.SetActive(false);

        // Fade in overlay
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = elapsed / fadeInDuration;
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Count up score
        elapsed = 0f;
        while (elapsed < scoreCountDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / scoreCountDuration;
            t = t * t * (3f - 2f * t); // smoothstep
            int displayScore = Mathf.RoundToInt(Mathf.Lerp(0, finalScore, t));
            finalScoreText.text = displayScore.ToString("N0");
            yield return null;
        }
        finalScoreText.text = finalScore.ToString("N0");

        // Show best comparison
        bestCompareText.text = "BEST: " + personalBest.ToString("N0");

        // NEW RECORD animation
        if (isNewRecord)
        {
            yield return new WaitForSecondsRealtime(newRecordDelay);
            newRecordText.gameObject.SetActive(true);
            StartCoroutine(PulseAnimation(newRecordText.transform));
        }

        // Enable interaction
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private IEnumerator PulseAnimation(Transform target)
    {
        while (target != null && target.gameObject.activeSelf)
        {
            float scale = 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.08f;
            target.localScale = Vector3.one * scale;
            yield return null;
        }
    }

    private void OnRetryClicked()
    {
        isShowing = false;
        if (GameManager.Instance != null)
            GameManager.Instance.RestartGame();
    }

    private void OnShareClicked()
    {
        int score = (GameManager.Instance != null) ? GameManager.Instance.Score : 0;
        string shareText = "I scored " + score.ToString("N0") + " in Gravity Drift!";
        CopyToClipboard(shareText);

        // Brief feedback
        TextMeshProUGUI btnText = shareButton.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
        {
            StartCoroutine(ShareFeedback(btnText));
        }
    }

    private IEnumerator ShareFeedback(TextMeshProUGUI btnText)
    {
        string original = btnText.text;
        btnText.text = "COPIED!";
        yield return new WaitForSecondsRealtime(1.5f);
        if (btnText != null) btnText.text = original;
    }

    private static void CopyToClipboard(string text)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Use JS interop for WebGL clipboard access
        Application.ExternalEval(
            "navigator.clipboard.writeText('" + text.Replace("'", "\\'") + "');");
#else
        TextEditor te = new TextEditor { text = text };
        te.SelectAll();
        te.Copy();
#endif
    }

    // --- Helpers ---

    private TextMeshProUGUI CreateText(string name, Transform parent,
        Vector2 anchorPos, Vector2 size,
        float fontSize, Color color, string text, FontStyles style)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = anchorPos;
        rect.anchorMax = anchorPos;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;

        return tmp;
    }

    private Button CreateButton(string name, Transform parent,
        Vector2 anchorPos, Vector2 size, string label, float fontSize, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorPos;
        rect.anchorMax = anchorPos;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(color.r * 0.15f, color.g * 0.15f, color.b * 0.15f, 0.8f);

        Button btn = go.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;

        // Border effect via outline
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(2f, 2f);

        // Label
        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform);
        RectTransform labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;

        return btn;
    }
}
