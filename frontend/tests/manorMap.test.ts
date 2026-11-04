import { describe, expect, it } from 'vitest';
import { roomPosition } from '../src/graphics/ManorRunMap';

describe('Escape the Manor floorplan geometry', () => {
  it('lays out all 8 columns and 3 rows without wrapping or overlap', () => {
    const positions = new Set<string>();
    for (let x = 0; x < 8; x++) {
      for (let y = 1; y <= 3; y++) {
        const point = roomPosition({ x, y });
        expect(point.x).toBeGreaterThanOrEqual(70);
        expect(point.x).toBeLessThanOrEqual(931);
        expect(point.y).toBeGreaterThanOrEqual(70);
        expect(point.y).toBeLessThanOrEqual(338);
        positions.add(`${point.x}:${point.y}`);
      }
    }
    expect(positions.size).toBe(24);
  });
});
