import {finishModeBoot} from '../boot/finishModeBoot';
import {useCallback,useEffect,useMemo,useRef,useState} from 'react';
import {createRoot} from 'react-dom/client';
import {listenNeon,sendNeonIntent,symbols} from './bridge';
import type {NeonIntent,NeonSnapshot,NeonSpin,NeonSymbol} from './bridge';
import {SymbolArt} from './Symbols';
import {VaultAmbient} from './Ambient';
import {neonImpact,isImpactful} from './presentation';
import type {NeonImpact} from './presentation';
import './neonVault.css';

const PREVIEW:NeonSymbol[]=[
 'Pulse','Prism','Chip','Crown','Key',
 'Wild','Scatter','Pulse','VaultKey','Prism',
 'Crown','Key','Wild','Chip','Pulse',
 'VaultKey','Prism','Scatter','Key','Crown',
];
const STOPS=[650,980,1370,1760,2220];
const dollar=(v:string)=>'$'+Number(v).toLocaleString('en-US',{minimumFractionDigits:2,maximumFractionDigits:2});
const short=(v:string)=>'$'+Number(v).toLocaleString('en-US',{maximumFractionDigits:0});
const words={
 en:{back:'Casino lobby',subtitle:'HIGH-VOLTAGE VIDEO SLOTS · 1,024 WAYS',balance:'SHARED BANKROLL',
   total:'LAST RETURN',wager:'BET PER SPIN',spin:'SPIN',freeSpin:'FREE SPIN',auto:'AUTO ×20',
   stopAuto:'STOP AUTO',max:'MAX',waiting:'Vault systems standing by',spinning:'Decrypting reels…',
   ready:'5 REELS · 4 ROWS · ALL WAYS',none:'No matching ways. Vault remains sealed.',
   win:'ACCESS GRANTED',jackpot:'ACCESS KEY JACKPOT',blackout:'BLACKOUT',overload:'BLACKOUT OVERLOAD',
   totalBlackout:'TOTAL BLACKOUT',override:'SECURITY OVERRIDE',wild:'EXPANDING WILDS',
   break:'VAULT BREAK',nearMiss:'SECURITY SIGNAL DETECTED',energy:'VAULT ENERGY',
   highRoller:'HIGH ROLLER',tierStandard:'STANDARD ACCESS',tierHighRoller:'HIGH ROLLER',
   tierVaultClass:'VAULT CLASS',tierMaxExposure:'MAX VAULT EXPOSURE',freeRemaining:'FREE SPINS',
   ways:'PAYING WAYS',stats:'YOUR VAULT RECORD',spins:'Spins',wins:'Wins',
   freeSpins:'Free spins',blackouts:'Blackouts',overrides:'Overrides',jackpots:'Jackpots',
   net:'Net result',best:'Best return',paytable:'PAYTABLE',features:'SYSTEM FEATURES',
   settings:'SETTINGS',sound:'Slot sounds',motion:'Reel & neon animations',
   explain:'Matches pay on consecutive reels from the left, with Wild substitutes.',
   featureExplain:'3 / 4 / 5 Scatters grant 8 / 12 / 16 Free Spins. Wilds can expand; 12 Vault Energy charges a Security Override.',
   jackpotsExplain:'Access Keys unlock four jackpots. Each multiplier is based on your selected bet.',
   noMoney:'Insufficient balance',offline:'OFFLINE · PLAY MONEY ONLY',
   hint:'SPACE spins · A auto-spins · +/− changes bet · ESC returns to the lobby',
   uncertain:'The confirmation timed out. Reopen Neon Vault to recover the C# result; never spin again blindly.',
   welcome:'Authorizing the vault console…',xp:'Player XP',season:'Season XP',level:'LEVEL UP',
   bigWin:'HIGH VOLTAGE WIN',megaWin:'VAULT-SHATTERING WIN',
   scatter:'SCATTER PAYOUT',waysWin:'WAYS RETURN',extra:'BONUS & JACKPOT',loading:'Establishing secure vault connection…'},
 de:{back:'Casino-Lobby',subtitle:'VIDEO-SLOTS UNTER HOCHSPANNUNG · 1.024 GEWINNWEGE',balance:'GEMEINSAMES GUTHABEN',
   total:'LETZTE AUSZAHLUNG',wager:'EINSATZ PRO DREH',spin:'DREHEN',freeSpin:'FREIDREH',auto:'AUTO ×20',
   stopAuto:'AUTO STOPPEN',max:'MAX',waiting:'Vault-Systeme bereit',spinning:'Walzen werden entschlüsselt…',
   ready:'5 WALZEN · 4 REIHEN · ALLE WEGE',none:'Kein Gewinnweg. Der Tresor bleibt verschlossen.',
   win:'ZUGANG GEWÄHRT',jackpot:'ZUGANGSSCHLÜSSEL-JACKPOT',blackout:'BLACKOUT',overload:'BLACKOUT OVERLOAD',
   totalBlackout:'TOTAL BLACKOUT',override:'SICHERHEITSÜBERSTEUERUNG',wild:'EXPANDIERENDE WILDS',
   break:'VAULT BREAK',nearMiss:'SICHERHEITSSIGNAL ERKANNT',energy:'VAULT-ENERGIE',
   highRoller:'HIGH ROLLER',tierStandard:'STANDARDZUGANG',tierHighRoller:'HIGH ROLLER',
   tierVaultClass:'VAULT CLASS',tierMaxExposure:'MAXIMALE EXPOSITION',freeRemaining:'FREIDREHS',
   ways:'GEWINNWEGE',stats:'DEINE VAULT-STATISTIK',spins:'Drehs',wins:'Gewinne',
   freeSpins:'Freidrehs',blackouts:'Blackouts',overrides:'Übersteuerungen',jackpots:'Jackpots',
   net:'Nettoergebnis',best:'Höchste Auszahlung',paytable:'GEWINNTABELLE',features:'SYSTEMFUNKTIONEN',
   settings:'EINSTELLUNGEN',sound:'Automatengeräusche',motion:'Walzen- und Neonanimationen',
   explain:'Gewinnwege zählen auf aufeinanderfolgenden Walzen von links. Wild ersetzt normale Symbole.',
   featureExplain:'3 / 4 / 5 Scatters geben 8 / 12 / 16 Freidrehs. Wilds können expandieren. 12 Vault-Energie lädt eine Übersteuerung.',
   jackpotsExplain:'Zugangsschlüssel schalten vier Jackpots frei. Multiplikatoren beziehen sich auf den Einsatz.',
   noMoney:'Nicht genug Guthaben',offline:'OFFLINE · NUR SPIELGELD',
   hint:'LEERTASTE dreht · A startet Auto · +/− ändert Einsatz · ESC zur Lobby',
   uncertain:'Bestätigung fehlt. Öffne Neon Vault erneut, um das C#-Ergebnis sicher wiederherzustellen.',
   welcome:'Vault-Konsole wird autorisiert…',xp:'Spieler-XP',season:'Saison-XP',level:'LEVELAUFSTIEG',
   bigWin:'HOCHSPANNUNGS-GEWINN',megaWin:'TRESOR-SPRENGENDER GEWINN',
   scatter:'SCATTER-AUSZAHLUNG',waysWin:'WEGE-AUSZAHLUNG',extra:'BONUS & JACKPOT',loading:'Sichere Verbindung zum Vault wird hergestellt…'},
};
type Banner={key:string;label:string};
function winCells(result:NeonSpin|null):Set<number>{
 const out=new Set<number>();
 if(!result)return out;
 const s=result.finalGrid;
 for(const symbol of symbols.slice(0,5)){
   let count=0;
   for(let reel=0;reel<5;reel++){
     const matches=[0,1,2,3].map(row=>row*5+reel).filter(index=>s[index]===symbol||s[index]==='Wild');
     if(matches.length===0)break;
     count++;
   }
   if(count>=3)for(let reel=0;reel<count;reel++)
     for(let row=0;row<4;row++){
       const index=row*5+reel;
       if(s[index]===symbol||s[index]==='Wild')out.add(index);
     }
 }
 return out;
}
function VaultApp(){
 const [snapshot,setSnapshot]=useState<NeonSnapshot|null>(null);
 const [pending,setPending]=useState<string|null>(null);
 const [animating,setAnimating]=useState(false);
 const [uncertain,setUncertain]=useState(false);
 const [notice,setNotice]=useState<string|null>(null);
 const [grid,setGrid]=useState<NeonSymbol[]>(PREVIEW);
 const [moving,setMoving]=useState<number[]>([]);
 const [feature,setFeature]=useState<Banner|null>(null);
 const [impact,setImpact]=useState<NeonImpact|null>(null);
 const [finished,setFinished]=useState(false);
 const [paytableOpen,setPaytableOpen]=useState(false);
 const [settingsOpen,setSettingsOpen]=useState(false);
 const [auto,setAuto]=useState(0);
 const snapshotRef=useRef<NeonSnapshot|null>(null),pendingRef=useRef<string|null>(null);
 const first=useRef(true),lastId=useRef<string|null>(null);
 const timers=useRef<ReturnType<typeof setTimeout>[]>([]);
 const ticker=useRef<ReturnType<typeof setInterval>|null>(null);
 const settled=useRef(new Set<number>()),visual=useRef(0),autoScheduled=useRef<string|null>(null);
 const stopTimers=useCallback(()=>{
   for(const t of timers.current)clearTimeout(t);
   timers.current=[];
   if(ticker.current!==null){clearInterval(ticker.current);ticker.current=null;}
 },[]);
 const intent=useCallback((command:NeonIntent)=>{
   const current=snapshotRef.current;
   if(!current||pendingRef.current||animating||uncertain)return;
   const id=sendNeonIntent(current,command);
   pendingRef.current=id;setPending(id);setNotice(null);
   if(command.action==='spin'){setAnimating(true);setFinished(false);setFeature(null);setImpact(null);}
 },[animating,uncertain]);
 useEffect(()=>{
   const unsubscribe=listenNeon(event=>{
     if(event.type==='snapshot'){
       if(snapshotRef.current?.session===event.data.session
         &&event.data.revision<snapshotRef.current.revision)return;
       snapshotRef.current=event.data;setSnapshot(event.data);
     }else if(event.data.requestId===pendingRef.current){
       pendingRef.current=null;setPending(null);
       if(!event.data.success){setNotice(event.data.message);setAnimating(false);setAuto(0);}
       else setNotice(null);
     }
   });
   window.chrome?.webview?.postMessage({type:'neon-vault.ready'});
   return()=>{unsubscribe();stopTimers();};
 },[stopTimers]);
 useEffect(()=>{if(snapshot){finishModeBoot();window.chrome?.webview?.postMessage({type:'neon-vault.ui-ready'});}},[!!snapshot]);
 useEffect(()=>{
   if(!pending)return;
   const watchdog=setTimeout(()=>{
     setUncertain(true);setAuto(0);setAnimating(false);
     setNotice(words[snapshotRef.current?.state.language??'en'].uncertain);
   },9000);
   return()=>clearTimeout(watchdog);
 },[pending]);
 const result=snapshot?.state.lastSpin??null;
 useEffect(()=>{
   if(!snapshot)return;
   if(first.current){
     first.current=false;
     lastId.current=result?.id??null;
     setGrid(result?.finalGrid??PREVIEW);
     setFinished(!!result);
     return;
   }
   if(!result||lastId.current===result.id)return;
   lastId.current=result.id;stopTimers();settled.current.clear();setFinished(false);setImpact(null);
   const reduce=window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
   const reaction=neonImpact(result);
   const sendComplete=()=>window.chrome?.webview?.postMessage({
     type:'neon-vault.animation-complete',spinId:result.id});
   const complete=()=>{
     stopTimers();setGrid(result.finalGrid);setMoving([]);setAnimating(false);setFeature(null);
     setFinished(true);sendComplete();
     // Keep a short, nonblocking afterglow after the reel transaction is complete.
     timers.current.push(setTimeout(()=>setImpact(null),reaction.strength===3?2300:1450));
   };
   if(!snapshot.state.animationsEnabled||reduce){complete();return;}
   setAnimating(true);setMoving([0,1,2,3,4]);visual.current=0;
   ticker.current=setInterval(()=>{
     visual.current++;
     setGrid(g=>g.map((symbol,index)=>settled.current.has(index%5)
       ?symbol:symbols[(visual.current+index*5)%symbols.length]));
   },90);
   STOPS.forEach((delay,reel)=>timers.current.push(setTimeout(()=>{
     settled.current.add(reel);
     setMoving(v=>v.filter(n=>n!==reel));
     setGrid(g=>g.map((symbol,index)=>index%5===reel?result.baseGrid[index]:symbol));
     window.chrome?.webview?.postMessage({type:'neon-vault.reel-stop',spinId:result.id,reel});
     if(reel===3&&result.nearMiss!=='None')
       setFeature({key:'near',label:words[snapshot.state.language].nearMiss+' · '+result.nearMiss.toUpperCase()});
   },delay)));
   const featureDelay=STOPS[4]+220;
   timers.current.push(setTimeout(()=>{
     if(isImpactful(reaction)){
       setImpact(reaction);
       window.chrome?.webview?.postMessage({type:'neon-vault.feature-impact',spinId:result.id});
     }
     if(result.blackout!=='None')
       setFeature({key:'blackout',label:result.blackout==='Total'
         ?words[snapshot.state.language].totalBlackout
         :result.blackout==='Overload'?words[snapshot.state.language].overload
         :words[snapshot.state.language].blackout});
     else if(result.securityOverrideReel!==null)
       setFeature({key:'override',label:words[snapshot.state.language].override});
     else if(result.expandedReels.length)
       setFeature({key:'wild',label:words[snapshot.state.language].wild});
     else if(result.jackpot!=='None')
       setFeature({key:'jackpot',label:words[snapshot.state.language].jackpot+' · '+result.jackpot});
     else if(result.freeSpinsAwarded)
       setFeature({key:'free',label:words[snapshot.state.language].break+' · +'+result.freeSpinsAwarded});
     else if(reaction.winTier==='mega')
       setFeature({key:'mega-win',label:words[snapshot.state.language].megaWin});
     else if(reaction.winTier==='big')
       setFeature({key:'big-win',label:words[snapshot.state.language].bigWin});
     else setFeature(null);
     setGrid(result.finalGrid);
   },featureDelay));
   timers.current.push(setTimeout(complete,featureDelay+
     (result.blackout!=='None'||result.expandedReels.length||result.securityOverrideReel!==null?1050:480)));
   return()=>stopTimers();
 },[result?.id,snapshot?.state.animationsEnabled,stopTimers]);
 const close=useCallback(()=>{
   setAuto(0);
   if(uncertain)window.chrome?.webview?.postMessage({type:'neon-vault.recover'});
   else if(!pendingRef.current&&!snapshotRef.current?.state.bonusActive)
     window.chrome?.webview?.postMessage({type:'neon-vault.close'});
 },[uncertain]);
 useEffect(()=>{
   const key=(e:KeyboardEvent)=>{
     if(e.repeat||e.ctrlKey||e.altKey||e.metaKey
       ||e.target instanceof HTMLButtonElement||e.target instanceof HTMLInputElement)return;
     if(e.key==='Escape'){
       e.preventDefault();
       if(paytableOpen)setPaytableOpen(false);
       else if(settingsOpen)setSettingsOpen(false);
       else close();
     }else if(e.code==='Space'){e.preventDefault();intent({action:'spin'});}
     else if(e.key.toLowerCase()==='a'){e.preventDefault();setAuto(n=>n>0?0:20);}
     else if(e.key==='+'||e.key==='-'){
       const s=snapshotRef.current?.state;
       if(!s||s.bonusActive)return;
       const index=s.bets.indexOf(s.bet),next=s.bets[index+(e.key==='+'?1:-1)];
       if(next&&Number(next)<=Number(s.bankroll))intent({action:'bet.set',value:next});
     }
   };
   document.addEventListener('keydown',key);
   return()=>document.removeEventListener('keydown',key);
 },[close,intent,settingsOpen,paytableOpen]);
 useEffect(()=>{
   if(!auto||pending||animating||uncertain)return;
   const id=result?.id??'initial';
   if(autoScheduled.current===id)return;
   autoScheduled.current=id;
   if(!snapshot?.state.canSpin){setAuto(0);return;}
   const t=setTimeout(()=>{setAuto(n=>Math.max(0,n-1));intent({action:'spin'});},480);
   return()=>clearTimeout(t);
 },[auto,pending,animating,uncertain,snapshot?.state.canSpin,result?.id,intent]);
 const winning=useMemo(()=>finished?winCells(result):new Set<number>(),[finished,result]);
 if(!snapshot)return <div className="vault-loading"><div className="vault-loading-diamond">◇</div>
   <h1>NEON VAULT</h1><p>{words.en.loading}</p></div>;
 const s=snapshot.state,t=words[s.language];
 const locked=!!pending||animating||uncertain;
 const status=notice??(animating?t.spinning:
   !result?t.waiting:result.jackpot!=='None'?t.jackpot+' · '+result.jackpot:
   result.freeSpinsAwarded?t.break+' · +'+result.freeSpinsAwarded:
   result.blackout!=='None'?t.blackout+' · '+result.blackout:
   Number(result.payout)>0?t.win+' · '+dollar(result.payout):t.none);
 const currentBet=s.bets.indexOf(s.bet);
 const tierLabel=t[('tier'+s.betTier) as 'tierStandard'|'tierHighRoller'|'tierVaultClass'|'tierMaxExposure'];
 const energy=Math.min(100,Math.round(100*s.vaultEnergy/s.energyTarget));
 const activeCols=feature?.key==='blackout'?'blackout':'';
 const highlight=(i:number)=>finished&&(winning.has(i)
   ||result?.expandedReels.includes(i%5)||result?.blackoutWildCells.includes(i)
   ||result?.securityOverrideReel===i%5);
 return <div className="vault-app" data-motion={s.animationsEnabled} lang={s.language} data-testid="neon-vault-app"
   data-spin-id={result?.id??''} data-animating={animating} data-tier={s.betTier} data-bonus={s.bonusActive}
   data-impact={impact?.kind??'idle'} data-win-tier={impact?.winTier??'none'}>
   <VaultAmbient enabled={s.animationsEnabled&&!window.matchMedia?.('(prefers-reduced-motion: reduce)').matches}
     spinning={animating} energy={s.vaultEnergy} bonus={s.bonusActive} impact={impact}/>
   <header className="vault-header">
     <button className="vault-back" onClick={close} disabled={!!pending||s.bonusActive}>
       ← {t.back}<kbd>ESC</kbd></button>
     <div className="vault-brand"><small>THE MIDNIGHT CLUB <span>◆</span> LEVEL 09 ACCESS</small>
       <h1>NEON <em>VAULT</em></h1><div>{t.subtitle}</div></div>
     <div className="vault-header-wallet"><small>{t.balance}</small><strong>{dollar(s.bankroll)}</strong></div>
   </header>
   <div className="vault-layout">
     <main className="vault-machine" data-impact={impact?.kind??'idle'}>
       <div className="vault-scanlines" aria-hidden="true"/>
       <div className="vault-reactor-rail" aria-hidden="true"/>
       <div className="vault-reactor-pins" aria-hidden="true">
         {Array.from({length:12},(_,index)=><i key={index}/>)}
       </div>
       <div className="vault-machine-head">
         <div className="vault-access"><span className="vault-access-light"/> ENCRYPTION: AES–09</div>
         <div className={'vault-tier tier-'+s.betTier}><span>◆</span> {tierLabel}</div>
         <div className="vault-system-online">SYSTEM ONLINE <span>●</span></div>
       </div>
       <div className="vault-featurebar">
         <div className="vault-feature-title">
           <small>{t.energy}</small><strong>{s.vaultEnergy} / {s.energyTarget}</strong>
         </div>
         <div className="vault-energy" role="progressbar" aria-label={t.energy}
           aria-valuemin={0} aria-valuemax={12} aria-valuenow={Math.min(s.vaultEnergy,12)}>
           <i style={{width:energy+'%'}}/></div>
         <div className={'vault-bonus-badge'+(s.bonusActive?' active':'')} data-testid="neon-vault-bonus">
           <span>◇</span> {s.bonusActive?t.break+' · '+s.freeSpinsRemaining+' '+t.freeRemaining:
             t.freeRemaining+' · '+s.freeSpinsRemaining}</div>
       </div>
       <div className={'vault-board '+activeCols} data-testid="neon-vault-grid">
         <div className="vault-board-hologram" aria-hidden="true"/>
         <div className="vault-board-decoration top" aria-hidden="true">V A U L T &nbsp; A C C E S S &nbsp; A R R A Y</div>
         <div className="vault-grid">
           {grid.map((symbol,i)=><div key={i} data-symbol={symbol}
             className={'vault-cell'+(moving.includes(i%5)?' rolling':'')
               +(highlight(i)?' winner':'')
               +(finished&&winning.size>0&&!highlight(i)?' muted':'')
               +(feature?.key==='blackout'?' alarm':'')
               +(finished&&result?.blackoutWildCells.includes(i)?' converted':'')}>
             <SymbolArt symbol={symbol}/>
             {finished&&result?.securityOverrideReel===i%5&&<span className="vault-cell-tag">OVERRIDE</span>}
           </div>)}
         </div>
         <div className="vault-board-decoration bottom" aria-hidden="true">0 0 1 0 1 ◇ 0 1 1 0 0 ◇ S E C U R E D</div>
         {feature&&<div className={'vault-overlay overlay-'+feature.key} role="status">
           <span>◈</span><strong>{feature.label}</strong><span>◈</span></div>}
         {impact&&impact.winTier!=='none'&&<div className="vault-win-fanfare" aria-hidden="true">
           <span>{impact.winTier==='mega'?t.megaWin:t.bigWin}</span>
           <strong>{dollar(impact.payout)}</strong>
           <em>{impact.multiplier.toLocaleString(s.language==='de'?'de-DE':'en-US',{maximumFractionDigits:1})}×</em>
         </div>}
       </div>
       <div className="vault-feedback" role="status" aria-live="polite">
         <strong>{status}</strong>
         <small>{result&&!animating?'+'+result.playerXp+' '+t.xp+' · +'+result.seasonXp+' '+t.season+
           (result.levelUp?' · '+t.level:''):t.hint}</small>
       </div>
       <div className="vault-console">
         <div className="vault-meters">
           <div><small>{t.balance}</small><strong>{dollar(s.bankroll)}</strong></div>
           <div><small>{t.wager}</small><strong>{s.bonusActive?'FREE':dollar(s.bet)}</strong></div>
           <div><small>{t.total}</small><strong data-testid="neon-vault-last-win">
             {result&&Number(result.payout)>0?dollar(result.payout):'—'}</strong></div>
         </div>
         <div className="vault-betting">
           <button data-testid="neon-vault-bet-down" disabled={locked||s.bonusActive||currentBet<=0}
             onClick={()=>intent({action:'bet.set',value:s.bets[currentBet-1]})}>−</button>
           <div className="vault-bets" role="group" aria-label={t.wager}>
             {s.bets.map(wager=><button key={wager} data-bet={wager}
               className={wager===s.bet?'selected':''} disabled={locked||s.bonusActive
                 ||Number(wager)>Number(s.bankroll)}
               onClick={()=>intent({action:'bet.set',value:wager})}>{short(wager)}</button>)}
           </div>
           <button data-testid="neon-vault-bet-up" disabled={locked||s.bonusActive||currentBet===s.bets.length-1
             ||Number(s.bets[currentBet+1])>Number(s.bankroll)}
             onClick={()=>intent({action:'bet.set',value:s.bets[currentBet+1]})}>+</button>
           <button className="vault-max" data-testid="neon-vault-max"
             disabled={locked||s.bonusActive||Number(s.bankroll)<1}
             onClick={()=>{const max=s.bets.filter(b=>Number(b)<=Number(s.bankroll)).at(-1);
               if(max)intent({action:'bet.set',value:max});}}>{t.max}</button>
         </div>
         <div className="vault-actions">
           <button className="vault-spin" data-testid="neon-vault-spin" disabled={locked||!s.canSpin}
             onClick={()=>intent({action:'spin'})}><span>◇</span>
             {animating?t.spinning:s.bonusActive?t.freeSpin:s.canSpin?t.spin:t.noMoney}<kbd>SPACE</kbd></button>
           <button className="vault-auto" data-testid="neon-vault-auto"
             disabled={uncertain||(!s.canSpin&&auto===0)}
             onClick={()=>setAuto(n=>n>0?0:20)}>{auto>0?t.stopAuto:t.auto}</button>
         </div>
       </div>
     </main>
     <aside className="vault-sidebar">
       <section className="vault-panel jackpot-panel">
         <div className="vault-panel-title"><span>◈</span><h2>{t.jackpot}</h2></div>
         <div className="vault-jackpots">
           {s.jackpots.slice().reverse().map(item=><div key={item.id} className={'vault-jackpot '+item.id.toLowerCase()}>
             <small>{item.id==='Grand'?'GRAND VAULT':item.id.toUpperCase()} · {item.multiplier}×</small>
             <strong>{dollar(item.amount)}</strong></div>)}
         </div><p>{t.jackpotsExplain}</p>
       </section>
       <section className="vault-panel">
         <div className="vault-panel-title"><span>▦</span><h2>{t.paytable}</h2>
           <button data-testid="neon-vault-paytable" onClick={()=>setPaytableOpen(v=>!v)}>
             {paytableOpen?'−':'+'}</button></div>
         <div className="vault-paytable">
           {s.paytable.map(item=><div key={item.id} className="vault-payrow">
             <SymbolArt symbol={item.id} compact/><span>{item.name}</span>
             <strong>{item.id==='Scatter'?'FREE':
               item.id==='VaultKey'?'KEY':
               item.id==='Wild'?'WILD':item.fiveReels+'×'}</strong></div>)}
         </div>
         {paytableOpen&&<div className="vault-paytable-details" data-testid="neon-vault-paytable-details">
           <div>3 REELS <span>4 REELS</span> 5 REELS</div>
           {s.paytable.slice(0,5).map(item=><div key={item.id}>{item.name}
             <span>{item.threeReels}×</span><span>{item.fourReels}×</span><span>{item.fiveReels}×</span></div>)}
           <p>{t.explain}</p>
         </div>}
         <p>{t.featureExplain}</p>
       </section>
       <section className="vault-panel vault-record">
         <h2>{t.stats}</h2><small>PRESTIGE {s.profile.prestige} · LEVEL {s.profile.level} · ◆ {s.profile.coins}</small>
         <div className="vault-record-grid">
           <div>{t.spins}<strong>{s.stats.spins}</strong></div>
           <div>{t.wins}<strong>{s.stats.wins}</strong></div>
           <div>{t.freeSpins}<strong>{s.stats.freeSpins}</strong></div>
           <div>{t.blackouts}<strong>{s.stats.blackouts}</strong></div>
           <div>{t.overrides}<strong>{s.stats.securityOverrides}</strong></div>
           <div>{t.jackpots}<strong>{s.stats.jackpots}</strong></div>
         </div><p>{t.net}: <strong>{dollar(s.stats.netProfit)}</strong></p>
         <p>{t.best}: <strong>{dollar(s.stats.biggestPayout)}</strong></p>
       </section>
       <section className="vault-panel vault-settings">
         <div className="vault-panel-title"><h2>{t.settings}</h2>
           <button data-testid="neon-vault-settings" onClick={()=>setSettingsOpen(v=>!v)}>
             {settingsOpen?'−':'+'}</button></div>
         {settingsOpen&&<div className="vault-config" data-testid="neon-vault-settings-panel">
           <label><input type="checkbox" checked={s.soundEnabled} disabled={locked}
             onChange={e=>intent({action:'sound.set',value:String(e.target.checked)})}/>{t.sound}</label>
           <label><input type="checkbox" checked={s.animationsEnabled} disabled={locked}
             onChange={e=>intent({action:'animations.set',value:String(e.target.checked)})}/>{t.motion}</label>
         </div>}
         <small>{t.offline}</small>
       </section>
     </aside>
   </div>
 </div>;
}
createRoot(document.getElementById('root')!).render(<VaultApp/>);
