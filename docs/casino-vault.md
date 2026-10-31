# Casino Vault

Casino Vault is a play-money meta-progression system that turns wagering volume into a visible long-term reserve without changing any game's RNG, payout table, blackjack rules or theoretical RTP.

## Core rule

Every newly recorded eligible play-money wager contributes **2%** to the current Vault. The contribution is generated as meta progression; it is not deducted from the wager.

Eligible wager buckets are the already-deduplicated statistics for:

- Blackjack, including split/double hand wagers and insurance;
- Midnight Reels paid spins;
- Neon Vault paid stakes;
- Nine Lives paid stakes;
- Midnight Manor paid spins;
- Midnight Dice casino-table entry fees;
- preserved legacy slot wager totals for career-tier calculation.

Free spins, bonus payouts, Casino Empire income, Sudoku, challenge rewards, cosmetic currency and Vault payouts do not contribute.

## Vault tiers

| Tier | Lifetime wager unlock | Capacity | Full bonus |
| --- | ---: | ---: | ---: |
| Bronze Vault | $0 | $10,000 | +10% |
| Silver Vault | $250,000 | $25,000 | +15% |
| Gold Vault | $1,000,000 | $50,000 | +20% |
| Platinum Vault | $5,000,000 | $100,000 | +20% |
| Diamond Vault | $20,000,000 | $250,000 | +25% |

A tier upgrade never changes the capacity of a partially filled Vault. The newly unlocked tier is applied to the next fresh cycle after the current Vault is opened. Older profiles begin at the tier implied by their existing lifetime wager, but historical wagering is only used as a baseline and does **not** retroactively fill the Vault.

## Filling and overflow

Contributions are rounded to play-money cents using midpoint-away-from-zero rounding. The Vault stops exactly at capacity. Any contribution beyond the remaining capacity is discarded rather than banked invisibly for the next Vault.

Once full, the Vault no longer accumulates hidden progress. The player must explicitly open it.

## Opening

A full Vault pays its capacity plus the tier's full bonus. A Gold Vault therefore releases:

- $50,000 saved reserve;
- $10,000 full-vault bonus;
- $60,000 total bankroll credit.

The financial transition is resolved and persisted before the opening animation is presented. The desktop then plays the lock, door and reward sequence. Disabling animations skips directly to the result; disabling sound suppresses the synthesized Vault cues.

## Crash-safe payout receipt

Vault progress lives in the player profile while the live bankroll lives in `player-state.json`. Opening therefore uses a durable pending payout receipt:

1. profile records one pending payout ID and amount;
2. bankroll plus the same applied payout ID are saved together;
3. profile clears the pending receipt.

If the app stops between steps, startup reconciliation checks the receipt ID. A pending payout is either applied exactly once or finalized without paying twice.

## Presentation

The main header shows the current tier, amount/capacity and progress. New contributions produce a subtle header pulse. At 100% the control changes to `VAULT FULL / BREAK OPEN`.

The Vault window uses an original code-drawn safe with:

- tier-colored progress;
- a rotating lock wheel;
- near-full/full glow states;
- safe-door opening animation;
- separate reserve and bonus reveal;
- final bankroll payout presentation;
- synthesized mechanical and reward audio.

## Persistence and statistics

`CasinoVaultState` stores:

- schema version;
- current cycle tier and amount;
- observed eligible wager ledger;
- lifetime contributions;
- lifetime payouts and bonus amount;
- opened Vault count;
- biggest payout;
- last-open timestamp;
- pending payout receipt fields.

The observed-wager ledger is what prevents duplicate contributions when the same already-recorded game result is saved more than once.

## Design intent

Casino Vault is not a jackpot pity system. It does not inspect wins/losses and never changes an outcome. Its purpose is to make long play sessions feel cumulative even through bad variance and to create a gradual bridge toward the casino's high-stakes modes.
