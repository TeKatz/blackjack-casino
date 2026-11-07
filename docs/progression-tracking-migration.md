# Per-game progression and reward migration

## Integration contract

- `PlayerProfileState.Slots`: verified Midnight Reels statistics after migration.
- `PlayerProfileState.Manor`: existing Midnight Manor statistics, unchanged.
- `PlayerProfileState.NeonVault`: persistent `NeonVaultStatistics`, with the common
  `Spins`, `Wins`, `Pushes`, `TotalWagered`, `TotalReturned`, `BiggestPayout`, and
  computed `NetProfit` properties. Additional counters: `PaidSpins`, `FreeSpins`,
  `FreeSpinSessions`, `FreeSpinsAwarded`, `Jackpots`, `GrandJackpots`, `Blackouts`,
  `TotalBlackouts`, `SecurityOverrides`, and `BestPayoutMultiplier`.
- `PlayerProfileState.LegacyPooledSlots`: preserved pre-migration mixed Reels/Neon
  totals. Display/export as **Legacy pooled slots (game attribution unavailable)**.
- New challenge categories: `NeonVault`, `Lifetime`, `Prestige`. Existing enum values
  and challenge IDs retain their identities. `ChallengeDefinition.IsEndgame` marks
  25 additional long-term goals; the original 45 challenges remain in the catalog.
- Both `ProgressionService.ClaimAllChallenges()` and
  `PlayerProgressionController.ClaimAllChallenges()` return `ChallengeClaimAllResult`:
  `Success`, `Message`, `Challenges`, `ClaimsCount`, `CoinsEarned`. An empty batch
  returns `Success = false`, zero coins, and an empty challenge list.
- `PlayerProgressionController.AccruePlaytime()` updates the stored lifetime playtime
  with only whole session seconds not previously accounted for. Call it before
  opening/rendering meta views so playtime challenges reflect the current session.
  `SaveAll()`, `ClaimChallenge()`, `ClaimAllChallenges()`, and `Dispose()` accrue
  automatically. `TotalPlaytime` is the complete live lifetime `TimeSpan` for
  overview/statistics/export; use `(long)controller.TotalPlaytime.TotalSeconds` for
  seconds in an export. Do not add full `SessionElapsed` to stored profile seconds.
  `UnrecordedSessionElapsed` exposes only the remainder, while `SessionElapsed`
  continues to represent the full session duration. The optional third constructor
  argument is a `TimeProvider` for deterministic testing; production uses the
  system monotonic clock. Disposal freezes session elapsed and cannot double count
  prior accruals; fractional seconds are carried until the next whole second.

## Why historical totals cannot be split

Before schema 4, `SlotProgression.Process(SlotSpinResult)` and
`SlotProgression.Process(NeonVaultSpinResult)` both wrote `Slots`. The profile has
no historical source tag or Neon Vault spin ledger. There is no honest way to
subtract old Neon play from Reels or to recover either game's historical counts,
wagers, returns, or maximum payout. No ratio, estimate, or inferred attribution
is used.

Normalization moves the old `Slots` object intact into `LegacyPooledSlots`, then
starts verified `Slots` at zero. `NeonVault` is a distinct object and starts at
zero for old saves. Manor, Blackjack, XP, seasons, coins, owned cosmetics, prestige,
and already-claimed rewards remain intact. Existing Reels rewards stay claimed;
unclaimed game-specific challenges now use only verified play. A player's old
mixed progress may therefore no longer complete an unclaimed Reels challenge.

The marker `SlotTrackingVersion = 1` makes this a one-time operation. Its default
is zero deliberately: JSON without either version field is migrated too. The
profile schema becomes at least 4. Re-loading or saving an already-migrated profile
does not reset verified statistics or re-archive them. Missing/null game statistics,
collections, and dictionary values are initialized before use. Both the profile
store and progression service invoke the same core normalizer; Manor profile
adoption also carries the new tracking fields across.

Cross-game lifetime wager/play goals include the legacy pool once, plus verified
Reels, Neon, Manor, and Blackjack. That preserves known aggregate history without
assigning any of it to a particular slot machine. Play counts mean Blackjack
rounds plus individual slot spins, including free spins. Neon free spins wager
zero; their returns count fully. Wins mean positive net, pushes mean zero net;
session triggers exclude free-spin retriggers. Multiplier records use the spin
bet, including the locked bet for free spins, rather than the zero free-spin stake.

## Claim-all transaction

Single and bulk service claims lock the shared profile, collect only complete,
unclaimed catalog entries, and check reward and wallet arithmetic before making
any change. Bulk claims replace the claim ledger and add the aggregate reward
once. Concurrent service instances sharing a profile cannot pay the same reward
twice. Claimed IDs persist through reload and prestige.

The controller performs one strict atomic profile replacement for a successful
batch, zero saves for an empty batch, and no hand-history save. An I/O/access
failure restores the prior coin balance and claim ledger and returns a failure,
so the batch can be retried. The strict save callback can be injected to verify
the transaction without touching the user's profile. Core services still do no
I/O. Normal gameplay and individual reward APIs retain their existing behavior.

## 2026-11-07 Challenge Board and Chill Lounge statistics expansion

The central catalog adds 44 new claimable milestones: **22 Solitaire**, **16
Lifetime** and **6 Prestige**. All previously published challenge IDs and
claims remain unchanged. Ordinary milestones scale into `IsEndgame` rewards;
endgame still grants the established Epic collection pack. Prestige goals use
the persistent `PlayerProfileState.Prestige`, not the player's current level,
so a level reset does not erase challenge progress. Extra milestones require
no schema change and apply retroactively to existing stat counters. Both
single-claim and claim-all use the established atomic player-profile reward
transaction.

The React Statistics page now includes all six hosted Chill Lounge games:
Sudoku, Midnight Solitaire, Word Search, Memory, Jigsaw Puzzle and Tales from
the Backroom. It also shows Midnight Merge's independent production and
restoration statistics. These displays use durable per-game counters; no
placeholder zeros are fabricated for unknown history. A Chill Lounge
completions summary and cross-game milestones count completed Sudoku and
Word Search puzzles, Memory tables, Jigsaw puzzles and Solitaire wins once.
The existing `LifetimePlays` metric additionally counts verified Solitaire
wins, Jigsaw completions and completed adventures. Solitaire starts and
abandoned games do not count as completions. No existing slot history is
re-attributed or discarded, and no wagering odds, rewards or save schema
are altered.

German and English challenge names/descriptions are added in parallel and
statistic metric captions have German equivalents in the React view.
Acceptance covers the native challenge receipt flow, historical claims and
all additional real game cards in the Windows WebView2 Statistics smoke.
