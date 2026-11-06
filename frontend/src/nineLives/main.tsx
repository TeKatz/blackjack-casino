import {useCallback,useEffect,useRef,useState} from 'react';
import {createRoot} from 'react-dom/client';
import {listenNineLives,sendNineLivesIntent,paylineCells,symbols} from './bridge';
import type {NineLivesIntent,NineLivesSnapshot,NineLivesSpin,NineLivesSymbol} from './bridge';
import {RooftopAmbient} from './Ambient';
import './nineLives.css';

const PREVIEW:NineLivesSymbol[]=['BlackCat','Yarn','Fishbone','WhiteCat','Bonus','Bell','Fishbone','TabbyCat','Wild'];
const ART:Record<NineLivesSymbol,string>={
 Yarn:'yarn.jpg',Fishbone:'fishbone.jpg',Bell:'bell.jpg',TabbyCat:'tabby-cat.jpg',
 WhiteCat:'white-cat.jpg',BlackCat:'black-cat.jpg',Wild:'wild.jpg',Bonus:'bonus.jpg',
};
const money=(v:string)=>'$'+Number(v).toLocaleString('en-US',{minimumFractionDigits:2,maximumFractionDigits:2});
const dict={
 en:{back:'Casino lobby',top:'THE MOONLIT ROOFTOP',tag:'3 REELS · 5 PAYLINES · PLAY MONEY',
 balance:'SHARED BANKROLL',bet:'BET PER SPIN',win:'LAST WIN',spin:'SPIN',free:'FREE SPIN',
 stop:'STOP AUTO',auto:'AUTO ×20',max:'MAX',paytable:'PAYTABLE',settings:'SETTINGS',
 status:'The rooftop is waiting.',spinning:'The cats are on the move…',noWin:'The rooftop goes quiet.',
 near:'Two lives. The third stayed in the shadows.',winLabel:'WIN',frenzy:'FULL GRID FRENZY',
 awakened:'NINE LIVES AWAKENED',bonus:'FREE SPINS REMAINING',pounce:'POUNCE WILD',
 lines:'WINNING LINES',paylineNote:'Three matching symbols on any of five lines. Wild substitutes for all except Bonus.',
 bonusNote:'Three or more Nine Lives symbols award nine bonus spins. Free spins retain the trigger bet.',
 frenzyNote:'A full matching grid (including wild substitutes) pays 5,000×.',
 settingSound:'Game sound effects',settingMotion:'Reel, light and win animations',
 offline:'Offline · no real money',help:'SPACE spins · A toggles auto · +/− changes bet · ESC returns',
 stats:'YOUR RECORD',spins:'Spins',wins:'Wins',bonuses:'Bonus spins',pounces:'Pounce Wilds',
 frenzyCount:'Full Grid Frenzies',best:'Highest payout',net:'Net result',loading:'Preparing the rooftop…',
 uncertain:'Spin acknowledgement missing. Reopen Nine Lives to safely recover the C# result.',
 unaffordable:'Insufficient bankroll',xp:'Player XP',season:'Season XP',level:'LEVEL UP'},
 de:{back:'Casino-Lobby',top:'DAS MONDBESCHIENENE DACH',tag:'3 WALZEN · 5 GEWINNLINIEN · SPIELGELD',
 balance:'GEMEINSAMES GUTHABEN',bet:'EINSATZ PRO DREH',win:'LETZTER GEWINN',spin:'DREHEN',free:'FREIDREH',
 stop:'AUTO STOPPEN',auto:'AUTO ×20',max:'MAX',paytable:'GEWINNTABELLE',settings:'EINSTELLUNGEN',
 status:'Auf dem Dach ist es ruhig.',spinning:'Die Katzen sind unterwegs…',noWin:'Das Dach wird wieder still.',
 near:'Zwei Leben. Das dritte blieb im Schatten.',winLabel:'GEWINN',frenzy:'VOLLES FELD',
 awakened:'NEUN LEBEN ERWACHT',bonus:'VERBLEIBENDE FREIDREHS',pounce:'SPRUNG-WILD',
 lines:'GEWINNLINIEN',paylineNote:'Drei passende Symbole auf einer von fünf Linien. Wild ersetzt alle außer Bonus.',
 bonusNote:'Drei oder mehr Neun-Leben-Symbole geben neun Freidrehs zum ursprünglichen Einsatz.',
 frenzyNote:'Ein vollständig gleiches Feld (mit Wild-Ersatz) zahlt 5.000×.',
 settingSound:'Spielgeräusche',settingMotion:'Walzen-, Licht- und Gewinnanimationen',
 offline:'Offline · kein echtes Geld',help:'LEERTASTE dreht · A startet Auto · +/− ändert Einsatz · ESC zurück',
 stats:'DEINE STATISTIK',spins:'Drehs',wins:'Gewinne',bonuses:'Freidrehs',pounces:'Sprung-Wilds',
 frenzyCount:'Volle Felder',best:'Höchste Auszahlung',net:'Nettoergebnis',loading:'Das Dach wird vorbereitet…',
 uncertain:'Bestätigung des Drehs fehlt. Öffne Nine Lives erneut, um das C#-Ergebnis sicher wiederherzustellen.',
 unaffordable:'Nicht genug Guthaben',xp:'Spieler-XP',season:'Saison-XP',level:'LEVELAUFSTIEG'},
};
function SymbolArt({symbol}:{symbol:NineLivesSymbol}){
 return <img draggable={false} src={'/nine-lives-art/'+ART[symbol]} alt={symbol} loading="eager"/>;
}
function App(){
 const [snap,setSnap]=useState<NineLivesSnapshot|null>(null);
 const [pending,setPending]=useState<string|null>(null);
 const [uncertain,setUncertain]=useState(false);
 const [notice,setNotice]=useState<string|null>(null);
 const [grid,setGrid]=useState<NineLivesSymbol[]>(PREVIEW);
 const [animating,setAnimating]=useState(false);
 const [spinningColumns,setSpinningColumns]=useState<number[]>([]);
 const [activeWin,setActiveWin]=useState(false);
 const [paytableOpen,setPaytableOpen]=useState(false);
 const [settingsOpen,setSettingsOpen]=useState(false);
 const [autoRemaining,setAutoRemaining]=useState(0);
 const pendingRef=useRef<string|null>(null),snapshotRef=useRef<NineLivesSnapshot|null>(null);
 const firstSnapshot=useRef(true),lastSpinId=useRef<string|null>(null);
 const timerRef=useRef<ReturnType<typeof setTimeout>[]>([]);
 const intervalRef=useRef<ReturnType<typeof setInterval>|null>(null);
 const autoScheduled=useRef<string|null>(null),visualTick=useRef(0);
 const cleanTimers=useCallback(()=>{
   timerRef.current.forEach(clearTimeout);timerRef.current=[];
   if(intervalRef.current!==null){clearInterval(intervalRef.current);intervalRef.current=null;}
 },[]);
 const send=useCallback((intent:NineLivesIntent)=>{
   const current=snapshotRef.current;
   if(!current||pendingRef.current||uncertain||animating)return;
   const request=sendNineLivesIntent(current,intent);
   pendingRef.current=request;setPending(request);setNotice(null);
   if(intent.action==='spin'){setAnimating(true);setActiveWin(false);}
 },[uncertain,animating]);
 useEffect(()=>{
   const detach=listenNineLives(event=>{
     if(event.type==='snapshot'){
       if(snapshotRef.current?.session===event.data.session
         &&event.data.revision<snapshotRef.current.revision)return;
       snapshotRef.current=event.data;setSnap(event.data);
     }else if(event.data.requestId===pendingRef.current){
       pendingRef.current=null;setPending(null);
       if(!event.data.success){setAnimating(false);setAutoRemaining(0);setNotice(event.data.message);}
       else setNotice(null);
     }
   });
   window.chrome?.webview?.postMessage({type:'nine-lives.ready'});
   return()=>{detach();cleanTimers();};
 },[cleanTimers]);
 useEffect(()=>{if(snap)window.chrome?.webview?.postMessage({type:'nine-lives.ui-ready'});},[!!snap]);
 useEffect(()=>{
   if(!pending)return;
   const id=setTimeout(()=>{
     setUncertain(true);setAutoRemaining(0);setAnimating(false);
     setNotice(dict[snapshotRef.current?.state.language??'en'].uncertain);
   },9000);
   return()=>clearTimeout(id);
 },[pending]);
 const last=snap?.state.lastSpin;
 useEffect(()=>{
   if(!snap)return;
   if(firstSnapshot.current){
     firstSnapshot.current=false;
     lastSpinId.current=last?.id??null;
     setGrid(last?.grid??PREVIEW);
     setActiveWin(!!last);
     return;
   }
   if(!last||lastSpinId.current===last.id)return;
   lastSpinId.current=last.id;cleanTimers();setActiveWin(false);
   const reduced=window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
   const finish=()=>{
     cleanTimers();setGrid(last.grid);setSpinningColumns([]);setAnimating(false);setActiveWin(true);
     window.chrome?.webview?.postMessage({type:'nine-lives.animation-complete',spinId:last.id});
   };
   if(!snap.state.animationsEnabled||reduced){finish();return;}
   setAnim atingPlaceholder();
   function setAnim atingPlaceholder(){} 
 },[last?.id,snap?.state.animationsEnabled,cleanTimers]);
 const close=useCallback(()=>{
   setAutoRemaining(0);
   if(uncertain)window.chrome?.webview?.postMessage({type:'nine-lives.recover'});
   else if(!pendingRef.current)window.chrome?.webview?.postMessage({type:'nine-lives.close'});
 },[uncertain]);
 useEffect(()=>{
   const onKey=(event:KeyboardEvent)=>{
     if(event.ctrlKey||event.altKey||event.metaKey||event.repeat
       ||event.target instanceof HTMLButtonElement||event.target instanceof HTMLInputElement)return;
     if(event.key==='Escape'){
       event.preventDefault();
       if(paytableOpen)setPaytableOpen(false);
       else if(settingsOpen)setSettingsOpen(false);
       else close();
     }else if(event.code==='Space'){event.preventDefault();send({action:'spin'});}
     else if(event.key.toLowerCase()==='a'){event.preventDefault();setAutoRemaining(n=>n>0?0:20);}
     else if(event.key==='+'||event.key==='-'){
       const current=snapshotRef.current?.state;
       if(!current||current.bonusActive)return;
       const index=current.bets.indexOf(current.bet);
       const target=current.bets[index+(event.key==='+'?1:-1)];
       if(target&&Number(target)<=Number(current.bankroll))send({action:'bet.set',value:target});
     }
   };
   document.addEventListener('keydown',onKey);
   return()=>document.removeEventListener('keydown',onKey);
 },[close,paytableOpen,settingsOpen,send]);
 useEffect(()=>{
   if(!autoRemaining||animating||pending||uncertain)return;
   const spinId=last?.id??'initial';
   if(autoScheduled.current===spinId)return;
   autoScheduled.current=spinId;
   if(!snap?.state.canSpin){setAutoRemaining(0);return;}
   const timer=setTimeout(()=>{setAutoRemaining(n=>Math.max(0,n-1));send({action:'spin'});},500);
   return()=>clearTimeout(timer);
 },[autoRemaining,animating,pending,uncertain,last?.id,snap?.state.canSpin,send]);
 if(!snap)return <div className="nine-loading"><span>☾ ✦ ☾</span><h1>NINE LIVES</h1>
    <p>{dict.en.loading}</p><button onClick={close}>← Casino lobby</button></div>;
 const s=snap.state,t=dict[s.language];
 const locked=!!pending||animating||uncertain;
 const result: NineLivesSpin|null=s.lastSpin;
 const indexes=result?.winningCells??[];
 const winning=result?.winningLines??[];
 const mood=animating?t.spinning:notice??(!result?t.status:
   result.fullGridFrenzy?t.frenzy+' · 5,000×':
   result.bonusSpinsAwarded?t.awakened+' · +9':
   Number(result.payout)>0?t.winLabel+' · '+money(result.payout):
   result.bonusCount===2?t.near:t.noWin);
 const canChangeBet=!locked&&!s.bonusActive;
 const selectedIndex=s.bets.indexOf(s.bet);
 return <div className="nine-app" lang={s.language} data-language={s.language}
   data-testid="nine-lives-app" data-spin-id={result?.id??''}
   data-animating={animating} data-bonus={s.bonusActive}>
   <RooftopAmbient enabled={!window.matchMedia?.('(prefers-reduced-motion: reduce)').matches&&s.animationsEnabled}/>
   <header className="nine-header">
     <button className="nine-back" onClick={close}>← {t.back}<kbd>ESC</kbd></button>
     <div className="nine-title"><small>THE MIDNIGHT CLUB · {t.top}</small><h1>NINE <em>LIVES</em></h1></div>
     <div className="nine-header-bank"><small>{t.balance}</small><strong>{money(s.bankroll)}</strong></div>
   </header>
   <div className="nine-layout">
     <main className="nine-machine">
       <div className="nine-moon" aria-hidden="true"/><div className="nine-horizon" aria-hidden="true"/>
       <div className="nine-machine-heading"><span>✦</span><div><small>{t.tag}</small><h2>NINE LIVES</h2></div><span>✦</span></div>
       <div className="nine-bonus-banner" data-testid="nine-lives-bonus" data-active={s.bonusActive}>
         <span>☾</span> {s.bonusActive?t.bonus+' · '+s.bonusSpinsRemaining:t.bonusNote} <span>☾</span>
       </div>
       <div className="nine-reels-outer">
         <span className="nine-line-number left">I · II · III</span>
         <div className="nine-reels" data-testid="nine-lives-reels">
           {grid.map((symbol,i)=><div key={i} data-symbol={symbol}
             className={'nine-cell'+(spinningColumns.includes(i%3)?' rolling':'')
               +(activeWin&&indexes.includes(i)?' winning':'')
               +(activeWin&&result?.fullGridFrenzy?' frenzy':'')
               +(activeWin&&indexes.length>0&&!indexes.includes(i)?' dimmed':'')
               +(activeWin&&result?.pounceWildCell===i?' pounced':'')}>
             <SymbolArt symbol={symbol}/>
           </div>)}
           {!animating&&activeWin&&winning.length>0&&<svg className="nine-lines" viewBox="0 0 300 300" preserveAspectRatio="none" aria-hidden="true">
             {winning.map(i=><polyline key={i} points={paylineCells[i].map(cell=>
               ((cell%3)*100+50)+','+(Math.floor(cell/3)*100+50)).join(' ')}
               stroke={i<3?'#f9d985':'#d48aff'} strokeWidth="3" strokeLinecap="round" fill="none"/>)}
           </svg>}
         </div>
         <span className="nine-line-number right">III · II · I</span>
       </div>
       <div className="nine-status" role="status" aria-live="polite">
         <strong>{mood}</strong>
         <small>{result&&!animating?'+'+result.playerXp+' '+t.xp+' · +'+result.seasonXp+' '+t.season
           +(result.levelUp?' · '+t.level:''):t.help}</small>
       </div>
       <div className="nine-console">
         <div className="nine-meter">
           <div><small>{t.balance}</small><strong>{money(s.bankroll)}</strong></div>
           <div><small>{t.bet}</small><strong>{s.bonusActive?'FREE':money(s.bet)}</strong></div>
           <div><small>{t.win}</small><strong data-testid="nine-lives-last-win">{result&&Number(result.payout)>0?money(result.payout):'—'}</strong></div>
         </div>
         <div className="nine-betting">
           <button data-testid="nine-lives-bet-down" disabled={!canChangeBet||selectedIndex<=0}
             onClick={()=>send({action:'bet.set',value:s.bets[selectedIndex-1]})}>−</button>
           <div className="nine-bet-grid" role="group" aria-label={t.bet}>
             {s.bets.map(bet=><button key={bet} data-bet={bet}
               className={bet===s.bet?'selected':''} disabled={!canChangeBet||Number(bet)>Number(s.bankroll)}
               onClick={()=>send({action:'bet.set',value:bet})}>{'$'+Number(bet).toLocaleString('en-US')}</button>)}
           </div>
           <button data-testid="nine-lives-bet-up" disabled={!canChangeBet||selectedIndex===s.bets.length-1
             ||Number(s.bets[selectedIndex+1])>Number(s.bankroll)}
             onClick={()=>send({action:'bet.set',value:s.bets[selectedIndex+1]})}>+</button>
           <button className="nine-max" data-testid="nine-lives-max" disabled={!canChangeBet||Number(s.bankroll)<1}
             onClick={()=>{const max=s.bets.filter(b=>Number(b)<=Number(s.bankroll)).at(-1);if(max)send({action:'bet.set',value:max});}}>
             {t.max}</button>
         </div>
         <div className="nine-actions">
           <button className="nine-spin" data-testid="nine-lives-spin"
             disabled={locked||!s.canSpin} onClick={()=>send({action:'spin'})}>
             <span>✦</span>{locked?t.spinning:s.bonusActive?t.free:t.spin}<kbd>SPACE</kbd></button>
           <button className="nine-auto" data-testid="nine-lives-auto"
             disabled={uncertain||(!s.canSpin&&autoRemaining===0)}
             onClick={()=>setAutoRemaining(n=>n>0?0:20)}>{autoRemaining>0?t.stop:t.auto}</button>
         </div>
       </div>
     </main>
     <aside className="nine-sidebar">
       <section className="nine-card nine-facts">
         <div className="nine-section-title"><h2>{t.paytable}</h2><button data-testid="nine-lives-paytable"
           onClick={()=>setPaytableOpen(v=>!v)}>{paytableOpen?'−':'+'}</button></div>
         <p>{t.paylineNote}</p>
         <div className="nine-paytable-list">
           {s.paytable.slice().reverse().map(item=><div key={item.id} className="nine-payrow">
             <SymbolArt symbol={item.id}/><span>{item.name}</span>
             <strong>{item.id==='Bonus'?'9 FREE':item.lineMultiplier+'×'}</strong>
           </div>)}
         </div>
         <p>{t.bonusNote}</p><p>{t.frenzyNote}</p>
         {paytableOpen&&<div className="nine-detail" data-testid="nine-lives-paytable-details">
           <strong>{t.lines}</strong>
           {s.paylines.map((line,i)=><div key={i}>#{i+1}: {line.map(cell=>cell+1).join(' → ')}</div>)}
         </div>}
       </section>
       <section className="nine-card nine-record">
         <h2>{t.stats}</h2>
         <small>PRESTIGE {s.profile.prestige} · LEVEL {s.profile.level} · ◆ {s.profile.coins}</small>
         <div className="nine-record-grid">
           <div>{t.spins}<strong>{s.stats.spins}</strong></div><div>{t.wins}<strong>{s.stats.wins}</strong></div>
           <div>{t.bonuses}<strong>{s.stats.bonusSpins}</strong></div>
           <div>{t.pounces}<strong>{s.stats.pounceWilds}</strong></div>
           <div>{t.frenzyCount}<strong>{s.stats.fullGridFrenzies}</strong></div>
           <div>{t.net}<strong>{money(s.stats.netProfit)}</strong></div>
         </div>
         <p>{t.best}: <strong>{money(s.stats.biggestPayout)}</strong></p>
       </section>
       <section className="nine-card nine-config">
         <div className="nine-section-title"><h2>{t.settings}</h2>
           <button data-testid="nine-lives-settings" onClick={()=>setSettingsOpen(v=>!v)}>
             {settingsOpen?'−':'+'}</button></div>
         {settingsOpen&&<div className="nine-settings" data-testid="nine-lives-settings-panel">
           <label><input type="checkbox" checked={s.soundEnabled} disabled={locked}
             onChange={e=>send({action:'sound.set',value:String(e.target.checked)})}/>{t.settingSound}</label>
           <label><input type="checkbox" checked={s.animationsEnabled} disabled={locked}
             onChange={e=>send({action:'animations.set',value:String(e.target.checked)})}/>{t.settingMotion}</label>
         </div>}
         <small>{t.offline}</small>
       </section>
     </aside>
   </div>
 </div>;
}
createRoot(document.getElementById('root')!).render(<App/>);
