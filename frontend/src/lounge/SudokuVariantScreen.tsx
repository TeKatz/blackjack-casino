import {useEffect,useMemo,useRef,useState} from 'react';
import type {CSSProperties} from 'react';
import type {VariantGame,SudokuIntent} from './sudokuBridge';
import './sudokuVariant.css';

type Props={game:VariantGame;language:'de'|'en';pending:boolean;error:string;command:(intent:SudokuIntent)=>void};
const titles:Record<VariantGame['mode'],string>={x:'Sudoku X',killer:'Killer Sudoku',jigsaw:'Jigsaw Sudoku',samurai:'Samurai Sudoku',mini:'Mini Sudoku'};
function time(s:number){return String(Math.floor(s/60)).padStart(2,'0')+':'+String(s%60).padStart(2,'0');}
export function SudokuVariantScreen({game,language,pending,error,command}:Props){
 const de=language==='de',size=game.width;
 const [selected,setSelected]=useState(-1),[pencil,setPencil]=useState(false),[zoom,setZoom]=useState(1),[clock,setClock]=useState(0);
 const updated=useRef(Date.now());
 useEffect(()=>{setSelected(-1);setPencil(false);setZoom(1);setClock(0);updated.current=Date.now();},[game.id]);
 useEffect(()=>{updated.current=Date.now();setClock(0);},[game.elapsedSeconds]);
 useEffect(()=>{const id=window.setInterval(()=>setClock(Math.floor((Date.now()-updated.current)/1000)),1000);return()=>window.clearInterval(id);},[]);
 const wrong=useMemo(()=>new Set(game.incorrect),[game.incorrect]),conflicts=useMemo(()=>new Set(game.conflicts),[game.conflicts]);
 const cageMap=useMemo(()=>{const ids=Array<number>(game.board.length).fill(-1);game.cages.forEach((c,j)=>c.cells.forEach(i=>ids[i]=j));return ids;},[game.cages,game.board.length]);
 const cageLabels=useMemo(()=>new Map(game.cages.map(c=>[Math.min(...c.cells),c.sum])),[game.cages]);
 const editable=selected>=0&&game.givens[selected]==='0'&&!game.completed;
 const send=(digit:number)=>{if(editable&&!pending)command({action:pencil?'sudoku.variant.note':'sudoku.variant.value',index:selected,digit});};
 const erase=()=>{if(editable&&!pending)command({action:'sudoku.variant.erase',index:selected});};
 useEffect(()=>{
  const handle=(e:KeyboardEvent)=>{
   if(pending||game.completed||e.altKey||e.metaKey)return;
   if(e.target instanceof HTMLElement&&e.target.closest('input,textarea,select,[contenteditable=true]'))return;
   if(e.ctrlKey&&(e.key==='z'||e.key==='y')){e.preventDefault();command({action:e.key==='z'?'sudoku.variant.undo':'sudoku.variant.redo'});return;}
   if(e.ctrlKey)return;
   if(e.key===' '){e.preventDefault();setPencil(v=>!v);return;}
   if((e.key==='Backspace'||e.key==='Delete'||e.key==='0')&&editable){e.preventDefault();erase();return;}
   if(/^[1-9]$/.test(e.key)&&Number(e.key)<=game.digits){e.preventDefault();send(Number(e.key));return;}
   const directions:Record<string,[number,number]>={ArrowUp:[-1,0],ArrowDown:[1,0],ArrowLeft:[0,-1],ArrowRight:[0,1]};
   const d=directions[e.key];if(!d||selected<0)return;
   e.preventDefault();let r=Math.floor(selected/size),c=selected%size;
   for(let step=0;step<size;step++){r+=d[0];c+=d[1];if(r<0||c<0||r>=size||c>=size)break;if(game.board[r*size+c]!=='-'){setSelected(r*size+c);break;}}
  };
  window.addEventListener('keydown',handle);return()=>window.removeEventListener('keydown',handle);
 },[game,selected,pencil,editable,pending,command]);
 const px=game.mode==='samurai'?Math.round(25*zoom):game.mode==='mini'?(game.digits===4?68:56):52;
 // The desktop board fills the available height; cap Mini boards to preserve large, legible cells.
 // CSS owns viewport sizing, while zoom remains opt-in for Samurai.
 const maxBoard=game.mode==='mini'?(game.digits===4?'640px':'720px'):'790px';
 const boardStyle={'--variant-cell-px':px+'px','--variant-width':size,
  '--variant-board-cap':maxBoard,zoom:game.mode==='samurai'?zoom:1} as CSSProperties;
 return <section className="sudoku-variant" data-testid="sudoku-variant-game" data-mode={game.mode}>
  <header className="variant-heading">
   <div><span className="sudoku-eyebrow">THE MIDNIGHT CLUB / SUDOKU LOUNGE</span>
    <h1>{titles[game.mode]}{game.mode==='mini'?' · '+game.miniSize+'×'+game.miniSize:''}</h1>
    <p>{de?'Schwierigkeit':'Difficulty'}: {game.difficulty}</p></div>
   <div className="variant-timers"><span>{de?'ZEIT':'TIME'} <b>{time(game.elapsedSeconds+(game.completed?0:clock))}</b></span><span>{de?'HINWEISE':'HINTS'} <b>{game.hintsUsed}</b></span></div>
  </header>
  <div className="variant-layout">
   <div className="variant-board-panel">
    {game.mode==='samurai'&&<div className="variant-zoom"><span>Samurai · Zoom</span>
     <button onClick={()=>setZoom(z=>Math.max(.7,Math.round((z-.1)*10)/10))} disabled={zoom<=.7}>−</button>
     <b>{Math.round(zoom*100)}%</b>
     <button onClick={()=>setZoom(z=>Math.min(1.5,Math.round((z+.1)*10)/10))} disabled={zoom>=1.5}>+</button></div>}
    <div className="variant-scroll"><div className={'variant-grid variant-'+game.mode} style={boardStyle} data-digits={game.digits} role="grid" aria-label={titles[game.mode]}>
     {Array.from({length:game.board.length},(_,i)=>{
      if(game.board[i]==='-')return <span className="variant-void" key={i} aria-hidden="true"/>;
      const row=Math.floor(i/size),col=i%size,reg=game.regions[i],cage=cageMap[i];
      const edges=(map:number[],highlightRegions=false)=>{
       const top=row===0||map[i-size]!==map[i];
       const right=col===size-1||map[i+1]!==map[i];
       const bottom=row===size-1||map[i+size]!==map[i];
       const left=col===0||map[i-1]!==map[i];
       if(highlightRegions){
        // Draw each shared edge once (top/left) to keep irregular regions crisp,
        // instead of overlapping two thin, indistinguishable block borders.
        const accent='#dfa8ff',internal='#355960';
        return {
         borderTopWidth:top?'4px':'1px',borderTopColor:top?accent:internal,
         borderLeftWidth:left?'4px':'1px',borderLeftColor:left?accent:internal,
         borderRightWidth:col===size-1?'4px':'0px',borderRightColor:accent,
         borderBottomWidth:row===size-1?'4px':'0px',borderBottomColor:accent,
        } as CSSProperties;
       }
       return {
        borderTopWidth:top?'2px':'1px',
        borderRightWidth:right?'2px':'1px',
        borderBottomWidth:bottom?'2px':'1px',
        borderLeftWidth:left?'2px':'1px',
       } as CSSProperties;
      };
      const cls=['variant-cell',game.givens[i]!=='0'?'given':'',selected===i?'selected':'',
       wrong.has(i)?'incorrect':'',conflicts.has(i)?'conflict':'',
       game.mode==='x'&&(row===col||row+col===8)?'diagonal':''].filter(Boolean).join(' ');
      const cageEdges=cage>=0?edges(cageMap):null;
      return <button type="button" role="gridcell" key={i} data-testid={'variant-cell-'+i}
       aria-selected={selected===i} aria-label={'Row '+(row+1)+', column '+(col+1)}
       className={cls} style={edges(game.regions,game.mode==='jigsaw')} onClick={()=>setSelected(i)}
       onContextMenu={e=>{e.preventDefault();setSelected(i);setPencil(true);}}>
       {game.mode==='killer'&&cageEdges&&<span className="variant-cage-outline" style={cageEdges} aria-hidden="true"/>}
       {game.mode==='killer'&&cageLabels.has(i)&&<span className="variant-cage-label">{cageLabels.get(i)}</span>}
       {game.board[i]!=='0'?game.board[i]:<span className="variant-notes"
        style={{gridTemplateColumns:'repeat('+(game.digits===4?2:3)+',1fr)'}}>
        {Array.from({length:game.digits},(_,d)=><span key={d}>{game.notes[i]&(1<<(d+1))?d+1:''}</span>)}</span>}
      </button>;
     })}
    </div></div>
    <p className="variant-rule">{game.mode==='killer'?(de?'Jeder gestrichelte Käfig muss seine Summe ergeben, ohne doppelte Zahlen.':'Each dashed cage must total its sum without duplicate numbers.'):
     game.mode==='x'?(de?'Beide Diagonalen enthalten jede Zahl von 1 bis 9 einmal.':'Each diagonal contains all digits 1 to 9.'):
     game.mode==='jigsaw'?(de?'Die unregelmäßigen Gebiete enthalten jede Zahl genau einmal.':'Each irregular region contains every digit exactly once.'):
     game.mode==='samurai'?(de?'Fünf verbundene 9×9-Raster: gemeinsame Felder gelten in beiden.':'Five linked 9×9 grids: shared cells count for both.'):
     (de?'Fülle jede Zeile, Spalte und jeden Block mit allen Zahlen.':'Fill each row, column and block with every digit.')}</p>
   </div>
   <aside className="variant-controls">
    <span className="sudoku-eyebrow">{de?'ZAHL WÄHLEN':'CHOOSE NUMBER'}</span>
    <div className="variant-number-pad">{Array.from({length:game.digits},(_,d)=>
     <button key={d} disabled={!editable||pending} onClick={()=>send(d+1)}>{d+1}</button>)}</div>
    <div className="variant-actions">
     <button className={pencil?'active':''} onClick={()=>setPencil(v=>!v)}>{de?'NOTIZEN':'PENCIL'} {pencil?'✓':''}</button>
     <button disabled={!editable||pending} onClick={erase}>{de?'LÖSCHEN':'ERASE'}</button>
     <button disabled={!game.canUndo||pending} onClick={()=>command({action:'sudoku.variant.undo'})}>↶ {de?'ZURÜCK':'UNDO'}</button>
     <button disabled={!game.canRedo||pending} onClick={()=>command({action:'sudoku.variant.redo'})}>↷ {de?'VOR':'REDO'}</button>
    </div>
    <button className="variant-hint" disabled={pending||game.completed}
     onClick={()=>command({action:'sudoku.variant.hint',index:selected})}>{de?'EIN FELD AUFDECKEN':'REVEAL ONE CELL'}</button>
    <p className="variant-notice" role="status">{error||game.notice}</p>
    {game.completed&&<div className="variant-victory" data-testid="variant-completion" role="status">
     <span>✦ {de?'RÄTSEL GELÖST':'PUZZLE SOLVED'} ✦</span><h2>{titles[game.mode]}</h2>
     <p>+{game.completionXp} {de?'Spieler-XP':'Player XP'} · +{game.careerXp} Sudoku XP</p>
     <small>{game.rewardNotice}</small></div>}
    <p className="variant-help">{de?'Pfeiltasten: Auswahl · 1–9: Eingabe · Leertaste: Notizen':'Arrows: select · 1–9: input · Space: notes'}</p>
    <button className="variant-return" data-testid="variant-back" disabled={pending}
     onClick={()=>command({action:'sudoku.variant.back'})}>← {de?'Zur Varianten-Auswahl':'Back to variant selection'}</button>
   </aside>
  </div>
 </section>;
}
