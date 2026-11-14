import {useEffect,useMemo,useRef,useState} from 'react';
import type {PointerEvent as ReactPointerEvent} from 'react';
import type {TaleBoard,TaleBoardNode,TaleClass,TaleEncounter,TaleIntent,TaleRun,TaleState} from './adventureBridge';
import './adventure.css';
import {BackroomItemIcon} from './BackroomItemIcon';
import {BackroomD20} from './BackroomD20';

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
  pack:'PACK & COINS',coins:'COINS',empty:'No carried items.',use:'USE ITEM',eventLog:'EVENT LOG',
  board:'CASE BOARD',openBoard:'OPEN CASE BOARD',backStory:'← RETURN TO STORY',
  facts:'KNOWN FACTS',quotes:'NOTES & QUOTES',connections:'CONNECTIONS',
  zoomIn:'ZOOM +',zoomOut:'ZOOM −',reset:'RESET VIEW',node:'CLUE DETAILS',newClue:'NEW CASE FILE',panTip:'Drag with left mouse · scroll to zoom',
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
  pack:'INVENTAR & MÜNZEN',coins:'MÜNZEN',empty:'Noch keine Gegenstände.',use:'GEGENSTAND NUTZEN',eventLog:'EREIGNISPROTOKOLL',
  board:'ERMITTLUNGSBRETT',openBoard:'ERMITTLUNGSBRETT ÖFFNEN',backStory:'← ZURÜCK ZUR GESCHICHTE',
  facts:'BEKANNTE FAKTEN',quotes:'NOTIZEN & ZITATE',connections:'VERBINDUNGEN',
  zoomIn:'ZOOM +',zoomOut:'ZOOM −',reset:'ANSICHT ZURÜCKSETZEN',node:'HINWEISDETAILS',newClue:'NEUER EINTRAG',panTip:'Linke Maustaste ziehen · Mausrad zum Zoomen',
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
function eventLabel(event:string,de:boolean){
 const map:Record<string,[string,string]>={AddItem:['Item','Gegenstand'],RemoveItem:['Item used','Gegenstand verwendet'],ApplyCondition:['Condition','Zustand'],RemoveCondition:['Recovery','Erholung'],DiscoverCaseNode:['Clue','Hinweis'],RevealCaseConnection:['Connection','Verbindung'],ConfirmCaseConnection:['Evidence','Beweis'],ChangeCoins:['Coins','Münzen'],ChangeHitPoints:['Health','Gesundheit'],UnlockTheory:['Theory','Theorie']};
 return map[event]?.[de?1:0]??(de?'Ereignis':'Event');
}
function ChoiceButtons({run,copy,pending,send}:{
 run:TaleRun;copy:Copy;pending:boolean;send:(intent:TaleIntent)=>void}){
 return <div className="backroom-choices" aria-label={copy.what}>
  <h3>{copy.what}</h3>
  {run.choices.map((choice,i)=><button key={choice.id} aria-disabled={pending}
   className={'backroom-choice'+(choice.encounter?' encounter':'')+(choice.events?.length?' event-choice':'')}
   onClick={()=>send({action:'adventure.choice',choiceId:choice.id})}>
   <span className="backroom-choice-index">{String(i+1).padStart(2,'0')}</span>
   <span className="backroom-choice-copy"><strong>{choice.text}</strong>
   {choice.check&&<small>{choice.check.ability} · DC {choice.check.difficulty}</small>}
   {choice.events?.length&&<small className="backroom-choice-event">✦ {choice.events.map(event=>eventLabel(event,copy.roll==='W20 WÜRFELN')).join(' · ')}</small>}
   {choice.encounter&&<small>{choice.encounter==='Blackjack'?'BLACKJACK':'FIVE OF A KIND'} · ENCOUNTER</small>}</span>
  </button>)}
 </div>;
}
function CharacterSheet({run,copy,pending,send,animations}:{
 run:TaleRun;copy:Copy;pending:boolean;send:(intent:TaleIntent)=>void;animations:boolean}){
 const character=run.character;
 const [rolling,setRolling]=useState(false);
 const startedAt=useRef(0),startedRollCount=useRef(run.rollsMade);
 const resolved=rolling&&run.rollsMade>startedRollCount.current;
 useEffect(()=>{
  if(!rolling)return;
  // Never invent an outcome. Remain in motion until C# supplies the result,
  // then give the physical die time to settle on its actual chosen face.
  const timer=window.setTimeout(()=>setRolling(false),resolved?
   Math.max(550,1250-(Date.now()-startedAt.current)):8000);
  return()=>window.clearTimeout(timer);
 },[rolling,resolved]);
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
  <div className={'backroom-d20 '+(run.pending?'active ':'')+(rolling?'rolling':'')}>
   <strong>D20</strong>
   <BackroomD20 value={run.lastRoll?.chosenRoll??20} rolling={rolling}
     resolved={resolved} motion={animations}/>
   {run.pending?<><small>{run.pending.ability} · DC {run.pending.difficulty} · {copy.mod} {signed(run.pending.modifier)}
    {run.pending.mode==='Advantage'?' · '+copy.adv:run.pending.mode==='Disadvantage'?' · '+copy.disadv:''}</small>
    <button className="backroom-primary" disabled={rolling||pending} aria-disabled={pending||rolling}
      onClick={()=>{startedAt.current=Date.now();startedRollCount.current=run.rollsMade;setRolling(true);send({action:'adventure.roll'});}}>{copy.roll}</button>
   </>:run.lastRoll&&<small>{outcomeLabel(run.lastRoll.outcome,'en')} · {run.lastRoll.total}/{run.lastRoll.difficulty}</small>}
  </div>
 </aside>;
}
function Inventory({run,copy,pending,send}:{
 run:TaleRun;copy:Copy;pending:boolean;send:(intent:TaleIntent)=>void}){
 return <section className="backroom-inventory">
  <h3>{copy.pack}</h3><div className="backroom-items">
   <div className="backroom-item coins"><span className="backroom-coin-icon" aria-hidden="true">◆</span><strong>{run.character.coins}</strong><small>{copy.coins}</small></div>
   {run.inventory.length===0&&<p>{copy.empty}</p>}
   {run.inventory.map(item=><div key={item.itemId} className="backroom-item" title={item.description}>
     <BackroomItemIcon kind={item.kind} name={item.name}/>
     <strong>{item.name}</strong><small>{item.kind} · ×{item.quantity}</small>
     {item.useChoices.map(choice=><button key={choice.id} aria-disabled={pending}
       onClick={()=>send({action:'adventure.choice',choiceId:choice.id})}>
       {copy.use}: {choice.text}</button>)}
    </div>)}</div>
  {run.feedback.length>0&&<details className="backroom-feed">
    <summary>{copy.eventLog} <span>({run.feedback.length})</span></summary>
    <div className="backroom-feed-entries">
   {run.feedback.map((event,i)=><p className={'event-'+event.type.toLowerCase()+(event.type==='ItemAcquired'&&run.inventory.some(item=>item.kind==='Relic'&&event.title.includes(item.name))?' rare-event':'')} key={event.type+event.title+event.description+i}><strong>{event.title}</strong> {event.description}</p>)}
  </div>
   </details>}
 </section>;
}
function Story({run,copy,pending,send,animations}:{
 run:TaleRun;copy:Copy;pending:boolean;send:(intent:TaleIntent)=>void;animations:boolean}){
 return <div className="backroom-story-grid">
  <div className="backroom-story-column">
   <section className="backroom-story-panel" key={run.scene.id}>
    <div className="backroom-scene-top">
     <div><small className="backroom-eyebrow">{run.scene.act}</small>
      <h1>{run.scene.title}</h1>{run.scene.location&&<span>{run.scene.location}</span>}</div>
     <div className="backroom-top-actions">
      {run.board&&<button aria-disabled={pending} onClick={()=>send({action:'adventure.board.open'})}>{copy.openBoard}</button>}{run.board&&<button onClick={()=>window.chrome?.webview?.postMessage({type:'lounge.adventure.board-window',version:1})} title={copy.board}>↗ {copy.board}</button>}
      <button aria-disabled={pending} onClick={()=>send({action:'adventure.back'})}>{copy.back}</button>
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
  <CharacterSheet run={run} copy={copy} pending={pending} send={send} animations={animations}/>
 </div>;
}
function CaseBoard({board,copy,pending,send,error}:{
 board:TaleBoard;copy:Copy;pending:boolean;send:(intent:TaleIntent)=>void;error:string}){
 const [selected,setSelected]=useState<string|null>(null);
 const [visibleCategories,setVisibleCategories]=useState<Set<string>>(new Set(['Person','Location','Evidence','Lead','Theory']));
 const [local,setLocal]=useState<{id:string;x:number;y:number}|null>(null);
 const drag=useRef<{id:string;startX:number;startY:number;x:number;y:number}|null>(null);
 const [camera,setCamera]=useState({zoom:board.zoom,panX:board.panX,panY:board.panY});
 const cameraRef=useRef(camera);cameraRef.current=camera;
 const sendRef=useRef(send);sendRef.current=send;
 const pan=useRef<{id:number;x:number;y:number;panX:number;panY:number}|null>(null);
 const [panning,setPanning]=useState(false);
 const seen=useRef(new Set(board.nodes.map(n=>n.id)));
 const [fresh,setFresh]=useState<string[]>([]);
 useEffect(()=>{
  const incoming=board.nodes.map(n=>n.id).filter(id=>!seen.current.has(id));
  board.nodes.forEach(n=>seen.current.add(n.id));
  if(incoming.length===0)return;
  setFresh(incoming);
  const timer=window.setTimeout(()=>setFresh([]),3200);
  return()=>window.clearTimeout(timer);
 },[board.nodes]);
 useEffect(()=>setCamera({zoom:board.zoom,panX:board.panX,panY:board.panY}),[board.zoom,board.panX,board.panY]);
 const nodes=board.nodes.filter(n=>visibleCategories.has(n.category));
 const chosen=nodes.find(n=>n.id===selected);
 const effective=(n:TaleBoardNode)=>local?.id===n.id?local:n;
 const commitCamera=(next:typeof camera)=>{setCamera(next);send({action:'adventure.board.view',...next});};
 const start=(event:ReactPointerEvent<HTMLButtonElement>,n:TaleBoardNode)=>{
  if(event.button!==0)return;
  event.currentTarget.setPointerCapture(event.pointerId);
  drag.current={id:n.id,startX:event.clientX,startY:event.clientY,x:effective(n).x,y:effective(n).y};
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
  drag.current=null;
 };
 useEffect(()=>{if(local&&!drag.current){const saved=board.nodes.find(n=>n.id===local.id);
  if((saved&&Math.abs(saved.x-local.x)<1&&Math.abs(saved.y-local.y)<1)||!!error)setLocal(null);}
 },[board.nodes,pending,local,error]);
 const viewport=useRef<HTMLDivElement>(null);
 // Native non-passive wheel listener prevents the browser scrolling under the board.
 useEffect(()=>{
  const el=viewport.current;if(!el)return;
  let timer:ReturnType<typeof setTimeout>|null=null;
  const onWheel=(event:WheelEvent)=>{
   event.preventDefault();
   const rect=el.getBoundingClientRect();
   const previous=cameraRef.current;
   const zoom=Math.max(.2,Math.min(4,previous.zoom*Math.exp(-event.deltaY*.00115)));
   const pointerX=event.clientX-rect.left,pointerY=event.clientY-rect.top;
   const next={zoom,
    panX:Math.max(-5000,Math.min(5000,pointerX-(pointerX-previous.panX)*zoom/previous.zoom)),
    panY:Math.max(-5000,Math.min(5000,pointerY-(pointerY-previous.panY)*zoom/previous.zoom))};
   cameraRef.current=next;setCamera(next);
   if(timer!==null)window.clearTimeout(timer);
   timer=window.setTimeout(()=>sendRef.current({action:'adventure.board.view',...cameraRef.current}),120);
  };
  el.addEventListener('wheel',onWheel,{passive:false});
  return()=>{el.removeEventListener('wheel',onWheel);if(timer!==null)window.clearTimeout(timer);};
 },[]);
 const beginPan=(event:ReactPointerEvent<HTMLDivElement>)=>{
  if(event.button!==0||(event.target as Element).closest('.backroom-board-node'))return;
  event.currentTarget.setPointerCapture(event.pointerId);
  pan.current={id:event.pointerId,x:event.clientX,y:event.clientY,
   panX:cameraRef.current.panX,panY:cameraRef.current.panY};
  setPanning(true);
 };
 const movePan=(event:ReactPointerEvent<HTMLDivElement>)=>{
  const p=pan.current;if(!p||p.id!==event.pointerId)return;
  const next={...cameraRef.current,
   panX:Math.max(-5000,Math.min(5000,p.panX+event.clientX-p.x)),
   panY:Math.max(-5000,Math.min(5000,p.panY+event.clientY-p.y))};
  cameraRef.current=next;setCamera(next);
 };
 const finishPan=(event:ReactPointerEvent<HTMLDivElement>)=>{
  if(!pan.current||pan.current.id!==event.pointerId)return;
  pan.current=null;setPanning(false);
  sendRef.current({action:'adventure.board.view',...cameraRef.current});
 };
 const fit=()=>{const host=viewport.current;if(!host||!nodes.length)return;
  const minX=Math.min(...nodes.map(n=>n.x)),minY=Math.min(...nodes.map(n=>n.y));
  const w=Math.max(...nodes.map(n=>n.x+280))-minX,h=Math.max(...nodes.map(n=>n.y+150))-minY;
  const zoom=Math.max(.2,Math.min(1.2,(host.clientWidth-50)/w,(host.clientHeight-50)/h));
  commitCamera({zoom,panX:Math.max(-5000,Math.min(5000,25-minX*zoom)),panY:Math.max(-5000,Math.min(5000,25-minY*zoom))});
 };
 const categories=['Person','Location','Evidence','Lead','Theory'];
 return <div className="backroom-board-page">
  <div className="backroom-board-top"><div><small className="backroom-eyebrow">{copy.board}</small>
    <h1>{board.title}</h1></div>
    <div className="backroom-top-actions">
     <button onClick={()=>window.chrome?.webview?.postMessage({type:'lounge.adventure.board-window',version:1})}>↗ {copy.board}</button>
     <button aria-disabled={pending} onClick={()=>send({action:'adventure.board.close'})}>{copy.backStory}</button>
     <button onClick={()=>commitCamera({...camera,zoom:Math.min(4,camera.zoom*1.25)})}>{copy.zoomIn}</button>
     <button onClick={()=>commitCamera({...camera,zoom:Math.max(.2,camera.zoom*.8)})}>{copy.zoomOut}</button>
     <button onClick={fit}>{copy.reset}</button>
    </div></div>
  <div className="backroom-board-filters">{categories.map(c=><label key={c} className={'backroom-category-'+c.toLowerCase()}><input type="checkbox"
   checked={visibleCategories.has(c)} onChange={e=>setVisibleCategories(previous=>{
    const next=new Set(previous);if(e.target.checked)next.add(c);else next.delete(c);return next;
   })}/>{c}</label>)}</div>
  <div className="backroom-board-grid">
   <div className={'backroom-board-viewport'+(panning?' is-panning':'')} ref={viewport}
    title={copy.panTip} onPointerDown={beginPan} onPointerMove={movePan}
    onPointerUp={finishPan} onPointerCancel={()=>{pan.current=null;setPanning(false);}}> 
    {fresh.length>0&&<div className="backroom-new-entry-notice" role="status" key={fresh.join('|')}>
      <span aria-hidden="true">◇</span> {copy.newClue} · +{fresh.length}
     </div>}
    <div className="backroom-board-world" style={{
     transform:'translate('+camera.panX+'px,'+camera.panY+'px) scale('+camera.zoom+')'}}>
     <svg className="backroom-board-lines" width="5000" height="5000" viewBox="0 0 5000 5000"
      aria-hidden="true">{board.connections.filter(c=>nodes.some(n=>n.id===c.from)&&nodes.some(n=>n.id===c.to))
       .map(c=>{const from=nodes.find(n=>n.id===c.from)!,to=nodes.find(n=>n.id===c.to)!;
        const a=effective(from),b=effective(to);
        return <line key={c.id} x1={a.x+130} y1={a.y+60} x2={b.x+130} y2={b.y+60}
         className={c.status.toLowerCase()}/>;})}</svg>
     {nodes.map(n=>{const p=effective(n);return <button key={n.id}
      aria-disabled={pending} className={'backroom-board-node '+n.category.toLowerCase()+
       (selected===n.id?' selected':'')+(fresh.includes(n.id)?' new-entry':'')}
      style={{left:p.x,top:p.y}} title={n.description}
      onPointerDown={e=>{e.stopPropagation();start(e,n);}} onPointerMove={movement} onPointerUp={end}
      onPointerCancel={()=>{drag.current=null;setLocal(null);}}>
      <small>{n.category.toUpperCase()} · {n.reliability}</small><strong>{n.title}</strong>
      <span>{n.subtitle||n.lead}</span></button>;})}</div>
   </div>
   <aside className="backroom-board-details" aria-label={copy.node}>
    <div className="backroom-detail-heading"><span className="backroom-eyebrow">{copy.board}</span><h3>{copy.node}</h3></div>
    {chosen?<div className={'backroom-detail-content backroom-detail-'+chosen.category.toLowerCase()}>
      <div className="backroom-detail-status"><span>{chosen.category}</span><span>{chosen.reliability}</span></div>
      <h2>{chosen.title}</h2><p className="backroom-detail-description">{chosen.description}</p>
      {chosen.facts.length>0&&<><h3>{copy.facts}</h3>
       {chosen.facts.map((f,i)=><p key={i}>{f}</p>)}</>}
      {chosen.quotes.length>0&&<><h3>{copy.quotes}</h3>
       {chosen.quotes.map((f,i)=><blockquote key={i}>{f}</blockquote>)}</>}</div>
      : <div className="backroom-detail-empty"><span aria-hidden="true">◇</span><p>{nodes.length} {copy.node.toLowerCase()} · {board.connections.length} {copy.connections}</p></div>}
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
    disabled={!dice.canBank&&dice.rollsUsed===3||encounter.outcome!==null||dice.rollsUsed===0} aria-disabled={pending||!dice.canBank&&dice.rollsUsed===3||encounter.outcome!==null||dice.rollsUsed===0}
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
    <button disabled={!bj.canHit} aria-disabled={pending||!bj.canHit} onClick={()=>send({action:'adventure.encounter.hit'})}>{copy.hit}</button>
    <button className="backroom-primary" disabled={!bj.canStand} aria-disabled={pending||!bj.canStand}
     onClick={()=>send({action:'adventure.encounter.stand'})}>{copy.stand}</button></>}
   {dice&&encounter.outcome===null&&<>
    <button disabled={!dice.canRoll} aria-disabled={pending||!dice.canRoll} onClick={()=>send({action:'adventure.encounter.roll'})}>{copy.rollDice}</button>
    <button className="backroom-primary" disabled={!dice.canBank} aria-disabled={pending||!dice.canBank}
     onClick={()=>send({action:'adventure.encounter.bank'})}>{copy.bank}</button></>}
   {encounter.outcome&&<button className="backroom-primary" aria-disabled={pending}
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
   <button className="backroom-primary" aria-disabled={pending}
    onClick={()=>send({action:'adventure.restart',adventureId:run.adventureId})}>{copy.another}</button>
   <button aria-disabled={pending} onClick={()=>send({action:'adventure.back'})}>{copy.return}</button>
  </div></section>;
}
export function AdventureScreen({state,language,pending,error,command,animations}:Props){
 const t=words[language], [classId,setClassId]=useState('rogue');
 const [musicVolume,setMusicVolume]=useState(state.musicVolume);
 useEffect(()=>setMusicVolume(state.musicVolume),[state.musicVolume]);
 const selected=state.adventures.find(tale=>tale.id===state.selectedAdventureId);
 const selectedClass=state.classes.find(c=>c.id===classId)??state.classes[0];
 useEffect(()=>{if(!state.classes.some(c=>c.id===classId))setClassId(state.classes[0]?.id??'rogue');},
  [state.selectedAdventureId,state.classes,classId]);
 const [newBoard,setNewBoard]=useState<{id:string;title:string;count:number}|null>(null);
 const boardHistory=useRef<{runId:string;ids:Set<string>}|null>(null);
 useEffect(()=>{
  const run=state.run;
  if(!run?.board){
   if(!run)boardHistory.current=null;
   return;
  }
  const previous=boardHistory.current;
  if(!previous||previous.runId!==run.runId){
   boardHistory.current={runId:run.runId,ids:new Set(run.board.nodes.map(n=>n.id))};
   return;
  }
  const additions=run.board.nodes.filter(n=>!previous.ids.has(n.id));
  additions.forEach(n=>previous.ids.add(n.id));
  if(!additions.length)return;
  setNewBoard({id:run.runId+':'+additions.map(n=>n.id).join('|'),
   title:additions[0].title,count:additions.length});
 },[state.run?.runId,state.run?.board?.nodes]);
 useEffect(()=>{
  if(!newBoard)return;
  const timer=window.setTimeout(()=>setNewBoard(null),4300);
  return()=>window.clearTimeout(timer);
 },[newBoard?.id]);
 const send=(intent:TaleIntent)=>{if(!pending)command(intent);};
 const start=()=>{
  const replacing=selected?.status==='active';
  if(replacing&&!window.confirm(t.discard))return;
  send({action:'adventure.start',adventureId:state.selectedAdventureId,
   classId:selectedClass.id,...(replacing?{confirmReplace:true as const}:{})});
 };
 return <div className="backroom-shell" data-testid={'modern-adventure-'+state.screen}>
  {newBoard&&<div key={newBoard.id} className="backroom-discovery"
    data-testid="backroom-discovery" role="status" aria-live="polite">
    <span className="backroom-discovery-signal" aria-hidden="true"/>
    <div><small>{t.newClue}{newBoard.count>1?' · +'+newBoard.count:''}</small>
      <strong>{newBoard.title}</strong></div>
    {state.screen!=='board'&&<button type="button"
      onClick={()=>{setNewBoard(null);send({action:'adventure.board.open'});}}>
      {t.openBoard}</button>}
  </div>}
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
       <button disabled={tale.id===state.selectedAdventureId} aria-disabled={pending||tale.id===state.selectedAdventureId}
        onClick={()=>send({action:'adventure.select',adventureId:tale.id})}>{t.select}</button>
       {tale.status!=='unplayed'&&<button className="backroom-primary" aria-disabled={pending}
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
     <div className="backroom-music"><strong>BACKROOM SCORE · Lanterns Beyond the Door</strong>
      <button aria-disabled={pending} onClick={()=>send({action:'adventure.music',
       enabled:!state.musicEnabled,volume:musicVolume})}>
       MUSIC: {state.musicEnabled?'ON':'OFF'}</button>
      <label>VOLUME · {Math.round(musicVolume/.32*100)}%
       <input type="range" min="0" max="0.32" step="0.01" value={musicVolume}
        onChange={e=>setMusicVolume(Number(e.target.value))}
        onPointerUp={e=>send({action:'adventure.music',enabled:state.musicEnabled,
         volume:Number(e.currentTarget.value)})}
        onKeyUp={e=>send({action:'adventure.music',enabled:state.musicEnabled,
         volume:Number(e.currentTarget.value)})}/></label>
     </div>
     <button className="backroom-primary" disabled={!selectedClass} aria-disabled={pending||!selectedClass} onClick={start}>
      {selected?.status==='active'?t.replace:t.newRun}</button>
     <small>{t.sound}</small>
    </aside>
   </div>
  </>:state.run&&state.screen==='story'
   ?<Story run={state.run} copy={t} pending={pending} send={send} animations={animations}/>
   :state.run&&state.screen==='board'&&state.run.board
   ?<CaseBoard board={state.run.board} copy={t} pending={pending} send={send} error={error}/>
   :state.run&&state.screen==='encounter'&&state.encounter
   ?<Encounter encounter={state.encounter} copy={t} pending={pending} send={send}/>
   :state.run&&state.screen==='complete'
   ?<Completion run={state.run} copy={t} meta={state.metaNotice} pending={pending} send={send}/>
   :null}
  {error&&<p className="backroom-error" role="alert">{error}</p>}
 </div>;
}
