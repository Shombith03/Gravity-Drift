using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the in-game HUD: score, personal best, slow-mo charge bar,
/// and combo multiplier display. Creates all UI elements programmatically
/// on a Screen Space - Overlay Canvas.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Colors")]
    public Color scoreColor = new Color(1f, 1f, 1f, 0.9f);
    public Color bestColor = new Color(0.6f, 0.6f, 0.7f, 0.7f);
    public Color comboColor = new Color(1f, 0.8f, 0.2f, 1f);
    public Color chargeBarBg = new Color(0.15f, 0.15f, 0.25f, 0.6f);
    public Color chargeBarFill = new Color(0.3f, 1f, 0.9f, 0.9f);
    public Color chargeBarGlow = new Color(0.3f, 1f, 0.9f, 0.3f);

    [Header("Combo")]
    public float comboFadeInTime = 0.15f;
    public float comboDisplayTime = 1.5f;
    public float comboFadeOutTime = 0.5f;

    public static UIManager Instance { get; private set; }

    public Canvas HUDCanvas { get; private set; }

    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI bestText;
    private TextMeshProUGUI comboText;
    private Image chargeBarBackground;
    private Image chargeBarFillImage;
    private Image chargeBarGlowImage;
    private CanvasGroup comboGroup;

    private Coroutine comboCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        CreateCanvas();
        CreateScoreDisplay();
        CreateBestDisplay();
        CreateChargeBar();
        CreateComboDisplay();

        // Subscribe to events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += UpdateScore;
            GameManager.Instance.OnComboChanged += ShowCombo;
        }
        if (SlowMotionController.Instance != null)
        {
            SlowMotionController.Instance.OnChargesChanged += UpdateChargeBar;
        }

        UpdateScore(0);
        UpdateBest();
        UpdateChargeBar(0);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= UpdateScore;
            GameManager.Instance.OnComboChanged -= ShowCombo;
        }
        if (SlowMotionController.Instance != null)
        {
            SlowMotionController.Instance.OnChargesChanged -= UpdateChargeBar;
        }
    }

    private void Update()
    {
        // Continuously update charge bar fill for crystal progress
        UpdateChargeBarProgress();
    }

    // --- Creation ---

    private void CreateCanvas()
    {
        GameObject canvasGo = new GameObject("HUDCanvas");
        canvasGo.transform.SetParent(transform);
        HUDCanvas = canvasGo.AddComponent<Canvas>();
        HUDCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        HUDCanvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 540);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
    }

    private void CreateScoreDisplay()
    {
        scoreText = CreateText("ScoreText", HUDCanvas.transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(20f, -15f), new Vector2(300f, 60f),
            48, TextAlignmentOptions.TopLeft, scoreColor);
        scoreText.fontStyle = FontStyles.Bold;
    }

    private void CreateBestDisplay()
    {
        bestText = CreateText("BestText", HUDCanvas.transform,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-20f, -15f), new Vector2(250f, 40f),
            24, TextAlignmentOptions.TopRight, bestColor);
    }

    private void CreateChargeBar()
    {
        float barWidth = 200f;
        float barHeight = 8f;

        // Background
        GameObject bgGo = new GameObject("ChargeBarBg");
        bgGo.transform.SetParent(HUDCanvas.transform);
        RectTransform bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0f);
        bgRect.anchorMax = new Vector2(0.5f, 0f);
        bgRect.pivot = new Vector2(0.5f, 0f);
        bgRect.anchoredPosition = new Vector2(0f, 20f);
        bgRect.sizeDelta = new Vector2(barWidth + 4f, barHeight + 4f);
        chargeBarBackground = bgGo.AddComponent<Image>();
        chargeBarBackground.color = chargeBarBg;

        // Glow (slightly larger behind fill)
        GameObject glowGo = new GameObject("ChargeBarGlow");
        glowGo.transform.SetParent(bgGo.transform);
        RectTransform glowRect = glowGo.AddComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-3f, -3f);
        glowRect.offsetMax = new Vector2(3f, 3f);
        chargeBarGlowImage = glowGo.AddComponent<Image>();
        chargeBarGlowImage.color = chargeBarGlow;

        // Fill
        GameObject fillGo = new GameObject("ChargeBarFill");
        fillGo.transform.SetParent(bgGo.transform);
        RectTransform fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(2f, -2f);
        fillRect.sizeDelta = new Vector2(0f, fillRect.sizeDelta.y);
        chargeBarFillImage = fillGo.AddComponent<Image>();
        chargeBarFillImage.color = chargeBarFill;
    }

    private void CreateComboDisplay()
    {
        GameObject comboGo = new GameObject("ComboDisplay");
        comboGo.transform.SetParent(HUDCanvas.transform);
        RectTransform comboRect = comboGo.AddComponent<RectTransform>();
        comboRect.anchorMin = new Vector2(0.5f, 0.5f);
        comboRect.anchorMax = new Vector2(0.5f, 0.5f);
        comboRect.pivot = new Vector2(0.5f, 0.5f);
        comboRect.anchoredPosition = new Vector2(0f, 40f);
        comboRect.sizeDelta = new Vector2(300f, 80f);

        comboGroup = comboGo.AddComponent<CanvasGroup>();
        comboGroup.alpha = 0f;

        comboText = comboGo.AddComponent<TextMeshProUGUI>();
        comboText.fontSize = 56;
        comboText.alignment = TextAlignmentOptions.Center;
        comboText.color = comboColor;
        comboText.fontStyle = FontStyles.Bold;
        comboText.enableWordWrapping = false;
    }

    // --- Updates ---

    private void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = score.ToString("N0");
    }

    private void UpdateBest()
    {
        if (bestText == null) return;
        int best = (GameManager.Instance != null) ? GameManager.Instance.PersonalBest : 0;
        bestText.text = "BEST " + best.ToString("N0");
    }

    private void UpdateChargeBar(int charges)
    {
        UpdateChargeBarProgress();
    }

    private void UpdateChargeBarProgress()
    {
        if (chargeBarFillImage == null) return;
        if (SlowMotionController.Instance == null) return;

        SlowMotionController smc = SlowMotionController.Instance;
        float maxCharges = smc.maxCharges;
        float fullCharges = smc.CurrentCharges;
        float crystalProg = (float)smc.CrystalProgress / smc.crystalsPerCharge;
        float totalFill = (fullCharges + crystalProg) / maxCharges;

        float barWidth = chargeBarBackground.rectTransform.sizeDelta.x - 4f;
        RectTransform fillRect = chargeBarFillImage.rectTransform;
        fillRect.sizeDelta = new Vector2(barWidth * totalFill, fillRect.sizeDelta.y);

        // Glow pulses when a charge is full
        if (chargeBarGlowImage != null)
        {
            float glowAlpha = (smc.CurrentCharges > 0)
                ? 0.2f + Mathf.PingPong(Time.unscaledTime * 2f, 0.3f)
                : 0f;
            Color gc = chargeBarGlow;
            gc.a = glowAlpha;
            chargeBarGlowImage.color = gc;
        }
    }

    private void ShowCombo(int multiplier)
    {
        if (multiplier <= 1) return;

        if (comboCoroutine != null)
            StopCoroutine(comboCoroutine);
        comboCoroutine = StartCoroutine(ComboAnimation(multiplier));
    }

    private IEnumerator ComboAnimation(int multiplier)
    {
        comboText.text = "x" + multiplier;

        // Scale punch + fade in
        float elapsed = 0f;
        comboText.transform.localScale = Vector3.one * 1.5f;
        while (elapsed < comboFadeInTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / comboFadeInTime;
            comboGroup.alpha = t;
            float scale = Mathf.Lerp(1.5f, 1f, t * t);
            comboText.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        comboGroup.alpha = 1f;
        comboText.transform.localScale = Vector3.one;

        // Hold
        yield return new WaitForSecondsRealtime(comboDisplayTime);

        // Fade out
        elapsed = 0f;
        while (elapsed < comboFadeOutTime)
        {
            elapsed += Time.unscaledDeltaTime;
            comboGroup.alpha = 1f - (elapsed / comboFadeOutTime);
            yield return null;
        }
        comboGroup.alpha = 0f;
    }

    // --- Visibility ---

    public void Show()
    {
        if (HUDCanvas != null)
            HUDCanvas.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (HUDCanvas != null)
            HUDCanvas.gameObject.SetActive(false);
    }

    // --- Helpers ---

    private TextMeshProUGUI CreateText(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 position, Vector2 size,
        float fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;

        return tmp;
    }
}
