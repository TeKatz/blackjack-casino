extends RefCounted
## Project Midnight: deliberately unconfigured 12-phase construction loop.
## The twelve phases are stable IDs. Missing/empty requirements never complete
## automatically and never swallow cargo. Upcoming recipes/machines can be
## gated by the number of completed phases (0 = available from the start).
signal phase_completed(number: int)

const WIDTH := 12
const HEIGHT := 12
# True center of the expanded 224x176 world, with a clear factory approach.
# Existing saves keep their construction progress; the site geometry moves.
const ORIGIN := Vector2i(106, 82)
const PHASE_COUNT := 12
const DIRS := [Vector2i.RIGHT, Vector2i.DOWN, Vector2i.LEFT, Vector2i.UP]
const PHASE_IDS := [
	"phase_01", "phase_02", "phase_03", "phase_04",
	"phase_05", "phase_06", "phase_07", "phase_08",
	"phase_09", "phase_10", "phase_11", "phase_12"
]
# Intentionally empty until the material/recipe progression is designed.
# Future example: "phase_01": {"iron_ingot": 30}
const PHASE_REQUIREMENTS := {}
# A positive phase number unlocks the entry AFTER completing that phase.
# Future example: "metal_press": 1, "iron_plate": 1
const MACHINE_UNLOCK_PHASES := {
	"miner": 0, "belt": 0, "smelter": 0, "storage": 0
}
const RECIPE_UNLOCK_PHASES := {
	"iron_ingot": 0, "copper_ingot": 0, "gold_ingot": 0
}

var completed_phases := 0
var materials: Dictionary = {}
var _test_requirements: Dictionary = {}
var _test_machine_unlocks: Dictionary = {}
var _test_recipe_unlocks: Dictionary = {}

func _init(requirements_override: Dictionary = {}, machines_override: Dictionary = {},
	recipes_override: Dictionary = {}) -> void:
	# Injectable catalogs make future phased delivery and unlocking testable
	# without shipping fabricated construction requirements to players.
	_test_requirements = requirements_override.duplicate(true)
	_test_machine_unlocks = machines_override.duplicate(true)
	_test_recipe_unlocks = recipes_override.duplicate(true)

func inside(cell: Vector2i) -> bool:
	return cell.x >= ORIGIN.x and cell.x < ORIGIN.x + WIDTH and (
		cell.y >= ORIGIN.y and cell.y < ORIGIN.y + HEIGHT)

func is_input(destination: Vector2i, source: Vector2i, direction: int) -> bool:
	# Each of the 12 perimeter tiles per side has an inward-facing intake;
	# the four corners can accept from either of their bordering sides.
	if not inside(destination):
		return false
	if source + DIRS[posmod(direction, 4)] != destination:
		return false
	match posmod(direction, 4):
		0:
			return destination.x == ORIGIN.x
		1:
			return destination.y == ORIGIN.y
		2:
			return destination.x == ORIGIN.x + WIDTH - 1
		3:
			return destination.y == ORIGIN.y + HEIGHT - 1
	return false

func input_count() -> int:
	return 2 * WIDTH + 2 * HEIGHT

func phase_number() -> int:
	return mini(PHASE_COUNT, completed_phases + 1)

func current_id() -> String:
	if completed_phases >= PHASE_COUNT:
		return ""
	return str(PHASE_IDS[completed_phases])

func requirements() -> Dictionary:
	var key := current_id()
	if key == "":
		return {}
	if _test_requirements.has(key):
		return _test_requirements[key]
	return PHASE_REQUIREMENTS.get(key, {})

func is_machine_unlocked(kind: String) -> bool:
	var needed: int = int(_test_machine_unlocks.get(
		kind, MACHINE_UNLOCK_PHASES.get(kind, -1)))
	return needed >= 0 and completed_phases >= needed

func is_recipe_unlocked(recipe: String) -> bool:
	var needed: int = int(_test_recipe_unlocks.get(
		recipe, RECIPE_UNLOCK_PHASES.get(recipe, -1)))
	return needed >= 0 and completed_phases >= needed

func can_accept(item: String, already_reserved: int = 0) -> bool:
	if completed_phases >= PHASE_COUNT or already_reserved < 0:
		return false
	var targets: Dictionary = requirements()
	var target: int = int(targets.get(item, 0))
	if target <= 0:
		return false
	return int(materials.get(item, 0)) + already_reserved < target

func receive(item: String) -> bool:
	if not can_accept(item):
		return false
	materials[item] = int(materials.get(item, 0)) + 1
	if _requirements_fulfilled():
		completed_phases += 1
		materials.clear()
		phase_completed.emit(completed_phases)
	return true

func _requirements_fulfilled() -> bool:
	var targets: Dictionary = requirements()
	if targets.is_empty():
		return false
	for key in targets:
		var amount: int = int(targets[key])
		if amount <= 0 or int(materials.get(key, 0)) < amount:
			return false
	return true

func snapshot() -> Dictionary:
	return {"completed_phases": completed_phases, "materials": materials.duplicate()}

func restore(value: Variant) -> bool:
	# The old V1 saves had no construction field. Treat them as phase one
	# without changing their existing buildings, resources or deliveries.
	if typeof(value) != TYPE_DICTIONARY:
		return false
	var completed: Variant = value.get("completed_phases", 0)
	if typeof(completed) != TYPE_FLOAT and typeof(completed) != TYPE_INT:
		return false
	var done := int(completed)
	if done < 0 or done > PHASE_COUNT or float(done) != float(completed):
		return false
	var incoming: Variant = value.get("materials", {})
	if typeof(incoming) != TYPE_DICTIONARY:
		return false
	var clean: Dictionary = {}
	for item in incoming:
		if typeof(item) != TYPE_STRING or item == "" or item.length() > 80:
			return false
		var count: Variant = incoming[item]
		if typeof(count) != TYPE_FLOAT and typeof(count) != TYPE_INT:
			return false
		if count < 0 or count > 1000000000 or float(int(count)) != float(count):
			return false
		clean[item] = int(count)
	if done >= PHASE_COUNT and not clean.is_empty():
		return false
	completed_phases = done
	materials = clean
	return true
