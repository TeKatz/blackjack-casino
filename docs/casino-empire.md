# Casino Empire

Casino Empire is a dedicated **active incremental clicker** within The Midnight Club. The lobby title and artwork are preserved, but the old idle management mode has been replaced.

## Core loop

- Click the growing chip stack to earn construction credits.
- Buy finite upgrades across twelve casino buildings (up to level 8 each).
- Buildings generate passive credits; the income also scales the value of active clicking. Either playstyle can make progress, but active clicking remains useful.
- Each building has four visible development tiers (levels 1, 3, 5 and 8), shown on its expandable card.
- A single permanent milestone display shows the next reachable construction goal and its credit progress. The interface gradually reveals new buildings.
- Every 25th tap earns a deterministic triple-click reward. Reaching twenty building levels and the run-credit requirement unlocks a prestige reset with a permanent production multiplier.
- Unattended earning is limited to four hours; there are no automatic building purchases.

The 1.0 reboot intentionally removes the legacy property portfolio, level-200 rooms, management automation, research trees, staff, projects, contracts, events, and owner dividend system. It also **removes the 18 former challenge-to-Empire production boosts** without altering the challenges themselves or their regular coins/cosmetics/pack rewards.

## Architecture

The C# `EmpireService` is authoritative for taps, prices, passive income, unlock requirements and prestige. React renders the UI but cannot mint credits or purchase upgrades itself.

The WebView2 protocol is `empire.command` version **2**, accepting only `tap` (1–20 taps per batch), `building.upgrade`, or `prestige`. Each successful command is saved durably before an acknowledgement. Request IDs and expected revisions prevent duplicate payouts after retries and reconnects. The receipt cache is bounded; stale revisions cannot replay evicted commands.

The old Empire save schema 3 is **not compatible** with the new economy. Before loading a legacy started save, `PlayerStateStore` makes a one-time backup `player-state.pre-empire-clicker.json` alongside the native profile. The new Empire begins fresh; all non-Empire profile data is retained.

NativeShell embedded and detached WebView2 windows continue to use the same trusted route and profile writer. The outer casino bankroll stays independent of Empire construction credits.

## Next passes

This is an initial playable foundation. Balancing, richer stage artwork, ability synergies, animations and late-game prestige choices will be iterated on after real Windows playtests. Do not confuse initial milestone pacing with a finished long-term economy.
