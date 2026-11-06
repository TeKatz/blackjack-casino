# Legacy WPF inventory and safe cleanup boundaries

**Development branch:** \`feature/manor-modernization-phase-1\`  
**Scope:** React/WPF dependency separation, obsolete source cleanup and ongoing fallback inventory (modernization phase 4).

## Current status

The ordinary Chill Lounge opens the React hub and all six native-authoritative subgames without registering a WPF game module. \`ModernLoungeCatalog\` provides the exact six IDs in the same order as \`frontend/src/lounge/bridge.ts\`. \`MainWindow.ModernLounge.cs\` no longer calls or reads \`LoungeHostedGameRegistry\`, and invalid browser game IDs cannot fall through to a native UI.

\`OpenLegacyLounge\` owns WPF factories and visual decorators exclusively. The fallback remains invoked when the locally packaged WebView2 page is unavailable or fails. No C# game rules, progressive award ledgers, persistent schemas, save IDs or updater configuration were changed.

The WPF \`StartupWelcomeWindow.cs\` was fully removed because it had no product route; it was only constructed by two obsolete reflection-based smokes. Those tests were retired, while real React lobby, profile, update panel and German locale smoke coverage remains.

## Conservative reference inventory

The CI tool \`tools/check_ui_migration_boundaries.py\` scans all \`.cs\` and \`.xaml\` sources under src/, tests/ and tools/. It reports direct references to selected native presentation classes and asserts architecture boundaries. **References do not prove that an item must stay forever, and zero direct references do not authorize removal:** XAML markup, reflection, smoke coverage, emergency recovery and explicit rollback builds also matter.

| Remaining UI/adapter family | Current dependency | Disposition |
| --- | --- | --- |
| \`LoungeWindow\`, \`LoungeHostedGameRegistry\` and lounge visual decorators | WebView2 failure/unavailable recovery | Keep until an accepted alternate fallback exists |
| \`SudokuView\`, \`SolitaireView\`, \`WordSearchLoungeModule\`, \`MemoryLoungeModuleVisual\`, \`JigsawPlayView\` | WPF fallback, saves, WPF regression tests | Only initialize on fallback; remove after recovery transition |
| \`AdventureLibraryLoungeModule\`, \`AdventureCaseBoardView\` and old story minigame screens | WPF emergency story flow and investigation screens | Keep until full native alternative or tested recovery is accepted |
| \`ManorWindow\`, \`SlotMachineWindow\` and legacy Dice/Empire views | Emergency or controlled rollback and benchmark tests | Keep until independent per-game signoff |
| \`PlayerProfileWindow\`, \`ProgressionWindow\` and older album editor | Native export/emergency tooling | Keep until export and recovery move |
| \`StartupWelcomeWindow\` | No real product path | Removed together with reflection-only tests |

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

The feature branch remains separate from \`main\` and Stable. The Development publisher and update-channel guard remain unchanged.
