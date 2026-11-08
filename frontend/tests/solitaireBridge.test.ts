import {describe,expect,it} from 'vitest';
import {isSolitaireState,isSolitaireReply,variants} from '../src/lounge/solitaireBridge';

const options=variants.flatMap(variant=>(variant==='Spider'?[1,2,4]:[1])
 .map(spiderSuits=>({variant,spiderSuits,canResume:false,won:false,dealNumber:0,moves:0,elapsedSeconds:0})));
const card={rank:1,suit:'Spades',faceUp:true,selectable:true};
const game={gameId:'12345678-1234-4234-8234-123456789abc',variant:'Klondike',dealNumber:17,
 spiderSuits:1,moves:5,elapsedSeconds:200,completedRuns:0,won:false,rewardXp:300,
 piles:{Stock:[{...card,faceUp:false,selectable:false}],T0:[card],'F-Spades':[]}};
const stats={gamesStarted:1,wins:0,gamesAbandoned:0,bestWinStreak:0,currentWinStreak:0,
 winRate:0,achievements:[{id:'first',name:'First',description:'Win once',complete:false}]};
const state={screen:'library',cardSetId:'card-emerald-house',nextDealNumber:618,options,stats,
 game:null,notice:'',rewardNotice:''};
describe('C# solitaire snapshot contract',()=>{
 it('accepts six variants with three spider suit types and a valid 52-card UI projection',()=>{
  expect(options.length).toBe(8);
  expect(isSolitaireState(state)).toBe(true);
  expect(isSolitaireState({...state,screen:'game',game})).toBe(true);
 });
 it('rejects duplicates, invalid piles, extra ranks, oversized or dishonest snapshots',()=>{
  expect(isSolitaireState({...state,options:[...options.slice(0,7),options[0]]})).toBe(false);
  expect(isSolitaireState({...state,screen:'game',game:{...game,piles:{Invalid:[card]}}})).toBe(false);
  expect(isSolitaireState({...state,screen:'game',game:{...game,piles:{T0:[{...card,rank:14}],Stock:[]}}})).toBe(false);
  expect(isSolitaireState({...state,screen:'game',game:{...game,piles:{T0:[{...card,suit:'fake'}],Stock:[]}}})).toBe(false);
  expect(isSolitaireState({...state,screen:'game',game:{...game,elapsedSeconds:-1}})).toBe(false);
 });
 it('validates native receipts but never trusts a browser-submitted payout',()=>{
  const receipt={requestId:'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',revision:2,success:true,message:'saved'};
  expect(isSolitaireReply(receipt)).toBe(true);
  expect(isSolitaireReply({...receipt,revision:-1})).toBe(false);
  expect(isSolitaireReply({...receipt,requestId:'not-guid'})).toBe(false);
 });
});
