import {finishModeBoot} from '../boot/finishModeBoot';
import {useCallback,useEffect,useRef,useState} from 'react';
import {createRoot} from 'react-dom/client';
import {listenReels,sendReelsIntent} from './bridge';
import type {ReelsIntent,ReelsSnapshot,ReelsSpin,ReelSymbol} from './bridge';
import {ReelIcon} from './symbols';
import {ReelsVfx} from './ReelsVfx';
import {AnimatedReadout} from './AnimatedReadout';
import {armReelsCelebrationAudio,disposeReelsCelebrationAudio,playReelsCelebrationAudio} from './reelsCelebrationAudio';
import {AUTO_OPTIONS,SPEED_OPTIONS,SPEED_PRESETS,canQueueAutoSpin,readReelsSpeed,
  reactionFromSpin,reelTravel,saveReelsSpeed} from './reelsMotion';
import type {ReelsSpeed,ReelsReaction} from './reelsMotion';
import './reels.css';
import './reels-design.css';

const CHIPS=[1,5,25,100,500,1000];
const money=(value:string)=>'$'+Number(value).toLocaleString('en-US',{minimumFractionDigits:2,maximumFractionDigits:2});
const vocabulary={
  en:{back:'Casino lobby',tag:'THREE REELS · ONE PAYLINE · PLAY MONEY',balance:'SHARED BANKROLL',
    wager:'BET PER SPIN',spin:'SPIN',spinning:'SPINNING…',paytable:'THE PAYTABLE',
    paytableHint:'Three identical symbols on the centre payline',returnStake:'Two cherries return the stake.',
    rules:'Payouts include the wager. Only the centre row pays.',
    rtp:'Independent weighted reels · theoretical return 97.05%',
    stats:'YOUR SLOT RECORD',spins:'Spins',wins:'Wins',pushes:'Pushes',net:'Slot net',best:'Best return',
    xp:'Every spin: 20 XP + 15 Season XP. Net wins give 5 extra XP of each.',
    sound:'Slot sounds',animations:'Reel and light animations',offline:'Offline · play money only',
    hint:'Choose a bet and spin. Space also works.',ready:'The reels are waiting.',
    noMatch:'No match this time.',push:'Two cherries · stake returned',win:'WIN',
    jackpot:'MIDNIGHT JACKPOT',out:'Not enough play money.',uncertain:'Confirmation missing. Reopen the mode before another spin.',
    levelUp:'LEVEL UP',loading:'Preparing your slot cabinet…',
    auto:'AUTO SPINS',autoStop:'STOP AUTO',left:'LEFT',speed:'SLOT SPEED',
    normal:'Normal',fast:'Fast',turbo:'Turbo'},
  de:{back:'Casino-Lobby',tag:'DREI WALZEN · EINE GEWINNLINIE · SPIELGELD',balance:'GEMEINSAMES GUTHABEN',
    wager:'EINSATZ PRO DREH',spin:'DREHEN',spinning:'DREHT…',paytable:'GEWINNTABELLE',
    paytableHint:'Drei gleiche Symbole auf der mittleren Gewinnlinie',returnStake:'Zwei Kirschen zahlen den Einsatz zurück.',
    rules:'Auszahlungen enthalten den Einsatz. Nur die mittlere Reihe zählt.',
    rtp:'Unabhängige gewichtete Walzen · theoretische Rückgabe 97,05 %',
    stats:'DEINE AUTOMATENSTATISTIK',spins:'Drehs',wins:'Gewinne',pushes:'Gleichstand',net:'Automaten-Netto',best:'Höchste Auszahlung',
    xp:'Pro Dreh: 20 XP + 15 Saison-XP. Nettogewinne bringen jeweils 5 zusätzliche XP.',
    sound:'Automatensounds',animations:'Walzen- und Lichtanimationen',offline:'Offline · nur Spielgeld',
    hint:'Einsatz wählen und drehen. Leertaste funktioniert ebenfalls.',ready:'Die Walzen warten.',
    noMatch:'Diesmal kein Treffer.',push:'Zwei Kirschen · Einsatz zurück',win:'GEWINN',
    jackpot:'MIDNIGHT JACKPOT',out:'Nicht genug Spielgeld.',uncertain:'Bestätigung fehlt. Öffne den Modus vor einem weiteren Dreh erneut.',
    levelUp:'LEVELAUFSTIEG',loading:'Der Spielautomat wird vorbereitet…',
    auto:'AUTO-DREHS',autoStop:'AUTO STOPP',left:'ÜBRIG',speed:'SPIELTEMPO',
    normal:'Normal',fast:'Schnell',turbo:'Turbo'}
};
type Movement={base:number[];travel:number[];target:number[];cell:number;running:boolean;id:string};
function ReelsApp(){
  const [snapshot,setSnapshot]=useState<ReelsSnapshot|null>(null);
  const [pending,setPending]=useState<string|null>(null);
  const [message,setMessage]=useState<string|null>(null);
  const [uncertain,setUncertain]=useState(false);
  const [animating,setAnimating]=useState(false);
  const [stops,setStops]=useState([4,4,4]);
  const [movement,setMovement]=useState<Movement|null>(null);
  const current=useRef<ReelsSnapshot|null>(null);
  const pendingRef=useRef<string|null>(null);
  const shown=useRef([4,4,4]);
  const lastId=useRef<string|null>(null);
  const seenWin=useRef<string|null>(null);
  const [winBurst,setWinBurst]=useState<string|null>(null);
  const [speed,setSpeed]=useState<ReelsSpeed>(readReelsSpeed);
  const [autoActive,setAutoActive]=useState(false);
  const [autoRemaining,setAutoRemaining]=useState(0);
  const [revealedId,setRevealedId]=useState<string|null>(null);
  const [reaction,setReaction]=useState<{id:string;kind:ReelsReaction}|null>(null);
  const autoRef=useRef({active:false,remaining:0});
  const animatingRef=useRef(false);
  const autoTimer=useRef<ReturnType<typeof setTimeout>|null>(null);
  const reactionTimer=useRef<ReturnType<typeof setTimeout>|null>(null);
  const timers=useRef<ReturnType<typeof setTimeout>[]>([]);
  const cancelTimers=useCallback(()=>{for(const id of timers.current)clearTimeout(id);timers.current=[];},[]);
  const stopAuto=useCallback(()=>{
    autoRef.current={active:false,remaining:0};
    if(autoTimer.current!==null){clearTimeout(autoTimer.current);autoTimer.current=null;}
    setAutoActive(false);setAutoRemaining(0);
  },[]);
  useEffect(()=>{
    const unsub=listenReels(event=>{
      if(event.type==='snapshot'){
        if(!current.current){
          // Reopening a mode must never replay a previously committed result.
          const previous=event.data.state.lastSpin;
          seenWin.current=previous?.id??null;
          lastId.current=previous?.id??null;
          shown.current=previous?.stops.slice()??[4,4,4];
          setStops(shown.current.slice());
          setRevealedId(previous?.id??null);
        }
        if(current.current?.session===event.data.session&&event.data.revision<current.current.revision)return;
        current.current=event.data;setSnapshot(event.data);
      }else if(event.data.requestId===pendingRef.current){
        pendingRef.current=null;setPending(null);setUncertain(false);
        if(!event.data.success){
          animatingRef.current=false;setAnimating(false);stopAuto();
          setMessage(event.data.message);
        }
        else setMessage(null);
      }
    });
    // If the native surface was not visible at the first message, request the
    // initial snapshot again instead of waiting for the 18-second watchdog.
    const announce=()=>window.chrome?.webview?.postMessage({type:'reels.ready'});
    announce();
    const retry=window.setInterval(()=>{if(!current.current)announce();},1200);
    return()=>{window.clearInterval(retry);unsub();cancelTimers();};
  },[cancelTimers]);
  useEffect(()=>{if(snapshot){finishModeBoot();window.chrome?.webview?.postMessage({type:'reels.ui-ready'});}},[!!snapshot]);
  useEffect(()=>{
    if(!pending)return;
    const timer=setTimeout(()=>{
      stopAuto();setUncertain(true);animatingRef.current=false;setAnimating(false);
      setMessage(vocabulary[current.current?.state.language??'en'].uncertain);
    },8000);
    return()=>clearTimeout(timer);
  },[pending,stopAuto]);
  const last=snapshot?.state.lastSpin;
  useEffect(()=>{
    if(!snapshot||!last||last.id===lastId.current)return;
    lastId.current=last.id;cancelTimers();
    const target=last.stops.slice();
    const finish=()=>{
      shown.current=target;setStops(target);setMovement(null);
      animatingRef.current=false;setAnimating(false);setRevealedId(last.id);
      const outcome=reactionFromSpin(last);
      if(reactionTimer.current!==null)clearTimeout(reactionTimer.current);
      if(outcome!=='push'&&outcome!=='none'){
        setReaction({id:last.id,kind:outcome});
        reactionTimer.current=setTimeout(()=>setReaction(v=>v?.id===last.id?null:v),1400);
      }else setReaction(null);
      if(outcome==='bigwin'&&snapshot.state.soundEnabled&&
        !window.matchMedia?.('(prefers-reduced-motion: reduce)').matches)
        playReelsCelebrationAudio(last.multiplier>=50);
      window.chrome?.webview?.postMessage({type:'reels.animation-complete',spinId:last.id});
    };
    if(!snapshot.state.animationsEnabled||window.matchMedia?.('(prefers-reduced-motion: reduce)').matches){
      finish();return;
    }
    const from=shown.current.slice();
    const travel=target.map((n,i)=>reelTravel(from[i],n));
    const cell=document.querySelector('.reels-symbol')?.getBoundingClientRect().height||104;
    animatingRef.current=true;setAnimating(true);
    setMovement({base:from,travel,target,cell,running:false,id:last.id});
    let nextFrame=0;
    const raf=window.requestAnimationFrame(()=>{nextFrame=window.requestAnimationFrame(()=>
      setMovement(p=>p?.id===last.id?{...p,running:true}:p));});
    const delays=SPEED_PRESETS[speed];
    delays.forEach((delay,i)=>timers.current.push(setTimeout(()=>
      window.chrome?.webview?.postMessage({type:'reels.reel-stop',spinId:last.id,reel:i}),delay)));
    timers.current.push(setTimeout(finish,delays[2]+70));
    return()=>{window.cancelAnimationFrame(raf);window.cancelAnimationFrame(nextFrame);cancelTimers();};
  },[last?.id,snapshot?.state.animationsEnabled,speed,cancelTimers]);
  useEffect(()=>{
    if(!snapshot||!last||animating||!snapshot.state.animationsEnabled)return;
    if(seenWin.current===last.id)return;
    seenWin.current=last.id;
    if(Number(last.net)<=0)return;
    setWinBurst(last.id);
    const timer=window.setTimeout(()=>setWinBurst(id=>id===last.id?null:id),1700);
    return()=>window.clearTimeout(timer);
  },[snapshot?.state.lastSpin?.id,animating,snapshot?.state.animationsEnabled]);
  const act=useCallback((intent:ReelsIntent):boolean=>{
    const state=current.current;
    if(!state||pendingRef.current||uncertain||animatingRef.current)return false;
    if(intent.action==='spin'&&!state.state.canSpin)return false;
    if(intent.action==='spin'&&state.state.soundEnabled)armReelsCelebrationAudio();
    const id=sendReelsIntent(state,intent);
    pendingRef.current=id;setPending(id);setMessage(null);
    if(intent.action==='spin'){
      animatingRef.current=true;setAnimating(true);setReaction(null);
    }
    return true;
  },[uncertain]);
  const close=useCallback(()=>{
    stopAuto();
    if(pendingRef.current)return;
    cancelTimers();window.chrome?.webview?.postMessage({type:'reels.close'});
  },[cancelTimers,stopAuto]);
  const startAuto=useCallback((count:number)=>{
    if(!current.current?.state.canSpin||pendingRef.current||animatingRef.current||uncertain
      ||autoRef.current.active||!AUTO_OPTIONS.some(v=>v===count))return;
    if(current.current.state.soundEnabled)armReelsCelebrationAudio();
    autoRef.current={active:true,remaining:count};
    setAutoActive(true);setAutoRemaining(count);
  },[uncertain]);
  useEffect(()=>{
    if(!autoActive)return;
    if(autoRemaining<=0){
      if(!pendingRef.current&&!animatingRef.current)stopAuto();
      return;
    }
    const currentState=current.current;
    if(!currentState||uncertain){stopAuto();return;}
    if(!currentState.state.canSpin&&!pendingRef.current&&!animatingRef.current){
      stopAuto();return;
    }
    if(!canQueueAutoSpin({
      active:autoRef.current.active,remaining:autoRef.current.remaining,
      pending:!!pendingRef.current,animating:animatingRef.current,uncertain,
      canSpin:currentState.state.canSpin,
      latestId:currentState.state.lastSpin?.id??null,revealedId,
    }))return;
    // Deliberately pace sequential commits. A native receipt + final reel
    // presentation must both finish before the next bet is ever sent.
    const delay=speed==='turbo'?80:speed==='fast'?130:220;
    autoTimer.current=setTimeout(()=>{
      autoTimer.current=null;
      const latest=current.current;
      if(!latest||!canQueueAutoSpin({
        active:autoRef.current.active,remaining:autoRef.current.remaining,
        pending:!!pendingRef.current,animating:animatingRef.current,uncertain,
        canSpin:latest.state.canSpin,
        latestId:latest.state.lastSpin?.id??null,revealedId,
      })){return;}
      if(act({action:'spin'})){
        autoRef.current.remaining--;
        setAutoRemaining(autoRef.current.remaining);
      }
    },delay);
    return()=>{
      if(autoTimer.current!==null){clearTimeout(autoTimer.current);autoTimer.current=null;}
    };
  },[autoActive,autoRemaining,pending,animating,uncertain,snapshot?.revision,
     revealedId,speed,act,stopAuto]);
  useEffect(()=>()=>{stopAuto();if(reactionTimer.current!==null)clearTimeout(reactionTimer.current);
    disposeReelsCelebrationAudio();},[stopAuto]);
  useEffect(()=>{
    const key=(e:KeyboardEvent)=>{
      if(e.repeat||e.ctrlKey||e.altKey||e.metaKey
        ||e.target instanceof HTMLButtonElement||e.target instanceof HTMLInputElement
        ||e.target instanceof HTMLSelectElement)return;
      if(e.key==='Escape'){e.preventDefault();close();}
      if(e.code==='Space'){e.preventDefault();act({action:'spin'});}
    };
    document.addEventListener('keydown',key);
    return()=>document.removeEventListener('keydown',key);
  },[act,close]);
  if(!snapshot)return <div className="reels-loading" data-testid="reels-loading">
    <div>✦ 777 ✦</div><h1>MIDNIGHT REELS</h1>
    <p>{vocabulary.en.loading}</p><button onClick={close}>← Casino lobby</button>
  </div>;
  const s=snapshot.state,t=vocabulary[s.language],result:ReelsSpin|null=s.lastSpin;
  const locked=!!pending||uncertain||animating;
  const feedback=result?.multiplier===100?t.jackpot+' · '+money(result.payout):
    result&&Number(result.net)>0?t.win+' · '+result.multiplier+'× · '+money(result.payout):
    result?.multiplier===1?t.push:result?t.noMatch:t.ready;
  const columns=(index:number):ReelSymbol[]=>{
    const strip=s.reelStrip;
    if(!movement)return [-1,0,1].map(offset=>strip[(stops[index]+offset+32)%32]);
    return Array.from({length:movement.travel[index]+3},(_,n)=>
      strip[(movement.base[index]+n-1+32)%32]);
  };
  return <div className="reels-app" lang={s.language} data-language={s.language}
    data-testid="reels-app" data-motion={s.animationsEnabled}
     data-win-tier={!animating&&result&&Number(result.net)>0?(result.multiplier>=25?'jackpot':'win'):'none'}
     data-animating={animating} data-bet-pending={!!pending&&!animating} data-spin-id={result?.id??''}>
    <header className="reels-header">
      <button className="reels-back" onClick={close}><span aria-hidden="true">‹</span> {t.back} <kbd>ESC</kbd></button>
      <div className="reels-header-signature" aria-label="The Midnight Club">
        <span aria-hidden="true">♠</span><small>THE MIDNIGHT CLUB</small>
      </div>
      <div className="reels-header-wallet"><small>{t.balance}</small><strong>{money(s.bankroll)}</strong></div>
    </header>
    <div className="reels-body">
      <main className="reels-cabinet">
        <div className="reels-atmosphere" aria-hidden="true"><i/><i/><i/><i/><i/><i/></div>
        <ReelsVfx enabled={s.animationsEnabled} spinning={animating} celebrationId={winBurst}
          jackpot={!!result&&result.multiplier===100}/>
        <div className="reels-crown-engraving" aria-hidden="true"><i/><span>✧</span><i/></div>
        <div className="reels-brand">
          <span className="reels-brand-wing" aria-hidden="true">✦</span>
          <div className="reels-brand-name">
            <small>THE MIDNIGHT CLUB</small>
            <h2>MIDNIGHT <em>REELS</em></h2>
          </div>
          <span className="reels-brand-wing" aria-hidden="true">✦</span>
        </div>
        <p className="reels-tagline">{t.tag}</p>
        <div className={'reels-lightband'+(!animating&&result?.multiplier===100?' jackpot':'')} aria-hidden="true">
          {Array.from({length:11},(_,i)=><i key={i}/>)}
        </div>
        <div className="reels-stage" data-testid="reels-display">
          <div className="reels-stage-illumination" aria-hidden="true"><i/><i/><i/></div>
          <div className="reels-stage-etching" aria-hidden="true"><span>Ⅰ</span><span>Ⅱ</span><span>Ⅲ</span></div>
          <div className="reels-stage-glass" aria-hidden="true"/>
          {winBurst===result?.id&&<div key={winBurst} className={'reels-win-burst'+
             (result&&result.multiplier>=25?' jackpot':'')} aria-hidden="true">
             {Array.from({length:16},(_,i)=><i key={i} style={{
              '--spark-angle':(i*22.5)+'deg','--spark-distance':(65+i%4*22)+'px',
              '--spark-delay':(i%5*34)+'ms',
             } as React.CSSProperties}/>)}
             <span>{result?.multiplier===100?'100×':result?.multiplier+'×'}</span>
           </div>}
          <div className="reels-payline" aria-hidden="true"/>
          <span className="reels-pointer left" aria-hidden="true">▸</span>
          <div className="reels-grid">
            {[0,1,2].map(i=><div className="reels-reel" key={i}>
              <div className="reels-strip" style={{
                transform:movement?.running?'translateY(-'+(movement.travel[i]*movement.cell)+'px)':'translateY(0px)',
                transition:movement?.running?'transform '+STOP_DELAYS[i]+'ms cubic-bezier(.13,.64,.18,1)':'none'
              }}>
                {columns(i).map((symbol,j)=><div className="reels-symbol" key={j} data-symbol={symbol}>
                  <ReelIcon symbol={symbol}/>
                </div>)}
              </div>
            </div>)}
          </div>
          <span className="reels-pointer right" aria-hidden="true">◂</span>
          <div className="reels-stage-shadow" aria-hidden="true"/>
        </div>
        <div className="reels-feedback" role="status" aria-live="polite">
          <strong>{message??(animating?t.spinning:feedback)}</strong>
          <small>{!animating&&result?('+'+result.playerXp+' XP · +'+result.seasonXp+' Season XP'+(result.levelUp?' · '+t.levelUp:'')):t.hint}</small>
        </div>
        <div className="reels-cabinet-footer">
          <div className="reels-balances">
            <div><small>{t.balance}</small><strong>{money(s.bankroll)}</strong></div>
            <div><small>{t.wager}</small><strong>{money(s.bet)}</strong></div>
            <div className="reels-last-return"><small>{t.win}</small><strong>{money(result?.payout??'0')}</strong></div>
          </div>
          <div className="reels-chips" role="group" aria-label={t.wager}>
            {CHIPS.map(value=><button key={value} className={Number(s.bet)===value?'selected':''}
              disabled={locked||value>Number(s.bankroll)}
              onClick={()=>act({action:'bet.set',value:String(value)})}>
                {'$'+value.toLocaleString('en-US')}
              </button>)}
          </div>
          <button className="reels-spin" data-testid="reels-spin" disabled={locked||!s.canSpin}
            onClick={()=>act({action:'spin'})}>
            {animating?t.spinning:s.canSpin?t.spin:t.out}<kbd>SPACE</kbd>
          </button>
        </div>
      </main>
      <aside className="reels-sidebar">
        <section className="reels-paytable">
          <div className="reels-section-heading"><span aria-hidden="true">✧</span><h2>{t.paytable}</h2></div>
          <p>{t.paytableHint}</p>
          {s.paytable.slice().reverse().map(item=><div className="reels-payrow" key={item.id}>
            <div className="reels-payicon"><ReelIcon symbol={item.id}/></div>
            <span>{item.name} × 3</span><strong>{item.tripleMultiplier}×</strong>
          </div>)}
          <p>{t.returnStake}</p><p>{t.rules}</p><p>{t.rtp}</p>
        </section>
        <section className="reels-profile">
          <div className="reels-section-heading"><span aria-hidden="true">♠</span><h2>{t.stats}</h2></div>
          <div className="reels-level">PRESTIGE {s.profile.prestige} · LEVEL {s.profile.level} · ◆ {s.profile.coins.toLocaleString()}</div>
          <div className="reels-stats">
            <div>{t.spins}<strong>{s.stats.spins.toLocaleString()}</strong></div>
            <div>{t.wins}<strong>{s.stats.wins.toLocaleString()}</strong></div>
            <div>{t.pushes}<strong>{s.stats.pushes.toLocaleString()}</strong></div>
            <div>{t.net}<strong>{money(s.stats.netProfit)}</strong></div>
          </div>
          <p>{t.best}: <strong>{money(s.stats.biggestPayout)}</strong></p>
          <small>{t.xp}</small>
        </section>
        <section className="reels-settings">
          <label><input data-testid="reels-sound" type="checkbox" checked={s.soundEnabled} disabled={locked}
            onChange={e=>act({action:'sound.set',value:String(e.target.checked)})}/>{t.sound}</label>
          <label><input data-testid="reels-animation" type="checkbox" checked={s.animationsEnabled} disabled={locked}
            onChange={e=>act({action:'animations.set',value:String(e.target.checked)})}/>{t.animations}</label>
          <small>{t.offline}</small>
        </section>
      </aside>
    </div>
  </div>;
}
createRoot(document.getElementById('root')!).render(<ReelsApp/>);
