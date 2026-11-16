/** Presentation-only parameters. No effect on the C# ticket or payout. */
export const SCRATCH_BRUSH_WIDTH = 25;
export const SCRATCH_REVEAL_COVERAGE = 0.50;
export const SCRATCH_SAMPLE_STEP = 4;

export type ScratchPoint = {x:number;y:number};
export type ScratchRect = {left:number;right:number;top:number;bottom:number};

/** Broad-phase test for one continuous stroke across arbitrary adjacent cells.
    The canvas itself clips the line, so a bounding-box false positive is harmless. */
export function strokeOverlapsRect(from:ScratchPoint,to:ScratchPoint,rect:ScratchRect,margin=0):boolean {
  return Number.isFinite(from.x)&&Number.isFinite(from.y)&&
    Number.isFinite(to.x)&&Number.isFinite(to.y)&&
    Math.max(from.x,to.x)>=rect.left-margin&&Math.min(from.x,to.x)<=rect.right+margin&&
    Math.max(from.y,to.y)>=rect.top-margin&&Math.min(from.y,to.y)<=rect.bottom+margin;
}

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

// The scratch mask and winning highlight must belong to the displayed receipt.
// A new purchase can arrive before the React effect resets its local reveal state.
export function revealedIndexesForTicket(
  ticketId:string|null, collected:boolean,
  session:{ticketId:string|null;indexes:number[]},
): number[] {
  if(!ticketId)return [];
  if(collected)return [0,1,2,3,4,5,6,7,8];
  return session.ticketId===ticketId?session.indexes:[];
}
