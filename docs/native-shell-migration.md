# WPF-free native shell: implementation and parity gate

**Integrated baseline:** `main`; **Development source:** `dev` (historical implementation: `feature/manor-modernization-phase-1`)  
**Status:** Independent WinForms/WebView2 executable with genuine native Blackjack, Roulette, Midnight Reels, Neon Vault, Nine Lives, Midnight Merge, Midnight Dice and Midnight Manor game bridges. **Preview, not release replacement.** The production installer and auto-updater still package `BlackjackCasino.Desktop.exe` (WPF).

## What is already functional without WPF

- A separate `BlackjackCasino.NativeShell.exe` compiled with `<UseWindowsForms>true</UseWindowsForms>` and `<UseWPF>false</UseWPF>`. No XAML or PresentationFramework references.
- A single offline WebView2 game surface serving the *existing* local `blackjack.html` and `reels.html` pages, with their original React visuals and native Core rules. The shell accepts the exact versioned, GUID/revision-validated protocols; the authoritative Blackjack hand, SlotMachineSession, paid spin, bankroll and progression awards still come from C#.
- Strict exact-document origin validation; no remote navigation, popups, host objects, browser devtools or permissions. The WebView2 user profile is always outside the installation directory.
- Browser init/readiness watchdog, retry/lobby recovery without automatic wagers, and fresh browser command sessions. Reels recovery retains native game result state.
- Source-linked preferences, profile, progression, receipt and save contracts from the actual desktop application, not a newly invented game-engine implementation.
- A **separate preview save directory** under `%LOCALAPPDATA%\BlackjackCasino\NativeShellPreview`. This pilot never writes over production `player-state.json`, `profile.json` or history. A clean profile and bankroll are expected on first run; this is deliberately not a production-state migration.
- Windows CI builds the project, runs the headless protocol, replay and isolated-storage smoke, publishes a *separate* native-shell pilot artifact, and checks that all seven React documents are bundled. The Development updater's WPF executable, channel, manifest, user saves and release package remain unchanged.

## Run the independent shell

Build the frontend once with `npm ci && npm run build` in `frontend/`, then from the repository root:

```powershell
dotnet run --project src/BlackjackCasino.NativeShell/BlackjackCasino.NativeShell.csproj --configuration Release
```

Run a headless architecture/receipt check with:

```powershell
dotnet run --project src/BlackjackCasino.NativeShell/BlackjackCasino.NativeShell.csproj --configuration Release -- --smoke
```

For packaged binary, use the CI artifact **blackjack-casino-native-shell-preview-win-x64**. The pilot must not be swapped in for the production executable or used with the old updater.

## Not yet migrated — release blockers

| Area | Required before removing WPF |
| --- | --- |
| Full React Casino Lobby | Exact account/shop/vault/album/updates/history/season command bridge and all nine navigation routes, including secondary Empire window |
| Chill Lounge | Six native-authoritative React game services and Tales detached investigation board in the independent host |
| Dice, Neon Vault, Nine Lives, Midnight Merge | Their retained native services, transactions, audio signals and browser bridges |
| Midnight Manor | Full native game controller and browser session in the new shell, including separate-window lifecycle |
| Progression/account | Production profile import, durable history, cosmetics, season rewards, claim idempotency, daily login |
| System lifecycle | Real user migration without double writers, updater/rollback, first-run and failure recovery, DPI/multi-monitor/keyboard/focus, audio |
| Release gate | Windows 10/11 runtime parity; update, rollback, restart and crash tests; all old WPF smoke tests replaced by equivalent real WebView2 tests |

A successful CI build of this pilot **does not prove** full migration. Do not set the Development/stable publisher to it, delete `BlackjackCasino.Desktop`, disable WPF smokes or claim WPF has been removed. Only switch the release target after every row is implemented and the old/new host runs pass the same functional acceptance suite.

## Independent real-WebView2 acceptance

The separate Windows CI `--web-smoke` mode now opens all seven native-host routes through the real WinForms WebView2 control and the packaged React pages, requires the UI-ready handshake and a rendered document, then verifies lobby return. Each run uses its own temporary browser profile and player state. The existing headless `--smoke` additionally validates original EN/DE Merge resource lookup, native game actions, spin/order receipts and rejection of replay. This tests host startup and native command bridging; it does **not** replace complete game interaction, player migration, audio, update or screen-reader acceptance.

## Midnight Dice native service (2026-11-07)

The native pilot now also hosts `midnight-dice.html`. The reusable `ModernDiceSnapshot` no longer depends on WPF `KniffelView`, `DiceAppearance` or `Localization`: category names and cosmetic palette tokens are shared with the old WPF view as pure C# data. The WinForms host reuses Core `KniffelGame`, `KniffelTable` and `KniffelCash` to support casual sheets and paid casino tables. A complete candidate dice checkpoint and wallet change are written before changing live state. Opponent turns use Core AI away from the UI thread; a native receipt revision is bumped only after the turn has been durably committed. The existing profile progression service remains authoritative.

This is still **preview-only**. Full production replay/save compatibility, browser fault injection on every game, multiple AI table cycles, standalone detached windows and audio parity are not yet proven. The original WPF release is not replaced.

## Midnight Manor journal-backed route (2026-11-07)

The native pilot now hosts `index.html` via the existing strict V2 `ManorPreviewProtocol`, source-linked `ManorController` and `ManorModernGameService`. The wallet, profile, room decisions and optimistic revision token are committed together in the same write-ahead `ManorPersistence` journal that the existing WPF application uses. The journal is scoped exclusively to the pilot save directory. On launch, a pending journal is recovered **before** either player mirror is loaded. On uncertain commit errors, new game transactions are blocked until the preview restarts; stale normal saves cannot overwrite an authoritative unresolved journal. A headless test checks replay IDs and a real React/WebView2 test confirms Manor's `ui.ready` handshake.

The view in the pilot is embedded in its new WinForms shell rather than a separate WPF Window. Detached-window parity and all Manor animations/sounds are still subject to the full UI acceptance suite before release cutover.

## Phase A host parity protection

The current canonical source-level parity matrix, preview route allowlist, host-neutral navigation port, preview storage boundary and CI gates are documented in [Host migration Phase A](host-phase-a-readiness.md). This does not authorize replacing the WPF release application.
