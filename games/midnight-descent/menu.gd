extends Control
## Standalone engine menu. No wagers, profile edits or simulated gameplay.
const GOLD := Color("#dbba81")
const MUTED := Color("#b1c0bb")
const LIGHT := Color("#f3eddc")
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
var cinematic_playing := false
var cinematic_clock := 0.0
var cinematic_changed_scene := false
var interface_frame: VBoxContainer
var cinematic_fade: ColorRect
var inspected_set := "warden_guard"
var loadout_cards: Array[Button]=[]
var footer_left: Label
var footer_right: Label
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
	UI.install_theme(self)
	if not visual_smoke: progress.load_save()
	inspected_set=progress.selected
	add_child(audio)
	build_shell()
	show_page("home")
	if "--smoke-test" in OS.get_cmdline_user_args():
		call_deferred("smoke_test")

func tr_text(en: String, de: String) -> String:
	return de if german else en

func box_style(fill: Color, edge: Color, width: int = 1) -> StyleBoxFlat:
	var style := UI.panel(fill,edge,12)
	style.set_border_width_all(width)
	style.border_width_top=maxi(2,width)
	return style

func label(value: String, font_size: int, color: Color) -> Label:
	var node := Label.new()
	node.text = value
	node.add_theme_font_size_override("font_size", font_size)
	node.add_theme_color_override("font_color", color)
	if font_size >= 23: UI.set_display(node)
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
	interface_frame=frame
	frame.add_theme_constant_override("separation",12)
	margins.add_child(frame)
	var header:=PanelContainer.new()
	header.add_theme_stylebox_override("panel",UI.panel(Color("#132326ed"),Color("#6d8b87"),12))
	frame.add_child(header)
	var nav:=HBoxContainer.new()
	nav.add_theme_constant_override("separation",8)
	header.add_child(nav)
	var brand:=label("MIDNIGHT DESCENT",21,GOLD)
	UI.set_display(brand)
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
	content_panel.add_theme_stylebox_override("panel",UI.panel(Color("#172729ec"),Color("#547b78"),20))
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
	preview_panel.add_theme_stylebox_override("panel",UI.panel(Color("#1d2d2df2"),Color("#738d7d"),18))
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
	footer_left=label("THE MIDNIGHT CLUB  /  ROGUELITE",12,GOLD)
	footer_left.autowrap_mode=TextServer.AUTOWRAP_OFF
	footer_left.custom_minimum_size=Vector2(280,20)
	footer.add_child(footer_left)
	var fill:=Control.new()
	fill.size_flags_horizontal=Control.SIZE_EXPAND_FILL
	footer.add_child(fill)
	footer_right=label(tr_text("ESC  •  BACK TO GATE","ESC  •  ZURÜCK ZUM TOR"),12,MUTED)
	footer_right.autowrap_mode=TextServer.AUTOWRAP_OFF
	footer_right.custom_minimum_size=Vector2(220,20)
	footer_right.horizontal_alignment=HORIZONTAL_ALIGNMENT_RIGHT
	footer.add_child(footer_right)
	cinematic_fade=ColorRect.new()
	cinematic_fade.color=Color(0.03,0.02,0.06,0)
	cinematic_fade.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	cinematic_fade.mouse_filter=Control.MOUSE_FILTER_IGNORE
	add_child(cinematic_fade)

func _process(dt: float) -> void:
	if cinematic_playing:
		cinematic_clock+=dt
		if cinematic_clock>=1.96:
			cinematic_fade.color=Color(0.03,0.02,0.06,clampf((cinematic_clock-1.96)/0.55,0,1))
		if cinematic_clock>=2.56 and not cinematic_changed_scene:
			cinematic_changed_scene=true
			get_tree().change_scene_to_file("res://run.tscn")
		return
	if not is_instance_valid(preview_sprite) or not get_tree().get_meta("descent_menu_motion",true): return
	preview_clock+=dt
	if preview_sprite.texture is AtlasTexture and page in ["home","expedition","sets","codex"]:
		var texture: AtlasTexture=preview_sprite.texture
		texture.region=Rect2(int(preview_clock*2)%2*32,0,32,32)

func make_tree() -> void:
	var tree_scroll:=ScrollContainer.new()
	tree_scroll.custom_minimum_size=Vector2(748,345)
	tree_scroll.size_flags_horizontal=Control.SIZE_EXPAND_FILL
	tree_scroll.horizontal_scroll_mode=ScrollContainer.SCROLL_MODE_DISABLED
	body.add_child(tree_scroll)
	tree=TechTree.new()
	tree_scroll.add_child(tree)
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
	var can_buy: bool=tree_rank==owned+1 and tree_rank<=int(item[5]) and progress.boss_souls>=progress.cost(tree_key)
	tree_buy.disabled=not can_buy
	if tree_rank<=owned: tree_buy.text=tr_text("Rank acquired","Rang erworben")
	elif tree_rank!=owned+1: tree_buy.text=tr_text("Requires previous rank","Benötigt vorherigen Rang")
	else: tree_buy.text=tr_text("Unlock rank","Rang freischalten")+" %d  •  %d " % [tree_rank,progress.cost(tree_key)]+tr_text("boss souls","Boss-Seelen")
	preview_title.text=item[1 if german else 0].to_upper()
	preview_detail.text=item[3 if german else 2]
	preview_info.text=tr_text("Available boss souls","Verfügbare Boss-Seelen")+"   "+str(progress.boss_souls)+"\n"+tr_text("Essence for character unlocks","Essenz für Charaktere")+"   "+str(progress.essence)+"\n"+tr_text("One soul per defeated boss. Later ranks cost more.","Eine Seele pro Boss. Höhere Ränge kosten mehr.")
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
	tween.tween_property(button,"modulate",Color(1.10,1.17,1.15) if entered else Color.WHITE,0.18)

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
			var spec: Dictionary=Content.SETS[inspected_set]
			preview_title.text=Content.character_name(spec.character,german).to_upper()
			preview_sprite.texture=atlas_icon(PixelWorld.ACTORS[spec.character],Rect2(0,0,32,32))
			preview_detail.text=Content.loadout_display_name(inspected_set,german)+"\n"+loadout_description(inspected_set)
			preview_info.text=tr_text("BASE STATS","BASISWERTE")+"\n"+tr_text("Health","Lebenspunkte")+"  %d\n" % spec.hp+tr_text("Main damage","Hauptschaden")+"  %d\n" % spec.damage+tr_text("Special damage","Spezialschaden")+"  %d\n" % spec.bolt+tr_text("Attack / special cooldown","Angriff / Spezial-Abklingzeit")+"  %.2f / %.2fs\n" % [spec.attack,spec.special]+tr_text("Movement / dash","Tempo / Dash")+"  %d / %.2fs" % [spec.speed,spec.dash]
		"progress":
			preview_title.text = tr_text("BOSS SOULS","BOSS-SEELEN")
			preview_detail.text = str(progress.boss_souls)+"  •  "+tr_text("For the skill tree","Für den Skill Tree")
			preview_info.text = tr_text("Essence for loadouts","Essenz für Start-Sets")+"  "+str(progress.essence)+"\n"+tr_text("Expeditions / victories / kills","Expeditionen / Siege / Gegner")+"  %d / %d / %d" % [progress.attempts,progress.wins,progress.total_kills]+"\n"+tr_text("Achievements","Errungenschaften")+"  %d/7  •  " % progress.achievements.size()+tr_text("Elites","Elite")+" %d/5" % progress.elite_kills+"\n"+tr_text("Deepest boss","Tiefster Boss")+"  %d/3" % progress.deepest_boss
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
	button.add_theme_color_override("font_hover_color", LIGHT)
	button.add_theme_color_override("font_pressed_color", GOLD)
	button.add_theme_color_override("font_disabled_color", Color("#81908f"))
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
	content_panel.custom_minimum_size=Vector2(790 if page in ["progress","expedition","sets"] else 620,0)
	preview_panel.visible=page!="home"
	preview_panel.size_flags_vertical=Control.SIZE_FILL if page in ["expedition","sets"] else Control.SIZE_SHRINK_END
	content_panel.add_theme_stylebox_override("panel",UI.panel(Color("#172729db") if page=="home" else Color("#18282af3"),Color("#648582"),18))
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
			var hero:=label("MIDNIGHT\nDESCENT",47,LIGHT)
			# Prevent dark second glyph outlines at fractional window scales.
			hero.add_theme_color_override("font_shadow_color",Color.TRANSPARENT)
			hero.add_theme_constant_override("shadow_offset_y",0)
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
				UI.style_button(button,Color("#83b9ac"))
				button.mouse_entered.connect(func(): audio.play("hover"); animate_hover(button,true))
				button.mouse_exited.connect(func(): animate_hover(button,false))
				button.pressed.connect(func(): show_page(id))
				cards.add_child(button)
			body.add_child(label(tr_text("Boss souls","Boss-Seelen")+"  "+str(progress.boss_souls)+"    •    "+tr_text("Essence","Essenz")+"  "+str(progress.essence)+"    •    "+tr_text("Victories","Siege")+"  "+str(progress.wins),15,GOLD))
			add_action(tr_text("Return to casino","Zurück zum Casino"),func(): get_tree().quit())
		"expedition":
			detail_label.text=tr_text("Inspect any of the eight sets. Choose an unlocked set to begin.","Sieh dir alle acht Sets an. Wähle ein freigeschaltetes Set für den Run.")
			build_loadout_grid()
		"progress":
			detail_label.text=tr_text("Eight paths. Defeat bosses to earn souls for permanent upgrades.","Acht Pfade. Besiege Bosse und investiere ihre Seelen in permanente Upgrades.")
			make_tree()
			if not progress.save_ok: body.add_child(label(tr_text("Save failed","Speichern fehlgeschlagen"),14,Color("#ff839a")))
		"sets":
			detail_label.text=tr_text("Inspect every character and unlock new playstyles.","Entdecke alle Charaktere und schalte neue Spielweisen frei.")
			build_loadout_grid()
		"codex":
			detail_label.text = tr_text("Relic library — discoveries are saved after each run.","Reliktbibliothek — Entdeckungen werden nach Runs gespeichert.")
			body.add_child(label(tr_text("RELICS DISCOVERED","RELIKTE ENTDECKT")+"  %d / %d" % [progress.discovered.size(),Content.RELICS.size()],14,GOLD))
			var ids: Array = Content.RELICS.keys()
			for i in range(codex_page*6,mini(codex_page*6+6,ids.size())):
				var id: String = ids[i]
				var item: Array=Content.RELICS[id]
				var rarity: int=Content.rarity(id)
				var card:=PanelContainer.new()
				card.add_theme_stylebox_override("panel",UI.panel(Color("#233539c9"),Color("#54726d"),8))
				body.add_child(card)
				var row:=HBoxContainer.new()
				row.add_theme_constant_override("separation",12)
				card.add_child(row)
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
			volume.modulate=Color("#9fd2c2")
			volume.min_value = 0
			volume.max_value = 1
			volume.step = 0.05
			volume.value = 0.0 if AudioServer.is_bus_mute(0) else db_to_linear(AudioServer.get_bus_volume_db(0))
			volume.custom_minimum_size = Vector2(0,30)
			volume.value_changed.connect(set_volume)
			body.add_child(volume)
			var motion := CheckButton.new()
			motion.add_theme_color_override("font_color",LIGHT)
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

func loadout_description(id: String) -> String:
	match id:
		"warden_guard": return tr_text("Balanced sword fighter. Main: close-range slash. Special: rapid arcane bolt.","Ausgewogener Schwertkämpfer. Haupt: Nahkampfschlag. Spezial: schnelles Arkangeschoss.")
		"warden_executioner": return tr_text("Heavy melee fighter. Wide swings and a powerful radial sweep.","Schwerer Nahkämpfer. Weite Schläge und starker Rundumschlag.")
		"dealer_cards": return tr_text("Mobile ranged fighter. Fast cards and a three-card spread.","Mobiler Fernkämpfer. Schnelle Karten und Dreifachsalve.")
		"dealer_joker": return tr_text("Risky ranged build. Special card damage varies randomly.","Riskanter Fernkampf. Spezialkarten verursachen zufälligen Schaden.")
		"arcanist_fire": return tr_text("Burning projectiles and explosive fire burst.","Brennende Geschosse und explosive Feuerwelle.")
		"arcanist_frost": return tr_text("Chill enemies with icy shots and a freezing nova.","Verlangsame Gegner mit Eisgeschossen und Frostnova.")
		"soulbinder_reaper": return tr_text("Collect souls from kills. Spend up to three for area damage and healing.","Sammle Seelen durch Kills. Verbrauche bis zu drei für Flächenschaden und Heilung.")
		"soulbinder_wraith": return tr_text("Ranged soul magic. Spend souls to summon a longer-lived spirit and fire more bolts.","Seelenmagie aus der Ferne. Seelen verlängern die Geisterbeschwörung und erhöhen die Geschosszahl.")
	return ""

func build_loadout_grid() -> void:
	loadout_cards.clear()
	body.add_child(label(tr_text("ESSENCE","ESSENZ")+"  "+str(progress.essence)+"    •    "+tr_text("Unlocked","Freigeschaltet")+"  %d/8" % progress.unlocked.size(),16,GOLD))
	var grid:=GridContainer.new()
	grid.columns=2
	grid.add_theme_constant_override("h_separation",10)
	grid.add_theme_constant_override("v_separation",8)
	body.add_child(grid)
	for id in Content.SETS:
		var key: String=id
		var spec: Dictionary=Content.SETS[key]
		var owned: bool=progress.unlocked.has(key)
		var color: Color={"warden":GOLD,"dealer":Color("#d69fa0"),"arcanist":Color("#95bad5"),"soulbinder":Color("#9acbb0")}[spec.character]
		var card:=Button.new()
		card.text=Content.character_name(spec.character,german)+" / "+Content.loadout_display_name(key,german)+"\n"+(tr_text("Equipped","Ausgewählt") if progress.selected==key else (tr_text("Unlocked","Freigeschaltet") if owned else tr_text("Locked","Gesperrt")+"  •  "+str(spec.cost)+" "+tr_text("essence","Essenz")))
		card.icon=atlas_icon(PixelWorld.ACTORS[spec.character],Rect2(0,0,32,32))
		card.expand_icon=true
		card.add_theme_constant_override("icon_max_width",52)
		card.custom_minimum_size=Vector2(0,76)
		card.size_flags_horizontal=Control.SIZE_EXPAND_FILL
		card.alignment=HORIZONTAL_ALIGNMENT_LEFT
		card.add_theme_font_size_override("font_size",15)
		UI.style_button(card,color,key==inspected_set)
		card.tooltip_text=loadout_description(key)
		card.mouse_entered.connect(func(): audio.play("hover"); animate_hover(card,true))
		card.mouse_exited.connect(func(): animate_hover(card,false))
		card.pressed.connect(func(): inspect_loadout(key))
		grid.add_child(card)
		loadout_cards.append(card)
	var owned: bool=progress.unlocked.has(inspected_set)
	var spec: Dictionary=Content.SETS[inspected_set]
	var required: String=Content.required_set(inspected_set)
	if page=="expedition":
		var primary:=add_action(tr_text("START EXPEDITION  ›","EXPEDITION STARTEN  ›"),start_run,not owned)
		primary.custom_minimum_size=Vector2(0,60)
		primary.add_theme_font_size_override("font_size",24)
		UI.style_button(primary,GOLD,true)
		if not owned:
			primary.text=tr_text("Unlock this set in Loadouts","Schalte dieses Set unter Start-Sets frei")
	else:
		var available: bool=not owned and progress.essence>=int(spec.cost) and (required.is_empty() or progress.unlocked.has(required))
		var caption: String=tr_text("Unlock selected set","Ausgewähltes Set freischalten")+"  •  "+str(spec.cost)
		if owned: caption=tr_text("Already unlocked","Bereits freigeschaltet")
		elif not required.is_empty() and not progress.unlocked.has(required): caption=tr_text("Requires ","Benötigt ")+Content.loadout_display_name(required,german)
		var unlock:=add_action(caption,func(): buy_set(inspected_set),not available)
		UI.style_button(unlock,GOLD,available)
		unlock.custom_minimum_size=Vector2(0,54)
		if owned: add_action(tr_text("Use in expedition  ›","In Expedition verwenden  ›"),func(): inspect_loadout(inspected_set); show_page("expedition"))
	update_preview()

func inspect_loadout(id: String) -> void:
	inspected_set=id
	if progress.unlocked.has(id) and page=="expedition":
		progress.selected=id
		if not visual_smoke: progress.save()
	show_page(page)

func start_run() -> void:
	if cinematic_playing or not progress.unlocked.has(inspected_set): return
	progress.selected=inspected_set
	if not visual_smoke: progress.save()
	cinematic_playing=true
	cinematic_clock=0.0
	cinematic_changed_scene=false
	get_viewport().gui_release_focus()
	backdrop.call("begin_cinematic",Content.SETS[inspected_set].character)
	audio.play("portal_charge")
	for button in action_buttons:
		button.disabled=true
	for button in nav_buttons.values():
		button.disabled=true
	var fade:=create_tween()
	fade.set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_IN_OUT)
	fade.tween_property(interface_frame,"modulate:a",0.0,0.52)

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
	if cinematic_playing: return
	if event.is_action_pressed("ui_cancel") and page != "home":
		show_page("home")
		get_viewport().set_input_as_handled()

func smoke_test() -> void:
	show_page("expedition")
	await get_tree().process_frame
	if footer_left.size.x<260 or footer_right.size.x<200 or footer_left.size.y>30 or footer_right.size.y>30:
		push_error("Menu footer layout regression")
		get_tree().quit(23)
		return
	if loadout_cards.size()!=8 or preview_sprite.texture==null:
		get_tree().quit(1)
		return
	show_page("progress")
	if tree.nodes.size()!=34 or tree_buy.disabled==false:
		get_tree().quit(2)
		return
	progress.boss_souls=1
	select_tree_rank("arcane",2)
	if not tree_buy.disabled:
		get_tree().quit(21)
		return
	select_tree_rank("arcane",1)
	buy_tree_rank()
	if progress.ranks.arcane!=1 or progress.boss_souls!=0:
		get_tree().quit(22)
		return
	progress=Progress.new()
	show_page("sets")
	if loadout_cards.size()!=8:
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
	# The selected loadout must drive the actual gate shot, not always the Warden.
	progress.unlocked.append("dealer_cards")
	inspected_set="dealer_cards"
	show_page("expedition")
	start_run()
	if not cinematic_playing or progress.selected!="dealer_cards" or backdrop.get("cinematic_character")!="dealer" or not backdrop.get("cinematic_active") or cinematic_fade==null:
		push_error("Descent selected-character intro regression")
		get_tree().quit(29)
		return
	print("Midnight Descent gate cinematic smoke PASS")
	print("Midnight Descent menu smoke PASS")
	print("Midnight Descent run smoke PASS")
	get_tree().quit(0)
