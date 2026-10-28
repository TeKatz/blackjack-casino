# Phase 3 — Full Table

Phase 3 turns the playable single-player table into an optional multi-seat casino table without removing the fast solo experience.

## Play modes

- **Solo** — the player and dealer only. This remains the default and uses the same zero-AI code path as Phase 2.
- **Full Table (AI)** — the player chooses 1–5 AI seats. All occupied seats and the dealer draw from the exact same shoe.

Changing mode or AI seat count is only allowed between rounds. The player's bankroll is preserved when switching modes; changing the table composition starts a fresh shoe.

## AI behavior

AI seats use deterministic multi-deck basic strategy. They can hit, stand, double, split and late-surrender when the configured table rules allow those actions. AI never takes insurance. AI wagers are simulated independently and never affect the human player's bankroll.

The human player occupies first base for now, so the action order is:

1. human player;
2. AI seats from left to right;
3. dealer.

The initial deal is round-robin across every occupied seat and the dealer. AI hits, doubles and split cards therefore consume cards from the same shoe before the dealer draws.

## Phase 3 follow-up

After the multi-seat engine/UI is stable, Phase 3 polish can layer card/chip animations, audio and richer table/rule settings on top without changing the shared-shoe model.
