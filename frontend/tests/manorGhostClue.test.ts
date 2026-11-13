import { describe, expect, it } from 'vitest';
import { ghostBand } from '../src/graphics/manorGhostClue';

describe('3D Manor C# ghost clue presentation', () => {
  it('does not invent a ghost coordinate for proximity clues', () => {
    expect(ghostBand('Distant footsteps echo through the Manor. The ghost is far away.'))
      .toBe('distant');
    expect(ghostBand('A door slams to the west. The ghost is approaching.'))
      .toBe('approaching');
    expect(ghostBand('Cold air creeps from the east. The ghost is CLOSE (within two rooms).'))
      .toBe('danger');
    expect(ghostBand('')).toBe('uncertain');
  });
  it('marks C# candle reveals distinctly from anonymous proximity', () => {
    expect(ghostBand('Candle reveals: Crypt.')).toBe('revealed');
  });
});
