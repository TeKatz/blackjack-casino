import {memo,useEffect,useMemo,useRef} from 'react';
import {NineSymbolArt} from './SymbolArt';
import {GalaxyBackdrop} from './GalaxyBackdrop';
import {paylineCells} from './bridge';
import type {NineLivesSpin,NineLivesSymbol} from './bridge';
import {reelDuration,reelFrames,reelStrip} from './reelMotion';
import type {NineReelSpeed} from './reelMotion';

type Props={
 displayGrid:readonly NineLivesSymbol[];
 result:NineLivesSpin|null;
 spinning:readonly number[];
 activeWin:boolean;
 speed:NineReelSpeed;
 reducedMotion:boolean;
 onStop:(id:string,reel:number)=>void;
};
const cellSymbols=(grid:readonly NineLivesSymbol[],reel:number)=>
 [grid[reel],grid[reel+3],grid[reel+6]];

function Column({reel,visible,result,rolling,activeWin,speed,reducedMotion,onStop}:{
 reel:number;visible:readonly NineLivesSymbol[];result:NineLivesSpin|null;
 rolling:boolean;activeWin:boolean;speed:NineReelSpeed;reducedMotion:boolean;
 onStop:(id:string,reel:number)=>void;
}){
 const viewport=useRef<HTMLDivElement>(null),tapeRef=useRef<HTMLDivElement>(null);
 const stopRef=useRef(onStop);stopRef.current=onStop;
 const spinId=result?.id??null;
 const tape=useMemo(()=>spinId&&result?reelStrip(spinId,reel,visible,cellSymbols(result.grid,reel)):[],
   [spinId,reel]);
 const showTape=rolling&&!reducedMotion&&tape.length>0;
 useEffect(()=>{
  if(!showTape||!spinId)return;
  const el=viewport.current,tapeEl=tapeRef.current;
  if(!el||!tapeEl)return;
  let disposed=false,done=false,animation:Animation|null=null;
  let fallback:ReturnType<typeof setTimeout>|null=null;
  const complete=()=>{
   if(disposed||done)return;
   done=true;
   if(fallback!==null)clearTimeout(fallback);
   stopRef.current(spinId,reel);
  };
  const start=()=>{
   if(disposed||done||animation)return;
   const computed=getComputedStyle(el);
   const gap=Number.parseFloat(computed.rowGap)||7;
   const height=(el.clientHeight-gap*2)/3;
   if(height<=0)return;
   tapeEl.style.setProperty('--nine-tile-height',height+'px');
   tapeEl.style.setProperty('--nine-track-gap',gap+'px');
   const duration=reelDuration(reel,speed);
   animation=tapeEl.animate(reelFrames(height+gap,reel),{
    duration,iterations:1,fill:'forwards',easing:'linear',
   });
   animation.onfinish=complete;
   fallback=setTimeout(complete,duration+280);
  };
  const observer=typeof ResizeObserver!=='undefined'?new ResizeObserver(start):null;
  observer?.observe(el);
  start();
  if(!animation)fallback=setTimeout(complete,reelDuration(reel,speed)+280);
  return()=>{
   disposed=true;
   observer?.disconnect();
   if(fallback!==null)clearTimeout(fallback);
   animation?.cancel();
  };
 },[showTape,spinId,reel,speed,tape.length]);
 return <div className={'nine-reel-column'+(showTape?' is-rolling':'')} ref={viewport}
   data-reel={reel} data-reel-state={showTape?'spinning':'settled'}>
   {visible.map((symbol,row)=>{
    const index=row*3+reel;
    return <div key={row} data-symbol={symbol}
      className={'nine-cell'+(activeWin&&result?.winningCells.includes(index)?' winning':'')
       +(activeWin&&result?.fullGridFrenzy?' frenzy':'')
       +(activeWin&&result?.winningCells.length&&!result.winningCells.includes(index)?' dimmed':'')
       +(activeWin&&result?.pounceWildCell===index?' pounced':'')}>
      <GalaxyBackdrop cell={index}/>
      <NineSymbolArt symbol={symbol}/>
     </div>;
   })}
   {showTape&&<div className="nine-reel-viewport" aria-hidden="true">
     <div className="nine-reel-tape" ref={tapeRef}>
       {tape.map((symbol,i)=><div className="nine-tape-tile" key={i} data-symbol={symbol}>
         <GalaxyBackdrop cell={9+reel*64+i} compact/>
         <NineSymbolArt symbol={symbol}/>
       </div>)}
     </div>
   </div>}
 </div>;
}
function ReelStageView({displayGrid,result,spinning,activeWin,speed,reducedMotion,onStop}:Props){
 const lines=activeWin?result?.winningLines??[]:[];
 return <div className="nine-reels" data-testid="nine-lives-reels" data-reel-engine="nine-compositor-v1">
   {[0,1,2].map(reel=><Column key={reel} reel={reel}
     visible={cellSymbols(displayGrid,reel)} result={result}
     rolling={spinning.includes(reel)} activeWin={activeWin}
     speed={speed} reducedMotion={reducedMotion} onStop={onStop}/>)}
   {lines.length>0&&<svg className="nine-lines" viewBox="0 0 300 300"
     preserveAspectRatio="none" aria-hidden="true">
     {lines.map(i=><polyline key={i}
       points={paylineCells[i].map(cell=>((cell%3)*100+50)+','+(Math.floor(cell/3)*100+50)).join(' ')}
       stroke={i<3?'#f9d985':'#d48aff'} strokeWidth="3" strokeLinecap="round" fill="none"/>)}
   </svg>}
 </div>;
}
/** Bet and settings updates do not restart or recompose the reel animation. */
export const ReelStage=memo(ReelStageView,(a,b)=>
 a.displayGrid===b.displayGrid&&a.result?.id===b.result?.id
 &&a.spinning===b.spinning&&a.activeWin===b.activeWin
 &&a.speed===b.speed&&a.reducedMotion===b.reducedMotion&&a.onStop===b.onStop);
