#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Phase 1 cube world: visible cube pedestal under terrain; gameplay uses terrain height/collider.
/// Menu: <b>GeoWorld → World → Apply Cube World (Phase 1)</b> · <b>Refit Cube To Terrain</b> · <b>Restore Terrain</b>.
/// Batch: <c>-executeMethod GeoWorldCubeWorldSetup.ApplyCubeWorldBatchAndQuit</c>
/// </summary>
static class GeoWorldCubeWorldSetup
{
    const string MainScenePath = "Assets/_SCENES/GeoWorldMain.unity";
    const string CubeWorldRootName = "CubeWorld";
    const float TopFaceY = 0f;
    const float EnemySpawnClearance = 0.35f;
    /// <summary>Cube top sits this far below terrain base to prevent z-fighting (terrain draws on top).</summary>
    const float CubeTopGapBelowTerrain = 0.5f;

    // Fallback when no terrain is in the scene yet.
    const float DefaultHalfWidth = 260f;
    const float DefaultHalfDepth = 235f;

    static readonly string[] TerrainDataPaths =
    {
        "Assets/_TERRAIN/TerrainData_7740e17a-5caa-4eff-a26f-bf6720839bf6.asset",
        "Assets/_TERRAIN/TerrainData_6d501390-5315-48d1-9f30-7f9dc816afd4.asset",
    };

    struct TerrainFootprint
    {
        public float Width;
        public float Depth;
        public float HalfWidth;
        public float HalfDepth;
        public float CubeDepth;
        public Vector3 Center;
        public Vector3 Corner;
    }

    [MenuItem("GeoWorld/World/Restore Terrain (visual only)", false, 31)]
    static void RestoreTerrainFromMenu()
    {
        try
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var hadTerrain = FindTerrainObject() != null;
            ConfigureTerrainForCubeWorld();
            var recreated = !hadTerrain;
            RefitCubeWorldToTerrain(saveScene: false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            var message = recreated
                ? "Terrain was recreated and aligned on the cube pedestal. Walk on terrain; cube mesh stays hidden below."
                : "Terrain is visible and aligned. Walk on terrain hills; cube mesh stays hidden below.";
            EditorUtility.DisplayDialog("Terrain restored", message, "OK");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[GeoWorld] Restore terrain failed: " + ex);
            EditorUtility.DisplayDialog("Terrain", "Restore failed. See Console.", "OK");
        }
    }

    [MenuItem("GeoWorld/World/Refit Cube To Terrain", false, 32)]
    static void RefitCubeToTerrainFromMenu()
    {
        try
        {
            RefitCubeWorldToTerrain(saveScene: true);
            EditorUtility.DisplayDialog(
                "Cube refit",
                "CubeWorld size and position now match the terrain footprint. Terrain base sits on the cube top.",
                "OK");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[GeoWorld] Refit cube to terrain failed: " + ex);
            EditorUtility.DisplayDialog("Cube refit", "Refit failed. See Console.", "OK");
        }
    }

    public static void RestoreTerrainBatchAndQuit()
    {
        try
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            ConfigureTerrainForCubeWorld();
            RefitCubeWorldToTerrain(saveScene: false);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GeoWorld] Terrain restored (batch mode).");
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[GeoWorld] Restore terrain batch failed: " + ex);
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("GeoWorld/World/Apply Cube World (Phase 1)", false, 30)]
    static void ApplyFromMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Cube World (Phase 1)",
                "Updates GeoWorldMain: cube sized to terrain, terrain aligned on cube top (+Y play face), flattened spawns, edge walls/kill zones.\n\nSave your scene first if you have other edits.",
                "Apply",
                "Cancel"))
            return;

        try
        {
            ApplyToMainScene(saveScene: true);
            EditorUtility.DisplayDialog("Cube World", "Phase 1 cube world applied to GeoWorldMain.", "OK");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[GeoWorld] Cube world setup failed: " + ex);
            EditorUtility.DisplayDialog("Cube World", "Setup failed. See Console for details.", "OK");
        }
    }

    public static void ApplyCubeWorldBatchAndQuit()
    {
        try
        {
            ApplyToMainScene(saveScene: true);
            Debug.Log("[GeoWorld] Phase 1 cube world applied (batch mode).");
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[GeoWorld] Cube world batch setup failed: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void ApplyToMainScene(bool saveScene)
    {
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
            throw new System.InvalidOperationException("Could not open " + MainScenePath);

        ConfigureTerrainForCubeWorld();
        var footprint = GetFootprintForScene();
        AlignTerrainToCubeTop(footprint);
        RemoveStaleCubeWorld();
        BuildCubeWorld(footprint);
        RepositionSpawnsAndPlayer();
        WireEnemyGenerator();
        RepositionOrCreateEdgeContainment(footprint);
        CleanupGravityAndMissingScripts();

        EditorSceneManager.MarkSceneDirty(scene);
        if (saveScene)
            EditorSceneManager.SaveScene(scene);
    }

    static void RefitCubeWorldToTerrain(bool saveScene)
    {
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
            throw new System.InvalidOperationException("Could not open " + MainScenePath);

        var terrain = GetTerrainComponent();
        if (terrain == null)
            throw new System.InvalidOperationException("No Terrain in scene. Run Restore Terrain first.");

        var footprint = ComputeFootprint(terrain);
        AlignTerrainToCubeTop(footprint);
        RemoveStaleCubeWorld();
        BuildCubeWorld(footprint);
        RepositionSpawnsAndPlayer();
        RepositionOrCreateEdgeContainment(footprint);
        CleanupGravityAndMissingScripts();

        EditorSceneManager.MarkSceneDirty(scene);
        if (saveScene)
            EditorSceneManager.SaveScene(scene);

        Debug.Log(
            $"[GeoWorld] Cube refit to terrain: {footprint.Width:F1} x {footprint.Depth:F1} footprint, depth {footprint.CubeDepth:F1}, center {footprint.Center}.");
    }

    static TerrainFootprint GetFootprintForScene()
    {
        var terrain = GetTerrainComponent();
        if (terrain != null)
            return ComputeFootprint(terrain);

        Debug.LogWarning("[GeoWorld] No terrain found; using spawn bounds for cube footprint.");
        return FootprintFromSpawnBounds();
    }

    static TerrainFootprint ComputeFootprint(Terrain terrain)
    {
        var data = terrain.terrainData;
        var size = data.size;
        var pos = terrain.transform.position;

        var footprint = new TerrainFootprint
        {
            Width = size.x,
            Depth = size.z,
            HalfWidth = size.x * 0.5f,
            HalfDepth = size.z * 0.5f,
            CubeDepth = Mathf.Max(size.x, size.z),
            Corner = new Vector3(pos.x, TopFaceY, pos.z),
            Center = new Vector3(pos.x + size.x * 0.5f, TopFaceY, pos.z + size.z * 0.5f),
        };
        return footprint;
    }

    static TerrainFootprint FootprintFromSpawnBounds()
    {
        var xz = new List<Vector2>();
        foreach (var go in EnumerateSpawnObjects())
        {
            if (go == null)
                continue;
            var p = go.transform.position;
            xz.Add(new Vector2(p.x, p.z));
        }

        var player = GameObject.FindGameObjectWithTag("Player1");
        if (player != null)
        {
            var p = player.transform.position;
            xz.Add(new Vector2(p.x, p.z));
        }

        float minX;
        float maxX;
        float minZ;
        float maxZ;
        if (xz.Count == 0)
        {
            minX = -DefaultHalfWidth;
            maxX = DefaultHalfWidth;
            minZ = -DefaultHalfDepth;
            maxZ = DefaultHalfDepth;
        }
        else
        {
            minX = xz.Min(v => v.x);
            maxX = xz.Max(v => v.x);
            minZ = xz.Min(v => v.y);
            maxZ = xz.Max(v => v.y);
        }

        var width = maxX - minX;
        var depth = maxZ - minZ;
        var corner = new Vector3(minX, TopFaceY, minZ);
        return new TerrainFootprint
        {
            Width = width,
            Depth = depth,
            HalfWidth = width * 0.5f,
            HalfDepth = depth * 0.5f,
            CubeDepth = Mathf.Max(width, depth),
            Corner = corner,
            Center = new Vector3(minX + width * 0.5f, TopFaceY, minZ + depth * 0.5f),
        };
    }

    /// <summary>Seat terrain base on the cube top; corner aligned to footprint.</summary>
    static void AlignTerrainToCubeTop(TerrainFootprint footprint)
    {
        var terrain = GetTerrainComponent();
        if (terrain == null)
            return;

        terrain.transform.position = footprint.Corner;
        EditorUtility.SetDirty(terrain.gameObject);
        Debug.Log($"[GeoWorld] Terrain aligned: corner {footprint.Corner}, size {footprint.Width:F1} x {footprint.Depth:F1}.");
    }

    static IEnumerable<GameObject> EnumerateSpawnObjects()
    {
        for (var i = 1; i <= 20; i++)
        {
            var go = GameObject.Find("SpawnPoint " + i);
            if (go != null)
                yield return go;
        }

        for (var i = 1; i <= 4; i++)
        {
            var go = GameObject.Find("SpawnPointEndBoss " + i);
            if (go != null)
                yield return go;
        }
    }

    static void ConfigureTerrainForCubeWorld()
    {
        var hadTerrain = FindTerrainObject() != null;
        var terrainGo = EnsureTerrainGameObject();

        terrainGo.SetActive(true);

        var terrainCollider = terrainGo.GetComponent<TerrainCollider>();
        if (terrainCollider != null)
            terrainCollider.enabled = true;

        var terrain = terrainGo.GetComponent<Terrain>();
        if (terrain != null)
            terrain.drawHeightmap = true;

        if (!hadTerrain)
            Debug.Log("[GeoWorld] Terrain GameObject was missing; recreated from Assets/_TERRAIN.");
        else
            Debug.Log("[GeoWorld] Terrain collider enabled for walking; cube pedestal visible below terrain.");
    }

    static float SampleTerrainSurfaceY(Terrain terrain, float worldX, float worldZ)
    {
        if (terrain == null)
            return TopFaceY;
        return terrain.SampleHeight(new Vector3(worldX, 0f, worldZ));
    }

    static float GetPlayerSpawnY(Terrain terrain, GameObject player, float worldX, float worldZ)
    {
        var ground = SampleTerrainSurfaceY(terrain, worldX, worldZ);
        if (player == null)
            return ground + 1.8f;

        var cc = player.GetComponent<CharacterController>();
        if (cc == null)
            return ground + 1.8f;

        return ground + cc.height * 0.5f + cc.skinWidth + 0.05f;
    }

    static Terrain GetTerrainComponent()
    {
        var go = FindTerrainObject();
        return go != null ? go.GetComponent<Terrain>() : null;
    }

    static GameObject FindTerrainObject()
    {
        var named = GameObject.Find("Terrain");
        if (named != null)
            return named;

        foreach (var terrain in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (terrain != null && terrain.gameObject != null)
                return terrain.gameObject;
        }

        return null;
    }

    static TerrainData LoadTerrainDataAsset()
    {
        foreach (var path in TerrainDataPaths)
        {
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (data != null)
                return data;
        }

        foreach (var guid in AssetDatabase.FindAssets("t:TerrainData", new[] { "Assets/_TERRAIN" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (data != null)
                return data;
        }

        return null;
    }

    static GameObject EnsureTerrainGameObject()
    {
        var existing = FindTerrainObject();
        if (existing != null)
            return existing;

        var data = LoadTerrainDataAsset();
        if (data == null)
        {
            throw new System.InvalidOperationException(
                "No TerrainData asset under Assets/_TERRAIN. Cannot recreate the Terrain GameObject.");
        }

        var terrainGo = Terrain.CreateTerrainGameObject(data);
        terrainGo.name = "Terrain";
        terrainGo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        Undo.RegisterCreatedObjectUndo(terrainGo, "Recreate Terrain");
        Debug.Log("[GeoWorld] Recreated Terrain from '" + AssetDatabase.GetAssetPath(data) + "'.");
        return terrainGo;
    }

    static void RemoveStaleCubeWorld()
    {
        var existing = GameObject.Find(CubeWorldRootName);
        if (existing != null)
            Object.DestroyImmediate(existing);
    }

    static void BuildCubeWorld(TerrainFootprint footprint)
    {
        var root = new GameObject(CubeWorldRootName);
        root.transform.position = footprint.Center;

        var cubeLocalPos = new Vector3(0f, -(footprint.CubeDepth * 0.5f + CubeTopGapBelowTerrain), 0f);
        var cubeLocalScale = new Vector3(footprint.Width, footprint.CubeDepth, footprint.Depth);

        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "GeoCubeVisual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = cubeLocalScale;
        visual.transform.localPosition = cubeLocalPos;
        var visualCol = visual.GetComponent<Collider>();
        if (visualCol != null)
            Object.DestroyImmediate(visualCol);

        var physics = new GameObject("CubePhysics");
        physics.transform.SetParent(root.transform, false);
        physics.transform.localScale = cubeLocalScale;
        physics.transform.localPosition = cubeLocalPos;
        var box = physics.AddComponent<BoxCollider>();
        box.isTrigger = false;

        var renderer = visual.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
            var mat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            if (mat != null)
                renderer.sharedMaterial = mat;
        }

        EnsureCubeWorldRuntimeComponents(root, footprint);

        Debug.Log(
            $"[GeoWorld] CubeWorld at {footprint.Center}: visible pedestal {footprint.Width:F0}x{footprint.Depth:F0}, top {CubeTopGapBelowTerrain}u under terrain.");
    }

    static void EnsureCubeWorldRuntimeComponents(GameObject cubeWorldRoot, TerrainFootprint footprint)
    {
        var anchor = cubeWorldRoot.GetComponent<CubeWorldAnchor>();
        if (anchor == null)
            anchor = cubeWorldRoot.AddComponent<CubeWorldAnchor>();
        anchor.Configure(footprint.Center, footprint.Width, footprint.Depth, TopFaceY);
    }

    static void CleanupGravityAndMissingScripts()
    {
        var cubeWorld = GameObject.Find(CubeWorldRootName);
        if (cubeWorld != null)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(cubeWorld);
            foreach (Transform child in cubeWorld.transform)
            {
                if (child != null)
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
            }
        }

        var player = GameObject.FindGameObjectWithTag("Player1");
        if (player != null)
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(player);
    }

    static void RepositionSpawnsAndPlayer()
    {
        var terrain = GetTerrainComponent();

        foreach (var go in EnumerateSpawnObjects())
        {
            if (go == null)
                continue;
            var p = go.transform.position;
            var ground = SampleTerrainSurfaceY(terrain, p.x, p.z);
            go.transform.position = new Vector3(p.x, ground + EnemySpawnClearance, p.z);
        }

        var player = GameObject.FindGameObjectWithTag("Player1");
        if (player != null)
        {
            var p = player.transform.position;
            var spawnY = GetPlayerSpawnY(terrain, player, p.x, p.z);
            player.transform.position = new Vector3(p.x, spawnY, p.z);
        }

        Debug.Log("[GeoWorld] Spawn points and player placed on terrain surface heights.");
    }

    static void WireEnemyGenerator()
    {
        var gen = Object.FindFirstObjectByType<EnemyGenerator>();
        if (gen == null)
        {
            Debug.LogWarning("[GeoWorld] EnemyGenerator not found; spawn arrays not wired.");
            return;
        }

        var spawns = new List<GameObject>();
        for (var i = 1; i <= 20; i++)
        {
            var go = GameObject.Find("SpawnPoint " + i);
            if (go != null)
                spawns.Add(go);
        }

        var greater = new List<GameObject>();
        GameObject endBoss = null;
        for (var i = 1; i <= 4; i++)
        {
            var go = GameObject.Find("SpawnPointEndBoss " + i);
            if (go == null)
                continue;
            if (endBoss == null)
                endBoss = go;
            greater.Add(go);
        }

        var so = new SerializedObject(gen);
        so.FindProperty("spawnPoints").arraySize = spawns.Count;
        for (var i = 0; i < spawns.Count; i++)
            so.FindProperty("spawnPoints").GetArrayElementAtIndex(i).objectReferenceValue = spawns[i];

        so.FindProperty("greaterEnemySpawnPoints").arraySize = greater.Count;
        for (var i = 0; i < greater.Count; i++)
            so.FindProperty("greaterEnemySpawnPoints").GetArrayElementAtIndex(i).objectReferenceValue = greater[i];

        so.FindProperty("endBossSpawnPoint").objectReferenceValue = endBoss;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(gen);
        Debug.Log($"[GeoWorld] EnemyGenerator wired: {spawns.Count} spawns, {greater.Count} greater/boss points.");
    }

    static void RepositionOrCreateEdgeContainment(TerrainFootprint footprint)
    {
        DisableLegacyInvisibleWalls();

        var root = GameObject.Find(CubeWorldRootName);
        if (root == null)
            throw new System.InvalidOperationException("CubeWorld root missing after build.");

        var wallsRoot = root.transform.Find("EdgeWalls");
        if (wallsRoot != null)
            ClearChildren(wallsRoot);

        var killsRoot = root.transform.Find("EdgeKillZones");
        if (killsRoot != null)
            ClearChildren(killsRoot);

        RemoveAllArenaEdgeKillZones();

        Debug.Log("[GeoWorld] Edge kill zones removed for cube play area.");
    }

    static void RemoveAllArenaEdgeKillZones()
    {
        foreach (var zone in Object.FindObjectsByType<ArenaEdgeKillZone>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (zone != null && zone.gameObject != null)
                Object.DestroyImmediate(zone.gameObject);
        }
    }

    static void DisableLegacyInvisibleWalls()
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go == null || !go.name.StartsWith("Invisible Wall"))
                continue;
            go.SetActive(false);
        }
    }

    static Transform GetOrCreateChild(Transform parent, string childName)
    {
        var t = parent.Find(childName);
        if (t != null)
            return t;
        var go = new GameObject(childName);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static void ClearChildren(Transform parent)
    {
        for (var i = parent.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

}
#endif
