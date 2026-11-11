extends Node
## Original synthesized effects: independent envelopes, timbres and pitches.
const SPECS = {
	"swing":[0.18,170,0.8,0.72,0.4],"bolt":[0.22,570,0.25,0.08,1.5],
	"warden_guard_main":[0.19,160,0.9,0.76,0.45],"warden_guard_special":[0.32,520,0.3,0.18,1.7],
	"warden_executioner_main":[0.29,90,0.65,0.85,0.38],"warden_executioner_special":[0.45,110,0.8,0.65,0.46],
	"dealer_cards_main":[0.12,1050,1.2,0.30,0.65],"dealer_cards_special":[0.3,760,0.55,0.16,1.4],
	"dealer_joker_main":[0.16,830,0.75,0.3,0.8],"dealer_joker_special":[0.38,490,0.5,0.20,2.0],
	"arcanist_fire_main":[0.20,150,0.7,0.7,0.6],"arcanist_fire_special":[0.48,80,0.55,0.86,0.5],
	"arcanist_frost_main":[0.21,890,0.45,0.12,0.6],"arcanist_frost_special":[0.48,710,0.45,0.25,0.48],
	"soulbinder_reaper_main":[0.26,210,0.7,0.38,0.45],"soulbinder_reaper_special":[0.5,250,0.7,0.19,0.55],
	"soulbinder_wraith_main":[0.19,380,0.6,0.1,0.75],"soulbinder_wraith_special":[0.55,330,0.45,0.15,1.8],
	"enemy_stalker":[0.15,130,0.65,0.7,0.6],"enemy_caster":[0.23,420,0.5,0.15,0.65],
	"enemy_archer":[0.14,1250,0.9,0.5,0.45],"enemy_brute":[0.29,72,0.7,0.7,0.45],
	"enemy_shield":[0.21,240,0.5,0.5,0.45],"enemy_spore":[0.28,180,0.65,0.55,0.8],
	"enemy_vine":[0.26,360,0.8,0.45,0.5],"enemy_mushroom":[0.25,95,0.5,0.5,0.6],
	"enemy_revenant":[0.16,225,0.65,0.65,0.7],"enemy_hexer":[0.36,310,0.35,0.22,0.5],
	"enemy_sentinel":[0.32,82,0.6,0.85,0.4],"enemy_boss":[0.48,105,0.65,0.45,0.5],
	"enemy_rootking":[0.5,72,0.55,0.68,0.8],"enemy_hollowking":[0.55,155,0.45,0.3,0.45],
	"hit":[0.09,165,0.5,0.68,0.5],"hurt":[0.24,75,0.5,0.45,0.6],
	"kill":[0.19,260,0.5,0.3,0.55],"boss_kill":[0.65,130,0.3,0.3,1.7],
	"clear":[0.48,440,0.25,0.02,1.5],"relic":[0.45,780,0.25,0.02,1.5],
	"dash":[0.16,270,0.8,0.66,0.38],"spike_warn":[0.22,630,0.6,0.24,0.8],
	"spike_rise":[0.23,100,0.6,0.8,1.8],"spike_retract":[0.18,160,0.7,0.54,0.4],
	"sporepool":[0.34,150,0.5,0.4,0.8],"rune":[0.34,480,0.5,0.12,0.55],
	"rune_strike":[0.40,190,0.45,0.26,1.8],"rune_fade":[0.32,660,0.55,0.10,0.48],"portal_near":[0.32,440,0.25,0.03,1.35],"relic_inspect":[0.18,910,0.45,0.04,0.85],"wave":[0.65,180,0.35,0.28,1.6],"heal":[0.42,520,0.2,0.02,1.8],
	"time":[0.55,290,0.3,0.10,0.4],"bomb":[0.38,65,0.5,0.85,0.5],
	"shield":[0.35,360,0.5,0.12,1.3],"dice":[0.28,750,0.7,0.35,1.6],"mirror":[0.42,1080,0.3,0.08,0.5],
	"hover":[0.065,680,0.35,0.04,1.15],"click":[0.105,420,0.6,0.20,0.8],
	"map":[0.28,350,0.4,0.04,1.4],"shop":[0.32,870,0.3,0.08,1.3],
	"forge":[0.38,210,0.45,0.55,0.65],"treasure":[0.40,650,0.25,0.05,1.5],
	"event":[0.36,300,0.35,0.08,0.6],"shrine":[0.44,580,0.2,0.03,1.5],
	"secret":[0.40,440,0.4,0.05,0.75],"pause":[0.20,350,0.3,0.04,0.65],
	"victory":[0.85,392,0.25,0.02,1.7],"defeat":[0.70,175,0.4,0.08,0.45],
	"portal_charge":[0.82,260,0.30,0.11,1.7],"portal_enter":[0.60,420,0.27,0.10,0.52],"descend":[0.45,210,0.4,0.18,0.55],"purchase":[0.25,950,0.4,0.16,1.5],
	"navigate":[0.16,470,0.3,0.05,1.25],"unlock":[0.45,660,0.3,0.03,1.6],
	"contract_accept":[0.24,420,0.35,0.12,1.35],"contract_complete":[0.66,720,0.24,0.04,1.85],
	"forge_select":[0.62,190,0.55,0.35,1.65],"forge_guard":[0.33,230,0.35,0.22,0.63],
	"synergy_complete":[0.67,850,0.22,0.04,1.80],"dash_nova":[0.42,115,0.67,0.65,0.45],
	"boss_charge":[0.62,230,0.35,0.12,1.9],"boss_release":[0.65,95,0.55,0.46,0.52],
	"boss_phase":[0.90,150,0.28,0.38,1.95],
	"boss_reveal":[1.35,128,0.26,0.42,2.15],
	"boss_soul":[0.78,790,0.16,0.03,1.95],
	"class_cards":[0.17,1050,0.72,0.23,0.64],
	"class_ace":[0.27,690,0.5,0.18,1.5],
	"class_coin":[0.19,1280,0.25,0.06,1.34],
	"class_smoke":[0.32,170,0.71,0.76,0.41],
	"class_lance":[0.27,380,0.37,0.17,1.86],
	"class_shield":[0.38,175,0.63,0.52,0.72],
	"class_oath":[0.38,610,0.22,0.05,1.63],
	"class_thorns":[0.23,280,0.78,0.63,0.85],
	"class_banner":[0.32,570,0.16,0.07,1.3],
	"class_ice":[0.31,880,0.36,0.18,0.63],
	"class_crystal":[0.31,1040,0.22,0.09,1.5],
	"class_wisp":[0.17,950,0.19,0.04,1.55],
	"class_surge":[0.48,110,0.62,0.55,1.7],
	"class_ash":[0.38,140,0.75,0.78,0.51],
	"class_soul":[0.36,450,0.32,0.23,0.44],
	"class_ghost":[0.28,640,0.28,0.13,0.66],
	"class_grave":[0.43,105,0.52,0.68,0.6],
	"class_blood":[0.35,160,0.55,0.46,0.46],
	"relic_sun":[0.085,670,0.2,0.04,0.63],
	"sp_ritual_enter":[0.62,215,0.25,0.16,1.35],
	"sp_ritual_anchor":[0.38,1010,0.28,0.06,0.51],
	"sp_ritual_breach":[1.05,112,0.46,0.4,0.3],
	"sp_ritual_success":[0.93,550,0.2,0.1,2.14],
	"sp_survival_enter":[0.51,580,0.15,0.12,0.72],
	"sp_survival_wave":[0.43,1130,0.66,0.26,0.51],
	"sp_survival_success":[0.91,665,0.22,0.08,1.95],
	"sp_bounty_enter":[0.59,144,0.41,0.49,0.38],
	"sp_bounty_backup":[0.44,295,0.7,0.59,0.66],
	"sp_bounty_success":[0.96,980,0.2,0.07,1.62],
	"sp_unstable_enter":[0.64,520,0.18,0.19,1.66],
	"sp_unstable_charge":[0.73,780,0.38,0.13,2.33],
	"sp_unstable_burst":[0.48,96,0.77,0.9,0.4],
	"sp_unstable_success":[0.91,630,0.18,0.13,1.83],
	"sp_blood_altar_enter":[0.69,180,0.25,0.29,0.65],
	"sp_blood_altar_sacrifice":[0.86,87,0.52,0.67,1.85],
	"sp_blood_altar_success":[0.79,320,0.19,0.2,1.51],
	"sp_forgotten_forge_enter":[0.55,260,0.47,0.58,0.41],
	"sp_forgotten_forge_temper":[0.68,190,0.66,0.79,1.94],
	"sp_forgotten_forge_success":[0.87,440,0.26,0.28,1.63],
	"sp_archive_enter":[0.63,680,0.33,0.08,0.4],
	"sp_archive_exchange":[0.73,920,0.19,0.1,2.1],
	"sp_archive_success":[0.92,740,0.14,0.1,1.76]
}
var cache: Dictionary={}
var voices: Array[AudioStreamPlayer]=[]
var last: Dictionary={}
var cursor:=0
func _ready() -> void:
	for i in range(12):
		var voice:=AudioStreamPlayer.new()
		voice.volume_db=-7
		add_child(voice)
		voices.append(voice)
func play(kind: String) -> void:
	if not SPECS.has(kind) or voices.is_empty(): return
	var now:=Time.get_ticks_msec()
	if now-int(last.get(kind,-1000))<45: return
	last[kind]=now
	if not cache.has(kind): cache[kind]=synthesize(kind)
	var voice: AudioStreamPlayer=voices[cursor]
	for candidate in voices:
		if not candidate.playing:
			voice=candidate
			break
	cursor=(cursor+1)%voices.size()
	voice.stream=cache[kind]
	voice.volume_db=-15 if kind=="hover" else -7
	voice.play()
func synthesize(kind: String) -> AudioStreamWAV:
	var spec: Array=SPECS[kind]
	var duration: float=spec[0]
	var frequency: float=spec[1]
	var decay: float=spec[2]
	var noise_mix: float=spec[3]
	var sweep: float=spec[4]
	var sample:=AudioStreamWAV.new()
	sample.format=AudioStreamWAV.FORMAT_16_BITS
	sample.mix_rate=22050
	var count:=int(duration*22050)
	var data:=PackedByteArray()
	data.resize(count*2)
	var random:=RandomNumberGenerator.new()
	random.seed=kind.hash()
	var phase:=0.0
	var previous:=0.0
	for i in range(count):
		var t:=float(i)/count
		phase+=TAU*frequency*lerpf(1,sweep,t)/22050
		var noise:=random.randf_range(-1,1)
		previous=lerpf(previous,noise,0.38)
		var tone:=sin(phase)+0.22*sin(phase*2.01)+0.12*sin(phase*3.04)
		if kind in ["relic","clear","victory","unlock","shrine","purchase"]: tone+=0.33*sin(phase*1.5)+0.18*sin(phase*2.5)
		var envelope:=minf(1,t*35)*pow(1-t,1.2+decay*3)
		var value:=lerpf(tone,noise*0.55+previous,noise_mix)*envelope
		# Different sound families have different physical textures, not just pitch.
		if kind.begins_with("dealer"):
			value=(noise*0.8*exp(-t*15)+sin(phase*2.8)*0.38*exp(-t*5)+tone*0.18)*minf(1,t*70)*(1-t)
		elif kind.begins_with("arcanist_fire") or kind=="bomb":
			value=(previous*0.85+sin(phase)*0.42+noise*0.20)*envelope*(0.7+0.3*sin(t*28))
		elif kind.begins_with("arcanist_frost") or kind=="mirror":
			value=(sin(phase)+0.46*sin(phase*2.76)+0.26*sin(phase*4.11))*envelope*0.62
		elif kind.begins_with("soulbinder") or kind=="enemy_hollowking":
			value=(sin(phase+0.8*sin(t*16))+0.5*sin(phase*1.51)+previous*0.18)*envelope*0.68
		elif kind.begins_with("warden") or kind=="spike_rise" or kind=="spike_retract" or kind=="forge":
			value=(noise*0.65+previous*0.4+sin(phase*3.7)*0.36*exp(-t*3)+sin(phase)*0.25)*envelope
		elif kind.begins_with("sp_ritual_"):
			# Dissonant pulsing rune harmonics and heavy reversed undertone.
			value=(0.57*sin(phase+sin(t*15)*1.3)+0.26*sin(phase*2.13)+0.3*sin(phase*0.48)+previous*0.11)*envelope*(0.55+0.45*sin(t*21)*sin(t*21))
		elif kind.begins_with("sp_survival_"):
			# Dry ticking gears and accelerated clock chimes.
			var tick: float=pow(maxf(0,sin(t*TAU*19)),18)
			value=(0.52*sin(phase*2)+0.65*tick*(noise*0.7+sin(phase*4)))*envelope
		elif kind.begins_with("sp_bounty_"):
			# Metallic hunting horn over a syncopated marching percussion.
			value=(0.45*sin(phase*0.5)+0.35*sin(phase*1.5)+0.5*noise*pow(maxf(0,sin(t*TAU*7)),5))*envelope
		elif kind.begins_with("sp_unstable_"):
			# Electrical crackle interleaved with rising crystalline pitch.
			value=(sin(phase+sin(t*38)*2.0)*0.34+previous*0.63+noise*0.35)*envelope*(0.65+0.35*sin(t*75)*sin(t*75))
		elif kind.begins_with("sp_blood_altar_"):
			# Heartbeat thumps and deep ceremonial harmonic drift.
			var beat: float=pow(maxf(0,sin(t*TAU*3)),9)
			value=(sin(phase*0.55)*0.45+beat*previous*0.9+sin(phase*1.4)*0.23)*envelope
		elif kind.begins_with("sp_forgotten_forge_"):
			# Hammer-on-anvil transient with a decaying metallic ring.
			var strike: float=pow(maxf(0,sin(t*TAU*6)),14)
			value=(strike*noise*0.62+sin(phase*3.15)*0.53+sin(phase*7.8)*0.17)*envelope
		elif kind.begins_with("sp_archive_"):
			# Turning spectral pages with rising, slowly detuned glass notes.
			value=(sin(phase)*0.42+sin(phase*1.618+sin(t*7))*0.3+previous*0.23*sin(t*TAU*4))*envelope
		elif kind=="victory" or kind=="relic" or kind=="clear" or kind=="unlock":
			var note: float=[1.0,1.25,1.5,2.0][mini(3,int(t*4))]
			value=(sin(TAU*frequency*note*float(i)/22050)+0.32*sin(TAU*frequency*note*2*float(i)/22050))*envelope*0.7
		data.encode_s16(i*2,int(clampf(value,-1,1)*8500))
	sample.data=data
	return sample

func smoke_test() -> bool:
	var seen: Dictionary={}
	for key in SPECS:
		var clip:=synthesize(key)
		if clip.data.size()<100 or clip.mix_rate!=22050: return false
		var digest: String=clip.data.hex_encode().sha256_text()
		if seen.has(digest): return false
		seen[digest]=true
	return seen.size()==SPECS.size()
