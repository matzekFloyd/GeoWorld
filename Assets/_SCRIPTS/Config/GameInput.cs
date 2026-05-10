using UnityEngine;

/// <summary>
/// Central place for legacy Input Manager keys and buttons. Swap values here instead of hunting scripts.
/// (Migrating to the new Input System package would be a larger follow-up task.)
/// </summary>
public static class GameInput
{
    public const string FirePrimary = "Fire1";
    public const string FireSecondaryMouse = "Fire2"; // unused today; GeoBlast uses mouse button index

    public const KeyCode PauseOrQuit = KeyCode.Escape;

    public const KeyCode DebugInstantLevelUp = KeyCode.T;

    public const KeyCode SkillHeal = KeyCode.Q;
    public const KeyCode SkillMeteor = KeyCode.E;
    public const KeyCode SkillBloodRitual = KeyCode.R;
    public const KeyCode SkillFreezeTime = KeyCode.F;

    public const int MouseButtonSecondary = 1;

    public static bool FirePrimaryDown => Input.GetButtonDown(FirePrimary);

    public static bool SkillHealUp => Input.GetKeyUp(SkillHeal);
    public static bool SkillMeteorUp => Input.GetKeyUp(SkillMeteor);
    public static bool SkillBloodRitualUp => Input.GetKeyUp(SkillBloodRitual);
    public static bool SkillFreezeTimeUp => Input.GetKeyUp(SkillFreezeTime);
    public static bool DebugInstantLevelUpUp => Input.GetKeyUp(DebugInstantLevelUp);
    public static bool PauseOrQuitUp => Input.GetKeyUp(PauseOrQuit);

    public static bool SecondaryMouseDown => Input.GetMouseButtonDown(MouseButtonSecondary);
}
