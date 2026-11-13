export type GhostBand = 'revealed' | 'danger' | 'approaching' | 'distant' | 'uncertain';

export function ghostBand(clue: string): GhostBand {
  if (clue.startsWith('Candle reveals:')) return 'revealed';
  if (clue.includes('CLOSE (within two rooms)')) return 'danger';
  if (clue.includes('ghost is approaching')) return 'approaching';
  if (clue.includes('ghost is far away')) return 'distant';
  return 'uncertain';
}

