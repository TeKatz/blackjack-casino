extends RefCounted
## Deterministic geography. The machine simulation and renderer use the exact
## same resource/water coordinates, so water can never become a buildable tile.
const WIDTH := 224
const HEIGHT := 176
const SITE_ORIGIN := Vector2i(106, 82)
const SITE_SIZE := 12
# The two former large deposits at (61,47) and (82,65) have been moved out
# of the casino approach. The small starter ore patches remain undisturbed.
const PATCHES := [
	{"pos": Vector2i(16, 17), "radius": 5, "kind": "iron"},
	{"pos": Vector2i(36, 22), "radius": 6, "kind": "copper"},
	{"pos": Vector2i(44, 124), "radius": 10, "kind": "iron"},
	{"pos": Vector2i(174, 128), "radius": 9, "kind": "copper"},
	{"pos": Vector2i(181, 27), "radius": 7, "kind": "iron"},
	{"pos": Vector2i(25, 94), "radius": 6, "kind": "iron"},
	{"pos": Vector2i(202, 72), "radius": 6, "kind": "copper"},
	{"pos": Vector2i(77, 150), "radius": 5, "kind": "copper"},
	{"pos": Vector2i(130, 146), "radius": 6, "kind": "iron"},
	{"pos": Vector2i(83, 22), "radius": 5, "kind": "copper"},
	# Gold is scarce: small, peripheral nodes, no giant middle-map field.
	{"pos": Vector2i(13, 154), "radius": 3, "kind": "gold"},
	{"pos": Vector2i(205, 154), "radius": 3, "kind": "gold"},
	{"pos": Vector2i(207, 13), "radius": 2, "kind": "gold"},
	{"pos": Vector2i(17, 72), "radius": 3, "kind": "gold"},
	{"pos": Vector2i(131, 14), "radius": 2, "kind": "gold"}
]
# Deterministic compatibility with the five deposits from the old 128x96 map.
const LEGACY_PATCHES := [
	{"pos": Vector2i(16, 17), "radius": 5, "kind": "iron"},
	{"pos": Vector2i(36, 22), "radius": 6, "kind": "copper"},
	{"pos": Vector2i(61, 47), "radius": 8, "kind": "iron"},
	{"pos": Vector2i(82, 65), "radius": 7, "kind": "copper"},
	{"pos": Vector2i(107, 28), "radius": 6, "kind": "iron"}
]
const LAKES := [
	Vector4(61.0, 78.0, 11.0, 8.0),
	Vector4(201.0, 100.0, 12.0, 9.0),
	Vector4(105.0, 158.0, 12.0, 8.0),
	Vector4(29.0, 151.0, 8.0, 6.0)
]

static func inside(cell: Vector2i) -> bool:
	return cell.x >= 0 and cell.y >= 0 and cell.x < WIDTH and cell.y < HEIGHT

static func near_site(cell: Vector2i, margin: int = 0) -> bool:
	return cell.x >= SITE_ORIGIN.x - margin and cell.y >= SITE_ORIGIN.y - margin \
		and cell.x < SITE_ORIGIN.x + SITE_SIZE + margin \
		and cell.y < SITE_ORIGIN.y + SITE_SIZE + margin

static func _belongs(cell: Vector2i, patch: Dictionary) -> bool:
	var center: Vector2i = patch["pos"]
	var radius: int = int(patch["radius"])
	var distance := cell.distance_squared_to(center)
	return distance <= radius * radius \
		and (distance < 5 or posmod(cell.x * 17 + cell.y * 31, 11) != 0)

static func legacy_resource(cell: Vector2i) -> String:
	for patch in LEGACY_PATCHES:
		if _belongs(cell, patch):
			return str(patch["kind"])
	return ""

static func populate(resources: Dictionary, water: Dictionary) -> void:
	resources.clear()
	water.clear()
	for patch in PATCHES:
		var center: Vector2i = patch["pos"]
		var radius: int = int(patch["radius"])
		for y in range(center.y - radius, center.y + radius + 1):
			for x in range(center.x - radius, center.x + radius + 1):
				var cell := Vector2i(x, y)
				if inside(cell) and not near_site(cell, 17) and _belongs(cell, patch):
					resources[cell] = str(patch["kind"])
	# The water mask is shared by collision, rendering and the overview.
	# Rivers and lakes are placed around ore, with an extra two-tile margin.
	for lake in LAKES:
		for y in range(maxi(0, floori(lake.y - lake.w - 2.0)),
			mini(HEIGHT, ceili(lake.y + lake.w + 2.0))):
			for x in range(maxi(0, floori(lake.x - lake.z - 2.0)),
				mini(WIDTH, ceili(lake.x + lake.z + 2.0))):
				var cell := Vector2i(x, y)
				var nx := (float(x) - lake.x) / lake.z
				var ny := (float(y) - lake.y) / lake.w
				var distorted := nx * nx + ny * ny \
					+ 0.16 * sin(float(x) * 0.37 + float(y) * 0.13) \
					+ 0.09 * cos(float(y) * 0.43 - float(x) * 0.21)
				if distorted <= 1.0:
					_claim_water(cell, "lake", Vector2.ZERO, resources, water)
	# Eastbound, winding northern river.
	for x in range(WIDTH):
		var fx := float(x)
		var center_y := 48.0 + 11.0 * sin(fx * 0.032 + 0.3) \
			+ 3.0 * sin(fx * 0.097)
		var slope := 0.352 * cos(fx * 0.032 + 0.3) \
			+ 0.291 * cos(fx * 0.097)
		var flow := Vector2(1.0, slope).normalized()
		var radius := 2.6 + 0.7 * sin(fx * 0.05 + 0.8)
		for y in range(maxi(0, floori(center_y - radius - 1.0)),
			mini(HEIGHT, ceili(center_y + radius + 1.0))):
			if absf(float(y) - center_y) <= radius:
				_claim_water(Vector2i(x, y), "river", flow, resources, water)
	# A southern-flowing second river; it meets the northern waterway.
	for y in range(HEIGHT):
		var fy := float(y)
		var center_x := 154.0 + 11.0 * sin(fy * 0.041 + 0.7) \
			+ 3.0 * sin(fy * 0.105)
		var slope := 0.451 * cos(fy * 0.041 + 0.7) \
			+ 0.315 * cos(fy * 0.105)
		var flow := Vector2(slope, 1.0).normalized()
		var radius := 2.4 + 0.7 * sin(fy * 0.049 + 0.2)
		for x in range(maxi(0, floori(center_x - radius - 1.0)),
			mini(WIDTH, ceili(center_x + radius + 1.0))):
			if absf(float(x) - center_x) <= radius:
				_claim_water(Vector2i(x, y), "river", flow, resources, water)

static func _claim_water(cell: Vector2i, kind: String, flow: Vector2,
	resources: Dictionary, water: Dictionary) -> void:
	if not inside(cell) or near_site(cell, 12):
		return
	for y in range(cell.y - 2, cell.y + 3):
		for x in range(cell.x - 2, cell.x + 3):
			if resources.has(Vector2i(x, y)):
				return
	water[cell] = {"kind": kind, "flow": flow}

static func restore_legacy_miner(cell: Vector2i, resources: Dictionary,
	water: Dictionary) -> bool:
	var kind := legacy_resource(cell)
	if kind == "":
		return false
	resources[cell] = kind
	# Old save-game miners must remain usable even when the terrain changed.
	for y in range(cell.y - 1, cell.y + 2):
		for x in range(cell.x - 1, cell.x + 2):
			water.erase(Vector2i(x, y))
	return true
