using System.Collections;
using UnityEngine;

/// <summary>
/// Freezes the game for a brief moment on death impact (hit stop / hit freeze).
/// Sets timeScale to 0 for a configurable real-time duration, then restores it.
/// </summary>
public class HitFreeze : MonoBehaviour
{
    [Tooltip("Duration of the freeze in real-time seconds")]
    public float freezeDuration = 0.05f;

    public static HitFreeze Instance { get; private set; }

    private float savedTimeScale;
    private float savedFixedDeltaTime;
    private bool isFrozen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public void Freeze(System.Action onComplete = null)
    {
        if (isFrozen) return;
        StartCoroutine(FreezeCoroutine(onComplete));
    }

    private IEnumerator FreezeCoroutine(System.Action onComplete)
    {
        isFrozen = true;
        savedTimeScale = Time.timeScale;
        savedFixedDeltaTime = Time.fixedDeltaTime;

        Time.timeScale = 0f;

        // Wait in real time
        float elapsed = 0f;
        while (elapsed < freezeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = savedTimeScale;
        Time.fixedDeltaTime = savedFixedDeltaTime;
        isFrozen = false;

        onComplete?.Invoke();
    }
}
