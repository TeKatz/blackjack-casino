import type { ManorRun } from '../bridge/protocol';
import { legalMapDestinations, makeDemoMap } from './manorGodotMapData';

export type GodotChoice = { id: number; name: string };
export type GodotState = {
  source: 'manor-react';
  type: 'state';
  demo: boolean;
  roomName: string;
  roomId: number;
  active?: boolean;
  reducedMotion?: boolean;
  options: GodotChoice[];
};
export type GodotMessage = { source: 'manor-godot'; type: 'ready' }
  | { source: 'manor-godot'; type: 'choice'; room: number }
  | { source: 'manor-godot'; type: 'hover'; room: number | null };

export function demoState(roomId: number): GodotState {
  const map = makeDemoMap(roomId);
  const room = map.rooms.find(value => value.id === map.playerRoom);
  return {
    source: 'manor-react', type: 'state', demo: true,
    roomName: room?.name ?? 'Entrance Hall', roomId: map.playerRoom,
    options: legalMapDestinations(map).slice(0, 3)
      .map(value => ({ id: value.id, name: value.name })),
  };
}

export function liveState(run: ManorRun): GodotState {
  const room = run.rooms.find(value => value.id === run.playerRoom);
  return {
    source: 'manor-react', type: 'state', demo: false,
    roomName: room?.name ?? 'Entrance Hall', roomId: run.playerRoom,
    // Never invent routes. The authoritative C# snapshot marks legal destinations.
    // Three physical doors only; any fourth valid route is exposed via the map.
    options: run.rooms.filter(value => value.available)
      .slice(0, 3).map(value => ({ id: value.id, name: value.name })),
  };
}

export function isGodotMessage(value: unknown): value is GodotMessage {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return false;
  const msg = value as Record<string, unknown>;
  if (msg.source !== 'manor-godot') return false;
  if (msg.type === 'ready') return Object.keys(msg).every(k => k === 'source' || k === 'type');
  if (msg.type !== 'choice' && msg.type !== 'hover') return false;
  if (!Object.keys(msg).every(k => k === 'source' || k === 'type' || k === 'room')) return false;
  if (msg.type === 'hover' && msg.room === null) return true;
  return typeof msg.room === 'number' && Number.isInteger(msg.room) &&
    msg.room >= 0 && msg.room <= 16;
}

/** The message bridge accepts only one of the three actual 3D door labels. */
export function isAllowedGodotChoice(state: GodotState, room: number): boolean {
  return state.options.some(choice => choice.id === room);
}
