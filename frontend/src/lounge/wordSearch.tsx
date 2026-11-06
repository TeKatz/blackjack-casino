import {useEffect,useMemo,useRef,useState} from 'react';
import type {PointerEvent} from 'react';
import type {WordSearchDifficulty,WordSearchIntent,WordSearchPuzzle,WordSearchState} from './wordSearchBridge';
import './wordSearch.css';
type Language='en'|'de';
type Point={row:number;column:number};
type Props={state:WordSearchState;language:Language;pending:boolean;error:string;animations:boolean;command:(v:WordSearchIntent)=>void};
const names:Record<Language,Record<WordSearchDifficulty,string>>={
 en:{Relaxed:'Relaxed',Easy:'Easy',Medium:'Medium',Hard:'Hard',Expert:'Expert',Master:'Master'},
 de:{Relaxed:'Entspannt',Easy:'Leicht',Medium:'Mittel',Hard:'Schwer',Expert:'Experte',Master:'Meister'}};
const words={
 en:{eyebrow:'WORD SEARCH · THE MIDNIGHT CLUB',title:'Find the words. Keep the pace yours.',
 intro:'Discover a larger collection of hidden words and themes. No bets, no rush. Every find is saved.',
 solved:'SOLVED PUZZLES',themes:'THEMES',words:'WORDS FOUND',library:'Choose a difficulty',
 levels:'48 puzzles per difficulty',play:'PLAY NEXT',replay:'REPLAY',resume:'CONTINUE YOUR PUZZLE',
 noResume:'Choose a difficulty to begin.',hint:'REVEAL STARTING LETTER',hintInfo:'Hints never reduce your XP.',
 wordList:'WORDS TO DISCOVER',found:'FOUND',total:'WORDS',time:'TIME',hints:'HINTS',
 back:'← WORD SEARCH LEVELS',complete:'PUZZLE COMPLETE',finished:'Every hidden word found. Your completion was saved.',
 xp:'PLAYER XP',season:'SEASON XP',first:'Click two endpoints or drag across a complete word.',
 required:'Finish your current puzzle before starting another one.',pending:'Saving discovery…',
 saved:'Autosaved',dirRelaxed:'Horizontal + vertical',dirEasy:'Diagonals included',dirHard:'All eight directions, backwards too',
 noRush:'No speed or hint penalties. Relax and enjoy.',newWords:'New themes and vocabulary in 288 puzzles.'},
 de:{eyebrow:'WORTSUCHE · THE MIDNIGHT CLUB',title:'Finde die Wörter. In deinem Tempo.',
 intro:'Entdecke neue Wörter und Themenwelten. Ohne Einsätze und Zeitdruck. Jeder Fund bleibt gespeichert.',
 solved:'GELÖSTE RÄTSEL',themes:'THEMEN',words:'GEFUNDENE WÖRTER',library:'Wähle deinen Schwierigkeitsgrad',
 levels:'48 Rätsel je Schwierigkeitsgrad',play:'NÄCHSTES RÄTSEL',replay:'WIEDERHOLEN',resume:'SPIEL FORTSETZEN',
 noResume:'Wähle einen Schwierigkeitsgrad.',hint:'ANFANGSBUCHSTABEN ZEIGEN',hintInfo:'Hinweise verringern deine XP nicht.',
 wordList:'GESUCHTE WÖRTER',found:'GEFUNDEN',total:'WÖRTER',time:'ZEIT',hints:'HINWEISE',
 back:'← WORTSUCHE-AUSWAHL',complete:'RÄTSEL GELÖST',finished:'Alle Wörter gefunden. Dein Erfolg wurde gespeichert.',
 xp:'SPIELER-XP',season:'SAISON-XP',first:'Zwei Endpunkte anklicken oder eine Linie über das Wort ziehen.',
 required:'Beende zuerst dein begonnenes Rätsel.',pending:'Fund wird gespeichert…',
 saved:'Automatisch gespeichert',dirRelaxed:'Waagerecht + senkrecht',dirEasy:'Auch diagonal',dirHard:'Alle acht Richtungen, auch rückwärts',
 noRush:'Keine Zeit- oder Hinweisstrafen. Viel Spaß beim Suchen.',newWords:'Neue Themen und Wortvielfalt in 288 Rätseln.'}};
const formatTime=(s:number)=>{const h=Math.floor(s/3600),m=Math.floor(s%3600/60),sec=s%60;return(h?String(h).padStart(2,'0')+':':'')+String(m).padStart(2,'0')+':'+String(sec).padStart(2,'0')};
const same=(a:Point,b:Point)=>a.row===b.row&&a.column===b.column;
const line=(a:Point,b:Point):Point[]=>{
 const dr=b.row-a.row,dc=b.column-a.column;
 if(dr!==0&&dc!==0&&Math.abs(dr)!==Math.abs(dc))return [];
 const length=Math.max(Math.abs(dr),Math.abs(dc));
 return Array.from({length:length+1},(_,i)=>({row:a.row+Math.sign(dr)*i,column:a.column+Math.sign(dc)*i}));
};
const snap=(a:Point,b:Point):Point=>{
 const dr=b.row-a.row,dc=b.column-a.column;
 if(dr===0||dc===0||Math.abs(dr)===Math.abs(dc))return b;
 if(Math.abs(dr)>Math.abs(dc)*2)return {row:b.row,column:a.column};
 if(Math.abs(dc)>Math.abs(dr)*2)return {row:a.row,column:b.column};
 const n=Math.min(Math.abs(dr),Math.abs(dc));
 return {row:a.row+Math.sign(dr)*n,column:a.column+Math.sign(dc)*n};
};
function Board({game,language,pending,command,animations}:{game:WordSearchPuzzle;language:Language;pending:boolean;command:Props['command'];animations:boolean}){
 const [anchor,setAnchor]=useState<Point|null>(null),[target,setTarget]=useState<Point|null>(null);
 const anchorRef=useRef<Point|null>(null),dragRef=useRef(false),board=useRef<HTMLDivElement>(null);
 const size=game.size;
 useEffect(()=>{anchorRef.current=null;setAnchor(null);setTarget(null);},[game.id,game.found.length]);
 const found=useMemo(()=>{
  const set=new Set<number>();
  for(const p of game.foundPaths)for(const c of line({row:p.startRow,column:p.startColumn},{row:p.endRow,column:p.endColumn}))
   set.add(c.row*size+c.column);
  return set;
 },[game.foundPaths,size]);
 const preview=useMemo(()=>new Set(anchor?line(anchor,target??anchor).map(c=>c.row*size+c.column):[]),[anchor,target]);
 function submit(start:Point,end:Point){
  const last=snap(start,end);if(same(start,last))return;
  anchorRef.current=null;setAnchor(null);setTarget(null);
  command({action:'word-search.select',startRow:start.row,startColumn:start.column,endRow:last.row,endColumn:last.column});
 }
 function choose(p:Point){
  if(pending||game.completed)return;
  const current=anchorRef.current;
  if(!current){anchorRef.current=p;setAnchor(p);setTarget(p);}
  else if(same(current,p)){anchorRef.current=null;setAnchor(null);setTarget(null);}
  else submit(current,p);
 }
 function at(e:PointerEvent):Point|null{
  const rect=board.current?.getBoundingClientRect();
  if(!rect||!rect.width||!rect.height)return null;
  return {row:Math.max(0,Math.min(size-1,Math.floor((e.clientY-rect.top)/rect.height*size))),
   column:Math.max(0,Math.min(size-1,Math.floor((e.clientX-rect.left)/rect.width*size)))};
 }
 function down(e:PointerEvent){
  if(pending||game.completed||e.button!==0)return;
  const p=at(e);if(!p)return;
  e.preventDefault();dragRef.current=true;board.current?.setPointerCapture(e.pointerId);
  if(!anchorRef.current){anchorRef.current=p;setAnchor(p);}
  setTarget(p);
 }
 function move(e:PointerEvent){
  if(!dragRef.current||pending)return;
  const p=at(e),a=anchorRef.current;if(p&&a)setTarget(snap(a,p));
 }
 function up(e:PointerEvent){
  if(!dragRef.current)return;dragRef.current=false;
  const p=at(e),a=anchorRef.current;
  if(p&&a){const end=snap(a,p);if(!same(a,end))submit(a,end);else setTarget(a);}
  if(board.current?.hasPointerCapture(e.pointerId))board.current.releasePointerCapture(e.pointerId);
 }
 return <div className="ws-board-frame">
  <div className={'ws-board'+(!animations?' reduced':'')} role="grid" ref={board}
   aria-label={language==='de'?'Wortsuchraster':'Word search grid'} data-testid="word-search-board"
   style={{gridTemplateColumns:'repeat('+size+',minmax(0,1fr))'}}
   onPointerDown={down} onPointerMove={move} onPointerUp={up}
   onPointerCancel={()=>{dragRef.current=false;setTarget(anchorRef.current);}}>
   {Array.from({length:size*size},(_,i)=>{
    const row=Math.floor(i/size),column=i%size;
    return <button key={i} type="button" role="gridcell"
      data-testid={'word-search-cell-'+row+'-'+column} disabled={pending||game.completed}
      className={'ws-cell'+(found.has(i)?' found':'')+(preview.has(i)?' selected':'')+
       (game.hint?.startRow===row&&game.hint?.startColumn===column?' hint':'')}
      aria-label={(language==='de'?'Zeile ':'Row ')+(row+1)+(language==='de'?' Spalte ':' column ')+(column+1)+' '+game.grid[i]}
      onClick={e=>{if(e.detail===0)choose({row,column});}}>{game.grid[i]}</button>;
   })}
  </div>
  <p className="ws-help">{words[language].first}</p>
 </div>;
}
export function WordSearchScreen({state,language,pending,error,command,animations}:Props){
 const t=words[language],game=state.game;
 const [clock,setClock]=useState(0);
 useEffect(()=>{if(!game||game.completed)return;
  const timer=window.setInterval(()=>setClock(v=>v+1),1000);return()=>window.clearInterval(timer);
 },[game?.id,game?.completed]);
 useEffect(()=>setClock(0),[game?.id,game?.elapsedSeconds]);
 if(!game)return <section className="ws-library" data-testid="word-search-library">
  <div className="ws-hero"><span className="ws-eyebrow">{t.eyebrow}</span>
   <h1>{t.title}</h1><p>{t.intro}</p>
   <div className="ws-stats">
    <span><b>{state.completedTotal}/{state.catalogTotal}</b>{t.solved}</span>
    <span><b>{state.themeTotal}</b>{t.themes}</span>
    <span><b>{state.stats.wordsFound.toLocaleString(language)}</b>{t.words}</span>
   </div>
  </div>
  {state.resume&&<div className="ws-resume">
   <div><span className="ws-eyebrow">{t.resume}</span><h2>{state.resume.theme}</h2>
    <p>{names[language][state.resume.difficulty]} · {state.resume.found}/{state.resume.total} {t.found.toLowerCase()} · {formatTime(state.resume.elapsedSeconds)}</p></div>
   <button data-testid="word-search-resume" disabled={pending}
    onClick={()=>command({action:'word-search.resume'})}>{t.resume} →</button>
  </div>}
  <div className="ws-library-heading"><h2>{t.library}</h2><small>{t.levels}</small></div>
  <div className="ws-levels">
   {state.difficulties.map((d,i)=><article className="ws-level" key={d.difficulty}>
    <div className="ws-level-top"><span>0{i+1} / 06</span><span>+{d.rewardXp} XP</span></div>
    <span className="ws-decoration" aria-hidden="true">{['◇','✦','♣','♛','❖','✶'][i]}</span>
    <h3>{names[language][d.difficulty]}</h3><p>{d.size} × {d.size} · {d.wordCount} {t.total}</p>
    <small>{i===0?t.dirRelaxed:i===1?t.dirEasy:t.dirHard}</small>
    <div className="ws-progress"><div style={{width:(d.completed/d.total*100)+'%'}}/></div>
    <span className="ws-progress-text">{d.completed}/{d.total} {t.solved.toLowerCase()}</span>
    <button data-testid={'word-search-start-'+d.difficulty} disabled={pending||!!state.resume}
     onClick={()=>command({action:'word-search.start',difficulty:d.difficulty})}>
     {d.completed>=d.total?t.replay:t.play}</button>
   </article>)}
  </div>
  <p className="ws-expansion">{t.newWords}</p>
  {state.resume&&<p className="ws-help">{t.required}</p>}
  {error&&<p className="ws-alert" role="alert">{error}</p>}
 </section>;
 const found=new Set(game.found);
 return <section className="ws-game" data-testid="modern-word-search-game" data-puzzle-id={game.id}>
   <div className="ws-game-heading">
    <div><span className="ws-eyebrow">{t.eyebrow} · {names[language][game.difficulty]}</span>
     <h1>{game.theme}</h1><p>{t.first}</p></div>
    <div className="ws-game-stats">
     <span><small>{t.found}</small><b data-testid="word-search-found">{game.found.length}/{game.words.length}</b></span>
     <span><small>{t.time}</small><b>{formatTime(game.elapsedSeconds+(game.completed?0:clock))}</b></span>
     <span><small>{t.hints}</small><b data-testid="word-search-hints">{game.hintsUsed}</b></span>
    </div>
   </div>
   <div className="ws-columns">
    <Board game={game} language={language} pending={pending} command={command} animations={animations}/>
    <aside className="ws-side">
     <div className="ws-word-heading"><span className="ws-eyebrow">{t.wordList}</span>
       <b>{game.found.length}/{game.words.length}</b></div>
     <div className="ws-progress"><div style={{width:(game.found.length/game.words.length*100)+'%'}}/></div>
     <div className="ws-words" data-testid="word-search-words">
      {game.words.map(word=><span key={word} className={found.has(word)?'found':''}>
        <b>{found.has(word)?'✓':'○'}</b> {word}</span>)}
     </div>
     <button className="ws-hint-button" data-testid="word-search-hint" disabled={pending||game.completed}
      onClick={()=>command({action:'word-search.hint'})}>{t.hint}</button>
     <p className="ws-note">{t.hintInfo}</p>
     <div className="ws-status" role="status">{error||state.notice}</div>
     {pending&&<small>{t.pending}</small>}<span className="ws-saved">{t.saved}</span>
     <button className="ws-back" data-testid="word-search-back" disabled={pending}
      onClick={()=>command({action:'word-search.back'})}>{t.back}</button>
    </aside>
   </div>
   {game.completed&&<div className="ws-complete" data-testid="word-search-completion" role="status">
    <h2>✦ {t.complete} ✦</h2><p>{t.finished}</p>
    <div><span><b>{game.rewardXp}</b>{t.xp}</span><span><b>{Math.floor(game.rewardXp/2)}</b>{t.season}</span></div>
    <p>{state.rewardNotice}</p>
    <button onClick={()=>command({action:'word-search.back'})}>{t.back}</button>
   </div>}
   <p className="ws-note ws-bottom-note">{t.noRush}</p>
 </section>;
}
