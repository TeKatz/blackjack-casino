import {useEffect,useState} from 'react';
import {color} from './rouletteModel';
import type {Receipt} from './rouletteModel';
type Result=Extract<Receipt,{success:true}>;
export function RouletteSettlement({result,pending,motion,de,format}:{result:Result|null;pending:boolean;motion:boolean;de:boolean;format:(n:number)=>string}){
 const [amount,setAmount]=useState(0);
 useEffect(()=>{if(!result){setAmount(0);return;}if(!motion){setAmount(result.net);return;}let frame=0;const start=performance.now();setAmount(0);const animate=(now:number)=>{const t=Math.min(1,(now-start)/850);setAmount(Math.round(result.net*(1-Math.pow(1-t,3))));if(t<1)frame=requestAnimationFrame(animate);};frame=requestAnimationFrame(animate);return()=>cancelAnimationFrame(frame);},[result,motion]);
 const status=result?(result.net>0?'win':result.net<0?'loss':'even'):pending?'rolling':'idle';
 const label=result?(result.net>0?(de?'GEWONNEN':'WINNER'):result.net<0?(de?'VERLOREN':'LOSS'):(de?'AUSGEGLICHEN':'BREAK EVEN')):pending?(de?'DIE KUGEL ROLLT':'BALL IN MOTION'):(de?'EINSÄTZE BITTE':'PLACE YOUR BETS');
 const signed=(n:number)=>(n>0?'+':'')+format(n);
 return <div className={'roulette-settlement '+status} data-state={status} aria-live="polite" aria-atomic="true">
  <div className="settlement-sheen" aria-hidden="true"/>
  <div className="settlement-top"><span className="settlement-eyebrow"><i/>{label}</span>{result&&<span className={'settlement-pocket '+color(result.number)}>{result.number}<small>{result.number===0?(de?'GRÜN':'GREEN'):color(result.number)==='red'?(de?'ROT':'RED'):(de?'SCHWARZ':'BLACK')}</small></span>}</div>
  <div className="settlement-main">{result?<><strong aria-hidden="true" key={result.roundId}>{signed(motion?amount:result.net)}</strong><span className="roulette-sr-only">{signed(result.net)}</span><small>{de?'NETTOERGEBNIS':'NET RESULT'}</small></>:<><strong>{pending?(de?'Kessel dreht sich …':'Wheel spinning …'):(de?'Setze deine Jetons':'Place your chips')}</strong><small>{pending?(de?'EINSÄTZE GESCHLOSSEN':'BETTING CLOSED'):(de?'WÄHLE EINE ZAHL ODER CHANCE':'SELECT A NUMBER OR CHANCE')}</small></>}</div>
  {result?<div className="settlement-details"><span>{de?'Einsatz':'Stake'} <b>{format(result.stake)}</b></span><span>{de?'Auszahlung':'Return'} <b>{format(result.return)}</b></span></div>:<div className="settlement-track" aria-hidden="true"><span/></div>}
  {status==='win'&&motion&&<div className="settlement-sparks" aria-hidden="true">{Array.from({length:12},(_,i)=><i key={i} style={{'--spark-i':i} as React.CSSProperties}/>)}</div>}
 </div>;
}
