import {memo,useLayoutEffect,useMemo,useRef} from 'react';
import {RubySymbolArt} from './SymbolArt';
import {baseReelDuration,baseReelTape,bonusCellDuration,bonusCellTape,bonusKeyframes,reelKeyframes} from './reelMotion';
import type {SymbolId} from './bridge';

type Props={
 displayGrid:readonly SymbolId[];
 targetBase:readonly SymbolId[]|null;
 targetBonus:readonly SymbolId[]|null;
 spinId:string|null;
 rollingReels:readonly number[];
 rollingCells:readonly number[];
 heldCells:readonly number[];
 winningLines:readonly number[];
 showWins:boolean;
 anticipation:boolean;
 reducedMotion:boolean;
 onBaseStop:(id:string,reel:number)=>void;
 onBonusStop:(id:string,cell:number)=>void;
};

const getColumn=(grid:readonly SymbolId[],reel:number)=>
 [grid[reel],grid[reel+5],grid[reel+10]];

const lineCells=[
 [0,1,2,3,4],[5,6,7,8,9],[10,11,12,13,14],
 [0,6,12,8,4],[10,6,2,8,14],[0,1,7,3,4],
 [10,11,7,13,14],[5,1,2,3,9],[5,11,12,13,9],[0,6,7,8,14],
] as const;

const label=(symbol:SymbolId)=>symbol==='Elephant'?'ELEPHANT':symbol==='Ruby'?'WILD ×2':
 symbol==='Empty'?'':undefined;

const ReelCell=memo(function ReelCell({cell,symbol,held,winning,rolling,target,spinId,
 reducedMotion,onStop}:{cell:number;symbol:SymbolId;held:boolean;winning:boolean;
 rolling:boolean;target:SymbolId|null;spinId:string|null;reducedMotion:boolean;
 onStop:(id:string,cell:number)=>void}){
 const element=useRef<HTMLDivElement>(null),track=useRef<HTMLDivElement>(null);
 const stop=useRef(onStop);stop.current=onStop;
 const tape=useMemo(()=>rolling&&target&&spinId
  ?bonusCellTape(spinId,cell,symbol,target):[],
  [rolling,target,spinId,cell,symbol]);
 const active=rolling&&!reducedMotion&&tape.length>0;
 useLayoutEffect(()=>{
  if(!active||!spinId)return;
  const viewport=element.current,rail=track.current;
  if(!viewport||!rail)return;
  let closed=false,finished=false,animation:Animation|null=null;
  let fallback:ReturnType<typeof setTimeout>|null=null;
  const finish=()=>{
   if(closed||finished)return;
   finished=true;
   if(fallback!==null)clearTimeout(fallback);
   stop.current(spinId,cell);
  };
  const duration=bonusCellDuration(cell);
  const start=()=>{
   if(closed||finished||animation)return;
   const height=viewport.clientHeight;
   if(height<=0)return;
   rail.style.setProperty('--ruby-bonus-tile-height',height+'px');
   rail.style.visibility='visible';
   rail.style.transform='translate3d(0,0,0)';
   animation=rail.animate(bonusKeyframes(height,tape.length),{
    duration,iterations:1,fill:'forwards',easing:'linear',
   });
   viewport.dataset.tapeReady='true';
   animation.onfinish=finish;
   fallback=setTimeout(finish,duration+240);
  };
  const observer=typeof ResizeObserver==='undefined'?null:new ResizeObserver(start);
  observer?.observe(viewport);start();
  if(!animation)fallback=setTimeout(finish,duration+350);
  return()=>{
   closed=true;observer?.disconnect();
   if(fallback!==null)clearTimeout(fallback);
   if(viewport)delete viewport.dataset.tapeReady;
   animation?.cancel();
  };
 },[active,spinId,cell,tape.length]);
 return <div ref={element} className={'ruby-cell s-'+symbol.toLowerCase()+
   (held?' held':'')+(winning?' winning':'')+(active?' is-respinning':'')}
   data-index={cell} data-symbol={symbol} data-held={held?'true':'false'}>
   <div className="ruby-symbol-settled"><RubySymbolArt symbol={symbol}/>
    {label(symbol)&&<small>{label(symbol)}</small>}</div>
   <span className="ruby-cell-sheen" aria-hidden="true"/>
   {active&&<div className="ruby-cell-respin-viewport" aria-hidden="true">
     <div className="ruby-cell-respin-tape" ref={track}>
      {tape.map((item,i)=><div key={i} className="ruby-bonus-tile" data-symbol={item}>
       <RubySymbolArt symbol={item}/></div>)}
     </div>
   </div>}
   {held&&<span className="ruby-held-corners" aria-hidden="true"/>}
 </div>;
});

function ReelColumn({reel,displayGrid,baseGrid,bonusGrid,spinId,rolling,rollingCells,heldCells,
 winningCells,reducedMotion,onBaseStop,onBonusStop}:{reel:number;
 displayGrid:readonly SymbolId[];baseGrid:readonly SymbolId[]|null;
 bonusGrid:readonly SymbolId[]|null;spinId:string|null;rolling:boolean;
 rollingCells:readonly number[];heldCells:readonly number[];winningCells:ReadonlySet<number>;
 reducedMotion:boolean;onBaseStop:(id:string,reel:number)=>void;
 onBonusStop:(id:string,cell:number)=>void}){
 const viewport=useRef<HTMLDivElement>(null),track=useRef<HTMLDivElement>(null);
 const stop=useRef(onBaseStop);stop.current=onBaseStop;
 const visible=useMemo(()=>getColumn(displayGrid,reel),[displayGrid,reel]);
 const destination=useMemo(()=>baseGrid?getColumn(baseGrid,reel):[],[baseGrid,reel]);
 const tape=useMemo(()=>rolling&&spinId&&destination.length===3
  ?baseReelTape(spinId,reel,visible,destination):[],
  [rolling,spinId,reel,visible,destination]);
 const active=rolling&&!reducedMotion&&tape.length>0;
 useLayoutEffect(()=>{
  if(!active||!spinId)return;
  const element=viewport.current,rail=track.current;
  if(!element||!rail)return;
  let closed=false,finished=false,animation:Animation|null=null;
  let fallback:ReturnType<typeof setTimeout>|null=null;
  const finish=()=>{
   if(closed||finished)return;
   finished=true;
   if(fallback!==null)clearTimeout(fallback);
   stop.current(spinId,reel);
  };
  const duration=baseReelDuration(reel);
  const start=()=>{
   if(closed||finished||animation)return;
   const gap=Number.parseFloat(getComputedStyle(element).rowGap)||5;
   const tile=(element.clientHeight-gap*2)/3;
   if(tile<=0)return;
   rail.style.setProperty('--ruby-reel-tile-height',tile+'px');
   rail.style.setProperty('--ruby-reel-track-gap',gap+'px');
   rail.style.transform=`translate3d(0,${-(tile+gap)}px,0)`;
   rail.style.visibility='visible';
   animation=rail.animate(reelKeyframes(tile+gap,tape.length),{
    duration,iterations:1,fill:'forwards',easing:'linear',
   });
   element.dataset.tapeReady='true';
   animation.onfinish=finish;
   fallback=setTimeout(finish,duration+250);
  };
  const observer=typeof ResizeObserver==='undefined'?null:new ResizeObserver(start);
  observer?.observe(element);start();
  if(!animation)fallback=setTimeout(finish,duration+350);
  return()=>{
   closed=true;observer?.disconnect();
   if(fallback!==null)clearTimeout(fallback);
   if(element)delete element.dataset.tapeReady;
   animation?.cancel();
  };
 },[active,reel,spinId,tape.length]);
 return <div className={'ruby-reel-column'+(active?' rolling':'')} data-reel={reel} ref={viewport}>
  {visible.map((symbol,row)=>{
   const cell=row*5+reel;
   return <ReelCell key={row} cell={cell} symbol={symbol}
    held={heldCells.includes(cell)} winning={winningCells.has(cell)}
    rolling={rollingCells.includes(cell)} target={bonusGrid?.[cell]??null}
    spinId={spinId} reducedMotion={reducedMotion} onStop={onBonusStop}/>;
  })}
  {active&&<div className="ruby-reel-viewport" aria-hidden="true">
    <div className="ruby-reel-tape" ref={track}>
     {tape.map((symbol,i)=><div className="ruby-tape-cell" key={i} data-symbol={symbol}>
      <RubySymbolArt symbol={symbol}/>
     </div>)}
    </div>
   </div>}
 </div>;
}

function RubyReelStageView({displayGrid,targetBase,targetBonus,spinId,rollingReels,
 rollingCells,heldCells,winningLines,showWins,anticipation,reducedMotion,onBaseStop,onBonusStop}:Props){
 const winners=useMemo(()=>{
  const cells=new Set<number>();
  if(showWins)for(const line of winningLines){
   for(const index of lineCells[line]??[])cells.add(index);
  }
  return cells;
 },[winningLines,showWins]);
 return <div className={'ruby-reels'+(anticipation?' anticipating':'')+
   (showWins&&winningLines.length?' is-winning':'')} data-testid="indian-ruby-reels"
   data-reel-engine="five-reel-compositor" aria-label={displayGrid.join(', ')} role="img">
  <div className="ruby-reels-surface">
   {Array.from({length:5},(_,reel)=><ReelColumn key={reel} reel={reel}
    displayGrid={displayGrid} baseGrid={targetBase} bonusGrid={targetBonus}
    spinId={spinId} rolling={rollingReels.includes(reel)}
    rollingCells={rollingCells} heldCells={heldCells} winningCells={winners}
    reducedMotion={reducedMotion} onBaseStop={onBaseStop} onBonusStop={onBonusStop}/>)}
  </div>
  {showWins&&winningLines.length>0&&<svg className="ruby-winning-lines"
   viewBox="0 0 500 300" preserveAspectRatio="none" aria-hidden="true">
   {winningLines.map((line,i)=><polyline key={line} fill="none"
    points={lineCells[line]?.map(cell=>((cell%5)*100+50)+','+(Math.floor(cell/5)*100+50)).join(' ')??''}
    style={{animationDelay:(i*.18)+'s'}}/>)}
  </svg>}
  <div className="ruby-reel-glass" aria-hidden="true"/>
 </div>;
}
/** The entire reel subtree remains stable through bet, wallet, sound and settings snapshots. */
export const RubyReelStage=memo(RubyReelStageView);
