extends RefCounted
const Atlas = preload("res://factory_atlas.gd")
const ITEMS := [
	{"id":"iron_ore", "de":"Eisenerz", "en":"Iron ore", "category":"ore", "sprite":0, "mark":"Fe"},
	{"id":"copper_ore", "de":"Kupfererz", "en":"Copper ore", "category":"ore", "sprite":1, "mark":"Cu"},
	{"id":"gold_ore", "de":"Golderz", "en":"Gold ore", "category":"ore", "sprite":2, "mark":"Au"},
	{"id":"iron_ingot", "de":"Eisenbarren", "en":"Iron ingot", "category":"ingot", "sprite":3, "mark":"Fe"},
	{"id":"copper_ingot", "de":"Kupferbarren", "en":"Copper ingot", "category":"ingot", "sprite":4, "mark":"Cu"},
	{"id":"gold_ingot", "de":"Goldbarren", "en":"Gold ingot", "category":"ingot", "sprite":5, "mark":"Au"}
]
const MACHINES := [
	{"id":"miner", "de":"Abbaumaschine", "en":"Mining drill", "category":"mining", "key":1},
	{"id":"belt", "de":"Förderband", "en":"Conveyor belt", "category":"logistics", "key":2},
	{"id":"storage", "de":"Zwischenlager", "en":"Warehouse", "category":"logistics", "key":4},
	{"id":"depot", "de":"Globales Depot", "en":"Global depot", "category":"logistics", "key":5},
	{"id":"smelter", "de":"Schmelzofen", "en":"Smelting furnace", "category":"production", "key":3}
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
	var entry := item(id)
	return Atlas.sprite(int(entry.sprite), 1) if not entry.is_empty() else null

static func machine(id: String) -> Dictionary:
	for entry in MACHINES:
		if entry.id == id:
			return entry
	return {}
