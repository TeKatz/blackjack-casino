extends Control
## Modal global construction inventory. No capacity limits are invented yet:
## only material actually arriving at depots is spendable.
const Economy = preload("res://factory_economy.gd")
const UiSkin = preload("res://factory_skin.gd")
const TEXT := Color("#EAF4F5")
const MUTED := Color("#9DB5C0")
const AMBER := Color("#F5BC59")
const CYAN := Color("#19D5F2")
const METALS := [
	["iron_ingot", "EISENBARREN", "IRON INGOTS", Color("#8DAAB8")],
	["copper_ingot", "KUPFERBARREN", "COPPER INGOTS", Color("#D08A53")],
	["gold_ingot", "GOLDBARREN", "GOLD INGOTS", Color("#EAC358")],
	["iron_ore", "EISENERZ", "IRON ORE", Color("#76909D")],
	["copper_ore", "KUPFERERZ", "COPPER ORE", Color("#B46D44")],
	["gold_ore", "GOLDERZ", "GOLD ORE", Color("#C89F39")]
]
var model
var german := true
var _close_rect := Rect2()

func _ready() -> void:
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_STOP
	visible = false

func open_inventory() -> void:
	visible = true
	queue_redraw()

func close_inventory() -> void:
	visible = false

func _process(_delta: float) -> void:
	if visible:
		queue_redraw()

func _word(de: String, en: String) -> String:
	return de if german else en

func _text(font: Font, p: Vector2, content: String, size_px: int,
	tint: Color = TEXT) -> void:
	draw_string(font, p, content, HORIZONTAL_ALIGNMENT_LEFT, -1, size_px, tint)

func _draw() -> void:
	if model == null:
		return
	var font: Font = UiSkin.font()
	draw_rect(Rect2(Vector2.ZERO, size), Color("#061118", 0.97))
	var w := minf(1040.0, size.x - 60.0)
	var h := minf(690.0, size.y - 90.0)
	var base := Vector2((size.x - w) * 0.5, (size.y - h) * 0.5)
	var frame := Rect2(base, Vector2(w, h))
	draw_rect(frame, Color("#152630"))
	draw_rect(frame, Color("#516672"), false, 2.0)
	draw_rect(Rect2(base + Vector2(12, 12), Vector2(w - 24.0, 70.0)),
		Color("#21343E"))
	_text(font, base + Vector2(35, 52), "MIDNIGHT INDUSTRIES   /   " +
		_word("GLOBALES BAULAGER", "GLOBAL BUILD INVENTORY"), 23, TEXT)
	_text(font, base + Vector2(36, 73), _word(
		"Lieferungen an Depots finanzieren neue Maschinen.",
		"Materials delivered to depots fund new machinery."), 12, MUTED)
	_close_rect = Rect2(base + Vector2(w - 58, 25), Vector2(33, 31))
	draw_rect(_close_rect, Color("#3B5260"))
	_text(font, _close_rect.position + Vector2(11, 23), "X", 16, TEXT)

	var left_width := w * 0.53
	var separator_x := base.x + left_width
	draw_line(Vector2(separator_x, base.y + 100),
		Vector2(separator_x, base.y + h - 45), Color("#385260"), 1.0)
	_text(font, base + Vector2(35, 124), _word("MATERIALIEN", "MATERIALS"),
		15, AMBER)
	var row_h := minf(72.0, (h - 208.0) / 6.0)
	for i in range(METALS.size()):
		var spec: Array = METALS[i]
		var material: String = str(spec[0])
		var y: float = base.y + 140.0 + i * row_h
		draw_rect(Rect2(base.x + 28, y, left_width - 51, row_h - 9),
			Color("#1D343F"))
		draw_rect(Rect2(base.x + 44, y + 14, 13, 21), spec[3])
		_text(font, Vector2(base.x + 68, y + 30),
			str(spec[1] if german else spec[2]), 15, TEXT)
		_text(font, Vector2(base.x + left_width - 146, y + 30),
			"%d" % model.economy.available(material), 17, CYAN)

	var labels := {
		"miner": ["ABBAUMASCHINE", "MINING DRILL"],
		"belt": ["FÖRDERBAND", "CONVEYOR BELT"],
		"smelter": ["SCHMELZOFEN", "SMELTING FURNACE"],
		"storage": ["WAREHOUSE", "WAREHOUSE"],
		"depot": ["DEPOT", "DEPOT"]
	}
	var costs_start := separator_x + 28.0
	_text(font, Vector2(costs_start, base.y + 124),
		_word("BAUKOSTEN", "BUILD COSTS"), 15, AMBER)
	for i in range(Economy.COSTS.size()):
		var machine: String = str(Economy.COSTS.keys()[i])
		var y := base.y + 160.0 + i * 79.0
		_text(font, Vector2(costs_start, y),
			str(labels[machine][0 if german else 1]), 14, TEXT)
		var pieces: Array[String] = []
		for material in Economy.COSTS[machine]:
			var need: int = int(Economy.COSTS[machine][material])
			var have: int = model.economy.available(material)
			var short := "Fe" if material == "iron_ingot" else (
				"Cu" if material == "copper_ingot" else "Au")
			pieces.append("%s %d/%d" % [short, mini(have, need), need])
		var can_buy: bool = model.economy.has_funds(machine)
		var caption := "  ·  ".join(pieces)
		if model.economy.has_free_kit(machine):
			caption = _word("UMZUGS-KIT · GRATIS", "RELOCATION KIT · FREE")
		elif machine == "depot" and model.economy.free_depot_available:
			caption = _word("1x GRATIS DEPOT", "1x FREE DEPOT")
		_text(font, Vector2(costs_start, y + 24),
			caption, 12, CYAN if can_buy else Color("#F69A72"))
	var num_depots := int(model.counts().get("depot", 0))
	_text(font, Vector2(costs_start, base.y + h - 96),
		_word("VERBUNDENE DEPOTS: %d", "CONNECTED DEPOTS: %d") % num_depots,
		13, AMBER)
	if model.economy.free_depot_available:
		_text(font, Vector2(costs_start, base.y + h - 73),
			_word("1 KOSTENLOSES DEPOT VERFÜGBAR", "1 FREE DEPOT AVAILABLE"),
			12, CYAN)
	_text(font, base + Vector2(37, h - 25),
		_word("I / ESC SCHLIESSEN   ·   KEINE LAGERGRENZE",
			"I / ESC CLOSE   ·   NO CAPACITY LIMIT"), 12, MUTED)

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed \
		and event.button_index == MOUSE_BUTTON_LEFT:
		if _close_rect.has_point(event.position):
			close_inventory()
		accept_event()
