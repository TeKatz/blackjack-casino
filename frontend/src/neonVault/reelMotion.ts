import {symbols} from './bridge';
import type {NeonSymbol} from './bridge';

/** Pure, visual-only timing. Never used for C# RNG, payout, or bonus decisions. */
export const REEL_STOP_MS=[760,1120,1510,1920,2430] as const;
export const FEATURE_REVEAL_MS=REEL_STOP_MS[4]+240;
export type ReelSpeed='normal'|'fast'|'turbo';
const SPEED_FACTOR:Record<ReelSpeed,number>={normal:1,fast:.65,turbo:.38};
export function reelStopDuration(reel:number,speed:ReelSpeed):number{
 if(reel<0||reel>=REEL_STOP_MS.length)throw new RangeError('Invalid reel');
 return Math.round(REEL_STOP_MS[reel]*SPEED_FACTOR[speed]);
}
export function featureRevealDuration(speed:ReelSpeed):number{
 return reelStopDuration(4,speed)+Math.round(240*SPEED_FACTOR[speed]);
}
export function reelAfterglowDuration(speed:ReelSpeed,feature:boolean):number{
 return Math.round((feature?1050:480)*SPEED_FACTOR[speed]);
}
export const INITIAL_GUARD_ROWS=1;
const spinSeed=(id:string,reel:number):number=>{
 let hash=(2166136261 ^ reel*97)>>>0;
 for(let i=0;i<id.length;i++)hash=Math.imul(hash^id.charCodeAt(i),16777619)>>>0;
 return hash;
};
const filler=(spinId:string,reel:number,numberOfSymbols:number):NeonSymbol[]=>{
 let state=spinSeed(spinId,reel);
 return Array.from({length:numberOfSymbols},()=>{
  state=(Math.imul(1664525,state)+1013904223)>>>0;
  return symbols[(state>>>8)%symbols.length];
 });
};
export function reelVisualStrip(spinId:string,reel:number,
 initial:readonly NeonSymbol[],final:readonly NeonSymbol[]):NeonSymbol[]{
 if(reel<0||reel>4||initial.length!==4||final.length!==4)
  throw new RangeError('Reel visual strip requires four visible cells and a valid reel.');
 const fill=filler(spinId,reel,8+3*reel);
 return [initial[3],...initial,...fill,...final,final[3]];
}
export function reelTrackGeometry(step:number,stripLength:number){
 if(!(step>0)||stripLength<10)throw new RangeError('Invalid reel dimensions');
 const travelRows=stripLength-4-INITIAL_GUARD_ROWS-1;
 const start=-INITIAL_GUARD_ROWS*step;
 const finish=-(INITIAL_GUARD_ROWS+travelRows)*step;
 return {start,finish,distance:travelRows*step,travelRows};
}
export function reelKeyframes(step:number,length:number):Keyframe[]{
 const {start,finish,distance}=reelTrackGeometry(step,length);
 const translate=(amount:number)=>`translate3d(0,${amount.toFixed(3)}px,0)`;
 return [
  {offset:0,transform:translate(start),easing:'cubic-bezier(.4,0,.6,1)'},
  {offset:.075,transform:translate(start+step*.23),easing:'cubic-bezier(.55,0,.7,.7)'},
  {offset:.255,transform:translate(start-distance*.22),easing:'linear'},
  {offset:.64,transform:translate(start-distance*.73),easing:'cubic-bezier(.12,.65,.14,1)'},
  {offset:.845,transform:translate(start-distance*.965),easing:'cubic-bezier(.16,.72,.19,1)'},
  {offset:.955,transform:translate(finish-step*.13),easing:'cubic-bezier(.25,0,.32,1)'},
  {offset:1,transform:translate(finish)},
 ];
}
