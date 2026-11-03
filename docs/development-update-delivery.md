# Development update delivery

The in-app Development manifest remains:

`https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json`

The original Manor PR only uploaded a CI artifact. Its `slots/midnight-manor` branch was outside the Development publisher's triggers, leaving the public channel at `0.6.5-dev.71` from main. A final `0.6.5` also sorts above any `0.6.5-dev.N` under SemVer. Changing a URL or comparison only in a new executable cannot repair an already-installed client.

At the user's request, the Manor branch was explicitly included in the Development workflow, and later feature branches have taken over the same mutable Development channel. No Stable release is required for Development test delivery. The active source branch is selected explicitly in the publisher workflow.

Before publication, the workflow validates localization resources, builds, runs all tests and the actual Windows WPF smoke, and packages the application with its separate updater. It then verifies the release target and required assets. Package/manifest filenames, SHA-256 verification and updater schema are unchanged. Mutable-manifest requests send Cache-Control: no-cache, no-store to revalidate subsequent checks. The UI shows installed/available versions when no newer update is found.

Versioning rule: start a new Development cycle on a version base strictly above the latest final version. Preserve SemVer ordering; do not offer silent downgrades. PR preview artifacts use dev.0 and are superseded by the published Development run number.

## Current test channel: Shop cosmetics expansion

The shared `development` release is pinned to `feature/shop-cosmetics-expansion`,
created fresh from main at `c4b72ef075104cff05d9426a9bffbe161f10d360`.
The publisher's push trigger and job guard both select this branch. The existing
in-app manifest URL, package names and updater schema remain unchanged.

Versions continue as `0.10.1-dev.N` using the existing increasing workflow run
number. Every publication validates the complete application, actual Windows UI
behavior, package hash and size, application/manifest version agreement and the
published release target before reporting success.

This branch adds six card sets and six dice sets across three price tiers,
refines Ruby Velvet and Aurora Prism with subtle glows, and replaces the nine
requested default and season portraits with individually generated artwork.
The Windows checks export full/compact card faces and backs, D6/D20 palettes and
circular portrait crops. Cosmetic purchases, equipment and save compatibility
are covered by the complete unit suite. See `shop-cosmetics-expansion.md` for
prices, rendering details and image prompts.

`tools/check_localization.py` is a mandatory CI and Development-publisher gate. Every translated resource file must contain exactly the neutral English key set and every value must be non-empty. English remains the fallback when a runtime key cannot be resolved in a selected translation. Proper product and mode names such as The Midnight Club, Midnight Solitaire, Midnight Manor and Neon Vault remain untranslated unless explicitly changed by product direction.

The source pin lives in the publishing workflow's push trigger and job guard. When changing the active feature branch again, update both fields together on the selected branch. Normal feature-branch CI previews remain available as artifacts and do not replace the shared Development release unless that branch is explicitly pinned here.
