# Neon Vault — Design and Implementation

Neon Vault is the casino's modern high-roller video slot. It is intentionally more feature-heavy and theatrical than Midnight Reels while remaining fully deterministic at the rules layer: every spin result is resolved before any anticipation, Near Miss or win-presentation animation begins.

## Core layout

- 5 reels × 4 rows;
- 1,024 Ways evaluation;
- shared play-money bankroll;
- bet range from $1 to $250,000;
- High Roller presentation begins at $10,000;
- Vault Class presentation begins at $100,000;
- $250,000 is the Max Exposure tier.

The current bet ladder is owned by `NeonVaultRules` in `BlackjackCasino.Core`.

## Symbols

The engine currently uses eight symbol identities:

- Pulse;
- Prism;
- Chip;
- Crown;
- Key;
- Wild;
- Scatter;
- VaultKey.

The WPF presentation maps these stable engine identities to custom Neon Vault artwork. Keeping the engine enum stable prevents presentation changes from accidentally changing saved data, symbol weights or payout math.

Generated symbol artwork is stored as compact embedded data, decoded once and cached as frozen WPF image sources. The 20 reel cells reuse those images rather than recreating image/control trees while the reels are moving.

## Paying ways

Normal paying symbols evaluate from the leftmost reel across consecutive reels. Wild substitutes for paying symbols. The 5×4 layout produces up to 1,024 all-ways combinations, and payout normalization is handled by the core engine so the four-row layout does not multiply returns unintentionally.

## Expanding Wilds

Wilds can trigger reel expansion according to the core feature result. During presentation the reel is visually charged and the affected cells become Wild without changing the already-resolved outcome.

Vault Break Free Spins strengthen the expanding-Wild behavior and can combine it with the Vault Energy system.

## Vault Break Free Spins

Scatter awards are currently:

- 3 Scatters → 8 Free Spins;
- 4 Scatters → 12 Free Spins;
- 5+ Scatters → 16 Free Spins.

Free Spins may retrigger. The presentation changes status, feature overlays and energy feedback while the same `NeonVaultSession` owns the actual sequence state.

## Vault Energy / Security Override

Vault Energy fills during the bonus sequence. The target is 12. When the engine resolves a Security Override, a reel is forced into the Wild state by the resolved result and the presentation plays the dedicated override sequence.

## BLACKOUT events

Neon Vault has three escalating system events:

- BLACKOUT;
- BLACKOUT OVERLOAD;
- TOTAL BLACKOUT.

The engine determines whether an event occurs, which cells become Wild and whether a full reel is affected. The desktop layer only presents that resolved data using warning states, overlays, wild-cell updates and paced delays.

A Blackout Near Miss may display a system-instability tease without granting the feature. It is presentation-only.

## Access-Key jackpots

`VaultKey` symbols feed the jackpot resolver. Current jackpot multipliers are:

- MINI — 25× bet;
- MINOR — 75× bet;
- MAJOR — 250× bet;
- GRAND VAULT — 1,000× bet.

The exact key-count/reel conditions live in `NeonVaultRules.ResolveJackpot` and are tested in the core test suite.

## Near Miss and anticipation policy

Near Miss is never allowed to alter a spin result. The engine returns a presentation hint only after the real outcome has already been generated.

The desktop layer may then:

- slow the final reel;
- emphasize Scatter or Access-Key candidates;
- pulse the final reel frame;
- show system-instability messaging;
- let the teased symbol pass visually before the resolved grid is shown.

This separation keeps presentation dramatic without coupling animation timing to game math.

## Candidate and win feedback

Neon Vault does not rely on a generic full-row flash. As reels settle, the presentation layer evaluates the already-visible symbols and marks plausible left-to-right candidate chains.

Candidate presentation includes:

- stronger cell borders;
- a small scale emphasis;
- dimming of irrelevant settled cells;
- feature-specific accents for Wild, Scatter and Access Key states;
- subtle neon connection paths between matching candidate cells.

After the final grid is visible, the presentation switches from candidates to confirmed winning cells. Winning cells pulse more strongly and confirmed connection paths are rendered with a stronger treatment.

The feedback layer does not participate in payout calculation.

## Performance principles

An early version of the slot rebuilt many WPF symbol trees on a short timer and applied expensive effects to continuously changing reel content. That approach produced visibly poor frame pacing.

The current implementation therefore follows these rules:

- build reel/cell controls once;
- update existing presenters in place;
- cache/freeze brushes and decoded image assets;
- use transforms/opacity for repeated motion;
- reserve expensive effects for short feature moments;
- avoid outcome-dependent UI work inside the high-frequency preview tick where possible.

## Embedded casino hosting

Neon Vault opens from the casino lobby and is displayed inside the main application window. The player does not see a second taskbar/native game window.

The hidden shell remains responsible for lifecycle and game-local resources while `MainWindow.EmbeddedGames` reparents the visible content into the casino host. Leaving Neon Vault returns to the lobby and restores the host header/state.

## Progression and statistics

Neon Vault shares global systems such as bankroll, player level, Prestige, Coins and Season progress, but its verified game statistics use a dedicated Neon Vault bucket.

Midnight Reels and Neon Vault must not write to the same modern per-game statistics. Older saves created before source-aware slot tracking are preserved in the legacy earlier-slot-history bucket described in [progression-tracking-migration.md](progression-tracking-migration.md).

## Regression coverage

Current automated coverage includes:

- `NeonVaultTests` for core rules/features;
- ordinary solution build and unit tests;
- Windows casino presentation regression at multiple window sizes;
- verification that Neon Vault stays inside the single visible casino window;
- checks for the Vault Energy target and high-roller controls;
- preview/Development Windows publishing through CI.

For the surrounding application structure see [architecture.md](architecture.md), and for the presentation test strategy see [presentation-regression.md](presentation-regression.md).
