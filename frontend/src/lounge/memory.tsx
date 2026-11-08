import {useEffect,useState} from 'react';
import type {MemoryDeck,MemoryDifficulty,MemoryIntent,MemoryState,MemorySymbol} from './memoryBridge';
import './memory.css';
type Lang='en'|'de';
type Props={state:MemoryState;language:Lang;pending:boolean;error:string;animations:boolean;
 command:(intent:MemoryIntent)=>void};
const difficultyName:Record<Lang,Record<MemoryDifficulty,string>>={
 en:{Easy:'Easy',Normal:'Normal',Hard:'Hard',Expert:'Expert',Master:'Master'},
 de:{Easy:'Leicht',Normal:'Normal',Hard:'Schwer',Expert:'Experte',Master:'Meister'}};
const deckNames:Record<Lang,Record<string,string>>={
 en:{'casino-icons':'Casino Icons','card-suits':'Card Suits',animals:'Animals',fruits:'Fruits & Treats'},
 de:{'casino-icons':'Casino-Symbole','card-suits':'Spielkarten',animals:'Tiere',fruits:'Früchte & Leckereien'}};
const copy={
 en:{kicker:'MEMORY · THE MIDNIGHT CLUB',title:'A moment to remember.',
 subtitle:'Find the pairs. Take your time. One quiet table at a time.',
 completed:'TABLES CLEARED',pairs:'PAIRS FOUND',perfect:'PERFECT TABLES',best:'BEST STREAK',
 decks:'Choose a card deck',play:'CHOOSE YOUR TABLE',new:'PLAY TABLE',resume:'CONTINUE YOUR TABLE',
 resumeCaption:'A table is waiting exactly where you left it.',bestMoves:'BEST MOVES',
 noBest:'NO BEST YET',moves:'MOVES',misses:'MISSES',accuracy:'ACCURACY',
 streak:'MATCH STREAK',deck:'DECK',time:'TIME',found:'PAIRS',streakHint:'Consecutive matches without a miss.',
 hint:'Flip two cards. Matching pairs remain face up, and other cards turn back over.',
 reward:'COMPLETE FOR',xp:'PLAYER XP',season:'SEASON XP',back:'← MEMORY CABINET',
 success:'BOARD CLEARED',perfectTitle:'PERFECT MEMORY',again:'PLAY AGAIN',
 done:'All pairs found. Your result and rewards have been saved.',
 saved:'Saved locally',waiting:'Cards turn back over…',
 noRush:'No bets, no countdown, no XP penalties for mistakes.',
 mustResume:'Resume or complete your current table before starting another one.',selected:'SELECTED',
 table:'TABLE',stars:'MATCH THE PAIRS',another:'Choose another difficulty or deck for your next table.',
 sound:'Card sounds use your Lounge preferences'},
 de:{kicker:'MEMORY · THE MIDNIGHT CLUB',title:'Ein Moment zum Erinnern.',
 subtitle:'Finde die Paare. Lass dir Zeit. Ein ruhiger Spieltisch nach dem anderen.',
 completed:'ABGESCHLOSSENE TISCHE',pairs:'GEFUNDENE PAARE',perfect:'FEHLERFREIE PARTIEN',best:'BESTE SERIE',
 decks:'Kartendeck auswählen',play:'WÄHLE DEINEN SPIELTISCH',new:'SPIEL STARTEN',resume:'SPIEL FORTSETZEN',
 resumeCaption:'Deine letzte Partie wartet genau dort, wo du sie verlassen hast.',bestMoves:'BESTE ZÜGE',
 noBest:'NOCH KEIN REKORD',moves:'ZÜGE',misses:'FEHLVERSUCHE',accuracy:'GENAUIGKEIT',
 streak:'PAARSERIE',deck:'DECK',time:'ZEIT',found:'PAARE',streakHint:'Aufeinanderfolgende Paare ohne Fehlversuch.',
 hint:'Decke zwei Karten auf. Richtige Paare bleiben sichtbar, falsche drehen sich wieder um.',
 reward:'ABSCHLUSS-BELOHNUNG',xp:'SPIELER-XP',season:'SAISON-XP',back:'← MEMORY-AUSWAHL',
 success:'TISCH ABGESCHLOSSEN',perfectTitle:'PERFEKTES MEMORY',again:'NOCH EINMAL',
 done:'Alle Paare gefunden. Dein Ergebnis und deine Belohnungen wurden gespeichert.',
 saved:'Lokal gespeichert',waiting:'Karten werden wieder verdeckt…',
 noRush:'Keine Einsätze, kein Zeitdruck, keine XP-Strafen für Fehler.',
 mustResume:'Beende oder setze die gespeicherte Partie fort, bevor du neu startest.',selected:'AUSGEWÄHLT',
 table:'SPIELTISCH',stars:'FINDE DIE PAARE',another:'Wähle eine weitere Schwierigkeit oder ein Deck.',
 sound:'Kartensounds entsprechen deinen Lounge-Einstellungen'}};
const casinoMotifs:Record<string,string>={
 chip:'◎',dice:'⚄',roulette:'◉',seven:'7',bell:'♛',vault:'▣',crown:'♕',
 cocktail:'♧',ace:'A',horseshoe:'Ω',star:'✦',diamond:'◆',slot:'777',
 cards:'♠',cash:'＄',key:'⚿',bar:'BAR',vip:'VIP',gold:'▰',token:'◈',
 wheel:'◉',dealer:'D',table:'♣',moon:'☾',club:'✥',spin:'↻',king:'K',
 queen:'Q',jack:'J',twentyone:'21',clover:'☘',midnight:'✧'};
function emblem(deckId:string,glyph:string){
 return deckId==='casino-icons'?(casinoMotifs[glyph]??'◆'):glyph;
}
function Face({deckId,symbol}:{deckId:string;symbol:MemorySymbol}){
 const isPlaying=deckId==='card-suits';
 const isCasino=deckId==='casino-icons';
 const glyph=emblem(deckId,symbol.glyph);
 return <div className={'memo-face '+(isPlaying?'playing':isCasino?'casino':'medallion')}
   style={{'--memo-accent':symbol.accent} as React.CSSProperties}>
  {isPlaying&&<span className="memo-corner">{symbol.glyph}</span>}
  <span className="memo-glyph" aria-hidden="true">{glyph}</span>
  {isPlaying&&<span className="memo-corner inverted">{symbol.glyph}</span>}
  {!isPlaying&&<span className="memo-face-name">{symbol.name}</span>}
 </div>;
}
const time=(n:number)=>{const m=Math.floor(n/60),s=n%60;return String(m).padStart(2,'0')+':'+String(s).padStart(2,'0')};
export function MemoryScreen({state,language,pending,error,animations,command}:Props){
 const t=copy[language],game=state.game;
 const [seconds,setSeconds]=useState(0);
 useEffect(()=>{if(state.screen!=='game')return;
  const timer=window.setInterval(()=>setSeconds(s=>s+1),1000);
  return()=>window.clearInterval(timer);
 },[state.screen,game?.gameId]);
 useEffect(()=>setSeconds(0),[game?.elapsedSeconds,game?.gameId]);
 const selected=state.decks.find(deck=>deck.id===state.preferredDeckId)??state.decks[0];
 if(state.screen==='library'||!game)return <section className="memo-library" data-testid="memory-library">
   <div className="memo-hero"><div className="memo-hero-copy"><span className="memo-kicker">{t.kicker}</span>
    <h1>{t.title}</h1><p>{t.subtitle}</p></div>
    <div className="memo-stat-grid">
     <span><b>{state.stats.completedGames.toLocaleString(language)}</b>{t.completed}</span>
     <span><b>{state.stats.pairsFound.toLocaleString(language)}</b>{t.pairs}</span>
     <span><b>{state.stats.perfectGames}</b>{t.perfect}</span>
     <span><b>×{state.stats.bestStreak}</b>{t.best}</span>
    </div>
   </div>
   {state.resume&&<div className="memo-resume">
     <div><span className="memo-kicker">{t.resume}</span>
      <h2>{difficultyName[language][state.resume.difficulty]} · {deckNames[language][state.resume.deckId]}</h2>
      <p>{state.resume.pairsFound}/{state.resume.pairCount} {t.pairs.toLowerCase()} · {state.resume.moves} {t.moves.toLowerCase()}</p></div>
     <button data-testid="memory-resume" aria-disabled={pending}
       onClick={()=>command({action:'memory.resume'})}>{t.resume} →</button>
    </div>}
   <div className="memo-cabinet">
    <section className="memo-decks"><h2>{t.decks}</h2>
     <div className="memo-deck-grid">
      {state.decks.map(deck=><button className={'memo-deck'+(state.preferredDeckId===deck.id?' active':'')}
       key={deck.id} data-testid={'memory-deck-'+deck.id} disabled={!!state.resume} aria-disabled={pending||!!state.resume}
       aria-pressed={state.preferredDeckId===deck.id} onClick={()=>command({action:'memory.deck',deckId:deck.id})}>
        <span className="memo-deck-label">{deckNames[language][deck.id]}{state.preferredDeckId===deck.id&&<b>✓</b>}</span>
        <span className="memo-preview">
         {deck.preview.map((p,i)=><span className="memo-preview-card" key={i}>
           <Face deckId={deck.id} symbol={p}/></span>)}
        </span>
       </button>)}
     </div>
     <div className="memo-selected">
      <span className="memo-kicker">{t.selected} · {deckNames[language][selected.id]}</span>
      <p>{selected.description}</p><small>{t.sound}</small>
     </div>
    </section>
    <section className="memo-difficulties">
     <h2>{t.play}</h2>
     <div className="memo-tier-grid">
      {state.difficulties.map((d,i)=><article className="memo-tier" key={d.difficulty}>
       <div className="memo-tier-top"><span>{'0'+(i+1)} / 05</span><strong>+{d.rewardXp} XP</strong></div>
       <div className="memo-tier-main"><h3>{difficultyName[language][d.difficulty]}</h3>
        <b>{d.rows} × {d.columns}</b><span>{d.pairs} {t.pairs.toLowerCase()}</span></div>
       <div className="memo-tier-record">{d.bestMoves>0?t.bestMoves+' · '+d.bestMoves:t.noBest}</div>
       <button data-testid={'memory-start-'+d.difficulty} disabled={!!state.resume} aria-disabled={pending||!!state.resume}
        onClick={()=>command({action:'memory.start',difficulty:d.difficulty})}>{t.new}</button>
      </article>)}
     </div>
     <p className="memo-guidance">{state.resume?t.mustResume:t.hint}</p>
    </section>
   </div>
   {error&&<p className="memo-error" role="alert">{error}</p>}
   <footer className="memo-foot">{t.noRush}</footer>
  </section>;
 const closed=state.screen==='complete';
 const percent=game.pairCount?game.pairsFound/game.pairCount*100:0;
 return <section className="memo-game" data-testid={closed?'modern-memory-complete':'modern-memory-game'}
   data-difficulty={game.difficulty} data-deck={game.deckId} data-game-id={game.gameId}>
    <div className="memo-game-header">
     <div><span className="memo-kicker">{t.kicker} · {difficultyName[language][game.difficulty]}</span>
      <h1>{deckNames[language][game.deckId]}</h1><p>{game.rows} × {game.columns} · {game.pairCount} {t.pairs.toLowerCase()}</p></div>
     <div className="memo-game-metrics">
      <span><small>{t.found}</small><b data-testid="memory-pairs">{game.pairsFound}/{game.pairCount}</b></span>
      <span><small>{t.moves}</small><b data-testid="memory-moves">{game.moves}</b></span>
      <span><small>{t.time}</small><b>{time(game.elapsedSeconds+(closed?0:seconds))}</b></span>
     </div>
    </div>
    <div className="memo-play-layout">
     <div className={'memo-board-frame'+(animations?'':' reduced')}>
      <div className="memo-board" role="grid" aria-label={t.stars} data-testid="memory-board"
       style={{gridTemplateColumns:'repeat('+game.columns+',minmax(0,1fr))',maxWidth:String(game.columns*105)+'px'}}>
       {game.cards.map(card=><button type="button" key={card.index} role="gridcell"
         className={'memo-card '+(card.faceUp?'face-up ':'face-down ')+(card.matched?'matched':'')}
         data-testid={'memory-card-'+card.index} data-face-up={card.faceUp} data-matched={card.matched}
         disabled={closed||card.faceUp||game.resolvingMismatch} aria-disabled={pending||closed||card.faceUp||game.resolvingMismatch}
         aria-label={card.faceUp?(card.name??''):(language==='de'?'Verdeckte Karte ':'Hidden card ')+(card.index+1)}
         onClick={()=>command({action:'memory.flip',index:card.index})}>
         {card.faceUp&&card.glyph&&card.name&&card.accent
          ? <Face deckId={game.deckId} symbol={{glyph:card.glyph,name:card.name,accent:card.accent}}/>
          : <span className="memo-card-back"><span>✦</span><b>M</b><i>◆</i></span>}
         {card.matched&&<span className="memo-matched-seal">✓</span>}
        </button>)}
      </div>
      <div className="memo-progress-bar"><span style={{width:percent+'%'}}/></div>
     </div>
     <aside className="memo-sidebar">
      <div className="memo-streak"><span className="memo-kicker">{t.streak}</span>
       <strong data-testid="memory-streak">×{game.currentStreak}</strong>
       <p>{t.streakHint}</p><small>{t.best}: ×{game.bestStreak}</small></div>
      <div className="memo-table-stats">
       <span><small>{t.accuracy}</small><b>{Math.round(game.accuracy*100)}%</b></span>
       <span><small>{t.misses}</small><b data-testid="memory-misses">{game.wrongMatches}</b></span>
       <span><small>{t.found}</small><b>{game.pairsFound}/{game.pairCount}</b></span>
       <span><small>{t.reward}</small><b>+{game.rewardXp} XP</b></span>
      </div>
      <p className="memo-notice" role="status">{error||state.notice}</p>
      {game.resolvingMismatch&&<p className="memo-mismatch">{t.waiting}</p>}
      <p className="memo-saved">{t.saved}</p>
      {!closed&&<button className="memo-back" data-testid="memory-back" aria-disabled={pending}
       onClick={()=>command({action:'memory.back'})}>{t.back}</button>}
     </aside>
    </div>
    {closed&&<div className={'memo-finale'+(game.perfect?' perfect':'')} data-testid="memory-completion">
     <span className="memo-kicker">{game.perfect?t.perfectTitle:t.success}</span>
     <h2>{game.perfect?'✦ '+t.perfectTitle+' ✦':t.success}</h2>
     <p>{t.done}</p>
     <div className="memo-final-stats">
      <span><b>{game.moves}</b>{t.moves}</span><span><b>{Math.round(game.accuracy*100)}%</b>{t.accuracy}</span>
      <span><b>{game.wrongMatches}</b>{t.misses}</span><span><b>×{game.bestStreak}</b>{t.best}</span>
      <span><b>{game.pairCount}</b>{t.pairs}</span><span><b>{time(game.elapsedSeconds)}</b>{t.time}</span>
     </div>
     <p className="memo-award">{state.rewardNotice||'+'+game.rewardXp+' '+t.xp+' · +'+Math.floor(game.rewardXp/2)+' '+t.season}</p>
     <div className="memo-final-actions">
      <button data-testid="memory-result-back" onClick={()=>command({action:'memory.back'})}>{t.back}</button>
      <button data-testid="memory-result-again" onClick={()=>command({action:'memory.again'})}>{t.again}</button>
     </div>
    </div>}
    <p className="memo-foot">{t.noRush}</p>
  </section>;
}
