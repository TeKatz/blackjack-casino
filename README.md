# The Midnight Club — Blackjack Casino

An **offline-first, single-player casino for Windows**, built around virtual credits and progression. No real-money gambling, payments, accounts, advertisements or online services are required for gameplay.

The Windows application runs on **.NET 10 with a WPF-free WinForms/WebView2 NativeShell**. The casino lobby and all game surfaces are local **React + TypeScript**, with PixiJS and embedded Godot where appropriate. C# remains authoritative for game outcomes, wagers, progression, receipts and durable saves. The former WPF application has been removed from the active source tree. A small **WPF-free** `BlackjackCasino.Desktop.exe` compatibility alias remains in published ZIPs solely for existing shortcuts and older updater restart contracts; the actual application is `BlackjackCasino.NativeShell.exe`.

The current migration repairs and device acceptance checks are documented in [Migration regression repairs](docs/migration-regression-repairs.md). Phase 5 consolidates seven React game routes behind a shared foreground-surface adapter and the ten React page hosts behind a common offline WebView2 security bootstrap; see [Desktop host modernization](docs/host-modernization-phase5.md).

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
| **Chill Lounge** | Sudoku, Midnight Solitaire, Word Search, Memory, Jigsaw Puzzle, Tales from the Backroom, Midnight Bastion (Tower Defense) and Midnight Sweep (Minesweeper) |
| **Midnight Merge** | Collection and combination gameplay with persistent orders |
| **Arcade Games** | Twenty offline skill minigames, spanning endless four-key rhythm, racing, stealth, mining, optical puzzles, circuits and timing; independent high scores and per-game challenges |
| **Tales from the Backroom** | Branching adventures and investigations, including *Dead Man's Hand* |
| **Midnight Descent** | Standalone Godot 2D action roguelite: first playable five-room expedition, sword/bolt/dash combat, three enemy archetypes, boss and relic choices |

A shared virtual bankroll connects the wagering games. The wider progression system includes experience, Prestige, Seasons, challenges, cosmetic Coins, the Collector's Album, a cosmetic shop and the Casino Vault. Casino Empire maintains a separate construction economy.

## Casino lobby: modern presentation

The Daily Login panel has a dark emerald palette, an illuminated border and an original post-commit claim chime. The Casino Vault uses a bounded brushed-metal safe facade. The Collector's Album keeps five-card results native-authoritative while presenting the stored pack receipt with a staged seal rupture and rarity-sensitive card reveals.


The casino lobby uses its own React/TypeScript entry point (`frontend/lobby.html`) beside the Midnight Manor game. It keeps **all nine casino activities**, a category navigation rail, the player's live header and a compact overview of Season Pass, album, cosmetics, challenges and statistics. Its design follows the emerald-and-gold Midnight Club reference.

- Offline-local **generated artwork atlases** in `frontend/public/lobby/`: the background, hero, background illustrations and decorative navigation icons; mode previews reuse their actual card, dice and symbol artwork. The browser never downloads art from a server.
- Responsive DOM layout with accessible keyboard-focus states, compact screen-height handling, card hover lift, image zoom, reflected highlights and gold button feedback.
- Transparent **PixiJS particle atmosphere**, limited to 30 FPS and paused while the document is hidden; it respects reduced-motion settings and the game's animation preference.
- **C#-owned** player balances, progression and mode availability. The isolated WebView2 lobby sends only allowed navigation intents; it cannot calculate rewards or change saved finances.
- If the React lobby or WebView2 is unavailable, a native WinForms emergency screen offers supported game routes and recovery without starting WPF. The normal lobby preserves its stable React/PixiJS tile objects across tab switches.

For native builds, run `npm run build` from `frontend/` before the .NET build. Vite emits `index.html` (Manor), `lobby.html`, `empire.html`, `blackjack.html` and `reels.html`, plus their local assets into `frontend/dist/`; the NativeShell project packages that whole directory as `ModernFrontend`.

## Blackjack: native table removed

The sole playable Blackjack interface is React/TypeScript (`frontend/blackjack.html`). `BlackjackTableController` holds the current Core session, wagers, rules, AI seats and presentation preferences without hidden WPF table controls. The old XAML table and fallback are gone; a failed browser returns to the casino lobby, preserving the current in-memory hand for a fresh React opening. See [Blackjack modernization](docs/blackjack-modernization.md).

## Midnight Merge: React/PixiJS restoration workshop

Midnight Merge runs as a responsive local React/PixiJS experience (`frontend/midnight-merge.html`): a persistent 7×6 board with drag/drop and keyboard-friendly click selection, six supply producers, 46 repeatable delivery orders and **ten casino rooms with 40 progressively revealed renovation stages**. The one-screen desktop layout reserves scrolling for delivery orders, while the premium stats, readable supply tiles, How to Play and Settings controls, ten-room progress strip and layered panoramas remain visible. The old workshop-status footer is removed, and spawn/merge no longer briefly dims unrelated board tiles while saves complete. Each finished room gains subtle ambient life, and the expanded view displays the entire uncropped room instead of zooming; German and English milestone texts, player XP and native audio remain integrated. `MergeBoard` remains the C# authority for spawning, moving, combining, delivering and saving. Every accepted action is saved atomically before the browser renders it; the versioned receipt protocol prevents duplicate rewards and stale commands across a WebView2 failure. The former `MidnightMergeView` WPF gameplay view has been retired, without changing the durable game schema or rewards. See [Midnight Merge architecture](docs/midnight-merge-foundation.md).

## Chill Lounge: all eight games in React

Word Search and Solitaire use visible completion dialogs with time, progress and XP instead of easily overlooked footer notices. Starting a different Word Search puzzle while one is saved requires confirmation; completed levels and collected discoveries are retained. Until dedicated tracks are selected, the Lounge hub and its activities share the casino lobby soundtrack.


The Chill Lounge uses a local React/TypeScript hub (`frontend/lounge.html`) with responsive navigation, eight activity tiles, including side-by-side featured Midnight Bastion and Midnight Sweep tiles, and German/English labels. **Sudoku is now rendered and played directly inside this React Lounge**, without opening the old WPF board in normal navigation. Its six difficulties, seeded and generated offline puzzle pools, daily assignments, 81-cell board, wrong-value feedback, hints, pencil notes, undo/redo, number completion, selectable guides, keyboard shortcuts, elapsed time, automatic saves and rewards are all supported. React sends allow-listed actions and displays C#-verified results; the C# `SudokuBoard`, `SudokuProgressState` and progression services remain authoritative and save-compatible. Completion XP is granted at most once through the existing profile receipts. See [Sudoku modernization](docs/sudoku-modernization.md).

**Midnight Solitaire now plays entirely inside the same React Lounge**, with the existing C# engine for Klondike, FreeCell, Spider (1, 2 and 4 suits), Pyramid, Golf and TriPeaks. Players can select or drag card runs, click a destination, double-click cards to send them to foundations, draw stock cards, see all eight table choices, resume independent saved variants, start numbered FreeCell deals and browse the existing achievements. The modern cards use the active collection skin palette; only C# determines legality, shuffles, completion, milestones and rewards. Revisions, command receipts and persistence-first moves avoid duplicate actions and partial game-state updates. See [Midnight Solitaire modernization](docs/solitaire-modernization.md).

**Word Search is now a word-discovery collection inside the React Lounge.** An in-mode **Words Discovered** album permanently records each distinct word found, with theme, difficulty and discovery date, plus search and filtering. New native-saved finds receive a soft sound cue and a short purple **New** marker beside the crossed-out target. The library now contains **96 deterministic puzzles per difficulty** (**576 total**) in 36 bilingual themes, including 12 new discovery worlds and hundreds of additional English/German vocabulary candidates. The original 288 puzzle IDs, seeds, saved grids, completion records and challenge rewards remain intact. The original XP rules stay authoritative in C#, with new endgame challenge milestones for the enlarged library. See [Word Search modernization](docs/word-search-modernization.md).

**Memory now plays directly in the React Lounge** as well. It preserves the five original table sizes (4×4, 4×5, 6×6, 6×8 and 8×8), all four curated decks (Casino Icons, Card Suits, Animals and Fruits & Treats), the native C# match/mismatch rules, flipped-card animations, the original flip/match/miss/perfect sound effects, streaks, accuracy, autosaved resume, and the completion summary with exactly-once XP awards. Only face-up and matched cards have identifying symbols in the browser: the native shuffle/solution remains private. See [Memory modernization](docs/memory-modernization.md).

**Jigsaw Puzzle now plays in the React Lounge with a PixiJS board.** The twelve offline images, eight grid sizes (24–1,000), saved piece positions, native snapping/group movement, thirty achievements and completion rewards remain compatible with the original C# implementation. The new studio includes the gallery, tray, zoom/panning, reference view, resume and completion. See [Jigsaw modernization](docs/jigsaw-modernization.md). **Tales from the Backroom** now also runs in React, joining the other React Chill Lounge routes. Its three public stories retain the native AdventureEngine, character classes, inventory and conditions, D20 checks, mystery board, Blackjack/Five-of-a-Kind encounters, saves and completion progression. The older Strangers' Wager remains a loadable compatibility/test story rather than a fourth public adventure. The legacy WPF presentation files have been removed; C# save compatibility is retained. See [Tales modernization](docs/adventure-modernization.md).

**Midnight Bastion (Tower Defense)** is the seventh lounge activity, shown as a featured custom strategy tile with its own vector fortress illustration. Phase 2 adds locally bundled illustrated unit sprites and a moonlit battlefield, projectile and impact FX, an expanded tower workshop, ambient fireflies and gesture-unlocked, preference-aware synthesized audio. Phase 1 has a PixiJS battlefield with one fixed-path map, 10 build pads, three upgradeable tower types, four enemy classes, 15 manually started waves, pause/resume, 1×/2× speed, selling, defeat/victory and native C#-verified simulation. The game autosaves inside existing app preferences and continues paused after reopening, including recovery of saves affected by the old negative spawn-clock defect. The wave indicator counts confirmed kills instead of enemy spawns or shots; native state distinguishes escapes. A compact desktop layout keeps the map and workshop in view, with expanded causeway/forest detailing and distinct tower-attack sounds. Tower Defense has no wagers or shared XP/coin payouts in this initial phase. See [Tower Defense phase 1](docs/tower-defense-phase1.md) and [current presentation and reliability notes](docs/tower-defense-phase2.md).

**Midnight Sweep (Minesweeper)** is the eighth Chill Lounge game, next to Bastion in the featured bottom row. Its original observatory-themed PixiJS field, React controls, WebAudio cues and win/loss overlays do not reuse assets from other games. The native C# rules support 8×8, 12×12 and 16×16 boards, a safe first 3×3 area, flags, number-assisted chording, connected empty-cell reveals, and one optional Echo Scan per game. The scan reveals the count of hazards within a 3×3 neighborhood without opening the selected tile. Only native Core sees hazard locations before a run finishes. Saved games, terminal receipts, challenge goals and profile statistics survive reopening; there are no casino wagers. See [Midnight Sweep architecture](docs/midnight-sweep.md).\n
## Midnight Dice: React/PixiJS dice lounge

Idle and held dice show one flat, readable result face. The six-sided 3D model is mounted only during a throw and hands the view back to the flat result after landing. Outcomes, holds, scoring and settlements remain authoritative in native C#.


Midnight Dice now opens through the local `frontend/midnight-dice.html` WebView2 page. Both the free solo score sheet and the paid four-player casino table use a responsive React interface with five equipped cosmetic dice, thirteen native score categories, individual hold/reroll controls, AI standings and opponent sheets, the existing entry tiers, result receipts and gold five-of-a-kind celebrations. The same C# `KniffelGame`, `KniffelCash`, `KniffelTable` and progression services resolve every dice face, score, fee, payout and reward **before** the browser updates. Session receipts prevent duplicate actions after a browser failure, and saved casual/casino states retain their original formats. The WPF dice source has been removed; the existing native C# score, receipt and persistence contracts remain. See [Midnight Dice](docs/kniffel.md).

## Neon Vault: React/PixiJS high-voltage casino

Neon Vault is now rendered in a responsive, local React/PixiJS cabinet (`frontend/neon-vault.html`). The established five-reel, four-row, 1,024-ways C# game resolves spins and all bonus features **before** the browser visualizes them. The modern client presents the existing tier ladder through $250K, the four Access Key jackpots, Vault Break free spins, expanding Wilds, Vault Energy / Security Override, three Blackout events and anticipation without calculating payouts. The original WPF slot window, reel animations and its old symbol textures are retired; the WPF-free NativeShell now owns the entire game lifecycle. See [Neon Vault implementation](docs/neon-vault.md).

## Nine Lives: React/PixiJS rooftop

The normal Nine Lives destination is now the responsive React/PixiJS cabinet (`frontend/nine-lives.html`) with the original eight 1024px symbol images served locally. The 3×3 slot, five paylines, Pounce Wilds, nine free spins, auto-spin, paytable, statistics and audio/animation preferences remain native-authoritative through `NineLivesSession` and a strict receipt/revision bridge. The former WPF Nine Lives window and reel renderer are no longer part of the desktop build. Browser failure returns to the lobby while retaining the already-settled native result and bonus-spin session. See [modernization roadmap](docs/modernization-roadmap.md).

## Casino Empire: modern presentation

Empire opens directly in React/TypeScript/WebView2, both inside the casino and in its optional detached window. The estate gallery, room inspector, floors, circuits, research, projects, properties, specialists, automation, events, contracts, statistics and prestige use the existing C# economy and transaction/save paths. One retained PixiJS lighting scene decorates the selected room; text and controls remain DOM content. Original room artwork is packaged offline at its existing source resolution.

The independent one-second economy clock is unchanged. Hidden/minimized views and reduced-motion settings stop decorative rendering. Purchases require a validated session/revision and request ID; previews use isolated C# state, and uncertain responses never automatically repeat a purchase. The former WPF Empire presentation is removed; native Empire state and transaction checks remain.

Native Windows/WebView2 smoke covers real lobby and game-route hosting; independent manual performance, DPI and extended-session acceptance remains required. See [Empire](docs/casino-empire.md) for the protocol, measurement limitations and manual acceptance checklist.

## Midnight Manor: modern presentation

**Midnight Manor has one lobby entry.** It opens the modern React/PixiJS version directly; the separate playtest button and selectable legacy WPF version have been retired.

- Five reels, scalable local symbol artwork, five staged reel stops and layered win feedback
- Fixed play-money bets of $50, $100, $250, $1,000, $2,500 and $5,000 (manual stakes $1–$5,000)
- 10/25/50/100 automatic paid-spin sessions with an immediate stop control; bonus games, free spins, insufficient balance and uncertain saves halt the queue
- The original Manor soundtrack, continuous reel movement sounds and an ornate startup cover
- Integrated 17-room Godot 3D Escape the Manor bonus with a permanent full map, ghost/item/checkpoint play and C#-committed payouts; the retired 2D bonus UI is gone. Godot is completely unloaded on bonus completion or exit to preserve PixiJS slot performance.
- German and English game controls and rules
- Shared virtual wallet, progression, saved bonus state and crash recovery
- Original C# game logic for random outcomes, payouts, bet validation and player decisions
- Local rendering and audio; the browser interface cannot directly change a balance or award

The integrated local Godot 3D Escape the Manor bonus replaces the older 2D bonus presentation. Its rooms, ghost/item logic, checkpoints and final payouts remain tied to validated native C# session state and receipts; the scene is unloaded on exit.

The former WPF Manor source is removed. Save formats, payout tables and game rules remain unchanged.

Technical details: [modernization roadmap](docs/modernization-roadmap.md), [Manor design](docs/midnight-manor-design.md), [acceptance and recovery tests](docs/midnight-manor-acceptance.md).

## Installation and updates

Download a packaged Windows build from [GitHub Releases](https://github.com/TeKatz/blackjack-casino/releases). A GitHub source-code archive is not an installable application.

| Channel | Purpose | Delivery |
| --- | --- | --- |
| **Stable** | Regular releases | Latest stable GitHub release |
| **Development** | Early access to validated changes | Mutable [development release](https://github.com/TeKatz/blackjack-casino/releases/tag/development), also available through the in-app updater |

The updater validates package size and SHA-256 before staged installation. Player data is stored outside the installation directory under `%LOCALAPPDATA%\BlackjackCasino`; NativeShell retains the already-used `NativeShellPreview/Candidates/development` profile for previous Development players and never silently reimports older data over it. Keep a backup when validating changes to persistence or updating an older installation.

**Branch model:** `main` is the tested integration baseline, and **`dev`** is the active source of the mutable Development updater. Only successfully verified builds from the [Development Release workflow](.github/workflows/development-release.yml) publish to the [existing in-app manifest](https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json). Feature/PR builds are not in-app updates. `main` does not automatically publish a Stable release. See [branching policy](docs/branching-and-release-policy.md).

See [Development update delivery](docs/development-update-delivery.md) for release mechanics, versioning and verification.

## Technology and repository layout

| Path | Responsibility |
| --- | --- |
| `src/BlackjackCasino.Core/` | Game rules, bankroll, rewards, progression and state models |
| `src/BlackjackCasino.NativeShell/` | Production WinForms/WebView2 host, game bridges and native recovery |
| `src/BlackjackCasino.HostShared/` | Shared C# save, protocol, audio and profile sources (compiled into NativeShell and tests) |
| `src/BlackjackCasino.Assets/` | Original offline artwork, icons, licensed audio and provenance |
| `src/BlackjackCasino.CompatLauncher/` | WPF-free launcher with the old executable filename for older upgrades |
| `frontend/` | Modern lobby and Midnight Manor pages, local generated asset atlases and PixiJS effects |
| `src/BlackjackCasino.Update/` | Update manifest, version and package validation |
| `src/BlackjackCasino.Updater/` | Staged installation and restart helper |
| `tests/BlackjackCasino.Tests/` | Unit and integration tests |
| `tools/` | Build safeguards, localization checks and Windows smoke tests |
| `docs/` | Architecture, gameplay specifications and implementation history |
| `.github/workflows/` | CI, frontend, presentation and release workflows |

**Architecture principle:** C# remains authoritative for random results, payouts and saved progression. React/PixiJS handles presentation only. In particular, the React Casino Vault uses the existing crash-recoverable C# payout transaction; React profile actions go through the original identity validation, cosmetic ownership and persistence services. Collector's Album cards, pack inventory, duplicate points, completion rewards and pending openings also stay C#-authoritative; the browser only animates stored receipts. The game must remain fully playable offline.

Read the [architecture guide](docs/architecture.md) for service boundaries and the [modernization roadmap](docs/modernization-roadmap.md) for the phased migration of other game interfaces.

## Building and testing

Development requires Windows and the **.NET 10 SDK** compatible with [global.json](global.json). The modern frontend uses **Node.js 22**.

Build the frontend before NativeShell so its offline bundles are included in the release output:

```powershell
cd frontend
npm ci --no-audit --no-fund
npm test
npm run build
cd ..

python tools/check_repo_contract.py
python tools/check_native_cutover.py
python tools/check_localization.py
python tools/check_localization_coverage.py --strict-shell
python tools/check_adventure_localization.py
dotnet restore BlackjackCasino.sln
dotnet build BlackjackCasino.sln --configuration Release
dotnet test BlackjackCasino.sln --configuration Release --no-build
dotnet run --project src/BlackjackCasino.NativeShell --configuration Release -- --smoke
dotnet run --project src/BlackjackCasino.NativeShell --configuration Release -- --web-smoke
```

Windows CI additionally runs NativeShell receipt, real WebView2 startup/game navigation and packaged artwork checks. A successful frontend build or documentation check alone does not prove an installable Windows release. Production packages are created and verified by the release workflows.

## Final native-host cutover

Development publishes `BlackjackCasino.NativeShell.exe` directly, with a WPF-free compatibility alias for existing updater installations. The original WPF, XAML, fallback games and WPF smoke-tool sources have been retired. On its first launch the NativeShell imports an older WPF save only if no persistent native profile already exists. An existing Development native profile always takes precedence; neither a failed import nor a browser crash overwrites the original legacy data. The external updater stages, validates and atomically replaces program files, then restarts NativeShell. Stable changes are released only by explicit verified tags from `main`. See [native host cutover](docs/native-shell-migration.md).

## Current engineering boundaries

- **Midnight Manor:** modern presentation is the primary route; remaining subjective audio, display-scaling and long-session performance checks are tracked separately.
- **Blackjack:** the retired native table, sidebar and custom rule dialog have been removed. React is the only player-facing Blackjack table; its C# session survives a WebView failure and can be rehosted.
- **Other game interfaces:** All Chill Lounge activities, Midnight Dice and other casino surfaces use local React/WebView2. Native C# rules, state, progression and financial receipts remain authoritative; there is no WPF game fallback.
- **Localization:** English/German shell support is present; some older game and adventure content still needs translation.
- **Player data and financial outcomes:** preserve backward compatibility, deterministic payout tests and exactly-once committed transitions.
- **Releases:** `dev` publishes the mutable Development channel after verification; Stable is an explicitly tagged release from the tested integrated code on `main`.

The roadmap tracks completed migration stages and outstanding quality gates; historical milestone notes are not separate active feature variants.

## Development guides

- [Contributing](CONTRIBUTING.md) and [coding-agent agreement](AGENTS.md)
- [Modernization roadmap](docs/modernization-roadmap.md)
- [Development release and updater](docs/development-update-delivery.md)
- [Branching, integration and release policy](docs/branching-and-release-policy.md)
- [Tower Defense graphics and implementation](docs/tower-defense-phase2.md)
- [Manor quality and release acceptance](docs/manor-phase-3-signoff.md)
- [Localization status](docs/localization-overhaul.md)
- [Maintenance audit](docs/maintenance-audit.md)
- [Jigsaw Puzzle](docs/jigsaw.md)
- [Jigsaw React migration](docs/jigsaw-modernization.md)
- [Cosmetic shop expansion](docs/shop-cosmetics-expansion.md)
- [Casino Empire](docs/casino-empire.md)
- [Tales from the Backroom](docs/adventure-engine.md)
- [Tales React migration](docs/adventure-modernization.md)
- [Historical WPF cleanup inventory](docs/legacy-wpf-inventory.md)

### Midnight Bastion campaign

Midnight Bastion in Chill Lounge now includes forest and Frostpass maps, an independent Bastion XP level and fully unlockable seven-skill mastery tree, Ballista/Flame Spire level-gated towers, saved map progress, optional endless defence after each campaign victory, drag-to-build controls, and global challenge/statistics integration. See [Bastion design and persistence](docs/tower-defense-phase2.md). No casino credits are wagered in this mode.
