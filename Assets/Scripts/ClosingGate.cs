using UnityEngine;

/// <summary>
/// Two blocks extending from floor and ceiling with a gap between them that shrinks over time.
/// The player must pass through the gap before it closes.
/// </summary>
public class ClosingGate : Obstacle
{
    public float GapSize { get; private set; }
    public float CloseSpeed { get; private set; }
    public float MinGap { get; private set; }
    public float BlockWidth { get; private set; }

    private Transform topBlock;
    private Transform bottomBlock;
    private float floorY;
    private float ceilingY;
    private float tunnelHeight;

    private void Awake()
    {
        bottomBlock = CreateBlock("GateBottom", new Color(0.8f, 0.2f, 0.8f, 1f));
        topBlock = CreateBlock("GateTop", new Color(0.8f, 0.2f, 0.8f, 1f));
    }

    public void Setup(float width, float initialGap, float minGap, float closeSpeed,
                      float floor, float ceiling)
    {
        BlockWidth = width;
        GapSize = initialGap;
        MinGap = minGap;
        CloseSpeed = closeSpeed;
        floorY = floor;
        ceilingY = ceiling;
        tunnelHeight = ceiling - floor;

        UpdateBlockPositions();
    }

    public override void Activate(Vector2 position, float despawnBehindPlayer)
    {
        base.Activate(position, despawnBehindPlayer);
        topBlock.gameObject.SetActive(true);
        bottomBlock.gameObject.SetActive(true);
        UpdateBlockPositions();
    }

    public override void Deactivate()
    {
        topBlock.gameObject.SetActive(false);
        bottomBlock.gameObject.SetActive(false);
        base.Deactivate();
    }

    protected override void Update()
    {
        if (!IsActive) return;

        if (GapSize > MinGap)
        {
            GapSize -= CloseSpeed * Time.deltaTime;
            if (GapSize < MinGap) GapSize = MinGap;
            UpdateBlockPositions();
        }

        base.Update();
    }

    private void UpdateBlockPositions()
    {
        float blockHeight = (tunnelHeight - GapSize) * 0.5f;
        if (blockHeight < 0.1f) blockHeight = 0.1f;

        float centerY = (floorY + ceilingY) * 0.5f;

        bottomBlock.localPosition = new Vector3(0f, (floorY + blockHeight * 0.5f) - transform.position.y, 0f);
        bottomBlock.localScale = new Vector3(BlockWidth, blockHeight, 1f);

        topBlock.localPosition = new Vector3(0f, (ceilingY - blockHeight * 0.5f) - transform.position.y, 0f);
        topBlock.localScale = new Vector3(BlockWidth, blockHeight, 1f);
    }

    private Transform CreateBlock(string blockName, Color color)
    {
        GameObject block = new GameObject(blockName);
        block.transform.SetParent(transform);
        block.transform.localPosition = Vector3.zero;

        SpriteRenderer sr = block.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = color;
        sr.sortingOrder = 1;

        BoxCollider2D col = block.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        block.SetActive(false);
        return block.transform;
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
