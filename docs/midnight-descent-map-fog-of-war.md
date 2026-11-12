# Midnight Descent — Dungeon map fog of war

The existing **26-node branch graph remains visible**, so a player can understand the branching topology without seeing the contents of distant rooms.

- **Visited/current room**: retain the original room icon/name, completed check and encounter danger; visited rooms remain visible but cannot be revisited.
- **Directly selectable next rooms**: reveal actual room icon, translated title, danger level and encounter tooltip; these nodes can be clicked as before.
- **All other rooms**: render a neutral, centered **?** without an icon, rarity color, danger numeral, encounter profile, boss/secret/elite identity or revealing tooltip. Future destinations cannot be clicked.
- The same rule holds on all three stages and resets naturally when a new floor is generated. Boss-gate progress requirements stay in the top heading, but the boss-map node remains hidden until it is reachable.
- This is **the preparatory fog-of-war change only**. No Oracle NPC, Shard transaction or remote-reveal purchase is introduced by this patch.

The run model owns the map_node_visibility(index) truth, so a later Oracle can build a paid reveal on top without changing traversal rules. Headless regression validates both model visibility after navigation and actual button text/icons/tooltips on the live map overlay.
