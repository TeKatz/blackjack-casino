extends RefCounted
## Authoritative construction economy. All depots share one account; warehouses
## are only local logistics buffers. The casino site has a separate ledger.
const COSTS := {
	"miner": {}, "belt": {}, "depot": {}, "smelter": {},
	"storage": {}, "power_pole": {}, "water_wheel": {}
}
const MATERIALS := [
	"iron_ore", "copper_ore", "gold_ore",
	"iron_ingot", "copper_ingot", "gold_ingot"
]
const START_COPPER := 0

var stock: Dictionary = {}
var free_depot_available := false
var relocation_kits: Dictionary = {}

func _init() -> void:
	for material in MATERIALS:
		stock[material] = 0
	# No free starter material is required: every building is cost-free.

func cost(kind: String) -> Dictionary:
	return COSTS.get(kind, {}).duplicate()

func registered(kind: String) -> bool:
	return COSTS.has(kind)

func available(material: String) -> int:
	return int(stock.get(material, 0))

func has_free_kit(_kind: String) -> bool:
	# Legacy saves may contain unused coupons; free buildings no longer need
	# or consume them, and no new relocation kits are created.
	return false

func return_kit(_kind: String) -> void:
	pass

func has_funds(kind: String) -> bool:
	return registered(kind)

func pay(kind: String) -> Dictionary:
	# No resource debt, including for generators and future machines.
	if not registered(kind):
		return {}
	return {}

func refund(paid: Dictionary) -> void:
	# One-time compensation for genuinely paid legacy buildings, if removed.
	for material in paid:
		if material in MATERIALS:
			stock[material] = available(material) + maxi(0, int(paid[material]))


func credit(material: String, amount: int = 1) -> bool:
	if not material in MATERIALS or amount <= 0:
		return false
	stock[material] = available(material) + amount
	return true

func missing(_kind: String) -> Dictionary:
	return {}

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
	
	free_depot_available = true
