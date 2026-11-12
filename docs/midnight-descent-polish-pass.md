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
