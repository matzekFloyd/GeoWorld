using UnityEngine;

/// <summary>
/// Run-wide malus/bonus from deaths, normal-kill recovery, and boss kills. Respawn uses the player
/// transform captured on <see cref="Start"/> (run start position in <c>GeoWorldMain</c>).
/// </summary>
[DisallowMultipleComponent]
public sealed class RunModifiers : MonoBehaviour
{
    public static RunModifiers Instance { get; private set; }

    [SerializeField] PlayerCharacter _player;

    Vector3 _runStartPosition;
    Quaternion _runStartRotation;
    bool _runStartCaptured;

    int _deathCount;
    int _respawnsUsed;
    float _malusPercent;
    float _bossBonusPercent;
    float _timeBonusPercent;
    int _normalKillsTowardMalusRecovery;
    int _lastTimeBonusMinuteGranted = -1;
    float _runElapsedSeconds;

    public int DeathCount => _deathCount;
    public int RespawnsUsed => _respawnsUsed;
    public int RespawnsRemaining => Mathf.Max(0, GameBalanceHelper.MaxRespawnsPerRun - _respawnsUsed);
    public float MalusPercent => _malusPercent;
    public float BossBonusPercent => _bossBonusPercent;
    public float TimeBonusPercent => _timeBonusPercent;
    public float BonusPercent => _bossBonusPercent + _timeBonusPercent;
    /// <summary>Single HUD value: bonus − malus (e.g. malus 40% and bonus 15% → −25%).</summary>
    public float NetModifierPercent => BonusPercent - _malusPercent;

    public float StatMultiplier
    {
        get
        {
            float mult = 1f + NetModifierPercent / 100f;
            return Mathf.Max(0.05f, mult);
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        if (_player == null)
            _player = GetComponent<PlayerCharacter>();
    }

    /// <summary>Resolves the player singleton when kills fire before other Awakes (defensive).</summary>
    public static RunModifiers Resolve()
    {
        if (Instance != null)
            return Instance;
        var p = GameObject.FindGameObjectWithTag("Player1");
        return p != null ? p.GetComponent<RunModifiers>() : null;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        CaptureRunStartPose();
    }

    void Update()
    {
        TickTimeBonus();
    }

    void TickTimeBonus()
    {
        var session = GameSession.Instance;
        if (session == null || !session.IsRunActive)
            return;

        _runElapsedSeconds += Time.deltaTime;
        int minutes = Mathf.FloorToInt(_runElapsedSeconds / 60f);
        if (minutes <= _lastTimeBonusMinuteGranted)
            return;

        _lastTimeBonusMinuteGranted = minutes;
        if (minutes <= 0)
            return;

        float delta = GameBalanceHelper.TimeBonusPercentPerMinute;
        _timeBonusPercent = minutes * delta;
        BattleLog.AppendRunBonusGained(delta, BonusPercent, $"{minutes} min survived");
        NotifyModifiersChanged();
    }

    void CaptureRunStartPose()
    {
        if (_player == null)
            return;
        _runStartPosition = _player.transform.position;
        _runStartRotation = _player.transform.rotation;
        _runStartCaptured = true;
    }

    public float ScaleOutgoingDamage(float baseDamage)
    {
        if (baseDamage <= 0f)
            return baseDamage;
        return baseDamage * StatMultiplier;
    }

    /// <summary>Returns false when the run should end (no respawns left).</summary>
    public bool TryRespawnAfterDeath(GameOver gameOver)
    {
        if (_player == null)
            return false;

        _deathCount++;
        if (gameOver != null)
            gameOver.deathCounter++;

        if (_respawnsUsed >= GameBalanceHelper.MaxRespawnsPerRun)
        {
            BattleLog.AppendLifeLost(0);
            return false;
        }

        _respawnsUsed++;
        BattleLog.AppendLifeLost(RespawnsRemaining + 1);
        ApplyDeathModifiers();
        PerformRespawn();
        return true;
    }

    void ApplyDeathModifiers()
    {
        bool hadBossBonus = _bossBonusPercent > 0.001f;
        float add = hadBossBonus
            ? GameBalanceHelper.DeathMalusPercentWhenBonusActive
            : GameBalanceHelper.DeathMalusPercentPerDeath;
        _malusPercent = Mathf.Min(100f, _malusPercent + add);
        BattleLog.AppendRunMalusGained(add, _malusPercent, hadBossBonus ? "death while boss bonus active" : "death");
        if (hadBossBonus)
        {
            float lostBossBonus = _bossBonusPercent;
            _bossBonusPercent = 0f;
            BattleLog.AppendRunBonusLost(lostBossBonus, "death");
        }
        NotifyModifiersChanged();
    }

    void PerformRespawn()
    {
        if (!_runStartCaptured)
            CaptureRunStartPose();

        var t = _player.transform;
        var cc = t.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        t.SetPositionAndRotation(_runStartPosition, _runStartRotation);

        if (cc != null)
            cc.enabled = true;

        var rb = t.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        _player.RestoreResourcesAfterRespawn();
    }

    /// <summary>Called when an enemy dies. Greater enemies do not count toward malus recovery.</summary>
    public void RegisterEnemyDefeated(bool isBoss, bool isGreaterEnemy)
    {
        if (isBoss)
        {
            float delta = GameBalanceHelper.BossKillBonusPercent;
            _bossBonusPercent = Mathf.Min(200f, _bossBonusPercent + delta);
            BattleLog.AppendRunBonusGained(delta, BonusPercent, "boss defeated");
            NotifyModifiersChanged();
            return;
        }

        if (isGreaterEnemy)
            return;

        _normalKillsTowardMalusRecovery++;
        int need = GameBalanceHelper.GetNormalKillsRequiredForMalusRecovery(_player.getCurLevel());
        bool recovered = false;
        float recoveryTick = GameBalanceHelper.MalusRecoveryPercentPerTick;
        while (_normalKillsTowardMalusRecovery >= need && _malusPercent > 0.001f)
        {
            _normalKillsTowardMalusRecovery -= need;
            _malusPercent = Mathf.Max(0f, _malusPercent - recoveryTick);
            BattleLog.AppendRunMalusRecovered(recoveryTick, _malusPercent);
            recovered = true;
        }

        if (recovered)
            NotifyModifiersChanged();
    }

    void NotifyModifiersChanged()
    {
        if (_player != null)
            _player.ApplyModifierStatChange();
    }

    public string GetNetModifierHudLabel()
    {
        float net = NetModifierPercent;
        if (Mathf.Abs(net) < 0.5f)
            return "0%";
        return (net > 0f ? "+" : "") + Mathf.RoundToInt(net) + "%";
    }
}
