#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds / rebuilds GeoPhysics player VFX on the scene object tagged Player1 (or named Player).
/// </summary>
static class GeoPhysicsPlayerVfxEditor
{
    [MenuItem("GeoWorld/VFX/Ensure GeoPhysics Player VFX", false, 60)]
    static void EnsureGeoPhysicsPlayerVfx()
    {
        var player = FindPlayerRoot();
        if (player == null)
        {
            EditorUtility.DisplayDialog(
                "GeoPhysics VFX",
                "Could not find a player in the open scene.\n\nLooked for tag \"Player1\" or a GameObject named \"Player\" with PlayerCharacter.",
                "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(player, "Ensure GeoPhysics Player VFX");

        var boot = player.GetComponent<GeoPhysicsPlayerVfxBootstrap>();
        if (boot == null)
            boot = Undo.AddComponent<GeoPhysicsPlayerVfxBootstrap>(player);

        boot.RebuildForSceneObject();

        if (player.GetComponent<GeoPhysicsPlayerVfx>() == null)
            Undo.AddComponent<GeoPhysicsPlayerVfx>(player);

        EditorUtility.SetDirty(player);
        Debug.Log("[GeoWorld] GeoPhysics player VFX baked on: " + player.name);
    }

    static GameObject FindPlayerRoot()
    {
        var tagged = GameObject.FindGameObjectWithTag("Player1");
        if (tagged != null)
            return tagged;

        var pcs = Object.FindObjectsByType<PlayerCharacter>();
        for (int i = 0; i < pcs.Length; i++)
        {
            if (pcs[i] != null && pcs[i].gameObject.name == "Player")
                return pcs[i].gameObject;
        }

        return pcs.Length > 0 ? pcs[0].gameObject : null;
    }
}
#endif
