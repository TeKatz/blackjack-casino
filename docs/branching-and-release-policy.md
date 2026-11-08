# Branching, integration and releases

**Current policy (2026-11-08):** The completed cumulative modernization was consolidated via [integration PR #36](https://github.com/TeKatz/blackjack-casino/pull/36). The old `feature/manor-modernization-phase-1` ref remains a historical branch. New work is based on `develop`.

| Source | Responsibility |
| --- | --- |
| `main` | Tested, reviewed integration baseline; source for explicitly tagged Stable releases |
| `develop` | Permanent cumulative development branch; only source of the mutable in-app Development release |
| `feature/*`, `fix/*` | Focused changes based on `develop`, integrated back through tests/PRs, not direct in-app updates |
| `vX.Y.Z` tags | Opt-in Stable release workflow from reviewed integration code |

The Development workflow has a **single `develop` push filter and matching job-level guard**. It runs tests and Windows runtime smokes, assembles artifacts and verifies the published package. A build that fails does not create a valid Development update. The installed update-manifest path is unchanged:

`https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json`

The on-demand CC0 music importer is launched from default `main`, but it checks out/commits to `develop` and may explicitly dispatch Development Release on `develop`. A commit to `main` does not auto-publish Stable; an explicit version tag and release validation are required.

The 2026-11-04 main revert of the early read-only Manor preview was part of the historical timeline. The later reviewed consolidation supersedes the reverted preview with the complete modern implementation while retaining subsequent main-only changes, including the music importer and robust Windows case-board smoke synchronization.

## Integration checklist

1. Synchronize with the newest integration baseline, inspect divergent commits, resolve conflicts deliberately without silently discarding changes, and keep save/RNG/payout contracts stable.
2. Run repository documentation checks, localization parity, frontend tests/build, .NET tests, Windows startup and presentation regression, plus updater/checksum validation for releases.
3. Confirm **the exact integration SHA** has completed required checks before merging. Passing automation does not substitute for manual visual/audio/DPI/extended-session acceptance.
4. Merge through reviewable PRs. No unrelated force-push, tag, release-channel switch or deletion of historical branches.

For implementation details see [Development delivery](development-update-delivery.md), [architecture](architecture.md), and [contribution guide](../CONTRIBUTING.md).
