import {useEffect,useRef,useState} from 'react';
import {embeddedLobby} from './lobbyBridge';
import type {LobbyMode} from './lobbyBridge';
import {RouletteWheel} from './RouletteWheel';
import {color,covered,insideKey,LIMIT,randomNumber,settlePreview} from './rouletteModel';
import type {Bet,Receipt} from './rouletteModel';
import './roulette.css';
export function rouletteLobbyMode(language:'de'|'en'):LobbyMode{return {id:'roulette',title:'Roulette',category:'Table games',description:language==='de'?'Der große Kessel. Europäisches Roulette, hohe Einsätze und 37 Möglichkeiten.':'The grand wheel. European roulette, high stakes and 37 possibilities.',enabled:true,secondary:false};}
const CHIPS=[100,500,1000,5000,10000,25000,50000,100000];
const outside=['low','even','red','black','odd','high'];
const labels={de:{low:'1–18',even:'GERADE',red:'ROT',black:'SCHWARZ',odd:'UNGERADE',high:'19–36'},en:{low:'1–18',even:'EVEN',red:'RED',black:'BLACK',odd:'ODD',high:'19–36'}};
export function RouletteTable({language,bankroll,bankrollValue,motion,sound,onBack}:{language:'de'|'en';bankroll:string;bankrollValue?:number;motion:boolean;sound:boolean;onBack:()=>void}){
 const de=language==='de',fmt=(v:number)=>new Intl.NumberFormat(de?'de-DE':'en-US',{style:'currency',currency:'USD',maximumFractionDigits:0}).format(v);
 const [demo,setDemo]=useState(100000);
 const cash=embeddedLobby?(bankrollValue??0):demo;
 const [chip,setChip]=useState(100),[bets,setBets]=useState<Bet[]>([]),[undo,setUndo]=useState<Bet[][]>([]),[previous,setPrevious]=useState<Bet[]>([]);
 const [pending,setPending]=useState(false),[result,setResult]=useState<Extract<Receipt,{success:true}>|null>(null),[revealed,setRevealed]=useState(false),[history,setHistory]=useState<number[]>([]),[notice,setNotice]=useState('');
 const [inside,setInside]=useState(false),[selection,setSelection]=useState<number[]>([]);
 const request=useRef<{roundId:string;bets:Bet[]}|null>(null),lock=useRef(false),audio=useRef<AudioContext|null>(null);
 const [retry,setRetry]=useState(false);
 const total=bets.reduce((s,b)=>s+b.amount,0);
 function tone(freq:number,duration=.07){if(!sound)return;try{const ctx=audio.current??(audio.current=new AudioContext());void ctx.resume();const osc=ctx.createOscillator(),g=ctx.createGain();osc.type='sine';osc.frequency.value=freq;g.gain.setValueAtTime(.035,ctx.currentTime);g.gain.exponentialRampToValueAtTime(.001,ctx.currentTime+duration);osc.connect(g);g.connect(ctx.destination);osc.start();osc.stop(ctx.currentTime+duration);}catch{/* optional sound */}}
 useEffect(()=>()=>{void audio.current?.close()},[]);
 function accept(receipt:Receipt){
  if(!request.current||receipt.roundId!==request.current.roundId)return;
  request.current=null;setRetry(false);
  if(!receipt.success){lock.current=false;setPending(false);setNotice(receipt.message);return;}
  setResult(receipt);setRevealed(false);if(!embeddedLobby)setDemo(receipt.bankroll);
 }
 useEffect(()=>{
  const host=window.chrome?.webview;if(!host)return;
  const listener=(event:MessageEvent)=>{const m=event.data;if(m?.type!=='lobby.roulette.result')return;const d=m.data;
   if(typeof d?.roundId!=='string'||typeof d.success!=='boolean')return;
   if(d.success&&(!Number.isInteger(d.number)||d.number<0||d.number>36||!['stake','return','net','bankroll'].every(k=>typeof d[k]==='number'&&Number.isFinite(d[k]))))return;
   if(!d.success&&typeof d.message!=='string')return;accept(d as Receipt);
  };host.addEventListener('message',listener);return()=>host.removeEventListener('message',listener);
 },[]);
 useEffect(()=>{if(!pending||result)return;const t=setTimeout(()=>setRetry(true),7000);return()=>clearTimeout(t)},[pending,result]);
 function change(next:Bet[]){setUndo(old=>[...old,bets]);setBets(next);setNotice('');setRevealed(false);setResult(null);}
 function place(key:string){if(lock.current)return;if(total+chip>Math.min(cash,LIMIT)){setNotice(de?'Guthaben oder Tischlimit erreicht.':'Bankroll or table limit reached.');return;}
  const old=bets.find(b=>b.key===key);change(old?bets.map(b=>b.key===key?{...b,amount:b.amount+chip}:b):[...bets,{key,amount:chip}]);tone(520);}
 function pick(n:number){if(inside){setSelection(old=>old.includes(n)?old.filter(x=>x!==n):old.length<6?[...old,n]:old);return;}place('n'+n)}
 function send(){if(request.current)window.chrome?.webview?.postMessage({type:'lobby.roulette.spin',...request.current});}
 function spin(){if(lock.current||!total||total>cash||total>LIMIT)return;lock.current=true;setPending(true);setResult(null);setRevealed(false);setNotice('');setRetry(false);setPrevious(bets.map(b=>({...b})));const roundId=crypto.randomUUID();request.current={roundId,bets};tone(260,.2);
  if(embeddedLobby)send();else accept(settlePreview(roundId,bets,cash,randomNumber()));
 }
 function finish(){if(!result||!lock.current)return;lock.current=false;setPending(false);setRevealed(true);setHistory(old=>[result.number,...old].slice(0,16));setBets([]);setUndo([]);setSelection([]);tone(result.net>0?880:330,.25);}
 const key=insideKey(selection);
 function betButton(id:string,label:string,cls=''){const amount=bets.find(b=>b.key===id)?.amount;const won=revealed&&result&&covered(id).includes(result.number);return <button key={id} type="button" className={'roulette-cell '+cls+(won?' winning':'')} disabled={pending} onClick={()=>place(id)} aria-label={label+(amount?' '+fmt(amount):'')}><span>{label}</span>{amount&&<b className="roulette-placed">{fmt(amount)}</b>}</button>;}
 return <main className="roulette-room">
 <header className="roulette-header"><div><button className="roulette-back" onClick={onBack} disabled={pending}>{de?'← Zur Lobby':'← Lobby'}</button><small>THE MIDNIGHT CLUB · HIGH LIMIT</small><h1>Roulette</h1><p>{de?'Europäischer Tisch · 0–36':'European table · 0–36'}</p></div><div className="roulette-bank"><small>{de?'SPIELGELD':'PLAY MONEY'}</small><strong>{embeddedLobby?bankroll:fmt(demo)}</strong><span>{de?'Tischlimit':'Table limit'} {fmt(LIMIT)}</span></div></header>
 <div className="roulette-stage"><section className="roulette-wheel-panel"><div className="roulette-wheel-top"><small>GRAND ROULETTE</small><span>{pending?(de?'KEINE EINSÄTZE MEHR':'NO MORE BETS'):(de?'EINSÄTZE BITTE':'PLACE YOUR BETS')}</span></div>
 <RouletteWheel number={result?.number??null} spinId={result?.roundId??''} motion={motion} onDone={finish}/>
 <div className={'roulette-result '+(revealed&&result?color(result.number):'')} aria-live="polite">{revealed&&result?<><strong>{result.number}</strong><span>{result.net>=0?'+':''}{fmt(result.net)} {de?'Netto':'net'}</span></>:<span>{pending?(de?'Der Kessel dreht sich …':'The wheel is spinning …'):(de?'Setze deine Jetons':'Place your chips')}</span>}</div>
 <div className="roulette-history" aria-label={de?'Letzte Ergebnisse':'Recent results'}>{history.map((n,i)=><span key={i} className={color(n)}>{n}</span>)}</div></section>
 <section className="roulette-felt"><div className="roulette-felt-title"><div><small>EUROPEAN · SINGLE ZERO</small><h2>{de?'Der Tisch':'The table'}</h2></div><span>{de?'Einsatz':'Stake'} <b>{fmt(total)}</b></span></div>
 <div className="roulette-board"><button className={'roulette-cell green roulette-zero'+(selection.includes(0)?' selected':'')+(revealed&&result?.number===0?' winning':'')} disabled={pending} onClick={()=>pick(0)}>0{bets.find(b=>b.key==='n0')&&<b className="roulette-placed">{fmt(bets.find(b=>b.key==='n0')!.amount)}</b>}</button>
 <div className="roulette-numbers">{[3,2,1].flatMap(row=>Array.from({length:12},(_,i)=>{const n=i*3+row,amount=bets.find(b=>b.key==='n'+n)?.amount;return <button type="button" key={n} className={'roulette-cell '+color(n)+(selection.includes(n)?' selected':'')+(revealed&&result?.number===n?' winning':'')} disabled={pending} onClick={()=>pick(n)} aria-label={String(n)+(amount?' '+fmt(amount):'')}><span>{n}</span>{amount&&<b className="roulette-placed">{fmt(amount)}</b>}</button>;}))}</div>
 <div className="roulette-columns">{[3,2,1].map(n=>betButton('c'+n,'2:1'))}</div>
 <div className="roulette-dozens">{[1,2,3].map(n=>betButton('d'+n,de?n+'. Dutzend':n===1?'1st 12':n===2?'2nd 12':'3rd 12'))}</div>
 <div className="roulette-outside">{outside.map(id=>betButton(id,labels[language][id as keyof typeof labels.en],id==='red'||id==='black'?id:''))}</div></div>
 <div className="roulette-inside"><button className={inside?'active':''} disabled={pending} onClick={()=>{setInside(!inside);setSelection([])}}>{de?'Kombinationswetten':'Inside combinations'}</button>{inside&&<><span>{selection.length?selection.join(' · '):de?'2, 3, 4 oder 6 benachbarte Zahlen wählen':'Select 2, 3, 4 or 6 adjacent numbers'}</span><button disabled={!key||pending} onClick={()=>{if(key){place(key);setSelection([])}}}>{de?'Setzen':'Place'} {key?36/covered(key).length-1+':1':''}</button></>}</div>
 <div className="roulette-bets">{bets.map(b=><span key={b.key}>{b.key[0]==='n'?b.key.slice(1):b.key[0]==='i'?b.key.slice(1):b.key.toUpperCase()} <b>{fmt(b.amount)}</b></span>)}</div>
 <div className="roulette-chips">{CHIPS.map(c=><button type="button" key={c} className={'roulette-chip '+(chip===c?'active':'')} aria-pressed={chip===c} disabled={pending} onClick={()=>{setChip(c);tone(440)}} style={{'--chip-hue':String(CHIPS.indexOf(c)*47)} as React.CSSProperties}>{c>=1000?c/1000+'K':c}</button>)}</div>
 <div className="roulette-controls"><button disabled={pending||!undo.length} onClick={()=>{setBets(undo[undo.length-1]);setUndo(undo.slice(0,-1));setNotice('')}}>{de?'Rückgängig':'Undo'}</button><button disabled={pending||!total} onClick={()=>change([])}>{de?'Leeren':'Clear'}</button><button disabled={pending||!previous.length||previous.reduce((s,b)=>s+b.amount,0)>Math.min(cash,LIMIT)} onClick={()=>change(previous.map(b=>({...b})))}>{de?'Wiederholen':'Rebet'}</button><button disabled={pending||!total||total*2>Math.min(cash,LIMIT)} onClick={()=>change(bets.map(b=>({...b,amount:b.amount*2})))}>×2</button><button className="roulette-spin" disabled={pending||!total||total>cash||total>LIMIT} onClick={spin}>{pending?(de?'DREHT':'SPINNING'):'SPIN'} <span>↻</span></button></div>
 {retry&&pending&&!result&&<button onClick={()=>{send();setRetry(false);setTimeout(()=>{if(request.current)setRetry(true)},7000)}}>{de?'Ergebnis erneut abrufen':'Retry result'}</button>}
 <p className="roulette-notice" role="status">{notice|| (de?'Einzelzahl 35:1 · Dutzend/Spalte 2:1 · Einfache Chancen 1:1':'Straight 35:1 · Dozens/columns 2:1 · Even money 1:1')}</p>
 </section></div>{!embeddedLobby&&<p className="roulette-demo">{de?'Browser-Demo · getrenntes Testguthaben':'Browser demo · separate test bankroll'}</p>}</main>;
}
