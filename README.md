# GeoWorld

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Unity **6** project (**6000.4.5f1**). A third-person style **survival / horde** prototype built around a **GeoMancer** player: manage health, mana, and XP on a timer while enemies scale with your level.

## What the game is

- **Core loop**: Stay alive, kill waves of enemies, level up (up to **50** in current player logic), and survive until the round timer expires—or lose if health hits zero.
- **Round rules** (`GameOver`): countdown duration, spawn density, greater/boss thresholds, and boss multipliers come from **`GameBalance`** (ScriptableObject) when assigned; **defaults** match the old design (**900** s round, **`level × 40`** target enemies, greater from level **10+**, boss attempts when level is a multiple of **5**). Kill counters cover normal and **greater** enemies; game over on death or time up.
- **Fantasy hook**: Earth / “geo” themed skills (projectiles, blast, meteor, time freeze, blood ritual, heal, etc.) against spawned enemies including stronger variants and periodic **boss** spawns tied to level milestones (`EnemyGenerator`).

Main scenes (under `Assets/_SCENES/`):

- **`Start.unity`** — Press **G** to load the main scene (`GameStart`).
- **`GeoWorldMain.unity`** — Primary gameplay scene.

## Gameplay systems (high level)

| Area | Role |
|------|------|
| **`PlayerCharacter`** | Level, XP curve, mana, health regen, leveling scales enemy stats via **`EnemyGenerator`**. Warns if the **`Spawn`** tag is missing (generator dependency). |
| **`EnemyGenerator`** | State machine (`Initialize` → `Setup` → `SpawnEnemy`): target living count and thresholds read **`GameBalanceHelper`** (from the active **`GameBalance`** asset or defaults). Boss instances from **`spawnEndBoss`** get **`EnemyCharacter.isBoss`** set at spawn. |
| **`SkillBasic` + skills** | Shared mana/cooldown helpers; cache **`PlayerCharacter`** / **`GameOver`** where refactored; input keys go through **`GameInput`**; skills respect **`GameOver`** when dead or time over. |
| **`UserInterface`** | Drives the gameplay **uGUI** HUD: bars, skill columns, crosshair, and low-health vignette. Reads player/skills in **`Update`** and pushes strings/fill amounts into **`GameplayHudView`** with dirty checks (no **`OnGUI`** on the hot path). |
| **`GameplayHudView`** | **`UnityEngine.UI`** Canvas built at runtime as a child **`GameplayHUD`** (see **Gameplay HUD** below). Hosts fullscreen skill flashes (heal / blood ritual / freeze) and caches **`Sprite`** instances per texture. |
| **`GameOver`** | Timer from balance, kill UI (**Unity UI `Text`**, null-safe), end screens. End-of-round copy updates in **`Update`** (no **`OnGUI`**). **Escape** uses **`GameInput`**. **WebGL**: no **`Application.Quit`**; copy prompts the player to close the tab. Standalone/editor use quit or exit play mode as appropriate. |
| **`BackgroundMusic`** | **`AudioSource`** loop; optional **`alternateTracks`** + random pick (pool includes **`backGroundMusic`** when set), **`playbackVolume`**. Subclasses **`GameOver`** only to stop on game-over flags (legacy layout). |

Enemy behaviour lives under `Assets/_SCRIPTS/Behaviour/` (`EnemyAI`, `GreaterEnemyAI`, `HomingMissileAI`, etc.).

### Gameplay HUD (uGUI)

- **Where it lives:** The same GameObject that has **`UserInterface`** (and usually **`GameOver`**) — often on the **player** in **`GeoWorldMain`**. At runtime, **`UserInterface`** ensures a **`GameplayHudView`** component on that object and creates a child **`GameplayHUD`** with a **Screen Space Overlay** Canvas, **`CanvasScaler`** (reference **2020×1136**, ~**95%** UI scale on 1080p vs. a 1920×1080 reference), bars, an 8-column skill strip, crosshair, low-health vignette, and fullscreen FX **`Image`**s.
- **Scripts:** `Assets/_SCRIPTS/GUI/GameplayHudView.cs` (layout + widgets), `Assets/_SCRIPTS/GUI/UserInterface.cs` (data + dirty refresh). **`HealSelf`**, **`BloodRitual`**, and **`FreezeTime`** push fullscreen overlays through **`GameplayHudView.Instance`** in **`LateUpdate`** (no **`OnGUI`**).
- **Customization:** Open **`GeoWorldMain`** in the Editor, select the object with **`UserInterface`**, expand **`GameplayHUD`** after entering Play once (or duplicate that subtree into `Assets/_PREFABS/` if you want a prefab-driven layout). Re-assign **`UserInterface`**’s **`Texture2D`** fields as before for bar/skill art.

## Tuning & configuration

| Asset / script | Purpose |
|----------------|---------|
| **`GameBalance`** (`Assets → Create → GeoWorld → Game Balance`) | Round length, `enemiesPerPlayerLevel`, greater/boss level rules, boss HP/XP multipliers and score bonus. Assign the asset on the **`GameOver`** component’s **`gameBalance`** field; if empty, **`GameBalanceHelper`** keeps the historical defaults. |
| **`GameInput`** (`Assets/_SCRIPTS/Config/GameInput.cs`) | Façade over the **Input System**: loads JSON from **`Assets/Resources/Input/GeoWorldInputActions.txt`** via **`InputActionAsset.LoadFromJson`**. Exposes the same static API as before (`FirePrimaryDown`, skill `*Up`, `PauseOrQuitUp`, etc.). |

### Changing default bindings (keyboard / mouse)

1. Edit **`Assets/Resources/Input/GeoWorldInputActions.txt`**. It is standard **Input Actions** JSON (same format as a `.inputactions` file). Adjust paths under the **Gameplay** map’s **`bindings`** (e.g. **FirePrimary** → `<Mouse>/leftButton`, skills **Q/E/R/F**, **PauseOrQuit** → `<Keyboard>/escape`, **DebugLevelUp** → **T**).
2. Save the file. **`GameInput`** loads **`Resources`** path **`Input/GeoWorldInputActions`** at runtime; no code changes for rebinding.
3. Optional: copy the JSON to a **`.inputactions`** file elsewhere in the project if you want Unity’s **Input Actions** visual editor, then paste changes back into **`GeoWorldInputActions.txt`** when done.
4. **Player settings**: Gameplay uses the **new Input System**; **Standard Assets** still use the **legacy** manager. The repo includes an Editor script that sets **Project Settings → Player → Active Input Handling** to **Both** so both stacks work. If you reset project settings, set **Both** again (or **Input System Package + Old**). **`GameInput`** also falls back to **`UnityEngine.Input`** when **`ENABLE_LEGACY_INPUT_MANAGER`** is defined, so skills and mouse still work if the new backend is not active (e.g. project stuck on **Input Manager (Old)** only).
5. **WebGL player builds**: **`GameInput`** uses **legacy `Input` only** (no `InputAction` reads) to avoid a **maximum call stack** / WASM–JS re-entrancy issue when mixing the two backends in the browser. Rebinding via **`GeoWorldInputActions.txt`** applies to **non-WebGL** targets; WebGL uses the default **Fire1** / **Q,E,R,F** / mouse buttons from the **Input Manager** (`ProjectSettings/InputManager.asset`).

## Repository layout

```
Assets/
  _SCRIPTS/          # Game code (Config/, characters, skills, GUI, AI, …)
  _SCENES/           # GameStart + GeoWorldMain (+ .meta)
  _ASSETS/, _PREFABS/, _TERRAIN/, etc.
  Standard Assets/   # Legacy Unity Standard Assets (effects, water, input, vehicles…)
Packages/
  manifest.json      # Includes com.unity.ugui, com.unity.inputsystem; mostly built-in modules
ProjectSettings/
```

**Design note**: The project mixes **old Standard Assets** (Unity 5 era) with **Unity 6**; much of the maintenance work is updating obsolete APIs in those packages and keeping the custom `_SCRIPTS` compiling.

## Development patterns (as implemented)

These reflect how the game was built historically—not necessarily current Unity “best practice”:

1. **MonoBehaviour-centric**  
   No formal service layer; systems are components on GameObjects, wired in the Inspector or found at runtime.

2. **Discovery by tag and `GetComponent`**  
   Many systems still resolve the player by tag **`Player1`**; gameplay/UI paths that were hot refactored cache **`PlayerCharacter`** / **`GameOver`** instead of calling **`GetComponent`** every frame.

3. **Dual UI stack**  
   - HUD and overlays: **`OnGUI`** (`UserInterface`, parts of `GameOver`).  
   - Some screens/widgets: **`UnityEngine.UI`** (`Text` on `GameOver`).

4. **Inheritance for skills**  
   `SkillBasic` base class (mana, cooldown, reference to player); concrete skills (`GeoShot`, `Meteor`, …) override behaviour in `Update` and input.

5. **Explicit state machine for spawning**  
   `EnemyGenerator` uses an enum `State` and a `switch` in `Update` rather than coroutines or async.

6. **Balancing and TODOs in-repo**  
   Runtime tuning prefers **`GameBalance`** + **`GameBalanceHelper`** over scattered magic numbers. See `Assets/TO-DO.txt` (German): boss tuning, damage floaters, lifesteal edge cases, meteor VFX vs level, etc.

## Requirements

- **Unity Hub** + Editor **6000.4.5f1** (or compatible **Unity 6.4** line; project version is in `ProjectSettings/ProjectVersion.txt`).
- Open the project folder that contains **`Assets`**, **`Packages`**, and **`ProjectSettings`** (repository root).

## Build output

Do **not** set the build output to the project root. Use a subfolder, e.g. `Builds/Windows` or `Builds/WebGL`.

## WebGL CI and Netlify

GitHub Actions builds **WebGL** with [game-ci `unity-builder`](https://game.ci/docs/github/builder) and deploys the static output to [Netlify](https://www.netlify.com/) on:

- pushes to **`master`** or **`main`**
- tags matching **`v*`** (e.g. `v1.0.0`), including release tags you push from git—avoid duplicating a separate “on release” trigger so the workflow does not run twice for the same tag
- manual runs (**Actions → WebGL → Netlify → Run workflow**)

The WebGL player output in CI is **`Build/WebGL/GeoWorld/`** (platform folder + `buildName`; see `.github/workflows/webgl-netlify.yml`). Netlify must publish **that** folder so `index.html` is at the site root—publishing only `Build/WebGL/` leaves a nested folder and yields a **Page not found** on `/`. Root [`netlify.toml`](netlify.toml) sets caching and compression-related headers for typical Unity WebGL files. If you use **Brotli/Gzip** compression and the build still fails to load, enable **Player Settings → Publishing Settings → Decompression Fallback** (see Unity’s WebGL hosting docs), or adjust headers to match your exact output filenames.

### GitHub repository secrets

| Secret | Purpose |
|--------|---------|
| `UNITY_EMAIL` | Unity ID email (same as [activation](https://game.ci/docs/github/activation)). |
| `UNITY_PASSWORD` | Unity ID password. GameCI recommends avoiding special characters in this password. |
| `UNITY_LICENSE` | **Personal:** full contents of the license `.ulf` file from manual activation. **Not** used the same way for Pro—see GameCI “Professional license”. |
| `NETLIFY_AUTH_TOKEN` | Netlify personal access token (Dashboard → User settings → Applications → Personal access tokens). |
| `NETLIFY_SITE_ID` | Site ID (Site configuration → Site details → Site ID). |

**Unity Pro / Plus:** follow GameCI’s professional license env vars and add a repository secret `UNITY_SERIAL`, then extend the workflow’s `env` on the `game-ci/unity-builder` step with `UNITY_SERIAL: ${{ secrets.UNITY_SERIAL }}` (do **not** add `UNITY_LICENSE` for the Pro flow if GameCI’s docs say to omit it for your license type).

### Netlify site settings

1. Create a site (empty starter is fine). **Build command** and **publish directory** in the UI are optional if you **only** deploy from GitHub Actions—the CLI `--dir=Build/WebGL` publishes artifacts directly.
2. Ensure the GitHub repo has the secrets above. The first successful workflow run should attach a production deploy to that site.
3. If you use **multithreaded WebGL**, you may need **COOP/COEP** headers; see commented block in `netlify.toml` and Unity’s threading/hosting notes before enabling.

### Docker image availability

The workflow uses `unityVersion: auto` (reads `ProjectSettings/ProjectVersion.txt`). If CI reports a missing `unityci/editor` image for your exact patch version, check [GameCI Docker versions](https://game.ci/docs/docker/versions) or set `customImage` / `unityVersion` on the builder step to a published tag.

### Troubleshooting: “Page not found” on Netlify

- **Wrong publish folder:** The playable files must sit at the **root** of the deployed directory (see **`Build/WebGL/GeoWorld/`** above). Deploying the parent `Build/WebGL/` folder alone nests `index.html` one level down.
- **Netlify’s own Git builds:** If the site is also connected to GitHub for automatic builds, Netlify may deploy the raw repo (no WebGL build) after your Action runs and **overwrite** a good deploy. Prefer **stopping Netlify builds** / disconnecting repo deploys when using **only** CLI deploy from Actions.

## Contributing / picking it back up

1. Open **`GeoWorldMain`** from `Assets/_SCENES` (or run from **`Start`** and press **G**).  
2. Optional: create a **`Game Balance`** asset and assign it on **`GameOver`** so round length and spawn rules are editable without code changes (see **Tuning & configuration**).  
3. Prefer fixing gameplay in **`Assets/_SCRIPTS`**; treat **`Standard Assets`** as legacy third-party code unless you plan a full replacement.  
4. After big Unity upgrades, expect more obsolete API warnings in **Standard Assets**; the custom game scripts are the source of truth for design intent.

## License

Your original project code in this repository is licensed under the **MIT License** — see [`LICENSE`](LICENSE).

**Third-party content** (for example **Unity Standard Assets** and Asset Store / pack assets under `Assets/`) may be governed by **other** licenses from Unity Technologies or those publishers. The MIT license applies to what you own and contribute here, not necessarily to every file in `Assets/`.

---