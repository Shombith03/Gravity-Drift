using UnityEngine;

/// <summary>
/// Manages all particle effects in Gravity Drift:
/// - Orb trail (color shifts blue/magenta based on gravity state)
/// - Radial burst on crystal collect
/// - Directional explosion on death
/// - Background speed lines that increase density with gameSpeed
/// </summary>
public class ParticleManager : MonoBehaviour
{
    [Header("Trail")]
    public float trailEmissionRate = 30f;
    public float trailLifetime = 0.4f;
    public float trailSize = 0.15f;
    public Color floorTrailColor = new Color(0.2f, 0.5f, 1f, 1f);     // Blue
    public Color ceilingTrailColor = new Color(1f, 0.2f, 0.8f, 1f);   // Magenta

    [Header("Crystal Burst")]
    public int crystalBurstCount = 20;
    public Color crystalBurstColor = new Color(0.3f, 1f, 0.9f, 1f);   // Cyan

    [Header("Death Explosion")]
    public int deathParticleCount = 40;
    public float deathExplosionSpeed = 8f;

    [Header("Speed Lines")]
    public float baseSpeedLineRate = 5f;
    public float speedLineLength = 0.8f;
    public Color speedLineColor = new Color(0.3f, 0.3f, 0.5f, 0.3f);

    public static ParticleManager Instance { get; private set; }

    private ParticleSystem trailSystem;
    private ParticleSystem crystalBurstSystem;
    private ParticleSystem deathExplosionSystem;
    private ParticleSystem speedLineSystem;

    private Transform playerTransform;
    private PlayerController playerController;
    private Camera mainCamera;

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
        mainCamera = Camera.main;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
                playerController.OnFlip += OnPlayerFlip;
        }

        CreateTrailSystem();
        CreateCrystalBurstSystem();
        CreateDeathExplosionSystem();
        CreateSpeedLineSystem();
    }

    private void Update()
    {
        UpdateTrail();
        UpdateSpeedLines();
    }

    private void OnDestroy()
    {
        if (playerController != null)
            playerController.OnFlip -= OnPlayerFlip;
    }

    // --- Trail ---

    private void CreateTrailSystem()
    {
        GameObject go = new GameObject("TrailParticles");
        go.transform.SetParent(transform);
        trailSystem = go.AddComponent<ParticleSystem>();

        var main = trailSystem.main;
        main.startLifetime = trailLifetime;
        main.startSize = trailSize;
        main.startSpeed = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        main.startColor = floorTrailColor;
        main.playOnAwake = true;

        var emission = trailSystem.emission;
        emission.rateOverTime = trailEmissionRate;

        var shape = trailSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        var colorOverLifetime = trailSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = trailSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Disable default renderer shape, use sprite
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMaterial();
        renderer.sortingOrder = 0;
    }

    private void UpdateTrail()
    {
        if (playerTransform == null || trailSystem == null) return;

        trailSystem.transform.position = playerTransform.position;

        if (playerController != null)
        {
            var main = trailSystem.main;
            Color targetColor = (playerController.CurrentState == PlayerController.GravityState.FLOOR)
                ? floorTrailColor : ceilingTrailColor;
            main.startColor = targetColor;
        }
    }

    private void OnPlayerFlip(PlayerController.GravityState newState)
    {
        if (trailSystem == null) return;
        // Emit a small burst on flip for extra flair
        trailSystem.Emit(8);
    }

    // --- Crystal Burst ---

    private void CreateCrystalBurstSystem()
    {
        GameObject go = new GameObject("CrystalBurstParticles");
        go.transform.SetParent(transform);
        crystalBurstSystem = go.AddComponent<ParticleSystem>();

        var main = crystalBurstSystem.main;
        main.startLifetime = 0.5f;
        main.startSize = 0.12f;
        main.startSpeed = 4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        main.startColor = crystalBurstColor;
        main.playOnAwake = false;
        main.stopAction = ParticleSystemStopAction.None;

        var emission = crystalBurstSystem.emission;
        emission.rateOverTime = 0f;

        var shape = crystalBurstSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var colorOverLifetime = crystalBurstSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(crystalBurstColor, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = crystalBurstSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMaterial();
        renderer.sortingOrder = 3;
    }

    public void PlayCrystalBurst(Vector3 position)
    {
        if (crystalBurstSystem == null) return;
        crystalBurstSystem.transform.position = position;
        crystalBurstSystem.Emit(crystalBurstCount);
    }

    // --- Death Explosion ---

    private void CreateDeathExplosionSystem()
    {
        GameObject go = new GameObject("DeathExplosionParticles");
        go.transform.SetParent(transform);
        deathExplosionSystem = go.AddComponent<ParticleSystem>();

        var main = deathExplosionSystem.main;
        main.startLifetime = 1f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSpeed = deathExplosionSpeed;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;
        main.playOnAwake = false;
        main.gravityModifier = 1f;

        // Two-color gradient: blue to magenta
        var colorBySpeed = deathExplosionSystem.colorBySpeed;
        colorBySpeed.enabled = false;

        var startColorGrad = new ParticleSystem.MinMaxGradient(floorTrailColor, ceilingTrailColor);
        main.startColor = startColorGrad;

        var emission = deathExplosionSystem.emission;
        emission.rateOverTime = 0f;

        var shape = deathExplosionSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var colorOverLifetime = deathExplosionSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.red, 0.5f),
                new GradientColorKey(Color.black, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMaterial();
        renderer.sortingOrder = 5;
    }

    public void PlayDeathExplosion(Vector3 position)
    {
        if (deathExplosionSystem == null) return;
        deathExplosionSystem.transform.position = position;
        deathExplosionSystem.Emit(deathParticleCount);
    }

    // --- Speed Lines ---

    private void CreateSpeedLineSystem()
    {
        GameObject go = new GameObject("SpeedLineParticles");
        go.transform.SetParent(transform);
        speedLineSystem = go.AddComponent<ParticleSystem>();

        var main = speedLineSystem.main;
        main.startLifetime = 0.6f;
        main.startSize3D = true;
        main.startSizeX = speedLineLength;
        main.startSizeY = 0.02f;
        main.startSizeZ = 1f;
        main.startSpeed = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;
        main.startColor = speedLineColor;
        main.playOnAwake = true;

        var emission = speedLineSystem.emission;
        emission.rateOverTime = baseSpeedLineRate;

        var shape = speedLineSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(1f, 10f, 1f);
        shape.rotation = new Vector3(0f, 0f, 90f);

        var velocity = speedLineSystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = -15f;
        velocity.y = 0f;
        velocity.z = 0f;

        var colorOverLifetime = speedLineSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.4f, 0.2f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMaterial();
        renderer.sortingOrder = -1;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 3f;
    }

    private void UpdateSpeedLines()
    {
        if (speedLineSystem == null || mainCamera == null) return;

        // Position at right edge of camera
        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        speedLineSystem.transform.position = new Vector3(
            mainCamera.transform.position.x + halfWidth + 1f,
            mainCamera.transform.position.y,
            0f
        );

        // Increase density with gameSpeed
        float speedRatio = ObstacleSpawner.GameSpeed / 5f;
        var emission = speedLineSystem.emission;
        emission.rateOverTime = baseSpeedLineRate * speedRatio * speedRatio;

        // Speed lines move faster with game speed
        var velocity = speedLineSystem.velocityOverLifetime;
        velocity.x = -ObstacleSpawner.GameSpeed * 3f;
    }

    // --- Helpers ---

    private static Material cachedMaterial;

    private static Material CreateParticleMaterial()
    {
        if (cachedMaterial != null) return cachedMaterial;

        // Use a built-in particle shader
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");

        cachedMaterial = new Material(shader);
        cachedMaterial.color = Color.white;

        // Create a simple white circle texture
        int size = 16;
        Texture2D tex = new Texture2D(size, size);
        float center = size * 0.5f;
        float radius = center - 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(1f - (dist / radius));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        cachedMaterial.mainTexture = tex;

        return cachedMaterial;
    }
}
