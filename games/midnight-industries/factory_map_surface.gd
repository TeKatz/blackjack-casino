extends Control
## Clipped, interactive map viewport. Map state and save schema stay in the
## parent atlas, while pan/zoom can no longer draw across the surrounding HUD.
var atlas: Control

func _ready() -> void:
	clip_contents = true
	mouse_filter = Control.MOUSE_FILTER_STOP
	texture_filter = CanvasItem.TEXTURE_FILTER_LINEAR

func _draw() -> void:
	if atlas != null:
		atlas._draw_surface(self)

func _gui_input(event: InputEvent) -> void:
	if atlas != null:
		atlas._surface_input(event)
