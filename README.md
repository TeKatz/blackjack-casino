# Blackjack Casino

A free, offline-first single-player Blackjack game for Windows. No real-money gambling, ads or accounts.

The project is currently in **Phase 4: Casino Presentation & Polish**. The blackjack and shared-table engines are playable; Phase 4 focuses on making the desktop experience feel like a real small casino game rather than a rules demo.

## Current gameplay

- play-money bankroll and chip betting;
- Hit, Stand, Double, Split, Late Surrender and Insurance;
- 6-deck S17 table with 3:2 Blackjack;
- persistent shoe and cut-card reshuffle;
- Solo mode or Full Table with 1–5 AI seats;
- deterministic basic-strategy AI sharing the exact same shoe;
- three table themes with layered felt texture and casino-table depth;
- dark themed controls with subtle hover/pressed feedback;
- optional staggered card animations and custom casino sound effects;
- optional procedurally generated lo-fi background music with persistent volume;
- F11 fullscreen mode;
- persistent bankroll, bet, play mode, AI seats, appearance, audio, window state and update channel;
- Stable and Development updater channels with SHA-256 package verification.

## Projects

| Project | Purpose |
| --- | --- |
| `BlackjackCasino.Core` | Cards, shoe, hands, table AI, rule engine and settlement |
| `BlackjackCasino.Update` | Manifest protocol, semantic versions, download and SHA-256 validation |
| `BlackjackCasino.Updater` | Out-of-process staged installation replacement |
| `BlackjackCasino.Desktop` | Windows casino table, presentation, audio and local preferences |
| `BlackjackCasino.Tests` | Core, full-table and updater tests |

## Development

Requires the .NET 10 SDK.

```powershell
dotnet restore BlackjackCasino.sln
dotnet build BlackjackCasino.sln -c Release
dotnet test BlackjackCasino.sln -c Release --no-build
```

See [`docs/phase-4.md`](docs/phase-4.md) for the current presentation work and [`docs/architecture.md`](docs/architecture.md) for the project architecture.
