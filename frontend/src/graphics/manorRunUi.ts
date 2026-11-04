import type { ManorRoom, ManorRun } from '../bridge/protocol';

export type GhostBand = 'revealed' | 'danger' | 'approaching' | 'distant' | 'uncertain';

export function ghostBand(clue: string): GhostBand {
  if (clue.startsWith('Candle reveals:')) return 'revealed';
  if (clue.includes('CLOSE (within two rooms)')) return 'danger';
  if (clue.includes('ghost is approaching')) return 'approaching';
  if (clue.includes('ghost is far away')) return 'distant';
  return 'uncertain';
}

export const ghostBandLabel: Record<GhostBand, string> = {
  revealed: 'CANDLE REVEAL',
  danger: 'GHOST NEARBY',
  approaching: 'APPROACHING',
  distant: 'DISTANT FOOTSTEPS',
  uncertain: 'UNSEEN PRESENCE',
};

/** Legal routes come exclusively from C#'s Available flag. */
export function availableManorRooms(run: ManorRun): ManorRoom[] {
  return run.rooms.filter(room => room.available && room.id !== run.playerRoom)
    .sort((a, b) => a.id - b.id);
}

/** Pixel math for scrolling the *fixed artwork canvas*; never modifies game navigation. */
export function mapScrollLeft(x: number, viewportWidth: number, contentWidth = 1000): number {
  if (![x, viewportWidth, contentWidth].every(Number.isFinite) || contentWidth <= 0) return 0;
  return Math.max(0, Math.min(Math.max(0, contentWidth - viewportWidth), x - viewportWidth / 2));
}
