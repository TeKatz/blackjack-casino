import {describe,it,expect} from 'vitest';
import {isRubySpin,isRubySnapshot,isRubyReceipt} from '../src/indianRuby/bridge';

const guid='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
const base=['Elephant','Ruby','Necklace','Vase','Spices','Ace','King','Queen','Jack','Ten','Empty','Ace','King','Queen','Ten'];
const noBonus=['Elephant','Ruby','Necklace','Vase','Spices','Ace','King','Queen','Jack','Ten','Empty','Ace','King','Queen','Ten'];
const spin={
 id:guid,bet:'25',bankrollBefore:'1000',bankrollAfter:'975',
 baseGrid:base,finalGrid:noBonus,heldCells:[],winningLines:[],
 elephantBonus:false,basePayout:'0',bonusPayout:'0',payout:'0',net:'-25',
 playerXp:20,seasonXp:15,levelUp:false,
};
const lines=[
 [0,1,2,3,4],[5,6,7,8,9],[10,11,12,13,14],
 [0,6,12,8,4],[10,6,2,8,14],[0,1,7,3,4],
 [10,11,7,13,14],[5,1,2,3,9],[5,11,12,13,9],[0,6,7,8,14],
];
const paytable=['Elephant','Ruby','Necklace','Vase','Spices','Ace','King','Queen','Jack','Ten']
 .map(symbol=>({symbol,two:symbol==='Elephant'?10:0,three:25,four:75,five:250,
  outerPercent:symbol==='Ruby'?0:3.5,innerPercent:3}));
const state={
 language:'de',bankroll:'975',bet:'25',canSpin:true,soundEnabled:true,animationsEnabled:true,
 bets:['1','5','10','25','50','100','250','500','1000'],paylines:lines,
 paytable,bonusTriggerPercent:8.7,bonusElephantPercent:13,bonusRubyInnerPercent:9,
 profile:{level:1,prestige:0,coins:0},stats:{spins:1,wins:0,netProfit:'-25',biggestPayout:'0'},
 lastSpin:spin,
};
const snapshot={version:1,session:guid,revision:1,state};
describe('Sunset Ruby host bridge',()=>{
 it('accepts only native, complete 5 by 3 outcomes and ten lines',()=>{
  expect(isRubySnapshot(snapshot)).toBe(true);
  expect(isRubySnapshot({...snapshot,state:{...state,bankroll:975}})).toBe(false);
  expect(isRubySnapshot({...snapshot,state:{...state,paylines:lines.slice(0,9)}})).toBe(false);
  expect(isRubySnapshot({...snapshot,state:{...state,paytable:[]}})).toBe(false);
  expect(isRubySnapshot({...snapshot,state:{...state,bonusTriggerPercent:'99'}})).toBe(false);
  expect(isRubySnapshot({...snapshot,state:{...state,bets:['25']}})).toBe(false);
  expect(isRubySnapshot({...snapshot,revision:-1})).toBe(false);
  expect(isRubySpin({...spin,payout:'$1000'})).toBe(false);
  expect(isRubySpin({...spin,baseGrid:base.slice(0,14)})).toBe(false);
 });
 it('enforces elephant bonus trigger and held positions',()=>{
  expect(isRubySpin({...spin,elephantBonus:true})).toBe(false);
  expect(isRubySpin({...spin,heldCells:[0]})).toBe(false);
  const bonusBase=[...base];bonusBase[5]='Elephant';
  const bonusFinal=bonusBase.map((s,i)=>s==='Elephant'||s==='Ruby'?s:'Empty');
  const heldCells=bonusBase.flatMap((s,i)=>s==='Elephant'||s==='Ruby'?[i]:[]);
  expect(isRubySpin({...spin,baseGrid:bonusBase,finalGrid:bonusFinal,
   elephantBonus:true,heldCells})).toBe(true);
  expect(isRubySpin({...spin,baseGrid:bonusBase,finalGrid:bonusFinal,
   elephantBonus:true,heldCells:heldCells.concat([4])})).toBe(false);
 });
 it('rejects malformed receipts',()=>{
  expect(isRubyReceipt({requestId:guid,success:true,message:'ok',revision:1})).toBe(true);
  expect(isRubyReceipt({requestId:guid,success:true,message:'ok',revision:-1})).toBe(false);
 });
});
