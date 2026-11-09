import {useEffect,useRef,useState} from 'react';
import type {KeyboardEvent,MouseEvent} from 'react';
import type {SweepState,SweepIntent} from './minesweeperBridge';
import {SweepField} from './SweepField';
import {SweepAudio} from './minesweeperAudio';
import type {SweepCue} from './minesweeperAudio';
import './minesweeper.css';

type Props={state:SweepState;language:'en'|'de';pending:boolean;error:string;
 animations:boolean;soundEnabled:boolean;command:(intent:SweepIntent)=>void};
type Tool='reveal'|'flag'|'scan';
const difficulties=[
 {id:'easy' as const,number:'01',dimensions:'8 × 8',mines:10,en:'Stargazer',de:'Sternenblick'},
 {id:'standard' as const,number:'02',dimensions:'12 × 12',mines:24,en:'Nightfall',de:'Nachteinbruch'},
 {id:'expert' as const,number:'03',dimensions:'16 × 16',mines:40,en:'Event Horizon',de:'Ereignishorizont'},
];
const labels={
 en:{
 eyebrow:'THE MIDNIGHT CLUB · CHILL LOUNGE',title:'Midnight Sweep',
 intro:'Chart the dark. Read the numbers. One false step can extinguish the stars.',
 tagline:'CONSTELLATION SURVEY',choose:'Choose your starfield',
 chooseSub:'Three hand-crafted challenges, each with a guaranteed safe opening.',
 start:'ENTER THE FIELD',resume:'CONTINUE LAST FIELD',
 live:'LIVE SURVEY',flags:'Markers',timer:'Time',safe:'Safe tiles',echo:'Echo pulse',
 echoReady:'1 signal remaining',echoSpent:'Echo signal spent',
 echoHelp:'Scan one hidden tile to see the number of hazards within its 3 × 3 neighborhood — even if that tile hides a mine.',
 reveal:'Reveal',flag:'Mark',scan:'Echo scan',reset:'New field',return:'Choose field',
 guide:'Left-click to uncover · right-click to mark · click a revealed number to open its neighbors when markers match.',
 touch:'Select a tool for touch or keyboard. F toggles a marker on the focused tile.',
 notStarted:'Your first move and its surrounding tiles are always safe.',
 victory:'The sky is yours.',defeat:'The stars went dark.',
 victoryDetail:'Every safe tile charted. The constellation is complete.',
 defeatDetail:'A hidden charge was disturbed. The field has gone silent.',
 playAgain:'SURVEY AGAIN',other:'CHOOSE ANOTHER FIELD',
 result:'SURVEY COMPLETE',failed:'SIGNAL LOST',noScan:'No Echo Scan used',
 danger:'hazards',cancel:'Dismiss',detonated:'Detonated mine',
 tile:'Tile',hidden:'hidden',revealed:'revealed',flagged:'marked',scanned:'scanned',hazard:'hazard',
 },
 de:{
 eyebrow:'THE MIDNIGHT CLUB · CHILL LOUNGE',title:'Midnight Sweep',
 intro:'Kartiere die Dunkelheit. Lies die Zahlen. Ein falscher Schritt kann die Sterne erlöschen lassen.',
 tagline:'STERNENKARTIERUNG',choose:'Wähle dein Sternenfeld',
 chooseSub:'Drei Herausforderungen mit garantiert sicherem ersten Zug.',
 start:'FELD BETRETEN',resume:'LETZTES FELD FORTSETZEN',
 live:'AKTIVE ERKUNDUNG',flags:'Markierungen',timer:'Zeit',safe:'Sichere Felder',echo:'Echo-Impuls',
 echoReady:'1 Signal verfügbar',echoSpent:'Echo-Signal verbraucht',
 echoHelp:'Scanne ein verdecktes Feld, um die Zahl der Gefahren in seinem 3 × 3-Bereich zu sehen – auch wenn im Feld selbst eine Mine liegt.',
 reveal:'Aufdecken',flag:'Markieren',scan:'Echo-Scan',reset:'Neues Feld',return:'Feldauswahl',
 guide:'Linksklick zum Aufdecken · Rechtsklick zum Markieren · aufgedeckte Zahl anklicken, wenn passende Markierungen gesetzt sind.',
 touch:'Wähle ein Werkzeug für Touch oder Tastatur. F setzt eine Markierung auf dem fokussierten Feld.',
 notStarted:'Dein erster Zug und die umliegenden Felder sind garantiert sicher.',
 victory:'Der Himmel gehört dir.',defeat:'Die Sterne sind erloschen.',
 victoryDetail:'Alle sicheren Felder kartiert. Das Sternbild ist vollständig.',
 defeatDetail:'Eine verborgene Mine wurde ausgelöst. Das Feld ist verstummt.',
 playAgain:'NOCH EINMAL',other:'ANDERES FELD WÄHLEN',
 result:'ERKUNDUNG ABGESCHLOSSEN',failed:'SIGNAL VERLOREN',noScan:'Ohne Echo-Scan',
 danger:'Gefahren',cancel:'Schließen',detonated:'Explodierte Mine',
 tile:'Feld',hidden:'verdeckt',revealed:'aufgedeckt',flagged:'markiert',scanned:'gescannt',hazard:'Gefahr',
 },
};
const time=(seconds:number)=>`${String(Math.floor(seconds/60)).padStart(2,'0')}:${String(seconds%60).padStart(2,'0')}`;

export function MinesweeperScreen({state,language,pending,error,animations,soundEnabled,command}:Props){
 const t=labels[language];
 const [tool,setTool]=useState<Tool>('reveal');
 const audio=useRef<SweepAudio|null>(null);
 const previous=useRef<SweepState|null>(null);
 const [help,setHelp]=useState(false);
 useEffect(()=>{
  const sound=new SweepAudio();audio.current=sound;sound.setEnabled(soundEnabled);
  return()=>{sound.dispose();audio.current=null;};
 },[]);
 useEffect(()=>{audio.current?.setEnabled(soundEnabled);},[soundEnabled]);
 useEffect(()=>{
  const old=previous.current;
  if(old?.runId===state.runId){
   if(state.phase!==old.phase&&state.phase==='won')audio.current?.play('win');
   else if(state.phase!==old.phase&&state.phase==='lost')audio.current?.play('lose');
   else if(state.scansUsed>old.scansUsed)audio.current?.play('scan');
   else if(state.flagged!==old.flagged)audio.current?.play(state.flagged>old.flagged?'flag':'unflag');
   else if(state.safeRevealed>old.safeRevealed)audio.current?.play('reveal');
  }else if(old&&state.screen==='game')audio.current?.play('start');
  previous.current=state;
 },[state]);
 const dispatch=(intent:SweepIntent,cue?:SweepCue)=>{
  if(pending)return;
  audio.current?.arm();
  if(cue)audio.current?.play(cue);
  command(intent);
 };
 const isFinished=state.phase==='won'||state.phase==='lost';
 const tileDescription=(index:number)=>{
  const cell=state.cells[index],column=index%state.width+1,row=Math.floor(index/state.width)+1;
  let status=cell.flagged?t.flagged:cell.revealed?t.revealed:t.hidden;
  if(cell.scanned)status+=', '+t.scanned+' '+cell.scanCount;
  if(cell.revealed&&cell.adjacent>0)status+=', '+cell.adjacent+' '+t.danger;
  if(cell.mine)status+=', '+(index===state.explodedIndex?t.detonated:t.hazard);
  return `${t.tile} ${row}, ${column}: ${status}`;
 };
 const actionFor=(index:number,kind:Tool=tool)=>{
  const cell=state.cells[index];
  if(isFinished)return;
  if(kind==='flag')return dispatch({action:'mine.flag',index});
  if(kind==='scan')return dispatch({action:'mine.scan',index});
  return dispatch({action:cell.revealed?'mine.chord':'mine.reveal',index});
 };
 function keyDown(event:KeyboardEvent<HTMLButtonElement>,index:number){
  if(event.key.toLowerCase()==='f'){
   event.preventDefault();actionFor(index,'flag');
  }
 }
 function contextMenu(event:MouseEvent<HTMLButtonElement>,index:number){
  event.preventDefault();actionFor(index,'flag');
 }
 const current=difficulties.find(x=>x.id===state.difficulty)!;
 if(state.screen==='menu')return <section className="sweep-screen sweep-library">
  <div className="sweep-atmosphere" aria-hidden="true"/>
  <header className="sweep-intro">
   <div><p className="sweep-overline">{t.eyebrow}</p><span className="sweep-kicker">{t.tagline}</span>
    <h1>{t.title}</h1><p>{t.intro}</p></div>
   <div className="sweep-orbit" aria-hidden="true"><div className="sweep-orbit-inner"><span>✧</span></div></div>
  </header>
  <div className="sweep-library-head"><h2>{t.choose}</h2><span>{t.chooseSub}</span></div>
  <div className="sweep-difficulty-grid">
   {difficulties.map((level,idx)=><button key={level.id} className={'sweep-difficulty sweep-difficulty-'+level.id}
    type="button" disabled={pending} onClick={()=>dispatch({action:'mine.new',difficulty:level.id})}>
    <div className="sweep-difficulty-art" aria-hidden="true">
     <span className="sweep-art-orbit"/><span className="sweep-art-star">✧</span>
     <span className="sweep-art-grid">{Array.from({length:25},(_,i)=><i key={i} className={
      i===(idx*3+7)||i===(idx*2+17)?'hazard':i%7===idx?'lit':''}/>)}</span>
    </div>
    <div className="sweep-difficulty-info"><span className="sweep-number">{level.number} · {level.dimensions}</span>
     <strong>{language==='de'?level.de:level.en}</strong>
     <span>{level.mines} {t.danger}</span><b>{t.start} <span aria-hidden="true">→</span></b>
    </div>
   </button>)}
  </div>
  {state.phase!=='ready'&&!state.phase.includes('lost')&&<button type="button" className="sweep-resume"
    disabled={pending} onClick={()=>dispatch({action:'mine.resume'})}>{t.resume} · {current.dimensions} →</button>}
  {error&&<p className="sweep-error" role="alert">{error}</p>}
 </section>;
 return <section className={'sweep-screen sweep-game'+(isFinished?' sweep-complete':'')}>
  <div className="sweep-game-top">
   <div><span className="sweep-overline">{t.live} · {current.number}</span>
    <h1>{language==='de'?current.de:current.en}</h1>
    <p>{current.dimensions} · {state.mines} {t.danger}</p></div>
   <div className="sweep-game-actions">
    <button type="button" onClick={()=>dispatch({action:'mine.menu'})} disabled={pending}>{t.return}</button>
    <button type="button" onClick={()=>dispatch({action:'mine.new',difficulty:state.difficulty})} disabled={pending}>{t.reset}</button>
   </div>
  </div>
  <div className="sweep-play-layout">
   <div className="sweep-play-center">
    <div className="sweep-board-frame">
     <div className="sweep-board-wrap" style={{'--sweep-count':state.width} as React.CSSProperties}>
      <SweepField state={state} animations={animations}/>
      <div className="sweep-hit-grid" role="group" aria-label={t.title}
       style={{gridTemplateColumns:`repeat(${state.width},minmax(0,1fr))`}}>
       {state.cells.map((cell,index)=><button key={state.runId+'-'+index} type="button"
        disabled={pending||isFinished||(tool==='scan'&&(state.scansUsed>0||cell.revealed||cell.flagged||cell.scanned))}
        className={'sweep-tile-target'+(cell.revealed?' opened':'')+(cell.flagged?' marked':'')}
        aria-label={tileDescription(index)} title={tileDescription(index)}
        onClick={()=>actionFor(index)} onContextMenu={event=>contextMenu(event,index)}
        onKeyDown={event=>keyDown(event,index)} />)}
      </div>
     </div>
    </div>
    <div className="sweep-board-guide">{t.guide}</div>
    {state.phase==='ready'&&<div className="sweep-first-safe">{t.notStarted}</div>}
   </div>
   <aside className="sweep-control-deck">
    <div className="sweep-control-label">{t.tagline}</div>
    <div className="sweep-stats">
     <div><span>{t.flags}</span><strong>{state.flagged}<small>/{state.mines}</small></strong></div>
     <div><span>{t.timer}</span><strong>{time(state.seconds)}</strong></div>
     <div><span>{t.safe}</span><strong>{state.safeRevealed}<small>/{state.width*state.height-state.mines}</small></strong></div>
    </div>
    <div className="sweep-toolbox">
     <button type="button" aria-pressed={tool==='reveal'} className={tool==='reveal'?'active':''}
      onClick={()=>{audio.current?.arm();setTool('reveal');}}><span>✦</span>{t.reveal}</button>
     <button type="button" aria-pressed={tool==='flag'} className={tool==='flag'?'active':''}
      onClick={()=>{audio.current?.arm();setTool('flag');}}><span>⚑</span>{t.flag}</button>
     <button type="button" aria-pressed={tool==='scan'} className={tool==='scan'?'active':''}
      disabled={state.scansUsed>0||state.phase==='ready'} onClick={()=>{audio.current?.arm();setTool('scan');}}>
      <span>◎</span>{t.scan}</button>
    </div>
    <div className={'sweep-echo'+(state.scansUsed?' spent':'')}>
     <div className="sweep-echo-symbol" aria-hidden="true">◎</div>
     <div><strong>{t.echo}</strong><span>{state.scansUsed?t.echoSpent:t.echoReady}</span></div>
    </div>
    <button type="button" className="sweep-help-toggle" aria-expanded={help} onClick={()=>setHelp(x=>!x)}>
     {t.echoHelp.length>0?'ⓘ ':''}{t.echo} {help?'−':'+'}</button>
    {help&&<p className="sweep-help">{t.echoHelp} {t.touch}</p>}
    {error&&<p className="sweep-error" role="alert">{error}</p>}
   </aside>
  </div>
  {isFinished&&<div className={'sweep-finish-screen '+(state.phase==='won'?'is-win':'is-loss')} role="dialog"
   aria-modal="true" aria-labelledby="sweep-finish-title">
   <div className="sweep-finish-glow" aria-hidden="true"/>
   <div className="sweep-finish-panel">
    <div className="sweep-result-symbol" aria-hidden="true">{state.phase==='won'?'✧':'◇'}</div>
    <p className="sweep-overline">{state.phase==='won'?t.result:t.failed}</p>
    <h2 id="sweep-finish-title">{state.phase==='won'?t.victory:t.defeat}</h2>
    <p className="sweep-finish-description">{state.phase==='won'?t.victoryDetail:t.defeatDetail}</p>
    <div className="sweep-finish-stats">
     <span><strong>{time(state.seconds)}</strong>{t.timer}</span>
     <span><strong>{state.safeRevealed}</strong>{t.safe}</span>
     <span><strong>{state.scansUsed===0?'✧':'◎'}</strong>{state.scansUsed===0?t.noScan:t.echoSpent}</span>
    </div>
    <div className="sweep-finish-actions">
     <button type="button" disabled={pending} onClick={()=>dispatch({action:'mine.new',difficulty:state.difficulty},'start')}>{t.playAgain}</button>
     <button type="button" disabled={pending} onClick={()=>dispatch({action:'mine.menu'})}>{t.other}</button>
    </div>
   </div>
  </div>}
 </section>;
}
