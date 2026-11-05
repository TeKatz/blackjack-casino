# Midnight Reels

The **primary Casino Lobby route** now opens the offline React/WebView2 Midnight Reels cabinet (`frontend/reels.html`). The old WPF `SlotMachineWindow` remains available only if the packaged frontend is missing or WebView2 fails, including during a committed spin. The WPF fallback is still exercised by Windows regression tests.

This migration is developed on `feature/manor-modernization-phase-1` without merging to `main`. Finish any active Blackjack hand before switching modes. The .NET gameplay engine and existing saves remain the only authority.

## Play

- Three independent 32-stop weighted reels; only the centre horizontal line pays.
- Bets use the existing Casual table's $1 minimum / $1,000 maximum and the same dollar bankroll, with quick choices $1 / $5 / $25 / $100 / $500 / $1,000.
- Spin with the button or Space; return to the casino lobby with Back or Escape.
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

A spin resolves synchronously using `RandomNumberGenerator.GetInt32`, validates all stops, and updates the shared balance/profile before animation. Invalid bets or failed random draws leave the slot wallet unchanged. The UI locks betting/spinning during the reel sequence. Closing mid-animation stops the React animation and preserves the already committed result: no refund, replay or second award. The host does not accept any result fields from WebView2.

The host uses the existing between-round `CasinoSession.ResetBankroll(amount)` operation to synchronize the shared wallet. Blackjack shoe and history survive the visit. No blackjack engine, shoe, strategy, rules, settlement or XP formula is changed.

## Presentation

Original resolution-independent vector textures are bundled in `Presentation/SlotTextures.xaml`: five illustrated symbols, brushed cabinet grain, reel paper and metallic gold trim. The displayed scrolling strips use the same weighted symbol order as the result generator; they decelerate and stop individually on the actual sampled symbols. Profit produces a cabinet light pulse and 777 has a longer celebration. Original PCM cues cover spin-up, reel ticks, individual stops, wins, jackpot, push and loss. No downloaded assets, fonts, network calls or external audio packages are required.

Sound and animation toggles respect the existing preferences, and changes carry back to the table. With animations off, the same result/award is shown immediately. The React cabinet adapts to the available casino-host height and width, maintains the three independent weighted 32-stop strips and highlights the actual centre payline. A native WebView2 overlay replaces the old WPF Viewbox for normal play.

## Validation

Run the standard repository commands with .NET 10. Linux compilation additionally requires `-p:EnableWindowsTargeting=true`. Behavioral tests cover every triple/pair payout, the exhaustive return, funds conservation, stake validation, failed draws, duplicate XP protection, bet-independent XP, shared rewards, profile round trips and Prestige retention, alongside all existing blackjack/updater tests.

Windows runtime acceptance remains separate from compilation: open Slots after a settled hand; spin using mouse/keyboard; change bets and toggle sound/animation; close during a spin and reopen; confirm balance and XP after restarting; check reel alignment and sound, and return to play another Blackjack hand. Also check the minimum window size and high-DPI scaling.


## Modern transport and fallback

\`ModernReelsProtocol\` accepts only session-bound, revisioned, GUID-receipted
\`spin\`, \`bet.set\`, \`sound.set\` and \`animations.set\` intents from the
packaged \`/reels.html\` origin. Duplicate request IDs return their original
receipt and never spin twice. Invalid, expired and stale commands cannot
mutate the shared wallet. React receives a read-only result containing
the sampled stop indices, exactly matching the C# symbol strip, the
committed payout, wallet and progression receipt. The browser must not
supply stops, outcomes or balances.

\`MainWindow.ModernReels\` persists the shared blackjack wallet, slots
statistics, XP, last wager and area-specific sound/animation settings
**before** the optional reel-animation sequence begins. Individual reel
stop and final result cues use the original native \`SlotAudio\`; the
Reels soundtrack uses \`BackgroundMusic\`. Animations and sounds honor
existing accessibility and presentation preferences.

The WebView2 browser stays visible from the first WPF layout pass to
avoid the historic hover-triggered rendering bug. A bounded readiness
watchdog falls back automatically to the old WPF slot on missing browser
acknowledgement or browser/process failure, retaining the committed
shared wallet. No second native casino window is created for normal
play. Historical WPF visuals and control tests remain as fallback coverage.

Acceptance: TypeScript snapshot validation, authoritative native slot
behavior, direct lobby launch, wager round trip, exactly one saved
spin/XP settlement, return/reopen, browser-crash recovery, Windows
presentation layout and packaged offline assets. Device-specific
DPI/GPU, prolonged memory usage and subjective audiovisual polish
still require real hardware review.
