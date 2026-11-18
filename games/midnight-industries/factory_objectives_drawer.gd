extends Control
## Collapsible nonintrusive objectives rail. Future objectives can be supplied
## through data without redesigning the surface or breaking saved view state.
const MenuTheme = preload("res://factory_menu_theme.gd")
const Icons = preload("res://factory_ui_icons.gd")
const ItemCatalog = preload("res://item_catalog.gd")

var german := false
var reduced_motion := false
var expanded := false
var _tween: Tween
var toggle_button: Button
var heading: Label
var body: Label
var phase_line: Label
const OPEN_X := 11.0
const CLOSED_X := -292.0
const WIDTH := 324.0

func _ready() -> void:
	name = "ObjectivesDrawer"
	size = Vector2(WIDTH, 252)
	mouse_filter = Control.MOUSE_FILTER_PASS
	_build()
	_update_slide(false)

func _word(de: String, en: String) -> String:
	return de if german else en

func _build() -> void:
	var frame := PanelContainer.new()
	frame.name = "ObjectivesPanel"
	frame.add_theme_stylebox_override("panel", MenuTheme.panel("main"))
	frame.position = Vector2(0, 0)
	frame.size = Vector2(306, 238)
	add_child(frame)
	var column := VBoxContainer.new()
	column.add_theme_constant_override("separation", 12)
	frame.add_child(column)
	var stripe := ColorRect.new()
	stripe.color = MenuTheme.CYAN
	stripe.custom_minimum_size.y = 3
	stripe.mouse_filter = Control.MOUSE_FILTER_IGNORE
	column.add_child(stripe)
	heading = Label.new()
	MenuTheme.label(heading, 17)
	heading.text = _word("ZIELE", "OBJECTIVES")
	column.add_child(heading)
	phase_line = Label.new()
	MenuTheme.label(phase_line, 13, MenuTheme.AMBER)
	phase_line.text = _word("CASINO · PHASE 01/12", "CASINO · PHASE 01/12")
	column.add_child(phase_line)
	var divider := ColorRect.new()
	divider.color = MenuTheme.BORDER
	divider.custom_minimum_size.y = 1
	divider.mouse_filter = Control.MOUSE_FILTER_IGNORE
	column.add_child(divider)
	body = Label.new()
	MenuTheme.label(body, 13, MenuTheme.MUTED)
	body.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	body.text = _word("Ziele und Aufgaben werden mit den kommenden Casino-Phasen ergänzt.",
		"Goals and assignments will appear when the casino phases are designed.")
	column.add_child(body)
	toggle_button = Button.new()
	toggle_button.name = "ObjectivesToggle"
	toggle_button.position = Vector2(295, 85)
	toggle_button.custom_minimum_size = Vector2(31, 59)
	toggle_button.size = Vector2(31, 59)
	MenuTheme.style_button(toggle_button, false, false)
	toggle_button.icon = Icons.icon("arrow")
	toggle_button.tooltip_text = _word("Ziele ein- / ausklappen", "Expand / collapse objectives")
	toggle_button.pressed.connect(toggle)
	add_child(toggle_button)

func _update_slide(animated: bool) -> void:
	if _tween != null and _tween.is_running():
		_tween.kill()
	var target: float = OPEN_X if expanded else CLOSED_X
	if not animated or reduced_motion:
		position.x = target
	else:
		_tween = create_tween()
		_tween.tween_property(self, "position:x", target, 0.24).set_trans(
			Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	toggle_button.tooltip_text = _word(
		"Ziele einklappen" if expanded else "Ziele ausklappen",
		"Collapse objectives" if expanded else "Expand objectives")
	toggle_button.text = "‹" if expanded else "›"
	toggle_button.icon = null

func toggle() -> void:
	expanded = not expanded
	_update_slide(true)

func set_expanded(value: bool, animated: bool = false) -> void:
	expanded = value
	_update_slide(animated)

func set_progression(factory) -> void:
	if factory == null:
		return
	var job: Dictionary = factory.progression.current_job()
	if job.is_empty():
		heading.text = _word("HAUPTAUFTRAG ERLEDIGT", "MAIN JOB COMPLETED")
		body.text = _word("Alle aktuellen Hauptaufträge abgeschlossen.",
			"All current main jobs completed.")
	else:
		heading.text = _word("AKTUELLER HAUPTAUFTRAG", "CURRENT MAIN JOB")
		body.text = str(job.get("description_de" if german else "description_en", ""))
		for item in job.get("delivery", {}):
			var current: int = factory.progression.delivered_count(str(job.id), str(item))
			body.text += "\n" + ItemCatalog.name_for(str(item), german) + (
				": %d / %d" % [current, int(job.delivery[item])])
	if factory.progression.is_complete("first_iron_delivery") and not \
		factory.progression.is_complete("furnace_expansion"):
		var active: int = factory.progression.running_furnaces(
			factory.buildings, factory.power)
		var built: int = int(factory.counts().get("smelter", 0))
		body.text += "\n" + _word("Nebenjob: ", "Side job: ") + (
			"%d/3 + %d/4" % [mini(3, active), mini(4, built)])
	factory._update_power()
	body.text += "\n" + _word("Stromslots: ", "Power slots: ") + \
		"%d/%d" % [factory.power.slot_used, factory.power.slot_limit]
	body.text += "\n" + _word("Stromleistung: ", "Power supply: ") + \
		"%d/%d kW" % [factory.power.total_powered, factory.power.total_supply]

func set_phase(phase: int) -> void:
	phase_line.text = _word("CASINO · PHASE ", "CASINO · PHASE ") + "%02d/12" % phase

func is_over(point: Vector2) -> bool:
	return get_global_rect().has_point(point)
