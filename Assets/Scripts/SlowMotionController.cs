using UnityEngine;

/// <summary>
/// Manages slow-motion ability. Collecting 3 crystals fills one charge.
/// Shift key or double-tap activates 0.3x timeScale for 2 seconds.
/// Properly adjusts fixedDeltaTime to keep physics consistent.
/// </summary>
public class SlowMotionController : MonoBehaviour
{
    [Header("Charge")]
    [Tooltip("Crystals needed to fill one charge")]
    public int crystalsPerCharge = 3;
    [Tooltip("Maximum charges that can be stored")]
    public int maxCharges = 3;

    [Header("Slow Motion")]
    [Tooltip("Time scale during slow motion (0.3 = 30% speed)")]
    public float slowTimeScale = 0.3f;
    [Tooltip("Duration of slow motion in real-time seconds")]
    public float slowDuration = 2f;

    [Header("Double-Tap")]
    [Tooltip("Max time between taps for a double-tap")]
    public float doubleTapWindow = 0.3f;

    public static SlowMotionController Instance { get; private set; }

    public int CurrentCharges { get; private set; }
    public int CrystalProgress { get; private set; }
    public bool IsSlowMotionActive { get; private set; }
    public float SlowMotionTimeRemaining { get; private set; }

    public event System.Action<int> OnChargesChanged;
    public event System.Action<bool> OnSlowMotionToggled;

    private float defaultFixedDeltaTime;
    private float lastTapTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (IsSlowMotionActive)
        {
            // Use unscaledDeltaTime since timeScale is modified
            SlowMotionTimeRemaining -= Time.unscaledDeltaTime;
            if (SlowMotionTimeRemaining <= 0f)
            {
                DeactivateSlowMotion();
            }
        }

        CheckActivationInput();
    }

    public void AddCrystalCharge()
    {
        CrystalProgress++;
        if (CrystalProgress >= crystalsPerCharge)
        {
            CrystalProgress = 0;
            if (CurrentCharges < maxCharges)
            {
                CurrentCharges++;
                OnChargesChanged?.Invoke(CurrentCharges);
            }
        }
    }

    private void CheckActivationInput()
    {
        if (CurrentCharges <= 0) return;

        bool activate = false;

        // Shift key
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            activate = true;
        }

        // Double-tap detection (touch or mouse)
        if (!activate && (HasTapInput()))
        {
            float timeSinceLastTap = Time.unscaledTime - lastTapTime;
            if (timeSinceLastTap <= doubleTapWindow)
            {
                activate = true;
                lastTapTime = 0f; // Reset to prevent triple-tap triggering
            }
            else
            {
                lastTapTime = Time.unscaledTime;
            }
        }

        if (activate && !IsSlowMotionActive)
        {
            ActivateSlowMotion();
        }
    }

    private void ActivateSlowMotion()
    {
        CurrentCharges--;
        OnChargesChanged?.Invoke(CurrentCharges);

        IsSlowMotionActive = true;
        SlowMotionTimeRemaining = slowDuration;
        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * slowTimeScale;

        OnSlowMotionToggled?.Invoke(true);
    }

    private void DeactivateSlowMotion()
    {
        IsSlowMotionActive = false;
        SlowMotionTimeRemaining = 0f;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;

        OnSlowMotionToggled?.Invoke(false);
    }

    private void OnDestroy()
    {
        // Ensure timeScale is restored
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
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
