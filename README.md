# Blackjack Casino

A free, offline-first single-player Blackjack game for Windows. No real-money gambling, ads or accounts.

The project is now in **Phase 2: playable table**. The Windows client has a functional casino table backed by the rule engine created in Phase 1.

## Current playable features

- six-deck shared shoe with cut-card reshuffle behavior;
- persistent local play-money bankroll;
- chip betting;
- Hit, Stand, Double, Split, Late Surrender and Insurance;
- dealer hole-card conceal/reveal;
- split-hand rendering and active-hand highlighting;
- correct wager reservation for Double, Split and Insurance;
- blackjack 3:2, S17 and dealer peek;
- keyboard shortcuts;
- Stable and Development update channels.

## Projects

| Project | Purpose |
| --- | --- |
| `BlackjackCasino.Core` | Cards, shoe, round rules and long-lived casino session/bankroll |
| `BlackjackCasino.Update` | Manifest protocol, semantic versions, download and SHA-256 validation |
| `BlackjackCasino.Updater` | Out-of-process staged installation replacement |
| `BlackjackCasino.Desktop` | Windows casino-table client |
| `BlackjackCasino.Tests` | Core, session and updater tests |

## Development

Requires the .NET 10 SDK.

```powershell
dotnet restore BlackjackCasino.sln
dotnet build BlackjackCasino.sln -c Release
dotnet test BlackjackCasino.sln -c Release --no-build
```

CI publishes a self-contained `win-x64` preview artifact from working branches, so testers do not need the .NET runtime installed.

## Releases and updates

- **Stable** — immutable SemVer-tagged GitHub Releases such as `v0.2.0`.
- **Development** — mutable `development` prerelease built from the `develop` branch.

Both channels use the same schema-versioned manifest and verify package size plus SHA-256 before installation.

See [`docs/architecture.md`](docs/architecture.md), [`docs/phase-1.md`](docs/phase-1.md) and [`docs/phase-2.md`](docs/phase-2.md).
