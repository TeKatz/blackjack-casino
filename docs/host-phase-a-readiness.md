# Host migration Phase A — parity inventory and protective boundaries

**Status:** Phase A infrastructure implemented on `dev`; **not a production host switch**.
The existing `BlackjackCasino.Desktop.exe` remains the Development/Stable application.
The `BlackjackCasino.NativeShell.exe` stays a separately published test pilot.

## Single source of truth

`src/BlackjackCasino.Core/Hosting/HostMigrationContracts.cs` defines:
- All **11** existing top-level lobby routes, in the real WPF registry order.
- Exact route aliases and local HTML pages for the **7** NativeShell preview bridges.
- All **8** Chill Lounge subgames, matching `ModernLoungeCatalog.GameIds`.
- Explicit, non-approved release criteria; preview boot success never implies parity.
- A host-neutral `IHostModeNavigator` port implemented by `CasinoModeRegistry`.
- `HostPreviewStorage`, which prevents preview writes to the production user-data
  directory (or sibling folders within that tree).

`tools/check_host_phase_a.py` compares the manifest with the source registry,
the actual preview launcher, frontend HTML documents, Lounge catalog, WPF flags,
the storage guard, and current release targets. This guard runs on CI and repository
hygiene. xUnit covers route mapping, negative allowlist cases, release-state and
the production/preview path boundary.

## Current capability parity

| Production lobby route | NativeShell alias | Pilot status | Still required for production |
| --- | --- | --- | --- |
| `blackjack` | `blackjack` | React bridge in pilot | Complete profile/history/account parity |
| `roulette` | — | Missing | Full native roulette bridge and acceptance |
| `midnight-reels` | `reels` | React bridge in pilot | Recovery, music, user data and lifecycle parity |
| `neon-vault` | `neon-vault` | React bridge in pilot | Full bonus/reward restart and persistence QA |
| `nine-lives` | `nine-lives` | React bridge in pilot | Full bonus/reward restart and persistence QA |
| `midnight-manor` | `midnight-manor` | Journal-backed React bridge in pilot | Detached window, 3D bonus, animation/audio and crash QA |
| `casino-empire` | — | Missing | Management screens, offline income, detached window |
| `midnight-merge` | `midnight-merge` | React bridge in pilot | Full persistent orders/rooms and replay acceptance |
| `chill-lounge` | — | Missing | React hub + all eight subgames and Tales board |
| `kniffel` | `midnight-dice` | React bridge in pilot | Multi-AI tables, cross-version saves and sound |
| `midnight-descent` | — | Missing | External Godot launch and fullscreen return/focus |

The Lounge subgames are `sudoku`, `solitaire`, `word-search`, `memory`,
`jigsaw`, `tales-backroom`, `tower-defense`, and `minesweeper`.
Having an HTML document does not imply that its C# bridge, persistence,
audio, recovery or browser security is implemented in NativeShell.

## Important behavior boundaries

1. **Player data:** NativeShell continues to use a separate preview tree in
   `%LOCALAPPDATA%\BlackjackCasino\NativeShellPreview`; it does not import
   `player-state.json`, `profile.json`, `hand-history.json`, or existing
   journal data from production. The guard rejects the production root and
   arbitrary child paths inside it. Independently allocated temporary test
   directories remain supported.
2. **Transactional rules:** `PlayerStateStore`, progression, the versioned
   browser command receipts, and `ManorPersistence` retain ownership of real
   state. Phase A does not alter wager/award algorithms, save schemas or IDs.
   Production import needs a dedicated atomic migration and single-writer gate.
3. **Browser:** Each mode retains its exact-document origin validation,
   protocol epoch/revision verification, and no silent paid-command replay.
   The manifest selects only a known local page; it never broadens origins.
4. **Recovery:** `BrowserRecoverySession` is host-neutral and single-choice;
   the WPF emergency fallback stays available. NativeShell needs complete
   feature-specific recovery before replacing it.
5. **Release:** Neither Development nor Stable may publish the pilot until
   each `HostReleaseRequirement` has been verified with evidence, and the old
   WPF smoke coverage has been replaced by equivalent modern runtime checks.

## Next engineering work (Phase B), in order

1. Complete full React lobby, records, profile, album, season, shop, Vault and
   update commands in the native host without creating a separate progression
   engine. Preserve `CasinoModeRegistry` primary/secondary action semantics.
2. Add roulette, Empire (including detached mode), Lounge + eight subgames,
   the Tales investigation window, and Midnight Descent process lifecycle.
3. Complete audio, native fullscreen/multi-monitor/DPI/focus, browser recovery
   and the packaged Manor 3D bonus lifecycle.
4. Implement **read-only** production-data inventory and a verified migration
   plan first; introduce any writes only behind backup, exclusive writer
   ownership, journal recovery and rollback tests.
5. Make old and new hosts pass the same Windows 10/11 gameplay, save, crash,
   accessibility, updater and packaged-release acceptance suite.

## Verification

- `python tools/check_host_phase_a.py`
- `dotnet test tests/BlackjackCasino.Tests/BlackjackCasino.Tests.csproj --filter FullyQualifiedName~HostMigrationContractsTests`
- `dotnet run --project src/BlackjackCasino.NativeShell -- --smoke`
- `dotnet run --project src/BlackjackCasino.NativeShell -- --web-smoke` (Windows/WebView2)

Windows integration and release approval are **not** asserted by source checks alone.
