import {describe,it,expect} from 'vitest';
import {reelVisualStrip,reelTrackGeometry,reelKeyframes,REEL_STOP_MS,FEATURE_REVEAL_MS,reelStopDuration,featureRevealDuration} from '../src/neonVault/reelMotion';
import type {NeonSymbol} from '../src/neonVault/bridge';
const initial:NeonSymbol[]=['Pulse','Prism','Chip','Crown'];
const final:NeonSymbol[]=['Wild','Scatter','VaultKey','Key'];
const spin='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
describe('premium reel motion is visual-only and geometrically exact',()=>{
 it('holds real C# base-grid symbols as the four final visible cells on all five reels',()=>{
  for(let reel=0;reel<5;reel++){
   const strip=reelVisualStrip(spin,reel,initial,final);
   const geometry=reelTrackGeometry(126,strip.length);
   expect(strip.slice(-5,-1)).toEqual(final);
   expect(strip.slice(1,5)).toEqual(initial);
   expect(geometry.travelRows).toBe(4+8+3*reel);
   expect(geometry.finish).toBe(-(5+8+3*reel)*126);
   expect(geometry.finish).toBeLessThan(geometry.start);
  }
 });
 it('uses stable deterministic filler without interpreting the symbols as results',()=>{
  const a=reelVisualStrip(spin,2,initial,final);
  expect(a).toEqual(reelVisualStrip(spin,2,initial,final));
  expect(a).not.toEqual(reelVisualStrip('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',2,initial,final));
  expect(initial).toEqual(['Pulse','Prism','Chip','Crown']);
  expect(final).toEqual(['Wild','Scatter','VaultKey','Key']);
 });
 it('starts with a pullback, decelerates, overshoots, then settles exactly on four symbols',()=>{
  const strip=reelVisualStrip(spin,4,initial,final);
  const geometry=reelTrackGeometry(110,strip.length);
  const frames=reelKeyframes(110,strip.length);
  const y=(f:Keyframe)=>Number(/translate3d\(0,([-\d.]+)px/.exec(String(f.transform))?.[1]);
  expect(frames).toHaveLength(7);
  expect(frames.map(f=>f.offset)).toEqual([0,.075,.255,.64,.845,.955,1]);
  expect(y(frames[1])).toBeGreaterThan(y(frames[0]));
  expect(y(frames[5])).toBeLessThan(y(frames[6]));
  expect(y(frames[0])).toBe(geometry.start);
  expect(y(frames[6])).toBe(geometry.finish);
 });
 it('offers decreasing normal, fast and turbo durations without changing reel symbols',()=>{
  expect(reelStopDuration(4,'normal')).toBe(REEL_STOP_MS[4]);
  expect(reelStopDuration(4,'fast')).toBeLessThan(reelStopDuration(4,'normal'));
  expect(reelStopDuration(4,'turbo')).toBeLessThan(reelStopDuration(4,'fast'));
  for(const speed of ['normal','fast','turbo'] as const)
   expect(featureRevealDuration(speed)).toBeGreaterThan(reelStopDuration(4,speed));
 });
 it('keeps stop times ordered and feature reveal after the last reel settles',()=>{
  expect(REEL_STOP_MS).toHaveLength(5);
  expect(REEL_STOP_MS.every((t,i)=>i===0||t>REEL_STOP_MS[i-1])).toBe(true);
  expect(FEATURE_REVEAL_MS).toBeGreaterThan(REEL_STOP_MS[4]);
  expect(()=>reelVisualStrip(spin,5,initial,final)).toThrow(RangeError);
 });
});
