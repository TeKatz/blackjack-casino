import {symbols} from './bridge';
import type {SymbolId} from './bridge';

export type SunsetSpeed='normal'|'fast'|'turbo';
export const BASE_STOP_MS=[1080,1460,1840,2210,2580] as const;
export const BONUS_START_PAUSE_MS=2450;
export const BONUS_FINISH_PAUSE_MS=500;
const BASE_SPEED:Record<SunsetSpeed,number>={normal:1,fast:.68,turbo:.44};
const BONUS_SPEED:Record<SunsetSpeed,number>={normal:1,fast:.89,turbo:.80};
export function bonusStartPause(speed:SunsetSpeed):number{
 return Math.round(BONUS_START_PAUSE_MS*BONUS_SPEED[speed]);
}
export function bonusFinishPause(speed:SunsetSpeed):number{
 return Math.round(BONUS_FINISH_PAUSE_MS*BONUS_SPEED[speed]);
}
/** Only the real native outcome may qualify: one elephant is one short of the feature. */
export function isElephantNearMiss(grid:readonly SymbolId[]):boolean{
 return grid.length===15&&grid.filter(symbol=>symbol==='Elephant').length===1;
}
const hash=(input:string)=>{
 let n=2166136261;
 for(let i=0;i<input.length;i++)n=Math.imul(n^input.charCodeAt(i),16777619)>>>0;
 return n>>>0;
};
function makeRng(key:string){
 let n=hash(key)||1;
 return ()=>{
  n^=n<<13;n^=n>>>17;n^=n<<5;
  return n>>>0;
 };
}
const draw=(next:()=>number):SymbolId=>symbols[next()%10];
export function baseReelDuration(reel:number,speed:SunsetSpeed='normal',nearMiss=false):number{
 if(!Number.isInteger(reel)||reel<0||reel>=5)throw new RangeError('Invalid reel');
 // The real, unaltered outcome has exactly one elephant. Pause at the final reel.
 const emphasis=nearMiss&&reel===4?730:0;
 return Math.round((BASE_STOP_MS[reel]+emphasis)*BASE_SPEED[speed]);
}
export function bonusCellDuration(cell:number,speed:SunsetSpeed='normal'):number{
 if(!Number.isInteger(cell)||cell<0||cell>=15)throw new RangeError('Invalid cell');
 // Even Turbo cannot rush through the elephant feature.
 return Math.round((1650+(cell%5)*160+Math.floor(cell/5)*120)*BONUS_SPEED[speed]);
}
/** Completely presentation-only deterministic tape; no browser money RNG. */
export function baseReelTape(spinId:string,reel:number,previous:readonly SymbolId[],
 target:readonly SymbolId[]):SymbolId[]{
 if(!spinId||reel<0||reel>=5||previous.length!==3||target.length!==3)
  throw new RangeError('Invalid base tape');
 const next=makeRng(spinId+'|base|'+reel);
 const filler=Array.from({length:9+reel*3},()=>draw(next));
 return [previous[2],...previous,...filler,...target,target[0]];
}
export function bonusCellTape(spinId:string,cell:number,previous:SymbolId,target:SymbolId):SymbolId[]{
 if(!spinId||cell<0||cell>=15)throw new RangeError('Invalid bonus tape');
 const next=makeRng(spinId+'|bonus|'+cell);
 const filler=Array.from({length:6+cell%3},()=>draw(next));
 return [previous,...filler,target,target];
}
export function reelKeyframes(step:number,tapeLength:number):Keyframe[]{
 if(!Number.isFinite(step)||step<=0||!Number.isInteger(tapeLength)||tapeLength<8)
  throw new RangeError('Invalid tape geometry');
 const start=-step;
 const end=-(tapeLength-4)*step;
 const span=end-start;
 return [
  {offset:0,transform:`translate3d(0,${start}px,0)`},
  {offset:.065,transform:`translate3d(0,${start+step*.27}px,0)`},
  {offset:.26,transform:`translate3d(0,${start+span*.35}px,0)`},
  {offset:.76,transform:`translate3d(0,${start+span*.88}px,0)`},
  {offset:.93,transform:`translate3d(0,${end-step*.2}px,0)`},
  {offset:1,transform:`translate3d(0,${end}px,0)`},
 ];
}
export function bonusKeyframes(step:number,tapeLength:number):Keyframe[]{
 if(!Number.isFinite(step)||step<=0||!Number.isInteger(tapeLength)||tapeLength<4)
  throw new RangeError('Invalid tape geometry');
 const end=-(tapeLength-2)*step;
 return [
  {offset:0,transform:'translate3d(0,0px,0)'},
  {offset:.76,transform:`translate3d(0,${end+step*.42}px,0)`},
  {offset:.93,transform:`translate3d(0,${end-step*.10}px,0)`},
  {offset:1,transform:`translate3d(0,${end}px,0)`},
 ];
}
