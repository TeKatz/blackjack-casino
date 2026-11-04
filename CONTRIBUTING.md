# Contributing and development workflow

The Midnight Club is an offline-first, play-money Windows casino. Gameplay and visual direction come from the game director; engineers and coding agents handle technical implementation, verification and release delivery.

## Before starting

1. Inspect recent branches and open pull requests. For Manor modernization, the Development publisher is pinned to `feature/manor-modernization-phase-1`. This is independent of the protected main source and does not require merging.
2. Start from the most recent **validated cumulative base**, not the oldest feature branch. Never overwrite another active workstream.
3. Continue related Manor changes on the **existing `feature/manor-modernization-phase-1`** branch. Other unrelated projects may use separate focused branches. Never merge to main to deliver a Development update without explicit user approval.
4. Read [AGENTS.md](AGENTS.md), [architecture](docs/architecture.md), [Development delivery](docs/development-update-delivery.md), and the relevant game-specific documentation.

## Local validation (Windows)

Requires .NET 10 SDK matching [global.json](global.json). The current desktop application is WPF/Windows-only.

```powershell
python tools/check_repo_contract.py
python tools/check_localization.py
python tools/check_localization_coverage.py --strict-shell
python tools/check_adventure_localization.py
dotnet restore BlackjackCasino.sln
dotnet build BlackjackCasino.sln -c Release
dotnet test BlackjackCasino.sln -c Release --no-build
```

For affected visual surfaces, run the corresponding Windows presentation/startup/Manor or game-specific smoke tests from `tools/`. Automated tests and local review are complementary: test window sizes, DPI, animation-disabled mode, sound transitions, navigation, save/reopen, and German/English content where relevant.

## Definition of done

- Requirements and user-facing behavior are described, and the change remains compatible with existing saves.
- Gameplay logic and payouts have deterministic regression tests; completion rewards and financial transactions are idempotent.
- UI work has layout and animation-state coverage. Performance assertions have measured before/after evidence.
- Localization and documentation are updated. No outdated or falsely completed features are claimed.
- CI passes, or blockers are explicitly reported and fixed before a Development release is repointed.
- The final report distinguishes implemented functionality, tests actually run, and remaining work.

## Pull requests and releases

Use the PR template and link supporting screenshots/benchmarks when relevant. A preview is not automatically a Development release. Development switches require an explicit source-branch update in **both** workflow guards, a valid increasing SemVer and successful package/updater verification. Do not merge, delete branches, repoint Development or release during another ongoing game change without coordinating it.

## Modernization

The first planned new-frontend migration is **Midnight Manor**, after branch consolidation and Jigsaw tester acceptance. See [modernization roadmap](docs/modernization-roadmap.md). Existing C# game engines remain authoritative; PixiJS and React are a presentation layer, not a replacement for casino rules.
