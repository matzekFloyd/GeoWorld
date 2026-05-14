# 🌍 GeoWorld

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE) [![Unity CI](https://github.com/matzekFloyd/GeoWorld/actions/workflows/unity-ci.yml/badge.svg)](https://github.com/matzekFloyd/GeoWorld/actions/workflows/unity-ci.yml) [![WebGL — Netlify & GitHub releases](https://github.com/matzekFloyd/GeoWorld/actions/workflows/webgl-netlify.yml/badge.svg)](https://github.com/matzekFloyd/GeoWorld/actions/workflows/webgl-netlify.yml)

Unity **6** project (**6000.4.5f1**). A third-person style **survival / horde** prototype built around a **GeoMancer** player: manage health, mana, and XP on a timer while enemies scale with your level.

## What the game is

- **Core loop**: Stay alive, kill waves of enemies, level up (up to **50** in current player logic), and survive until the round timer expires—or lose if health hits zero.
- **Round rules** (`GameOver`): countdown, spawn targets, greater/boss gates, and boss encounter tuning come from a **`GameBalance`** asset when assigned on **`GameOver`**; if none is assigned, **`GameBalanceHelper`** applies built-in defaults (see **Game Balance asset** under **Tuning & configuration**). Kill UI: **enemies** (all kills), **greater** (non-boss greater only), **bosses defeated** plus optional **boss bonus score**; game over on death or time up.
- **Fantasy hook**: Earth / “geo” themed skills (projectiles, blast, meteor, time freeze, blood ritual, heal, etc.) against spawned enemies including stronger variants and periodic **boss** spawns tied to level milestones (`EnemyGenerator`), with a telegraphed **boss incoming** moment (HUD banner + tint + optional SFX) before the boss entity appears.

Main scenes (under `Assets/_SCENES/`):

- **`Start.unity`** — Title screen (**GeoWorld** / “Press any key or click to start”); `GameStart` loads **`GeoWorldMain`** on the first key or mouse button (see `Assets/_SCENES/GameStart.cs`). **Editor:** Unity’s Play button normally runs the **scene you have open**; this repo sets **`Start.unity`** as the default **Play Mode Start Scene** on domain reload (`Assets/Editor/GeoWorldPlayModeStartScene.cs`) so you still see the title flow while working on other scenes. To change that: **GeoWorld → Play Mode → Use currently open scene when pressing Play** (or **Project Settings → Editor → Play Mode** and assign another scene).
- **`GeoWorldMain.unity`** — Primary gameplay scene.

## Gameplay systems (high level)

| Area | Role |
|------|------|
| **`PlayerCharacter`** | Level, XP curve, mana, health regen, leveling scales enemy stats via **`EnemyGenerator`**. Warns if the **`Spawn`** tag is missing (generator dependency). |
| **`EnemyGenerator`** | State machine (`Initialize` → `Setup` → `SpawnEnemy`): target living count from **`GameBalanceHelper`**. **Boss:** when player level ≥ `greaterEnemiesMinPlayerLevel` and level % `bossSpawnLevelMultiple` == 0, starts a **telegraph** (real-time, from `bossTelegraphDurationSeconds`) then spawns if no boss is already alive; sets **`EnemyCharacter.isBoss`**. Spawn position: **`endBossSpawnPoint`** if set, else a random non-null **`greaterEnemySpawnPoints`** entry. **`spawnEndBoss()`** still exists for immediate spawns (no telegraph). |
| **`SkillBasic` + skills** | Shared mana/cooldown helpers; cache **`PlayerCharacter`**; input keys go through **`GameInput`**; skills respect **`GameSession.IsRunActive`** (driven by **`GameOver`**) when dead or time over. |
| **`UserInterface`** | Drives the gameplay **uGUI** HUD: bars, skill columns, crosshair, and low-health vignette. Reads player/skills in **`Update`** and pushes strings/fill amounts into **`GameplayHudView`** with dirty checks (no **`OnGUI`** on the hot path). Uses **`GameSession`** for whether the HUD/minimap should show during an active run. |
| **`GameplayHudView`** | **`UnityEngine.UI`** Canvas built at runtime as a child **`GameplayHUD`** (see **Gameplay HUD** below). Hosts fullscreen skill flashes (heal / blood ritual / freeze), **boss incoming** telegraph overlay, and caches **`Sprite`** instances per texture. |
| **`GameSession`** | Small façade: **`IsRunActive`**, **`Player`**. Created on the **`Player1`** object when **`GameOver.Start`** runs (and **`UserInterface.Start`** calls **`EnsureForScene`** early so HUD order is safe). **`GameOver`** calls **`SyncRunState`** each frame after updating death/time flags. |
| **`GameOver`** | Timer from balance, kill counters, and end-screen lines use **`UnityEngine.UI`** **`Text`** (null-safe). End-of-round strings refresh from **`Update`** when the outcome or kill counters change (not every frame once stable). **Enter** reloads **`GeoWorldMain`** (play again); **B** loads the title scene (**`Start`** by default); **Escape** still exits play mode (Editor), quits (standalone), or is documented for WebGL tab close. **No runtime `OnGUI`** here. |
| **`BackgroundMusic`** | Same GameObject as **`AudioSource`**: inspector **`backGroundMusic`**, **`alternateTracks`**, **`playbackVolume`**; random track rules in **Background music** under **Tuning & configuration**. Stops when **`GameSession`** reports the run has ended (treats a missing session as “still playing” until **`GameOver`** has started). |

Enemy behaviour lives under `Assets/_SCRIPTS/Behaviour/` (`EnemyAI`, `GreaterEnemyAI`, `HomingMissileAI`, etc.).

### Gameplay HUD (uGUI)

**Stack:** Gameplay-facing UI is built with **`UnityEngine.UI` (uGUI)** — not immediate-mode **`OnGUI`**. **`UserInterface`** + **`GameplayHudView`** own the HUD; **`MinimapRadar`** draws the bottom-left radar on the same Canvas stack; **`GameplayPause`** adds a pause overlay to that Canvas. **`GameOver`** uses scene-assigned **`Text`** components for the top-right round timer / kill counters during play and for the end-game scoreboard lines.

- **Where it lives:** The same GameObject that has **`UserInterface`** and **`GameOver`** — often on the **player** in **`GeoWorldMain`**. At runtime, **`UserInterface`** ensures a **`GameplayHudView`** component on that object and creates a child **`GameplayHUD`** with a **Screen Space Overlay** Canvas, **`CanvasScaler`** (reference **2020×1136**, ~**95%** UI scale on 1080p vs. a 1920×1080 reference), bars, an 8-column skill strip, crosshair, low-health vignette, and fullscreen FX **`Image`**s.
- **Scripts:** `Assets/_SCRIPTS/GUI/GameplayHudView.cs` (layout + widgets), `Assets/_SCRIPTS/GUI/UserInterface.cs` (data + dirty refresh into the view). **`HealSelf`**, **`BloodRitual`**, and **`FreezeTime`** push fullscreen overlays through **`GameplayHudView.Instance`** in **`LateUpdate`**.
- **Title screen (`Start.unity`):** `GameStart` builds its own **Screen Space Overlay** Canvas in code (`Assets/_SCENES/GameStart.cs`) — also uGUI, not **`OnGUI`**.
- **Customization (art):** Open **`GeoWorldMain`**, select the object with **`UserInterface`**, and assign the **`Texture2D`** fields in the Inspector (health/mana/exp bars, skill icons, crosshair, frame, blood vignette textures, etc.). No code change needed for simple reskins.
- **Customization (layout / hierarchy):** The **`GameplayHUD`** subtree is created at runtime by **`GameplayHudView.EnsureBuilt`**. To adjust layout or add widgets, enter **Play** once, copy the generated **`GameplayHUD`** hierarchy (or maintain a prefab under **`Assets/_PREFABS/`** and wire it). If you replace the default hierarchy in the scene, assign a **`GameplayHudView`** reference on **`UserInterface`** so **`EnsureBuilt`** does not overwrite your edits (see class summary on **`UserInterface`**).

## Tuning & configuration

| Asset / script | Purpose |
|----------------|---------|
| **`GameBalance`** (ScriptableObject) | Round length, spawn targets, greater/boss gates and cadence, boss telegraph, boss HP/XP/score tuning. See **Game Balance asset** below for creation, assignment, and **`GameBalanceHelper`** fallbacks. |
| **`GameInput`** (`Assets/_SCRIPTS/Config/GameInput.cs`) | Façade over the **Input System**: loads JSON from **`Assets/Resources/Input/GeoWorldInputActions.txt`** via **`InputActionAsset.LoadFromJson`**. Exposes the same static API as before (`FirePrimaryDown`, skill `*Up`, `PauseOrQuitUp`, etc.). |

### Game Balance asset

**Create:** In the Unity Editor, **Assets → Create → GeoWorld → Game Balance**. This creates a **`GameBalance`** asset (see `Assets/_SCRIPTS/Config/GameBalance.cs`).

**Assign:** On the GameObject that has **`GameOver`** (same object as the round UI / HUD host in **`GeoWorldMain`** is typical), set the Inspector field **`gameBalance`** / **Game Balance** to that asset. At runtime, **`GameOver.Start`** calls **`GameBalanceHelper.Register(gameBalance)`** (`Assets/_SCRIPTS/GUI/GameOver.cs`).

**When unassigned:** `GameBalanceHelper.Active` is **null**. All reads go through **`GameBalanceHelper`** static getters in `Assets/_SCRIPTS/Config/GameBalance.cs`, which mirror the **default field values** on **`GameBalance`** and safe fallbacks:

| Area | Fallback when `Active == null` (or invalid field) |
|------|---------------------------------------------------|
| Round duration | **900** s |
| Enemies at player level 1 | **12** (also if asset field ≤ **0**) |
| Enemies at player level 2 | **28** (also if asset field ≤ **0**) |
| Enemies per level (level ≥ 3) | **22** (also if asset field ≤ **0**) |
| Greater enemies / boss gate level | **10** |
| Boss spawn level multiple | **5** (minimum **1** when asset present) |
| Boss telegraph duration | **2.2** s (if asset value ≤ **0.05** s, uses **2.2** s) |
| Boss telegraph tint alpha | **0.14** (clamped **0–1** when asset present) |
| Boss health multiplier | **3** |
| Boss EXP multiplier | **2** |
| Boss bonus XP flat | **250** |
| Boss score bonus on kill | **500** |

Gameplay scenes should still assign an asset for designers to tweak without hunting constants; the table above is what code does **today** if the reference is empty.

### Background music (`BackgroundMusic`)

Script: **`Assets/_SCRIPTS/BackgroundMusic.cs`**. Put **`BackgroundMusic`** on the **same GameObject** as an **`AudioSource`** (looping BGM).

| Inspector field | Role |
|-----------------|------|
| **`backGroundMusic`** | Primary clip when there are **no** alternate tracks, and part of the random pool when alternates exist. |
| **`alternateTracks`** | Optional array of extra clips. |
| **`playbackVolume`** | **`Range(0,1)`** — applied to **`AudioSource.volume`** in **`Start`**. |

**Random track selection** (`PickTrack`): If **`alternateTracks`** is **null** or **empty**, the clip is **`backGroundMusic`**. If alternates exist but **`backGroundMusic`** is **null**, Unity picks **`Random.Range(0, alternateTracks.Length)`**. If **both** are set, the pool size is **`alternateTracks.Length + 1`**: roll **`Random.Range(0, pool)`**; indices **`0 … Length−1`** map to **`alternateTracks[i]`**, and the last index picks **`backGroundMusic`**.

**End of round:** Playback stops when **`GameSession.Instance.IsRunActive`** is false. If **`GameSession`** is not yet created, music is treated as still allowed (see WebGL autoplay notes earlier in this README).

### Boss encounters (tuning & behaviour)

- **Cadence** (`EnemyGenerator` + `GameBalance`): a boss is **scheduled** when player level ≥ **`greaterEnemiesMinPlayerLevel`** and **`level % bossSpawnLevelMultiple == 0`** (defaults: first boss at level **10**, then **15**, **20**, …). Only **one living boss** is allowed in `targets` at a time to avoid spam.
- **Telegraph**: before the prefab spawns, **`GameplayHudView.PlayBossIncomingTelegraph`** shows a banner + purple screen tint for **`bossTelegraphDurationSeconds`** (real time, unscaled). **`GameplaySfx.PlayBossIncoming`** plays optional clip **`bossIncomingStinger`** on the **`GameplaySfx`** component (same object as **`UserInterface`**).
- **`EnemyCharacter.isBoss` at spawn:** The telegraphed spawn coroutine and **`EnemyGenerator.spawnEndBoss()`** both instantiate (or pool-acquire) **`endBossPrefab`**, then set **`bossChar.isBoss = true`** on the instance’s **`EnemyCharacter`** when that component exists (`Assets/_SCRIPTS/Behaviour/EnemyGenerator.cs`). Anything that counts boss kills, boss score, XP bonuses, or boss-specific VFX/audio keys off **`isBoss`**.
- **Manual boss prefabs / hand-placed bosses:** Prefabs or scene instances that are **not** spawned through those paths must have **`EnemyCharacter.isBoss`** set correctly in the **prefab** (or at runtime) so behaviour stays consistent with spawned bosses.
- **Counters & score** (`GameOver`): **`bossKillCounter`** counts boss defeats separately from **`greaterEnemyKillCounter`** (greater-only, no boss). **`bossBonusScoreTotal`** accumulates **`bossScoreBonusOnKill`** per boss. **`enemyKillCounter`** still increments for every enemy death including bosses.
- **XP**: `EnemyCharacter` applies **`bossExpMultiplier`** to `expOnKill`; death handlers add **`bossBonusXpFlat`** when `isBoss`.

### Changing default bindings (keyboard / mouse)

1. Edit **`Assets/Resources/Input/GeoWorldInputActions.txt`**. It is standard **Input Actions** JSON (same format as a `.inputactions` file). Adjust paths under the **Gameplay** map’s **`bindings`** (e.g. **FirePrimary** → `<Mouse>/leftButton`, skills **Q/E/R/F**, **PauseOrQuit** → `<Keyboard>/escape`, **DebugLevelUp** → **T**, **PostRoundReplay** → `<Keyboard>/enter`, **PostRoundTitle** → `<Keyboard>/b`).
2. Save the file. **`GameInput`** loads **`Resources`** path **`Input/GeoWorldInputActions`** at runtime; no code changes for rebinding.
3. Optional: copy the JSON to a **`.inputactions`** file elsewhere in the project if you want Unity’s **Input Actions** visual editor, then paste changes back into **`GeoWorldInputActions.txt`** when done.
4. **Player settings**: Set **Edit → Project Settings → Player → Other Settings → Active Input Handling** to **Input System Package (New)** (or **Both** only if you still need the old manager elsewhere). Standard Assets paths use **`GeoWorldInputCompat`** (`Assets/Plugins/GeoWorldInputCompat.cs`) so keyboard/mouse reads work without the legacy **Input Manager** backend.
5. **WebGL**: **`GameInput`** uses the same **Input Actions** JSON as other platforms. If you hit input or audio quirks in the browser, test with **Both** temporarily and check the Unity issue tracker for your Editor version.

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
   Many systems still resolve the player by tag **`Player1`**; gameplay/UI paths that were hot refactored cache **`PlayerCharacter`** instead of calling **`GetComponent`** every frame. **`GameSession`** exposes **`IsRunActive`** and the active **`PlayerCharacter`** so audio/skills/HUD do not query **`GameOver`** for death/timer flags.

3. **UI stack (gameplay)**  
   **HUD, minimap, mid-round pause overlay, and end-of-run scoreboard** use **`UnityEngine.UI` (uGUI)** — `GameplayHudView`, `UserInterface`, `MinimapRadar`, `GameplayPause`, and `GameOver`’s **`Text`** references. **Immediate-mode `OnGUI`** is **not** used on those paths. (Some **Standard Assets** packages still contain **Editor-only** `OnGUI` in custom **PropertyDrawer**s; that is unrelated to in-game HUD rendering.)

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

## WebGL: ship-ready behavior and hosting

This section is the checklist for a **browser** build: loading, audio policy, fullscreen, hosting headers, and differences from desktop.

### Behavior implemented in this project

| Topic | What we do |
|--------|----------------|
| **Tab focus / visibility** | `WebGlShipReadyRuntime` (WebGL **player** builds only) sets `AudioListener.pause` from `OnApplicationPause` / `OnApplicationFocus` when the tab loses or regains focus. While **`GameSession.IsRunActive`** is true, it also sets **`Time.timeScale` to 0** until the tab is focused again, then restores the previous time scale (so the round timer and simulation do not run in the background). It does **not** change time scale when no run is active (title / game-over), so **`GameOver`** can keep `Time.timeScale` at 0 at end of round without this fighting it. |
| **Background music & autoplay** | Browsers block **autoplay with sound** until a **user gesture**. If the player came from **`Start.unity`**, `GameStart` calls `GeoWorldSessionStart.NotifyGameplayStartingFromTitleScreen()` before loading **`GeoWorldMain`**, and `BackgroundMusic` tries `Play()` on the first gameplay frame, then falls back to “wait for key/mouse” if the clip still is not playing. If the first scene is **`GeoWorldMain`** (no title), `BackgroundMusic` still waits for the first **key** or **mouse button** in `Update`. |
| **Quit / post-round** | After the round, **Enter** reloads gameplay and **B** returns to the title scene (see **`GameOver`**); **Escape** still exits play mode in the Editor, calls **`Application.Quit()`** on standalone, and on WebGL the UI explains closing the tab (no **`Application.Quit`**). |
| **Input** | WebGL **player** builds use **legacy `Input` only** in `GameInput` to avoid WASM/JS re-entrancy with the new Input System (see **Tuning & configuration**). |

### Fullscreen API (Unity template)

Fullscreen is provided by the **Unity WebGL player template** (fullscreen control in the page chrome), which uses the browser **Fullscreen API** where supported.

**Limitations (expect these in the wild):**

- **iOS Safari** and some mobile browsers **do not** support true keyboard/mouse-lock fullscreen the same way as desktop Chrome/Firefox; the control may be missing or may only expand the canvas inside the page.
- The user can **exit** fullscreen with **Esc** (desktop); do not assume the game always stays fullscreen.
- **Embedded iframes** (e.g. some storefront embeds) may block fullscreen unless the host sets **`allowfullscreen`** (and related permissions). Test on the real embed URL.
- Programmatic **`Screen.SetResolution` / `Screen.fullScreen`** from C# may be ignored or gated; prefer the template’s user-initiated fullscreen where possible.

### Loading UX (progress, first frame)

- Unity’s default template shows **download / decompress progress** while the `.data` / `.wasm` payload loads.
- The **first rendered frame** may arrive a few seconds after the progress bar completes; hosts with aggressive caching still need correct **`Content-Type`** and **`Content-Encoding`** for `.wasm` / `.js` / `.data` (see `netlify.toml` in this repo).
- If you enable **Brotli** or **Gzip** compression in **Player Settings → Publishing Settings**, ensure the host serves **precompressed** files **with** matching `Content-Encoding`, or enable **Decompression Fallback** so the loader can decompress in the client when headers are wrong.

### Audio: compression and import settings (Editor)

- Prefer **Vorbis** (or platform-default compressed) for music and most SFX; tune **quality** vs. download size. Very short UI blips can stay **PCM** or **ADPCM** if you want zero decode latency.
- Long loops: consider **Load Type = Streaming** and **Load In Background** where appropriate to reduce peak memory (see Unity’s AudioClip import docs).
- **Mute when tab in background** is handled via **`AudioListener.pause`** (see table above), not by stopping every `AudioSource` individually.

### Desktop vs WebGL assumptions (acceptance checklist)

| Avoid on WebGL | Status in this repo |
|----------------|---------------------|
| `Application.Quit()` as the only way to “exit” | Handled in `GameOver` (WebGL shows copy to close the tab). |
| **Blocking** the main thread (`Thread.Sleep`, synchronous network/file waits in `Update`) | Custom `_SCRIPTS` do not use `Thread` / `Task` patterns; keep new code off blocking I/O on the main thread in WebGL. |
| **Background threads** doing Unity API calls | Not used; **Player Settings → WebGL → Threads** should stay **off** unless you deliberately adopt multithreaded WebGL and matching hosting (COOP/COEP). |

### Hosting checklist

**Compression & MIME**

- Serve **`.wasm`** as `application/wasm`.
- If files are **`.br` / `.gz`**, set **`Content-Encoding: br`** or **`gzip`** respectively (see `netlify.toml`).
- Mismatch between actual bytes and headers is a common **“stuck loading”** failure mode; **Decompression Fallback** in the player is the safety net.

**COOP / COEP (cross-origin isolation)**

- Required only for **multithreaded WebGL** (and some advanced browser APIs). **This project does not enable COOP/COEP by default**; see the commented block in `netlify.toml`. **Do not** turn on isolation headers unless **Player Settings** use threads and Unity’s hosting notes say you need them—otherwise third-party scripts or assets can break.

**itch.io (HTML / WebGL uploads)**

- Upload a **zip** of the **contents** of your build folder so **`index.html` is at the root** of the zip (same idea as Netlify publish root).
- Respect **file size** and **iframe** limits documented on itch; large `.data` builds may need stronger compression or a split strategy per Unity/itch guidance.
- Ensure the itch page allows **fullscreen** if you rely on it.

**Tested host (CI)**

- **Netlify** static deploy from GitHub Actions (see below): publish directory **`Build/WebGL/GeoWorld/`** with the headers in `netlify.toml`.

### WebGL build & upload (quick path)

1. **Unity Editor:** *File → Build Profiles* (or *Build Settings*), switch to **WebGL**, then **Build** into a clean folder (e.g. `Builds/WebGL/GeoWorld/`). Do **not** use the repository root as the output path.
2. **Publishing:** enable **compression** (Brotli/Gzip) consistent with your host; enable **Decompression Fallback** if you are unsure about CDN headers.
3. **Upload:** upload the **folder that contains `index.html`** at its root (for Netlify CLI: `--dir=Build/WebGL/GeoWorld`; for itch: zip those files).
4. **Smoke-test:** first load, **click or press a key** to confirm music (autoplay policy), then **tab away** and back to confirm audio mutes/resumes and that an **in-progress round** does not advance while the tab is hidden (WebGL player build).

## WebGL CI and Netlify

Player-facing WebGL behavior (tab mute, autoplay, fullscreen limits, hosting) is documented in **WebGL: ship-ready behavior and hosting** above.

GitHub Actions builds **WebGL** with [game-ci `unity-builder`](https://game.ci/docs/github/builder) and deploys the static output to [Netlify](https://www.netlify.com/) on:

- pushes to **`master`** or **`main`**
- tags matching **`v*`** (e.g. `v1.0.0` or `v1.0.0-rc.1`), including release tags you push from git—avoid duplicating a separate “on release” trigger so the workflow does not run twice for the same tag
- manual runs (**Actions → WebGL — Netlify & GitHub releases → Run workflow**)

**Pull requests** do not deploy to Netlify. They run the separate **Unity CI** workflow (`.github/workflows/unity-ci.yml`), which performs a **StandaloneLinux64** smoke build with the same GameCI license secrets—faster feedback than a full WebGL compile. The project does not ship `com.unity.test-framework` yet; when EditMode/PlayMode tests are added, extend that workflow with [game-ci `unity-test-runner`](https://game.ci/docs/github/test-runner).

**GitHub Releases:** pushing a **`v*`** tag whose commit is **on the repository default branch** (`master` / `main` per GitHub settings) runs the WebGL job, then creates (or updates) a **GitHub Release** for that tag with **`GeoWorld-WebGL-<tag>.zip`** attached. Tags whose commit is not on the default branch fail the branch guard step so Netlify and Releases are not updated from stray tags. Pre-releases: tags whose name contains **`-`** (for example **`v1.0.0-rc.1`**) are marked **pre-release** on GitHub (heuristic; adjust the workflow if you use a different convention).

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

For pull requests, scope, and third-party asset notes, see **[`CONTRIBUTING.md`](CONTRIBUTING.md)**. For reporting security issues in this repo’s code or automation, see **[`SECURITY.md`](SECURITY.md)**.

1. Open **`GeoWorldMain`** from `Assets/_SCENES` (or run from **`Start`** and confirm the title screen with any key or mouse click).  
2. Optional: create a **`Game Balance`** asset (**Assets → Create → GeoWorld → Game Balance**) and assign it on the **`GameOver`** component’s **`gameBalance`** field (see **Game Balance asset** under **Tuning & configuration**).  
3. Prefer fixing gameplay in **`Assets/_SCRIPTS`**; treat **`Standard Assets`** as legacy third-party code unless you plan a full replacement.  
4. After big Unity upgrades, expect more obsolete API warnings in **Standard Assets**; the custom game scripts are the source of truth for design intent.

## License

Your original project code in this repository is licensed under the **MIT License** — see [`LICENSE`](LICENSE).

**Third-party content** (for example **Unity Standard Assets** and Asset Store / pack assets under `Assets/`) may be governed by **other** licenses from Unity Technologies or those publishers. The MIT license applies to what you own and contribute here, not necessarily to every file in `Assets/`.

---