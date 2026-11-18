extends RefCounted
const Atlas = preload("res://factory_atlas.gd")
const IconArt = preload("res://factory_item_icon_art.gd")
const ITEMS := [
	{"id":"iron_ore", "de":"Eisenerz", "en":"Iron ore", "category":"ore", "sprite":0, "mark":"Fe"},
	{"id":"copper_ore", "de":"Kupfererz", "en":"Copper ore", "category":"ore", "sprite":1, "mark":"Cu"},
	{"id":"gold_ore", "de":"Golderz", "en":"Gold ore", "category":"ore", "sprite":2, "mark":"Au"},
	{"id":"iron_ingot", "de":"Eisenbarren", "en":"Iron ingot", "category":"ingot", "sprite":3, "mark":"Fe"},
	{"id":"copper_ingot", "de":"Kupferbarren", "en":"Copper ingot", "category":"ingot", "sprite":4, "mark":"Cu"},
	{"id":"gold_ingot", "de":"Goldbarren", "en":"Gold ingot", "category":"ingot", "sprite":5, "mark":"Au"},
	{"id":"iron_plate", "de":"Eisenplatte", "en":"Iron plate", "category":"component", "mark":"Fe"},
	{"id":"copper_plate", "de":"Kupferplatte", "en":"Copper plate", "category":"component", "mark":"Cu"}
]
const MACHINES := [
	{"id":"miner", "de":"Abbaumaschine", "en":"Mining drill", "category":"mining", "key":1},
	{"id":"belt", "de":"Förderband", "en":"Conveyor belt", "category":"logistics", "key":2},
	{"id":"storage", "de":"Zwischenlager", "en":"Warehouse", "category":"logistics", "key":4},
	{"id":"depot", "de":"Globales Depot", "en":"Global depot", "category":"logistics", "key":5},
	{"id":"smelter", "de":"Schmelzofen", "en":"Smelting furnace", "category":"production", "key":3, "unlock_job":"first_iron_delivery"},
	{"id":"constructor", "de":"Constructor", "en":"Constructor", "category":"production", "key":8, "unlock_job":"metal_ingot_delivery"},
	{"id":"power_pole", "de":"Strommast", "en":"Power pole", "category":"energy", "key":6, "unlock_job":"first_iron_delivery"},
	{"id":"water_wheel", "de":"Wasserrad", "en":"Water wheel", "category":"energy", "key":7, "unlock_job":"first_iron_delivery"}
]

static func item(id: String) -> Dictionary:
	for entry in ITEMS:
		if entry.id == id:
			return entry
	return {}

static func name_for(id: String, german: bool = true) -> String:
	var entry := item(id)
	return str(entry.get("de" if german else "en", id))

static func icon(id: String) -> Texture2D:
	var entry: Dictionary = item(id)
	# Future items may specify authored image assets directly in their
	# catalog entry: "icon_path": "res://assets/items/..."
	var path: String = str(entry.get("icon_path", ""))
	if not path.is_empty() and ResourceLoader.exists(path):
		var loaded: Resource = load(path)
		if loaded is Texture2D:
			return loaded
	# Use a distinct illustrated object, not the old incorrect ingot mapping
	# from the generic industrial atlas. Unknown items get a 3D component.
	return IconArt.icon(id, entry)

static func all_item_ids() -> Array[String]:
	var ids: Array[String] = []
	for entry in ITEMS:
		var id: String = str(entry.get("id", ""))
		if not id.is_empty() and not ids.has(id):
			ids.append(id)
	return ids

static func machine(id: String) -> Dictionary:
	for entry in MACHINES:
		if entry.id == id:
			return entry
	return {}
