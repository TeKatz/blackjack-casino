# Neon Vault — React/PixiJS presentation and native gameplay

Neon Vault is a high-voltage, offline play-money video slot in the modern casino lobby. The playable surface is now `frontend/neon-vault.html`, served locally through WebView2. React renders text, controls and result information; PixiJS renders a small decorative particle field. Eight bespoke scalable cyber-symbol SVG designs reproduce the distinctive Pulse, Prism, Chip, Crown, Key, Wild, Vault and Access Key identities. The retired WPF game window and its prerasterized symbol renderer have been deleted.

## Native game contract — unchanged

`NeonVaultSession` in `BlackjackCasino.Core` remains the only outcome authority:

- Five reels × four rows, 1,024-ways left-to-right evaluation (normalization and odds unchanged).
- Original 16-step wager ladder from $1 to $250,000; High Roller >= $10,000; Vault Class >= $100,000; Max Exposure at $250,000.
- Three / four / five Scatters grant eight / twelve / sixteen Vault Break free spins; retriggers work as before.
- Expanding Wilds, persistent Vault Energy (target twelve), and an armed Security Override on a later free spin.
- BLACKOUT, BLACKOUT OVERLOAD and TOTAL BLACKOUT feature events, plus presentation-only near misses.
- Access Key MINI / MINOR / MAJOR / GRAND payouts of 25× / 75× / 250× / 1,000×.
- The native `SlotProgression` writes Neon Vault statistics to its independent existing profile bucket, never the Blackjack hand ledger or Midnight Reels bucket.

No math, symbol weights, RNG, jackpot criteria, bonus schedule, saved-profile schema or award rules changed during the presentation migration.

## Architecture

The new `MainWindow.ModernNeonVault.cs` adapter creates the session, handles a shared C# wallet, routes native audio, updates preference and progression persistence and owns WebView2 lifetime. `ModernNeonVaultSnapshot` exposes **read-only** paytable, feature, grid and statistics data. An action from JavaScript is a small, versioned intent: `spin`, `bet.set` (only listed bet steps), `sound.set` or `animations.set`.

`ModernNeonVaultProtocol` requires the exact packaged HTTPS virtual origin `/neon-vault.html`, a GUID session/command ID and an expected revision. `ModernNeonVaultSession` deduplicates receipts and rejects stale or expired requests **outside** the disposable browser. A spin result and shared wallet are committed in native C# before React receives the result. UI speed, reduced-motion, animation callbacks, preview near misses and audio cannot change the outcome.

A failed WebView2 navigates back to the lobby, preserves the **in-memory** Neon Vault C# machine (including pending Vault Break spins / Vault Energy / Security Override) and keeps the receipt ledger. Reentering instantiates a new browser against that state, without replaying any wager. On a deliberate exit, the transient game-local session retires; the React Back control follows the old native rule and is disabled while free spins are pending. A full process crash/restart does not yet persist an unfinished bonus sequence: durable mid-bonus recovery is a separate future scope.

## Modern presentation

The modern cabinet includes:

- 20 scalable neon symbol cells, a five-reel staggered animation, left-to-right visual candidate emphasis, confirmed winning combinations and a staged BaseGrid → FinalGrid transformation (without independent game logic).
- Feature-specific status overlays for Blackouts, Security Override, Wild expansion, Scatter free spins, Access Key jackpots and final results.
- A live 12-unit Vault Energy meter, high roller tiers, all four wager-dependent jackpot values, complete 3/4/5 symbol paytable, scoped statistics, last return, balance, wager ladder and MAX shortcut.
- Optional 20-spin automatic play, sound and animation preferences, EN/DE copy, keyboard shortcuts, focus and reduced-motion support. Browser motion can never issue an unacknowledged double wager.
- GPU particle ambience that can pause when hidden, independent of React text accessibility, without an extra network dependency.

## Verification

`ModernNeonVaultBridgeTests` checks origin, strict intents, idempotent receipts, exact 20-cell grid, jackpot tiers, money projection and bonus-state recovery. `frontend/tests/neonVaultBridge.test.ts` checks that the browser rejects corrupt results and fabricated feature data. The Windows `ModernNeonVaultWebSmoke` opens the actual packaged page, checks all 20 symbols, all bet/chip options, feature and jackpot widgets, confirms one native spin and reward/wallet update, exercises navigation, injects a browser failure and verifies the original C# machine is retained. The full CI, frontend build, presentation regression and Development publishing remain release gates.

### Manual device acceptance

Subjective 1080p/1440p/DPI readability, audio mix, effects layering, long autoplay sessions and perceived pacing need verification on target Windows hardware. The retired WPF layout and source are no longer fallbacks. Further work on the shared WPF shell is explicitly separate.
