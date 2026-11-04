import { describe, expect, it } from 'vitest';
import { browserPreview, isSnapshot, parseServerMessage } from '../src/bridge/protocol';

describe('Manor read-only WebView2 protocol', () => {
  it('accepts a correctly versioned snapshot', () => {
    const msg = { protocolVersion: 1, type: 'snapshot', requestId: 'r1', payload: browserPreview };
    expect(parseServerMessage(msg)).toEqual(msg);
    expect(parseServerMessage(JSON.stringify(msg))).toEqual(msg);
  });
  it('rejects other versions and unsupported commands', () => {
    expect(parseServerMessage({ protocolVersion: 2, type: 'snapshot', requestId: 'x', payload: browserPreview })).toBeNull();
    expect(parseServerMessage({ protocolVersion: 1, type: 'settle', requestId: 'x', payload: browserPreview })).toBeNull();
    expect(parseServerMessage('{')).toBeNull();
  });
  it('rejects malformed reel and balance states', () => {
    expect(isSnapshot({ ...browserPreview, cells: ['Wild'] })).toBe(false);
    expect(isSnapshot({ ...browserPreview, bankroll: 1000 })).toBe(false);
    expect(isSnapshot({ ...browserPreview, winningCells: [15] })).toBe(false);
  });
});
