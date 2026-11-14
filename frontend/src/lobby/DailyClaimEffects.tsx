import {useEffect,useRef,useState} from 'react';
import type {DailyLoginSnapshot} from './lobbyBridge';

export type PendingDailyClaim={totalClaims:number;day:number;bankrollValue:number|null};
export type DailyClaimReceipt={id:number;day:number;before:number|null;after:number|null};

export function isConfirmedDailyClaim(pending:PendingDailyClaim|null,daily:DailyLoginSnapshot) {
  return pending!==null && daily.totalClaims>pending.totalClaims &&
    daily.claimedToday && !daily.canClaim;
}

/** Only the authoritative host snapshot can trigger this non-interactive effect. */
export function DailyClaimFlight({receipt,enabled}:{
  receipt:DailyClaimReceipt|null;enabled:boolean;
}) {
  const overlay=useRef<HTMLDivElement>(null);
  useEffect(()=>{
    const layer=overlay.current;
    if(!receipt||!enabled||!layer)return;
    const source=document.querySelector<HTMLElement>('[data-daily-reward-day="'+receipt.day+'"]');
    const target=document.querySelector<HTMLElement>('[data-bankroll-target]');
    if(!source||!target||!source.getClientRects().length||!target.getClientRects().length)return;
    const a=source.getBoundingClientRect(),b=target.getBoundingClientRect();
    const x=a.left+a.width*.5,y=a.top+a.height*.5;
    const dx=b.left+b.width*.5-x,dy=b.top+b.height*.5-y;
    const animations:Animation[]=[];
    const nodes:HTMLElement[]=[];
    for(let i=0;i<9;i++){
      const coin=document.createElement('span');
      coin.className='lobby-flight-coin';
      coin.style.left=x+'px';
      coin.style.top=y+'px';
      coin.style.setProperty('--coin-size',i%3===0?'16px':'12px');
      layer.appendChild(coin);
      nodes.push(coin);
      const spread=(i-4)*10;
      animations.push(coin.animate([
        {offset:0,opacity:0,transform:'translate3d(0,0,0) scale(.35)'},
        {offset:.19,opacity:1,transform:'translate3d('+spread+'px,-30px,0) scale(1)'},
        {offset:.55,opacity:1,transform:'translate3d('+(dx*.46+spread*.7)+'px,'+(dy*.42-38)+'px,0) scale(.85)'},
        {offset:1,opacity:0,transform:'translate3d('+dx+'px,'+dy+'px,0) scale(.25)'},
      ],{duration:710+i*24,delay:i*26,easing:'cubic-bezier(.3,.5,.2,1)',fill:'forwards'}));
    }
    animations.push(source.animate([
      {boxShadow:'inset 0 0 0 #f5d38b00'},
      {boxShadow:'inset 0 0 24px #f5d38b99'},
      {boxShadow:'inset 0 0 0 #f5d38b00'},
    ],{duration:650,easing:'ease-out'}));
    animations.push(target.animate([
      {filter:'brightness(1)'},
      {filter:'brightness(1.65)'},
      {filter:'brightness(1)'},
    ],{duration:480,delay:690,easing:'ease-out'}));
    void Promise.all(animations.map(a=>a.finished.catch(()=>undefined))).then(()=>{
      for(const node of nodes)node.remove();
    });
    return ()=>{
      for(const animation of animations)animation.cancel();
      for(const node of nodes)node.remove();
    };
  },[receipt,enabled]);
  return <div ref={overlay} className="lobby-claim-flight" aria-hidden="true"/>;
}

/** The value remains native-owned; this only interpolates confirmed changes. */
export function BankrollCounter({value,numeric,receipt,enabled}:{
  value:string;numeric?:number;receipt:DailyClaimReceipt|null;enabled:boolean;
}) {
  const [display,setDisplay]=useState(value);
  const lastReceipt=useRef<number>(-1);
  useEffect(()=>{
    if(!receipt||lastReceipt.current===receipt.id||!enabled||
       receipt.before===null||receipt.after===null||receipt.after<=receipt.before){
      setDisplay(value);return;
    }
    lastReceipt.current=receipt.id;
    const before=receipt.before,after=receipt.after;
    let frame=0,timeout=0;
    setDisplay(new Intl.NumberFormat('en-US',{style:'currency',currency:'USD'}).format(before));
    const start=()=>{
      const begin=performance.now();
      const formatter=new Intl.NumberFormat('en-US',{style:'currency',currency:'USD'});
      const step=(time:number)=>{
        const t=Math.min(1,(time-begin)/690);
        const eased=1-Math.pow(1-t,3);
        setDisplay(t===1?value:formatter.format(before+(after-before)*eased));
        if(t<1)frame=requestAnimationFrame(step);
      };
      frame=requestAnimationFrame(step);
    };
    timeout=window.setTimeout(start,350);
    return ()=>{clearTimeout(timeout);cancelAnimationFrame(frame);};
  },[value,numeric,receipt,enabled]);
  return <strong data-bankroll-target="true">{display}</strong>;
}
