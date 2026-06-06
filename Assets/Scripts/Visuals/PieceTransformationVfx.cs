using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class PieceTransformationVfx : MonoBehaviour
{
    [Header("Playback")]
    [SerializeField] private bool _playOnAwake = true;
    [SerializeField] private bool _disableExistingRootParticleSystem = true;
    [SerializeField] private bool _selfDestroy = true;
    [SerializeField] private float _destroyAfter = 1.8f;
    [SerializeField] private bool _logPlayback;
    [SerializeField] private bool _showVisibilityFlash;

    [Header("Look")]
    [SerializeField] private float _heightOffset = 0.45f;
    [SerializeField] private float _radius = 0.7f;
    [SerializeField] private Color _coreColor = new Color(1f, 0.86f, 0.35f, 1f);
    [SerializeField] private Color _sparkColor = new Color(1f, 0.48f, 0.14f, 1f);
    [SerializeField] private Color _magicColor = new Color(0.55f, 0.2f, 1f, 1f);
    [SerializeField] private Color _smokeColor = new Color(0.24f, 0.12f, 0.08f, 0.38f);

    [Header("Juice")]
    [SerializeField, Range(0.25f, 2.5f)] private float _sparkleIntensity = 1.25f;
    [SerializeField, Range(1f, 6f)] private float _hdrGlowIntensity = 2.8f;
    [SerializeField] private bool _useSmoke = true;
    [SerializeField] private Color _glitterColor = new Color(1f, 0.96f, 0.58f, 1f);
    [SerializeField] private Color _twinkleColor = new Color(0.68f, 0.92f, 1f, 1f);
    [SerializeField] private Material _particleMaterial;
    [SerializeField] private Material _sparkleMaterial;

    private ParticleSystem[] _systems;
    private Material _runtimeParticleMaterial;
    private Material _runtimeSparkleMaterial;
    private Material _runtimeFlashMaterial;
    private Texture2D _runtimeSparkleTexture;
    private Mesh _runtimeFlashMesh;
    private Mesh _runtimeSparkleMesh;
    private Mesh _runtimeRingMesh;
    private MeshRenderer _flashRenderer;
    private MeshRenderer _flashRingRenderer;
    private Transform _flashTransform;
    private Transform _flashRingTransform;
    private MaterialPropertyBlock _flashBlock;
    private float _flashStartedAt;
    private bool _flashActive;
    private bool _hasPlayed;

    private void Awake()
    {
        if (_disableExistingRootParticleSystem)
        {
            DisableExistingRootParticleSystem();
        }
    }

    private void Start()
    {
        Build();

        if (_playOnAwake && !_hasPlayed)
        {
            Play();
        }
    }

    private void LateUpdate()
    {
        UpdateVisibleFlash();
    }

    public void Play()
    {
        if (_systems == null || _systems.Length == 0)
        {
            Build();
        }

        _hasPlayed = true;
        if (_showVisibilityFlash)
        {
            PlayVisibleFlash();
        }

        for (int i = 0; i < _systems.Length; i++)
        {
            _systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _systems[i].Clear(true);
            _systems[i].Play(true);
            _systems[i].Emit(GetImmediateEmissionCount(_systems[i].name));
        }

        if (_logPlayback)
        {
            Debug.Log($"Transformation VFX playing at {transform.position} with {_systems.Length} systems", this);
        }

        if (_selfDestroy)
        {
            Destroy(gameObject, Mathf.Max(0.1f, _destroyAfter));
        }
    }

    private void Build()
    {
        if (_systems != null && _systems.Length > 0)
        {
            return;
        }

        if (_showVisibilityFlash)
        {
            CreateVisibleFlash();
        }

        _systems = _useSmoke
            ? new[]
            {
                CreateSparkleBurst(),
                CreateShootingGlitters(),
                CreateCoreFlash(),
                CreateMagicRing(),
                CreateStarPop(),
                CreateSparkleCloud(),
                CreateSparks(),
                CreateSmoke()
            }
            : new[]
        {
            CreateSparkleBurst(),
            CreateShootingGlitters(),
            CreateCoreFlash(),
            CreateMagicRing(),
            CreateStarPop(),
            CreateSparkleCloud(),
            CreateSparks(),
        };
    }

    private ParticleSystem CreateSparkleBurst()
    {
        ParticleSystem system = CreateSystem("Sparkle Burst", Vector3.up * (_heightOffset + 0.12f), true, true);

        ParticleSystem.MainModule main = system.main;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.42f, 0.78f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 4.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(_radius * 0.08f, _radius * 0.18f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(ToHdr(_glitterColor, 1.15f), ToHdr(_twinkleColor));
        main.gravityModifier = -0.06f;
        main.maxParticles = ScaledCount(180);

        ParticleSystem.EmissionModule emission = system.emission;
        emission.SetBursts(new[]
        {
            CreateBurst(0f, 82),
            CreateBurst(0.08f, 42),
            CreateBurst(0.18f, 20)
        });

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = _radius * 0.12f;

        ParticleSystem.ForceOverLifetimeModule force = system.forceOverLifetime;
        force.enabled = true;
        force.y = new ParticleSystem.MinMaxCurve(0.12f, 0.85f);

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.7f;
        noise.frequency = 2.8f;

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.05f),
            new Keyframe(0.08f, 1.35f),
            new Keyframe(0.24f, 0.38f),
            new Keyframe(0.52f, 1.05f),
            new Keyframe(1f, 0f)));

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(FadeGradient(ToHdr(_glitterColor, 1.15f), ToHdr(WithAlpha(_twinkleColor, 0f))));

        ParticleSystem.TrailModule trails = system.trails;
        trails.enabled = true;
        trails.ratio = 0.72f;
        trails.lifetime = 0.11f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(_radius * 0.018f);

        return system;
    }

    private ParticleSystem CreateShootingGlitters()
    {
        ParticleSystem system = CreateSystem("Shooting Glitters", Vector3.up * (_heightOffset + 0.06f), true, true);

        ParticleSystem.MainModule main = system.main;
        main.duration = 0.45f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.56f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.8f, 6.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(_radius * 0.035f, _radius * 0.085f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(ToHdr(_coreColor, 1.2f), ToHdr(_sparkColor));
        main.gravityModifier = 0.05f;
        main.maxParticles = ScaledCount(120);

        ParticleSystem.EmissionModule emission = system.emission;
        emission.SetBursts(new[]
        {
            CreateBurst(0.02f, 58),
            CreateBurst(0.11f, 26)
        });

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = _radius * 0.18f;

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 4.5f;

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.3f),
            new Keyframe(0.12f, 1f),
            new Keyframe(0.5f, 0.45f),
            new Keyframe(1f, 0f)));

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(FadeGradient(ToHdr(_coreColor, 1.2f), ToHdr(WithAlpha(_sparkColor, 0f))));

        ParticleSystem.TrailModule trails = system.trails;
        trails.enabled = true;
        trails.ratio = 0.95f;
        trails.lifetime = 0.16f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(_radius * 0.012f);

        return system;
    }

    private ParticleSystem CreateCoreFlash()
    {
        ParticleSystem system = CreateSystem("Core Flash", Vector3.up * _heightOffset);

        ParticleSystem.MainModule main = system.main;
        main.duration = 0.12f;
        main.startLifetime = 0.12f;
        main.startSpeed = 0f;
        main.startSize = _radius * 0.85f;
        main.startColor = new ParticleSystem.MinMaxGradient(ToHdr(Color.Lerp(_coreColor, Color.white, 0.35f), 0.85f), ToHdr(_coreColor, 0.85f));
        main.maxParticles = 6;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.SetBursts(new[]
        {
            CreateBurst(0f, 1),
            CreateBurst(0.08f, 1)
        });

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.03f;

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.12f),
            new Keyframe(0.25f, 1.35f),
            new Keyframe(1f, 0.03f)));

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(FadeGradient(ToHdr(_coreColor, 0.85f), ToHdr(WithAlpha(_coreColor, 0f), 0.85f)));

        return system;
    }

    private ParticleSystem CreateMagicRing()
    {
        ParticleSystem system = CreateSystem("Arcane Ring", Vector3.up * (_heightOffset * 0.72f), true, true);

        ParticleSystem.MainModule main = system.main;
        main.duration = 0.34f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.4f, 4.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(_radius * 0.04f, _radius * 0.09f);
        main.startColor = new ParticleSystem.MinMaxGradient(ToHdr(_magicColor, 0.95f), ToHdr(_sparkColor));
        main.maxParticles = ScaledCount(150);

        ParticleSystem.EmissionModule emission = system.emission;
        emission.SetBursts(new[]
        {
            CreateBurst(0.02f, 78),
            CreateBurst(0.1f, 36)
        });

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = _radius * 0.12f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 2f;

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(FadeGradient(ToHdr(_magicColor, 0.95f), ToHdr(WithAlpha(_sparkColor, 0f))));

        ParticleSystem.TrailModule trails = system.trails;
        trails.enabled = true;
        trails.ratio = 0.85f;
        trails.lifetime = 0.12f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(_radius * 0.014f);

        return system;
    }

    private ParticleSystem CreateStarPop()
    {
        ParticleSystem system = CreateSystem("Star Pop", Vector3.up * (_heightOffset + 0.04f), true, true);

        ParticleSystem.MainModule main = system.main;
        main.duration = 0.42f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.34f, 0.62f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 3.7f);
        main.startSize = new ParticleSystem.MinMaxCurve(_radius * 0.075f, _radius * 0.16f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(ToHdr(_glitterColor, 1.1f), ToHdr(_twinkleColor));
        main.gravityModifier = -0.08f;
        main.maxParticles = ScaledCount(96);

        ParticleSystem.EmissionModule emission = system.emission;
        emission.SetBursts(new[]
        {
            CreateBurst(0.04f, 52),
            CreateBurst(0.14f, 28)
        });

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = _radius * 0.11f;

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.16f, 1.15f),
            new Keyframe(0.7f, 0.55f),
            new Keyframe(1f, 0f)));

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(FadeGradient(ToHdr(_glitterColor, 1.1f), ToHdr(WithAlpha(_twinkleColor, 0f))));

        return system;
    }

    private ParticleSystem CreateSparkleCloud()
    {
        ParticleSystem system = CreateSystem("Twinkle Sparkles", Vector3.up * (_heightOffset + 0.08f), true, true);

        ParticleSystem.MainModule main = system.main;
        main.duration = 0.85f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.42f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.9f, 2.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(_radius * 0.035f, _radius * 0.105f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(ToHdr(_twinkleColor), ToHdr(_glitterColor, 1.05f));
        main.gravityModifier = -0.08f;
        main.maxParticles = ScaledCount(130);

        ParticleSystem.EmissionModule emission = system.emission;
        emission.SetBursts(new[]
        {
            CreateBurst(0.1f, 52),
            CreateBurst(0.28f, 36),
            CreateBurst(0.48f, 22)
        });

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = _radius * 0.28f;

        ParticleSystem.ForceOverLifetimeModule force = system.forceOverLifetime;
        force.enabled = true;
        force.y = new ParticleSystem.MinMaxCurve(0.08f, 0.45f);

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.28f;
        noise.frequency = 2.4f;

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.12f, 1f),
            new Keyframe(0.36f, 0.15f),
            new Keyframe(0.62f, 0.85f),
            new Keyframe(1f, 0f)));

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(FadeGradient(ToHdr(_twinkleColor), ToHdr(WithAlpha(_glitterColor, 0f), 1.05f)));

        return system;
    }

    private ParticleSystem CreateSparks()
    {
        ParticleSystem system = CreateSystem("Rising Sparks", Vector3.up * _heightOffset, true, true);

        ParticleSystem.MainModule main = system.main;
        main.duration = 0.65f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 4.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(_radius * 0.028f, _radius * 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(ToHdr(_sparkColor), ToHdr(_coreColor, 1.1f));
        main.gravityModifier = -0.15f;
        main.maxParticles = ScaledCount(130);

        ParticleSystem.EmissionModule emission = system.emission;
        emission.SetBursts(new[]
        {
            CreateBurst(0.06f, 58),
            CreateBurst(0.18f, 28)
        });

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = _radius * 0.32f;

        ParticleSystem.ForceOverLifetimeModule force = system.forceOverLifetime;
        force.enabled = true;
        force.y = new ParticleSystem.MinMaxCurve(0.55f, 1.25f);

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.75f;
        noise.frequency = 1.6f;

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(FadeGradient(ToHdr(_sparkColor), ToHdr(WithAlpha(_coreColor, 0f), 1.1f)));

        ParticleSystem.TrailModule trails = system.trails;
        trails.enabled = true;
        trails.ratio = 0.35f;
        trails.lifetime = 0.1f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.018f);

        return system;
    }

    private ParticleSystem CreateSmoke()
    {
        ParticleSystem system = CreateSystem("Soft Smoke", Vector3.up * (_heightOffset * 0.55f));

        ParticleSystem.MainModule main = system.main;
        main.duration = 0.7f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.38f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
        main.startColor = WithAlpha(_smokeColor, _smokeColor.a * 0.55f);
        main.maxParticles = 18;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.SetBursts(new[] { CreateBurst(0.14f, 12) });

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = _radius * 0.28f;

        ParticleSystem.ForceOverLifetimeModule force = system.forceOverLifetime;
        force.enabled = true;
        force.y = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.4f;
        noise.frequency = 0.9f;

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.35f),
            new Keyframe(0.45f, 1f),
            new Keyframe(1f, 1.4f)));

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(FadeGradient(_smokeColor, WithAlpha(_smokeColor, 0f)));

        return system;
    }

    private ParticleSystem CreateSystem(string systemName, Vector3 localPosition, bool useSparkleMaterial = false, bool useSparkleMesh = false)
    {
        GameObject systemObject = new GameObject(systemName);
        systemObject.transform.SetParent(transform, false);
        systemObject.transform.localPosition = localPosition;

        ParticleSystem system = systemObject.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        system.Clear(true);

        ParticleSystem.MainModule main = system.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;

        ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
        Material material = useSparkleMesh ? GetFlashMaterial() : useSparkleMaterial ? GetSparkleMaterial() : GetParticleMaterial();
        if (material != null)
        {
            renderer.sharedMaterial = material;
            renderer.trailMaterial = material;
        }

        if (useSparkleMesh)
        {
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = GetSparkleMesh();
            renderer.sortingOrder = 18;
            SetRendererColor(renderer, GetSystemTint(systemName));
        }
        else
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = useSparkleMaterial ? 12 : 10;
        }

        return system;
    }

    private void CreateVisibleFlash()
    {
        if (_flashRenderer != null)
        {
            return;
        }

        GameObject flashObject = new GameObject("Visible Transformation Flash");
        flashObject.layer = gameObject.layer;
        flashObject.transform.SetParent(transform, false);
        flashObject.transform.localPosition = Vector3.up * (_heightOffset + 0.05f);
        flashObject.transform.localScale = Vector3.zero;

        MeshFilter filter = flashObject.AddComponent<MeshFilter>();
        filter.sharedMesh = GetFlashMesh();

        _flashRenderer = flashObject.AddComponent<MeshRenderer>();
        _flashRenderer.sharedMaterial = GetFlashMaterial();
        _flashRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _flashRenderer.receiveShadows = false;
        _flashRenderer.sortingOrder = 24;

        _flashTransform = flashObject.transform;
        _flashBlock = new MaterialPropertyBlock();
        flashObject.SetActive(false);

        GameObject ringObject = new GameObject("Visible Transformation Ring");
        ringObject.layer = gameObject.layer;
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.localPosition = Vector3.up * 0.08f;
        ringObject.transform.localScale = Vector3.zero;

        MeshFilter ringFilter = ringObject.AddComponent<MeshFilter>();
        ringFilter.sharedMesh = GetRingMesh();

        _flashRingRenderer = ringObject.AddComponent<MeshRenderer>();
        _flashRingRenderer.sharedMaterial = GetFlashMaterial();
        _flashRingRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _flashRingRenderer.receiveShadows = false;
        _flashRingRenderer.sortingOrder = 23;

        _flashRingTransform = ringObject.transform;
        ringObject.SetActive(false);
    }

    private void PlayVisibleFlash()
    {
        if (_flashRenderer == null)
        {
            return;
        }

        _flashStartedAt = Time.time;
        _flashActive = true;
        _flashRenderer.gameObject.SetActive(true);
        if (_flashRingRenderer != null)
        {
            _flashRingRenderer.gameObject.SetActive(true);
        }
        UpdateVisibleFlash();
    }

    private void UpdateVisibleFlash()
    {
        if (!_flashActive || _flashRenderer == null)
        {
            return;
        }

        const float duration = 0.42f;
        float elapsed = Mathf.Max(0f, Time.time - _flashStartedAt);
        float t = Mathf.Clamp01(elapsed / duration);

        float easeOut = 1f - Mathf.Pow(1f - t, 3f);
        float alpha = Mathf.Clamp01(Mathf.Sin(t * Mathf.PI) * 1.25f);
        float burstScale = Mathf.Lerp(_radius * 0.08f, _radius * 0.9f, easeOut);
        _flashTransform.localScale = new Vector3(burstScale, burstScale * 1.35f, burstScale);
        _flashTransform.localRotation = Quaternion.Euler(elapsed * 260f, elapsed * 420f, elapsed * 180f);

        if (_flashRingTransform != null)
        {
            float ringScale = Mathf.Lerp(_radius * 0.15f, _radius * 1.65f, easeOut);
            _flashRingTransform.localScale = new Vector3(ringScale, 1f, ringScale);
            _flashRingTransform.localRotation = Quaternion.Euler(0f, elapsed * 180f, 0f);
        }

        Color flashColor = Color.Lerp(_glitterColor, _twinkleColor, Mathf.SmoothStep(0f, 1f, t));
        flashColor.a = alpha;

        if (_flashBlock == null)
        {
            _flashBlock = new MaterialPropertyBlock();
        }

        _flashRenderer.GetPropertyBlock(_flashBlock);
        _flashBlock.SetColor("_BaseColor", flashColor);
        _flashBlock.SetColor("_Color", flashColor);
        _flashRenderer.SetPropertyBlock(_flashBlock);

        if (_flashRingRenderer != null)
        {
            Color ringColor = Color.Lerp(_magicColor, _sparkColor, Mathf.SmoothStep(0f, 1f, t));
            ringColor.a = alpha;
            _flashRingRenderer.GetPropertyBlock(_flashBlock);
            _flashBlock.SetColor("_BaseColor", ringColor);
            _flashBlock.SetColor("_Color", ringColor);
            _flashRingRenderer.SetPropertyBlock(_flashBlock);
        }

        if (t >= 1f)
        {
            _flashActive = false;
            _flashRenderer.gameObject.SetActive(false);
            if (_flashRingRenderer != null)
            {
                _flashRingRenderer.gameObject.SetActive(false);
            }
        }
    }

    private Mesh GetFlashMesh()
    {
        if (_runtimeFlashMesh != null)
        {
            return _runtimeFlashMesh;
        }

        _runtimeFlashMesh = new Mesh
        {
            name = "Runtime Transformation Flash Mesh",
            hideFlags = HideFlags.HideAndDontSave
        };

        _runtimeFlashMesh.SetVertices(new[]
        {
            new Vector3(0f, 0.65f, 0f),
            new Vector3(0f, -0.65f, 0f),
            new Vector3(-0.5f, 0f, 0f),
            new Vector3(0.5f, 0f, 0f),
            new Vector3(0f, 0f, 0.5f),
            new Vector3(0f, 0f, -0.5f)
        });

        _runtimeFlashMesh.SetUVs(0, new[]
        {
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f)
        });

        _runtimeFlashMesh.SetTriangles(new[]
        {
            0, 4, 3,
            0, 3, 5,
            0, 5, 2,
            0, 2, 4,
            1, 3, 4,
            1, 5, 3,
            1, 2, 5,
            1, 4, 2
        }, 0);
        _runtimeFlashMesh.RecalculateNormals();
        _runtimeFlashMesh.RecalculateBounds();

        return _runtimeFlashMesh;
    }

    private Mesh GetSparkleMesh()
    {
        if (_runtimeSparkleMesh != null)
        {
            return _runtimeSparkleMesh;
        }

        _runtimeSparkleMesh = new Mesh
        {
            name = "Runtime Transformation Sparkle Mesh",
            hideFlags = HideFlags.HideAndDontSave
        };

        _runtimeSparkleMesh.SetVertices(new[]
        {
            new Vector3(0f, 0.58f, 0f),
            new Vector3(0f, -0.58f, 0f),
            new Vector3(-0.18f, 0f, 0f),
            new Vector3(0.18f, 0f, 0f),
            new Vector3(0f, 0f, 0.18f),
            new Vector3(0f, 0f, -0.18f)
        });

        _runtimeSparkleMesh.SetUVs(0, new[]
        {
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f)
        });

        _runtimeSparkleMesh.SetTriangles(new[]
        {
            0, 4, 3,
            0, 3, 5,
            0, 5, 2,
            0, 2, 4,
            1, 3, 4,
            1, 5, 3,
            1, 2, 5,
            1, 4, 2
        }, 0);
        _runtimeSparkleMesh.RecalculateNormals();
        _runtimeSparkleMesh.RecalculateBounds();

        return _runtimeSparkleMesh;
    }

    private Mesh GetRingMesh()
    {
        if (_runtimeRingMesh != null)
        {
            return _runtimeRingMesh;
        }

        const int segments = 48;
        const float innerRadius = 0.78f;
        const float outerRadius = 1f;

        Vector3[] vertices = new Vector3[segments * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[segments * 12];

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);
            int outer = i * 2;
            int inner = outer + 1;

            vertices[outer] = new Vector3(cos * outerRadius, 0f, sin * outerRadius);
            vertices[inner] = new Vector3(cos * innerRadius, 0f, sin * innerRadius);
            uvs[outer] = new Vector2(1f, i / (float)segments);
            uvs[inner] = new Vector2(0f, i / (float)segments);
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            int outer = i * 2;
            int inner = outer + 1;
            int nextOuter = next * 2;
            int nextInner = nextOuter + 1;
            int triangleIndex = i * 12;

            triangles[triangleIndex] = outer;
            triangles[triangleIndex + 1] = inner;
            triangles[triangleIndex + 2] = nextOuter;
            triangles[triangleIndex + 3] = nextOuter;
            triangles[triangleIndex + 4] = inner;
            triangles[triangleIndex + 5] = nextInner;
            triangles[triangleIndex + 6] = nextOuter;
            triangles[triangleIndex + 7] = inner;
            triangles[triangleIndex + 8] = outer;
            triangles[triangleIndex + 9] = nextInner;
            triangles[triangleIndex + 10] = inner;
            triangles[triangleIndex + 11] = nextOuter;
        }

        _runtimeRingMesh = new Mesh
        {
            name = "Runtime Transformation Ring Mesh",
            hideFlags = HideFlags.HideAndDontSave
        };
        _runtimeRingMesh.vertices = vertices;
        _runtimeRingMesh.uv = uvs;
        _runtimeRingMesh.triangles = triangles;
        _runtimeRingMesh.RecalculateNormals();
        _runtimeRingMesh.RecalculateBounds();

        return _runtimeRingMesh;
    }

    private Material GetFlashMaterial()
    {
        if (_runtimeFlashMaterial != null)
        {
            return _runtimeFlashMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            return null;
        }

        _runtimeFlashMaterial = new Material(shader)
        {
            name = "Runtime Transformation Flash Material",
            hideFlags = HideFlags.HideAndDontSave,
            renderQueue = (int)RenderQueue.GeometryLast
        };

        Color visibleColor = ToHdr(new Color(1f, 0.92f, 0.15f, 1f), 1.2f);
        if (_runtimeFlashMaterial.HasProperty("_BaseColor"))
        {
            _runtimeFlashMaterial.SetColor("_BaseColor", visibleColor);
        }

        if (_runtimeFlashMaterial.HasProperty("_Color"))
        {
            _runtimeFlashMaterial.SetColor("_Color", visibleColor);
        }

        if (_runtimeFlashMaterial.HasProperty("_EmissionColor"))
        {
            _runtimeFlashMaterial.SetColor("_EmissionColor", visibleColor);
            _runtimeFlashMaterial.EnableKeyword("_EMISSION");
        }

        if (_runtimeFlashMaterial.HasProperty("_Surface"))
        {
            _runtimeFlashMaterial.SetFloat("_Surface", 0f);
        }

        if (_runtimeFlashMaterial.HasProperty("_Cull"))
        {
            _runtimeFlashMaterial.SetFloat("_Cull", 0f);
        }

        return _runtimeFlashMaterial;
    }

    private Material GetSparkleMaterial()
    {
        if (_sparkleMaterial != null)
        {
            return _sparkleMaterial;
        }

        if (_runtimeSparkleMaterial != null)
        {
            return _runtimeSparkleMaterial;
        }

        Material baseMaterial = GetParticleMaterial();
        if (baseMaterial != null)
        {
            _runtimeSparkleMaterial = new Material(baseMaterial);
        }
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                return null;
            }

            _runtimeSparkleMaterial = new Material(shader);
        }

        _runtimeSparkleMaterial.name = "Runtime Transformation Sparkle Material";
        _runtimeSparkleMaterial.hideFlags = HideFlags.HideAndDontSave;

        _runtimeSparkleTexture = CreateSparkleTexture();
        if (_runtimeSparkleMaterial.HasProperty("_BaseMap"))
        {
            _runtimeSparkleMaterial.SetTexture("_BaseMap", _runtimeSparkleTexture);
        }

        if (_runtimeSparkleMaterial.HasProperty("_MainTex"))
        {
            _runtimeSparkleMaterial.SetTexture("_MainTex", _runtimeSparkleTexture);
        }

        if (_runtimeSparkleMaterial.HasProperty("_BaseColor"))
        {
            _runtimeSparkleMaterial.SetColor("_BaseColor", Color.white);
        }

        if (_runtimeSparkleMaterial.HasProperty("_Color"))
        {
            _runtimeSparkleMaterial.SetColor("_Color", Color.white);
        }

        return _runtimeSparkleMaterial;
    }

    private Color GetSystemTint(string systemName)
    {
        switch (systemName)
        {
            case "Sparkle Burst":
            case "Star Pop":
                return ToHdr(Color.Lerp(_glitterColor, Color.white, 0.25f), 1.15f);
            case "Shooting Glitters":
            case "Rising Sparks":
                return ToHdr(Color.Lerp(_sparkColor, _coreColor, 0.3f), 1.1f);
            case "Arcane Ring":
                return ToHdr(Color.Lerp(_magicColor, _twinkleColor, 0.25f));
            case "Twinkle Sparkles":
                return ToHdr(_twinkleColor);
            default:
                return ToHdr(_coreColor);
        }
    }

    private Color ToHdr(Color color, float multiplier = 1f)
    {
        float intensity = Mathf.Max(1f, _hdrGlowIntensity * multiplier);
        color.r *= intensity;
        color.g *= intensity;
        color.b *= intensity;
        return color;
    }

    private static void SetRendererColor(Renderer renderer, Color color)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        block.SetColor("_EmissionColor", color);
        renderer.SetPropertyBlock(block);
    }

    private Material GetParticleMaterial()
    {
        if (_particleMaterial != null)
        {
            return _particleMaterial;
        }

        if (_runtimeParticleMaterial != null)
        {
            return _runtimeParticleMaterial;
        }

        ParticleSystemRenderer rootRenderer = GetComponent<ParticleSystemRenderer>();
        if (rootRenderer != null && rootRenderer.sharedMaterial != null)
        {
            return rootRenderer.sharedMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            return null;
        }

        _runtimeParticleMaterial = new Material(shader)
        {
            name = "Runtime Transformation Particle Material",
            hideFlags = HideFlags.HideAndDontSave
        };

        if (_runtimeParticleMaterial.HasProperty("_BaseColor"))
        {
            _runtimeParticleMaterial.SetColor("_BaseColor", Color.white);
        }

        if (_runtimeParticleMaterial.HasProperty("_Color"))
        {
            _runtimeParticleMaterial.SetColor("_Color", Color.white);
        }

        return _runtimeParticleMaterial;
    }

    private void OnDestroy()
    {
        DestroyRuntimeAsset(_runtimeFlashMesh);
        DestroyRuntimeAsset(_runtimeSparkleMesh);
        DestroyRuntimeAsset(_runtimeRingMesh);
        DestroyRuntimeAsset(_runtimeFlashMaterial);
        DestroyRuntimeAsset(_runtimeParticleMaterial);
        DestroyRuntimeAsset(_runtimeSparkleMaterial);
        DestroyRuntimeAsset(_runtimeSparkleTexture);
    }

    private void DisableExistingRootParticleSystem()
    {
        ParticleSystem rootSystem = GetComponent<ParticleSystem>();
        if (rootSystem != null)
        {
            rootSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            rootSystem.Clear(true);

            ParticleSystem.MainModule main = rootSystem.main;
            main.playOnAwake = false;
        }

        ParticleSystemRenderer rootRenderer = GetComponent<ParticleSystemRenderer>();
        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
        }
    }

    private static Gradient FadeGradient(Color start, Color end)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(start, 0f),
                new GradientColorKey(end, 1f)
            },
            new[]
            {
                new GradientAlphaKey(start.a, 0f),
                new GradientAlphaKey(end.a, 1f)
            });

        return gradient;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private short ScaledCount(int count)
    {
        return (short)Mathf.Clamp(Mathf.RoundToInt(count * Mathf.Max(0.05f, _sparkleIntensity)), 1, short.MaxValue);
    }

    private ParticleSystem.Burst CreateBurst(float time, int count)
    {
        return new ParticleSystem.Burst(time, ScaledCount(count));
    }

    private int GetImmediateEmissionCount(string systemName)
    {
        switch (systemName)
        {
            case "Sparkle Burst":
                return ScaledCount(90);
            case "Shooting Glitters":
                return ScaledCount(52);
            case "Core Flash":
                return 1;
            case "Arcane Ring":
                return ScaledCount(56);
            case "Star Pop":
                return ScaledCount(48);
            case "Twinkle Sparkles":
                return ScaledCount(52);
            case "Rising Sparks":
                return ScaledCount(44);
            case "Soft Smoke":
                return _useSmoke ? 8 : 0;
            default:
                return 0;
        }
    }

    private static Texture2D CreateSparkleTexture()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime Sparkle Particle Texture",
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = ((x + 0.5f) / size) * 2f - 1f;
                float py = ((y + 0.5f) / size) * 2f - 1f;
                float absX = Mathf.Abs(px);
                float absY = Mathf.Abs(py);
                float distance = Mathf.Sqrt(px * px + py * py);

                float core = 1f - Mathf.SmoothStep(0f, 0.22f, distance);
                float horizontal = (1f - Mathf.SmoothStep(0f, 0.055f, absY)) * (1f - Mathf.SmoothStep(0.15f, 1f, absX));
                float vertical = (1f - Mathf.SmoothStep(0f, 0.055f, absX)) * (1f - Mathf.SmoothStep(0.15f, 1f, absY));
                float diagonalA = (1f - Mathf.SmoothStep(0f, 0.07f, Mathf.Abs(px - py))) * (1f - Mathf.SmoothStep(0.05f, 1f, distance)) * 0.45f;
                float diagonalB = (1f - Mathf.SmoothStep(0f, 0.07f, Mathf.Abs(px + py))) * (1f - Mathf.SmoothStep(0.05f, 1f, distance)) * 0.45f;
                float alpha = Mathf.Max(core, Mathf.Max(horizontal, Mathf.Max(vertical, Mathf.Max(diagonalA, diagonalB))));
                alpha *= 1f - Mathf.SmoothStep(0.86f, 1f, distance);
                alpha = Mathf.Clamp01(alpha);

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
        }

        texture.Apply(false, true);
        return texture;
    }

    private static void DestroyRuntimeAsset(Object asset)
    {
        if (asset == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(asset);
        }
        else
        {
            DestroyImmediate(asset);
        }
    }
}

[DisallowMultipleComponent]
internal sealed class RuntimeCaptureFeedbackVfxFallback : MonoBehaviour
{
    [SerializeField] private Color _sparkColor = new Color(1f, 0.72f, 0.24f, 1f);
    [SerializeField] private Color _flashColor = new Color(1f, 0.93f, 0.48f, 0.92f);
    [SerializeField] private Color _dustColor = new Color(0.5f, 0.25f, 0.12f, 0.6f);
    [SerializeField] private float _destroyAfter = 1.25f;
    [SerializeField] private float _heightOffset = 0.22f;
    [SerializeField] private float _radius = 0.48f;
    [SerializeField] private bool _useEditableChildSystems = true;
    [SerializeField] private bool _buildFallbackIfNoChildSystems = true;
    [SerializeField] private bool _scaleEditablePrefabWithIntensity = true;

    private const float ImpactFlashDuration = 0.38f;

    private ParticleSystem _sparkBurst;
    private ParticleSystem _sparkRing;
    private ParticleSystem _dustBurst;
    private MeshRenderer _flashRenderer;
    private MeshRenderer _ringRenderer;
    private Transform _flashTransform;
    private Transform _ringTransform;
    private MaterialPropertyBlock _propertyBlock;
    private ParticleSystem[] _editableSystems;
    private float _impactFlashStartedAt;
    private bool _impactFlashActive;
    private bool _usingEditableSystems;

    private static Material _sparkMaterial;
    private static Material _dustMaterial;
    private static Mesh _sparkMesh;
    private static Mesh _flashMesh;
    private static Mesh _ringMesh;
    private static Texture2D _sparkTexture;
    private static Texture2D _dustTexture;

    private void Awake()
    {
        EnsureSystems();
    }

    private void LateUpdate()
    {
        UpdateImpactFlash();
    }

    public void Play(float intensity = 1f)
    {
        EnsureSystems();

        intensity = Mathf.Max(0.2f, intensity);
        transform.position += Vector3.up * _heightOffset;

        if (_usingEditableSystems)
        {
            PlayEditableSystems(intensity);
            Destroy(gameObject, _destroyAfter);
            return;
        }

        if (_sparkBurst == null)
        {
            Destroy(gameObject, _destroyAfter);
            return;
        }

        PlayImpactFlash();
        PlaySystem(_sparkBurst, Mathf.RoundToInt(42 * intensity));
        PlaySystem(_sparkRing, Mathf.RoundToInt(28 * intensity));
        PlaySystem(_dustBurst, Mathf.RoundToInt(16 * intensity));

        Destroy(gameObject, _destroyAfter);
    }

    private void EnsureSystems()
    {
        if (_sparkBurst != null || _usingEditableSystems)
            return;

        if (TryUseEditableChildSystems())
            return;

        if (!_buildFallbackIfNoChildSystems)
            return;

        CreateImpactFlash();
        _sparkBurst = CreateSparkBurst();
        _sparkRing = CreateSparkRing();
        _dustBurst = CreateDustBurst();
    }

    private bool TryUseEditableChildSystems()
    {
        if (!_useEditableChildSystems)
            return false;

        _editableSystems = GetComponentsInChildren<ParticleSystem>(true);
        if (_editableSystems == null || _editableSystems.Length == 0)
            return false;

        _usingEditableSystems = true;
        return true;
    }

    private void PlayEditableSystems(float intensity)
    {
        if (_scaleEditablePrefabWithIntensity)
        {
            transform.localScale *= Mathf.Sqrt(intensity);
        }

        for (int i = 0; i < _editableSystems.Length; i++)
        {
            ParticleSystem system = _editableSystems[i];
            if (system == null)
                continue;

            system.gameObject.SetActive(true);
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.Clear(true);
            system.Play(true);
        }
    }

    private ParticleSystem CreateSparkBurst()
    {
        ParticleSystem system = CreateChildSystem("Capture Shards", _sparkColor, true);

        ParticleSystem.MainModule main = system.main;
        main.duration = 0.58f;
        main.maxParticles = 110;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.46f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.6f, 6.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.11f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0.18f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = _radius * 0.14f;

        ParticleSystem.ForceOverLifetimeModule force = system.forceOverLifetime;
        force.enabled = true;
        force.y = new ParticleSystem.MinMaxCurve(0.12f, 0.55f);

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.28f;
        noise.frequency = 3.5f;

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.22f),
            new Keyframe(0.12f, 1.2f),
            new Keyframe(0.55f, 0.5f),
            new Keyframe(1f, 0f)));

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(CreateFadeGradient(Color.Lerp(_sparkColor, Color.white, 0.18f), WithAlpha(_sparkColor, 0f)));

        ParticleSystem.TrailModule trails = system.trails;
        trails.enabled = true;
        trails.ratio = 0.82f;
        trails.lifetime = 0.11f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.018f);

        return system;
    }

    private ParticleSystem CreateSparkRing()
    {
        ParticleSystem system = CreateChildSystem("Capture Ring Shards", _flashColor, true);

        ParticleSystem.MainModule main = system.main;
        main.duration = 0.5f;
        main.maxParticles = 80;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.52f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.9f, 3.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.085f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = _radius * 0.58f;
        shape.radiusThickness = 0.08f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.45f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.strength = 0.18f;
        noise.frequency = 2.2f;

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.1f),
            new Keyframe(0.18f, 1f),
            new Keyframe(0.72f, 0.48f),
            new Keyframe(1f, 0f)));

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(CreateFadeGradient(_flashColor, WithAlpha(_sparkColor, 0f)));

        ParticleSystem.TrailModule trails = system.trails;
        trails.enabled = true;
        trails.ratio = 0.9f;
        trails.lifetime = 0.1f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.014f);

        return system;
    }

    private ParticleSystem CreateDustBurst()
    {
        ParticleSystem system = CreateChildSystem("Capture Soft Dust", _dustColor, false);

        ParticleSystem.MainModule main = system.main;
        main.duration = 0.75f;
        main.maxParticles = 36;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.42f, 0.78f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = -0.04f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = _radius * 0.42f;

        ParticleSystem.ForceOverLifetimeModule force = system.forceOverLifetime;
        force.enabled = true;
        force.y = new ParticleSystem.MinMaxCurve(0.08f, 0.32f);

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.32f, 1f),
            new Keyframe(1f, 1.25f)));

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(CreateFadeGradient(_dustColor, WithAlpha(_dustColor, 0f)));

        return system;
    }

    private ParticleSystem CreateChildSystem(string systemName, Color startColor, bool useSparkMesh)
    {
        GameObject child = new GameObject(systemName);
        child.transform.SetParent(transform, false);

        ParticleSystem system = child.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        system.Clear(true);

        ParticleSystem.MainModule main = system.main;
        main.loop = false;
        main.duration = 0.65f;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startColor = startColor;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = false;
        emission.rateOverTime = 0f;

        ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
        Material material = useSparkMesh ? GetSparkMaterial() : GetDustMaterial();
        if (material != null)
        {
            renderer.sharedMaterial = material;
            renderer.trailMaterial = material;
        }

        if (useSparkMesh)
        {
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = GetSparkMesh();
            renderer.sortingOrder = 16;
        }
        else
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 8;
        }

        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        return system;
    }

    private void CreateImpactFlash()
    {
        Material material = GetSparkMaterial();
        if (material == null)
            return;

        GameObject flashObject = new GameObject("Capture Impact Flash");
        flashObject.transform.SetParent(transform, false);
        flashObject.transform.localPosition = Vector3.zero;
        flashObject.transform.localScale = Vector3.zero;

        MeshFilter flashFilter = flashObject.AddComponent<MeshFilter>();
        flashFilter.sharedMesh = GetFlashMesh();

        _flashRenderer = flashObject.AddComponent<MeshRenderer>();
        _flashRenderer.sharedMaterial = material;
        _flashRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _flashRenderer.receiveShadows = false;
        _flashRenderer.sortingOrder = 20;
        _flashTransform = flashObject.transform;
        flashObject.SetActive(false);

        GameObject ringObject = new GameObject("Capture Impact Ring");
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.localPosition = Vector3.up * 0.04f;
        ringObject.transform.localScale = Vector3.zero;

        MeshFilter ringFilter = ringObject.AddComponent<MeshFilter>();
        ringFilter.sharedMesh = GetRingMesh();

        _ringRenderer = ringObject.AddComponent<MeshRenderer>();
        _ringRenderer.sharedMaterial = material;
        _ringRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _ringRenderer.receiveShadows = false;
        _ringRenderer.sortingOrder = 19;
        _ringTransform = ringObject.transform;
        ringObject.SetActive(false);

        _propertyBlock = new MaterialPropertyBlock();
    }

    private void PlayImpactFlash()
    {
        if (_flashRenderer == null)
            return;

        _impactFlashStartedAt = Time.time;
        _impactFlashActive = true;
        _flashRenderer.gameObject.SetActive(true);

        if (_ringRenderer != null)
        {
            _ringRenderer.gameObject.SetActive(true);
        }

        UpdateImpactFlash();
    }

    private void UpdateImpactFlash()
    {
        if (!_impactFlashActive || _flashRenderer == null)
            return;

        float elapsed = Mathf.Max(0f, Time.time - _impactFlashStartedAt);
        float t = Mathf.Clamp01(elapsed / ImpactFlashDuration);
        float easeOut = 1f - Mathf.Pow(1f - t, 3f);
        float alpha = Mathf.Clamp01(Mathf.Sin(t * Mathf.PI) * 1.15f);

        float flashScale = Mathf.Lerp(_radius * 0.12f, _radius * 0.95f, easeOut);
        _flashTransform.localScale = new Vector3(flashScale, flashScale * 1.15f, flashScale);
        _flashTransform.localRotation = Quaternion.Euler(elapsed * 420f, elapsed * 280f, elapsed * 360f);

        Color flashColor = Color.Lerp(_flashColor, Color.white, 0.2f);
        flashColor.a *= alpha;
        ApplyRendererColor(_flashRenderer, flashColor);

        if (_ringRenderer != null)
        {
            float ringScale = Mathf.Lerp(_radius * 0.22f, _radius * 1.75f, easeOut);
            _ringTransform.localScale = new Vector3(ringScale, 1f, ringScale);
            _ringTransform.localRotation = Quaternion.Euler(0f, elapsed * 210f, 0f);

            Color ringColor = Color.Lerp(_sparkColor, _flashColor, Mathf.SmoothStep(0f, 1f, t));
            ringColor.a *= alpha * (1f - t);
            ApplyRendererColor(_ringRenderer, ringColor);
        }

        if (t >= 1f)
        {
            _impactFlashActive = false;
            _flashRenderer.gameObject.SetActive(false);

            if (_ringRenderer != null)
            {
                _ringRenderer.gameObject.SetActive(false);
            }
        }
    }

    private void ApplyRendererColor(Renderer renderer, Color color)
    {
        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor("_BaseColor", color);
        _propertyBlock.SetColor("_Color", color);
        _propertyBlock.SetColor("_EmissionColor", color);
        renderer.SetPropertyBlock(_propertyBlock);
    }

    private static void PlaySystem(ParticleSystem system, int count)
    {
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        system.Play(true);
        system.Emit(count);
    }

    private static Material GetSparkMaterial()
    {
        if (_sparkMaterial != null)
            return _sparkMaterial;

        _sparkMaterial = CreateParticleMaterial("Runtime Capture Spark Material", GetSparkTexture());
        return _sparkMaterial;
    }

    private static Material GetDustMaterial()
    {
        if (_dustMaterial != null)
            return _dustMaterial;

        _dustMaterial = CreateParticleMaterial("Runtime Capture Dust Material", GetDustTexture());
        return _dustMaterial;
    }

    private static Material CreateParticleMaterial(string materialName, Texture2D texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            return null;

        Material material = new Material(shader)
        {
            name = materialName,
            hideFlags = HideFlags.HideAndDontSave,
            renderQueue = (int)RenderQueue.Transparent
        };

        if (texture != null)
        {
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
        }

        Color color = Color.white;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color);
            material.EnableKeyword("_EMISSION");
        }

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", 0f);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return material;
    }

    private static Mesh GetSparkMesh()
    {
        if (_sparkMesh != null)
            return _sparkMesh;

        _sparkMesh = new Mesh
        {
            name = "Runtime Capture Spark Mesh",
            hideFlags = HideFlags.HideAndDontSave
        };

        _sparkMesh.SetVertices(new[]
        {
            new Vector3(0f, 0.62f, 0f),
            new Vector3(0f, -0.62f, 0f),
            new Vector3(-0.18f, 0f, 0f),
            new Vector3(0.18f, 0f, 0f),
            new Vector3(0f, 0f, 0.18f),
            new Vector3(0f, 0f, -0.18f)
        });

        _sparkMesh.SetUVs(0, new[]
        {
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f)
        });

        _sparkMesh.SetTriangles(new[]
        {
            0, 4, 3,
            0, 3, 5,
            0, 5, 2,
            0, 2, 4,
            1, 3, 4,
            1, 5, 3,
            1, 2, 5,
            1, 4, 2
        }, 0);
        _sparkMesh.RecalculateNormals();
        _sparkMesh.RecalculateBounds();

        return _sparkMesh;
    }

    private static Mesh GetFlashMesh()
    {
        if (_flashMesh != null)
            return _flashMesh;

        _flashMesh = new Mesh
        {
            name = "Runtime Capture Flash Mesh",
            hideFlags = HideFlags.HideAndDontSave
        };

        _flashMesh.SetVertices(new[]
        {
            new Vector3(0f, 0.66f, 0f),
            new Vector3(0f, -0.66f, 0f),
            new Vector3(-0.5f, 0f, 0f),
            new Vector3(0.5f, 0f, 0f),
            new Vector3(0f, 0f, 0.5f),
            new Vector3(0f, 0f, -0.5f)
        });

        _flashMesh.SetTriangles(new[]
        {
            0, 4, 3,
            0, 3, 5,
            0, 5, 2,
            0, 2, 4,
            1, 3, 4,
            1, 5, 3,
            1, 2, 5,
            1, 4, 2
        }, 0);
        _flashMesh.RecalculateNormals();
        _flashMesh.RecalculateBounds();

        return _flashMesh;
    }

    private static Mesh GetRingMesh()
    {
        if (_ringMesh != null)
            return _ringMesh;

        const int segments = 56;
        const float innerRadius = 0.78f;
        const float outerRadius = 1f;

        Vector3[] vertices = new Vector3[segments * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[segments * 6];

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);
            int outer = i * 2;
            int inner = outer + 1;

            vertices[outer] = new Vector3(cos * outerRadius, 0f, sin * outerRadius);
            vertices[inner] = new Vector3(cos * innerRadius, 0f, sin * innerRadius);
            uvs[outer] = new Vector2(1f, i / (float)segments);
            uvs[inner] = new Vector2(0f, i / (float)segments);
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            int outer = i * 2;
            int inner = outer + 1;
            int nextOuter = next * 2;
            int nextInner = nextOuter + 1;
            int triangleIndex = i * 6;

            triangles[triangleIndex] = outer;
            triangles[triangleIndex + 1] = inner;
            triangles[triangleIndex + 2] = nextOuter;
            triangles[triangleIndex + 3] = nextOuter;
            triangles[triangleIndex + 4] = inner;
            triangles[triangleIndex + 5] = nextInner;
        }

        _ringMesh = new Mesh
        {
            name = "Runtime Capture Ring Mesh",
            hideFlags = HideFlags.HideAndDontSave
        };
        _ringMesh.vertices = vertices;
        _ringMesh.uv = uvs;
        _ringMesh.triangles = triangles;
        _ringMesh.RecalculateNormals();
        _ringMesh.RecalculateBounds();

        return _ringMesh;
    }

    private static Texture2D GetSparkTexture()
    {
        if (_sparkTexture != null)
            return _sparkTexture;

        const int size = 32;
        _sparkTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime Capture Spark Texture",
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = ((x + 0.5f) / size) * 2f - 1f;
                float py = ((y + 0.5f) / size) * 2f - 1f;
                float absX = Mathf.Abs(px);
                float absY = Mathf.Abs(py);
                float distance = Mathf.Sqrt(px * px + py * py);
                float core = 1f - Mathf.SmoothStep(0f, 0.22f, distance);
                float horizontal = (1f - Mathf.SmoothStep(0f, 0.055f, absY)) * (1f - Mathf.SmoothStep(0.1f, 1f, absX));
                float vertical = (1f - Mathf.SmoothStep(0f, 0.055f, absX)) * (1f - Mathf.SmoothStep(0.1f, 1f, absY));
                float alpha = Mathf.Max(core, Mathf.Max(horizontal, vertical));
                alpha *= 1f - Mathf.SmoothStep(0.84f, 1f, distance);
                alpha = Mathf.Clamp01(alpha);

                _sparkTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
        }

        _sparkTexture.Apply(false, true);
        return _sparkTexture;
    }

    private static Texture2D GetDustTexture()
    {
        if (_dustTexture != null)
            return _dustTexture;

        const int size = 32;
        _dustTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime Capture Dust Texture",
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = ((x + 0.5f) / size) * 2f - 1f;
                float py = ((y + 0.5f) / size) * 2f - 1f;
                float distance = Mathf.Sqrt(px * px + py * py);
                float alpha = 1f - Mathf.SmoothStep(0.1f, 1f, distance);
                alpha *= alpha;

                _dustTexture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
            }
        }

        _dustTexture.Apply(false, true);
        return _dustTexture;
    }

    private static Gradient CreateFadeGradient(Color start, Color end)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(start, 0f),
                new GradientColorKey(Color.Lerp(start, Color.white, 0.22f), 0.22f),
                new GradientColorKey(end, 1f)
            },
            new[]
            {
                new GradientAlphaKey(start.a, 0f),
                new GradientAlphaKey(start.a * 0.95f, 0.35f),
                new GradientAlphaKey(end.a, 1f)
            });

        return gradient;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
