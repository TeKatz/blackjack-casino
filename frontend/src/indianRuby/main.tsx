import {memo,useCallback,useEffect,useRef,useState} from 'react';
import {createRoot} from 'react-dom/client';
import {finishModeBoot} from '../boot/finishModeBoot';
import {onRubyMessage,sendRuby} from './bridge';
import type {RubyIntent,RubySnapshot,RubySpin,SymbolId} from './bridge';
import {RubyReelStage} from './RubyReelStage';
import {RubySymbolArt,RubyHerdElephant} from './SymbolArt';
import {playRubyCue} from './rubyAudio';
import {BONUS_FINISH_PAUSE_MS,BONUS_START_PAUSE_MS} from './reelMotion';
import './indianRuby.css';
import './rubyPremium.css';

const PREVIEW:SymbolId[]=[
 'Queen','Vase','Ace','Necklace','Ten',
 'Elephant','Jack','Spices','King','Queen',
 'Ruby','Ace','Spices','Ten','Vase',
];
const REELS=[0,1,2,3,4] as const;
const languages={
 de:{
  back:'Zur Lobby',strap:'THE MIDNIGHT CLUB · ROYAL COLLECTION',tag:'5 WALZEN · 10 GEWINNLINIEN',
  money:'SPIELGUTHABEN',bet:'EINSATZ',win:'GEWINN',spin:'DREHEN',sound:'TON',motion:'BEWEGUNG',
  info:'Spielregeln',stats:'SPIELSTATISTIK',rounds:'Drehs',wins:'Gewinne',max:'Rekordgewinn',
  bonus:'ELEPHANT BONUS',herd:'DIE HERDE KOMMT',noWin:'Die nächste Runde wartet.',
  ready:'Der Schatz wartet auf dich.',rolling:'Die Walzen drehen …',anticipation:'Ein Elefant nähert sich …',
  err:'Das Ergebnis konnte nicht bestätigt werden. Öffne Indian Ruby erneut.',
  help:'LEERTASTE · Drehen     ESC · Lobby',
  wild:'Rubin-Wild ersetzt reguläre Symbole und verdoppelt den jeweiligen Liniengewinn.',
  elephants:'Zwei Elefanten irgendwo auf den Walzen starten den Elephant Bonus. Elefanten und Rubine bleiben stehen, die übrigen Felder drehen erneut.',
  tuning:'Eigene Test-Wahrscheinlichkeiten – keine bestätigte Merkur-Gewinnmathematik.',
  base:'Basisgewinn',feature:'Bonusertrag',winLines:'Gewinnlinien',disabled:'Nicht genug Spielgeld',
  notNative:'Indian Ruby ist über das Desktop-Casino spielbar.',offline:'OFFLINE · NUR SPIELGELD',
  subtitle:'Ein Schatz aus Gold, Rubinen und Elefanten',
  marquee:'ROYAL ELEPHANT EDITION',held:'GEHALTENE SYMBOLE',presentation:'PRÄSENTATION',
  choose:'EINSATZ WÄHLEN',bonusInfo:'ELEPHANT BONUS · 2+ SYMBOLE',total:'GESAMTGEWINN',
  jackpot:'KÖNIGLICHER GEWINN',close:'Schließen',
 },
 en:{
  back:'Casino lobby',strap:'THE MIDNIGHT CLUB · ROYAL COLLECTION',tag:'5 REELS · 10 PAYLINES',
  money:'PLAY BANKROLL',bet:'WAGER',win:'WIN',spin:'SPIN',sound:'SOUND',motion:'MOTION',
  info:'Game rules',stats:'YOUR RECORD',rounds:'Spins',wins:'Wins',max:'Top payout',
  bonus:'ELEPHANT BONUS',herd:'THE HERD APPROACHES',noWin:'Another spin awaits.',
  ready:'Your treasure awaits.',rolling:'Reels in motion …',anticipation:'An elephant is approaching …',
  err:'Result not confirmed. Reopen Indian Ruby to reconcile.',
  help:'SPACE · Spin     ESC · Lobby',
  wild:'Ruby Wild replaces regular symbols and doubles that line’s win.',
  elephants:'Two elephants anywhere trigger an Elephant Bonus. Elephants and Rubies stay in place while other positions respin.',
  tuning:'Independent prototype odds – not manufacturer-verified reel math.',
  base:'Base payout',feature:'Feature payout',winLines:'Winning lines',disabled:'Not enough play money',
  notNative:'Play Indian Ruby inside the desktop casino.',offline:'OFFLINE · PLAY MONEY ONLY',
  subtitle:'A treasure of gold, rubies and elephants',
  marquee:'ROYAL ELEPHANT EDITION',held:'HELD SYMBOLS',presentation:'PRESENTATION',
  choose:'SELECT BET',bonusInfo:'ELEPHANT BONUS · 2+ SYMBOLS',total:'TOTAL WIN',
  jackpot:'ROYAL PAYOUT',close:'Close',
 },
};
const cash=(value:string|number)=>'$'+Number(value).toLocaleString('en-US',{
 minimumFractionDigits:2,maximumFractionDigits:2,
});
type Phase='idle'|'awaiting'|'base'|'bonus-intro'|'bonus'|'result';
type Pending={id:string;expectedRevision:number;action:RubyIntent['action'];ackRevision:number|null};

function RubyScene({herd,reduced}:{herd:boolean;reduced:boolean}){
 return <div className={'ruby-landscape'+(herd?' herd-active':'')+(reduced?' no-motion':'')}
  aria-hidden="true">
  <div className="ruby-sun"/><div className="ruby-palace"><i/><i/><i/><i/><i/></div>
  <div className="ruby-hills"/>
  <div className="ruby-palace-lamps"><i/><i/><i/><i/><i/><i/></div>
  <div className="ruby-herd">
   <RubyHerdElephant/><RubyHerdElephant small/><RubyHerdElephant/>
   <RubyHerdElephant small/><RubyHerdElephant/>
  </div>
  <div className="ruby-dust"/>
  <div className="ruby-mist"/>
 </div>;
}
const RubyLamps=memo(function RubyLamps({count=18}:{count?:number}){
 return <div className="ruby-light-rail" aria-hidden="true">
  {Array.from({length:count},(_,i)=><i key={i} style={{animationDelay:(i*.17)+'s'}}/>)}
 </div>;
});
function AnimatedWin({value,animate,reduced}:{value:string;animate:boolean;reduced:boolean}){
 const [shown,setShown]=useState(()=>Number(value));
 useEffect(()=>{
  const target=Number(value);
  if(!Number.isFinite(target)||!animate||reduced){setShown(target||0);return;}
  let frame=0;
  const started=performance.now(),duration=Math.min(1300,650+Math.log10(Math.max(1,target))*85);
  const update=(now:number)=>{
   const ratio=Math.min(1,(now-started)/duration);
   const eased=1-Math.pow(1-ratio,3);
   setShown(target*eased);
   if(ratio<1)frame=requestAnimationFrame(update);
  };
  setShown(0);frame=requestAnimationFrame(update);
  return()=>cancelAnimationFrame(frame);
 },[value,animate,reduced]);
 return <strong className="ruby-countup" aria-live="off">{cash(shown)}</strong>;
}
const BetSelector=memo(function BetSelector({bet,bets,canDecrease,canIncrease,onBet,label}:{
 bet:string;bets:string[];canDecrease:boolean;canIncrease:boolean;
 onBet:(bet:string)=>void;label:string;
}){
 const index=bets.indexOf(bet);
 return <div className="ruby-stake" data-testid="ruby-stable-bet">
  <small>{label}</small>
  <div className="ruby-stepper">
   <button type="button" aria-label="Bet decrease" className="ruby-bet-minus"
    disabled={!canDecrease||index<=0} onClick={()=>onBet(bets[index-1])}>−</button>
   <output className="ruby-bet-value" data-testid="ruby-bet-value">{cash(bet)}</output>
   <button type="button" aria-label="Bet increase" className="ruby-bet-plus"
    disabled={!canIncrease||index>=bets.length-1} onClick={()=>onBet(bets[index+1])}>+</button>
  </div>
 </div>;
});
function App(){
 const [snapshot,setSnapshot]=useState<RubySnapshot|null>(null);
 const snapshotRef=useRef<RubySnapshot|null>(null);
 const [pending,setPending]=useState(false);
 const pendingRef=useRef<Pending|null>(null);
 const [uncertain,setUncertain]=useState(false);
 const [notice,setNotice]=useState('');
 const [grid,setGrid]=useState<SymbolId[]>(PREVIEW);
 const gridRef=useRef<SymbolId[]>(PREVIEW);
 const [spinResult,setSpinResult]=useState<RubySpin|null>(null);
 const resultRef=useRef<RubySpin|null>(null);
 const [targetBase,setTargetBase]=useState<readonly SymbolId[]|null>(null);
 const [targetBonus,setTargetBonus]=useState<readonly SymbolId[]|null>(null);
 const [rollingReels,setRollingReels]=useState<readonly number[]>([]);
 const [rollingCells,setRollingCells]=useState<readonly number[]>([]);
 const [held,setHeld]=useState<readonly number[]>([]);
 const [winners,setWinners]=useState<readonly number[]>([]);
 const [phase,setPhase]=useState<Phase>('idle');
 const [anticipation,setAnticipation]=useState(false);
 const [rules,setRules]=useState(false);
 const timers=useRef<ReturnType<typeof setTimeout>[]>([]);
 const firstSnapshot=useRef(true);
 const seen=useRef<string|null>(null);
 const stoppedReels=useRef(new Set<number>());
 const pendingBonus=useRef(new Set<number>());
 const anticipated=useRef(false);
 const audio=useRef<AudioContext|null>(null);
 const reduced=useRef(window.matchMedia('(prefers-reduced-motion: reduce)').matches).current;
 const clearTimers=useCallback(()=>{timers.current.forEach(clearTimeout);timers.current=[];},[]);
 const schedule=useCallback((fn:()=>void,ms:number)=>{
  timers.current.push(setTimeout(fn,ms));
 },[]);
 const makeSound=useCallback((cue:Parameters<typeof playRubyCue>[1])=>{
  playRubyCue(audio,cue,!!snapshotRef.current?.state.soundEnabled);
 },[]);
 const finishSpin=useCallback((id:string)=>{
  const result=resultRef.current;
  if(!result||result.id!==id)return;
  gridRef.current=[...result.finalGrid];
  setGrid(gridRef.current);
  setRollingReels([]);setRollingCells([]);setAnticipation(false);
  setHeld(result.elephantBonus?result.heldCells:[]);
  setWinners(result.winningLines);
  setPhase('result');
  if(Number(result.payout)>0)
   makeSound(Number(result.payout)>=Number(result.bet)*30?'big-win':'win');
 },[makeSound]);
 const onBonusStop=useCallback((id:string,cell:number)=>{
  const result=resultRef.current;
  if(!result||result.id!==id||!pendingBonus.current.delete(cell))return;
  setRollingCells(previous=>previous.filter(index=>index!==cell));
  setGrid(previous=>{
   const next=previous.map((symbol,index)=>index===cell?result.finalGrid[index]:symbol);
   gridRef.current=next;return next;
  });
  makeSound('stop');
  if(pendingBonus.current.size===0)schedule(()=>finishSpin(id),BONUS_FINISH_PAUSE_MS);
 },[finishSpin,makeSound,schedule]);
 const onBaseStop=useCallback((id:string,reel:number)=>{
  const result=resultRef.current;
  if(!result||result.id!==id||stoppedReels.current.has(reel))return;
  stoppedReels.current.add(reel);
  setRollingReels(previous=>previous.filter(index=>index!==reel));
  setGrid(previous=>{
   const next=previous.map((symbol,index)=>index%5===reel?result.baseGrid[index]:symbol);
   gridRef.current=next;return next;
  });
  makeSound('stop');
  const elephantCount=result.baseGrid.filter((symbol,index)=>
   symbol==='Elephant'&&stoppedReels.current.has(index%5)).length;
  if(result.elephantBonus&&elephantCount>0&&!anticipated.current
    &&stoppedReels.current.size<5){
   anticipated.current=true;setAnticipation(true);makeSound('anticipation');
  }
  if(stoppedReels.current.size!==5)return;
  setAnticipation(false);
  if(!result.elephantBonus){schedule(()=>finishSpin(id),190);return;}
  setHeld(result.heldCells);setPhase('bonus-intro');
  makeSound('bonus');
  schedule(()=>{
   if(resultRef.current?.id!==id)return;
   const free=Array.from({length:15},(_,index)=>index)
    .filter(index=>!result.heldCells.includes(index));
   if(free.length===0){finishSpin(id);return;}
   pendingBonus.current=new Set(free);
   setRollingCells(free);setPhase('bonus');
   makeSound('hoof');
  },BONUS_START_PAUSE_MS);
 },[finishSpin,makeSound,schedule]);
 const send=useCallback((intent:RubyIntent)=>{
  const current=snapshotRef.current;
  if(!current||pendingRef.current||uncertain||
   phase==='awaiting'||phase==='base'||phase==='bonus-intro'||phase==='bonus')return;
  if(intent.action==='spin'&&!current.state.canSpin)return;
  const id=sendRuby(current,intent);
  pendingRef.current={id,expectedRevision:current.revision,action:intent.action,ackRevision:null};
  setPending(true);setNotice('');
  if(intent.action==='spin'){
   clearTimers();resultRef.current=null;setSpinResult(null);
   stoppedReels.current.clear();pendingBonus.current.clear();anticipated.current=false;
   setAnticipation(false);setWinners([]);setHeld([]);setRollingReels([]);setRollingCells([]);
   setTargetBase(null);setTargetBonus(null);setPhase('awaiting');
   makeSound('spin');
  }else if(intent.action==='bet.set')makeSound('bet');
 },[uncertain,phase,clearTimers,makeSound]);
 useEffect(()=>{
  const release=(revision:number)=>{
   const request=pendingRef.current;
   if(request&&request.ackRevision!==null&&revision>=request.ackRevision){
    pendingRef.current=null;setPending(false);
   }
  };
  const detach=onRubyMessage(event=>{
   if(event.type==='snapshot'){
    if(snapshotRef.current?.session===event.data.session&&
       event.data.revision<snapshotRef.current.revision)return;
    snapshotRef.current=event.data;
    setSnapshot(event.data);
    release(event.data.revision);
   }else if(event.data.requestId===pendingRef.current?.id){
    if(!event.data.success){
     pendingRef.current=null;setPending(false);setPhase('idle');
     setNotice(event.data.message);
    }else{
     pendingRef.current.ackRevision=event.data.revision;
     release(snapshotRef.current?.revision??-1);
    }
   }
  });
  window.chrome?.webview?.postMessage({type:'indian-ruby.ready'});
  return()=>{detach();clearTimers();void audio.current?.close();audio.current=null;};
 },[clearTimers]);
 useEffect(()=>{
  if(!snapshot)return;
  finishModeBoot();
  window.chrome?.webview?.postMessage({type:'indian-ruby.ui-ready'});
 },[!!snapshot]);
 useEffect(()=>{
  if(!snapshot)return;
  const result=snapshot.state.lastSpin;
  if(firstSnapshot.current){
   firstSnapshot.current=false;
   if(result){
    seen.current=result.id;resultRef.current=result;
    setSpinResult(result);setTargetBase(result.baseGrid);setTargetBonus(result.finalGrid);
    gridRef.current=[...result.finalGrid];setGrid(gridRef.current);
    setHeld(result.elephantBonus?result.heldCells:[]);
    setWinners(result.winningLines);setPhase('result');
   }
   return;
  }
  if(!result||seen.current===result.id)return;
  seen.current=result.id;resultRef.current=result;setSpinResult(result);
  clearTimers();stoppedReels.current.clear();pendingBonus.current.clear();
  anticipated.current=false;setAnticipation(false);setWinners([]);
  setTargetBase(result.baseGrid);setTargetBonus(result.finalGrid);
  const motion=snapshot.state.animationsEnabled&&!reduced;
  if(!motion){finishSpin(result.id);return;}
  setRollingReels([...REELS]);setPhase('base');
 },[snapshot?.state.lastSpin?.id,clearTimers,finishSpin,reduced]);
 useEffect(()=>{
  if(!pending)return;
  const id=setTimeout(()=>{
   if(!pendingRef.current)return;
   setUncertain(true);setNotice(languages[snapshotRef.current?.state.language??'de'].err);
   setPending(false);setPhase('idle');
  },10000);
  return()=>clearTimeout(id);
 },[pending]);
 const close=useCallback(()=>window.chrome?.webview?.postMessage({type:'indian-ruby.close'}),[]);
 useEffect(()=>{
  const onKey=(event:KeyboardEvent)=>{
   if(event.key==='Escape'){
    event.preventDefault();if(rules)setRules(false);else close();
   }
   if(event.code==='Space'&&!event.repeat&&!rules&&
    !(event.target instanceof HTMLButtonElement)&&
    !(event.target instanceof HTMLInputElement)){
    event.preventDefault();send({action:'spin'});
   }
  };
  document.addEventListener('keydown',onKey);
  return()=>document.removeEventListener('keydown',onKey);
 },[send,rules,close]);
 const state=snapshot?.state;
 const de=state?.language!=='en',t=languages[de?'de':'en'];
 const animating=phase==='awaiting'||phase==='base'||phase==='bonus-intro'||phase==='bonus';
 const canInteract=!!snapshot&&!pending&&!uncertain&&!animating;
 // While a bet command is pending the clicks are guarded by pendingRef;
 // do not toggle the visual disabled state (the original flicker source).
 const canBet=!!snapshot&&!uncertain&&!animating;
 const idx=state?.bets.indexOf(state.bet)??-1;
 const onBet=useCallback((value:string)=>send({action:'bet.set',value}),[send]);
 const winAmount=spinResult?.payout??'0';
 const hasWin=phase==='result'&&!!spinResult&&Number(winAmount)>0;
 const isHerd=phase==='bonus-intro'||phase==='bonus';
 const bonusLive=isHerd||phase==='result'&&!!spinResult?.elephantBonus;
 return <div className="ruby-shell ruby-premium-shell"
   data-reduced={reduced||!state?.animationsEnabled} data-phase={phase}
   data-bonus={bonusLive?'true':'false'}>
  <RubyScene herd={isHerd} reduced={reduced||!state?.animationsEnabled}/>
  <div className="ruby-atmosphere" aria-hidden="true">
   {Array.from({length:17},(_,i)=><i key={i} style={{
    left:((i*37+8)%96)+'%',top:((i*27+9)%83)+'%',
    animationDelay:(i*.49)+'s',
   }}/>)}
  </div>
  <header className="ruby-header">
   <button type="button" className="ruby-back" onClick={close}>← {t.back}</button>
   <div className="ruby-title">
    <small>{t.strap}</small><h1>INDIAN <span>RUBY</span></h1><p>{t.subtitle}</p>
   </div>
   <div className="ruby-top-balance"><small>{t.money}</small>
    <strong>{cash(state?.bankroll??'0')}</strong></div>
  </header>
  <main className="ruby-main">
   <aside className="ruby-story">
    <div className="ruby-story-crest"><RubySymbolArt symbol="Elephant"/></div>
    <p className="ruby-eyebrow">{t.bonusInfo}</p>
    <h2>{isHerd?t.herd:t.bonus}</h2>
    <p>{t.elephants}</p>
    <div className="ruby-story-diamonds" aria-hidden="true">✦ ◆ ✦</div>
   </aside>
   <section className={'ruby-machine'+(isHerd?' bonus-illuminated':'')+(hasWin?' win-illuminated':'')}
    aria-label={t.tag}>
    <RubyLamps/>
    <div className="ruby-machine-top">
     <span className="ruby-marquee-left" aria-hidden="true">❖</span>
     <div><small>{t.marquee}</small><strong>INDIAN RUBY</strong></div>
     <span className="ruby-marquee-right" aria-hidden="true">❖</span>
    </div>
    <div className="ruby-reel-bezel">
     <div className="ruby-side-jewels left" aria-hidden="true"><i/><i/><i/></div>
     <RubyReelStage displayGrid={grid} targetBase={targetBase} targetBonus={targetBonus}
      spinId={spinResult?.id??null} rollingReels={rollingReels} rollingCells={rollingCells}
      heldCells={held} winningLines={winners}
      showWins={phase==='result'&&winners.length>0}
      anticipation={anticipation} reducedMotion={reduced||!state?.animationsEnabled}
      onBaseStop={onBaseStop} onBonusStop={onBonusStop}/>
     <div className="ruby-side-jewels right" aria-hidden="true"><i/><i/><i/></div>
    </div>
    <div className={'ruby-result'+(hasWin?' has-win':'')} role="status" aria-live="polite">
     <div className="ruby-result-title">
      {isHerd?t.herd:anticipation?t.anticipation:animating?t.rolling:
       hasWin?t.total:spinResult?t.noWin:notice||t.ready}
     </div>
     <div className="ruby-result-amount">
      <AnimatedWin value={phase==='result'?winAmount:'0'}
       animate={phase==='result'} reduced={reduced||!state?.animationsEnabled}/>
     </div>
     <span className="ruby-result-spark" aria-hidden="true">✦</span>
    </div>
    <div className="ruby-controls">
     <BetSelector bet={state?.bet??'1'} bets={state?.bets??[]}
      canDecrease={canBet&&idx>0}
      canIncrease={canBet&&idx>=0&&idx<(state?.bets.length??0)-1}
      onBet={onBet} label={t.choose}/>
     <button type="button" className="ruby-spin"
      disabled={!state?.canSpin||animating||uncertain}
      onClick={()=>send({action:'spin'})}>
      <span className="ruby-spin-gem" aria-hidden="true">◆</span>
      <span>{t.spin}</span>
     </button>
     <button type="button" className="ruby-info-button"
      onClick={()=>setRules(true)} aria-label={t.info}>
      <span aria-hidden="true">ⓘ</span><small>{t.info}</small>
     </button>
    </div>
    <RubyLamps count={18}/>
    {!window.chrome?.webview&&<p className="ruby-bridge-notice">{t.notNative}</p>}
    {notice&&<p className="ruby-bridge-notice" role="alert">{notice}</p>}
    {!state?.canSpin&&state&&<p className="ruby-bridge-notice">{t.disabled}</p>}
   </section>
   <aside className="ruby-details">
    <div className="ruby-detail-card ruby-collectibles">
     <h2>{t.stats}</h2>
     <div><span>{t.rounds}</span><strong>{state?.stats.spins??0}</strong></div>
     <div><span>{t.wins}</span><strong>{state?.stats.wins??0}</strong></div>
     <div><span>{t.max}</span><strong>{cash(state?.stats.biggestPayout??'0')}</strong></div>
    </div>
    <div className="ruby-detail-card ruby-sound">
     <h2>{t.presentation}</h2>
     <button type="button" disabled={!canInteract}
      onClick={()=>send({action:'sound.set',value:String(!state?.soundEnabled)})}>
      {t.sound}<strong>{state?.soundEnabled?'ON':'OFF'}</strong></button>
     <button type="button" disabled={!canInteract}
      onClick={()=>send({action:'animations.set',value:String(!state?.animationsEnabled)})}>
      {t.motion}<strong>{state?.animationsEnabled?'ON':'OFF'}</strong></button>
    </div>
    {spinResult?.elephantBonus&&<div className="ruby-detail-card ruby-feature-receipt">
     <h2>{t.bonus}</h2>
     <div><span>{t.base}</span><strong>{cash(spinResult.basePayout)}</strong></div>
     <div><span>{t.feature}</span><strong>{cash(spinResult.bonusPayout)}</strong></div>
     <div><span>{t.winLines}</span><strong>{winners.length}</strong></div>
    </div>}
   </aside>
  </main>
  <footer className="ruby-footer">
   <span>{t.help}</span><span>{t.offline}</span>
  </footer>
  {rules&&<div className="ruby-modal-backdrop" onClick={()=>setRules(false)}>
   <section className="ruby-modal" role="dialog" aria-modal="true" aria-label={t.info}
    onClick={event=>event.stopPropagation()}>
    <button className="ruby-modal-close" onClick={()=>setRules(false)}
     aria-label={t.close}>×</button>
    <h2>{t.info}</h2><p>{t.wild}</p><p>{t.elephants}</p>
    <p>Elephant 2/3/4/5: ×10 / ×50 / ×200 / ×1000</p>
    <p>Necklace 3/4/5: ×25 / ×100 / ×500</p>
    <p>{t.tuning}</p>
    <button onClick={()=>setRules(false)}>{t.close}</button>
   </section>
  </div>}
 </div>;
}
createRoot(document.getElementById('root')!).render(<App/>);
