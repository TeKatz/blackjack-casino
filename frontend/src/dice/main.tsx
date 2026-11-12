import {finishModeBoot} from '../boot/finishModeBoot';
import {useCallback,useEffect,useMemo,useRef,useState} from 'react';
import {createRoot} from 'react-dom/client';
import {categories,issueDiceIntent,listenDice} from './bridge';
import type {DiceCategory,DiceCelebration,DiceGame,DiceIntent,DiceSnapshot} from './bridge';
import {DiceAmbient} from './Ambient';
import {DiceCube,Die} from './DiceCube';
import {diceSkin} from './diceSkins';
import './dice.css';

const names:Record<'de'|'en',Record<DiceCategory,string>>={
 en:{Ones:'Ones',Twos:'Twos',Threes:'Threes',Fours:'Fours',Fives:'Fives',Sixes:'Sixes',
 ThreeOfAKind:'Three of a kind',FourOfAKind:'Four of a kind',FullHouse:'Full house',
 SmallStraight:'Small straight',LargeStraight:'Large straight',Kniffel:'Five of a kind',Chance:'Chance'},
 de:{Ones:'Einser',Twos:'Zweier',Threes:'Dreier',Fours:'Vierer',Fives:'Fünfer',Sixes:'Sechser',
 ThreeOfAKind:'Dreierpasch',FourOfAKind:'Viererpasch',FullHouse:'Full House',
 SmallStraight:'Kleine Straße',LargeStraight:'Große Straße',Kniffel:'Kniffel',Chance:'Chance'},
};
const labels={
 en:{back:'← Casino lobby',setup:'Game setup',title:'MIDNIGHT',subtitle:'DICE',
  eyebrow:'THE MIDNIGHT CLUB · LOUNGE TABLES',casual:'CASUAL · SOLO',casino:'CASINO · FOUR PLAYERS',
  round:'TURN',rolls:'ROLLS',total:'TOTAL POINTS',roll:'ROLL DICE',reroll:'ROLL AGAIN',
  finished:'GAME COMPLETE',waiting:'OPPONENTS PLAYING',held:'HELD',free:'ROLL',result:'RESULT',
  hold:'Click a die to hold or release it · Keys 1–5',scores:'SCORE SHEET',
  upper:'UPPER SECTION',lower:'LOWER SECTION',subtotal:'Subtotal',upperBonus:'Upper bonus',
  lowerSubtotal:'Lower total',extraBonus:'Extra fives',goal:'63 needed for +35',
  score:'RECORD SCORE',scratch:'SCRATCH 0',select:'SELECT A SCORE BOX',choose:'Choose a category after rolling.',
  diceNote:'Up to three rolls per turn. Each score is recorded once.',
  selected:'SELECTED CATEGORY',preview:'POSSIBLE',booked:'BOOKED',scoreHead:'SCORE',
  points:'PTS',player:'YOU',live:'AUTOSAVED',profile:'PLAYER LEVEL',
  opponents:'View opponents’ sheets',receipt:'MATCH RESULTS',help:'R: roll · 1–5: hold · Esc: close',
  settings:'SETTINGS',sound:'Sound effects',animations:'Animations',music:'Lounge music',
  bonus:'ADDITIONAL FIVE OF A KIND',bonusDetail:'Extra +50 points on the completed box.',
  setupTitle:'CHOOSE YOUR DICE NIGHT',setupDesc:'Two independent saves. Casino entry uses play money only.',
  casualTitle:'PLAY AT YOUR PACE',casualDesc:'No entry fee, no AI opponents. Completion awards player and season XP.',
  continue:'Continue casual sheet',newCasual:'Start new casual game',startNew:'Start anyway',
  cancel:'Cancel',confirmReset:'Replace saved casual game?',confirmResetDetail:'This discards the unfinished casual score sheet. Casino progress is unaffected.',
  casinoTitle:'TAKE A SEAT',casinoDesc:'Face Mara, Felix and June across thirteen rounds.',
  stake:'ENTRY FEE',prizes:'POTENTIAL GROSS PRIZES',noCash:'Not enough play money',
  startCasino:'Pay entry & start',activeTable:'Finish or forfeit your active paid table first.',
  resumeCasino:'Resume paid table (no extra fee)',pastCasino:'View last casino result',
  forfeit:'Forfeit paid table…',confirmForfeit:'Confirm forfeit · lose entry',forfeitWarning:'The paid entry will not be refunded. No completion XP or prize is awarded.',
  backGame:'Back to game',standings:'STANDINGS',aiSheets:'OPPONENT SCORE SHEETS',backTable:'Back to dice',
  matchReceipt:'THE MIDNIGHT CLUB / MATCH RECEIPT',casualComplete:'Your sheet is complete',
  casinoComplete:'Your casino result',forfeited:'Table forfeited',tie:' (tied)',
  entry:'ENTRY PAID',payout:'PRIZE RETURNED',net:'NET',netWin:'NET WIN',netLoss:'NET LOSS',
  bankroll:'CASINO BANKROLL',upperShort:'Upper',lowerShort:'Lower',additional:'Extra',kniffels:'Five-of-a-kind rolls',
  completion:'COMPLETION REWARDS',scratchCount:'Scratched boxes',closeResult:'Back to score sheet',
  goSetup:'Game setup',goCasino:'Return to casino',reopenResult:'Match results',
  confirm:'This operation writes to your saved game.',
  noResult:'An unfinished match has no final receipt.',
  loading:'Opening the Midnight Dice tables…',saveUncertain:'No save confirmation received. Reopen the table to recover the committed C# state.',
  recover:'Recover the table',saved:'Your last move is saved before the animation plays.',
  status:'TABLE STATUS',plus:'ADDITIONAL FIVE-OF-A-KIND!',scored:'scored',netLossDetail:'Entry fee minus any returned prize',
  ready:'Ready for your roll',season:'Season XP',xp:'Player XP'},
 de:{back:'← Casino-Lobby',setup:'Spielauswahl',title:'MIDNIGHT',subtitle:'DICE',
  eyebrow:'THE MIDNIGHT CLUB · WÜRFELLOUNGE',casual:'FREIES SPIEL · SOLO',casino:'CASINO · VIER SPIELER',
  round:'RUNDE',rolls:'WÜRFE',total:'GESAMTPUNKTE',roll:'WÜRFELN',reroll:'ERNEUT WÜRFELN',
  finished:'SPIEL BEENDET',waiting:'GEGNER WÜRFELN',held:'GEHALTEN',free:'WÜRFELN',result:'ERGEBNIS',
  hold:'Würfel anklicken zum Halten/Freigeben · Tasten 1–5',scores:'PUNKTEBLOCK',
  upper:'OBERER BLOCK',lower:'UNTERER BLOCK',subtotal:'Zwischensumme',upperBonus:'Oberer Bonus',
  lowerSubtotal:'Unterer Block',extraBonus:'Weitere Kniffel',goal:'63 Punkte für +35',
  score:'PUNKTE EINTRAGEN',scratch:'STREICHEN (0)',select:'FELD AUSWÄHLEN',choose:'Nach dem Wurf eine Kategorie auswählen.',
  diceNote:'Maximal drei Würfe pro Runde. Jede Kategorie nur einmal.',
  selected:'AUSGEWÄHLTES FELD',preview:'MÖGLICH',booked:'GEBUCHT',scoreHead:'PUNKTE',
  points:'PKT',player:'DU',live:'AUTOMATISCH GESPEICHERT',profile:'SPIELERLEVEL',
  opponents:'Gegnerische Blöcke ansehen',receipt:'ERGEBNISSE',help:'R: Würfeln · 1–5: Halten · Esc: Zurück',
  settings:'EINSTELLUNGEN',sound:'Geräusche',animations:'Animationen',music:'Lounge-Musik',
  bonus:'WEITERER KNIFFEL',bonusDetail:'Zusätzliche +50 Punkte nach dem Eintragen.',
  setupTitle:'WÄHLE DEIN WÜRFELSPIEL',setupDesc:'Zwei getrennte Spielstände. Casino-Einsatz nur mit Spielgeld.',
  casualTitle:'IN DEINEM TEMPO',casualDesc:'Kein Einsatz, keine KI-Gegner. Abschluss bringt Spieler- und Saison-XP.',
  continue:'Solo-Spiel fortsetzen',newCasual:'Neues Solo-Spiel',startNew:'Trotzdem neu starten',
  cancel:'Abbrechen',confirmReset:'Solo-Spielstand ersetzen?',confirmResetDetail:'Der unvollständige Punkteblock geht verloren. Der Casino-Tisch bleibt unberührt.',
  casinoTitle:'NIMM PLATZ',casinoDesc:'Gegen Mara, Felix und June über dreizehn Runden.',
  stake:'STARTGELD',prizes:'MÖGLICHE BRUTTOPRÄMIEN',noCash:'Nicht genug Spielgeld',
  startCasino:'Einsatz zahlen & starten',activeTable:'Aktiven Casino-Tisch zuerst abschließen oder aufgeben.',
  resumeCasino:'Casino-Tisch fortsetzen (ohne neuen Einsatz)',pastCasino:'Letztes Casino-Ergebnis ansehen',
  forfeit:'Casino-Tisch aufgeben…',confirmForfeit:'Aufgabe bestätigen · Einsatz verlieren',forfeitWarning:'Der Einsatz wird nicht erstattet. Keine Abschluss-XP und keine Prämie.',
  backGame:'Zurück zum Spiel',standings:'ZWISCHENSTAND',aiSheets:'GEGNERISCHE PUNKTEBLÖCKE',backTable:'Zurück zum Tisch',
  matchReceipt:'THE MIDNIGHT CLUB / SPIELERGEBNIS',casualComplete:'Dein Punkteblock ist voll',
  casinoComplete:'Dein Casino-Ergebnis',forfeited:'Casino-Tisch aufgegeben',tie:' (Gleichstand)',
  entry:'EINSATZ GEZAHLT',payout:'PRÄMIE AUSGEZAHLT',net:'NETTO',netWin:'NETTOGEWINN',netLoss:'NETTOVERLUST',
  bankroll:'CASINO-KONTOSTAND',upperShort:'Oben',lowerShort:'Unten',additional:'Zusatz',kniffels:'Kniffel-Würfe',
  completion:'ABSCHLUSSBELONUNGEN',scratchCount:'Gestrichene Felder',closeResult:'Zurück zum Punkteblock',
  goSetup:'Spielauswahl',goCasino:'Zur Casino-Lobby',reopenResult:'Spielergebnis',
  confirm:'Diese Aktion verändert deinen gespeicherten Spielstand.',
  noResult:'Eine unvollständige Partie hat noch kein Endergebnis.',
  loading:'Würfellounge wird vorbereitet…',saveUncertain:'Keine Speicherbestätigung. Öffne den Tisch erneut, um den C#-Spielstand sicher zu laden.',
  recover:'Tisch wiederherstellen',saved:'Jeder Wurf wird gespeichert, bevor die Animation startet.',
  status:'SPIELSTATUS',plus:'ZUSÄTZLICHER KNIFFEL!',scored:'erzielt',netLossDetail:'Einsatz abzüglich ausgezahlter Prämie',
  ready:'Bereit zum Würfeln',season:'Saison-XP',xp:'Spieler-XP'},
};
function Face({n}:{n:number}){return <span className="md-face">#{n}</span>}
function CategoryRows({game,language,compact=false}:{game:DiceGame;language:'en'|'de';compact?:boolean}){
 return <div className={'md-sheet-rows'+(compact?' compact':'')}>
  {game.categories.map((r,i)=><div key={r.id} className={'md-sheet-static'+(i===6?' break':'')}>
   <span>{names[language][r.id]}</span><strong>{r.stored??'—'}</strong></div>)}
 </div>;
}
function App(){
 const [snapshot,setSnapshot]=useState<DiceSnapshot|null>(null);
 const current=useRef<DiceSnapshot|null>(null);
 const [pending,setPending]=useState<string|null>(null);
 const pendingRef=useRef<string|null>(null);
 const [error,setError]=useState<string|null>(null);
 const [uncertain,setUncertain]=useState(false);
 const [selected,setSelected]=useState<DiceCategory|null>(null);
 const [showSetup,setShowSetup]=useState(false);
 const [showOpponent,setShowOpponent]=useState(false);
 const [showSettings,setShowSettings]=useState(false);
 const [confirmCasual,setConfirmCasual]=useState(false);
 const [confirmForfeit,setConfirmForfeit]=useState(false);
 const [stake,setStake]=useState('100');
 const [rolling,setRolling]=useState(false);
 const [rollingCells,setRollingCells]=useState<number[]>([]);
 const lastRoll=useRef(-1);
 const rollTimer=useRef<ReturnType<typeof setTimeout>|null>(null);
 const [celebration,setCelebration]=useState<DiceCelebration|null>(null);
 const celebrationRef=useRef<DiceCelebration|null>(null);
 const celebrationQueue=useRef<DiceCelebration[]>([]);
 const seenEvents=useRef(new Set<number>());
 const celebrationTimer=useRef<ReturnType<typeof setTimeout>|null>(null);
 const [dismissedResult,setDismissedResult]=useState('');
 const [showResult,setShowResult]=useState(false);
 const state=snapshot?.state,game=state?.game;
 const lang=state?.language??'en',t=labels[lang];
 const animations=!!state?.animationsEnabled&&!window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
 const blocked=!!pending||uncertain||rolling||!!celebration;
 const waiting=state?.mode==='casino'&&state.table?.waiting;
 const finished=!!game&&(state?.mode==='casino'?!!state.table?.settled:game.isComplete);
 const receiptKey=game?.gameId+'-'+state?.mode+'-'+state?.table?.settled+'-'+state?.table?.forfeited;
 const report=(intent:DiceIntent)=>{
  if(!current.current||pendingRef.current||uncertain||rolling||celebrationRef.current)return;
  const id=issueDiceIntent(current.current,intent);
  pendingRef.current=id;setPending(id);setError(null);
 };
 const back=useCallback(()=>{
  if(uncertain){window.chrome?.webview?.postMessage({type:'midnight-dice.recover'});return;}
  if(!pendingRef.current)window.chrome?.webview?.postMessage({type:'midnight-dice.close'});
 },[uncertain]);
 useEffect(()=>{
  const detach=listenDice(msg=>{
   if(msg.type==='reply'){
    if(msg.data.requestId!==pendingRef.current)return;
    pendingRef.current=null;setPending(null);
    if(!msg.data.success)setError(msg.data.message);
    return;
   }
   const next=msg.data;
   if(current.current?.session===next.session&&next.revision<current.current.revision)return;
   if(lastRoll.current>=0&&next.state.rollToken>lastRoll.current
     &&next.state.animationsEnabled&&!window.matchMedia?.('(prefers-reduced-motion: reduce)').matches){
    setRolling(true);setRollingCells(next.state.rollingIndices);
    if(rollTimer.current)clearTimeout(rollTimer.current);
    rollTimer.current=setTimeout(()=>{setRolling(false);setRollingCells([]);},1640);
   }
   lastRoll.current=next.state.rollToken;
   for(const event of next.state.events){
    if(seenEvents.current.has(event.id))continue;
    seenEvents.current.add(event.id);celebrationQueue.current.push(event);
   }
   if(!celebrationRef.current&&celebrationQueue.current.length){
    const event=celebrationQueue.current.shift()!;
    celebrationRef.current=event;setCelebration(event);
   }
   current.current=next;setSnapshot(next);
  });
  window.chrome?.webview?.postMessage({type:'midnight-dice.ready'});
  return()=>{
   detach();if(rollTimer.current)clearTimeout(rollTimer.current);
   if(celebrationTimer.current)clearTimeout(celebrationTimer.current);
  };
 },[]);
 useEffect(()=>{if(snapshot){finishModeBoot();window.chrome?.webview?.postMessage({type:'midnight-dice.ui-ready'});}},[!!snapshot]);
 useEffect(()=>{
  if(!pending)return;
  const timer=setTimeout(()=>{
   setUncertain(true);
   setError(labels[current.current?.state.language??'en'].saveUncertain);
  },12000);
  return()=>clearTimeout(timer);
 },[pending]);
 useEffect(()=>{
  if(!celebration)return;
  celebrationTimer.current=setTimeout(()=>{
   const next=celebrationQueue.current.shift()??null;
   celebrationRef.current=next;setCelebration(next);
  },2500);
  return()=>{if(celebrationTimer.current)clearTimeout(celebrationTimer.current);};
 },[celebration]);
 useEffect(()=>{
  if(finished&&!celebration&&!showSetup&&!showOpponent&&receiptKey!==dismissedResult)
   setShowResult(true);
 },[finished,celebration,showSetup,showOpponent,receiptKey,dismissedResult]);
 useEffect(()=>{setSelected(null);},[state?.mode,game?.filledCount]);
 const onAction=(intent:DiceIntent)=>{
  report(intent);
  if(intent.action==='casual.start'||intent.action==='casual.resume'||intent.action==='casino.start'||intent.action==='casino.resume'){
    setShowSetup(false);setConfirmCasual(false);setConfirmForfeit(false);
    setShowResult(false);setDismissedResult(receiptKey);
  }else if(intent.action==='casino.forfeit'){
    setShowSetup(false);setConfirmForfeit(false);
  }
 };
 const onCloseResult=()=>{
  setShowResult(false);setDismissedResult(receiptKey);
 };
 const openSetup=()=>{
  if(pendingRef.current||rolling||celebrationRef.current)return;
  setShowResult(false);setShowOpponent(false);setShowSetup(true);
  setConfirmForfeit(false);setConfirmCasual(false);
 };
 useEffect(()=>{
  const onKey=(e:KeyboardEvent)=>{
   if(e.ctrlKey||e.altKey||e.metaKey||e.repeat)return;
   const target=e.target as HTMLElement;
   if(target?.tagName==='INPUT'||target?.tagName==='SELECT')return;
   if(e.key==='Escape'){
    e.preventDefault();
    if(showSettings){setShowSettings(false);return;}
    if(showOpponent){setShowOpponent(false);return;}
    if(showResult){onCloseResult();return;}
    if(showSetup){setShowSetup(false);return;}
    back();return;
   }
   if(showResult||showSetup||showOpponent||blocked||waiting||finished)return;
   if(e.key.toLowerCase()==='r'&&game?.canRoll){e.preventDefault();report({action:'roll'});}
   if(/^[1-5]$/.test(e.key)&&game?.canScore&&game.rollsUsed<3){
    e.preventDefault();report({action:'hold',index:Number(e.key)-1});
   }
  };
  document.addEventListener('keydown',onKey);
  return()=>document.removeEventListener('keydown',onKey);
 },[back,showSettings,showOpponent,showResult,showSetup,blocked,waiting,finished,game]);
 const byCategory=useMemo(()=>new Map(game?.categories.map(x=>[x.id,x])??[]),[game?.categories]);
 const selectedScore=selected?byCategory.get(selected):undefined;
 const palette=state?.dicePalette??{top:'#fffcee',bottom:'#d7cbb2',pips:'#2d3730',edge:'#bda36e',glow:null,engraved:false};
 const prizes=state?.stakePrizes.find(x=>x.stake===stake)?.prizes??[];
 const cash=(value:string,sign=false)=>{
  const n=Number(value);
  return (sign&&n>0?'+':n<0?'−':'')+'$'+Math.abs(n).toLocaleString('en-US',{minimumFractionDigits:2,maximumFractionDigits:2});
 };
 const message=error??state?.notice??t.ready;
 return <div className="md-app" data-motion={animations} data-testid="modern-dice-app" data-revision={snapshot?.revision??-1}
  data-mode={state?.mode??'loading'} data-dice-variant={state?.diceVariant??'dice-ivory'} lang={lang}>
  <DiceAmbient enabled={animations}/>
  <header className="md-header">
    <div className="md-logo"><small>{t.eyebrow}</small>
      <h1>{t.title} <em>{t.subtitle}</em></h1></div>
    <div className="md-header-right">
      <div className="md-header-stat"><small>{t.profile}</small><strong>{state?.profile?.level??'—'}</strong></div>
      <div className="md-header-stat"><small>{t.bankroll}</small><strong>{state?cash(state.bankroll):'—'}</strong></div>
      <button data-testid="dice-setup" className="md-outline" disabled={blocked} onClick={openSetup}>{t.setup}</button>
      <button data-testid="dice-back" className="md-outline" disabled={!!pending&&!uncertain} onClick={back}>{t.back}</button>
    </div>
  </header>
  {!state||!game?<main className="md-loading"><span>✦</span>{t.loading}</main>:
  <div className="md-content">
    <div className="md-topline">
      <span className="md-mode">{state.mode==='casino'?t.casino:t.casual}</span>
      <span className="md-save-dot">● {t.live}</span>
      <span className="md-equipped-dice" data-testid="dice-equipped-skin">
        <i aria-hidden="true">◆</i>
        <span>{lang==='de'?'AUSGERÜSTETES WÜRFELSET':'EQUIPPED DICE SET'}</span>
        <strong>{lang==='de'?diceSkin(state.diceVariant).de:diceSkin(state.diceVariant).name}</strong>
      </span>
      {state.mode==='casino'&&state.table&&<span className="md-stake">
        {t.stake}: {cash(state.table.stake)}</span>}
    </div>
    <div className="md-columns">
      <section className="md-table">
        <div className="md-table-header">
          <div><small>{t.round}</small><strong data-testid="dice-turn">{game.isComplete?13:game.filledCount+1}<span> / 13</span></strong></div>
          <div><small>{t.rolls}</small><strong data-testid="dice-rolls">{game.rollsUsed}<span> / 3</span></strong></div>
          <div className="md-total"><small>{t.total}</small><strong data-testid="dice-total">{game.total}</strong></div>
        </div>
        <div className="md-felt">
          <div className="md-felt-inner">
            <div className="md-felt-brand"><span>✦</span> THE MIDNIGHT CLUB <span>✦</span></div>
            <div className="md-felt-arc"/>
            <div className="md-dice">
              {game.dice.map((number,i)=>
               <button key={i} type="button" className={'md-die'+(game.held[i]?' held':'')
                 +(rolling&&rollingCells.includes(i)?' rolling':'')}
                 data-testid={'dice-die-'+i} data-held={String(game.held[i])} data-face={number} data-roll-token={state.rollToken}
                 aria-label={(lang==='de'?'Würfel ':'Die ')+(i+1)+': '+number+', '+(game.held[i]?t.held:t.free)}
                 disabled={blocked||waiting||finished||!game.canScore||game.rollsUsed===3}
                 onClick={()=>report({action:'hold',index:i})}>
                  <DiceCube value={number} palette={palette} variant={state.diceVariant}
                    rolling={rolling&&rollingCells.includes(i)} index={i} token={state.rollToken}/>
                  <span className="md-die-status">{game.held[i]?t.held:game.rollsUsed===3?t.result:t.free}</span>

               </button>)}
            </div>
            <div className="md-felt-hint">{t.hold}</div>
          </div>
        </div>
        <div className="md-roll-actions">
          <button data-testid="dice-roll" className="md-roll-button"
           disabled={blocked||waiting||finished||!game.canRoll} onClick={()=>report({action:'roll'})}>
           <span className="md-roll-icon">⚄</span>
           {rolling? (lang==='de'?'WÜRFEL ROLLEN…':'ROLLING DICE…') :
             waiting?t.waiting:finished?t.finished:game.rollsUsed===0?t.roll:t.reroll}
          </button>
          <div className="md-roll-note">{game.isAdditionalKniffel?t.bonus+' · +50':t.diceNote}</div>
        </div>
        {state.mode==='casino'&&state.table&&<div className="md-standings">
          <div className="md-section-title"><strong>{t.standings}</strong>
           <button data-testid="dice-opponents" onClick={()=>setShowOpponent(true)}>{t.opponents}</button></div>
          <div className="md-standing-row">
           {[{name:t.player,score:game.total,player:true},...state.table.opponents.map(op=>({name:op.name,score:op.game.total,player:false}))].map(seat=>
             <div key={seat.name} className={seat.player?'player':''}><span>{seat.name}</span><strong>{seat.score}</strong></div>)}
          </div>
          {waiting&&<p className="md-waiting">{t.waiting} · Mara · Felix · June</p>}
        </div>}
        <div className="md-status" role="status" aria-live="polite">
          <small>{t.status} {pending?' · SAVING':''}</small>
          <strong data-testid="dice-notice">{message}</strong>
          {state.completionNotice&&<span className="md-xp-reward">{state.completionNotice}</span>}
          <p>{t.saved}</p>
        </div>
      </section>
      <section className="md-scorecard">
        <div className="md-scorecard-top">
          <h2>{t.scores}</h2>
          <div><small>{t.total}</small><strong>{game.total}</strong></div>
        </div>
        <div className="md-scorecard-heading"><span>{t.upper}</span><span>{t.scoreHead}</span></div>
        <div className="md-category-list" data-testid="dice-score-sheet">
          {game.categories.map((row,index)=>
           <div key={row.id} className={index===6?'md-lower-break':''}>
            {index===6&&<div className="md-scorecard-heading"><span>{t.lower}</span><span>{t.scoreHead}</span></div>}
            <button type="button" data-testid={'dice-category-'+row.id}
             data-filled={row.stored!==null} data-preview={row.preview??''}
             className={'md-category'+(selected===row.id?' selected':'')
                +(row.stored!==null?' filled':row.preview!==null&&row.preview>0?' positive':'')}
             disabled={blocked||waiting||finished||!game.canScore||row.stored!==null}
             onClick={()=>setSelected(row.id)}>
             <span className="md-category-name">{names[lang][row.id]}</span>
             {row.stored!==null?<strong>{row.stored}</strong>:
              row.preview!==null?<strong className={row.preview>0?'candidate':''}>
                {row.preview>0?'+'+row.preview:'0 ×'}</strong>:<strong>—</strong>}
            </button>
           </div>)}
        </div>
        <div className="md-bonus-totals">
          <div><span>{t.subtotal}</span><strong>{game.upperSubtotal}</strong></div>
          <div><span>{t.upperBonus} <small>({t.goal})</small></span><strong>+{game.upperBonus}</strong></div>
          <div><span>{t.lowerSubtotal}</span><strong>{game.lowerSubtotal}</strong></div>
          <div><span>{t.extraBonus}</span><strong>+{game.additionalKniffelBonus}</strong></div>
        </div>
        <div className="md-selection-controls">
         <div className="md-selection-caption">{selectedScore?t.selected:t.select}</div>
         <strong className="md-selection-name">{selectedScore?names[lang][selectedScore.id]:t.choose}</strong>
         <div className="md-scoring-buttons">
          <button data-testid="dice-commit" disabled={blocked||waiting||finished||!selectedScore||!(selectedScore.preview&&selectedScore.preview>0)}
           onClick={()=>selected&&report({action:'score',category:selected})}>{t.score}
           {selectedScore?.preview? ' · '+selectedScore.preview:''}</button>
          <button data-testid="dice-scratch" disabled={blocked||waiting||finished||!selectedScore}
           onClick={()=>selected&&report({action:'scratch',category:selected})}>{t.scratch}</button>
         </div>
        </div>
      </section>
    </div>
    <footer className="md-footer">
      <div>✦ {t.help}</div>
      <div className="md-footer-actions">
       {(finished)&&<button data-testid="dice-result" onClick={()=>setShowResult(true)}>{t.reopenResult}</button>}
       <button data-testid="dice-settings" onClick={()=>setShowSettings(x=>!x)}>⚙ {t.settings}</button>
      </div>
    </footer>
    {showSettings&&<div className="md-settings-panel" data-testid="dice-settings-panel">
      {[['sound.set',t.sound,state.soundEnabled],['animations.set',t.animations,state.animationsEnabled],
       ['music.set',t.music,state.musicEnabled]].map(([action,title,checked])=>
       <label key={String(action)}>
        <input type="checkbox" checked={Boolean(checked)} disabled={!!pending}
         onChange={e=>report({action:action as 'sound.set'|'animations.set'|'music.set',
           value:e.target.checked?'true':'false'})}/>
        {String(title)}</label>)}
     </div>}
   </div>}
   {celebration&&<div className="md-celebration-layer" data-testid="dice-celebration">
    <div className={'md-celebration'+(animations?' animated':'')}>
      <small>{celebration.player} {t.scored}</small><h2>✦ {t.plus} ✦</h2>
      <div className="md-five-dice">{Array.from({length:5},(_,i)=><div key={i}>
       {celebration.face>0?<Die value={celebration.face} palette={palette} variant={state.diceVariant}/>:<span>★</span>}</div>)}</div>
      <p>{names[lang][celebration.category]} · {celebration.points} {t.points}
        {celebration.bonus>0?' · +'+celebration.bonus+' BONUS':''}</p>
    </div>
   </div>}
   {state&&showSetup&&<div className="md-modal-shade" role="presentation">
    <div className="md-modal md-setup-modal" role="dialog" aria-modal="true" aria-label={t.setupTitle}
      data-testid="dice-setup-panel">
      <div className="md-modal-head"><div><small>{t.eyebrow}</small><h2>{t.setupTitle}</h2>
         <p>{t.setupDesc}</p></div>
       <button aria-label={t.cancel} onClick={()=>setShowSetup(false)}>×</button></div>
      <div className="md-game-choices">
       <section className="md-choice">
        <span className="md-choice-tag">{t.casual}</span><h3>{t.casualTitle}</h3>
        <p>{t.casualDesc}</p>
        <p className="md-choice-muted">{state.casualHasSaved?
          lang==='de'?'Gespeicherter Punkteblock vorhanden.':'Saved score sheet available.':
          lang==='de'?'Noch kein Solo-Fortschritt.':'No existing casual progress.'}</p>
        {state.casualHasSaved&&<button className="md-cta secondary" data-testid="dice-casual-resume"
          disabled={!!pending} onClick={()=>onAction({action:'casual.resume'})}>{t.continue}</button>}
        <button className="md-cta" data-testid="dice-casual-new" disabled={!!pending}
          onClick={()=>state.casualHasSaved&&!state.casualCompleted?
            setConfirmCasual(true):onAction({action:'casual.start'})}>{t.newCasual}</button>
        {confirmCasual&&<div className="md-confirm" data-testid="dice-casual-confirm">
          <strong>{t.confirmReset}</strong><p>{t.confirmResetDetail}</p>
          <button onClick={()=>onAction({action:'casual.start'})}>{t.startNew}</button>
          <button onClick={()=>setConfirmCasual(false)}>{t.cancel}</button>
        </div>}
       </section>
       <section className="md-choice casino">
        <span className="md-choice-tag">{t.casino}</span><h3>{t.casinoTitle}</h3>
        <p>{t.casinoDesc}</p>
        <label className="md-stake-label">{t.stake}
         <select data-testid="dice-stake" value={stake} onChange={e=>setStake(e.target.value)}>
          {state.stakeOptions.map(x=><option key={x} value={x}>{cash(x)}</option>)}
         </select></label>
        <div className="md-prize-grid"><small>{t.prizes}</small><div>
         {prizes.map((x,i)=><span key={i}>#{i+1} <strong>{cash(x)}</strong></span>)}</div></div>
        <button className="md-cta" data-testid="dice-casino-start"
         disabled={!!pending||!!(state.table&&!state.table.settled)||Number(stake)>Number(state.bankroll)}
         onClick={()=>onAction({action:'casino.start',value:stake})}>
          {state.table&&!state.table.settled?t.activeTable:Number(stake)>Number(state.bankroll)?
            t.noCash:t.startCasino+' · '+cash(stake)}
        </button>
        {state.table&&<>
          <button className="md-cta secondary" data-testid="dice-casino-resume"
            disabled={!!pending} onClick={()=>onAction({action:'casino.resume'})}>
             {state.table.settled?t.pastCasino:t.resumeCasino}</button>
          {!state.table.settled&&<div className="md-forfeit">
             <button data-testid="dice-casino-forfeit" className="md-danger" disabled={!!pending||state.table.waiting}
                onClick={()=>confirmForfeit?onAction({action:'casino.forfeit'}):setConfirmForfeit(true)}>
                {confirmForfeit?t.confirmForfeit:t.forfeit}</button>
             {confirmForfeit&&<p>{t.forfeitWarning}</p>}
           </div>}
        </>}
       </section>
      </div>
      {error&&<div className="md-error">{error}</div>}
      <button className="md-modal-back" data-testid="dice-setup-close" onClick={()=>setShowSetup(false)}>{t.backGame}</button>
    </div>
   </div>}
   {state&&game&&showOpponent&&state.table&&<div className="md-modal-shade">
     <div className="md-modal md-opponent-modal" role="dialog" aria-modal="true" aria-label={t.aiSheets}
       data-testid="dice-opponents-panel">
       <div className="md-modal-head"><div><small>{t.standings}</small><h2>{t.aiSheets}</h2></div>
        <button aria-label={t.cancel} onClick={()=>setShowOpponent(false)}>×</button></div>
       <div className="md-ai-grids">
        {state.table.opponents.map(op=><section key={op.name}><h3>{op.name} <small>{op.style}</small></h3>
          <CategoryRows game={op.game} language={lang} compact/>
          <div className="md-ai-total">{t.total} <strong>{op.game.total}</strong></div>
          <small>{t.upperBonus}: +{op.game.upperBonus} · {t.extraBonus}: +{op.game.additionalKniffelBonus}</small>
        </section>)}
       </div>
       <button data-testid="dice-opponents-close" className="md-modal-back" onClick={()=>setShowOpponent(false)}>{t.backTable}</button>
     </div>
   </div>}
   {state&&game&&showResult&&finished&&!celebration&&<div className="md-modal-shade">
    <div className="md-modal md-result-modal" role="dialog" aria-modal="true" aria-label={t.matchReceipt}
      data-testid="dice-result-panel">
     <div className="md-modal-head"><div><small>{t.matchReceipt}</small><h2>{state.table?.forfeited&&state.mode==='casino'?t.forfeited:
        state.mode==='casino'?t.casinoComplete:t.casualComplete}</h2>
       <p>{state.mode==='casino'?t.casino+' · '+(state.table?.settled?'#'+state.table.placement:'')+(
         state.table?.tied?t.tie:''):t.casual+' · 13 '+t.round}</p></div>
       <button aria-label={t.cancel} onClick={onCloseResult}>×</button></div>
     {state.mode==='casino'&&state.table&&<div className="md-receipt-money">
       {[{label:t.entry,value:'−'+cash(state.table.stake)},
         {label:t.payout,value:'+'+cash(state.table.payout)},
         {label:Number(state.table.net)>=0?t.netWin:t.netLoss,value:cash(state.table.net,true)}].map(x=>
           <div key={x.label}><small>{x.label}</small><strong>{x.value}</strong></div>)}
       <p>{t.bankroll} · {cash(state.bankroll)}</p>
     </div>}
     <div className="md-results-scroll">
      <div className="md-results-standings">
       <div className="md-result-heading">{t.standings}</div>
       {[{name:t.player,game,player:true},...(state.mode==='casino'?state.table?.opponents.map(x=>({
         name:x.name,game:x.game,player:false}))??[]:[])].sort((a,b)=>b.game.total-a.game.total)
        .map((x,i)=><div key={x.name} className={'md-result-seat'+(x.player?' player':'')}>
           <span>{state.table?.forfeited?'—':'#'+(1+[game,...(state.table?.opponents.map(y=>y.game)??[])].filter(y=>y.total>x.game.total).length)}</span>
           <strong>{x.name}</strong><span>{t.upperShort} {x.game.upperSubtotal}+{x.game.upperBonus}</span>
           <span>{t.lowerShort} {x.game.lowerSubtotal}</span>
           <span>{t.additional} {x.game.additionalKniffelBonus}</span>
           <strong>{x.game.total}</strong>
         </div>)}
      </div>
      <div className="md-result-detail">
       <span>{game.filledCount}/13 {t.scores} · {game.categories.filter(x=>x.stored===0).length} {t.scratchCount}</span>
       <span>{t.kniffels}: {game.kniffelsScored}</span>
       <span>{state.table?.forfeited?'0 '+t.xp+' · 0 '+t.season:
         state.completionXp+' '+t.xp+' · '+state.completionSeasonXp+' '+t.season}</span>
       {state.completionNotice&&<strong>{state.completionNotice}</strong>}
      </div>
     </div>
     <div className="md-result-actions">
       <button data-testid="dice-result-close" onClick={onCloseResult}>{t.closeResult}</button>
       <button data-testid="dice-result-setup" onClick={()=>{onCloseResult();openSetup();}}>{t.goSetup}</button>
       <button data-testid="dice-result-lobby" onClick={back}>{t.goCasino}</button>
     </div>
    </div>
   </div>}
  </div>;
}
createRoot(document.getElementById('root')!).render(<App/>);
