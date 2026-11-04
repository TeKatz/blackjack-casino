import { describe, expect, it } from 'vitest';
import {
  availableActions, browserPreview, isSnapshot, parseHostMessage,
} from '../src/bridge/protocol';

describe('Manor v2 bridge contract', () => {
  it('accepts a valid revisioned C# snapshot and preserves it', () => {
    const envelope = {
      protocolVersion: 2, type: 'snapshot', requestId: 'r1', payload: browserPreview,
    };
    expect(parseHostMessage(envelope)).toEqual(envelope);
    expect(parseHostMessage(JSON.stringify(envelope))).toEqual(envelope);
  });

  it('rejects stale protocol versions, malformed spins, and non-snapshot results', () => {
    expect(parseHostMessage({
      protocolVersion: 1, type: 'snapshot', requestId: 'r1', payload: browserPreview,
    })).toBeNull();
    expect(parseHostMessage({
      protocolVersion: 2, type: 'spin.paid', requestId: 'r1', payload: browserPreview,
    })).toBeNull();
    expect(parseHostMessage('{')).toBeNull();
    expect(isSnapshot({ ...browserPreview, cells: ['Wild'] })).toBe(false);
    expect(isSnapshot({ ...browserPreview, bankroll: 1000 })).toBe(false);
    expect(isSnapshot({ ...browserPreview, winningCells: [15] })).toBe(false);
    expect(isSnapshot({ ...browserPreview, revision: -1 })).toBe(false);
    expect(isSnapshot({ ...browserPreview, revision: '1.5' })).toBe(false);
    expect(isSnapshot({ ...browserPreview, language: 'fr' })).toBe(false);
    expect(isSnapshot({ ...browserPreview, rules: { ...browserPreview.rules, paylines: [] } })).toBe(false);
    expect(isSnapshot({ ...browserPreview, rules: { ...browserPreview.rules, symbols: [] } })).toBe(false);
    expect(isSnapshot({ ...browserPreview, winningLineDetails: [
      { number: 21, count: 3, rows: [0, 0, 0, 0, 0], amount: '$5.00' },
    ] })).toBe(false);
    expect(isSnapshot({ ...browserPreview, winningLineDetails: [
      { number: 1, count: 3, rows: [0, 0, 4, 0, 0], amount: '$5.00' },
    ] })).toBe(false);
  });

  it('accepts replay-safe error responses with an authoritative updated snapshot', () => {
    const response = {
      protocolVersion: 2, type: 'error', requestId: 'a1',
      payload: { message: 'Stale command', fatal: false, snapshot: browserPreview },
    };
    expect(parseHostMessage(response)).toEqual(response);
    expect(parseHostMessage({
      ...response, payload: { ...response.payload, snapshot: null },
    })).toBeNull();
  });

  it('exposes only the next legal phase of gameplay', () => {
    expect(availableActions(browserPreview)).toEqual([]);
    const ready = { ...browserPreview, canSpin: true };
    expect(availableActions(ready)).toEqual(['spin.paid']);
    const free = { ...ready, canSpin: false, canFreeSpin: true, cycleActive: true };
    expect(availableActions(free)).toEqual(['spin.free']);
    const run = {
      status: 'Hunting', message: 'Enter the library', ghostClue: 'Far away',
      playerRoom: 0, turn: 0, exitTarget: 250, current: '5×', banked: '5×',
      hasCandle: false, hasKey: false, hasCharm: false, rooms: [], passages: [],
    };
    expect(availableActions({ ...free, run })).toEqual([
      'run.move', 'run.cashout', 'run.follow', 'run.auto',
    ]);
    expect(availableActions({ ...free, run: { ...run, hasCandle: true } }))
      .toContain('run.candle');
    expect(isSnapshot({ ...free, run, winningLineDetails: [
      { number: 1, count: 3, rows: [1, 1, 1, 1, 1], amount: '$15.00' },
    ] })).toBe(true);
    expect(isSnapshot({ ...free, run: { ...run, passages: [
      { from: 0, to: 17, requiresKey: false },
    ] } })).toBe(false);
  });
});
