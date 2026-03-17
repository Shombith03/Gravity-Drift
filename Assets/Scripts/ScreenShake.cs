using UnityEngine;

/// <summary>
/// Configurable screen shake system with preset intensities.
/// Near-miss: 3px equivalent (0.03 world units). Death: 8px equivalent (0.08 world units).
/// Uses Perlin noise for smoother shake patterns. Fades out over duration.
/// </summary>
public class ScreenShake : MonoBehaviour
{
    [Header("Presets (world units, ~100px per unit)")]
    [Tooltip("Shake amplitude for near-miss events")]
    public float nearMissAmplitude = 0.03f;
    [Tooltip("Shake duration for near-miss events")]
    public float nearMissDuration = 0.15f;
    [Tooltip("Shake amplitude for death event")]
    public float deathAmplitude = 0.08f;
    [Tooltip("Shake duration for death event")]
    public float deathDuration = 0.4f;

    [Header("Shake Settings")]
    [Tooltip("Frequency of the shake oscillation")]
    public float frequency = 25f;

    public static ScreenShake Instance { get; private set; }

    public Vector3 ShakeOffset { get; private set; }

    private float shakeDuration;
    private float shakeAmplitude;
    private float shakeTimer;
    private float seedX;
    private float seedY;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(shakeTimer / shakeDuration);
            float amplitude = shakeAmplitude * t;

            float elapsed = (shakeDuration - shakeTimer) * frequency;
            float offsetX = (Mathf.PerlinNoise(seedX + elapsed, 0f) - 0.5f) * 2f * amplitude;
            float offsetY = (Mathf.PerlinNoise(0f, seedY + elapsed) - 0.5f) * 2f * amplitude;

            ShakeOffset = new Vector3(offsetX, offsetY, 0f);
        }
        else
        {
            ShakeOffset = Vector3.zero;
        }
    }

    public void Shake(float duration, float amplitude)
    {
        if (shakeTimer > 0f && amplitude < shakeAmplitude) return;

        shakeDuration = duration;
        shakeAmplitude = amplitude;
        shakeTimer = duration;
        seedX = Random.Range(0f, 100f);
        seedY = Random.Range(0f, 100f);
    }

    public void ShakeNearMiss()
    {
        Shake(nearMissDuration, nearMissAmplitude);
    }

    public void ShakeDeath()
    {
        Shake(deathDuration, deathAmplitude);
    }
}
