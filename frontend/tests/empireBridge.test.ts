import {describe,it,expect} from 'vitest';
import {fmt,isEmpireActivation,isEmpireSnapshot,isReply} from '../src/empire/empireBridge';
const fixture={
  version:2,session:'demo',revision:0,visitId:1,notice:null,lastReply:null,offline:null,
  state:{language:'de',animationsEnabled:true,soundEnabled:true,detached:false,economy:{
    credits:'0',rate:'0',tapValue:'1',clicks:0,prestige:0,totalLevels:0,runCredits:'0',
    prestigeRequirement:'250000',canPrestige:false,
    buildings:Array.from({length:12},(_,i)=>({id:'building-'+i,name:'Building '+i,description:'Demo',
      level:0,maxLevel:8,unlocked:i===0,visible:i<2,canBuy:false,cost:'12',
      rate:'0',visualTier:0,requirement:'',nextUnlock:null})),
    goal:{id:'building-0',title:'Upgrade',description:'Next level',current:'0',target:'12',progress:0}
  }},
};
describe('Empire clicker bridge',()=>{
 it('validates only the versioned clicker snapshot',()=>{
   expect(isEmpireSnapshot(fixture)).toBe(true);
   expect(isEmpireSnapshot({...fixture,version:1})).toBe(false);
   expect(isEmpireSnapshot({...fixture,visitId:0})).toBe(false);
   expect(isEmpireSnapshot({...fixture,state:{...fixture.state,economy:{...fixture.state.economy,buildings:[]}}})).toBe(false);
   expect(isEmpireSnapshot({...fixture,state:{...fixture.state,economy:{...fixture.state.economy,credits:42}}})).toBe(false);
 });
 it('enforces valid clicker receipts and activations',()=>{
   expect(isReply({requestId:'x',success:true,message:'OK',revision:2})).toBe(true);
   expect(isReply({requestId:'x',success:true,message:'OK',revision:NaN})).toBe(false);
   expect(isEmpireActivation({type:'empire.activated',data:{visitId:1}})).toBe(true);
   expect(isEmpireActivation({type:'empire.activated',data:{visitId:0}})).toBe(false);
 });
 it('compacts large credits without losing the underlying string contract',()=>{
   expect(fmt('1000000','de')).toContain('M');
   expect(fmt('100','de')).toBe('100');
 });
});
