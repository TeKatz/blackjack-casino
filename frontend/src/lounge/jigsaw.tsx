import {useEffect,useMemo,useRef,useState} from 'react';
import type {PointerEvent as ReactPointerEvent,MouseEvent as ReactMouseEvent} from 'react';
import {Application,Assets,Container,Graphics,Rectangle,Sprite,Texture} from 'pixi.js';
import type {JigsawGame,JigsawIntent,JigsawPiece,JigsawState} from './jigsawBridge';
import './jigsaw.css';

type Lang='en'|'de';
type Props={state:JigsawState;language:Lang;pending:boolean;error:string;animations:boolean;
 command:(intent:JigsawIntent)=>void};
type View={zoom:number;x:number;y:number};
const images=(id:string)=>'/album/atlas/'+id+'.png';
const fmt=(s:number)=>[Math.floor(s/3600),Math.floor(s%3600/60),s%60]
 .map(n=>String(n).padStart(2,'0')).join(':');
const copy={
 en:{title:'The Puzzle Studio',intro:'Twelve scenes. Eight sizes. No pressure.',
  completed:'COMPLETED',pieces:'PIECES PLACED',largest:'LARGEST PUZZLE',achievements:'ACHIEVEMENTS',
  motif:'CHOOSE A SCENE',size:'SELECT PIECE COUNT',start:'START PUZZLE',
  replace:'START A NEW PUZZLE',resume:'RESUME SAVED PUZZLE',ask:'Discard the unfinished puzzle?',
  back:'← PUZZLE MENU',timer:'TIMER',on:'ON',off:'OFF',reference:'REFERENCE',
  reset:'RESET VIEW',zoomIn:'ZOOM +',zoomOut:'ZOOM −',tray:'PIECE TRAY',page:'PAGE',
  return:'RETURN SELECTED PIECE',clear:'CLEAR SELECTION',saved:'Moves save automatically.',
  guide:'Select a piece in the tray, then click the board. Drag placed pieces to connect matching edges; right-click returns a loose group to the tray.',
  result:'THE FINISHED PICTURE',finish:'Puzzle completed and saved.',
  another:'ANOTHER PUZZLE',none:'No time tracked',left:'pieces remaining'},
 de:{title:'Das Puzzle-Atelier',intro:'Zwölf Motive. Acht Größen. Kein Zeitdruck.',
  completed:'ABGESCHLOSSEN',pieces:'TEILE GELEGT',largest:'GRÖSSTES PUZZLE',achievements:'ERFOLGE',
  motif:'MOTIV WÄHLEN',size:'TEILEANZAHL WÄHLEN',start:'PUZZLE STARTEN',
  replace:'NEUES PUZZLE STARTEN',resume:'PUZZLE FORTSETZEN',ask:'Das unfertige Puzzle verwerfen?',
  back:'← PUZZLE-MENÜ',timer:'TIMER',on:'AN',off:'AUS',reference:'VORLAGE',
  reset:'ANSICHT',zoomIn:'ZOOM +',zoomOut:'ZOOM −',tray:'TEILEABLAGE',page:'SEITE',
  return:'AUSGEWÄHLTES TEIL ZURÜCKLEGEN',clear:'AUSWAHL AUFHEBEN',saved:'Züge werden automatisch gespeichert.',
  guide:'Wähle ein Teil aus der Ablage und klicke auf das Brett. Ziehe gelegte Teile zu passenden Kanten; ein Rechtsklick legt lose Gruppen zurück.',
  result:'DAS FERTIGE BILD',finish:'Puzzle abgeschlossen und gespeichert.',
  another:'WEITERES PUZZLE',none:'Ohne Zeitmessung',left:'Teile übrig'},
};
function connected(game:JigsawGame,id:number):number[]{
 if(!game.pieces[id].onBoard)return [];
 const found=new Set<number>([id]),queue=[id],w=960/game.columns,h=600/game.rows;
 for(let i=0;i<queue.length;i++){
  const p=queue[i],c=p%game.columns,r=Math.floor(p/game.columns);
  for(const n of [c>0?p-1:-1,c+1<game.columns?p+1:-1,r>0?p-game.columns:-1,
   r+1<game.rows?p+game.columns:-1]){
   if(n<0||found.has(n))continue;
   const a=game.pieces[p],b=game.pieces[n];
   if(b.onBoard&&Math.abs((b.x-a.x)-(n%game.columns-c)*w)<.02
      &&Math.abs((b.y-a.y)-(Math.floor(n/game.columns)-r)*h)<.02){
    found.add(n);queue.push(n);
   }
  }
 }
 return queue;
}
function PixiBoard({game,reference,view,selected,onPlace,onMove,onReturn,onSelect,onView}:{
 game:JigsawGame;reference:boolean;view:View;selected:number|null;
 onPlace:(index:number,x:number,y:number)=>void;onMove:(index:number,x:number,y:number)=>void;
 onReturn:(index:number)=>void;onSelect:(index:number|null)=>void;onView:(next:View)=>void;
}){
 const mount=useRef<HTMLDivElement>(null),instance=useRef<Application|null>(null);
 const layer=useRef<Container|null>(null),pieces=useRef<Sprite[]>([]);
 const panning=useRef<{clientX:number;clientY:number;view:View}|null>(null);
 const live=useRef({game,reference,view}),drag=useRef<{
  id:number;original:JigsawPiece;start:{x:number;y:number};group:number[]
 }|null>(null);
 live.current={game,reference,view};
 useEffect(()=>{
  const host=mount.current;if(!host)return;
  let cancelled=false;
  const app=new Application();
  (async()=>{
   await app.init({width:960,height:600,backgroundColor:0x0a1e1c,
    antialias:false,resolution:Math.min(2,window.devicePixelRatio||1),autoDensity:true});
   if(cancelled){app.destroy(true);return;}
   host.appendChild(app.canvas);instance.current=app;
   const stage=new Container();app.stage.addChild(stage);layer.current=stage;
   const texture=await Assets.load<Texture>(images(game.motifId));
   if(cancelled)return;
   const backing=new Sprite(texture);
   backing.width=960;backing.height=600;backing.alpha=.12;stage.addChild(backing);
   const tx=texture.width/game.columns,ty=texture.height/game.rows;
   const cw=960/game.columns,ch=600/game.rows;
   const sprites:Sprite[]=[];
   for(let id=0;id<game.pieceCount;id++){
    const x=id%game.columns,y=Math.floor(id/game.columns);
    const tile=new Sprite(new Texture({source:texture.source,frame:new Rectangle(tx*x,ty*y,tx,ty)}));
    tile.width=cw;tile.height=ch;stage.addChild(tile);sprites.push(tile);
   }
   pieces.current=sprites;
   const grid=new Graphics();
   for(let i=0;i<=game.columns;i++)grid.moveTo(i*cw,0).lineTo(i*cw,600);
   for(let i=0;i<=game.rows;i++)grid.moveTo(0,i*ch).lineTo(960,i*ch);
   grid.stroke({color:0xd1b677,width:.4,alpha:.35});stage.addChild(grid);
   const current=live.current;
   stage.position.set(current.view.x,current.view.y);
   stage.scale.set(current.view.zoom);
   for(let id=0;id<sprites.length;id++){
    const p=current.game.pieces[id],tile=sprites[id];
    tile.visible=p.onBoard;tile.position.set(p.x,p.y);tile.alpha=p.locked?1:.93;
   }
  })().catch(()=>{if(!cancelled)host.textContent='Puzzle renderer unavailable';});
  return()=>{cancelled=true;if(instance.current===app){instance.current=null;layer.current=null;pieces.current=[];}
   try{app.destroy(true);}catch{/* async unmount */}};
 },[game.gameId,game.motifId,game.pieceCount,game.columns,game.rows]);
 useEffect(()=>{
  if(!layer.current)return;
  layer.current.position.set(view.x,view.y);layer.current.scale.set(view.zoom);
  if(layer.current.children[0])layer.current.children[0].alpha=reference?.27:.12;
  for(let i=0;i<pieces.current.length;i++){
   const p=game.pieces[i],s=pieces.current[i];
   s.visible=p.onBoard;s.position.set(p.x,p.y);s.alpha=p.locked?1:.93;
  }
 },[game,view,reference]);
 const point=(event:ReactPointerEvent<HTMLDivElement>)=>{
  const r=event.currentTarget.getBoundingClientRect();
  return {x:((event.clientX-r.left)*960/r.width-view.x)/view.zoom,
   y:((event.clientY-r.top)*600/r.height-view.y)/view.zoom};
 };
 const pick=(p:{x:number;y:number})=>{
  for(let id=game.pieces.length-1;id>=0;id--){
   const item=game.pieces[id];
   if(item.onBoard&&!item.locked&&p.x>=item.x&&p.y>=item.y
    &&p.x<=item.x+960/game.columns&&p.y<=item.y+600/game.rows)return id;
  }
  return null;
 };
 const down=(event:ReactPointerEvent<HTMLDivElement>)=>{
  if(event.button===1||event.ctrlKey||event.metaKey){
   panning.current={clientX:event.clientX,clientY:event.clientY,view};
   event.currentTarget.setPointerCapture(event.pointerId);return;
  }
  if(event.button!==0)return;
  const at=point(event),id=pick(at);
  if(id!==null){
   drag.current={id,start:at,original:game.pieces[id],group:connected(game,id)};
   onSelect(id);
   event.currentTarget.setPointerCapture(event.pointerId);
  }
 };
 const move=(event:ReactPointerEvent<HTMLDivElement>)=>{
  const pan=panning.current;
  if(pan){const rect=event.currentTarget.getBoundingClientRect();
   onView({...pan.view,x:pan.view.x+(event.clientX-pan.clientX)*960/rect.width,
    y:pan.view.y+(event.clientY-pan.clientY)*600/rect.height});return;
  }
  const d=drag.current;if(!d)return;
  const at=point(event),dx=at.x-d.start.x,dy=at.y-d.start.y;
  for(const id of d.group){
   const s=pieces.current[id],p=game.pieces[id];
   if(s&&!p.locked)s.position.set(p.x+dx,p.y+dy);
  }
 };
 const up=(event:ReactPointerEvent<HTMLDivElement>)=>{
  if(panning.current){panning.current=null;return;}
  const at=point(event),d=drag.current;drag.current=null;
  if(d){
   if(Math.hypot(at.x-d.start.x,at.y-d.start.y)>1)
    onMove(d.id,d.original.x+at.x-d.start.x,d.original.y+at.y-d.start.y);
   return;
  }
  if(selected!==null&&!game.pieces[selected].onBoard)
   onPlace(selected,at.x-480/game.columns,at.y-300/game.rows);
 };
 const context=(event:ReactMouseEvent<HTMLDivElement>)=>{
  event.preventDefault();
  const r=event.currentTarget.getBoundingClientRect();
  const at={x:((event.clientX-r.left)*960/r.width-view.x)/view.zoom,
   y:((event.clientY-r.top)*600/r.height-view.y)/view.zoom};
  const id=pick(at);if(id!==null)onReturn(id);
 };
 const wheel=(event:React.WheelEvent<HTMLDivElement>)=>{
  event.preventDefault();
  const change=event.deltaY<0?1.25:.8;
  onView({...view,zoom:Math.min(3,Math.max(1,view.zoom*change))});
 };
 return <div ref={mount} className="jigsaw-board" onPointerDown={down}
   onPointerMove={move} onPointerUp={up} onPointerCancel={()=>{drag.current=null;panning.current=null;}}
   onWheel={wheel} onContextMenu={context} role="group" aria-label="Jigsaw board"/>;
}
export function JigsawScreen({state,language,pending,error,command}:Props){
 const t=copy[language],game=state.game;
 const [selected,setSelected]=useState<number|null>(null),[page,setPage]=useState(0);
 const [view,setView]=useState<View>({zoom:1,x:0,y:0}),[reference,setReference]=useState(false);
 const available=useMemo(()=>game?.pieces.filter(p=>!p.onBoard)??[],[game]);
 const pages=Math.max(1,Math.ceil(available.length/32)),currentPage=Math.min(page,pages-1);
 const on=(intent:JigsawIntent)=>{if(!pending)command(intent);};
 const zoom=(factor:number)=>setView(v=>({...v,zoom:Math.max(1,Math.min(3,v.zoom*factor))}));
 const motif=state.motifs.find(x=>x.id===(game?.motifId??state.selectedMotifId));
 useEffect(()=>{setPage(0);setSelected(null);setView({zoom:1,x:0,y:0});},[game?.gameId]);
 const base=<div className="jigsaw-heading"><div><small>THE MIDNIGHT CLUB · CHILL LOUNGE</small>
  <h1>{t.title}</h1><p>{t.intro}</p></div>
  <div className="jigsaw-stats"><span>{t.completed} <b>{state.stats.completedGames}</b></span>
   <span>{t.pieces} <b>{state.stats.piecesCompleted}</b></span>
   <span>{t.largest} <b>{state.stats.largestPuzzle}</b></span></div></div>;
 if(state.screen==='achievements')return <section className="jigsaw-shell">
  <h1>{t.achievements}</h1><button onClick={()=>on({action:'jigsaw.back'})}>{t.back}</button>
  <div className="jigsaw-achievements">{state.achievements.map(a=><article key={a.id}>
   <strong>{a.unlocked?'✓ ':'◇ '}{a.name}</strong><small>{a.tier}</small>
   <p>{a.description}</p><span>{a.current}/{a.goal} · +{a.rewardXp} XP · +{a.rewardCoins} coins</span>
  </article>)}</div></section>;
 if(state.screen==='library')return <section className="jigsaw-shell">
  {base}<h2>{t.motif}</h2>
  <div className="jigsaw-gallery">{state.motifs.map(m=><button key={m.id} disabled={pending}
   aria-pressed={state.selectedMotifId===m.id} className={state.selectedMotifId===m.id?'selected':''}
   onClick={()=>on({action:'jigsaw.motif',motifId:m.id})}>
   <img src={images(m.id)} alt=""/><strong>{m.name}</strong><small>{m.category}</small>
  </button>)}</div>
  <h2>{t.size}</h2><div className="jigsaw-sizes">{state.sizes.map(s=><button key={s.count}
   disabled={pending} aria-pressed={state.selectedPieceCount===s.count}
   className={state.selectedPieceCount===s.count?'selected':''}
   onClick={()=>on({action:'jigsaw.size',pieceCount:s.count})}>
   {s.count}<small>{s.columns}×{s.rows} · {s.rewardXp} XP</small></button>)}</div>
  <div className="jigsaw-actions">
   <button disabled={pending} onClick={()=>on({action:'jigsaw.timer',enabled:!state.timerEnabled})}>
    {t.timer}: {state.timerEnabled?t.on:t.off}</button>
   <button onClick={()=>on({action:'jigsaw.achievements'})}>{t.achievements} {state.stats.achievementsUnlocked}/30</button>
   {state.resume&&<button className="jigsaw-primary" disabled={pending}
    onClick={()=>on({action:'jigsaw.resume'})}>{t.resume} · {state.resume.lockedCount}/{state.resume.pieceCount}</button>}
   <button className="jigsaw-primary" disabled={pending} onClick={()=>{
    if(state.resume&&!window.confirm(t.ask))return;
    on({action:'jigsaw.start',...(state.resume?{confirmReplace:true as const}:{})});
   }}>{state.resume?t.replace:t.start}</button></div>
  {error&&<p role="alert" className="jigsaw-error">{error}</p>}
 </section>;
 if(!game)return null;
 if(state.screen==='complete')return <section className="jigsaw-shell jigsaw-result">
  <h1>{t.result}</h1><img src={images(game.motifId)} alt={motif?.name??''}/>
  <h2>{motif?.name}</h2><p>{t.finish}</p>
  <p>{game.pieceCount} · {game.timerEnabled?fmt(game.elapsedSeconds):t.none}</p>
  <p className="jigsaw-rewards">{state.rewardNotice}</p>
  <button className="jigsaw-primary" disabled={pending} onClick={()=>on({action:'jigsaw.back'})}>{t.another}</button>
 </section>;
 return <section className="jigsaw-shell jigsaw-playing">
  <div className="jigsaw-heading"><div><small>CHILL LOUNGE · JIGSAW PUZZLE</small>
   <h1>{motif?.name}</h1><p>{game.lockedCount}/{game.pieceCount} · {game.timerEnabled?fmt(game.elapsedSeconds):t.none}</p></div>
   <div className="jigsaw-actions"><button onClick={()=>zoom(.8)}>{t.zoomOut}</button>
    <button onClick={()=>zoom(1.25)}>{t.zoomIn}</button>
    <button onClick={()=>setView({zoom:1,x:0,y:0})}>{t.reset}</button>
    <button aria-pressed={reference} onClick={()=>setReference(v=>!v)}>{t.reference}</button>
    <button disabled={pending} onClick={()=>on({action:'jigsaw.timer',enabled:!state.timerEnabled})}>
     {t.timer}: {game.timerEnabled?t.on:t.off}</button>
    <button disabled={pending} onClick={()=>on({action:'jigsaw.back'})}>{t.back}</button>
   </div></div>
  <div className="jigsaw-layout"><div className="jigsaw-board-panel">
   <PixiBoard game={game} reference={reference} view={view} onView={setView} selected={selected}
    onSelect={setSelected} onPlace={(index,x,y)=>{
     on({action:'jigsaw.place',index,x:Math.round(x*100)/100,y:Math.round(y*100)/100});setSelected(null);
    }}
    onMove={(index,x,y)=>on({action:'jigsaw.move',index,x:Math.round(x*100)/100,y:Math.round(y*100)/100})}
    onReturn={index=>on({action:'jigsaw.tray',index})}/>
   <p className="jigsaw-hint">{t.guide}</p>{error&&<p role="alert" className="jigsaw-error">{error}</p>}</div>
   <aside className="jigsaw-tray"><h2>{t.tray}</h2><p>{available.length} {t.left}</p>
    <div className="jigsaw-tray-grid">{available.slice(currentPage*32,(currentPage+1)*32).map(p=><button
     key={p.id} disabled={pending} aria-pressed={selected===p.id} className={selected===p.id?'selected':''}
     title={'Piece '+(p.id+1)} onClick={()=>setSelected(v=>v===p.id?null:p.id)}>
     <span style={{backgroundImage:'url('+images(game.motifId)+')',
      backgroundSize:(game.columns*100)+'% '+(game.rows*100)+'%',
      backgroundPosition:(p.id%game.columns)/(game.columns-1)*100+'% '+
       Math.floor(p.id/game.columns)/(game.rows-1)*100+'%'}}/></button>)}</div>
    <div className="jigsaw-pages"><button disabled={currentPage===0} onClick={()=>setPage(v=>Math.max(0,v-1))}>◀</button>
     <span>{t.page} {currentPage+1}/{pages}</span>
     <button disabled={currentPage>=pages-1} onClick={()=>setPage(v=>Math.min(pages-1,v+1))}>▶</button></div>
    {selected!==null&&<div className="jigsaw-actions">
     {game.pieces[selected].onBoard&&!game.pieces[selected].locked
      &&<button onClick={()=>on({action:'jigsaw.tray',index:selected})}>{t.return}</button>}
     <button onClick={()=>setSelected(null)}>{t.clear}</button></div>}
    <p>{t.saved}</p>
   </aside></div>
 </section>;
}
