# Blackjack Casino

A free, offline-first single-player Blackjack game for Windows. No real-money gambling, ads or accounts.

The project is currently in **Phase 3: Full Table**. The playable table can be used as a fast solo game or with 1–5 basic-strategy AI players sharing the same shoe.

## Goals

- authentic configurable Blackjack rules;
- optional solo or multi-seat AI table;
- one shared shoe for the player, AI seats and dealer;
- polished offline Windows experience;
- local-only player saves and statistics;
- optional strategy/card-counting tools rather than forced coaching;
- reproducible CI builds and safe in-app updates.

## Current gameplay

- play-money bankroll and chip betting;
- Hit, Stand, Double, Split, Late Surrender and Insurance;
- 6-deck S17 table with 3:2 Blackjack;
- persistent shoe and cut-card reshuffle;
- Solo mode or Full Table with 1–5 AI seats;
- AI uses deterministic basic strategy and can split/double/surrender;
- all occupied seats draw from the exact same shoe;
- local bankroll persistence;
- Stable and Development update channels.

## Projects

| Project | Purpose |
| --- | --- |
| `BlackjackCasino.Core` | Cards, shoe, hands, table AI, rule engine and settlement |
| `BlackjackCasino.Update` | Manifest protocol, semantic versions, download and SHA-256 validation |
| `BlackjackCasino.Updater` | Out-of-process staged installation replacement |
| `BlackjackCasino.Desktop` | Windows casino table UI |
| `BlackjackCasino.Tests` | Core, full-table and updater tests |

## Development

Requires the .NET 10 SDK.

```powershell
dotnet restore BlackjackCasino.sln
dotnet build BlackjackCasino.sln -c Release
dotnet test BlackjackCasino.sln -c Release --no-build
```

See [`docs/phase-3.md`](docs/phase-3.md) for the current table model and [`docs/architecture.md`](docs/architecture.md) for the project architecture.
