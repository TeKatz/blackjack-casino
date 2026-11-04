# Repository maintenance audit

**Checked 2026-11-04.** Scope: workflow tooling, pinned .NET test dependencies, available documentation, and migration readiness. This is a record of *verified current state*, not a claim that every dependency is already upgraded.

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

- Some older architecture/feature documents are historical and refer to the WPF implementation; they remain useful reference material, but the new frontend is **only planned**.
- Full German localization remains incomplete; `--strict-shell` checks only its explicitly covered surfaces.
- Jigsaw is in active development; defer branch cleanup, game engine migration and disruptive toolchain changes until that work completes.
- The intended first PixiJS/TypeScript proof of migration is Midnight Manor, **not** a current installed feature.

See [roadmap](modernization-roadmap.md), [agent workflow](../AGENTS.md), and [Development delivery](development-update-delivery.md).
