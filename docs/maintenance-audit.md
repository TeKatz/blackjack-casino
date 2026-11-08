# Repository maintenance audit

**Latest branch/workflow review: 2026-11-08.** Package-version comparisons below are preserved from their 2026-11-04 audit and require renewed verification before dependency upgrades.

## Cumulative integration audit (2026-11-08)

- The cumulative Manor/Casino modernization is integrated through PR #36. `main` becomes the tested integration baseline, while `develop` is the permanent Development update source.
- The integration incorporates the main-only CC0 Casino Music Importer and the resilient detached-caseboard smoke synchronization fix. The historical earlier Manor preview revert is superseded by the fully tested current frontend.
- CI, frontend, presentation and repository-contract workflows cover `develop`; Development release changes only after its full Windows tests and package verification succeed. Stable still requires an explicit version tag.
- The manifest URL, package schema, end-user save location, C# RNG, wager math and reward contracts are unchanged by the branch consolidation.
- Branch-sensitive Markdown is being updated for the permanent `main`/`develop` model; dated feature-phase documents retain historical context. See [branch policy](branching-and-release-policy.md).

**Historical audit checked 2026-11-04.** Scope: workflow tooling, pinned .NET test dependencies, available documentation, and migration readiness. This is a record of *verified current state*, not a claim that every dependency is already upgraded.

## Completed in the preparation branch

- Updated README and the architectural/release documents to reflect current playable modes and the active Development source.
- Introduced `AGENTS.md`, `CONTRIBUTING.md`, a pull request template, and the Midnight Manor-first modernization roadmap.
- Added the repository-contract check to CI, Development release and a lightweight standalone workflow.
- Updated GitHub-hosted Actions references: `actions/checkout@v7`, `actions/setup-dotnet@v6`, `actions/upload-artifact@v7`. Check actual runner results before incorporating these changes into the Development publisher.
- Did not change game rules, save schemas, releases, Jigsaw code, `main`, the current Development source branch or the updater protocol.

## .NET test dependencies: separate compatibility work

`Directory.Packages.props` currently pins:

| Package | Current pin | Newer published version at audit | Decision |
| --- | --- | --- | --- |
| `Microsoft.NET.Test.Sdk` | 17.14.1 | 18.11.0 | Upgrade in an isolated compatibility PR and run Windows tests |
| `xunit` | 2.9.3 | No newer maintained xUnit v2 package; xUnit v3 is a different package/API line | Plan a separate test migration; do not replace blindly |
| `xunit.runner.visualstudio` | 3.1.5 | 4.0.1 | Upgrade only after test-framework compatibility verification |
| `coverlet.collector` | 6.0.4 | 10.1.0 | Upgrade separately and verify XPlat coverage results |

Verify published versions again immediately before an update:
- https://www.nuget.org/packages/Microsoft.NET.Test.Sdk
- https://www.nuget.org/packages/xunit
- https://www.nuget.org/packages/xunit.runner.visualstudio
- https://www.nuget.org/packages/coverlet.collector

The existing suite links some Desktop persistence and presentation helper files into the test assembly; test framework updates must not silently drop these regression checks. Never claim an upgrade complete until `dotnet restore`, `dotnet build`, `dotnet test`, Windows smoke and artifact publishing actually pass.

## Periodic upkeep process

1. Review current branches/PRs and whether a game workstream is active; wait before consolidation.
2. Check SDK `global.json`, framework support, central NuGet package pins and the GitHub Action major releases.
3. Upgrade one independent toolchain family at a time on a new branch, with before/after CI evidence and a fallback commit.
4. Run the repository contract, localization checks, .NET build/tests, Windows presentation smoke and a real installer/update test when the release path changes.
5. Update README and this audit with the new verified state. Do not merge or publish based on version numbers alone.

## Known non-blocking debt

- Some architecture/feature documents cover historical WPF-only implementations; modern Manor is already active in Development, while other modes remain on WPF.
- Full German localization remains incomplete; `--strict-shell` checks only its explicitly covered surfaces.
- Jigsaw has its own regression and quality gates. Coordinate broad dependency updates and branch cleanup with other active development workstreams.
- Midnight Manor is the first shipped React/PixiJS game in Development, with a single lobby entry and source-only WPF rollback.

See [roadmap](modernization-roadmap.md), [agent workflow](../AGENTS.md), and [Development delivery](development-update-delivery.md).

## Consolidation follow-up (2026-11-04)

The consolidated branch changes the Development publisher from `feature/casino-quality-phase-4` to `main` in **both** required workflow gates. The older note above describes the preparatory state before consolidation. NuGet/xUnit upgrades remain deferred and require a separate testable change. Verify actual main CI and the Development artifact before deleting old feature branches.

## Manor Development channel ownership (historical, 2026-11-04)

At that time, the active Manor development and testing branch was `feature/manor-modernization-phase-1`, without merging into `main`. Both Development release workflow guards target that branch. Earlier main-publishing notes are historical; the unintended merge was corrected without force-pushing. That arrangement has been superseded by `develop` and PR #36.
