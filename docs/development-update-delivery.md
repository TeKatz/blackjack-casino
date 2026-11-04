# Development update delivery

The in-app Development manifest remains:

`https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json`

The original Manor PR only uploaded a CI artifact. Its `slots/midnight-manor` branch was outside the Development publisher's triggers, leaving the public channel at `0.6.5-dev.71` from main. A final `0.6.5` also sorts above any `0.6.5-dev.N` under SemVer. Changing a URL or comparison only in a new executable cannot repair an already-installed client.

At the user's request, the Manor branch was explicitly included in the Development workflow, and later feature branches have taken over the same mutable Development channel. No Stable release is required for Development test delivery. The active source branch is selected explicitly in the publisher workflow.

Before publication, the workflow validates localization resources, builds, runs all tests and the actual Windows WPF smoke, and packages the application with its separate updater. It then verifies the release target and required assets. Package/manifest filenames, SHA-256 verification and updater schema are unchanged. Mutable-manifest requests send Cache-Control: no-cache, no-store to revalidate subsequent checks. The UI shows installed/available versions when no newer update is found.

Versioning rule: start a new Development cycle on a version base strictly above the latest final version. Preserve SemVer ordering; do not offer silent downgrades. PR preview artifacts use dev.0 and are superseded by the published Development run number.

## Current test channel: Casino quality – Phase 4

The mutable in-app `development` release is now pinned to
`feature/casino-quality-phase-4`, building on all work from Phases 1–3
and the earlier shop-cosmetics expansion. Both the workflow push filter and
the job-level guard select this branch. The pinned branch is the only intended
source for future Development builds; do not publish the superseded branch.

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
