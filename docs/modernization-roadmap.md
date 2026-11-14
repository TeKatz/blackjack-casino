# Current architecture and quality roadmap

The Midnight Club is an offline play-money Windows casino. The native WinForms/WebView2 host and local React/PixiJS game interfaces have **replaced the former WPF application in both `dev` and the integrated `main` source**. The project is no longer in a dual-host migration phase.

This guide summarizes the current engineering direction. The original step-by-step worklog is retained in the [2026 modernization archive](archive/modernization-roadmap-2026.md).

## Implemented product baseline

- Native `BlackjackCasino.NativeShell.exe` hosts the local casino lobby, table games, slot machines, account/collection features and all eight Chill Lounge activities.
- C# remains authoritative for RNG, wagers, rewards, inventories, progression and persistence; the browser displays validated results and sends constrained commands.
- Midnight Manor embeds its Godot 3D Escape bonus; Midnight Descent is a separately launched Godot roguelite.
- Existing Development saves remain under the established `NativeShellPreview` path; a verified one-time legacy import is available, and old updaters are supported by a WPF-free executable alias.
- `dev` is the sole Development publisher. A native Stable release requires the same validation gates and an explicit tagged build from the now-integrated `main` source.

For the full product overview, see the [README](../README.md), [architecture](architecture.md) and [documentation index](index.md).

## Ongoing quality and maintenance work

These are **acceptance areas**, not claims that a new feature is already complete:

1. **User-experience verification:** Test text legibility, mouse/keyboard focus, F11, full-window and DPI scaling on actual Windows 10/11 hardware.
2. **Graphics and performance:** Verify lobby tile transitions, long-session memory use, slot animations, WebView2 recovery and loading/unloading the Manor 3D bonus.
3. **Sound and localization:** Check win/reel audio in every machine and bilingual coverage, especially large adventure text catalogs.
4. **Save and update continuity:** Exercise older installations, migration precedence, writer locks, interrupted save recovery, update manifests and legacy-shortcut compatibility.
5. **Feature maintenance:** Preserve existing game rules, RTP, high scores, achievement IDs and profile data when refreshing UI or expanding modes.

Passing CI is required for integration, but not a substitute for Windows device and long-session QA.

## Validation and change boundaries

- Frontend tests/build and [current presentation regressions](presentation-regression.md).
- Native .NET build, unit tests, protocol/storage smoke, real Windows WebView2 smoke.
- Offline migration/rollback simulations in `tools/NativeMigrationDiagnostics/`, never in the published casino.
- Godot exports and packaged artwork/audio hash verification.
- Verified Development release packages; no implicit Stable release from a `dev` merge.

Current responsibilities and workflows: [Contributing](../CONTRIBUTING.md), [release policy](branching-and-release-policy.md) and [Development updates](development-update-delivery.md).
