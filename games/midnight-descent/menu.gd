extends Control
## Standalone engine menu. No wagers, profile edits or simulated gameplay.
const GOLD := Color("#e6c58a")
const MUTED := Color("#b7aaca")
const LIGHT := Color("#f6eefb")
const Backdrop = preload("res://menu_backdrop.gd")
const PixelWorld = preload("res://pixel_world.gd")
const Content = preload("res://content.gd")
const Progress = preload("res://progress.gd")
const UI = preload("res://ui_theme.gd")
var progress = Progress.new()
var codex_page := 0
var codex_filter := "all"
var feedback := ""
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
	if not "--smoke-test" in OS.get_cmdline_user_args(): progress.load_save()
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
	style.content_margin_top = 8
	style.content_margin_bottom = 8
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
	panel.custom_minimum_size = Vector2(580,0)
	panel.add_theme_stylebox_override("panel", box_style(Color("#161124f2"), Color("#876a85"),2))
	horizontal.add_child(panel)
	body = VBoxContainer.new()
	body.add_theme_constant_override("separation", 6)
	panel.add_child(body)
	var spacer := Control.new()
	spacer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	spacer.mouse_filter = Control.MOUSE_FILTER_IGNORE
	horizontal.add_child(spacer)
	var right := PanelContainer.new()
	right.custom_minimum_size = Vector2(350,0)
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
			preview_info.text = tr_text("2 floors  •  40 relics  •  6 starting sets","2 Ebenen  •  40 Relikte  •  6 Start-Sets")
		"expedition","sets":
			var spec: Dictionary = Content.SETS[progress.selected]
			preview_title.text = tr_text("WARDEN","WÄCHTER") if spec.character=="warden" else ("DEALER" if spec.character=="dealer" else tr_text("ARCANIST","ARKANIST"))
			preview_sprite.texture = atlas_icon(PixelWorld.ACTORS[spec.character],Rect2(0,0,32,32))
			preview_detail.text = Content.loadout_display_name(progress.selected,german)
			preview_info.text = tr_text("Base HP / main / special","Basis-LP / Haupt / Spezial")+"  %d / %d / %d" % [spec.hp,spec.damage,spec.bolt]
		"progress":
			preview_title.text = tr_text("SOUL ESSENCE","SEELENESSENZ")
			preview_detail.text = str(progress.essence)+"  •  "+tr_text("Permanent currency","Permanente Währung")
			preview_info.text = tr_text("Expeditions / victories / kills","Expeditionen / Siege / Gegner")+"  %d / %d / %d" % [progress.attempts,progress.wins,progress.total_kills]+"\n"+tr_text("Achievements","Errungenschaften")+"  %d/2  •  " % progress.achievements.size()+tr_text("Elites","Elite")+" %d/5" % progress.elite_kills
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
	icon.texture = UI.atlas(UI.RELICS,index)
	icon.custom_minimum_size = Vector2(40,40)
	icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	icon.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	row.add_child(icon)
	var caption := label(text,15,LIGHT)
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
	button.custom_minimum_size = Vector2(0, 40)
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
	UI.style_button(button,GOLD)
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
			body.add_child(label(tr_text("Explore two depths, forge your build and return stronger.","Erkunde zwei Ebenen, forme deinen Build und kehre stärker zurück."),18,LIGHT))
			body.add_child(label(tr_text("Soul essence","Seelenessenz")+"   "+str(progress.essence)+"     •     "+tr_text("Victories","Siege")+"   "+str(progress.wins),15,GOLD))
			var primary := add_action(tr_text("New expedition  ›", "Neue Expedition  ›"), func(): show_page("expedition"))
			primary.add_theme_stylebox_override("normal",box_style(Color("#6a4936"),GOLD,2))
			add_action(tr_text("Progression", "Fortschritt"), func(): show_page("progress"))
			add_action(tr_text("Codex", "Kodex"), func(): show_page("codex"))
			add_action(tr_text("Settings", "Einstellungen"), func(): show_page("settings"))
			add_action(tr_text("Return to casino", "Zurück zum Casino"), func(): get_tree().quit())
		"expedition":
			detail_label.text = tr_text("Choose a loadout. Each run starts fresh.","Wähle ein Start-Set. Jeder Run beginnt neu.")
			body.add_child(label(tr_text("AVAILABLE LOADOUTS","VERFÜGBARE START-SETS"),14,GOLD))
			for id in progress.unlocked:
				var chosen_id: String = id
				var spec: Dictionary=Content.SETS[id]
				var button:=add_action(Content.loadout_display_name(id,german)+("  •  "+tr_text("Selected","Ausgewählt") if progress.selected==id else ""),func(): select_set(chosen_id))
				button.icon=atlas_icon(PixelWorld.ACTORS[spec.character],Rect2(0,0,32,32))
				button.expand_icon=true
				button.add_theme_constant_override("icon_max_width",32)
				UI.style_button(button,GOLD,progress.selected==id)
			add_action(tr_text("Start run  ›","Run starten  ›"),start_run)
			add_action(tr_text("Back","Zurück"),func(): show_page("home"))
		"progress":
			detail_label.text = tr_text("Invest essence earned from completed runs.","Investiere Seelenessenz aus abgeschlossenen Runs.")
			body.add_child(label(tr_text("ESSENCE","ESSENZ")+"   "+str(progress.essence),20,GOLD))
			if not progress.save_ok: body.add_child(label(tr_text("Save failed","Speichern fehlgeschlagen"),14,Color("#ff839a")))
			for key in Content.UPGRADES:
				var upgrade: Array = Content.UPGRADES[key]
				var id: String = key
				var rank: int = progress.ranks[key]
				var caption: String = upgrade[1 if german else 0]+"  %d/%d  •  " % [rank,upgrade[5]]+str(progress.cost(key))+"  |  "+upgrade[3 if german else 2]
				var button := add_action(caption,func(): buy_rank(id),rank>=upgrade[5] or progress.essence<progress.cost(key))
				button.tooltip_text = upgrade[3 if german else 2]
				button.add_theme_font_size_override("font_size",15)
			add_action(tr_text("Unlock characters & sets  ›","Charaktere & Sets freischalten  ›"),func(): show_page("sets"))
			add_action(tr_text("Back","Zurück"),func(): show_page("home"))
		"sets":
			detail_label.text = tr_text("Unlock distinct starting playstyles.","Schalte neue Start-Spielstile frei.")
			body.add_child(label(tr_text("ESSENCE","ESSENZ")+"   "+str(progress.essence),20,GOLD))
			for id in Content.SETS:
				var spec: Dictionary = Content.SETS[id]
				var set_key: String = id
				var owned: bool = progress.unlocked.has(id)
				var locked: bool = owned or progress.essence<int(spec.cost) or (id=="dealer_joker" and not progress.unlocked.has("dealer_cards")) or (id=="arcanist_frost" and not progress.unlocked.has("arcanist_fire"))
				var caption := (tr_text("Warden","Wächter") if spec.character=="warden" else ("Dealer" if spec.character=="dealer" else tr_text("Arcanist","Arkanist")))+"  /  "+Content.loadout_display_name(id,german)
				caption += "  •  "+(tr_text("Owned","Freigeschaltet") if owned else str(spec.cost))
				var button:=add_action(caption,func(): buy_set(set_key),locked)
				button.icon=atlas_icon(PixelWorld.ACTORS[spec.character],Rect2(0,0,32,32))
				button.expand_icon=true
				button.add_theme_constant_override("icon_max_width",30)
			add_action(tr_text("Back","Zurück"),func(): show_page("progress"))
		"codex":
			detail_label.text = tr_text("Relic library — discoveries are saved after each run.","Reliktbibliothek — Entdeckungen werden nach Runs gespeichert.")
			body.add_child(label(tr_text("RELICS DISCOVERED","RELIKTE ENTDECKT")+"  %d / %d" % [progress.discovered.size(),Content.RELICS.size()],14,GOLD))
			var ids: Array = Content.RELICS.keys()
			for i in range(codex_page*6,mini(codex_page*6+6,ids.size())):
				var id: String = ids[i]
				var item: Array=Content.RELICS[id]
				var rarity: int=Content.rarity(id)
				var row:=HBoxContainer.new()
				row.add_theme_constant_override("separation",10)
				body.add_child(row)
				var icon:=TextureRect.new()
				icon.texture=UI.atlas(UI.RELICS,int(item[4]))
				icon.custom_minimum_size=Vector2(38,38)
				icon.expand_mode=TextureRect.EXPAND_IGNORE_SIZE
				icon.stretch_mode=TextureRect.STRETCH_KEEP_ASPECT_CENTERED
				icon.texture_filter=CanvasItem.TEXTURE_FILTER_NEAREST
				row.add_child(icon)
				var details:=VBoxContainer.new()
				details.size_flags_horizontal=Control.SIZE_EXPAND_FILL
				row.add_child(details)
				details.add_child(label(item[1 if german else 0]+("  ✓" if progress.discovered.has(id) else ""),16,UI.RARITY_COLORS[rarity]))
				details.add_child(label(item[3 if german else 2],13,MUTED))
			add_action(tr_text("Next page","Nächste Seite")+"  %d/7" % (codex_page+1),func(): next_codex())
			add_action(tr_text("Back","Zurück"),func(): show_page("home"))
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
	progress.save()
	get_tree().change_scene_to_file("res://run.tscn")

func select_set(id: String) -> void:
	if progress.unlocked.has(id):
		progress.selected = id
		progress.save()
		show_page("expedition")

func buy_rank(id: String) -> void:
	if progress.buy_upgrade(id):
		progress.save()
		show_page("progress")

func buy_set(id: String) -> void:
	if progress.unlock_set(id):
		progress.save()
		show_page("sets")

func next_codex() -> void:
	codex_page = (codex_page+1)%7
	show_page("codex")

func toggle_fullscreen() -> void:
	var window := get_window()
	window.mode = Window.MODE_WINDOWED if window.mode == Window.MODE_FULLSCREEN else Window.MODE_FULLSCREEN

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel") and page != "home":
		show_page("home")
		get_viewport().set_input_as_handled()

func smoke_test() -> void:
	show_page("expedition")
	if action_buttons.size()!=3 or preview_sprite.texture==null:
		get_tree().quit(1)
		return
	show_page("progress")
	if action_buttons.size()!=6:
		get_tree().quit(2)
		return
	show_page("sets")
	if action_buttons.size()!=7:
		get_tree().quit(3)
		return
	show_page("codex")
	next_codex()
	if codex_page!=1:
		get_tree().quit(4)
		return
	show_page("settings")
	toggle_motion(false)
	if backdrop.get("motion") != false:
		get_tree().quit(11)
		return
	toggle_motion(true)
	var simulation = preload("res://run_model.gd").new()
	if not simulation.smoke_test():
		push_error("Descent run model regression failed")
		get_tree().quit(5)
		return
	if not progress.smoke_test():
		push_error("Descent progression regression failed")
		get_tree().quit(5)
		return
	print("Midnight Descent menu smoke PASS")
	print("Midnight Descent run smoke PASS")
	get_tree().quit(0)
