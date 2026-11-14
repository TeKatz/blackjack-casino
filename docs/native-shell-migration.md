# NativeShell and player-data compatibility

**Current status:** The Midnight Club runs directly as `BlackjackCasino.NativeShell.exe`. The retired WPF UI and its XAML game fallbacks are not part of the app. The original cutover checklists and step-by-step implementation notes are preserved in the [historical migration archive](archive/native-shell-migration-phases.md).

## Startup and profile selection

NativeShell's normal launch (without arguments) starts the real React/WebView2 casino. Existing user progress takes priority over any older data:

1. If the verified `NativeShellPreview\Candidates\development` profile exists, **reuse it**.
2. Otherwise, if a native profile already exists at `NativeShellPreview`, **reuse that**.
3. Otherwise, if older WPF save data exists, stage a **verified, copy-only first import** into the native Development candidate directory.
4. Otherwise, create a fresh native profile.

This policy intentionally keeps the historical directory name `NativeShellPreview`. Renaming it would require a separate, tested migration and must not happen as a cosmetic cleanup.

## Persistence and financial safety

- Installed user data lives under `%LOCALAPPDATA%\BlackjackCasino`, **outside the game installation directory**.
- NativeShell holds an exclusive writer lock for its active profile; concurrent writers cannot commit to the same journal.
- Player/roulette/Manor transaction checkpoints recover interrupted changes before normal play resumes.
- The first-run importer copies validated save files and local Arcade high-score browser storage rather than rewriting the original profile.
- An invalid or interrupted import blocks startup safely; it does not silently start the retired WPF host or overwrite old data.

## Updater and launch compatibility

The published casino contains `BlackjackCasino.NativeShell.exe` plus a tiny **WPF-free** `BlackjackCasino.Desktop.exe` launch alias for shortcuts and older updaters. New updaters restart NativeShell directly. Stable and Development channel manifests retain their established URLs and integrity validation.

The old checkpoint, export-to-WPF, rollback and simulated crash-recovery commands are **developer diagnostics only** in `tools/NativeMigrationDiagnostics/`. Their source and automated regression tests are retained, but they are not distributed in the casino package. The installed save path and manifest schema are unchanged.

## Current verification

Windows CI tests the no-WPF architecture, first-import/protocol smoke, journal integrity, browser navigation and real WebView2 startup. The separate diagnostics suite verifies the historical cutover and torn-rollback scenarios using temporary synthetic data. Release workflows check packaged executables and assets.

See [architecture](architecture.md), [release delivery](development-update-delivery.md) and [contributing](../CONTRIBUTING.md).
