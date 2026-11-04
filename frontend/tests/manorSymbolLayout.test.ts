import { describe, expect, it } from 'vitest';
import { MANOR_ARTWORK_TOP, MANOR_ARTWORK_HEIGHT, MANOR_SYMBOL_LABEL_Y }
  from '../src/graphics/ManorStage';
describe('slot symbol card readable vertical proportions',()=>{
  it('leaves physical separation between art and its lower name',()=>{
    expect(MANOR_SYMBOL_LABEL_Y - (MANOR_ARTWORK_TOP + MANOR_ARTWORK_HEIGHT))
      .toBeGreaterThanOrEqual(14);
    expect(MANOR_SYMBOL_LABEL_Y).toBeLessThanOrEqual(90);
  });
});
