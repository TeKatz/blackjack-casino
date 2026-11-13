/**
 * A hidden Godot iframe still owns a WebGL context, GPU textures and
 * a WebAssembly render loop, even when Engine.max_fps is reduced to 8.
 * Mount only for a currently displayed 3D bonus. Unmount immediately
 * on practice exit / acknowledged payout, never keep a warm iframe
 * behind the PixiJS reels. Cached web assets may still reduce future loads.
 */
export function shouldMountManorGodot(active: boolean): boolean {
  return active;
}
