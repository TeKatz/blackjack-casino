# The Midnight Club — Blackjack Casino

An **offline-first, single-player casino for Windows**, built around virtual credits and progression. No real-money gambling, payments, accounts, advertisements or online services are required for gameplay.

The desktop application uses **.NET 10 and WPF**. The casino lobby and Midnight Manor now have separate **React + TypeScript + PixiJS** presentations, hosted locally through **WebView2**. Existing game logic and persistent player data remain in C#.

## Games and experiences

| Mode | Gameplay |
| --- | --- |
| **Blackjack** | Solo and AI tables with Hit, Stand, Double, Split, Surrender, Insurance and configurable table rules |
| **Midnight Reels** | Traditional three-reel slot machine |
| **Midnight Manor** | Gothic five-reel slot with 20 paylines, Haunted Night free spins and the resumable Escape the Manor bonus |
| **Neon Vault** | High-stakes five-reel video slot, 1,024 ways and Vault Break free spins |
| **Nine Lives** | Cat-themed slot with Pounce Wilds and bonus spins |
| **Midnight Dice** | Kniffel-style dice game, solo or against AI |
| **Casino Empire** | Casino management with properties, research, upgrades and offline progression |
| **Chill Lounge** | Sudoku, Midnight Solitaire, Word Search, Memory and Jigsaw Puzzle |
| **Midnight Merge** | Collection and combination gameplay with persistent orders |
| **Tales from the Backroom** | Branching adventures and investigations, including *Dead Man's Hand* |

A shared virtual bankroll connects the wagering games. The wider progression system includes experience, Prestige, Seasons, challenges, cosmetic Coins, the Collector's Album, a cosmetic shop and the Casino Vault. Casino Empire maintains a separate construction economy.

## Casino lobby: modern presentation

The Development lobby uses its own React/TypeScript entry point (`frontend/lobby.html`) beside the Midnight Manor game. It keeps **all nine casino activities**, a category navigation rail, the player's live header and a compact overview of Season Pass, album, cosmetics, challenges and statistics. Its design follows the emerald-and-gold Midnight Club reference.

- Offline-local **generated artwork atlases** in `frontend/public/lobby/`: the background, hero, eight standard game illustrations, Midnight Merge and decorative navigation/profile/overview icons. The browser never downloads art from a server.
- Responsive DOM layout with accessible keyboard-focus states, compact screen-height handling, card hover lift, image zoom, reflected highlights and gold button feedback.
- Transparent **PixiJS particle atmosphere**, limited to 30 FPS and paused while the document is hidden; it respects reduced-motion settings and the game's animation preference.
- **C#-owned** player balances, progression and mode availability. The isolated WebView2 lobby sends only allowed navigation intents; it cannot calculate rewards or change saved finances.
- The original **WPF lobby remains as a working fallback** if WebView2 is unavailable or the modern page does not load. Every existing game opens through the original registered C# action, including Casino Empire's detached-window option.

For native builds, run `npm run build` from `frontend/` before the .NET build. Vite emits both `index.html` (Manor) and `lobby.html`, plus their local assets into `frontend/dist/`; the desktop project packages that whole directory as `ModernFrontend`.

## Midnight Manor: modern presentation

**In Development builds, Midnight Manor has one lobby entry.** It opens the modern React/PixiJS version directly; the separate playtest button and selectable legacy WPF version have been retired.

- Five reels, scalable local symbol artwork, five staged reel stops and layered win feedback
- Fixed play-money bets of $50, $100, $250, $1,000, $2,500 and $5,000 (manual stakes $1–$5,000)
- 10/25/50/100 automatic paid-spin sessions with an immediate stop control; bonus games, free spins, insufficient balance and uncertain saves halt the queue
- The original Manor soundtrack, continuous reel movement sounds and an ornate startup cover
- Integrated Escape the Manor bonus with six room views, an interactive map, perspective entry and camera-like room transitions
- German and English game controls and rules
- Shared virtual wallet, progression, saved bonus state and crash recovery
- Original C# game logic for random outcomes, payouts, bet validation and player decisions
- Local rendering and audio; the browser interface cannot directly change a balance or award

The legacy WPF Manor source remains available for a controlled rollback build, but is **not a second player-selectable game**. Its removal from the menu does not change the save format, payout tables or game rules.

Technical details: [modernization roadmap](docs/modernization-roadmap.md), [Manor design](docs/midnight-manor-design.md), [acceptance and recovery tests](docs/midnight-manor-acceptance.md).

## Installation and updates

Download a packaged Windows build from [GitHub Releases](https://github.com/TeKatz/blackjack-casino/releases). A GitHub source-code archive is not an installable application.

| Channel | Purpose | Delivery |
| --- | --- | --- |
| **Stable** | Regular releases | Latest stable GitHub release |
| **Development** | Early access to validated changes | Mutable [development release](https://github.com/TeKatz/blackjack-casino/releases/tag/development), also available through the in-app updater |

The updater validates package size and SHA-256 before staged installation. Player data is stored outside the installation directory, in `%LOCALAPPDATA%\BlackjackCasino`. Keep a backup when validating changes to persistence or updating an older installation.

**Active development source:** `feature/manor-modernization-phase-1`. The Development publisher builds directly from this branch, without merging into `main`. Its source is pinned in both the push trigger and job guard of [development-release.yml](.github/workflows/development-release.yml). The usual [update manifest](https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json) remains unchanged. Branch builds and PR preview artifacts do not automatically become in-app updates.

See [Development update delivery](docs/development-update-delivery.md) for release mechanics, versioning and verification.

## Technology and repository layout

| Path | Responsibility |
| --- | --- |
| `src/BlackjackCasino.Core/` | Game rules, bankroll, rewards, progression and state models |
| `src/BlackjackCasino.Desktop/` | Windows shell, existing WPF games, WebView2 host, settings and persistence |
| `frontend/` | Modern lobby and Midnight Manor pages, local generated asset atlases and PixiJS effects |
| `src/BlackjackCasino.Update/` | Update manifest, version and package validation |
| `src/BlackjackCasino.Updater/` | Staged installation and restart helper |
| `tests/BlackjackCasino.Tests/` | Unit and integration tests |
| `tools/` | Build safeguards, localization checks and Windows smoke tests |
| `docs/` | Architecture, gameplay specifications and implementation history |
| `.github/workflows/` | CI, frontend, presentation and release workflows |

**Architecture principle:** C# remains authoritative for random results, payouts and saved progression. React/PixiJS handles presentation only. The game must remain fully playable offline.

Read the [architecture guide](docs/architecture.md) for service boundaries and the [modernization roadmap](docs/modernization-roadmap.md) for the phased migration of other game interfaces.

## Building and testing

Development requires Windows and the **.NET 10 SDK** compatible with [global.json](global.json). The modern frontend uses **Node.js 22**.

Build the frontend before the Windows client so its bundled files are included in the desktop output:

```powershell
cd frontend
npm install --no-package-lock --no-audit --no-fund
npm test
npm run build
cd ..

python tools/check_repo_contract.py
python tools/check_localization.py
python tools/check_localization_coverage.py --strict-shell
python tools/check_adventure_localization.py
dotnet restore BlackjackCasino.sln
dotnet build BlackjackCasino.sln --configuration Release
dotnet test BlackjackCasino.sln --configuration Release --no-build
```

Windows CI additionally runs actual desktop startup, Manor runtime and presentation regression tests. A successful frontend build or documentation check alone does not prove an installable Windows release. Production packages are created and verified by the release workflows.

## Current engineering boundaries

- **Midnight Manor:** modern presentation is the default in Development; remaining subjective audio, display-scaling and long-session performance checks are tracked separately.
- **Other game interfaces:** still use WPF until migrated individually; the lobby and Midnight Manor are already modern, with WPF lobby fallback retained.
- **Localization:** English/German shell support is present; some older game and adventure content still needs translation.
- **Player data and financial outcomes:** preserve backward compatibility, deterministic payout tests and exactly-once committed transitions.
- **Releases:** `main` and Stable do not inherit Development changes automatically.

The roadmap tracks completed migration stages and outstanding quality gates; historical milestone notes are not separate active feature variants.

## Development guides

- [Contributing](CONTRIBUTING.md) and [coding-agent agreement](AGENTS.md)
- [Modernization roadmap](docs/modernization-roadmap.md)
- [Development release and updater](docs/development-update-delivery.md)
- [Manor quality and release acceptance](docs/manor-phase-3-signoff.md)
- [Localization status](docs/localization-overhaul.md)
- [Maintenance audit](docs/maintenance-audit.md)
- [Jigsaw Puzzle](docs/jigsaw.md)
- [Cosmetic shop expansion](docs/shop-cosmetics-expansion.md)
- [Casino Empire](docs/casino-empire.md)
- [Tales from the Backroom](docs/adventure-engine.md)
