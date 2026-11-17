extends RefCounted
## Categories are metadata. Adding a machine to item_catalog.gd is enough
## to populate its category here; unknown/locked machines remain visible.
const Catalog = preload("res://item_catalog.gd")
const Construction = preload("res://factory_construction.gd")
const CATEGORIES := [
	{"id":"mining", "de":"Abbau", "en":"Mining", "icon":"mining", "order":10},
	{"id":"logistics", "de":"Logistik", "en":"Logistics", "icon":"logistics", "order":20},
	{"id":"production", "de":"Produktion", "en":"Production", "icon":"production", "order":30},
	{"id":"energy", "de":"Energie", "en":"Energy", "icon":"energy", "order":40},
	{"id":"special", "de":"Spezial", "en":"Special", "icon":"special", "order":50}
]

static func categories() -> Array[Dictionary]:
	var all: Array[Dictionary] = []
	for category in CATEGORIES:
		all.append(category.duplicate())
	# Future categories specified in the machine catalog appear automatically.
	for machine in Catalog.MACHINES:
		var key: String = str(machine.get("category", "special"))
		var exists := false
		for entry in all:
			if entry.id == key:
				exists = true
				break
		if not exists:
			all.append({"id":key, "de":key.capitalize(), "en":key.capitalize(),
				"icon":"special", "order":80})
	all.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		return int(a.order) < int(b.order))
	return all

static func machines(category: String) -> Array[Dictionary]:
	var matches: Array[Dictionary] = []
	for entry in Catalog.MACHINES:
		if str(entry.get("category", "special")) == category \
			and bool(entry.get("is_visible", true)):
			matches.append(entry.duplicate())
	matches.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		return int(a.get("sort_order", a.get("key", 99))) < \
			int(b.get("sort_order", b.get("key", 99))))
	return matches

static func phase_required(machine: Dictionary) -> int:
	var id: String = str(machine.get("id", ""))
	return int(machine.get("phase_required",
		Construction.MACHINE_UNLOCK_PHASES.get(id, -1)))

static func unlocked(machine: Dictionary, model) -> bool:
	var kind: String = str(machine.get("id", ""))
	return model != null and model.is_unlocked(kind) \
		and phase_required(machine) >= 0 \
		and model.construction.completed_phases >= phase_required(machine)
