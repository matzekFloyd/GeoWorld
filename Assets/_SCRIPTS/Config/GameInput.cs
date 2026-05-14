using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gameplay input façade. Bindings load from <c>Resources/Input/GeoWorldInputActions</c> (JSON via <see cref="InputActionAsset.LoadFromJson"/>).
/// Requires <b>Active Input Handling</b>: <b>Input System Package (New)</b> (or Both).
/// </summary>
public static class GameInput
{
    const string JsonResourcePath = "Input/GeoWorldInputActions";

    /// <summary>Holds runtime instance when loaded from JSON (not an imported <see cref="InputActionAsset"/>).</summary>
    static InputActionAsset s_LoadedAsset;

    static InputActionMap s_Gameplay;
    static InputAction s_FirePrimary;
    static InputAction s_SecondaryFire;
    static InputAction s_SkillHeal;
    static InputAction s_SkillMeteor;
    static InputAction s_SkillBloodRitual;
    static InputAction s_SkillFreezeTime;
    static InputAction s_PauseOrQuit;
    static InputAction s_DebugLevelUp;

    /// <summary>Default Fire1 name (legacy axis name); gameplay reads from the Input System asset.</summary>
    public const string FirePrimary = "Fire1";
    public const string FireSecondaryMouse = "Fire2";

    public const KeyCode PauseOrQuit = KeyCode.Escape;
    public const KeyCode DebugInstantLevelUp = KeyCode.T;
    public const KeyCode SkillHeal = KeyCode.Q;
    public const KeyCode SkillMeteor = KeyCode.E;
    public const KeyCode SkillBloodRitual = KeyCode.R;
    public const KeyCode SkillFreezeTime = KeyCode.F;

    public const int MouseButtonSecondary = 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        Ensure();
    }

    static void Ensure()
    {
        if (s_Gameplay != null)
            return;

        InputActionAsset asset = null;
        var json = Resources.Load<TextAsset>(JsonResourcePath);
        if (json != null)
        {
            asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "GeoWorldInput";
            asset.LoadFromJson(json.text);
            s_LoadedAsset = asset;
        }

        if (asset == null)
        {
            Debug.LogError(
                "GeoWorld: Missing input JSON at Resources path \"" + JsonResourcePath +
                "\". Expected: Assets/Resources/Input/GeoWorldInputActions.txt (TextAsset).");
            return;
        }

        s_Gameplay = asset.FindActionMap("Gameplay");
        if (s_Gameplay == null)
        {
            Debug.LogError("GeoWorld: Input asset has no \"Gameplay\" action map.");
            return;
        }

        s_FirePrimary = s_Gameplay.FindAction("FirePrimary");
        s_SecondaryFire = s_Gameplay.FindAction("SecondaryFire");
        s_SkillHeal = s_Gameplay.FindAction("SkillHeal");
        s_SkillMeteor = s_Gameplay.FindAction("SkillMeteor");
        s_SkillBloodRitual = s_Gameplay.FindAction("SkillBloodRitual");
        s_SkillFreezeTime = s_Gameplay.FindAction("SkillFreezeTime");
        s_PauseOrQuit = s_Gameplay.FindAction("PauseOrQuit");
        s_DebugLevelUp = s_Gameplay.FindAction("DebugLevelUp");

        s_Gameplay.Enable();
    }

    public static bool FirePrimaryDown
    {
        get
        {
            Ensure();
            return s_FirePrimary != null && s_FirePrimary.WasPressedThisFrame();
        }
    }

    public static bool SkillHealUp
    {
        get
        {
            Ensure();
            return s_SkillHeal != null && s_SkillHeal.WasReleasedThisFrame();
        }
    }

    public static bool SkillMeteorUp
    {
        get
        {
            Ensure();
            return s_SkillMeteor != null && s_SkillMeteor.WasReleasedThisFrame();
        }
    }

    public static bool SkillBloodRitualUp
    {
        get
        {
            Ensure();
            return s_SkillBloodRitual != null && s_SkillBloodRitual.WasReleasedThisFrame();
        }
    }

    public static bool SkillFreezeTimeUp
    {
        get
        {
            Ensure();
            return s_SkillFreezeTime != null && s_SkillFreezeTime.WasReleasedThisFrame();
        }
    }

    public static bool DebugInstantLevelUpUp
    {
        get
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            return false;
#else
            Ensure();
            return s_DebugLevelUp != null && s_DebugLevelUp.WasReleasedThisFrame();
#endif
        }
    }

    public static bool PauseOrQuitUp
    {
        get
        {
            Ensure();
            return s_PauseOrQuit != null && s_PauseOrQuit.WasReleasedThisFrame();
        }
    }

    public static bool SecondaryMouseDown
    {
        get
        {
            Ensure();
            return s_SecondaryFire != null && s_SecondaryFire.WasPressedThisFrame();
        }
    }
}
