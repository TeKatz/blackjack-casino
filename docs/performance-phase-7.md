# Performance Phase 7 — Windowed rendering, Casino Empire, and Season Pass loading

**Historical performance-phase record.** This page documents a specific optimization and its original measurement scope. It is not a guarantee of current FPS or GPU/CPU use. Consult [current presentation validation](presentation-regression.md) and the [quality roadmap](modernization-roadmap.md).

## Scope

Phase 7 addresses a reported regression that was strongly mode-specific: the desktop client remained smooth in fullscreen, while windowed mode became noticeably less responsive after the standard Windows frame was replaced with the custom casino frame. The same report also identified smaller remaining slowdowns in Casino Empire and while opening the Season Pass.

No save schema, progression economy, reward definitions, game rules, or content data are changed in this phase.

## 1. Main-window custom frame without WPF WindowChrome

The custom-frame implementation used `WindowStyle=None` together with `System.Windows.Shell.WindowChrome` for ordinary windows. The fullscreen path explicitly removed that `WindowChrome`, making it the key rendering-path difference between the smooth fullscreen mode and the slower windowed mode.

The main casino window now keeps the same custom visual template and title bar while avoiding WPF `WindowChrome` entirely. A lightweight client-area frame helper preserves normal desktop behavior through native window messages:

- resize hit testing for all edges and corners,
- title-bar dragging,
- double-click maximize/restore,
- right-click system menu,
- monitor work-area aware maximize sizing so the taskbar is respected.

The change is intentionally limited to `MainWindow`. Other normal dialogs retain their existing WPF `WindowChrome` implementation so this performance fix does not unnecessarily broaden frame behavior changes.

Fullscreen behavior remains unchanged: it still has no WPF `WindowChrome`.

## 2. Casino Empire ambient redraw budget

Casino Empire room artwork used a 40 ms dispatcher timer (25 Hz). Each tick advanced decorative animation state for every visible room artwork and invalidated its custom drawing surface, even when the underlying simulation had no gameplay state to update.

The decorative ambient interval is now 80 ms (12.5 Hz). This halves the scheduled custom-art redraw pressure while retaining visible ambient motion. Production, simulation, purchases, wallet state, rewards, persistence, and gameplay timing are unchanged.

## 3. Season Pass duplicate reward-track construction

The Season Pass had two synchronous rendering paths active during first materialization:

1. the legacy generic progression-tab `RefreshSeason()` path built the full tier list, then
2. the newer Season Pass library immediately cleared and rebuilt the configured reward track again.

The current pass contains 120 tiers, so opening the tab could create the same large WPF tree twice in immediate succession.

The content lifecycle now treats the Season Pass library as the owner of Season Pass rendering. The legacy generic renderer is skipped for that tab, while all other progression tabs retain their existing lazy-render behavior. Opening the Season Pass therefore materializes its reward track once instead of twice.

Season XP, tier requirements, reward claims, pass activation/switching, and reward definitions are untouched.

## Validation

Phase 7 is covered by the existing warnings-as-errors build, unit tests, shared-control contracts, table wager regression, Windows Casino presentation runtime regression, desktop startup smoke, Manor runtime smoke, and the separate Windows hover/layout presentation workflow.

The most important remaining validation is real-device observation of windowed rendering smoothness. CI can verify correctness and presentation behavior, but it cannot reproduce every target GPU/driver/Desktop Window Manager composition path or provide a trustworthy before/after FPS measurement for that machine.
