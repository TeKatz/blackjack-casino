extends Node
## Build matching native cursor textures directly from the source vector art.
## Godot can parse this autoload before imported SVG resources become available.
const NORMAL_SVG := """<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32">
<defs><linearGradient id="metal" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#fff1be"/><stop offset=".36" stop-color="#ebc477"/><stop offset=".78" stop-color="#b47c32"/><stop offset="1" stop-color="#f8d89b"/></linearGradient></defs>
<path d="M3.5 2.5 4.6 25.2 10.6 19.7 15.4 29 20.2 26.5 15.4 17.1 25.5 14.4Z" fill="#020f0f" stroke="#02090a" stroke-width="2" stroke-linejoin="round"/>
<path d="M3.5 2.5 4.6 25.2 10.6 19.7 15.4 29 20.2 26.5 15.4 17.1 25.5 14.4Z" fill="url(#metal)" stroke="#d1a253" stroke-width="1.25" stroke-linejoin="round"/>
<path d="M6.2 7.8 7 19.4 11.4 15.6 17.5 14.2Z" fill="#0b3b31" stroke="#073027" stroke-width=".6" stroke-linejoin="round"/>
<path d="m5.2 4.4 1.1 10.2" stroke="#fff6da" stroke-width="1.15" stroke-linecap="round" opacity=".9"/>
<path d="m11.8 20.5 3.5 6.4" stroke="#fff0c1" stroke-width=".85" stroke-linecap="round" opacity=".7"/>
</svg>"""
const PRESSED_SVG := """<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32">
<defs><linearGradient id="metal" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#fffbe1"/><stop offset=".4" stop-color="#ffe3a0"/><stop offset=".8" stop-color="#d59d4c"/><stop offset="1" stop-color="#ffefb9"/></linearGradient></defs>
<g transform="translate(.75 .65)">
<path d="M3.5 2.5 4.6 25.2 10.6 19.7 15.4 29 20.2 26.5 15.4 17.1 25.5 14.4Z" fill="#06140f" stroke="#050c0b" stroke-width="2" stroke-linejoin="round"/>
<path d="M3.5 2.5 4.6 25.2 10.6 19.7 15.4 29 20.2 26.5 15.4 17.1 25.5 14.4Z" fill="url(#metal)" stroke="#ffebbd" stroke-width="1.4" stroke-linejoin="round"/>
<path d="M6.2 7.8 7 19.4 11.4 15.6 17.5 14.2Z" fill="#1b6855" stroke="#0e433a" stroke-width=".7" stroke-linejoin="round"/>
<path d="m5.2 4.4 1.1 10.2" stroke="#fff9e6" stroke-width="1.4" stroke-linecap="round"/>
<path d="m11.8 20.5 3.5 6.4" stroke="#fff6d7" stroke-width="1" stroke-linecap="round"/></g></svg>"""
var _normal_texture: Texture2D
var _pressed_texture: Texture2D
var _pressed := false

func _ready() -> void:
    _normal_texture = _make_texture(NORMAL_SVG)
    _pressed_texture = _make_texture(PRESSED_SVG)
    _apply(false)

func _make_texture(source: String) -> Texture2D:
    var picture := Image.new()
    if picture.load_svg_from_string(source, 1.0) != OK:
        return null
    return ImageTexture.create_from_image(picture)

func _process(_delta: float) -> void:
    var down := Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT)
    if down != _pressed:
        _apply(down)

func _apply(down: bool) -> void:
    _pressed = down
    var texture := _pressed_texture if down else _normal_texture
    if texture == null:
        return
    Input.set_custom_mouse_cursor(texture, Input.CURSOR_ARROW, Vector2(4, 2))
    Input.set_custom_mouse_cursor(texture, Input.CURSOR_POINTING_HAND, Vector2(4, 2))
