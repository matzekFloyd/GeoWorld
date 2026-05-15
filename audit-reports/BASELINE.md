# GeoWorld asset audit — baseline (2026-05-15)

## Repo size

| When | `Assets/` on disk |
|------|-------------------|
| Before cleanup (2026-05-15) | **1242.9 MB** |
| After pack removal (2026-05-15) | **1209.1 MB** (~34 MB removed) |
| After full orphan CSV cleanup (2026-05-15) | **272.5 MB** (~970 MB total removed) |

Removed: `New Terrain.asset`, `Assets/_Recovery/`, `KY_effects/`, `Particle Ribbon/`, then all paths in `unused-assets-20260515-115007.csv` with an `Orphan*` category (~1047 files). Kept assets still referenced from `GeoWorldMain`, `_PREFABS`, `Resources`, and required `Standard Assets` / terrain / audio paths.

`.git/` was **891.1 MB** at baseline.

Re-measure after further quarantine/removal:

```powershell
(Get-ChildItem Assets -Recurse -File | Measure-Object Length -Sum).Sum / 1MB
```

Build size: run your usual CI / WebGL or StandaloneLinux64 build and compare player artifact size (not captured in this pass).

## Tooling added

| Item | Purpose |
|------|---------|
| `Assets/Editor/GeoWorldUnusedAssetAudit.cs` | Unity `AssetDatabase.GetDependencies` audit (authoritative) |
| `audit-reports/run-reference-scan.ps1` | Optional YAML/grep cross-check — **not reliable** when scenes are binary |
| `Assets/_Quarantine/` | Move targets here after human sign-off (not delete) |

### Run the authoritative audit

**Option A — Editor (Unity project already open):**

1. **GeoWorld → Assets → Audit Unused Assets**
2. Open the CSV under `audit-reports/unused-assets-<timestamp>.csv`
3. Review categories (see below)
4. After sign-off: **GeoWorld → Assets → Quarantine high-confidence orphans…**

**Option B — Batch (close the Unity Editor first):**

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" `
  -batchmode -quit `
  -projectPath "C:\Users\mathi\Projects\GeoWorld" `
  -executeMethod GeoWorldUnusedAssetAudit.RunBatchAndQuit `
  -logFile "audit-reports\unity-audit.log"
```

## Report columns

| Column | Meaning |
|--------|---------|
| `path` | Asset path under `Assets/` |
| `type` | Main asset type |
| `size_bytes` / `size_human` | File size on disk |
| `category` | See below |
| `reference_summary` | Short chain or `no references found` |

### Categories

| Category | Action |
|----------|--------|
| **OrphanHighConfidence** | Not reachable from build scenes, `_PREFABS`, `Resources`, or script `Resources.Load` paths; **not** in protected folders — candidate for quarantine after review |
| **OrphanSceneOnlyThirdParty** | Only used from demo/sample scenes in license packs — **do not bulk-delete** |
| **OrphanProtectedReview** | Under `_ASSETS`, `_PREFABS`, Standard Assets, etc. — **requires explicit human sign-off** |

## Protected folders (never auto-quarantined)

- `Assets/_ASSETS/`, `Assets/_PREFABS/`, `Assets/_SCENES/`
- `Assets/Standard Assets/`, terrain/VFX/audio packs (see script `ProtectedPrefixes`)

## Primary dependency roots

- **Build scenes:** `Start.unity`, `GeoWorldMain.unity` (Editor Build Settings)
- **Play Mode start scene** (if set)
- **`Assets/_PREFABS/`**, **`Assets/_SCENES/`**, **`Assets/Resources/`**
- **`Resources.Load`:** `Input/GeoWorldInputActions` → `Assets/Resources/Input/GeoWorldInputActions.txt`
- Third-party **`.unity`** files (secondary scan for scene-only usage)

## Smoke test after any quarantine

1. Play from **Start.unity** (or default Play Mode start scene)
2. Confirm **GeoWorldMain** loads; skills, enemies, HUD materials/VFX intact
3. CI: StandaloneLinux64 smoke build (`.github/workflows/unity-ci.yml`)

## Notes

- `GeoWorldObjectPools.cs` runtime pooling is unrelated to this asset pass.
- Grep-only scans cannot read **binary** `GeoWorldMain.unity`; use the Unity audit.
- `_TerrainAutoUpgrade/` is Unity-generated; verify against `_TERRAIN` before removing.
