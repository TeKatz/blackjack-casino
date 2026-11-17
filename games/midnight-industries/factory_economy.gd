extends RefCounted
## Authoritative construction economy. All depots share one account; warehouses
## are only local logistics buffers. The casino site has a separate ledger.
const COSTS := {
	"miner": {"iron_ingot": 8},
	"belt": {"iron_ingot": 1},
	"smelter": {"iron_ingot": 12, "copper_ingot": 6},
	"storage": {"iron_ingot": 15},
	"depot": {"iron_ingot": 20, "copper_ingot": 10}
}
const MATERIALS := [
	"iron_ore", "copper_ore", "gold_ore",
	"iron_ingot", "copper_ingot", "gold_ingot"
]
const START_COPPER := 12

var stock: Dictionary = {}
var free_depot_available := false
var relocation_kits: Dictionary = {}

func _init() -> void:
	for material in MATERIALS:
		stock[material] = 0
	# Bootstrap copper for the first paid copper smelter: without this,
	# the initial iron-only production line would softlock copper forever.
	stock["copper_ingot"] = START_COPPER

func cost(kind: String) -> Dictionary:
	return COSTS.get(kind, {}).duplicate()

func registered(kind: String) -> bool:
	return COSTS.has(kind)

func available(material: String) -> int:
	return int(stock.get(material, 0))

func has_free_kit(kind: String) -> bool:
	return int(relocation_kits.get(kind, 0)) > 0

func return_kit(kind: String) -> void:
	if registered(kind):
		relocation_kits[kind] = int(relocation_kits.get(kind, 0)) + 1

func has_funds(kind: String) -> bool:
	if not registered(kind):
		return false
	if has_free_kit(kind) or (kind == "depot" and free_depot_available):
		return true
	for material in COSTS[kind]:
		if available(material) < int(COSTS[kind][material]):
			return false
	return true

func pay(kind: String) -> Dictionary:
	# Returns actual spend, so only paid materials can be refunded later.
	if not has_funds(kind):
		return {}
	if has_free_kit(kind):
		relocation_kits[kind] = int(relocation_kits[kind]) - 1
		if int(relocation_kits[kind]) == 0:
			relocation_kits.erase(kind)
		return {}
	if kind == "depot" and free_depot_available:
		free_depot_available = false
		return {}
	var paid: Dictionary = cost(kind)
	for material in paid:
		stock[material] = available(material) - int(paid[material])
	return paid

func refund(paid: Dictionary) -> void:
	for material in paid:
		if material in MATERIALS:
			var amount := maxi(0, int(paid[material]))
			stock[material] = available(material) + amount

func credit(material: String, amount: int = 1) -> bool:
	if not material in MATERIALS or amount <= 0:
		return false
	stock[material] = available(material) + amount
	return true

func missing(kind: String) -> Dictionary:
	var result: Dictionary = {}
	if has_free_kit(kind) or (kind == "depot" and free_depot_available):
		return result
	for material in cost(kind):
		var need: int = int(COSTS[kind][material]) - available(material)
		if need > 0:
			result[material] = need
	return result

func snapshot() -> Dictionary:
	return {"stock": stock.duplicate(), "free_depot": free_depot_available,
		"relocation_kits": relocation_kits.duplicate()}

func restore(raw: Variant) -> bool:
	if typeof(raw) != TYPE_DICTIONARY:
		return false
	var current: Variant = raw.get("stock", null)
	if typeof(current) != TYPE_DICTIONARY:
		return false
	var clean: Dictionary = {}
	for material in MATERIALS:
		var value: Variant = current.get(material, 0)
		if typeof(value) != TYPE_INT and typeof(value) != TYPE_FLOAT:
			return false
		if not is_finite(float(value)) or float(value) < 0.0 \
			or float(value) > 1000000000.0 or float(int(value)) != float(value):
			return false
		clean[material] = int(value)
	if typeof(raw.get("free_depot", false)) != TYPE_BOOL:
		return false
	var raw_kits: Variant = raw.get("relocation_kits", {})
	if typeof(raw_kits) != TYPE_DICTIONARY:
		return false
	var kits: Dictionary = {}
	for kind in raw_kits:
		var amount: Variant = raw_kits[kind]
		if not registered(str(kind)) or (
			typeof(amount) != TYPE_INT and typeof(amount) != TYPE_FLOAT
		) or not is_finite(float(amount)) or float(amount) < 0.0 \
			or float(amount) > 100000.0 or float(int(amount)) != float(amount):
			return false
		if int(amount) > 0:
			kits[str(kind)] = int(amount)
	stock = clean
	free_depot_available = bool(raw.get("free_depot", false))
	relocation_kits = kits
	return true

func restore_legacy(delivered: Dictionary) -> void:
	# Old warehouses credited a shared delivered counter. Move that existing
	# production into the new spendable cloud once; further saves carry stock
	# explicitly, so a reload can never duplicate the credit.
	for material in MATERIALS:
		stock[material] = maxi(0, int(delivered.get(material, 0)))
	stock["copper_ingot"] = maxi(START_COPPER, available("copper_ingot"))
	free_depot_available = true
