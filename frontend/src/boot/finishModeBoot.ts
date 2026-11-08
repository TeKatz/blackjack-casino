/**
 * Keep the document-level splash over the WebView until React has committed
 * an authoritative first snapshot (and Pixi has initialized for Manor).
 * No animation frame is required: hidden lobby WebViews may suspend rAF.
 */
export function finishModeBoot(): void {
  const splash = document.getElementById('mode-boot');
  if (!splash || splash.dataset.state === 'ready') return;
  splash.dataset.state = 'ready';
  window.setTimeout(() => splash.remove(), 280);
}
