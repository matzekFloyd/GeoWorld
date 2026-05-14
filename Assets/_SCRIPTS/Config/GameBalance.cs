using UnityEngine;

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
    public float roundDurationSeconds = 900f;

    [Header("Spawning")]
    [Tooltip("Desired living normal enemies at player level 1 (intro).")]
    public int enemiesAtPlayerLevel1 = 12;

    [Tooltip("Desired living normal enemies at player level 2 (bridge before the linear ramp).")]
    public int enemiesAtPlayerLevel2 = 28;

    [Tooltip("For player level 3 and up: desired count ≈ level × this (see EnemyGenerator).")]
    public int enemiesPerPlayerLevel = 22;

    [Tooltip("Player level at which greater enemies and boss spawn logic can activate (see EnemyGenerator).")]
    public int greaterEnemiesMinPlayerLevel = 10;

    [Tooltip(
        "Boss spawn cadence: a boss is scheduled when player level ≥ greaterEnemiesMinPlayerLevel AND level is a multiple of this value. " +
        "Example: 5 with min level 10 → bosses at levels 10, 15, 20, … Only one living boss is allowed; see EnemyGenerator.")]
    public int bossSpawnLevelMultiple = 5;

    [Tooltip("For player level ≥ 3: extra desired living enemies ≈ (level − 2)² × this, added after level × enemiesPerPlayerLevel.")]
    public float livingEnemyQuadraticSpawnBonus = 0.1f;

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

    [Header("Kill sustain (on enemy death)")]
    [Tooltip("HP restored to the player when a normal enemy dies (same moment as kill XP).")]
    public float killRestoreHealthNormal = 4f;

    [Tooltip("Mana restored when a normal enemy dies.")]
    public float killRestoreManaNormal = 3f;

    [Tooltip("HP restored when a greater enemy (non-boss) dies.")]
    public float killRestoreHealthGreater = 10f;

    [Tooltip("Mana restored when a greater enemy (non-boss) dies.")]
    public float killRestoreManaGreater = 6f;

    [Tooltip("HP restored when a boss dies.")]
    public float killRestoreHealthBoss = 35f;

    [Tooltip("Mana restored when a boss dies.")]
    public float killRestoreManaBoss = 20f;
}

/// <summary>
/// Runtime view of balance data. <see cref="GameOver"/> registers the active asset in <c>Start</c>.
/// </summary>
public static class GameBalanceHelper
{
    public static GameBalance Active { get; private set; }

    public static void Register(GameBalance balance)
    {
        Active = balance;
    }

    public static float RoundDurationSeconds => Active != null ? Active.roundDurationSeconds : 900f;

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
                return 22;
            return Active.enemiesPerPlayerLevel > 0 ? Active.enemiesPerPlayerLevel : 22;
        }
    }

    public static int GreaterEnemiesMinPlayerLevel => Active != null ? Active.greaterEnemiesMinPlayerLevel : 10;

    public static int BossSpawnLevelMultiple => Active != null ? Mathf.Max(1, Active.bossSpawnLevelMultiple) : 5;

    public static float BossTelegraphDurationSeconds =>
        Active != null && Active.bossTelegraphDurationSeconds > 0.05f ? Active.bossTelegraphDurationSeconds : 2.2f;

    public static float BossTelegraphTintAlpha =>
        Active != null ? Mathf.Clamp01(Active.bossTelegraphTintAlpha) : 0.14f;

    public static float BossHealthMultiplier => Active != null ? Active.bossHealthMultiplier : 3f;

    public static float BossExpMultiplier => Active != null ? Active.bossExpMultiplier : 2f;

    public static float BossBonusXpFlat => Active != null ? Active.bossBonusXpFlat : 250f;

    public static int BossScoreBonusOnKill => Active != null ? Active.bossScoreBonusOnKill : 500;

    /// <summary>Target living normal + pooled wave enemies from balance asset (with high-level curve).</summary>
    public static int GetDesiredLivingEnemyCount(int playerLevel)
    {
        int level = Mathf.Max(1, playerLevel);
        if (level <= 1)
            return EnemiesAtPlayerLevel1;
        if (level == 2)
            return EnemiesAtPlayerLevel2;

        int linear = level * EnemiesPerPlayerLevel;
        float quadCoef = Active != null ? Active.livingEnemyQuadraticSpawnBonus : 0.1f;
        int extra = 0;
        if (quadCoef > 0f && level >= 3)
            extra = Mathf.RoundToInt((level - 2) * (level - 2) * quadCoef);

        int total = linear + extra;
        return Mathf.Max(EnemiesAtPlayerLevel2, total);
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
            mp = Active != null ? Active.killRestoreManaBoss : 20f;
        }
        else if (isGreaterEnemy)
        {
            hp = Active != null ? Active.killRestoreHealthGreater : 10f;
            mp = Active != null ? Active.killRestoreManaGreater : 6f;
        }
        else
        {
            hp = Active != null ? Active.killRestoreHealthNormal : 4f;
            mp = Active != null ? Active.killRestoreManaNormal : 3f;
        }

        if (hp > 0f)
            pc.changeCurrentHealth(hp);
        if (mp > 0f)
            pc.changeCurrentMana(mp);
    }
}
