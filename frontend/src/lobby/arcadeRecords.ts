import {isPlayableArcade} from './arcadeEngine';
import type {PlayableArcadeId} from './arcadeEngine';

/** Local-only personal arcade records. No bankroll, XP or remote leaderboard writes. */
const PREFIX='midnight-club.arcade.v1.';
export type ArcadeRecord={best:number;last:number;runs:number;bestSeconds:number};
const empty=():ArcadeRecord=>({best:0,last:0,runs:0,bestSeconds:0});
type Store=Pick<Storage,'getItem'|'setItem'>;
const getStore=():Store|null=>{
 try{return typeof window!=='undefined'?window.localStorage:null;}
 catch{return null;}
};
const validNumber=(n:unknown,max:number)=>typeof n==='number'&&Number.isSafeInteger(n)&&n>=0&&n<=max;
export function readArcadeRecord(id:PlayableArcadeId,storage:Store|null=getStore()):ArcadeRecord{
 if(!storage||!isPlayableArcade(id))return empty();
 try{
  const raw=storage.getItem(PREFIX+id);
  if(!raw||raw.length>512)return empty();
  const value:unknown=JSON.parse(raw);
  if(!value||typeof value!=='object'||Array.isArray(value))return empty();
  const r=value as Partial<ArcadeRecord>;
  if(!validNumber(r.best,100000000)||!validNumber(r.last,100000000)||
   !validNumber(r.runs,10000000)||!validNumber(r.bestSeconds,10000000))return empty();
  return {best:r.best!,last:r.last!,runs:r.runs!,bestSeconds:r.bestSeconds!};
 }catch{return empty();}
}
export function saveArcadeRecord(id:PlayableArcadeId,score:number,seconds:number,
 storage:Store|null=getStore()):ArcadeRecord{
 const current=readArcadeRecord(id,storage);
 if(!isPlayableArcade(id)||!validNumber(score,100000000)||!Number.isFinite(seconds)||seconds<0)
  return current;
 const next:ArcadeRecord={
  best:Math.max(current.best,score),
  last:score,
  runs:Math.min(10000000,current.runs+1),
  bestSeconds:Math.max(current.bestSeconds,Math.floor(seconds)),
 };
 try{storage?.setItem(PREFIX+id,JSON.stringify(next));}
 catch{/* Storage may be blocked or full. Never crash an arcade run. */}
 return next;
}
