import {describe,it,expect} from 'vitest';
import {isReelsSnapshot,isReelsSpin,isReelsReply} from '../src/reels/bridge';

const strip=Array.from({length:32},(_,i)=>i<12?'Cherry':i<20?'Lemon':i<26?'Bell':i<30?'Bar':'Seven');
const spin={id:'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',bet:'25',
 bankrollBefore:'1000',bankrollAfter:'975',payout:'0',net:'-25',multiplier:0,
 stops:[0,12,20],symbols:['Cherry','Lemon','Bell'],playerXp:20,seasonXp:15,levelUp:false};
const snapshot={version:1,session:'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',revision:1,
 state:{language:'en',bankroll:'975',bet:'25',canSpin:true,
 soundEnabled:true,animationsEnabled:true,reelStrip:strip,
 paytable:[{id:'Cherry',name:'Cherries',stops:12,tripleMultiplier:5},
 {id:'Lemon',name:'Lemon',stops:8,tripleMultiplier:10},
 {id:'Bell',name:'Bell',stops:6,tripleMultiplier:25},
 {id:'Bar',name:'BAR',stops:4,tripleMultiplier:50},
 {id:'Seven',name:'Seven',stops:2,tripleMultiplier:100}],
 profile:{level:4,prestige:0,coins:12},
 stats:{spins:1,wins:0,pushes:0,netProfit:'-25',biggestPayout:'0'},
 lastSpin:spin}};
describe('Reels read-only browser contract',()=>{
 it('accepts validated authoritative results and rejects inconsistent stop symbols',()=>{
  expect(isReelsSnapshot(snapshot)).toBe(true);
  expect(isReelsSnapshot({...snapshot,state:{...snapshot.state,lastSpin:{...spin,symbols:['Seven','Lemon','Bell']}}})).toBe(false);
  expect(isReelsSnapshot({...snapshot,state:{...snapshot.state,bankroll:975}})).toBe(false);
  expect(isReelsSnapshot({...snapshot,state:{...snapshot.state,language:'fr'}})).toBe(false);
  expect(isReelsSnapshot({...snapshot,state:{...snapshot.state,reelStrip:strip.slice(1)}})).toBe(false);
  expect(isReelsSnapshot({...snapshot,revision:-1})).toBe(false);
 });
 it('validates saved spin receipts and rejects fabricated amounts',()=>{
  expect(isReelsSpin(spin)).toBe(true);
  expect(isReelsSpin({...spin,stops:[-1,4,6]})).toBe(false);
  expect(isReelsSpin({...spin,payout:'$1000'})).toBe(false);
  expect(isReelsReply({requestId:spin.id,success:true,message:'ok',revision:2})).toBe(true);
  expect(isReelsReply({requestId:spin.id,success:true,message:'ok',revision:-1})).toBe(false);
 });
});
