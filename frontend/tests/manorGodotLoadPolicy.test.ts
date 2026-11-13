import { describe, expect, it } from 'vitest';
import { shouldMountManorGodot } from '../src/graphics/manorGodotLoadPolicy';

describe('Manor 3D exclusive WebGL lifetime', () => {
  it('never creates a GPU context during normal reel play', () => {
    expect(shouldMountManorGodot(false)).toBe(false);
  });
  it('mounts the renderer for a real or practice bonus', () => {
    expect(shouldMountManorGodot(true)).toBe(true);
  });
  it('unmounts immediately after leaving, instead of keeping an 8 FPS iframe', () => {
    const sequence = [false, true, false, false, true, false];
    expect(sequence.map(shouldMountManorGodot)).toEqual(sequence);
  });
});
