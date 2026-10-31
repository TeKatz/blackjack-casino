# Chill Lounge game infrastructure

This document defines the shared conventions for adding more Chill Lounge games without turning the lounge shell, progression code or presentation into per-game special cases.

## Goals

New lounge games should be easy to add, independently testable and able to reuse the same presentation and progression conventions. Game rules and save formats stay game-specific; navigation, registration, reward plumbing and visual primitives should not be copied for every mode.

## Menu registration

`LoungeGame` is the menu contract. A game supplies:

- stable `Id`
- `Title`
- `Subtitle`
- `Description`
- `Open` route
- optional artwork factory
- optional `SortOrder`
- optional dynamic `Status`

`LoungeGameCatalog.Normalize` validates registrations and sorts the result. Duplicate ids fail fast. The menu remains unaware of game rules, save models or progression details.

Use stable ids from the first implementation. They may later be referenced by statistics, achievements, smoke tests, telemetry-free local receipts or migration code.

## Shared lounge presentation

`LoungeUi` contains the common Lounge palette and reusable button/card primitives. New games should start from these helpers and only introduce local styles where the game has a deliberate visual identity.

This keeps navigation, cards and small actions visually consistent while still allowing Solitaire, Mahjong, Word Search, Memory and Midnight Merge to have distinct play surfaces.

## Core / Desktop boundary

Game rules, deterministic validation, completion records and statistics belong in `BlackjackCasino.Core` whenever practical.

WPF views, animation, sound, input handling and the Lounge menu route belong in `BlackjackCasino.Desktop`.

Do not make core game logic depend on WPF types.

## Persistence

Prefer typed state per game instead of an untyped shared JSON blob. Every state model should have explicit normalization/migration rules and safe defaults so older profiles remain loadable.

UI-only preferences such as timer visibility may live in `AppPreferences`. Durable career statistics and reward receipts belong in `PlayerProfileState` or a typed child statistics object.

An in-progress game should be snapshotted when leaving its play surface or closing the Lounge. A completed game should remain safe to reopen without paying progression rewards twice.

## Progression rewards

Non-wagering Lounge games should use `LoungeProgression.GrantOnce` for the common reward transaction inside their game-specific progression class.

Each game still owns its durable receipt. The flow is:

1. Validate the completion record.
2. Attempt to record its stable completion receipt in game-specific statistics.
3. If the receipt already exists, return `ProgressionAward.None`.
4. Otherwise grant player XP and Season XP through `LoungeProgression.GrantOnce`.
5. Persist the candidate profile using the existing transactional profile-save pattern before adopting it in memory.

Sudoku already follows this contract and is the reference implementation.

The helper intentionally does not decide reward amounts. Solitaire, Mahjong, Word Search and Memory can use different schedules without duplicating level/season award plumbing.

## Statistics

Do not force unrelated games into one giant generic statistics model. Give each game the fields that actually matter to it, for example:

- Solitaire: wins, losses, moves, time, streaks, variant-specific clears.
- Mahjong: clears, layouts, no-shuffle clears, hints/shuffles, best times.
- Word Search: boards completed, sizes, hints, best times, themed packs.
- Memory: clears, board sizes, flips/misses, best times, low-move clears.
- Midnight Merge: orders completed, merges, highest tiers, areas restored.

Shared presentation can aggregate those statistics later without weakening type safety.

## Achievements and challenges

Achievements/challenges should read the durable game statistics rather than transient UI state. Completion receipts must be idempotent so reloads, autosaves and retries cannot manufacture progress.

## Audio and animation

Respect the existing global sound and animation preferences. A game may have bespoke sounds and effects, but should not invent another global setting for the same purpose.

## Adding a new game

Recommended sequence:

1. Implement and test the core rules/state.
2. Add typed career statistics and a stable completion receipt if the game pays progression rewards.
3. Add a game-specific progression class that uses `LoungeProgression.GrantOnce`.
4. Add the WPF play surface and save-on-exit lifecycle.
5. Register one `LoungeGame` tile with a stable id, order, artwork and optional status.
6. Reuse `LoungeUi` for common Lounge chrome.
7. Add XP, statistics and achievement tests.
8. Add a Windows smoke path for opening, playing/saving and returning to the Lounge.

## Planned order

The current planned sequence is:

1. Solitaire
2. Mahjong Solitaire
3. Word Search
4. Memory
5. Casino Collection System (cross-game meta feature)
6. Midnight Merge

The Collection System is not itself a normal Lounge board game, but the preceding games should expose stable statistics and reward hooks so it can integrate with them without retrofitting ad-hoc signals later.
