import {finishModeBoot} from '../boot/finishModeBoot';
import { useEffect, useMemo, useRef, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { isLoungeId, readLoungeSnapshot, sendLoungeCommand } from './bridge';
import type { LoungeId, LoungeSnapshot } from './bridge';
import { SudokuScreen } from './sudoku';
import { SolitaireScreen } from './solitaire';
import { WordSearchScreen } from './wordSearch';
import { MemoryScreen } from './memory';
import { JigsawScreen } from './jigsaw';
import { AdventureScreen } from './adventure';
import { TowerDefenseScreen } from './towerDefense';
import { MinesweeperScreen } from './minesweeper';
import { isSudokuReply, issueSudokuIntent } from './sudokuBridge';
import type { SudokuIntent } from './sudokuBridge';
import { isSolitaireReply, issueSolitaireIntent } from './solitaireBridge';
import type { SolitaireIntent } from './solitaireBridge';
import { isWordSearchReply, issueWordSearchIntent } from './wordSearchBridge';
import type { WordSearchIntent } from './wordSearchBridge';
import { isMemoryReply, issueMemoryIntent } from './memoryBridge';
import type { MemoryIntent } from './memoryBridge';
import { isJigsawReply, issueJigsawIntent } from './jigsawBridge';
import type { JigsawIntent } from './jigsawBridge';
import { isTaleReply, issueTaleIntent } from './adventureBridge';
import type { TaleIntent } from './adventureBridge';
import {isTowerReply,issueTowerIntent} from './towerDefenseBridge';
import type {TowerIntent} from './towerDefenseBridge';
import {isSweepReply,issueSweepIntent} from './minesweeperBridge';
import type {SweepIntent} from './minesweeperBridge';
import {LoungePreview} from './LoungePreview';
import './lounge.css';

type Language = 'de' | 'en';
type Filter = 'all' | 'puzzles' | 'cards' | 'stories' | 'strategy';
type Localized = { en: string; de: string };
type GameInfo = { id: LoungeId; category: Exclude<Filter,'all'>; title: Localized; subtitle: Localized;
  description: Localized; icon: string; decoration: string; };
const catalog: GameInfo[] = [
  { id:'sudoku', category:'puzzles', title:{en:'Sudoku',de:'Sudoku'},
    subtitle:{en:'The quiet grid',de:'Das stille Raster'},
    description:{en:'Six difficulties, daily puzzles and a saved game for every quiet moment.',
      de:'Sechs Schwierigkeitsgrade, Tagesrätsel und gespeicherte Partien für jede ruhige Minute.'},
    icon:'▦',decoration:'1  ·  9  ·  3' },
  { id:'solitaire', category:'cards', title:{en:'Midnight Solitaire',de:'Midnight Solitaire'},
    subtitle:{en:'A well-played hand',de:'Ein ruhiges Kartenspiel'},
    description:{en:'Klondike, FreeCell, Spider, Pyramid, Golf and TriPeaks. Your deals stay saved.',
      de:'Klondike, FreeCell, Spider, Pyramid, Golf und TriPeaks. Deine Partien bleiben gespeichert.'},
    icon:'♠',decoration:'A  ♠  K  ♥' },
  { id:'word-search',category:'puzzles',title:{en:'Word Search',de:'Wortsuche'},
    subtitle:{en:'Find your words',de:'Finde deine Wörter'},
    description:{en:'Explore themed letter grids without a countdown. Continue your last puzzle anytime.',
      de:'Entdecke Themenrätsel ohne Zeitdruck. Dein letztes Rätsel lässt sich jederzeit fortsetzen.'},
    icon:'⌕',decoration:'W  O  R  D' },
  { id:'memory',category:'cards',title:{en:'Memory',de:'Memory'},
    subtitle:{en:'A little recollection',de:'Eine kleine Erinnerung'},
    description:{en:'Turn cards, uncover pairs and build your streak. Every match at your pace.',
      de:'Decke Karten auf, finde Paare und baue deine Serie aus. Ganz in deinem Tempo.'},
    icon:'◈',decoration:'◇  ◆  ◇  ◆' },
  { id:'jigsaw',category:'puzzles',title:{en:'Jigsaw Puzzle',de:'Puzzle'},
    subtitle:{en:'Piece by piece',de:'Stück für Stück'},
    description:{en:'Pick a scene and a piece count. Leave and return to the same unfinished picture.',
      de:'Wähle Motiv und Teilezahl. Ein begonnenes Bild wartet beim nächsten Besuch auf dich.'},
    icon:'✧',decoration:'◪  ◩  ◰' },
  { id:'tales-backroom',category:'stories',title:{en:'Tales from the Backroom',de:'Tales from the Backroom'},
    subtitle:{en:'A story behind the door',de:'Geschichten hinter der Tür'},
    description:{en:'Create a character, roll the d20 and follow a branching tabletop adventure.',
      de:'Erstelle deinen Charakter, wirf den W20 und erlebe verzweigte Pen-&-Paper-Abenteuer.'},
    icon:'✦',decoration:'D20  ·  STORY' },
  { id:'tower-defense',category:'strategy',title:{en:'Midnight Bastion',de:'Midnight Bastion'},
    subtitle:{en:'Hold the midnight line',de:'Verteidige die Mitternachtsfeste'},
    description:{en:'Build towers, upgrade defences and face fifteen enemy waves. A strategy game at your own pace.',
      de:'Errichte Türme, verbessere deine Verteidigung und überstehe 15 Gegnerwellen. Strategie in deinem Tempo.'},
    icon:'♜',decoration:'DEFEND  ·  UPGRADE  ·  SURVIVE' },
  { id:'minesweeper',category:'strategy',title:{en:'Midnight Sweep',de:'Midnight Sweep'},
    subtitle:{en:'Chart the unknown',de:'Kartiere die Dunkelheit'},
    description:{en:'Reveal a nocturnal minefield, mark hazards and use one Echo Scan to read the stars.',
      de:'Decke ein nächtliches Minenfeld auf, markiere Gefahren und nutze einen Echo-Scan, um die Sterne zu lesen.'},
    icon:'✺',decoration:'OBSERVE  ·  MARK  ·  SURVIVE' },
];
const words = {
 en: {club:'THE MIDNIGHT CLUB',back:'← Casino lobby',kicker:'A QUIETER CORNER',
 title:'Your time, your pace.',intro:'Take a seat away from the casino floor. Eight distinct ways to unwind, all available offline.',
 all:'All activities',puzzles:'Puzzles',cards:'Cards & memory',stories:'Adventures',strategy:'Strategy',
 play:'ENTER GAME',continued:'Your progress is preserved',loading:'Opening your lounge…',
 foot:'THE CHILL LOUNGE',note:'No entry fees. No rush. Save states and rewards stay with your existing profile.',
 version:'LOCAL · OFFLINE'},
 de: {club:'THE MIDNIGHT CLUB',back:'← Casino-Lobby',kicker:'EINE RUHIGERE ECKE',
 title:'Deine Zeit. Dein Tempo.',intro:'Eine kleine Auszeit vom Casino. Acht unterschiedliche Spiele, vollständig offline verfügbar.',
 all:'Alle Aktivitäten',puzzles:'Rätsel',cards:'Karten & Memory',stories:'Abenteuer',strategy:'Strategie',
 play:'SPIEL ÖFFNEN',continued:'Dein Fortschritt bleibt gespeichert',loading:'Deine Lounge wird geöffnet…',
 foot:'THE CHILL LOUNGE',note:'Keine Einsätze. Kein Zeitdruck. Spielstände und Belohnungen bleiben erhalten.',
 version:'LOKAL · OFFLINE'},
};
function App() {
 const [snapshot,setSnapshot]=useState<LoungeSnapshot|null>(null);
 const [filter,setFilter]=useState<Filter>('all');
 const [pending,setPending]=useState<string|null>(null);
 const [sudokuError,setSudokuError]=useState('');
 const [solitairePending,setSolitairePending]=useState<string|null>(null);
 const [solitaireError,setSolitaireError]=useState('');
 const [wordSearchPending,setWordSearchPending]=useState<string|null>(null);
 const [wordSearchError,setWordSearchError]=useState('');
 const [memoryPending,setMemoryPending]=useState<string|null>(null);
 const [memoryError,setMemoryError]=useState('');
 const [jigsawPending,setJigsawPending]=useState<string|null>(null);
 const [jigsawError,setJigsawError]=useState('');
 const [adventurePending,setAdventurePending]=useState<string|null>(null);
 const [adventureError,setAdventureError]=useState('');
 const [towerDefensePending,setTowerDefensePending]=useState<string|null>(null);
 const [towerDefenseError,setTowerDefenseError]=useState('');
 const [sweepPending,setSweepPending]=useState<string|null>(null);
 const [sweepError,setSweepError]=useState('');
 const signalled=useRef(false);
 useEffect(()=>{
   const webview=window.chrome?.webview;
   const handler=(event:MessageEvent)=>{
     const incoming=readLoungeSnapshot(event.data);
     if (incoming) {
       setSnapshot(incoming);
       // Clear the stale-revision notice only after a valid authoritative frame arrives.
       if(incoming.towerDefense)
         setTowerDefenseError(previous=>previous==='Defence changed. Refreshing.'?'':previous);
     }
     const msg:unknown=event.data;
     if(msg&&typeof msg==='object'&&!Array.isArray(msg)
       &&'type' in msg&&msg.type==='lounge.tower-defense.result'
       &&'data' in msg&&isTowerReply(msg.data)){
       const reply=msg.data;
       setTowerDefensePending(prev=>prev===reply.requestId?null:prev);
       setTowerDefenseError(reply.success?'':reply.message);
       if(!reply.success && reply.message==='Defence changed. Refreshing.')
         sendLoungeCommand('lounge.ready');
     }
     if(msg&&typeof msg==='object'&&!Array.isArray(msg)
       &&'type' in msg&&msg.type==='lounge.minesweeper.result'
       &&'data' in msg&&isSweepReply(msg.data)){
       const reply=msg.data;
       setSweepPending(prev=>prev===reply.requestId?null:prev);
       setSweepError(reply.success?'':reply.message);
       if(!reply.success&&reply.message==='Field changed. Refreshing.')
         sendLoungeCommand('lounge.ready');
     }
     if(msg&&typeof msg==='object'&&!Array.isArray(msg)
       &&'type' in msg&&msg.type==='lounge.sudoku.result'
       &&'data' in msg&&isSudokuReply(msg.data)){
       const reply=msg.data;
       setPending(prev=>prev===reply.requestId?null:prev);
       if(!reply.success)setSudokuError(reply.message);
       else setSudokuError('');
     }
     if(msg&&typeof msg==='object'&&!Array.isArray(msg)
       &&'type' in msg&&msg.type==='lounge.solitaire.result'
       &&'data' in msg&&isSolitaireReply(msg.data)){
       const reply=msg.data;
       setSolitairePending(prev=>prev===reply.requestId?null:prev);
       setSolitaireError(reply.success?'':reply.message);
     }
     if(msg&&typeof msg==='object'&&!Array.isArray(msg)
       &&'type' in msg&&msg.type==='lounge.word-search.result'
       &&'data' in msg&&isWordSearchReply(msg.data)){
       const reply=msg.data;
       setWordSearchPending(prev=>prev===reply.requestId?null:prev);
       setWordSearchError(reply.success?'':reply.message);
     }
     if(msg&&typeof msg==='object'&&!Array.isArray(msg)
       &&'type' in msg&&msg.type==='lounge.memory.result'
       &&'data' in msg&&isMemoryReply(msg.data)){
       const reply=msg.data;
       setMemoryPending(prev=>prev===reply.requestId?null:prev);
       setMemoryError(reply.success?'':reply.message);
     }
     if(msg&&typeof msg==='object'&&!Array.isArray(msg)
       &&'type' in msg&&msg.type==='lounge.jigsaw.result'
       &&'data' in msg&&isJigsawReply(msg.data)){
       const reply=msg.data;
       setJigsawPending(prev=>prev===reply.requestId?null:prev);
       setJigsawError(reply.success?'':reply.message);
     }
     if(msg&&typeof msg==='object'&&!Array.isArray(msg)
       &&'type' in msg&&msg.type==='lounge.adventure.result'
       &&'data' in msg&&isTaleReply(msg.data)){
       const reply=msg.data;
       setAdventurePending(prev=>prev===reply.requestId?null:prev);
       setAdventureError(reply.success?'':reply.message);
     }
   };
   webview?.addEventListener('message',handler);
   sendLoungeCommand('lounge.ready');
   return()=>webview?.removeEventListener('message',handler);
 },[]);
 useEffect(()=>{
   if (snapshot && !signalled.current) {
     signalled.current=true;
     finishModeBoot();
      sendLoungeCommand('lounge.ui-ready');
   }
 },[snapshot]);
 useEffect(()=>{
  if(!pending&&!solitairePending&&!wordSearchPending&&!memoryPending&&!jigsawPending&&!adventurePending&&!towerDefensePending&&!sweepPending)return;
  const id=window.setTimeout(()=>{
   const message=snapshot?.language==='de'?'Bestätigung fehlt. Der aktuelle Spielstand wird neu geladen.':'Confirmation missing. Refreshing the saved game.';
   if(pending){setPending(null);setSudokuError(message);}
   if(solitairePending){setSolitairePending(null);setSolitaireError(message);}
   if(wordSearchPending){setWordSearchPending(null);setWordSearchError(message);}
   if(memoryPending){setMemoryPending(null);setMemoryError(message);}
   if(jigsawPending){setJigsawPending(null);setJigsawError(message);}
   if(adventurePending){setAdventurePending(null);setAdventureError(message);}
   if(towerDefensePending){setTowerDefensePending(null);setTowerDefenseError(message);}
   if(sweepPending){setSweepPending(null);setSweepError(message);}
   sendLoungeCommand('lounge.ready');
  },8000);
  return()=>window.clearTimeout(id);
 },[pending,solitairePending,wordSearchPending,memoryPending,jigsawPending,adventurePending,towerDefensePending,sweepPending,snapshot?.language]);
 const language:Language=snapshot?.language??'en', t=words[language];
 const showingOverview=!!snapshot&&!snapshot.sudoku&&!snapshot.solitaire
   &&!snapshot.wordSearch&&!snapshot.memory&&!snapshot.jigsaw&&!snapshot.adventure
   &&!snapshot.towerDefense&&!snapshot.minesweeper;
 const filtered=useMemo(()=>catalog.filter(game=>filter==='all'||game.category===filter),[filter]);
 const statusMap=useMemo(()=>new Map<LoungeId,string|null>((snapshot?.games??[]).map(game=>[game.id,game.status] as const)),[snapshot]);
 const groups: {id:Filter;name:string}[]=[
   {id:'all',name:t.all},{id:'puzzles',name:t.puzzles},
   {id:'cards',name:t.cards},{id:'stories',name:t.stories},{id:'strategy',name:t.strategy},
 ];
 function open(id:LoungeId) { if(snapshot && isLoungeId(id))sendLoungeCommand('lounge.open',id); }
 function towerDefenseCommand(intent:TowerIntent){
   if(!snapshot||(towerDefensePending&&intent.action!=='tower.exit')||!snapshot.towerDefense)return;
   setTowerDefenseError('');
   setTowerDefensePending(issueTowerIntent(snapshot.towerDefenseSession,snapshot.towerDefenseRevision,intent));
 }
 function sweepCommand(intent:SweepIntent){
   if(!snapshot||!snapshot.minesweeper||(sweepPending&&intent.action!=='mine.exit'))return;
   setSweepError('');
   setSweepPending(issueSweepIntent(snapshot.minesweeperSession,snapshot.minesweeperRevision,intent));
 }
 function command(intent:SudokuIntent) {
   if(!snapshot||pending||!snapshot.sudoku)return;
   setSudokuError('');
   setPending(issueSudokuIntent(snapshot.session,snapshot.revision,intent));
 }
 function solitaireCommand(intent:SolitaireIntent){
   if(!snapshot||solitairePending||!snapshot.solitaire)return;
   setSolitaireError('');
   setSolitairePending(issueSolitaireIntent(snapshot.solitaireSession,snapshot.solitaireRevision,intent));
 }
 function wordSearchCommand(intent:WordSearchIntent){
   if(!snapshot||wordSearchPending||!snapshot.wordSearch)return;
   setWordSearchError('');
   setWordSearchPending(issueWordSearchIntent(snapshot.wordSearchSession,snapshot.wordSearchRevision,intent));
 }
 function adventureCommand(intent:TaleIntent){
   if(!snapshot||adventurePending||!snapshot.adventure)return;
   setAdventureError('');
   setAdventurePending(issueTaleIntent(snapshot.adventureSession,snapshot.adventureRevision,intent));
 }
 function jigsawCommand(intent:JigsawIntent){
   if(!snapshot||jigsawPending||!snapshot.jigsaw)return;
   setJigsawError('');
   setJigsawPending(issueJigsawIntent(snapshot.jigsawSession,snapshot.jigsawRevision,intent));
 }
 function memoryCommand(intent:MemoryIntent){
   if(!snapshot||memoryPending||!snapshot.memory)return;
   setMemoryError('');
   setMemoryPending(issueMemoryIntent(snapshot.memorySession,snapshot.memoryRevision,intent));
 }
 return <main className={"chill-shell"+(snapshot?.boardOnly?" board-only":"")+(snapshot?.towerDefense?" bastion-open":"")+(snapshot?.minesweeper?" sweep-open":"")+(showingOverview?" chill-home":"")+(snapshot?.memory?.screen==='library'?" memory-library-open":"")+(snapshot?.jigsaw?.screen==="game"?" jigsaw-open":"")} data-pending={!!(pending||solitairePending||wordSearchPending||memoryPending||jigsawPending||adventurePending||towerDefensePending||sweepPending)} data-motion={snapshot?.animationsEnabled??true}>
   <div className="chill-sparkles" aria-hidden="true"/>
   <header className="chill-header">
     <div className="chill-mark" aria-hidden="true">✦</div>
     <div className="chill-brand"><span>{t.club}</span><strong>Chill Lounge</strong></div>
     <span className="chill-local">{t.version}</span>
     {snapshot?.minesweeper
       ? <button className="chill-exit" disabled={!!sweepPending}
           onClick={()=>sweepCommand({action:snapshot.minesweeper?.screen==='menu'?'mine.exit':'mine.menu'})}>
           {snapshot.minesweeper.screen==='menu'?'← Chill Lounge':
            (language==='de'?'← Feldauswahl':'← Field selection')}</button>
       : snapshot?.towerDefense
       ? <button className="chill-exit"
           onClick={()=>towerDefenseCommand({action:snapshot.towerDefense?.screen==='menu'?'tower.exit':'tower.menu'})}>
             {snapshot.towerDefense.screen==='menu'?'← Chill Lounge':(language==='de'?'← Kartenauswahl':'← Map selection')}</button>
       : snapshot?.adventure
       ? <button className="chill-exit" disabled={!!adventurePending||snapshot.adventure.screen==='encounter'}
           onClick={()=>adventureCommand({action:snapshot.adventure?.screen==='library'?'adventure.exit':
             snapshot.adventure?.screen==='board'?'adventure.board.close':'adventure.back'})}>
           {snapshot.adventure.screen==='library'?'← Chill Lounge':
             snapshot.adventure.screen==='board'?(language==='de'?'← Zur Geschichte':'← Back to story'):
             snapshot.adventure.screen==='encounter'?(language==='de'?'Tabletop-Begegnung':'Tabletop encounter'):
             (language==='de'?'← Abenteuer':'← Adventure library')}</button>
       : snapshot?.jigsaw
       ? <button className="chill-exit" disabled={!!jigsawPending}
           onClick={()=>jigsawCommand({action:snapshot.jigsaw?.screen==='library'?'jigsaw.exit':'jigsaw.back'})}>
           {snapshot.jigsaw.screen==='library'?'← Chill Lounge':'← Puzzle-Studio'}</button>
       : snapshot?.memory
       ? <button className="chill-exit" disabled={!!memoryPending}
           onClick={()=>memoryCommand({action:snapshot.memory?.screen==='library'?'memory.exit':'memory.back'})}>
           {snapshot.memory.screen==='library'?'← Chill Lounge'
             :(language==='de'?'← Memory-Auswahl':'← Memory cabinet')}</button>
       : snapshot?.wordSearch
       ? <button className="chill-exit" disabled={!!wordSearchPending}
           onClick={()=>wordSearchCommand({action:snapshot.wordSearch?.screen==='game'?'word-search.back':'word-search.exit'})}>
           {snapshot.wordSearch.screen==='game'
             ? (language==='de'?'← Wortsuche-Auswahl':'← Word Search library')
             : '← Chill Lounge'}</button>
       : snapshot?.solitaire
       ? <button className="chill-exit" disabled={!!solitairePending}
           onClick={()=>solitaireCommand({action:snapshot.solitaire?.screen==='game'?'solitaire.back':'solitaire.exit'})}>
           {snapshot.solitaire.screen==='game'
             ? (language==='de'?'← Solitaire-Auswahl':'← Solitaire cabinet')
             : '← Chill Lounge'}</button>
       : snapshot?.sudoku
       ? <button className="chill-exit" disabled={!!pending}
           onClick={()=>command({action:snapshot.sudoku?.variant?'sudoku.variant.back':
             snapshot.sudoku?.screen==='game'?'sudoku.back':'sudoku.exit'})}>
           {snapshot.sudoku.variant
             ? (language==='de'?'← Varianten-Auswahl':'← Variant selection')
             : snapshot.sudoku.screen==='game'
             ? (language==='de'?'← Sudoku-Auswahl':'← Sudoku library')
             : (language==='de'?'← Chill Lounge':'← Chill Lounge')}</button>
       : <button className="chill-exit" onClick={()=>sendLoungeCommand('lounge.close')}>{t.back}</button>}
   </header>
   <div className="chill-content">
    {snapshot?.minesweeper
      ? <MinesweeperScreen state={snapshot.minesweeper} language={language} pending={!!sweepPending}
          error={sweepError} command={sweepCommand} animations={snapshot.animationsEnabled}
          soundEnabled={snapshot.soundEnabled}/>
      : snapshot?.towerDefense
      ? <TowerDefenseScreen state={snapshot.towerDefense} language={language} pending={!!towerDefensePending}
          error={towerDefenseError} command={towerDefenseCommand} animations={snapshot.animationsEnabled} soundEnabled={snapshot.soundEnabled}/>
      : snapshot?.adventure
      ? <AdventureScreen state={snapshot.boardOnly&&snapshot.adventure.run?.board?{...snapshot.adventure,screen:'board'}:snapshot.adventure} language={language} pending={!!adventurePending}
          error={adventureError} command={adventureCommand} animations={snapshot.animationsEnabled}/>
      : snapshot?.jigsaw
      ? <JigsawScreen state={snapshot.jigsaw} language={language} pending={!!jigsawPending} soundEnabled={snapshot.soundEnabled}
          error={jigsawError} command={jigsawCommand} animations={snapshot.animationsEnabled}/>
      : snapshot?.memory
      ? <MemoryScreen state={snapshot.memory} language={language} pending={!!memoryPending}
          error={memoryError} command={memoryCommand} animations={snapshot.animationsEnabled}/>
      : snapshot?.wordSearch
      ? <WordSearchScreen state={snapshot.wordSearch} language={language} pending={!!wordSearchPending}
          error={wordSearchError} command={wordSearchCommand} animations={snapshot.animationsEnabled}/>
      : snapshot?.solitaire
      ? <SolitaireScreen state={snapshot.solitaire} language={language} pending={!!solitairePending}
          error={solitaireError} command={solitaireCommand}/>
      : snapshot?.sudoku
      ? <SudokuScreen key="sudoku" state={snapshot.sudoku} language={language}
          pending={!!pending} error={sudokuError} command={command}/>
      : <>
     <section className="chill-hero" aria-labelledby="chill-title">
       <div className="chill-hero-copy">
         <p className="chill-kicker">{t.kicker}</p>
         <h1 id="chill-title">{t.title}</h1>
         <p className="chill-intro">{t.intro}</p>
       </div>
       <div className="chill-lantern" aria-hidden="true"><span>✦</span></div>
     </section>
     <div className="chill-filter" role="group" aria-label={language==='de'?'Aktivitäten filtern':'Filter activities'}>
       {groups.map(group=><button key={group.id} className={filter===group.id?'selected':''}
         aria-pressed={filter===group.id} onClick={()=>setFilter(group.id)}>{group.name}</button>)}
     </div>
     <section className="chill-grid" data-filter={filter} aria-label={t.all}>
       {filtered.map((game,index)=><button key={game.id} className={'chill-card chill-'+game.id}
         onClick={()=>open(game.id)} disabled={!snapshot}
         style={{animationDelay: String(index*55)+'ms'}}>
         <span className="chill-card-art" aria-hidden="true">
           <span className="chill-decoration">{game.decoration}</span>
           <LoungePreview id={game.id}/>
         </span>
         <span className="chill-card-body">
           <span className="chill-card-category">{groups.find(g=>g.id===game.category)?.name}</span>
           <strong>{game.title[language]}</strong>
           <em>{game.subtitle[language]}</em>
           <span className="chill-description">{game.description[language]}</span>
           <span className="chill-card-bottom">
             <span>{statusMap.get(game.id) || t.continued}</span>
             <b>{t.play} <span aria-hidden="true">→</span></b>
           </span>
         </span>
       </button>)}
     </section>
     <footer className="chill-footer"><strong>{t.foot}</strong><span>{t.note}</span></footer>
     </>}
   </div>
   {!snapshot && <div className="chill-loading" role="status">{t.loading}</div>}
 </main>;
}
createRoot(document.getElementById('root')!).render(<App/>);
