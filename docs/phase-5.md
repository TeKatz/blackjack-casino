# Phase 5 — Table Rules & Casino Setup

Phase 5 expands Blackjack Casino from one fixed rule set into a configurable blackjack table without compromising the default laid-back single-player flow.

## Design principle: Casual Solo stays first-class

A new installation still opens on a single-player Solo table. No setup screen is required before the first hand.

The default **Casual 3:2** preset keeps the original relaxed rules:

- 6 decks
- dealer stands on soft 17 (S17)
- 3:2 blackjack
- US hole card with dealer peek
- insurance
- late surrender
- double on any first two cards
- double after split
- up to four split hands
- $1 minimum / $1,000 maximum play-money bet

AI seats remain optional through the existing Solo / Full Table selector and are independent from the selected rules preset.

## Presets

Phase 5 includes the following starting profiles:

- Casual 3:2
- Las Vegas Strip
- Downtown Vegas
- Atlantic City
- European No-Hole-Card
- Single Deck
- Custom

Casino-named profiles are representative game configurations, not claims that every real casino in those regions uses identical rules.

## Advanced custom rules

`Advanced table setup…` is available between hands and can configure:

- 1 / 2 / 4 / 6 / 8 decks
- S17 / H17
- US hole card or European no-hole-card (ENHC)
- dealer blackjack peek
- insurance
- late surrender
- blackjack 3:2 / 6:5
- double on any two / 9–11 / 10–11
- double after split
- resplit aces
- hit split aces
- maximum split hands
- shoe penetration
- table minimum and maximum bet

Custom rules and the chosen preset are persisted in the existing local player settings file.

## Core engine changes

Rule legality remains owned by `BlackjackCasino.Core`, not by the UI. This includes double restrictions, surrender availability, split rules and dealer hole-card timing.

ENHC tables defer the dealer's second card until all player/AI actions have completed. This means a late dealer blackjack can defeat doubled or split wagers, matching the core no-hole-card risk model.

## History groundwork

Each completed `CasinoSession` round now creates an in-memory `RoundHistoryEntry` containing:

- start/completion timestamps
- bankroll before/after
- initial wager and net result
- rule snapshot
- dealer cards
- player hands/results
- player action sequence
- AI hands/results

Phase 6 can persist and visualize this data without changing the round engine again.
