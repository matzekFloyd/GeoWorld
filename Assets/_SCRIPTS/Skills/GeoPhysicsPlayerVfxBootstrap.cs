using UnityEngine;

/// <summary>
/// Builds a small always-on / foot-dust particle rig for <see cref="GeoPhysicsPlayerVfx"/>.
/// Hierarchy: <see cref="VfxRootName"/> → BodyAura, FootDust.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public sealed class GeoPhysicsPlayerVfxBootstrap : MonoBehaviour
{
    public const string VfxRootName = "GeoPhysics_VfxRoot";
    const string BodyAuraChildName = "BodyAura";
    const string FootDustChildName = "FootDust";

    [Header("Look (edit after menu bake)")]
    [SerializeField] Color _auraTint = new Color(0.35f, 0.92f, 1f, 0.55f);
    [SerializeField] Color _dustTint = new Color(0.72f, 0.68f, 0.62f, 0.55f);
    [SerializeField] Vector3 _vfxLocalOffset = new Vector3(0f, 0.12f, 0f);

    [SerializeField] GameObject _vfxRoot;
    [SerializeField] ParticleSystem _bodyAura;
    [SerializeField] ParticleSystem _footDust;

    public GameObject VfxRoot => _vfxRoot;
    public ParticleSystem BodyAura => _bodyAura;
    public ParticleSystem FootDust => _footDust;

    void Awake()
    {
        BuildVfxFull();
        BindSerializedRefs();
        RefreshExistingLook();
    }

    void BindSerializedRefs()
    {
        if (_vfxRoot == null)
        {
            var t = transform.Find(VfxRootName);
            if (t != null)
                _vfxRoot = t.gameObject;
        }

        if (_vfxRoot == null)
            return;

        if (_bodyAura == null)
        {
            var auraT = _vfxRoot.transform.Find(BodyAuraChildName);
            if (auraT != null)
                _bodyAura = auraT.GetComponent<ParticleSystem>();
        }

        if (_footDust == null)
        {
            var dustT = _vfxRoot.transform.Find(FootDustChildName);
            if (dustT != null)
                _footDust = dustT.GetComponent<ParticleSystem>();
        }
    }

#if UNITY_EDITOR
    public void RebuildForSceneObject()
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

        _vfxRoot = null;
        _bodyAura = null;
        _footDust = null;
    }
#endif

    void BuildVfxFull()
    {
        if (transform.Find(VfxRootName) != null)
        {
            BindSerializedRefs();
            return;
        }

        _vfxRoot = new GameObject(VfxRootName);
        _vfxRoot.transform.SetParent(transform, false);
        _vfxRoot.transform.localPosition = _vfxLocalOffset;
        _vfxRoot.transform.localRotation = Quaternion.identity;
        _vfxRoot.transform.localScale = Vector3.one;
        _vfxRoot.SetActive(false);

        if (_vfxRoot.GetComponent<PooledVfxResetOnEnable>() == null)
            _vfxRoot.AddComponent<PooledVfxResetOnEnable>();

        var auraGo = new GameObject(BodyAuraChildName);
        auraGo.transform.SetParent(_vfxRoot.transform, false);
        auraGo.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        _bodyAura = auraGo.AddComponent<ParticleSystem>();
        ConfigureBodyAura(_bodyAura);

        var dustGo = new GameObject(FootDustChildName);
        dustGo.transform.SetParent(_vfxRoot.transform, false);
        dustGo.transform.localPosition = Vector3.zero;
        _footDust = dustGo.AddComponent<ParticleSystem>();
        ConfigureFootDust(_footDust);
    }

    void ConfigureBodyAura(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.18f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        main.startColor = _auraTint;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 28;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var em = ps.emission;
        em.rateOverTime = 14f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.22f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(_auraTint, 0f), new GradientColorKey(_auraTint, 1f) },
            new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        ApplyAuraRenderer(ps);
    }

    void ConfigureFootDust(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startColor = _dustTint;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 24;
        main.gravityModifier = 0.35f;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.rateOverDistance = 0f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.08f;
        shape.rotation = new Vector3(180f, 0f, 0f);

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.65f),
            new Keyframe(0.35f, 1f),
            new Keyframe(1f, 0.15f)));

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        var dustGrad = new Gradient();
        dustGrad.SetKeys(
            new[] { new GradientColorKey(_dustTint, 0f), new GradientColorKey(_dustTint, 1f) },
            new[] { new GradientAlphaKey(0.65f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLife.color = new ParticleSystem.MinMaxGradient(dustGrad);

        ApplyFootDustRenderer(ps);
    }

    void RefreshExistingLook()
    {
        if (_bodyAura != null)
        {
            ConfigureBodyAura(_bodyAura);
            ApplyAuraRenderer(_bodyAura);
        }

        if (_footDust != null)
        {
            ConfigureFootDust(_footDust);
            ApplyFootDustRenderer(_footDust);
        }
    }

    static void ApplyAuraRenderer(ParticleSystem ps)
    {
        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.material = GeoVfxSoftParticleMaterialUtil.AuraMaterial;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        rend.sortingFudge = -1f;
    }

    static void ApplyFootDustRenderer(ParticleSystem ps)
    {
        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.material = GeoVfxSoftParticleMaterialUtil.DustMaterial;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        rend.sortingFudge = 0f;
    }
}
