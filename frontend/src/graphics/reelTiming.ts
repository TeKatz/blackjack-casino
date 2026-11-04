/** Visual-only timeline. Results and money always come from C#. */
export type ReelSpeed = 'normal' | 'fast' | 'turbo';
const TIMES: Record<ReelSpeed, readonly number[]> = {
  normal: [750, 930, 1110, 1290, 1470],
  fast: [320, 415, 510, 605, 700],
  turbo: [145, 185, 225, 265, 305],
};
export function reelStops(speed: ReelSpeed, reducedMotion: boolean): number[] {
  return reducedMotion ? [0, 0, 0, 0, 0] : [...TIMES[speed]];
}
/** Deterministic cycling during animation is cosmetic, never a game spin. */
export function visualIndex(step: number, reel: number, row: number): number {
  return ((step + 5 * reel + 7 * row) % 14 + 14) % 14;
}
