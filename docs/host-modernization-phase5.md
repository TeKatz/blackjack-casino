# Phase 5 — desktop host modernization

**Status:** first bounded host-extraction slice implemented on `feature/manor-modernization-phase-1`; the executable still uses .NET 10 WPF with offline WebView2.

## Baseline preserved

The preceding Development build includes the migration presentation repairs documented in [Migration regression repairs](migration-regression-repairs.md), including lobby News, live cosmetic previews, 100-credit archived Season rewards, redesigned vault receipt, card/dice/reel animations, the Jigsaw tray and Tales detached investigation board. This phase does not change React screens or their CSS, the native game controllers, browser protocol revisions, persistent saves, probabilities, reward math or localization.

## Extracted host contract

The original window contained seven almost-identical groups of code that directly:

1. disabled every native child beneath a newly opened React game;
2. set the shared embedded WPF content control to the browser page;
3. made the overlay visible and set cyclic keyboard focus;
4. detached the page on close; and
5. restored the old controls' exact enabled values.

Those responsibilities now belong to `ModernGameSurface`. `MainWindow.EmbeddedGames.cs` exposes the two small methods `MountModernSurface` and `UnmountModernSurface`; all seven game routes call them. The adapter rejects a second page when another modern game or a native fallback owns the surface, and rejects closing a page that is not active. It also restores the old input state after an attachment error.

This split makes further host replacement possible without changing each game-specific controller or WebView2 command handler. The adapter is deliberately WPF-specific during the transition; it is not a new gameplay rules engine.

## Regression gates

- The Windows `ModernGameSurfaceSmoke` constructs real WPF controls in the STA process and verifies exclusive ownership, inability to steal an active native fallback, rejection of a mismatched close, proper page/overlay teardown and restoration of previously disabled buttons. It also verifies a second open/close cycle.
- Existing real Windows StartupSmoke exercises the seven normal destinations through the same shared shell; modern Lounge and its six subgames remain a required navigation/play suite.
- `tools/check_ui_migration_boundaries.py` asserts that each modern game route uses the common surface rather than reintroducing duplicated WPF overlay/coverage code.
- Repository contracts, .NET unit tests, frontend tests, Windows presentation regression and packaged Development smoke remain required before a release.

## Still in the old WPF host

- The main window's frame, custom chrome, updater and preferences.
- The WebView2 initialization, virtual-host mappings, origin checks and per-game bridge handlers. These are still intentionally separate to preserve their strict message contracts.
- Legacy `ShowEmbeddedGame` for old WPF windows and Midnight Manor resource/lifecycle behavior.
- The native emergency lobby and full fallback Lounge; deleting them before real-device recovery sign-off would lose functionality.
- The separate Tales investigation window and its native owner relationship.

## Next host steps

First extract the common WebView2 lifecycle/environment and trust policy in small test-backed changes while preserving per-route allowlists and session/receipt checks. Then replace the oversized frame's responsibilities with focused shell services (window chrome, updater, saved preferences, navigation, audio, foreground view ownership). Only after emergency recovery, detached windows, release-upgrade behavior, Windows 10/11 and 125–200% DPI acceptance can the WPF executable be replaced by a smaller native shell. No merge to `main` or stable-channel rollout is part of this milestone.
