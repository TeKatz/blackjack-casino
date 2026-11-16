import {memo,useCallback,useEffect,useRef,useState} from 'react';
import type {CSSProperties,RefObject} from 'react';
import {createRoot} from 'react-dom/client';
import {finishModeBoot} from '../boot/finishModeBoot';
import {onRubyMessage,sendRuby} from './bridge';
import type {RubyIntent,RubySnapshot,RubySpin,SymbolId} from './bridge';
import {RubyReelStage} from './RubyReelStage';
import {RubySymbolArt} from './SymbolArt';
import {RubyBonusHerd} from './RubyBonusHerd';
import {playRubyCue} from './rubyAudio';
import {bonusFinishPause,bonusStartPause,isElephantNearMiss} from './reelMotion';
import type {SunsetSpeed} from './reelMotion';
import {SunsetRules} from './SunsetRules';
import './indianRuby.css';
import './rubyPremium.css';
import './sunsetExtensions.css';
import './sunsetRefinements.css';

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
  err:'Das Ergebnis konnte nicht bestätigt werden. Öffne Sunset Ruby erneut.',
  help:'LEERTASTE · Drehen     ESC · Lobby',
  wild:'Rubin-Wild ersetzt reguläre Symbole und verdoppelt den jeweiligen Liniengewinn.',
  elephants:'Zwei Elefanten irgendwo auf den Walzen starten den Elephant Bonus. Elefanten und Rubine bleiben stehen, die übrigen Felder drehen erneut.',
  tuning:'Eigene Test-Wahrscheinlichkeiten – keine bestätigte Merkur-Gewinnmathematik.',
  base:'Basisgewinn',feature:'Bonusertrag',winLines:'Gewinnlinien',disabled:'Nicht genug Spielgeld',
  notNative:'Sunset Ruby ist über das Desktop-Casino spielbar.',offline:'OFFLINE · NUR SPIELGELD',
  subtitle:'Ein Schatz aus Gold, Rubinen und Elefanten',
  marquee:'ROYAL ELEPHANT EDITION',held:'GEHALTENE SYMBOLE',presentation:'PRÄSENTATION',
  choose:'EINSATZ WÄHLEN',bonusInfo:'ELEPHANT BONUS · 2+ SYMBOLE',total:'GESAMTGEWINN',
  jackpot:'KÖNIGLICHER GEWINN',close:'Schließen',
 auto:'AUTO-SPINS',autoStart:'AUTO START',autoStop:'STOPP',autoCount:'ANZAHL',autoLeft:'ÜBRIG',
 speed:'SLOT SPEED',normal:'Normal',fast:'Fast',turbo:'Turbo',
 stopBonus:'Bei Bonus stoppen',nearMiss:'FAST! NUR EIN ELEFANT',
 },
 en:{
  back:'Casino lobby',strap:'THE MIDNIGHT CLUB · ROYAL COLLECTION',tag:'5 REELS · 10 PAYLINES',
  money:'PLAY BANKROLL',bet:'WAGER',win:'WIN',spin:'SPIN',sound:'SOUND',motion:'MOTION',
  info:'Game rules',stats:'YOUR RECORD',rounds:'Spins',wins:'Wins',max:'Top payout',
  bonus:'ELEPHANT BONUS',herd:'THE HERD APPROACHES',noWin:'Another spin awaits.',
  ready:'Your treasure awaits.',rolling:'Reels in motion …',anticipation:'An elephant is approaching …',
  err:'Result not confirmed. Reopen Sunset Ruby to reconcile.',
  help:'SPACE · Spin     ESC · Lobby',
  wild:'Ruby Wild replaces regular symbols and doubles that line’s win.',
  elephants:'Two elephants anywhere trigger an Elephant Bonus. Elephants and Rubies stay in place while other positions respin.',
  tuning:'Independent prototype odds – not manufacturer-verified reel math.',
  base:'Base payout',feature:'Feature payout',winLines:'Winning lines',disabled:'Not enough play money',
  notNative:'Play Sunset Ruby inside the desktop casino.',offline:'OFFLINE · PLAY MONEY ONLY',
  subtitle:'A treasure of gold, rubies and elephants',
  marquee:'ROYAL ELEPHANT EDITION',held:'HELD SYMBOLS',presentation:'PRESENTATION',
  choose:'SELECT BET',bonusInfo:'ELEPHANT BONUS · 2+ SYMBOLS',total:'TOTAL WIN',
  jackpot:'ROYAL PAYOUT',close:'Close',
 auto:'AUTO SPINS',autoStart:'START AUTO',autoStop:'STOP',autoCount:'SPINS',autoLeft:'LEFT',
 speed:'SLOT SPEED',normal:'Normal',fast:'Fast',turbo:'Turbo',
 stopBonus:'Stop on bonus',nearMiss:'CLOSE! ONE ELEPHANT',
 },
};
const cash=(value:string|number)=>'$'+Number(value).toLocaleString('en-US',{
 minimumFractionDigits:2,maximumFractionDigits:2,
});
type Phase='idle'|'awaiting'|'base'|'bonus-intro'|'bonus'|'result';
type Pending={id:string;expectedRevision:number;action:RubyIntent['action'];ackRevision:number|null};
type Feedback='win'|'loss'|'push'|'none';
type Transfer={id:string;amount:number;x:number;y:number;dx:number;dy:number};
const getFeedback=(net:string):Exclude<Feedback,'none'>=>Number(net)>0?'win':Number(net)<0?'loss':'push';

const RubyScene=memo(function RubyScene({herdBackground}:{herdBackground:RefObject<HTMLDivElement|null>}){
 return <div className="ruby-landscape" aria-hidden="true">
  <div className="ruby-sun"/><div className="ruby-palace"><i/><i/><i/><i/><i/></div>
  <div className="ruby-hills"/>
  <div className="ruby-palace-lamps"><i/><i/><i/><i/><i/><i/></div>
  <div className="ruby-mist"/>
  <div className="ruby-herd-background" ref={herdBackground}/>
 </div>;
});
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
 return <div className="ruby-stake sunset-bet-console" data-testid="ruby-stable-bet">
  <div className="sunset-bet-label" aria-hidden="true">
   <span className="sunset-bet-label-gem">◆</span><small>{label}</small>
   <span className="sunset-bet-label-gem">◆</span>
  </div>
  <div className="ruby-stepper sunset-bet-stepper">
   <button type="button" aria-label="Bet decrease" className="ruby-bet-minus"
    disabled={!canDecrease||index<=0} onClick={()=>onBet(bets[index-1])}>
    <span aria-hidden="true">−</span>
   </button>
   <output className="ruby-bet-value" data-testid="ruby-bet-value">{cash(bet)}</output>
   <button type="button" aria-label="Bet increase" className="ruby-bet-plus"
    disabled={!canIncrease||index>=bets.length-1} onClick={()=>onBet(bets[index+1])}>
    <span aria-hidden="true">+</span>
   </button>
  </div>
  <div className="sunset-bet-ticks" aria-hidden="true">
   {bets.map((value,i)=><span key={value}
    className={i===index?'current':i<index?'passed':''}/>)}
  </div>
 </div>;
});
function AnimatedBankroll({balance,spin,settled,reduced}:{balance:string;spin:RubySpin|null;settled:boolean;reduced:boolean}){
 const [shown,setShown]=useState(()=>Number(balance));
 useEffect(()=>{
  const before=spin?Number(spin.bankrollBefore):Number(balance);
  const after=spin?Number(spin.bankrollAfter):Number(balance);
  if(!spin){setShown(after);return;}
  if(!settled){setShown(before);return;}
  if(reduced||before===after){setShown(after);return;}
  let frame=0;
  const start=performance.now();
  const tick=(time:number)=>{
   const progress=Math.min(1,(time-start)/1150);
   setShown(before+(after-before)*(1-Math.pow(1-progress,3)));
   if(progress<1)frame=requestAnimationFrame(tick);
  };
  frame=requestAnimationFrame(tick);
  return()=>cancelAnimationFrame(frame);
 },[balance,spin?.id,settled,reduced]);
 return <strong className="sunset-bankroll-value" aria-label={cash(balance)}>{cash(shown)}</strong>;
}
function App(){
 const [snapshot,setSnapshot]=useState<RubySnapshot|null>(null);
 const [speed,setSpeed]=useState<SunsetSpeed>('normal');
 const speedRef=useRef<SunsetSpeed>('normal');
 const [autoCount,setAutoCount]=useState(10);
 const [autoActive,setAutoActive]=useState(false);
 const [autoLeft,setAutoLeft]=useState(0);
 const [stopBonus,setStopBonus]=useState(true);
 const autoRef=useRef({active:false,remaining:0,lastHandled:''});
 const [nearMiss,setNearMiss]=useState(false);
 const [feedback,setFeedback]=useState<Feedback>('none');
 const [settledId,setSettledId]=useState<string|null>(null);
 const [transfer,setTransfer]=useState<Transfer|null>(null);
 const balanceRef=useRef<HTMLDivElement>(null);
 const resultAreaRef=useRef<HTMLDivElement>(null);
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
 const herdForeground=useRef<HTMLDivElement>(null),herdBackground=useRef<HTMLDivElement>(null);
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
  setSettledId(id);
  setNearMiss(isElephantNearMiss(result.baseGrid)&&!result.elephantBonus);
  const outcome=getFeedback(result.net);
  setFeedback(outcome);
  const source=resultAreaRef.current?.getBoundingClientRect();
  const destination=balanceRef.current?.getBoundingClientRect();
  // Positive net wins travel into the wallet. A loss remains a local red
  // deduction inside the wallet panel — never a flying overlay.
  if(source&&destination&&outcome==='win'&&!reduced){
   const x=source.left+source.width/2,y=source.top+source.height/2;
   setTransfer({id,amount:Number(result.net),x,y,
    dx:destination.left+destination.width/2-x,
    dy:destination.top+destination.height/2-y});
  }else setTransfer(null);
  schedule(()=>{setFeedback('none');setTransfer(null);},1650);
  if(Number(result.payout)>0)
   makeSound(Number(result.payout)>=Number(result.bet)*30?'big-win':'win');
 },[makeSound,reduced,schedule]);
 const onBonusStop=useCallback((id:string,cell:number)=>{
  const result=resultRef.current;
  if(!result||result.id!==id||!pendingBonus.current.delete(cell))return;
  setRollingCells(previous=>previous.filter(index=>index!==cell));
  setGrid(previous=>{
   const next=previous.map((symbol,index)=>index===cell?result.finalGrid[index]:symbol);
   gridRef.current=next;return next;
  });
  makeSound('stop');
  if(pendingBonus.current.size===0)schedule(()=>finishSpin(id),bonusFinishPause(speedRef.current));
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
  if(elephantCount>0&&!anticipated.current
    &&stoppedReels.current.size<5){
   anticipated.current=true;setAnticipation(true);makeSound('anticipation');
  }
  if(stoppedReels.current.size!==5)return;
  setAnticipation(false);
  if(!result.elephantBonus){schedule(()=>finishSpin(id),190);return;}
  setHeld(result.heldCells);setPhase('bonus-intro');
  makeSound('bonus');makeSound('hoof');
  schedule(()=>{
   if(resultRef.current?.id!==id)return;
   const free=Array.from({length:15},(_,index)=>index)
    .filter(index=>!result.heldCells.includes(index));
   if(free.length===0){finishSpin(id);return;}
   pendingBonus.current=new Set(free);
   setRollingCells(free);setPhase('bonus');
  },bonusStartPause(speedRef.current));
 },[finishSpin,makeSound,schedule]);
 const send=useCallback((intent:RubyIntent)=>{
  const current=snapshotRef.current;
  if(!current||pendingRef.current||uncertain||
   phase==='awaiting'||phase==='base'||phase==='bonus-intro'||phase==='bonus')return false;
  if(intent.action==='spin'&&!current.state.canSpin)return false;
  const id=sendRuby(current,intent);
  pendingRef.current={id,expectedRevision:current.revision,action:intent.action,ackRevision:null};
  setPending(true);setNotice('');
  if(intent.action==='spin'){
   clearTimers();resultRef.current=null;setSpinResult(null);
   setFeedback('none');setSettledId(null);setTransfer(null);setNearMiss(false);
   stoppedReels.current.clear();pendingBonus.current.clear();anticipated.current=false;
   setAnticipation(false);setWinners([]);setHeld([]);setRollingReels([]);setRollingCells([]);
   setTargetBase(null);setTargetBonus(null);setPhase('awaiting');
   makeSound('spin');
  }else if(intent.action==='bet.set')makeSound('bet');
  return true;
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
     autoRef.current.active=false;setAutoActive(false);setAutoLeft(0);
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
 },[snapshot?.session,snapshot?.state.lastSpin?.id,clearTimers,finishSpin,reduced]);
 useEffect(()=>{
  if(!pending)return;
  const id=setTimeout(()=>{
   if(!pendingRef.current)return;
   autoRef.current.active=false;setAutoActive(false);setAutoLeft(0);
   setUncertain(true);setNotice(languages[snapshotRef.current?.state.language??'de'].err);
   setPending(false);setPhase('idle');
  },10000);
  return()=>clearTimeout(id);
 },[pending]);
 const stopAutoSpins=useCallback(()=>{
  autoRef.current.active=false;autoRef.current.remaining=0;
  setAutoActive(false);setAutoLeft(0);
 },[]);
 const close=useCallback(()=>{
  autoRef.current.active=false;
  window.chrome?.webview?.postMessage({type:'indian-ruby.close'});
 },[]);
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
 const setSlotSpeed=useCallback((next:SunsetSpeed)=>{
  if(phase==='base'||phase==='bonus'||phase==='bonus-intro'||phase==='awaiting'||autoRef.current.active)return;
  speedRef.current=next;setSpeed(next);
 },[phase]);
 const startAutoSpins=useCallback(()=>{
  if(autoRef.current.active||pendingRef.current||rules||uncertain)return;
  if(!send({action:'spin'}))return;
  autoRef.current={active:true,remaining:autoCount-1,lastHandled:''};
  setAutoActive(true);setAutoLeft(autoCount);
 },[autoCount,rules,send,uncertain]);
 useEffect(()=>{
  const result=spinResult;
  if(phase!=='result'||!result||pending||!autoRef.current.active||
   autoRef.current.lastHandled===result.id)return;
  autoRef.current.lastHandled=result.id;
  if((result.elephantBonus&&stopBonus)||autoRef.current.remaining<=0||
   !snapshot?.state.canSpin||rules||uncertain){
   stopAutoSpins();return;
  }
  schedule(()=>{
   if(!autoRef.current.active||pendingRef.current||rules||uncertain)return;
   if(send({action:'spin'})){
    autoRef.current.remaining--;
    setAutoLeft(autoRef.current.remaining+1);
   }else stopAutoSpins();
  },speed==='turbo'?300:speed==='fast'?520:790);
 },[phase,spinResult?.id,pending,snapshot?.state.canSpin,send,schedule,stopAutoSpins,
    speed,stopBonus,rules,uncertain]);
 useEffect(()=>{if(rules&&autoRef.current.active)stopAutoSpins();},[rules,stopAutoSpins]);

 const de=state?.language!=='en',t=languages[de?'de':'en'];
 const animating=phase==='awaiting'||phase==='base'||phase==='bonus-intro'||phase==='bonus';
 const canInteract=!!snapshot&&!pending&&!uncertain&&!animating;
 // While a bet command is pending the clicks are guarded by pendingRef;
 // do not toggle the visual disabled state (the original flicker source).
 const canBet=!!snapshot&&!uncertain&&!animating&&!autoActive;
 const idx=state?.bets.indexOf(state.bet)??-1;
 const onBet=useCallback((value:string)=>send({action:'bet.set',value}),[send]);
 const winAmount=spinResult?.payout??'0';
 const hasWin=phase==='result'&&!!spinResult&&Number(winAmount)>0;

 const isHerd=phase==='bonus-intro'||phase==='bonus';
 const bonusLive=isHerd||phase==='result'&&!!spinResult?.elephantBonus;
 const herdPhase=phase==='base'?'prepare':phase==='bonus-intro'?'intro':phase==='bonus'?'background':null;
 const motionEnabled=!reduced&&!!state?.animationsEnabled;
 return <div className="ruby-shell ruby-premium-shell"
   data-reduced={reduced||!state?.animationsEnabled} data-phase={phase}
   data-bonus={bonusLive?'true':'false'}>
  <RubyScene herdBackground={herdBackground}/>
  {motionEnabled&&spinResult?.elephantBonus&&herdPhase&&
   <RubyBonusHerd key={spinResult.id} phase={herdPhase} introDuration={bonusStartPause(speed)}
    foreground={herdForeground} background={herdBackground}/>}
  <div className="ruby-atmosphere" aria-hidden="true">
   {Array.from({length:17},(_,i)=><i key={i} style={{
    left:((i*37+8)%96)+'%',top:((i*27+9)%83)+'%',
    animationDelay:(i*.49)+'s',
   }}/>)}
  </div>
  <header className="ruby-header">
   <button type="button" className="ruby-back" onClick={close}>← {t.back}</button>
   <div className="ruby-title sunset-title-lockup">
    <small className="sunset-title-strap">{t.strap}</small>
    <h1 className="sunset-title">SUNSET <span>RUBY</span></h1>
    <p className="sunset-title-subtitle">{t.subtitle}</p>
   </div>
   <div className={'ruby-top-balance sunset-balance'+(feedback!=='none'?' feedback-'+feedback:'')}
    ref={balanceRef}><small>{t.money}</small>
    <AnimatedBankroll balance={state?.bankroll??'0'} spin={spinResult}
     settled={!!spinResult&&settledId===spinResult.id}
     reduced={reduced||!state?.animationsEnabled}/>
    {feedback==='loss'&&spinResult&&settledId===spinResult.id&&
     !(reduced||!state?.animationsEnabled)&&
     <span key={spinResult.id} className="sunset-bankroll-debit" aria-hidden="true">
      −{cash(Math.abs(Number(spinResult.net)))}
     </span>}
   </div>
  </header>
  <main className="ruby-main">
   <aside className="ruby-story">
    <div className="ruby-story-crest"><RubySymbolArt symbol="Elephant"/></div>
    <p className="ruby-eyebrow">{t.bonusInfo}</p>
    <h2>{phase==='bonus-intro'?t.herd:t.bonus}</h2>
    <p>{t.elephants}</p>
    <div className="ruby-story-diamonds" aria-hidden="true">✦ ◆ ✦</div>
   </aside>
   <section className={'ruby-machine'+(isHerd?' bonus-illuminated':'')+
    (hasWin?' win-illuminated':'')+(feedback!=='none'?' outcome-'+feedback:'')}
    aria-label={t.tag}>
    <RubyLamps/>
    <div className="ruby-machine-top">
     <span className="ruby-marquee-left" aria-hidden="true">❖</span>
     <div><strong className="sunset-cabinet-crest">✦ {t.marquee} ✦</strong></div>
     <span className="ruby-marquee-right" aria-hidden="true">❖</span>
    </div>
    <div className="ruby-reel-bezel">
     <div className="ruby-side-jewels left" aria-hidden="true"><i/><i/><i/></div>
     <RubyReelStage displayGrid={grid} targetBase={targetBase} targetBonus={targetBonus}
      spinId={spinResult?.id??null} rollingReels={rollingReels} rollingCells={rollingCells}
      heldCells={held} winningLines={winners}
      showWins={phase==='result'&&winners.length>0}
      anticipation={anticipation} nearMiss={isElephantNearMiss(spinResult?.baseGrid??[])}
      speed={speed} reducedMotion={reduced||!state?.animationsEnabled}
      onBaseStop={onBaseStop} onBonusStop={onBonusStop} herdForeground={herdForeground}/>
     <div className="ruby-side-jewels right" aria-hidden="true"><i/><i/><i/></div>
    </div>
    <div ref={resultAreaRef} className={'ruby-result'+(hasWin?' has-win':'')} role="status" aria-live="polite">
     <div className="ruby-result-title">
      {isHerd?(phase==='bonus-intro'?t.herd:t.bonus):anticipation?t.anticipation:animating?t.rolling:
       hasWin?t.total:nearMiss?t.nearMiss:spinResult?t.noWin:notice||t.ready}
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
      disabled={!state?.canSpin||animating||uncertain||autoActive||pending}
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
    <div className="ruby-detail-card sunset-auto-panel">
     <h2>{t.auto} · {t.speed}</h2>
     <div className="sunset-speed-tabs" role="group" aria-label={t.speed}>
      {(['normal','fast','turbo'] as const).map(option=><button key={option}
       type="button" className={speed===option?'selected':''}
       aria-pressed={speed===option} disabled={animating||autoActive||pending}
       onClick={()=>setSlotSpeed(option)}>
       {t[option]}</button>)}
     </div>
     <div className="sunset-auto-row">
      <label htmlFor="sunset-auto-count">{t.autoCount}</label>
      <select id="sunset-auto-count" value={autoCount} disabled={autoActive||animating||pending}
       onChange={event=>setAutoCount(Number(event.currentTarget.value))}>
       {[10,25,50,100].map(count=><option key={count} value={count}>{count}</option>)}
      </select>
     </div>
     <label className="sunset-stop-bonus">
      <input type="checkbox" checked={stopBonus} disabled={autoActive}
       onChange={event=>setStopBonus(event.currentTarget.checked)}/>
      {t.stopBonus}
     </label>
     <button className={'sunset-auto-trigger'+(autoActive?' running':'')} type="button"
      disabled={!autoActive&&(!canInteract||!state?.canSpin||rules)}
      onClick={autoActive?stopAutoSpins:startAutoSpins}>
      {autoActive?t.autoStop:t.autoStart}
     </button>
     {autoActive&&<p className="sunset-auto-left">{t.autoLeft}: {autoLeft}</p>}
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
  {transfer&&<div key={transfer.id} className="sunset-credit-transfer win"
    aria-hidden="true" style={{left:transfer.x,top:transfer.y,
     '--flow-x':transfer.dx+'px','--flow-y':transfer.dy+'px'} as CSSProperties}>
    <span>+{cash(transfer.amount)}</span>
   </div>}
  {rules&&<SunsetRules state={state??null} language={de?'de':'en'}
    close={()=>setRules(false)}/>}
 </div>;
}
createRoot(document.getElementById('root')!).render(<App/>);
