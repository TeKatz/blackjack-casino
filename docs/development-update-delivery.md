# Development updates and installation compatibility

## Current release sources

| Channel | Source | Publication |
| --- | --- | --- |
| **Development** | `dev` | Tested Windows workflow publishes the mutable `development` GitHub release |
| **Stable** | Reviewed `main` and explicit `vX.Y.Z` version tag | Stable release workflow; **not** triggered merely by merging `dev` |
| **Feature branches** | `feature/*`, `fix/*`, other PR branches | CI previews only; **not** in-app updates |

**Availability (November 2026):** The public Releases list currently offers the `development` pre-release, not a newly tagged native Stable build. The `main` source now contains the native host cutover; an explicit tagged build and release validation are still required before advertising a new native Stable version.

The Development workflow in `.github/workflows/development-release.yml` has matching **`dev` push filter and job-level guard**. A failed CI/job/package verification is not a published Development update. The published in-app manifest remains:

`https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json`

The full historical migration between `feature/*`, `develop` and `dev`, along with earlier updater repairs, is preserved in the [release history archive](archive/development-release-history.md).

## What is published

- A self-contained Windows `blackjack-casino-win-x64.zip` application archive.
- `BlackjackCasino.NativeShell.exe`, locally bundled React/PixiJS pages, the required Godot programs/assets and the external updater.
- A **WPF-free** `BlackjackCasino.Desktop.exe` compatibility alias for older shortcuts and pre-cutover updater contracts.
- Versioned release notes, the update manifest and package integrity metadata (including exact size and SHA-256).

`BlackjackCasino.MigrationDiagnostics.exe` is **not** published. It is a developer-only offline rehearsal program.

## Player-facing News and technical release notes

The NativeShell displays **News once per new process start**, after the genuine React lobby receives the first native snapshot. Returning from a game to the lobby does not reopen the dialog; the sidebar **News** entry can always reopen it manually. This is a session-only flag and is not written to the player's profile.

Release publishing continues to generate the technical changelog from Git commits for contributors. Separately, `tools/generate_release_notes.py` uses the bilingual, editorial `tools/player_news_catalog.json` to generate a maximum of six short, nontechnical `playerNews` entries in the same versioned `release-notes.json` file. The native app loads the installed local copy from its application directory; it **does not query GitHub on launch** or display raw commit subjects. Only curated copy is accepted. If an older or damaged package has no editorial entries, the lobby falls back to a small, offline list of real casino highlights rather than a blank News dialog. The dialog indicates whether it displays version-specific notes or general highlights.

Maintainers update the editorial text and recognized topic keywords alongside meaningful user-visible changes. A recognized topic only appears when the compared release commit messages mention it; unknown technical changes are never translated verbatim into player-facing text. Both German and English entries are reviewed together. The existing version pinning, package checksum, changelog `changes` array and updater behavior remain unchanged.

## In-app updates and safety

The update service reads the selected Stable or Development manifest and checks semantic-version ordering, channel compatibility and minimum updater version. Downloads are staged outside the running application directory, validated before activation, and installed by the separate updater process. Transient file locks are retried; an activation failure attempts to restore the previous installation, and diagnostics are written to the local updater log.

The existing `NativeShellPreview` player-data root and `Candidates/development` profile are **not** installation files and must never be replaced by a ZIP update. The normal native startup selects existing progress before attempting a one-time copy-only import of older saves. Modern updates restart NativeShell directly; the WPF-free old-name alias supports legacy upgrades.

A GitHub PR build or CI artifact is **not** the same as a successful published Development release. For checking current delivery, inspect the workflow run and the assets on the [Development release](https://github.com/TeKatz/blackjack-casino/releases/tag/development).

## Contributor verification

1. Check the exact `dev` HEAD and all required Windows/frontend/contract checks.
2. Verify Godot and native package assets, both executable names, version metadata and checksum.
3. Verify the manifest and downloadable release assets; separately test updates from older installed versions.
4. Do not change the Development source branch or Stable tag policy without an explicit reviewed release-routing change.

See [branch policy](branching-and-release-policy.md), [architecture](architecture.md), [Contributing](../CONTRIBUTING.md), and [current roadmap](modernization-roadmap.md).
