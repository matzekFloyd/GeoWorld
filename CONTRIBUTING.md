# Contributing to GeoWorld

Thanks for helping improve the project. This repository is a **Unity 6** game prototype; most day-to-day work happens in `Assets/_SCRIPTS/`.

## Before you start

- **Editor version:** Match **`ProjectSettings/ProjectVersion.txt`** (currently **6000.4.5f1** or a compatible **Unity 6.4** line). Opening the project in a much older or newer Unity may cause upgrade noise.
- **Scenes:** Primary gameplay is **`Assets/_SCENES/GeoWorldMain.unity`**; title flow is **`Assets/_SCENES/Start.unity`**. See **`README.md`** for play mode defaults and tuning (`GameBalance`, `GameInput`, WebGL notes).
- **Scope:** Prefer changes in **`Assets/_SCRIPTS`**. **`Standard Assets`** and third-party packs are legacy; avoid large drive-by refactors there unless you intend to replace or fork them.

## How to contribute

1. **Fork** the repository and create a **branch** from `main` / `master` (whichever is default).
2. **Keep PRs focused**—one logical change (fix, feature, or doc update) is easier to review than a mixed bag.
3. **Describe the change** in the PR: what problem it solves, how to verify (Editor steps, scene, platform), and any trade-offs.
4. **License:** By opening a PR, you agree your contributions are under the same terms as the repo (**MIT** for your original code; see **`README.md`** and **`LICENSE`** for third-party assets).

## Code and Unity habits

- Follow existing naming and patterns in the touched files.
- Prefer **null-safe** UI and gameplay paths where the codebase already does.
- If you change balance or spawn rules, update **`README.md`** (Tuning & configuration) when behaviour or defaults change.
- Do **not** commit secrets (Unity license files, API keys, personal tokens). CI secrets belong in the GitHub repository settings, not in git history.

## Assets and legal

- Not everything under **`Assets/`** is MIT-licensed. Respect licenses for **Standard Assets**, store packs, and other third-party content. When in doubt, ask before re-licensing or redistributing art/audio from those folders.

## Polish backlog (GitHub)

Older notes lived in **`Assets/TO-DO.txt`**; items are now **GitHub issues** so progress is visible to contributors and on project boards.

- **Index / mapping table:** [`Assets/TO-DO.txt`](Assets/TO-DO.txt) (short links to each issue).
- **Migration checklist (closed when done):** [#53](https://github.com/matzekFloyd/GeoWorld/issues/53) — use this to see how each former bullet maps to an issue or descoped work.
- **QoL umbrella:** [#43](https://github.com/matzekFloyd/GeoWorld/issues/43) (epic).
- **Good first issue (polish starters):** [#80](https://github.com/matzekFloyd/GeoWorld/issues/80) (Blood Ritual mana bar over-max flash), [#83](https://github.com/matzekFloyd/GeoWorld/issues/83) (blood stain fade), or filter [open `good first issue`](https://github.com/matzekFloyd/GeoWorld/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22).

When you finish a backlog item, **close the GitHub issue** and optionally trim or update `TO-DO.txt` if the table row is obsolete.

## Questions
