using System.Collections.Generic;
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

    static int s_HudFpFirePrimary = int.MinValue;
    static int s_HudFpSecondaryFire = int.MinValue;
    static int s_HudFpSkillHeal = int.MinValue;
    static int s_HudFpSkillMeteor = int.MinValue;
    static int s_HudFpSkillBloodRitual = int.MinValue;
    static int s_HudFpSkillFreezeTime = int.MinValue;
    static string s_HudLabelFirePrimary;
    static string s_HudLabelSecondaryFire;
    static string s_HudLabelSkillHeal;
    static string s_HudLabelSkillMeteor;
    static string s_HudLabelSkillBloodRitual;
    static string s_HudLabelSkillFreezeTime;

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

    /// <summary>
    /// Fills eight HUD skill-strip key labels from the loaded <see cref="JsonResourcePath"/> actions (indices 2 and 7 stay empty).
    /// Call each frame is fine: labels refresh only when binding paths change.
    /// </summary>
    public static void FillSkillStripKeyLabels(string[] labels)
    {
        Ensure();
        if (labels == null || labels.Length < 8)
            return;

        labels[0] = CachedHudLabel(ref s_HudFpFirePrimary, ref s_HudLabelFirePrimary, s_FirePrimary);
        labels[1] = CachedHudLabel(ref s_HudFpSecondaryFire, ref s_HudLabelSecondaryFire, s_SecondaryFire);
        labels[2] = "";
        labels[3] = CachedHudLabel(ref s_HudFpSkillHeal, ref s_HudLabelSkillHeal, s_SkillHeal);
        labels[4] = CachedHudLabel(ref s_HudFpSkillMeteor, ref s_HudLabelSkillMeteor, s_SkillMeteor);
        labels[5] = CachedHudLabel(ref s_HudFpSkillBloodRitual, ref s_HudLabelSkillBloodRitual, s_SkillBloodRitual);
        labels[6] = CachedHudLabel(ref s_HudFpSkillFreezeTime, ref s_HudLabelSkillFreezeTime, s_SkillFreezeTime);
        labels[7] = "";
    }

    static string CachedHudLabel(ref int fpCache, ref string labelCache, InputAction action)
    {
        int fp = BindingPathFingerprint(action);
        if (fp != fpCache)
        {
            fpCache = fp;
            labelCache = FormatHudActionLabel(action);
        }

        return labelCache ?? "";
    }

    static string BindingPathResolved(in InputBinding b)
    {
        return !string.IsNullOrEmpty(b.overridePath) ? b.overridePath : b.path;
    }

    static int BindingPathFingerprint(InputAction action)
    {
        if (action == null)
            return 0;
        unchecked
        {
            int h = 17;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var b = action.bindings[i];
                string p = BindingPathResolved(in b);
                h = h * 31 + (p != null ? p.GetHashCode() : 0);
            }

            return h;
        }
    }

    static string FormatHudActionLabel(InputAction action)
    {
        if (action == null)
            return "";

        var parts = new List<string>(4);
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var b = action.bindings[i];
            if (b.isComposite)
                continue;

            string path = BindingPathResolved(in b);
            if (string.IsNullOrEmpty(path))
                continue;

            string piece = path switch
            {
                "<Mouse>/leftButton" => "M1",
                "<Mouse>/rightButton" => "M2",
                "<Mouse>/middleButton" => "M3",
                _ => action.GetBindingDisplayString(i)
            };
            if (string.IsNullOrEmpty(piece))
                continue;
            bool dup = false;
            for (int j = 0; j < parts.Count; j++)
            {
                if (parts[j] == piece)
                {
                    dup = true;
                    break;
                }
            }

            if (!dup)
                parts.Add(piece);
        }

        if (parts.Count == 0)
            return action.GetBindingDisplayString();

        if (parts.Count == 1)
            return parts[0];

        return string.Join("/", parts);
    }

    public static bool FirePrimaryHeld
    {
        get
        {
            Ensure();
            return s_FirePrimary != null && s_FirePrimary.IsPressed();
        }
    }

    public static bool SkillHealHeld
    {
        get
        {
            Ensure();
            return s_SkillHeal != null && s_SkillHeal.IsPressed();
        }
    }

    public static bool SkillMeteorHeld
    {
        get
        {
            Ensure();
            return s_SkillMeteor != null && s_SkillMeteor.IsPressed();
        }
    }

    public static bool SkillBloodRitualHeld
    {
        get
        {
            Ensure();
            return s_SkillBloodRitual != null && s_SkillBloodRitual.IsPressed();
        }
    }

    public static bool SkillFreezeTimeHeld
    {
        get
        {
            Ensure();
            return s_SkillFreezeTime != null && s_SkillFreezeTime.IsPressed();
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

    public static bool SecondaryFireHeld
    {
        get
        {
            Ensure();
            return s_SecondaryFire != null && s_SecondaryFire.IsPressed();
        }
    }
}
