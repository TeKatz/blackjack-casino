import {useEffect,useMemo,useRef,useState} from 'react';
import type {PointerEvent as ReactPointerEvent} from 'react';
import type {TaleBoard,TaleBoardNode,TaleClass,TaleEncounter,TaleIntent,TaleRun,TaleState} from './adventureBridge';
import './adventure.css';

type Lang='en'|'de';
type Props={state:TaleState;language:Lang;pending:boolean;error:string;animations:boolean;
 command:(intent:TaleIntent)=>void};
const words={
 en:{library:'TALES FROM THE BACKROOM',libraryIntro:'Behind every door, a different story.',
  subtitle:'Stories with consequences. Your decisions, characters and investigations survive between visits.',
  completed:'COMPLETED',started:'STARTED',acts:'acts',scenes:'scenes',
  unplayed:'Not yet started',active:'In progress',finished:'Completed',
  select:'SELECT STORY',resume:'CONTINUE',newRun:'BEGIN NEW RUN',replace:'START NEW STORY',
  discard:'This will replace the unfinished character and save for this adventure. Continue?',
  class:'CHOOSE AN ARCHETYPE',health:'HP',armor:'ARMOR',level:'LEVEL',experience:'XP',
  special:'SPECIAL',profile:'PLAYER NAME',back:'← ADVENTURE LIBRARY',club:'← CHILL LOUNGE',
  go:'ENTER STORY',story:'STORY',what:'WHAT DO YOU DO?',roll:'ROLL THE D20',
  waiting:'FATE IS WAITING FOR YOUR ROLL',mod:'MODIFIER',adv:'ADVANTAGE',disadv:'DISADVANTAGE',
  stats:'CHARACTER SHEET',abilities:'ABILITIES',skills:'SKILLS',conditions:'CONDITIONS',
  pack:'PACK & COINS',coins:'COINS',empty:'No carried items.',use:'USE ITEM',
  board:'CASE BOARD',openBoard:'OPEN CASE BOARD',backStory:'← RETURN TO STORY',
  facts:'KNOWN FACTS',quotes:'NOTES & QUOTES',connections:'CONNECTIONS',
  zoomIn:'ZOOM +',zoomOut:'ZOOM −',reset:'RESET VIEW',node:'CLUE DETAILS',
  finish:'ADVENTURE COMPLETE',end:'ENDING',another:'REPLAY THIS ADVENTURE',
  return:'BACK TO ADVENTURES',checks:'CHECKS',choices:'CHOICES',encounters:'ENCOUNTERS',
  hand:'YOUR HAND',dealer:'DEALER',hit:'HIT',stand:'STAND',total:'TOTAL',
  rollDice:'ROLL DICE',bank:'BANK BEST HAND',hold:'HOLD',result:'RETURN TO STORY',
  played:'ROLLS',opponent:'OPPONENT',yourScore:'YOUR SCORE',opponentScore:'OPPONENT SCORE',
  waitingOpponent:'Waiting for opponent',scene:'SCENE',sound:'Audio and visuals follow your Lounge settings.'},
 de:{library:'TALES FROM THE BACKROOM',libraryIntro:'Hinter jeder Tür wartet eine andere Geschichte.',
  subtitle:'Geschichten mit Folgen. Entscheidungen, Figuren und Ermittlungen bleiben gespeichert.',
  completed:'ABGESCHLOSSEN',started:'GESTARTET',acts:'Akte',scenes:'Szenen',
  unplayed:'Noch nicht gespielt',active:'Im Gange',finished:'Abgeschlossen',
  select:'GESCHICHTE WÄHLEN',resume:'FORTSETZEN',newRun:'NEU BEGINNEN',replace:'NEUE GESCHICHTE',
  discard:'Dadurch wird die unfertige Figur und ihr Spielstand ersetzt. Fortfahren?',
  class:'KLASSE WÄHLEN',health:'LP',armor:'RÜSTUNG',level:'STUFE',experience:'EP',
  special:'SPEZIAL',profile:'SPIELERNAME',back:'← ABENTEUER-BIBLIOTHEK',club:'← CHILL LOUNGE',
  go:'GESCHICHTE BETRETEN',story:'GESCHICHTE',what:'WAS TUST DU?',roll:'W20 WÜRFELN',
  waiting:'DAS SCHICKSAL WARTET AUF DEINEN WURF',mod:'MODIFIKATOR',adv:'VORTEIL',disadv:'NACHTEIL',
  stats:'CHARAKTERBOGEN',abilities:'ATTRIBUTE',skills:'FERTIGKEITEN',conditions:'ZUSTÄNDE',
  pack:'INVENTAR & MÜNZEN',coins:'MÜNZEN',empty:'Noch keine Gegenstände.',use:'GEGENSTAND NUTZEN',
  board:'ERMITTLUNGSBRETT',openBoard:'ERMITTLUNGSBRETT ÖFFNEN',backStory:'← ZURÜCK ZUR GESCHICHTE',
  facts:'BEKANNTE FAKTEN',quotes:'NOTIZEN & ZITATE',connections:'VERBINDUNGEN',
  zoomIn:'ZOOM +',zoomOut:'ZOOM −',reset:'ANSICHT ZURÜCKSETZEN',node:'HINWEISDETAILS',
  finish:'ABENTEUER ABGESCHLOSSEN',end:'ENDE',another:'ERNEUT SPIELEN',
  return:'ZURÜCK ZU ABENTEUERN',checks:'PROBEN',choices:'ENTSCHEIDUNGEN',encounters:'BEGEGNUNGEN',
  hand:'DEINE HAND',dealer:'GEBER',hit:'KARTE',stand:'HALTEN',total:'SUMME',
  rollDice:'WÜRFELN',bank:'BESTES BLATT SICHERN',hold:'HALTEN',result:'ZURÜCK ZUR GESCHICHTE',
  played:'WÜRFE',opponent:'GEGNER',yourScore:'DEINE PUNKTE',opponentScore:'GEGNERPUNKTE',
  waitingOpponent:'Gegner wartet',scene:'SZENE',sound:'Audio und Effekte folgen den Lounge-Einstellungen.'},
};
type Copy=typeof words.en;
const signed=(n:number)=>n>=0?'+'+n:String(n);
const outcomeLabel=(outcome:string,language:Lang)=>{
 const labels:Record<string,[string,string]>={CriticalSuccess:['Critical success','Kritischer Erfolg'],
 CriticalFailure:['Critical failure','Kritischer Fehlschlag'],
 Success:['Success','Erfolg'],Failure:['Failure','Fehlschlag'],
 Win:['Victory','Sieg'],Loss:['Defeat','Niederlage'],Draw:['Draw','Unentschieden']};
 return labels[outcome]?.[language==='de'?1:0]??outcome;
};
function ChoiceButtons({run,copy,pending,send}:{
 run:TaleRun;copy:Copy;pending:boolean;send:(intent:TaleIntent)=>void}){
 return <div className="backroom-choices" aria-label={copy.what}>
  <h3>{copy.what}</h3>
  {run.choices.map((choice,i)=><button key={choice.id} disabled={pending}
   className={'backroom-choice'+(choice.encounter?' encounter':'')}
   onClick={()=>send({action:'adventure.choice',choiceId:choice.id})}>
   <span className="backroom-choice-index">{String(i+1).padStart(2,'0')}</span>
   <span className="backroom-choice-copy"><strong>{choice.text}</strong>
   {choice.check&&<small>{choice.check.ability} · DC {choice.check.difficulty}</small>}
   {choice.encounter&&<small>{choice.encounter==='Blackjack'?'BLACKJACK':'FIVE OF A KIND'} · ENCOUNTER</small>}</span>
  </button>)}
 </div>;
}
function CharacterSheet({run,copy,pending,send}:{
 run:TaleRun;copy:Copy;pending:boolean;send:(intent:TaleIntent)=>void}){
 const character=run.character;
 return <aside className="backroom-sheet">
  <small className="backroom-eyebrow">{copy.stats}</small>
  <h2>{character.name}</h2><p>{character.classId} · {copy.level} {character.level}</p>
  <div className="backroom-vitals">
   <div><span>{copy.health}</span><strong>{character.currentHitPoints}/{character.maxHitPoints}</strong></div>
   <div><span>{copy.armor}</span><strong>{character.armor}</strong></div>
   <div><span>{copy.experience}</span><strong>{character.experience}</strong></div>
  </div>
  <h3>{copy.abilities}</h3>
  <div className="backroom-abilities">{character.abilities.map(a=><div key={a.id}
   className={run.pending?.ability.toLowerCase()===a.id.toLowerCase()?'highlight':''}>
   <small>{a.name}</small><strong>{a.score}</strong><span>{signed(a.modifier)}</span>
  </div>)}</div>
  <h3>{copy.skills}</h3>
  <div className="backroom-skills">{character.skills.map(s=><div key={s.id}>
   <span>{s.name}</span><b>{signed(s.bonus)}</b></div>)}</div>
  {run.conditions.length>0&&<><h3>{copy.conditions}</h3>
   <div className="backroom-conditions">{run.conditions.map(c=><span key={c.conditionId}
     title={c.remaining+' scenes'}>{c.name} · {c.remaining}</span>)}</div></>}
  <div className={'backroom-d20 '+(run.pending?'active':'')}>
   <strong>D20</strong>
   <span>{run.pending?copy.waiting:run.lastRoll?run.lastRoll.chosenRoll:'20'}</span>
   {run.pending?<><small>{run.pending.ability} · DC {run.pending.difficulty} · {copy.mod} {signed(run.pending.modifier)}
    {run.pending.mode==='Advantage'?' · '+copy.adv:run.pending.mode==='Disadvantage'?' · '+copy.disadv:''}</small>
    <button className="backroom-primary" disabled={pending} onClick={()=>send({action:'adventure.roll'})}>{copy.roll}</button>
   </>:run.lastRoll&&<small>{outcomeLabel(run.lastRoll.outcome,'en')} · {run.lastRoll.total}/{run.lastRoll.difficulty}</small>}
  </div>
 </aside>;
}
function Inventory({run,copy,pending,send}:{
 run:TaleRun;copy:Copy;pending:boolean;send:(intent:TaleIntent)=>void}){
 return <section className="backroom-inventory">
  <h3>{copy.pack}</h3><div className="backroom-items">
   <div className="backroom-item coins"><strong>◆ {run.character.coins}</strong><small>{copy.coins}</small></div>
   {run.inventory.length===0&&<p>{copy.empty}</p>}
   {run.inventory.map(item=><div key={item.itemId} className="backroom-item" title={item.description}>
     <strong>{item.name}</strong><small>{item.kind} · ×{item.quantity}</small>
     {item.useChoices.map(choice=><button key={choice.id} disabled={pending}
       onClick={()=>send({action:'adventure.choice',choiceId:choice.id})}>
       {copy.use}: {choice.text}</button>)}
    </div>)}</div>
  {run.feedback.length>0&&<div className="backroom-feed" aria-live="polite">
   {run.feedback.slice(-3).map((event,i)=><p key={i}><strong>{event.title}</strong> {event.description}</p>)}
  </div>}
 </section>;
}
function Story({run,copy,pending,send}:{
 run:TaleRun;copy:Copy;pending:boolean;send:(intent:TaleIntent)=>void}){
 return <div className="backroom-story-grid">
  <div className="backroom-story-column">
   <section className="backroom-story-panel">
    <div className="backroom-scene-top">
     <div><small className="backroom-eyebrow">{run.scene.act}</small>
      <h1>{run.scene.title}</h1>{run.scene.location&&<span>{run.scene.location}</span>}</div>
     <div className="backroom-top-actions">
      {run.board&&<button disabled={pending} onClick={()=>send({action:'adventure.board.open'})}>{copy.openBoard}</button>}
      <button disabled={pending} onClick={()=>send({action:'adventure.back'})}>{copy.back}</button>
     </div></div>
    <div className="backroom-story-prose">
     {run.scene.speaker&&<small className="backroom-speaker">{run.scene.speaker}</small>}
     {run.scene.body.split(/\n\s*\n/).filter(Boolean).map((paragraph,i)=><p key={i}>{paragraph}</p>)}
     {run.lastRoll&&<div className={'backroom-check-result '+run.lastRoll.outcome.toLowerCase()}>
      <strong>{run.lastRoll.chosenRoll} · {outcomeLabel(run.lastRoll.outcome,'en')}</strong>
      <span>D20 {run.lastRoll.chosenRoll} + {signed(run.lastRoll.abilityModifier+
         run.lastRoll.skillBonus+run.lastRoll.additionalModifier)} = {run.lastRoll.total} · DC {run.lastRoll.difficulty}</span>
     </div>}
     {run.pending?<div className="backroom-awaiting">{copy.waiting}</div>
      :<ChoiceButtons run={run} copy={copy} pending={pending} send={send}/>}
    </div>
   </section>
   <Inventory run={run} copy={copy} pending={pending} send={send}/>
  </div>
  <CharacterSheet run={run} copy={copy} pending={pending} send={send}/>
 </div>;
}
function CaseBoard({board,copy,pending,send}:{
 board:TaleBoard;copy:Copy;pending:boolean;send:(intent:TaleIntent)=>void}){
 const [selected,setSelected]=useState<string|null>(null);
 const [visibleCategories,setVisibleCategories]=useState<Set<string>>(new Set(['Person','Location','Evidence','Lead','Theory']));
 const [local,setLocal]=useState<{id:string;x:number;y:number}|null>(null);
 const drag=useRef<{id:string;startX:number;startY:number;x:number;y:number}|null>(null);
 const [camera,setCamera]=useState({zoom:board.zoom,panX:board.panX,panY:board.panY});
 useEffect(()=>setCamera({zoom:board.zoom,panX:board.panX,panY:board.panY}),[board.zoom,board.panX,board.panY]);
 const nodes=board.nodes.filter(n=>visibleCategories.has(n.category));
 const chosen=nodes.find(n=>n.id===selected);
 const effective=(n:TaleBoardNode)=>local?.id===n.id?local:n;
 const commitCamera=(next:typeof camera)=>{setCamera(next);send({action:'adventure.board.view',...next});};
 const start=(event:ReactPointerEvent<HTMLButtonElement>,n:TaleBoardNode)=>{
  if(event.button!==0)return;
  event.currentTarget.setPointerCapture(event.pointerId);
  drag.current={id:n.id,startX:event.clientX,startY:event.clientY,x:n.x,y:n.y};
  setSelected(n.id);
 };
 const movement=(event:ReactPointerEvent<HTMLButtonElement>)=>{
  const d=drag.current;if(!d)return;
  const factor=camera.zoom;
  setLocal({id:d.id,x:Math.max(0,Math.min(5000,d.x+(event.clientX-d.startX)/factor)),
   y:Math.max(0,Math.min(5000,d.y+(event.clientY-d.startY)/factor))});
 };
 const end=()=>{
  if(drag.current&&local&&local.id===drag.current.id)
   send({action:'adventure.board.move',nodeId:local.id,x:Math.round(local.x),y:Math.round(local.y)});
  drag.current=null;setLocal(null);
 };
 const categories=['Person','Location','Evidence','Lead','Theory'];
 return <div className="backroom-board-page">
  <div className="backroom-board-top"><div><small className="backroom-eyebrow">{copy.board}</small>
    <h1>{board.title}</h1></div>
    <div className="backroom-top-actions">
     <button disabled={pending} onClick={()=>send({action:'adventure.board.close'})}>{copy.backStory}</button>
     <button onClick={()=>commitCamera({...camera,zoom:Math.min(4,camera.zoom*1.25)})}>{copy.zoomIn}</button>
     <button onClick={()=>commitCamera({...camera,zoom:Math.max(.2,camera.zoom*.8)})}>{copy.zoomOut}</button>
     <button onClick={()=>commitCamera({zoom:1,panX:0,panY:0})}>{copy.reset}</button>
    </div></div>
  <div className="backroom-board-filters">{categories.map(c=><label key={c}><input type="checkbox"
   checked={visibleCategories.has(c)} onChange={e=>setVisibleCategories(previous=>{
    const next=new Set(previous);if(e.target.checked)next.add(c);else next.delete(c);return next;
   })}/>{c}</label>)}</div>
  <div className="backroom-board-grid">
   <div className="backroom-board-viewport">
    <div className="backroom-board-world" style={{
     transform:'translate('+camera.panX+'px,'+camera.panY+'px) scale('+camera.zoom+')'}}>
     <svg className="backroom-board-lines" width="5000" height="5000" viewBox="0 0 5000 5000"
      aria-hidden="true">{board.connections.filter(c=>nodes.some(n=>n.id===c.from)&&nodes.some(n=>n.id===c.to))
       .map(c=>{const from=nodes.find(n=>n.id===c.from)!,to=nodes.find(n=>n.id===c.to)!;
        const a=effective(from),b=effective(to);
        return <line key={c.id} x1={a.x+130} y1={a.y+60} x2={b.x+130} y2={b.y+60}
         className={c.status.toLowerCase()}/>;})}</svg>
     {nodes.map(n=>{const p=effective(n);return <button key={n.id}
      className={'backroom-board-node '+n.category.toLowerCase()+(selected===n.id?' selected':'')}
      style={{left:p.x,top:p.y}} title={n.description}
      onPointerDown={e=>start(e,n)} onPointerMove={movement} onPointerUp={end}
      onPointerCancel={()=>{drag.current=null;setLocal(null);}}>
      <small>{n.category.toUpperCase()} · {n.reliability}</small><strong>{n.title}</strong>
      <span>{n.subtitle||n.lead}</span></button>;})}</div>
   </div>
   <aside className="backroom-board-details"><h3>{copy.node}</h3>
    {chosen?<><h2>{chosen.title}</h2><p>{chosen.description}</p>
      <p>{chosen.category} · {chosen.reliability}</p>
      {chosen.facts.length>0&&<><h3>{copy.facts}</h3>
       {chosen.facts.map((f,i)=><p key={i}>{f}</p>)}</>}
      {chosen.quotes.length>0&&<><h3>{copy.quotes}</h3>
       {chosen.quotes.map((f,i)=><blockquote key={i}>{f}</blockquote>)}</>}</>
      :<p>{nodes.length} clues · {board.connections.length} {copy.connections}</p>}
   </aside>
  </div>
 </div>;
}
function BlackjackCard({card}:{card:{hidden:boolean;rank:string|null;suit:string|null}}){
 const suit=card.suit==='Spades'?'♠':card.suit==='Clubs'?'♣':card.suit==='Hearts'?'♥':'♦';
 const symbol=card.rank==='Ace'?'A':card.rank==='King'?'K':card.rank==='Queen'?'Q':
  card.rank==='Jack'?'J':card.rank==='Ten'?'10':card.rank?.replace(/^(Two|Three|Four|Five|Six|Seven|Eight|Nine)$/,
   x=>String(['Two','Three','Four','Five','Six','Seven','Eight','Nine'].indexOf(x)+2))??'';
 return <div className={'backroom-card '+(card.hidden?'down':(card.suit==='Hearts'||card.suit==='Diamonds'?'red':''))}
  aria-label={card.hidden?'Hidden card':symbol+' of '+card.suit}>{card.hidden?'✦':<><strong>{symbol}</strong><span>{suit}</span></>}</div>;
}
function Encounter({encounter,copy,pending,send}:{
 encounter:TaleEncounter;copy:Copy;pending:boolean;send:(intent:TaleIntent)=>void}){
 const bj=encounter.blackjack,dice=encounter.dice;
 return <section className="backroom-encounter">
  <small className="backroom-eyebrow">THE MIDNIGHT CLUB · {encounter.type==='Blackjack'?'BLACKJACK':'FIVE OF A KIND'}</small>
  <h1>{encounter.title}</h1><h2>{encounter.opponentName}</h2>
  <p>{encounter.intro}</p><div className="backroom-stakes">{encounter.stakes}</div>
  {bj&&<div className="backroom-card-table">
   <h3>{copy.dealer} · {bj.dealerTotal===null?'?':bj.dealerTotal}</h3>
   <div className="backroom-cards">{bj.dealer.map((card,i)=><BlackjackCard key={i} card={card}/>)}</div>
   <h3>{copy.hand} · {bj.playerTotal}</h3>
   <div className="backroom-cards">{bj.player.map((card,i)=><BlackjackCard key={i} card={card}/>)}</div>
   </div>}
  {dice&&<div className="backroom-dice-table">
   <h3>{copy.hand} · {copy.played} {dice.rollsUsed}/3</h3>
   <div className="backroom-dice-row">{dice.player.map((value,i)=><button key={i}
    disabled={pending||!dice.canBank&&dice.rollsUsed===3||encounter.outcome!==null||dice.rollsUsed===0}
    aria-pressed={dice.held[i]} className={dice.held[i]?'held':''}
    onClick={()=>send({action:'adventure.encounter.hold',index:i})}>
    {value||'◇'}<small>{dice.held[i]?copy.hold:''}</small></button>)}</div>
   <p>{dice.bestCategory&&dice.bestPoints!==null?dice.bestCategory+' · '+dice.bestPoints:''}</p>
   <h3>{copy.opponent}</h3>
   <div className="backroom-dice-row opponent">
    {dice.opponent.length?dice.opponent.map((d,i)=><span key={i}>{d}</span>)
    :<p>{copy.waitingOpponent}</p>}</div>
   {dice.playerScore!==null&&<p>{copy.yourScore}: {dice.playerScore} · {copy.opponentScore}: {dice.opponentScore}</p>}
   </div>}
  {encounter.outcome&&<div className="backroom-encounter-outcome">
   <strong>{outcomeLabel(encounter.outcome,'en')}</strong></div>}
  <div className="backroom-actions">
   {bj&&encounter.outcome===null&&<>
    <button disabled={pending||!bj.canHit} onClick={()=>send({action:'adventure.encounter.hit'})}>{copy.hit}</button>
    <button className="backroom-primary" disabled={pending||!bj.canStand}
     onClick={()=>send({action:'adventure.encounter.stand'})}>{copy.stand}</button></>}
   {dice&&encounter.outcome===null&&<>
    <button disabled={pending||!dice.canRoll} onClick={()=>send({action:'adventure.encounter.roll'})}>{copy.rollDice}</button>
    <button className="backroom-primary" disabled={pending||!dice.canBank}
     onClick={()=>send({action:'adventure.encounter.bank'})}>{copy.bank}</button></>}
   {encounter.outcome&&<button className="backroom-primary" disabled={pending}
    onClick={()=>send({action:'adventure.encounter.finish'})}>{copy.result}</button>}
  </div>
 </section>;
}
function Completion({run,copy,meta,pending,send}:{
 run:TaleRun;copy:Copy;meta:string;pending:boolean;send:(intent:TaleIntent)=>void}){
 return <section className="backroom-completion">
  <small className="backroom-eyebrow">THE MIDNIGHT CLUB · TALES FROM THE BACKROOM</small>
  <h1>{copy.finish}</h1>
  <h2>{run.scene.title}</h2>
  <p>{copy.end}: {run.endingId}</p>
  <p className="backroom-completion-body">{run.scene.body}</p>
  <div className="backroom-result-stats">
   <span>{copy.choices}<b>{run.statistics.choicesMade}</b></span>
   <span>{copy.checks}<b>{run.statistics.checksSucceeded}/{run.statistics.checksFailed}</b></span>
   <span>D20<b>{run.rollsMade}</b></span>
   <span>{copy.encounters}<b>{run.statistics.encountersWon}/{run.statistics.encountersLost}</b></span>
  </div>
  {meta&&<p className="backroom-reward">{meta}</p>}
  <div className="backroom-actions">
   <button className="backroom-primary" disabled={pending}
    onClick={()=>send({action:'adventure.restart',adventureId:run.adventureId})}>{copy.another}</button>
   <button disabled={pending} onClick={()=>send({action:'adventure.back'})}>{copy.return}</button>
  </div></section>;
}
export function AdventureScreen({state,language,pending,error,command}:Props){
 const t=words[language], [classId,setClassId]=useState('rogue');
 const selected=state.adventures.find(tale=>tale.id===state.selectedAdventureId);
 const selectedClass=state.classes.find(c=>c.id===classId)??state.classes[0];
 useEffect(()=>{if(!state.classes.some(c=>c.id===classId))setClassId(state.classes[0]?.id??'rogue');},
  [state.selectedAdventureId,state.classes,classId]);
 const send=(intent:TaleIntent)=>{if(!pending)command(intent);};
 const start=()=>{
  const replacing=selected?.status==='active';
  if(replacing&&!window.confirm(t.discard))return;
  send({action:'adventure.start',adventureId:state.selectedAdventureId,
   classId:selectedClass.id,...(replacing?{confirmReplace:true as const}:{})});
 };
 return <div className="backroom-shell" data-testid={'modern-adventure-'+state.screen}>
  {state.screen==='library'?<>
   <header className="backroom-library-header">
    <div><small className="backroom-eyebrow">THE MIDNIGHT CLUB · CHILL LOUNGE</small>
     <h1>{t.library}</h1><h2>{t.libraryIntro}</h2><p>{t.subtitle}</p></div>
    <div className="backroom-library-stats">
     <span>{t.started}<b>{state.started}</b></span><span>{t.completed}<b>{state.completed}</b></span>
    </div></header>
   <div className="backroom-library-layout">
    <section><h3>{t.library}</h3>
     <div className="backroom-tale-grid">{state.adventures.map(tale=><article
      className={tale.id===state.selectedAdventureId?'selected':''} key={tale.id}>
      <span className="backroom-tale-status">{t[tale.status==='active'?'active':tale.status==='completed'?'finished':'unplayed']}</span>
      <h2>{tale.title}</h2><p>{tale.subtitle}</p>
      <small>{tale.acts} {t.acts} · {tale.scenes} {t.scenes}</small>
      <div className="backroom-tale-actions">
       <button disabled={pending||tale.id===state.selectedAdventureId}
        onClick={()=>send({action:'adventure.select',adventureId:tale.id})}>{t.select}</button>
       {tale.status!=='unplayed'&&<button className="backroom-primary" disabled={pending}
        onClick={()=>send({action:'adventure.resume',adventureId:tale.id})}>{t.resume}</button>}
      </div></article>)}</div>
    </section>
    <aside className="backroom-creator"><small className="backroom-eyebrow">{t.newRun}</small>
     <h2>{selected?.title}</h2>
     <label>{t.class}<select value={selectedClass?.id??''} onChange={e=>setClassId(e.target.value)}>
      {state.classes.map(cls=><option key={cls.id} value={cls.id}>{cls.name}</option>)}</select></label>
     {selectedClass&&<><h3>{selectedClass.name}</h3><p>{selectedClass.description}</p>
      <div className="backroom-class-stats"><span>{t.health} {selectedClass.maxHitPoints}</span>
       <span>{t.armor} {selectedClass.armor}</span></div>
      <div className="backroom-class-abilities">{selectedClass.abilities.map(a=><span key={a.id}>
       {a.name} <b>{a.score}</b></span>)}</div>
      {selectedClass.special&&<p>{t.special} · {selectedClass.special}</p>}</>}
     <p>{t.profile}: {state.defaultPlayerName}</p>
     <button className="backroom-primary" disabled={pending||!selectedClass} onClick={start}>
      {selected?.status==='active'?t.replace:t.newRun}</button>
     <small>{t.sound}</small>
    </aside>
   </div>
  </>:state.run&&state.screen==='story'
   ?<Story run={state.run} copy={t} pending={pending} send={send}/>
   :state.run&&state.screen==='board'&&state.run.board
   ?<CaseBoard board={state.run.board} copy={t} pending={pending} send={send}/>
   :state.run&&state.screen==='encounter'&&state.encounter
   ?<Encounter encounter={state.encounter} copy={t} pending={pending} send={send}/>
   :state.run&&state.screen==='complete'
   ?<Completion run={state.run} copy={t} meta={state.metaNotice} pending={pending} send={send}/>
   :null}
  {error&&<p className="backroom-error" role="alert">{error}</p>}
 </div>;
}
