# Midnight Descent polish: tavern, character select, directional sprites and boss entrances

This update deliberately preserves the existing tavern contracts, forge offers, loadout balances, boss phase gates, and permanent progression.

## Selection without UI flashes
Clicking another hero or starting set now changes only selection styles, descriptions, stats and button state. The eight card widgets, navigation, background and page alpha/tweens are **not recreated**. Page transitions still animate when navigating between genuinely different menu pages. Scene smoke asserts original card identity and untouched page tween after switching between unlocked and locked sets.

## Tavern: seating and lighting
The player is gently lowered into a bespoke cropped torso pose, with bent legs and feet on the chair rail; the previous 6px hop and standing sprite intersecting the bar are removed. The stool cushion is physically positioned in front of the counter. The card-table gamblers have coherent lower legs, knees, clothing seams and grounded chairs. A distinct regular sits on the fifth stool and raises a foamy tankard to his lips on a **6.5-second loop**; this stool is deliberately unavailable to the player.

All three wall sconces have matching solid collision volumes registered in the model. Brushing against one does not allow walking through it and triggers a briefly damped **pendulum swing**, including the light source, while the mounting rail remains stationary. Lantern motion is per-instance and reset on new expeditions.

## Keeper and forge cards
The plain multiline option buttons are replaced by raised, tinted, structured offer cards. Each shows its own icon, title, benefit/objective and reward or cost; percentages, counts and other important values receive colored **bold** emphasis. The **one per run** constraints and existing contract/forge logic remain unchanged. Confirmation panels use matching surfaces and clear consequences.

## Boss entrances and balance
Boss HP now uses **2.85x** base HP multiplier instead of 1.5625x, an approximately **82.4% increase** over the prior patch, for all three bosses. Phase thresholds remain at 68% and 32%. The opening invulnerability interval rises to 3.25 seconds. Upon the arena trigger, player input is temporarily held and the camera pans from the player to the actual boss, holds with a subtitled cinematic letterbox/name card, then returns before combat begins. Shutter collision and its audio still advance during the intro.

## All four playable characters
Warden, Dealer, Arcanist and Soulbinder now have **eight actual left-facing and eight right-facing profile frames** on rows 3/4 of their existing 256×128 atlases. Side views feature class-specific equipment (shield/sword, cards/hat, glowing staff, hood/scythe), one visible eye/profile, leg motion, attack and hurt frames. They are not duplicate front frames. Up/down frames remain intact.

## Validation
Godot scene smoke checks loadout switching without page rebuild, premium tavern offer cards, sprite atlas imports and boss camera focus/return. Pure model tests exercise collision/swing resets, reserved bar patron seat, actual stool sit/stand transitions, boss HP scaling, and the phase-lock/boss entrance.

## Corrective pass after gameplay feedback (12 November 2026)

The first polish patch accidentally committed an old copy of `pixel_world.gd` over the newly authored tavern art. That reverted **all four** visual changes (seated hero, card gamblers, swaying lamp cage, and drinking regular) while keeping related model state and tests. This pass restores the actual `draw_seated_player`, `draw_card_guest`, `draw_bar_regular`, and `draw_lantern_cage` implementations and checks they exist in the real scene's readiness smoke; these are not just model-only behavior tests.

**Seating:** a cropped up-facing torso eases downward onto the stool cushion with bent leg and boot shapes at the correct depth, rather than shifting the ordinary standing sprite up through the bar. Players sit facing the counter. Both card-table guests now have visible knees, seat contact, and boots below the table. The patron at the fifth stool is drawn in the visible tavern scene on every frame and raises a real mug in a looping sequence.

**Lanterns:** the three fixtures are **not physical walls**. Moving through their head-height proximity volumes produces a single impulse, followed by a damped angular cage swing and matching moving point light. The mounts stay attached to the wall, the cage draws in front of the passing hero, and contact re-arms only after leaving the soft trigger. Dedicated tests prove passing through them, registering the swing, decaying the impulse and resetting contact.

**Boss intro:** the old opaque 490×133 screen-center reveal box was still drawn simultaneously with the new cinematic and blocked the boss. It is now removed. The cinematic keeps its camera pan, letterbox and boss name at the bottom; the usual top boss HP/name banner stays hidden during the reveal and returns when fighting starts.

**HP:** all three boss health pools use **4.5× base HP** (up from 2.85× in the first patch, a further **57.9% increase**); phase gates still begin at 68% and 32% health.
