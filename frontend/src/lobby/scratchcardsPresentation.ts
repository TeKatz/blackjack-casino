/** Presentation-only parameters. No effect on the C# ticket or payout. */
export const SCRATCH_BRUSH_WIDTH = 18;
export const SCRATCH_REVEAL_COVERAGE = 0.63;
export const SCRATCH_SAMPLE_STEP = 4;

/** Coverage, not pointer distance: repeatedly scrubbing one line cannot reveal the cell. */
export function clearedFraction(
  rgba: Uint8ClampedArray, width: number, height: number, step = SCRATCH_SAMPLE_STEP,
): number {
  if (!Number.isInteger(width) || !Number.isInteger(height) ||
      width <= 0 || height <= 0 || !Number.isInteger(step) || step <= 0 ||
      rgba.length < width * height * 4) return 0;
  let cleared = 0, samples = 0;
  for (let y = Math.floor(step / 2); y < height; y += step) {
    for (let x = Math.floor(step / 2); x < width; x += step) {
      samples++;
      if (rgba[(y * width + x) * 4 + 3] < 80) cleared++;
    }
  }
  return samples ? cleared / samples : 0;
}

/** Only the three winning cells glow; a loss never highlights decorative pairs. */
export function winningCellIndexes(symbols: readonly number[], prize: number): number[] {
  if (prize <= 0 || symbols.length !== 9) return [];
  for (let symbol = 0; symbol < 5; symbol++) {
    const indexes = symbols.flatMap((value, index) => value === symbol ? [index] : []);
    if (indexes.length === 3) return indexes;
  }
  return [];
}
