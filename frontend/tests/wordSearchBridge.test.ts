import {describe,it,expect} from 'vitest';
import {createElement} from 'react';
import {renderToStaticMarkup} from 'react-dom/server';
import {WordSearchScreen} from '../src/lounge/wordSearch';
import type {WordSearchState} from '../src/lounge/wordSearchBridge';
import {difficulties,isWordSearchState,isWordSearchReply} from '../src/lounge/wordSearchBridge';

const tiers=difficulties.map((difficulty,index)=>({
 difficulty,size:8+index,wordCount:[6,7,8,10,12,14][index],total:96,completed:1,rewardXp:100}));
const stats={completedPuzzles:6,wordsFound:57,noHintCompletions:4,expertCompleted:1,masterCompleted:1};
const base={screen:'library',catalogTotal:576,themeTotal:36,completedTotal:6,difficulties:tiers,
 stats,resume:null,game:null,discoveredWords:[],lastFind:null,notice:'',rewardNotice:''};
const first='CASINO';
const grid=first+'A'.repeat(58);
const game={id:'word-search-relaxed-025',difficulty:'Relaxed',theme:'Midnight Club',
 size:8,grid,words:[first,'TABLE','CHIPS','DEALER','CARDS','JAZZ'],found:[first],
 foundPaths:[{word:first,startRow:0,startColumn:0,endRow:0,endColumn:5}],
 hint:null,hintsUsed:0,elapsedSeconds:20,completed:false,rewardXp:100};

describe('authoritative Word Search snapshot',()=>{
 it('accepts 96 puzzles per difficulty and the original 8–13 letter-grid sizes',()=>{
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
 it('accepts authoritative album entries and rejects duplicate or invented finds',()=>{
  const entry={word:'CASINO',theme:'Midnight Club',difficulty:'Relaxed',
   puzzleId:'word-search-relaxed-001',discoveredAt:'2026-11-08T00:00:00+00:00'};
  const changed={...base,discoveredWords:[entry]};
  expect(isWordSearchState(changed)).toBe(true);
  expect(isWordSearchState({...changed,discoveredWords:[entry,entry]})).toBe(false);
  expect(isWordSearchState({...changed,discoveredWords:[{...entry,word:'FAKE!'}]})).toBe(false);
  expect(isWordSearchState({...changed,discoveredWords:[{...entry,discoveredAt:'tomorrow'}]})).toBe(false);
  const recent={word:'CASINO',new:true,id:'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'};
  expect(isWordSearchState({...changed,screen:'game',game,lastFind:recent})).toBe(true);
  expect(isWordSearchState({...changed,screen:'game',game,lastFind:{...recent,word:'UNKNOWN'}})).toBe(false);
 });
 it('validates versioned C# receipts',()=>{
  const r={requestId:'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',success:true,message:'saved',revision:3};
  expect(isWordSearchReply(r)).toBe(true);
  expect(isWordSearchReply({...r,revision:-1})).toBe(false);
  expect(isWordSearchReply({...r,requestId:'invalid'})).toBe(false);
 });
});

describe('Word Search replacement choice',()=>{
 it('offers a new level even when an unfinished save is resumable',()=>{
  const saved:WordSearchState={...base,screen:'library',
   resume:{id:'word-search-relaxed-025',difficulty:'Relaxed',theme:'Midnight Club',
    found:1,total:6,elapsedSeconds:20}};
  const html=renderToStaticMarkup(createElement(WordSearchScreen,{
   state:saved,language:'de',pending:false,error:'',animations:false,command:()=>{},
  }));
  expect(html).toContain('data-testid="word-search-resume"');
  expect(html).toContain('data-testid="word-search-start-Relaxed"');
  expect(html).toContain('data-testid="word-search-start-Easy"');
  expect(html).toContain('Dein aktuelles Rätsel bleibt gespeichert');
  expect(html).not.toMatch(/data-testid="word-search-start-Relaxed" disabled/);
 },15_000);
});
