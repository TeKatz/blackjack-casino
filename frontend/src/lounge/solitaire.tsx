import {useEffect,useMemo,useState} from 'react';
import type {CSSProperties,DragEvent} from 'react';
import {resolveCardSkin} from '../blackjack/cardSkins';
import type {SolitaireCard,SolitaireGame,SolitaireIntent,SolitaireState,SolitaireVariant} from './solitaireBridge';
import {variants} from './solitaireBridge';
import './solitaire.css';
import {PuzzleCompletion} from './PuzzleCompletion';

type Language='de'|'en';
type Props={state:SolitaireState;language:Language;pending:boolean;error:string;command:(intent:SolitaireIntent)=>void};
type Picked={pile:string;index:number};
type Slot={id:string;x:number;y:number;step:number;label?:string};
const foundation=['F-Clubs','F-Diamonds','F-Hearts','F-Spades'];
const suitSymbols:Record<string,string>={Clubs:'♣',Diamonds:'♦',Hearts:'♥',Spades:'♠'};
const rankSymbol=(n:number)=>n===1?'A':n===11?'J':n===12?'Q':n===13?'K':String(n);
const variantName:Record<SolitaireVariant,{en:string;de:string}>={
 Klondike:{en:'Klondike',de:'Klondike'},FreeCell:{en:'FreeCell',de:'FreeCell'},
 Spider:{en:'Spider',de:'Spider'},Pyramid:{en:'Pyramid',de:'Pyramide'},
 Golf:{en:'Golf',de:'Golf'},TriPeaks:{en:'TriPeaks',de:'TriPeaks'},
};
const descriptions:Record<SolitaireVariant,{en:string;de:string}>={
 Klondike:{en:'Classic seven-column solitaire. Build down in alternating colors.',
  de:'Der Klassiker mit sieben Reihen. Absteigend und abwechselnd rot/schwarz sortieren.'},
 FreeCell:{en:'Eight open columns and four free cells. Reproducible numbered deals.',
  de:'Acht offene Reihen und vier freie Felder. Nummerierte, wiederholbare Partien.'},
 Spider:{en:'Build same-suit runs from King to Ace. Choose 1, 2 or 4 suits.',
  de:'Gleichfarbige Folgen von König bis Ass bilden. Mit 1, 2 oder 4 Farben.'},
 Pyramid:{en:'Clear exposed pairs that add to thirteen. Kings clear alone.',
  de:'Freie Kartenpaare mit Summe 13 entfernen. Könige verschwinden allein.'},
 Golf:{en:'Clear seven columns with a card one rank higher or lower than waste.',
  de:'Sieben Reihen mit Karten genau einen Rang über oder unter dem Ablagestapel räumen.'},
 TriPeaks:{en:'Uncover three peaks with cards one rank above or below waste.',
  de:'Drei Gipfel räumen. Jede Karte muss einen Rang höher oder niedriger sein.'},
};
const words={
 en:{eyebrow:'MIDNIGHT SOLITAIRE · THE MIDNIGHT CLUB',title:'Let the cards settle.',
 description:'Six solitaire classics, eight table types, and no wagers or countdowns. Your decisions stay saved.',
 choose:'Choose a table',resume:'RESUME',view:'VIEW SOLVED',new:'NEW DEAL',
 saved:'In progress',solved:'Solved',noSaved:'No saved game',moves:'MOVES',deal:'DEAL',time:'TIME',
 wins:'WINS',started:'GAMES STARTED',streak:'BEST STREAK',achievements:'ACHIEVEMENTS',
 achievementsShow:'VIEW ACHIEVEMENTS',achievementsHide:'HIDE ACHIEVEMENTS',
 spiderSuits:'SUITS',completedRuns:'COMPLETED RUNS',freeCellNumber:'FREECELL DEAL NUMBER',
 numbered:'PLAY THIS NUMBER',back:'← SOLITAIRE CABINET',leave:'← CHILL LOUNGE',
 stock:'STOCK',waste:'WASTE',foundations:'FOUNDATIONS',tableau:'TABLEAU',
 freeCells:'FREE CELLS',draw:'DRAW',drawCard:'DRAW CARD',empty:'EMPTY',
 chosen:'SELECTED',selectHelp:'Select a card and then its destination. Double-click a top card to move it to the foundation.',
 matchHelp:'Select two exposed cards totaling 13. Kings clear with a single click.',
 adjacentHelp:'Click an uncovered card one rank above or below the waste.',
 spiderHelp:'Choose a same-suit run, then a target column. Stock deals ten cards at once.',
 win:'A QUIET VICTORY',winNote:'Your game and progression reward were recorded in your existing casino profile.',
 boardView:'VIEW FINISHED TABLE',finishBack:'BACK TO TABLES',
 playerXp:'PLAYER XP',seasonXp:'SEASON XP',newConfirm:'Replace unfinished deal?',
 confirmText:'A new deal replaces the unfinished save for this table and records an abandonment. Other games stay saved.',
 cancel:'KEEP PLAYING',replace:'START NEW DEAL',waiting:'Saving your move…',
 invalidNumber:'Enter a valid FreeCell deal number from 1 to 2,000,000,000.',
 noRush:'No speed bonuses. No wagers. The rules, saves and rewards all remain in C#.',
 cardBack:'Face-down card',tap:'Click or drag to place selected cards.',
 noMove:'Move rejected by the game rules.',select:'Select a card'},
 de:{eyebrow:'MIDNIGHT SOLITAIRE · THE MIDNIGHT CLUB',title:'Lass die Karten zur Ruhe kommen.',
 description:'Sechs Solitär-Klassiker, acht Spieltische und kein Einsatz oder Zeitdruck. Deine Partien bleiben gespeichert.',
 choose:'Wähle deinen Tisch',resume:'FORTSETZEN',view:'GELÖSTES SPIEL',new:'NEUE PARTIE',
 saved:'Begonnen',solved:'Gelöst',noSaved:'Kein Spielstand',moves:'ZÜGE',deal:'PARTIE',time:'ZEIT',
 wins:'SIEGE',started:'GESTARTETE SPIELE',streak:'BESTE SERIE',achievements:'ERFOLGE',
 achievementsShow:'ERFOLGE ANZEIGEN',achievementsHide:'ERFOLGE SCHLIESSEN',
 spiderSuits:'FARBEN',completedRuns:'FERTIGE FOLGEN',freeCellNumber:'FREECELL-PARTIENUMMER',
 numbered:'NUMMER SPIELEN',back:'← SOLITAIRE-AUSWAHL',leave:'← CHILL LOUNGE',
 stock:'STOCK',waste:'ABLAGE',foundations:'ZIELSTAPEL',tableau:'SPIELFELD',
 freeCells:'FREIE FELDER',draw:'ZIEHEN',drawCard:'KARTE ZIEHEN',empty:'LEER',
 chosen:'AUSGEWÄHLT',selectHelp:'Karte auswählen und Ziel anklicken. Doppelklick verschiebt die oberste Karte auf den Zielstapel.',
 matchHelp:'Zwei freie Karten mit Summe 13 auswählen. Könige verschwinden sofort.',
 adjacentHelp:'Eine freie Karte einen Rang über oder unter der Ablage anklicken.',
 spiderHelp:'Gleichfarbige Kartenfolge auswählen und Zielspalte anklicken. Stock verteilt zehn Karten.',
 win:'STILLER SIEG',winNote:'Spielstand und Fortschrittsbelohnung wurden in deinem Casino-Profil gespeichert.',
 boardView:'SPIELFELD ANSEHEN',finishBack:'ZUR SPIELAUSWAHL',
 playerXp:'SPIELER-XP',seasonXp:'SAISON-XP',newConfirm:'Unfertige Partie ersetzen?',
 confirmText:'Die neue Partie ersetzt den unfertigen Spielstand dieses Tisches und wird als Aufgabe erfasst. Andere Partien bleiben gespeichert.',
 cancel:'WEITERSPIELEN',replace:'NEU STARTEN',waiting:'Spielzug wird gespeichert…',
 invalidNumber:'Gültige FreeCell-Nummer zwischen 1 und 2.000.000.000 eingeben.',
 noRush:'Keine Geschwindigkeitsboni, keine Einsätze. Regeln, Spielstände und Belohnungen bleiben in C#.',
 cardBack:'Verdeckte Karte',tap:'Karten anklicken oder zum Ziel ziehen.',
 noMove:'Dieser Zug ist nach den Spielregeln nicht erlaubt.',select:'Karte auswählen'},
};
const formatTime=(seconds:number)=>{
 const hours=Math.floor(seconds/3600),mins=Math.floor(seconds/60)%60,sec=seconds%60;
 return(hours?String(hours).padStart(2,'0')+':':'')+
  String(mins).padStart(2,'0')+':'+String(sec).padStart(2,'0');
};
function layout(g:SolitaireGame):Slot[]{
 const s:Slot[]=[];
 const add=(id:string,x:number,y:number,step=0,label?:string)=>s.push({id,x,y,step,label});
 if(g.variant==='Klondike'){
  add('Stock',35,20);add('Waste',135,20);
  foundation.forEach((id,i)=>add(id,600+i*105,20,0,suitSymbols[id.slice(2)]));
  for(let i=0;i<7;i++)add('T'+i,45+i*145,178,31);
 }else if(g.variant==='FreeCell'){
  for(let i=0;i<4;i++)add('Cell'+i,35+i*105,20,0,'CELL '+(i+1));
  foundation.forEach((id,i)=>add(id,650+i*105,20,0,suitSymbols[id.slice(2)]));
  for(let i=0;i<8;i++)add('C'+i,25+i*135,178,31);
 }else if(g.variant==='Spider'){
  add('Stock',30,20);
  for(let i=0;i<10;i++)add('T'+i,18+i*108,153,27);
 }else if(g.variant==='Pyramid'){
  for(let row=0;row<7;row++){
   const start=row*(row+1)/2;
   for(let col=0;col<=row;col++)add('P'+(start+col),540+(col-row/2)*92,25+row*67);
  }
  add('Stock',70,505);add('Waste',175,505);
 }else if(g.variant==='Golf'){
  for(let i=0;i<7;i++)add('G'+i,55+i*145,80,43);
  add('Stock',380,445);add('Waste',500,445);
 }else{
  const coords:Array<[number,number]>=[
   [165,20],[495,20],[825,20],
   [110,85],[220,85],[440,85],[550,85],[770,85],[880,85],
   [55,150],[165,150],[275,150],[385,150],[495,150],[605,150],[715,150],[825,150],[935,150],
   [0,215],[110,215],[220,215],[330,215],[440,215],[550,215],[660,215],[770,215],[880,215],[990,215]];
  coords.forEach(([x,y],i)=>add('P'+i,x,y));
  add('Stock',390,440);add('Waste',510,440);
 }
 return s;
}
function CardFace({card,backLabel,mark}:{card:SolitaireCard;backLabel:string;mark:string}){
 const sym=suitSymbols[card.suit]||'♠',isRed=card.suit==='Hearts'||card.suit==='Diamonds';
 return card.faceUp
  ? <span className={'sol-card-face '+(isRed?'red':'black')}>
     <span className="sol-corner"><b>{rankSymbol(card.rank)}</b><span>{sym}</span></span>
     <span className="sol-card-center">{sym}</span>
     <span className="sol-corner bottom"><b>{rankSymbol(card.rank)}</b><span>{sym}</span></span>
    </span>
  : <span className="sol-card-back" aria-label={backLabel}>
      <span className="sol-back-frame"><span>{mark}</span></span>
    </span>;
}
function GameTable({game,skinId,selected,setSelected,pending,command,language}:{
 game:SolitaireGame;skinId:string;selected:Picked|null;setSelected:(v:Picked|null)=>void;
 pending:boolean;command:Props['command'];language:Language;
}){
 const t=words[language],skin=resolveCardSkin(skinId);
 const slots=useMemo(()=>layout(game),[game.variant,game.spiderSuits,game.gameId]);
 const style={
  '--sol-face-a':skin.faceA,'--sol-face-b':skin.faceB,'--sol-border':skin.border,
  '--sol-red':skin.red,'--sol-black':skin.black,'--sol-back-a':skin.backA,
  '--sol-back-b':skin.backB,'--sol-accent':skin.accent,'--sol-mark':skin.mark,
 } as CSSProperties;
 const doMove=(target:string)=>{
  if(pending||!selected||game.won||selected.pile===target)return;
  command({action:'solitaire.move',source:selected.pile,index:selected.index,target});
 };
 function clicked(id:string,index:number,card:SolitaireCard){
  if(pending||game.won)return;
  if(game.variant==='Golf'||game.variant==='TriPeaks'){
    if(card.selectable)command({action:'solitaire.activate',source:id,index});
    return;
  }
  if(game.variant==='Pyramid'){
    if(!card.selectable)return;
    if(card.rank===13){command({action:'solitaire.activate',source:id,index});return;}
    if(selected?.pile===id&&selected.index===index){setSelected(null);return;}
    if(selected&&selected.pile!==id){doMove(id);return;}
    setSelected({pile:id,index});return;
  }
  if(!card.faceUp){command({action:'solitaire.activate',source:id,index});return;}
  if(selected?.pile===id&&selected.index===index){setSelected(null);return;}
  if(selected&&selected.pile!==id){doMove(id);return;}
  if(card.selectable)setSelected({pile:id,index});
 }
 function doubleClicked(id:string,index:number){
  if(!pending&&!game.won&&(game.variant==='Klondike'||game.variant==='FreeCell'))
   command({action:'solitaire.auto',source:id,index});
 }
 function dragStart(e:DragEvent<HTMLButtonElement>,id:string,index:number,card:SolitaireCard){
  if(pending||game.won||!card.selectable){e.preventDefault();return;}
  e.dataTransfer.effectAllowed='move';e.dataTransfer.setData('text/plain',id+':'+index);
  setSelected({pile:id,index});
 }
 function drop(e:DragEvent<HTMLDivElement>,target:string){
  e.preventDefault();
  if(pending||game.won)return;
  const data=e.dataTransfer.getData('text/plain');
  const match=/^([A-Za-z0-9-]+):(\d{1,3})$/.exec(data);
  if(!match||match[1]===target)return;
  command({action:'solitaire.move',source:match[1],index:Number(match[2]),target});
 }
 return <div className="sol-table-scroll" data-testid="solitaire-table">
   <div className={'sol-table sol-variant-'+game.variant.toLowerCase()} style={style}>
    <div className="sol-table-ornament">✦  THE MIDNIGHT CLUB  ✦</div>
    {slots.map((slot,slotIndex)=>{
      const cards=game.piles[slot.id]??[],isStock=slot.id==='Stock',
       isTopOnly=isStock||slot.id==='Waste'||slot.id.startsWith('F-')||slot.id.startsWith('Cell');
      const display=isTopOnly?cards.slice(-1).map((card)=>({card,index:cards.length-1})):
         cards.map((card,index)=>({card,index}));
      let offset=0;
      const naturalEnd=cards.slice(0,-1).reduce((sum,card)=>sum+(card.faceUp?slot.step:15),0);
      const fanScale=naturalEnd>0?Math.min(1,Math.max(0,640-slot.y-108-16)/naturalEnd):1;
      return <div className={'sol-slot '+(selected&&selected.pile!==slot.id?'destination':'')}
         key={slot.id} data-testid={'solitaire-pile-'+slot.id}
         style={{left:slot.x/1120*100+'%',top:slot.y/640*100+'%',zIndex:10+slotIndex}}
         onDragOver={e=>e.preventDefault()} onDrop={e=>drop(e,slot.id)}
         onClick={()=>{if(display.length===0){
          if(isStock&&!pending&&!game.won)command({action:'solitaire.draw'});
          else doMove(slot.id);
         }}}>
       <span className="sol-slot-ghost">{slot.label??(isStock?t.stock:slot.id)}</span>
       {display.map(({card,index},n)=>{
        if(!isTopOnly){
          offset=0;
          for(let i=0;i<index;i++)offset+=(cards[i].faceUp?slot.step:15)*fanScale;
        }
        const isSelected=selected?.pile===slot.id&&selected.index===index;
        const title=card.faceUp?rankSymbol(card.rank)+suitSymbols[card.suit]:t.cardBack;
        return <button key={index} type="button" draggable={card.selectable&&!game.won&&!pending}
          data-testid={'solitaire-card-'+slot.id+'-'+index}
          title={title} aria-label={title+(card.selectable?' · '+t.select:'')}
          aria-pressed={isSelected} disabled={game.won} aria-disabled={pending||game.won}
          className={'sol-playing-card '+(isSelected?'selected ':'')+(!card.faceUp?'is-back ':'')+(!card.selectable?'not-selectable':'')}
          style={{top:offset/108*100+'%',zIndex:index+1}}
          onDragStart={e=>dragStart(e,slot.id,index,card)}
          onClick={e=>{
            e.stopPropagation();
            if(isStock){if(!pending&&!game.won)command({action:'solitaire.draw'});}
            else clicked(slot.id,index,card);
          }}
          onDoubleClick={e=>{e.stopPropagation();doubleClicked(slot.id,index);}}>
          <CardFace card={card} backLabel={t.cardBack} mark={skin.mark}/>
         </button>;
       })}
       {isStock&&cards.length>0&&<span className="sol-stock-count">{cards.length}</span>}
      </div>;
    })}
    {game.variant==='Spider'&&<div className="sol-runs-counter">{t.completedRuns} · {game.completedRuns}/8</div>}
   </div>
  </div>;
}
export function SolitaireScreen({state,language,pending,error,command}:Props){
 const t=words[language];
 const [dealNumber,setDealNumber]=useState(String(state.nextDealNumber));
 const [showAchievements,setShowAchievements]=useState(false);
 const [confirm,setConfirm]=useState<{variant:SolitaireVariant;suits:number;dealNumber?:number}|null>(null);
 const [localError,setLocalError]=useState('');
 const [selected,setSelected]=useState<Picked|null>(null);
 const [clock,setClock]=useState(0);
 const [dismissedCompletion,setDismissedCompletion]=useState<string|null>(null);
 const game=state.game;
 useEffect(()=>setSelected(null),[game?.gameId,game?.moves]);
 useEffect(()=>{
   if(!game||game.won)return;
   const id=window.setInterval(()=>setClock(v=>v+1),1000);
   return()=>window.clearInterval(id);
 },[game?.gameId,game?.won]);
 useEffect(()=>setClock(0),[game?.elapsedSeconds,game?.gameId]);
 const selectedCard=selected?game?.piles[selected.pile]?.[selected.index]:null;
 const suggestions:Record<SolitaireVariant,string>={
  Klondike:t.selectHelp,FreeCell:t.selectHelp,Spider:t.spiderHelp,
  Pyramid:t.matchHelp,Golf:t.adjacentHelp,TriPeaks:t.adjacentHelp,
 };
 function start(variant:SolitaireVariant,suits:number,number?:number){
   if(pending)return;
   const saved=state.options.find(o=>o.variant===variant&&o.spiderSuits===suits);
   if(saved?.canResume&&!saved.won){setConfirm({variant,suits,dealNumber:number});return;}
   command({action:'solitaire.new',variant,spiderSuits:suits,...(number?{dealNumber:number}:{})});
 }
 function numberStart(){
   const n=Number(dealNumber);
   if(!/^\d+$/.test(dealNumber)||!Number.isSafeInteger(n)||n<1||n>2_000_000_000){
     setLocalError(t.invalidNumber);return;
   }
   setLocalError('');start('FreeCell',1,n);
 }
 if(!game)return <section className="sol-cabinet" data-testid="solitaire-library">
   <div className="sol-banner"><span className="sol-eyebrow">{t.eyebrow}</span><h1>{t.title}</h1>
     <p>{t.description}</p>
     <div className="sol-career">
      <span><b>{state.stats.wins.toLocaleString(language)}</b>{t.wins}</span>
      <span><b>{state.stats.gamesStarted.toLocaleString(language)}</b>{t.started}</span>
      <span><b>{state.stats.bestWinStreak.toLocaleString(language)}</b>{t.streak}</span>
      <span><b>{state.stats.achievements.filter(a=>a.complete).length}/{state.stats.achievements.length}</b>{t.achievements}</span>
     </div></div>
   <h2 className="sol-section-title">{t.choose}</h2>
   <div className="sol-options">
    {variants.flatMap(variant=>(variant==='Spider'?[1,2,4]:[1]).map(suits=>{
     const option=state.options.find(o=>o.variant===variant&&o.spiderSuits===suits);
     return <article className="sol-option" key={variant+'-'+suits}>
       <span className="sol-option-label">{variant==='Spider' ? suits+' '+t.spiderSuits : 'THE MIDNIGHT CLUB'}</span>
       <div className="sol-option-symbol" aria-hidden="true">{({Klondike:'♠',FreeCell:'♣',Spider:'♟',Pyramid:'◆',Golf:'♥',TriPeaks:'♜'} as Record<SolitaireVariant,string>)[variant]}</div>
       <h3>{variantName[variant][language]}{variant==='Spider'?' · '+suits:''}</h3>
       <p>{descriptions[variant][language]}</p>
       <small>{option?.canResume? (option.won?t.solved:t.saved)+' · #'+option.dealNumber+' · '+option.moves+' '+t.moves.toLowerCase() : t.noSaved}</small>
       <div className="sol-option-actions">
        <button data-testid={'solitaire-resume-'+variant+'-'+suits}
          disabled={!option?.canResume} aria-disabled={!option?.canResume||pending}
          onClick={()=>command({action:'solitaire.resume',variant,spiderSuits:suits})}>
           {option?.won?t.view:t.resume}</button>
        <button data-testid={'solitaire-new-'+variant+'-'+suits} className="primary"
          aria-disabled={pending} onClick={()=>start(variant,suits)}>{t.new}</button>
       </div>
      </article>;
    }))}
   </div>
   <div className="sol-numbered">
    <div><span className="sol-eyebrow">FREECELL</span><h2>{t.freeCellNumber}</h2>
      <p>{descriptions.FreeCell[language]}</p></div>
    <div className="sol-deal-entry"><input type="text" inputMode="numeric" maxLength={10}
      aria-label={t.freeCellNumber} data-testid="solitaire-deal-number"
      value={dealNumber} onChange={e=>setDealNumber(e.target.value.replace(/\D/g,''))}/>
      <button data-testid="solitaire-numbered" aria-disabled={pending} onClick={numberStart}>{t.numbered}</button></div>
   </div>
   <div className="sol-achievements">
    <button data-testid="solitaire-achievements-toggle" aria-expanded={showAchievements}
      onClick={()=>setShowAchievements(v=>!v)}>
       {showAchievements?t.achievementsHide:t.achievementsShow} · {state.stats.achievements.filter(a=>a.complete).length}/{state.stats.achievements.length}
    </button>
    {showAchievements&&<div className="sol-achievement-grid">
      {state.stats.achievements.map(a=><div className={a.complete?'complete':''} key={a.id}>
       <strong>{a.complete?'✓':'○'} {a.name}</strong><span>{a.description}</span>
      </div>)}</div>}
   </div>
   {(error||localError)&&<p className="sol-alert" role="alert">{error||localError}</p>}
   {confirm&&<div className="sol-dialog" role="dialog" aria-modal="true">
     <div><h2>{t.newConfirm}</h2><p>{t.confirmText}</p><div>
      <button onClick={()=>setConfirm(null)}>{t.cancel}</button>
      <button className="primary" data-testid="solitaire-confirm-new" onClick={()=>{
        const request=confirm;setConfirm(null);
        command({action:'solitaire.new',variant:request.variant,spiderSuits:request.suits,
          ...(request.dealNumber?{dealNumber:request.dealNumber}:{})});
       }}>{t.replace}</button>
     </div></div></div>}
  </section>;
 return <section className="sol-game" data-testid="modern-solitaire-game"
   data-game-id={game.gameId} data-variant={game.variant}>
  <div className="sol-game-header">
   <div><span className="sol-eyebrow">{t.eyebrow}</span>
    <h1>{variantName[game.variant][language]}{game.variant==='Spider'?' · '+game.spiderSuits:''}</h1>
    <p>{suggestions[game.variant]}</p></div>
   <div className="sol-game-stats">
    <span><small>{t.deal}</small><b>#{game.dealNumber}</b></span>
    <span><small>{t.moves}</small><b data-testid="solitaire-moves">{game.moves}</b></span>
    <span><small>{t.time}</small><b>{formatTime(game.elapsedSeconds+(game.won?0:clock))}</b></span>
   </div>
  </div>
  <GameTable game={game} skinId={state.cardSetId} language={language}
    selected={selected} setSelected={setSelected} pending={pending} command={command}/>
  <div className="sol-game-footer">
   <div><p className="sol-selection" data-testid="solitaire-selection">
     {selected&&selectedCard?t.chosen+' · '+rankSymbol(selectedCard.rank)+suitSymbols[selectedCard.suit]
      +' · '+selected.pile+' · '+(game.piles[selected.pile].length-selected.index)+' cards':t.tap}</p>
     <p className="sol-notice" role="status">{error||state.notice}</p>
     {pending&&<small>{t.waiting}</small>}
    </div>
   <button data-testid="solitaire-back" aria-disabled={pending}
     onClick={()=>command({action:'solitaire.back'})}>{t.back}</button>
  </div>
  {game.won&&dismissedCompletion!==game.gameId&&<PuzzleCompletion
    testId="solitaire-win" kind="cards" title={t.win} subtitle={t.winNote}
    metrics={[{label:t.deal,value:'#'+game.dealNumber},{label:t.moves,value:game.moves},
      {label:t.time,value:formatTime(game.elapsedSeconds)}]}
    rewardXp={game.rewardXp} seasonXp={Math.floor(game.rewardXp/2)}
    rewardNotice={state.rewardNotice} xpLabel={t.playerXp} seasonLabel={t.seasonXp}
    viewLabel={t.boardView} backLabel={t.finishBack}
    onView={()=>setDismissedCompletion(game.gameId)}
    onBack={()=>command({action:'solitaire.back'})}/>
  <p className="sol-disclaimer">{t.noRush}</p>
 </section>;
}
