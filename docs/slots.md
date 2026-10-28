# Midnight Reels

An optional, offline, play-money slot machine accessible from **Casino Games** at the Blackjack table. Casual Solo still opens directly, with no setup required. Finish the current blackjack hand before opening Slots; the modal window prevents concurrent wagers in two games.

## Play

- Three independent 32-stop weighted reels; only the centre horizontal line pays.
- Bets use the existing Casual table's $1 minimum / $1,000 maximum and the same dollar bankroll, with quick choices $1 / $5 / $25 / $100 / $500 / $1,000.
- Spin with the button or Space; return to Blackjack with Back, Escape or the close button.
- The last slot bet is saved separately from the blackjack bet.
- If the balance drops below the selected stake, choose a smaller stake. Below $1, the existing Blackjack bankroll reset remains available.

| Symbol | Stops per reel | Three matching symbols |
| --- | ---: | ---: |
| Cherries | 12 | 5× |
| Lemon | 8 | 10× |
| Bell | 6 | 25× |
| BAR | 4 | 50× |
| Seven | 2 | 100× |

Exactly two cherries in any order return 1×. All multipliers include the original stake. Every other combination returns zero. Enumerating all 32³ combinations gives a theoretical return of 97.0458984375%; this is a long-term mathematical value, not a promised session result.

## Progression and persistence

Each completed spin earns 20 player XP and 15 Season XP, with +5 of each for a net profit. These match the existing normal-hand base/win amounts. Larger bets or jackpots do not increase XP. Existing level, Prestige, cosmetic Coin and free Season rewards apply through `ProgressionService` without changing blackjack XP calculation.

Slot statistics are additive fields in the existing profile: spins, net wins, pushes, total wagered/returned and largest payout. Slots do not create blackjack hand histories or alter blackjack statistics. Older profiles get empty slot statistics automatically; Prestige keeps them.

A spin resolves synchronously using `RandomNumberGenerator.GetInt32`, validates all stops, and updates the shared balance/profile before animation. Invalid bets or failed random draws leave the slot wallet unchanged. The UI locks betting/spinning during the reel sequence. Closing mid-animation stops audio/timers and retains the already-settled result, with no refund or second award.

The host uses the existing between-round `CasinoSession.ResetBankroll(amount)` operation to synchronize the shared wallet. Blackjack shoe and history survive the visit. No blackjack engine, shoe, strategy, rules, settlement or XP formula is changed.

## Presentation

Original resolution-independent vector textures are bundled in `Presentation/SlotTextures.xaml`: five illustrated symbols, brushed cabinet grain, reel paper and metallic gold trim. The displayed scrolling strips use the same weighted symbol order as the result generator; they decelerate and stop individually on the actual sampled symbols. Profit produces a cabinet light pulse and 777 has a longer celebration. Original PCM cues cover spin-up, reel ticks, individual stops, wins, jackpot, push and loss. No downloaded assets, fonts, network calls or external audio packages are required.

Sound and animation toggles respect the existing preferences, and changes carry back to the table. With animations off, the same result/award is shown immediately. The window scales the full layout to its available space.

## Validation

Run the standard repository commands with .NET 10. Linux compilation additionally requires `-p:EnableWindowsTargeting=true`. Behavioral tests cover every triple/pair payout, the exhaustive return, funds conservation, stake validation, failed draws, duplicate XP protection, bet-independent XP, shared rewards, profile round trips and Prestige retention, alongside all existing blackjack/updater tests.

Windows runtime acceptance remains separate from compilation: open Slots after a settled hand; spin using mouse/keyboard; change bets and toggle sound/animation; close during a spin and reopen; confirm balance and XP after restarting; check reel alignment and sound, and return to play another Blackjack hand. Also check the minimum window size and high-DPI scaling.
