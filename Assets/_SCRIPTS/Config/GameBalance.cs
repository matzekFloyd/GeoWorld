using UnityEngine;

/// <summary>
/// Central tuning data. Create via Assets → Create → GeoWorld → Game Balance,
/// then assign it on the GameObject that has <see cref="GameOver"/> (same object is fine).
/// If unassigned, <see cref="GameBalanceHelper"/> uses built-in defaults matching the old hard-coded values.
/// </summary>
[CreateAssetMenu(fileName = "GameBalance", menuName = "GeoWorld/Game Balance", order = 0)]
public class GameBalance : ScriptableObject
{
    [Header("Round")]
    [Tooltip("Countdown in seconds until GeoWorld win condition.")]
    public float roundDurationSeconds = 900f;

    [Header("Spawning")]
    [Tooltip("Desired living enemies ≈ playerLevel × this value (see EnemyGenerator).")]
    public int enemiesPerPlayerLevel = 40;

    [Tooltip("Player level at which greater enemies / boss logic can activate.")]
    public int greaterEnemiesMinPlayerLevel = 10;

    [Tooltip("Boss spawn attempt when player level is a multiple of this (e.g. 5 → levels 10,15,20… with current generator logic).")]
    public int bossSpawnLevelMultiple = 5;

    [Header("Boss (EnemyCharacter.isBoss)")]
    [Tooltip("Multiplies max health after normal/greater stats are computed.")]
    public float bossHealthMultiplier = 3f;

    [Tooltip("Multiplies EXP reward after base expOnKill is computed.")]
    public float bossExpMultiplier = 2f;

    [Tooltip("Added to the Greater Enemies kill counter when a boss dies (scoreboard feel).")]
    public int bossGreaterKillCounterBonus = 3;
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

    public static int EnemiesPerPlayerLevel => Active != null ? Active.enemiesPerPlayerLevel : 40;

    public static int GreaterEnemiesMinPlayerLevel => Active != null ? Active.greaterEnemiesMinPlayerLevel : 10;

    public static int BossSpawnLevelMultiple => Active != null ? Active.bossSpawnLevelMultiple : 5;

    public static float BossHealthMultiplier => Active != null ? Active.bossHealthMultiplier : 3f;

    public static float BossExpMultiplier => Active != null ? Active.bossExpMultiplier : 2f;

    public static int BossGreaterKillCounterBonus => Active != null ? Active.bossGreaterKillCounterBonus : 3;
}
