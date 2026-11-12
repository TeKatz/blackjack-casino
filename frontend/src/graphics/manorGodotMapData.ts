import type { ManorPassage, ManorRoom, ManorRun } from '../bridge/protocol';

/** 3D HUD projection of the native Manor graph. Never computes payouts or ghost movement. */
export type ManorMapSnapshot = Pick<ManorRun,
  'rooms' | 'passages' | 'playerRoom' | 'hasKey' | 'ghostClue'>;

const DEMO_LAYOUT: Array<Pick<ManorRoom, 'id' | 'name' | 'x' | 'y' | 'area'>> = [
  { id: 0, name: 'Entrance Hall', x: 0, y: 2, area: 'Entrance' },
  { id: 1, name: "Servants' Corridor", x: 1, y: 2, area: 'Corridor' },
  { id: 2, name: 'Candle Landing', x: 2, y: 2, area: 'Corridor' },
  { id: 3, name: 'Grand Staircase', x: 3, y: 2, area: 'Entrance' },
  { id: 4, name: 'Library', x: 1, y: 1, area: 'Library' },
  { id: 5, name: 'Reading Room', x: 2, y: 1, area: 'Library' },
  { id: 6, name: 'Study', x: 3, y: 1, area: 'Library' },
  { id: 7, name: 'Ballroom', x: 1, y: 3, area: 'Ballroom' },
  { id: 8, name: 'Music Room', x: 2, y: 3, area: 'Ballroom' },
  { id: 9, name: 'Winter Garden', x: 3, y: 3, area: 'Ballroom' },
  { id: 10, name: 'Attic', x: 4, y: 2, area: 'Attic' },
  { id: 11, name: 'Forbidden Wing', x: 4, y: 1, area: 'Crypt' },
  { id: 12, name: 'Crypt', x: 4, y: 3, area: 'Crypt' },
  { id: 13, name: 'Moonlit Landing', x: 5, y: 2, area: 'Attic' },
  { id: 14, name: 'Secret Passage', x: 5, y: 3, area: 'Crypt' },
  { id: 15, name: 'Courtyard', x: 6, y: 2, area: 'Entrance' },
  { id: 16, name: 'Moonlit Gate', x: 7, y: 2, area: 'Entrance' },
];

const DEMO_EDGE_PAIRS: Array<[number, number]> = [
  [0, 1], [0, 4], [0, 7], [1, 2], [1, 4], [1, 7],
  [4, 5], [7, 8], [2, 3], [2, 5], [2, 8], [5, 6], [8, 9],
  [3, 10], [6, 11], [9, 12], [10, 13], [11, 13], [12, 14],
  [13, 15], [14, 15], [15, 16], [14, 16],
];

export const DEMO_PASSAGES: ManorPassage[] = DEMO_EDGE_PAIRS.map(([from, to]) => ({
  from, to, requiresKey: (from === 6 && to === 11) || (from === 14 && to === 16),
}));

/** Only the settings preview uses local graph data; live paths come from C#. */
export function makeDemoMap(roomId: number, visited: readonly number[] = [0]): ManorMapSnapshot {
  const current = DEMO_LAYOUT.some(room => room.id === roomId) ? roomId : 0;
  const adjacent = DEMO_PASSAGES
    .filter(edge => !edge.requiresKey && (edge.from === current || edge.to === current))
    .map(edge => edge.from === current ? edge.to : edge.from);
  return {
    rooms: DEMO_LAYOUT.map(room => ({
      ...room, visited: visited.includes(room.id),
      available: adjacent.includes(room.id),
      locked: DEMO_PASSAGES.some(edge => edge.requiresKey &&
        ((edge.from === current && edge.to === room.id) ||
         (edge.to === current && edge.from === room.id))),
    })),
    passages: DEMO_PASSAGES,
    playerRoom: current, hasKey: false, ghostClue: '',
  };
}

export function legalMapDestinations(map: ManorMapSnapshot): ManorRoom[] {
  return map.rooms.filter(room => room.available && room.id !== map.playerRoom);
}

/** Exact ghost locations are shown ONLY when the C# candle clue reveals one. */
export function candleReveal(clue: string, rooms: readonly ManorRoom[]):
  | { roomId: number; name: string }
  | { roomId: null; name: string }
  | null {
  const match = /^Candle reveals: (.+)\.$/.exec(clue);
  if (!match) return null;
  const room = rooms.find(item => item.name === match[1]);
  return { roomId: room?.id ?? null, name: match[1] };
}

export function mapCoordinates(room: Pick<ManorRoom, 'x' | 'y'>): { x: number; y: number } {
  return { x: 31 + room.x * 39, y: 33 + (room.y - 1) * 46 };
}
