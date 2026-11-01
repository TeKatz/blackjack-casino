# Tales from the Backroom — adventure engine

`BlackjackCasino.Core.Adventure` is a reusable, data-driven tabletop text-adventure layer for the Chill Lounge. The first playable vertical slice is **The Black Stag**.

## Architecture

The core rule is: **content is data; systems are code**. Adventure scenes do not own WPF behavior. The desktop client renders whatever scene and pending check the core engine exposes.

The initial engine provides:

- versioned adventure definitions and run saves;
- reusable character sheets with six abilities, trained skills, HP, armor and character XP;
- conditions for world flags, inventory, stats, skills and NPC relation;
- effects for flags, inventory, NPC relation, HP and character XP;
- explicit d20 checks with normal, advantage and disadvantage modes;
- natural 1 / natural 20 outcomes and optional critical routes;
- fail-forward success/failure routing;
- conditional choices and multiple endings;
- a catalog boundary so later adventures can be added without changing the story UI;
- `AdventureContentJson` for validated JSON import/export of adventure packages.

## Desktop flow

The Lounge tile opens an adventure hub. Starting a run chooses a class preset and creates a versioned `AdventureRunState`. The play surface is split into story content on the left and a permanent character sheet on the right.

When a choice requires a check, `AdventureEngine.SelectChoice` stores a pending choice instead of rolling automatically. The character sheet highlights the relevant ability/skill and activates the D20 tray. Only clicking the die resolves the check.

The D20 reuses `PlayerProfileState.EquippedDiceSetId` and the existing Dice Set visual variants. Dice cosmetics remain appearance-only.

## Adding an adventure

1. Give the adventure a stable id and positive content version.
2. Define scenes with stable scene ids.
3. Express availability through `AdventureCondition` rather than UI-specific checks.
4. Express mutations through `AdventureEffect` rather than scene-specific code.
5. Route checked choices through `AdventureCheckDefinition` success/failure destinations.
6. Mark ending scenes with `IsEnding` and a stable `EndingId`.
7. Serialize or load the definition through `AdventureContentJson` when external content is desired.
8. Run `AdventureDefinitionValidator.Validate` and add engine tests for important branches.

`AdventureContentJson` uses camel-case JSON, string enum values, comments and trailing commas for author-friendly packages. Deserialization always runs the same graph validator before content can enter the engine.

The model also includes localization-key slots on scenes and choices so story text can move to external localized content without changing the engine contract.

## Planned extensions

The current contracts are intentionally prepared for additional systems rather than hardcoding The Black Stag. Natural next layers are encounter/combat definitions that reuse the same checks/effects, richer equipment and conditions, quest/objective tracking, deeper NPC knowledge/state, save migrations and an internal authoring/editor surface.
