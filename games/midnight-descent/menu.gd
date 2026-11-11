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
const SoundBank = preload("res://sound_bank.gd")
const TechTree = preload("res://tech_tree.gd")
var content_panel: PanelContainer
var preview_panel: PanelContainer
var nav_buttons: Dictionary = {}
var page_tween: Tween
var tree: Control
var tree_key := "vitality"
var tree_rank := 1
var tree_detail: Label
var tree_buy: Button
var preview_clock := 0.0
var visual_smoke := false
var audio = SoundBank.new()
var progress = Progress.new()
var codex_page := 0
var sets_page := 0
var expedition_page := 0
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
	visual_smoke="--smoke-test" in OS.get_cmdline_user_args()
	if not visual_smoke: progress.load_save()
	add_child(audio)
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
	backdrop=Backdrop.new()
	add_child(backdrop)
	backdrop.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	backdrop.set("motion",get_tree().get_meta("descent_menu_motion",true))
	var margins:=MarginContainer.new()
	margins.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	for edge in ["left","right","top","bottom"]: margins.add_theme_constant_override("margin_"+edge,26)
	add_child(margins)
	var frame:=VBoxContainer.new()
	frame.add_theme_constant_override("separation",12)
	margins.add_child(frame)
	var header:=PanelContainer.new()
	header.add_theme_stylebox_override("panel",UI.panel(Color("#171222ed"),Color("#826b87"),10))
	frame.add_child(header)
	var nav:=HBoxContainer.new()
	nav.add_theme_constant_override("separation",8)
	header.add_child(nav)
	var brand:=label("MIDNIGHT / DESCENT",21,GOLD)
	brand.custom_minimum_size=Vector2(235,0)
	nav.add_child(brand)
	var spacer:=Control.new()
	spacer.size_flags_horizontal=Control.SIZE_EXPAND_FILL
	nav.add_child(spacer)
	for entry in [["home","Gate","Tor"],["expedition","Expedition","Expedition"],["progress","Tech tree","Tech Tree"],["sets","Loadouts","Start-Sets"],["codex","Codex","Kodex"],["settings","Settings","Optionen"]]:
		var id: String=entry[0]
		var button:=Button.new()
		button.text=tr_text(entry[1],entry[2])
		button.custom_minimum_size=Vector2(110,40)
		button.add_theme_font_size_override("font_size",15)
		UI.style_button(button,GOLD)
		button.mouse_entered.connect(func(): audio.play("hover"); animate_hover(button,true))
		button.mouse_exited.connect(func(): animate_hover(button,false))
		button.pressed.connect(func(): show_page(id))
		nav.add_child(button)
		nav_buttons[id]=button
	title_label=label("",25,LIGHT)
	frame.add_child(title_label)
	detail_label=label("",16,MUTED)
	frame.add_child(detail_label)
	var horizontal:=HBoxContainer.new()
	horizontal.size_flags_vertical=Control.SIZE_EXPAND_FILL
	horizontal.add_theme_constant_override("separation",18)
	frame.add_child(horizontal)
	content_panel=PanelContainer.new()
	content_panel.custom_minimum_size=Vector2(620,0)
	content_panel.add_theme_stylebox_override("panel",UI.panel(Color("#181326ed"),Color("#8c6c8b"),18))
	horizontal.add_child(content_panel)
	body=VBoxContainer.new()
	body.add_theme_constant_override("separation",8)
	content_panel.add_child(body)
	var space:=Control.new()
	space.size_flags_horizontal=Control.SIZE_EXPAND_FILL
	space.mouse_filter=Control.MOUSE_FILTER_IGNORE
	horizontal.add_child(space)
	preview_panel=PanelContainer.new()
	preview_panel.custom_minimum_size=Vector2(320,0)
	preview_panel.size_flags_vertical=Control.SIZE_SHRINK_END
	preview_panel.add_theme_stylebox_override("panel",UI.panel(Color("#1c1529f0"),Color("#a5868b"),16))
	horizontal.add_child(preview_panel)
	preview=VBoxContainer.new()
	preview.add_theme_constant_override("separation",10)
	preview_panel.add_child(preview)
	preview_title=label("",23,GOLD)
	preview.add_child(preview_title)
	preview_sprite=TextureRect.new()
	preview_sprite.custom_minimum_size=Vector2(0,128)
	preview_sprite.expand_mode=TextureRect.EXPAND_IGNORE_SIZE
	preview_sprite.stretch_mode=TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	preview_sprite.texture_filter=CanvasItem.TEXTURE_FILTER_NEAREST
	preview.add_child(preview_sprite)
	preview_detail=label("",17,LIGHT)
	preview.add_child(preview_detail)
	preview_info=label("",14,MUTED)
	preview.add_child(preview_info)
	var footer:=HBoxContainer.new()
	frame.add_child(footer)
	footer.add_child(label("THE MIDNIGHT CLUB  /  ROGUELITE",12,GOLD))
	var fill:=Control.new()
	fill.size_flags_horizontal=Control.SIZE_EXPAND_FILL
	footer.add_child(fill)
	footer.add_child(label(tr_text("ESC  •  BACK TO GATE","ESC  •  ZURÜCK ZUM TOR"),12,MUTED))

func _process(dt: float) -> void:
	if not is_instance_valid(preview_sprite) or not get_tree().get_meta("descent_menu_motion",true): return
	preview_clock+=dt
	if preview_sprite.texture is AtlasTexture and page in ["home","expedition","sets","codex"]:
		var texture: AtlasTexture=preview_sprite.texture
		texture.region=Rect2(int(preview_clock*2)%2*32,0,32,32)

func make_tree() -> void:
	tree=TechTree.new()
	body.add_child(tree)
	tree.motion=get_tree().get_meta("descent_menu_motion",true)
	tree.configure(progress,german,tree_key,tree_rank)
	tree.rank_selected.connect(select_tree_rank)
	tree_detail=label("",16,LIGHT)
	body.add_child(tree_detail)
	tree_buy=add_action("",buy_tree_rank)
	tree_buy.custom_minimum_size=Vector2(0,44)
	update_tree_detail()

func select_tree_rank(id: String, rank: int) -> void:
	tree_key=id
	tree_rank=rank
	audio.play("click")
	tree.configure(progress,german,tree_key,tree_rank)
	update_tree_detail()

func update_tree_detail() -> void:
	var item: Array=Content.UPGRADES[tree_key]
	var owned: int=progress.ranks[tree_key]
	tree_detail.text=item[1 if german else 0]+"  •  "+item[3 if german else 2]+"  |  "+tr_text("Rank","Rang")+" %d/%d" % [owned,item[5]]
	var can_buy: bool=tree_rank==owned+1 and tree_rank<=int(item[5]) and progress.essence>=progress.cost(tree_key)
	tree_buy.disabled=not can_buy
	if tree_rank<=owned: tree_buy.text=tr_text("Rank acquired","Rang erworben")
	elif tree_rank!=owned+1: tree_buy.text=tr_text("Requires previous rank","Benötigt vorherigen Rang")
	else: tree_buy.text=tr_text("Unlock rank","Rang freischalten")+" %d  •  %d " % [tree_rank,progress.cost(tree_key)]+tr_text("essence","Essenz")
	preview_title.text=item[1 if german else 0].to_upper()
	preview_detail.text=item[3 if german else 2]
	preview_info.text=tr_text("Available essence","Verfügbare Essenz")+"   "+str(progress.essence)+"\n"+tr_text("Select a node, then unlock its rank.","Wähle einen Knoten und schalte seinen Rang frei.")
	preview_sprite.texture=UI.atlas(UI.RELICS,TechTree.ICONS[TechTree.KEYS.find(tree_key)])

func buy_tree_rank() -> void:
	if tree_rank!=progress.ranks[tree_key]+1: return
	if progress.buy_upgrade(tree_key):
		audio.play("purchase")
		if not "--smoke-test" in OS.get_cmdline_user_args(): progress.save()
		tree.configure(progress,german,tree_key,tree_rank)
		update_tree_detail()
		if get_tree().get_meta("descent_menu_motion",true):
			tree.modulate=Color(1.35,1.25,1.05)
			tree.create_tween().tween_property(tree,"modulate",Color.WHITE,0.5)

func animate_hover(button: Button, entered: bool) -> void:
	if visual_smoke or not get_tree().get_meta("descent_menu_motion",true): return
	if not is_instance_valid(button) or button.disabled: return
	var previous: Variant=button.get_meta("hover_tween",null)
	if previous is Tween and previous.is_valid(): previous.kill()
	var tween:=button.create_tween()
	button.set_meta("hover_tween",tween)
	tween.set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	tween.tween_property(button,"modulate",Color(1.13,1.08,1.03) if entered else Color.WHITE,0.15)

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
			preview_info.text = tr_text("3 floors  •  78 rooms  •  48 relics","3 Ebenen  •  78 Räume  •  48 Relikte")
		"expedition","sets":
			var spec: Dictionary = Content.SETS[progress.selected]
			preview_title.text = Content.character_name(spec.character,german).to_upper()
			preview_sprite.texture = atlas_icon(PixelWorld.ACTORS[spec.character],Rect2(0,0,32,32))
			preview_detail.text = Content.loadout_display_name(progress.selected,german)
			if spec.character=="soulbinder":
				preview_detail.text+=" • "+tr_text("Harvest souls from kills","Seelen durch Kills sammeln")
			preview_info.text = tr_text("Base HP / main / special","Basis-LP / Haupt / Spezial")+"  %d / %d / %d" % [spec.hp,spec.damage,spec.bolt]
		"progress":
			preview_title.text = tr_text("SOUL ESSENCE","SEELENESSENZ")
			preview_detail.text = str(progress.essence)+"  •  "+tr_text("Permanent currency","Permanente Währung")
			preview_info.text = tr_text("Expeditions / victories / kills","Expeditionen / Siege / Gegner")+"  %d / %d / %d" % [progress.attempts,progress.wins,progress.total_kills]+"\n"+tr_text("Achievements","Errungenschaften")+"  %d/7  •  " % progress.achievements.size()+tr_text("Elites","Elite")+" %d/5" % progress.elite_kills+"\n"+tr_text("Deepest boss","Tiefster Boss")+"  %d/3" % progress.deepest_boss
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
	button.mouse_entered.connect(func(): audio.play("hover"); animate_hover(button,true))
	button.mouse_exited.connect(func(): animate_hover(button,false))
	button.pressed.connect(func(): audio.play("click"); callback.call())
	body.add_child(button)
	action_buttons.append(button)
	return button

func show_page(next: String) -> void:
	if page!=next: audio.play("navigate")
	page = next
	content_panel.custom_minimum_size=Vector2(790 if page=="progress" else 620,0)
	preview_panel.visible=page!="home"
	content_panel.add_theme_stylebox_override("panel",UI.panel(Color("#181326c9") if page=="home" else Color("#181326f5"),Color("#9b7d91"),16))
	for id in nav_buttons: UI.style_button(nav_buttons[id],GOLD,id==page)
	title_label.text={"home":tr_text("THE GATE","DAS TOR"),"expedition":tr_text("CHOOSE YOUR LOADOUT","WÄHLE DEIN START-SET"),"progress":tr_text("SOUL CONSTELLATION","SEELENKONSTELLATION"),"sets":tr_text("CHARACTERS & SETS","CHARAKTERE & SETS"),"codex":tr_text("RELIC ARCHIVE","RELIKTARCHIV"),"settings":tr_text("SETTINGS","EINSTELLUNGEN")}.get(page,"")
	for child in body.get_children():
		body.remove_child(child)
		child.queue_free()
	action_buttons.clear()
	update_preview()
	match page:
		"home":
			detail_label.text=tr_text("Fortune ends at the door. The depths are waiting.","Das Glück endet an der Tür. Die Tiefen warten.")
			var hero:=label("MIDNIGHT\nDESCENT",51,LIGHT)
			hero.add_theme_color_override("font_shadow_color",Color("#08060e"))
			hero.add_theme_constant_override("shadow_offset_y",5)
			body.add_child(hero)
			body.add_child(label(tr_text("THREE DEPTHS  /  78 ROOMS  /  48 RELICS","DREI EBENEN  /  78 RÄUME  /  48 RELIKTE"),14,GOLD))
			var primary:=add_action(tr_text("Begin expedition  ›","Expedition beginnen  ›"),func(): show_page("expedition"))
			primary.custom_minimum_size=Vector2(0,65)
			primary.add_theme_font_size_override("font_size",24)
			UI.style_button(primary,GOLD,true)
			var cards:=GridContainer.new()
			cards.columns=2
			cards.add_theme_constant_override("h_separation",10)
			cards.add_theme_constant_override("v_separation",10)
			body.add_child(cards)
			for entry in [["progress",tr_text("Soul constellation","Seelenkonstellation"),tr_text("Permanent upgrades","Permanente Upgrades"),3],["sets",tr_text("Characters","Charaktere"),tr_text("Eight starting sets","Acht Start-Sets"),4],["codex",tr_text("Relic archive","Reliktarchiv"),tr_text("Discover your builds","Entdecke deine Builds"),6],["settings",tr_text("Settings","Einstellungen"),tr_text("Display & audio","Anzeige & Audio"),14]]:
				var id: String=entry[0]
				var button:=Button.new()
				button.text=entry[1]+"\n"+entry[2]
				button.icon=UI.atlas(UI.RELICS,int(entry[3]))
				button.expand_icon=true
				button.add_theme_constant_override("icon_max_width",42)
				button.custom_minimum_size=Vector2(0,64)
				button.size_flags_horizontal=Control.SIZE_EXPAND_FILL
				button.add_theme_font_size_override("font_size",16)
				UI.style_button(button,Color("#ad90d5"))
				button.mouse_entered.connect(func(): audio.play("hover"); animate_hover(button,true))
				button.mouse_exited.connect(func(): animate_hover(button,false))
				button.pressed.connect(func(): show_page(id))
				cards.add_child(button)
			body.add_child(label(tr_text("Essence","Essenz")+"  "+str(progress.essence)+"    •    "+tr_text("Victories","Siege")+"  "+str(progress.wins)+"    •    "+tr_text("Deepest boss","Tiefster Boss")+"  %d/3" % progress.deepest_boss,15,GOLD))
			add_action(tr_text("Return to casino","Zurück zum Casino"),func(): get_tree().quit())
		"expedition":
			detail_label.text = tr_text("Choose a loadout. Each run starts fresh.","Wähle ein Start-Set. Jeder Run beginnt neu.")
			body.add_child(label(tr_text("AVAILABLE LOADOUTS","VERFÜGBARE START-SETS"),14,GOLD))
			var available: Array=progress.unlocked
			for i in range(expedition_page*4,mini(expedition_page*4+4,available.size())):
				var id: String=available[i]
				var chosen_id: String = id
				var spec: Dictionary=Content.SETS[id]
				var button:=add_action(Content.loadout_display_name(id,german)+("  •  "+tr_text("Selected","Ausgewählt") if progress.selected==id else ""),func(): select_set(chosen_id))
				button.icon=atlas_icon(PixelWorld.ACTORS[spec.character],Rect2(0,0,32,32))
				button.expand_icon=true
				button.add_theme_constant_override("icon_max_width",32)
				UI.style_button(button,GOLD,progress.selected==id)
			if available.size()>4:
				add_action(tr_text("More loadouts","Weitere Start-Sets")+"  %d/2" % (expedition_page+1),func(): expedition_page=1-expedition_page; show_page("expedition"))
			add_action(tr_text("Start run  ›","Run starten  ›"),start_run)
			add_action(tr_text("Back","Zurück"),func(): show_page("home"))
		"progress":
			detail_label.text=tr_text("Six linked paths. Select a rank to inspect and unlock it.","Sechs verbundene Pfade. Wähle einen Rang zum Ansehen und Freischalten.")
			make_tree()
			if not progress.save_ok: body.add_child(label(tr_text("Save failed","Speichern fehlgeschlagen"),14,Color("#ff839a")))
		"sets":
			detail_label.text = tr_text("Unlock distinct starting playstyles.","Schalte neue Start-Spielstile frei.")
			body.add_child(label(tr_text("ESSENCE","ESSENZ")+"   "+str(progress.essence),20,GOLD))
			var ids: Array=Content.SETS.keys()
			for i in range(sets_page*4,mini(sets_page*4+4,ids.size())):
				var id: String=ids[i]
				var spec: Dictionary = Content.SETS[id]
				var set_key: String = id
				var owned: bool = progress.unlocked.has(id)
				var locked: bool = owned or progress.essence<int(spec.cost) or (not Content.required_set(id).is_empty() and not progress.unlocked.has(Content.required_set(id)))
				var caption := Content.character_name(spec.character,german)+"  /  "+Content.loadout_display_name(id,german)
				caption += "  •  "+(tr_text("Owned","Freigeschaltet") if owned else str(spec.cost))
				var button:=add_action(caption,func(): buy_set(set_key),locked)
				button.icon=atlas_icon(PixelWorld.ACTORS[spec.character],Rect2(0,0,32,32))
				button.expand_icon=true
				button.add_theme_constant_override("icon_max_width",30)
			add_action(tr_text("More sets","Weitere Sets")+"  %d/2" % (sets_page+1),func(): sets_page=1-sets_page; show_page("sets"))
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
			add_action(tr_text("Next page","Nächste Seite")+"  %d/8" % (codex_page+1),func(): next_codex())
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
	if is_instance_valid(page_tween): page_tween.kill()
	body.modulate=Color.WHITE
	if get_tree().get_meta("descent_menu_motion",true) and not "--smoke-test" in OS.get_cmdline_user_args():
		body.modulate.a=0.0
		page_tween=create_tween()
		page_tween.set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
		page_tween.tween_property(body,"modulate:a",1.0,0.24)

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
		audio.play("purchase")
		progress.save()
		show_page("progress")

func buy_set(id: String) -> void:
	if progress.unlock_set(id):
		audio.play("unlock")
		progress.save()
		show_page("sets")

func next_codex() -> void:
	codex_page = (codex_page+1)%8
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
	if tree.nodes.size()!=26 or tree_buy.disabled==false:
		get_tree().quit(2)
		return
	progress.essence=100
	select_tree_rank("arcane",2)
	if not tree_buy.disabled:
		get_tree().quit(21)
		return
	select_tree_rank("arcane",1)
	buy_tree_rank()
	if progress.ranks.arcane!=1 or progress.essence!=82:
		get_tree().quit(22)
		return
	progress=Progress.new()
	show_page("sets")
	if action_buttons.size()!=6:
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
	if not audio.smoke_test():
		push_error("Descent sound bank regression failed")
		get_tree().quit(20)
		return
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
