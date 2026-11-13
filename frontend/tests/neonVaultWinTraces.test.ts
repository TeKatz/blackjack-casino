import {describe,it,expect} from 'vitest';
import {winningTraces} from '../src/neonVault/winTraces';
import type {NeonSymbol} from '../src/neonVault/bridge';
describe('Neon Vault live win routes',()=>{
 it('does not reveal a win before the third reel has physically stopped',()=>{
  const grid=Array.from({length:20},()=> 'Pulse' as NeonSymbol);
  expect(winningTraces(grid,2)).toEqual([]);
  expect(winningTraces(grid,3)).toMatchObject([{symbol:'Pulse',cells:[5,6,7]}]);
 });
 it('only draws a true contiguous paying way and stops at the mismatch',()=>{
  const grid=Array.from({length:20},()=> 'Scatter' as NeonSymbol);
  grid[5]='Prism';grid[6]='Prism';grid[7]='Wild';grid[8]='Crown';
  expect(winningTraces(grid,5)).toEqual([{symbol:'Prism',cells:[5,6,7]}]);
  expect(winningTraces(grid,3)).toEqual([{symbol:'Prism',cells:[5,6,7]}]);
 });
 it('ignores nonpaying specials and does not mutate the outcome',()=>{
  const grid=Array.from({length:20},()=> 'VaultKey' as NeonSymbol);
  const original=[...grid];
  expect(winningTraces(grid,5)).toEqual([]);
  expect(grid).toEqual(original);
 });
});
