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

func has_funds(kind: String) -> bool:
	if not registered(kind):
		return false
	if kind == "depot" and free_depot_available:
		return true
	for material in COSTS[kind]:
		if available(material) < int(COSTS[kind][material]):
			return false
	return true

func pay(kind: String) -> Dictionary:
	# Returns actual spend, so only paid materials can be refunded later.
	if not has_funds(kind):
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
	if kind == "depot" and free_depot_available:
		return result
	for material in cost(kind):
		var need: int = int(COSTS[kind][material]) - available(material)
		if need > 0:
			result[material] = need
	return result

func snapshot() -> Dictionary:
	return {"stock": stock.duplicate(), "free_depot": free_depot_available}

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
	stock = clean
	free_depot_available = bool(raw.get("free_depot", false))
	return true

func restore_legacy(delivered: Dictionary) -> void:
	# Old warehouses credited a shared delivered counter. Move that existing
	# production into the new spendable cloud once; further saves carry stock
	# explicitly, so a reload can never duplicate the credit.
	for material in MATERIALS:
		stock[material] = maxi(0, int(delivered.get(material, 0)))
	stock["copper_ingot"] = maxi(START_COPPER, available("copper_ingot"))
	free_depot_available = true
