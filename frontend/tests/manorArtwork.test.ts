import { describe, expect, it } from 'vitest';
import { MANOR_SYMBOL_NAMES, manorSymbolSvg } from '../src/graphics/manorArtwork';

describe('offline Victorian collectible texture pack', () => {
  it('contains fourteen original, distinct and complete Manor symbols', () => {
    expect(MANOR_SYMBOL_NAMES).toEqual([
      'Jack', 'Queen', 'King', 'Ace', 'Candelabrum', 'Watch', 'Raven',
      'Key', 'Portrait', 'Rose', 'Lady', 'Wild', 'Scatter', 'Bonus',
    ]);
    const art = MANOR_SYMBOL_NAMES.map(manorSymbolSvg);
    expect(new Set(art).size).toBe(14);
    for (const image of art) {
      expect(image).toContain('<svg');
      expect(image).toContain('width="1200" height="320"');
      expect(image).toContain('viewBox="0 0 300 160"');
      expect(image).toContain('</svg>');
      expect(image.length).toBeGreaterThan(1800);
      expect(image).not.toMatch(/<script|foreignObject|https?:\/\/(?!www\.w3\.org\/2000\/svg)/i);
    }
  });
  it('draws each rare trigger with artwork different from the generic rank cards', () => {
    const reference = manorSymbolSvg('Jack');
    for (const name of ['Lady', 'Wild', 'Scatter', 'Bonus'] as const) {
      const svg = manorSymbolSvg(name);
      expect(svg).not.toEqual(reference);
      expect(svg).toMatch(/url\(#brass\)|url\(#moon\)|url\(#mirror\)/);
    }
  });
});
