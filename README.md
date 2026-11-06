# The Midnight Club — Blackjack Casino

An **offline-first, single-player casino for Windows**, built around virtual credits and progression. No real-money gambling, payments, accounts, advertisements or online services are required for gameplay.

The desktop application uses **.NET 10 with WPF as the Windows host**. The casino lobby, Chill Lounge hub, Blackjack table, Midnight Reels, Neon Vault, Nine Lives, Midnight Merge, Midnight Dice, Casino Empire and Midnight Manor now have modern **React + TypeScript** presentations, hosted locally through **WebView2**; PixiJS powers richer scenes where appropriate. Player profile editing (display name, owned titles/portraits), Season Pass management, Blackjack Hand History and the lobby's Casino Vault now live in React, alongside the shop, challenges and statistics. The Collector's Album (including its five-card pack-opening animations, artwork archive and sequential Open All) has also moved to React. Progress export and the emergency Album fallback remain native. Existing game logic and persistent player data remain in C#.

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
- The original **WPF lobby remains as an emergency fallback** if WebView2 is unavailable or the modern page does not load; the Blackjack table is never the startup surface. The obsolete native Daily Login drawer and automatic startup welcome were removed from the normal startup path. Every existing game opens through the original registered C# action, including Casino Empire's detached-window option.

For native builds, run `npm run build` from `frontend/` before the .NET build. Vite emits `index.html` (Manor), `lobby.html`, `empire.html`, `blackjack.html` and `reels.html`, plus their local assets into `frontend/dist/`; the desktop project packages that whole directory as `ModernFrontend`.

## Blackjack: native table removed

The sole playable Blackjack interface is React/TypeScript (`frontend/blackjack.html`). `BlackjackTableController` holds the current Core session, wagers, rules, AI seats and presentation preferences without hidden WPF table controls. The old XAML table and fallback are gone; a failed browser returns to the casino lobby, preserving the current in-memory hand for a fresh React opening. See [Blackjack modernization](docs/blackjack-modernization.md).

## Midnight Merge: React/PixiJS restoration workshop

Midnight Merge runs as a responsive local React/PixiJS experience (`frontend/midnight-merge.html`): a persistent 7×6 board with drag/drop and keyboard-friendly click selection, both original producer chains, fourteen supply orders, staged lobby/bar restoration, translated milestones, profile XP and native audio. `MergeBoard` remains the C# authority for spawning, moving, combining, delivering and saving. Every accepted action is saved atomically before the browser renders it; the versioned receipt protocol prevents duplicate rewards and stale commands across a WebView2 failure. The former `MidnightMergeView` WPF gameplay view has been retired, without changing the durable game schema or rewards. See [Midnight Merge architecture](docs/midnight-merge-foundation.md).

## Chill Lounge: all six games in React

The Chill Lounge uses a local React/TypeScript hub (`frontend/lounge.html`) with responsive navigation, six activity tiles and German/English labels. **Sudoku is now rendered and played directly inside this React Lounge**, without opening the old WPF board in normal navigation. Its six difficulties, seeded and generated offline puzzle pools, daily assignments, 81-cell board, wrong-value feedback, hints, pencil notes, undo/redo, number completion, selectable guides, keyboard shortcuts, elapsed time, automatic saves and rewards are all supported. React sends allow-listed actions and displays C#-verified results; the C# `SudokuBoard`, `SudokuProgressState` and progression services remain authoritative and save-compatible. Completion XP is granted at most once through the existing profile receipts. See [Sudoku modernization](docs/sudoku-modernization.md).

**Midnight Solitaire now plays entirely inside the same React Lounge**, with the existing C# engine for Klondike, FreeCell, Spider (1, 2 and 4 suits), Pyramid, Golf and TriPeaks. Players can select or drag card runs, click a destination, double-click cards to send them to foundations, draw stock cards, see all eight table choices, resume independent saved variants, start numbered FreeCell deals and browse the existing achievements. The modern cards use the active collection skin palette; only C# determines legality, shuffles, completion, milestones and rewards. Revisions, command receipts and persistence-first moves avoid duplicate actions and partial game-state updates. See [Midnight Solitaire modernization](docs/solitaire-modernization.md).

**Word Search has also moved directly into the React Lounge.** Its interactive letter grid supports mouse/touch drag selection and two-endpoint keyboard/click selection, highlights native found-word lines, saves hints and puzzle progress, and shows completion rewards from the original C# progression engine. At the same time, the offline catalog was **doubled from 24 to 48 puzzles per difficulty** (144 → **288 puzzles total**). Twelve new bilingual themed word banks bring the total to 24 themes and broaden the vocabulary. All original 144 puzzle definitions, IDs, seeds, saved letter grids and earned completion receipts remain compatible; the existing 144-puzzle challenge milestones remain claimable, with new higher goals for the expanded catalog. See [Word Search modernization](docs/word-search-modernization.md).

**Memory now plays directly in the React Lounge** as well. It preserves the five original table sizes (4×4, 4×5, 6×6, 6×8 and 8×8), all four curated decks (Casino Icons, Card Suits, Animals and Fruits & Treats), the native C# match/mismatch rules, flipped-card animations, the original flip/match/miss/perfect sound effects, streaks, accuracy, autosaved resume, and the completion summary with exactly-once XP awards. Only face-up and matched cards have identifying symbols in the browser: the native shuffle/solution remains private. See [Memory modernization](docs/memory-modernization.md).

**Jigsaw Puzzle now plays in the React Lounge with a PixiJS board.** The twelve offline images, eight grid sizes (24–1,000), saved piece positions, native snapping/group movement, thirty achievements and completion rewards remain compatible with the original C# implementation. The new studio includes the gallery, tray, zoom/panning, reference view, resume and completion. See [Jigsaw modernization](docs/jigsaw-modernization.md). **Tales from the Backroom** now also runs in React, completing the six normal Chill Lounge routes. Its three public stories retain the native AdventureEngine, character classes, inventory and conditions, D20 checks, mystery board, Blackjack/Five-of-a-Kind encounters, saves and completion progression. The older Strangers' Wager remains a loadable compatibility/test story rather than a fourth public adventure. Legacy WPF presentations remain as rollback coverage. See [Tales modernization](docs/adventure-modernization.md).

## Midnight Dice: React/PixiJS dice lounge

Midnight Dice now opens through the local `frontend/midnight-dice.html` WebView2 page. Both the free solo score sheet and the paid four-player casino table use a responsive React interface with five equipped cosmetic dice, thirteen native score categories, individual hold/reroll controls, AI standings and opponent sheets, the existing entry tiers, result receipts and gold five-of-a-kind celebrations. The same C# `KniffelGame`, `KniffelCash`, `KniffelTable` and progression services resolve every dice face, score, fee, payout and reward **before** the browser updates. Session receipts prevent duplicate actions after a browser failure, and saved casual/casino states retain their original formats. The WPF dice interface is retained only as unlinked source for rollback while device visual/audio acceptance continues. See [Midnight Dice](docs/kniffel.md).

## Neon Vault: React/PixiJS high-voltage casino

Neon Vault is now rendered in a responsive, local React/PixiJS cabinet (`frontend/neon-vault.html`). The established five-reel, four-row, 1,024-ways C# game resolves spins and all bonus features **before** the browser visualizes them. The modern client presents the existing tier ladder through $250K, the four Access Key jackpots, Vault Break free spins, expanding Wilds, Vault Energy / Security Override, three Blackout events and anticipation without calculating payouts. The original WPF slot window, reel animations and its old symbol textures are retired; the native host and lobby artwork remain until the surrounding shell is replaced. See [Neon Vault implementation](docs/neon-vault.md).

## Nine Lives: React/PixiJS rooftop

The normal Nine Lives destination is now the responsive React/PixiJS cabinet (`frontend/nine-lives.html`) with the original eight 1024px symbol images served locally. The 3×3 slot, five paylines, Pounce Wilds, nine free spins, auto-spin, paytable, statistics and audio/animation preferences remain native-authoritative through `NineLivesSession` and a strict receipt/revision bridge. The former WPF Nine Lives window and reel renderer are no longer part of the desktop build. Browser failure returns to the lobby while retaining the already-settled native result and bonus-spin session. See [modernization roadmap](docs/modernization-roadmap.md).

## Casino Empire: modern presentation

Empire opens directly in React/TypeScript/WebView2, both inside the casino and in its optional detached window. The estate gallery, room inspector, floors, circuits, research, projects, properties, specialists, automation, events, contracts, statistics and prestige use the existing C# economy and transaction/save paths. One retained PixiJS lighting scene decorates the selected room; text and controls remain DOM content. Original room artwork is packaged offline at its existing source resolution.

The independent one-second economy clock is unchanged. Hidden/minimized views and reduced-motion settings stop decorative rendering. Purchases require a validated session/revision and request ID; previews use isolated C# state, and uncertain responses never automatically repeat a purchase. The old Empire WPF source is retained for rollback and benchmark comparison, outside the normal navigation path.

Windows smoke captures the three supported desktop sizes and detached hosting. A sequential WPF/modern benchmark writes actual CPU, memory and callback interval samples to `empire-presentation-performance.json`. These runner diagnostics do not certify GPU performance, long sessions or visual acceptance. See [Empire](docs/casino-empire.md) for the protocol, measurement limitations and manual acceptance checklist.

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

**Architecture principle:** C# remains authoritative for random results, payouts and saved progression. React/PixiJS handles presentation only. In particular, the React Casino Vault uses the existing crash-recoverable C# payout transaction; React profile actions go through the original identity validation, cosmetic ownership and persistence services. Collector's Album cards, pack inventory, duplicate points, completion rewards and pending openings also stay C#-authoritative; the browser only animates stored receipts. The game must remain fully playable offline.

Read the [architecture guide](docs/architecture.md) for service boundaries and the [modernization roadmap](docs/modernization-roadmap.md) for the phased migration of other game interfaces.

## Building and testing

Development requires Windows and the **.NET 10 SDK** compatible with [global.json](global.json). The modern frontend uses **Node.js 22**.

Build the frontend before the Windows client so its bundled files are included in the desktop output:

```powershell
cd frontend
npm ci --no-audit --no-fund
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
- **Blackjack:** the retired native table, sidebar and custom rule dialog have been removed. React is the only player-facing Blackjack table; its C# session survives a WebView failure and can be rehosted.
- **Other game interfaces:** the Chill Lounge hub and all six activities—Sudoku, six Midnight Solitaire variants, Word Search, Memory, Jigsaw and Tales from the Backroom—are React. The old Lounge/Jigsaw/Adventure WPF code remains as emergency fallback and regression coverage. Old Sudoku, Solitaire, Word Search and Memory presentations persist only for fallback and legacy regression checks, not normal navigation. The Midnight Dice WPF source remains for rollback and legacy visual tests, but its normal play route is React. The shared WPF host and emergency lobby remain until all destinations are accepted.
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
- [Jigsaw React migration](docs/jigsaw-modernization.md)
- [Cosmetic shop expansion](docs/shop-cosmetics-expansion.md)
- [Casino Empire](docs/casino-empire.md)
- [Tales from the Backroom](docs/adventure-engine.md)
- [Tales React migration](docs/adventure-modernization.md)

