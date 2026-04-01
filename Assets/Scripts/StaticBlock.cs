using UnityEngine;

/// <summary>
/// A rectangular obstacle anchored to the floor or ceiling.
/// The player must be on the opposite surface to survive.
/// </summary>
public class StaticBlock : Obstacle
{
    public enum Surface { Floor, Ceiling }

    public Surface AttachedSurface { get; private set; }
    public float BlockHeight { get; private set; }
    public float BlockWidth { get; private set; }

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
        spriteRenderer.color = new Color(0.9f, 0.2f, 0.2f, 1f);
        spriteRenderer.sortingOrder = 1;
    }

    public void Setup(Surface surface, float width, float height, float floorY, float ceilingY)
    {
        AttachedSurface = surface;
        BlockWidth = width;
        BlockHeight = height;

        transform.localScale = new Vector3(width, height, 1f);
        boxCollider.size = Vector2.one;

        float yPos = (surface == Surface.Floor)
            ? floorY + height * 0.5f
            : ceilingY - height * 0.5f;

        transform.position = new Vector3(transform.position.x, yPos, 0f);
    }

    public override void Activate(Vector2 position, float despawnBehindPlayer)
    {
        base.Activate(position, despawnBehindPlayer);
    }

    public float GetTopY()
    {
        return transform.position.y + BlockHeight * 0.5f;
    }

    public float GetBottomY()
    {
        return transform.position.y - BlockHeight * 0.5f;
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
