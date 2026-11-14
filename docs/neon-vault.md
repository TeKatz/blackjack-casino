# Neon Vault

**Neon Vault** is a high-energy five-reel, four-row slot machine with **1,024 ways to win**, played entirely with virtual casino credits. The cyber-themed cabinet is rendered locally in React/PixiJS and hosted by NativeShell.

## Features

- A wide bet ladder, including High Roller and Vault Class tiers.
- **Vault Break** free spins, expanding Wilds and accumulated Vault Energy.
- Security Override, Blackout events and Access Key jackpot tiers.
- Animated reel stops, symbol highlights, sound and pace controls.
- A shared casino bankroll with native saved statistics and progression.

All five reels, reward combinations and bonus results are generated and settled in the original C# `NeonVaultSession` before visuals run. The browser does not calculate RNG, bet exposure, payouts or jackpots. Near-miss effects and presentation speed are cosmetic only.

## Recovery and validation

The validated `ModernNeonVaultProtocol` accepts strict origin-bound, session/revisioned intents and deduplicates request IDs. NativeShell preserves settled results and money transactions if WebView2 is closed or restarted. The former WPF cabinet is not a fallback.

Tests cover 1,024-ways math, legal stakes, bonus outcomes, payout continuity and browser command protection. Hardware QA additionally covers 1080p/1440p, different DPI levels, audio, rendering and long auto-spin sessions.

See the [historical Neon Vault design and visual iterations](archive/neon-vault-history.md), [presentation contract](presentation-regression.md) and [architecture](architecture.md).
