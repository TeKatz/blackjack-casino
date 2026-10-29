# Phase 6 — Stats, Profile & Progression

Phase 6 adds long-term progression without changing the laid-back casino loop. A player can still launch the game, choose a mode and play without opening any meta-system.

## Player level

- levels run from 0 to 100;
- the full curve is roughly 115k XP per Prestige cycle;
- completed Blackjack rounds and supported slot play grant progression XP;
- wager size is deliberately not used to scale XP rewards;
- wins and notable actions/features can grant small bonuses;
- level-ups grant cosmetic currency (Coins).

## Prestige

At level 100 the player may voluntarily enter Prestige. Prestige resets only level and current level XP. It keeps bankroll, Coins, cosmetics, verified statistics, history, settings and Season progress.

Prestige grants 500 Coins plus milestone-exclusive titles/frames. Reward milestones include P1, P2, P3, P5, P10 and P25 and can be extended without changing the persistence format.

## Statistics

Persistent statistics are now split by game where the source is known:

- Blackjack;
- Midnight Reels;
- Midnight Manor;
- Neon Vault.

Casino-wide summaries aggregate the verified per-game buckets plus preserved legacy history. Older saves created when Midnight Reels and Neon Vault shared one source-less slot counter cannot be separated safely after the fact, so that history is retained in an **earlier slot history** bucket instead of being guessed into one of the modern modes.

See [progression tracking migration](progression-tracking-migration.md) for the compatibility rules.

## Hand history

Up to 1,000 completed Blackjack rounds are stored locally in `hand-history.json`. History includes cards, actions, outcomes, bets, insurance, table rules and bankroll movement.

## Coins and cosmetic shop

Coins are earned through level-ups, Prestige, challenges and the free Season track. They cannot affect game outcomes, bankroll rules or slot math.

The current cosmetic system includes titles, frames and premium portrait artwork. Shop content is searchable, sortable and paged; owned cosmetics can be equipped from the same in-app flow.

## Challenges

Challenges cover Blackjack, Midnight Reels, Midnight Manor, Neon Vault, lifetime casino play and long-term Prestige goals.

The Challenges tab provides:

- game/status filters;
- text search;
- bounded pagination;
- persistent claim state;
- **Claim All**, which collects every currently eligible unclaimed reward even when some entries are hidden by the active search/filter.

Claim All persists as one batch and rolls back the batch if saving fails.

## Season Pass

Season 1 is **Midnight Shift**:

- 50 tiers;
- one free track, no premium track;
- separate Season XP earned by ordinary play;
- Coins and exclusive cosmetic rewards;
- seasons are designed to remain accessible instead of disappearing for FOMO.

The presentation uses a clear reward track with current tier, next reward, milestone cards and distinct unlocked/next/locked states.

## In-app profile and progression navigation

Profile and progression no longer appear as separate visible Windows windows. They are hosted inside the main casino shell with a back stack:

- opening Progression from Profile returns to Profile;
- opening Progression from the lobby returns to the lobby;
- covered Blackjack actions are disabled while a meta page is active;
- Escape navigates back one in-app level;
- F11 continues to control the main application window.

The Profile screen uses the same casino design language as Progression and includes persistent identity information, rename support and a searchable owned portrait collection.

See [table/profile/progression refresh](table-profile-progression.md).

## Persistence

Progression is stored separately from ordinary app preferences:

- `profile.json` — level, Prestige, Coins, cosmetics, Season state and aggregate/per-game statistics;
- `hand-history.json` — capped detailed Blackjack round history;
- `player-state.json` — bankroll and ordinary game/presentation preferences from earlier phases.

Normalization/migration preserves older compatible save data and keeps ambiguous historical slot statistics visible rather than silently reassigning or deleting them.

## Verification

Unit tests cover progression, tracking migration, profile identity, Claim All behavior and playtime accrual. Windows presentation regression additionally covers profile/progression hosting, focus isolation, navigation, search, pagination and minimum/regular layout sizes.
