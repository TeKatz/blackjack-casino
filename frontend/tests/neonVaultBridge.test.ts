import {describe,expect,it} from 'vitest';
import {isNeonSnapshot,isNeonSpin,isNeonReceipt} from '../src/neonVault/bridge';
const id='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
const finalGrid=Array.from({length:20},(_,index)=>['Pulse','Prism','Chip','Crown','Key'][index%5]);
const spin={
 id,bet:'100',stake:'100',bankrollBefore:'10000',bankrollAfter:'9900',
 baseGrid:finalGrid,finalGrid,blackout:'None',blackoutWildCells:[],blackoutFullReel:null,
 expandedReels:[],securityOverrideReel:null,nearMiss:'None',jackpot:'None',
 waysPayout:'0',scatterPayout:'0',jackpotPayout:'0',payout:'0',net:'-100',
 waysWon:0,freeSpinsAwarded:0,freeSpinsRemaining:0,vaultEnergyAfter:0,
 isFreeSpin:false,betTier:'Standard',totalMultiplier:'0',playerXp:20,seasonXp:15,levelUp:false,
};
const state={
 language:'en',bankroll:'9900',bet:'100',canSpin:true,bonusActive:false,
 freeSpinsRemaining:0,vaultEnergy:0,energyTarget:12,betTier:'Standard',
 soundEnabled:true,animationsEnabled:true,
 bets:['1','5','10','25','50','100','250','500','1000','2500','5000','10000','25000','50000','100000','250000'],
 paytable:['Pulse','Prism','Chip','Crown','Key','Wild','Scatter','VaultKey']
   .map((symbol,i)=>({id:symbol,name:symbol,weight:20-i,
    threeReels:'0.1',fourReels:'0.4',fiveReels:'1.2'})),
 jackpots:['Mini','Minor','Major','Grand'].map((key,i)=>({
    id:key,multiplier:[25,75,250,1000][i],amount:'2500'})),
 profile:{level:1,prestige:0,coins:0},
 stats:{spins:1,wins:0,paidSpins:1,freeSpins:0,freeSpinSessions:0,
 freeSpinsAwarded:0,jackpots:0,grandJackpots:0,blackouts:0,totalBlackouts:0,
 securityOverrides:0,netProfit:'-100',biggestPayout:'0',bestPayoutMultiplier:'0'},
 lastSpin:spin,
};
const snapshot={version:1,session:id,revision:1,state};
describe('Neon Vault browser contract',()=>{
 it('checks C#-supplied twenty cells, jackpots, energy and signed wallet data',()=>{
  expect(isNeonSnapshot(snapshot)).toBe(true);
  expect(isNeonSnapshot({...snapshot,state:{...state,bankroll:9900}})).toBe(false);
  expect(isNeonSnapshot({...snapshot,state:{...state,energyTarget:9999}})).toBe(false);
  expect(isNeonSnapshot({...snapshot,state:{...state,bonusActive:true}})).toBe(false);
  expect(isNeonSnapshot({...snapshot,state:{...state,language:'fr'}})).toBe(false);
  expect(isNeonSnapshot({...snapshot,state:{...state,lastSpin:{...spin,finalGrid:finalGrid.slice(1)}}})).toBe(false);
 });
 it('validates every wild conversion and rejects spoofed receipts',()=>{
  expect(isNeonSpin(spin)).toBe(true);
  expect(isNeonSpin({...spin,blackoutWildCells:[3]})).toBe(false);
  expect(isNeonSpin({...spin,expandedReels:[1]})).toBe(false);
  expect(isNeonSpin({...spin,payout:'$1,000'})).toBe(false);
  expect(isNeonReceipt({requestId:id,success:true,message:'ok',revision:2})).toBe(true);
  expect(isNeonReceipt({requestId:id,success:true,message:'ok',revision:-1})).toBe(false);
 });
});