#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Finds textures, meshes, audio, prefabs, scenes, and materials under <c>Assets/</c> that are not
/// reachable from build scenes, gameplay prefabs, Resources, or script-declared Resources paths.
/// Writes CSV + summary under <c>audit-reports/</c> at the project root. Does not delete assets;
/// use <b>GeoWorld → Assets → Quarantine high-confidence orphans…</b> after human review.
/// </summary>
static class GeoWorldUnusedAssetAudit
{
    const string ReportFolderName = "audit-reports";
    const string LastReportPrefKey = "GeoWorld.UnusedAssetAudit.LastReportCsv";

    static readonly string[] AuditedExtensions =
    {
        ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tif", ".tiff", ".exr", ".hdr", ".gif",
        ".fbx", ".obj", ".dae", ".blend", ".mb", ".ma",
        ".wav", ".mp3", ".ogg", ".aiff", ".aif",
        ".prefab", ".unity", ".mat", ".shader", ".shadergraph", ".anim", ".controller",
        ".asset", ".physicMaterial", ".physicsMaterial2D", ".flare", ".fontsettings",
        ".cubemap", ".rendertexture", ".spriteatlas", ".playable", ".mask", ".brush",
    };

    /// <summary>Folders that require explicit human sign-off before quarantine.</summary>
    static readonly string[] ProtectedPrefixes =
    {
        "Assets/_ASSETS/",
        "Assets/_PREFABS/",
        "Assets/_SCENES/",
        "Assets/_SCRIPTS/",
        "Assets/Resources/",
        "Assets/Standard Assets/",
        "Assets/Realistic Terrain Collection/",
        "Assets/NatureStarterKit2/",
        "Assets/Nature textures pack/",
        "Assets/Forest Grounds - Terrain Texture Pack/",
        "Assets/KY_effects/",
        "Assets/Fantasy Sfx/",
        "Assets/Particle Ribbon/",
        "Assets/Editor/",
        "Assets/Plugins/",
    };

    /// <summary>Third-party roots: all scenes here are extra dependency roots (scene-only usage).</summary>
    static readonly string[] ThirdPartySceneRoots =
    {
        "Assets/Standard Assets/",
        "Assets/Realistic Terrain Collection/",
        "Assets/NatureStarterKit2/",
        "Assets/Nature textures pack/",
        "Assets/Forest Grounds - Terrain Texture Pack/",
        "Assets/KY_effects/",
        "Assets/Fantasy Sfx/",
        "Assets/Particle Ribbon/",
        "Assets/_ASSETS/",
    };

    static readonly string[] PrimaryRoots =
    {
        "Assets/_SCENES/",
        "Assets/_PREFABS/",
        "Assets/Resources/",
        "Assets/_SCRIPTS/",
        "Assets/_TERRAIN/",
        "Assets/_GUI MATERIAL/",
        "Assets/Tree_Textures/",
    };

    [MenuItem("GeoWorld/Assets/Audit Unused Assets", false, 200)]
    public static void RunFromMenu()
    {
        var result = RunAudit();
        Debug.Log($"[GeoWorld] Unused asset audit complete. CSV: {result.CsvPath}\nSummary: {result.SummaryPath}");
        EditorUtility.RevealInFinder(result.CsvPath);
    }

    /// <summary>Batchmode entry: Unity -executeMethod GeoWorldUnusedAssetAudit.RunBatchAndQuit</summary>
    public static void RunBatchAndQuit()
    {
        try
        {
            var result = RunAudit();
            Debug.Log($"[GeoWorld] Batch audit OK. Orphans (high confidence): {result.HighConfidenceOrphanCount}. CSV: {result.CsvPath}");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError("[GeoWorld] Unused asset audit failed: " + ex);
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("GeoWorld/Assets/Quarantine high-confidence orphans (from last report)...", false, 201)]
    static void QuarantineFromLastReport()
    {
        var csvPath = EditorPrefs.GetString(LastReportPrefKey, "");
        if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
        {
            EditorUtility.DisplayDialog("GeoWorld audit", "Run GeoWorld → Assets → Audit Unused Assets first.", "OK");
            return;
        }

        var lines = File.ReadAllLines(csvPath).Skip(1).ToList();
        var candidates = new List<string>();
        foreach (var line in lines)
        {
            var cols = SplitCsvLine(line);
            if (cols.Count < 6)
                continue;
            var category = cols[4];
            var path = cols[0];
            if (category == "OrphanHighConfidence" && !IsProtected(path))
                candidates.Add(path);
        }

        if (candidates.Count == 0)
        {
            EditorUtility.DisplayDialog("GeoWorld audit", "No high-confidence, non-protected orphans in the last report.", "OK");
            return;
        }

        var msg =
            $"Move {candidates.Count} asset(s) to Assets/_Quarantine/UnusedAssetAudit/?\n\n" +
            "This does not delete files. Protected folders (_ASSETS, _PREFABS, license packs) are excluded.\n\n" +
            "Review the CSV before confirming.";
        if (!EditorUtility.DisplayDialog("Quarantine orphans", msg, "Quarantine", "Cancel"))
            return;

        QuarantinePaths(candidates);
    }

    public static AuditResult RunAudit()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
        var reportDir = Path.Combine(projectRoot, ReportFolderName);
        Directory.CreateDirectory(reportDir);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var csvPath = Path.Combine(reportDir, $"unused-assets-{timestamp}.csv");
        var summaryPath = Path.Combine(reportDir, $"unused-assets-{timestamp}-summary.txt");

        var primaryRoots = CollectPrimaryRoots();
        var thirdPartySceneRoots = CollectThirdPartyScenePaths();
        var resourcesPaths = CollectResourcesLoadPaths(projectRoot);

        var primaryUsed = CollectDependencyClosure(primaryRoots);
        foreach (var res in resourcesPaths)
        {
            if (AssetDatabase.LoadMainAssetAtPath(res) != null)
                MergeDependencies(primaryUsed, res);
        }

        var sceneOnlyUsed = CollectDependencyClosure(thirdPartySceneRoots);
        foreach (var p in primaryUsed)
            sceneOnlyUsed.Add(p);

        var allAudited = ListAuditedAssets();
        var rows = new List<AuditRow>();
        long orphanBytes = 0;
        int highConfidenceCount = 0;

        foreach (var path in allAudited)
        {
            if (primaryUsed.Contains(path))
                continue;

            var size = GetFileSize(path);
            var type = AssetDatabase.GetMainAssetTypeAtPath(path)?.Name ?? "Unknown";
            string category;
            string referenceSummary;

            if (sceneOnlyUsed.Contains(path))
            {
                category = "OrphanSceneOnlyThirdParty";
                referenceSummary = DescribeSceneOnlyReference(path, thirdPartySceneRoots);
            }
            else if (IsProtected(path))
            {
                category = "OrphanProtectedReview";
                referenceSummary = "no references found (protected folder — manual sign-off required)";
                orphanBytes += size;
            }
            else
            {
                category = "OrphanHighConfidence";
                referenceSummary = "no references found";
                orphanBytes += size;
                highConfidenceCount++;
            }

            rows.Add(new AuditRow(path, type, size, category, referenceSummary));
        }

        rows.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
        WriteCsv(csvPath, rows);
        WriteSummary(summaryPath, projectRoot, primaryRoots, thirdPartySceneRoots, resourcesPaths,
            primaryUsed.Count, allAudited.Count, rows, orphanBytes, highConfidenceCount);

        EditorPrefs.SetString(LastReportPrefKey, csvPath);

        return new AuditResult(csvPath, summaryPath, highConfidenceCount, rows.Count);
    }

    static List<string> CollectPrimaryRoots()
    {
        var roots = new HashSet<string>(StringComparer.Ordinal);

        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.enabled && !string.IsNullOrEmpty(s.path))
                roots.Add(s.path);
        }

        var playStart = EditorSceneManager.playModeStartScene;
        if (playStart != null)
        {
            var p = AssetDatabase.GetAssetPath(playStart);
            if (!string.IsNullOrEmpty(p))
                roots.Add(p);
        }

        foreach (var prefix in PrimaryRoots)
            AddAssetsUnder(roots, prefix);

        AddAssetsUnder(roots, "Assets/_PREFABS/", "t:Prefab");
        AddAssetsUnder(roots, "Assets/_SCENES/", "t:Scene");

        return roots.ToList();
    }

    static List<string> CollectThirdPartyScenePaths()
    {
        var scenes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var prefix in ThirdPartySceneRoots)
            AddAssetsUnder(scenes, prefix, "t:Scene");
        return scenes.ToList();
    }

    static void AddAssetsUnder(HashSet<string> dest, string folder, string filter = null)
    {
        if (!AssetDatabase.IsValidFolder(folder.TrimEnd('/')))
            return;
        var searchFilter = string.IsNullOrEmpty(filter) ? "" : filter;
        var guids = AssetDatabase.FindAssets(searchFilter, new[] { folder.TrimEnd('/') });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path) && !path.EndsWith(".meta", StringComparison.Ordinal))
                dest.Add(path);
        }
    }

    static HashSet<string> CollectDependencyClosure(IReadOnlyList<string> rootPaths)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var root in rootPaths)
            MergeDependencies(used, root);
        return used;
    }

    static void MergeDependencies(HashSet<string> used, string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            return;

        string[] deps;
        try
        {
            deps = AssetDatabase.GetDependencies(assetPath, true);
        }
        catch
        {
            return;
        }

        foreach (var dep in deps)
        {
            if (dep.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                dep.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                dep.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase))
                continue;
            used.Add(dep);
        }
    }

    static List<string> CollectResourcesLoadPaths(string projectRoot)
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        var scriptsDir = Path.Combine(projectRoot, "Assets");
        var pattern = new Regex(@"Resources\.Load(?:<[^>]+>)?\s*\(\s*""([^""]+)""", RegexOptions.Compiled);
        foreach (var file in Directory.EnumerateFiles(scriptsDir, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Replace('\\', '/').Contains("/Editor/"))
                continue;
            var text = File.ReadAllText(file);
            foreach (Match m in pattern.Matches(text))
            {
                var resourcePath = m.Groups[1].Value;
                var assetPath = $"Assets/Resources/{resourcePath}";
                if (File.Exists(assetPath))
                    paths.Add(assetPath);
                else if (File.Exists(assetPath + ".txt"))
                    paths.Add(assetPath + ".txt");
                else if (File.Exists(assetPath + ".asset"))
                    paths.Add(assetPath + ".asset");
            }
        }
        return paths.ToList();
    }

    static List<string> ListAuditedAssets()
    {
        var list = new List<string>();
        var guids = AssetDatabase.FindAssets("");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal))
                continue;
            if (path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                continue;
            if (path.StartsWith("Assets/_Quarantine/", StringComparison.Ordinal))
                continue;
            if (Directory.Exists(path))
                continue;
            var ext = Path.GetExtension(path);
            if (string.IsNullOrEmpty(ext))
                continue;
            if (!AuditedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                continue;
            list.Add(path);
        }
        return list;
    }

    static string DescribeSceneOnlyReference(string assetPath, IReadOnlyList<string> thirdPartyScenes)
    {
        var referencers = new List<string>();
        foreach (var scene in thirdPartyScenes)
        {
            if (scene == assetPath)
                continue;
            string[] deps;
            try
            {
                deps = AssetDatabase.GetDependencies(scene, true);
            }
            catch
            {
                continue;
            }
            if (deps.Contains(assetPath))
                referencers.Add(scene);
            if (referencers.Count >= 3)
                break;
        }

        if (referencers.Count == 0)
            return "possibly indirect / unlisted scene reference";
        var chain = string.Join(" → ", referencers.Take(2));
        if (referencers.Count > 2)
            chain += $" (+{referencers.Count - 2} more)";
        return $"scene-only via {chain}";
    }

    static bool IsProtected(string path)
    {
        foreach (var prefix in ProtectedPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    static long GetFileSize(string assetPath)
    {
        var full = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, assetPath);
        return File.Exists(full) ? new FileInfo(full).Length : 0;
    }

    static void WriteCsv(string csvPath, List<AuditRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("path,type,size_bytes,size_human,category,reference_summary");
        foreach (var r in rows)
            sb.AppendLine($"{CsvEscape(r.Path)},{CsvEscape(r.Type)},{r.SizeBytes},{CsvEscape(FormatBytes(r.SizeBytes))},{CsvEscape(r.Category)},{CsvEscape(r.ReferenceSummary)}");
        File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);
    }

    static void WriteSummary(string summaryPath, string projectRoot, List<string> primaryRoots,
        List<string> thirdPartyScenes, List<string> resourcesPaths, int usedCount, int auditedCount,
        List<AuditRow> orphans, long orphanBytes, int highConfidenceCount)
    {
        var assetsBytes = DirSizeBytes(Path.Combine(projectRoot, "Assets"));
        var sb = new StringBuilder();
        sb.AppendLine("GeoWorld unused asset audit");
        sb.AppendLine($"UTC: {DateTime.UtcNow:O}");
        sb.AppendLine($"Unity: {Application.unityVersion}");
        sb.AppendLine();
        sb.AppendLine("=== Size baseline ===");
        sb.AppendLine($"Assets/ on disk: {FormatBytes(assetsBytes)} ({assetsBytes:N0} bytes)");
        sb.AppendLine("Re-run after quarantine/removal and compare.");
        sb.AppendLine();
        sb.AppendLine("=== Roots (primary) ===");
        foreach (var r in primaryRoots.OrderBy(x => x))
            sb.AppendLine("  " + r);
        sb.AppendLine();
        sb.AppendLine("=== Resources.Load paths (from scripts) ===");
        foreach (var r in resourcesPaths.OrderBy(x => x))
            sb.AppendLine("  " + r);
        sb.AppendLine();
        sb.AppendLine($"Primary dependency closure: {usedCount} assets");
        sb.AppendLine($"Audited asset types scanned: {auditedCount}");
        sb.AppendLine($"Candidate orphans listed: {orphans.Count}");
        sb.AppendLine($"  OrphanHighConfidence (non-protected): {highConfidenceCount}");
        sb.AppendLine($"  OrphanSceneOnlyThirdParty: {orphans.Count(r => r.Category == "OrphanSceneOnlyThirdParty")}");
        sb.AppendLine($"  OrphanProtectedReview: {orphans.Count(r => r.Category == "OrphanProtectedReview")}");
        sb.AppendLine($"Total orphan payload (all categories): {FormatBytes(orphanBytes)}");
        sb.AppendLine();
        sb.AppendLine("=== Third-party scene roots (extra scan) ===");
        sb.AppendLine($"Scenes: {thirdPartyScenes.Count}");
        sb.AppendLine();
        sb.AppendLine("Human review required before quarantine/deletion for _ASSETS, _PREFABS, license packs.");
        sb.AppendLine("Use GeoWorld → Assets → Quarantine high-confidence orphans only after CSV review.");
        File.WriteAllText(summaryPath, sb.ToString(), Encoding.UTF8);
    }

    static void QuarantinePaths(IReadOnlyList<string> paths)
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var quarantineRoot = $"Assets/_Quarantine/UnusedAssetAudit_{stamp}";
        if (!AssetDatabase.IsValidFolder("Assets/_Quarantine"))
            AssetDatabase.CreateFolder("Assets", "_Quarantine");

        var folderName = $"UnusedAssetAudit_{stamp}";
        AssetDatabase.CreateFolder("Assets/_Quarantine", folderName);

        var moved = 0;
        foreach (var path in paths)
        {
            if (!File.Exists(Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, path)))
                continue;
            var relative = path.StartsWith("Assets/", StringComparison.Ordinal) ? path.Substring("Assets/".Length) : path;
            var dest = $"{quarantineRoot}/{relative}";
            var destDir = Path.GetDirectoryName(dest)!.Replace('\\', '/');
            EnsureAssetFolders(destDir);
            var err = AssetDatabase.MoveAsset(path, dest);
            if (string.IsNullOrEmpty(err))
                moved++;
            else
                Debug.LogWarning($"[GeoWorld] Quarantine skip {path}: {err}");
        }

        AssetDatabase.Refresh();
        Debug.Log($"[GeoWorld] Quarantined {moved}/{paths.Count} assets under {quarantineRoot}");
    }

    static void EnsureAssetFolders(string assetFolderPath)
    {
        var parts = assetFolderPath.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static long DirSizeBytes(string dir)
    {
        if (!Directory.Exists(dir))
            return 0;
        return Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
    }

    static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double size = bytes;
        var u = 0;
        while (size >= 1024 && u < units.Length - 1)
        {
            size /= 1024;
            u++;
        }
        return $"{size:0.##} {units[u]}";
    }

    static string CsvEscape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                    inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
                sb.Append(c);
        }
        result.Add(sb.ToString());
        return result;
    }

    readonly struct AuditRow
    {
        public readonly string Path;
        public readonly string Type;
        public readonly long SizeBytes;
        public readonly string Category;
        public readonly string ReferenceSummary;

        public AuditRow(string path, string type, long sizeBytes, string category, string referenceSummary)
        {
            Path = path;
            Type = type;
            SizeBytes = sizeBytes;
            Category = category;
            ReferenceSummary = referenceSummary;
        }
    }

    public readonly struct AuditResult
    {
        public readonly string CsvPath;
        public readonly string SummaryPath;
        public readonly int HighConfidenceOrphanCount;
        public readonly int TotalOrphanCount;

        public AuditResult(string csvPath, string summaryPath, int highConfidenceOrphanCount, int totalOrphanCount)
        {
            CsvPath = csvPath;
            SummaryPath = summaryPath;
            HighConfidenceOrphanCount = highConfidenceOrphanCount;
            TotalOrphanCount = totalOrphanCount;
        }
    }
}
#endif
