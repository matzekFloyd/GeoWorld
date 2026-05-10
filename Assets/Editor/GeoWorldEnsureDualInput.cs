#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// GeoWorld gameplay uses the new Input System; Standard Assets still expect the legacy manager.
/// Force <b>Both</b> so movement / mouselook keep working without migrating those assets.
/// Uses serialized ProjectSettings (<c>activeInputHandler</c>) because <c>PlayerSettings.activeInputHandler</c>
/// is not exposed in all Unity Editor API surfaces.
/// </summary>
[InitializeOnLoad]
static class GeoWorldEnsureDualInput
{
    const string ProjectSettingsPath = "ProjectSettings/ProjectSettings.asset";

    /// <summary>0 = Input Manager, 1 = Input System, 2 = Both.</summary>
    const int ActiveInputHandlerBoth = 2;

    static GeoWorldEnsureDualInput()
    {
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(ProjectSettingsPath))
        {
            if (obj == null)
                continue;

            var so = new SerializedObject(obj);
            var prop = so.FindProperty("activeInputHandler") ?? so.FindProperty("m_ActiveInputHandler");
            if (prop == null)
                continue;

            if (prop.intValue != ActiveInputHandlerBoth)
            {
                prop.intValue = ActiveInputHandlerBoth;
                so.ApplyModifiedProperties();
                UnityEngine.Debug.Log(
                    "[GeoWorld] Set Player Settings → Active Input Handling to Both (Input System + Legacy).");
            }

            return;
        }

        UnityEngine.Debug.LogWarning(
            "[GeoWorld] Could not find serialized activeInputHandler in ProjectSettings. " +
            "Set Edit → Project Settings → Player → Active Input Handling to **Both** so Input System actions work alongside Standard Assets.");
    }
}
#endif
