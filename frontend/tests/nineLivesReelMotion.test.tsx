import {describe,expect,it} from 'vitest';
import {createElement} from 'react';
import {renderToStaticMarkup} from 'react-dom/server';
import {reelDuration,reelFrames,reelStrip,revealDuration,STOP_MS} from '../src/nineLives/reelMotion';
import {ReelStage} from '../src/nineLives/ReelStage';
import type {NineLivesSymbol} from '../src/nineLives/bridge';

const id='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
const shown:NineLivesSymbol[]=['Yarn','Bell','BlackCat'];
const landed:NineLivesSymbol[]=['Bonus','Wild','Fishbone'];

describe('Nine Lives three independent compositor reels',()=>{
 it('keeps the authoritative C# three visible landing symbols in each tape',()=>{
  for(let reel=0;reel<3;reel++){
   const strip=reelStrip(id,reel,shown,landed);
   const first=4+9+reel*4;
   expect(strip.slice(1,4)).toEqual(shown);
   expect(strip.slice(first,first+3)).toEqual(landed);
   expect(strip.at(-1)).toBe(landed[0]);
   expect(reelFrames(110,reel).at(-1)?.transform).toBe(`translate3d(0,${-first*110}px,0)`);
  }
 });
 it('uses reproducible cosmetic filler, not random results or React symbol rerolls',()=>{
  const first=reelStrip(id,1,shown,landed);
  expect(first).toEqual(reelStrip(id,1,shown,landed));
  expect(first).not.toEqual(reelStrip('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',1,shown,landed));
  expect(landed).toEqual(['Bonus','Wild','Fishbone']);
 });
 it('decelerates smoothly with recoil and supports three speed settings',()=>{
  for(const speed of ['normal','fast','turbo'] as const){
   expect(revealDuration(speed)).toBeGreaterThan(reelDuration(2,speed));
   expect(reelDuration(0,speed)).toBeLessThan(reelDuration(1,speed));
   expect(reelDuration(1,speed)).toBeLessThan(reelDuration(2,speed));
  }
  expect(reelDuration(2,'normal')).toBe(STOP_MS[2]);
  expect(reelDuration(2,'turbo')).toBeLessThan(reelDuration(2,'fast'));
  expect(reelFrames(100,1).map(x=>x.offset)).toEqual([0,.055,.23,.69,.87,.96,1]);
  expect(()=>reelDuration(3,'normal')).toThrow(RangeError);
 });
 it('shows nine static art images and three stable reel columns before spinning',()=>{
  const displayGrid=Array.from({length:9},(_,i)=>shown[i%3]);
  const html=renderToStaticMarkup(createElement(ReelStage,{
   displayGrid,result:null,spinning:[],activeWin:false,speed:'normal',
   reducedMotion:false,onStop:()=>{},
  }));
  expect(html.match(/class="nine-cell"/g)).toHaveLength(9);
  expect(html.match(/class="nine-reel-column"/g)).toHaveLength(3);
  expect(html.match(/class="nine-symbol-art"/g)).toHaveLength(9);
  expect(html).toContain('data-reel-engine="nine-compositor-v1"');
  expect(html).not.toContain('nine-reel-tape');
 });
});
