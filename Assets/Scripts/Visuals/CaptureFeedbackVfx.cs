using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class CaptureFeedbackVfx : MonoBehaviour
{
    [SerializeField] private Color _sparkColor = new Color(1f, 0.72f, 0.24f, 1f);
    [SerializeField] private Color _flashColor = new Color(1f, 0.93f, 0.48f, 0.92f);
    [SerializeField] private Color _dustColor = new Color(0.5f, 0.25f, 0.12f, 0.6f);
    [SerializeField] private float _destroyAfter = 1.55f;
    [SerializeField] private float _heightOffset = 0.14f;
    [SerializeField] private float _radius = 0.48f;
    [SerializeField] private bool _useEditableChildSystems = true;
    [SerializeField] private bool _buildFallbackIfNoChildSystems = true;
    [SerializeField] private bool _scaleEditablePrefabWithIntensity = true;

    private ParticleSystem[] _editableSystems;
    private static Material _fallbackMaterial;

    private void Awake()
    {
        CacheEditableSystems();
    }

    public void Play(float intensity = 1f)
    {
        intensity = Mathf.Max(0.2f, intensity);
        transform.position += Vector3.up * _heightOffset;

        if (!HasEditableSystems() && _buildFallbackIfNoChildSystems)
        {
            BuildFallbackSystems();
        }

        if (_scaleEditablePrefabWithIntensity)
        {
            transform.localScale *= Mathf.Sqrt(intensity);
        }

        PlaySystems();
        Destroy(gameObject, Mathf.Max(0.1f, _destroyAfter));
    }

    private bool HasEditableSystems()
    {
        if (_editableSystems != null && _editableSystems.Length > 0)
            return true;

        return CacheEditableSystems();
    }

    private bool CacheEditableSystems()
    {
        if (!_useEditableChildSystems)
            return false;

        _editableSystems = GetComponentsInChildren<ParticleSystem>(true);
        return _editableSystems != null && _editableSystems.Length > 0;
    }

    private void PlaySystems()
    {
        if (_editableSystems == null)
            return;

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

    private void BuildFallbackSystems()
    {
        if (_editableSystems != null && _editableSystems.Length > 0)
            return;

        CreateFallbackSystem("Fallback Capture Sparks", _sparkColor, 48, 4.8f, 0.09f, true);
        CreateFallbackSystem("Fallback Capture Dust", _dustColor, 18, 1.1f, 0.24f, false);
        _editableSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void CreateFallbackSystem(string systemName, Color startColor, short burstCount, float speed, float size, bool useSphere)
    {
        GameObject child = new GameObject(systemName);
        child.transform.SetParent(transform, false);

        ParticleSystem system = child.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        system.Clear(true);

        ParticleSystem.MainModule main = system.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.75f;
        main.startLifetime = useSphere
            ? new ParticleSystem.MinMaxCurve(0.24f, 0.52f)
            : new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        main.startSpeed = useSphere
            ? new ParticleSystem.MinMaxCurve(speed * 0.75f, speed * 1.2f)
            : new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.65f, size * 1.35f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = startColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.gravityModifier = useSphere ? 0.12f : -0.04f;
        main.maxParticles = Mathf.Max(24, burstCount * 2);

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burstCount) });

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = useSphere ? ParticleSystemShapeType.Sphere : ParticleSystemShapeType.Hemisphere;
        shape.radius = useSphere ? _radius * 0.14f : _radius * 0.42f;

        ParticleSystem.SizeOverLifetimeModule sizeOverLife = system.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.18f),
            new Keyframe(0.16f, 1f),
            new Keyframe(1f, useSphere ? 0f : 1.2f)));

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(CreateFadeGradient(startColor, WithAlpha(startColor, 0f)));

        ParticleSystem.TrailModule trails = system.trails;
        trails.enabled = useSphere;
        trails.ratio = 0.75f;
        trails.lifetime = 0.12f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.018f);

        ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = GetFallbackMaterial();
        renderer.trailMaterial = renderer.sharedMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = useSphere ? 16 : 8;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static Material GetFallbackMaterial()
    {
        if (_fallbackMaterial != null)
            return _fallbackMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        _fallbackMaterial = new Material(shader)
        {
            name = "Runtime Capture Feedback Fallback Material",
            hideFlags = HideFlags.HideAndDontSave,
            renderQueue = (int)RenderQueue.Transparent
        };

        return _fallbackMaterial;
    }

    private static Gradient CreateFadeGradient(Color start, Color end)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(start, 0f),
                new GradientColorKey(Color.Lerp(start, Color.white, 0.2f), 0.25f),
                new GradientColorKey(end, 1f)
            },
            new[]
            {
                new GradientAlphaKey(start.a, 0f),
                new GradientAlphaKey(start.a * 0.9f, 0.35f),
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
