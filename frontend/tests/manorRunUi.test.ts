import { describe, expect, it } from 'vitest';
import type { ManorRun } from '../src/bridge/protocol';
import { availableManorRooms, ghostBand, ghostBandLabel, mapScrollLeft } from '../src/graphics/manorRunUi';

const run: ManorRun = {
  status: 'Hunting', message: 'Enter', ghostClue: 'Distant footsteps echo through the Manor. The ghost is far away.',
  playerRoom: 0, turn: 0, exitTarget: 250, current: '5×', banked: '5×',
  hasKey: false, hasCandle: false, hasCharm: false, passages: [],
  rooms: [
    { id: 7, name: 'Ballroom', x: 1, y: 3, area: 'Ballroom', visited: false, available: true, locked: false },
    { id: 0, name: 'Entrance Hall', x: 0, y: 2, area: 'Entrance', visited: true, available: false, locked: false },
    { id: 1, name: 'Servants Corridor', x: 1, y: 2, area: 'Corridor', visited: false, available: true, locked: false },
    { id: 11, name: 'Forbidden Wing', x: 4, y: 1, area: 'Crypt', visited: false, available: false, locked: true },
  ],
};

describe('Manor navigation affordances preserve C# authority', () => {
  it('only offers authorized moves and never infers a path through locked rooms', () => {
    expect(availableManorRooms(run).map(room => room.id)).toEqual([1, 7]);
    expect(availableManorRooms({ ...run, rooms: run.rooms.map(r => ({ ...r, available: false })) })).toEqual([]);
    expect(availableManorRooms({ ...run, rooms: [
      { ...run.rooms[1], available: true },
    ] })).toEqual([]);
  });
  it('classifies only the C# clue string and does not derive a hidden ghost position', () => {
    expect(ghostBand(run.ghostClue)).toBe('distant');
    expect(ghostBand('A door slams to the west. The ghost is approaching.')).toBe('approaching');
    expect(ghostBand('Cold air creeps from the south. The ghost is CLOSE (within two rooms).')).toBe('danger');
    expect(ghostBand('Candle reveals: Crypt.')).toBe('revealed');
    expect(ghostBand('')).toBe('uncertain');
    expect(ghostBandLabel.danger).toBe('GHOST NEARBY');
  });
  it('centers an active room in the viewport without scrolling outside the map', () => {
    expect(mapScrollLeft(70, 460)).toBe(0);
    expect(mapScrollLeft(439, 460)).toBe(209);
    expect(mapScrollLeft(931, 460)).toBe(540);
    expect(mapScrollLeft(400, 1200)).toBe(0);
    expect(mapScrollLeft(400, 0)).toBe(400);
    expect(mapScrollLeft(NaN, 460)).toBe(0);
  });
});
