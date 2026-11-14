# Architecture

**Current native architecture (November 2026).** The Windows casino runs directly as
`BlackjackCasino.NativeShell.exe` (.NET 10 WinForms + offline WebView2), with no
WPF, XAML or PresentationFramework references in the production build.

## Layers and ownership

| Layer | Source | Responsibility |
| --- | --- | --- |
| Native Windows host | `src/BlackjackCasino.NativeShell/` | Real window, F11/focus, lifecycle, exact-origin WebView2, navigation, native emergency UI, separate Empire and Tales windows |
| Authoritative C# games | `src/BlackjackCasino.Core/` | RNG, wagers, settlement, completion, progression and game catalogs |
| Shared host sources | `src/BlackjackCasino.HostShared/` | Original save formats, preference/profile storage, validated command protocols, receipts and sound synthesis; linked as source into NativeShell and unit tests |
| React/PixiJS UI | `frontend/` | The packaged local lobby, casino machines, Lounge activities, Arcade, Album and account screens |
| Audio and artwork | `src/BlackjackCasino.Assets/` | Offline graphics, music, licensed material, icons, localization provenance |
| Update protocol | `src/BlackjackCasino.Update/` | Version/channel/manifest parsing and SHA-256 verification |
| External updater | `src/BlackjackCasino.Updater/` | Atomic staged replacement, rollback, restart of NativeShell |
| Compatibility name | `src/BlackjackCasino.CompatLauncher/` | **WPF-free** `BlackjackCasino.Desktop.exe` that starts NativeShell for older installed shortcuts/update helpers |

## Game authority and browser trust

All wagers, spin results, level rewards, profile changes, shop purchases,
Roulette receipts, Blackjack settlements, Empire transactions, Manor checkpoints
and Lounge completions are resolved in C# before the browser is updated.
React/PixiJS and local Godot display settled state; they do not determine balances.
Each WebView2 page is loaded from local packaged assets and uses restricted,
versioned command protocols with exact page-origin checks, session/revision tokens
and idempotent request receipts. External navigation, popups, browser privileges
and WebView2 host objects are denied.

On a browser fault, the WinForms recovery surface provides retry/lobby or a
native game route where safe. There is **no WPF game fallback**.

## Saves and migration

Save and browser data are outside the installation folder under
`%LOCALAPPDATA%\BlackjackCasino`.
The existing Development NativeShell profile
`NativeShellPreview\Candidates\development` remains authoritative when present:
it must **never** be overwritten by a fresh copy of older legacy/WPF saves.
On first native launch without such a profile, the verified source importer stages
existing `player-state.json`, `profile.json`, `hand-history.json`, Manor
checkpoints and Arcade WebView2 localStorage. Fresh players use the existing
NativeShell default directory for compatibility.

The native runtime takes an OS-exclusive file writer lock; journals recover
interrupted receipt/payout state before gameplay. Invalid or incomplete imports
block the new host without starting the retired WPF client or destroying data.
Historical offline rollback/rehearsal commands remain available but are
**not** permission for concurrent native/legacy writers.

## Distribution

Development publishes the WPF-free NativeShell directly from `dev`.
Stable is explicitly tagged from reviewed `main`; it is not published by
merging Development. Both packages also carry a small WPF-free legacy-name
compatibility executable, because already-installed older updaters validate
the existence of `BlackjackCasino.Desktop.exe` before atomic activation.
Modern updaters restart `BlackjackCasino.NativeShell.exe` directly.

CI gates include frontend tests, locale/art integrity, .NET unit and receipt
tests, native WebView2 startup/game navigation, packaged native album/Empire/Nine
Lives music and artwork hashes, and release SHA-256 verification.
Hardware Windows 10/11, accessibility, extended sessions and audio/device QA
are additional release acceptance tasks, not implied by a passing CI.

See [native migration history](native-shell-migration.md),
[Development delivery](development-update-delivery.md) and
[release branch policy](branching-and-release-policy.md).
