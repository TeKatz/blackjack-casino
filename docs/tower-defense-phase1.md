# Midnight Bastion — Tower Defense phase 1

## Current gameplay

Midnight Bastion is the seventh Chill Lounge activity. Its featured custom tile is visible under All Activities and Strategy, inside the existing offline WebView2 host.

Phase 1 implements one 960×600 battlefield with a fixed road, 10 build pads and a fortress gate; three towers (Archer, Frost, Cannon) with three levels each; four enemy classes (grunt, runner, brute, shield); 15 manually started waves; build, upgrade, sell, pause/resume, 1×/2× speed, restart, and victory/defeat states. UI strings are available in German and English.

The native game has a separate, wager-free building economy. Casino balance, shared XP, Season points and achievements are not awarded in phase 1. There are no entry fees, online services, multiplayer features or dynamic roadblocking.

## Architecture

The authoritative fixed-step combat, economy and victory logic lives in src/BlackjackCasino.Core/TowerDefense/TowerDefenseGame.cs. The native service and intent protocol live in src/BlackjackCasino.Desktop/ModernTowerDefenseService.cs and ModernTowerDefenseProtocol.cs. The frontend cannot send enemy health, damages, or coin amounts, only an allowlisted user action with a session and revision. Duplicate or stale requests are rejected or replayed without a second mutation.

The frontend displays native-verified state in the PixiJS battlefield and React controls under frontend/src/lounge/towerDefense.tsx. Cosmetic movement interpolation never changes outcomes. Tower Defense state is saved in the existing AppPreferences record; accepted commands are persisted immediately, and running waves checkpoint at most every 2 seconds. A further checkpoint is made when closing the Lounge. Loading an in-progress battle pauses it.

## Manual acceptance on Windows Development channel

1. Build the frontend and desktop application. Run tests on CI.
2. Confirm featured Midnight Bastion art, text and Strategy filter, including EN/DE.
3. Enter the game and confirm one map, 10 pads, 320 starting coins and 20 lives.
4. Build an Archer for 70 coins. Attempt a duplicate build, upgrade and sell; verify arithmetic and receipts.
5. Start wave one and verify movement, targeting, health bars, bounty, and wave-end bonus.
6. Pause, resume, toggle 2× and confirm a paused game does not move.
7. Exit and reopen; the wave and towers must resume paused with the save intact.
8. Try a full victory and defeat, restart with confirmation, and verify the shared casino bankroll does not change.
9. Recheck all six preexisting Lounge games and their save states.
10. Check rendering, input, memory and behavior on lower-resolution Windows displays.

Automated regression tests include tests/BlackjackCasino.Tests/TowerDefensePhaseOneTests.cs and frontend/tests/loungeBridge.test.ts, plus the original Modern Lounge catalog tests.

## Phase-1 limitations

The scene uses original generated vector shapes rather than dedicated animated art. There is one fixed path, ten build pads and three defensive towers. Additional maps, enemy attack animations, sound design, achievements, advanced balancing and high-load optimization are future expansions. A green CI result does not replace visual device acceptance.
