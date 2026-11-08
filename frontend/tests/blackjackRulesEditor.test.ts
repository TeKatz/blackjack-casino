import {describe,expect,it} from 'vitest';
import {isTableSnapshot} from '../src/blackjack/bridge';
import {resolveCardSkin} from '../src/blackjack/cardSkins';
describe('Blackjack rule state validation',()=>{
  it('styles the requested WPF card cosmetic instead of hard-coded classic',()=>{
    expect(resolveCardSkin('card-midnight-sovereign').dark).toBe(true);
  });
  it('requires a complete custom-rule source snapshot',()=>{
    expect(isTableSnapshot({version:1,session:'x',revision:0,state:{rules:{}}})).toBe(false);
  });
});
