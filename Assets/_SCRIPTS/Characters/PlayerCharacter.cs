using UnityEngine;
using System.Collections;

public class PlayerCharacter : BaseCharacter {

    /// <summary>With Geo Mania, HP may exceed <see cref="maxHealth"/> up to this multiple (overheal).</summary>
    public const float GeoManiaOverhealMaxMultiplier = 1.5f;

    /// <summary>Blood Ritual may push <see cref="curMana"/> up to this × <see cref="maxMana"/> (overmana).</summary>
    public const float BloodRitualOvermanaMaxMultiplier = 1.5f;

    private GameObject enemy;
    RunModifiers _runModifiers;

    public float curMana;
    public float maxMana;

    public float curExp;
    public float expNeededForLevelUp;

    private GameObject enemyGenerator;

    [Header("Health regen (out of combat)")]
    [Tooltip("Seconds with no incoming damage before passive HP regen can begin.")]
    [SerializeField] float healthRegenLockoutAfterHitSeconds = 3f;

    [Tooltip("After lockout, seconds of not being hit to approach full regen rate.")]
    [SerializeField] float healthRegenRampToFullSeconds = 18f;

    [Tooltip("Minimum HP/s right when lockout ends (before ramp).")]
    [SerializeField] float healthRegenPerSecondFloor = 0.18f;

    [Tooltip("Added to floor/scaling: maxHealth × this per second at full ramp.")]
    [SerializeField] float healthRegenMaxFractionOfMaxPerSecond = 0.006f;

    [Tooltip("Extra HP/s at full ramp from player level (full rate up to soft cap).")]
    [SerializeField] float healthRegenPerSecondPerLevelAtFull = 0.42f;

    [Tooltip("Above this level, per-level regen contribution is reduced to limit late-game kiting.")]
    [SerializeField] int healthRegenPerLevelSoftCapLevel = 45;

    [SerializeField, Range(0.2f, 1f)]
    float healthRegenPerLevelAboveSoftCapMultiplier = 0.4f;

    /// <summary>Time.time of last enemy/missile damage via <see cref="ApplyIncomingDamage"/>. Negative = never damaged this run.</summary>
    float _lastIncomingDamageTime = -1f;

    /// <summary>Time.time of last mana spend (negative cost via <see cref="changeCurrentMana"/>). Negative = no spend yet this run.</summary>
    float _lastManaSpendTime = -1f;

    [Header("Overheal decay (Geo Mania)")]
    [Tooltip("HP removed per second only from HP above nominal max; stops when at or below maxHealth.")]
    [SerializeField] float overhealDegenerationPerSecond = 4f;

    [Header("Overmana decay (Blood Ritual)")]
    [Tooltip("Mana removed per second only from mana above nominal max; stops when at or below maxMana.")]
    [SerializeField] float overmanaDegenerationPerSecond = 4f;




    void Awake()
    {
        _runModifiers = GetComponent<RunModifiers>();
        if (_runModifiers == null)
            _runModifiers = gameObject.AddComponent<RunModifiers>();
    }

    // Use this for initialization
    void Start () {

        if (GetComponent<PlayerFirstPersonRunGate>() == null)
        {
            if (GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>() != null
                || GetComponent<UnityStandardAssets.Characters.FirstPerson.RigidbodyFirstPersonController>() != null)
                gameObject.AddComponent<PlayerFirstPersonRunGate>();
        }

        enemyGenerator = GameObject.FindGameObjectWithTag("Spawn");
        if (enemyGenerator == null)
            Debug.LogWarning("GeoWorld: No GameObject with tag 'Spawn' found. Enemy wave scaling (LevelUp → EnemyGenerator) will not work.");
        setInitialPlayerStatistics();
    }

    /// <summary>Enemy/missile damage with world origin for directional HUD feedback.</summary>
    /// <param name="rollEnemyCrit">When true, may crit if player level ≥ <see cref="EnemyCritHelper.MinPlayerLevelForEnemyCrit"/>.</param>
    /// <param name="enemyAttackerIsBossTier">Higher crit chance (bosses, big missiles, etc.).</param>
    public void ApplyIncomingDamage(float amount, Vector3 worldSource, bool hasWorldSource, CombatHitSeverity severity, bool rollEnemyCrit = false, bool enemyAttackerIsBossTier = false)
    {
        if (amount <= 0f)
            return;
        float dealt = Mathf.Abs(amount);
        bool enemyCrit = rollEnemyCrit && EnemyCritHelper.TryApplyEnemyCritAgainstPlayer(this, ref dealt, enemyAttackerIsBossTier);
        ApplyHealthChange(-dealt);
        _lastIncomingDamageTime = Time.time;
        var fx = CombatFeedback.Instance;
        if (fx != null)
            fx.NotifyPlayerDamaged(dealt, worldSource, hasWorldSource, severity, enemyCrit);
    }

    public float GetEffectiveMaxHealth() => maxHealth * GetStatMultiplier();

    public float GetEffectiveMaxMana() => maxMana * GetStatMultiplier();

    public float ScaleOutgoingDamage(float baseDamage) =>
        _runModifiers != null ? _runModifiers.ScaleOutgoingDamage(baseDamage) : baseDamage;

    float GetStatMultiplier() => _runModifiers != null ? _runModifiers.StatMultiplier : 1f;

    /// <summary>Re-clamp HP/mana when malus/bonus changes mid-run (e.g. after kills or death).</summary>
    public void ApplyModifierStatChange()
    {
        curHealth = Mathf.Min(curHealth, GetHealthUpperClamp());
        curMana = Mathf.Min(curMana, GetManaUpperClamp());
    }

    protected override float GetHealthUpperClamp()
    {
        float nominal = GetEffectiveMaxHealth();
        if (skillAvailable(GameBalanceHelper.SkillUnlockGeoMania))
            return nominal * GeoManiaOverhealMaxMultiplier;
        return nominal;
    }

    float GetManaUpperClamp() => GetEffectiveMaxMana() * BloodRitualOvermanaMaxMultiplier;

    private void setInitialPlayerStatistics()
    {
        curExp = 0;
        curLevel = 1;
        maxLevel = GameBalanceHelper.MaxPlayerLevel;
        expNeededForLevelUp = GameBalanceHelper.GetExpRequiredAtLevel(1);
        maxHealth = 110;
        curHealth = maxHealth;
        baseHealthRegeneration = 0f;
        maxMana = 75;
        curMana = maxMana;

    }
	
	// Update is called once per frame
	void Update () {

        changeCurrentHealth(0);
        float manaRegen = calculateManaRegeneration(curLevel)
            * GetManaRegenIdleMultiplier()
            * GetStatMultiplier();
        changeCurrentMana(manaRegen * Time.deltaTime);

        regnerateHealth(GetPassiveHealthRegenPerSecond() * GetStatMultiplier() * Time.deltaTime);

        ApplyOverhealDecay();
        ApplyOvermanaDecay();

        //FÜR TESTZWECKE
        if (GameInput.DebugInstantLevelUpUp && GameSession.Instance != null && GameSession.Instance.IsRunActive)
        {
                LevelUp();
        }
    }

    public void AddExp(float expValue)
    {
        curExp += expValue;

        CalculateLevel();
    }

    public void CalculateLevel()
    {
        if(curExp >= expNeededForLevelUp) 
        {
            LevelUp();
        }
    }

    public void LevelUp()
    {
        if(curLevel < maxLevel)
        {
            const float nominalEpsilon = 0.5f;
            float effHpBefore = GetEffectiveMaxHealth();
            float effMpBefore = GetEffectiveMaxMana();
            bool hadOverheal = curHealth > effHpBefore + nominalEpsilon;
            bool hadOvermana = curMana > effMpBefore + nominalEpsilon;
            float overhealFractionAboveNominal = hadOverheal && effHpBefore > 0.001f
                ? (curHealth - effHpBefore) / effHpBefore
                : 0f;
            float overmanaFractionAboveNominal = hadOvermana && effMpBefore > 0.001f
                ? (curMana - effMpBefore) / effMpBefore
                : 0f;

            curLevel += 1;
            BattleLog.AppendPlayerLevelUp(curLevel, maxLevel);

            maxHealth += GameBalanceHelper.RollLevelUpMaxHealthDelta(curLevel);
            maxMana += GameBalanceHelper.RollLevelUpMaxManaDelta(curLevel);

            ApplyLevelUpResourceRestore(
                hadOverheal,
                hadOvermana,
                overhealFractionAboveNominal,
                overmanaFractionAboveNominal);

            curExp = 0;

            expNeededForLevelUp = GameBalanceHelper.GetExpRequiredAtLevel(curLevel);
                                
        }

        //Wenn der Spieler ein Level aufsteigt -> passe die Werte aller Gegner an
        EnemyGenerator gen = enemyGenerator.GetComponent<EnemyGenerator>();
        if (gen == null) return;

        for (int i = 0; i < gen.targets.Count; i++)
        {
            gen.targets[i].GetComponent<EnemyCharacter>().setEnemyStatistics(this.curLevel, this.expNeededForLevelUp);
        }
        gen.state = EnemyGenerator.State.SpawnEnemy;

     }

    /// <summary>Fills HP/mana to 100% of new nominal max; keeps overheal/overmana ratio if it was active before the level-up.</summary>
    void ApplyLevelUpResourceRestore(
        bool hadOverheal,
        bool hadOvermana,
        float overhealFractionAboveNominal,
        float overmanaFractionAboveNominal)
    {
        float effHp = GetEffectiveMaxHealth();
        float effMp = GetEffectiveMaxMana();
        curHealth = effHp;
        curMana = effMp;

        if (hadOverheal && skillAvailable(GameBalanceHelper.SkillUnlockGeoMania) && overhealFractionAboveNominal > 0f)
        {
            curHealth = Mathf.Min(
                GetHealthUpperClamp(),
                effHp + effHp * overhealFractionAboveNominal);
        }

        if (hadOvermana && overmanaFractionAboveNominal > 0f)
        {
            curMana = Mathf.Min(
                GetManaUpperClamp(),
                effMp + effMp * overmanaFractionAboveNominal);
        }
    }

    /// <summary>After respawn: fill to effective max; preserve overheal/overmana ratios like level-up.</summary>
    public void RestoreResourcesAfterRespawn()
    {
        const float nominalEpsilon = 0.5f;
        bool hadOverheal = curHealth > GetEffectiveMaxHealth() + nominalEpsilon;
        bool hadOvermana = curMana > GetEffectiveMaxMana() + nominalEpsilon;
        float effHp = GetEffectiveMaxHealth();
        float effMp = GetEffectiveMaxMana();
        float overhealFraction = hadOverheal && effHp > 0.001f
            ? (curHealth - effHp) / effHp
            : 0f;
        float overmanaFraction = hadOvermana && effMp > 0.001f
            ? (curMana - effMp) / effMp
            : 0f;

        ApplyLevelUpResourceRestore(hadOverheal, hadOvermana, overhealFraction, overmanaFraction);
        _lastIncomingDamageTime = -1f;
    }

    public void regnerateHealth(float valueHealthRegenaration)
    {
        if (valueHealthRegenaration <= 0f)
            return;
        float room = GetEffectiveMaxHealth() - curHealth;
        if (room <= 0f)
            return;
        changeCurrentHealth(Mathf.Min(valueHealthRegenaration, room));
    }

    void ApplyOverhealDecay()
    {
        if (overhealDegenerationPerSecond <= 0f || !skillAvailable(GameBalanceHelper.SkillUnlockGeoMania))
            return;
        float overhead = curHealth - GetEffectiveMaxHealth();
        if (overhead <= 0.0001f)
            return;
        float remove = overhealDegenerationPerSecond * Time.deltaTime;
        changeCurrentHealth(-Mathf.Min(overhead, remove));
    }

    void ApplyOvermanaDecay()
    {
        if (overmanaDegenerationPerSecond <= 0f)
            return;
        float overhead = curMana - GetEffectiveMaxMana();
        if (overhead <= 0.0001f)
            return;
        float remove = overmanaDegenerationPerSecond * Time.deltaTime;
        curMana -= Mathf.Min(overhead, remove);
        if (curMana < 0f)
            curMana = 0f;
    }

    /// <summary>Mana from Blood Ritual only: can exceed <see cref="maxMana"/> up to <see cref="BloodRitualOvermanaMaxMultiplier"/>.</summary>
    public void AddManaFromBloodRitual(float amount)
    {
        if (amount <= 0f || maxMana < 0.0001f)
            return;
        curMana = Mathf.Min(curMana + amount, GetManaUpperClamp());
    }

    public void changeCurrentMana(float change)
    {
        if (change < 0f)
        {
            _lastManaSpendTime = Time.time;
            curMana += change;
            if (curMana < 0f)
                curMana = 0f;
        }
        else if (change > 0f)
        {
            float effMax = GetEffectiveMaxMana();
            if (curMana >= effMax)
                return;
            curMana = Mathf.Min(curMana + change, effMax);
        }

        if (maxMana < 1f)
            maxMana = 1f;
    }

    /// <summary>Passive regen applies tiny per-frame gains; keep those off the crosshair readout.</summary>
    const float HealReadoutMinPositiveChunk = 0.45f;

    public override void changeCurrentHealth(float change)
    {
        base.changeCurrentHealth(change);
        if (change >= HealReadoutMinPositiveChunk)
            GameplayHudView.Instance?.NotifyRecentHeal(change);
    }

    public float calculateManaRegeneration(int curLevel) =>
        GameBalanceHelper.GetManaRegenerationPerSecond(curLevel);

    /// <summary>Multiplier applied to base mana regen; rises the longer the player has not spent mana (see <see cref="changeCurrentMana"/> negative deltas).</summary>
    public float GetManaRegenIdleMultiplier()
    {
        float maxM = GameBalanceHelper.ManaRegenMaxMultiplierAtFullIdle;
        float rampSec = GameBalanceHelper.ManaRegenRampIdleSeconds;
        if (rampSec <= 0.001f)
            return maxM;

        float sinceSpend = _lastManaSpendTime < 0f
            ? rampSec
            : Time.time - _lastManaSpendTime;
        float u = Mathf.Clamp01(sinceSpend / rampSec);
        u = u * u * (3f - 2f * u);
        return Mathf.Lerp(1f, maxM, u);
    }

    /// <summary>Passive HP/s from staying safe (0 during post-hit lockout, then ramps up).</summary>
    public float GetPassiveHealthRegenPerSecond()
    {
        float now = Time.time;
        if (_lastIncomingDamageTime < 0f)
            return GetPeakPassiveHealthRegenPerSecond();

        float sinceHit = now - _lastIncomingDamageTime;
        if (sinceHit < healthRegenLockoutAfterHitSeconds)
            return 0f;

        float rampT = sinceHit - healthRegenLockoutAfterHitSeconds;
        float u = healthRegenRampToFullSeconds > 0.001f
            ? Mathf.Clamp01(rampT / healthRegenRampToFullSeconds)
            : 1f;
        u = u * u * (3f - 2f * u);
        float minR = healthRegenPerSecondFloor + maxHealth * 0.00035f;
        float maxR = GetPeakPassiveHealthRegenPerSecond();
        return Mathf.Lerp(minR, maxR, u);
    }

    /// <summary>HP/s when fully ramped; used for GeoPhysics HUD and internal cap.</summary>
    public float GetPeakPassiveHealthRegenPerSecond()
    {
        return maxHealth * healthRegenMaxFractionOfMaxPerSecond
               + GetPassiveRegenLevelContribution(curLevel);
    }

    float GetPassiveRegenLevelContribution(int level)
    {
        int lv = Mathf.Max(0, level);
        int cap = Mathf.Max(1, healthRegenPerLevelSoftCapLevel);
        if (lv <= cap)
            return lv * healthRegenPerSecondPerLevelAtFull;

        float full = cap * healthRegenPerSecondPerLevelAtFull;
        float tail = (lv - cap) * healthRegenPerSecondPerLevelAtFull * healthRegenPerLevelAboveSoftCapMultiplier;
        return full + tail;
    }

    public bool skillAvailable(int levelNeeded)
    {
        return curLevel >= levelNeeded;
    }

    //GETTER + SETTER METHODEN
    public float getCurMana()
    {
        return curMana;
    }

    public float getMaxMana()
    {
        return GetEffectiveMaxMana();
    }

    public new float getMaxHealth()
    {
        return GetEffectiveMaxHealth();
    }

    public float getCurExp()
    {
        return curExp;
    }

    public float getExpNeededForLevelUp()
    {
        return expNeededForLevelUp;
    }
}




