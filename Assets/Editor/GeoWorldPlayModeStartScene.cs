#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Pressing Play in the Editor loads the <b>currently open</b> scene unless
/// <see cref="EditorSceneManager.playModeStartScene"/> is set. This keeps <c>Start.unity</c> (title screen) as the
/// default entry so Play matches the shipped flow. Disable via <b>GeoWorld → Play Mode</b> when you want to iterate
/// on another scene without the title screen.
/// </summary>
[InitializeOnLoad]
static class GeoWorldPlayModeStartScene
{
    const string EditorPrefUseStartScene = "GeoWorld.UseStartSceneForPlayMode";
    const string StartSceneGuid = "948a83288e3b2354eaedbd5f09e74c76";

    static GeoWorldPlayModeStartScene()
    {
        ApplyFromPreference();
    }

    internal static void ApplyFromPreference()
    {
        if (!EditorPrefs.GetBool(EditorPrefUseStartScene, true))
        {
            EditorSceneManager.playModeStartScene = null;
            return;
        }

        var path = AssetDatabase.GUIDToAssetPath(StartSceneGuid);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("[GeoWorld] Start.unity not found (GUID). Play Mode will use the open scene.");
            return;
        }

        EnsureStartSceneInEditorBuildSettings(path);

        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        if (sceneAsset != null)
            EditorSceneManager.playModeStartScene = sceneAsset;
    }

    static void EnsureStartSceneInEditorBuildSettings(string sceneAssetPath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (var i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path != sceneAssetPath)
                continue;
            var s = scenes[i];
            if (s.enabled)
                return;
            s.enabled = true;
            scenes[i] = s;
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[GeoWorld] Enabled Start.unity in Editor Build Settings.");
            return;
        }

        scenes.Insert(0, new EditorBuildSettingsScene(sceneAssetPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[GeoWorld] Added Start.unity to Editor Build Settings (required for Play Mode start scene).");
    }

    [MenuItem("GeoWorld/Play Mode/Use Start.unity when pressing Play", false, 0)]
    static void MenuUseStartScene()
    {
        EditorPrefs.SetBool(EditorPrefUseStartScene, true);
        ApplyFromPreference();
        Debug.Log("[GeoWorld] Play Mode will begin in Start.unity (title screen).");
    }

    [MenuItem("GeoWorld/Play Mode/Use currently open scene when pressing Play", false, 11)]
    static void MenuUseOpenScene()
    {
        EditorPrefs.SetBool(EditorPrefUseStartScene, false);
        EditorSceneManager.playModeStartScene = null;
        Debug.Log("[GeoWorld] Play Mode uses whichever scene is open (Unity default).");
    }
}
#endif
