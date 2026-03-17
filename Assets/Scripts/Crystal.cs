using UnityEngine;

/// <summary>
/// Collectible crystal that awards points and charges the slow-motion meter.
/// Uses a trigger collider for overlap detection with the player.
/// Managed via object pool by ObstacleSpawner.
/// </summary>
public class Crystal : MonoBehaviour
{
    public bool IsActive { get; private set; }

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;
    private float despawnX;
    private float bobPhase;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
            circleCollider = gameObject.AddComponent<CircleCollider2D>();

        circleCollider.isTrigger = true;
        circleCollider.radius = 0.5f;

        spriteRenderer.sprite = CreateDiamondSprite();
        spriteRenderer.color = new Color(0.3f, 1f, 0.9f, 1f);
        spriteRenderer.sortingOrder = 2;

        transform.localScale = new Vector3(0.4f, 0.4f, 1f);
    }

    public void Activate(Vector2 position, float despawnBehind)
    {
        transform.position = new Vector3(position.x, position.y, 0f);
        despawnX = despawnBehind;
        bobPhase = Random.Range(0f, Mathf.PI * 2f);
        IsActive = true;
        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        IsActive = false;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsActive) return;

        float speed = ObstacleSpawner.GameSpeed;
        transform.Translate(Vector3.left * speed * Time.deltaTime, Space.World);

        // Gentle vertical bob
        bobPhase += 3f * Time.deltaTime;
        Vector3 pos = transform.position;
        pos.y += Mathf.Sin(bobPhase) * 0.3f * Time.deltaTime;
        transform.position = pos;

        if (transform.position.x < despawnX)
        {
            Deactivate();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsActive) return;
        if (!other.CompareTag("Player")) return;

        if (GameManager.Instance != null)
            GameManager.Instance.CollectCrystal();

        if (SlowMotionController.Instance != null)
            SlowMotionController.Instance.AddCrystalCharge();

        Deactivate();
    }

    private static Sprite cachedSprite;

    private static Sprite CreateDiamondSprite()
    {
        if (cachedSprite != null) return cachedSprite;
        Texture2D tex = new Texture2D(8, 8);
        Color clear = new Color(0, 0, 0, 0);
        Color white = Color.white;

        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                tex.SetPixel(x, y, clear);

        // Diamond shape
        tex.SetPixel(3, 0, white); tex.SetPixel(4, 0, white);
        tex.SetPixel(2, 1, white); tex.SetPixel(3, 1, white); tex.SetPixel(4, 1, white); tex.SetPixel(5, 1, white);
        tex.SetPixel(1, 2, white); tex.SetPixel(2, 2, white); tex.SetPixel(3, 2, white); tex.SetPixel(4, 2, white); tex.SetPixel(5, 2, white); tex.SetPixel(6, 2, white);
        tex.SetPixel(0, 3, white); tex.SetPixel(1, 3, white); tex.SetPixel(2, 3, white); tex.SetPixel(3, 3, white); tex.SetPixel(4, 3, white); tex.SetPixel(5, 3, white); tex.SetPixel(6, 3, white); tex.SetPixel(7, 3, white);
        tex.SetPixel(0, 4, white); tex.SetPixel(1, 4, white); tex.SetPixel(2, 4, white); tex.SetPixel(3, 4, white); tex.SetPixel(4, 4, white); tex.SetPixel(5, 4, white); tex.SetPixel(6, 4, white); tex.SetPixel(7, 4, white);
        tex.SetPixel(1, 5, white); tex.SetPixel(2, 5, white); tex.SetPixel(3, 5, white); tex.SetPixel(4, 5, white); tex.SetPixel(5, 5, white); tex.SetPixel(6, 5, white);
        tex.SetPixel(2, 6, white); tex.SetPixel(3, 6, white); tex.SetPixel(4, 6, white); tex.SetPixel(5, 6, white);
        tex.SetPixel(3, 7, white); tex.SetPixel(4, 7, white);

        tex.filterMode = FilterMode.Point;
        tex.Apply();
        cachedSprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
        return cachedSprite;
    }
}
