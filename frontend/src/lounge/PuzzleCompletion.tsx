import {useEffect,useRef} from 'react';
import type {CSSProperties,ReactNode} from 'react';
import './puzzleCompletion.css';

export type CompletionMetric={label:string;value:string|number};
type Props={
 testId:string;kind:'words'|'cards';title:string;subtitle:string;
 metrics:CompletionMetric[];rewardXp:number;seasonXp:number;rewardNotice:string;
 xpLabel:string;seasonLabel:string;viewLabel:string;backLabel:string;
 onView:()=>void;onBack:()=>void;animated?:boolean;children?:ReactNode;
};
export function PuzzleCompletion({testId,kind,title,subtitle,metrics,rewardXp,seasonXp,rewardNotice,
 xpLabel,seasonLabel,viewLabel,backLabel,onView,onBack,animated=true,children}:Props){
 const viewRef=useRef<HTMLButtonElement>(null);
 const backRef=useRef<HTMLButtonElement>(null);
 useEffect(()=>{
  const previous=document.activeElement as HTMLElement|null;
  viewRef.current?.focus();
  const onKey=(event:KeyboardEvent)=>{
   if(event.key==='Escape'){event.preventDefault();event.stopPropagation();onView();}
   if(event.key!=='Tab')return;
   const first=viewRef.current,last=backRef.current;
   if(!first||!last)return;
   if(event.shiftKey&&(document.activeElement===first||!first.parentElement?.contains(document.activeElement))){
    event.preventDefault();last.focus();
   }else if(!event.shiftKey&&document.activeElement===last){
    event.preventDefault();first.focus();
   }
  };
  document.addEventListener('keydown',onKey,true);
  return()=>{document.removeEventListener('keydown',onKey,true);previous?.focus?.();};
 },[onView]);
 return <div className={'puzzle-finish-backdrop '+(animated?'is-animated':'is-static')}
  role="dialog" aria-modal="true" aria-label={title} data-testid={testId}>
  <div className={'puzzle-finish-panel puzzle-finish-'+kind}>
   <div className="puzzle-finish-glow" aria-hidden="true"/>
   <div className="puzzle-finish-celebration" aria-hidden="true">
    {Array.from({length:22},(_,i)=><span key={i} style={{'--i':i} as CSSProperties}/>)}
   </div>
   <div className="puzzle-finish-crest" aria-hidden="true">
    <span>{kind==='words'?'✧':'♠'}</span>
    <i/><i/><i/>
   </div>
   <p className="puzzle-finish-eyebrow">THE MIDNIGHT CLUB · COMPLETED</p>
   <h2>{title}</h2>
   <p className="puzzle-finish-subtitle">{subtitle}</p>
   {children}
   <div className="puzzle-finish-metrics">
    {metrics.map(metric=><div key={metric.label}><small>{metric.label}</small><strong>{metric.value}</strong></div>)}
   </div>
   <div className="puzzle-finish-reward">
    <span className="puzzle-finish-reward-label">✦ &nbsp; PROGRESSION</span>
    <div><span><strong>+{rewardXp}</strong><small>{xpLabel}</small></span>
     <i/><span><strong>+{seasonXp}</strong><small>{seasonLabel}</small></span></div>
   </div>
   {rewardNotice&&<p className="puzzle-finish-receipt" role="status">{rewardNotice}</p>}
   <div className="puzzle-finish-actions">
    <button ref={viewRef} className="puzzle-finish-secondary" type="button" onClick={onView}>{viewLabel}</button>
    <button ref={backRef} className="puzzle-finish-primary" type="button" onClick={onBack}>{backLabel} →</button>
   </div>
  </div>
 </div>;
}
