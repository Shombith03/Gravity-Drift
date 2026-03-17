using UnityEngine;

/// <summary>
/// Controls the player orb in Gravity Drift.
/// Binary gravity states (FLOOR/CEILING) with spring-damper movement between surfaces.
/// </summary>
public class PlayerController : MonoBehaviour
{
    public enum GravityState { FLOOR, CEILING }

    [Header("Tunnel Geometry")]
    [Tooltip("Y position of the floor surface")]
    public float floorY = -2.5f;
    [Tooltip("Y position of the ceiling surface")]
    public float ceilingY = 2.5f;

    [Header("Spring-Damper Settings")]
    [Tooltip("Spring force pulling toward target surface")]
    public float springForce = 50f;
    [Tooltip("Damping ratio (0 = no damping, 1 = critically damped)")]
    [Range(0f, 2f)]
    public float dampingRatio = 0.7f;

    [Header("Movement")]
    [Tooltip("Constant forward speed on X axis")]
    public float forwardSpeed = 5f;

    [Header("Flip")]
    [Tooltip("Cooldown between gravity flips in seconds")]
    public float flipCooldown = 0.15f;

    public GravityState CurrentState { get; private set; } = GravityState.FLOOR;

    private Rigidbody2D rb;
    private float flipTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }

    private void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        flipTimer -= Time.deltaTime;

        if (flipTimer <= 0f && (Input.GetKeyDown(KeyCode.Space) || HasTapInput()))
        {
            Flip();
        }
    }

    private void FixedUpdate()
    {
        ApplySpringDamper();
        ApplyForwardMovement();
    }

    private void Flip()
    {
        CurrentState = (CurrentState == GravityState.FLOOR) ? GravityState.CEILING : GravityState.FLOOR;
        flipTimer = flipCooldown;
    }

    private void ApplySpringDamper()
    {
        float targetY = (CurrentState == GravityState.FLOOR) ? floorY : ceilingY;
        float displacement = targetY - rb.position.y;
        float dampingForce = -2f * dampingRatio * Mathf.Sqrt(springForce) * rb.linearVelocity.y;
        float totalForce = springForce * displacement + dampingForce;

        rb.AddForce(new Vector2(0f, totalForce));
    }

    private void ApplyForwardMovement()
    {
        float speed = ObstacleSpawner.GameSpeed > 0f ? ObstacleSpawner.GameSpeed : forwardSpeed;
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Obstacle obstacle = collision.collider.GetComponentInParent<Obstacle>();
        if (obstacle != null && obstacle.IsActive)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerGameOver();
        }
    }

    private bool HasTapInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            return touch.phase == TouchPhase.Began;
        }
        return Input.GetMouseButtonDown(0);
    }
}
