#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bakes VFX under <c>Assets/_PREFABS/Projectiles/GeoShotProjectile.prefab</c> and can strip missing scripts
/// (Unity refuses to save prefabs that still reference deleted scripts).
/// </summary>
static class GeoShotProjectileVfxEditor
{
    const string PrefabPath = "Assets/_PREFABS/Projectiles/GeoShotProjectile.prefab";

    [MenuItem("GeoWorld/Projectiles/Ensure GeoShot Projectile VFX", false, 50)]
    static void EnsureGeoShotProjectileVfx()
    {
        if (!TryLoadPrefabRoot(out var root))
            return;

        try
        {
            var removed = RemoveAllMissingScripts(root);
            if (removed > 0)
                Debug.Log($"[GeoWorld] Removed {removed} missing script slot(s) from GeoShotProjectile prefab.");

            var boot = root.GetComponent<GeoShotProjectileVfxBootstrap>();
            if (boot == null)
                boot = root.AddComponent<GeoShotProjectileVfxBootstrap>();

            boot.RebuildForPrefab();
            SavePrefabOrReport(root);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[GeoWorld] GeoShot projectile VFX baked into prefab: " + PrefabPath);
    }

    [MenuItem("GeoWorld/Projectiles/Strip missing scripts (GeoShot prefab only)", false, 51)]
    static void StripMissingScriptsOnly()
    {
        if (!TryLoadPrefabRoot(out var root))
            return;

        try
        {
            var removed = RemoveAllMissingScripts(root);
            SavePrefabOrReport(root);
            Debug.Log($"[GeoWorld] GeoShotProjectile: removed {removed} missing script slot(s), prefab saved.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
    }

    static bool TryLoadPrefabRoot(out GameObject root)
    {
        root = null;
        if (!System.IO.File.Exists(PrefabPath))
        {
            EditorUtility.DisplayDialog(
                "GeoShot projectile",
                "Prefab not found at:\n" + PrefabPath,
                "OK");
            return false;
        }

        root = PrefabUtility.LoadPrefabContents(PrefabPath);
        return root != null;
    }

    static void SavePrefabOrReport(GameObject root)
    {
        try
        {
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        catch (System.Exception ex)
        {
            EditorUtility.DisplayDialog(
                "GeoShot prefab save failed",
                ex.Message + "\n\nTry: GeoWorld → Projectiles → Strip missing scripts, or open the prefab and remove any \"Missing (Mono Script)\" components manually.",
                "OK");
        }
    }

    /// <summary>Removes missing-script placeholders on every GameObject (including inactive children).</summary>
    static int RemoveAllMissingScripts(GameObject root)
    {
        if (root == null)
            return 0;
        var total = 0;
        var trs = root.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < trs.Length; i++)
        {
            var go = trs[i].gameObject;
            total += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }

        return total;
    }
}
#endif
