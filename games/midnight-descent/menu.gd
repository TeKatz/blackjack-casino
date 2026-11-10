extends Control
## Standalone engine menu. No wagers, profile edits or simulated gameplay.
const GOLD := Color("#e6c58a")
const MUTED := Color("#b7aaca")
const LIGHT := Color("#f6eefb")
const Backdrop = preload("res://menu_backdrop.gd")
const PixelWorld = preload("res://pixel_world.gd")
var backdrop: Control
var preview: VBoxContainer
var preview_title: Label
var preview_detail: Label
var preview_sprite: TextureRect
var preview_info: Label
var german := false
var page := "home"
var body: VBoxContainer
var title_label: Label
var detail_label: Label
var chosen := "warden"
var action_buttons: Array[Button] = []

func _ready() -> void:
	for arg in OS.get_cmdline_user_args():
		if arg == "--language=de":
			german = true
	build_shell()
	show_page("home")
	if "--smoke-test" in OS.get_cmdline_user_args():
		call_deferred("smoke_test")

func tr_text(en: String, de: String) -> String:
	return de if german else en

func box_style(fill: Color, edge: Color, width: int = 1) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = fill
	style.border_color = edge
	style.set_border_width_all(width)
	style.set_corner_radius_all(8)
	style.shadow_color = Color(0,0,0,0.45)
	style.shadow_size = 5
	style.shadow_offset = Vector2(0,3)
	style.content_margin_left = 20
	style.content_margin_right = 20
	style.content_margin_top = 12
	style.content_margin_bottom = 12
	return style

func label(value: String, font_size: int, color: Color) -> Label:
	var node := Label.new()
	node.text = value
	node.add_theme_font_size_override("font_size", font_size)
	node.add_theme_color_override("font_color", color)
	node.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	return node

func build_shell() -> void:
	backdrop = Backdrop.new()
	add_child(backdrop)
	backdrop.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	backdrop.set("motion",get_tree().get_meta("descent_menu_motion",true))
	var margins := MarginContainer.new()
	margins.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	for edge in ["left", "right", "top", "bottom"]:
		margins.add_theme_constant_override("margin_" + edge, 34)
	add_child(margins)
	var frame := VBoxContainer.new()
	frame.add_theme_constant_override("separation", 8)
	margins.add_child(frame)
	frame.add_child(label("THE MIDNIGHT CLUB   /   ROGUELITE", 14, GOLD))
	title_label = label("MIDNIGHT DESCENT", 48, LIGHT)
	title_label.add_theme_color_override("font_shadow_color",Color("#08060e"))
	title_label.add_theme_constant_override("shadow_offset_y",4)
	frame.add_child(title_label)
	detail_label = label("", 18, MUTED)
	detail_label.custom_minimum_size = Vector2(0, 29)
	frame.add_child(detail_label)
	var horizontal := HBoxContainer.new()
	horizontal.size_flags_vertical = Control.SIZE_EXPAND_FILL
	horizontal.add_theme_constant_override("separation", 24)
	frame.add_child(horizontal)
	var panel := PanelContainer.new()
	panel.custom_minimum_size = Vector2(510,0)
	panel.add_theme_stylebox_override("panel", box_style(Color("#161124f2"), Color("#876a85"),2))
	horizontal.add_child(panel)
	body = VBoxContainer.new()
	body.add_theme_constant_override("separation", 8)
	panel.add_child(body)
	var spacer := Control.new()
	spacer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	spacer.mouse_filter = Control.MOUSE_FILTER_IGNORE
	horizontal.add_child(spacer)
	var right := PanelContainer.new()
	right.custom_minimum_size = Vector2(326,0)
	right.size_flags_vertical = Control.SIZE_SHRINK_END
	right.add_theme_stylebox_override("panel",box_style(Color("#171224ee"),Color("#8c6d7e"),2))
	horizontal.add_child(right)
	preview = VBoxContainer.new()
	preview.add_theme_constant_override("separation",8)
	right.add_child(preview)
	preview_title = label("",23,GOLD)
	preview.add_child(preview_title)
	preview_sprite = TextureRect.new()
	preview_sprite.custom_minimum_size = Vector2(0,112)
	preview_sprite.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	preview_sprite.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	preview_sprite.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	preview.add_child(preview_sprite)
	preview_detail = label("",16,LIGHT)
	preview.add_child(preview_detail)
	preview_info = label("",14,MUTED)
	preview.add_child(preview_info)
	frame.add_child(label(tr_text("THE FIRST DESCENT  •  PLAYABLE ALPHA   |   ESC  BACK", "DER ERSTE ABSTIEG  •  SPIELBARE ALPHA   |   ESC  ZURÜCK"), 13, GOLD))

func atlas_icon(texture: Texture2D, region: Rect2) -> AtlasTexture:
	var icon := AtlasTexture.new()
	icon.atlas = texture
	icon.region = region
	return icon

func update_preview() -> void:
	preview_sprite.texture = atlas_icon(PixelWorld.ACTORS["warden"],Rect2(0,0,32,32))
	match page:
		"home":
			preview_title.text = tr_text("THE FORGOTTEN VAULT","DAS VERGESSENE GEWÖLBE")
			preview_detail.text = tr_text("Beneath the club, the old gate waits.","Unter dem Club wartet das alte Tor.")
			preview_info.text = tr_text("5 rooms  •  3 enemy types  •  1 boss","5 Räume  •  3 Gegnertypen  •  1 Boss")
		"expedition":
			preview_title.text = tr_text("WARDEN","WÄCHTER")
			preview_detail.text = tr_text("Sword  /  Arcane bolt  /  Dash","Schwert  /  Arkanes Geschoss  /  Dash")
			preview_info.text = tr_text("100 HP  •  26 sword damage  •  17 bolt damage","100 LP  •  26 Schwertschaden  •  17 Geschossschaden")
		"codex":
			preview_title.text = tr_text("VAULT KEEPER","TRESORWÄCHTER")
			preview_sprite.texture = atlas_icon(PixelWorld.ACTORS["boss"],Rect2(0,0,32,32))
			preview_detail.text = tr_text("The final guardian of the vault.","Der letzte Wächter des Gewölbes.")
			preview_info.text = tr_text("Radial bolts and a more dangerous second phase.","Kreisförmige Geschosse und eine gefährlichere zweite Phase.")
		"settings":
			preview_title.text = tr_text("DISPLAY & AUDIO","ANZEIGE & AUDIO")
			preview_detail.text = tr_text("Adjust your experience.","Passe dein Spielerlebnis an.")
			preview_info.text = tr_text("Changes apply immediately in this session.","Änderungen gelten sofort für diese Sitzung.")

func codex_entry(index: int, text: String) -> void:
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation",10)
	body.add_child(row)
	var icon := TextureRect.new()
	icon.texture = atlas_icon(PixelWorld.RELICS,Rect2(index*24,0,24,24))
	icon.custom_minimum_size = Vector2(28,28)
	icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	icon.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	row.add_child(icon)
	var caption := label(text,16,LIGHT)
	caption.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(caption)

func set_volume(value: float) -> void:
	AudioServer.set_bus_volume_db(0,linear_to_db(maxf(value,0.001)))
	AudioServer.set_bus_mute(0,value<=0.001)

func toggle_motion(value: bool) -> void:
	get_tree().set_meta("descent_menu_motion",value)
	backdrop.set("motion",value)

func add_action(caption: String, callback: Callable, locked: bool = false) -> Button:
	var button := Button.new()
	button.text = caption
	button.custom_minimum_size = Vector2(0, 51)
	button.alignment = HORIZONTAL_ALIGNMENT_LEFT
	button.add_theme_font_size_override("font_size", 19)
	button.add_theme_color_override("font_color", LIGHT)
	button.add_theme_color_override("font_hover_color", GOLD)
	button.add_theme_color_override("font_pressed_color", GOLD)
	button.add_theme_color_override("font_disabled_color", Color("#827591"))
	button.add_theme_stylebox_override("normal", box_style(Color("#2b223b"), Color("#886c89"),1))
	button.add_theme_stylebox_override("hover", box_style(Color("#534064"), GOLD, 2))
	button.add_theme_stylebox_override("pressed", box_style(Color("#614680"), GOLD, 2))
	button.add_theme_stylebox_override("focus", box_style(Color("#00000000"), GOLD, 2))
	button.add_theme_stylebox_override("disabled", box_style(Color("#181526"), Color("#392d4c")))
	button.disabled = locked
	button.pressed.connect(callback)
	body.add_child(button)
	action_buttons.append(button)
	return button

func show_page(next: String) -> void:
	page = next
	for child in body.get_children():
		body.remove_child(child)
		child.queue_free()
	action_buttons.clear()
	update_preview()
	match page:
		"home":
			detail_label.text = tr_text("Fortune ends at the door. The depths are waiting.", "Das Glück endet an der Tür. Die Tiefen warten.")
			body.add_child(label(tr_text("THE GATE", "DAS TOR"), 14, GOLD))
			var primary := add_action(tr_text("New expedition  ›", "Neue Expedition  ›"), func(): show_page("expedition"))
			primary.add_theme_stylebox_override("normal",box_style(Color("#6a4936"),GOLD,2))
			add_action(tr_text("Codex", "Kodex"), func(): show_page("codex"))
			add_action(tr_text("Settings", "Einstellungen"), func(): show_page("settings"))
			add_action(tr_text("Return to casino", "Zurück zum Casino"), func(): get_tree().quit())
		"expedition":
			detail_label.text = tr_text("Choose your path into the depths.", "Wähle deinen Weg in die Tiefen.")
			body.add_child(label(tr_text("CHARACTER SELECTION", "CHARAKTERAUSWAHL"), 14, GOLD))
			add_action(tr_text("Warden  •  Selected", "Wächter  •  Ausgewählt"), select_warden)
			body.add_child(label(tr_text("A steadfast fighter. Sword, arcane bolts and a quick evasive dash.", "Ein standhafter Kämpfer. Schwert, arkane Geschosse und ein schneller Ausweichschritt."), 17, MUTED))
			add_action(tr_text("Dealer  •  Locked", "Dealer  •  Gesperrt"), func(): pass, true)
			add_action(tr_text("Start run  •  The Forgotten Vault", "Run starten  •  Das vergessene Gewölbe"), start_run)
			add_action(tr_text("Back", "Zurück"), func(): show_page("home"))
		"codex":
			detail_label.text = tr_text("Your discoveries will be recorded here.", "Deine Entdeckungen werden hier festgehalten.")
			body.add_child(label(tr_text("THE ARCHIVE", "DAS ARCHIV"), 14, GOLD))
			codex_entry(0,tr_text("Gilded Edge   +9 sword damage","Goldene Klinge   +9 Schwertschaden"))
			codex_entry(1,tr_text("Ruby Heart   +20 max HP, heal 35","Rubinherz   +20 max. LP, heilt 35"))
			codex_entry(2,tr_text("Shadow Steps   +32 movement speed","Schattenschritte   +32 Bewegungstempo"))
			codex_entry(3,tr_text("Arcane Focus   +11 bolt damage","Arkaner Fokus   +11 Geschossschaden"))
			codex_entry(4,tr_text("Iron Ward   +4 damage reduction","Eisenwall   +4 Schadensreduktion"))
			codex_entry(5,tr_text("Quickdraw   faster dash recharge","Schnellzug   kürzere Dash-Abklingzeit"))
			body.add_child(label(tr_text("Enemies: Stalker · Caster · Brute","Gegner: Stalker · Caster · Brute"),14,MUTED))
			add_action(tr_text("Back", "Zurück"), func(): show_page("home"))
		"settings":
			detail_label.text = tr_text("Adjust the standalone game window.", "Passe das eigenständige Spielfenster an.")
			body.add_child(label(tr_text("DISPLAY", "ANZEIGE"), 14, GOLD))
			add_action(tr_text("Toggle fullscreen", "Vollbild umschalten"), toggle_fullscreen)
			body.add_child(label(tr_text("MASTER VOLUME","GESAMTLAUTSTÄRKE"),14,GOLD))
			var volume := HSlider.new()
			volume.min_value = 0
			volume.max_value = 1
			volume.step = 0.05
			volume.value = 0.0 if AudioServer.is_bus_mute(0) else db_to_linear(AudioServer.get_bus_volume_db(0))
			volume.custom_minimum_size = Vector2(0,30)
			volume.value_changed.connect(set_volume)
			body.add_child(volume)
			var motion := CheckButton.new()
			motion.text = tr_text("Animated menu background","Animierter Menühintergrund")
			motion.button_pressed = get_tree().get_meta("descent_menu_motion",true)
			motion.toggled.connect(toggle_motion)
			body.add_child(motion)
			add_action(tr_text("Back", "Zurück"), func(): show_page("home"))
	if not action_buttons.is_empty():
		action_buttons[0].grab_focus()

func start_run() -> void:
	get_tree().change_scene_to_file("res://run.tscn")

func select_warden() -> void:
	chosen = "warden"
	show_page("expedition")

func toggle_fullscreen() -> void:
	var window := get_window()
	window.mode = Window.MODE_WINDOWED if window.mode == Window.MODE_FULLSCREEN else Window.MODE_FULLSCREEN

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel") and page != "home":
		show_page("home")
		get_viewport().set_input_as_handled()

func smoke_test() -> void:
	action_buttons[0].pressed.emit()
	if page != "expedition" or action_buttons[2].disabled or preview_sprite.texture == null:
		get_tree().quit(1)
		return
	action_buttons[3].pressed.emit()
	action_buttons[1].pressed.emit()
	if page != "codex":
		get_tree().quit(2)
		return
	action_buttons[0].pressed.emit()
	action_buttons[2].pressed.emit()
	if page != "settings":
		get_tree().quit(3)
		return
	action_buttons[1].pressed.emit()
	if page != "home":
		get_tree().quit(4)
		return
	var simulation = preload("res://run_model.gd").new()
	if not simulation.smoke_test():
		get_tree().quit(5)
		return
	print("Midnight Descent menu smoke PASS")
	print("Midnight Descent run smoke PASS")
	get_tree().quit(0)
