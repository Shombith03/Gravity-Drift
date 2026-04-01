using UnityEngine;

/// <summary>
/// An obstacle that moves vertically between floor and ceiling surfaces.
/// The player must time their flips to pass through when the gap aligns.
/// </summary>
public class Oscillator : Obstacle
{
    public float OscillationSpeed { get; private set; }
    public float BlockWidth { get; private set; }
    public float BlockHeight { get; private set; }

    private float floorY;
    private float ceilingY;
    private float minY;
    private float maxY;
    private float phase;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
            boxCollider = gameObject.AddComponent<BoxCollider2D>();

        spriteRenderer.sprite = CreateSquareSprite();
        spriteRenderer.color = new Color(1f, 0.6f, 0f, 1f);
        spriteRenderer.sortingOrder = 1;
    }

    public void Setup(float width, float height, float speed, float floor, float ceiling, float startPhase)
    {
        BlockWidth = width;
        BlockHeight = height;
        OscillationSpeed = speed;
        floorY = floor;
        ceilingY = ceiling;
        phase = startPhase;

        transform.localScale = new Vector3(width, height, 1f);
        boxCollider.size = Vector2.one;

        minY = floorY + height * 0.5f;
        maxY = ceilingY - height * 0.5f;
    }

    protected override void Update()
    {
        if (!IsActive) return;

        phase += OscillationSpeed * Time.deltaTime;
        float t = (Mathf.Sin(phase) + 1f) * 0.5f;
        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(minY, maxY, t);
        transform.position = pos;

        base.Update();
    }

    public float GetCurrentY()
    {
        return transform.position.y;
    }

    private static Sprite cachedSprite;

    private static Sprite CreateSquareSprite()
    {
        if (cachedSprite != null) return cachedSprite;
        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        cachedSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return cachedSprite;
    }
}
