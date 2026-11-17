# Midnight Industries — premium-industrial factory prototype

Standalone Godot 4.5.1 Windows mode integrated into The Midnight Club via the **Empire → Midnight Industries** left navigation link, without adding a lobby tile. The mode is offline, keeps its own versioned save file and never changes casino credits.

## Industrial art direction

The visual identity is **Modern Industrial / Premium Automation**. Previous green-teal debug grids, flat rounded button boxes, primitive geometric machine drawings, placeholder ore icons and old selection rectangles are removed from the rendering path.

- **World:** a subdued green meadow with independent 1024px grass and stone detail textures, now containing explicitly painted small grass blades and gravel facets (not just blurred smooth noise). Ore patches sit in natural grey-brown stony soil, with noise-broken, smoothly blended margins rather than grid-aligned edges. A single masked terrain shader replaces the former metal floor tiles; machinery stays cached in independently redrawable 16×16-cell chunks.
- **Machines:** dedicated 144-pixel metallic art for drill, conveyor, furnace and storage. The warehouse and other machines sample isolated cached 144px textures rather than neighboring atlas cells, preventing mipmap bleed and left-neighbor clipping. Smelters are optically offset two pixels upward for clean conveyor joins. Four conveyor orientations use dedicated artwork: the approved right-facing machine remains intact; the left-facing moving track is reversed inside the original chassis; north/south combine the original detailed tread with independently lit supports. Terrain deposits now use **twelve distinct boulder formations per mineral** with different outlines, layered heights, individual rock sizes and clustered silhouettes, textured from the approved source art. Transported ore uses distinct single-stone cargo artwork with contact shadows and belt-rail occlusion rather than a pasted full deposit icon; ingots retain their approved source look.
- **Production animation:** belts show directional cyan pulses, goods interpolate at the display framerate, drills indicate mechanical motion, active furnaces emit warm glow, sparks and smoke, storage displays a subtle status light.
- **Interaction:** selection uses amber industrial scan brackets; build previews show the final machine graphic and a strong outgoing arrow; invalid placement uses red hazard feedback.
- **UI:** distinct embossed graphite and steel panel families, amber active controls, restrained cyan information channels, a compact HUD and a collapsible idle inspector, a German/English machine catalog with genuine sprite icons, selected-machine telemetry, resource readouts, quick slots, an animated menu and usable typography (Bahnschrift/Segoe UI with fallbacks).

The approved 144-pixel machine and material artwork is shipped as `assets/industrial_hd_atlas.webp`. `factory_art.gd` varies ground mineral clusters by arranging this existing artwork; `factory_belt_art.gd` combines the approved illustrated conveyor surface with new north/south metal frames once. `factory_terrain.gd` builds cached 1024px grass/stone detail textures and a resource-aware transition mask once on startup. `factory_skin.gd` caches nine-slice metal UI textures; `factory_chunk.gd` and `factory_static.gd` draw only placed assets and visible resource deposits; `factory_motion.gd` contains animation only. No runtime downloads are required.

Colors: graphite `#111A21`, steel `#273743`, illuminated amber `#F5C04A`, neon cyan `#00D8F4`, furnace orange `#FF982D`, critical red `#FF5B50`. Avoid the old green pixel-fantasy casino panel language.

## Gameplay and controls (unchanged)

| Control | Action |
| --- | --- |
| WASD / arrows | Move camera |
| Left-click and hold + drag (selection mode) | Pan camera; a short click still selects machinery |
| Middle-drag / mouse wheel | Pan (all modes) / zoom |
| V | Inspect/select mode; click to inspect a building |
| B | Construction mode; click to place the active machine |
| 1–5 | Choose mining drill, belt, furnace, warehouse, depot |
| R | Rotate build preview or selected machine |
| Right click | Cancel build / clear selection (never destructive) |
| Space | Pause production |
| Ctrl+S | Save |
| G or main-menu toggle | Show/hide the optional placement grid (visual preference saved separately) |
| C or main-menu button | Center the camera on the casino construction site |
| I or header Storage button | Open/close global depot construction inventory |
| M | Open/close full-screen map (Escape also closes) |
| Left-click inside world map | Recenter camera on that location and close map |
| Escape | Open/close the main menu |

The main menu supports continue, save, confirmed new factory, motion and grid settings, and **Save & Return to Casino**. The obsolete Sector Overview panel has been removed to free the right side of the screen. A built-in starter iron chain mines, transports, smelts and deposits ingots to the global bank. Copper deposits and additional patches allow building other chains.

## Project Midnight — construction and progression foundation

The 224×176 world now contains a permanent **12×12-grid casino construction site**, centrally positioned at grid origin `(106, 82)`. The two former oversized deposits beside the casino have moved to remote biomes, leaving a wide clear approach. Mineral patches, the starter production line and existing save-game machines are never moved. New factory buildings cannot be placed inside the reserved construction footprint; machines loaded there from historical saves remain intact and functional.

Each of the site's **48 intake connections** is tied to a separate side/grid position: 12 on the north border, 12 east, 12 south, and 12 west. A belt or producing machine can feed a side input only when it faces *into* that specific boundary cell. Corners can receive from either adjacent side. Input delivery uses the same two-phase simulation transport as the factory, reserving requirements per tick to avoid consuming excess items.

`factory_construction.gd` defines **12 stable phases** (`phase_01` through `phase_12`), saved per-phase material receipts and unlock gates for machinery and recipes. `MACHINE_UNLOCK_PHASES` and `RECIPE_UNLOCK_PHASES` map an identifier to the **number of completed phases** required (zero means available from the beginning). All current machines and current ingot recipes remain unlocked. Later machines and recipes can be registered here without inventing placeholder content now.

**No requirements are configured for any of the 12 phases yet.** An empty phase cannot complete automatically and will not consume conveyor items. When requirements are eventually defined in `PHASE_REQUIREMENTS`, the foundation can collect those exact materials from any of its 48 inputs, keep partial progress, complete the current phase and open the next when the last requirement arrives. Actual building growth and new content are intentionally deferred.

Construction progress is stored in an optional `construction` section in the existing **version-1 save format**; older saves without it load into phase one, leaving machine inventories, delivered ingots and ore resources untouched. The foundation is rendered by `factory_construction_view.gd`, with a compact HUD phase label and **C** / main-menu camera jump to the site.

Current factory recipes, throughput, production ticks, warehouse counters and casino integration remain unchanged.

## Expanded world, minerals and water

- World dimensions: **224×176 tiles** (formerly 128×96), rendered via 16×16 cached chunks. The small original starter iron/copper deposits and starter production line remain in place.
- The oversized iron and copper clusters formerly at `(61,47)` and `(82,65)` moved to `(44,124)` and `(174,128)`. Other remote deposits support larger factory layouts. The casino is now central at `(106,82)` with clear space around it.
- **Gold**: five small, rare outer-map deposits. Gold rock and ingot textures are made from the previously approved high-resolution mineral artwork, preserving its metallic lighting; existing iron/copper sprites remain unchanged. Gold can be mined, belted, smelted into ingots and deposited at warehouses with distinct stored counts.
- `factory_world.gd` generates winding directional rivers and natural-looking lakes. Flow vectors drive inexpensive GPU water motion in the landscape shader; lakes ripple more slowly. No water grid cell overlaps any ore field, and waterways keep an extra two-tile ore margin. The player cannot place machines or conveyor belts on water.
- **M** shows a modal full-screen map with terrain, rivers/lakes, mineral colors, existing machines, casino plot and camera viewport. It is hidden otherwise. M/Escape closes it; left-click on the map moves the camera there.
- Existing saves remain readable. Older miners on relocated ore are preserved with local legacy ore underneath them, and previously built factory cells are kept clear of newly generated water. Gold totals default to zero for older saves. Save version stays v1.

All twelve casino phase requirements remain deliberately empty; the new mineral and geography update does not invent any construction quotas.
## Depots, construction costs and the global build inventory

The factory now has a physical **Depot** machine (tool **5**) and a separate global materials economy in `factory_economy.gd`. No factory building is free to place repeatedly: each normal placement checks the global depot stock and removes the entire cost atomically; insufficient stock leaves the world and balance unchanged.

| Machine | Materials |
| --- | --- |
| Mining drill | 8 iron ingots |
| Conveyor belt | 1 iron ingot per tile |
| Smelting furnace | 12 iron ingots + 6 copper ingots |
| Warehouse | 15 iron ingots |
| Depot | 20 iron ingots + 10 copper ingots |

- **Depot** is a one-tile conveyor destination with a separate metallic/cyan sprite. Every item delivered physically to any depot adds one unit to the **global, uncapped** spendable inventory, accessible from anywhere on the map. Depots do not output goods automatically.
- **Warehouse** no longer spends material into a global counter. It has its own local **64-item buffer**, accepts all six current material types and sends inventory out along its chosen direction when a receiving belt or depot is available. The Warehouse inspector reports the real local buffer.
- The permanent casino construction site still receives only physical deliveries to its 48 perimeter intakes. Those requirements and its 12-phase progress remain **completely separate** from the global machine-building bank.
- The **I** key or **Lager [I] / Storage [I]** header button opens the full-screen construction inventory: six material stocks, costs, available/short materials and connected depot count. **I/Escape** closes it. In Build mode, the selected machine's cost and affordability are shown in the catalog and a red build preview indicates insufficient stock. The keyboard has **1–5** quick slots.
- **New games** spawn with the original functional starter production line feeding a **complimentary depot at (25,17)**. A one-time 12-copper-ingot bootstrap inventory avoids the smelting-cost bootstrap paradox; iron is still produced and physically delivered. Starter structures are recorded as unpaid.
- **100% of actually paid construction resources** are returned when a machine is demolished, including depots and conveyor pieces. Free starter or gifted machines return a **reusable, type-specific relocation kit**, not raw building materials: a free drill can be moved and rebuilt once for free but cannot be duplicated or sold for ingots. This prevents a factory softlock from deleting the last complimentary drill, furnace, belt or depot. Contents inside demolished machines are discarded; paid construction bills are still refunded in full.
- **Existing save games** import old Warehouse delivered totals as global inventory exactly once, preserving already produced materials. When possible the migration attaches one complimentary depot to an existing Warehouse's output; otherwise it grants a single free depot placement voucher. Subsequent saves store the global stock, one-time voucher, per-warehouse buffers and actual paid construction bills without changing the save version.

All original iron/copper/gold production recipes remain available. More expensive higher-tier recipes and machines can later use this same economy and the pre-existing casino-phase unlock registry.

## Validation

```sh
godot --headless --path games/midnight-industries --editor --import
godot --headless --path games/midnight-industries --script res://smoke_test.gd
godot --headless --path games/midnight-industries --script res://ui_smoke.gd
godot --headless --path games/midnight-industries --script res://visual_smoke.gd
godot --headless --path games/midnight-industries --script res://performance_smoke.gd
```

The CI workflow validates 1024px grass/stone details, unchanged raw material artwork, different ground-ore layouts, the vertical belt textures, safe machine footprints, the grid toggle, sector-panel removal, atlas decoding, machine icons, building/inspection UI and production/model behavior. The Windows export script additionally validates the premium visual package before packaging. A headless factory benchmark is not a GPU framerate guarantee; device-specific 1080p/1440p quality and performance still require visual testing on a Windows client.

### Current quality/performance pass

- A protected 12×12 casino foundation adds twelve empty progression phases and 48 inward-facing item intakes, without altering existing ore patches or the original material recipes.
- The old repetitive metal platform was replaced with one meadow-to-rock shader surface. Two 1024px detail textures now hold sharp material variation at close zoom, while ore deposits retain softly blended organic rocky patches. No per-frame primitive ground drawing.
- The machine inspector collapses when no building is selected; header, catalog and quickbar are substantially narrower.
- Approved original ore and cargo sprites were restored; only ground-cluster arrangements vary. Vertical conveyors have dedicated lighting-correct artwork, belts and ore have no oversized drop shadows, and warehouses no longer overlap left neighbors.
- Actual Windows FPS remains a device-side acceptance check; headless simulation speed is not a GPU benchmark.

## Menu, world selection and settings refinement

Menus, pause controls and settings were redesigned as a **separate native Godot
menu layer**. The project now uses modern recessed panels with rounded edges,
soft shadows, consistent typography, clear selected states, and deliberately
avoids the old stretched gold-corner menu art. No changes were made to the
existing factory HUD or physical machine sprites.

- **English is the default language.** Prior implicit German defaults migrate
  to English, while an explicit language selection in the new dropdown is
  persisted and reflected immediately across menus.
- **Settings** uses Audio / Video / Game & UI / Controls tabs, a language
  dropdown and actively highlighted effect/display/tooltip options; all
  control labels are localized. No settings are applied repeatedly on slider
  hover, and UI scaling is committed only when leaving the settings overlay.
- **Pause menu** contains only Resume, Save, Settings, Worlds & Saves, and
  Save & Return to Casino. Grid, Animations and Find Casino Site are not
  duplicated in this menu.
- **World selection** is substantially wider, with spacious individual
  entries, phase and playtime metadata, and distinct red Delete controls.
- **Window mode** offers a native Godot custom borderless titlebar in Windowed
  mode, supporting OS-integrated drag, resize, minimize and maximize. A
  borderless-maximized desktop view is the default to avoid the unwanted
  classic white Windows decoration. Window size respects usable monitor area.
- The preexisting Shapez experiment and oversized depot quick-pins are still
  **excluded**, pending proper separate art direction.

## Restored: world selection, settings and interactive map

The tested Phase-1 HUD overhaul was rolled back. The subsequent selective
recovery keeps the **original compact construction catalog** and **original
full-screen depot inventory** while bringing back only the following useful
parts:

- A real main menu on game launch, with **Continue**, **New World**, and
  **Worlds & Saves**. Each world has its own saved factory, camera view and
  interactive-map markers and layers. Backups, recovery, renaming and deletion
  are managed by \`world_manager.gd\` with three rotating backups. Existing
  single-save data is imported once; the original save is not overwritten.
- A standalone settings panel with audio bus volumes/mute, VSync, frame limit,
  screen mode/resolution, visual effects, water animation, language, input
  bindings, grid, tooltips, contrast, autosave and UI scale. Settings are
  global, independent of the chosen world.
- **Flicker guard**: changing audio sliders, checkboxes and quality settings
  no longer resizes the OS window, rebuilds the HUD or re-renders the complete
  interface. Resolution and window mode change only on explicit user request;
  UI scale is committed after the settings screen closes, and language
  refreshes once after closing. Hover tweens on controls are canceled when
  replaced.
- The more interactive map has fully revealed terrain **without fog of war**,
  mouse-centric zoom, drag-to-pan, resource overlays and filter toggles,
  river animation, editable named markers, and a double-click camera jump.
  Map markers/layer choices persist with each world.
- The **oversized quick-pin depot bar** and the **Phase-1 bottom Shapez dock**
  are intentionally *not* reintroduced. Those interfaces require a separate
  visual design and review before further implementation.

Relevant recovered files: \`factory_navigation.gd\`, \`world_manager.gd\`,
\`industries_settings.gd\`, \`factory_minimap.gd\`, and \`item_catalog.gd\`.
Regression tests include \`restored_systems_smoke.gd\` and
\`restored_ui_smoke.gd\`.


## Consistent World Atlas and inventory UI

The full-screen **World Atlas** and **Global Build Inventory** now share
\`factory_menu_theme.gd\` with the approved menu redesign. The atlas keeps
its fully revealed geography (no fog of war), mouse-centered zoom, pan,
camera jump, region and resource filters, animated water, editable markers
and per-world marker/layer persistence. The actual map is rendered through
a clipped \`factory_map_surface.gd\` canvas so zooming cannot draw across the
separate legend and navigation controls. The right-hand panel has legible
resource icons and native Godot toggle controls.

The global inventory is a fully **data-driven**, scrollable native UI:
- Every entry in \`item_catalog.gd::ITEMS\` appears automatically, even with
  zero stock; items discovered in stock or cost recipes are additionally
  included. New catalog items require **no inventory view changes**.
- Catalog item IDs, names, categories and optional \`icon_path\` assets
  drive visuals. \`factory_item_icon_art.gd\` provides original
  material-specific, dimensional ingot and ore illustrations when no
  dedicated image asset exists, and a component illustration for future
  categories. The old colored-square icons have been removed.
- Search, category filters, live material counts, and scroll containers
  scale with future recipes and material types. The right-hand machine
  cost overview also derives its rows dynamically from machine recipes.
- Materials are deducted only by existing game economy actions; viewing
  inventory is read-only. New world and old-save content are unaffected.

