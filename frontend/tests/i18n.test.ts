import { describe, expect, it } from 'vitest';
import {
  localizedKeys, germanKeys, localizeRoom, localizeArea, localizeFeature,
  localizeRunText, t,
} from '../src/i18n';

describe('Modern Manor local UI localization', () => {
  it('has matching German and English keys with real nonempty strings', () => {
    expect(germanKeys().sort()).toEqual(localizedKeys().sort());
    for (const key of localizedKeys()) {
      expect(t('en', key).length).toBeGreaterThan(0);
      expect(t('de', key).length).toBeGreaterThan(0);
    }
    expect(t('de', 'revision', 18)).toContain('18');
  });
  it('translates C# mode states but never changes saved text or values', () => {
    expect(localizeFeature('Haunted Night · 8 free spins saved', 'de'))
      .toBe('Haunted Night · 8 Freispiele gespeichert');
    expect(localizeFeature('Escape the Manor · Caught', 'de'))
      .toBe('Escape the Manor · Gefangen');
    expect(localizeFeature('Unknown new C# feature', 'de')).toBe('Unknown new C# feature');
    expect(localizeRunText('You enter Library. +10×. Checkpoint secured. Candle found.', 'de'))
      .toBe('Du betrittst Bibliothek. +10×. Kontrollpunkt gesichert. Kerze gefunden.');
    expect(localizeRunText('Candle reveals: Crypt.', 'de'))
      .toBe('Die Kerze enthüllt: Krypta.');
    expect(localizeRunText('Unknown ghost clue', 'de')).toBe('Unknown ghost clue');
  });
  it('does not guess concealed ghost locations and preserves the direction clue', () => {
    const clue = 'Cold air creeps from the west. The ghost is CLOSE (within two rooms).';
    expect(localizeRunText(clue, 'de')).toContain('Westen');
    expect(localizeRunText(clue, 'de')).toContain('zwei Räume');
    expect(localizeRunText(clue, 'en')).toBe(clue);
    expect(localizeRoom('Crypt', 'de')).toBe('Krypta');
    expect(localizeArea('Attic', 'de')).toBe('Dachboden');
  });
});
