using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Main menu screen: title with animated floating orb in background,
/// single Play button, minimal neon aesthetic. Loads game scene on play.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Colors")]
    public Color backgroundColor = new Color(0.039f, 0.039f, 0.180f, 1f);
    public Color titleColor = new Color(0.3f, 1f, 0.9f, 1f);
    public Color subtitleColor = new Color(0.6f, 0.6f, 0.7f, 0.8f);
    public Color orbColorA = new Color(0.2f, 0.5f, 1f, 1f);
    public Color orbColorB = new Color(1f, 0.2f, 0.8f, 1f);
    public Color playButtonColor = new Color(0.3f, 1f, 0.9f, 1f);

    [Header("Orb Animation")]
    public float orbFloatSpeed = 1.5f;
    public float orbFloatAmplitude = 30f;
    public float orbColorSpeed = 0.5f;
    public float orbSize = 80f;

    [Header("Scene")]
    [Tooltip("Build index of the game scene (1 if MainMenu is 0)")]
    public int gameSceneIndex = 1;

    private Canvas menuCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform orbTransform;
    private Image orbImage;
    private Vector2 orbBasePos;

    private void Start()
    {
        CreateMenu();
    }

    private void Update()
    {
        AnimateOrb();
    }

    private void CreateMenu()
    {
        // Canvas
        GameObject canvasGo = new GameObject("MenuCanvas");
        canvasGo.transform.SetParent(transform);
        menuCanvas = canvasGo.AddComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = 30;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 540);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        canvasGroup = canvasGo.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        // Full-screen background
        GameObject bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform);
        RectTransform bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = bgGo.AddComponent<Image>();
        bgImage.color = backgroundColor;

        // Floating orb
        GameObject orbGo = new GameObject("FloatingOrb");
        orbGo.transform.SetParent(canvasGo.transform);
        orbTransform = orbGo.AddComponent<RectTransform>();
        orbTransform.anchorMin = orbTransform.anchorMax = new Vector2(0.5f, 0.55f);
        orbTransform.pivot = new Vector2(0.5f, 0.5f);
        orbTransform.anchoredPosition = Vector2.zero;
        orbTransform.sizeDelta = new Vector2(orbSize, orbSize);
        orbBasePos = orbTransform.anchoredPosition;

        orbImage = orbGo.AddComponent<Image>();
        orbImage.sprite = CreateCircleSprite();
        orbImage.color = orbColorA;

        // Orb glow (larger, faint behind)
        GameObject glowGo = new GameObject("OrbGlow");
        glowGo.transform.SetParent(orbGo.transform);
        RectTransform glowRect = glowGo.AddComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-40f, -40f);
        glowRect.offsetMax = new Vector2(40f, 40f);
        Image glowImage = glowGo.AddComponent<Image>();
        glowImage.sprite = CreateCircleSprite();
        glowImage.color = new Color(orbColorA.r, orbColorA.g, orbColorA.b, 0.15f);
        glowGo.transform.SetAsFirstSibling();

        // Title
        CreateText("Title", canvasGo.transform,
            new Vector2(0.5f, 0.85f), new Vector2(600f, 80f),
            64, titleColor, "GRAVITY DRIFT", FontStyles.Bold);

        // Subtitle
        CreateText("Subtitle", canvasGo.transform,
            new Vector2(0.5f, 0.78f), new Vector2(400f, 30f),
            20, subtitleColor, "TAP TO FLIP  /  SHIFT FOR SLOW-MO", FontStyles.Normal);

        // Play button
        Button playBtn = CreateButton("PlayBtn", canvasGo.transform,
            new Vector2(0.5f, 0.18f), new Vector2(240f, 60f),
            "PLAY", 36, playButtonColor);
        playBtn.onClick.AddListener(OnPlayClicked);

        // Best score
        int best = PlayerPrefs.GetInt("GravityDrift_PersonalBest", 0);
        if (best > 0)
        {
            CreateText("BestScore", canvasGo.transform,
                new Vector2(0.5f, 0.08f), new Vector2(300f, 30f),
                20, subtitleColor, "BEST: " + best.ToString("N0"), FontStyles.Normal);
        }
    }

    private void AnimateOrb()
    {
        if (orbTransform == null) return;

        float t = Time.unscaledTime;
        float yOffset = Mathf.Sin(t * orbFloatSpeed) * orbFloatAmplitude;
        float xOffset = Mathf.Cos(t * orbFloatSpeed * 0.7f) * orbFloatAmplitude * 0.3f;
        orbTransform.anchoredPosition = orbBasePos + new Vector2(xOffset, yOffset);

        // Color cycle
        float colorT = (Mathf.Sin(t * orbColorSpeed) + 1f) * 0.5f;
        orbImage.color = Color.Lerp(orbColorA, orbColorB, colorT);
    }

    private void OnPlayClicked()
    {
        StartCoroutine(FadeAndLoadGame());
    }

    private IEnumerator FadeAndLoadGame()
    {
        float duration = 0.4f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - (elapsed / duration);
            yield return null;
        }

        // If there's a separate game scene, load it; otherwise destroy menu
        if (SceneManager.sceneCountInBuildSettings > gameSceneIndex)
        {
            SceneManager.LoadScene(gameSceneIndex);
        }
        else
        {
            // Single-scene mode: destroy menu and start game
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CurrentState = GameManager.GameState.Playing;
            }
            Destroy(menuCanvas.gameObject);
            Destroy(gameObject);
        }
    }

    // --- Helpers ---

    private TextMeshProUGUI CreateText(string name, Transform parent,
        Vector2 anchorPos, Vector2 size,
        float fontSize, Color color, string text, FontStyles style)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorPos;
        rect.anchorMax = anchorPos;
        rect.pivot = new Vector2(0.5f, 0.5f);
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
        bg.color = new Color(color.r * 0.12f, color.g * 0.12f, color.b * 0.12f, 0.9f);

        Button btn = go.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        btn.colors = colors;

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

    private static Sprite cachedCircle;

    private static Sprite CreateCircleSprite()
    {
        if (cachedCircle != null) return cachedCircle;
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        float center = size * 0.5f;
        float radius = center - 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(1f - (dist - radius + 1f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        cachedCircle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedCircle;
    }
}
