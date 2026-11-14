# Contributing and development workflow

The Midnight Club is an offline-first, play-money Windows casino. Gameplay and visual direction come from the game director; engineers and coding agents handle technical implementation, verification and release delivery.

## Before starting

1. Inspect recent branches and open pull requests. The Development publisher is pinned to `dev`; `main` is the tested integration baseline. Verify both workflow guards before changing release routing.
2. Start from the most recent validated `dev` baseline. Never overwrite another active workstream.
3. Create a focused `feature/*` or `fix/*` branch for independent changes. Create focused feature/fix PRs from `dev` and merge them back into `dev` after checks. Keep PRs reviewable and avoid changing release pins as a side effect.
4. Read the [documentation index](docs/index.md), [AGENTS.md](AGENTS.md), [branch policy](docs/branching-and-release-policy.md), [architecture](docs/architecture.md), [Development delivery](docs/development-update-delivery.md), and the relevant game-specific documentation.

## Local validation (Windows)

Requires .NET 10 SDK matching [global.json](global.json). The current casino Windows host is WPF-free WinForms/WebView2.

```powershell
python tools/check_repo_contract.py
python tools/check_native_cutover.py
python tools/check_localization.py
python tools/check_localization_coverage.py --strict-shell
python tools/check_adventure_localization.py
cd frontend
npm ci --no-audit --no-fund
npm test
npm run build
cd ..
dotnet restore BlackjackCasino.sln
dotnet build BlackjackCasino.sln -c Release
dotnet test BlackjackCasino.sln -c Release --no-build
dotnet run --project src/BlackjackCasino.NativeShell -- --smoke
dotnet run --project src/BlackjackCasino.NativeShell -- --web-smoke
dotnet run --project tools/NativeMigrationDiagnostics -- --rehearsal-smoke
```

For affected visual surfaces, run the NativeShell real-Windows WebView2, receipt and game-specific regression tests. Automated tests and local review are complementary: test window sizes, DPI, animation-disabled mode, sound transitions, navigation, save/reopen, and German/English content where relevant.

## Definition of done

- Requirements and user-facing behavior are described, and the change remains compatible with existing saves.
- Gameplay logic and payouts have deterministic regression tests; completion rewards and financial transactions are idempotent.
- UI work has layout and animation-state coverage. Performance assertions have measured before/after evidence.
- Localization and documentation are updated. No outdated or falsely completed features are claimed.
- CI passes, or blockers are explicitly reported and fixed before a Development release is repointed.
- The final report distinguishes implemented functionality, tests actually run, and remaining work.

## Documentation

Write release notes, README sections and feature guides as project documentation rather than a transcript. Describe functionality, constraints, verified outcomes and remaining work without attributing decisions to conversations or specific participants. The repository contract checks this convention across project Markdown files.

## Pull requests and releases

Use `dev` for ongoing work, with reviewable feature/fix PRs. Integrate to `main` at validated milestones rather than merely to publish Development updates. Use the PR template and link supporting screenshots/benchmarks when relevant. A preview is not automatically a Development release. Development switches require an explicit source-branch update in **both** workflow guards, a valid increasing SemVer and successful package/updater verification. Do not merge, delete branches, repoint Development or release during another ongoing game change without coordinating it.

## Modernization

**Midnight Manor** was the first modernized game and remains the only player-facing Manor selection. React/PixiJS renders the game via WebView2 while the C# engine, persistent journal and player wallet remain authoritative. The legacy WPF Manor source has been removed; native receipts and rollback-compatible saves remain. The retired host's history is preserved in the [migration archive](docs/archive/native-shell-migration-phases.md). The [current roadmap](docs/modernization-roadmap.md) focuses on quality verification and further maintenance.
