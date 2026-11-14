# Midnight Descent – atmosphere, HUD and menu polish

**Historical implementation/polish snapshot.** The current Midnight Descent roguelite has expanded beyond this isolated pass. For live game systems and controls, see [Midnight Descent](midnight-descent.md); the old five-room prototype is not the current content limit.

The existing five-room Forgotten Vault simulation is unchanged. The world now uses a separate CanvasLayer with CanvasModulate, brighter warm torch lights, two lower sconces, player and projectile light, boss lighting, an animated gate and pillar shadow occluders. Dust and sparse stone details break up the floor repetition. Combat telegraphs and HUD stay on the unmodulated canvas.

The HUD presents portrait, animated health, room diamonds, run statistics, sword/bolt/dash cooldowns, an explicitly empty active-item slot, relic slots and a boss meter. Reward choices have atlas icons and panel depth. The new main menu uses the same pixel atlases and an animated vault chamber, character preview, six actual relic descriptions, enemy names, fullscreen, session volume and menu motion settings.

No character unlocks, permanent upgrades, new item mechanics or balance changes are included. Active item remains a reserved slot. Visual quality, screen scaling and GPU shadow contrast still need real Windows inspection; the existing Godot headless menu and gameplay tests remain the regression gate.
