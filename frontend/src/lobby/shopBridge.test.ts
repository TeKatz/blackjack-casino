import {describe,it,expect} from 'vitest';
import {filterShop,isShopSnapshot,isShopResult,previewShop} from './shopBridge';
describe('native shop bridge',()=>{
  it('accepts the structured inventory and rejects malformed economy data',()=>{
    expect(isShopSnapshot(previewShop)).toBe(true);
    expect(isShopSnapshot({...previewShop,items:[{...previewShop.items[0],price:-100}]})).toBe(false);
    expect(isShopSnapshot({...previewShop,items:[{...previewShop.items[0],id:'../../bad'}]})).toBe(false);
    expect(isShopSnapshot({...previewShop,packs:[{...previewShop.packs[0],canBuy:'yes'}]})).toBe(false);
  });
  it('filters by ownership and affordability without authorizing purchases',()=>{
    expect(filterShop(previewShop.items,'All','affordable','','recommended').map(x=>x.id)).toEqual(['frame-emerald','diceset-aurora']);
    expect(filterShop(previewShop.items,'Portrait','locked','eclipse','recommended').map(x=>x.id)).toEqual(['portrait-eclipse-raven']);
    expect(filterShop(previewShop.items,'All','owned','classic','recommended')[0]?.equipped).toBe(true);
    expect(filterShop(previewShop.items,'All','all','zz_not_found','recommended')).toHaveLength(0);
  });
  it('validates purchase notifications',()=>{
    expect(isShopResult({id:'frame-slate',success:true,message:'Equipped.'})).toBe(true);
    expect(isShopResult({id:'frame-slate',success:1,message:'Equipped.'})).toBe(false);
  });
});
