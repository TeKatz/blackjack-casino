import { describe, expect, it } from 'vitest';
import { reelStops, visualIndex } from '../src/graphics/reelTiming';
describe('Manor visual-only reel timeline', () => {
  it('stops the five reels in increasing order for every speed', () => {
    for (const speed of ['normal', 'fast', 'turbo'] as const) {
      const stops = reelStops(speed, false);
      expect(stops).toHaveLength(5);
      expect(stops.every((ms, i) => i === 0 || ms > stops[i - 1])).toBe(true);
    }
    expect(reelStops('normal', false)[4]).toBeGreaterThan(reelStops('fast', false)[4]);
    expect(reelStops('fast', false)[4]).toBeGreaterThan(reelStops('turbo', false)[4]);
  });
  it('does not delay presentation when reduced motion is selected', () => {
    for (const speed of ['normal', 'fast', 'turbo'] as const)
      expect(reelStops(speed, true)).toEqual([0, 0, 0, 0, 0]);
  });
  it('cycles only through the fourteen existing symbol names', () => {
    for (let n = -100; n <= 100; n++) {
      const result = visualIndex(n, 4, 2);
      expect(Number.isInteger(result)).toBe(true);
      expect(result).toBeGreaterThanOrEqual(0);
      expect(result).toBeLessThan(14);
    }
  });
});
