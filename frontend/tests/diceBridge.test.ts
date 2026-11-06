import {describe,expect,it} from 'vitest';
import {categories,isDiceReply,isDiceSnapshot} from '../src/dice/bridge';
const guid='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
const game={gameId:guid,dice:[0,0,0,0,0],held:[false,false,false,false,false],
 rollsUsed:0,filledCount:0,isComplete:false,canScore:false,canRoll:true,isAdditionalKniffel:false,
 kniffelsScored:0,upperSubtotal:0,upperBonus:0,lowerSubtotal:0,additionalKniffelBonus:0,total:0,
 categories:categories.map((id,i)=>({id,name:id,stored:null,preview:null,upper:i<6}))};
const state={language:'en',mode:'casual',bankroll:'500',stakeOptions:['100','500','2500','10000','50000','100000'],
 stakePrizes:['100','500','2500','10000','50000','100000'].map(stake=>({stake,prizes:['5250','2110','500','250']})),
 casualHasSaved:false,casualCompleted:false,game,table:null,profile:null,diceVariant:'dice-aurora',
 dicePalette:{top:'#389E97',bottom:'#573776',pips:'#E1FFF4',edge:'#8BE5D3',glow:'#86EDDA',engraved:false},
 soundEnabled:true,animationsEnabled:true,musicEnabled:true,musicVolume:0.5,notice:'Ready',completionNotice:'',
 events:[],rollToken:0,rollingIndices:[],completionXp:200,completionSeasonXp:100};
const snapshot={version:1,session:guid,revision:0,state};
describe('Midnight Dice native snapshot validation',()=>{
 it('accepts exact C# dice, 13 categories and established casino price list',()=>expect(isDiceSnapshot(snapshot)).toBe(true));
 it('rejects forged, malformed or duplicate scoring data',()=>{
   expect(isDiceSnapshot({...snapshot,state:{...state,game:{...game,dice:[1,1,1,1,9]}}})).toBe(false);
   expect(isDiceSnapshot({...snapshot,state:{...state,game:{...game,rollsUsed:1,dice:[0,0,0,0,0]}}})).toBe(false);
   expect(isDiceSnapshot({...snapshot,state:{...state,game:{...game,categories:[...game.categories.slice(0,12),game.categories[0]]}}})).toBe(false);
   expect(isDiceSnapshot({...snapshot,state:{...state,bankroll:'Infinity'}})).toBe(false);
   expect(isDiceSnapshot({...snapshot,state:{...state,mode:'casino',table:null}})).toBe(false);
   expect(isDiceSnapshot({...snapshot,state:{...state,dicePalette:{...state.dicePalette,edge:'javascript:evil'}}})).toBe(false);
 });
 it('receipts require a matching canonical GUID, revision and status',()=>{
   expect(isDiceReply({requestId:guid,success:true,revision:2,message:'saved'})).toBe(true);
   expect(isDiceReply({requestId:'not-a-guid',success:true,revision:2,message:'saved'})).toBe(false);
   expect(isDiceReply({requestId:guid,success:true,revision:-1,message:'saved'})).toBe(false);
 });
});