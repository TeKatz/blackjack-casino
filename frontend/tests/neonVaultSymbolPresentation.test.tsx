import {describe,it,expect} from 'vitest';
import {createElement} from 'react';
import {renderToStaticMarkup} from 'react-dom/server';
import {SymbolArt} from '../src/neonVault/Symbols';
import {ReelStage} from '../src/neonVault/ReelStage';
import type {NeonSymbol} from '../src/neonVault/bridge';
const variants:NeonSymbol[]=['Pulse','Prism','Chip','Crown','Key','Wild','Scatter','VaultKey'];
describe('Neon Vault premium symbols and reel viewport',()=>{
 it('uses a full-width landscape frame and distinct looping layers in all eight symbols',()=>{
  for(const symbol of variants){
   const markup=renderToStaticMarkup(createElement(SymbolArt,{symbol}));
   expect(markup).toContain('viewBox="0 0 200 136"');
   expect(markup).toContain('vault-symbol-animated');
   expect(markup).toContain('vault-symbol-core');
   expect(markup).toContain('vault-symbol-energy');
   expect(markup).toContain('vault-symbol-foil-sweep');
   expect(markup).toContain('data-symbol="'+symbol+'"');
  }
 });
 it('keeps the compact paytable representation still and the winning symbol highlighted',()=>{
  const compact=renderToStaticMarkup(createElement(SymbolArt,{symbol:'Crown',compact:true}));
  const winner=renderToStaticMarkup(createElement(SymbolArt,{symbol:'Crown',emphasis:true}));
  expect(compact).not.toContain('vault-symbol-animated');
  expect(compact).toContain('vault-symbol-compact');
  expect(winner).toContain('vault-symbol-celebrate');
 });
 it('preserves exactly twenty inspectable static cells in five independent columns',()=>{
  const displayGrid=Array.from({length:20},(_,i)=>variants[i%variants.length]);
  const markup=renderToStaticMarkup(createElement(ReelStage,{
    displayGrid,result:null,rolling:[],finished:false,reducedMotion:false,
    winning:new Set<number>(),onStop:()=>{},
  }));
  expect(markup.match(/class="vault-cell"/g)).toHaveLength(20);
  expect(markup.match(/class="vault-reel-column"/g)).toHaveLength(5);
  expect(markup.match(/viewBox="0 0 200 136"/g)).toHaveLength(20);
  expect(markup).toContain('data-reel-engine="compositor-strips-v2"');
  expect(markup).not.toContain('vault-reel-tape');
 });
});
