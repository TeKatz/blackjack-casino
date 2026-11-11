extends RefCounted
## Stable IDs are used by saves, run logic and presentation.
const SETS := {
	"warden_guard": {"character":"warden","en":"Guardian","de":"Beschützer","hp":100.0,"damage":26.0,"bolt":17.0,"speed":245.0,"attack":0.36,"special":0.62,"dash":1.3,"cost":0},
	"warden_executioner": {"character":"warden","en":"Executioner","de":"Vollstrecker","hp":115.0,"damage":44.0,"bolt":22.0,"speed":215.0,"attack":0.65,"special":2.2,"dash":1.5,"cost":35},
	"dealer_cards": {"character":"dealer","en":"Cardsharp","de":"Kartenmeister","hp":75.0,"damage":14.0,"bolt":20.0,"speed":270.0,"attack":0.24,"special":1.1,"dash":1.1,"cost":55},
	"dealer_joker": {"character":"dealer","en":"Joker","de":"Joker","hp":85.0,"damage":17.0,"bolt":27.0,"speed":255.0,"attack":0.32,"special":1.5,"dash":1.2,"cost":85},
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
	"bounty":["Bounty Seal","Kopfgeldsiegel","Elite and boss kills award +10 shards","Elite- und Bosskills geben +10 Splitter",46],
	"sacrifice":["Crimson Crown","Blutkrone","+20% damage, lose 2 HP on new combat rooms","+20 % Schaden, -2 LP bei neuen Kampfräumen",47]
}
const UPGRADES := {
	"vitality":["Vitality","Vitalität","+5 starting HP per rank","+5 Start-LP je Rang",12,5],
	"power":["Power","Kraft","+2 main attack damage per rank","+2 Hauptangriffsschaden je Rang",15,5],
	"agility":["Agility","Agilität","+8 movement speed per rank","+8 Bewegungstempo je Rang",12,4],
	"alchemy":["Alchemy","Alchemie","+3 active heal per rank","+3 aktive Heilung je Rang",14,4],
	"arcane":["Arcane mastery","Arkane Meisterschaft","+3 starting special damage per rank","+3 Start-Spezialschaden je Rang",18,4],
	"fortune":["Fortune","Fortuna","+5% expedition essence per rank","+5 % Expeditionsessenz je Rang",20,4]
}
static func rarity(id: String) -> int:
	var index: int = RELICS[id][4]
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
