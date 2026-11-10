extends Node2D
## Dedicated Godot gameplay scene; simulation is independent of rendering.
const Model = preload("res://run_model.gd")
const PixelWorld = preload("res://pixel_world.gd")
const Content = preload("res://content.gd")
const Progress = preload("res://progress.gd")
const RouteMap = preload("res://route_map.gd")
const UI = preload("res://ui_theme.gd")
var progress = Progress.new()
var smoke_mode := false
var last_active := false
var world = PixelWorld.new()
var displayed_hp := 100.0
var room_banner := 0.0
var banner_room := -1
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
	if paused or run.state in ["reward","victory","defeat","route","service"]:
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
	shake = move_toward(shake, 0, dt*30)
	if run.state in ["victory","defeat"] and not run.settled:
		progress.settle(run)
		if not smoke_mode: progress.save()
	var target: String = "pause" if paused else run.state
	if target != overlay_state:
		overlay_state = target
		rebuild_overlay()
	world.update_view(dt)
	displayed_hp = move_toward(displayed_hp,run.hp,dt*52.0)
	if banner_room != run.room:
		banner_room = run.room
		room_banner = 2.0
	room_banner = maxf(0,room_banner-dt)
	queue_redraw()

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		if run.state in ["fight","exit"]:
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
		var color := Color("#e9b963") if hazard.warn>0 else Color("#77c96d")
		draw_circle(hazard.pos,hazard.r,Color(color,0.14))
		draw_arc(hazard.pos,hazard.r,0,TAU,36,color,2,true)
	if run.relics.has("sun"):
		var orb: Vector2=run.player+Vector2.from_angle(clock*2.5)*70
		draw_circle(orb,12,Color("#c284ec"))
		draw_circle(orb,8,Color("#1b102f"))
	for enemy in run.enemies:
		var pos: Vector2 = enemy.pos
		if enemy.wind > 0:
			var rad: float = 126.0 if enemy.kind in ["boss","rootking"] else float(enemy.r)+44
			if enemy.kind in ["caster","archer","spore","vine"]:
				draw_line(pos,enemy.target,Color(1,0.32,0.44,0.45),3,true)
			else:
				draw_circle(pos,rad,Color(0.95,0.15,0.3,0.12))
				draw_arc(pos,rad,0,TAU,48,Color("#ff6481"),2,true)
	for shot in run.shots:
		var pos: Vector2 = shot.pos
		var vel: Vector2 = shot.vel
		var color := Color("#ff6b89") if shot.hostile else Color("#8deaff")
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
	if not run.next_room() or not run.travel(1):
		get_tree().quit(8)
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
	draw_texture_rect_region(PixelWorld.RELICS,Rect2(x+8,669,32,32),Rect2(icon_index*24,0,24,24))
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
	text_at(Vector2(99,30),(tr_text("THE FORGOTTEN VAULT","DAS VERGESSENE GEWÖLBE") if run.floor_index==0 else tr_text("THE ABYSS GARDEN","DER ABGRUNDGARTEN")),13,GOLD)
	text_at(Vector2(99,55),(tr_text("WARDEN","WÄCHTER") if run.character=="warden" else ("DEALER" if run.character=="dealer" else tr_text("ARCANIST","ARKANIST"))),23,WHITE)
	meter(Rect2(99,68,235,13),displayed_hp/run.max_hp,Color("#a94565"))
	var health_width := 235.0*clampf(run.hp/run.max_hp,0,1)
	if health_width > 0:
		draw_rect(Rect2(99,68,health_width,9),Color("#dc6e87"))
		draw_rect(Rect2(99,68,health_width,2),Color("#ffb0bc"))
	text_at(Vector2(343,80),"%d/%d" % [run.hp,run.max_hp],15,WHITE)
	text_at(Vector2(460,31),tr_text("EXPEDITION","EXPEDITION"),12,MUTED)
	text_at(Vector2(460,54),tr_text("FLOOR","EBENE")+"  %d / 2" % (run.floor_index+1),21,GOLD)
	for i in range(Model.ROOM_COUNT):
		var center := Vector2(469+i*34,76)
		if i<Model.ROOM_COUNT-1: draw_line(center,center+Vector2(34,0),Color("#654e75"),2)
		var points := PackedVector2Array([center+Vector2(0,-9),center+Vector2(9,0),center+Vector2(0,9),center+Vector2(-9,0)])
		draw_colored_polygon(points,GOLD if run.dungeon.nodes[i].visited else Color("#3b2d50"))
		draw_polyline(PackedVector2Array([points[0],points[1],points[2],points[3],points[0]]),Color("#b6a0bf"),1,true)
		if i==run.room: draw_circle(center,14,Color(1,0.77,0.36,0.14+sin(clock*4)*0.06))
	text_at(Vector2(844,33),tr_text("SHARDS","SPLITTER"),12,MUTED)
	text_at(Vector2(844,59),str(run.shards),24,GOLD)
	text_at(Vector2(957,33),tr_text("DEFEATED","BESIEGT"),12,MUTED)
	text_at(Vector2(957,59),str(run.kills),24,WHITE)
	text_at(Vector2(1100,33),tr_text("RUN TIME","RUN-ZEIT"),12,MUTED)
	text_at(Vector2(1100,59),"%02d:%02d" % [int(run.elapsed)/60,int(run.elapsed)%60],24,WHITE)
	text_at(Vector2(1100,79),"ESC  "+tr_text("Pause","Pause"),12,MUTED)
	ability_slot(18,0,"LMB" if not german else "LMT",(tr_text("Card","Karte") if run.character=="dealer" else (tr_text("Magic","Magie") if run.character=="arcanist" else tr_text("Sword","Schwert"))),1.0-run.attack_timer/run.attack_cd,GOLD)
	ability_slot(184,3,"RMB" if not german else "RMT",(tr_text("Sweep","Wirbel") if run.set_id=="warden_executioner" else tr_text("Bolt","Geschoss")),1.0-run.bolt_timer/run.special_cd,Color("#72cadf"))
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
			if index>=0: draw_texture_rect_region(PixelWorld.RELICS,rect,Rect2(index*24,0,24,24))
	if run.state=="fight" and run.room_kind=="boss":
		for enemy in run.enemies:
			if enemy.kind=="boss":
				hud_panel(Rect2(415,107,450,37),Color("#ed7c9e"))
				text_at(Vector2(434,123),(tr_text("THE VAULT KEEPER","DER TRESORWÄCHTER") if run.floor_index==0 else tr_text("THE ROOT KING","DER WURZELKÖNIG")),15,Color("#ff91bd"))
				meter(Rect2(434,130,412,7),float(enemy.hp)/float(enemy.max_hp),Color("#e36b86"))
				break
	if room_banner>0 and run.state=="fight":
		var opacity := minf(1,room_banner*2.0)
		draw_rect(Rect2(480,153,320,42),Color(0.06,0.04,0.1,0.77*opacity))
		text_at(Vector2(498,180),tr_text("DESCENT","ABSTIEG")+"  %02d" % (run.room+1),18,Color(1,0.82,0.52,opacity))

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
	return kind

func use_service(choice: int) -> void:
	if run.service_action(choice):
		overlay_state=""
		rebuild_overlay()

func show_route_map(column: VBoxContainer) -> void:
	add_label(column,tr_text("DUNGEON MAP","DUNGEON-KARTE")+"  •  "+str(run.floor_index+1)+" / 2",30,GOLD)
	add_label(column,tr_text("Gold = current  ·  Lit = reachable  ·  ✓ = cleared","Gold = aktuell  ·  Hell = erreichbar  ·  ✓ = gesäubert"),14,MUTED)
	add_label(column,tr_text("Choose a connected room. Cleared rooms can be revisited.","Wähle einen verbundenen Raum. Gesäuberte Räume sind erneut betretbar."),16,MUTED)
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
		if i==run.room: caption+=" • "+tr_text("HERE","HIER")
		elif node.cleared: caption+=" ✓"
		var button:=add_button(map,caption,func(): run.travel(id))
		button.custom_minimum_size=Vector2(174,42)
		button.size=Vector2(174,42)
		button.position=RouteMap.POSITIONS[i]-Vector2(87,21)
		button.add_theme_font_size_override("font_size",15)
		button.icon=UI.atlas(UI.ICONS,int(UI.ROOM_ICONS[visible_kind]))
		button.expand_icon=true
		button.add_theme_constant_override("icon_max_width",24)
		UI.style_button(button,UI.ROOM_COLORS[visible_kind],i==run.room)
		button.disabled=not run.route_options().has(i)
		button.tooltip_text=tr_text("Current room","Aktueller Raum") if i==run.room else (tr_text("Travel to this room","Diesen Raum betreten") if not button.disabled else tr_text("Not connected to current room","Nicht direkt verbunden"))
	if run.room_kind=="boss" and run.floor_index==0 and run.dungeon.nodes[run.room].cleared:
		add_button(column,tr_text("Descend to the Abyss Garden","Abstieg in den Abgrundgarten"),func(): run.descend())
	add_button(column,tr_text("Close map","Karte schließen"),func(): run.state="exit")

func rebuild_overlay() -> void:
	for child in overlay.get_children():
		overlay.remove_child(child)
		child.queue_free()
	if overlay_state not in ["pause","reward","victory","defeat","route","service"]:
		return
	var shade := ColorRect.new()
	shade.color = Color(0.025,0.02,0.05,0.88)
	shade.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	overlay.add_child(shade)
	var center := CenterContainer.new()
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	overlay.add_child(center)
	var panel := PanelContainer.new()
	panel.custom_minimum_size = Vector2(940 if overlay_state in ["reward","route"] else 680,0)
	var style := StyleBoxFlat.new()
	style.bg_color = Color("#191325")
	style.border_color = GOLD
	style.set_border_width_all(2)
	style.set_corner_radius_all(14)
	style.shadow_color = Color(0,0,0,0.65)
	style.shadow_size = 18
	style.shadow_offset = Vector2(0,8)
	style.content_margin_left = 28
	style.content_margin_right = 28
	style.content_margin_top = 24
	style.content_margin_bottom = 24
	panel.add_theme_stylebox_override("panel",style)
	center.add_child(panel)
	var column := VBoxContainer.new()
	column.add_theme_constant_override("separation",14)
	panel.add_child(column)
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
			add_label(column,tr_text("Shards","Splitter")+"  "+str(run.shards),18,MUTED)
			match run.room_kind:
				"treasure","secret":
					add_button(column,tr_text("Choose a relic","Relikt wählen"),func(): use_service(0))
					add_button(column,tr_text("Find a new active item","Neues aktives Item finden"),func(): use_service(1))
				"shop":
					var relic_button:=add_button(column,tr_text("Relic offer — 12 shards","Reliktangebot — 12 Splitter"),func(): use_service(0))
					relic_button.disabled=run.shards<12
					var heal_button:=add_button(column,tr_text("Heal 35 — 8 shards","35 LP heilen — 8 Splitter"),func(): use_service(1))
					heal_button.disabled=run.shards<8
					var item_button:=add_button(column,tr_text("Active item — 10 shards","Aktives Item — 10 Splitter"),func(): use_service(2))
					item_button.disabled=run.shards<10
					add_button(column,tr_text("Leave","Verlassen"),func(): use_service(3))
				"event":
					add_button(column,tr_text("Sacrifice 15 HP for a relic","Opfere 15 LP für ein Relikt"),func(): use_service(0))
					add_button(column,tr_text("Take 8 shards","8 Splitter nehmen"),func(): use_service(1))
				"shrine":
					add_button(column,tr_text("Recover 40 HP","40 LP regenerieren"),func(): use_service(0))
					add_button(column,tr_text("Gain 10 max HP","10 maximale LP erhalten"),func(): use_service(1))
		"reward":
			add_label(column,tr_text("ROOM CLEARED","RAUM GESÄUBERT"),30,GOLD)
			add_label(column,tr_text("Choose a relic. Recover 12 HP and open the route map.","Wähle ein Relikt. Heile 12 LP und öffne die Karte."),17,MUTED)
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
				card.custom_minimum_size=Vector2(278,270)
				card.size_flags_horizontal=Control.SIZE_EXPAND_FILL
				card.add_theme_stylebox_override("panel",UI.panel(Color("#241c34"),color,14))
				cards.add_child(card)
				var stack:=VBoxContainer.new()
				stack.add_theme_constant_override("separation",10)
				card.add_child(stack)
				var rarity_label:=["Common","Rare","Epic","Legendary"][rarity] if not german else ["Gewöhnlich","Selten","Episch","Legendär"][rarity]
				add_label(stack,rarity_label.to_upper(),13,color)
				var icon:=TextureRect.new()
				icon.texture=UI.atlas(UI.RELICS,int(item[4]))
				icon.custom_minimum_size=Vector2(0,96)
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
		"pause":
			add_label(column,tr_text("EXPEDITION PAUSED","EXPEDITION PAUSIERT"),30,GOLD)
			add_button(column,tr_text("Continue","Fortsetzen"),func(): paused=false)
			add_button(column,tr_text("Abandon run / Main menu","Run aufgeben / Hauptmenü"),return_to_menu)
		"victory","defeat":
			var won := overlay_state=="victory"
			add_label(column,tr_text("VAULT CONQUERED","GEWÖLBE EROBERT") if won else tr_text("THE DEPTHS CLAIM YOU","DIE TIEFEN FORDERN DICH"),30,GOLD if won else Color("#f095a9"))
			add_label(column,tr_text("Run summary","Run-Zusammenfassung")+"\n"+tr_text("Rooms visited","Räume besucht")+"  %d  •  " % run.visited_count+tr_text("Defeated","Besiegt")+"  %d  •  " % run.kills+tr_text("Relics","Relikte")+"  %d" % run.relics.size(),19,WHITE)
			add_label(column,tr_text("Soul essence earned","Seelenessenz erhalten")+"  +%d" % progress.last_reward,24,GOLD)
			add_label(column,tr_text("Total essence","Essenz gesamt")+"  %d" % progress.essence,16,MUTED)
			if not progress.save_ok: add_label(column,tr_text("Save failed — progress not saved.","Speicherfehler — Fortschritt nicht gespeichert."),16,Color("#ff839a"))
			add_button(column,tr_text("New expedition","Neue Expedition"),restart)
			add_button(column,tr_text("Main menu","Hauptmenü"),return_to_menu)

func restart() -> void:
	paused = false
	run.begin(int(Time.get_ticks_usec()),progress.selected,progress.ranks)
	displayed_hp = run.hp
	banner_room = -1

func return_to_menu() -> void:
	get_tree().change_scene_to_file("res://menu.tscn")
