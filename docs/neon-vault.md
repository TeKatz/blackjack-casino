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


## High-roller reactive cabinet (PixiJS 8 / React)

The cabinet now uses a **single dedicated PixiJS 8 renderer** for both the
permanent ambient field and its full-machine reactive FX, replacing the tiny
standalone dust effect. It tracks the actual DOM bounds of the cabinet and
20-cell reel window on each capped 30-FPS tick, and renders:

- Additive-bloom perimeter lines, moving PCB conductors and 20 mobile neon
  contacts wrapping the **entire** cabinet, not only the slot grid.
- Forty-four ambient particles, a calm holographic reel scan, a subtle
  per-symbol sheen and twelve energy nodes driven by the **real** C# energy
  counter. Idle tint changes with high-roller wager tier and bonus state.
- Thirty-six reusable sparks, three concentric energy rings and four
  outward PCB traces on verified features and high wins. Blackout, overload,
  total blackout, security override, Wild, free-spin award, major jackpot and
  large win multipliers have distinct frame colors/intensities.
- A short win-result ribbon, independent of the outcome status, for wins of
  at least 10x the native wager. Wins of 50x+ or a GRAND jackpot get the
  maximum reactor intensity. Blackout warnings retain priority when they
  coincide with a payout; the win ribbon still reports the host payout.

All effects are presentation-only: no JavaScript outcome generation, wager
mutation, new game mechanics or save-schema changes. The native C# result
drives `neonImpact` and its thresholds; old results on opening/recovery never
replay the impact. Spinner/bonus/readiness and 16-step wager handling are
unchanged. The engine stops when the page is hidden and destroys its canvas
on unmount, gracefully keeping CSS presentation if WebGL is unavailable.
Both the user's animation preference and OS reduced-motion setting disable
GPU animation. The new CSS pulses affect dedicated border/light elements,
**not** the entire WebView: this avoids global compositing flicker and does
not move the buttons.

The audio remains **native** instead of layering a second WebAudio mixer
on top of the existing `CasinoAudio` PCM output. A dedicated, host-verified
`neon-vault.feature-impact` event aligns a unique native cue with the
first frame of the reaction (Lockdown, Overload, Total Blackout, Override).
Big Win and Mega Win now have longer original synthesized victory fanfares
from the existing `SlotAudio` engine, with separate volume/mute behavior
inherited from Neon Vault. Both stages verify the active spin ID, guard
against duplicate events and fall back to the native completion event when
reduced motion skips animation. The interaction callback can only emit a
sound; it has no effect on rewards, session state or the RNG.

Acceptance checks cover the durable 20-cell layout, native spin persistence,
frame/FX DOM surface and reel controls under compact and 1440p resolutions.
`neonVaultPresentation.test.ts` verifies event classification and priority.


## Premium symbol and physical reel upgrade (2026-11-08)

The eight Neon Vault symbols now use **wide 200×136 SVG artwork** that fills
the landscape reel window instead of letterboxed 160×160 black squares. Each
symbol is rebuilt with premium metal/faceted surfaces, a separate energy
halo, plated circuit glass, foil highlights, and a characteristic visual core:
an electrocardiogram with concentric pulses, multi-faceted prism, heavy
casino chip, jeweled crown, large mechanical vault key, plasma Wild,
mechanical vault-break wheel, and a purple access-key crest.

During idle, each family has a different slow motion: breathing circuitry,
prism color refraction, chip tilt, crown jewels, key-polish highlight,
magnetized Wild, vault-wheel rotation, and access-core power. Winning cells
amplify their inner glow and animate independently. A global reduced-motion
setting and the Neon Vault animation preference stop this motion.

The former 90-ms `setInterval` icon replacement has been removed.
`ReelStage.tsx` now renders **five real vertical strips** of SVG symbols,
with an initial recoil, gradual acceleration, smooth velocity change,
near-end deceleration, a 13%-cell mechanical overshoot, and magnetic
settle. Animation uses the browser Web Animations compositor rather than
repeated React state updates. Each strip contains an initial guard,
the four previously displayed cells, deterministic **cosmetic-only**
intermediate cells, the four **C#-authoritative base-grid** cells, and an end
guard. The final stop reveals exactly those four base cells: generated
intermediate symbols are never treated as outcomes or counted in the wallet.
Reel-stop sound events fire only after the corresponding visual strip
settles, by verified spin ID, once per reel.

Different reels finish at 760/1120/1510/1920/2430 ms. The existing
near-miss and feature overlay then reveal the C# final grid after the
last reel; Blackout, Wild and Override still execute only from the
real host receipt. The fallback for Windows reduced motion or disabled
animations resolves all reels and the native sound path immediately.
Animation cleanup cancels the strips on navigation/recovery, and a
bounded timeout prevents a stuck animation from blocking the controls.

The dedicated `reelMotion.ts` tests check exact final-cell geometry,
deterministic non-gameplay filler, ordered stops, recoil, overshoot and
settle; React render tests cover all eight wide, animated symbols and the
20 original visible cells. Native WebView2 smoke verifies all five
columns and clears the temporary strips before leaving the game.

All player balances, payouts, reel probabilities, feature frequencies,
progression and save formats remain unchanged.
