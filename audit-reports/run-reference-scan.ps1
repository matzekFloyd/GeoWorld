# Cross-check when Unity batchmode is unavailable: GUID reference graph from YAML assets.
# Full dependency closure still requires GeoWorldUnusedAssetAudit in the Unity Editor.
param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"
$assets = Join-Path $ProjectRoot "Assets"
$exts = @(
    ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tif", ".tiff", ".exr", ".hdr",
    ".fbx", ".obj", ".wav", ".mp3", ".ogg", ".prefab", ".unity", ".mat", ".shader",
    ".anim", ".controller", ".asset", ".flare", ".cubemap", ".rendertexture"
)

$guidToPath = @{}
Get-ChildItem $assets -Recurse -Filter "*.meta" | ForEach-Object {
    $text = [System.IO.File]::ReadAllText($_.FullName)
    if ($text -match "guid:\s*([a-f0-9]{32})") {
        $assetPath = $_.FullName.Substring($assets.Length + 1) -replace '\\', '/'
        $assetPath = "Assets/" + $assetPath.Substring(0, $assetPath.Length - 5)
        $guidToPath[$Matches[1]] = $assetPath
    }
}

$pathToGuid = @{}
foreach ($kv in $guidToPath.GetEnumerator()) { $pathToGuid[$kv.Value] = $kv.Key }

$guidPattern = [regex]'guid:\s*([a-f0-9]{32})'
$refScanExt = @(".unity", ".prefab", ".mat", ".asset", ".controller", ".anim", ".overrideController", ".mask", ".playable", ".shader", ".unitypackage")

function Get-TransitiveUsed([string[]]$rootPaths) {
    $used = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $queue = [System.Collections.Generic.Queue[string]]::new()
    foreach ($r in $rootPaths) {
        if ([string]::IsNullOrWhiteSpace($r)) { continue }
        if ($used.Add($r)) { $queue.Enqueue($r) }
    }
    while ($queue.Count -gt 0) {
        $current = $queue.Dequeue()
        $full = Join-Path $ProjectRoot ($current -replace '/', [IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path $full -PathType Leaf)) { continue }
        $text = [System.IO.File]::ReadAllText($full)
        foreach ($m in $guidPattern.Matches($text)) {
            $g = $m.Groups[1].Value
            if (-not $guidToPath.ContainsKey($g)) { continue }
            $dep = $guidToPath[$g]
            if ($used.Add($dep)) { $queue.Enqueue($dep) }
        }
    }
    return $used
}

$primaryRoots = @(
    "Assets/_SCENES/Start.unity",
    "Assets/_SCENES/GeoWorldMain.unity"
)
Get-ChildItem (Join-Path $assets "_PREFABS") -Recurse -Filter "*.prefab" -ErrorAction SilentlyContinue | ForEach-Object {
    $primaryRoots += "Assets/" + ($_.FullName.Substring($assets.Length + 1) -replace '\\', '/')
}
Get-ChildItem (Join-Path $assets "Resources") -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
    if (-not $_.PSIsContainer) {
        $primaryRoots += "Assets/" + ($_.FullName.Substring($assets.Length + 1) -replace '\\', '/')
    }
}

$thirdPartyPrefixes = @(
    "Assets/Standard Assets/",
    "Assets/KY_effects/",
    "Assets/NatureStarterKit2/",
    "Assets/_ASSETS/"
)
$thirdPartyScenes = Get-ChildItem $assets -Recurse -Filter "*.unity" | Where-Object {
    $p = "Assets/" + ($_.FullName.Substring($assets.Length + 1) -replace '\\', '/')
    ($thirdPartyPrefixes | Where-Object { $p.StartsWith($_) }).Count -gt 0
} | ForEach-Object { "Assets/" + ($_.FullName.Substring($assets.Length + 1) -replace '\\', '/') }

$primaryUsed = Get-TransitiveUsed $primaryRoots
$sceneOnlyUsed = Get-TransitiveUsed ($thirdPartyScenes + $primaryRoots)

$audited = Get-ChildItem $assets -Recurse -File | Where-Object {
    $exts -contains $_.Extension.ToLower() -and $_.FullName -notmatch '[\\/]_Quarantine[\\/]'
} | ForEach-Object { "Assets/" + ($_.FullName.Substring($assets.Length + 1) -replace '\\', '/') }

$protected = @(
    "Assets/_ASSETS/", "Assets/_PREFABS/", "Assets/_SCENES/", "Assets/Standard Assets/",
    "Assets/KY_effects/", "Assets/NatureStarterKit2/", "Assets/Fantasy Sfx/"
)

$timestamp = [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")
$csvPath = Join-Path $PSScriptRoot "unused-assets-grep-$timestamp.csv"
$rows = New-Object System.Collections.Generic.List[string]
$rows.Add("path,type,size_bytes,size_human,category,reference_summary")

$orphanBytes = 0L
foreach ($path in ($audited | Sort-Object)) {
    if ($primaryUsed.Contains($path)) { continue }
    $full = Join-Path $ProjectRoot $path
    $size = (Get-Item $full).Length
    $type = [System.IO.Path]::GetExtension($path).TrimStart('.')
    $isProtected = ($protected | Where-Object { $path.StartsWith($_) }).Count -gt 0
    if ($sceneOnlyUsed.Contains($path)) {
        $cat = "OrphanSceneOnlyThirdParty"
        $ref = "grep: scene-only (see third-party .unity)"
    }
    elseif ($isProtected) {
        $cat = "OrphanProtectedReview"
        $ref = "no references found (protected folder)"
    }
    else {
        $cat = "OrphanHighConfidence"
        $ref = "no references found (grep scan)"
    }
    $orphanBytes += $size
    $human = if ($size -ge 1MB) { "{0:N2} MB" -f ($size / 1MB) } elseif ($size -ge 1KB) { "{0:N2} KB" -f ($size / 1KB) } else { "$size B" }
    $rows.Add(('"{0}",{1},{2},{3},{4},"{5}"' -f $path, $type, $size, $human, $cat, $ref))
}

$rows | Set-Content $csvPath -Encoding UTF8
$assetsBytes = (Get-ChildItem $assets -Recurse -File | Measure-Object -Property Length -Sum).Sum
$binaryScenes = Get-ChildItem $assets -Recurse -Filter "*.unity" | Where-Object {
    $_.Length -gt 0 -and -not ([System.IO.File]::ReadAllText($_.FullName).Contains("guid:"))
}
if ($binaryScenes.Count -gt 0) {
    Write-Warning "Found $($binaryScenes.Count) binary .unity scene(s). Grep scan cannot traverse them — results are NOT authoritative. Use GeoWorldUnusedAssetAudit in Unity."
}

$summary = @"
GeoWorld grep-based unused asset scan (fallback — UNRELIABLE if binary scenes exist)
UTC: $([DateTime]::UtcNow.ToString("o"))
Assets/ size: $([math]::Round($assetsBytes/1MB, 2)) MB
Primary roots: $($primaryRoots.Count) | Third-party scenes: $($thirdPartyScenes.Count)
Orphans listed: $($rows.Count - 1) | Payload: $([math]::Round($orphanBytes/1MB, 2)) MB
CSV: $csvPath
"@
$summaryPath = $csvPath -replace '\.csv$', '-summary.txt'
$summary | Set-Content $summaryPath -Encoding UTF8
Write-Output $summary
