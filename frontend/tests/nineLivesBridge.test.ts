import {describe,expect,it} from 'vitest';
import {isNineLivesReceipt,isNineLivesSnapshot,isNineLivesSpin,paylineCells} from '../src/nineLives/bridge';
const guid='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
const spin={id:guid,bet:'25',stake:'25',bankrollBefore:'1000',bankrollAfter:'975',
  grid:['Yarn','Fishbone','Bell','TabbyCat','WhiteCat','BlackCat','Wild','Bonus','Yarn'],
  winningLines:[],winningCells:[],pounceWildCell:null,bonusCount:1,
  bonusSpinsAwarded:0,bonusSpinsRemaining:0,isBonusSpin:false,
  fullGridFrenzy:false,payout:'0',net:'-25',totalMultiplier:0,
  playerXp:20,seasonXp:15,levelUp:false};
const state={language:'en',bankroll:'975',bet:'25',canSpin:true,
 bonusActive:false,bonusSpinsRemaining:0,soundEnabled:true,animationsEnabled:true,
 bets:['1','5','10','25','50','100','250','500','1000','2500','5000','10000','25000'],
 paylines:paylineCells.map(x=>[...x]),
 paytable:['Yarn','Fishbone','Bell','TabbyCat','WhiteCat','BlackCat','Wild','Bonus']
   .map((id,i)=>({id,name:id,weight:10-i,lineMultiplier:i})),
 profile:{level:1,prestige:0,coins:0},
 stats:{spins:1,wins:0,paidSpins:1,bonusSpins:0,bonusSessions:0,
   pounceWilds:0,fullGridFrenzies:0,netProfit:'-25',biggestPayout:'0',bestPayoutMultiplier:0},
 lastSpin:spin};
const snapshot={version:1,session:guid,revision:1,state};
describe('Nine Lives read-only C# bridge',()=>{
 it('checks complete native 3×3/5-line snapshot and rejects fabricated wallet or payouts',()=>{
  expect(isNineLivesSnapshot(snapshot)).toBe(true);
  expect(isNineLivesSnapshot({...snapshot,state:{...state,bankroll:975}})).toBe(false);
  expect(isNineLivesSnapshot({...snapshot,state:{...state,language:'fr'}})).toBe(false);
  expect(isNineLivesSnapshot({...snapshot,state:{...state,paylines:[[0,1,2]]}})).toBe(false);
  expect(isNineLivesSnapshot({...snapshot,revision:-1})).toBe(false);
  expect(isNineLivesSpin({...spin,payout:'$1000'})).toBe(false);
 });
 it('refuses false bonus claims, mismatched pounce cells, and malformed receipts',()=>{
  expect(isNineLivesSpin({...spin,bonusCount:3})).toBe(false);
  expect(isNineLivesSpin({...spin,pounceWildCell:1})).toBe(false);
  expect(isNineLivesSpin({...spin,winningLines:[3],winningCells:[0,4]})).toBe(false);
  expect(isNineLivesReceipt({requestId:guid,success:true,message:'ok',revision:1})).toBe(true);
  expect(isNineLivesReceipt({requestId:guid,success:true,message:'ok',revision:-1})).toBe(false);
 });
});