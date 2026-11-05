import {describe,expect,it} from 'vitest';
import {availableCardVariants,resolveCardSkin} from '../src/blackjack/cardSkins';

describe('Blackjack card cosmetics',()=>{
  it('has a complete styled palette for every equipped card variant',()=>{
    expect(availableCardVariants()).toHaveLength(12);
    for(const id of availableCardVariants()){
      const card=resolveCardSkin(id);
      for(const key of ['faceA','faceB','border','black','red','backA','backB','accent'])
        expect(card[key as keyof typeof card]).toMatch(/^#[0-9a-f]{6}$/i);
      expect(card.mark.length).toBeGreaterThan(0);
    }
    expect(resolveCardSkin('card-solar-eclipse').dark).toBe(true);
    expect(resolveCardSkin('invalid')).toEqual(resolveCardSkin('card-classic'));
  });
});
