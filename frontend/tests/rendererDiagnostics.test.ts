import { describe, expect, it } from 'vitest';
import { boundedPixelRatio, calculateCanvasLayout, summarizeFrameTimes } from '../src/graphics/rendererDiagnostics';

describe('Manor DPI and measured ticker diagnostics', () => {
  it('preserves the machine aspect ratio and centers letterboxing for arbitrary host sizes', () => {
    expect(calculateCanvasLayout(960, 500, 1.5)).toMatchObject({
      scale: 1, x: 0, y: 0, pixelRatio: 1.5,
    });
    expect(calculateCanvasLayout(480, 250, 2)).toMatchObject({
      scale: .5, x: 0, y: 0, pixelRatio: 2,
    });
    expect(calculateCanvasLayout(960, 750, 2)).toMatchObject({
      scale: 1, x: 0, y: 125,
    });
    expect(calculateCanvasLayout(1200, 500, 1)).toMatchObject({
      scale: 1, x: 120, y: 0,
    });
    expect(boundedPixelRatio(1.25)).toBe(1.25);
    expect(boundedPixelRatio(1.5)).toBe(1.5);
    expect(boundedPixelRatio(2.5)).toBe(2);
    expect(boundedPixelRatio(0)).toBe(1);
    expect(boundedPixelRatio(NaN)).toBe(1);
  });

  it('reports observed intervals, not an invented 60 FPS guarantee', () => {
    const values = Array.from({ length: 100 }, (_, i) => i < 95 ? 16 : 34);
    const out = summarizeFrameTimes(values, 960, 500, 1.25);
    expect(out).not.toBeNull();
    expect(out?.frames).toBe(100);
    expect(out?.overBudget).toBe(5);
    expect(out?.p95Ms).toBe(16);
    expect(out?.fpsEstimate).toBe(59);
    expect(out?.pixelRatio).toBe(1.25);
  });

  it('rejects low-quality samples and sleeping-tab gaps', () => {
    expect(summarizeFrameTimes([], 960, 500, 1)).toBeNull();
    const out = summarizeFrameTimes([...Array(20).fill(20), 1000, Infinity], 500, 300, 3);
    expect(out).not.toBeNull();
    expect(out?.frames).toBe(20);
    expect(out?.overBudget).toBe(20);
    expect(out?.pixelRatio).toBe(2);
  });
});
