# The Midnight Club — Blackjack Casino

**Offline-first, single-player, play-money casino for Windows.** No real-money wagers, payments, advertising, account requirement or online service are needed to play. The repository's executable client is currently **WPF/.NET 10**. The planned React/TypeScript/PixiJS modernization is **not yet implemented**.

> **Consolidation — 2026-11-04.** The cumulative casino-quality, Jigsaw and workflow improvements are being consolidated into `main`. After merge, `main` also supplies the **Development** update channel. Older feature branches are historical and can be deleted by the owner once their unique changes are verified as integrated. Midnight Manor is the first *planned* rendering migration; no React/PixiJS frontend is shipped yet. See [Development delivery](docs/development-update-delivery.md) and [agent workflow](AGENTS.md).

## Play modes at a glance

| Area | Available experience | Key systems |
| --- | --- | --- |
| **Blackjack** | Solo or full AI table | Hit, Stand, Double, Split, Surrender, Insurance, configurable rules, persistent shoe and table themes |
| **Midnight Dice** | Solo and AI-table Kniffel | Five dice, scorecard, persistent rounds, achievements and progression |
| **Midnight Reels** | Classic three-reel slot | Animated reels, shared play-money bankroll |
| **Midnight Manor** | Gothic 5×3 video slot | 20 paylines, Haunted Night free spins, Escape the Manor, resumable features |
| **Neon Vault** | 5×4 high-roller video slot | 1,024 Ways, Wilds, BLACKOUT events, jackpots and large virtual stakes |
| **Nine Lives** | Cat-themed 3×3 video slot | Five lines, Pounce Wilds, nine-spin bonus and Full Grid Frenzy |
| **Casino Empire** | Offline-capable management/idle game | Multiple properties, departments, research, Prestige, automation and owner dividends |
| **Chill Lounge** | No-wager single-player games | Sudoku, Midnight Solitaire, Word Search, Memory and Jigsaw Puzzle |
| **Midnight Merge** | Collection/merge game | Persistent orders, combinations and progression |
| **Tales from the Backroom** | Story-driven tabletop adventures | Characters, inventory, branching scenes and persistent cases; Dead Man's Hand is a six-act noir investigation |

The main lobby connects these experiences into one Windows application. Games with wagers share a play-money balance; progression includes player level, Prestige, Seasons, cosmetic Coins, challenges and game-specific statistics. The Collector's Album, Casino Vault, profile customization and cosmetic shop provide additional long-term systems. Casino Empire also maintains its own construction-credit economy.

### Recently expanded areas

- **Jigsaw Puzzle:** 12 collection-art motifs, puzzle sizes from 24 to 1,000 pieces, resumable saves, timer toggle, completion screen and 30 achievements tied into shared challenges. **Recent UI/performance improvements have entered the cumulative development line; game-tester acceptance is still required.** See [Jigsaw guide](docs/jigsaw.md).
- **Tales from the Backroom:** *Dead Man's Hand* has six investigation acts, multiple branching endings, evidence items and a case board. See [campaign documentation](docs/dead-mans-hand.md).
- **Cosmetics and collection:** new card/dice sets, portrait art and visual refinements; the collectible album tracks permanent collections and duplicate recycling. See [shop expansion](docs/shop-cosmetics-expansion.md) and [album](docs/collectible-album.md).
- **Localization:** English and German resources are supported, but the migration of all legacy strings and adventure text is **still incomplete**. A green shell localization check is not proof of full program coverage. See [localization plan](docs/localization-overhaul.md).
- **Desktop quality:** ongoing sharpening, layout, runtime regression and release-note work is developed on the cumulative casino-quality branches. Some visual polish remains in progress.

## Installation and updates

Windows builds are published through [GitHub Releases](https://github.com/TeKatz/blackjack-casino/releases). Use a released Windows package, not the source ZIP, to run the game. The **Stable** channel follows the latest normal release; the opt-in **Development** channel follows the mutable `development` prerelease. The Development manifest is at [development/update-manifest.json](https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json).

The existing updater uses versioned manifests, package-size and SHA-256 verification, and staged replacement with a rollback path. Saves are outside the installation directory, under `%LOCALAPPDATA%\BlackjackCasino`, and should remain intact when updating. Preserve backups when testing migration scenarios.

**Do not assume** a GitHub PR preview artifact is already published to the in-game Development updater. The publisher's source branch is deliberately pinned to `main` in `.github/workflows/development-release.yml` after consolidation; see [release delivery](docs/development-update-delivery.md).

## Repository layout

| Path | Responsibility |
| --- | --- |
| `src/BlackjackCasino.Core` | UI-independent game rules, persistent game state, math, progression and statistics |
| `src/BlackjackCasino.Desktop` | Existing WPF shell, game presentation, navigation, settings, local persistence and integration |
| `src/BlackjackCasino.Update` | Manifest and package validation, semantic versions |
| `src/BlackjackCasino.Updater` | External Windows staged replacement and restart helper |
| `tests/BlackjackCasino.Tests` | Gameplay, state, progression, wallet-related, save and updater unit/regression tests |
| `tools/` | Windows visual and runtime smokes, reports, localization checks and repository checks |
| `docs/` | Current feature specs, historical work, release details and modernization direction |
| `.github/workflows/` | Windows CI, presentation checks, release publishing and repository-contract checks |

Full details: [architecture](docs/architecture.md).

## Build and validation

Install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) on Windows. [global.json](global.json) defines the SDK roll-forward policy. The existing desktop client and visual smoke tools require Windows.

```powershell
python tools/check_repo_contract.py
python tools/check_localization.py
python tools/check_localization_coverage.py --strict-shell
python tools/check_adventure_localization.py
dotnet restore BlackjackCasino.sln
dotnet build BlackjackCasino.sln -c Release
dotnet test BlackjackCasino.sln -c Release --no-build
```

The GitHub workflows also run screenshot/runtime checks including presentation, startup and Manor smoke tools. Always report **which checks actually ran**; passing a documentation check alone does not establish a playable Windows build.

## Team workflow and planned modernization

The **game director** owns gameplay vision and hands-on bug testing. Coding agents/ChatGPT Work own implementation, maintenance, tests and delivery; the game director should not need to write code or manage builds. Contributions use small scoped branches, automated checks, game tester feedback and release gates. Read [AGENTS.md](AGENTS.md), [CONTRIBUTING.md](CONTRIBUTING.md) and [pull request checklist](.github/pull_request_template.md).

**Next rendering pilot: Midnight Manor.** Following consolidation and Jigsaw acceptance, we plan to build a React/TypeScript + PixiJS presentation hosted initially through WebView2, while keeping the C# core authoritative for game results and save data. This is a staged replacement of WPF, **not** a one-shot rewrite and **not** an already-shipped feature. See [modernization roadmap](docs/modernization-roadmap.md).

For package/runtime maintenance status and deliberate upgrade gates, see [maintenance audit](docs/maintenance-audit.md).

## Feature documentation

- [Blackjack/table and player experience](docs/table-profile-progression.md)
- [Midnight Dice](docs/kniffel.md)
- [Midnight Manor rules and recovery](docs/midnight-manor-design.md)
- [Neon Vault](docs/neon-vault.md)
- [Casino Empire](docs/casino-empire.md)
- [Chill Lounge infrastructure](docs/chill-game-infrastructure.md)
- [Sudoku](docs/sudoku.md)
- [Jigsaw](docs/jigsaw.md)
- [Tales from the Backroom](docs/adventure-engine.md)
- [Development updating](docs/development-update-delivery.md)
