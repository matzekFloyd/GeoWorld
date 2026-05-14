using UnityEngine;

/// <summary>
/// When <see cref="PlayerCharacter.skillAvailable"/>(10) (Geo Mania) is active, player damage rolls can crit.
/// Crit chance scales with the skill's current mana cost and max cooldown; crit damage multiplier scales with level.
/// </summary>
public static class PlayerCritHelper
{
    const float CritChanceCap = 0.52f;
    const float BaseCritChance = 0.065f;

    /// <summary>
    /// If Geo Mania is active, rolls crit; on success multiplies <paramref name="damage"/> and returns true.
    /// </summary>
    public static bool TryApplyGeoManiaCrit(PlayerCharacter pc, ref float damage, float currentManacost, float maxCooldownSeconds)
    {
        if (pc == null || damage <= 0f || !pc.skillAvailable(10))
            return false;

        float mc = Mathf.Max(0f, currentManacost);
        float cd = Mathf.Max(0f, maxCooldownSeconds);
        float fromMana = Mathf.Clamp01(mc / (mc + 34f)) * 0.17f;
        float fromCd = Mathf.Clamp01(cd / (cd + 0.4f)) * 0.14f;
        float chance = Mathf.Min(CritChanceCap, BaseCritChance + fromMana + fromCd);

        if (Random.value >= chance)
            return false;

        int lv = Mathf.Max(1, pc.getCurLevel());
        float mult = 1.42f + (lv - 1) * 0.034f;
        damage *= mult;
        return true;
    }
}
