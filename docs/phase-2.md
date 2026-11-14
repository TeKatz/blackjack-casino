# Phase 2 — Playable table

**Historical development phase.** The feature work below was part of the original casino build-out. The current application is the native WinForms/WebView2 casino, not the WPF-era interface described in some early checkpoints. See the [product overview](../README.md) and [current architecture](architecture.md).

Phase 2 turns the Phase 1 foundation into an actual offline blackjack game.

## First playable slice

- casino-table desktop UI;
- persistent local play-money bankroll;
- chip selection and betting;
- shared six-deck shoe across rounds;
- dealer hole-card conceal/reveal;
- Hit, Stand, Double, Split and Late Surrender controls;
- Insurance decisions;
- keyboard shortcuts;
- split-hand rendering and active-hand highlighting;
- result and payout display;
- bankroll-aware action gating (Split/Double are unavailable if the additional wager cannot be covered);
- the Phase 1 Stable/Development updater remains available from the table UI.

## Phase 2 follow-up

The first playable slice intentionally prioritizes correct gameplay over presentation polish. Follow-up work includes:

- card dealing and chip animations;
- sound effects and volume controls;
- table/rule configuration UI;
- more card and felt themes;
- session statistics and hand history;
- optional AI seats sharing the same shoe;
- richer dealer presentation;
- pause/settings overlay and display options.
