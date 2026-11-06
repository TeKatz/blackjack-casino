import {describe,it,expect} from 'vitest';
import {difficulties,isWordSearchState,isWordSearchReply} from '../src/lounge/wordSearchBridge';

const tiers=difficulties.map((difficulty,index)=>({
 difficulty,size:8+index,wordCount:[6,7,8,10,12,14][index],total:48,completed:1,rewardXp:100}));
const stats={completedPuzzles:6,wordsFound:57,noHintCompletions:4,expertCompleted:1,masterCompleted:1};
const base={screen:'library',catalogTotal:288,themeTotal:24,completedTotal:6,difficulties:tiers,
 stats,resume:null,game:null,notice:'',rewardNotice:''};
const first='CASINO';
const grid=first+'A'.repeat(58);
const game={id:'word-search-relaxed-025',difficulty:'Relaxed',theme:'Midnight Club',
 size:8,grid,words:[first,'TABLE','CHIPS','DEALER','CARDS','JAZZ'],found:[first],
 foundPaths:[{word:first,startRow:0,startColumn:0,endRow:0,endColumn:5}],
 hint:null,hintsUsed:0,elapsedSeconds:20,completed:false,rewardXp:100};

describe('authoritative Word Search snapshot',()=>{
 it('accepts 48 puzzles per difficulty and the original 8–13 letter-grid sizes',()=>{
  expect(isWordSearchState(base)).toBe(true);
  expect(isWordSearchState({...base,screen:'game',game})).toBe(true);
 });
 it('rejects forged grids, word placements, duplicate words and malformed counts',()=>{
  expect(isWordSearchState({...base,screen:'game',game:{...game,grid:'A'.repeat(64)}})).toBe(false);
  expect(isWordSearchState({...base,screen:'game',game:{...game,foundPaths:[{...game.foundPaths[0],endColumn:7}]}})).toBe(false);
  expect(isWordSearchState({...base,screen:'game',game:{...game,words:[...game.words,first]}})).toBe(false);
  expect(isWordSearchState({...base,catalogTotal:144})).toBe(false);
  expect(isWordSearchState({...base,difficulties:tiers.slice(1)})).toBe(false);
  expect(isWordSearchState({...base,screen:'game',game:{...game,hintsUsed:-1}})).toBe(false);
 });
 it('validates versioned C# receipts',()=>{
  const r={requestId:'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',success:true,message:'saved',revision:3};
  expect(isWordSearchReply(r)).toBe(true);
  expect(isWordSearchReply({...r,revision:-1})).toBe(false);
  expect(isWordSearchReply({...r,requestId:'invalid'})).toBe(false);
 });
});
