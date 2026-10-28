# Blackjack Casino

A free, offline-first single-player Blackjack game for Windows. No real-money gambling, ads or accounts.

The project is currently in **Phase 1: foundation**. The repository already contains the rule engine, automated tests, Windows bootstrap application and the complete release/update pipeline. The actual casino table UI follows in Phase 2.

## Goals

- authentic configurable Blackjack rules;
- shared shoe for the player, dealer and later AI seats;
- polished offline Windows experience;
- local-only player saves and statistics;
- optional strategy/card-counting tools rather than forced coaching;
- reproducible CI builds and safe in-app updates from day one.

## Projects

| Project | Purpose |
| --- | --- |
| `BlackjackCasino.Core` | Cards, shoe, hand values, legal actions and settlement |
| `BlackjackCasino.Update` | Manifest protocol, semantic versions, download and SHA-256 validation |
| `BlackjackCasino.Updater` | Out-of-process staged installation replacement |
| `BlackjackCasino.Desktop` | Windows client/bootstrap shell |
| `BlackjackCasino.Tests` | Core and updater tests |

## Development

Requires the .NET 10 SDK.

```powershell
dotnet restore BlackjackCasino.sln
dotnet build BlackjackCasino.sln -c Release
dotnet test BlackjackCasino.sln -c Release --no-build
```

## Releases and updates

The updater has two channels from the start:

- **Stable** — immutable SemVer-tagged GitHub Releases such as `v0.1.0`.
- **Development** — a mutable `development` prerelease whose assets are replaced by successful builds from the `develop` branch.

Stable releases are produced from SemVer tags:

```text
v0.1.0
v0.2.0
v1.0.0
```

The stable release workflow builds and tests the solution, publishes a self-contained `win-x64` package, publishes the standalone updater, computes SHA-256, generates `update-manifest.json`, and creates the GitHub Release. The development workflow uses the same package contract and integrity checks.

The desktop client can switch between Stable and Development and checks the corresponding GitHub-hosted manifest.

An update is installed only after its exact byte size and SHA-256 digest match the selected channel manifest. The running application hands installation off to a temporary updater process, which stages the new files before replacing the current installation and restores the previous directory if the swap itself fails.

See [`docs/architecture.md`](docs/architecture.md) and [`docs/phase-1.md`](docs/phase-1.md).
