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

## Shared offline WebView2 startup (second extraction)

`ModernWebViewBootstrap` now initializes the browser and enforces the same restrictive local-document host policy for ten views: lobby, six primary embedded games (Blackjack, Dice, Reels, Merge, Neon Vault, Nine Lives), the Chill Lounge, Empire, and Manor. It disables context menus, developer tools, host objects, status bar and zoom controls; enables only the existing validated browser-message channel; maps the packaged `ModernFrontend` directory with `DenyCors`; and rejects untrusted document navigation, pop-ups and permission requests.

The helper accepts the *existing per-view trusted page predicate*. There is no new permissive origin list: `lobby.html` cannot navigate to `lounge.html` simply because both reside on the virtual host. The caller still owns WebView2 navigation failures, process restarts/crashes, page-specific bridge parsing, watchdogs, session/revision validation and exactly-once native receipts. If a view was closed before initialization completed, the helper returns without attaching handlers or navigating the browser.

The Windows StartupSmoke now checks hardened settings on initialized, packaged Lobby and Lounge WebView2 instances. CI also audits all ten view classes to prevent duplicated or bypassed setup logic. Browser-origin/security behavior remains covered by the existing protocol tests and packaged runtime tests.

## Still in the old WPF host

- The main window's frame, custom chrome, updater and preferences.
- Shared browser startup and security are centralized. Per-game origin predicates, navigation/process-failure recovery, watchdogs and validated command/receipt bridges intentionally remain separate.
- Legacy `ShowEmbeddedGame` for old WPF windows and Midnight Manor resource/lifecycle behavior.
- The native emergency lobby and full fallback Lounge; deleting them before real-device recovery sign-off would lose functionality.
- The separate Tales investigation window and its native owner relationship.

## Next host steps

Next continue separating the oversized frame's responsibilities into focused shell services (window chrome, updater, saved preferences, navigation, audio, foreground view ownership). Only after emergency recovery, detached windows, release-upgrade behavior, Windows 10/11 and 125–200% DPI acceptance can the WPF executable be replaced by a smaller native shell. No merge to `main` or stable-channel rollout is part of this milestone.

## Third extraction — updater and window placement

The desktop's update transport now lives in `DesktopUpdateService`, without WPF references. It retains the same Stable/Development manifest URLs, channel verification, 20-second HTTP timeout, minimum-updater check, verified package downloader, and user-local staging directory. `MainWindow` still owns UI messages, busy/selection state, the exact pending manifest, explicit updater launch and application shutdown; no installer or update recovery semantics were changed. Unit regression tests check pinned channel URLs, minimum-updater parsing and transport request/channel validation.

The fullscreen transition and persisted restore-bounds computation now live in `CasinoWindowGeometry`. `MainWindow` delegates the existing F11 command and the window dimensions saved into `player-state.json`; the original WPF frame styling, DPI behavior, fullscreen restore sequence and preference schema remain unchanged. A real Windows startup smoke tests the adapter's enter/exit behavior and its saved window placement while fullscreen.

**Boundary:** This does not remove WPF from the executable. Navigation, audio orchestration, legacy recovery windows and the final shell swap are still separate work. No game engines, React/CSS, saves or updater binaries were changed in this extraction.
