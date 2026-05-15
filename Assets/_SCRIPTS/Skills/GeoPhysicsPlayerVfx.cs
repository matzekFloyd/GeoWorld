using UnityEngine;

/// <summary>
/// Subtle passive VFX while GeoPhysics is active (<see cref="PlayerCharacter.skillAvailable"/>(1)):
/// low-rate body aura + throttled foot dust when grounded and moving.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public sealed class GeoPhysicsPlayerVfx : SkillBasic
{
    static int GeoPhysicsSkillSlotLevel => GameBalanceHelper.SkillUnlockGeoPhysics;

    [Header("Foot dust")]
    [SerializeField] float footDustMinHorizontalSpeed = 0.35f;
    [SerializeField] float footDustEmitInterval = 0.16f;
    [SerializeField] int footDustEmitCount = 2;
    [SerializeField] float dustCheckInterval = 0.1f;

    [Header("Aura (scales slightly with level)")]
    [SerializeField] float auraEmissionBase = 14f;
    [SerializeField] float auraEmissionPerLevel = 0.35f;
    [SerializeField] float auraEmissionReducedMotionScale = 0.45f;

    GeoPhysicsPlayerVfxBootstrap _bootstrap;
    CharacterController _characterController;
    GameObject _vfxRoot;
    ParticleSystem _bodyAura;
    ParticleSystem _footDust;
    bool _wasActive;
    int _lastAuraLevel = -1;
    float _nextDustCheckTime;
    float _nextDustEmitTime;

    public static void EnsureOn(GameObject playerRoot)
    {
        if (playerRoot == null)
            return;

        if (playerRoot.GetComponent<GeoPhysicsPlayerVfx>() != null)
            return;

        if (playerRoot.GetComponent<GeoPhysicsPlayerVfxBootstrap>() == null)
            playerRoot.AddComponent<GeoPhysicsPlayerVfxBootstrap>();

        playerRoot.AddComponent<GeoPhysicsPlayerVfx>();
    }

    void Start()
    {
        _bootstrap = GetComponent<GeoPhysicsPlayerVfxBootstrap>();
        _characterController = GetComponent<CharacterController>();
        if (_bootstrap != null)
        {
            _vfxRoot = _bootstrap.VfxRoot;
            _bodyAura = _bootstrap.BodyAura;
            _footDust = _bootstrap.FootDust;
        }

        RefreshActiveState(force: true);
    }

    void Update()
    {
        bool active = IsPassiveActive();
        if (active != _wasActive)
            RefreshActiveState(force: true);

        if (!active)
            return;

        int level = m_Player.getCurLevel();
        if (level != _lastAuraLevel)
        {
            _lastAuraLevel = level;
            ApplyAuraEmissionForLevel();
        }

        if (Time.time >= _nextDustCheckTime)
        {
            _nextDustCheckTime = Time.time + dustCheckInterval;
            TryEmitFootDust();
        }
    }

    void OnDisable()
    {
        SetVfxRootActive(false);
        _wasActive = false;
    }

    bool IsPassiveActive()
    {
        return m_Player != null && m_Player.skillAvailable(GeoPhysicsSkillSlotLevel);
    }

    void RefreshActiveState(bool force)
    {
        bool active = IsPassiveActive();
        if (!force && active == _wasActive)
            return;

        _wasActive = active;
        SetVfxRootActive(active);
        if (active)
        {
            _lastAuraLevel = m_Player != null ? m_Player.getCurLevel() : -1;
            ApplyAuraEmissionForLevel();
        }
        else
        {
            _lastAuraLevel = -1;
        }
    }

    void SetVfxRootActive(bool active)
    {
        if (_vfxRoot == null)
            return;

        if (_vfxRoot.activeSelf == active)
            return;

        _vfxRoot.SetActive(active);
        if (active)
            PooledVfxSpawnReset.Apply(_vfxRoot);
    }

    void ApplyAuraEmissionForLevel()
    {
        if (_bodyAura == null || m_Player == null)
            return;

        float rate = auraEmissionBase + m_Player.getCurLevel() * auraEmissionPerLevel;
        if (CombatFeedback.ReducedMotion)
            rate *= auraEmissionReducedMotionScale;

        var em = _bodyAura.emission;
        em.rateOverTime = rate;
    }

    void TryEmitFootDust()
    {
        if (_footDust == null || _characterController == null)
            return;

        if (CombatFeedback.ReducedMotion)
            return;

        if (!_characterController.isGrounded)
            return;

        var vel = _characterController.velocity;
        var horizontal = new Vector3(vel.x, 0f, vel.z);
        if (horizontal.sqrMagnitude < footDustMinHorizontalSpeed * footDustMinHorizontalSpeed)
            return;

        float interval = footDustEmitInterval;
        if (Time.time < _nextDustEmitTime)
            return;

        _nextDustEmitTime = Time.time + interval;
        _footDust.Emit(footDustEmitCount);
    }
}
