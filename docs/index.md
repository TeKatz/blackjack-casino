# Documentation

This is the documentation entry point for **The Midnight Club**. The [README](../README.md) is the short product overview of the currently published Development build. The reviewed `main` Stable source has not yet received the full native cutover. The guides below are divided into **current gameplay and engineering references** and **historical implementation records**; old WPF migration notes are not instructions for today's native Windows app.

## Games and features

| Topic | Guide |
| --- | --- |
| Blackjack | [Modern Blackjack](blackjack-modernization.md) |
| Roulette and classic slots | [Midnight Reels](slots.md) |
| Slot machines | [Midnight Manor](midnight-manor-design.md), [3D Escape bonus](manor-godot-bonus.md), [Neon Vault](neon-vault.md), [Nine Lives](nine-lives.md) |
| Dice and merging | [Midnight Dice](kniffel.md), [Midnight Merge](midnight-merge-foundation.md) |
| Casino management | [Casino Empire](casino-empire.md) |
| Lounge: puzzles | [Sudoku](sudoku.md), [Jigsaw](jigsaw.md), [Solitaire](solitaire-modernization.md), [Word Search](word-search-modernization.md), [Memory](memory-modernization.md) |
| Lounge: adventure and strategy | [Tales from the Backroom](adventure-engine.md), [Midnight Bastion](tower-defense-phase2.md), [Midnight Sweep](midnight-sweep.md) |
| Action roguelite | [Midnight Descent](midnight-descent.md) |
| Progression and customization | [Progression](table-profile-progression.md), [Collector's Album](collectible-album.md), [Casino Vault](casino-vault.md), [Cosmetics](shop-cosmetics-expansion.md) |

## Development and operations

- [Architecture](architecture.md) — native host, C# rules and receipt journals, WebView2 and native data profiles.
- [Contributing](../CONTRIBUTING.md) — Windows build, test and pull-request guidance.
- [Branch and release policy](branching-and-release-policy.md) — current `dev`, `main` and tagged releases.
- [Development updates](development-update-delivery.md) — publishing and updater contract.
- [Presentation regression](presentation-regression.md) — present-day visual/UI checks.
- [Localization](localization-overhaul.md) — current English/German coverage and historical rollout details.
- [Performance notes](performance-phase-7.md) — historical measurements and acceptance context, not a current universal FPS guarantee.
- [Repository maintenance audit](maintenance-audit.md) — latest verified notes and historical dependency audits.

## Historical reference and migration archive

These files retain implementation chronology, original requirements and regression evidence. They may mention removed WPF classes, past branches or functionality that was incomplete **at that time**. Use the guides above and [architecture](architecture.md) for the current implementation.

- [Modernization roadmap and milestone history](modernization-roadmap.md)
- [Final NativeShell cutover and prior migration stages](native-shell-migration.md)
- [WPF inventory (retired)](legacy-wpf-inventory.md)
- [Host modernization phase 5](host-modernization-phase5.md)
- [Initial host readiness assessment](host-phase-a-readiness.md)
- [Migration regression repairs](migration-regression-repairs.md)
- [Original project phases 1–6](phase-1.md)
- [Midnight Manor acceptance history](midnight-manor-acceptance.md)

Historical WPF return and rollback test code lives only in `tools/NativeMigrationDiagnostics/`; it is **not part of the distributed casino**. The existing `NativeShellPreview` player-data folder is a compatibility path, not an experimental game mode.
