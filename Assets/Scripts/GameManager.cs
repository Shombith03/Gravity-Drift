using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Central game state manager for Gravity Drift.
/// Tracks score (distance + crystals + near-miss bonuses), combo multiplier,
/// game states, and personal best via PlayerPrefs.
/// Ensures an EventSystem exists for UI interaction.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum GameState { Menu, Playing, GameOver, Paused }

    private const string BestScoreKey = "GravityDrift_PersonalBest";

    [Header("Scoring")]
    [Tooltip("Points awarded per unit of distance traveled")]
    public int pointsPerUnit = 1;
    [Tooltip("Points awarded per crystal collected")]
    public int pointsPerCrystal = 50;
    [Tooltip("Base points for a near-miss")]
    public int nearMissBasePoints = 25;

    [Header("Combo")]
    [Tooltip("Maximum combo multiplier")]
    public int maxComboMultiplier = 4;
    [Tooltip("Seconds without a near-miss before combo resets")]
    public float comboResetTime = 3f;

    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; set; } = GameState.Menu;
    public int Score { get; private set; }
    public int PersonalBest { get; private set; }
    public int ComboMultiplier { get; private set; } = 1;
    public int CrystalsCollected { get; private set; }
    public int NearMissCount { get; private set; }

    public event System.Action<int> OnScoreChanged;
    public event System.Action<int, int> OnNearMiss; // bonus points, multiplier
    public event System.Action<int> OnCrystalCollected; // total crystals
    public event System.Action<int> OnComboChanged;
    public event System.Action OnGameOver;

    private Transform playerTransform;
    private float lastPlayerX;
    private float distanceAccumulator;
    private float comboTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        PersonalBest = PlayerPrefs.GetInt(BestScoreKey, 0);
    }

    private void Start()
    {
        // Ensure EventSystem exists for UI
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            lastPlayerX = playerTransform.position.x;
        }

        // Fade in on scene load
        if (UIFader.Instance != null)
            UIFader.Instance.FadeIn(0.5f);
    }

    private void Update()
    {
        if (CurrentState != GameState.Playing) return;

        UpdateDistanceScore();
        UpdateComboTimer();
    }

    private void UpdateDistanceScore()
    {
        if (playerTransform == null) return;

        float currentX = playerTransform.position.x;
        float delta = currentX - lastPlayerX;
        lastPlayerX = currentX;

        if (delta > 0f)
        {
            distanceAccumulator += delta;
            while (distanceAccumulator >= 1f)
            {
                distanceAccumulator -= 1f;
                AddScore(pointsPerUnit);
            }
        }
    }

    private void UpdateComboTimer()
    {
        if (ComboMultiplier > 1)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                ComboMultiplier = 1;
                OnComboChanged?.Invoke(ComboMultiplier);
            }
        }
    }

    public void CollectCrystal()
    {
        if (CurrentState != GameState.Playing) return;

        CrystalsCollected++;
        AddScore(pointsPerCrystal);
        OnCrystalCollected?.Invoke(CrystalsCollected);
    }

    public void RegisterNearMiss()
    {
        if (CurrentState != GameState.Playing) return;

        NearMissCount++;
        int bonus = nearMissBasePoints * ComboMultiplier;
        AddScore(bonus);
        OnNearMiss?.Invoke(bonus, ComboMultiplier);

        // Advance combo
        if (ComboMultiplier < maxComboMultiplier)
        {
            ComboMultiplier++;
            OnComboChanged?.Invoke(ComboMultiplier);
        }
        comboTimer = comboResetTime;
    }

    public void TriggerGameOver()
    {
        if (CurrentState == GameState.GameOver) return;

        CurrentState = GameState.GameOver;

        if (Score > PersonalBest)
        {
            PersonalBest = Score;
            PlayerPrefs.SetInt(BestScoreKey, PersonalBest);
            PlayerPrefs.Save();
        }

        OnGameOver?.Invoke();
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.Paused;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
    }

    public void RestartGame()
    {
        Score = 0;
        CrystalsCollected = 0;
        NearMissCount = 0;
        ComboMultiplier = 1;
        comboTimer = 0f;
        distanceAccumulator = 0f;
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;

        if (UIFader.Instance != null)
        {
            UIFader.Instance.FadeOutIn(() =>
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            });
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void AddScore(int points)
    {
        Score += points;
        OnScoreChanged?.Invoke(Score);
    }
}
