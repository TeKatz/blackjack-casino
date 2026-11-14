extends Node
## Global two-state club cursor; mouse capture/hidden modes remain engine-owned.
const NORMAL: Texture2D = preload("res://art/casino-cursor-normal.svg")
const PRESSED: Texture2D = preload("res://art/casino-cursor-pressed.svg")
var _pressed := false

func _ready() -> void:
    _apply(false)

func _process(_delta: float) -> void:
    var down := Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT)
    if down != _pressed:
        _apply(down)

func _apply(down: bool) -> void:
    _pressed = down
    var texture := PRESSED if down else NORMAL
    Input.set_custom_mouse_cursor(texture, Input.CURSOR_ARROW, Vector2(4, 2))
    Input.set_custom_mouse_cursor(texture, Input.CURSOR_POINTING_HAND, Vector2(4, 2))
