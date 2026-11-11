extends Node2D
## Dedicated Godot gameplay scene; simulation is independent of rendering.
const Model = preload("res://run_model.gd")
const PixelWorld = preload("res://pixel_world.gd")
const Content = preload("res://content.gd")
const Progress = preload("res://progress.gd")
const RouteMap = preload("res://route_map.gd")
const UI = preload("res://ui_theme.gd")
const Atmosphere = preload("res://overlay_atmosphere.gd")
var modal_root: Control
var modal_tween: Tween
var modal_room := -1
var modal_floor := -1
var input_grace := 0.0
var progress = Progress.new()
var smoke_mode := false
var last_active := false
var world = PixelWorld.new()
var displayed_hp := 100.0
var hud_hp := 100.0
var hud_shards := 0
var hud_kills := 0
var hud_hit := 0.0
var hud_heal := 0.0
var hud_gain := 0.0
var hud_kill := 0.0
var hud_ready: Array[float]=[0.0,0.0,0.0,0.0]
var hud_previous: Array[float]=[1.0,1.0,1.0,1.0]
var room_banner := 0.0
var banner_room := -1
var banner_floor := -1
const GOLD := Color("#efc987")
const VIOLET := Color("#b38aff")
const WHITE := Color("#f6edf9")
const MUTED := Color("#ada1bc")
const FLOOR := Color("#211b32")
var run = Model.new()
var german := false
var paused := false
var relics_open := false
var relic_rows := 0
var relic_detail: Label
var relic_portrait: TextureRect
var wave_banner := 0.0
var last_wave := 0
var gate_prompt := 0.0
var gate_near := false
var overlay: Control
var overlay_state := ""
var clock := 0.0
var shake := 0.0
var font: Font = ThemeDB.fallback_font
const SoundBank = preload("res://sound_bank.gd")
var audio = SoundBank.new()
var last_dash := false
var last_interact := false
var hub_transition_stage := 0
var hub_transition_time := 0.0
var hub_fade := 1.0
var hub_prompt := 0.0
var hub_keeper_prompt := 0.0
var hub_forge_prompt := 0.0
var hub_panel := ""
var synergy_toast_timer := 0.0
var boss_soul_toast_timer := 0.0
var camera_offset: Vector2=Vector2.ZERO
var camera_room_key := ""
var boss_reveal_toast := 0.0

func _ready() -> void:
	german = "--language=de" in OS.get_cmdline_user_args()
	smoke_mode = "--run-smoke" in OS.get_cmdline_user_args()
	if not smoke_mode: progress.load_save()
	run.begin(int(Time.get_ticks_usec()),progress.selected,progress.ranks)
	displayed_hp = run.hp
	hud_hp=run.hp
	hud_shards=run.shards
	hud_kills=run.kills
	hub_fade=0.0 if smoke_mode else 1.0
	light_mask = 0
	# Isolate ambient world lighting from the sharp HUD and combat telegraphs.
	var world_layer := CanvasLayer.new()
	world_layer.layer = -1
	add_child(world_layer)
	world_layer.add_child(world)
	world.setup(run)
	add_child(audio)
	var layer := CanvasLayer.new()
	add_child(layer)
	overlay = Control.new()
	overlay.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	overlay.mouse_filter = Control.MOUSE_FILTER_IGNORE
	layer.add_child(overlay)
	set_process(true)
	if "--run-smoke" in OS.get_cmdline_user_args():
		call_deferred("run_scene_smoke")

func tr_text(en: String, de: String) -> String:
	return de if german else en

func _physics_process(dt: float) -> void:
	if paused or hub_panel!="" or input_grace>0 or hub_transition_stage!=0 or run.state in ["reward","victory","defeat","route","service"]:
		return
	var direction:=Vector2(float(Input.is_physical_key_pressed(KEY_D) or Input.is_physical_key_pressed(KEY_RIGHT))-float(Input.is_physical_key_pressed(KEY_A) or Input.is_physical_key_pressed(KEY_LEFT)),float(Input.is_physical_key_pressed(KEY_S) or Input.is_physical_key_pressed(KEY_DOWN))-float(Input.is_physical_key_pressed(KEY_W) or Input.is_physical_key_pressed(KEY_UP)))
	var dash:=Input.is_physical_key_pressed(KEY_SPACE)
	var interact:=Input.is_physical_key_pressed(KEY_E)
	if run.state=="hub":
		run.tick(dt,direction,Model.HUB_PORTAL,false,false,false)
		if interact and not last_interact:
			if run.hub_portal_available():
				hub_transition_stage=1
				hub_transition_time=0.0
				play_sound("portal_charge")
			elif run.hub_forge_available():
				hub_panel="forge"
				play_sound("forge")
			elif run.hub_bartender_available():
				hub_panel="barkeep"
				play_sound("shop")
	else:
		# Mouse aim must be transformed back from the fixed HUD screen into
		# the moving world coordinates used by projectiles and hit detection.
		var mouse_world: Vector2=get_global_mouse_position()-camera_offset
		run.tick(dt,direction,mouse_world,Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT),Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT),dash and not last_dash,Input.is_physical_key_pressed(KEY_Q) and not last_active)
		if interact and not last_interact: run.next_room()
	last_active=Input.is_physical_key_pressed(KEY_Q)
	last_dash=dash
	last_interact=interact
	for notice in run.notices:
		play_sound(notice)
		if notice=="hurt": shake=7.0
		if notice=="kill": shake=maxf(shake,2.0)
		if notice=="synergy_complete": synergy_toast_timer=2.7
		if notice=="boss_phase": shake=maxf(shake,5.0)
		if notice=="boss_reveal":
			boss_reveal_toast=2.0
			shake=maxf(shake,5.0)
		if notice=="boss_soul": boss_soul_toast_timer=3.0
		if notice=="dash_nova": shake=maxf(shake,3.0)
	run.notices.clear()

func _process(dt: float) -> void:
	clock += dt
	boss_reveal_toast=maxf(0.0,boss_reveal_toast-dt)
	synergy_toast_timer=maxf(0,synergy_toast_timer-dt)
	boss_soul_toast_timer=maxf(0,boss_soul_toast_timer-dt)
	if hub_transition_stage==1:
		hub_transition_time+=dt
		hub_fade=clampf(hub_transition_time/0.48,0,1)
		if hub_transition_time>=0.48:
			run.player=Model.HUB_PORTAL
			if run.enter_first_room():
				banner_room=-1
				play_sound("portal_enter")
				hub_transition_stage=2
				hub_transition_time=0.0
			else:
				hub_transition_stage=0
				hub_fade=0.0
	elif hub_transition_stage==2:
		hub_transition_time+=dt
		hub_fade=1.0-clampf(hub_transition_time/0.65,0,1)
		if hub_transition_time>=0.65:
			hub_transition_stage=0
			hub_fade=0.0
	elif run.state=="hub":
		hub_fade=maxf(0,hub_fade-dt*1.5)
	var hub_interactive: bool=run.state=="hub" and not paused and hub_panel=="" and hub_transition_stage==0
	hub_prompt=move_toward(hub_prompt,1.0 if hub_interactive and run.hub_portal_available() else 0.0,dt*5.0)
	hub_keeper_prompt=move_toward(hub_keeper_prompt,1.0 if hub_interactive and run.hub_bartender_available() else 0.0,dt*5.0)
	hub_forge_prompt=move_toward(hub_forge_prompt,1.0 if hub_interactive and run.hub_forge_available() else 0.0,dt*5.0)
	update_hud_feedback(dt)
	input_grace=maxf(0,input_grace-dt)
	shake = move_toward(shake, 0, dt*30)
	if run.state in ["victory","defeat"] and not run.settled:
		progress.settle(run)
		if not smoke_mode: progress.save()
	var target: String = ("relics" if relics_open else "pause") if paused else ("hub_"+hub_panel if run.state=="hub" and hub_panel!="" else run.state)
	if target != overlay_state or (target=="route" and (modal_room!=run.room or modal_floor!=run.floor_index)):
		if target=="route": play_sound("map")
		elif target=="service": play_sound("sp_"+run.room_kind+"_enter" if Model.Dungeon.SPECIAL_SERVICE.has(run.room_kind) else run.room_kind)
		elif target in ["pause","victory","defeat"]: play_sound(target)
		elif target=="relics": play_sound("relic_inspect")
		elif target=="fight" and modal_floor!=run.floor_index: play_sound("descend")
		overlay_state = target
		modal_room=run.room
		modal_floor=run.floor_index
		rebuild_overlay()
	update_chamber_camera(dt)
	world.update_view(dt)
	displayed_hp = move_toward(displayed_hp,run.hp,dt*52.0)
	if banner_room != run.room or banner_floor!=run.floor_index:
		banner_room = run.room
		banner_floor=run.floor_index
		room_banner = 2.0
		last_wave=0
	room_banner = maxf(0,room_banner-dt)
	if run.state=="fight" and run.wave_total>1 and last_wave!=run.wave:
		last_wave=run.wave
		wave_banner=2.2
		if run.wave==1: play_sound("wave")
	if not paused: wave_banner=maxf(0,wave_banner-dt)
	var nearby: bool=run.state=="exit" and run.player.distance_to(run.exit_point)<150 and not paused
	if nearby and not gate_near: play_sound("portal_near")
	gate_near=nearby
	gate_prompt=move_toward(gate_prompt,1.0 if nearby else 0.0,dt*5.0)
	queue_redraw()

func update_chamber_camera(dt: float) -> void:
	var signature: String=str(run.floor_index)+":"+str(run.room)+":"+str(run.seed_value)+":"+str(run.large_room)
	var target: Vector2=Vector2.ZERO
	if run.large_room:
		# Character stays around the center, with restrained aim look-ahead.
		# Unlike viewport clamping, this reveals the entrance corridor from
		# the player's perspective even right at the edge of a massive arena.
		var look_ahead: Vector2=run.aim.normalized()*37.0 if run.state=="fight" else Vector2.ZERO
		target=Vector2(640,360)-run.player-look_ahead
	if camera_room_key!=signature:
		camera_offset=target
		camera_room_key=signature
	else:
		camera_offset=camera_offset.lerp(target,clampf(dt*6.5,0,1))
	world.position=camera_offset

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		if hub_panel!="":
			close_hub_panel()
			get_viewport().set_input_as_handled()
			return
		if relics_open:
			relics_open=false
			get_viewport().set_input_as_handled()
			return
		if run.state in ["hub","fight","exit","route","service"]:
			paused = not paused
			get_viewport().set_input_as_handled()
	elif event is InputEventKey and event.pressed and not event.echo and run.state=="hub" and hub_panel=="barkeep" and event.physical_keycode==KEY_E:
		close_hub_panel()
		get_viewport().set_input_as_handled()
	elif event is InputEventKey and event.pressed and not event.echo and run.state == "reward":
		var index := -1
		match event.physical_keycode:
			KEY_1: index = 0
			KEY_2: index = 1
			KEY_3: index = 2
		if index >= 0:
			select_relic(index)
			get_viewport().set_input_as_handled()

func text_at(pos: Vector2, value: String, size: int, color: Color = WHITE) -> void:
	draw_string(font, pos, value, HORIZONTAL_ALIGNMENT_LEFT, -1, size, color)

func meter(rect: Rect2, ratio: float, color: Color) -> void:
	draw_rect(rect.grow(2), Color("#0c0a13"))
	draw_rect(rect, Color("#211a2b"))
	var width := rect.size.x*clampf(ratio,0,1)
	if width > 0:
		draw_rect(Rect2(rect.position, Vector2(width,rect.size.y)), color.darkened(0.18))
		draw_rect(Rect2(rect.position, Vector2(width,rect.size.y*0.28)), color.lightened(0.25))
	draw_rect(rect, Color("#756179"), false, 1)

func _draw() -> void:
	if not is_instance_valid(world) or world.model == null:
		return
	if run.state=="hub":
		draw_hub_ui()
		if hub_fade>0: draw_rect(Rect2(0,0,1280,720),Color(0.02,0.012,0.035,hub_fade))
		return
	var gate: Vector2=run.exit_point
	# Camera transform affects only combat visuals. HUD remains fixed 1280x720.
	draw_set_transform(camera_offset)
	if gate_prompt>0.01 and run.state=="exit":
		var p:=gate+Vector2(-72,-104+sin(clock*2.6)*3)
		var rect:=Rect2(p,Vector2(144,36))
		draw_rect(rect.grow(3),Color(0.06,0.04,0.12,gate_prompt*0.45))
		draw_rect(rect,Color(0.12,0.08,0.21,gate_prompt*0.93))
		draw_rect(rect,Color(0.78,0.60,1,gate_prompt),false,1)
		draw_rect(Rect2(p+Vector2(7,7),Vector2(25,22)),Color(0.45,0.30,0.62,gate_prompt))
		text_at(p+Vector2(15,24),"E",16,Color(1,0.95,0.82,gate_prompt))
		text_at(p+Vector2(40,24),tr_text("Open map","Karte öffnen"),13,Color(1,0.88,0.70,gate_prompt))
	# Attack telegraphs and projectiles remain above the pixel-world layer.
	for hazard in run.hazards:
		var kind: String=hazard.get("type","")
		if kind in ["spikes","rune"]: continue # drawn on the lit world canvas, below obstacles and actors
		var color:=Color("#e9b963")
		if hazard.warn<=0:
			match kind:
				"boss_lane": color=Color("#d8a7ef")
				"boss_root": color=Color("#9bd783")
				"boss_blast": color=Color("#f58a71")
				"volatile": color=Color("#f16a83")
				"rune": color=Color("#e17fbc")
				"spikes": color=Color("#f19c7b")
				_: color=Color("#77c96d")
		if hazard.has("rect"):
			var rect: Rect2=hazard.rect
			draw_rect(rect,Color(color,0.14 if hazard.warn>0 else 0.37))
			draw_rect(rect,color,false,2)
			var vertical: bool=rect.size.y>rect.size.x
			var length: float=rect.size.y if vertical else rect.size.x
			for j in range(int(length/24)):
				var pos: Vector2=rect.position+(Vector2(rect.size.x*0.5,j*24+12) if vertical else Vector2(j*24+12,rect.size.y*0.5))
				if kind=="spikes" and hazard.warn<=0:
					draw_colored_polygon(PackedVector2Array([pos+Vector2(-7,8),pos+Vector2(0,-12),pos+Vector2(7,8)]),Color("#eed8b7"))
				else:
					draw_rect(Rect2(pos-Vector2(3,3),Vector2(6,6)),Color(color,0.7))
		else:
			draw_circle(hazard.pos,hazard.r,Color(color,0.14 if hazard.warn>0 else 0.28))
			draw_arc(hazard.pos,hazard.r,0,TAU,36,color,2,true)
			if kind=="sporepool" and hazard.warn<=0:
				for j in range(8):
					var pos: Vector2=hazard.pos+Vector2.from_angle(j*TAU/8+clock*0.4)*hazard.r*0.65
					draw_circle(pos,3+sin(clock*3+j),Color("#a9e388"))
	if run.relics.has("sun"):
		var sun_count: int=run.relics.count("sun")
		var visible_orbits: int=mini(8,sun_count)
		for orb_index in range(visible_orbits):
			var angle: float=run.elapsed*2.6+float(orb_index)*TAU/float(visible_orbits)
			var radius: float=73.0+float(orb_index%3)*17.0
			var orb: Vector2=run.player+Vector2.from_angle(angle)*radius
			var halo: Color=Color("#c284ec") if orb_index%2==0 else Color("#f2a66f")
			draw_arc(run.player,radius,angle-0.19,angle+0.19,9,Color(halo,0.28),2,true)
			draw_circle(orb,14,Color(halo,0.16))
			draw_circle(orb,10,halo)
			draw_circle(orb,6,Color("#1b102f"))
			draw_rect(Rect2(orb+Vector2(-2,-2),Vector2(4,4)),Color("#fce7c6"))
	if run.relics.has("a_orbs"):
		var wisp_count: int=mini(4,run.relics.count("a_orbs"))
		var colors: Array[Color]=[Color("#a4e8fb"),Color("#e5a9ed"),Color("#e6ca83"),Color("#91efbe")]
		for i in range(wisp_count):
			var angle: float=run.elapsed*1.4+float(i)*TAU/wisp_count
			var orb: Vector2=run.player+Vector2.from_angle(angle)*62
			var glow: float=0.7+0.3*sin(clock*5+i*2)
			draw_circle(orb,13*glow,Color(colors[i],0.13))
			draw_circle(orb,6*glow,colors[i])
			draw_rect(Rect2(orb-Vector2(2,2),Vector2(4,4)),Color("#fff6d6"))
	for enemy in run.enemies:
		var pos: Vector2 = enemy.pos
		if enemy.wind > 0:
			var rad: float = 126.0 if enemy.kind in ["boss","rootking","hollowking"] else float(enemy.r)+44
			if enemy.kind in ["caster","archer","spore","vine","hexer"]:
				draw_line(pos,enemy.target,Color(1,0.32,0.44,0.45),3,true)
			else:
				draw_circle(pos,rad,Color(0.95,0.15,0.3,0.12))
				draw_arc(pos,rad,0,TAU,48,Color("#ff6481"),2,true)
	for shot in run.shots:
		draw_projectile(shot)
	for enemy in run.enemies:
		var pos: Vector2 = enemy.pos
		var radius: float = enemy.r
		meter(Rect2(pos+Vector2(-radius,-radius-19),Vector2(radius*2,5)),float(enemy.hp)/float(enemy.max_hp),Color("#e36b86"))
	for effect in run.effects:
		draw_effect(effect)
	draw_set_transform(Vector2.ZERO)
	draw_hud()
	if hub_fade>0: draw_rect(Rect2(0,0,1280,720),Color(0.02,0.012,0.035,hub_fade))
	if hud_hit>0:
		for i in range(4):
			var tint:=Color(0.9,0.12,0.24,hud_hit*0.07*(1-float(i)/4))
			draw_rect(Rect2(0,i*5,1280,5),tint)
			draw_rect(Rect2(i*5,0,5,720),tint)
			draw_rect(Rect2(1275-i*5,0,5,720),tint)
			draw_rect(Rect2(0,715-i*5,1280,5),tint)

func hub_keycap(rect: Rect2, key_text: String, accent: Color) -> void:
	draw_rect(Rect2(rect.position+Vector2(0,3),rect.size),Color("#100c19"))
	draw_rect(rect,Color("#30253d"))
	draw_rect(rect,accent,false,1)
	draw_rect(Rect2(rect.position+Vector2(2,2),Vector2(rect.size.x-4,3)),Color(accent,0.33))
	draw_string(font,rect.position+Vector2(0,rect.size.y*0.73),key_text,HORIZONTAL_ALIGNMENT_CENTER,rect.size.x,13,WHITE)

func draw_hub_action_prompt(anchor: Vector2, caption: String, alpha: float, accent: Color, width: float) -> void:
	if alpha<=0.01: return
	var p:=anchor+Vector2(-width*0.5,-65+sin(clock*2.6)*2.0)
	var rect:=Rect2(p,Vector2(width,43))
	draw_rect(Rect2(rect.position+Vector2(3,5),rect.size),Color(0.02,0.01,0.04,0.55*alpha))
	draw_rect(rect,Color(0.11,0.075,0.18,0.95*alpha))
	draw_rect(rect,Color(accent,alpha),false,2)
	draw_rect(Rect2(p+Vector2(4,4),Vector2(width-8,2)),Color(accent,0.38*alpha))
	draw_rect(Rect2(p+Vector2(9,8),Vector2(26,27)),Color(0.32,0.22,0.44,alpha))
	draw_rect(Rect2(p+Vector2(9,8),Vector2(26,27)),Color(accent,alpha),false,1)
	text_at(p+Vector2(17,28),"E",17,Color(1,0.95,0.83,alpha))
	text_at(p+Vector2(43,27),caption,15,Color(1,0.91,0.77,alpha))

func draw_hub_ui() -> void:
	# A centered tavern crest replaces the large top-left information slab.
	hud_panel(Rect2(278,10,724,84),GOLD)
	draw_rect(Rect2(298,21,684,2),Color("#ab805a"))
	draw_rect(Rect2(298,84,684,2),Color("#ab805a"))
	for direction in [-1.0,1.0]:
		var x: float=640+direction*298
		draw_colored_polygon(PackedVector2Array([Vector2(x,44),Vector2(x+direction*13,55),Vector2(x,66),Vector2(x-direction*13,55)]),Color("#af805f"))
		draw_circle(Vector2(x,55),3,Color("#f9d89a"))
	draw_string(font,Vector2(355,48),tr_text("THE THRESHOLD TAVERN","DIE SCHWELLENTAVERNE"),HORIZONTAL_ALIGNMENT_CENTER,570,26,GOLD)
	draw_string(font,Vector2(360,74),tr_text("YOUR DESCENT BEGINS BEYOND THE GATE","DEIN ABSTIEG BEGINNT HINTER DEM TOR"),HORIZONTAL_ALIGNMENT_CENTER,560,12,WHITE)
	# Compact keycaps and distinct actions remain readable at native 1280x720.
	hud_panel(Rect2(24,643,645,65),VIOLET)
	text_at(Vector2(43,657),tr_text("EXPLORATION","STEUERUNG"),10,MUTED)
	for divider_x in [244.0,445.0]:
		draw_rect(Rect2(divider_x,662,1,36),Color("#685079"))
	hub_keycap(Rect2(43,669,62,27),"WASD",VIOLET)
	text_at(Vector2(117,689),tr_text("Move","Bewegen"),15,WHITE)
	hub_keycap(Rect2(271,669,38,27),"E",GOLD)
	text_at(Vector2(321,689),tr_text("Interact","Interagieren"),15,WHITE)
	hub_keycap(Rect2(466,669,53,27),"ESC",VIOLET)
	text_at(Vector2(532,689),tr_text("Pause","Pause"),15,WHITE)
	draw_hub_action_prompt(Model.HUB_BARTENDER_POINT+Vector2(0,-151),tr_text("Talk to keeper","Barkeeper ansprechen"),hub_keeper_prompt,GOLD,224)
	draw_hub_action_prompt(Model.HUB_FORGE_POINT+Vector2(0,47),tr_text("Inspect forge","Schmiede öffnen"),hub_forge_prompt,Color("#eeab7c"),218)
	draw_hub_action_prompt(Model.HUB_PORTAL+Vector2(0,-72),tr_text("Begin descent","Abstieg beginnen"),hub_prompt,VIOLET,222)
	# The loadout is visible even without opening another menu.
	hud_panel(Rect2(17,10,248,84),GOLD)
	text_at(Vector2(32,33),tr_text("KEEPER'S CONTRACT","AUFTRAG DES WIRTS"),11,GOLD)
	if run.contract_id!="":
		var contract: Dictionary=Model.CONTRACT_DATA[run.contract_id]
		text_at(Vector2(32,55),str(contract["de" if german else "en"]),14,WHITE)
		text_at(Vector2(32,76),tr_text("COMPLETE","ERLEDIGT") if run.contract_completed else "%d / %d" % [run.contract_progress,int(contract["goal"])],13,Color("#91d4b0") if run.contract_completed else MUTED)
	else:
		text_at(Vector2(32,56),tr_text("Optional • Speak to keeper","Optional • Wirt ansprechen"),12,WHITE)
		hud_keycap_hint(Vector2(32,74),tr_text("Choose one for this run","Einen pro Run wählen"))
	hud_panel(Rect2(1015,10,248,84),Color("#e8a879"))
	text_at(Vector2(1030,33),tr_text("RUN FORGE","RUN-SCHMIEDE"),11,Color("#e8a879"))
	if run.forge_id!="":
		text_at(Vector2(1030,55),str(Model.FORGE_DATA[run.forge_id]["de" if german else "en"]),15,WHITE)
		text_at(Vector2(1030,76),tr_text("Equipped for this run","Für diesen Run aktiv"),12,Color("#91d4b0"))
	else:
		text_at(Vector2(1030,56),tr_text("Optional • Change your build","Optional • Kampfstil ändern"),12,WHITE)
		hud_keycap_hint(Vector2(1030,74),tr_text("One powerful modification","Eine mächtige Modifikation"))

func hud_keycap_hint(pos: Vector2, message: String) -> void:
	text_at(pos,message,11,MUTED)

func choose_hub_contract(id: String) -> void:
	if run.select_hub_contract(id):
		play_sound("contract_accept")
		close_hub_panel()

func choose_hub_forge(id: String) -> void:
	if run.select_hub_forge(id):
		play_sound("forge_select")
		close_hub_panel()

func close_hub_panel() -> void:
	hub_panel=""
	input_grace=0.14

func run_scene_smoke() -> void:
	# Exercise the actual scene and its overlay construction, not just the model.
	await get_tree().process_frame
	if not world.is_ready_for_smoke() or run.state!="hub" or not run.enemies.is_empty() or run.visited_count!=0:
		push_error("Hub scene spawned enemies or consumed a dungeon node")
		get_tree().quit(10)
		return
	run.player=Vector2(482,351)
	if not run.hub_bartender_available():
		get_tree().quit(29)
		return
	hub_panel="barkeep"
	_process(0.0)
	if overlay_state!="hub_barkeep" or overlay.get_child_count()==0:
		get_tree().quit(30)
		return
	hub_panel=""
	_process(0.0)
	run.player=Vector2(790,350)
	if not run.hub_forge_available():
		get_tree().quit(31)
		return
	hub_panel="forge"
	_process(0.0)
	if overlay_state!="hub_forge" or overlay.get_child_count()==0:
		get_tree().quit(32)
		return
	hub_panel=""
	_process(0.0)
	# Choices mutate only run-scoped state and are impossible to repeat.
	var contract_choice: String=run.contract_offers[0]
	var forge_choice: String=run.forge_offers[0]
	if not run.select_hub_contract(contract_choice) or not run.select_hub_forge(forge_choice):
		get_tree().quit(33)
		return
	if run.select_hub_contract(contract_choice) or run.select_hub_forge(forge_choice):
		get_tree().quit(34)
		return
	run.player=Model.HUB_PORTAL
	if not run.enter_first_room() or run.state!="fight":
		get_tree().quit(26)
		return
	run.begin(512)
	if run.contract_id!="" or run.forge_id!="":
		get_tree().quit(35)
		return
	run.player=Model.HUB_PORTAL
	if not run.enter_first_room():
		get_tree().quit(27)
		return
	run.player = Vector2(200,385)
	run.enemies.clear()
	run.spawn_enemy("stalker",Vector2(250,385))
	run.tick(0.016,Vector2.ZERO,Vector2(250,385),true,false,false)
	if run.enemies[0].hp >= run.enemies[0].max_hp:
		get_tree().quit(6)
		return
	run.enemies[0].hp = 0
	run.tick(0.016,Vector2.ZERO,Vector2(250,385),false,false,false)
	_process(0.0)
	if run.state != "reward" or overlay.get_child_count() == 0:
		get_tree().quit(7)
		return
	select_relic(0)
	run.player = Model.EXIT
	if not run.next_room():
		get_tree().quit(8)
		return
	_process(0.0)
	if overlay_state!="route" or overlay.get_child_count()==0 or not run.travel(1):
		get_tree().quit(8)
		return
	if run.room_kind in Model.Dungeon.SPECIAL_COMBAT:
		run.complete_special_combat()
	else:
		for unit in run.enemies: unit.hp=0
		for cleanup in range(5):
			if run.state!="fight": break
			for unit in run.enemies: unit.hp=0
			run.tick(0.016,Vector2.ZERO,Vector2(700,385),false,false,false)
	if run.state=="reward": select_relic(0)
	run.player=run.exit_point
	if not run.next_room() or not run.travel(4):
		get_tree().quit(15)
		return
	_process(0.0)
	var choice: int=3 if run.room_kind in ["shop","forge","archive","forgotten_forge"] else (2 if run.room_kind=="blood_altar" else (0 if run.room_kind=="shrine" else 1))
	if overlay_state!="service" or not run.service_action(choice):
		get_tree().quit(15)
		return
	_process(0.0)
	if overlay_state!="route" or run.room!=4:
		get_tree().quit(16)
		return
	paused = true
	_process(0.0)
	if overlay_state != "pause":
		get_tree().quit(9)
		return
	var original: Array=run.relics.duplicate()
	for i in range(21): run.relics.append(Content.RELICS.keys()[i%12])
	relics_open=true
	_process(0.0)
	if overlay_state!="relics" or relic_rows<12 or not paused or relic_portrait.texture==null:
		get_tree().quit(25)
		return
	relics_open=false
	run.relics.assign(original)
	_process(0.0)
	paused = false
	run.state = "victory"
	_process(0.0)
	if not run.settled or overlay_state != "victory" or progress.attempts != 1:
		push_error("Descent victory settlement or overlay failed")
		get_tree().quit(12)
		return
	var earned: int = progress.essence
	_process(0.0)
	if progress.essence != earned:
		push_error("Descent run paid out twice")
		get_tree().quit(13)
		return
	restart()
	_process(0.0)
	if run.state != "hub" or run.settled or not run.relics.is_empty() or run.visited_count!=0 or run.dungeon.nodes[0].visited:
		push_error("Descent restart failed to reset run")
		get_tree().quit(14)
		return
	run.player=Model.HUB_PORTAL
	if not run.enter_first_room():
		get_tree().quit(28)
		return
	run.floor_index=2
	run.dungeon.generate(run.seed_value,2)
	run.room=17
	run.enter_room()
	run.shards=20
	_process(0.0)
	if overlay_state!="service" or run.room_kind!="forge" or not run.service_action(1):
		push_error("Crown scene: forge overlay failed")
		get_tree().quit(17)
		return
	_process(0.0)
	if overlay_state!="route" or run.shards!=4:
		get_tree().quit(18)
		return
	var crown_icon:=UI.atlas(UI.RELICS,47)
	if crown_icon.atlas!=UI.CROWN_RELICS or crown_icon.region!=Rect2(336,0,48,48):
		get_tree().quit(19)
		return
	# The standalone pixel atlas must contain the new Shadow Steps illustration
	# plus all 24 class relics at exactly 48 px per cell.
	if UI.CLASS_RELICS.get_width()!=1200 or UI.CLASS_RELICS.get_height()!=48:
		get_tree().quit(36)
		return
	if UI.atlas(UI.RELICS,2).atlas!=UI.CLASS_RELICS or UI.atlas(UI.RELICS,2).region!=Rect2(0,0,48,48):
		get_tree().quit(37)
		return
	for i in range(48,72):
		var class_icon:=UI.atlas(UI.RELICS,i)
		if class_icon.atlas!=UI.CLASS_RELICS or class_icon.region!=Rect2((i-47)*48,0,48,48):
			get_tree().quit(38)
			return
	# Each of the seven chambers owns a complete four-frame imported sprite,
	# a matching standalone map glyph and valid scene renderer.
	if UI.SPECIAL_ROOM_ICONS.get_width()!=168 or UI.SPECIAL_ROOM_ICONS.get_height()!=24:
		get_tree().quit(39)
		return
	for id in Model.Dungeon.SPECIAL_COMBAT+Model.Dungeon.SPECIAL_SERVICE:
		if not world.SPECIAL_ART.has(id):
			get_tree().quit(40)
			return
		var tex: Texture2D=world.SPECIAL_ART[id]
		if tex.get_width()!=512 or tex.get_height()!=128 or UI.room_icon(id).atlas!=UI.SPECIAL_ROOM_ICONS:
			push_error("Special chamber art missing: "+id)
			get_tree().quit(41)
			return
	# Test reserved decor bounds in each generated room, including all biome fixtures.
	for floor in range(3):
		run.floor_index=floor
		run.dungeon.generate(12345,floor)
		for room_id in range(run.dungeon.nodes.size()):
			run.room=room_id
			run.enter_room()
			world.update_view(0.0)
			if not world.decor_is_clear():
				push_error("Decor footprint overlap on floor %d room %d" % [floor,room_id])
				get_tree().quit(24)
				return
	print("Midnight Descent gameplay scene smoke PASS")
	get_tree().quit()

func attack_color(style: String) -> Color:
	if style.begins_with("dealer"): return Color("#f3cd83")
	if style.begins_with("arcanist_fire"): return Color("#ff8549")
	if style.begins_with("arcanist_frost"): return Color("#96e7ff")
	if style.begins_with("soulbinder"): return Color("#8ce6b9")
	return Color("#c3b2ff")

func draw_projectile(shot: Dictionary) -> void:
	var pos: Vector2=shot.pos
	var direction: Vector2=shot.vel.normalized()
	var style: String=shot.get("style","")
	var color: Color=attack_color(style)
	if shot.hostile:
		color=Color("#ff7891")
		if style=="spore": color=Color("#b1de75")
		elif style=="hexer" or style=="hollowking": color=Color("#ed88d9")
		elif style=="archer": color=Color("#f8b873")
	var side:=direction.orthogonal()
	if style.begins_with("dealer"):
		var points:=PackedVector2Array([pos-direction*9-side*6,pos+direction*9-side*6,pos+direction*9+side*6,pos-direction*9+side*6])
		draw_colored_polygon(points,Color("#f7e5ae"))
		draw_polyline(PackedVector2Array([points[0],points[1],points[2],points[3],points[0]]),Color("#b47741"),2,true)
		draw_line(pos-side*3,pos+side*3,Color("#a75b55"),2)
	elif style=="archer" or style.begins_with("arcanist_frost"):
		draw_line(pos-direction*20,pos+direction*8,color,3,true)
		draw_colored_polygon(PackedVector2Array([pos+direction*10,pos-direction*4+side*5,pos-direction*4-side*5]),color)
		draw_line(pos-direction*13,pos-direction*20+side*5,color,2,true)
	else:
		draw_line(pos-direction*25,pos,color,6,true)
		draw_circle(pos,13,Color(color,0.12))
		draw_circle(pos,6,color)
		draw_circle(pos+direction*2,3,Color("#fff1d8"))
		if style.begins_with("arcanist_fire") or style=="spore":
			for i in range(3):
				var spark:=pos-direction*(8+i*7)+side*sin(clock*20+i*2)*5
				draw_rect(Rect2(spark,Vector2(3,3)),Color(color,0.7))

func draw_effect(fx: Dictionary) -> void:
	var alpha := float(fx.time)/float(fx.max)
	var pos: Vector2 = fx.pos
	var dir: Vector2 = fx.dir
	var kind: String=fx.kind
	if kind=="damage":
		text_at(pos+Vector2(-12,-25-30*(1-alpha)),str(fx.amount),16,Color(1,0.9,0.72,alpha))
		return
	if kind.begins_with("class_") or kind=="relic_sun_hit":
		var class_color: Color=Color("#e2be83")
		if kind in ["class_cards","class_ace","class_coin","class_smoke"]: class_color=Color("#d2a4f4")
		elif kind in ["class_ice","class_crystal","class_wisp","class_surge"]: class_color=Color("#8bd9ed")
		elif kind in ["class_ash","class_blood"]: class_color=Color("#f49b87")
		elif kind in ["class_soul","class_ghost","class_grave"]: class_color=Color("#9de0c0")
		elif kind=="relic_sun_hit": class_color=Color("#cda6f4")
		var progress: float=1.0-alpha
		var max_radius: float=145.0
		if kind in ["class_lance","class_cards","class_ace","class_coin","class_wisp","class_ghost"]: max_radius=52.0
		if kind in ["class_thorns","class_surge","class_smoke","class_grave"]: max_radius=195.0
		var radius: float=10+max_radius*progress
		draw_circle(pos,radius,Color(class_color,0.12*alpha))
		draw_arc(pos,radius,0,TAU,36,Color(class_color,alpha*0.84),4,true)
		if kind in ["class_lance","class_cards","class_ace","class_ghost"]:
			var forward: Vector2=dir.normalized()
			if forward==Vector2.ZERO: forward=Vector2.RIGHT
			for j in range(3):
				var angle: float=(j-1)*0.35
				var ray: Vector2=forward.rotated(angle)
				var tail: Vector2=pos+ray*(20+progress*120)
				draw_line(tail-ray*20,tail+ray*14,Color(class_color,alpha),5,true)
		else:
			for j in range(16):
				var angle: float=float(j)*TAU/16.0+progress*0.9
				var ray: Vector2=Vector2.from_angle(angle)
				var particle: Vector2=pos+ray*(radius+4*sin(float(j)*2.4))
				var size: float=2.0+float(j%3)
				draw_rect(Rect2(particle-Vector2(size*0.5,size*0.5),Vector2(size,size)),Color(class_color,alpha))
		return
	if kind.begins_with("dash_"):
		var dash_color:=Color("#88ebbf")
		if kind.contains("warden"): dash_color=Color("#edc587")
		elif kind.contains("dealer"): dash_color=Color("#d3a2fa")
		elif kind.contains("arcanist"): dash_color=Color("#79ddf6")
		if "frost" in kind: dash_color=Color("#a5e5ff")
		if "fire" in kind or "cinder" in kind: dash_color=Color("#ffa06a")
		if "arcane" in kind: dash_color=Color("#caa4ff")
		var progress: float=1.0-alpha
		if kind.begins_with("dash_trail_"):
			for i in range(12):
				var side: Vector2=Vector2(-dir.y,dir.x)
				var offset: Vector2=-dir*(progress*26+i*2.8)+side*sin(float(i)*2.4+float(fx.max)*18.0)*float(i+2)*1.25
				var particle: Vector2=pos+offset
				var size: float=maxf(1.5,(1.0-progress)*5.0-float(i)*0.18)
				draw_rect(Rect2(particle-Vector2(size/2,size/2),Vector2(size,size)),Color(dash_color,alpha*(0.9-float(i)*0.045)))
		elif kind.begins_with("dash_burst_") or kind.begins_with("dash_land_"):
			var span: float=20.0+progress*55.0
			draw_arc(pos,span,0,TAU,36,Color(dash_color,alpha*0.75),3,true)
			for i in range(14):
				var angle: float=float(i)*TAU/14.0+dir.angle()*0.2
				var ray: Vector2=Vector2.from_angle(angle)
				var point: Vector2=pos+ray*(20+progress*(60+float(i%3)*10))
				draw_line(point-ray*12,point+ray*5,Color(dash_color,alpha),3,true)
		else:
			var radius: float=16.0+progress*(135.0 if kind.begins_with("dash_nova_") else 75.0)
			draw_circle(pos,radius,Color(dash_color,alpha*0.12))
			draw_arc(pos,radius,0,TAU,48,Color(dash_color,alpha),6,true)
			for i in range(16):
				var vector: Vector2=Vector2.from_angle(float(i)*TAU/16.0+progress*0.55)
				var spark: Vector2=pos+vector*radius
				draw_rect(Rect2(spark-Vector2(3,3),Vector2(6,6)),Color(dash_color,alpha))
		return
	if kind.begins_with("synergy_"):
		var synergy_color: Color=Color("#7dde96") if kind=="synergy_toxic" else (Color("#f5a066") if kind in ["synergy_wildfire","synergy_inferno"] else (Color("#9bdef9") if kind=="synergy_frost" else (Color("#c4a5ff") if kind=="synergy_ricochet" else Color("#a1e8ce"))))
		if kind=="synergy_unlock": synergy_color=Color("#f5db9a")
		var burst: float=15.0+115.0*(1.0-alpha)
		draw_circle(pos,burst,Color(synergy_color,0.13*alpha))
		draw_arc(pos,burst,0,TAU,40,Color(synergy_color,0.9*alpha),5,true)
		for i in range(16):
			var ray: Vector2=Vector2.from_angle(float(i)*TAU/16.0+clock*0.35)
			var point: Vector2=pos+ray*(burst+sin(float(i)*2.2)*9)
			draw_line(point-ray*10,point+ray*6,Color(synergy_color,alpha),3,true)
		return
	if kind.begins_with("boss_"):
		var phase_color: Color=Color("#d7b37b") if kind.ends_with("_boss") else (Color("#9bdf8d") if kind.ends_with("_rootking") else Color("#dba2ff"))
		var progress: float=1.0-alpha
		if kind.begins_with("boss_charge_"):
			for ring in range(3):
				var radius: float=35.0+float(ring)*18.0+sin(clock*11.0+ring)*5
				draw_arc(pos,radius,dir.angle()-1.0+clock*0.2,dir.angle()+1.0+clock*0.2,28,Color(phase_color,alpha),4,true)
		elif kind.begins_with("boss_phase_"):
			for ring in range(3):
				var radius: float=55.0+progress*115.0+float(ring)*25.0
				draw_arc(pos,radius,0,TAU,48,Color(phase_color,alpha*(0.9-ring*0.2)),5,true)
		else:
			draw_arc(pos,32+progress*90,0,TAU,48,Color(phase_color,alpha),7,true)
		for i in range(12):
			var ray: Vector2=Vector2.from_angle(float(i)*TAU/12.0+clock*0.1)
			var point: Vector2=pos+ray*(28+progress*110)
			draw_rect(Rect2(point-Vector2(4,4),Vector2(8,8)),Color(phase_color,alpha))
		return
	if kind.begins_with("forge_") or kind=="contract_complete":
		var accent: Color=Color("#84dbf4") if kind in ["forge_storm","forge_echo"] else (Color("#e8bb7e") if kind in ["forge_guard","forge_shatter"] else Color("#b89cff"))
		if kind=="contract_complete": accent=Color("#95e5ae")
		var radius: float=(38 if kind=="forge_echo" else 150)*(1.0-alpha)+24
		draw_arc(pos,radius,0,TAU,40,Color(accent,alpha*0.75),6,true)
		draw_circle(pos,radius,Color(accent,alpha*0.07))
		for i in range(10):
			var angle: float=TAU*float(i)/10.0+clock*0.85
			var spark: Vector2=pos+Vector2.from_angle(angle)*radius
			draw_rect(Rect2(spark-Vector2(3,3),Vector2(6,6)),Color(accent,alpha))
		return
	if kind.begins_with("slash_"):
		var style: String=kind.trim_prefix("slash_")
		var color: Color=Color("#f7cf86") if style.begins_with("warden") else Color("#91e6c0")
		var radius: float=94 if style in ["warden_executioner","soulbinder_reaper"] else 74
		var angle: float=dir.angle()-1.3+0.45*(1-alpha)
		draw_arc(pos,radius,angle,angle+2.4,32,Color(color,alpha*0.32),18,true)
		draw_arc(pos,radius+5,angle,angle+2.4,32,Color(color,alpha),5,true)
		draw_arc(pos,radius+8,angle+0.15,angle+2.15,32,Color(1,0.96,0.86,alpha),2,true)
		return
	if kind.begins_with("special_") or kind.begins_with("active_"):
		var style: String=kind.trim_prefix("special_").trim_prefix("active_")
		var color: Color=attack_color(style)
		if style=="heal": color=Color("#9ce5a7")
		elif style=="bomb": color=Color("#fca35f")
		elif style=="shield": color=Color("#e5cf92")
		var radius: float=30+110*(1-alpha)
		draw_arc(pos,radius,0,TAU,48,Color(color,alpha*0.8),4,true)
		draw_circle(pos,radius,Color(color,alpha*0.07))
		for i in range(10):
			var ray:=Vector2.from_angle(i*TAU/10+clock*0.5)
			var p:=pos+ray*radius
			draw_rect(Rect2(p-Vector2(3,3),Vector2(6,6)),Color(color,alpha))
		return
	if kind.begins_with("enemy_"):
		var style: String=kind.trim_prefix("enemy_")
		var color:=Color("#f18c81")
		if style in ["spore","vine","rootking"]: color=Color("#b3d67e")
		elif style in ["hexer","hollowking"]: color=Color("#e499d9")
		if style in ["archer","caster","spore","hexer","vine"]:
			draw_line(pos,pos+dir*45,Color(color,alpha),5,true)
			draw_circle(pos+dir*22,12*(1-alpha)+5,Color(color,alpha*0.6))
		else:
			draw_arc(pos,34+55*(1-alpha),dir.angle()-1.1,dir.angle()+1.1,24,Color(color,alpha),6,true)
		return
	match kind:
		"dash":
			draw_line(pos-dir*60,pos+dir*23,Color(0.65,0.52,1,alpha*0.8),9,true)
		"burst","hit","hurt","death":
			var color := Color(1,0.35,0.45,alpha) if kind=="hurt" else Color(1,0.78,0.42,alpha)
			for i in range(12 if kind=="death" else 7):
				var ray := Vector2.from_angle(float(i)*TAU/12+clock)
				var p:=pos+ray*(8+35*(1-alpha))
				draw_line(p,p+ray*8,color,2,true)
			if kind=="death": draw_arc(pos,15+45*(1-alpha),0,TAU,32,color,2,true)

func update_hud_feedback(dt: float) -> void:
	if run.hp<hud_hp: hud_hit=1.0
	elif run.hp>hud_hp: hud_heal=1.0
	if run.shards>hud_shards: hud_gain=1.0
	if run.kills>hud_kills: hud_kill=1.0
	hud_hp=run.hp
	hud_shards=run.shards
	hud_kills=run.kills
	hud_hit=move_toward(hud_hit,0,dt*2.6)
	hud_heal=move_toward(hud_heal,0,dt*2.0)
	hud_gain=move_toward(hud_gain,0,dt*1.8)
	hud_kill=move_toward(hud_kill,0,dt*2.0)
	var values: Array[float]=[1-run.attack_timer/run.attack_cd,1-run.bolt_timer/run.special_cd,1-run.dash_timer/run.dash_cooldown,1-run.active_timer/run.active_cd]
	for i in range(4):
		if values[i]>=0.99 and hud_previous[i]<0.99: hud_ready[i]=1.0
		hud_previous[i]=values[i]
		hud_ready[i]=move_toward(hud_ready[i],0,dt*2.5)

func hud_panel(rect: Rect2, accent: Color = GOLD) -> void:
	draw_rect(Rect2(rect.position+Vector2(0,5),rect.size),Color("#08060d"))
	draw_rect(rect,Color("#110e1b"))
	draw_rect(rect.grow(-2),Color("#2b2338"))
	draw_rect(Rect2(rect.position+Vector2(4,4),Vector2(rect.size.x-8,rect.size.y-8)),Color("#1b1728"))
	for i in range(6):
		draw_rect(Rect2(rect.position+Vector2(5,5+i*3),Vector2(rect.size.x-10,3)),Color(0.25,0.19,0.34,0.30*(1-float(i)/6)))
	draw_rect(rect.grow(-1),accent.darkened(0.48),false,1)
	draw_line(rect.position+Vector2(12,3),rect.position+Vector2(rect.size.x-12,3),accent.darkened(0.12),2)
	for x in [rect.position.x+5,rect.end.x-10]:
		draw_rect(Rect2(x,rect.position.y+5,5,5),accent)
		draw_rect(Rect2(x,rect.end.y-9,5,3),accent.darkened(0.35))

func ability_slot(x: float, icon_index: int, key: String, name: String, ratio: float, color: Color) -> void:
	var rect:=Rect2(x,657,156,58)
	var index: int=clampi(int(round((x-18)/166)),0,3)
	var pulse: float=hud_ready[index]
	hud_panel(rect,color.lightened(pulse*0.45))
	if pulse>0: draw_rect(rect.grow(-3),Color(color,pulse*0.16))
	draw_rect(Rect2(x+8,666,38,38),Color("#100c1b"))
	draw_rect(Rect2(x+8,666,38,38),color.darkened(0.4),false,1)
	if index==3:
		var active_index: int={"heal":9,"time":10,"bomb":11,"shield":12,"dice":13,"mirror":14}.get(run.active_item,9)
		draw_texture_rect_region(UI.ICONS,Rect2(x+11,669,32,32),Rect2(active_index*24,0,24,24))
	else:
		draw_texture_rect_region(PixelWorld.RELICS,Rect2(x+11,669,32,32),Rect2(icon_index*48,0,48,48))
	text_at(Vector2(x+52,672),key,10,color)
	var font_size: int=12
	while font.get_string_size(name,HORIZONTAL_ALIGNMENT_LEFT,-1,font_size).x>94 and font_size>8: font_size-=1
	text_at(Vector2(x+52,686),name,font_size,WHITE)
	meter(Rect2(x+52,691,94,5),ratio,color)
	var cooldowns: Array[float]=[run.attack_timer,run.bolt_timer,run.dash_timer,run.active_timer]
	if ratio>=0.99:
		draw_rect(Rect2(x+52,698,5,5),color)
		text_at(Vector2(x+62,711),tr_text("READY","BEREIT"),10,color)
	else:
		text_at(Vector2(x+52,711),"%.1fs" % maxf(0,cooldowns[index]),11,MUTED)

func draw_hud() -> void:
	draw_rect(Rect2(0,0,1280,103),Color("#100d19"))
	draw_rect(Rect2(0,651,1280,69),Color("#100d19"))
	hud_panel(Rect2(17,10,406,84),GOLD.lerp(Color("#ff526f"),hud_hit))
	if hud_hit>0: draw_rect(Rect2(20,13,400,78),Color(1,0.12,0.25,hud_hit*0.18))
	if hud_heal>0: draw_rect(Rect2(20,13,400,78),Color(0.3,1,0.55,hud_heal*0.12))
	hud_panel(Rect2(438,10,368,84),VIOLET)
	hud_panel(Rect2(821,10,442,84),GOLD.lightened(hud_gain*0.45))
	if hud_gain>0: draw_rect(Rect2(824,13,436,78),Color(1,0.72,0.32,hud_gain*0.12))
	draw_rect(Rect2(26,20,67,67),Color("#100e1b"))
	draw_rect(Rect2(26,20,67,67),Color("#8b6f92"),false,2)
	draw_texture_rect_region(PixelWorld.ACTORS[run.character],Rect2(29,23,64,64),Rect2(int(clock*2)%2*32,0,32,32))
	text_at(Vector2(99,30),Content.floor_name(run.floor_index,german).to_upper(),13,GOLD)
	text_at(Vector2(99,55),Content.character_name(run.character,german).to_upper(),23,WHITE)
	meter(Rect2(99,68,235,13),displayed_hp/run.max_hp,Color("#a94565"))
	var health_width := 235.0*clampf(run.hp/run.max_hp,0,1)
	if health_width > 0:
		draw_rect(Rect2(99,68,health_width,9),Color("#dc6e87"))
		draw_rect(Rect2(99,68,health_width,2),Color("#ffb0bc"))
	text_at(Vector2(343,80),"%d/%d" % [run.hp,run.max_hp],15,WHITE)
	if run.character=="soulbinder":
		text_at(Vector2(300,55),tr_text("SOULS","SEELEN")+"  %d/6" % run.souls,13,Color("#8de1cb"))
	text_at(Vector2(460,31),tr_text("EXPEDITION","EXPEDITION"),12,MUTED)
	text_at(Vector2(460,54),tr_text("FLOOR","EBENE")+"  %d / %d" % [run.floor_index+1,Model.FLOOR_COUNT],21,GOLD)
	if run.bosses_defeated>0:
		text_at(Vector2(687,54),tr_text("BOSS SOULS ","BOSS-SEELEN ")+str(run.bosses_defeated),12,Color("#c7a6ff"))
	var cleared: int=run.dungeon.cleared_fights()
	var required: int=run.dungeon.MIN_FIGHTS[run.floor_index]
	var elite_required: int=run.dungeon.MIN_ELITES[run.floor_index]
	text_at(Vector2(460,78),tr_text("FIGHTS","KÄMPFE")+" %d/%d" % [cleared,required],13,GOLD)
	text_at(Vector2(565,78),"ELITE %d/%d" % [run.dungeon.cleared_elites(),elite_required],13,MUTED)
	text_at(Vector2(682,78),tr_text("BOSS READY","BOSS BEREIT") if run.dungeon.boss_ready() else tr_text("BOSS LOCKED","BOSS GESPERRT"),12,Color("#8cdbb1") if run.dungeon.boss_ready() else Color("#c99c87"))
	text_at(Vector2(844,33),tr_text("SHARDS","SPLITTER"),12,MUTED)
	text_at(Vector2(844,59),str(run.shards),24,GOLD)
	text_at(Vector2(957,33),tr_text("DEFEATED","BESIEGT"),12,MUTED)
	text_at(Vector2(957,59),str(run.kills),24,WHITE.lerp(Color("#ffe0a1"),hud_kill))
	text_at(Vector2(1100,33),tr_text("RUN TIME","RUN-ZEIT"),12,MUTED)
	text_at(Vector2(1100,59),"%02d:%02d" % [int(run.elapsed)/60,int(run.elapsed)%60],24,WHITE)
	text_at(Vector2(1100,79),"ESC  "+tr_text("Pause","Pause"),12,MUTED)
	ability_slot(18,13 if run.character=="dealer" else (3 if run.character=="arcanist" or run.set_id=="soulbinder_wraith" else 0),"LMB" if not german else "LMT",(tr_text("Card","Karte") if run.character=="dealer" else (tr_text("Magic","Magie") if run.character=="arcanist" or run.set_id=="soulbinder_wraith" else (tr_text("Scythe","Sense") if run.character=="soulbinder" else tr_text("Sword","Schwert")))),1.0-run.attack_timer/run.attack_cd,GOLD)
	ability_slot(184,3,"RMB" if not german else "RMT",(tr_text("Sweep","Wirbel") if run.set_id in ["warden_executioner","soulbinder_reaper"] else (tr_text("Summon","Beschwören") if run.set_id=="soulbinder_wraith" else (tr_text("Nova","Nova") if run.set_id=="arcanist_frost" else tr_text("Bolt","Geschoss")))),1.0-run.bolt_timer/run.special_cd,Color("#72cadf"))
	ability_slot(350,2,"SPACE" if not german else "LEER", "Dash",1.0-run.dash_timer/run.dash_cooldown,VIOLET)
	ability_slot(516,1,"Q",active_name(),1.0-run.active_timer/run.active_cd,Color("#e8899b"))
	hud_panel(Rect2(683,659,580,53),VIOLET)
	text_at(Vector2(695,679),tr_text("RELICS","RELIKTE")+"  %d" % run.relics.size(),13,GOLD)
	text_at(Vector2(695,699),tr_text("ESC  •  Run relics","ESC  •  Run-Relikte"),12,MUTED)
	for i in range(6):
		var rect := Rect2(952+i*40,669,34,34)
		draw_rect(rect,Color("#100d1c"))
		draw_rect(rect,Color("#695471"),false,1)
		if i<run.relics.size():
			var index: int = Content.RELICS[run.relics[maxi(0,run.relics.size()-6)+i]][4]
			if index>=0:
				var icon:=UI.atlas(UI.RELICS,index)
				draw_texture_rect_region(icon.atlas,rect,icon.region)
	if run.forge_id!="":
		hud_panel(Rect2(18,107,350,39),Color("#e8a879"))
		var forge_name: String=str(Model.FORGE_DATA[run.forge_id]["de" if german else "en"])
		text_at(Vector2(31,124),tr_text("FORGE","SCHMIEDE")+" • "+forge_name,13,Color("#e8a879"))
		var status: String=tr_text("Run modifier active","Run-Modifikator aktiv")
		if run.forge_id=="iron_aegis":
			status=tr_text("Shields this room: ","Schilde in diesem Raum: ")+str(run.forge_guard_charges)+" / 2"
		text_at(Vector2(31,140),status,12,WHITE)
	if run.contract_id!="":
		var quest: Dictionary=Model.CONTRACT_DATA[run.contract_id]
		var status_color: Color=Color("#91d4b0") if run.contract_completed else GOLD
		hud_panel(Rect2(875,107,375,39),status_color)
		var title: String=str(quest["de" if german else "en"])
		text_at(Vector2(889,124),tr_text("CONTRACT","AUFTRAG")+" • "+title,13,status_color)
		text_at(Vector2(889,140),tr_text("DONE • REWARD GRANTED","ERLEDIGT • BONUS ERHALTEN") if run.contract_completed else "%d / %d" % [run.contract_progress,int(quest["goal"])],12,WHITE)
	if run.state=="fight" and Model.Dungeon.SPECIAL_COMBAT.has(run.room_kind):
		var room_tint: Color=UI.ROOM_COLORS[run.room_kind]
		hud_panel(Rect2(412,107,453,42),room_tint)
		var goal: String=""
		match run.room_kind:
			"ritual":
				goal=tr_text("ANCHORS ","ANKER ")+str(run.special_objectives)+"/3  •  "+tr_text("RITUAL ","RITUAL ")+("%.0fs" % run.special_timer)
				if run.special_stage==1: goal=tr_text("RITUAL BROKEN • Destroy anchors and guardian","RITUAL VOLLENDET • Zerstöre Anker und Wächter")
			"survival": goal=tr_text("SURVIVE ","ÜBERLEBE ")+("%.0fs" % run.special_timer)+tr_text(" • No need to kill all enemies"," • Nicht alle Gegner nötig")
			"bounty":
				var hp_ratio: float=0.0
				for enemy in run.enemies:
					if bool(enemy.get("bounty",false)): hp_ratio=float(enemy.hp)/maxf(1.0,float(enemy.max_hp))
				goal=tr_text("TARGET CHAMPION ","ZIEL-CHAMPION ")+("%.0f%%" % (hp_ratio*100.0))
			"unstable": goal=tr_text("REACTOR CORES • Explosions hit enemies too","REAKTORKERNE • Explosionen treffen auch Gegner")
		text_at(Vector2(425,132),goal,13,room_tint)
		var progress: float=clampf(run.special_timer/(36.0 if run.room_kind=="ritual" else 45.0),0,1) if run.room_kind in ["ritual","survival"] else (1.0-run.special_wave_timer/4.0 if run.room_kind=="unstable" else 0.0)
		if progress>0: meter(Rect2(426,138,424,5),progress,room_tint)
	if run.state=="fight" and run.room_kind=="boss":
		for enemy in run.enemies:
			if enemy.kind in ["boss","rootking","hollowking"]:
				hud_panel(Rect2(415,107,450,37),Color("#ed7c9e"))
				text_at(Vector2(434,123),Content.boss_name(run.floor_index,german).to_upper(),15,Color("#ff91bd"))
				meter(Rect2(434,130,412,7),float(enemy.hp)/float(enemy.max_hp),Color("#e36b86"))
				text_at(Vector2(745,123),"PHASE "+str(enemy.get("phase",1))+" / 3",12,Color("#ffe1b2"))
				break
	if boss_soul_toast_timer>0.0:
		var soul_alpha: float=minf(1.0,boss_soul_toast_timer*2.5)
		var soul_y: float=290.0 if synergy_toast_timer>0.0 else 203.0
		var soul_rect:=Rect2(454,soul_y,372,58)
		draw_rect(soul_rect,Color(0.10,0.07,0.20,0.93*soul_alpha))
		draw_rect(soul_rect,Color("#c9a7f5")*Color(1,1,1,soul_alpha),false,2)
		text_at(Vector2(481,soul_y+24),tr_text("BOSS SOUL ACQUIRED  +1","BOSS-SEELE ERHALTEN  +1"),18,Color(0.91,0.81,1.0,soul_alpha))
		text_at(Vector2(481,soul_y+45),tr_text("Spend it in the menu skill tree","Im Skill Tree des Hauptmenüs ausgeben"),12,Color(0.83,0.75,0.93,soul_alpha))
	if synergy_toast_timer>0.0 and run.last_synergy_id!="" and Content.SYNERGIES.has(run.last_synergy_id):
		var remaining: float=clampf(synergy_toast_timer/2.7,0,1)
		var fade: float=minf(1.0,synergy_toast_timer*3.0)
		var card_y: float=208.0-14.0*(1.0-remaining)
		var toast_rect:=Rect2(424,card_y,434,63)
		draw_rect(Rect2(toast_rect.position+Vector2(3,5),toast_rect.size),Color(0.03,0.02,0.05,0.55*fade))
		draw_rect(toast_rect,Color(0.10,0.19,0.16,0.94*fade))
		draw_rect(toast_rect,Color(0.56,0.91,0.69,fade),false,2)
		var record: Dictionary=Content.SYNERGIES[run.last_synergy_id]
		text_at(Vector2(443,card_y+22),tr_text("SYNERGY UNLOCKED","SYNERGIE AKTIVIERT"),15,Color(0.73,1.0,0.80,fade))
		text_at(Vector2(443,card_y+48),str(record["de" if german else "en"]),21,Color(1.0,0.94,0.78,fade))
	if run.state=="fight" and run.wave_total>1:
		hud_panel(Rect2(1000,150,218,37),GOLD)
		text_at(Vector2(1016,175),tr_text("WAVE","WELLE")+" %d / %d" % [run.wave,run.wave_total],19,GOLD)
		if wave_banner>0:
			var alpha: float=minf(1.0,minf((2.2-wave_banner)*5,wave_banner*2))
			var rect:=Rect2(410,258,460,104)
			draw_rect(rect.grow(5),Color(0.02,0.01,0.05,alpha*0.55))
			draw_rect(rect,Color(0.12,0.07,0.19,alpha*0.91))
			draw_rect(rect,Color(0.94,0.73,0.46,alpha),false,2)
			var title: String=tr_text("FINAL WAVE","LETZTE WELLE") if run.wave==run.wave_total else tr_text("WAVE","WELLE")+"  %d / %d" % [run.wave,run.wave_total]
			text_at(Vector2(640-font.get_string_size(title,HORIZONTAL_ALIGNMENT_LEFT,-1,30).x/2,310),title,30,Color(1,0.83,0.55,alpha))
			var sub: String=tr_text("Defeat the reinforcements","Besiege die Verstärkung")
			text_at(Vector2(640-font.get_string_size(sub,HORIZONTAL_ALIGNMENT_LEFT,-1,15).x/2,337),sub,15,Color(0.86,0.78,0.94,alpha))
	if room_banner>0 and run.state=="fight":
		var opacity := minf(1,room_banner*2.0)
		var danger: String=["I","II","III"][run.encounter_threat]
		var message: String=(room_name(run.room_kind) if Model.Dungeon.SPECIAL_COMBAT.has(run.room_kind) else encounter_name(run.encounter_id)).to_upper()+"  •  "+tr_text("DANGER","GEFAHR")+" "+danger
		draw_rect(Rect2(466,153,348,45),Color(0.06,0.04,0.1,0.86*opacity))
		draw_rect(Rect2(466,153,348,45),Color(0.77,0.49,0.27,opacity),false,1)
		text_at(Vector2(485,182),message,17,Color(1,0.83,0.55,opacity))

func play_sound(kind: String) -> void:
	if not smoke_mode: audio.play(kind)

func add_label(parent: Control, value: String, size: int, color: Color) -> void:
	var node := Label.new()
	node.text = value
	node.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	node.add_theme_font_size_override("font_size",size)
	node.add_theme_color_override("font_color",color)
	node.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	parent.add_child(node)

func add_button(parent: Control, caption: String, callback: Callable) -> Button:
	var node := Button.new()
	node.text = caption
	node.custom_minimum_size = Vector2(0,52)
	node.add_theme_font_size_override("font_size",20)
	UI.style_button(node,GOLD)
	node.mouse_entered.connect(func(): play_sound("hover"))
	node.pressed.connect(func(): play_sound("click"); callback.call())
	parent.add_child(node)
	return node

func relic_description(id: String) -> String:
	return Content.relic_text(id,german)

func select_relic(index: int) -> void:
	if run.choose_relic(index):
		play_sound("relic")
		if run.room_kind in ["blood_altar","archive"]:
			play_sound("sp_"+run.room_kind+"_success")

func active_name() -> String:
	match run.active_item:
		"time": return tr_text("Time","Zeit")
		"bomb": return tr_text("Bomb","Bombe")
		"shield": return tr_text("Ward","Schutz")
		"dice": return tr_text("Dice","Würfel")
		"mirror": return tr_text("Mirror","Spiegel")
	return tr_text("Heal","Heilung")

func encounter_name(id: String) -> String:
	match id:
		"crossfire": return tr_text("Crossfire","Kreuzfeuer")
		"siege": return tr_text("Siege","Belagerung")
		"onslaught": return tr_text("Onslaught","Ansturm")
		"skirmish": return tr_text("Skirmish","Scharmützel")
	return tr_text("Unknown threat","Unbekannte Gefahr")

func room_name(kind: String) -> String:
	match kind:
		"ritual": return tr_text("Ritual","Ritual")
		"survival": return tr_text("Survive","Überleben")
		"bounty": return tr_text("Bounty","Kopfgeld")
		"unstable": return tr_text("Reactor","Reaktor")
		"blood_altar": return tr_text("Blood Altar","Blutaltar")
		"forgotten_forge": return tr_text("Old Forge","Altschmiede")
		"archive": return tr_text("Archive","Archiv")
		"fight": return tr_text("Combat","Kampf")
		"elite": return "Elite"
		"treasure": return tr_text("Treasure","Schatz")
		"shop": return tr_text("Merchant","Händler")
		"event": return tr_text("Event","Ereignis")
		"shrine": return tr_text("Shrine","Heiligtum")
		"secret": return tr_text("Secret","Geheimnis")
		"boss": return "Boss"
		"forge": return tr_text("Forge","Schmiede")
	return kind

func use_service(choice: int) -> void:
	if run.service_action(choice):
		if Model.Dungeon.SPECIAL_SERVICE.has(run.room_kind):
			var cue: String={"blood_altar":"sacrifice","forgotten_forge":"temper","archive":"exchange"}[run.room_kind]
			var leaving: bool=choice==3 or (run.room_kind=="blood_altar" and choice==2)
			play_sound("sp_"+run.room_kind+("_success" if leaving else "_"+cue))
		else:
			play_sound("purchase" if run.room_kind in ["shop","forge"] else run.room_kind)
		# Service decisions happen while physics is paused by the modal.
		run.notices.clear()

func show_route_map(column: VBoxContainer) -> void:
	add_label(column,tr_text("DUNGEON MAP","DUNGEON-KARTE")+"  •  %d / %d" % [run.floor_index+1,Model.FLOOR_COUNT],30,GOLD)
	add_label(column,tr_text("Choose one forward route per column. No backtracking. I–III = danger and rewards.","Wähle einen Vorwärtsweg je Spalte. Kein Zurück. I–III = Gefahr und Beute."),13,MUTED)
	add_label(column,tr_text("Boss gate: ","Boss-Tor: ")+tr_text("Fights","Kämpfe")+" %d/%d  •  Elite %d/%d" % [run.dungeon.cleared_fights(),run.dungeon.MIN_FIGHTS[run.floor_index],run.dungeon.cleared_elites(),run.dungeon.MIN_ELITES[run.floor_index]]+("  •  "+tr_text("OPEN","OFFEN") if run.dungeon.boss_ready() else "  •  "+tr_text("LOCKED","GESPERRT")),16,GOLD)
	var map := RouteMap.new()
	map.nodes=run.dungeon.nodes
	map.current=run.room
	column.add_child(map)
	for i in range(run.dungeon.nodes.size()):
		var id: int = i
		var node: Dictionary=run.dungeon.nodes[i]
		var visible_kind: String = node.kind
		if visible_kind=="secret" and not node.visited and not run.route_options().has(i): visible_kind="?"
		# Fixed 92 px cards in 110 px column slots, with a minimum 18 px gap.
		# Danger uses its own second line instead of overflowing the button text.
		var caption: String=room_name(visible_kind)
		if node.kind in ["fight","elite"]:
			caption+="\n"+["I","II","III"][int(node.get("threat",0))]
		if node.cleared: caption+=" ✓"
		var button:=add_button(map,caption,func(): run.travel(id))
		button.custom_minimum_size=Vector2(92,52)
		button.size=Vector2(92,52)
		button.position=map.position_for(i)-Vector2(46,26)
		button.add_theme_font_size_override("font_size",11)
		button.icon=UI.room_icon(visible_kind)
		button.expand_icon=true
		button.add_theme_constant_override("icon_max_width",14)
		UI.style_button(button,UI.ROOM_COLORS[visible_kind],i==run.room)
		# Shrink style padding specifically for dense route-map nodes.
		for variant in ["normal","hover","pressed","disabled"]:
			var stylebox: StyleBoxFlat=button.get_theme_stylebox(variant).duplicate()
			stylebox.content_margin_left=3
			stylebox.content_margin_right=3
			stylebox.content_margin_top=3
			stylebox.content_margin_bottom=3
			button.add_theme_stylebox_override(variant,stylebox)
		button.clip_text=true
		button.disabled=not run.route_options().has(i)
		if i==run.room:
			button.add_theme_stylebox_override("disabled",UI.panel(Color("#594333"),GOLD,8))
			button.add_theme_color_override("font_disabled_color",Color("#fff0c8"))
		elif node.cleared:
			button.add_theme_color_override("font_disabled_color",Color("#8db6a8"))
		if node.kind=="boss" and not run.dungeon.boss_ready():
			button.text=tr_text("Locked","Gesperrt")
		button.tooltip_text=tr_text("Current room","Aktueller Raum") if i==run.room else (tr_text("Travel to this room","Diesen Raum betreten") if not button.disabled else tr_text("Not connected to current room","Nicht direkt verbunden"))
		if node.kind in ["fight","elite"]:
			button.tooltip_text+="\n"+encounter_name(str(node.get("encounter","skirmish")))+" • "+tr_text("Danger","Gefahr")+" "+["I","II","III"][int(node.get("threat",0))]
	var footer:=HBoxContainer.new()
	footer.add_theme_constant_override("separation",12)
	column.add_child(footer)
	if run.room_kind=="boss" and run.floor_index<Model.FLOOR_COUNT-1 and run.dungeon.nodes[run.room].cleared:
		var descent:=add_button(footer,tr_text("Descend  ›  ","Abstieg  ›  ")+Content.floor_name(run.floor_index+1,german),func(): run.descend())
		descent.size_flags_horizontal=Control.SIZE_EXPAND_FILL
		UI.style_button(descent,Color("#8bcea1"),true)
	var back: Button
	if run.room_kind in ["fight","elite","boss"]+Model.Dungeon.SPECIAL_COMBAT and run.player.distance_to(Model.EXIT)<=75:
		back=add_button(footer,tr_text("Close map","Karte schließen"),func(): run.state="exit")
	else:
		back=add_button(footer,tr_text("Pause expedition","Expedition pausieren"),func(): paused=true)
	back.size_flags_horizontal=Control.SIZE_EXPAND_FILL
	UI.style_button(back,Color("#a5afd2"))

func disable_modal_input(node: Node) -> void:
	if node is Control:
		node.mouse_filter=Control.MOUSE_FILTER_IGNORE
		node.focus_mode=Control.FOCUS_NONE
	if node is BaseButton: node.disabled=true
	for child in node.get_children(): disable_modal_input(child)

func modal_accent() -> Color:
	match overlay_state:
		"route": return Color("#91a9e8")
		"hub_barkeep": return GOLD
		"hub_forge": return Color("#e8a879")
		"service": return UI.ROOM_COLORS[run.room_kind]
		"victory": return Color("#f1cc7e")
		"defeat": return Color("#e67c9a")
		"pause": return Color("#a8a0c8")
	return GOLD

func modal_fill() -> Color:
	match overlay_state:
		"route": return Color("#151d33")
		"hub_barkeep": return Color("#32232b")
		"hub_forge": return Color("#31242a")
		"service": return Color("#202337") if run.room_kind=="shop" else Color("#211b30")
		"victory": return Color("#312431")
		"defeat": return Color("#2e1828")
		"pause": return Color("#201c30")
	return Color("#23182e")

func service_option(parent: Control, caption: String, detail: String, icon_index: int, accent: Color, choice: int, available: bool=true) -> Button:
	var button:=add_button(parent,caption+"\n"+detail,func(): use_service(choice))
	button.custom_minimum_size=Vector2(0,64)
	button.alignment=HORIZONTAL_ALIGNMENT_LEFT
	button.add_theme_font_size_override("font_size",16)
	button.icon=UI.atlas(UI.RELICS,icon_index)
	button.expand_icon=true
	button.add_theme_constant_override("icon_max_width",48)
	UI.style_button(button,accent)
	button.disabled=not available
	return button

func special_service_option(parent: Control, caption: String, detail: String, choice: int, enabled: bool=true) -> Button:
	var id: String=run.room_kind
	var button:=add_button(parent,caption+"\n"+detail,func(): use_service(choice))
	button.custom_minimum_size=Vector2(0,68)
	button.alignment=HORIZONTAL_ALIGNMENT_LEFT
	button.add_theme_font_size_override("font_size",15)
	button.icon=UI.room_icon(id)
	button.expand_icon=true
	button.add_theme_constant_override("icon_max_width",46)
	UI.style_button(button,UI.ROOM_COLORS[id])
	button.disabled=not enabled
	return button

func service_intro() -> String:
	match run.room_kind:
		"blood_altar": return tr_text("An ancient bowl demands a permanent piece of your vitality.","Eine uralte Schale verlangt einen dauerhaften Teil deiner Lebenskraft.")
		"forgotten_forge": return tr_text("The forgotten anvil can temper a relic you already carry.","Der vergessene Amboss kann eines deiner Relikte verdoppeln und härten.")
		"archive": return tr_text("Erase one relic from your run to discover a new possibility.","Lösche ein Relikt aus deinem Run und wähle eine neue Möglichkeit.")
		"shop": return tr_text("A lantern burns above a merchant's travelling wares.","Eine Laterne leuchtet über den Waren eines reisenden Händlers.")
		"treasure": return tr_text("A sealed cache awaits. Choose its reward.","Eine versiegelte Truhe wartet. Wähle ihre Belohnung.")
		"secret": return tr_text("A hidden sanctuary holds a forgotten treasure.","Ein verborgenes Heiligtum birgt einen vergessenen Schatz.")
		"event": return tr_text("A strange presence offers a bargain.","Eine seltsame Präsenz bietet dir einen Handel an.")
		"shrine": return tr_text("Ancient magic restores body and spirit.","Alte Magie stärkt Körper und Geist.")
		"forge": return tr_text("The royal forge still burns. Temper your equipment.","Die königliche Schmiede brennt noch. Härte deine Ausrüstung.")
	return ""

func rebuild_overlay() -> void:
	if is_instance_valid(modal_tween): modal_tween.kill()
	if is_instance_valid(modal_root):
		var previous: Control=modal_root
		disable_modal_input(previous)
		if smoke_mode:
			previous.free()
		else:
			var fade:=create_tween()
			fade.tween_property(previous,"modulate:a",0.0,0.16)
			fade.tween_callback(previous.queue_free)
		modal_root=null
	if overlay_state not in ["pause","relics","reward","victory","defeat","route","service","hub_barkeep","hub_forge"]:
		input_grace=0.18 if not smoke_mode else 0.0
		return
	var accent: Color=modal_accent()
	modal_root=Control.new()
	modal_root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	modal_root.mouse_filter=Control.MOUSE_FILTER_IGNORE
	overlay.add_child(modal_root)
	var shade:=ColorRect.new()
	shade.color=Color(0.02,0.012,0.04,0.63)
	shade.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	modal_root.add_child(shade)
	var atmosphere:=Atmosphere.new()
	atmosphere.accent=accent
	atmosphere.mode=overlay_state
	modal_root.add_child(atmosphere)
	var center:=CenterContainer.new()
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	center.mouse_filter=Control.MOUSE_FILTER_IGNORE
	modal_root.add_child(center)
	var panel:=PanelContainer.new()
	panel.custom_minimum_size=Vector2(1160 if overlay_state=="route" else (1000 if overlay_state=="relics" else (940 if overlay_state=="reward" else 680)),0)
	var style:=UI.panel(modal_fill(),accent,24)
	style.set_corner_radius_all(14)
	style.border_width_top=3
	style.shadow_size=24
	style.shadow_offset=Vector2(0,12)
	panel.add_theme_stylebox_override("panel",style)
	center.add_child(panel)
	var column:=VBoxContainer.new()
	column.add_theme_constant_override("separation",12)
	panel.add_child(column)
	var crest:=Label.new()
	crest.text="M I D N I G H T   D E S C E N T"
	crest.horizontal_alignment=HORIZONTAL_ALIGNMENT_CENTER
	crest.add_theme_font_size_override("font_size",11)
	crest.add_theme_color_override("font_color",accent)
	column.add_child(crest)
	if not smoke_mode:
		modal_root.modulate.a=0.0
		modal_tween=create_tween()
		modal_tween.set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
		modal_tween.tween_property(modal_root,"modulate:a",1.0,0.24)
	match overlay_state:
		"hub_barkeep":
			add_label(column,tr_text("KEEPER'S CONTRACTS","AUFTRÄGE DES WIRTS"),30,GOLD)
			if run.contract_id=="":
				add_label(column,tr_text("One optional contract per expedition. The reward is granted immediately when completed.","Ein optionaler Auftrag pro Expedition. Die Belohnung erhältst du sofort beim Abschluss."),15,WHITE)
				for id in run.contract_offers:
					var choice: String=id
					var data: Dictionary=Model.CONTRACT_DATA[choice]
					var caption: String=str(data["de" if german else "en"])+"\n"+str(data["de_goal" if german else "en_goal"])+"\n"+str(data["de_reward" if german else "en_reward"])
					var option:=add_button(column,caption,func(): choose_hub_contract(choice))
					option.custom_minimum_size=Vector2(0,92)
					option.add_theme_font_size_override("font_size",15)
					option.alignment=HORIZONTAL_ALIGNMENT_LEFT
					UI.style_button(option,GOLD)
			else:
				var data: Dictionary=Model.CONTRACT_DATA[run.contract_id]
				add_label(column,str(data["de" if german else "en"]),23,GOLD)
				add_label(column,str(data["de_goal" if german else "en_goal"]),17,WHITE)
				add_label(column,"%d / %d" % [run.contract_progress,int(data["goal"])],24,Color("#96ddb8"))
				add_label(column,tr_text("COMPLETED • Reward granted","ABGESCHLOSSEN • Belohnung erhalten") if run.contract_completed else str(data["de_reward" if german else "en_reward"]),16,MUTED)
			var leave_keeper:=add_button(column,tr_text("Leave keeper","Wirt verlassen"),func(): close_hub_panel())
			UI.style_button(leave_keeper,GOLD,true)
		"hub_forge":
			add_label(column,tr_text("FORGE OF FATE","SCHICKSALSSCHMIEDE"),30,Color("#e8a879"))
			if run.forge_id=="":
				add_label(column,tr_text("Choose ONE dramatic modification for this run. Every power comes at a serious cost.","Wähle EINE mächtige Veränderung für diesen Run. Jede Macht hat einen hohen Preis."),15,WHITE)
				for id in run.forge_offers:
					var choice: String=id
					var data: Dictionary=Model.FORGE_DATA[choice]
					var caption: String=str(data["de" if german else "en"])+"\n"+str(data["de_effect" if german else "en_effect"])+"\n"+str(data["de_cost" if german else "en_cost"])
					var option:=add_button(column,caption,func(): choose_hub_forge(choice))
					option.custom_minimum_size=Vector2(0,96)
					option.add_theme_font_size_override("font_size",15)
					option.alignment=HORIZONTAL_ALIGNMENT_LEFT
					UI.style_button(option,Color("#e8a879"))
			else:
				var data: Dictionary=Model.FORGE_DATA[run.forge_id]
				add_label(column,tr_text("BOUND TO THIS RUN","FÜR DIESEN RUN GEBUNDEN"),16,GOLD)
				add_label(column,str(data["de" if german else "en"]),24,Color("#e8a879"))
				add_label(column,str(data["de_effect" if german else "en_effect"]),17,WHITE)
				add_label(column,str(data["de_cost" if german else "en_cost"]),16,Color("#f1a8a0"))
			var leave_forge:=add_button(column,tr_text("Leave forge","Schmiede verlassen"),func(): close_hub_panel())
			UI.style_button(leave_forge,Color("#e8a879"),true)
		"relics":
			show_run_relics(column)
		"route":
			show_route_map(column)
		"service":
			var service_icon:=TextureRect.new()
			service_icon.texture=UI.room_icon(run.room_kind)
			service_icon.custom_minimum_size=Vector2(0,72)
			service_icon.expand_mode=TextureRect.EXPAND_IGNORE_SIZE
			service_icon.stretch_mode=TextureRect.STRETCH_KEEP_ASPECT_CENTERED
			service_icon.texture_filter=CanvasItem.TEXTURE_FILTER_NEAREST
			column.add_child(service_icon)
			add_label(column,room_name(run.room_kind).to_upper(),30,UI.ROOM_COLORS[run.room_kind])
			add_label(column,tr_text("Shards","Splitter")+"  "+str(run.shards)+"   •   "+tr_text("HP","LP")+"  %d/%d" % [run.hp,run.max_hp],18,MUTED)
			add_label(column,service_intro(),16,WHITE)
			add_label(column,tr_text("One decision per room; relic selections continue through their own cards.","Eine Entscheidung pro Raum; Relikte werden anschließend separat gewählt."),13,UI.ROOM_COLORS[run.room_kind])
			match run.room_kind:
				"treasure","secret":
					service_option(column,tr_text("Choose a relic","Relikt wählen"),tr_text("Three relics to choose from","Drei Relikte zur Auswahl"),6,GOLD,0)
					service_option(column,tr_text("Find a new active item","Neues aktives Item finden"),tr_text("Replace your current Q item","Ersetzt dein aktuelles Q-Item"),15,Color("#9dacf0"),1)
				"shop":
					service_option(column,tr_text("Relic offer","Reliktangebot"),tr_text("12 shards  •  choose one of three","12 Splitter  •  wähle eines von drei"),6,GOLD,0,run.shards>=12)
					service_option(column,tr_text("Recover 25 HP","25 LP heilen"),tr_text("8 shards","8 Splitter"),1,Color("#e58fa2"),1,run.shards>=8)
					service_option(column,tr_text("New active item","Neues aktives Item"),tr_text("10 shards  •  replaces current item","10 Splitter  •  ersetzt aktuelles Item"),15,Color("#8acdb5"),2,run.shards>=10)
					service_option(column,tr_text("Leave merchant","Händler verlassen"),tr_text("Return to the map without buying","Ohne Kauf zur Karte zurück"),32,MUTED,3)
				"event":
					service_option(column,tr_text("Blood bargain","Bluthandel"),tr_text("Sacrifice 15 HP for a relic","Opfere 15 LP für ein Relikt"),24,Color("#e6809a"),0)
					service_option(column,tr_text("Take 8 shards","8 Splitter nehmen"),tr_text("Leave the strange presence untouched","Lass die seltsame Präsenz unberührt"),13,GOLD,1)
				"forge":
					service_option(column,tr_text("Temper weapon","Waffe härten"),tr_text("16 shards • +12 main damage","16 Splitter • +12 Hauptschaden"),0,Color("#e9ae71"),0,run.shards>=16)
					service_option(column,tr_text("Imbue focus","Fokus verzaubern"),tr_text("16 shards • +14 special damage, -10% cooldown","16 Splitter • +14 Spezialschaden, -10 % Abklingzeit"),3,VIOLET,1,run.shards>=16)
					service_option(column,tr_text("Reinforce armor","Rüstung verstärken"),tr_text("12 shards • +4 armor","12 Splitter • +4 Rüstung"),4,Color("#9acbd9"),2,run.shards>=12)
					service_option(column,tr_text("Leave forge","Schmiede verlassen"),tr_text("Return to the map","Zur Karte zurück"),32,MUTED,3)
				"shrine":
					service_option(column,tr_text("Recover 30 HP","30 LP regenerieren"),tr_text("Restore health","Regeneriert Lebenspunkte"),31,Color("#91d4b0"),0)
					service_option(column,tr_text("Gain 10 max HP","10 maximale LP erhalten"),tr_text("Permanent for this expedition","Für diese Expedition dauerhaft"),1,Color("#e990a8"),1)
				"blood_altar":
					special_service_option(column,tr_text("Greater Blood Sacrifice","Großes Blutopfer"),tr_text("-20% maximum HP, guaranteed epic relic selection","-20 % maximale LP, mindestens ein episches Relikt"),0,run.max_hp>25)
					special_service_option(column,tr_text("Lesser Blood Oath","Kleiner Bluteid"),tr_text("-10% maximum HP, +12 main and +9 special damage","-10 % maximale LP, +12 Haupt- und +9 Spezialschaden"),1,run.max_hp>25)
					special_service_option(column,tr_text("Leave the altar","Den Altar verlassen"),tr_text("Take 4 shards and reject the bargain","Nimm 4 Splitter und lehne den Handel ab"),2)
				"forgotten_forge":
					var targets: Array[String]=run.forge_relic_targets()
					if targets.is_empty():
						add_label(column,tr_text("You have no relic to temper yet.","Du besitzt noch kein Relikt zum Härten."),14,MUTED)
					for i in range(targets.size()):
						var id: String=targets[i]
						var item: Array=Content.RELICS[id]
						special_service_option(column,tr_text("Temper: ","Härten: ")+str(item[1 if german else 0]),tr_text("12 shards • gain an extra stack of this relic","12 Splitter • erhalte eine weitere Stufe dieses Relikts"),i,run.shards>=12)
					special_service_option(column,tr_text("Leave the anvil","Den Amboss verlassen"),tr_text("Keep your current relics and shards","Relikte und Splitter behalten"),3)
				"archive":
					var targets: Array[String]=run.archive_relic_targets()
					if targets.is_empty():
						add_label(column,tr_text("The archive finds no relic to rewrite.","Das Archiv findet noch kein Relikt zum Umschreiben."),14,MUTED)
					for i in range(targets.size()):
						var id: String=targets[i]
						var item: Array=Content.RELICS[id]
						special_service_option(column,tr_text("Rewrite: ","Umschreiben: ")+str(item[1 if german else 0]),tr_text("Discard this relic • choose one of three new offers","Dieses Relikt ablegen • eines von drei neuen wählen"),i)
					special_service_option(column,tr_text("Leave the archive","Archiv verlassen"),tr_text("Preserve your current build","Behalte deinen bisherigen Build"),3)
		"reward":
			add_label(column,tr_text("CHOOSE YOUR RELIC","WÄHLE DEIN RELIKT") if run.room_kind in ["treasure","shop","event","secret","archive","blood_altar"] else tr_text("ROOM CLEARED","RAUM GESÄUBERT"),30,GOLD)
			add_label(column,tr_text("Choose a relic • recover ","Wähle ein Relikt • heile ")+str(5 if run.floor_index==0 else 3)+tr_text(" HP • continue your expedition"," LP • setze deine Expedition fort"),16,MUTED)
			var cards:=HBoxContainer.new()
			cards.add_theme_constant_override("separation",14)
			column.add_child(cards)
			for i in range(run.offers.size()):
				var index:=i
				var id: String=run.offers[i]
				var item: Array=Content.RELICS[id]
				var rarity: int=Content.rarity(id)
				var color: Color=UI.RARITY_COLORS[rarity]
				var preview: Dictionary=run.synergy_preview(id)
				var ready: bool=not preview.is_empty() and preview["state"]=="ready"
				var border_color: Color=Color("#89ebae") if ready else color
				var card:=PanelContainer.new()
				card.custom_minimum_size=Vector2(278,329)
				card.size_flags_horizontal=Control.SIZE_EXPAND_FILL
				var card_style:=UI.panel(Color("#211a30").lerp(border_color,0.16 if ready else 0.09+rarity*0.035),border_color,14)
				card_style.shadow_size=12
				card_style.shadow_offset=Vector2(0,7)
				card_style.border_width_top=6 if ready else 4
				card.add_theme_stylebox_override("panel",card_style)
				cards.add_child(card)
				var stack:=VBoxContainer.new()
				stack.add_theme_constant_override("separation",10)
				card.add_child(stack)
				var rarity_label: String =["Common","Rare","Epic","Legendary"][rarity] if not german else ["Gewöhnlich","Selten","Episch","Legendär"][rarity]
				var ribbon:=PanelContainer.new()
				ribbon.add_theme_stylebox_override("panel",UI.panel(color.darkened(0.78),color.darkened(0.25),5))
				stack.add_child(ribbon)
				add_label(ribbon,rarity_label.to_upper(),13,color)
				var icon:=TextureRect.new()
				icon.texture=UI.atlas(UI.RELICS,int(item[4]))
				icon.custom_minimum_size=Vector2(0,112)
				icon.expand_mode=TextureRect.EXPAND_IGNORE_SIZE
				icon.stretch_mode=TextureRect.STRETCH_KEEP_ASPECT_CENTERED
				icon.texture_filter=CanvasItem.TEXTURE_FILTER_NEAREST
				stack.add_child(icon)
				var owner: String=Content.relic_owner(id)
				if owner!="":
					var class_tint:=Color("#e6c18a")
					match owner:
						"dealer": class_tint=Color("#e2a3c9")
						"arcanist": class_tint=Color("#8fdde8")
						"soulbinder": class_tint=Color("#a9d6c1")
					add_label(stack,tr_text("CLASS RELIC • ","KLASSENRELIKT • ")+Content.character_name(owner,german),12,class_tint)
				add_label(stack,item[1 if german else 0],21,WHITE)
				add_label(stack,item[3 if german else 2],16,MUTED)
				if not preview.is_empty():
					var synergy_info: Dictionary=Content.SYNERGIES[str(preview["id"])]
					var synergy_state: String=str(preview["state"])
					var synergy_color: Color=Color("#89ebae") if ready else (Color("#bca6ec") if synergy_state=="building" else Color("#99d3f1"))
					var label_text: String=tr_text("SYNERGY READY • ","SYNERGIE BEREIT • ") if ready else (tr_text("COMBO PIECE • ","KOMBO-TEIL • ") if synergy_state=="building" else tr_text("ACTIVE • ","AKTIV • "))
					add_label(stack,label_text+str(synergy_info["de" if german else "en"]),13,synergy_color)
					if ready: add_label(stack,str(synergy_info["de_effect" if german else "en_effect"]),12,Color("#d8f6e3"))
				var spacer:=Control.new()
				spacer.size_flags_vertical=Control.SIZE_EXPAND_FILL
				stack.add_child(spacer)
				var button:=add_button(stack,"%d  •  " % (i+1)+tr_text("Choose","Wählen"),func(): select_relic(index))
				UI.style_button(button,border_color)
				button.mouse_entered.connect(func(): animate_relic_card(card,card_style,border_color,true))
				button.mouse_exited.connect(func(): animate_relic_card(card,card_style,border_color,false))
				button.focus_entered.connect(func(): animate_relic_card(card,card_style,border_color,true))
				button.focus_exited.connect(func(): animate_relic_card(card,card_style,border_color,false))
		"pause":
			add_label(column,tr_text("EXPEDITION PAUSED","EXPEDITION PAUSIERT"),30,GOLD)
			add_label(column,tr_text("Your expedition is frozen. Take your time.","Deine Expedition ist angehalten."),16,MUTED)
			var resume:=add_button(column,tr_text("Continue  ›","Fortsetzen  ›"),func(): paused=false)
			UI.style_button(resume,GOLD,true)
			add_button(column,tr_text("Run relics","Run-Relikte")+"  •  "+str(run.relics.size()),func(): relics_open=true)
			var abandon:=add_button(column,tr_text("Abandon run / Main menu","Run aufgeben / Hauptmenü"),return_to_menu)
			UI.style_button(abandon,Color("#da8297"))
		"victory","defeat":
			var won := overlay_state=="victory"
			add_label(column,tr_text("THE HOLLOW CROWN SHATTERS","DIE HOHLE KRONE ZERBRICHT") if won else tr_text("THE DEPTHS CLAIM YOU","DIE TIEFEN FORDERN DICH"),30,GOLD if won else Color("#f095a9"))
			add_label(column,tr_text("Run summary","Run-Zusammenfassung")+"\n"+tr_text("Rooms visited","Räume besucht")+"  %d  •  " % run.visited_count+tr_text("Defeated","Besiegt")+"  %d  •  " % run.kills+tr_text("Relics","Relikte")+"  %d" % run.relics.size(),19,WHITE)
			add_label(column,tr_text("Boss souls earned","Boss-Seelen erhalten")+"  +%d" % progress.last_souls,26,Color("#bda4ff"))
			add_label(column,tr_text("Boss souls available","Verfügbare Boss-Seelen")+"  %d" % progress.boss_souls,17,Color("#d8c9ff"))
			add_label(column,tr_text("Soul essence earned","Seelenessenz erhalten")+"  +%d" % progress.last_reward,20,GOLD)
			add_label(column,tr_text("Essence for loadouts","Essenz für Start-Sets")+"  %d" % progress.essence,15,MUTED)
			add_label(column,tr_text("Combat rooms","Kampfräume")+"  %d  •  " % run.combat_clears+tr_text("Bosses","Bosse")+"  %d" % run.bosses_defeated,15,MUTED)
			if not progress.save_ok: add_label(column,tr_text("Save failed — progress not saved.","Speicherfehler — Fortschritt nicht gespeichert."),16,Color("#ff839a"))
			add_button(column,tr_text("New expedition","Neue Expedition"),restart)
			add_button(column,tr_text("Main menu","Hauptmenü"),return_to_menu)

func show_run_relics(column: VBoxContainer) -> void:
	add_label(column,tr_text("RUN RELICS","RUN-RELIKTE")+"  •  "+str(run.relics.size()),28,GOLD)
	add_label(column,tr_text("Inspect your current build. Duplicate relics are grouped.","Untersuche deinen aktuellen Build. Doppelte Relikte sind gruppiert."),15,MUTED)
	var horizontal:=HBoxContainer.new()
	horizontal.add_theme_constant_override("separation",16)
	column.add_child(horizontal)
	var scroll:=ScrollContainer.new()
	scroll.custom_minimum_size=Vector2(520,354)
	scroll.horizontal_scroll_mode=ScrollContainer.SCROLL_MODE_DISABLED
	horizontal.add_child(scroll)
	var entries:=VBoxContainer.new()
	entries.size_flags_horizontal=Control.SIZE_EXPAND_FILL
	entries.add_theme_constant_override("separation",6)
	scroll.add_child(entries)
	var counts: Dictionary={}
	for id in run.relics: counts[id]=int(counts.get(id,0))+1
	relic_rows=0
	var detail_panel:=PanelContainer.new()
	detail_panel.custom_minimum_size=Vector2(390,0)
	detail_panel.size_flags_horizontal=Control.SIZE_EXPAND_FILL
	detail_panel.add_theme_stylebox_override("panel",UI.panel(Color("#211a31"),VIOLET,12))
	horizontal.add_child(detail_panel)
	var detail:=VBoxContainer.new()
	detail.add_theme_constant_override("separation",10)
	detail_panel.add_child(detail)
	relic_portrait=TextureRect.new()
	relic_portrait.custom_minimum_size=Vector2(0,90)
	relic_portrait.expand_mode=TextureRect.EXPAND_IGNORE_SIZE
	relic_portrait.stretch_mode=TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	relic_portrait.texture_filter=CanvasItem.TEXTURE_FILTER_NEAREST
	detail.add_child(relic_portrait)
	relic_detail=Label.new()
	relic_detail.autowrap_mode=TextServer.AUTOWRAP_WORD_SMART
	relic_detail.add_theme_font_size_override("font_size",17)
	relic_detail.custom_minimum_size=Vector2(340,115)
	detail.add_child(relic_detail)
	add_label(detail,tr_text("CURRENT BUILD","AKTUELLER BUILD"),14,GOLD)
	add_label(detail,tr_text("Main / special / armor","Haupt / Spezial / Rüstung")+"   %.0f / %.0f / %.0f" % [run.damage,run.bolt_damage,run.armor],14,WHITE)
	add_label(detail,tr_text("Max HP / speed","Max. LP / Tempo")+"   %.0f / %.0f" % [run.max_hp,run.speed],14,WHITE)
	add_label(detail,tr_text("SYNERGIES","SYNERGIEN"),14,VIOLET)
	var displayed:=0
	for synergy_id in Content.SYNERGIES:
		var synergy_data: Dictionary=Content.SYNERGIES[synergy_id]
		var owned:=0
		for part in synergy_data["parts"]:
			if run.relics.has(str(part)): owned+=1
		if owned==0: continue
		var activated: bool=run.has_synergy(str(synergy_id))
		var synergy_label: String=str(synergy_data["de" if german else "en"])
		add_label(detail,(tr_text("ACTIVE • ","AKTIV • ") if activated else "1/2 • ")+synergy_label,13,Color("#94e8b0") if activated else MUTED)
		if activated: add_label(detail,str(synergy_data["de_effect" if german else "en_effect"]),12,WHITE)
		displayed+=1
	if displayed==0: add_label(detail,tr_text("Collect matching relics to discover combinations.","Finde passende Relikte für mächtige Kombinationen."),13,MUTED)
	for id in counts:
		var key: String=id
		var item: Array=Content.RELICS[key]
		var color: Color=UI.RARITY_COLORS[Content.rarity(key)]
		var button:=add_button(entries,item[1 if german else 0]+"  ×"+str(counts[key]),func(): inspect_run_relic(key,int(counts[key])))
		button.custom_minimum_size=Vector2(0,54)
		button.alignment=HORIZONTAL_ALIGNMENT_LEFT
		button.icon=UI.atlas(UI.RELICS,int(item[4]))
		button.expand_icon=true
		button.add_theme_constant_override("icon_max_width",40)
		button.add_theme_font_size_override("font_size",17)
		UI.style_button(button,color)
		relic_rows+=1
	if counts.is_empty():
		add_label(entries,tr_text("No relics collected yet.","Noch keine Relikte gesammelt."),16,MUTED)
		relic_detail.text=tr_text("Your relics appear here as you collect them.","Gesammelte Relikte erscheinen hier.")
	else:
		var first: String=counts.keys()[0]
		inspect_run_relic(first,int(counts[first]),false)
	add_button(column,tr_text("Back to pause","Zurück zur Pause"),func(): relics_open=false)

func inspect_run_relic(id: String, count: int, sound: bool=true) -> void:
	var item: Array=Content.RELICS[id]
	var rarity: int=Content.rarity(id)
	var names: Array=["Common","Rare","Epic","Legendary"] if not german else ["Gewöhnlich","Selten","Episch","Legendär"]
	relic_portrait.texture=UI.atlas(UI.RELICS,int(item[4]))
	relic_detail.text=item[1 if german else 0]+"  ×"+str(count)+"\n"+names[rarity]+"\n\n"+item[3 if german else 2]
	relic_detail.add_theme_color_override("font_color",UI.RARITY_COLORS[rarity])
	if sound: play_sound("relic_inspect")

func animate_relic_card(card: Control, style: StyleBoxFlat, color: Color, hovered: bool) -> void:
	if not is_instance_valid(card): return
	var tween:=card.create_tween().set_parallel(true)
	tween.tween_property(style,"shadow_color",Color(color,0.45) if hovered else Color(0,0,0,0.6),0.16)
	tween.tween_property(style,"bg_color",Color("#211a30").lerp(color,0.20 if hovered else 0.10),0.16)
	tween.tween_property(card,"modulate",Color(1.06,1.06,1.06) if hovered else Color.WHITE,0.16)

func restart() -> void:
	paused = false
	relics_open=false
	hub_panel=""
	synergy_toast_timer=0.0
	boss_soul_toast_timer=0.0
	hub_prompt=0.0
	hub_keeper_prompt=0.0
	hub_forge_prompt=0.0
	hub_transition_stage=0
	hub_transition_time=0.0
	hub_fade=0.0 if smoke_mode else 1.0
	run.begin(int(Time.get_ticks_usec()),progress.selected,progress.ranks)
	displayed_hp = run.hp
	banner_room=-1
	camera_offset=Vector2.ZERO
	camera_room_key=""
	boss_reveal_toast=0.0

func return_to_menu() -> void:
	# Leaving after a conquered boss must not discard earned boss souls.
	# Ending an unfinished run is a defeat for progression purposes.
	if not run.settled and run.bosses_defeated>0:
		run.state="defeat"
		progress.settle(run)
		if not smoke_mode: progress.save()
	get_tree().change_scene_to_file("res://menu.tscn")
