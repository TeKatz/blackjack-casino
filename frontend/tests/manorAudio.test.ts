import { describe, expect, it } from 'vitest';
import { boundedVolume, MANOR_CUES } from '../src/audio/manorAudio';

describe('Midnight Manor audio safety contract', () => {
  it('has an entirely local, low-volume sound cue for each presentation', () => {
    expect(Object.keys(MANOR_CUES)).toHaveLength(12);
    for (const voices of Object.values(MANOR_CUES)) {
      expect(voices.length).toBeGreaterThan(0);
      expect(voices.length).toBeLessThanOrEqual(5);
      expect(voices.reduce((sum, x) => sum + x.level, 0)).toBeLessThan(.52);
      for (const voice of voices) {
        expect(voice.hz).toBeGreaterThanOrEqual(50);
        expect(voice.hz).toBeLessThan(1500);
        expect(voice.duration).toBeGreaterThan(0);
        expect(voice.duration).toBeLessThanOrEqual(1);
        expect(voice.offset).toBeGreaterThanOrEqual(0);
        expect(voice.offset).toBeLessThan(1);
      }
    }
  });
  it('clamps volume and fails closed for invalid values', () => {
    expect(boundedVolume(-2)).toBe(0);
    expect(boundedVolume(0.4)).toBe(.4);
    expect(boundedVolume(100)).toBe(1);
    expect(boundedVolume(NaN)).toBe(0);
    expect(boundedVolume(Infinity)).toBe(0);
  });
});
