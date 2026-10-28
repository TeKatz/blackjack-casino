# Phase 6 — Stats, Profile & Progression

Phase 6 adds long-term progression without changing the core laid-back blackjack loop. A player can still launch the game, stay on Casual Solo and play without opening any meta-system.

## Player level

- levels run from 0 to 100;
- the full curve is roughly 115k XP per Prestige cycle;
- every completed round grants XP, including losses and pushes;
- wager size is deliberately not part of the XP formula;
- wins, natural blackjacks, successful doubles and split play give small bonuses;
- level-ups grant cosmetic currency (Coins).

## Prestige

At level 100 the player may voluntarily enter Prestige. Prestige resets only level and current level XP. It keeps bankroll, Coins, cosmetics, stats, history, settings and Season Pass progress.

Prestige grants 500 Coins plus milestone-exclusive titles/frames. The initial reward catalog includes rewards at P1, P2, P3, P5, P10 and P25 and can be expanded without changing the progression format.

## Statistics

Persistent lifetime stats include rounds/hands, W/L/push, win rate inputs, blackjacks, doubles, double wins, splits, surrender, insurance, total wagered, net profit, biggest win/loss, bankroll highs/lows and streaks.

The same counters exist for the current app session and per table preset/custom rules profile.

## Hand history

Up to 1,000 completed rounds are stored locally in `hand-history.json`. History includes cards, actions, outcomes, bets, insurance, table rules and bankroll movement.

## Coins and cosmetic shop

Coins are earned through level-ups, Prestige and the free Season track. They cannot affect blackjack outcomes, bankroll or rules. The first shop contains profile titles and profile frames only.

## Season Pass

Season 1 is **Midnight Shift**:

- 50 tiers;
- one free track, no premium track;
- separate Season XP earned by ordinary play;
- Coins and exclusive titles/frames as rewards;
- seasons are designed to remain accessible instead of disappearing for FOMO.

## Persistence

Progression is stored separately from ordinary app preferences:

- `profile.json` — level, Prestige, Coins, cosmetics, season state and aggregate stats;
- `hand-history.json` — capped detailed round history;
- `player-state.json` — bankroll and ordinary game/presentation preferences from earlier phases.
