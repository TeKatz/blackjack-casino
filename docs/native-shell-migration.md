# NativeShell migration: final host cutover and historical checkpoints

**Integrated baseline:** `main`; **Development source:** `dev` (historical implementation: `feature/manor-modernization-phase-1`)  
**Current status (final cutover branch, 2026-11-14):** `BlackjackCasino.NativeShell.exe` is the direct Development package host. WPF/XAML sources and WPF-only runtime tests are retired. Stable changes remain subject to an explicit reviewed tag; they are not automatically published. The chronological phase entries below describe **historical checkpoints**, not the current host state.

## Final WPF-free cutover (2026-11-14)

- Production-style launch without arguments starts NativeShell directly. The already-used Development native profile is reused before considering any legacy save import.
- `BlackjackCasino.Desktop.exe` in the release ZIP is a tiny **WPF-free WinForms compatibility launcher** for old updaters and shortcuts. It only starts NativeShell. The new `UpdaterLauncher` restarts NativeShell directly.
- New WinForms browser-recovery routes replace legacy UI fallbacks; no WPF assemblies, XAML, old window code or WPF smoke projects are included.
- 72 shared C# files, 92 original assets and 26 resource files were moved out of the retired desktop project without changing game mechanics, receipt IDs or save schemas.
- The existing immutable original legacy saves remain available; failure to safely import blocks startup without opening another writer. Rehearsal and return-export tools remain available as offline safety workflows.
- Development and Stable packaging scripts now publish NativeShell, the compatibility shim, updater, frontend, audio and Godot assets. Stable still requires explicit tagged signoff.
- Real WinForms/WebView2 and domain tests are mandatory. Windows 10/11 hardware, audio, DPI, crash/restore, extended play and accessibility signoff are separate from code-level CI.

### Historical migration checkpoints

The following sections record the staged approach and constraints **at the time
of each phase**. Earlier claims that NativeShell was only a preview or that WPF
was still the Development release target were superseded by this final cutover.

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

### Native synthesized sound effects
The separate host now routes successful gameplay receipts and browser
presentation events to the **original** WPF-independent PCM cue generators:
SlotAudio, NineLivesAudio, CasinoSoundSynthesis (Dice), MergeSoundSynthesis,
MemorySoundSynthesis and WordSearchSoundSynthesis. The listener validates the
local WebView2 origin and authoritative command receipt before playing
transaction-dependent cues; reel-stop effects remain presentation-only.
The existing independent browser effects (Blackjack, Roulette, Manor, Arcade,
album packs) are not duplicated. The user's per-area sound setting is honored.
Visual/audio mix and timing parity still require manual Windows testing.

## Phase D: reversible dual-host package and offline rollback rehearsal (2026-11-13)

The existing Development and stable updater payloads continue to package and restart
`BlackjackCasino.Desktop.exe`. CI now also creates an **isolated, rehearsal-only**
artifact with both published executables. The rehearsal verifies overlapping WPF
and NativeShell assets by SHA-256, includes the existing updater and Godot game,
and emits a checksum and an explicit `productionApproved=false` manifest.

New disposable installer regression tests cover upgrading a WPF-only install to
a dual-host install, selecting each restart executable, restoring the WPF-only
package, rejecting a native-only payload from the legacy restart contract,
and rolling back a deliberately failed in-place replacement of both binaries.
Player JSON and browser records are fingerprinted and must remain unchanged.

The rehearsal ZIP is **never** copied to `artifacts/app` or the mutable Development
GitHub release. It is a separate Actions artifact, not a supported distribution
channel. A production cutover still requires an explicit launch-selection
contract, safe production-data promotion with one writer, cross-version save
rollback, real updated-client restart coverage, and Windows 10/11 device testing.

### Read-only NativeShell update inspection
The isolated native candidate now reuses the existing WPF-independent
`DesktopUpdateService` and `UpdateClient` to check real published stable or
Development manifests on demand. Channel selection is retained in isolated
preferences. The UI explicitly disables installation and shows that the
published updater package belongs to the installed WPF host. No download,
relaunch, or modification of the user's installed application is permitted.
Cancellation on window close avoids a late WebView2 update of a disposed form.

## Phase D: explicit opt-in launch chooser and per-profile writer lock

The dual-host rehearsal now bundles `BlackjackCasino.HostCutoverLauncher.exe`.
It offers the existing WPF host as the default and offers NativeShell only for
previously staged, validated candidate profiles. NativeShell receives the
`--candidate NAME` argument; the chooser never promotes a copied profile to
production and does not modify installation defaults or the update channel.

Command-line rehearsal options: `--legacy` and `--native test2`. These require
the same validation as the graphical selector, including a matching staged
manifest and both protected player-save files.

Every NativeShell runtime now holds an exclusive OS file lock in its isolated
save directory for its entire lifetime, preventing concurrent sessions from
writing the same wallet, history or Manor checkpoints. Closing the process
or a crash releases the lock without changing the original installed saves.

## Phase E — two-sided production-data checkpoint (2026-11-13)

The new production-cutover preparation commands operate **only on the existing
legacy data and isolated native candidate**, never by assigning NativeShell a
production write path. They do not change the installed executable or updater:

```powershell
.\BlackjackCasino.NativeShell.exe --prepare-cutover test2 before-switch
.\BlackjackCasino.NativeShell.exe --verify-cutover before-switch
.\BlackjackCasino.NativeShell.exe --rehearse-rollback before-switch legacy C:\CasinoRollbackRehearsal
```

Close **all** running casino processes and updater before preparation. A
NativeShell candidate with an active writer lock, a pending Manor/roulette/player
aggregate journal, or a corrupt wallet/profile cannot pass the checkpoint
gate. Most importantly, if the WPF data changed since the original candidate
import, preparation refuses to merge **divergent wallets or high scores**:
stage a **new candidate** instead. A candidate imported before browser-storage
manifest v2 must also be re-staged.

A verified checkpoint is stored at
`%LOCALAPPDATA%\BlackjackCasino\HostCutoverBackups\BACKUP_NAME`, preserving
both the WPF-side snapshot and independent NativeShell candidate snapshot.
Top-level JSON and WebView2 localStorage LevelDB data are captured with SHA-256
evidence, and neither checkpoint names nor rollback destinations are overwritten.
The rollback rehearsal restores to a **new external folder**, NEVER into the
installed game. A backup stored on the same drive is not a substitute for a
separate offline backup.

**Still blocked:** production activation, dual-write/automatic save merging,
updater restart switch, and overwrite-based rollback. These remain separate
release gates; creating a checkpoint is not authorization to activate the new
host against live data.

### Production writer exclusivity (2026-11-13)

The installed WPF host acquires an OS-enforced, exclusive
`%LOCALAPPDATA%\BlackjackCasino\.casino-host-writer.lock` for its entire
normal session, releasing it **after** all deferred player saves have been
flushed. A second WPF instance and any migration attempt are blocked while
that lease is held. The migration snapshot and two-sided cutover checkpoint
acquire the identical lease for the full read/hash/copy/publish transaction,
so a newly launched WPF instance cannot modify the source halfway through.

The separate NativeShell test profile continues to use its independent
`.native-host-writer.lock`, so the existing isolated preview workflow is
unchanged. An abandoned lock file after a crash is safe; only an open OS file
handle prevents acquisition. Cross-host single-writer enforcement is now
tested by both the .NET tests and the Windows native migration smoke.

Production activation is **still locked** pending a current-state-compatible
rollback and an explicit new-host release gate.

### Promotion rehearsal without changing production (2026-11-13)

A verified cutover checkpoint now supports a **separate writable handover
workspace**. It represents what the new host would load after cutover, while
the installed WPF profile and the original NativeShell candidate remain intact:

```powershell
.\BlackjackCasino.NativeShell.exe --stage-live-rehearsal before-switch live-test
.\BlackjackCasino.NativeShell.exe --live-rehearsal live-test
```

Preparation requires matching historical WPF and candidate SHA-256 snapshots,
the production root lease, the candidate writer lease and no unresolved game
journals. It copies the immutable candidate backup to a fresh directory under
`%LOCALAPPDATA%\BlackjackCasino\NativeShellPreview\CutoverWorkspaces\live-test`,
publishing only after all files have been verified. Subsequent gameplay writes
are isolated to that workspace, and its provenance marker is checked before
launch.

This deliberately does **not** make the NativeShell the default executable,
activate production storage or provide live rollback. Those remain separate
approval gates; the WPF production save and Development updater are unchanged.

## Phase F — NativeShell-to-WPF return export (2026-11-13)

The first offline cross-host return now exports **current NativeShell progress**
to a fresh, verifiable WPF-readable save folder without writing into the live
casino's production storage. After closing every host and updater:

```powershell
.\BlackjackCasino.NativeShell.exe --stage-legacy-return live-test return1
.\BlackjackCasino.NativeShell.exe --verify-legacy-return return1
```

The export contains the current player wallet and preferences, player profile,
hand history, Manor checkpoint and WebView2 localStorage LevelDB (including
Arcade records). The export reuses the established source hash/checkpoint chain,
blocks divergent WPF edits, waits for exclusive WPF and native writer leases,
and rejects unresolved native transaction journals. It validates the payload
through the WPF player-state, hand-history and Manor persistence readers.
Its manifest records the before/after wallet and SHA-256 hash of every copied
file. Existing exports cannot be overwritten, and tampered exports fail verify.

Native-only aggregate and roulette receipt journals remain in the native
workspace and are **not** injected into WPF, which cannot replay them.
Releasing a real two-way switch additionally requires a tested receipt
handoff/replay policy and crash-safe atomic activation. For now these commands
are an **offline proof of compatibility**, not a live rollback operation.
`BlackjackCasino.Desktop.exe`, the installed player saves and the updater
continue to operate unchanged.

## Phase G — journaled WPF return rehearsal and receipt blockade (2026-11-13)

The NativeShell now supports a **sandbox-only, journaled directory switch**
between the original WPF player files and the latest verified WPF-compatible
NativeShell export:

```powershell
.\BlackjackCasino.NativeShell.exe --rehearse-wpf-return return1 sandbox-return
```

Both data sets are copied and SHA-256-verified first. The test then renames the
original player directory to `previous` and replaces `current` with the new
export. A durable journal records whether the directory transition completed.
Automated Windows tests simulate crashes **after either rename**, then invoke
the same recovery logic. Recovery restores the original WPF files while
preserving the interrupted NativeShell contents separately for investigation.
The operation is confined to
`NativeShellPreview\LegacyRollbackRehearsals\NAME`: neither the installed
WPF profile nor the Development updater is changed.

**Native roulette receipts are a blocking compatibility gate.** The NativeShell
retains settlement request IDs in `native-roulette-checkpoint.json`, whereas
the old WPF runtime has no equivalent receipt ledger. Until continuity is
implemented, a rollback rehearsal with any retained native roulette receipts
is refused, even if the wallet itself is valid. This prevents treating an
unverifiable replay state as a safe production swap.

A previously interrupted sandbox can be recovered explicitly with
`--recover-wpf-rehearsal NAME`. This is not a command to recover or overwrite
the production WPF save. A real production cutover remains disabled until
receipt compatibility, startup crash-recovery gating and final Windows QA
are completed.

## Phase H — shared WPF/NativeShell roulette receipt journal

The legacy WPF client now **compiles the same WPF-independent roulette
write-ahead-log implementation** as NativeShell, without referencing the native
executable. Both hosts read and write the existing schema-v1
`native-roulette-checkpoint.json`, retain all successful roulette request IDs
across restarts, and complete an interrupted pending wallet mirror **before**
the WPF MainWindow opens. A WPF roulette round is durably recorded before
the UI returns success; uncertain writes close the old host instead of
accepting more bets. The old WPF in-memory receipt cache remains only for
isolated synthetic test sessions.

Native-to-WPF return exports now include the full original roulette journal
and verify it with the WPF reader. The sandbox rollback refuses an old export
that does not contain newly created roulette receipts, but permits an up-to-date,
verified export carrying the intact ledger. Regression tests check existing
v1 receipt compatibility, cross-host duplicate request rejection, pending
recovery and corrupted/duplicate-ID refusal.

This resolves the specific roulette receipt gap. It is **not** a production
host cutover: other native transaction/journal compatibility and crash-safe
production activation remain unapproved. The installed WPF launcher and
Development update channel are unchanged.

## Development host switch (2026-11-13)

The **Development update package** now includes the previously separate
`BlackjackCasino.NativeShell.exe` together with its WPF fallback, bundled
updater, the existing Host Cutover launcher and all required frontend/Godot
assets. Overlapping published native and WPF files must be byte-for-byte equal,
and both executables must have the exact same build version.

Only the Development package contains `native-development-default.flag`.
Launching `BlackjackCasino.Desktop.exe` in this package delegates to
`BlackjackCasino.NativeShell.exe --development` *before* WPF opens the player
state. First-run NativeShell copies the existing WPF JSON and Arcade WebView2
localStorage into an isolated persistent `Candidates/development` profile and
continues using that profile on subsequent launches. WPF data is never
overwritten. For a fresh installation without WPF saves, NativeShell uses its
own new profile. If migration is unsafe the old WPF host is reopened with
`--legacy`. The user can also launch `BlackjackCasino.Desktop.exe --legacy`
manually as an explicit fallback; this does not merge divergent progress.

The managed NativeShell can check, download and SHA-256-verify Development
updates and invoke the existing separate updater. Updater restarts the
Development Desktop executable, which again delegates to NativeShell.
Stable builds keep their original WPF executable and updater behavior and
never receive the Development marker. Isolated `--candidate` and other
preview commands still cannot install updates.

**Important:** The two independent save roots make this a Development
cutover, not bidirectional real-time synchronization. Switching manually
back to WPF does not copy later NativeShell earnings into the old profile.
The verified return-export and rollback path should be used before a
permanent downgrade. Windows 10/11 manual QA remains necessary before
removing WPF or allowing the Stable channel to run NativeShell.
