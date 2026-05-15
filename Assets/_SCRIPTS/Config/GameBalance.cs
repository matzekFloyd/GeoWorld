using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Central tuning data. Create via Assets → Create → GeoWorld → Game Balance,
/// then assign it on the GameObject that has <see cref="GameOver"/> (same object as the round UI is fine).
/// If unassigned, <see cref="GameBalanceHelper"/> uses conservative built-in defaults for spawn counts and kill sustain.
/// </summary>
[CreateAssetMenu(fileName = "GameBalance", menuName = "GeoWorld/Game Balance", order = 0)]
public class GameBalance : ScriptableObject
{
    [Header("Round")]
    [Tooltip("Countdown in seconds until GeoWorld win condition.")]
    public float roundDurationSeconds = 1200f;

    [Header("Progression")]
    [Tooltip("Maximum player level for a single run.")]
    public int maxPlayerLevel = 100;

    // Playtest targets (#101): ~level 22–28 by 15:00 and ~28–36 by 20:00 on a typical kill pace
    // (tune xpPerLevelSquaredCoefficient / soft cap if runs feel starved or spike too hard).

    [Tooltip("XP required while at level 1 to reach level 2.")]
    public float firstLevelExpToReachLevel2 = 100f;

    [Tooltip("After level 2, XP to advance ≈ (current level)² × this (soft-capped toward max level).")]
    public float xpPerLevelSquaredCoefficient = 48f;

    [Tooltip("From this level onward, XP requirements ease off so level 100 stays reachable in long runs.")]
    public int xpSoftCapStartLevel = 32;

    [Tooltip("Level at which the XP soft cap reaches xpSoftCapMinFactorAtMaxLevel.")]
    public int xpSoftCapEndLevel = 100;

    [Range(0.3f, 1f)]
    [Tooltip("Multiplier on XP requirement at xpSoftCapEndLevel (1 = no reduction).")]
    public float xpSoftCapMinFactorAtMaxLevel = 0.52f;

    [Tooltip("Unlock levels: GeoShot, GeoBlast, GeoPhysics, Heal, Meteor, Blood Ritual, Freeze Time, Geo Mania.")]
    public int[] skillUnlockLevels = { 1, 1, 1, 4, 8, 12, 16, 20 };

    [Header("Lives & run modifiers (#103)")]
    [Tooltip("Respawns allowed before final death (2 = up to 2 deaths with respawn, 3rd death ends the run).")]
    public int maxRespawnsPerRun = 2;

    [Tooltip("Max HP / mana / outgoing damage malus added per death while no boss bonus is active.")]
    public float deathMalusPercentPerDeath = 15f;

    [Tooltip("Malus added on death when a boss bonus was active (bonus is cleared; half of deathMalusPercentPerDeath).")]
    public float deathMalusPercentWhenBonusActive = 7.5f;

    [Tooltip("Normal kills at player level 1 to remove one malusRecoveryPercentPerTick.")]
    [FormerlySerializedAs("normalKillsPerMalusRecoveryPercent")]
    public int normalKillsPerMalusRecoveryAtLevel1 = 2;

    [Tooltip("Extra normal kills required per player level above 1 (level 50 with 0.1 → ~7 kills per 1% malus).")]
    public float extraNormalKillsPerMalusRecoveryPerLevel = 0.1f;

    [Tooltip("Malus removed per recovery tick (see normalKillsPerMalusRecoveryAtLevel1).")]
    public float malusRecoveryPercentPerTick = 1f;

    [Tooltip("Boss kill bonus to HP, mana, and damage; stacks on multiple bosses.")]
    public float bossKillBonusPercent = 15f;

    [Tooltip("HP / mana / damage bonus per full minute survived (stacks; not cleared on respawn).")]
    public float timeBonusPercentPerMinute = 2f;

    [Header("Enemy combat scaling (late game)")]
    [Tooltip("Above this player level, enemy HP/XP use a damped effective level so level 50–100 is not instant death.")]
    public int enemyStatSoftCapStartLevel = 42;

    [Range(0.2f, 1f)]
    [Tooltip("Effective-level factor at maxPlayerLevel (lower = gentler enemy growth).")]
    public float enemyStatScaleFactorAtMaxLevel = 0.38f;

    [Header("Spawning")]
    [Tooltip("Desired living normal enemies at player level 1 (intro).")]
    public int enemiesAtPlayerLevel1 = 12;

    [Tooltip("Desired living normal enemies at player level 2 (bridge before the linear ramp).")]
    public int enemiesAtPlayerLevel2 = 28;

    [Tooltip("For player level 3 and up: desired count ≈ level × this (see EnemyGenerator).")]
    public int enemiesPerPlayerLevel = 14;

    [Tooltip("Hard cap on desired living enemies (levels 50–100 stay playable).")]
    public int maxLivingEnemyCap = 200;

    [Tooltip("Player level at which greater enemies and boss spawn logic can activate (see EnemyGenerator).")]
    public int greaterEnemiesMinPlayerLevel = 12;

    [Tooltip(
        "Boss spawn cadence: a boss is scheduled when player level ≥ greaterEnemiesMinPlayerLevel AND level is a multiple of this value. " +
        "Example: 10 with min level 12 → bosses at 20, 30, 40, … Only one living boss is allowed; see EnemyGenerator.")]
    public int bossSpawnLevelMultiple = 10;

    [Tooltip("For player level ≥ 3: extra desired living enemies ≈ (level − 2)² × this, added after level × enemiesPerPlayerLevel.")]
    public float livingEnemyQuadraticSpawnBonus = 0.035f;

    [Header("Boss encounter (EnemyCharacter.isBoss)")]
    [Tooltip("Seconds (real time, unscaled) before the boss entity spawns after the UI telegraph begins.")]
    public float bossTelegraphDurationSeconds = 2.2f;

    [Tooltip("Peak alpha for the full-screen tint during the boss-incoming telegraph.")]
    [Range(0f, 0.45f)]
    public float bossTelegraphTintAlpha = 0.14f;

    [Tooltip("Multiplies max health after normal/greater stats are computed.")]
    public float bossHealthMultiplier = 3f;

    [Tooltip("Multiplies EXP reward after base expOnKill is computed (applied in EnemyCharacter).")]
    public float bossExpMultiplier = 2f;

    [Tooltip("Extra XP added when a boss dies (in addition to expOnKill after multipliers).")]
    public float bossBonusXpFlat = 250f;

    [Tooltip("Bonus score accumulated when a boss is defeated (shown on end screen; separate from kill counts).")]
    public int bossScoreBonusOnKill = 500;

    [Header("Mana regeneration (#104)")]
    [Tooltip(
        "Combat mana/s = base + level×linear + level²×quadratic (idle multiplier = 1). " +
        "Playtest targets at nominal max mana, no run malus: L1 ~20s empty→full (~75 mana), " +
        "L25 ~55s (~4.4k), L50 ~90s (~16k), L100 ~2.3 min (~62k). Idle ramp adds up to manaRegenMaxMultiplierAtFullIdle after manaRegenRampIdleSeconds.")]
    public float manaRegenBaseConstant = 1f;

    public float manaRegenPerLevel = 2.65f;

    public float manaRegenPerLevelSquared = 0.019f;

    [Tooltip("Seconds without spending mana to reach manaRegenMaxMultiplierAtFullIdle.")]
    public float manaRegenRampIdleSeconds = 5f;

    [Tooltip("Regen multiplier at full idle (applied on top of combat base regen).")]
    public float manaRegenMaxMultiplierAtFullIdle = 2.1f;

    [Header("Kill sustain (on enemy death)")]
    [Tooltip("HP restored to the player when a normal enemy dies (same moment as kill XP).")]
    public float killRestoreHealthNormal = 4f;

    [Tooltip("Mana restored when a normal enemy dies.")]
    public float killRestoreManaNormal = 4f;

    [Tooltip("HP restored when a greater enemy (non-boss) dies.")]
    public float killRestoreHealthGreater = 10f;

    [Tooltip("Mana restored when a greater enemy (non-boss) dies.")]
    public float killRestoreManaGreater = 8f;

    [Tooltip("HP restored when a boss dies.")]
    public float killRestoreHealthBoss = 35f;

    [Tooltip("Mana restored when a boss dies.")]
    public float killRestoreManaBoss = 24f;

    [Header("Level-up max HP / max mana (variance)")]
    [Tooltip(
        "Baseline max HP gained on level-up = 45 + (new level) × 22. Each level rolls independently for HP and for mana (not a shared quality roll). " +
        "Uses UnityEngine.Random; injectable/seeded RNG is a follow-up for tests/replays.")]
    public float levelUpMaxHealthGainMinMultiplier = 0.88f;

    [Tooltip("Upper multiplier applied to the baseline HP delta before rounding.")]
    public float levelUpMaxHealthGainMaxMultiplier = 1.12f;

    [Tooltip(
        "Hard floor: after the random roll, the HP gain is at least this fraction of the baseline delta (rounded). " +
        "Documented minimum gain vs the old deterministic curve — tune with min/max multipliers.")]
    [Range(0.5f, 1f)]
    public float levelUpMaxHealthGainFloorFractionOfBaseline = 0.82f;

    [Tooltip("Baseline max mana gained on level-up = 18 + (new level) × 12.")]
    public float levelUpMaxManaGainMinMultiplier = 0.88f;

    [Tooltip("Upper multiplier applied to the baseline mana delta before rounding.")]
    public float levelUpMaxManaGainMaxMultiplier = 1.12f;

    [Tooltip("Hard floor for mana gain vs baseline delta (rounded), same idea as HP.")]
    [Range(0.5f, 1f)]
    public float levelUpMaxManaGainFloorFractionOfBaseline = 0.82f;

    [Header("Player skills")]
    [Tooltip("Multiplies per-frame skill mana costs (primary/secondary and cooldown skills). Below 1 eases pressure when mana regen is modest.")]
    [Range(0.35f, 1f)]
    public float skillManaCostScale = 0.78f;

    [Tooltip("Meteor mana cost = (base + level × perLevel) × skillManaCostScale.")]
    public float meteorManaCostBase = 40f;

    public float meteorManaCostPerLevel = 34f;

    [Tooltip("Explosion damage scale per player level (× distance falloff in MeteorProjectile).")]
    public float meteorExplosionDamagePerLevel = 130f;

    [Tooltip("Rounded damage shown on the Meteor skill HUD column (approximate center hit).")]
    public float meteorHudDamagePerLevel = 65f;

    void OnValidate()
    {
        maxPlayerLevel = Mathf.Max(1, maxPlayerLevel);
        xpSoftCapEndLevel = Mathf.Clamp(xpSoftCapEndLevel, 2, maxPlayerLevel);
        xpSoftCapStartLevel = Mathf.Clamp(xpSoftCapStartLevel, 2, xpSoftCapEndLevel);
        maxLivingEnemyCap = Mathf.Max(enemiesAtPlayerLevel2, maxLivingEnemyCap);
        if (skillUnlockLevels == null || skillUnlockLevels.Length < 8)
            skillUnlockLevels = new[] { 1, 1, 1, 4, 8, 12, 16, 20 };
        levelUpMaxHealthGainMinMultiplier = Mathf.Clamp(levelUpMaxHealthGainMinMultiplier, 0.05f, 3f);
        levelUpMaxHealthGainMaxMultiplier = Mathf.Clamp(levelUpMaxHealthGainMaxMultiplier, 0.05f, 3f);
        levelUpMaxManaGainMinMultiplier = Mathf.Clamp(levelUpMaxManaGainMinMultiplier, 0.05f, 3f);
        levelUpMaxManaGainMaxMultiplier = Mathf.Clamp(levelUpMaxManaGainMaxMultiplier, 0.05f, 3f);
        skillManaCostScale = Mathf.Clamp(skillManaCostScale, 0.35f, 1f);
        manaRegenBaseConstant = Mathf.Max(0f, manaRegenBaseConstant);
        manaRegenPerLevel = Mathf.Max(0f, manaRegenPerLevel);
        manaRegenPerLevelSquared = Mathf.Max(0f, manaRegenPerLevelSquared);
        manaRegenRampIdleSeconds = Mathf.Max(0f, manaRegenRampIdleSeconds);
        manaRegenMaxMultiplierAtFullIdle = Mathf.Max(1f, manaRegenMaxMultiplierAtFullIdle);
        meteorManaCostBase = Mathf.Max(0f, meteorManaCostBase);
        meteorManaCostPerLevel = Mathf.Max(0f, meteorManaCostPerLevel);
        meteorExplosionDamagePerLevel = Mathf.Max(0f, meteorExplosionDamagePerLevel);
        meteorHudDamagePerLevel = Mathf.Max(0f, meteorHudDamagePerLevel);
    }
}

/// <summary>
/// Runtime view of balance data. <see cref="GameOver"/> registers the active asset in <c>Start</c>.
/// </summary>
public static class GameBalanceHelper
{
    public const int SkillSlotCount = 8;

    static readonly int[] DefaultSkillUnlockLevels = { 1, 1, 1, 4, 8, 12, 16, 20 };

    public static GameBalance Active { get; private set; }

    public static void Register(GameBalance balance)
    {
        Active = balance;
    }

    public static float RoundDurationSeconds => Active != null ? Active.roundDurationSeconds : 1200f;

    public static int MaxPlayerLevel => Active != null ? Mathf.Max(1, Active.maxPlayerLevel) : 100;

    public static int SkillUnlockGeoShot => GetSkillUnlockLevel(0);
    public static int SkillUnlockGeoBlast => GetSkillUnlockLevel(1);
    public static int SkillUnlockGeoPhysics => GetSkillUnlockLevel(2);
    public static int SkillUnlockHeal => GetSkillUnlockLevel(3);
    public static int SkillUnlockMeteor => GetSkillUnlockLevel(4);
    public static int SkillUnlockBloodRitual => GetSkillUnlockLevel(5);
    public static int SkillUnlockFreezeTime => GetSkillUnlockLevel(6);
    public static int SkillUnlockGeoMania => GetSkillUnlockLevel(7);

    /// <summary>Enemy crits vs the player unlock when Geo Mania unlocks (same cadence as overheal).</summary>
    public static int EnemyCritMinPlayerLevel => SkillUnlockGeoMania;

    public static int MaxRespawnsPerRun => Active != null ? Mathf.Max(0, Active.maxRespawnsPerRun) : 2;

    public static float DeathMalusPercentPerDeath => Active != null ? Active.deathMalusPercentPerDeath : 15f;

    public static float DeathMalusPercentWhenBonusActive =>
        Active != null ? Active.deathMalusPercentWhenBonusActive : 7.5f;

    public static float MalusRecoveryPercentPerTick =>
        Active != null ? Active.malusRecoveryPercentPerTick : 1f;

    public static float BossKillBonusPercent => Active != null ? Active.bossKillBonusPercent : 15f;

    public static float TimeBonusPercentPerMinute => Active != null ? Active.timeBonusPercentPerMinute : 2f;

    /// <summary>Normal enemy kills needed to remove one <see cref="MalusRecoveryPercentPerTick"/> at the given player level.</summary>
    public static int GetNormalKillsRequiredForMalusRecovery(int playerLevel)
    {
        int level = Mathf.Max(1, playerLevel);
        int baseKills = Active != null ? Mathf.Max(1, Active.normalKillsPerMalusRecoveryAtLevel1) : 2;
        float extraPerLevel = Active != null ? Active.extraNormalKillsPerMalusRecoveryPerLevel : 0.1f;
        return Mathf.Max(1, Mathf.RoundToInt(baseKills + (level - 1) * extraPerLevel));
    }

    public static int GetSkillUnlockLevel(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= SkillSlotCount)
            return 1;
        if (Active != null && Active.skillUnlockLevels != null && skillIndex < Active.skillUnlockLevels.Length)
            return Mathf.Max(1, Active.skillUnlockLevels[skillIndex]);
        return DefaultSkillUnlockLevels[skillIndex];
    }

    /// <summary>XP required while at <paramref name="level"/> to reach the next level.</summary>
    public static float GetExpRequiredAtLevel(int level)
    {
        int lv = Mathf.Max(1, level);
        if (lv <= 1)
            return Active != null ? Active.firstLevelExpToReachLevel2 : 100f;

        float coef = Active != null ? Active.xpPerLevelSquaredCoefficient : 48f;
        float req = lv * lv * coef;

        int softStart = Active != null ? Active.xpSoftCapStartLevel : 32;
        int softEnd = Active != null ? Mathf.Max(softStart, Active.xpSoftCapEndLevel) : 100;
        float minFactor = Active != null ? Active.xpSoftCapMinFactorAtMaxLevel : 0.52f;

        if (lv <= softStart || softEnd <= softStart)
            return req;

        float t = Mathf.Clamp01((lv - softStart) / (float)(softEnd - softStart));
        return req * Mathf.Lerp(1f, minFactor, t);
    }

    /// <summary>Dampens enemy HP/XP scaling so player levels 50–100 stay survivable.</summary>
    public static float GetEnemyScalingLevel(float playerLevel)
    {
        int lv = Mathf.Max(1, Mathf.RoundToInt(playerLevel));
        int capStart = Active != null ? Active.enemyStatSoftCapStartLevel : 42;
        int maxLv = MaxPlayerLevel;
        if (lv <= capStart)
            return lv;

        float endFactor = Active != null ? Active.enemyStatScaleFactorAtMaxLevel : 0.38f;
        if (maxLv <= capStart)
            return lv * endFactor;

        float t = Mathf.Clamp01((lv - capStart) / (float)(maxLv - capStart));
        float factor = Mathf.Lerp(1f, endFactor, t);
        return capStart + (lv - capStart) * factor;
    }

    static int MaxLivingEnemyCap => Active != null ? Mathf.Max(Active.enemiesAtPlayerLevel2, Active.maxLivingEnemyCap) : 200;

    /// <summary>Target living count at player level 1. Values ≤ 0 fall back to 12 so older assets never request zero.</summary>
    public static int EnemiesAtPlayerLevel1
    {
        get
        {
            if (Active == null)
                return 12;
            return Active.enemiesAtPlayerLevel1 > 0 ? Active.enemiesAtPlayerLevel1 : 12;
        }
    }

    /// <summary>Target living count at player level 2. Values ≤ 0 fall back to 28.</summary>
    public static int EnemiesAtPlayerLevel2
    {
        get
        {
            if (Active == null)
                return 28;
            return Active.enemiesAtPlayerLevel2 > 0 ? Active.enemiesAtPlayerLevel2 : 28;
        }
    }

    /// <summary>Per-level multiplier for player level ≥ 3. Values ≤ 0 fall back to 22.</summary>
    public static int EnemiesPerPlayerLevel
    {
        get
        {
            if (Active == null)
                return 14;
            return Active.enemiesPerPlayerLevel > 0 ? Active.enemiesPerPlayerLevel : 14;
        }
    }

    public static int GreaterEnemiesMinPlayerLevel => Active != null ? Active.greaterEnemiesMinPlayerLevel : 12;

    public static int BossSpawnLevelMultiple => Active != null ? Mathf.Max(1, Active.bossSpawnLevelMultiple) : 10;

    public static float BossTelegraphDurationSeconds =>
        Active != null && Active.bossTelegraphDurationSeconds > 0.05f ? Active.bossTelegraphDurationSeconds : 2.2f;

    public static float BossTelegraphTintAlpha =>
        Active != null ? Mathf.Clamp01(Active.bossTelegraphTintAlpha) : 0.14f;

    public static float BossHealthMultiplier => Active != null ? Active.bossHealthMultiplier : 3f;

    public static float BossExpMultiplier => Active != null ? Active.bossExpMultiplier : 2f;

    public static float BossBonusXpFlat => Active != null ? Active.bossBonusXpFlat : 250f;

    public static int BossScoreBonusOnKill => Active != null ? Active.bossScoreBonusOnKill : 500;

    /// <summary>Combat mana/s at <paramref name="playerLevel"/> (before idle ramp and run modifier multiplier).</summary>
    public static float GetManaRegenerationPerSecond(int playerLevel)
    {
        int lv = Mathf.Max(1, playerLevel);
        float b = Active != null ? Active.manaRegenBaseConstant : 1f;
        float lin = Active != null ? Active.manaRegenPerLevel : 2.65f;
        float quad = Active != null ? Active.manaRegenPerLevelSquared : 0.019f;
        return b + lv * lin + lv * lv * quad;
    }

    public static float ManaRegenRampIdleSeconds =>
        Active != null ? Active.manaRegenRampIdleSeconds : 5f;

    public static float ManaRegenMaxMultiplierAtFullIdle =>
        Active != null ? Mathf.Max(1f, Active.manaRegenMaxMultiplierAtFullIdle) : 2.1f;

    public static float GetMeteorManaCost(int playerLevel)
    {
        int lv = Mathf.Max(1, playerLevel);
        float b = Active != null ? Active.meteorManaCostBase : 40f;
        float per = Active != null ? Active.meteorManaCostPerLevel : 34f;
        return (b + lv * per) * SkillManaCostScale;
    }

    public static float GetMeteorExplosionDamagePerLevel(int playerLevel) =>
        Mathf.Max(0f, Active != null ? Active.meteorExplosionDamagePerLevel : 130f) * Mathf.Max(1, playerLevel);

    public static float GetMeteorHudDamageDisplay(int playerLevel) =>
        Mathf.Max(0f, Active != null ? Active.meteorHudDamagePerLevel : 65f) * Mathf.Max(1, playerLevel);

    /// <summary>Scales recurring skill <c>manacost</c> values (see individual skills' <c>Update</c>). Default eases mana pressure vs regen.</summary>
    public static float SkillManaCostScale
    {
        get
        {
            if (Active == null)
                return 0.78f;
            float s = Active.skillManaCostScale;
            if (s <= 0.05f || s > 1f)
                return 0.78f;
            return s;
        }
    }

    /// <summary>Target living normal + pooled wave enemies from balance asset (with high-level curve).</summary>
    public static int GetDesiredLivingEnemyCount(int playerLevel)
    {
        int level = Mathf.Max(1, playerLevel);
        if (level <= 1)
            return EnemiesAtPlayerLevel1;
        if (level == 2)
            return EnemiesAtPlayerLevel2;

        int linear = level * EnemiesPerPlayerLevel;
        float quadCoef = Active != null ? Active.livingEnemyQuadraticSpawnBonus : 0.035f;
        int extra = 0;
        if (quadCoef > 0f && level >= 3)
            extra = Mathf.RoundToInt((level - 2) * (level - 2) * quadCoef);

        int total = linear + extra;
        total = Mathf.Max(EnemiesAtPlayerLevel2, total);
        return Mathf.Min(total, MaxLivingEnemyCap);
    }

    /// <summary>HP/mana bump when an enemy dies; uses <see cref="GameBalance"/> kill-sustain fields (boss &gt; greater &gt; normal).</summary>
    public static void ApplyKillSustain(PlayerCharacter pc, bool isBoss, bool isGreaterEnemy)
    {
        if (pc == null)
            return;
        float hp;
        float mp;
        if (isBoss)
        {
            hp = Active != null ? Active.killRestoreHealthBoss : 35f;
            mp = Active != null ? Active.killRestoreManaBoss : 24f;
        }
        else if (isGreaterEnemy)
        {
            hp = Active != null ? Active.killRestoreHealthGreater : 10f;
            mp = Active != null ? Active.killRestoreManaGreater : 8f;
        }
        else
        {
            hp = Active != null ? Active.killRestoreHealthNormal : 4f;
            mp = Active != null ? Active.killRestoreManaNormal : 4f;
        }

        if (hp > 0f)
            pc.changeCurrentHealth(hp);
        if (mp > 0f)
            pc.changeCurrentMana(mp);
    }

    /// <summary>Deterministic baseline HP delta for one level-up at <paramref name="newPlayerLevel"/> (after increment).</summary>
    public static float GetLevelUpMaxHealthBaselineDelta(int newPlayerLevel)
    {
        int lv = Mathf.Max(1, newPlayerLevel);
        return 45f + lv * 22f;
    }

    /// <summary>Deterministic baseline mana delta for one level-up at <paramref name="newPlayerLevel"/> (after increment).</summary>
    public static float GetLevelUpMaxManaBaselineDelta(int newPlayerLevel)
    {
        int lv = Mathf.Max(1, newPlayerLevel);
        return 18f + lv * 12f;
    }

    /// <summary>
    /// Random max-HP gain for one level-up. Independent from <see cref="RollLevelUpMaxManaDelta"/>.
    /// Uses <see cref="UnityEngine.Random"/>; no injectable RNG yet (ticket follow-up).
    /// </summary>
    public static int RollLevelUpMaxHealthDelta(int newPlayerLevel)
    {
        float baseline = GetLevelUpMaxHealthBaselineDelta(newPlayerLevel);
        float minM = Active != null ? Active.levelUpMaxHealthGainMinMultiplier : 0.88f;
        float maxM = Active != null ? Active.levelUpMaxHealthGainMaxMultiplier : 1.12f;
        float floorF = Active != null ? Active.levelUpMaxHealthGainFloorFractionOfBaseline : 0.82f;
        return RollBoundedIntDelta(baseline, minM, maxM, floorF);
    }

    /// <summary>
    /// Random max-mana gain for one level-up. Independent from <see cref="RollLevelUpMaxHealthDelta"/>.
    /// </summary>
    public static int RollLevelUpMaxManaDelta(int newPlayerLevel)
    {
        float baseline = GetLevelUpMaxManaBaselineDelta(newPlayerLevel);
        float minM = Active != null ? Active.levelUpMaxManaGainMinMultiplier : 0.88f;
        float maxM = Active != null ? Active.levelUpMaxManaGainMaxMultiplier : 1.12f;
        float floorF = Active != null ? Active.levelUpMaxManaGainFloorFractionOfBaseline : 0.82f;
        return RollBoundedIntDelta(baseline, minM, maxM, floorF);
    }

    static int RollBoundedIntDelta(float baseline, float minMultiplier, float maxMultiplier, float floorFractionOfBaseline)
    {
        if (baseline <= 0f)
            return 0;
        float lo = minMultiplier;
        float hi = maxMultiplier;
        if (lo > hi)
        {
            float t = lo;
            lo = hi;
            hi = t;
        }

        float rolled = Random.Range(baseline * lo, baseline * hi);
        int gain = Mathf.RoundToInt(rolled);
        floorFractionOfBaseline = Mathf.Clamp01(floorFractionOfBaseline);
        int floorGain = Mathf.Max(1, Mathf.RoundToInt(baseline * floorFractionOfBaseline));
        return Mathf.Max(gain, floorGain);
    }
}
