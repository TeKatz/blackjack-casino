# Development update delivery

The in-app Development manifest remains:

`https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json`

The original Manor PR only uploaded a CI artifact. Its `slots/midnight-manor` branch was outside the Development publisher's triggers, leaving the public channel at `0.6.5-dev.71` from main. A final `0.6.5` also sorts above any `0.6.5-dev.N` under SemVer. Changing a URL or comparison only in a new executable cannot repair an already-installed client.

At the user's request, the Manor branch was explicitly included in the Development workflow, and later feature branches have taken over the same mutable Development channel. No Stable release is required for Development test delivery. The active source branch is selected explicitly in the publisher workflow.

Before publication, the workflow validates localization resources, builds, runs all tests and the actual Windows WPF smoke, and packages the application with its separate updater. It then verifies the release target and required assets. Package/manifest filenames, SHA-256 verification and updater schema are unchanged. Mutable-manifest requests send Cache-Control: no-cache, no-store to revalidate subsequent checks. The UI shows installed/available versions when no newer update is found.

Versioning rule: start a new Development cycle on a version base strictly above the latest final version. Preserve SemVer ordering; do not offer silent downgrades. PR preview artifacts use dev.0 and are superseded by the published Development run number.

## Current test channel: Casino Empire command center

The shared `development` release is pinned to `feature/empire-command-center`,
created fresh from main at `b345f2a9a6e5582bee8786f52f1564b11a4626c1`.
The publisher's push trigger and job guard both select this branch. The existing
in-app manifest URL, package names and updater schema remain unchanged.

Versions continue as `0.10.1-dev.N` using the existing increasing workflow run
number. Every publication validates the complete application, actual Windows UI
behavior, package hash and size, application/manifest version agreement and the
published release target before reporting success.

This branch redesigns Casino Empire around an Estate command center: direct
section navigation, live project and contract actions, room and prestige controls,
connected research branches, synergy progress and compact catalog pagination.
Existing Empire simulation, profile storage, other games and shared wallet guards
remain in place. The Windows suites cover standard desktop layouts, research
prerequisites, direct dashboard transactions, managers, automation inputs, events,
contracts, property switching, statistics and detached hosting.

`tools/check_localization.py` is a mandatory CI and Development-publisher gate. Every translated resource file must contain exactly the neutral English key set and every value must be non-empty. English remains the fallback when a runtime key cannot be resolved in a selected translation. Proper product and mode names such as The Midnight Club, Midnight Solitaire, Midnight Manor and Neon Vault remain untranslated unless explicitly changed by product direction.

The source pin lives in the publishing workflow's push trigger and job guard. When changing the active feature branch again, update both fields together on the selected branch. Normal feature-branch CI previews remain available as artifacts and do not replace the shared Development release unless that branch is explicitly pinned here.
