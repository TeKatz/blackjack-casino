# Branching, integration and releases

**Current policy (November 2026):** `dev` is the active long-lived Development branch. `main` is the reviewed integration baseline for explicitly tagged Stable releases. The historical `develop` branch was consolidated through [PR #59](https://github.com/TeKatz/blackjack-casino/pull/59); it is not the current Development publisher.

| Source | Responsibility |
| --- | --- |
| `main` | Tested, reviewed integration baseline; source for explicitly tagged Stable releases |
| `dev` | Active cumulative development branch; only source of the mutable in-app Development release |
| `feature/*`, `fix/*` | Focused changes based on `dev`, integrated back through tests/PRs, not direct in-app updates |
| `vX.Y.Z` tags | Opt-in Stable release workflow from reviewed integration code |

The Development workflow has a **single `dev` push filter and matching job-level guard**. It runs tests and Windows runtime smokes, assembles artifacts and verifies the published package. A build that fails does not create a valid Development update. The installed update-manifest path is unchanged:

`https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json`

The on-demand CC0 music importer is launched from default `main`, but it checks out/commits to `dev` and may explicitly dispatch Development Release on `dev`. A commit to `main` does not auto-publish Stable; an explicit version tag and release validation are required.

**History:** The earlier frontend integration (PR #36) and branch consolidation (PR #59) led to the current NativeShell-only architecture. Further post-cutover cleanup runs on `dev`; old WPF host code is not an alternative release path. For its timeline, see the [migration archive](archive/native-shell-migration-phases.md).

## Integration checklist

1. Synchronize with the newest integration baseline, inspect divergent commits, resolve conflicts deliberately without silently discarding changes, and keep save/RNG/payout contracts stable.
2. Run repository documentation and WPF-free architecture checks, localization parity, frontend tests/build, .NET tests, native WinForms/WebView2 game/navigation regression, plus updater/checksum validation for releases.
3. Confirm **the exact integration SHA** has completed required checks before merging. Passing automation does not substitute for manual visual/audio/DPI/extended-session acceptance.
4. Merge through reviewable PRs. No unrelated force-push, tag, release-channel switch or deletion of historical branches.

For implementation details see [Development delivery](development-update-delivery.md), [architecture](architecture.md), and [contribution guide](../CONTRIBUTING.md).
