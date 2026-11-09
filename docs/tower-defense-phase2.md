# Midnight Bastion — Phase 2 visual and audio pass

## Delivered presentation

The original Phase 1 15-wave C# rules and the same preferences save remain the source of truth. Phase 2 does not change enemy health, money, win/loss conditions, combat outcomes or persisted schema.

- Eight original locally bundled SVG assets: one layered moonlit battlefield (with road, gate, trees, paving and lighting), three distinct tower illustrations, and four visually unique opponents.
- PixiJS asset loading and cached textures. Tower and enemy sprites are retained by pad/ID rather than recreated each frame. Fallback rendering and an explicit renderer status remain available.
- Visual feedback: live range indicator, level markers, per-enemy health bars, freeze auras, bob/idle animations, actual-shot projectile flights (arrows, frost orbs, arcing cannonballs), impact rings, upgrade particles, defeat/gate flashes and subtle ambient motes.
- An illustrated tower workshop with detailed selected-tower portrait, accurate damage/range readouts, highlighted build choices and deployment progress.
- Local synthesis for build/upgrade/sell, start wave, three attack cues, kill, gate damage, victory and defeat. No licensed music or network sound assets required. WebAudio starts only after a game control is used, obeys the Lounge's Sound Enabled preference and is disposed on exit. Combat SFX are rate-limited.
- Optional animations obey both the host animation setting and reduced-motion preference. Rendering pauses when the WebView2 document is hidden. The C# fixed-step clock is unchanged; cosmetic motion does not change combat results.
- All existing German and English game UI remains functional. The custom Lounge tile and Strategy filter remain intact.

## Engineering notes

The battlefield is in frontend/src/lounge/TowerDefenseBattlefield.tsx. Native effect identity is reconstructed from fixed-tick effects; no changes to the native C# protocol or persistent saves are necessary. A small effect and particle budget prevents unbounded growth, while cached background textures avoid rebuilding static environment details. Local sound is in frontend/src/lounge/towerDefenseAudio.ts.

## Acceptance checklist

1. Update from the Development channel, open Chill Lounge → Strategy → Midnight Bastion, and check the newly illustrated scene.
2. Verify that the attack sounds stay silent until the first game interaction and follow the global Lounge sound preference. Confirm no sound continues after closing the game.
3. Build/upgrade/sell all three tower types and inspect their artwork, rank, and damage/range readouts.
4. Trigger all four enemy types (shield from wave 5, brute from wave 3); inspect spawn, motion, health bars and frost slow rings.
5. Check arrow flights, icy shots and cannon explosions, plus gate damage and wave/victory/defeat FX.
6. Pause and resume, use 2× speed, leave and restore a running battle; check that the combat state and economy do not diverge.
7. Disable animations and enable OS reduced motion; verify that the game remains fully operable with limited FX.
8. Inspect common Windows desktop sizes, fullscreen, resize/DPI, and WebView2 renderer. Confirm assets are packed for offline use.
9. Run frontend Vitest/build, solution tests, Windows startup smoke, presentation regression and Development publisher before declaring release ready.

Phase 2 is an art and feedback pass, not a full production-quality art pipeline. Further sprite animation frames, directional character animation and dynamic light/shadow systems would be later work.

## Reliability and viewport update

The simulation now holds its spawn clock at zero once the final enemy of a wave has deployed. Earlier versions continued decrementing it below zero, producing invalid host snapshots, apparent command revision conflicts, and lost waves when loading affected saves. The loader accepts and clamps previously saved negative spawn clocks, preserving their wave, towers, enemies, coins, and lives instead of silently restarting.

Wave deployment, kills and escapes are distinct. The HUD's completion bar now advances only on actual enemy defeats; spawning and tower shots do not count. The game still ends a wave only after every enemy has spawned **and** every remaining enemy has been defeated or escaped. Native per-wave kill/escape counters are checkpointed with the existing version-1 preferences save; old saves lacking those counters deserialize to zero and continue to load.

Stale UI actions prompt a fresh native snapshot. An exit action deliberately remains available during a pending command or stale revision, because safe exit is preferable to trapping the player inside a stale view. The state-changing tower commands continue to require a matching session and revision.

For standard desktop viewports (at least 1100 × 630 CSS pixels), the battlefield, wave display, action row and workshop are arranged in one height-bounded view, without scrolling or changing the canvas coordinate model. Short or narrow windows retain a scrollable fallback for legible controls. The original illustrated map gains deterministic cached causeway stones and layered forest canopies. Bowstring, frost and cannon synth cues are differentiated and play with sound enabled even if cosmetic animations are disabled.

### Regression focus

- Run the native tower defence tests for end-of-deployment clock bounds, resumed saves (including legacy negative-clock cases), kills/escapes and stale-command exits.
- Run frontend snapshot validation and presentation tests. Check browser/SVG presentation on the Development channel.
- Manually play with and without towers long enough for every enemy to spawn, confirm progress does not advance on projectiles, close/reopen at several points and verify the saved wave.
- Test 1×/2× speed, pause, build/upgrade while a wave is active, use the back button after a simulated stale-revision error, and inspect a compact Windows viewport.
- Confirm Windows build, audio on/off, reduced-motion behaviour and the Development update artifact before release.

## Campaign and mastery expansion

- `tower.menu` opens a map hub with an independent run checkpoint per battlefield. `tower.play` validates map requirements in native code, restores that map's saved towers/wave and pauses active waves on reload.
- `forest` (15 campaign waves) and `frost` (20) use separate native paths, building pads and artwork. Frostpass unlocks at **Bastion level 3**. Initial login routes to the map hub; existing `TdState.Version=1` positions are retained through migration.
- Midnight Bastion owns its own level progression (`TdProgress`, level 1–100) separate from the casino-wide level. Completing waves awards XP; each level earns exactly one spendable skill point. Skill point count is derived from level minus purchased ranks. `tower.skill.buy` and `tower.skill.reset` are validated and applied natively.
- Skill branches: capital (additional initial coins), refund (up to 95% sell-back), engineer (cheaper construction and upgrades), fortify (more initial hearts), scouting (preview), focus (tower damage bonus), last-stand (once per run, survive otherwise lethal escape with one heart). All are fully unlockable; reset is free from the skill tree.
- Two additional tower types are unlocked globally: **Ballista** at level 2 (long-range heavy single target) and **Flame Spire** at level 4 (rapid short-range splash). The standard three towers remain unlocked. All five types have distinct SVGs, shot feedback and native damage/range rules.
- Drag a tower from the workshop to a free site, or click a site and build normally. Occupied site markers are omitted from the Pixi pad layer.
- On completing a campaign map, an end screen provides an in-place `tower.endless` continuation, map selection and retry. Endless runs retain the battlefield and enemies scale through a bounded wave cap.
- Shared challenge and player statistics backends receive Bastion's cumulative completed waves, defeated enemies, map victories, best waves and mastery levels. Challenges live in the normal global challenge catalog and claim using the established reward path.
- The old forest road striping and rotated triangle-tree SVGs are removed; snow-covered Frostpass has a distinct board/terrain.
- Data remains offline, credit-free, and native-owned. `TdState.Version` remains 1 so legacy save data loads with the new optional fields and default progress. A saved active wave reloads paused.
