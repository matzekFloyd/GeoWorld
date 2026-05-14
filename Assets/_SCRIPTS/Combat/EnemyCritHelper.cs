using UnityEngine;

/// <summary>
/// Enemy attacks can critically strike the player once the player reaches level 10.
/// Bosses (and big homing missiles) use a higher crit chance than normal enemies.
/// </summary>
public static class EnemyCritHelper
{
    public const int MinPlayerLevelForEnemyCrit = 10;

    const float CritChanceNormal = 0.04f;
    const float CritChanceBossTier = 0.11f;
    const float CritDamageMultiplier = 1.32f;

    /// <summary>
    /// When <paramref name="pc"/> is level ≥ <see cref="MinPlayerLevelForEnemyCrit"/>, rolls crit; on success scales <paramref name="damage"/> up.
    /// </summary>
    /// <param name="attackerIsBossTier">Boss character or other high-tier source (e.g. big homing missile).</param>
    public static bool TryApplyEnemyCritAgainstPlayer(PlayerCharacter pc, ref float damage, bool attackerIsBossTier)
    {
        if (pc == null || damage <= 0f || pc.getCurLevel() < MinPlayerLevelForEnemyCrit)
            return false;

        float chance = attackerIsBossTier ? CritChanceBossTier : CritChanceNormal;
        if (Random.value >= chance)
            return false;

        damage *= CritDamageMultiplier;
        return true;
    }
}
