$assets = (Resolve-Path (Join-Path $PSScriptRoot "..\Assets")).Path
$csv = Import-Csv (Join-Path $PSScriptRoot "unused-assets-20260515-115007.csv")
$orphanByPath = @{}
foreach ($r in $csv) { $orphanByPath[$r.path] = $r.category }
$exts = @('.png','.jpg','.fbx','.wav','.mp3','.prefab','.unity','.mat','.shader','.anim','.controller','.asset','.tga','.psd','.ogg','.flare','.cubemap')
$folders = @('Standard Assets','KY_effects','NatureStarterKit2','Realistic Terrain Collection','Nature textures pack',
    'Forest Grounds - Terrain Texture Pack','Fantasy Sfx','Particle Ribbon','UnityVS','_TerrainAutoUpgrade')
$results = @()
foreach ($f in $folders) {
    $dir = Join-Path $assets $f
    if (-not (Test-Path $dir)) { continue }
    $files = @(Get-ChildItem $dir -Recurse -File | Where-Object { $exts -contains $_.Extension.ToLower() })
    $used = 0; $orphan = 0; $missing = 0
    foreach ($file in $files) {
        $p = "Assets/" + ($file.FullName.Substring($assets.Length).TrimStart('\', '/') -replace '\\', '/')
        if (-not $orphanByPath.ContainsKey($p)) { $used++; continue }
        if ($orphanByPath[$p] -match 'Orphan') { $orphan++ } else { $used++ }
    }
    $results += [PSCustomObject]@{
        Folder = $f
        AuditedFiles = $files.Count
        UsedOrMissingFromCsv = $used
        Orphans = $orphan
        CanDeleteWholeFolder = ($files.Count -gt 0 -and $used -eq 0)
    }
}
$results | Format-Table -AutoSize
