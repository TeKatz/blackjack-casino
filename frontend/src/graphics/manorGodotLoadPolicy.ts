/**
 * Do NOT instantiate the Godot iframe during ordinary slot startup.
 * The single-threaded WebAssembly/WebGL bootstrap can block the WebView2 UI
 * for several seconds. First explicit use loads it once; later visits reuse it.
 */
export function shouldMountManorGodot(active: boolean, hasBeenOpened: boolean): boolean {
  return active || hasBeenOpened;
}
