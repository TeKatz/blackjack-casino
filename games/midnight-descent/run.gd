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
var overlay: Control
var overlay_state := ""
var clock := 0.0
var shake := 0.0
var font: Font = ThemeDB.fallback_font
var audio: AudioStreamPlayer
var sounds: Dictionary = {}
var last_dash := false
var last_interact := false

func _ready() -> void:
	german = "--language=de" in OS.get_cmdline_user_args()
	smoke_mode = "--run-smoke" in OS.get_cmdline_user_args()
	if not smoke_mode: progress.load_save()
	run.begin(int(Time.get_ticks_usec()),progress.selected,progress.ranks)
	displayed_hp = run.hp
	light_mask = 0
	# Isolate ambient world lighting from the sharp HUD and combat telegraphs.
	var world_layer := CanvasLayer.new()
	world_layer.layer = -1
	add_child(world_layer)
	world_layer.add_child(world)
	world.setup(run)
	audio = AudioStreamPlayer.new()
	add_child(audio)
	for key in ["swing","bolt","hit","hurt","kill","clear","relic"]:
		sounds[key] = make_sound(key)
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
	if paused or input_grace>0 or run.state in ["reward","victory","defeat","route","service"]:
		return
	var direction := Vector2(float(Input.is_physical_key_pressed(KEY_D) or Input.is_physical_key_pressed(KEY_RIGHT))-float(Input.is_physical_key_pressed(KEY_A) or Input.is_physical_key_pressed(KEY_LEFT)), float(Input.is_physical_key_pressed(KEY_S) or Input.is_physical_key_pressed(KEY_DOWN))-float(Input.is_physical_key_pressed(KEY_W) or Input.is_physical_key_pressed(KEY_UP)))
	var dash := Input.is_physical_key_pressed(KEY_SPACE)
	var interact := Input.is_physical_key_pressed(KEY_E)
	run.tick(dt, direction, get_global_mouse_position(), Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT), Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT), dash and not last_dash, Input.is_physical_key_pressed(KEY_Q) and not last_active)
	last_active = Input.is_physical_key_pressed(KEY_Q)
	if interact and not last_interact:
		run.next_room()
	last_dash = dash
	last_interact = interact
	for notice in run.notices:
		play_sound(notice)
		if notice == "hurt": shake = 7.0
		if notice == "kill": shake = maxf(shake, 2.0)
	run.notices.clear()

func _process(dt: float) -> void:
	clock += dt
	input_grace=maxf(0,input_grace-dt)
	shake = move_toward(shake, 0, dt*30)
	if run.state in ["victory","defeat"] and not run.settled:
		progress.settle(run)
		if not smoke_mode: progress.save()
	var target: String = "pause" if paused else run.state
	if target != overlay_state or (target=="route" and (modal_room!=run.room or modal_floor!=run.floor_index)):
		overlay_state = target
		modal_room=run.room
		modal_floor=run.floor_index
		rebuild_overlay()
	world.update_view(dt)
	displayed_hp = move_toward(displayed_hp,run.hp,dt*52.0)
	if banner_room != run.room or banner_floor!=run.floor_index:
		banner_room = run.room
		banner_floor=run.floor_index
		room_banner = 2.0
	room_banner = maxf(0,room_banner-dt)
	queue_redraw()

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		if run.state in ["fight","exit","route","service"]:
			paused = not paused
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
	var gate: Vector2 = Model.EXIT
	if run.state == "exit":
		text_at(gate+Vector2(-86,76),tr_text("E  •  Map","E  •  Karte"),16,GOLD)
	# Attack telegraphs and projectiles remain above the pixel-world layer.
	for hazard in run.hazards:
		var kind: String=hazard.get("type","")
		var color := Color("#e9b963") if hazard.warn>0 else (Color("#e17fbc") if kind=="rune" else (Color("#f19c7b") if kind=="spikes" else Color("#77c96d")))
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
		var orb: Vector2=run.player+Vector2.from_angle(clock*2.5)*70
		draw_circle(orb,12,Color("#c284ec"))
		draw_circle(orb,8,Color("#1b102f"))
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
		var pos: Vector2 = shot.pos
		var vel: Vector2 = shot.vel
		var color := Color("#ff6b89") if shot.hostile else (Color("#93e4bd") if run.character=="soulbinder" else Color("#8deaff"))
		draw_line(pos-vel.normalized()*22,pos,color,5,true)
		draw_circle(pos,10,Color(color,0.17))
		draw_circle(pos,5,color)
	for enemy in run.enemies:
		var pos: Vector2 = enemy.pos
		var radius: float = enemy.r
		meter(Rect2(pos+Vector2(-radius,-radius-19),Vector2(radius*2,5)),float(enemy.hp)/float(enemy.max_hp),Color("#e36b86"))
	for effect in run.effects:
		draw_effect(effect)
	draw_hud()
	if shake > 0:
		draw_rect(Rect2(0,0,1280,720),Color(0.9,0.15,0.25,shake*0.012))

func run_scene_smoke() -> void:
	# Exercise the actual scene and its overlay construction, not just the model.
	await get_tree().process_frame
	if not world.is_ready_for_smoke():
		get_tree().quit(10)
		return
	run.begin(512)
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
	for unit in run.enemies: unit.hp=0
	run.tick(0.016,Vector2.ZERO,Vector2(700,385),false,false,false)
	if run.state=="reward": select_relic(0)
	run.player=Model.EXIT
	if not run.next_room() or not run.travel(4):
		get_tree().quit(15)
		return
	_process(0.0)
	var choice: int=3 if run.room_kind in ["shop","forge"] else (0 if run.room_kind=="shrine" else 1)
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
	if run.state != "fight" or run.settled or not run.relics.is_empty():
		push_error("Descent restart failed to reset run")
		get_tree().quit(14)
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
	print("Midnight Descent gameplay scene smoke PASS")
	get_tree().quit()

func draw_effect(fx: Dictionary) -> void:
	var alpha := float(fx.time)/float(fx.max)
	var pos: Vector2 = fx.pos
	var dir: Vector2 = fx.dir
	match fx.kind:
		"slash":
			draw_arc(pos,68,dir.angle()-1.25,dir.angle()+1.25,28,Color(1,0.84,0.55,alpha),8,true)
			draw_arc(pos,80,dir.angle()-1.1,dir.angle()+1.1,28,Color(1,0.95,0.8,alpha*0.8),2,true)
		"dash":
			draw_line(pos-dir*60,pos+dir*23,Color(0.65,0.52,1,alpha*0.8),15,true)
		"burst","hit","hurt":
			var color := Color(1,0.35,0.45,alpha) if fx.kind=="hurt" else Color(1,0.78,0.42,alpha)
			for i in range(8):
				var ray := Vector2.from_angle(float(i)*TAU/8+clock)
				draw_line(pos+ray*(8+35*(1-alpha)),pos+ray*(15+42*(1-alpha)),color,2,true)

func hud_panel(rect: Rect2, accent: Color = GOLD) -> void:
	draw_rect(Rect2(rect.position+Vector2(0,4),rect.size),Color("#090710"))
	draw_rect(rect,Color("#1c1729"))
	draw_rect(Rect2(rect.position+Vector2(2,2),Vector2(rect.size.x-4,rect.size.y*0.44)),Color("#272036"))
	draw_rect(rect,Color("#64516b"),false,1)
	draw_line(rect.position+Vector2(8,1),rect.position+Vector2(rect.size.x-8,1),accent.darkened(0.35),2)
	for x in [rect.position.x+4,rect.end.x-9]:
		draw_rect(Rect2(x,rect.position.y+4,5,5),accent)

func ability_slot(x: float, icon_index: int, key: String, name: String, ratio: float, color: Color) -> void:
	var rect := Rect2(x,659,156,53)
	hud_panel(rect,color)
	draw_texture_rect_region(PixelWorld.RELICS,Rect2(x+8,669,32,32),Rect2(icon_index*48,0,48,48))
	text_at(Vector2(x+47,677),key+"  "+name,12,WHITE)
	meter(Rect2(x+47,686,96,8),ratio,color)
	if ratio >= 0.99:
		draw_rect(Rect2(x+47,699,5,5),color)
		text_at(Vector2(x+57,706),tr_text("READY","BEREIT"),10,color)

func draw_hud() -> void:
	draw_rect(Rect2(0,0,1280,103),Color("#100d19"))
	draw_rect(Rect2(0,651,1280,69),Color("#100d19"))
	hud_panel(Rect2(17,10,406,84))
	hud_panel(Rect2(438,10,368,84),VIOLET)
	hud_panel(Rect2(821,10,442,84))
	draw_texture_rect_region(PixelWorld.ACTORS[run.character],Rect2(29,23,64,64),Rect2(0,0,32,32))
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
	var cleared: int=run.dungeon.cleared_fights()
	var required: int=run.dungeon.MIN_FIGHTS[run.floor_index]
	var elite_required: int=run.dungeon.MIN_ELITES[run.floor_index]
	text_at(Vector2(460,78),tr_text("FIGHTS","KÄMPFE")+" %d/%d" % [cleared,required],13,GOLD)
	text_at(Vector2(565,78),"ELITE %d/%d" % [run.dungeon.cleared_elites(),elite_required],13,MUTED)
	text_at(Vector2(682,78),tr_text("BOSS READY","BOSS BEREIT") if run.dungeon.boss_ready() else tr_text("BOSS LOCKED","BOSS GESPERRT"),12,Color("#8cdbb1") if run.dungeon.boss_ready() else Color("#c99c87"))
	text_at(Vector2(844,33),tr_text("SHARDS","SPLITTER"),12,MUTED)
	text_at(Vector2(844,59),str(run.shards),24,GOLD)
	text_at(Vector2(957,33),tr_text("DEFEATED","BESIEGT"),12,MUTED)
	text_at(Vector2(957,59),str(run.kills),24,WHITE)
	text_at(Vector2(1100,33),tr_text("RUN TIME","RUN-ZEIT"),12,MUTED)
	text_at(Vector2(1100,59),"%02d:%02d" % [int(run.elapsed)/60,int(run.elapsed)%60],24,WHITE)
	text_at(Vector2(1100,79),"ESC  "+tr_text("Pause","Pause"),12,MUTED)
	ability_slot(18,0,"LMB" if not german else "LMT",(tr_text("Card","Karte") if run.character=="dealer" else (tr_text("Magic","Magie") if run.character=="arcanist" or run.set_id=="soulbinder_wraith" else (tr_text("Scythe","Sense") if run.character=="soulbinder" else tr_text("Sword","Schwert")))),1.0-run.attack_timer/run.attack_cd,GOLD)
	ability_slot(184,3,"RMB" if not german else "RMT",(tr_text("Sweep","Wirbel") if run.set_id in ["warden_executioner","soulbinder_reaper"] else (tr_text("Summon","Beschwören") if run.set_id=="soulbinder_wraith" else (tr_text("Nova","Nova") if run.set_id=="arcanist_frost" else tr_text("Bolt","Geschoss")))),1.0-run.bolt_timer/run.special_cd,Color("#72cadf"))
	ability_slot(350,2,"SPACE" if not german else "LEER", "Dash",1.0-run.dash_timer/run.dash_cooldown,VIOLET)
	ability_slot(516,1,"Q",active_name(),1.0-run.active_timer/run.active_cd,Color("#e8899b"))
	var active_index: int = {"heal":9,"time":10,"bomb":11,"shield":12,"dice":13,"mirror":14}.get(run.active_item,9)
	draw_texture_rect_region(UI.ICONS,Rect2(524,669,32,32),Rect2(active_index*24,0,24,24))
	hud_panel(Rect2(683,659,580,53),VIOLET)
	text_at(Vector2(695,679),tr_text("RELICS","RELIKTE")+"  %d" % run.relics.size(),13,GOLD)
	text_at(Vector2(695,699),tr_text("WASD / E  Move / Gate","WASD / E  Bewegung / Tor"),11,MUTED)
	for i in range(8):
		var rect := Rect2(947+i*36,669,30,30)
		draw_rect(rect,Color("#100d1c"))
		draw_rect(rect,Color("#695471"),false,1)
		if i<run.relics.size():
			var index: int = Content.RELICS[run.relics[i]][4]
			if index>=0:
				var icon:=UI.atlas(UI.RELICS,index)
				draw_texture_rect_region(icon.atlas,rect,icon.region)
	if run.state=="fight" and run.room_kind=="boss":
		for enemy in run.enemies:
			if enemy.kind in ["boss","rootking","hollowking"]:
				hud_panel(Rect2(415,107,450,37),Color("#ed7c9e"))
				text_at(Vector2(434,123),Content.boss_name(run.floor_index,german).to_upper(),15,Color("#ff91bd"))
				meter(Rect2(434,130,412,7),float(enemy.hp)/float(enemy.max_hp),Color("#e36b86"))
				break
	if run.state=="fight" and run.wave_total>1:
		text_at(Vector2(1020,167),tr_text("WAVE","WELLE")+" %d/%d" % [run.wave,run.wave_total],14,GOLD)
	if room_banner>0 and run.state=="fight":
		var opacity := minf(1,room_banner*2.0)
		draw_rect(Rect2(480,153,320,42),Color(0.06,0.04,0.1,0.77*opacity))
		text_at(Vector2(498,180),[tr_text("PRESSURE PLATES","DRUCKPLATTEN"),tr_text("TOXIC SPORES","GIFTIGE SPOREN"),tr_text("ROYAL RUNES","KÖNIGLICHE RUNEN")][run.floor_index],18,Color(1,0.82,0.52,opacity))

func make_sound(kind: String) -> AudioStreamWAV:
	var sample := AudioStreamWAV.new()
	sample.format = AudioStreamWAV.FORMAT_16_BITS
	sample.mix_rate = 22050
	var duration := 0.15 if kind!="clear" else 0.42
	var count := int(duration*22050)
	var data := PackedByteArray()
	data.resize(count*2)
	var frequency := 220.0
	match kind:
		"swing": frequency=130
		"bolt": frequency=540
		"hit": frequency=180
		"hurt": frequency=95
		"kill": frequency=320
		"clear": frequency=660
		"relic": frequency=880
	for i in range(count):
		var t := float(i)/22050
		var envelope := pow(1.0-float(i)/count,2)*minf(1,t*150)
		var wave := sin(TAU*frequency*t*(1.0-t*1.4))+0.25*sin(TAU*frequency*2.1*t)
		data.encode_s16(i*2,int(wave*envelope*4200))
	sample.data = data
	return sample

func play_sound(kind: String) -> void:
	if sounds.has(kind):
		audio.stream = sounds[kind]
		audio.play()

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
	node.pressed.connect(callback)
	parent.add_child(node)
	return node

func relic_description(id: String) -> String:
	return Content.relic_text(id,german)

func select_relic(index: int) -> void:
	if run.choose_relic(index):
		play_sound("relic")

func active_name() -> String:
	match run.active_item:
		"time": return tr_text("Time","Zeit")
		"bomb": return tr_text("Bomb","Bombe")
		"shield": return tr_text("Ward","Schutz")
		"dice": return tr_text("Dice","Würfel")
		"mirror": return tr_text("Mirror","Spiegel")
	return tr_text("Heal","Heilung")

func room_name(kind: String) -> String:
	match kind:
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
		play_sound("relic")

func show_route_map(column: VBoxContainer) -> void:
	add_label(column,tr_text("DUNGEON MAP","DUNGEON-KARTE")+"  •  %d / %d" % [run.floor_index+1,Model.FLOOR_COUNT],30,GOLD)
	add_label(column,tr_text("Gold = current  ·  Lit = reachable  ·  ✓ = cleared","Gold = aktuell  ·  Hell = erreichbar  ·  ✓ = gesäubert"),13,MUTED)
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
		var caption := room_name(visible_kind)
		if i==run.room: caption+=" •"
		elif node.cleared: caption+=" ✓"
		var button:=add_button(map,caption,func(): run.travel(id))
		button.custom_minimum_size=Vector2(104,42)
		button.size=Vector2(104,42)
		button.position=map.position_for(i)-Vector2(52,21)
		button.add_theme_font_size_override("font_size",12)
		button.icon=UI.atlas(UI.ICONS,int(UI.ROOM_ICONS[visible_kind]))
		button.expand_icon=true
		button.add_theme_constant_override("icon_max_width",18)
		UI.style_button(button,UI.ROOM_COLORS[visible_kind],i==run.room)
		button.disabled=not run.route_options().has(i)
		if i==run.room:
			button.add_theme_stylebox_override("disabled",UI.panel(Color("#594333"),GOLD,8))
			button.add_theme_color_override("font_disabled_color",Color("#fff0c8"))
		elif node.cleared:
			button.add_theme_color_override("font_disabled_color",Color("#8db6a8"))
		if node.kind=="boss" and not run.dungeon.boss_ready():
			button.text=tr_text("Locked","Gesperrt")
		button.tooltip_text=tr_text("Current room","Aktueller Raum") if i==run.room else (tr_text("Travel to this room","Diesen Raum betreten") if not button.disabled else tr_text("Not connected to current room","Nicht direkt verbunden"))
	var footer:=HBoxContainer.new()
	footer.add_theme_constant_override("separation",12)
	column.add_child(footer)
	if run.room_kind=="boss" and run.floor_index<Model.FLOOR_COUNT-1 and run.dungeon.nodes[run.room].cleared:
		var descent:=add_button(footer,tr_text("Descend  ›  ","Abstieg  ›  ")+Content.floor_name(run.floor_index+1,german),func(): run.descend())
		descent.size_flags_horizontal=Control.SIZE_EXPAND_FILL
		UI.style_button(descent,Color("#8bcea1"),true)
	var back: Button
	if run.room_kind in ["fight","elite","boss"] and run.player.distance_to(Model.EXIT)<=75:
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
		"service": return UI.ROOM_COLORS[run.room_kind]
		"victory": return Color("#f1cc7e")
		"defeat": return Color("#e67c9a")
		"pause": return Color("#a8a0c8")
	return GOLD

func modal_fill() -> Color:
	match overlay_state:
		"route": return Color("#151d33")
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

func service_intro() -> String:
	match run.room_kind:
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
	if overlay_state not in ["pause","reward","victory","defeat","route","service"]:
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
	panel.custom_minimum_size=Vector2(1160 if overlay_state=="route" else (940 if overlay_state=="reward" else 680),0)
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
		"route":
			show_route_map(column)
		"service":
			var service_icon:=TextureRect.new()
			service_icon.texture=UI.atlas(UI.ICONS,int(UI.ROOM_ICONS[run.room_kind]))
			service_icon.custom_minimum_size=Vector2(0,72)
			service_icon.expand_mode=TextureRect.EXPAND_IGNORE_SIZE
			service_icon.stretch_mode=TextureRect.STRETCH_KEEP_ASPECT_CENTERED
			service_icon.texture_filter=CanvasItem.TEXTURE_FILTER_NEAREST
			column.add_child(service_icon)
			add_label(column,room_name(run.room_kind).to_upper(),30,UI.ROOM_COLORS[run.room_kind])
			add_label(column,tr_text("Shards","Splitter")+"  "+str(run.shards)+"   •   "+tr_text("HP","LP")+"  %d/%d" % [run.hp,run.max_hp],18,MUTED)
			add_label(column,service_intro(),16,WHITE)
			add_label(column,tr_text("Your choice returns you directly to the map.","Nach deiner Wahl kehrst du direkt zur Karte zurück."),13,UI.ROOM_COLORS[run.room_kind])
			match run.room_kind:
				"treasure","secret":
					service_option(column,tr_text("Choose a relic","Relikt wählen"),tr_text("Three relics to choose from","Drei Relikte zur Auswahl"),6,GOLD,0)
					service_option(column,tr_text("Find a new active item","Neues aktives Item finden"),tr_text("Replace your current Q item","Ersetzt dein aktuelles Q-Item"),15,Color("#9dacf0"),1)
				"shop":
					service_option(column,tr_text("Relic offer","Reliktangebot"),tr_text("12 shards  •  choose one of three","12 Splitter  •  wähle eines von drei"),6,GOLD,0,run.shards>=12)
					service_option(column,tr_text("Recover 35 HP","35 LP heilen"),tr_text("8 shards","8 Splitter"),1,Color("#e58fa2"),1,run.shards>=8)
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
					service_option(column,tr_text("Recover 40 HP","40 LP regenerieren"),tr_text("Restore health","Regeneriert Lebenspunkte"),31,Color("#91d4b0"),0)
					service_option(column,tr_text("Gain 10 max HP","10 maximale LP erhalten"),tr_text("Permanent for this expedition","Für diese Expedition dauerhaft"),1,Color("#e990a8"),1)
		"reward":
			add_label(column,tr_text("CHOOSE YOUR RELIC","WÄHLE DEIN RELIKT") if run.room_kind in ["treasure","shop","event","secret"] else tr_text("ROOM CLEARED","RAUM GESÄUBERT"),30,GOLD)
			add_label(column,tr_text("Choose a relic • recover ","Wähle ein Relikt • heile ")+str(10 if run.floor_index==0 else 8)+tr_text(" HP • continue your expedition"," LP • setze deine Expedition fort"),16,MUTED)
			var cards:=HBoxContainer.new()
			cards.add_theme_constant_override("separation",14)
			column.add_child(cards)
			for i in range(run.offers.size()):
				var index:=i
				var id: String=run.offers[i]
				var item: Array=Content.RELICS[id]
				var rarity: int=Content.rarity(id)
				var color: Color=UI.RARITY_COLORS[rarity]
				var card:=PanelContainer.new()
				card.custom_minimum_size=Vector2(278,290)
				card.size_flags_horizontal=Control.SIZE_EXPAND_FILL
				var card_style:=UI.panel(Color("#211a30").lerp(color,0.09+rarity*0.035),color,14)
				card_style.shadow_size=12
				card_style.shadow_offset=Vector2(0,7)
				card_style.border_width_top=4
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
				icon.custom_minimum_size=Vector2(0,144)
				icon.expand_mode=TextureRect.EXPAND_IGNORE_SIZE
				icon.stretch_mode=TextureRect.STRETCH_KEEP_ASPECT_CENTERED
				icon.texture_filter=CanvasItem.TEXTURE_FILTER_NEAREST
				stack.add_child(icon)
				add_label(stack,item[1 if german else 0],21,WHITE)
				add_label(stack,item[3 if german else 2],16,MUTED)
				var spacer:=Control.new()
				spacer.size_flags_vertical=Control.SIZE_EXPAND_FILL
				stack.add_child(spacer)
				var button:=add_button(stack,"%d  •  " % (i+1)+tr_text("Choose","Wählen"),func(): select_relic(index))
				UI.style_button(button,color)
				button.mouse_entered.connect(func(): animate_relic_card(card,card_style,color,true))
				button.mouse_exited.connect(func(): animate_relic_card(card,card_style,color,false))
				button.focus_entered.connect(func(): animate_relic_card(card,card_style,color,true))
				button.focus_exited.connect(func(): animate_relic_card(card,card_style,color,false))
		"pause":
			add_label(column,tr_text("EXPEDITION PAUSED","EXPEDITION PAUSIERT"),30,GOLD)
			add_label(column,tr_text("Your expedition is frozen. Take your time.","Deine Expedition ist angehalten."),16,MUTED)
			var resume:=add_button(column,tr_text("Continue  ›","Fortsetzen  ›"),func(): paused=false)
			UI.style_button(resume,GOLD,true)
			var abandon:=add_button(column,tr_text("Abandon run / Main menu","Run aufgeben / Hauptmenü"),return_to_menu)
			UI.style_button(abandon,Color("#da8297"))
		"victory","defeat":
			var won := overlay_state=="victory"
			add_label(column,tr_text("THE HOLLOW CROWN SHATTERS","DIE HOHLE KRONE ZERBRICHT") if won else tr_text("THE DEPTHS CLAIM YOU","DIE TIEFEN FORDERN DICH"),30,GOLD if won else Color("#f095a9"))
			add_label(column,tr_text("Run summary","Run-Zusammenfassung")+"\n"+tr_text("Rooms visited","Räume besucht")+"  %d  •  " % run.visited_count+tr_text("Defeated","Besiegt")+"  %d  •  " % run.kills+tr_text("Relics","Relikte")+"  %d" % run.relics.size(),19,WHITE)
			add_label(column,tr_text("Soul essence earned","Seelenessenz erhalten")+"  +%d" % progress.last_reward,24,GOLD)
			add_label(column,tr_text("Total essence","Essenz gesamt")+"  %d" % progress.essence,16,MUTED)
			if not progress.save_ok: add_label(column,tr_text("Save failed — progress not saved.","Speicherfehler — Fortschritt nicht gespeichert."),16,Color("#ff839a"))
			add_button(column,tr_text("New expedition","Neue Expedition"),restart)
			add_button(column,tr_text("Main menu","Hauptmenü"),return_to_menu)

func animate_relic_card(card: Control, style: StyleBoxFlat, color: Color, hovered: bool) -> void:
	if not is_instance_valid(card): return
	var tween:=card.create_tween().set_parallel(true)
	tween.tween_property(style,"shadow_color",Color(color,0.45) if hovered else Color(0,0,0,0.6),0.16)
	tween.tween_property(style,"bg_color",Color("#211a30").lerp(color,0.20 if hovered else 0.10),0.16)
	tween.tween_property(card,"modulate",Color(1.06,1.06,1.06) if hovered else Color.WHITE,0.16)

func restart() -> void:
	paused = false
	run.begin(int(Time.get_ticks_usec()),progress.selected,progress.ranks)
	displayed_hp = run.hp
	banner_room = -1

func return_to_menu() -> void:
	get_tree().change_scene_to_file("res://menu.tscn")
