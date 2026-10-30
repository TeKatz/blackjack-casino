# Midnight Dice — casual solo dice table

Midnight Dice is registered under Table games in the casino lobby and is hosted as a
UserControl inside the existing main window. It creates no game window or HWND.
This first version is a complete 13-turn solo score game. Opponents, entry fees,
prize money are deferred until their economy and opponent design have been agreed. It does not debit or credit casino cash or
cosmetic Coins.

## Playing

- Roll five dice, then optionally hold or release individual dice before rolling
  the others again. Each turn permits at most three rolls. You can score after
  the first or second roll, too.
- After each roll, every unused scoring box shows its actual possible score.
  Positive candidates glow green; zero candidates remain available to scratch.
  A large straight also highlights small straight, and a four/five-of-a-kind
  also satisfies the lower matching sets. Filled boxes cannot be overwritten.
- Click a box to select it, then **Record score** to commit the displayed points or
  **Scratch** to commit zero. Selection alone never changes the score.
- The next turn clears all dice and holds. Filling the 13th box ends the game;
  final upper/lower totals and bonuses remain visible.
- **R** rolls; **1–5** hold/release individual dice; **Escape** returns to the
  casino (or cancels the new-game confirmation). Tab/Enter work on all buttons.
- Starting over during a game requires confirmation inside the game surface.

## Scoring contract

| Box | Points |
| --- | --- |
| Ones through Sixes | Sum of dice showing that face |
| Three / Four of a kind | Sum of all five dice if at least 3 / 4 match |
| Full house | 25 for exactly three of one face and two of another |
| Small straight | 30 for four consecutive distinct faces; the fifth can duplicate |
| Large straight | 40 for five consecutive distinct faces |
| Five of a kind | 50 for five equal dice |
| Chance | Sum of all five dice |
| Upper bonus | 35 once the upper subtotal reaches 63 |

The rule baseline for further Kniffels is the current German classic
[Schmidt 49030 instructions](https://www.schmidtspiele.de/files/Produkte/4/49030%20-%20Kniffel/49030_Kniffel_DE.pdf),
not Yahtzee's 100-point variant: each subsequent five-of-a-kind used to finish a
turn grants 50 extra points and permits the maximum in any unused box. A first
five-of-a-kind counts when committed even if used in another box. Holding or
rerolling an unchanged Kniffel within the same turn cannot award it twice. Bonus
points are committed with the scoring action, never during a preview/animation.
Free voluntary scratching is supported as an explicit casual-table rule, even
when a positive entry is possible. Scratching changes the box to zero; a further
Kniffel's separate 50-point bonus still applies.

## State and presentation

`Core/Kniffel/KniffelGame.cs` is the UI-independent authority for legal actions,
dice generation, evaluation, scoring and turn transitions. Dice use
`RandomNumberGenerator.GetInt32(1, 7)`; tests inject a deterministic source.
Returned dice/holds are read-only, and snapshots copy all collections.

A versioned checkpoint lives in `AppPreferences.Kniffel` in the existing
player-state file. Every roll, hold and score updates it; returning to the lobby
or restarting the application preserves dice, holds, used rolls and completed
boxes. Malformed Kniffel checkpoints reset only this game, not the bankroll.

The outcome is generated and checkpointed before cosmetic animation. Only
unheld dice tumble and cycle faces; each settles to the committed result with a
slight stagger. Rolling, holding and scoring are locked during that animation.
Leaving mid-animation stops the timer and resumes the already committed roll.
Unloading/disposal also stops the timer. The existing animation preference is
respected. Candidate glows have short, bounded pulses; held dice have a mint rim
and a text label. Scoring is never conveyed by colour alone.

The felt table and paper sheet share a uniformly scaling 1160×748 design, keeping
all 13 boxes visible at the supported minimum window size without scrolling.
No runtime artwork downloads or external assets are needed.

## Validation

- Known-hand edge cases and exhaustive 7,776 ordered rolls, checked against
  combinatorial frequencies and permutation invariance.
- Held/released dice, all-held guard, first-roll scoring, three-roll limit,
  scratching, occupied boxes, complete games and exact upper-bonus threshold.
- Further Kniffel maximum previews and once-per-turn bonus settlement.
- Checkpoint round trips, deep-copy isolation, invalid data normalization and
  transactional rejection of invalid injected dice.
- `tools/StartupSmoke/KniffelSmoke.cs` exercises the actual hosted WPF surface:
  roll/hold/reroll, selection/confirmation/glow, a 13-turn game, resume, animated
  settlement, mid-animation exit, restart cancellation and one-window hosting.
  CI uploads screenshots at 1080×720 and 1280×900 with the existing startup smoke
  evidence.


## English presentation, audio and completion rewards

All table labels, status messages, tooltips and accessibility names are English.
The public name is **Midnight Dice**; the internal `kniffel` lobby/save IDs remain
unchanged for compatibility. The five-equal-dice box is labelled **Five of a kind**.
The existing German-classic scoring contract is preserved, including its 50-point
additional-five-of-a-kind rule; renaming the table does not substitute Yahtzee's
rules. Hasbro identifies Yahtzee as its trademark:
https://instructions.hasbro.com/en-in/instruction/yahtzee-classic

**A quiet roll** is a separate original 68 BPM, 64-beat lounge composition with
warm electric-piano-style chords, sparse melodic plucks, soft bass and restrained
percussion. `DiceLoungeSynthesis` renders it once on a background thread through
the existing soundtrack cache. Circular room reflections and a corrected boundary
keep the loop continuous. Existing sampled tracks are unchanged. Host music is
muted while the dice table is open and restored on return. The dice-roll cue uses
quiet, short felt/wood taps and plays once per actual roll, including when visual
animations are disabled. Existing music/sound switches and music volume apply;
leaving the table stops its music and effect owners.

A completed 13-box game grants **200 + 2 × score player XP** and
**100 + score Season XP**. Level and Season rewards use the shared progression
service; dice games do not increment Blackjack/slot statistics or grant direct
casino cash. For example, 223 points earns 646 XP and 323 Season XP. Completion
feedback appears in the existing status line without changing the table layout.

Every saved game has a stable GUID. XP, Season rewards and the GUID receipt are
persisted together in an atomic profile replacement before updating the live
profile. Reopening the result or restarting the application cannot award twice.
A failed profile write leaves the reward retryable. Existing saves migrate their
GUID before claiming rewards, so an already-completed pre-XP game can receive
its completion reward once. Starting over mid-game grants no completion reward.

Additional tests cover durable reward receipts, restart/reopen deduplication,
failed-write rollback/retry, old checkpoint migration, maximum-level XP, music
phrase length/stereo/level/seam, quiet dice effects, and the English hosted table
with visible completion XP in the actual Windows startup smoke.
