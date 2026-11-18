extends Control
## Dedicated first milestone view, backed exclusively by progression data.
## More main/side jobs can be appended to factory_progression.gd later.
const Menu = preload("res://factory_menu_theme.gd")
const Catalog = preload("res://item_catalog.gd")
const Progression = preload("res://factory_progression.gd")
const Icons = preload("res://factory_ui_icons.gd")
var model
var german := false
var title_label: Label
var description: Label
var job_label: Label
var progress_text: Label
var progress_bar: ProgressBar
var reward_label: Label
var next_label: Label
var energy_label: Label
var completed_label: Label
var side_card: PanelContainer
var side_heading: Label
var side_description: Label
var side_progress: Label
var side_reward: Label
var _elapsed := 0.0

func _ready() -> void:
	name = "FactoryJobsScreen"
	mouse_filter = Control.MOUSE_FILTER_STOP
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_build()
	visible = false

func _word(de: String, en: String) -> String:
	return de if german else en

func _label(parent: Node, value: String, font_size: int, color: Color) -> Label:
	var node := Label.new()
	node.text = value
	Menu.label(node, font_size, color)
	parent.add_child(node)
	return node

func _build() -> void:
	var fade := ColorRect.new()
	fade.name = "JobsBackdrop"
	fade.color = Color("#04121B", 0.91)
	fade.mouse_filter = Control.MOUSE_FILTER_STOP
	add_child(fade)
	fade.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	var center := CenterContainer.new()
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	center.mouse_filter = Control.MOUSE_FILTER_PASS
	add_child(center)
	var panel := PanelContainer.new()
	panel.name = "JobsMainPanel"
	panel.custom_minimum_size = Vector2(780, 575)
	panel.add_theme_stylebox_override("panel", Menu.panel("main"))
	center.add_child(panel)
	var col := VBoxContainer.new()
	col.add_theme_constant_override("separation", 17)
	panel.add_child(col)
	var stripe := ColorRect.new()
	stripe.color = Menu.CYAN
	stripe.custom_minimum_size.y = 3
	col.add_child(stripe)
	var heading := HBoxContainer.new()
	heading.add_theme_constant_override("separation", 12)
	col.add_child(heading)
	var labels := VBoxContainer.new()
	labels.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	labels.add_theme_constant_override("separation", 5)
	heading.add_child(labels)
	_label(labels, "MIDNIGHT / INDUSTRIES", 12, Menu.AMBER)
	title_label = _label(labels, "", 28, Menu.TEXT)
	var close := Button.new()
	close.name = "CloseJobs"
	close.text = "×"
	close.custom_minimum_size = Vector2(46, 44)
	Menu.style_button(close)
	close.add_theme_font_size_override("font_size", 23)
	close.pressed.connect(hide_jobs)
	heading.add_child(close)
	var overview := PanelContainer.new()
	overview.add_theme_stylebox_override("panel", Menu.panel("section"))
	col.add_child(overview)
	var section := VBoxContainer.new()
	section.add_theme_constant_override("separation", 10)
	overview.add_child(section)
	job_label = _label(section, "", 20, Menu.TEXT)
	description = _label(section, "", 15, Menu.MUTED)
	description.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	progress_text = _label(section, "", 13, Menu.CYAN)
	progress_bar = ProgressBar.new()
	progress_bar.show_percentage = false
	progress_bar.custom_minimum_size.y = 12
	progress_bar.min_value = 0
	progress_bar.max_value = 100
	progress_bar.add_theme_stylebox_override("background", Menu.slider_track())
	progress_bar.add_theme_stylebox_override("fill", Menu.slider_fill())
	section.add_child(progress_bar)
	completed_label = _label(section, "", 12, Menu.CYAN)
	var reward := PanelContainer.new()
	reward.add_theme_stylebox_override("panel", Menu.panel("section"))
	col.add_child(reward)
	var rewards := VBoxContainer.new()
	rewards.add_theme_constant_override("separation", 8)
	reward.add_child(rewards)
	_label(rewards, _word("BELOHNUNG · FREISCHALTUNGEN", "REWARDS · UNLOCKS"),
		13, Menu.AMBER)
	reward_label = _label(rewards, "", 15, Menu.TEXT)
	reward_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	# Side objectives remain visible beside the current main-stage mission,
	# with their own requirements and permanent slot rewards.
	side_card = PanelContainer.new()
	side_card.name = "OptionalFurnaceExpansion"
	side_card.add_theme_stylebox_override("panel", Menu.panel("section"))
	col.add_child(side_card)
	var side_col := VBoxContainer.new()
	side_col.add_theme_constant_override("separation", 7)
	side_card.add_child(side_col)
	side_heading = _label(side_col, "", 17, Menu.AMBER)
	side_description = _label(side_col, "", 13, Menu.MUTED)
	side_description.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	side_progress = _label(side_col, "", 13, Menu.CYAN)
	side_reward = _label(side_col, "", 13, Menu.TEXT)
	side_card.visible = false
	var electricity := PanelContainer.new()
	electricity.add_theme_stylebox_override("panel", Menu.panel("section"))
	col.add_child(electricity)
	var power_col := VBoxContainer.new()
	power_col.add_theme_constant_override("separation", 7)
	electricity.add_child(power_col)
	_label(power_col, _word("STROMVERSORGUNG", "POWER DISTRIBUTION"),
		13, Menu.AMBER)
	energy_label = _label(power_col, "", 14, Menu.TEXT)
	next_label = _label(col, "", 13, Menu.MUTED)
	next_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	var spacer := Control.new()
	spacer.size_flags_vertical = Control.SIZE_EXPAND_FILL
	col.add_child(spacer)
	_label(col, _word("J / ESC  SCHLIESSEN", "J / ESC  CLOSE"),
		12, Menu.MUTED)

func show_jobs() -> void:
	refresh()
	visible = true

func hide_jobs() -> void:
	visible = false

func refresh() -> void:
	if model == null or job_label == null:
		return
	title_label.text = _word("AUFTRÄGE & FORTSCHRITT", "JOBS & PROGRESSION")
	var entry: Dictionary = model.progression.current_job()
	var complete := false
	if entry.is_empty():
		for candidate in Progression.JOBS:
			if str(candidate.get("type", "")) == "main":
				entry = candidate
		complete = true
	else:
		complete = model.progression.is_complete(str(entry.id))
	if entry.is_empty():
		return
	var id: String = str(entry.id)
	var stage: int = int(entry.get("stage", 1))
	job_label.text = (_word("HAUPTAUFTRAG %03d · ", "MAIN JOB %03d · ") % stage) + \
		str(entry.get("de" if german else "en", ""))
	description.text = str(entry.get("description_de" if german else "description_en", ""))
	var parts: Array[String] = []
	var done := 0
	var total := 0
	for material in entry.get("delivery", {}):
		var required: int = int(entry.delivery[material])
		var received: int = model.progression.delivered_count(id, str(material))
		var name: String = Catalog.name_for(str(material), german)
		parts.append("%s: %d / %d" % [name, received, required])
		done += received
		total += required
	progress_text.text = "   ·   ".join(parts)
	progress_bar.value = 100.0 * float(done) / float(maxi(1, total))
	reward_label.text = str(entry.get("reward_de" if german else "reward_en", ""))
	completed_label.text = _word("ABGESCHLOSSEN · DAUERHAFT FREIGESCHALTET",
		"COMPLETED · PERMANENTLY UNLOCKED") if complete else _word(
		"Es zählen nur tatsächliche Anlieferungen im Depot.",
		"Only new physical depot deliveries count.")
	var optional: Dictionary = model.progression.job("furnace_expansion")
	var eligible: bool = model.progression.is_complete("first_iron_delivery")
	side_card.visible = eligible
	if eligible:
		var finished: bool = model.progression.is_complete("furnace_expansion")
		model._update_power()
		var built: int = int(model.counts().get("smelter", 0))
		var running: int = model.progression.running_furnaces(
			model.buildings, model.power)
		side_heading.text = _word("NEBENAUFTRAG · ", "SIDE JOB · ") + str(
			optional.get("de" if german else "en", ""))
		side_description.text = str(optional.get(
			"description_de" if german else "description_en", ""))
		side_progress.text = _word("Schmelzöfen in Betrieb: ", "Furnaces running: ") + \
			"%d / 3     ·     " % mini(running, 3) + \
			(_word("Gebaut: ", "Built: ") + "%d / 4" % mini(built, 4))
		side_reward.text = (_word("ABGESCHLOSSEN · ", "COMPLETED · ") if finished else
			_word("BELOHNUNG · ", "REWARD · ")) + str(optional.get(
			"reward_de" if german else "reward_en", ""))
	model._update_power()
	energy_label.text = _word("Stromslots: %d / %d     ·     Last / Erzeugung: %d / %d kW",
		"Power slots: %d / %d     ·     Demand / Supply: %d / %d kW") % [
		model.power.slot_used, model.power.slot_limit,
		model.power.total_demand, model.power.total_supply]
	next_label.text = _word("Weitere Aufträge und Maschinen folgen in späteren Stages.",
		"More jobs and machines will follow in later stages.")

func _process(delta: float) -> void:
	if not visible:
		return
	_elapsed += delta
	if _elapsed >= 0.25:
		_elapsed = 0.0
		refresh()
