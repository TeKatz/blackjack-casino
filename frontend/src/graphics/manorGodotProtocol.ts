import type { ManorRoom, ManorRun } from '../bridge/protocol';

export type GodotChoice = { id: number; name: string };
export type GodotState = {
  source: 'manor-react';
  type: 'state';
  demo: boolean;
  roomName: string;
  options: GodotChoice[];
};
export type GodotMessage = { source: 'manor-godot'; type: 'ready' }
  | { source: 'manor-godot'; type: 'choice'; room: number };

const DEMO_ROOMS: Record<number, { name: string; adjacent: number[] }> = {
  0: { name: 'Entrance Hall', adjacent: [1, 4, 7] },
  1: { name: "Servants' Corridor", adjacent: [0, 2, 4] },
  2: { name: 'Candle Landing', adjacent: [1, 3, 5] },
  3: { name: 'Grand Staircase', adjacent: [2, 10] },
  4: { name: 'Library', adjacent: [0, 1, 5] },
  5: { name: 'Reading Room', adjacent: [4, 2, 6] },
  6: { name: 'Study', adjacent: [5, 11] },
  7: { name: 'Ballroom', adjacent: [0, 1, 8] },
  8: { name: 'Music Room', adjacent: [7, 9, 2] },
  9: { name: 'Winter Garden', adjacent: [8, 12] },
  10: { name: 'Attic', adjacent: [3, 13] },
  11: { name: 'Forbidden Wing', adjacent: [6, 13] },
  12: { name: 'Crypt', adjacent: [9, 14] },
  13: { name: 'Moonlit Landing', adjacent: [10, 11, 15] },
  14: { name: 'Secret Passage', adjacent: [12, 15] },
  15: { name: 'Courtyard', adjacent: [13, 14, 16] },
  16: { name: 'Moonlit Gate', adjacent: [15] },
};

export function demoState(roomId: number): GodotState {
  const room = DEMO_ROOMS[roomId] ?? DEMO_ROOMS[0];
  return {
    source: 'manor-react', type: 'state', demo: true, roomName: room.name,
    options: room.adjacent.slice(0, 3).map(id => ({ id, name: DEMO_ROOMS[id].name })),
  };
}

export function liveState(run: ManorRun): GodotState {
  const room: ManorRoom | undefined = run.rooms.find(value => value.id === run.playerRoom);
  return {
    source: 'manor-react', type: 'state', demo: false,
    roomName: room?.name ?? 'Entrance Hall',
    // Never invent routes. The authoritative C# snapshot marks legal destinations.
    options: run.rooms.filter(value => value.available)
      .slice(0, 3).map(value => ({ id: value.id, name: value.name })),
  };
}

export function isGodotMessage(value: unknown): value is GodotMessage {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return false;
  const msg = value as Record<string, unknown>;
  if (msg.source !== 'manor-godot') return false;
  if (msg.type === 'ready') return Object.keys(msg).every(k => k === 'source' || k === 'type');
  return msg.type === 'choice' && Number.isInteger(msg.room) &&
    typeof msg.room === 'number' && msg.room >= 0 && msg.room <= 16 &&
    Object.keys(msg).every(k => k === 'source' || k === 'type' || k === 'room');
}

export function isAllowedGodotChoice(state: GodotState, room: number): boolean {
  return state.options.some(choice => choice.id === room);
}
