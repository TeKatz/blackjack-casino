# Sunset Ruby — first playable desktop slot

Sunset Ruby is an elephant-focused five-reel, three-row, ten-line video slot in the offline play-money casino. It opens from the **Sunset Ruby** subentry directly beneath **Slots** in the lobby's left navigation; it has **no lobby tile**. The existing lobby tile art, layout and PixiJS scenes remain unchanged.

## Current game

- Paid spins share the existing native casino bankroll. There are nine selectable total-wager steps from 1 to 1,000 play-money credits.
- Ten left-to-right lines pay independently. Each line receives one tenth of the total spin stake. Ruby substitutes for ordinary pay symbols and doubles a line win.
- At least two Elephant symbols anywhere on the original 5 × 3 grid trigger the Elephant Bonus. Existing Elephant and Ruby cells are held. All other cells receive one special respin, drawing only Elephant, Ruby or blank; the feature awards Elephant-line payouts. Base payouts and feature payouts are combined before the native result is shown to React.
- The first-pass cabinet has a sunset palace, its own rolling and win presentation, a passing herd, original synthesized elephant/trampling cues, mute/animation switches, and English/German labels. Reduced-motion preferences disable the herd movement.

**Prototype limitation:** The maker's proprietary reel strips, weight mapping, exact RTP and licensed sounds/art are not included. The prototype's symbol weights, multipliers and soundtrack are independent initial tuning, even where public paytable conventions are followed. The symbol artwork uses authored SVG, the animated herd uses PixiJS graphics, and the sound effects use original Web Audio synthesis. The illustrations and sounds are independent of the retail slot and can be replaced by commissioned, licensed art/audio later.

## Premium visual and motion pass

- The cabinet uses layered brass, red gemstone inlays, animated lamp rails, glass glints, ambient dust and refined gold-and-claret typography. Every illustration uses authored scalable paths rather than emoji art.
- `RubyReelStage` is a memoized five-column compositor: each reel uses a deterministic, presentation-only symbol strip and Web Animations API easing with staggered stops, overshoot and bounce. The result is *always* the native C# output.
- When two Elephant symbols occur, anticipation starts as the reels settle. A bonus fanfare accompanies the PixiJS herd across the reel glass. The same canvas then moves to the sunset landscape behind the cabinet, where the herd keeps walking until the feature settles. Held Elephant and Ruby cells never spin, and all final symbols and monetary totals come from the already committed result.
- The win panel counts up the committed payout while winning lines and cells glow; no UI animation submits a second spin.
- `BetSelector` stays mounted through every host revision, with stable dimensions and tabular numerals. Native bet acknowledgements no longer briefly dim the entire control desk or reset reel artwork, addressing the prior flicker.
- Sound cues include original layered elephant/brass, heavy footsteps, soft mechanical stops, bonus, anticipation and variable win sounds. Sound and reduced-motion switches are respected without changing odds or wagers.
- The reel subtree is independent from transient bet and settings state; presentation-only animations are cancelled on unmount and have guarded completion fallbacks.

## Architecture and state ownership

- C# Core: `IndianRubySession`, `IndianRubyRules` determine every base/bonus outcome and payout before presentation.
- NativeShell: the `indian-ruby.html` WebView2 origin and strict versioned command session accept only `spin`, `bet.set`, `sound.set`, and `animations.set`. Duplicate request IDs return prior receipts and cannot debit twice during one native session.
- React: `frontend/src/indianRuby` only animates committed results; no browser RNG or payout calculations.
- `PlayerProfileState.IndianRuby` keeps separate spins and payouts. Existing profile fields and slot stats remain intact. The standard native persistence path saves this state and the shared bankroll.

## Acceptance checks

1. From the lobby, select the indented Sunset Ruby subentry under Slots; verify no extra tile appears in the grid and existing tile animations still work.
2. Spin with valid and invalid balances; inspect shared bankroll after leaving and reopening the mode.
3. Trigger two elephants with a test seed / repeated spins; confirm all Elephant and Ruby cells hold, watch the herd cross the reel window once, then verify it keeps walking behind the cabinet while free cells respin and disappears when the result settles.
4. Test sound and animation settings, reduced motion, the ten-line payouts, and the Back/ESC navigation.
5. Run C# unit/contract tests, frontend typecheck/Vitest, release packaging and the NativeShell WebView2 smoke on Windows; visually review the cabinet at several DPI scales.

The result is a first playable, not a mathematically verified retail clone.

## Sunset Ruby motion and controls update

- The first actual spin animates: initial no-result snapshots are observed by session identity before any committed result is restored. Existing saved outcomes are displayed without replaying a spin.
- The only player-facing title is the glowing **Sunset Ruby** header; the cabinet marquee presents a subtitle instead of a second game name. Internal `indian-ruby` identifiers, profile saves and WebView2 routes are retained to preserve installed player data and update compatibility.
- 10/25/50/100 auto-spins execute strictly sequentially, waiting for each native result, animation, balance check and receipt; any failed acknowledgement, unavailable bet, manual stop, modal opening or navigation ends the sequence. Optional stop-on-bonus is enabled by default.
- Normal, Fast and Turbo scale base reel duration. The Elephant Bonus retains a long presentation across every speed; near misses slow only the actual outcome's final reel and do not manipulate C# symbols, winnings, odds or RNG.
- The rules dialog lists the complete per-line C# multipliers, per-position symbol chances on outer and middle reels and exact prototype probability of two or more elephant symbols; these data are projected from the native C# rule definitions and explicitly differ from unverifiable proprietary retail odds.
- Win/loss feedback uses **net** payout: a negative-net spin flashes red even if a smaller line payout occurs, while positive-net flashes green. The frame, 18 lamps on each rail, bankroll and a decorative credit transfer respond, but displayed balances reconcile to the native ledger.


## Screenshot follow-up: header, wallet and win-field polish

- The title lock-up is three deliberate stacked rows: collection eyebrow, one premium Sunset Ruby heading and subtitle. This removes the inline flow that pushed the eyebrow into the heading.
- The header bankroll is a compact, right-aligned fixed-width instrument, rather than filling the entire responsive grid column.
- Only positive **net** payouts spawn the green flight from the win display into the bankroll. Negative net payouts count down and flash red **inside** the header bankroll, with a brief local deduction badge; no loss flies across the reels or screen.
- The wager panel keeps its stable memoized React component and gains an engraved glass amount, brass increment/decrement keys and nine static stake markers. Disabled states do not dim/recreate the entire panel.
- Winning reel cells get independent light borders, reflective gold sweeps, accent sparkles and a subtle symbol lift. Animations are decorative and never alter native outcomes. Reduced-motion preferences retain static highlights without loops.
- The Windows WebView2 regression now verifies nonoverlapping title rows, bounded wallet width and the stable nine-marker wager UI. Vitest ensures glints appear only on winning cells.
