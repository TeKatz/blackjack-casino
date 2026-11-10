extends RefCounted
## Stable IDs are used by saves, run logic and presentation.
const SETS := {
	"warden_guard": {"character":"warden","en":"Guardian","de":"Beschützer","hp":100.0,"damage":26.0,"bolt":17.0,"speed":245.0,"attack":0.36,"special":0.62,"dash":1.3,"cost":0},
	"warden_executioner": {"character":"warden","en":"Executioner","de":"Vollstrecker","hp":115.0,"damage":44.0,"bolt":22.0,"speed":215.0,"attack":0.65,"special":2.2,"dash":1.5,"cost":35},
	"dealer_cards": {"character":"dealer","en":"Cardsharp","de":"Kartenmeister","hp":75.0,"damage":14.0,"bolt":20.0,"speed":270.0,"attack":0.24,"special":1.1,"dash":1.1,"cost":55},
	"dealer_joker": {"character":"dealer","en":"Joker","de":"Joker","hp":85.0,"damage":17.0,"bolt":27.0,"speed":255.0,"attack":0.32,"special":1.5,"dash":1.2,"cost":85}
}
const RELICS := {
	"blade":["Gilded Edge","Goldene Klinge","+9 sword/card damage","+9 Hauptangriffsschaden",0],
	"heart":["Ruby Heart","Rubinherz","+20 max HP; heal 35","+20 max. LP; heilt 35",1],
	"boots":["Shadow Steps","Schattenschritte","+32 movement speed","+32 Bewegungstempo",2],
	"focus":["Arcane Focus","Arkaner Fokus","+11 special damage","+11 Spezialschaden",3],
	"ward":["Iron Ward","Eisenwall","+4 damage reduction","+4 Schadensreduktion",4],
	"fury":["Quickdraw","Schnellzug","Faster dash recharge","Kürzere Dash-Abklingzeit",5],
	"split":["Prism Shard","Prismasplitter","Extra projectiles on special attacks","Zusätzliche Spezialprojektile",6],
	"pierce":["Silver Needle","Silbernadel","Projectiles pierce one more enemy","Projektile durchdringen einen weiteren Gegner",7],
	"echo":["Echo Ring","Echoring","Every third main attack releases a bolt","Jeder dritte Hauptangriff erzeugt ein Geschoss",8],
	"ember":["Ember Seal","Glutsiegel","Hits burn enemies for 3 seconds","Treffer verbrennen Gegner 3 Sekunden",9],
	"storm":["Storm Mark","Sturmmal","Burning enemies arc lightning on hit","Brennende Gegner lösen Kettenblitze aus",10],
	"leech":["Vampire Fang","Vampirzahn","Recover 2 HP per defeated enemy","2 LP je besiegtem Gegner",11],
	"dashfire":["Cinder Steps","Glutschritte","Dash damages nearby enemies","Dash schädigt Gegner in der Nähe",12],
	"crit":["Loaded Coin","Gezinkte Münze","20% chance of double damage","20 % Chance auf doppelten Schaden",13],
	"tempo":["Clockwork","Uhrwerk","Main attack 12% faster","Hauptangriff 12 % schneller",14],
	"potion":["Alchemist Flask","Alchemistenflasche","Active heal gains 15 HP per stack","Aktive Heilung +15 LP je Stapel",15]
}
const UPGRADES := {
	"vitality":["Vitality","Vitalität","+5 starting HP per rank","+5 Start-LP je Rang",12,5],
	"power":["Power","Kraft","+2 main attack damage per rank","+2 Hauptangriffsschaden je Rang",15,5],
	"agility":["Agility","Agilität","+8 movement speed per rank","+8 Bewegungstempo je Rang",12,4],
	"alchemy":["Alchemy","Alchemie","+5 active heal per rank","+5 aktive Heilung je Rang",14,4]
}
static func relic_text(id: String, german: bool) -> String:
	var item: Array = RELICS[id]
	return item[1 if german else 0]+"  •  "+item[3 if german else 2]
static func loadout_display_name(id: String, german: bool) -> String:
	return SETS[id]["de" if german else "en"]
