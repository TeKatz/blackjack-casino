import {symbols} from './bridge';
import type {NineLivesSymbol} from './bridge';

export type NineReelSpeed='normal'|'fast'|'turbo';
export const STOP_MS=[1060,1510,1960] as const;
const SCALE:Record<NineReelSpeed,number>={normal:1,fast:.64,turbo:.38};
const hash=(input:string)=>{
 let h=2166136261;
 for(let i=0;i<input.length;i++)h=Math.imul(h^input.charCodeAt(i),16777619)>>>0;
 return h;
};
export function reelDuration(reel:number,speed:NineReelSpeed):number{
 if(!Number.isInteger(reel)||reel<0||reel>=3)throw new RangeError('Invalid Nine Lives reel');
 return Math.round(STOP_MS[reel]*SCALE[speed]);
}
export function revealDuration(speed:NineReelSpeed):number{
 return reelDuration(2,speed)+Math.round(260*SCALE[speed]);
}
/** Guard tiles prevent overshoot exposing the edge of the strip. */
export function reelStrip(spinId:string,reel:number,visible:readonly NineLivesSymbol[],
 target:readonly NineLivesSymbol[]):NineLivesSymbol[]{
 if(reel<0||reel>=3||visible.length!==3||target.length!==3)throw new RangeError('Invalid strip');
 let seed=hash(spinId+'|'+reel);
 const random=()=>{seed^=seed<<13;seed^=seed>>>17;seed^=seed<<5;return seed>>>0;};
 const filler=Array.from({length:9+reel*4},()=>symbols[random()%symbols.length]);
 return [visible[2],...visible,...filler,...target,target[0]];
}
export function reelFrames(step:number,reel:number):Keyframe[]{
 if(!Number.isFinite(step)||step<=0||reel<0||reel>=3)throw new RangeError('Invalid reel geometry');
 const travel=4+9+4*reel;
 const start=-step;
 const end=-(travel)*step;
 const distance=end-start;
 return [
  {offset:0,transform:`translate3d(0,${start}px,0)`},
  {offset:.055,transform:`translate3d(0,${start+step*.24}px,0)`},
  {offset:.23,transform:`translate3d(0,${start+distance*.34}px,0)`},
  {offset:.69,transform:`translate3d(0,${start+distance*.85}px,0)`},
  {offset:.87,transform:`translate3d(0,${end+step*.38}px,0)`},
  {offset:.96,transform:`translate3d(0,${end-step*.13}px,0)`},
  {offset:1,transform:`translate3d(0,${end}px,0)`},
 ];
}
