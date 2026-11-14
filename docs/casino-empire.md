# Casino Empire

**Casino Empire** is a management and idle-progression game inside The Midnight Club. Build a casino portfolio, expand departments, research improvements and optimize income over time. It runs in the local React/WebView2 interface, either embedded in the main casino or in its detachable management window.

## Managing your empire

- Expand properties, rooms, floors and operating departments.
- Improve production through upgrades, research, staff, automation and capital projects.
- Balance events, contracts, specialists, prestige and long-term progression.
- Revisit the estate after time away; eligible offline advancement is computed and validated by the C# economy.
- Use the interactive estate atlas, management panels, statistics and themed room presentation.

Empire has a **separate construction currency and economy** from ordinary casino wagers. Its owner-dividend mechanic is the controlled connection back to the shared play-money bankroll. It does not alter Blackjack rules, dice outcomes or slot-machine payout tables.

## Architecture and saves

The C# `EmpireService` remains authoritative for production, prices, available upgrades, contracts and transactions. React can request a preview or action, but cannot choose the resulting money, state or rewards.

Every management mutation is checked against the current session, revision and unique request ID. The accepted result is persisted before new state is presented; failed or stale commands must not buy an item twice. NativeShell owns the WebView2 lifecycle, detached window and browser recovery. **No WPF host or WPF fallback is included in the current application.**

## Performance and acceptance

Decorative PixiJS effects respect animation preferences and visibility. Real Windows verification is still needed for cold/warm opening time, detached-window lifecycle, DPI, GPU recovery, offline progression and extended management sessions.

The detailed balancing rationale and migration measurements are preserved separately: [Empire balance guide](empire-balancing.md) and [historical implementation log](archive/casino-empire-history.md). See [architecture](architecture.md).
