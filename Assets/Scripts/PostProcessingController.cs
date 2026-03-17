using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controls URP post-processing effects:
/// - Bloom on all neon elements (always active, intensity scales with speed)
/// - Chromatic aberration during slow-motion (ramps up/down smoothly)
///
/// Creates a Volume with an inline VolumeProfile at runtime.
/// Requires URP with post-processing enabled on the camera.
/// </summary>
public class PostProcessingController : MonoBehaviour
{
    [Header("Bloom")]
    [Tooltip("Base bloom intensity")]
    public float baseBloomIntensity = 1.5f;
    [Tooltip("Additional bloom per unit of gameSpeed above base")]
    public float bloomPerSpeed = 0.1f;
    [Tooltip("Bloom threshold")]
    public float bloomThreshold = 0.8f;
    [Tooltip("Bloom scatter")]
    [Range(0f, 1f)] public float bloomScatter = 0.7f;

    [Header("Chromatic Aberration")]
    [Tooltip("Chromatic aberration intensity during slow-mo")]
    public float slowMoChromaticIntensity = 0.5f;
    [Tooltip("Speed of chromatic aberration ramp")]
    public float chromaticLerpSpeed = 8f;

    public static PostProcessingController Instance { get; private set; }

    private Volume volume;
    private Bloom bloom;
    private ChromaticAberration chromaticAberration;
    private float targetChromaticIntensity;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SetupVolume();
        EnableCameraPostProcessing();

        if (SlowMotionController.Instance != null)
            SlowMotionController.Instance.OnSlowMotionToggled += OnSlowMotionToggled;
    }

    private void OnDestroy()
    {
        if (SlowMotionController.Instance != null)
            SlowMotionController.Instance.OnSlowMotionToggled -= OnSlowMotionToggled;
    }

    private void Update()
    {
        UpdateBloom();
        UpdateChromaticAberration();
    }

    private void SetupVolume()
    {
        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

        // Bloom
        bloom = ScriptableObject.CreateInstance<Bloom>();
        bloom.active = true;
        bloom.threshold.Override(bloomThreshold);
        bloom.intensity.Override(baseBloomIntensity);
        bloom.scatter.Override(bloomScatter);
        profile.components.Add(bloom);

        // Chromatic Aberration
        chromaticAberration = ScriptableObject.CreateInstance<ChromaticAberration>();
        chromaticAberration.active = true;
        chromaticAberration.intensity.Override(0f);
        profile.components.Add(chromaticAberration);

        volume.profile = profile;
    }

    private void EnableCameraPostProcessing()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        var camData = cam.GetUniversalAdditionalCameraData();
        if (camData != null)
        {
            camData.renderPostProcessing = true;
        }
    }

    private void UpdateBloom()
    {
        if (bloom == null) return;

        float speedBonus = Mathf.Max(0f, ObstacleSpawner.GameSpeed - 5f) * bloomPerSpeed;
        bloom.intensity.Override(baseBloomIntensity + speedBonus);
    }

    private void UpdateChromaticAberration()
    {
        if (chromaticAberration == null) return;

        float current = chromaticAberration.intensity.value;
        float lerped = Mathf.Lerp(current, targetChromaticIntensity,
            chromaticLerpSpeed * Time.unscaledDeltaTime);
        chromaticAberration.intensity.Override(lerped);
    }

    private void OnSlowMotionToggled(bool active)
    {
        targetChromaticIntensity = active ? slowMoChromaticIntensity : 0f;
    }
}
