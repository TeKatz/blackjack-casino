# Phase 2 update bootstrap note

The Phase 1 preview updater already implements update schema v1, SHA-256 verification, staged replacement and restart. Therefore schema-v1 releases must advertise `minimumUpdaterVersion` no higher than `0.1.0-preview.1` until the updater protocol itself changes.

A release's application version is not the updater protocol version. Using stable `0.1.0` as the minimum stranded `0.1.0-preview.*` installations because Semantic Versioning correctly orders prereleases below the final release.
