# Midnight Manor — design and math, revision 2

This is an explicit, revisable balance specification for the first implementation, not inherited final commercial math. Revision 1 preceded UI implementation; revision 2 addresses Windows playtest feedback with a more frequent smaller Manor tier and stronger visual feedback. No real money, accounts, ads or purchasable currency. Blackjack and Midnight Reels stay intact and Casual Solo remains the default. Baseline: main 378a896; all 72 existing tests passed before implementation.

## Architecture and persistence assumptions

Core is pure .NET; WPF only presents committed state. Existing bankroll is a decimal dollar balance; the public between-hand wallet operation preserves the Blackjack shoe/history. Profile XP uses existing grant APIs, never fabricated Blackjack hands. The new machine is a separate session with a serializable pending paid cycle, Free Spins and genuine Manor run state. A write-ahead aggregate checkpoint contains wallet, profile and machine state, is atomically replaced before projecting to existing files, and is replayed on startup if interrupted. Open features resume; closing is not a refund, forced loss or duplicate award.

A shared `SlotPlayController` owns busy/auto/speed semantics. Reels keeps its established presentation in this change; the new abstractions can support it later without a second autoplay implementation. Autoplay is never restarted merely by loading saved preferences. Unlimited means until stopped or funds/stop conditions intervene. Cancellation stops the next spin immediately, never a committed payout.

## Reels, lines and stakes

Five independent 64-stop cyclic strips, three consecutive visible symbols per reel. The strips contain the same symbol counts, with rotation 11 × reel index. Moon at indices 9 and 49, Door at 31, Mirror at 60; remaining positions are filled by cycling the regular symbols with remaining counts in catalog order. Thus a column never contains two Moons, two Doors, or both Moon and Door. RNG uniformly samples [0,64) independently per reel; Manor seed uniformly samples [0,16). No speed, animation, autoplay or interaction setting alters these draws.

Total bet: whole play-money dollars from $1 to $1,000; one twentieth allocated to each of 20 fixed lines. Payouts include returned stakes; losses return zero. Paylines are indexed rows top=0, centre=1, bottom=2:

`11111 00000 22222 01210 21012 00100 22122 11011 11211 01110 21112 10001 12221 01010 21212 10101 12121 02020 20202 02120`

| Symbol | Count | 3 / 4 / 5 matching from left (line credits) |
| --- | ---: | --- |
| Ornate J | 10 | 14 / 36 / 90 |
| Ornate Q | 9 | 18 / 45 / 112 |
| Ornate K | 8 | 22 / 54 / 135 |
| Ornate A | 7 | 27 / 68 / 180 |
| Candelabrum | 6 | 36 / 90 / 270 |
| Pocket watch | 5 | 45 / 112 / 360 |
| Raven | 4 | 68 / 180 / 540 |
| Antique key | 3 | 81 / 225 / 675 |
| Haunted portrait | 3 | 90 / 270 / 900 |
| Black rose | 3 | 112 / 338 / 1125 |
| Lady of the Manor | 2 | 180 / 675 / 2700 |
| Haunted mirror / Wild | 1 | substitutes regular symbols only |
| Manor moon / Scatter | 2 | feature only |
| Manor door / Bonus | 1 | feature only |

For each line evaluate every regular symbol with Wild substitution and pay the highest applicable 3/4/5 award once; all-Wild uses the best regular award. No right-to-left or scatter cash awards. Non-winning lines do not pay.

## Free Spins

On a paid spin, 3/4/5 visible Moons award 8/12/20 free spins at the original total bet. The **Haunted Night** modifier doubles every line win in that session. The same strips/RNG/evaluation apply. Moons and Doors remain visible feature symbols but do not retrigger any feature during Free Spins; this rule is displayed. No extra wagers, XP or Coins per free spin. Free-spin remaining count, original stake and accumulated return survive closing/restarting.

## Escape the Manor

2/3/4/5 Doors start a run with an Exit target of 25×/250×/500×/1000× the triggering total bet. The two-Door visit uses the identical map, seeds and choices, with every prize unit worth 0.1× rather than 1×: initial current/checkpoint 0.5×, room awards and cash-outs scaled by the same factor. This is a trigger tier, never an interaction/skip penalty. Saved older runs default to scale 1 and keep their full awards. Payout-table versions are saved with every cycle and spin: earlier committed awards are validated with their original table, and pending older Free Spins finish at that table. Only a new paid cycle switches to revision 2. Exit replaces (does not add to) the current Manor multiplier. Initial current/checkpoint in the full-prize tiers are 5×/5×. A fixed, connected 19-room graph has multiple real routes, loops, a locked forbidden wing and a key shortcut; the actual Moonlit Gate is room 16. Two off-map approach rooms belong to the ghost. Distinct player areas: Entrance Hall, Library, Ballroom, Servants' Corridor, Attic and Crypt/Forbidden Wing.

Rooms grant their reward/item only once per run (visited bit mask). Corridors and checkpoints increase current reward; checkpoints secure the full current multiplier. Library supplies a reveal candle; Study supplies one protective charm; the main corridor supplies a key. Haunted/possessed rooms accelerate pressure. A key opens the forbidden wing and Crypt shortcut. Current multiplier never exceeds the Exit tier. Cash-out always pays current; caught pays checkpoint. Max 24 turns then the current checkpoint is paid. Using a candle consumes it and reveals exact ghost position for one turn; it does not advance time. Charm repels a catch along one actual adjacent edge and stuns the ghost for one turn. No teleportation or repeated-room reward farming.

The seed determines initial ghost approach room (17 for 0–7, 18 for 8–15), first-two-turn pace (seed parity) and burst phase (seed mod 4). Thereafter it moves one shortest-path edge toward the player, plus a second edge when `(turn + seed) % 4 == 0` or Haunt ≥ 6. Ties use ascending room IDs. Haunt = min(8, turns + haunted-room pressure). The ghost can pass locked edges. It pursues after a player move; reaching the Gate ends first. Distance bands give readable warnings (≤2 close, ≤4 approaching, otherwise distant); exact position is shown by candles. Rules and the risk are visible, no claim of dexterity/skill determining random payouts.

### Interaction and automatic resolution

The same state machine and uniform seeds are used in both modes. The reference policy takes the shortest legal path to the Gate (ascending-ID tie breaks), but takes current reward when the visible "close" warning applies and volatile reward above checkpoint is at least 50 prize units (5× for two Doors; 50× otherwise). A **Follow recommended route** action allows the identical policy interactively. Manual alternative routes/cash-out decisions genuinely change the risk and return; no claim of identical EV for every possible player strategy is made. This strategy dependence is explicit in UI and documentation. Disabling interaction incurs no fee, reduction or arbitrary payout-table replacement.

For reference policy, exact enumeration of all 16 seeds gives average Manor returns 10.875× / 108.75× / 140× / 202.5× for 2/3/4/5 Doors respectively. Interactive play following that policy yields exactly the same result for each seed. Other strategies can differ; these are not hidden modifiers.

## RTP, frequency, exposure

Base line-return expectation is computed exactly by enumerating all 14^5 symbol combinations with integer stop frequencies and the authoritative line evaluator. Since all lines have equal per-reel marginal distributions, dividing the total bet over 20 lines preserves that expectation: **0.5714641995728016** of total wager.

Per reel, P(visible Door)=3/64, P(visible Moon)=6/64. Trigger probabilities are binomial tails over five reels: Manor n≥2 = ~0.0199842341 (~1 in 50.04 paid spins), Free Spins n≥3 = ~0.0071244836 (~1 in 140 paid spins). The old n≥3-only Manor chance was ~1 in 1,043 spins, giving ~72.3% probability of no bonus across 338 spins. The new entry rate is about 20.84 times as frequent. Every spin remains independent; no guarantee or hidden pity adjustment. Three Moons and two Doors can occur together and award both features, with the run resolved before Free Spins. Exact expected free-spin count per paid spin is `sum(binomial(5,n,pMoon) * count[n])`; reference Manor return is `sum(binomial(5,n,pDoor) * expectedRun[n])`.

Full reference-policy theoretical RTP = baseReturn × (1 + 2 × expectedFreeSpinCount) + expectedManorReturn, approximately **95.0228%**. The exact computed report in `docs/midnight-manor-math.md` and executable tests are authoritative over rounded prose. This is a long-run expectation, not a session guarantee or universal manual-strategy RTP.

The revised line table pays at most 2700 credits; a 20-line base spin at most 2700× total bet, and a free spin at most 5400×. We retain the earlier, looser conservative paid-cycle bound of **148600× total bet** (3600 + 20×7200 + 1000) so loading pending older feature cycles never tightens their validation. This is a safety upper bound, not a claim that this combination is jointly reachable. No legitimate theoretical payout is clipped below that bound; each credit is checked against it and arithmetic overflow is rejected before financial mutation. Each paid cycle gets XP exactly once after all its features finish: 20 player/15 Season, plus 5/5 for net profit, regardless of stake, feature count, speed or auto mode.

## Presentation and audio contract

Original scalable Gothic vector art for all 14 symbols, carved wood/brass cabinet, Manor exterior, door and six first-person room variants. Restrained violet/burgundy/silver, candlelight, moonlight, fog and dust. No pumpkin/neon look. Winning cells breathe and sparkle inside bright cyan/gold frames drawn above reel shadows, others dim, every paid line is drawn only through its matching symbols with a dark keyline/bright gold core and moving white trace, and the win counts up; ≥25× big win, ≥100× supernatural win. Wild reveals a figure, Moon pulses, Door zoom/open leads to the same full-surface Manor scene (not a generic modal picker).

Normal/Fast/Turbo only affect durations. Animations off skips decorative delays, never core transitions. Autoplay options Off/10/25/50/100/Unlimited; stop, stop-on-big-win, stop-on-bonus, and minimum-balance settings. Interactive features pause and can resume; noninteractive features resolve before presentation. Idle autoplay always yields to the dispatcher, preventing a disabled-animation tight loop.

Soft synthesized cues (reel start/movement/5 stops, small/medium/big wins, Wild, Scatter, Door, heartbeat, fakeout, trigger, latch, open, distant/near ghost, checkpoint, item, escape/catch, free start/end and maximum exit); ambience layers include wind, fire, clock, wood, occasional thunder/whisper. Existing global sound/music enable and music volume are respected; no system sounds. Global table music ducks while the Manor window is active. No third-party asset dependencies.

Reel stops add a brief moonlight impact and mechanical bounce; spinning reels carry moving light streaks. Wins emit original deterministic particles, with a stronger burst/title swell for big wins and feature entries. The exterior has breathing moonlight, flickering windows, drifting fog/dust, and candle symbols glow. Normal/Fast/Turbo scale these durations. Animations off removes decorative clocks/delays. Both dropdowns own the entire inner toggle template, so hover/open/disabled states cannot paint Windows blue/white chrome; selected text remains readable during spins. Letter symbols are centred by actual glyph ink bounds rather than character-specific offsets.

## Acceptance boundary

Core tests, Windows build and WPF runtime smoke are required. Human audible-level, real-monitor DPI and gameplay acceptance remain gates for merging/Stable release. Development delivery was explicitly requested after initial implementation: `slots/midnight-manor` is now opted into the Development publisher alongside `main`; it builds/tests/runs the Windows smoke before replacing the existing Development assets. Version base is 0.7.0, which also makes this update visible to installed 0.6.5 clients without changing SemVer or allowing downgrades. The canonical package/manifest names and URL remain unchanged. Keep the PR as a draft until human acceptance, and keep all branches until authorized merge/publication checks are complete.
