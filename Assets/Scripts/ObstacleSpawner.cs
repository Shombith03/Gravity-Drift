using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns obstacles from an object pool at the right edge of the screen.
/// Manages difficulty progression and validates that every segment is survivable.
///
/// Difficulty curve:
///   0-15s  : Single static blocks on floor only
///   15-30s : Static blocks on both floor and ceiling
///   30-60s : Oscillators mixed in with static blocks
///   60s+   : Closing gates and fast oscillators
///
/// gameSpeed starts at 5 and increases by 0.5 every 10 seconds.
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Tunnel Geometry")]
    public float floorY = -2.5f;
    public float ceilingY = 2.5f;

    [Header("Speed")]
    public float baseSpeed = 5f;
    public float speedIncrement = 0.5f;
    public float speedIncrementInterval = 10f;

    [Header("Spawning")]
    [Tooltip("Spawn distance ahead of camera right edge")]
    public float spawnAheadDistance = 2f;
    [Tooltip("Distance behind camera left edge to despawn")]
    public float despawnBehindDistance = 3f;
    [Tooltip("Minimum spacing between obstacles in world units")]
    public float minSpacing = 3f;
    [Tooltip("Maximum spacing between obstacles in world units")]
    public float maxSpacing = 6f;

    [Header("Pool Sizes")]
    public int staticBlockPoolSize = 20;
    public int oscillatorPoolSize = 10;
    public int closingGatePoolSize = 6;

    [Header("Obstacle Dimensions")]
    public float staticBlockWidth = 1.5f;
    public float staticBlockMinHeight = 1.5f;
    public float staticBlockMaxHeight = 3f;
    public float oscillatorWidth = 1.2f;
    public float oscillatorHeight = 1.5f;
    public float gateWidth = 1.5f;
    public float gateInitialGap = 2.5f;
    public float gateMinGap = 1.2f;
    public float gateCloseSpeed = 0.5f;

    [Header("Validation")]
    [Tooltip("Minimum gap the player needs to pass through safely")]
    public float playerClearance = 1.1f;

    public static float GameSpeed { get; private set; }
    public static float ElapsedTime { get; private set; }

    private List<StaticBlock> staticBlockPool = new List<StaticBlock>();
    private List<Oscillator> oscillatorPool = new List<Oscillator>();
    private List<ClosingGate> closingGatePool = new List<ClosingGate>();

    private Transform playerTransform;
    private Camera mainCamera;
    private float nextSpawnX;
    private float halfScreenWidth;

    private enum DifficultyPhase
    {
        FloorOnly,       // 0-15s
        BothSurfaces,    // 15-30s
        WithOscillators, // 30-60s
        FullMix          // 60s+
    }

    private void Awake()
    {
        GameSpeed = baseSpeed;
        ElapsedTime = 0f;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        mainCamera = Camera.main;
        halfScreenWidth = mainCamera.orthographicSize * mainCamera.aspect;

        InitializePools();

        nextSpawnX = GetScreenRightEdge() + spawnAheadDistance;
    }

    private void Update()
    {
        ElapsedTime += Time.deltaTime;

        GameSpeed = baseSpeed + speedIncrement * Mathf.Floor(ElapsedTime / speedIncrementInterval);

        TrySpawn();
    }

    private DifficultyPhase GetCurrentPhase()
    {
        if (ElapsedTime < 15f) return DifficultyPhase.FloorOnly;
        if (ElapsedTime < 30f) return DifficultyPhase.BothSurfaces;
        if (ElapsedTime < 60f) return DifficultyPhase.WithOscillators;
        return DifficultyPhase.FullMix;
    }

    private void TrySpawn()
    {
        float rightEdge = GetScreenRightEdge();

        while (nextSpawnX < rightEdge + spawnAheadDistance + maxSpacing)
        {
            SpawnObstacleAtX(nextSpawnX);
            float spacing = Random.Range(minSpacing, maxSpacing);
            float speedFactor = GameSpeed / baseSpeed;
            spacing *= speedFactor;
            nextSpawnX += spacing;
        }
    }

    private void SpawnObstacleAtX(float x)
    {
        DifficultyPhase phase = GetCurrentPhase();
        const int maxAttempts = 10;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Obstacle spawned = TryCreateObstacle(x, phase);
            if (spawned != null && ValidateObstacle(spawned))
            {
                return;
            }
            if (spawned != null)
            {
                DeactivateObstacle(spawned);
            }
        }

        // Fallback: spawn a simple floor block (always survivable by being on ceiling)
        SpawnStaticBlock(x, StaticBlock.Surface.Floor, staticBlockMinHeight);
    }

    private Obstacle TryCreateObstacle(float x, DifficultyPhase phase)
    {
        float roll = Random.value;

        switch (phase)
        {
            case DifficultyPhase.FloorOnly:
                return SpawnStaticBlock(x, StaticBlock.Surface.Floor,
                    Random.Range(staticBlockMinHeight, staticBlockMaxHeight * 0.6f));

            case DifficultyPhase.BothSurfaces:
                StaticBlock.Surface surface = (roll < 0.5f)
                    ? StaticBlock.Surface.Floor
                    : StaticBlock.Surface.Ceiling;
                return SpawnStaticBlock(x, surface,
                    Random.Range(staticBlockMinHeight, staticBlockMaxHeight * 0.7f));

            case DifficultyPhase.WithOscillators:
                if (roll < 0.6f)
                {
                    StaticBlock.Surface s = (Random.value < 0.5f)
                        ? StaticBlock.Surface.Floor
                        : StaticBlock.Surface.Ceiling;
                    return SpawnStaticBlock(x, s,
                        Random.Range(staticBlockMinHeight, staticBlockMaxHeight));
                }
                else
                {
                    return SpawnOscillator(x, Random.Range(1.5f, 3f), Random.Range(0f, Mathf.PI * 2f));
                }

            case DifficultyPhase.FullMix:
                if (roll < 0.35f)
                {
                    StaticBlock.Surface s = (Random.value < 0.5f)
                        ? StaticBlock.Surface.Floor
                        : StaticBlock.Surface.Ceiling;
                    return SpawnStaticBlock(x, s,
                        Random.Range(staticBlockMinHeight, staticBlockMaxHeight));
                }
                else if (roll < 0.65f)
                {
                    float speed = Random.Range(2.5f, 5f);
                    return SpawnOscillator(x, speed, Random.Range(0f, Mathf.PI * 2f));
                }
                else
                {
                    return SpawnClosingGate(x);
                }

            default:
                return null;
        }
    }

    private StaticBlock SpawnStaticBlock(float x, StaticBlock.Surface surface, float height)
    {
        StaticBlock block = GetFromPool(staticBlockPool);
        if (block == null) return null;

        float yPos = (surface == StaticBlock.Surface.Floor)
            ? floorY + height * 0.5f
            : ceilingY - height * 0.5f;

        block.Setup(surface, staticBlockWidth, height, floorY, ceilingY);
        block.Activate(new Vector2(x, yPos), GetDespawnX());
        return block;
    }

    private Oscillator SpawnOscillator(float x, float oscSpeed, float phase)
    {
        Oscillator osc = GetFromPool(oscillatorPool);
        if (osc == null) return null;

        osc.Setup(oscillatorWidth, oscillatorHeight, oscSpeed, floorY, ceilingY, phase);
        float startY = (floorY + ceilingY) * 0.5f;
        osc.Activate(new Vector2(x, startY), GetDespawnX());
        return osc;
    }

    private ClosingGate SpawnClosingGate(float x)
    {
        ClosingGate gate = GetFromPool(closingGatePool);
        if (gate == null) return null;

        float closeSpeed = gateCloseSpeed + (ElapsedTime / 120f) * 0.5f;
        float minGap = Mathf.Max(gateMinGap, playerClearance);

        gate.Setup(gateWidth, gateInitialGap, minGap, closeSpeed, floorY, ceilingY);
        float centerY = (floorY + ceilingY) * 0.5f;
        gate.Activate(new Vector2(x, centerY), GetDespawnX());
        return gate;
    }

    /// <summary>
    /// Validates that the obstacle has at least one survivable path.
    /// For static blocks: checks the opposite surface is clear.
    /// For oscillators: checks that a gap exists at some point in the oscillation.
    /// For closing gates: checks that the minimum gap is wide enough for the player.
    /// </summary>
    private bool ValidateObstacle(Obstacle obstacle)
    {
        if (obstacle is StaticBlock block)
        {
            return ValidateStaticBlock(block);
        }
        if (obstacle is Oscillator osc)
        {
            return ValidateOscillator(osc);
        }
        if (obstacle is ClosingGate gate)
        {
            return ValidateClosingGate(gate);
        }
        return true;
    }

    private bool ValidateStaticBlock(StaticBlock block)
    {
        float tunnelHeight = ceilingY - floorY;
        float remainingGap = tunnelHeight - block.BlockHeight;
        return remainingGap >= playerClearance;
    }

    private bool ValidateOscillator(Oscillator osc)
    {
        // Check that the oscillator doesn't fill the entire tunnel at any position.
        // The oscillator moves between floorY + height/2 and ceilingY - height/2.
        // At floor position, gap above = tunnelHeight - height.
        // At ceiling position, gap below = tunnelHeight - height.
        // At mid positions, there are gaps both above and below.
        float tunnelHeight = ceilingY - floorY;
        float gapWhenAtEdge = tunnelHeight - osc.BlockHeight;

        // Player can survive if there's enough gap on either side when oscillator is at an extreme
        return gapWhenAtEdge >= playerClearance;
    }

    private bool ValidateClosingGate(ClosingGate gate)
    {
        return gate.MinGap >= playerClearance;
    }

    /// <summary>
    /// Validates a group of nearby obstacles to ensure at least one flip pattern survives.
    /// Called for obstacles within one screen-width of each other.
    /// </summary>
    public bool ValidateSegment(List<Obstacle> segment)
    {
        if (segment == null || segment.Count == 0) return true;

        // Simulate two possible states: player on floor or player on ceiling
        // For each obstacle, determine which states are survivable
        bool canBeOnFloor = true;
        bool canBeOnCeiling = true;

        for (int i = 0; i < segment.Count; i++)
        {
            Obstacle obs = segment[i];
            bool floorSafe = IsStateSafeForObstacle(obs, true);
            bool ceilingSafe = IsStateSafeForObstacle(obs, false);

            // After this obstacle, which states can the player be in?
            bool nextFloor = (canBeOnFloor && floorSafe) || (canBeOnCeiling && ceilingSafe);
            bool nextCeiling = (canBeOnFloor && floorSafe) || (canBeOnCeiling && ceilingSafe);

            // The player can always flip between obstacles, so both transitions are possible
            // if at least one state survived the current obstacle
            if (!nextFloor && !nextCeiling)
                return false;

            canBeOnFloor = nextFloor;
            canBeOnCeiling = nextCeiling;
        }

        return canBeOnFloor || canBeOnCeiling;
    }

    private bool IsStateSafeForObstacle(Obstacle obstacle, bool onFloor)
    {
        float playerY = onFloor ? floorY : ceilingY;

        if (obstacle is StaticBlock block)
        {
            if (block.AttachedSurface == StaticBlock.Surface.Floor && onFloor)
                return false;
            if (block.AttachedSurface == StaticBlock.Surface.Ceiling && !onFloor)
                return false;
            return true;
        }

        if (obstacle is Oscillator osc)
        {
            // Oscillator passes through the middle; the player is safe at extremes
            // when the oscillator is at the opposite extreme
            float tunnelHeight = ceilingY - floorY;
            float gap = tunnelHeight - osc.BlockHeight;
            return gap >= playerClearance;
        }

        if (obstacle is ClosingGate gate)
        {
            // Gates have a centered gap - player must be near center
            // This is survivable from either surface if the gap is large enough
            return gate.MinGap >= playerClearance;
        }

        return true;
    }

    private void DeactivateObstacle(Obstacle obstacle)
    {
        obstacle.Deactivate();
    }

    // --- Object Pool ---

    private void InitializePools()
    {
        Transform poolParent = new GameObject("ObstaclePool").transform;
        poolParent.SetParent(transform);

        for (int i = 0; i < staticBlockPoolSize; i++)
        {
            GameObject go = new GameObject("StaticBlock_" + i);
            go.transform.SetParent(poolParent);
            StaticBlock block = go.AddComponent<StaticBlock>();
            block.Deactivate();
            staticBlockPool.Add(block);
        }

        for (int i = 0; i < oscillatorPoolSize; i++)
        {
            GameObject go = new GameObject("Oscillator_" + i);
            go.transform.SetParent(poolParent);
            Oscillator osc = go.AddComponent<Oscillator>();
            osc.Deactivate();
            oscillatorPool.Add(osc);
        }

        for (int i = 0; i < closingGatePoolSize; i++)
        {
            GameObject go = new GameObject("ClosingGate_" + i);
            go.transform.SetParent(poolParent);
            ClosingGate gate = go.AddComponent<ClosingGate>();
            gate.Deactivate();
            closingGatePool.Add(gate);
        }
    }

    private T GetFromPool<T>(List<T> pool) where T : Obstacle
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].IsActive)
                return pool[i];
        }
        return null;
    }

    // --- Helpers ---

    private float GetScreenRightEdge()
    {
        if (mainCamera == null) return 0f;
        return mainCamera.transform.position.x + halfScreenWidth;
    }

    private float GetDespawnX()
    {
        if (mainCamera == null) return -100f;
        return mainCamera.transform.position.x - halfScreenWidth - despawnBehindDistance;
    }
}
