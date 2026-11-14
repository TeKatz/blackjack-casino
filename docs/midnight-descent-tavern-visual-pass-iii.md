# Midnight Descent — Threshold Tavern visual pass III (12 November 2026)

**Historical implementation record.** This pass predates the final WPF-free host cutover. Its art and Godot behavior remain relevant; the original desktop adapter file no longer exists. See [current Midnight Descent](midnight-descent.md) and [architecture](architecture.md).

This pass is coordinated with the canonical 1280×720 pixel-world geometry. It fixes the latest seven tavern presentation issues and the Windows fullscreen launch from the Blackjack desktop host.

## Seating and card-table anatomy

The bar counter's real footprint ends at `y=324`. Previous artwork ended at `y≈279`, leaving more than 50 pixels of apparent distance between the bartender and the seats. The rebuilt bar now has a wood-panel front, carved lip and lower skirting reaching `y≈322`. All five low, open barstools share the `run_model.gd` anchor `HUB_STOOL_SEAT_Y=330` and its crossbar `HUB_STOOL_FOOTREST_Y=364`. They no longer resemble enclosed high chairs.

The sitting hero uses the original character's **full-size, back-facing upper-body sprite**, with a near-stationary head, bent legs, boots and arms resting on the bar. The transition lowers the torso a few pixels instead of popping an upright standing figure into a tall chair texture. It is a bespoke composition rather than merely moving the entire sprite.

The two gamblers keep their seated legs and chairs; their animated playing arms now originate at approximately `(431,526)` and `(619,526)` (below their chests), bend at elbows and extend onto the felt. Their former shoulder/head-height start `y=495` was visibly wrong and has been removed.

The drinking patron sits on the reserved fifth **public-side stool**, faces north toward the bartender and shows his coat and the back of his hair. His arm slowly lifts a rendered beer mug to his head and lowers it on a 6.5-second loop. The old front-facing eyes and awkward bar orientation were removed.

## Forge and portal composition

The plain cabinet at `x735..843` is replaced with a custom-painted **working forge**: sooty chimney, labelled riveted metalwork, inset glowing firebox with iron grating, ember animation, an anvil, hammer, tongs and a heavy masonry apron. The abandoned generic rectangles are gone; the clickable forge footprint and its risk/reward menus remain unchanged.

The entrance portal moves from `(1045,388)` to **`(1034,293)`**, centered between the right sconces at `x900` and `x1168`. Its physical portal-frame collision is moved to `Rect2(978,211,112,165)`. A slim northbound runner now leads up to its entrance. The render remains inside the tavern's floor bounds with 60+ pixels horizontal clearance to either hanging lantern. The prompt appears *below* the portal and its entrance can be used from an unblocked floor position.

The anonymous block just to the left/behind the old portal is removed and replaced by the recognizable forge rather than moved along with the portal. The disconnected portal cap previously rendered **172 pixels above its own frame** is fixed to its real top/bottom edges.

## Fullscreen when launched from Blackjack

The current NativeShell Godot launcher passes the engine command `--fullscreen` **before** the `--` separator used for localization arguments. This means the Blackjack host launches a genuinely fullscreen Windows game process while minimizing itself, and restores its own window when Midnight Descent exits. The game project also defaults to `display/window/size/mode=3` (fullscreen), including standalone launches.

## Regression coverage

- Pure run-model smoke asserts the relocated gate is reachable from the floor, the forge does not intersect the portal, the portal has adequate lantern clearance and the bar/stool positions share sane dimensions.
- Actual `run.tscn` smoke verifies the drawn tavern's geometry and functional bar/shop and portal interactions.
- Menu smoke confirms the project fullscreen default.
- GitHub Actions checks the actual C# host launch arguments are ordered correctly relative to `--`. The Godot suite still imports and exercises both real game scenes.
