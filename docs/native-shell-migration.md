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

## Phase B: React casino lobby preview

The NativeShell preview now attempts the existing `lobby.html` React surface on startup and after returning from a game, backed by a read-only snapshot of the **real pilot** wallet, profile, levels, cosmetics, album, challenges and vault. A true `nativePreview` capability flag blocks unavailable shop, account editing, records, daily payouts, updater and arcade rewards rather than displaying fictional financial state. Exact-document browser navigation and click-to-mode mapping continue to be enforced by C#. The WinForms home remains the explicit browser-failure fallback. This is not full lobby parity, since transactional subviews still require native controller adapters and journaled writes.

## Phase B: Midnight Descent process adapter

The preview includes a WPF-free Godot launcher with a strict bundled `MidnightDescent.exe` + `.pck` presence check, the same fullscreen/language arguments as production, and minimized-host restoration after process exit. The CI pilot artifact now includes the exported Godot executable. On machines where the export is absent (such as a local development build), the lobby tile stays unavailable. A headless smoke covers the launch-argument contract; complete real-window focus/audio acceptance is outstanding.

## Phase B: Casino Empire embedded preview bridge

The native preview now links the original Empire protocol/snapshot and Core `EmpireService` instead of introducing a new simulation. Its embedded `empire.html` route handles session/revision-bound commands, read-only previews, authoritative transactional management purchases and single-file owner-dividend/wallet commits. Idle accrual ticks every two seconds while visible, checkpoints at most every thirty seconds and persists on close; production user storage remains untouched. The pilot **does not yet support detached Empire windows**, production save migration or complete performance/audio acceptance. The React lobby exposes Empire only as an embedded primary action, with secondary action disabled.

## Phase B: Chill Lounge native services

The independent NativeShell now links the existing Sudoku and variants, Solitaire, Word Search, Memory, Jigsaw, Tales from the Backroom, Tower Defense and Midnight Sweep C# services and versioned command sessions. The browser may only submit whitelisted intentions. Completion/profile and wallet rewards are protected by a dedicated preview-only aggregate checkpoint (pending before mirror projection, replayed on startup). A failed checkpoint disables subsequent Lounge commands until restart. The separate case-board window is implemented as an independent WinForms WebView2 view sharing the parent C# service. WPF fallback remains in the production app.

## Phase B: native Casino Empire secondary window

The React lobby now exposes the original Empire detached-window action. The native WinForms secondary surface has its own WebView2 session and command receipts, but shares a single authoritative Core Empire economy and preview storage with the main host. It restores focus, uses exact-page origin checks, and releases its timer and browser when closed. A detached window cannot be opened during an unresolved save or active blackjack wager.

## Phase B: native React lobby, account and progression transactions

The isolated pilot now uses the original React records/shop screens for account and title/frame cosmetics, seasons, challenge rewards, session statistics, history, the 16-theme album, shop packs, the Casino Vault and daily-login rewards. The same Core catalogs and progression classes used by WPF remain authoritative. Browser requests cannot supply balances or payouts. The preview's profile/wallet aggregate journal covers combined payouts before replacing either mirror, with restart recovery and a frozen writer after uncertain failure. The multi-game Arcade route reports terminal scores through the established Core receipt validation. The NativeShell can show the updater's status panel, but **must not** download or install updates through the production updater while it remains a test pilot.


## Phase C: isolated host switch rehearsal (2026-11-13)

The Development and Stable installers still launch `BlackjackCasino.Desktop.exe`.
The old updater and its manifest remain unchanged while migration acceptance is
incomplete. This is the **first host-cutover rehearsal**, not a release switch.

The Development CI now runs the native real-WebView2 smoke before publishing and
uploads `blackjack-casino-host-migration-candidate-win-x64` as a separate artifact,
including the Godot Manor Web bonus and Midnight Descent executable. It is not
used by the Development updater.

To rehearse your own progress in the independent host:

1. Fully **exit** the installed casino and its updater. Take a separate backup
   of `%LOCALAPPDATA%\\BlackjackCasino` if you wish to retain an off-device copy.
2. Extract the candidate artifact to a new test directory. From PowerShell
   run `./BlackjackCasino.NativeShell.exe --stage-migration rehearsal-1`.
   This validates and copies every *top-level JSON file*, including the wallet,
   profile, hand history, and any existing Manor checkpoint. The source remains
   untouched. A staged marker records SHA-256 fingerprints. An existing
   rehearsal name is never overwritten.
3. Run `./BlackjackCasino.NativeShell.exe --candidate rehearsal-1`.
   The candidate uses
   `%LOCALAPPDATA%\\BlackjackCasino\\NativeShellPreview\\Candidates\\rehearsal-1`.
   All candidate wagers, purchases and saves remain there, not in live state.
4. Compare the game modes and the imported wallet/profile with the original.
   Do not switch auto-update channels or delete the legacy installation.

Important limitations: JSON copies do not migrate browser-local Arcade high
scores, and the new host currently has no equivalent release updater or full
audio/history/Windows acceptance. This is a **copy-only migration rehearsal**,
not a single-writer production cutover. The candidate manifest explicitly
records these outstanding gates.

The candidate now explicitly loads and persists the imported Blackjack
`hand-history.json` through a path-scoped writer. This prevents empty history
screens after migration and keeps new candidate rounds outside the live WPF
player-data directory.


### Arcade high-score migration
The old WPF WebView2 profile stores the local personal Arcade records in
`%LOCALAPPDATA%\\BlackjackCasino\\WebView2\\Default\\Local Storage\\leveldb`.
The migration staging operation now copies this LevelDB directory byte-for-byte
into the matching isolated candidate browser profile before its first launch.
The original profile is **never opened for writing or modified**. This is a
browser-profile-level migration, not a reconstruction from the C# statistics.

**Important:** Previously staged candidates (for example, `test1`) are never
overwritten. To include these changes and the old Arcade high scores, use a new
candidate name, e.g. `--stage-migration test2` and `--candidate test2`,
after completely closing the old casino and all WebView2 processes.
The copy-only staging procedure rejects changed files, excessive size and
symbolic links. If no legacy localStorage directory exists, it leaves that
component empty rather than inventing scores.

### Phase C: WPF-free NativeShell soundtrack
The native host now links the original PCM synthesis, shared WinMM mixer, and
the user's already licensed MP3 tracks. The candidate package includes
`Music/Licensed/lobby.mp3`, `midnight-dice.mp3`, `midnight-manor.mp3`,
and `chill-lounge.mp3`. Track routing follows the active casino mode.
Music-enabled and global volume preferences are observed; synth fallback
continues when an optional MP3 is unavailable or cannot be opened.
Leaving a game stops its audio owner; launching Midnight Descent suspends
the host music to avoid two soundtracks playing simultaneously.

The host soundtrack is no longer silent. Individual per-action sound cue parity
and the full original soundtrack/volume acceptance still require Windows review.
