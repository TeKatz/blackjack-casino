# Phase 2 update bootstrap note

**Historical compatibility record.** The current NativeShell publisher retains the verified updater/manifest version contract and the WPF-free legacy-name launcher. This note describes compatibility with older preview-era updaters; see [development delivery](development-update-delivery.md) for the active workflow.

The Phase 1 preview updater already implements update schema v1, SHA-256 verification, staged replacement and restart. Therefore schema-v1 releases must advertise `minimumUpdaterVersion` no higher than `0.1.0-preview.1` until the updater protocol itself changes.

A release's application version is not the updater protocol version. Using stable `0.1.0` as the minimum stranded `0.1.0-preview.*` installations because Semantic Versioning correctly orders prereleases below the final release.
