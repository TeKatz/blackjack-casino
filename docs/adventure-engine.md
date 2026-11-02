# Tales from the Backroom — adventure engine

`BlackjackCasino.Core.Adventure` is a reusable, data-driven tabletop text-adventure platform for the Chill Lounge. **The Black Stag** is the first full campaign built on it, but the engine and desktop surface are intentionally adventure-agnostic.

## Architecture

The core rule is: **content is data; systems are code**. Adventure scenes do not own WPF behavior. The desktop client renders the current definition and run state exposed by the core engine.

The engine provides:

- versioned adventure definitions and versioned run saves;
- reusable character sheets with six abilities, trained skills, HP, armor and character XP;
- stable acts, scenes, locations, choices and endings;
- world flags, NPC relation state and generic integer counters;
- stackable inventory items with item categories and reusable icon keys;
- adventure-local coins and spend requirements;
- timed and permanent conditions with reusable definitions;
- condition-driven roll changes such as disadvantage;
- periodic condition effects such as Bleeding HP loss;
- item/condition/coin/HP feedback events for presentation layers;
- explicit d20 checks with normal, advantage and disadvantage modes;
- natural 1 / natural 20 outcomes and optional critical routes;
- fail-forward success/failure routing;
- conditional choices and multiple endings;
- a catalog boundary so later adventures can be added without changing the story UI;
- `AdventureContentJson` for validated JSON import/export of adventure packages.

## Blueprint boundary

An `AdventureDefinition` can provide all content needed by a completely separate adventure:

- `Scenes` — story graph and routes;
- `Acts` — chapter labels and navigation context;
- `Items` — names, descriptions, categories, stack limits and icon keys;
- `Conditions` — names, descriptions, icon keys, duration semantics and mechanical hooks.

Runtime state remains separate in `AdventureRunState`. A campaign therefore does not need to add fields to the engine for a new key, potion, clue, temporary status or investigation counter.

Use:

- `AdventureConditionKind.Flag` for boolean world state;
- `ItemOwned` for inventory-gated choices;
- `ActiveCondition` for condition-gated choices;
- `CoinsMinimum` for purchases;
- `CounterMinimum` for generic progress such as collected clues;
- `NpcRelationMinimum` for relationship-gated branches.

Use `AdventureEffect` for mutations rather than modifying runtime state from UI code. Supported reusable effects include inventory changes, coins, counters, NPC relation, HP, XP and condition application/removal.

## Inventory and condition presentation

The desktop play surface has three layers:

1. story and choices;
2. permanent character sheet and D20 tray;
3. a full-width adventure HUD containing **Pack & Coins**, **Conditions**, and the most recent event feedback.

Items are rendered from their adventure definition rather than a Black-Stag-specific switch. Quest items, consumables, utility items and relics can use different visual accents while sharing the same engine state.

Conditions render their remaining duration and description. Applying or removing an item/condition creates a presentation event, allowing the UI to animate changes without putting WPF concerns in the core engine.

`AdventureDurationUnit` currently supports `Scene`, `Turn` and `Permanent`. Story navigation advances scene durations. A future combat layer can call the same condition-advance API for turn durations.

## Dice flow

When a choice requires a check, `AdventureEngine.SelectChoice` stores a pending choice instead of rolling automatically. The character sheet highlights the relevant ability/skill and activates the D20 tray. Only clicking the die resolves the check.

An active condition can alter the pending roll mode without the story needing special-case code. For example, a condition configured with `DisadvantageOnAbilityChecks` changes a normal check to disadvantage and cancels advantage back to normal.

The D20 reuses `PlayerProfileState.EquippedDiceSetId` and the existing Dice Set visual variants. Dice cosmetics remain appearance-only.

## The Black Stag v2

The first campaign is now a five-act dark-adventure blueprint rather than a short engine demo:

1. **Act I — The Letter**
2. **Act II — The Missing Names**
3. **Act III — The Broken Antlers**
4. **Act IV — The Hunt**
5. **Act V — The Crown**

The campaign demonstrates investigation hubs, optional routes, clue counters, purchases, keys, potions, rope, conditions, NPC relationship state, class-specific choices, multiple infiltration routes, fail-forward checks and multiple endings.

The wood elf **Isi** is a central NPC whose trust and personal storyline can materially alter routes and ending choices. The Black Stag remains grounded as a covert courier/witness network while the origin of its older stag symbol deliberately retains a darker supernatural ambiguity for future adventures.

## Adding a separate adventure

1. Give the adventure a stable id and positive content version.
2. Define stable acts and scene ids.
3. Define adventure-specific items and conditions in the content package rather than in WPF.
4. Express choice availability through `AdventureCondition`.
5. Express mutations through `AdventureEffect`.
6. Route checked choices through `AdventureCheckDefinition` success/failure destinations.
7. Prefer fail-forward routes over dead-end failures.
8. Mark ending scenes with `IsEnding` and a stable `EndingId`.
9. Serialize or load the definition through `AdventureContentJson` when external content is desired.
10. Run `AdventureDefinitionValidator.Validate` and add engine tests for important routes and mechanics.

`AdventureContentJson` uses camel-case JSON, string enum values, comments and trailing commas for author-friendly packages. Deserialization always runs the same graph validator before content can enter the engine.

The model includes localization-key slots on scenes and choices so story text can move to external localized content without changing the engine contract.

## Planned extensions

The next natural engine layers are encounter/combat definitions that reuse the existing checks/effects/conditions, richer equipment actions and consumable use from the HUD, quest/objective tracking, deeper NPC knowledge state, save migrations and an internal authoring/editor surface. These should extend the shared blueprint rather than introduce campaign-specific runtime code.
