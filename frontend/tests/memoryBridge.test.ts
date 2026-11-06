import {describe,expect,it} from 'vitest';
import {deckIds,difficulties,isMemoryReply,isMemoryState} from '../src/lounge/memoryBridge';
const specs:[[number,number,number],[number,number,number],[number,number,number],[number,number,number],[number,number,number]]=[
 [4,4,8],[4,5,10],[6,6,18],[6,8,24],[8,8,32]];
const tiers=difficulties.map((difficulty,i)=>({
 difficulty,rows:specs[i][0],columns:specs[i][1],pairs:specs[i][2],rewardXp:100,bestMoves:0}));
const preview=Array.from({length:4},(_,i)=>({glyph:'♠',name:'Symbol '+i,accent:'#D2B87D'}));
const decks=deckIds.map(id=>({id,name:id,description:'A matching deck',preview}));
const stats={completedGames:3,pairsFound:24,cardsFlipped:56,wrongMatches:4,
 perfectGames:2,expertCompleted:0,masterCompleted:0,bestStreak:8,bestAccuracy:1};
const state={screen:'library',preferredDeckId:'casino-icons',decks,difficulties:tiers,
 stats,resume:null,game:null,notice:'',rewardNotice:''};
const cards=Array.from({length:16},(_,index)=>({
 index,faceUp:false,matched:false,glyph:null,name:null,accent:null}));
const game={gameId:'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',difficulty:'Easy',deckId:'casino-icons',
 deckName:'Casino Icons',rows:4,columns:4,cards,pairsFound:0,pairCount:8,
 moves:0,cardsFlipped:0,wrongMatches:0,currentStreak:0,bestStreak:0,
 accuracy:1,elapsedSeconds:0,completed:false,perfect:false,resolvingMismatch:false,rewardXp:100};
describe('C# Memory snapshot is a private view of the real table',()=>{
 it('accepts five C# table sizes and all four decks',()=>{
  expect(isMemoryState(state)).toBe(true);
  expect(isMemoryState({...state,screen:'game',game})).toBe(true);
  expect(decks.length).toBe(4);
  expect(tiers.map(x=>x.rows*x.columns)).toEqual([16,20,36,48,64]);
 });
 it('does not accept a hidden card that reveals a symbol',()=>{
  const leaked=cards.map((c,i)=>i===0?{...c,glyph:'chip'}:c);
  expect(isMemoryState({...state,screen:'game',game:{...game,cards:leaked}})).toBe(false);
  const shown=cards.map((c,i)=>i===0?{...c,faceUp:true,glyph:'⚄',name:'Dice',accent:'#D7B665'}:c);
  expect(isMemoryState({...state,screen:'game',game:{...game,cards:shown}})).toBe(true);
  expect(isMemoryState({...state,screen:'game',game:{...game,cards:shown,resolvingMismatch:true}})).toBe(false);
 });
 it('rejects invalid indices, layouts, deck aliases and dishonest completion',()=>{
  expect(isMemoryState({...state,screen:'game',game:{...game,cards:cards.map((c,i)=>i===2?{...c,index:3}:c)}})).toBe(false);
  expect(isMemoryState({...state,screen:'game',game:{...game,pairCount:7}})).toBe(false);
  expect(isMemoryState({...state,screen:'game',game:{...game,completed:true}})).toBe(false);
  expect(isMemoryState({...state,preferredDeckId:'custom'})).toBe(false);
  expect(isMemoryState({...state,decks:decks.slice(1)})).toBe(false);
 });
 it('accepts GUID native receipts with nonnegative revision only',()=>{
  const r={requestId:'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',success:true,message:'Saved',revision:2};
  expect(isMemoryReply(r)).toBe(true);
  expect(isMemoryReply({...r,revision:-1})).toBe(false);
  expect(isMemoryReply({...r,requestId:'invalid'})).toBe(false);
 });
});
