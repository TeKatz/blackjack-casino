# Historical WPF inventory — retired at final NativeShell cutover

**Historical note:** All entries below describe the pre-cutover WPF application. On the final cutover branch, the WPF/XAML source tree and its legacy smoke executables have been removed. This document is retained solely for provenance; consult [current architecture](architecture.md) for active components.

**Integrated baseline:** `main`. **Development source:** `develop`. **Historical implementation:** `feature/manor-modernization-phase-1`.
**Scope:** React/WPF dependency separation, obsolete source cleanup and ongoing fallback inventory (modernization phase 4).

## Current status

The ordinary Chill Lounge opens the React hub and all six native-authoritative subgames without registering a WPF game module. \`ModernLoungeCatalog\` provides the exact six IDs in the same order as \`frontend/src/lounge/bridge.ts\`. \`MainWindow.ModernLounge.cs\` no longer calls or reads \`LoungeHostedGameRegistry\`, and invalid browser game IDs cannot fall through to a native UI.

\`OpenLegacyLounge\` owns WPF factories and visual decorators exclusively. The fallback remains invoked when the locally packaged WebView2 page is unavailable or fails. No C# game rules, progressive award ledgers, persistent schemas, save IDs or updater configuration were changed.

The WPF \`StartupWelcomeWindow.cs\` is not part of normal startup, but remains in source because two legacy regression tests exercise its language and visual presentation. The modern React lobby, profile, updates panel and German UI have additional direct browser smoke coverage.

## Conservative reference inventory

The CI tool \`tools/check_ui_migration_boundaries.py\` scans all \`.cs\` and \`.xaml\` sources under src/, tests/ and tools/. It reports direct references to selected native presentation classes and asserts architecture boundaries. **References do not prove that an item must stay forever, and zero direct references do not authorize removal:** XAML markup, reflection, smoke coverage, emergency recovery and explicit rollback builds also matter.

| Remaining UI/adapter family | Current dependency | Disposition |
| --- | --- | --- |
| \`LoungeWindow\`, \`LoungeHostedGameRegistry\` and lounge visual decorators | WebView2 failure/unavailable recovery | Keep until an accepted alternate fallback exists |
| \`SudokuView\`, \`SolitaireView\`, \`WordSearchLoungeModule\`, \`MemoryLoungeModuleVisual\`, \`JigsawPlayView\` | WPF fallback, saves, WPF regression tests | Only initialize on fallback; remove after recovery transition |
| \`AdventureLibraryLoungeModule\`, \`AdventureCaseBoardView\` and old story minigame screens | WPF emergency story flow and investigation screens | Keep until full native alternative or tested recovery is accepted |
| \`ManorWindow\`, \`SlotMachineWindow\` and legacy Dice/Empire views | Emergency or controlled rollback and benchmark tests | Keep until independent per-game signoff |
| \`PlayerProfileWindow\`, \`ProgressionWindow\` and older album editor | Native export/emergency tooling | Keep until export and recovery move |
| \`StartupWelcomeWindow\` | No normal startup route; legacy localization and visual regression references | Retain until its older test coverage is safely superseded |

## Automated gates

- \`ModernLoungeCatalogTests\` verifies the exact six known IDs, status projection and no extra routes.
- \`ModernLoungeSmoke\` opens the real local React WebView2 page and asserts that \`LoungeHostedGameRegistry.Snapshot.Count == 0\`, then exercises the six gameplay routes and their C# game state.
- The repository-hygiene and Windows CI workflows execute \`tools/check_ui_migration_boundaries.py\`; it fails on reintroduced native registration in the React host, route fallback from game selection, or mismatched catalogs.
- Existing WPF fallback smokes remain enabled. This phase does not disable tests to obtain a green CI status.

## Before further deletion

1. Complete real-device Windows 10/11, DPI, keyboard, memory/GPU, crash restart and save compatibility checks for all six React routes, notably high-piece Jigsaw and multiple Tales endings.
2. Decide and implement a crash-safe UI replacement for the full native \`LoungeWindow\` (or explicitly accept the absence of gameplay on machines without WebView2). **Do not delete the only available fallback prematurely.**
3. Convert the remaining WPF-specific smoke assertions to real browser UI assertions or equivalent C# invariants, rather than dropping coverage.
4. Delete each now-unreferenced native UI family in a separate reversible commit, retain Core engines, and verify native tests, published ZIP, updates and rollback.
5. After cleanup, proceed to a smaller, WPF-independent Windows shell (phase 5), followed by device QA (phase 6) and stable-channel readiness (phase 7).

The historical feature-branch separation from `main` ended at integration PR #36. The permanent `develop` source now publishes Development; the existing update manifest URL is unchanged. Stable still needs an explicit release tag.

## Browser recovery decoupling (2026-11-06)

A React Chill Lounge startup/process/navigation failure no longer **automatically** creates the full legacy `LoungeWindow` with its six WPF game module factories. A small, standalone `BrowserRecoveryView` uses the same exclusive foreground surface to offer explicit Retry (when packaged assets exist), Original Games, and Casino Lobby actions. Missing `lounge.html` goes to this independent screen as well. `BrowserRecoverySession` is a WPF-free choice guard that accepts only one authorized action; the native game factory is still invoked **only** when the player explicitly chooses Original Games. The player-state and reward services are untouched.

The original native fallback is intentionally **not deleted**, because it remains the only gameplay path when WebView2 cannot run. The recovery screen still renders within the interim WPF host; it is independent of *legacy WPF game UIs*, not a complete WPF-free application shell. Do not interpret this as permission to remove the old Lounge, its test coverage, or the other WPF game fallbacks yet.

## Shared React game failure recovery (2026-11-07)

The same standalone host recovery screen now covers Blackjack, Midnight Dice, Midnight Reels, Neon Vault, Nine Lives and Midnight Merge in addition to the Chill Lounge. Missing packaged frontend assets and runtime browser crashes offer a safe explicit choice instead of a modal-only dead end. React-only games offer Retry and Lobby. Midnight Reels alone offers an additional **opt-in** old WPF slot machine; `CloseModernReels` no longer launches that native window automatically. The six routes retain their existing C# session/receipt contracts and their specific failure close logic before presenting recovery.

The browser-independent `BrowserRecoverySession` now separately validates whether modern assets exist and whether a compatible legacy game is available. A hidden or unavailable action cannot be invoked even through a synthetic event; Windows STA smoke verifies both cases and host-input restoration. A repository gate prevents silent fallback from returning to the Reels crash branch or other game failures. This is **not** deletion of the old WPF code nor a replacement of `MainWindow`; those remain distinct next milestones.
