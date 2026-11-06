import {useEffect,useMemo,useRef,useState} from 'react';
import type {Difficulty,SudokuGame,SudokuIntent,SudokuState} from './sudokuBridge';
import {difficulties} from './sudokuBridge';
import './sudoku.css';

type Lang='en'|'de';
type Props={state:SudokuState;language:Lang;pending:boolean;error:string;command:(intent:SudokuIntent)=>void};
const names:Record<Lang,Record<Difficulty,string>>={
 en:{Relaxed:'Relaxed',Easy:'Easy',Medium:'Medium',Hard:'Hard',Expert:'Expert',Master:'Master'},
 de:{Relaxed:'Entspannt',Easy:'Leicht',Medium:'Mittel',Hard:'Schwer',Expert:'Experte',Master:'Meister'},
};
const text={
 en:{eyebrow:'SUDOKU · THE MIDNIGHT CLUB',title:'Make room for a little focus.',
  description:'Six difficulty levels, thousands of offline puzzles, one quiet decision at a time.',
  saved:'Puzzles solved',library:'Choose your puzzle',new:'NEW PUZZLE',resume:'CONTINUE',
  noSaved:'No saved puzzle',inProgress:'In progress',solved:'Solved',daily:'DAILY SUDOKU',
  dailyDesc:'A fresh challenge assigned for your local calendar day. Resume it without losing your board.',
  startDaily:'OPEN TODAY’S PUZZLE',doneDaily:'Today’s puzzle complete',timer:'SHOW TIMER',
  puzzles:'Puzzles available',time:'TIME',hints:'HINTS USED',number:'CHOOSE A NUMBER',
  pencil:'PENCIL MODE',erase:'ERASE',undo:'UNDO',redo:'REDO',guides:'HIGHLIGHT GUIDES',
  hint:'REVEAL ONE CELL',back:'← PUZZLE LIBRARY',complete:'PUZZLE COMPLETE',
  completionDetail:'Your completion was saved with your existing casino profile.',
  playerXp:'PLAYER XP',seasonXp:'SEASON XP',noRush:'Time and hints never reduce your rewards.',
  shortcut:'Arrow keys: move · 1–9: enter · Space: pencil · G: guides · Ctrl+Z / Ctrl+Y: undo / redo',
  confirm:'Replace your unfinished puzzle?',confirmDescription:'Starting a new puzzle discards this difficulty’s unfinished board. Other difficulties and Daily Sudoku stay saved.',
  cancel:'KEEP PLAYING',replace:'START NEW',waiting:'Saving your move…',chosen:'Selected cell',
  error:'That number does not belong in this cell.',saves:'Saved locally',
  board:'Sudoku board',newStart:'New puzzle'},
 de:{eyebrow:'SUDOKU · THE MIDNIGHT CLUB',title:'Ein wenig Ruhe für den Kopf.',
  description:'Sechs Schwierigkeitsgrade, Tausende Offline-Rätsel und ein Schritt nach dem anderen.',
  saved:'Gelöste Rätsel',library:'Wähle dein Rätsel',new:'NEUES RÄTSEL',resume:'FORTSETZEN',
  noSaved:'Kein Spielstand',inProgress:'Begonnen',solved:'Gelöst',daily:'TAGES-SUDOKU',
  dailyDesc:'Ein festes Rätsel für deinen heutigen Kalendertag. Du kannst es jederzeit fortsetzen.',
  startDaily:'HEUTIGES RÄTSEL ÖFFNEN',doneDaily:'Heutiges Rätsel gelöst',timer:'TIMER ANZEIGEN',
  puzzles:'Rätsel verfügbar',time:'ZEIT',hints:'HINWEISE',number:'ZAHL WÄHLEN',
  pencil:'NOTIZMODUS',erase:'LÖSCHEN',undo:'RÜCKGÄNGIG',redo:'WIEDERHOLEN',guides:'MARKIERUNGSHILFEN',
  hint:'EIN FELD AUFDECKEN',back:'← RÄTSELAUSWAHL',complete:'RÄTSEL GELÖST',
  completionDetail:'Deine Belohnung wurde mit deinem Casino-Profil gespeichert.',
  playerXp:'SPIELER-XP',seasonXp:'SAISON-XP',noRush:'Zeit und Hinweise verringern deine Belohnung nicht.',
  shortcut:'Pfeiltasten: bewegen · 1–9: Zahl · Leertaste: Notizen · G: Hilfen · Strg+Z / Strg+Y: zurück / vor',
  confirm:'Unfertiges Rätsel ersetzen?',confirmDescription:'Das begonnene Rätsel dieses Schwierigkeitsgrades wird verworfen. Andere Rätsel und das Tages-Sudoku bleiben gespeichert.',
  cancel:'WEITERSPIELEN',replace:'NEUES STARTEN',waiting:'Spielstand wird gespeichert…',chosen:'Ausgewähltes Feld',
  error:'Diese Zahl gehört nicht in das Feld.',saves:'Lokal gespeichert',
  board:'Sudoku-Spielfeld',newStart:'Neues Rätsel'},
};
const formatTime=(seconds:number)=>{
 const h=Math.floor(seconds/3600),m=Math.floor(seconds%3600/60),s=seconds%60;
 const pad=(n:number)=>String(n).padStart(2,'0');
 return h>0?pad(h)+':'+pad(m)+':'+pad(s):pad(m)+':'+pad(s);
};
const isRelated=(a:number,b:number)=>Math.floor(a/9)===Math.floor(b/9)
 ||a%9===b%9||(Math.floor(a/27)===Math.floor(b/27)&&Math.floor(a%9/3)===Math.floor(b%9/3));
export function SudokuScreen({state,language,pending,error,command}:Props) {
 const t=text[language];
 const [selected,setSelected]=useState(-1);
 const [pencil,setPencil]=useState(false);
 const [confirm,setConfirm]=useState<Difficulty|null>(null);
 const [clock,setClock]=useState(0);
 const [lastUpdate,setLastUpdate]=useState(Date.now());
 const game=state.game;
 const boardRef=useRef<HTMLDivElement>(null);
 useEffect(()=>{setSelected(-1);setPencil(false);setClock(0);},[game?.puzzleId]);
 useEffect(()=>{setLastUpdate(Date.now());setClock(0);},[game?.elapsedSeconds,game?.puzzleId]);
 useEffect(()=>{
   const id=window.setInterval(()=>setClock(Math.floor((Date.now()-lastUpdate)/1000)),1000);
   return()=>window.clearInterval(id);
 },[lastUpdate]);
 const bad=useMemo(()=>new Set(game?.incorrect??[]),[game?.incorrect]);
 const conflicts=useMemo(()=>new Set(game?.conflicts??[]),[game?.conflicts]);
 const completeDigits=useMemo(()=>new Set(game?.completedDigits??[]),[game?.completedDigits]);
 const selectedValue=selected>=0?Number(game?.board[selected]??'0'):0;
 const isEditable=!!game&&selected>=0&&game.givens[selected]==='0'&&!game.completed;
 function enterDigit(digit:number) {
   if(!isEditable||pending||completeDigits.has(digit))return;
   command({action:pencil?'sudoku.note':'sudoku.value',index:selected,digit});
 }
 function edit(action:'sudoku.erase'|'sudoku.hint') {
   if(!game||pending)return;
   if(action==='sudoku.erase'&&!isEditable)return;
   command({action,index:action==='sudoku.hint'?selected:-1});
 }
 useEffect(()=>{
   if(!game)return;
   const key=(e:KeyboardEvent)=>{
     if(confirm||pending||e.altKey||e.metaKey)return;
     const target=e.target;
     if(target instanceof HTMLElement&&target.closest('input,textarea,select,[contenteditable=true]'))return;
     const ctrl=e.ctrlKey;
     if(ctrl&&e.key.toLowerCase()==='z'){e.preventDefault();if(game.canUndo)command({action:'sudoku.undo'});return;}
     if(ctrl&&e.key.toLowerCase()==='y'){e.preventDefault();if(game.canRedo)command({action:'sudoku.redo'});return;}
     if(ctrl)return;
     if(e.key===' '){e.preventDefault();setPencil(v=>!v);return;}
     if(e.key.toLowerCase()==='g'){e.preventDefault();command({action:'sudoku.guides',value:state.showGuides?'false':'true'});return;}
     if(e.key==='Backspace'||e.key==='Delete'||e.key==='0'){e.preventDefault();edit('sudoku.erase');return;}
     if(/^[1-9]$/.test(e.key)){e.preventDefault();enterDigit(Number(e.key));return;}
     const direction:Record<string,[number,number]>={ArrowUp:[-1,0],ArrowDown:[1,0],ArrowLeft:[0,-1],ArrowRight:[0,1]};
     const step=direction[e.key];
     if(step){e.preventDefault();const n=Math.max(0,selected);
       const row=Math.max(0,Math.min(8,Math.floor(n/9)+step[0]));
       const col=Math.max(0,Math.min(8,n%9+step[1]));setSelected(row*9+col);boardRef.current?.focus();}
   };
   window.addEventListener('keydown',key);
   return()=>window.removeEventListener('keydown',key);
 },[game,selected,pencil,pending,confirm,state.showGuides,command]);
 function newGame(difficulty:Difficulty,canResume:boolean){
   if(pending)return;
   if(canResume){setConfirm(difficulty);return;}
   command({action:'sudoku.new',value:difficulty});
 }
 if(!game) return <section className="sudoku-library" data-testid="sudoku-library">
   <div className="sudoku-banner">
     <span className="sudoku-eyebrow">{t.eyebrow}</span>
     <h1>{t.title}</h1><p>{t.description}</p>
     <div className="sudoku-stats"><span><b>{state.normalCompleted.toLocaleString(language)}</b>{t.saved}</span>
       <span><b>{state.normalTotal.toLocaleString(language)}</b>{t.puzzles}</span>
       <span><b>{state.dailyCompleted.toLocaleString(language)}</b>{t.daily}</span></div>
   </div>
   <div className="sudoku-library-heading"><h2>{t.library}</h2>
     <button className={'sudoku-toggle '+(state.showTimer?'active':'')}
       data-testid="sudoku-timer-toggle" disabled={pending}
       onClick={()=>command({action:'sudoku.timer',value:state.showTimer?'false':'true'})}>
       {t.timer}: {state.showTimer?'ON':'OFF'}</button></div>
   <div className="sudoku-difficulties">
    {state.entries.map((entry,index)=><article className="sudoku-difficulty" key={entry.id}>
      <div className="sudoku-tile-symbol" aria-hidden="true">{['◇','♠','✦','♜','☾','✧'][index]}</div>
      <span className="sudoku-level">{String(index+1).padStart(2,'0')} / 06</span>
      <h3>{names[language][entry.id]}</h3>
      <p>{entry.canResume?t.inProgress+' · '+formatTime(entry.elapsedSeconds):t.noSaved}</p>
      <span className="sudoku-solved-count">{entry.completed} {t.solved.toLowerCase()}</span>
      <div className="sudoku-tile-actions">
        <button data-testid={'sudoku-resume-'+entry.id} disabled={!entry.canResume||pending}
          onClick={()=>command({action:'sudoku.resume',value:entry.id})}>{t.resume}</button>
        <button className="primary" data-testid={'sudoku-new-'+entry.id} disabled={pending}
          onClick={()=>newGame(entry.id,entry.canResume)}>{t.new}</button>
      </div>
     </article>)}
   </div>
   <div className="sudoku-daily-card">
     <div><span className="sudoku-eyebrow">{state.daily.date}</span>
      <h2>{t.daily}</h2><p>{t.dailyDesc}</p>
      <small>{names[language][state.daily.difficulty]} · {state.daily.completed?t.doneDaily:t.inProgress}</small></div>
     <button data-testid="sudoku-daily" disabled={pending}
       onClick={()=>command({action:'sudoku.daily'})}>{t.startDaily} →</button>
   </div>
   {error&&<p className="sudoku-alert" role="alert">{error}</p>}
   {confirm&&<div className="sudoku-dialog" role="dialog" aria-modal="true" aria-labelledby="sudoku-confirm-title">
     <div><h2 id="sudoku-confirm-title">{t.confirm}</h2><p>{t.confirmDescription}</p>
      <div className="sudoku-dialog-actions">
       <button onClick={()=>setConfirm(null)}>{t.cancel}</button>
       <button className="primary" data-testid="sudoku-confirm-new"
         onClick={()=>{const d=confirm;setConfirm(null);command({action:'sudoku.new',value:d});}}>{t.replace}</button>
      </div></div></div>}
  </section>;
 const elapsed=game.elapsedSeconds+(game.completed?0:clock);
 return <section className="sudoku-play" data-testid="modern-sudoku-game" data-puzzle-id={game.puzzleId}>
   <div className="sudoku-play-heading">
     <div><span className="sudoku-eyebrow">{game.isDaily?t.daily:t.eyebrow}</span>
       <h1>{names[language][game.difficulty]}</h1></div>
     <div className="sudoku-play-status">
      {state.showTimer&&<span><small>{t.time}</small><b data-testid="sudoku-elapsed">{formatTime(elapsed)}</b></span>}
      <span><small>{t.hints}</small><b data-testid="sudoku-hints">{game.hintsUsed}</b></span>
      <span className="sudoku-saved">{t.saves}</span>
     </div>
   </div>
   <div className="sudoku-play-columns">
    <div className="sudoku-board-panel">
      <div className="sudoku-board" role="grid" aria-label={t.board} tabIndex={0} ref={boardRef}>
       {Array.from({length:81},(_,index)=>{
        const val=Number(game.board[index]),given=game.givens[index]!=='0';
        const isSelected=selected===index;
        const classes=['sudoku-cell',given?'given':'','',
         bad.has(index)||conflicts.has(index)?'incorrect':'',
         isSelected?'selected':(val!==0&&val===selectedValue?'matching':
           state.showGuides&&selected>=0&&isRelated(index,selected)?'related':'')].filter(Boolean).join(' ');
        return <button key={index} data-testid={'sudoku-cell-'+index} className={classes} role="gridcell"
          aria-selected={isSelected}
          aria-label={(language==='de'?'Zeile ':'Row ')+(Math.floor(index/9)+1)+
            (language==='de'?', Spalte ':', column ')+(index%9+1)+
            (given?(language==='de'?', vorgegeben ':', given '):', ')+(val||'empty')}
          style={{borderTopWidth:index<9||Math.floor(index/9)%3===0?3:1,
            borderLeftWidth:index%9===0||index%3===0?3:1,
            borderRightWidth:index%9===8?3:0,borderBottomWidth:index>=72?3:0}}
          onClick={()=>{setSelected(index);boardRef.current?.focus();}}>
          {val?String(val):<span className="sudoku-notes" aria-hidden="true">
            {Array.from({length:9},(_,i)=><span key={i}>{game.notes[index]&(1<<(i+1))?i+1:''}</span>)}
           </span>}
         </button>;
       })}
      </div>
      <p className="sudoku-help">{t.shortcut}</p>
    </div>
    <div className="sudoku-actions">
      <h2>{t.number}</h2>
      <div className="sudoku-numbers">
        {Array.from({length:9},(_,i)=><button key={i+1}
          data-testid={'sudoku-digit-'+(i+1)}
          className={selectedValue===i+1?'matching':''}
          disabled={!isEditable||pending||completeDigits.has(i+1)}
          onClick={()=>enterDigit(i+1)}>{i+1}</button>)}
      </div>
      <div className="sudoku-action-row">
        <button data-testid="sudoku-pencil" className={pencil?'active':''} aria-pressed={pencil}
          disabled={game.completed} onClick={()=>setPencil(v=>!v)}>{t.pencil}</button>
        <button data-testid="sudoku-erase" disabled={!isEditable||pending}
          onClick={()=>edit('sudoku.erase')}>{t.erase}</button>
      </div>
      <div className="sudoku-action-row">
        <button data-testid="sudoku-undo" disabled={!game.canUndo||pending}
          onClick={()=>command({action:'sudoku.undo'})}>{t.undo}</button>
        <button data-testid="sudoku-redo" disabled={!game.canRedo||pending}
          onClick={()=>command({action:'sudoku.redo'})}>{t.redo}</button>
      </div>
      <button data-testid="sudoku-guides" className={'sudoku-wide '+(state.showGuides?'active':'')}
        disabled={pending}
        onClick={()=>command({action:'sudoku.guides',value:state.showGuides?'false':'true'})}>
        {t.guides} · {state.showGuides?'ON':'OFF'}</button>
      <button data-testid="sudoku-hint" className="sudoku-wide sudoku-hint" disabled={pending||game.completed}
        onClick={()=>edit('sudoku.hint')}>{t.hint}</button>
      <p className="sudoku-game-notice" role="status">{error||state.notice}</p>
      {pending&&<small className="sudoku-pending">{t.waiting}</small>}
      <p className="sudoku-no-rush">{t.noRush}</p>
      {game.completed&&<div className="sudoku-completion" role="status" data-testid="sudoku-completion">
       <h2>{t.complete}</h2><p>{t.completionDetail}</p>
       <div><span><strong>{game.completionXp}</strong>{t.playerXp}</span>
            <span><strong>{game.completionSeasonXp}</strong>{t.seasonXp}</span></div>
       {state.rewardNotice&&<p>{state.rewardNotice}</p>}
      </div>}
      <button className="sudoku-back" data-testid="sudoku-back" disabled={pending}
        onClick={()=>command({action:'sudoku.back'})}>{t.back}</button>
    </div>
   </div>
  </section>;
}
