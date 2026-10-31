# Midnight Dice — casual and casino dice tables

Midnight Dice is registered under Table games in the casino lobby and is hosted as a
UserControl inside the existing main window. It creates no game window or HWND.
The table offers a complete 13-turn casual solo game and a separate paid casino table against three AI opponents. Casual does not debit or credit casino cash; casino entry fees and prizes use the shared bankroll.

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


## Casino table, statistics and achievements

**Game setup** switches between the independently saved Casual sheet and Casino
Table. A paid table seats the player alongside Mara (balanced), Felix (upper
collector) and June (adventurous). All four sheets use the same scoring engine,
three-roll limit and five fair dice. Stakes do not change opponent strength.
After each player entry, opponents take one turn; standings update on the table,
and **View opponents’ score sheets** shows their actual filled boxes and bonuses.
The next player roll stays locked until those turns are committed.

The strategy compares up to 32 hold/stop choices with a bounded expected-value
search using common Monte Carlo samples, scoring opportunity costs and upper-bonus
progress. It uses only its own dice and sheet, never the player's dice or future
random outcomes. Computation runs off the UI thread. Independent saved seat IDs
and turn-indexed random streams make an interrupted AI turn reproducible on resume.

Available entry fees are **100, 500, 2,500, 10,000, 50,000 and 100,000** casino cash.
The displayed gross prize includes any stake return and a fixed place bonus:

| Place | Gross payout |
| --- | --- |
| First | 2.5 × entry + 5,000 |
| Second | 1.1 × entry + 2,000 |
| Third | 500 |
| Fourth | 250 |

These fixed bonuses intentionally reward the time spent on a full game even at
small tables. This is a subsidized play-money tournament, not a closed prize pool.
At high stakes, lower placements can lose most of the entry. Ties share the
combined prizes of their occupied places, rounded down to cents. Shared first
counts as a win. Paying for a new table requires enough cash and cannot replace
an active table. Leaving for the casino pauses the paid game. **Forfeit table**
requires a second explicit confirmation, keeps the lost entry in statistics and
awards neither prize nor completion XP.

Bankroll and casino checkpoint are persisted atomically in `player-state.json`.
The entry is committed before play; a settlement records both the gross prize and
its settled flag in the same write before updating the live session. A duplicate
settlement is a no-op. Failed roll/hold/score checkpoint writes roll the view back
and allow retry. If the player leaves during an AI turn, its result is deferred
until this paid table is resumed, so another game's live bankroll/round cannot be
replaced. Cash is committed before profile statistics; durable entry/result/XP
receipts allow a profile-write failure to recover without charging or paying twice.
An old settled table's statistics must save before a new paid table can replace it.

The statistics Overview now has a Midnight Dice card with completed casual/casino
games, best/average score, casino wins/top-two/rate, forfeits, entry fees, gross
returns, biggest payout and combination totals. Entry fees/net contribute to the
casino-wide money totals, and completed dice games count toward lifetime plays.
Existing best scores/game totals are retained. Combination counts begin with games
completed after those counters were introduced.

There are **20 Midnight Dice achievements** under their own challenge filter:
completion milestones, 200/250/300-point sheets, upper bonuses, five of a kind,
Full House/Large Straight, paid-table completions, wins, top-two finishes, cumulative
entry fees and a large single payout. They use the existing coin rewards,
one-time claims and Claim All flow. Both modes keep the prior player/Season XP
formula; paid games award completion XP only once all opponents finish.

Casino tests cover placements/ties, atomic debit/settlement and failure handling,
legal AI decisions, deterministic recovery, complete matches, forfeit and receipt
idempotence. Windows smoke additionally plays a full paid table, checks resume
without a second fee, opponent sheets, exact payout, repeat-result protection,
confirmed forfeit, the statistics card and filtered achievements.

## Match feedback and result receipt

Recording a five of a kind now shows a short gold-glow banner with the scorer's
name, score-box points and any extra-five bonus, accompanied by a quiet original
five-note chime and a small particle burst. The player's actual matching faces
are shown; AI banners use five matching gold emblems because committed AI
checkpoints no longer retain rolled faces. Only successfully saved entries trigger
feedback. Player and opponent events queue in order and repeated refreshes do not
replay them. Reduced motion keeps the readable banner without movement or particles;
the shared sound setting and volume apply. Open dialogs defer queued feedback.

After the last opponents finish and cash is saved, a match receipt opens after
any queued celebrations. It shows ranked scores for all seats, upper/lower scores,
upper and extra-five bonuses, five-of-a-kind counts, scratched player boxes and
completion XP. The casino receipt separately lists entry paid, gross prize returned
and signed **net win/loss**, plus the current bankroll and any tie explanation.
A forfeited receipt clearly shows the lost entry and no completion XP. Casual
completion has its own score summary without a cash prize.

**Back to score sheet**, **Game setup** and **Return to casino** provide clear next
steps. **Match results** reopens the read-only receipt without awarding cash or XP
again. The Windows feedback smoke uses deterministic five-of-a-kind fixtures to
verify the player and all three named opponents, ordered/deduplicated sounds,
bonus entries in another box, failed-save rollback, reduced motion, dialog
deferral, endscreen timing, tied/loss receipts, minimum-window rendering and cleanup.

## Cosmetic dice sets and presentation refresh

Dice Sets are profile cosmetics bought and equipped through the shared shop.
House Ivory preserves the original appearance; Obsidian Gold, Jade Palace,
Royal Amethyst, Moonstone, Ruby Velvet and Aurora Prism supply distinct palettes.
Only `KniffelDie` drawing colours change. The five die controls, hit targets,
held border, question-mark state, pips, values, roll transforms, timings and
five-of-a-kind feedback continue to use their existing code paths.

The setup screen separates casual play from paid AI tables into two cards. Entry,
prizes, resume and confirmed forfeit retain their existing actions. Ten new
challenges bring Midnight Dice to 30 goals.
