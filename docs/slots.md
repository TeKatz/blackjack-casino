# Midnight Reels

**Midnight Reels** is a classic three-reel, single-payline slot machine in The Midnight Club. It opens inside the local React/WebView2 casino and uses the shared play-money bankroll.

## How to play

- Pick a stake from the available bets and spin using the on-screen controls or **Space**.
- Each reel draws from a weighted 32-stop symbol strip; the **middle horizontal line** determines the result.
- Matching symbols and the special two-Cherries result pay according to the in-game paytable. The C# engine, not the animations, chooses the stops and payout.
- The last stake, player statistics, sound and animation settings persist across sessions.

## Fair settlement and recovery

`ModernReelsProtocol` accepts only validated requests from the local `reels.html` page, with strict page origin, request ID and session revision. Each settled spin updates the native bankroll and progression exactly once, before any reel animation displays it. Repeated browser messages cannot trigger a second payout.

If the local browser fails, NativeShell offers its native recovery/lobby flow. **There is no WPF slot or automatic legacy-game fallback.**

## Testing

Automated regression covers weighted stops, payouts, insufficient funds, replay protection, updates to shared progression and native WebView2 routing. On Windows hardware, review reel alignment, mouse/keyboard interaction, sounds, DPI, accessibility options and save/reopen.

The full original paytable analysis and earlier renderer/host transition are in the [historical Reels notes](archive/midnight-reels-history.md). See [presentation checks](presentation-regression.md) and [architecture](architecture.md).
