# Casino Empire

Casino Empire is a dedicated **active incremental clicker** within The Midnight Club. The lobby title and artwork are preserved, but the old idle management mode has been replaced.

## Core loop

- Click the growing chip stack to earn construction credits.
- Buy finite upgrades across twelve casino buildings (up to level 8 each).
- Buildings generate passive credits; the income also scales the value of active clicking. Either playstyle can make progress, but active clicking remains useful.
- Each building has four visible development tiers (levels 1, 3, 5 and 8), shown on its expandable card.
- A single permanent milestone display shows the next reachable construction goal and its credit progress. The interface gradually reveals new buildings.
- Every 25th tap earns a deterministic triple-click reward (which can be improved in the active Prestige path). Reaching twenty building levels and the run-credit requirement unlocks a prestige reset with a permanent production multiplier.
- **Prestige I** introduces a left-hand **Legacy** rail without adding any UI clutter to a fresh run. It contains fifteen permanent purchases in three paths: High Roller (active clicking), Tycoon (passive production), and Inner Circle (both systems). Only the next purchase in each chain is revealed.
- Each reset grants **two Legacy points**, and twenty once-per-profile milestones grant **one point each**. Milestone tracks cover lifetime clicks, lifetime credits, peak building levels and prestige count; the UI shows the next goal in each group.
- Prestige investments and earned milestones survive further resets. The existing `v4` clicker save is maintained. Earlier Prestige levels are credited retrospectively on first load, once, without changing saved credits or building progress.
- Unattended earning is limited to four hours; there are no automatic building purchases.

The 1.0 reboot intentionally removes the legacy property portfolio, level-200 rooms, management automation, research trees, staff, projects, contracts, events, and owner dividend system. It also **removes the 18 former challenge-to-Empire production boosts** without altering the challenges themselves or their regular coins/cosmetics/pack rewards.

## Architecture

The C# `EmpireService` is authoritative for taps, prices, passive income, unlock requirements and prestige. React renders the UI but cannot mint credits or purchase upgrades itself.

The WebView2 protocol is `empire.command` version **2**, accepting only `tap` (1–20 taps per batch), `building.upgrade`, `prestige`, or the validated `legacy.buy` purchase action. Each successful command is saved durably before an acknowledgement. Request IDs and expected revisions prevent duplicate payouts after retries and reconnects. The receipt cache is bounded; stale revisions cannot replay evicted commands.

The old Empire save schema 3 is **not compatible** with the new economy. Before loading a legacy started save, `PlayerStateStore` makes a one-time backup `player-state.pre-empire-clicker.json` alongside the native profile. The new Empire begins fresh; all non-Empire profile data is retained.

NativeShell embedded and detached WebView2 windows continue to use the same trusted route and profile writer. The outer casino bankroll stays independent of Empire construction credits.

## Next passes

The Legacy paths are the first permanent content layer, not the finished endgame. Balancing, additional abilities and late-game casino expansions will be iterated on after real Windows playtests. Do not confuse initial milestone pacing with a finished long-term economy.
