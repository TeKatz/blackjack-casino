extends RefCounted
## Stable IDs are used by saves, run logic and presentation.
const SETS := {
	"warden_guard": {"character":"warden","en":"Guardian","de":"Beschützer","hp":100.0,"damage":26.0,"bolt":17.0,"speed":245.0,"attack":0.36,"special":0.62,"dash":1.3,"cost":0},
	"warden_executioner": {"character":"warden","en":"Executioner","de":"Vollstrecker","hp":115.0,"damage":44.0,"bolt":22.0,"speed":215.0,"attack":0.65,"special":2.2,"dash":1.5,"cost":35},
	"dealer_cards": {"character":"dealer","en":"Cardsharp","de":"Kartenmeister","hp":75.0,"damage":11.0,"bolt":17.0,"speed":270.0,"attack":0.33,"special":1.45,"dash":1.1,"cost":55},
	"dealer_joker": {"character":"dealer","en":"Joker","de":"Joker","hp":85.0,"damage":12.0,"bolt":22.0,"speed":255.0,"attack":0.41,"special":1.9,"dash":1.2,"cost":85},
	"arcanist_fire":{"character":"arcanist","en":"Pyromancer","de":"Pyromant","hp":80.0,"damage":16.0,"bolt":38.0,"speed":250.0,"attack":0.34,"special":1.8,"dash":1.25,"cost":100},
	"arcanist_frost":{"character":"arcanist","en":"Frostweaver","de":"Frostweber","hp":90.0,"damage":15.0,"bolt":28.0,"speed":240.0,"attack":0.35,"special":1.6,"dash":1.2,"cost":125},
	"soulbinder_reaper":{"character":"soulbinder","en":"Reaper","de":"Schnitter","hp":105.0,"damage":31.0,"bolt":36.0,"speed":240.0,"attack":0.46,"special":2.1,"dash":1.35,"cost":140},
	"soulbinder_wraith":{"character":"soulbinder","en":"Wraithcaller","de":"Geisterrufer","hp":82.0,"damage":19.0,"bolt":24.0,"speed":255.0,"attack":0.35,"special":2.4,"dash":1.2,"cost":175}
}
const RELICS := {
	"blade":["Gilded Edge","Goldene Klinge","+9 sword/card damage","+9 Hauptangriffsschaden",0],
	"heart":["Ruby Heart","Rubinherz","+20 max HP; heal 25","+20 max. LP; heilt 25",1],
	"boots":["Shadow Steps","Schattenschritte","+32 movement speed","+32 Bewegungstempo",2],
	"focus":["Arcane Focus","Arkaner Fokus","+11 special damage","+11 Spezialschaden",3],
	"ward":["Iron Ward","Eisenwall","+4 damage reduction","+4 Schadensreduktion",4],
	"fury":["Quickdraw","Schnellzug","Faster dash recharge","Kürzere Dash-Abklingzeit",5],
	"split":["Prism Shard","Prismasplitter","Extra projectiles on special attacks","Zusätzliche Spezialprojektile",6],
	"pierce":["Silver Needle","Silbernadel","Projectiles pierce one more enemy","Projektile durchdringen einen weiteren Gegner",7],
	"echo":["Echo Ring","Echoring","Every third main attack releases a bolt","Jeder dritte Hauptangriff erzeugt ein Geschoss",8],
	"ember":["Ember Seal","Glutsiegel","Hits burn enemies for 3 seconds","Treffer verbrennen Gegner 3 Sekunden",9],
	"storm":["Storm Mark","Sturmmal","Burning enemies arc lightning on hit","Brennende Gegner lösen Kettenblitze aus",10],
	"leech":["Vampire Fang","Vampirzahn","Recover 1 HP per defeated enemy","1 LP je besiegtem Gegner",11],
	"dashfire":["Cinder Steps","Glutschritte","Dash damages nearby enemies","Dash schädigt Gegner in der Nähe",12],
	"crit":["Loaded Coin","Gezinkte Münze","20% chance of double damage","20 % Chance auf doppelten Schaden",13],
	"tempo":["Clockwork","Uhrwerk","Main attack 12% faster","Hauptangriff 12 % schneller",14],
	"potion":["Alchemist Flask","Alchemistenflasche","Active heal gains 8 HP per stack","Aktive Heilung +8 LP je Stapel",15],
	"bounce":["Mirror Shard","Spiegelscherbe","Shots bounce off walls","Geschosse prallen ab",16],
	"frost":["Frost Heart","Frostherz","Hits chill enemies","Treffer verlangsamen Gegner",17],
	"shatter":["Shatter Tomb","Splittergrab","Chilled enemies burst on death","Unterkühlte Gegner explodieren",18],
	"sun":["Black Sun","Schwarze Sonne","Orbit damages nearby enemies","Orbit schädigt nahe Gegner",19],
	"curse":["Cursed Die","Verfluchter Würfel","Random damage modifier each room","Zufälliger Schadensbonus je Raum",20],
	"phoenix":["Phoenix Feather","Phönixfeder","Revive once per run","Einmalige Wiederbelebung",21],
	"cascade":["Arcane Cascade","Arkane Kaskade","Special attacks cause explosions","Spezialangriffe erzeugen Explosionen",22],
	"twin":["Twin Soul","Zwillingsseele","Main attacks release an extra bolt","Hauptangriffe erzeugen Zusatzgeschoss",23],
	"sanguine":["Sanguine Pact","Blutpakt","+6 attack, -8 max HP","+6 Angriff, -8 max. LP",24],
	"bulwark":["Bulwark","Bollwerk","+3 armor","+3 Rüstung",25],
	"swift":["Swift Feather","Windfeder","+18 movement speed","+18 Bewegungstempo",26],
	"mana":["Mana Crystal","Manakristall","Special cooldown -15%","Spezial-Abklingzeit -15 %",27],
	"titan":["Titan Bone","Titanenknochen","+30 max HP","+30 max. LP",28],
	"needle":["Thorn Needle","Dornnadel","+5 projectile damage","+5 Spezialschaden",29],
	"charge":["Charged Boots","Ladungsstiefel","Dash cooldown -0.15s","Dash-Abklingzeit -0,15 s",30],
	"medic":["Medic Seal","Heilsiegel","Heal 3 HP on room entry","3 LP bei Raumbetritt",31],
	"greed":["Gilded Purse","Goldbeutel","+2 shards per kill","+2 Splitter je Gegner",32],
	"battery":["Soul Battery","Seelenbatterie","Active cooldown -3s","Aktiv-Abklingzeit -3 s",33],
	"glass":["Glass Blade","Glasklinge","+16 attack, -3 armor","+16 Angriff, -3 Rüstung",34],
	"warding":["Warding Rune","Schutzrune","Longer hurt invulnerability","Längere Treffer-Unverwundbarkeit",35],
	"venom":["Venom Vial","Giftampulle","Hits poison enemies","Treffer vergiften Gegner",36],
	"nova":["Nova Core","Novakern","Dash releases a burst","Dash erzeugt Explosion",37],
	"hunter":["Hunter Eye","Jägerauge","+8 damage against elites/bosses","+8 Schaden gegen Elite/Bosse",38],
	"blood":["Blood Moon","Blutmond","Low HP increases damage","Wenig LP erhöht Schaden",39],
	"harvest":["Soul Chalice","Seelenkelch","Every 5 kills restores 8 HP","Jeder 5. Kill heilt 8 LP",40],
	"execution":["Executioner's Mark","Richtmal","Deal 30% more damage below 25% enemy HP","30 % mehr Schaden bei Gegnern unter 25 % LP",41],
	"guardpulse":["Aegis Bell","Ägisglocke","Taking damage releases a shockwave","Erlittener Schaden erzeugt eine Schockwelle",42],
	"frostfire":["Frostfire Sigil","Frostfeuersiegel","Burning chilled foes take +8 damage per hit","Brennende unterkühlte Gegner erleiden +8 Schaden",43],
	"soulstorm":["Soulstorm","Seelensturm","Kills release 3 spirit bolts","Kills erzeugen 3 Seelengeschosse",44],
	"overcharge":["Overcharge Coil","Überladungsspule","Every fourth special attack deals double damage","Jeder vierte Spezialangriff verursacht doppelten Schaden",45],
	"bounty":["Bounty Seal","Kopfgeldsiegel","Elite and boss kills award +5 shards","Elite- und Bosskills geben +5 Splitter",46],
	"sacrifice":["Crimson Crown","Blutkrone","+20% damage, lose 2 HP on new combat rooms","+20 % Schaden, -2 LP bei neuen Kampfräumen",47],
	"w_bastion":["Thorn Bastion","Dornenbastion","Taking damage strikes nearby enemies.","Bei Treffern erleiden nahe Gegner Gegenschaden.",48],
	"w_oath":["Oathstone","Eidstein","Every 4 kills grants brief protection.","Alle 4 Kills gewähren kurz Schutz.",49],
	"w_banner":["Rally Banner","Banner des Muts","Recover 6 HP after clearing a combat room.","Heilt nach jedem gesäuberten Kampfraum 6 LP.",50],
	"w_lance":["Sentinel Lance","Wächterlanze","Special attacks launch a piercing lance.","Spezialangriffe verschießen eine durchdringende Lanze.",51],
	"w_steel":["Living Plate","Lebendige Platte","+18 maximum HP and +1 armor.","+18 maximale LP und +1 Rüstung.",52],
	"w_charger":["Shieldbreaker","Schildbrecher","Dash landing releases an armored shockwave.","Dash-Landung entfesselt eine Schildschockwelle.",53],
	"d_marked":["Marked Deck","Gezinktes Deck","Every fourth main attack fires a card fan.","Jeder vierte Hauptangriff feuert einen Kartenfächer.",54],
	"d_sleight":["Sleight of Hand","Fingerfertigkeit","Your shots ricochet off one extra wall.","Geschosse prallen einmal zusätzlich ab.",55],
	"d_coin":["Jinx Coin","Fluchmünze","Critical hits mark enemies for extra damage.","Kritische Treffer markieren Gegner für Bonusschaden.",56],
	"d_decoy":["Smoke Joker","Rauchjoker","Dash landings burst into damaging smoke.","Dash-Landungen erzeugen eine schädliche Rauchwolke.",57],
	"d_ace":["Death Ace","Todesass","Every fifth special fires a heavy ace.","Jeder fünfte Spezialangriff wirft ein mächtiges Ass.",58],
	"d_purse":["House Treasury","Hauskasse","Defeated enemies drop +2 shards.","Besiegte Gegner lassen +2 Splitter fallen.",59],
	"a_ember":["Primordial Ember","Urglut","Your hits ignite enemies.","Deine Treffer entzünden Gegner.",60],
	"a_ash":["Ash Catalyst","Aschenkatalysator","Burning enemies explode on death.","Brennende Feinde explodieren beim Tod.",61],
	"a_frost":["Glacier Sigil","Gletschersiegel","Special casts freeze nearby enemies.","Spezialangriffe unterkühlen nahe Gegner.",62],
	"a_prism":["Crystal Lens","Kristalllinse","Chilled targets suffer bonus hit damage.","Unterkühlte Ziele erleiden Bonusschaden.",63],
	"a_orbs":["Astral Wisps","Astrallichter","Summon magical wisps that fire at enemies.","Beschwört magische Lichter, die Gegner angreifen.",64],
	"a_surge":["Arcane Dynamo","Arkan-Dynamo","Every third special triggers a pulse.","Jeder dritte Spezialangriff erzeugt eine Druckwelle.",65],
	"s_harvest":["Grim Scythe","Seelensense","Kills yield an additional soul.","Kills gewähren eine zusätzliche Seele.",66],
	"s_grim":["Soul Brand","Seelenbrand","Specials spend a soul to fire a cursed bolt.","Spezialangriffe opfern eine Seele für einen Fluchschuss.",67],
	"s_haunt":["Wraith Lantern","Geisterlaterne","Kills summon a seeking spirit projectile.","Kills beschwören ein zielsuchendes Geistergeschoss.",68],
	"s_coven":["Grave Choir","Grabchor","Every fifth kill releases a death pulse.","Jeder fünfte Kill entfesselt eine Todeswelle.",69],
	"s_blood":["Blood Vow","Bluteid","Taking damage restores one soul.","Erlittener Schaden gewährt eine Seele.",70],
	"s_pact":["Last Breath","Letzter Atemzug","Below 40% HP your specials grow stronger.","Unter 40 % LP verursachen Spezialangriffe mehr Schaden.",71]
}
# Six exclusive-to-class relics per class. Base relics 0–47 remain shared.
const CLASS_POOLS := {
	"warden":["w_bastion","w_oath","w_banner","w_lance","w_steel","w_charger"],
	"dealer":["d_marked","d_sleight","d_coin","d_decoy","d_ace","d_purse"],
	"arcanist":["a_ember","a_ash","a_frost","a_prism","a_orbs","a_surge"],
	"soulbinder":["s_harvest","s_grim","s_haunt","s_coven","s_blood","s_pact"]
}
static func relic_owner(id: String) -> String:
	for character in CLASS_POOLS:
		if CLASS_POOLS[character].has(id): return str(character)
	return ""
# Two-relic build transformations. IDs are stable and derived from the owned
# relic inventory, so duplicates and run restarts cannot grant phantom combos.
const SYNERGIES := {
	"toxic_jackpot":{"parts":["venom","crit"],"en":"Toxic Jackpot","de":"Gift-Jackpot","en_effect":"Critical hits against poisoned foes explode, spreading poison.","de_effect":"Kritische Treffer gegen vergiftete Gegner explodieren und verteilen Gift."},
	"wildfire_circuit":{"parts":["ember","storm"],"en":"Wildfire Circuit","de":"Flammenkette","en_effect":"Striking burning foes chains fire lightning to three enemies.","de_effect":"Treffer gegen brennende Gegner leiten Feuerblitze zu drei Zielen."},
	"permafrost":{"parts":["frost","shatter"],"en":"Permafrost Burst","de":"Eisgrab","en_effect":"Chilled enemies explode on death and freeze nearby foes.","de_effect":"Unterkühlte Feinde explodieren beim Tod und vereisen weitere Ziele."},
	"inferno_rush":{"parts":["dashfire","nova"],"en":"Inferno Rush","de":"Infernosturm","en_effect":"Every dash ends in a burning explosion.","de_effect":"Jeder Dash endet in einer brennenden Explosion."},
	"ricochet_storm":{"parts":["pierce","bounce"],"en":"Ricochet Storm","de":"Abprallsturm","en_effect":"Piercing projectiles split into two ricochets on enemy impact.","de_effect":"Durchschlagende Geschosse teilen sich beim Treffer in zwei Abpraller."},
	"soul_requiem":{"parts":["soulstorm","leech"],"en":"Soul Requiem","de":"Seelenrequiem","en_effect":"Kills restore extra health and unleash six spirit bolts.","de_effect":"Kills heilen zusätzlich und entfesseln sechs Seelengeschosse."},
	"w_fortress":{"parts":["w_bastion","w_steel"],"en":"Thorn Fortress","de":"Dornenfestung","en_effect":"Retaliation becomes a freezing, heavy shockwave.","de_effect":"Gegenangriffe werden zu mächtigen Frost-Schockwellen."},
	"w_sanctuary":{"parts":["w_oath","w_banner"],"en":"Unbroken Sanctuary","de":"Ungebrochener Hort","en_effect":"Every fourth kill grants healing, shield and a rally pulse.","de_effect":"Jeder vierte Kill bringt Heilung, Schutz und eine Mutwelle."},
	"w_phalanx":{"parts":["w_lance","w_charger"],"en":"Phalanx Rush","de":"Phalanxansturm","en_effect":"Dash landings release three armor-piercing lances.","de_effect":"Dash-Landungen entfesseln drei durchdringende Lanzen."},
	"d_royal":{"parts":["d_marked","d_sleight"],"en":"Royal Flush","de":"Royal Flush","en_effect":"Every fourth attack fans five ricocheting cards.","de_effect":"Jeder vierte Angriff entfesselt fünf abprallende Karten."},
	"d_bluff":{"parts":["d_coin","d_decoy"],"en":"Double Bluff","de":"Doppelbluff","en_effect":"Smoke explosions punish marked foes and spread curses.","de_effect":"Rauchexplosionen bestrafen markierte Gegner und verteilen Flüche."},
	"d_jackpot":{"parts":["d_ace","d_purse"],"en":"House Jackpot","de":"Hausjackpot","en_effect":"Every fifth special fires three aces and pays bonus shards.","de_effect":"Jeder fünfte Spezialangriff wirft drei Asse und bringt Extrasplitter."},
	"a_inferno":{"parts":["a_ember","a_ash"],"en":"Inferno Chain","de":"Infernokette","en_effect":"Burning kills cause wide, spreading fire explosions.","de_effect":"Brennende Kills erzeugen weite Kettenexplosionen."},
	"a_crystal":{"parts":["a_frost","a_prism"],"en":"Crystal Cataclysm","de":"Kristallkataklysmus","en_effect":"Frozen targets shatter into seeking ice shards.","de_effect":"Unterkühlte Ziele zerbersten in zielsuchende Eissplitter."},
	"a_convergence":{"parts":["a_orbs","a_surge"],"en":"Astral Convergence","de":"Astralkonvergenz","en_effect":"Wisps double their fire and special pulses strike twice.","de_effect":"Astrallichter feuern doppelt und Spezialwellen schlagen zweimal."},
	"s_requiem":{"parts":["s_harvest","s_grim"],"en":"Dark Requiem","de":"Dunkles Requiem","en_effect":"Spent souls heal and unleash extra dark bolts.","de_effect":"Geopferte Seelen heilen und entfesseln zusätzliche Fluchschüsse."},
	"s_legion":{"parts":["s_haunt","s_coven"],"en":"Legion of Echoes","de":"Legion der Echos","en_effect":"Spirit kills trigger more seeking spirits and grave pulses.","de_effect":"Geisterkills erzeugen mehr Geistergeschosse und Todeswellen."},
	"s_undying":{"parts":["s_blood","s_pact"],"en":"Undying Pact","de":"Unsterblicher Pakt","en_effect":"Low-health hits grant a shield and a vengeance blast.","de_effect":"Treffer bei wenig LP gewähren Schild und Vergeltungsexplosion."}
}
const UPGRADES := {
	"vitality":["Vitality","Vitalität","+5 starting HP per rank","+5 Start-LP je Rang",12,5],
	"power":["Power","Kraft","+2 main attack damage per rank","+2 Hauptangriffsschaden je Rang",15,5],
	"agility":["Agility","Agilität","+8 movement speed per rank","+8 Bewegungstempo je Rang",12,4],
	"alchemy":["Alchemy","Alchemie","+3 active heal per rank","+3 aktive Heilung je Rang",14,4],
	"arcane":["Arcane mastery","Arkane Meisterschaft","+3 starting special damage per rank","+3 Start-Spezialschaden je Rang",18,4],
	"fortune":["Fortune","Fortuna","+5% expedition essence per rank","+5 % Expeditionsessenz je Rang",20,4],
	"resolve":["Resolve","Entschlossenheit","Take 3% less enemy damage per rank","3 % weniger gegnerischer Schaden je Rang",24,4],
	"recovery":["Battle Recovery","Kampferholung","Recover 1 HP after each combat room per rank","1 LP Regeneration nach jedem Kampfraum je Rang",25,4]
}
static func rarity(id: String) -> int:
	var index: int = RELICS[id][4]
	if index>=48: return 1 if (index-48)%6<2 else 2
	return 3 if id in ["phoenix","cascade","twin","soulstorm","sacrifice"] else (2 if index>=20 else (1 if index>=8 else 0))

static func relic_text(id: String, german: bool) -> String:
	var item: Array = RELICS[id]
	return item[1 if german else 0]+"  •  "+item[3 if german else 2]
static func loadout_display_name(id: String, german: bool) -> String:
	return SETS[id]["de" if german else "en"]

static func character_name(id: String, german: bool) -> String:
	match id:
		"warden": return "Wächter" if german else "Warden"
		"dealer": return "Dealer"
		"arcanist": return "Arkanist" if german else "Arcanist"
		"soulbinder": return "Seelenbinder" if german else "Soulbinder"
	return id
static func floor_name(index: int, german: bool) -> String:
	return ["Das vergessene Gewölbe","Der Abgrundgarten","Die hohle Krone"][index] if german else ["The Forgotten Vault","The Abyss Garden","The Hollow Crown"][index]
static func boss_name(index: int, german: bool) -> String:
	return ["Der Tresorwächter","Der Wurzelkönig","Der Hohle König"][index] if german else ["The Vault Keeper","The Root King","The Hollow King"][index]
static func required_set(id: String) -> String:
	return {"dealer_joker":"dealer_cards","arcanist_frost":"arcanist_fire","soulbinder_wraith":"soulbinder_reaper"}.get(id,"")
