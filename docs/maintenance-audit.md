# Repository maintenance and current state

**Reference baseline: November 2026.** This page describes the current repository structure and follow-up verification areas. Older version-by-version notes and previously superseded branch recommendations are preserved in the [historical maintenance audit](archive/maintenance-audit-pre-cutover.md).

## Active repository

| Area | Current baseline |
| --- | --- |
| Development source | `dev`, the mutable Development release publisher |
| Stable source | Reviewed `main` plus explicit version tags |
| Windows host | .NET 10, WinForms, local WebView2, React and PixiJS; no WPF project |
| Rules and persistence | `BlackjackCasino.Core`, shared C# contracts and native write-ahead journals |
| Compatibility | Verified first-time legacy save import; WPF-free `Desktop.exe` alias for older installations |
| Historical tools | Isolated `tools/NativeMigrationDiagnostics` regression executable; not published |
| Visual gameplay | Local browser bundles and Godot-based Manor bonus / Midnight Descent |

## Maintenance priorities

- Recheck SDK, central NuGet pins and GitHub Actions version support **before** proposing upgrades. The historical audit's “newer version” table is not a live package registry.
- Update dependencies in reviewable, isolated changes with a rollback commit and complete Windows build/test evidence.
- Preserve RNG, payouts, persistent IDs, profile storage and exactly-once receipts across UI changes.
- Keep frontend/DOM, real WebView2, F11, Godot, audio, asset hash and localization gates in CI.
- Validate DPI, Windows-specific performance, translations and older-installation update paths on real devices, not just hosted runners.

Run the documented checks in [Contributing](../CONTRIBUTING.md). For current CI and publisher behavior, see [architecture](architecture.md), [branch policy](branching-and-release-policy.md) and [Development updates](development-update-delivery.md).
