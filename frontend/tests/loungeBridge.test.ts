import { describe, expect, it } from 'vitest';
import { isLoungeId, isLoungeSnapshot, loungeIds, readLoungeSnapshot } from '../src/lounge/bridge';

const games = loungeIds.map(id => ({id,status:null}));
const state = {version:1,language:'en',
  session:'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',revision:0,sudoku:null,
  solitaireSession:'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',solitaireRevision:0,solitaire:null,
  wordSearchSession:'cccccccc-cccc-4ccc-8ccc-cccccccccccc',wordSearchRevision:0,wordSearch:null,
  memorySession:'dddddddd-dddd-4ddd-8ddd-dddddddddddd',memoryRevision:0,memory:null,
  jigsawSession:'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee',jigsawRevision:0,jigsaw:null,
  adventureSession:'ffffffff-ffff-4fff-8fff-ffffffffffff',adventureRevision:0,adventure:null,
  musicEnabled:true,soundEnabled:true,
  animationsEnabled:true,games};

describe('Chill Lounge native navigation snapshot',()=>{
  it('accepts the six supported routes and their read-only statuses',()=>{
    expect(isLoungeSnapshot(state)).toBe(true);
    expect(isLoungeSnapshot({...state,language:'de',games:games.map(g=>({...g,status:'Saved'}))})).toBe(true);
    expect(readLoungeSnapshot({type:'lounge.snapshot',data:state})).toEqual(state);
  });
  it('rejects incomplete, duplicated, unknown or oversized routes',()=>{
    expect(isLoungeSnapshot({...state,games:games.slice(1)})).toBe(false);
    expect(isLoungeSnapshot({...state,games:[...games.slice(0,5),games[0]]})).toBe(false);
    expect(isLoungeSnapshot({...state,games:[...games.slice(0,5),{id:'blackjack',status:null}]})).toBe(false);
    expect(isLoungeSnapshot({...state,games:games.map(g=>({...g,status:'x'.repeat(241)}))})).toBe(false);
    expect(isLoungeSnapshot({...state,musicEnabled:'true'})).toBe(false);
    expect(readLoungeSnapshot({type:'untrusted.snapshot',data:state})).toBe(null);
    expect(isLoungeId('tales-backroom')).toBe(true);
    expect(isLoungeId('transfer-bankroll')).toBe(false);
  });
});
