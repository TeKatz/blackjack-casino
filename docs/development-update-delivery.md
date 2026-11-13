# Development update delivery

## Current Development publisher (2026-11-13)

The active source branch is **`develop2`**, created from the completed `develop` → `main` integration commit `24c26c3`. The former `develop` branch remains available for historical reference but cannot publish the shared Development channel. Both the `on.push.branches` filter and the `publish-development.if` ref guard in `.github/workflows/development-release.yml` select exactly that branch. The publisher builds and tests the frontend and native .NET solution, runs Windows startup/presentation checks, creates the installer package and verifies the SHA-256 checksum, updater manifest and published GitHub assets. Neither `main` nor `feature/*` pushes publish the mutable Development channel.

**The installed in-app Development manifest URL remains unchanged:**

`https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json`

The upgrade channel, asset names, semantic-version ordering, updater schema and player-data location are unchanged. Only a successful release publication will be offered to installed clients; a branch build or PR artifact is not a released update. The release contains `blackjack-casino-win-x64.zip`, its checksum, the update manifest and release notes.

The [Casino Music Importer](casino-music-importer.md) is still launched through Actions on the default `main` branch, but it explicitly checks out/commits into `develop2` and optionally dispatches the Development Release workflow from `develop2`.

New work branches from `develop2` and is tested before integration. GitHub Actions CI, presentation, repository contract and Godot workflows explicitly include `develop2` so every push is validated. Release publishing retains a single-branch push filter plus matching job guard. Stable publishing remains an explicit `v*.*.*` tag workflow, separate from a merge to `main`. The old Manor feature-branch publisher and PR #35 revert are historical context only. See [branch policy](branching-and-release-policy.md).

## Historical test channel: Casino quality – Phase 4

Before consolidation, the mutable in-app `development` release was pinned to
`feature/casino-quality-phase-4`, building on all work from Phases 1–3
and the earlier shop-cosmetics expansion. Both the workflow push filter and
the job-level guard select this branch. The historical pin was superseded during consolidation; the former Manor feature publisher was superseded by `develop` on 2026-11-08.

The installed client's Development manifest URL is unchanged, as are
the package, updater executable, updater schema and SHA-256 validation.
The publisher produces `0.10.1-dev.N` builds in monotonically
increasing workflow-run order, subject to normal SemVer precedence.

Phase 4 brings offline, version-matched release notes and the ongoing
German localization of Tales from the Backroom. German content coverage
is being expanded incrementally and remains incomplete outside translated
scenes and UI. The publisher gates on resource parity, the adventure
localization sanity checker, the full Windows test/smoke suite, and
release-package/version verification before replacing the mutable release.

### Switching channels

If a different branch takes over Development, update the workflow's
`on.push.branches` filter and `publish-development.if` guard together,
then update this document. Existing installations will find the newer
version through the same `development/update-manifest.json` URL.

`tools/check_localization.py` is a mandatory CI and Development-publisher gate. Every translated resource file must contain exactly the neutral English key set and every value must be non-empty. English remains the fallback when a runtime key cannot be resolved in a selected translation. Proper product and mode names such as The Midnight Club, Midnight Solitaire, Midnight Manor and Neon Vault remain untranslated unless explicitly changed by product direction.

The source pin lives in the publishing workflow's push trigger and job guard. When changing the active feature branch again, update both fields together on the selected branch. Normal feature-branch CI previews remain available as artifacts and do not replace the shared Development release unless that branch is explicitly pinned here.

## Repository-preparation branch (historical, 2026-11-04)

`feature/repository-workflow-refresh` contains documentation, agent instructions and a repository-contract check only. It was based on the cumulative `feature/casino-quality-phase-4` snapshot. During consolidation, its documentation and checks became part of `main`. The owner controls when historical branches are deleted. The React/TypeScript/PixiJS Manor is now the only lobby-selectable Manor interface in Development, and updates ship directly from the active feature branch without merging. See [modernization roadmap](modernization-roadmap.md).

The lightweight repository-contract workflow checks required documentation, local README/guide links and consistency between the Development workflow's push branch filter and its job guard. It does not substitute for Windows gameplay/build/smoke validation.

## Out-of-process updater recovery (2026-11-06)

The packaged updater now runs as a Windows GUI helper without a transient console window. It records each critical stage and recoverable directory-lock retries to `%LOCALAPPDATA%\\BlackjackCasino\\Updates\\updater.log`. A failed update displays the actionable Windows error and attempts to restart the still-present casino installation. This is particularly important when WebView2 descendants or antivirus activity briefly hold the application folder open after the parent window has closed.

Zip extraction and executable validation always finish in staging before touching the existing installation. Directory swaps and rollback are still atomic renames; transient `IOException` and `UnauthorizedAccessException` failures are retried with a finite delay budget instead of exiting at the first sharing violation. If activation fails, the prior installation is restored from its backup; if restoration also fails, its backup path remains preserved in the reported error. The downloaded ZIP is deleted only after a successful installation and restart launch.

The verified SHA-256, existing manifest schema, update channel and existing user-data paths do not change. The ordinary `PackageInstallerTests` additionally simulate temporary locks, permanently locked folders and non-retryable failures. Existing clients upgrade to this more resilient helper through the normal Development download; if an older helper fails before it can install the improvement, the published ZIP can still be extracted manually into the installation location with the app closed.

## Install-folder lock prevention — persistent WebView2 profile moved (2026-11-06)

An updater log from a Windows installation showed a verified package that could not rename the existing application directory after eleven temporary-lock retries. The exact owner of the access-denied lock is not known. Investigation found that the shared WebView2 initializer previously used `EnsureCoreWebView2Async()` without an explicit environment. For unpackaged Windows applications, WebView2 then creates a mutable `BlackjackCasino.Desktop.exe.WebView2` folder **inside the update target**. Browser/renderer children can keep handles open even after the desktop parent exits.

All ten local React WebView2 hosts, including the detached Tales investigation board, now reuse one `CoreWebView2Environment` with its user data folder at `%LOCALAPPDATA%\\BlackjackCasino\\WebView2`. The installed application directory contains only replaceable program assets. No browser data is deleted or automatically migrated: the app's authoritative wallet and saves already live in separate native files outside the installation. The old profile directory may remain from previous builds and must not be removed while a WebView2 process is using it.

The Windows Lobby and Lounge security smoke asserts the runtime-reported `core.Environment.UserDataFolder` equals that exact user-local path, and the source boundary audit rejects a return to implicit default-profile initialization. Existing per-page origin validation, browser permissions and navigation restrictions are retained.

This eliminates one plausible installation-directory lock source. It does not guarantee that antivirus, an open Explorer directory, or another process cannot lock the target. A currently running **older build** can still experience the old profile lock while attempting the first update to the fixed build; in that case close the app and all related WebView2 subprocesses, or install the verified published ZIP manually while no handles are open.

## Directory-rename-independent update fallback (2026-11-07)

The updater still prefers the original staged, whole-folder atomic swap. Repeated
real-world Windows logs showed that the installation **root itself** could stay
unrenamable for the entire 87-second retry budget even when a disposable sibling
directory could be renamed and no casino executable was detected. Longer retry
loops cannot resolve a permanent root-directory handle.

After four short attempts to rename the existing root, the updater now switches
to a **journaled in-place replacement of only the verified package files**.
This makes an open Windows handle on the root directory (without delete sharing)
independent of the ability to update the files inside it. Original versions of
every file that will be overwritten are copied to a unique sibling backup before
the first mutation; non-package files and user data are left untouched. Each
replacement is a same-volume file move, with the executable swapped last.
The durable backup journal and active marker are outside the installation root.

Any normal installation error rolls replaced files back before the old casino is
restarted. The next updater first checks for interrupted journaled transactions
and restores original files before extracting or activating another package.
If rollback is blocked, its backup remains and the updater **will not restart
a potentially mixed installation**. The SHA-256 package check, executable
validation, release channels and save locations are unchanged. The fallback
may retain obsolete program assets that were absent from the new package;
a future successful whole-folder swap removes them.

Windows tests hold an actual directory handle without `FILE_SHARE_DELETE`
and verify an in-place upgrade while the root rename is denied. Additional
tests cover a fault after partial file replacement, the recovery journal,
idempotent restart recovery, and preservation of unrelated local files.
These tests prove the specific directory-handle case, not arbitrary ACL denial,
antivirus blocks on individual executable files, or a second active casino.
