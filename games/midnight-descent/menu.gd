extends Control
## Standalone engine menu. No wagers, profile edits or simulated gameplay.
const GOLD := Color("#e6c58a")
const MUTED := Color("#b7aaca")
const LIGHT := Color("#f6eefb")
const ART = preload("res://art/descent.svg")
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
	var backdrop := TextureRect.new()
	backdrop.texture = ART
	backdrop.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	backdrop.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_COVERED
	backdrop.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	backdrop.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(backdrop)
	var margins := MarginContainer.new()
	margins.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	for edge in ["left", "right", "top", "bottom"]:
		margins.add_theme_constant_override("margin_" + edge, 46)
	add_child(margins)
	var frame := VBoxContainer.new()
	frame.add_theme_constant_override("separation", 12)
	margins.add_child(frame)
	frame.add_child(label("THE MIDNIGHT CLUB   /   ROGUELITE", 16, GOLD))
	title_label = label("MIDNIGHT DESCENT", 48, LIGHT)
	frame.add_child(title_label)
	detail_label = label("", 19, MUTED)
	detail_label.custom_minimum_size = Vector2(0, 42)
	frame.add_child(detail_label)
	var horizontal := HBoxContainer.new()
	horizontal.size_flags_vertical = Control.SIZE_EXPAND_FILL
	frame.add_child(horizontal)
	var panel := PanelContainer.new()
	panel.custom_minimum_size = Vector2(480, 0)
	panel.add_theme_stylebox_override("panel", box_style(Color("#100e20df"), Color("#5c447b")))
	horizontal.add_child(panel)
	body = VBoxContainer.new()
	body.add_theme_constant_override("separation", 9)
	panel.add_child(body)
	var spacer := Control.new()
	spacer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	spacer.mouse_filter = Control.MOUSE_FILTER_IGNORE
	horizontal.add_child(spacer)
	frame.add_child(label(tr_text("FOUNDATION BUILD  •  MENU PREVIEW  •  GODOT 4", "FOUNDATION BUILD  •  MENÜ-VORSCHAU  •  GODOT 4"), 14, GOLD))

func add_action(caption: String, callback: Callable, locked: bool = false) -> Button:
	var button := Button.new()
	button.text = caption
	button.custom_minimum_size = Vector2(0, 52)
	button.alignment = HORIZONTAL_ALIGNMENT_LEFT
	button.add_theme_font_size_override("font_size", 19)
	button.add_theme_color_override("font_color", LIGHT)
	button.add_theme_color_override("font_hover_color", GOLD)
	button.add_theme_color_override("font_pressed_color", GOLD)
	button.add_theme_color_override("font_disabled_color", Color("#827591"))
	button.add_theme_stylebox_override("normal", box_style(Color("#27203c"), Color("#6f568b")))
	button.add_theme_stylebox_override("hover", box_style(Color("#43305e"), GOLD, 2))
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
	match page:
		"home":
			detail_label.text = tr_text("Fortune ends at the door. The depths are waiting.", "Das Glück endet an der Tür. Die Tiefen warten.")
			body.add_child(label(tr_text("THE GATE", "DAS TOR"), 14, GOLD))
			add_action(tr_text("New expedition", "Neue Expedition"), func(): show_page("expedition"))
			add_action(tr_text("Codex", "Kodex"), func(): show_page("codex"))
			add_action(tr_text("Settings", "Einstellungen"), func(): show_page("settings"))
			add_action(tr_text("Return to casino", "Zurück zum Casino"), func(): get_tree().quit())
		"expedition":
			detail_label.text = tr_text("Choose your path into the depths.", "Wähle deinen Weg in die Tiefen.")
			body.add_child(label(tr_text("CHARACTER SELECTION", "CHARAKTERAUSWAHL"), 14, GOLD))
			add_action(tr_text("Warden  •  Selected", "Wächter  •  Ausgewählt"), select_warden)
			body.add_child(label(tr_text("A steadfast fighter. Balanced defence and close-range combat are planned for this class.", "Ein standhafter Kämpfer. Ausgewogene Verteidigung und Nahkampf sind für diese Klasse geplant."), 17, MUTED))
			add_action(tr_text("Dealer  •  Locked", "Dealer  •  Gesperrt"), func(): pass, true)
			add_action(tr_text("Start run  •  Coming in next phase", "Run starten  •  Nächste Phase"), func(): pass, true)
			add_action(tr_text("Back", "Zurück"), func(): show_page("home"))
		"codex":
			detail_label.text = tr_text("Your discoveries will be recorded here.", "Deine Entdeckungen werden hier festgehalten.")
			body.add_child(label(tr_text("THE ARCHIVE", "DAS ARCHIV"), 14, GOLD))
			body.add_child(label(tr_text("Relics, enemies, weapons and bosses will unlock as the expedition systems are built. No discoveries yet.", "Relikte, Gegner, Waffen und Bosse werden mit dem Ausbau der Expedition freigeschaltet. Noch keine Entdeckungen."), 21, LIGHT))
			add_action(tr_text("Back", "Zurück"), func(): show_page("home"))
		"settings":
			detail_label.text = tr_text("Adjust the standalone game window.", "Passe das eigenständige Spielfenster an.")
			body.add_child(label(tr_text("DISPLAY", "ANZEIGE"), 14, GOLD))
			add_action(tr_text("Toggle fullscreen", "Vollbild umschalten"), toggle_fullscreen)
			add_action(tr_text("Back", "Zurück"), func(): show_page("home"))
	if not action_buttons.is_empty():
		action_buttons[0].grab_focus()

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
	if page != "expedition" or not action_buttons[2].disabled:
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
	print("Midnight Descent menu smoke PASS")
	get_tree().quit(0)
