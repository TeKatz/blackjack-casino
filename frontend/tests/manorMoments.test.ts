import { describe, expect, it } from 'vitest';
import { browserPreview } from '../src/bridge/protocol';
import { manorWinTier, runCue, runMoment, spinCue, spinMoment,
  settledSpinMoment, localizeMoment } from '../src/graphics/manorMoments';

describe('Midnight Manor event presentation (authoritative snapshots only)', () => {
  const spin = { ...browserPreview, hasCommittedSpin: true, bet: '$25.00', payout: '$15.00',
    winningLines: 2, doors: 0, scatters: 0 };

  it('does not show a payout on initial mount, zero wins or invalid currency', () => {
    expect(spinMoment(browserPreview)).toBeNull();
    expect(spinMoment({ ...spin, payout: '$0.00' })).toBeNull();
    expect(manorWinTier('not money', '$25.00')).toBe('none');
    expect(manorWinTier('$1,234.50', '$25.00')).toBe('big');
    expect(manorWinTier('$2,500.00', '$25.00')).toBe('jackpot');
  });
  it('shows a quiet loss only once reels settle and localizes its text', () => {
    expect(spinMoment({ ...spin, payout: '$0.00' })).toBeNull();
    const loss = settledSpinMoment({ ...spin, payout: '$0.00' });
    expect(loss.key).toBe('loss');
    expect(loss.detail).not.toContain('
    expect(spinMoment(spin)?.detail).toContain('$15.00');
    expect(spinCue(spin)).toBe('smallWin');
    expect(spinMoment({ ...spin, payout: '$800.00' })?.heading).toBe('BIG WIN');
    expect(spinCue({ ...spin, payout: '$3,000.00' })).toBe('epicWin');
  });
  it('keeps simultaneous door and moon bonuses visible and preserves the original snapshot', () => {
    const both = { ...spin, doors: 2, scatters: 3, freeSpinsRemaining: 8 };
    const original = JSON.stringify(both);
    expect(spinMoment(both)).toEqual({
      key: 'door', heading: 'THE MANOR OPENS',
      detail: 'Escape the Manor has opened',
      secondary: 'Haunted Night free spins also unlocked',
    });
    expect(spinCue(both)).toBe('door');
    expect(JSON.stringify(both)).toBe(original);
    const free = { ...spin, doors: 0, scatters: 3, freeSpinsRemaining: 8 };
    expect(spinCue(free)).toBe('moon');
  });
  it('uses committed run text rather than hidden ghost coordinates or client navigation', () => {
    const run = {
      status: 'Hunting', message: 'Checkpoint secured', ghostClue: 'The ghost is CLOSE',
      playerRoom: 0, turn: 1, exitTarget: 250, current: '10×', banked: '10×',
      hasKey: false, hasCandle: false, hasCharm: false, passages: [],
      rooms: [{ id: 0, name: 'Entrance Hall', x: 0, y: 2, area: 'Entrance',
        visited: true, available: false, locked: false }],
    };
    expect(runMoment({ ...spin, run }, 'run.move').heading).toBe('ENTRANCE HALL');
    expect(runCue({ ...spin, run }, 'run.move')).toBe('checkpoint');
    expect(runCue({ ...spin, run: { ...run, message: 'Nothing' } }, 'run.move')).toBe('threat');
    expect(runCue({ ...spin, run: null }, 'run.cashout')).toBe('exit');
  });
});
);
    expect(localizeMoment(loss, 'de').heading).toBe('KEIN GEWINN');
    expect(localizeMoment(loss, 'de').detail).toBe('Das Haus schweigt');
    expect(settledSpinMoment(spin).key).toBe('win');
  });
  it('presents big-win tiers using the C# payout without modifying its value', () => {
    expect(spinMoment(spin)?.detail).toContain('$15.00');
    expect(spinCue(spin)).toBe('smallWin');
    expect(spinMoment({ ...spin, payout: '$800.00' })?.heading).toBe('BIG WIN');
    expect(spinCue({ ...spin, payout: '$3,000.00' })).toBe('epicWin');
  });
  it('keeps simultaneous door and moon bonuses visible and preserves the original snapshot', () => {
    const both = { ...spin, doors: 2, scatters: 3, freeSpinsRemaining: 8 };
    const original = JSON.stringify(both);
    expect(spinMoment(both)).toEqual({
      key: 'door', heading: 'THE MANOR OPENS',
      detail: 'Escape the Manor has opened',
      secondary: 'Haunted Night free spins also unlocked',
    });
    expect(spinCue(both)).toBe('door');
    expect(JSON.stringify(both)).toBe(original);
    const free = { ...spin, doors: 0, scatters: 3, freeSpinsRemaining: 8 };
    expect(spinCue(free)).toBe('moon');
  });
  it('uses committed run text rather than hidden ghost coordinates or client navigation', () => {
    const run = {
      status: 'Hunting', message: 'Checkpoint secured', ghostClue: 'The ghost is CLOSE',
      playerRoom: 0, turn: 1, exitTarget: 250, current: '10×', banked: '10×',
      hasKey: false, hasCandle: false, hasCharm: false, passages: [],
      rooms: [{ id: 0, name: 'Entrance Hall', x: 0, y: 2, area: 'Entrance',
        visited: true, available: false, locked: false }],
    };
    expect(runMoment({ ...spin, run }, 'run.move').heading).toBe('ENTRANCE HALL');
    expect(runCue({ ...spin, run }, 'run.move')).toBe('checkpoint');
    expect(runCue({ ...spin, run: { ...run, message: 'Nothing' } }, 'run.move')).toBe('threat');
    expect(runCue({ ...spin, run: null }, 'run.cashout')).toBe('exit');
  });
});
