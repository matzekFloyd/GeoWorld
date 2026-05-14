# Security policy

## Supported versions

Security-sensitive fixes are applied to the **default development branch** (usually `main` or `master`) for the **Unity editor version** pinned in **`ProjectSettings/ProjectVersion.txt`**. Older snapshots or forks may not receive backports unless someone volunteers to maintain them.

## Reporting a vulnerability

If you believe you have found a **security vulnerability** in **this repository’s code** (for example in build scripts, GitHub Actions workflows, or tooling under the repo root):

1. **Do not** open a public issue with exploit details.
2. Use **GitHub private vulnerability reporting** for this repository (**Security** tab → **Report a vulnerability**) if it is enabled for the project.
3. If private reporting is not available, contact the **repository owners** through a private channel they publish (for example maintainer email in their GitHub profile or organization security contact).

Please include:

- A short description of the issue and its impact
- Steps to reproduce (or a proof-of-concept) where safe
- Affected paths (e.g. `.github/workflows/…`, `Assets/Editor/…`)

We will try to acknowledge receipt within a reasonable time and coordinate a fix and disclosure timeline.

## Out of scope

The following are generally **out of scope** for this project’s security policy unless they directly involve **first-party code or configuration in this repo**:

- Vulnerabilities in **Unity Editor**, **Unity Player**, or **third-party packages** (report those to Unity or the package vendor).
- Compromise of **your** machine, Unity account, or GitHub account (use 2FA, rotate credentials, and follow GitHub’s guidance).
- **Game cheating** or client-side manipulation in a shipped build (design and anti-cheat are product decisions, not coordinated disclosure targets for this README).

## Safe harbour

If you make a good-faith effort to follow this policy and avoid harm to users or systems, we will not pursue legal action against you for accidental, policy-compliant research.
