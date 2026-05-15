# Deletes files listed as orphans in the Unity audit CSV (+ .meta). Skips missing paths.
param(
    [string]$CsvPath = (Join-Path $PSScriptRoot "unused-assets-20260515-115007.csv"),
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [switch]$WhatIf
)
$csv = Import-Csv $CsvPath
$orphans = $csv | Where-Object { $_.category -match '^Orphan' }
$deleted = 0; $skipped = 0; $bytes = 0L
foreach ($row in $orphans) {
    $rel = $row.path
    $full = Join-Path $ProjectRoot ($rel -replace '/', [IO.Path]::DirectorySeparatorChar)
    $meta = $full + ".meta"
    if (-not (Test-Path $full)) { $skipped++; continue }
    $bytes += (Get-Item $full).Length
    if ($WhatIf) { Write-Output "Would delete: $rel"; continue }
    Remove-Item $full -Force
    if (Test-Path $meta) { Remove-Item $meta -Force }
    $deleted++
}
if (-not $WhatIf) {
    $assets = Join-Path $ProjectRoot "Assets"
    $after = (Get-ChildItem $assets -Recurse -File | Measure-Object Length -Sum).Sum
    Write-Output "Deleted $deleted files ($([math]::Round($bytes/1MB,1)) MB), skipped $skipped missing."
    Write-Output "Assets/ now: $([math]::Round($after/1MB,1)) MB"
}
