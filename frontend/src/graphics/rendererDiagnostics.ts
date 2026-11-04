/** Pixi ticker sampling: useful for manual QA, not a GPU frame-time benchmark. */
export type RendererDiagnostics = {
  frames: number;
  fpsEstimate: number;
  averageMs: number;
  p95Ms: number;
  overBudget: number;
  width: number;
  height: number;
  pixelRatio: number;
};

export function boundedPixelRatio(value: number): number {
  if (!Number.isFinite(value) || value <= 0) return 1;
  return Math.max(1, Math.min(2, value));
}

export function calculateCanvasLayout(width: number, height: number, ratio: number) {
  const w = Number.isFinite(width) ? Math.max(1, width) : 1;
  const h = Number.isFinite(height) ? Math.max(1, height) : 1;
  const scale = Math.min(w / 960, h / 500);
  return {
    width: w, height: h,
    pixelRatio: boundedPixelRatio(ratio),
    scale,
    x: (w - 960 * scale) / 2,
    y: (h - 500 * scale) / 2,
  };
}

/** Ignores tab-suspension gaps and invalid measurements, reports observed ticker intervals. */
export function summarizeFrameTimes(samples: readonly number[],
  width: number, height: number, pixelRatio: number): RendererDiagnostics | null {
  const valid = samples.filter(value => Number.isFinite(value) && value > 0 && value <= 250);
  if (valid.length < 20) return null;
  const sorted = [...valid].sort((a, b) => a - b);
  const average = valid.reduce((sum, v) => sum + v, 0) / valid.length;
  return {
    frames: valid.length,
    fpsEstimate: Math.round(1000 / average),
    averageMs: Math.round(average * 10) / 10,
    p95Ms: Math.round(sorted[Math.ceil(sorted.length * .95) - 1] * 10) / 10,
    overBudget: valid.filter(value => value > 16.7).length,
    width: Math.round(width),
    height: Math.round(height),
    pixelRatio: boundedPixelRatio(pixelRatio),
  };
}
