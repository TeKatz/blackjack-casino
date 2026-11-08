import {memo,useEffect,useMemo,useRef} from 'react';
import type {CSSProperties} from 'react';
import type {NeonSpin,NeonSymbol} from './bridge';
import {SymbolArt} from './Symbols';
import {reelStopDuration,reelKeyframes,reelVisualStrip} from './reelMotion';
import type {ReelSpeed} from './reelMotion';
import type {WinTrace} from './winTraces';

type CellState={winner:boolean;muted:boolean;converted:boolean;override:boolean};
type Props={
 displayGrid:readonly NeonSymbol[];
 result:NeonSpin|null;
 rolling:readonly number[];
 finished:boolean;
 reducedMotion:boolean;
 winning:ReadonlySet<number>;
 speed?:ReelSpeed;
 traces?:readonly WinTrace[];
 onStop:(spinId:string,reel:number)=>void;
};
const ROWS=4,REELS=5;
function reelCells(grid:readonly NeonSymbol[],reel:number):NeonSymbol[]{
 return Array.from({length:ROWS},(_,row)=>grid[row*REELS+reel]);
}
function Cell({symbol,state}: {symbol:NeonSymbol;state:CellState}){
 return <div data-symbol={symbol} className={'vault-cell'+(state.winner?' winner':'')
   +(state.muted?' muted':'')+(state.converted?' converted':'')}>
   <SymbolArt symbol={symbol} animate={!state.muted} emphasis={state.winner}/>
   {state.override&&<span className="vault-cell-tag">OVERRIDE</span>}
 </div>;
}
/** One compositor-driven physical strip per reel; the outcome is never generated in React. */
function ReelColumn({reel,visible,spin,rolling,reducedMotion,finished,winning,speed,onStop}:{
 reel:number;visible:NeonSymbol[];spin:NeonSpin|null;rolling:boolean;
 reducedMotion:boolean;finished:boolean;winning:ReadonlySet<number>;speed:ReelSpeed;
 onStop:(spinId:string,reel:number)=>void;
}){
 const frameRef=useRef<HTMLDivElement>(null);
 const tapeRef=useRef<HTMLDivElement>(null);
 const onStopRef=useRef(onStop);
 onStopRef.current=onStop;
 const spinId=spin?.id??null;
 const target=spin?reelCells(spin.baseGrid,reel):null;
 // While another reel settles only its displayed cells change. The native
 // result and strip identity stay fixed for the lifetime of this animation.
 const tape=useMemo(()=>spinId&&target?
   reelVisualStrip(spinId,reel,visible,target):[],[spinId,reel]);
 useEffect(()=>{
  if(!rolling||!spinId||reducedMotion)return;
  const viewport=frameRef.current,tapeEl=tapeRef.current;
  if(!viewport||!tapeEl)return;
  let disposed=false,finishedOnce=false;
  let motion:Animation|null=null;
  let fallback:ReturnType<typeof setTimeout>|null=null;
  const complete=()=>{
   if(disposed||finishedOnce)return;
   finishedOnce=true;
   if(fallback!==null)clearTimeout(fallback);
   onStopRef.current(spinId,reel);
  };
  const start=()=>{
   if(disposed||finishedOnce||motion)return;
   const rowGap=Number.parseFloat(getComputedStyle(viewport).getPropertyValue('--vault-cell-gap'))||5;
   const height=(viewport.clientHeight-rowGap*(ROWS-1))/ROWS;
   if(height<=0)return;
   const step=height+rowGap;
   tapeEl.style.setProperty('--vault-reel-cell-height',height+'px');
   tapeEl.style.setProperty('--vault-cell-gap',rowGap+'px');
   // Guard rows before and after the four visible cells prevent clipping on
   // initial recoil and during the spring-like final overshoot.
   motion=tapeEl.animate(reelKeyframes(step,tape.length),{
     duration:reelStopDuration(reel,speed),iterations:1,fill:'forwards',
   });
   motion.onfinish=complete;
   fallback=setTimeout(complete,reelStopDuration(reel,speed)+320);
  };
  // Layout may not have produced measurable cells on the first React commit.
  const observer=typeof ResizeObserver!=='undefined'?new ResizeObserver(()=>{
   if(!motion)start();
  }):null;
  observer?.observe(viewport);
  start();
  if(!motion)fallback=setTimeout(complete,reelStopDuration(reel,speed)+320);
  return()=>{
   disposed=true;
   observer?.disconnect();
   if(fallback!==null)clearTimeout(fallback);
   if(motion)motion.cancel();
  };
 },[spinId,reel,rolling,reducedMotion,tape.length,speed]);
 const showTape=rolling&&!reducedMotion&&tape.length>0;
 return <div className={'vault-reel-column'+(showTape?' is-rolling':'')}
   data-reel={reel} data-reel-state={showTape?'spinning':'settled'} ref={frameRef}>
   <div className="vault-reel-static">
     {visible.map((symbol,row)=>{
       const index=row*REELS+reel;
       const highlight=finished&&(
         winning.has(index)||!!spin?.expandedReels.includes(reel)
         ||!!spin?.blackoutWildCells.includes(index)||spin?.securityOverrideReel===reel);
       return <Cell key={row} symbol={symbol} state={{
         winner:highlight,
         muted:finished&&winning.size>0&&!highlight,
         converted:finished&&!!spin?.blackoutWildCells.includes(index),
         override:finished&&spin?.securityOverrideReel===reel,
       }}/>;
     })}
   </div>
   {showTape&&<div className="vault-reel-viewport" aria-hidden="true">
     <div className="vault-reel-tape" ref={tapeRef}>
       {tape.map((symbol,index)=><div className="vault-reel-tile" key={index} data-symbol={symbol}>
         <SymbolArt symbol={symbol} compact animate={false}/>
       </div>)}
     </div>
   </div>}
   <div className="vault-reel-glass" aria-hidden="true"/>
 </div>;
}
function ReelStageView({displayGrid,result,rolling,finished,reducedMotion,winning,onStop,
 speed='normal',traces=[]}:Props){
 return <div className="vault-grid" data-reel-engine="compositor-strips-v2"
   data-win-trace-count={traces.length}>
   {Array.from({length:REELS},(_,reel)=><ReelColumn key={reel} reel={reel}
     visible={reelCells(displayGrid,reel)} spin={result}
     rolling={rolling.includes(reel)} reducedMotion={reducedMotion}
     finished={finished} winning={winning} speed={speed} onStop={onStop}/>)}
   {traces.length>0&&<svg className="vault-win-traces" viewBox="0 0 1000 800"
     preserveAspectRatio="none" aria-hidden="true">
     {traces.map(trace=>{
       const points=trace.cells.map(index=>[100+(index%5)*200,100+Math.floor(index/5)*200]);
       const path=points.map(([x,y],index)=>(index===0?'M':'L')+x+' '+y).join(' ');
       return <g key={trace.symbol}>
         <path className="vault-win-trace-halo" d={path}/>
         <path className="vault-win-trace-line" d={path}/>
         {points.map(([x,y],index)=><circle key={index} className="vault-win-trace-node" cx={x} cy={y} r="11"/>)}
       </g>;
     })}
   </svg>}
 </div>;
}
export const ReelStage=memo(ReelStageView,(a,b)=>a.displayGrid===b.displayGrid
 &&a.result?.id===b.result?.id&&a.rolling===b.rolling&&a.finished===b.finished
 &&a.reducedMotion===b.reducedMotion&&a.winning===b.winning
 &&a.speed===b.speed&&a.traces===b.traces&&a.onStop===b.onStop);
