using UnityEngine;

/// <summary>
/// Editor menu or first runtime spawn builds a small VFX hierarchy for <see cref="GeoShotProjectile"/>:
/// root <see cref="TrailRenderer"/>, child glow <see cref="ParticleSystem"/>, optional point light.
/// Works with <see cref="GeoWorldObjectPools.Acquire"/> + <see cref="PooledVfxSpawnReset.Apply"/> (particles replay)
/// and <see cref="PooledTrailClearOnEnable"/> (trail history cleared on each spawn).
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public sealed class GeoShotProjectileVfxBootstrap : MonoBehaviour
{
    public const string VfxRootName = "GeoShot_VfxRoot";
    const string GlowChildName = "GlowParticles";
    const string LightChildName = "MuzzleGlowLight";

    [Header("Look (edit after menu bake)")]
    [SerializeField] Color _trailColor = new Color(0.35f, 0.92f, 1f, 0.85f);
    [SerializeField] Color _glowTint = new Color(0.4f, 0.95f, 1f, 1f);
    [SerializeField] Color _lightColor = new Color(1f, 0.88f, 0.65f, 1f);
    [SerializeField] float _lightIntensity = 1.35f;
    [SerializeField] float _lightRange = 3.8f;

    [SerializeField] TrailRenderer _trail;
    [SerializeField] ParticleSystem _glowParticles;
    [SerializeField] Light _pointLight;

    void Awake()
    {
        BuildVfxFull();
    }

    void BindSerializedRefs()
    {
        var vfxRoot = transform.Find(VfxRootName);
        if (vfxRoot != null)
        {
            var glowT = vfxRoot.Find(GlowChildName);
            if (glowT != null)
                _glowParticles = glowT.GetComponent<ParticleSystem>();
            var lightT = vfxRoot.Find(LightChildName);
            if (lightT != null)
                _pointLight = lightT.GetComponent<Light>();
        }

        if (_trail == null)
            _trail = GetComponent<TrailRenderer>();
    }

#if UNITY_EDITOR
    /// <summary>Called from Editor tooling to rewrite the prefab hierarchy.</summary>
    public void RebuildForPrefab()
    {
        StripBuiltVfx();
        BuildVfxFull();
        BindSerializedRefs();
    }

    void StripBuiltVfx()
    {
        var existingRoot = transform.Find(VfxRootName);
        if (existingRoot != null)
            DestroyImmediate(existingRoot.gameObject);

        var tr = GetComponent<TrailRenderer>();
        if (tr != null)
            DestroyImmediate(tr);

        var trailClear = GetComponent<PooledTrailClearOnEnable>();
        if (trailClear != null)
            DestroyImmediate(trailClear);
    }
#endif

    void BuildVfxFull()
    {
        if (GetComponent<PooledTrailClearOnEnable>() == null)
            gameObject.AddComponent<PooledTrailClearOnEnable>();

        _trail = GetComponent<TrailRenderer>();
        if (_trail == null)
        {
            _trail = gameObject.AddComponent<TrailRenderer>();
            ConfigureTrail(_trail);
        }

        if (transform.Find(VfxRootName) != null)
        {
            BindSerializedRefs();
            return;
        }

        var vfxRoot = new GameObject(VfxRootName);
        vfxRoot.transform.SetParent(transform, false);
        vfxRoot.transform.localPosition = Vector3.zero;
        vfxRoot.transform.localRotation = Quaternion.identity;
        vfxRoot.transform.localScale = Vector3.one;

        var glowGo = new GameObject(GlowChildName);
        glowGo.transform.SetParent(vfxRoot.transform, false);
        glowGo.transform.localPosition = Vector3.zero;
        _glowParticles = glowGo.AddComponent<ParticleSystem>();
        ConfigureGlow(_glowParticles);

        var lightGo = new GameObject(LightChildName);
        lightGo.transform.SetParent(vfxRoot.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 0f, 0.15f);
        _pointLight = lightGo.AddComponent<Light>();
        _pointLight.type = LightType.Point;
        _pointLight.color = _lightColor;
        _pointLight.intensity = _lightIntensity;
        _pointLight.range = _lightRange;
        _pointLight.shadows = LightShadows.None;
        _pointLight.renderMode = LightRenderMode.Auto;
    }

    void ConfigureTrail(TrailRenderer tr)
    {
        tr.time = 0.32f;
        tr.minVertexDistance = 0.035f;
        tr.numCornerVertices = 3;
        tr.numCapVertices = 3;
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tr.receiveShadows = false;
        tr.generateLightingData = false;
        tr.autodestruct = false;
        tr.emitting = true;

        var w = tr.widthCurve = new AnimationCurve();
        w.AddKey(0f, 0.14f);
        w.AddKey(1f, 0.02f);

        var g = tr.colorGradient = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(_trailColor, 0f), new GradientColorKey(_trailColor, 1f) },
            new[] { new GradientAlphaKey(_trailColor.a, 0f), new GradientAlphaKey(0f, 1f) });

        var trailMat = new Material(ResolveTrailShader());
        trailMat.color = Color.white;
        tr.material = trailMat;
    }

    void ConfigureGlow(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 0.35f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.45f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
        main.startColor = _glowTint;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 64;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var em = ps.emission;
        em.rateOverTime = 38f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.11f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(_glowTint, 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.material = CreateParticleMaterial();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        rend.sortingFudge = -2f;
    }

    static Shader ResolveTrailShader()
    {
        string[] candidates =
        {
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Sprites/Default",
            "Unlit/Transparent",
            "Legacy Shaders/Particles/Additive"
        };
        foreach (var path in candidates)
        {
            var s = Shader.Find(path);
            if (s != null)
                return s;
        }

        return Shader.Find("Sprites/Default");
    }

    static Material CreateParticleMaterial()
    {
        string[] candidates =
        {
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Legacy Shaders/Particles/Additive",
            "Particles/Alpha Blended",
            "Sprites/Default"
        };
        foreach (var path in candidates)
        {
            var s = Shader.Find(path);
            if (s != null)
                return new Material(s);
        }

        return new Material(Shader.Find("Sprites/Default"));
    }
}
