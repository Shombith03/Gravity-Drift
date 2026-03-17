using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Audio manager with SFX pooling and procedurally generated sound effects.
/// - Flip whoosh (pitch-shifted based on direction)
/// - Crystal chime (pentatonic scale based on combo count)
/// - Near-miss bass thud
/// - Death impact
/// All audio is generated at runtime using AudioClip.Create with PCM data.
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Pool")]
    public int audioSourcePoolSize = 12;

    [Header("Flip Whoosh")]
    public float whooshFloorPitch = 0.8f;
    public float whooshCeilingPitch = 1.2f;

    public static AudioManager Instance { get; private set; }

    private List<AudioSource> sourcePool = new List<AudioSource>();
    private AudioClip whooshClip;
    private AudioClip nearMissClip;
    private AudioClip deathClip;
    private AudioClip[] chimeClips; // pentatonic scale

    private PlayerController playerController;
    private int chimeIndex;

    // Pentatonic scale frequencies (C major pentatonic, octave 5)
    private static readonly float[] PentatonicFreqs = { 523.25f, 587.33f, 659.25f, 783.99f, 880f };

    private const int SampleRate = 44100;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        GenerateClips();
        CreatePool();
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
                playerController.OnFlip += OnPlayerFlip;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNearMiss += OnNearMiss;
            GameManager.Instance.OnGameOver += OnDeath;
        }
    }

    private void OnDestroy()
    {
        if (playerController != null)
            playerController.OnFlip -= OnPlayerFlip;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNearMiss -= OnNearMiss;
            GameManager.Instance.OnGameOver -= OnDeath;
        }
    }

    // --- Public API ---

    public void PlayFlipWhoosh(bool toCeiling)
    {
        float pitch = toCeiling ? whooshCeilingPitch : whooshFloorPitch;
        PlayClip(whooshClip, sfxVolume * 0.5f, pitch);
    }

    public void PlayCrystalChime()
    {
        int comboMult = (GameManager.Instance != null) ? GameManager.Instance.ComboMultiplier : 1;
        int index = (chimeIndex + comboMult - 1) % chimeClips.Length;
        chimeIndex = (chimeIndex + 1) % chimeClips.Length;
        PlayClip(chimeClips[index], sfxVolume * 0.6f, 1f);
    }

    public void PlayNearMissThud()
    {
        PlayClip(nearMissClip, sfxVolume * 0.7f, 1f);
    }

    public void PlayDeathImpact()
    {
        PlayClip(deathClip, sfxVolume * 1f, 1f);
    }

    // --- Event Handlers ---

    private void OnPlayerFlip(PlayerController.GravityState newState)
    {
        PlayFlipWhoosh(newState == PlayerController.GravityState.CEILING);
    }

    private void OnNearMiss(int points, int multiplier)
    {
        PlayNearMissThud();
    }

    private void OnDeath()
    {
        PlayDeathImpact();
    }

    // --- Pool ---

    private void CreatePool()
    {
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            GameObject go = new GameObject("SFX_" + i);
            go.transform.SetParent(transform);
            AudioSource src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D
            sourcePool.Add(src);
        }
    }

    private void PlayClip(AudioClip clip, float volume, float pitch)
    {
        if (clip == null) return;

        AudioSource src = GetAvailableSource();
        if (src == null) return;

        src.clip = clip;
        src.volume = volume * masterVolume;
        src.pitch = pitch;
        src.Play();
    }

    private AudioSource GetAvailableSource()
    {
        for (int i = 0; i < sourcePool.Count; i++)
        {
            if (!sourcePool[i].isPlaying)
                return sourcePool[i];
        }
        // Steal oldest
        return sourcePool[0];
    }

    // --- Procedural Audio Generation ---

    private void GenerateClips()
    {
        whooshClip = GenerateWhoosh(0.12f);
        nearMissClip = GenerateBassThud(0.15f, 60f);
        deathClip = GenerateDeathImpact(0.5f);
        chimeClips = GeneratePentatonicChimes();
    }

    private AudioClip GenerateWhoosh(float duration)
    {
        int samples = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float envelope = Mathf.Sin(t * Mathf.PI); // bell curve
            // White noise filtered with envelope
            float noise = Random.Range(-1f, 1f);
            // Frequency sweep from high to low
            float freq = Mathf.Lerp(2000f, 500f, t);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * t * duration);
            data[i] = (noise * 0.3f + sine * 0.7f) * envelope * 0.4f;
        }

        AudioClip clip = AudioClip.Create("Whoosh", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateBassThud(float duration, float frequency)
    {
        int samples = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float envelope = Mathf.Exp(-t * 10f); // sharp decay
            float sine = Mathf.Sin(2f * Mathf.PI * frequency * ((float)i / SampleRate));
            // Add sub-harmonic
            float sub = Mathf.Sin(2f * Mathf.PI * (frequency * 0.5f) * ((float)i / SampleRate));
            data[i] = (sine * 0.6f + sub * 0.4f) * envelope * 0.5f;
        }

        AudioClip clip = AudioClip.Create("BassThud", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateDeathImpact(float duration)
    {
        int samples = Mathf.CeilToInt(SampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float envelope = Mathf.Exp(-t * 5f);

            // Low frequency impact
            float lowFreq = Mathf.Sin(2f * Mathf.PI * 40f * ((float)i / SampleRate));
            // Noise crunch
            float noise = Random.Range(-1f, 1f) * Mathf.Exp(-t * 15f);
            // Mid-range crack
            float crack = Mathf.Sin(2f * Mathf.PI * 200f * ((float)i / SampleRate)) * Mathf.Exp(-t * 20f);

            data[i] = (lowFreq * 0.4f + noise * 0.35f + crack * 0.25f) * envelope * 0.6f;
        }

        AudioClip clip = AudioClip.Create("DeathImpact", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip[] GeneratePentatonicChimes()
    {
        AudioClip[] clips = new AudioClip[PentatonicFreqs.Length];
        float duration = 0.3f;
        int samples = Mathf.CeilToInt(SampleRate * duration);

        for (int n = 0; n < PentatonicFreqs.Length; n++)
        {
            float freq = PentatonicFreqs[n];
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float time = (float)i / SampleRate;
                float envelope = Mathf.Exp(-t * 6f);

                // Fundamental + harmonics for bell-like quality
                float fundamental = Mathf.Sin(2f * Mathf.PI * freq * time);
                float harmonic2 = Mathf.Sin(2f * Mathf.PI * freq * 2.0f * time) * 0.5f;
                float harmonic3 = Mathf.Sin(2f * Mathf.PI * freq * 3.0f * time) * 0.25f;
                float harmonic5 = Mathf.Sin(2f * Mathf.PI * freq * 5.0f * time) * 0.1f;

                data[i] = (fundamental + harmonic2 + harmonic3 + harmonic5) * envelope * 0.3f;
            }

            clips[n] = AudioClip.Create("Chime_" + n, samples, 1, SampleRate, false);
            clips[n].SetData(data, 0);
        }

        return clips;
    }
}
