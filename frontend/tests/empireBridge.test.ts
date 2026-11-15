import {describe,it,expect} from 'vitest';
import {fmt,isEmpireActivation,isEmpireSnapshot,isReply} from '../src/empire/empireBridge';
const fixture={
  version:2,session:'demo',revision:0,visitId:1,notice:null,lastReply:null,offline:null,
  state:{language:'de',animationsEnabled:true,soundEnabled:true,detached:false,economy:{
    legacy:{unlocked:false,tokens:0,earned:0,total:20,
      upgrades:Array.from({length:15},(_,i)=>({id:'upgrade-'+i,path:i<5?'active':i<10?'passive':'hybrid',
        name:'Upgrade',description:'Effect',cost:1,minimumPrestige:1,previousId:null,
        owned:false,unlocked:false,canBuy:false})),
      milestones:Array.from({length:20},(_,i)=>({id:'milestone-'+i,
        name:'Milestone',description:'Goal',category:'clicks',target:'100',
        current:'0',earned:false,progress:0})),
    },
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
   expect(isEmpireSnapshot({...fixture,state:{...fixture.state,economy:{
     ...fixture.state.economy,legacy:{...fixture.state.economy.legacy,upgrades:[]}
   }}})).toBe(false);
   expect(isEmpireSnapshot({...fixture,state:{...fixture.state,economy:{
     ...fixture.state.economy,legacy:{...fixture.state.economy.legacy,earned:25}
   }}})).toBe(false);
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
