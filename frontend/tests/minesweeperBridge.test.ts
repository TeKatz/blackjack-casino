import {describe,expect,it} from 'vitest';
import {isSweepState,isSweepReply} from '../src/lounge/minesweeperBridge';
import {loungeIds,isLoungeId} from '../src/lounge/bridge';
const uuid='a3700365-661f-433d-8ae1-f3ebcda2331a';
const cell=()=>({revealed:false,flagged:false,scanned:false,adjacent:-1,scanCount:-1,mine:false});
function sample(difficulty:'easy'|'standard'|'expert'='easy'){
 const [width,height,mines]=difficulty==='easy'?[8,8,10]:difficulty==='standard'?[12,12,24]:[16,16,40];
 return {screen:'game' as const,runId:uuid,difficulty,width,height,mines,phase:'ready' as const,
  seconds:0,scansUsed:0,safeRevealed:0,flagged:0,explodedIndex:-1,
  cells:Array.from({length:width*height},cell)};
}
describe('Midnight Sweep frontend contract',()=>{
 it('exposes the eighth independent lounge destination',()=>{
  expect(isLoungeId('minesweeper')).toBe(true);
  expect(loungeIds).toHaveLength(8);
  expect(loungeIds.slice(-2)).toEqual(['tower-defense','minesweeper']);
 });
 it('accepts every intended board size',()=>{
  expect(isSweepState(sample('easy'))).toBe(true);
  expect(isSweepState(sample('standard'))).toBe(true);
  expect(isSweepState(sample('expert'))).toBe(true);
 });
 it('rejects disguised mines and unmasked pre-game numbers',()=>{
  const base=sample();
  expect(isSweepState({...base,cells:[{...base.cells[0],mine:true},...base.cells.slice(1)]})).toBe(false);
  expect(isSweepState({...base,cells:[{...base.cells[0],adjacent:3},...base.cells.slice(1)]})).toBe(false);
  expect(isSweepState({...base,runId:'forged'})).toBe(false);
  expect(isSweepState({...base,flagged:10})).toBe(false);
  expect(isSweepState({...base,scansUsed:2})).toBe(false);
  expect(isSweepState({...base,phase:'won',safeRevealed:1000})).toBe(false);
 });
 it('permits terminal mine disclosure but no replay forgery',()=>{
  const base=sample();
  const shown={...base,phase:'lost',cells:[{...base.cells[0],mine:true},...base.cells.slice(1)]};
  expect(isSweepState(shown)).toBe(true);
  expect(isSweepReply({requestId:uuid,success:true,message:'ok',revision:2})).toBe(true);
  expect(isSweepReply({requestId:uuid,success:true,message:'ok',revision:-1})).toBe(false);
 });
});
