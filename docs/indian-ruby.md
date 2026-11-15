# Indian Ruby — first playable desktop slot

Indian Ruby is an elephant-focused five-reel, three-row, ten-line video slot in the offline play-money casino. It opens from the **Indian Ruby** subentry directly beneath **Slots** in the lobby's left navigation; it has **no lobby tile**. The existing lobby tile art, layout and PixiJS scenes remain unchanged.

## Current game

- Paid spins share the existing native casino bankroll. There are nine selectable total-wager steps from 1 to 1,000 play-money credits.
- Ten left-to-right lines pay independently. Each line receives one tenth of the total spin stake. Ruby substitutes for ordinary pay symbols and doubles a line win.
- At least two Elephant symbols anywhere on the original 5 × 3 grid trigger the Elephant Bonus. Existing Elephant and Ruby cells are held. All other cells receive one special respin, drawing only Elephant, Ruby or blank; the feature awards Elephant-line payouts. Base payouts and feature payouts are combined before the native result is shown to React.
- The first-pass cabinet has a sunset palace, its own rolling and win presentation, a passing herd, original synthesized elephant/trampling cues, mute/animation switches, and English/German labels. Reduced-motion preferences disable the herd movement.

**Prototype limitation:** The maker's proprietary reel strips, weight mapping, exact RTP and licensed sounds/art are not included. The prototype's symbol weights, multipliers and soundtrack are independent initial tuning, even where public paytable conventions are followed. The passing herd and elephant symbols are currently lightweight placeholders intended for future artwork and sampled audio.

## Architecture and state ownership

- C# Core: `IndianRubySession`, `IndianRubyRules` determine every base/bonus outcome and payout before presentation.
- NativeShell: the `indian-ruby.html` WebView2 origin and strict versioned command session accept only `spin`, `bet.set`, `sound.set`, and `animations.set`. Duplicate request IDs return prior receipts and cannot debit twice during one native session.
- React: `frontend/src/indianRuby` only animates committed results; no browser RNG or payout calculations.
- `PlayerProfileState.IndianRuby` keeps separate spins and payouts. Existing profile fields and slot stats remain intact. The standard native persistence path saves this state and the shared bankroll.

## Acceptance checks

1. From the lobby, select the indented Indian Ruby subentry under Slots; verify no extra tile appears in the grid and existing tile animations still work.
2. Spin with valid and invalid balances; inspect shared bankroll after leaving and reopening the mode.
3. Trigger two elephants with a test seed / repeated spins; confirm all elephant and ruby cells hold, then a herd traverses the background and remaining cells change to feature-only symbols.
4. Test sound and animation settings, reduced motion, the ten-line payouts, and the Back/ESC navigation.
5. Run C# unit/contract tests, frontend typecheck/Vitest, release packaging and the NativeShell WebView2 smoke on Windows; visually review the cabinet at several DPI scales.

The result is a first playable, not a mathematically verified retail clone.
