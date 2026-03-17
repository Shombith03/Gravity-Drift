using UnityEngine;

/// <summary>
/// Applies a camera shake effect. Attach to the same GameObject as CameraFollow.
/// Call Shake() to trigger; the offset is applied on top of normal camera position.
/// </summary>
public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    public Vector3 ShakeOffset { get; private set; }

    private float shakeDuration;
    private float shakeIntensity;
    private float shakeTimer;

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
            float t = shakeTimer / shakeDuration;
            float intensity = shakeIntensity * t; // fade out
            ShakeOffset = new Vector3(
                Random.Range(-intensity, intensity),
                Random.Range(-intensity, intensity),
                0f
            );
        }
        else
        {
            ShakeOffset = Vector3.zero;
        }
    }

    public void Shake(float duration, float intensity)
    {
        // Allow stronger shakes to override weaker ones
        if (shakeTimer > 0f && intensity < shakeIntensity) return;

        shakeDuration = duration;
        shakeIntensity = intensity;
        shakeTimer = duration;
    }
}
