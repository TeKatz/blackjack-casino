extends RefCounted
## Local Descent-only persistence. Never reads or writes casino balances.
const Content = preload("res://content.gd")
const PATH := "user://descent_progress.cfg"
# Each completed boss awards one persistent soul at run settlement.
# Costs are intentionally nonlinear: a fully maxed tree takes many expeditions.
const SOUL_COST_BY_RANK := [1,2,4,7,11]
var essence := 0
var boss_souls := 0
var last_souls := 0
var attempts := 0
var wins := 0
var total_kills := 0
var elite_kills := 0
var achievements: Array[String] = []
var deepest_boss := 0
var character_wins := {"warden":0,"dealer":0,"arcanist":0,"soulbinder":0}
var unlocked: Array[String] = ["warden_guard"]
var ranks := {"vitality":0,"power":0,"agility":0,"alchemy":0,"arcane":0,"fortune":0,"resolve":0,"recovery":0}
var discovered: Array[String] = []
var selected := "warden_guard"
var last_reward := 0
var save_ok := true

func load_save() -> void:
	var cfg := ConfigFile.new()
	if cfg.load(PATH) != OK:
		return
	essence = clampi(int(cfg.get_value("profile","essence",0)),0,100000000)
	boss_souls=clampi(int(cfg.get_value("profile","boss_souls",0)),0,100000000)
	last_souls=0
	attempts = maxi(0,int(cfg.get_value("profile","attempts",0)))
	wins = clampi(int(cfg.get_value("profile","wins",0)),0,attempts)
	total_kills = maxi(0,int(cfg.get_value("profile","kills",0)))
	elite_kills=maxi(0,int(cfg.get_value("profile","elites",0)))
	deepest_boss=clampi(int(cfg.get_value("profile","deepest_boss",0)),0,3)
	for id in character_wins: character_wins[id]=maxi(0,int(cfg.get_value("character_wins",id,0)))
	var saved_achievements: Variant=cfg.get_value("profile","achievements",[])
	if saved_achievements is Array:
		for id in saved_achievements:
			if id is String and not achievements.has(id): achievements.append(id)
	var saved: Variant = cfg.get_value("profile","unlocked",[])
	if saved is Array:
		for id in saved:
			if Content.SETS.has(id) and not unlocked.has(id): unlocked.append(id)
	for key in ranks:
		ranks[key] = clampi(int(cfg.get_value("upgrades",key,0)),0,int(Content.UPGRADES[key][5]))
	var seen: Variant = cfg.get_value("profile","discovered",[])
	if seen is Array:
		for id in seen:
			if Content.RELICS.has(id) and not discovered.has(id): discovered.append(id)
	var wanted := str(cfg.get_value("profile","selected","warden_guard"))
	if unlocked.has(wanted): selected = wanted

func save() -> bool:
	var cfg := ConfigFile.new()
	cfg.set_value("profile","essence",essence)
	cfg.set_value("profile","boss_souls",boss_souls)
	cfg.set_value("profile","attempts",attempts)
	cfg.set_value("profile","wins",wins)
	cfg.set_value("profile","kills",total_kills)
	cfg.set_value("profile","elites",elite_kills)
	cfg.set_value("profile","deepest_boss",deepest_boss)
	for id in character_wins: cfg.set_value("character_wins",id,character_wins[id])
	cfg.set_value("profile","achievements",achievements)
	cfg.set_value("profile","unlocked",unlocked)
	cfg.set_value("profile","discovered",discovered)
	cfg.set_value("profile","selected",selected)
	for key in ranks: cfg.set_value("upgrades",key,ranks[key])
	save_ok = cfg.save(PATH) == OK
	return save_ok

func settle(run) -> int:
	if run.settled or run.state not in ["victory","defeat"]:
		return 0
	run.settled = true
	last_souls=clampi(int(run.bosses_defeated),0,3)
	boss_souls=mini(100000000,boss_souls+last_souls)
	attempts += 1
	total_kills += run.kills
	elite_kills+=run.elites
	if elite_kills>=5 and not achievements.has("elite_hunter"):
		achievements.append("elite_hunter")
		essence+=25
	deepest_boss=maxi(deepest_boss,run.bosses_defeated)
	if (run.bosses_defeated>=2 or run.state=="victory") and not achievements.has("depths_conquered"):
		achievements.append("depths_conquered")
		if not unlocked.has("arcanist_fire"): unlocked.append("arcanist_fire")
	if run.bosses_defeated>=3 and not achievements.has("hollow_crown"):
		achievements.append("hollow_crown")
		if not unlocked.has("soulbinder_reaper"): unlocked.append("soulbinder_reaper")
	if run.state == "victory":
		wins += 1
		character_wins[run.character]+=1
		var mastery: String="mastery_"+run.character
		if not achievements.has(mastery):
			achievements.append(mastery)
			essence+=30
	last_reward = 3+run.combat_clears*2+run.bosses_defeated*6+run.elites*2+(12 if run.state=="victory" else 0)
	# A small consolation for a genuine attempt; no payout for instantly abandoning.
	if run.state=="defeat" and run.kills==0 and run.combat_clears==0: last_reward=0
	last_reward += int(floor(last_reward*0.05*int(ranks.fortune)))
	essence += last_reward
	for id in run.relics:
		if not discovered.has(id): discovered.append(id)
	return last_reward

func cost(key: String) -> int:
	if not ranks.has(key) or not Content.UPGRADES.has(key): return 0
	if int(ranks[key])>=int(Content.UPGRADES[key][5]): return 0
	return int(SOUL_COST_BY_RANK[clampi(int(ranks[key]),0,SOUL_COST_BY_RANK.size()-1)])

func buy_upgrade(key: String) -> bool:
	if not ranks.has(key) or not Content.UPGRADES.has(key): return false
	if int(ranks[key])>=int(Content.UPGRADES[key][5]): return false
	var price: int=cost(key)
	if price<=0 or boss_souls<price: return false
	boss_souls-=price
	ranks[key]+=1
	return true

func unlock_set(id: String) -> bool:
	if not Content.SETS.has(id) or unlocked.has(id): return false
	var required: String=Content.required_set(id)
	if not required.is_empty() and not unlocked.has(required): return false
	var price: int = Content.SETS[id].cost
	if essence<price: return false
	essence -= price
	unlocked.append(id)
	return true

func smoke_test() -> bool:
	var model = preload("res://run_model.gd").new()
	model.begin(42)
	model.state="victory"
	model.shards=30
	model.relics.append("echo")
	var amount: int=settle(model)
	if amount!=15 or settle(model)!=0 or wins!=1 or not discovered.has("echo"): return false
	if boss_souls!=0 or buy_upgrade("vitality"): return false
	# Every boss, including those conquered in a run ending in defeat,
	# awards exactly one soul. Settlement is deliberately idempotent.
	model.begin(43,"dealer_cards",ranks)
	model.state="defeat"
	model.kills=3
	model.bosses_defeated=2
	var old_essence: int=essence
	if settle(model)<=0 or boss_souls!=2 or last_souls!=2: return false
	if settle(model)!=0 or boss_souls!=2: return false
	if not buy_upgrade("vitality") or ranks.vitality!=1 or boss_souls!=1: return false
	if cost("vitality")!=2 or buy_upgrade("vitality"): return false
	if essence<=old_essence: return false
	# Higher ranks cost 1,2,4,7,11 souls. Never let essence buy them.
	boss_souls=24
	if not buy_upgrade("vitality") or boss_souls!=22 or ranks.vitality!=2: return false
	if not buy_upgrade("vitality") or boss_souls!=18 or ranks.vitality!=3: return false
	if not buy_upgrade("vitality") or boss_souls!=11 or ranks.vitality!=4: return false
	if not buy_upgrade("vitality") or boss_souls!=0 or ranks.vitality!=5: return false
	if buy_upgrade("vitality") or cost("vitality")!=0: return false
	# Existing essence is still useful for unlocking character loadouts.
	essence=100
	if not unlock_set("dealer_cards") or unlock_set("dealer_cards"): return false
	model.begin(44,"dealer_cards",ranks)
	if model.hp!=100 or not model.relics.is_empty() or model.character!="dealer": return false
	# Newly introduced paths must apply only to enemy damage / combat clears.
	boss_souls=30
	if not buy_upgrade("resolve") or not buy_upgrade("recovery"): return false
	if ranks.resolve!=1 or ranks.recovery!=1 or boss_souls!=28: return false
	model.begin(45,"warden_guard",ranks)
	if model.resolve_rank!=1 or model.recovery_rank!=1: return false
	model.state="defeat"
	model.kills=4
	model.combat_clears=2
	model.bosses_defeated=3
	if settle(model)!=7 or last_souls!=3 or boss_souls!=31: return false
	return true
