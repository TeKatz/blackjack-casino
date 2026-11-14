import {memo,useEffect,useRef,useState} from 'react';
type Props={value:string;kind:'money'|'integer';spinId:string|null;revealedId:string|null;
 enabled:boolean;duration?:number;};
const renderValue=(v:string,kind:'money'|'integer')=>{
 const n=Number(v);
 if(!Number.isFinite(n))return v;
 return (kind==='money'?'$':'')+n.toLocaleString('en-US',
  kind==='money'?{minimumFractionDigits:2,maximumFractionDigits:2}:{maximumFractionDigits:0});
};
/** Only interpolates display text after a fully received + visually settled spin.
 * Native cents, statistics, bankroll and saved values are never modified. */
export const AnimatedReadout=memo(function AnimatedReadout({
 value,kind,spinId,revealedId,enabled,duration=650}:Props){
 const [shown,setShown]=useState(()=>renderValue(value,kind));
 const shownNumber=useRef(value);
 const rendered=useRef(value);
 const frame=useRef<number|null>(null);
 useEffect(()=>{
  if(frame.current!==null){cancelAnimationFrame(frame.current);frame.current=null;}
  if(spinId!==null&&spinId!==revealedId)return;
  if(rendered.current===value){
   setShown(renderValue(value,kind));return;
  }
  const from=Number(shownNumber.current),to=Number(value);
  rendered.current=value;
  if(!enabled||!Number.isFinite(from)||!Number.isFinite(to)||Math.abs(to-from)<.005
    ||Math.abs(from)>1e14||Math.abs(to)>1e14){
   shownNumber.current=value;setShown(renderValue(value,kind));return;
  }
  const start=performance.now();
  const step=(now:number)=>{
   const t=Math.min(1,(now-start)/duration);
   const ease=1-Math.pow(1-t,3);
   const amount=from+(to-from)*ease;
   shownNumber.current=String(amount);
   setShown(t===1?renderValue(value,kind):renderValue(String(amount),kind));
   if(t<1)frame.current=requestAnimationFrame(step);
   else{frame.current=null;shownNumber.current=value;}
  };
  frame.current=requestAnimationFrame(step);
  return()=>{if(frame.current!==null)cancelAnimationFrame(frame.current);frame.current=null;};
 },[value,kind,spinId,revealedId,enabled,duration]);
 return <span className="reels-animated-value" aria-label={renderValue(value,kind)}
   data-testid="reels-animated-value">{shown}</span>;
});
