using UnityEngine;
#if !(UNITY_WEBGL && !UNITY_EDITOR && ENABLE_LEGACY_INPUT_MANAGER)
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Gameplay input façade. Bindings load from <c>Resources/Input/GeoWorldInputActions</c> (JSON via <see cref="InputActionAsset.LoadFromJson"/>).
/// Standard Assets still use the legacy Input Manager — keep Player Settings on <b>Both</b> backends.
/// </summary>
public static class GameInput
{
    const string JsonResourcePath = "Input/GeoWorldInputActions";

#if !(UNITY_WEBGL && !UNITY_EDITOR && ENABLE_LEGACY_INPUT_MANAGER)
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
#endif

    /// <summary>Default Fire1 name (legacy Input Manager); gameplay uses the Input System asset.</summary>
    public const string FirePrimary = "Fire1";
    public const string FireSecondaryMouse = "Fire2";

    public const KeyCode PauseOrQuit = KeyCode.Escape;
    public const KeyCode DebugInstantLevelUp = KeyCode.T;
    public const KeyCode SkillHeal = KeyCode.Q;
    public const KeyCode SkillMeteor = KeyCode.E;
    public const KeyCode SkillBloodRitual = KeyCode.R;
    public const KeyCode SkillFreezeTime = KeyCode.F;

    public const int MouseButtonSecondary = 1;

#if UNITY_WEBGL && !UNITY_EDITOR && ENABLE_LEGACY_INPUT_MANAGER
    /// <summary>
    /// WebGL: Reading Input System actions and legacy <see cref="UnityEngine.Input"/> in the same frame can recurse
    /// through the WASM/JS boundary and exceed the browser stack. Use legacy only for player builds here.
    /// </summary>
    public static bool FirePrimaryDown => UnityEngine.Input.GetButtonDown(FirePrimary);

    public static bool SkillHealUp => UnityEngine.Input.GetKeyUp(SkillHeal);
    public static bool SkillMeteorUp => UnityEngine.Input.GetKeyUp(SkillMeteor);
    public static bool SkillBloodRitualUp => UnityEngine.Input.GetKeyUp(SkillBloodRitual);
    public static bool SkillFreezeTimeUp => UnityEngine.Input.GetKeyUp(SkillFreezeTime);

    public static bool DebugInstantLevelUpUp
    {
        get
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            return false;
#else
            return UnityEngine.Input.GetKeyUp(DebugInstantLevelUp);
#endif
        }
    }

    public static bool PauseOrQuitUp => UnityEngine.Input.GetKeyUp(PauseOrQuit);
    public static bool SecondaryMouseDown => UnityEngine.Input.GetMouseButtonDown(MouseButtonSecondary);
#else
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
            var fromActions = s_FirePrimary != null && s_FirePrimary.WasPressedThisFrame();
#if ENABLE_LEGACY_INPUT_MANAGER
            return fromActions || UnityEngine.Input.GetButtonDown(FirePrimary);
#else
            return fromActions;
#endif
        }
    }

    public static bool SkillHealUp
    {
        get
        {
            Ensure();
            var fromActions = s_SkillHeal != null && s_SkillHeal.WasReleasedThisFrame();
#if ENABLE_LEGACY_INPUT_MANAGER
            return fromActions || UnityEngine.Input.GetKeyUp(SkillHeal);
#else
            return fromActions;
#endif
        }
    }

    public static bool SkillMeteorUp
    {
        get
        {
            Ensure();
            var fromActions = s_SkillMeteor != null && s_SkillMeteor.WasReleasedThisFrame();
#if ENABLE_LEGACY_INPUT_MANAGER
            return fromActions || UnityEngine.Input.GetKeyUp(SkillMeteor);
#else
            return fromActions;
#endif
        }
    }

    public static bool SkillBloodRitualUp
    {
        get
        {
            Ensure();
            var fromActions = s_SkillBloodRitual != null && s_SkillBloodRitual.WasReleasedThisFrame();
#if ENABLE_LEGACY_INPUT_MANAGER
            return fromActions || UnityEngine.Input.GetKeyUp(SkillBloodRitual);
#else
            return fromActions;
#endif
        }
    }

    public static bool SkillFreezeTimeUp
    {
        get
        {
            Ensure();
            var fromActions = s_SkillFreezeTime != null && s_SkillFreezeTime.WasReleasedThisFrame();
#if ENABLE_LEGACY_INPUT_MANAGER
            return fromActions || UnityEngine.Input.GetKeyUp(SkillFreezeTime);
#else
            return fromActions;
#endif
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
            var fromActions = s_DebugLevelUp != null && s_DebugLevelUp.WasReleasedThisFrame();
#if ENABLE_LEGACY_INPUT_MANAGER
            return fromActions || UnityEngine.Input.GetKeyUp(DebugInstantLevelUp);
#else
            return fromActions;
#endif
#endif
        }
    }

    public static bool PauseOrQuitUp
    {
        get
        {
            Ensure();
            var fromActions = s_PauseOrQuit != null && s_PauseOrQuit.WasReleasedThisFrame();
#if ENABLE_LEGACY_INPUT_MANAGER
            return fromActions || UnityEngine.Input.GetKeyUp(PauseOrQuit);
#else
            return fromActions;
#endif
        }
    }

    public static bool SecondaryMouseDown
    {
        get
        {
            Ensure();
            var fromActions = s_SecondaryFire != null && s_SecondaryFire.WasPressedThisFrame();
#if ENABLE_LEGACY_INPUT_MANAGER
            return fromActions || UnityEngine.Input.GetMouseButtonDown(MouseButtonSecondary);
#else
            return fromActions;
#endif
        }
    }
#endif
}
