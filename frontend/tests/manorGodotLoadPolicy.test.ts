import { describe, expect, it } from 'vitest';
import { shouldMountManorGodot } from '../src/graphics/manorGodotLoadPolicy';

describe('Manor 3D on-demand WebAssembly load policy', () => {
  it('does not mount Godot on normal slot startup', () => {
    expect(shouldMountManorGodot(false, false)).toBe(false);
  });
  it('starts loading on the first explicit 3D action', () => {
    expect(shouldMountManorGodot(true, false)).toBe(true);
  });
  it('keeps the same loaded iframe for subsequent 3D visits', () => {
    expect(shouldMountManorGodot(false, true)).toBe(true);
    expect(shouldMountManorGodot(true, true)).toBe(true);
  });
});
