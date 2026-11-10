extends Node2D
## Dedicated Godot gameplay scene; simulation is independent of rendering.
const Model = preload("res://run_model.gd")
const PixelWorld = preload("res://pixel_world.gd")
var world = PixelWorld.new()
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
	run.begin(int(Time.get_ticks_usec()))
	light_mask = 0
	add_child(world)
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
	if paused or run.state in ["reward","victory","defeat"]:
		return
	var direction := Vector2(float(Input.is_physical_key_pressed(KEY_D) or Input.is_physical_key_pressed(KEY_RIGHT))-float(Input.is_physical_key_pressed(KEY_A) or Input.is_physical_key_pressed(KEY_LEFT)), float(Input.is_physical_key_pressed(KEY_S) or Input.is_physical_key_pressed(KEY_DOWN))-float(Input.is_physical_key_pressed(KEY_W) or Input.is_physical_key_pressed(KEY_UP)))
	var dash := Input.is_physical_key_pressed(KEY_SPACE)
	var interact := Input.is_physical_key_pressed(KEY_E)
	run.tick(dt, direction, get_global_mouse_position(), Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT), Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT), dash and not last_dash)
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
	var target: String = "pause" if paused else run.state
	if target != overlay_state:
		overlay_state = target
		rebuild_overlay()
	world.update_view(dt)
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
	draw_rect(rect, Color("#0e0c17"))
	draw_rect(Rect2(rect.position, Vector2(rect.size.x*clampf(ratio,0,1),rect.size.y)), color)
	draw_rect(rect, Color("#615273"), false, 1)

func _draw() -> void:
	if not is_instance_valid(world) or world.model == null:
		return
	var gate: Vector2 = Model.EXIT
	if run.state == "exit":
		text_at(gate+Vector2(-86,76),tr_text("E  •  Descend","E  •  Absteigen"),16,GOLD)
	# Attack telegraphs and projectiles remain above the pixel-world layer.
	for enemy in run.enemies:
		var pos: Vector2 = enemy.pos
		if enemy.wind > 0:
			var rad: float = 126.0 if enemy.kind=="boss" else float(enemy.r)+44
			if enemy.kind=="caster":
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
	if not run.next_room():
		get_tree().quit(8)
		return
	paused = true
	_process(0.0)
	if overlay_state != "pause":
		get_tree().quit(9)
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

func draw_hud() -> void:
	draw_rect(Rect2(0,0,1280,103),Color("#171322"))
	draw_rect(Rect2(0,651,1280,69),Color("#171322"))
	draw_rect(Rect2(18,12,405,82),Color("#211b30"))
	draw_rect(Rect2(18,12,405,82),Color("#6b526e"),false,1)
	draw_rect(Rect2(440,12,365,82),Color("#211b30"))
	draw_rect(Rect2(440,12,365,82),Color("#6b526e"),false,1)
	draw_rect(Rect2(823,12,439,82),Color("#211b30"))
	draw_rect(Rect2(823,12,439,82),Color("#6b526e"),false,1)
	text_at(Vector2(32,32),tr_text("THE FORGOTTEN VAULT","DAS VERGESSENE GEWÖLBE"),16,GOLD)
	text_at(Vector2(32,61),tr_text("WARDEN","WÄCHTER"),24,WHITE)
	meter(Rect2(32,72,274,13),run.hp/run.max_hp,Color("#e26483"))
	text_at(Vector2(318,83),"%d / %d" % [run.hp,run.max_hp],16,WHITE)
	text_at(Vector2(470,35),tr_text("ROOM","RAUM")+"  %d / %d" % [run.room+1,Model.ROOM_COUNT],21,GOLD)
	for i in range(Model.ROOM_COUNT):
		var center := Vector2(493+i*50,69)
		if i< Model.ROOM_COUNT-1: draw_line(center,center+Vector2(50,0),Color("#5b456d"),2)
		draw_circle(center,10,GOLD if i<=run.room else Color("#332942"))
		draw_circle(center,10,Color("#c5a5d9"),false,1)
	text_at(Vector2(850,32),tr_text("SHARDS","SPLITTER")+"  %d" % run.shards,17,GOLD)
	text_at(Vector2(850,58),tr_text("DEFEATED","BESIEGT")+"  %d" % run.kills,16,MUTED)
	text_at(Vector2(1090,32),"%02d:%02d" % [int(run.elapsed)/60,int(run.elapsed)%60],22,WHITE)
	text_at(Vector2(1090,59),"ESC  "+tr_text("Pause","Pause"),16,MUTED)
	draw_line(Vector2(30,656),Vector2(1250,656),Color("#5a456d"),1)
	text_at(Vector2(32,682),tr_text("WASD  Move    LMB  Sword    RMB  Bolt    SPACE  Dash    E  Gate","WASD  Bewegen    LMT  Schwert    RMT  Geschoss    LEERTASTE  Dash    E  Tor"),16,WHITE)
	text_at(Vector2(32,704),tr_text("SPECIAL","SPEZIAL"),12,GOLD)
	meter(Rect2(110,694,106,9),1.0-run.bolt_timer/0.62,Color("#72cadf"))
	text_at(Vector2(234,704),"DASH",12,GOLD)
	meter(Rect2(284,694,100,9),1.0-run.dash_timer/run.dash_cooldown,VIOLET)
	text_at(Vector2(412,704),tr_text("ACTIVE ITEM: EMPTY","AKTIVES ITEM: LEER"),12,MUTED)
	text_at(Vector2(850,682),tr_text("RELICS","RELIKTE")+"  %d" % run.relics.size(),16,GOLD)
	var ids := ["blade","heart","boots","focus","ward","fury"]
	for i in range(mini(run.relics.size(),8)):
		var index := ids.find(run.relics[i])
		if index >= 0:
			draw_texture_rect_region(PixelWorld.RELICS,Rect2(940+i*37,663,32,32),Rect2(index*24,0,24,24))
	if run.state=="fight" and run.room==Model.ROOM_COUNT-1:
		text_at(Vector2(460,122),tr_text("THE VAULT KEEPER","DER TRESORWÄCHTER"),18,Color("#ff91bd"))

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
	for style_name in ["normal","hover","pressed","focus"]:
		var style := StyleBoxFlat.new()
		style.bg_color = Color("#31243e") if style_name=="normal" else Color("#5b3a72")
		style.border_color = Color("#9872b1") if style_name=="normal" else GOLD
		style.set_border_width_all(2)
		style.set_corner_radius_all(8)
		node.add_theme_stylebox_override(style_name,style)
	node.pressed.connect(callback)
	parent.add_child(node)
	return node

func relic_description(id: String) -> String:
	match id:
		"blade": return tr_text("Gilded Edge   •   +9 sword damage","Goldene Klinge   •   +9 Schwertschaden")
		"heart": return tr_text("Ruby Heart   •   +20 max HP, heal 35","Rubinherz   •   +20 max. LP, heilt 35")
		"boots": return tr_text("Shadow Steps   •   +32 movement speed","Schattenschritte   •   +32 Bewegungstempo")
		"focus": return tr_text("Arcane Focus   •   +11 bolt damage","Arkaner Fokus   •   +11 Geschossschaden")
		"ward": return tr_text("Iron Ward   •   +4 damage reduction","Eisenwall   •   +4 Schadensreduktion")
		"fury": return tr_text("Quickdraw   •   faster dash recharge","Schnellzug   •   kürzere Dash-Abklingzeit")
	return id

func select_relic(index: int) -> void:
	if run.choose_relic(index):
		play_sound("relic")

func rebuild_overlay() -> void:
	for child in overlay.get_children():
		overlay.remove_child(child)
		child.queue_free()
	if overlay_state not in ["pause","reward","victory","defeat"]:
		return
	var shade := ColorRect.new()
	shade.color = Color(0.035,0.025,0.065,0.82)
	shade.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	overlay.add_child(shade)
	var center := CenterContainer.new()
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	overlay.add_child(center)
	var panel := PanelContainer.new()
	panel.custom_minimum_size = Vector2(660,0)
	var style := StyleBoxFlat.new()
	style.bg_color = Color("#191325")
	style.border_color = GOLD
	style.set_border_width_all(2)
	style.set_corner_radius_all(14)
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
		"reward":
			add_label(column,tr_text("ROOM CLEARED","RAUM GESÄUBERT"),30,GOLD)
			add_label(column,tr_text("Choose a relic. Recover 12 HP and unlock the next gate.","Wähle ein Relikt. Heile 12 LP und öffne das nächste Tor."),17,MUTED)
			for i in range(run.offers.size()):
				var index := i
				add_button(column,"%d  ·  %s" % [i+1,relic_description(run.offers[i])],func(): select_relic(index))
		"pause":
			add_label(column,tr_text("EXPEDITION PAUSED","EXPEDITION PAUSIERT"),30,GOLD)
			add_button(column,tr_text("Continue","Fortsetzen"),func(): paused=false)
			add_button(column,tr_text("Abandon run / Main menu","Run aufgeben / Hauptmenü"),return_to_menu)
		"victory","defeat":
			var won := overlay_state=="victory"
			add_label(column,tr_text("VAULT CONQUERED","GEWÖLBE EROBERT") if won else tr_text("THE DEPTHS CLAIM YOU","DIE TIEFEN FORDERN DICH"),30,GOLD if won else Color("#f095a9"))
			add_label(column,tr_text("Run summary","Run-Zusammenfassung")+"\n"+tr_text("Rooms","Räume")+"  %d / %d  •  " % [run.room+1,Model.ROOM_COUNT]+tr_text("Defeated","Besiegt")+"  %d  •  " % run.kills+tr_text("Relics","Relikte")+"  %d" % run.relics.size(),19,WHITE)
			add_button(column,tr_text("New expedition","Neue Expedition"),restart)
			add_button(column,tr_text("Main menu","Hauptmenü"),return_to_menu)

func restart() -> void:
	paused = false
	run.begin(int(Time.get_ticks_usec()))

func return_to_menu() -> void:
	get_tree().change_scene_to_file("res://menu.tscn")
