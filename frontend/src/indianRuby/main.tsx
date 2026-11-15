import {useCallback,useEffect,useRef,useState} from 'react';
import {createRoot} from 'react-dom/client';
import {finishModeBoot} from '../boot/finishModeBoot';
import {onRubyMessage,sendRuby} from './bridge';
import type {RubyIntent,RubySnapshot,RubySpin,SymbolId} from './bridge';
import './indianRuby.css';

const EMPTY:SymbolId[]=['Queen','Vase','Ace','Necklace','Ten','Elephant','Jack','Spices','King','Queen','Ruby','Ace','Spices','Ten','Vase'];
const GLYPHS:Record<SymbolId,string>={
 Elephant:'🐘',Ruby:'◆',Necklace:'✥',Vase:'♜',Spices:'✺',
 Ace:'A',King:'K',Queen:'Q',Jack:'J',Ten:'10',Empty:'·'
};
const languages={
 de:{back:'Zur Casino-Lobby',label:'DER ELEFANTEN-SCHATZ',tag:'5 WALZEN · 10 LINIEN · SPIELGELD',
 money:'GUTHABEN',bet:'EINSATZ',win:'GEWINN',spin:'DREHEN',sound:'TON',motion:'ANIMATION',
 info:'SPIELREGELN',stats:'DEINE SPIELE',rounds:'Drehs',wins:'Gewinne',max:'Höchster Gewinn',
 bonus:'ELEPHANT BONUS',herd:'DIE HERDE ZIEHT VORBEI',noWin:'Diesmal kein Gewinn.',
 ready:'Die Elefanten warten.',err:'Ergebnis nicht bestätigt. Bitte die Slot neu öffnen.',
 help:'LEERTASTE drehen · ESC zurück',line:'Rubin ist Wild und verdoppelt den Liniengewinn.',
 elephants:'Zwei Elefanten irgendwo im Feld lösen einen Respin aus. Elefanten und Rubine bleiben stehen.',
 tuning:'Erste spielbare Version: eigenständige Gewinngewichte, keine offizielle Merkur-RTP.',
 base:'Basisgewinn',feature:'Elefanten-Bonus',winLines:'Gewinnlinien',
 disabled:'Nicht genug Spielgeld',notNative:'Zum Spielen die Slot im Desktop-Casino öffnen.',
 },
 en:{back:'Casino lobby',label:'TREASURE OF THE ELEPHANTS',tag:'5 REELS · 10 LINES · PLAY MONEY',
 money:'BANKROLL',bet:'WAGER',win:'WIN',spin:'SPIN',sound:'SOUND',motion:'ANIMATION',
 info:'GAME RULES',stats:'YOUR RECORD',rounds:'Spins',wins:'Wins',max:'Biggest win',
 bonus:'ELEPHANT BONUS',herd:'THE HERD IS PASSING',noWin:'No win this time.',
 ready:'The elephants are waiting.',err:'Unconfirmed result. Reopen the slot to recover.',
 help:'SPACE spins · ESC returns',line:'Ruby is Wild and doubles the winning line.',
 elephants:'Two elephants anywhere trigger a respin. Elephants and rubies are held.',
 tuning:'First playable: original probability tuning, not official Merkur RTP.',
 base:'Base win',feature:'Elephant bonus',winLines:'Winning lines',
 disabled:'Not enough play money',notNative:'Open the slot inside the desktop casino to play.',
 },
};
const cash=(s:string)=>'$'+Number(s).toLocaleString('en-US',{minimumFractionDigits:2,maximumFractionDigits:2});
function audioCue(kind:'spin'|'stop'|'trumpet'|'steps'|'win',ref:{current:AudioContext|null}){
 try{
  const ctx=ref.current??new AudioContext();
  ref.current=ctx;
  if(ctx.state==='suspended')void ctx.resume();
  const now=ctx.currentTime;
  const tone=(freq:number,end:number,duration:number,offset:number,vol:number,wave:OscillatorType)=>{
   const o=ctx.createOscillator(),gain=ctx.createGain();
   o.type=wave;o.frequency.setValueAtTime(freq,now+offset);
   o.frequency.exponentialRampToValueAtTime(Math.max(20,end),now+offset+duration);
   gain.gain.setValueAtTime(.0001,now+offset);
   gain.gain.exponentialRampToValueAtTime(Math.max(.0002,vol),now+offset+.03);
   gain.gain.exponentialRampToValueAtTime(.0001,now+offset+duration);
   o.connect(gain);gain.connect(ctx.destination);
   o.start(now+offset);o.stop(now+offset+duration+.03);
  };
  if(kind==='spin'||kind==='stop')tone(kind==='spin'?350:180,kind==='spin'?190:75,.12,0,.045,'triangle');
  if(kind==='trumpet'){
   tone(130,410,.75,0,.095,'sawtooth');tone(195,580,.65,.09,.055,'triangle');
   tone(120,285,.55,.65,.07,'sawtooth');
  }
  if(kind==='steps')for(let i=0;i<6;i++)tone(72,32,.16,i*.25,.045,'triangle');
  if(kind==='win')for(let i=0;i<3;i++)tone(400+i*145,520+i*155,.26,i*.13,.045,'sine');
 }catch{/* WebAudio can be unavailable in hosted previews. */}
}
function Scene({active,reduced}:{active:boolean;reduced:boolean}){
 return <div className={'ruby-landscape'+(active?' herd-active':'')+(reduced?' no-motion':'')} aria-hidden="true">
   <div className="ruby-sun"/><div className="ruby-palace">
     <i/><i/><i/><i/><i/>
   </div>
   <div className="ruby-hills"/><div className="ruby-herd">
     <span>🐘</span><span>🐘</span><span>🐘</span><span>🐘</span><span>🐘</span>
   </div>
   <div className="ruby-mist"/>
 </div>;
}
function App(){
 const [snapshot,setSnapshot]=useState<RubySnapshot|null>(null);
 const snapRef=useRef<RubySnapshot|null>(null);
 const [pending,setPending]=useState(false);
 const pendingRef=useRef<string|null>(null);
 const [uncertain,setUncertain]=useState(false);
 const [notice,setNotice]=useState('');
 const [grid,setGrid]=useState<SymbolId[]>(EMPTY);
 const [held,setHeld]=useState<number[]>([]);
 const [winning,setWinning]=useState<number[]>([]);
 const [phase,setPhase]=useState<'ready'|'rolling'|'herd'|'winning'>('ready');
 const [rules,setRules]=useState(false);
 const timers=useRef<ReturnType<typeof setTimeout>[]>([]);
 const seen=useRef<string|null>(null);
 const first=useRef(true);
 const context=useRef<AudioContext|null>(null);
 const [spinResult,setSpinResult]=useState<RubySpin|null>(null);
 const reduced=useRef(window.matchMedia('(prefers-reduced-motion: reduce)').matches).current;
 const clearTimers=useCallback(()=>{timers.current.forEach(clearTimeout);timers.current=[];},[]);
 const delay=useCallback((fn:()=>void,ms:number)=>{timers.current.push(setTimeout(fn,ms));},[]);
 const send=useCallback((intent:RubyIntent)=>{
  const s=snapRef.current;
  if(!s||pendingRef.current||uncertain||phase==='rolling'||phase==='herd'||!window.chrome?.webview)return;
  if(intent.action==='spin'&&!s.state.canSpin)return;
  const id=sendRuby(s,intent);pendingRef.current=id;setPending(true);setNotice('');
  if(intent.action==='spin'){setPhase('rolling');setWinning([]);setHeld([]);setSpinResult(null);
   if(s.state.soundEnabled)audioCue('spin',context);}
 },[uncertain,phase]);
 useEffect(()=>{
  const off=onRubyMessage(message=>{
   if(message.type==='snapshot'){
    if(snapRef.current?.session===message.data.session&&message.data.revision<snapRef.current.revision)return;
    snapRef.current=message.data;setSnapshot(message.data);
   }else if(message.data.requestId===pendingRef.current){
    pendingRef.current=null;setPending(false);
    if(!message.data.success){setPhase('ready');setNotice(message.data.message);}
   }
  });
  window.chrome?.webview?.postMessage({type:'indian-ruby.ready'});
  return()=>{off();clearTimers();void context.current?.close();context.current=null;};
 },[clearTimers]);
 useEffect(()=>{
  if(!snapshot)return;
  finishModeBoot();
  window.chrome?.webview?.postMessage({type:'indian-ruby.ui-ready'});
 },[!!snapshot]);
 useEffect(()=>{
  if(!snapshot)return;
  const result=snapshot.state.lastSpin;
  if(!result)return;
  if(first.current){
   first.current=false;seen.current=result.id;setGrid(result.finalGrid);
   setSpinResult(result);setHeld(result.heldCells);setWinning(result.winningLines);
   setPhase('winning');return;
  }
  if(seen.current===result.id)return;
  seen.current=result.id;clearTimers();
  setGrid(result.baseGrid);setSpinResult(result);
  const motion=snapshot.state.animationsEnabled&&!reduced;
  const sound=snapshot.state.soundEnabled;
  if(!motion){
   setHeld(result.heldCells);setGrid(result.finalGrid);setWinning(result.winningLines);
   setPhase('winning');if(sound&&result.elephantBonus)audioCue('trumpet',context);return;
  }
  setPhase('rolling');
  delay(()=>{
   setGrid(result.baseGrid);
   if(sound)audioCue('stop',context);
   if(result.elephantBonus){
    setHeld(result.heldCells);setPhase('herd');
    if(sound){audioCue('trumpet',context);audioCue('steps',context);}
    delay(()=>{
     setGrid(result.finalGrid);setWinning(result.winningLines);setPhase('winning');
     if(sound&&Number(result.payout)>0)audioCue('win',context);
    },2200);
   }else{
    setGrid(result.finalGrid);setWinning(result.winningLines);setPhase('winning');
    if(sound&&Number(result.payout)>0)audioCue('win',context);
   }
  },800);
 },[snapshot?.state.lastSpin?.id,clearTimers,delay,reduced]);
 useEffect(()=>{if(snapshot&&!snapshot.state.lastSpin)first.current=false;},[!!snapshot]);
 useEffect(()=>{
  if(!pending)return;
  const id=setTimeout(()=>{if(!pendingRef.current)return;setUncertain(true);setPhase('ready');
   setNotice(languages[snapRef.current?.state.language??'de'].err);},10000);
  return()=>clearTimeout(id);
 },[pending]);
 const close=()=>window.chrome?.webview?.postMessage({type:'indian-ruby.close'});
 useEffect(()=>{
  const onKey=(e:KeyboardEvent)=>{
   if(e.key==='Escape'){e.preventDefault();if(rules)setRules(false);else close();}
   if(e.code==='Space'&&!e.repeat&&!(e.target instanceof HTMLButtonElement)){
    e.preventDefault();send({action:'spin'});
   }
  };
  document.addEventListener('keydown',onKey);
  return()=>document.removeEventListener('keydown',onKey);
 },[send,rules]);
 const state=snapshot?.state;
 const de=state?.language!=='en',t=languages[de?'de':'en'];
 const canAct=!!snapshot&&!pending&&!uncertain&&phase!=='rolling'&&phase!=='herd';
 const idx=state?.bets.indexOf(state.bet)??0;
 const value=spinResult?.payout??'0';
 return <div className="ruby-shell" data-reduced={reduced||!state?.animationsEnabled}>
  <Scene active={phase==='herd'} reduced={reduced||!state?.animationsEnabled}/>
  <header className="ruby-header"><button onClick={close} className="ruby-back">← {t.back}</button>
    <div className="ruby-title"><small>THE MIDNIGHT CLUB PRESENTS</small><h1>INDIAN <span>RUBY</span></h1>
     <p>{t.label}</p></div>
    <div className="ruby-top-balance"><small>{t.money}</small><strong>{cash(state?.bankroll??'0')}</strong></div>
  </header>
  <main className="ruby-main">
   <aside className="ruby-story">
    <div className="ruby-totem" aria-hidden="true">🐘</div>
    <p className="ruby-eyebrow">ELEPHANT BONUS</p>
    <h2>{phase==='herd'?t.herd:t.bonus}</h2>
    <p>{t.elephants}</p>
    <div className="ruby-ornament">◆ ── ✥ ── ◆</div>
   </aside>
   <section className="ruby-machine" aria-label={t.tag}>
    <div className="ruby-machine-top"><span>✦</span>{t.tag}<span>✦</span></div>
    <div className={'ruby-grid'+(phase==='rolling'?' spinning':'')+(phase==='herd'?' bonus':'')} role="img"
      aria-label={grid.join(', ')}>
      {grid.map((symbol,i)=><div key={i} className={'ruby-cell s-'+symbol.toLowerCase()+
       (held.includes(i)?' held':'')} data-index={i} aria-label={symbol}>
       <span className="ruby-cell-sheen"/><b>{GLYPHS[symbol]}</b>
       {symbol==='Elephant'&&<small>ELEPHANT</small>}
      </div>)}
    </div>
    <div className="ruby-result" role="status" aria-live="polite">
      {phase==='herd'?<strong className="ruby-bonus-title">{t.herd}</strong>:
      phase==='rolling'?<strong>✦ ✦ ✦</strong>:
      spinResult?<><span>{spinResult.elephantBonus?t.bonus:t.win}</span><strong>{cash(value)}</strong></>:
      <span>{notice||t.ready}</span>}
    </div>
    <div className="ruby-controls">
     <div className="ruby-stake"><small>{t.bet}</small><div className="ruby-stepper">
      <button disabled={!canAct||idx<=0} onClick={()=>send({action:'bet.set',value:state?.bets[idx-1]})}>−</button>
      <strong>{cash(state?.bet??'1')}</strong>
      <button disabled={!canAct||!state||idx>=state.bets.length-1} onClick={()=>send({action:'bet.set',value:state?.bets[idx+1]})}>+</button>
     </div></div>
     <button className="ruby-spin" disabled={!canAct||!state?.canSpin} onClick={()=>send({action:'spin'})}>
      <span>◆</span> {t.spin} <span>◆</span></button>
     <button className="ruby-info-button" onClick={()=>setRules(x=>!x)} aria-expanded={rules}>{t.info}</button>
    </div>
    {!window.chrome?.webview&&<p className="ruby-bridge-notice">{t.notNative}</p>}
    {notice&&<p className="ruby-bridge-notice" role="alert">{notice}</p>}
    {!state?.canSpin&&state&&<p className="ruby-bridge-notice">{t.disabled}</p>}
   </section>
   <aside className="ruby-details">
    <div className="ruby-detail-card">
     <h2>{t.stats}</h2>
     <div><span>{t.rounds}</span><strong>{state?.stats.spins??0}</strong></div>
     <div><span>{t.wins}</span><strong>{state?.stats.wins??0}</strong></div>
     <div><span>{t.max}</span><strong>{cash(state?.stats.biggestPayout??'0')}</strong></div>
    </div>
    <div className="ruby-detail-card ruby-sound"><h2>{de?'PRÄSENTATION':'PRESENTATION'}</h2>
     <button disabled={!canAct||!state} onClick={()=>send({action:'sound.set',value:String(!state?.soundEnabled)})}>
      {t.sound} <strong>{state?.soundEnabled?'ON':'OFF'}</strong></button>
     <button disabled={!canAct||!state} onClick={()=>send({action:'animations.set',value:String(!state?.animationsEnabled)})}>
      {t.motion} <strong>{state?.animationsEnabled?'ON':'OFF'}</strong></button>
    </div>
    {spinResult?.elephantBonus&&<div className="ruby-detail-card ruby-feature-receipt">
      <h2>{t.bonus}</h2><div><span>{t.base}</span><strong>{cash(spinResult.basePayout)}</strong></div>
      <div><span>{t.feature}</span><strong>{cash(spinResult.bonusPayout)}</strong></div>
      <div><span>{t.winLines}</span><strong>{winning.length}</strong></div>
    </div>}
   </aside>
  </main>
  <footer className="ruby-footer"><span>{t.help}</span><span>{t.tuning}</span></footer>
  {rules&&<div className="ruby-modal-backdrop" onClick={()=>setRules(false)}>
   <section className="ruby-modal" role="dialog" aria-modal="true" aria-label={t.info} onClick={e=>e.stopPropagation()}>
    <button className="ruby-modal-close" onClick={()=>setRules(false)}>×</button>
    <h2>{t.info}</h2><p>{t.line}</p><p>{t.elephants}</p>
    <p>Elephant 2/3/4/5: ×10 / ×50 / ×200 / ×1000 · Necklace 3/4/5: ×25 / ×100 / ×500</p>
    <p>{t.tuning}</p><button onClick={()=>setRules(false)}>{de?'Schließen':'Close'}</button>
   </section>
  </div>}
 </div>;
}
createRoot(document.getElementById('root')!).render(<App/>);
