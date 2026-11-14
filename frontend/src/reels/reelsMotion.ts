import type {ReelsSpin} from './bridge';

export type ReelsSpeed='normal'|'fast'|'turbo';
export type ReelsReaction='win'|'loss'|'push'|'bigwin'|'none';
export const SPEED_PRESETS:Record<ReelsSpeed,readonly [number,number,number]>={
 normal:[1160,1540,1950],
 fast:[640,880,1150],
 turbo:[240,345,480],
};
export const SPEED_OPTIONS:readonly ReelsSpeed[]=['normal','fast','turbo'];
export const AUTO_OPTIONS=[10,25,50] as const;
const STORAGE_KEY='midnight-reels-speed-v1';
export function readReelsSpeed():ReelsSpeed{
 try{
  const v=window.localStorage.getItem(STORAGE_KEY);
  if(v==='normal'||v==='fast'||v==='turbo')return v;
 }catch{/* Local storage may be unavailable in an isolated WebView2 profile. */}
 return 'normal';
}
export function saveReelsSpeed(speed:ReelsSpeed):void{
 try{window.localStorage.setItem(STORAGE_KEY,speed);}catch{/* UI preference is optional. */}
}
/** Visual distance only; authoritative reel stops always come from the C# receipt. */
export function reelTravel(from:number,target:number):number{
 return 32+(target-from+32)%32;
}
export function reactionFromSpin(spin:ReelsSpin|null):ReelsReaction{
 if(!spin)return 'none';
 const net=Number(spin.net);
 if(net>0)return spin.multiplier>=25?'bigwin':'win';
 return net<0?'loss':'push';
}
export function canQueueAutoSpin(args:{
 active:boolean;remaining:number;pending:boolean;animating:boolean;uncertain:boolean;
 canSpin:boolean;latestId:string|null;revealedId:string|null;
}):boolean{
 return args.active&&args.remaining>0&&!args.pending&&!args.animating&&!args.uncertain
  &&args.canSpin&&args.latestId===args.revealedId;
}
