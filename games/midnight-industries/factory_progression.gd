extends RefCounted
## Permanent unlocks earned from physically completed depot-delivery jobs.
## Keep the job definitions here; future stages extend the catalog without
## hardcoded UI switches or construction-phase coupling.
signal job_completed(id: String)
const START_MACHINES := ["miner", "belt", "depot"]
const JOBS := [
	{
		"id": "first_iron_delivery",
		"stage": 1,
		"de": "Die erste Lieferung",
		"en": "The first delivery",
		"description_de": "Liefere 100 Eisenerz an ein Depot.",
		"description_en": "Deliver 100 iron ore to a depot.",
		"delivery": {"iron_ore": 100},
		"unlocks": ["power_pole", "water_wheel", "smelter"],
		"slots_reward": 1,
		"reward_de": "Strommast · Wasserrad · Schmelzofen · +1 Stromplatz",
		"reward_en": "Power pole · Water wheel · Smelting furnace · +1 power slot"
	}
]
var progress: Dictionary = {}
var completed: Array[String] = []
var power_slots := 0

func job(id: String) -> Dictionary:
	for entry in JOBS:
		if str(entry.id) == id:
			return entry
	return {}

func is_complete(id: String) -> bool:
	return completed.has(id)

func current_job() -> Dictionary:
	for entry in JOBS:
		if not is_complete(str(entry.id)):
			return entry
	return {}

func delivered_count(id: String, material: String) -> int:
	var record: Dictionary = progress.get(id, {})
	return int(record.get(material, 0))

func unlocked(kind: String) -> bool:
	if START_MACHINES.has(kind):
		return true
	for id in completed:
		if kind in job(id).get("unlocks", []):
			return true
	return false

func record_delivery(material: String, amount: int = 1) -> void:
	if amount <= 0:
		return
	# Only count new, real deliveries. Stock changes or item recycling never
	# advance a quest retroactively.
	var entry: Dictionary = current_job()
	if entry.is_empty():
		return
	var id: String = str(entry.id)
	var requirements: Dictionary = entry.delivery
	if not requirements.has(material):
		return
	var counters: Dictionary = progress.get(id, {}).duplicate()
	counters[material] = mini(int(requirements[material]),
		int(counters.get(material, 0)) + amount)
	progress[id] = counters
	var ready := true
	for key in requirements:
		if int(counters.get(key, 0)) < int(requirements[key]):
			ready = false
	if ready:
		complete_job(id)

func complete_job(id: String) -> void:
	if is_complete(id) or job(id).is_empty():
		return
	var entry: Dictionary = job(id)
	if current_job().get("id", "") != id:
		return
	var requirements: Dictionary = entry.delivery
	for key in requirements:
		if int(progress.get(id, {}).get(key, 0)) < int(requirements[key]):
			return
	completed.append(id)
	power_slots += maxi(0, int(entry.get("slots_reward", 0)))
	job_completed.emit(id)

func snapshot() -> Dictionary:
	return {"progress": progress.duplicate(true), "completed": completed.duplicate(),
		"power_slots": power_slots}

func restore(raw: Variant) -> bool:
	if typeof(raw) != TYPE_DICTIONARY:
		return false
	var counts: Variant = raw.get("progress", {})
	var done: Variant = raw.get("completed", [])
	var slots: Variant = raw.get("power_slots", 0)
	if typeof(counts) != TYPE_DICTIONARY or typeof(done) != TYPE_ARRAY \
		or typeof(slots) != TYPE_INT and typeof(slots) != TYPE_FLOAT:
		return false
	var accepted: Dictionary = {}
	for key in counts:
		var entry: Dictionary = job(str(key))
		if entry.is_empty() or typeof(counts[key]) != TYPE_DICTIONARY:
			return false
		var sub: Dictionary = {}
		for item in counts[key]:
			if not entry.delivery.has(item):
				return false
			var value: Variant = counts[key][item]
			if typeof(value) != TYPE_INT and typeof(value) != TYPE_FLOAT:
				return false
			if not is_finite(float(value)) or value < 0 or value > entry.delivery[item] \
				or float(int(value)) != float(value):
				return false
			sub[item] = int(value)
		accepted[key] = sub
	var completed_ids: Array[String] = []
	var earned := 0
	for id in done:
		if typeof(id) != TYPE_STRING or job(id).is_empty() or completed_ids.has(id):
			return false
		var entry: Dictionary = job(id)
		for item in entry.delivery:
			if int(accepted.get(id, {}).get(item, 0)) != int(entry.delivery[item]):
				return false
		completed_ids.append(id)
		earned += int(entry.get("slots_reward", 0))
	# Prevent tampering with available generator capacity; future missions
	# may award more but the number must always match completed milestones.
	if earned != int(slots) or float(earned) != float(slots):
		return false
	progress = accepted
	completed = completed_ids
	power_slots = earned
	return true

func restore_legacy(had_smelter: bool) -> void:
	# Pre-progression saves can already contain a furnace. Grandfather
	# their first unlock so they aren't permanently locked out of their
	# own factory. Existing ore stock by itself NEVER grants the job.
	if not had_smelter:
		return
	var entry: Dictionary = JOBS[0]
	progress[entry.id] = entry.delivery.duplicate()
	completed.append(str(entry.id))
	power_slots = int(entry.slots_reward)
