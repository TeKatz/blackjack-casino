import { describe, expect, it } from 'vitest';
import { browserPreview } from '../src/bridge/protocol';
import { localizeMoment, spinMoment, runMoment } from '../src/graphics/manorMoments';

describe('Localized C#-acknowledged rewards', () => {
  it('keeps the exact settled amount', () => {
    const spin = { ...browserPreview, hasCommittedSpin: true,
      payout: '$250.00', bet: '$25.00', winningLines: 3 };
    const raw = spinMoment(spin)!;
    const translated = localizeMoment(raw, 'de');
    expect(translated.heading).toBe('GEWINN');
    expect(translated.detail).toContain('$250.00');
    expect(translated.detail).toContain('gewinnende');
    expect(localizeMoment(raw, 'en')).toEqual(raw);
  });
  it('retains combined bonus announcements', () => {
    const spin = { ...browserPreview, hasCommittedSpin: true, doors: 2, scatters: 3 };
    const translated = localizeMoment(spinMoment(spin)!, 'de');
    expect(translated.detail).toContain('geöffnet');
    expect(translated.secondary).toContain('ebenfalls');
  });
  it('translates room announcements and only the revealed ghost clue', () => {
    const run = { ...browserPreview, run: {
      status: 'Hunting', message: 'You enter Library. +10×. Checkpoint secured.',
      ghostClue: 'Candle reveals: Crypt.', playerRoom: 4, turn: 1, exitTarget: 250,
      current: '15×', banked: '15×', hasCandle: true, hasKey: false, hasCharm: false,
      passages: [], rooms: [{ id: 4, name: 'Library', x: 1, y: 1, area: 'Library',
        visited: true, available: false, locked: false }],
    } };
    const translated = localizeMoment(runMoment(run, 'run.move'), 'de');
    expect(translated.heading).toBe('BIBLIOTHEK');
    expect(translated.detail).toContain('Kontrollpunkt');
    expect(translated.secondary).toContain('Krypta');
  });
});
