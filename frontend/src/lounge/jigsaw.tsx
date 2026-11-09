import {useEffect,useLayoutEffect,useMemo,useRef,useState} from 'react';
import type {PointerEvent as ReactPointerEvent,WheelEvent as ReactWheelEvent} from 'react';
import {Application,Assets,Container,Graphics,Sprite,Texture} from 'pixi.js';
import type {JigsawGame,JigsawIntent,JigsawPiece,JigsawState} from './jigsawBridge';
import {ManorAudio} from '../audio/manorAudio';
import {pieceSegments,piecePath,trayRank} from './puzzleGeometry';
import {boundedView,viewport,toBoard,clampPiece,clampGroupMotion,needsRecovery} from './jigsawViewport';
import './jigsaw.css';

type Lang='en'|'de';
type Props={state:JigsawState;language:Lang;pending:boolean;error:string;animations:boolean;soundEnabled?:boolean;
 command:(intent:JigsawIntent)=>void};
type View={zoom:number;x:number;y:number};
type FloatingPiece={id:number;x:number;y:number;overBoard:boolean};
// Dedicated, single-scene, high-resolution art; not the legacy collection mosaics.
const images=(id:string)=>'/jigsaw/scenes/'+id+'.jpg';
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
  guide:'Select or drag a piece from the tray. Hold right or middle mouse to pan; a short right-click returns a loose group.',
  recover:'RECOVER OFFBOARD PIECE',
  result:'THE FINISHED PICTURE',finish:'Puzzle completed and saved.',
  another:'ANOTHER PUZZLE',none:'No time tracked',left:'pieces remaining'},
 de:{title:'Das Puzzle-Atelier',intro:'Zwölf Motive. Acht Größen. Kein Zeitdruck.',
  completed:'ABGESCHLOSSEN',pieces:'TEILE GELEGT',largest:'GRÖSSTES PUZZLE',achievements:'ERFOLGE',
  motif:'MOTIV WÄHLEN',size:'TEILEANZAHL WÄHLEN',start:'PUZZLE STARTEN',
  replace:'NEUES PUZZLE STARTEN',resume:'PUZZLE FORTSETZEN',ask:'Das unfertige Puzzle verwerfen?',
  back:'← PUZZLE-MENÜ',timer:'TIMER',on:'AN',off:'AUS',reference:'VORLAGE',
  reset:'ANSICHT',zoomIn:'ZOOM +',zoomOut:'ZOOM −',tray:'TEILEABLAGE',page:'SEITE',
  return:'AUSGEWÄHLTES TEIL ZURÜCKLEGEN',clear:'AUSWAHL AUFHEBEN',saved:'Züge werden automatisch gespeichert.',
  guide:'Wähle oder ziehe ein Teil aus der Ablage. Rechte oder mittlere Maustaste halten verschiebt die Ansicht; kurzer Rechtsklick legt lose Gruppen zurück.',
  recover:'VERLORENES TEIL ZURÜCKHOLEN',
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
 const resize=useRef<ResizeObserver|null>(null);
 const layer=useRef<Container|null>(null),pieces=useRef<Container[]>([]);
 const panning=useRef<{clientX:number;clientY:number;view:View;button:number;moved:boolean}|null>(null);
 const pendingDrag=useRef<{id:number;originX:number;originY:number;group:number[]}|null>(null);
 const live=useRef({game,reference,view}),drag=useRef<{
  id:number;original:JigsawPiece;start:{x:number;y:number};group:number[]
 }|null>(null);
 live.current={game,reference,view};
 useEffect(()=>{
  const host=mount.current;if(!host)return;
  let cancelled=false;
  const app=new Application();
  (async()=>{
   await app.init({width:Math.max(1,host.clientWidth),height:Math.max(1,host.clientHeight),backgroundColor:0x0a1e1c,
    antialias:false,resolution:Math.min(2,window.devicePixelRatio||1),autoDensity:true});
   if(cancelled){app.destroy(true);return;}
   host.appendChild(app.canvas);instance.current=app;
   const stage=new Container();app.stage.addChild(stage);layer.current=stage;
   const measure=()=>{
    if(cancelled)return;
    const frame={width:Math.max(1,host.clientWidth),height:Math.max(1,host.clientHeight)};
    app.renderer.resize(frame.width,frame.height);
    const g=viewport(frame,live.current.view);
    stage.position.set(g.offsetX*g.fit,g.offsetY*g.fit);
    stage.scale.set(g.fit*live.current.view.zoom);
    const safe=boundedView(frame,live.current.view);
    if(safe.x!==live.current.view.x||safe.y!==live.current.view.y)onView(safe);
   };
   resize.current=new ResizeObserver(measure);resize.current.observe(host);
   const texture=await Assets.load<Texture>(images(game.motifId));
   if(cancelled)return;
   const backing=new Sprite(texture);
   backing.width=960;backing.height=600;backing.alpha=live.current.reference?.27:0;stage.addChild(backing);
   const cw=960/game.columns,ch=600/game.rows;
   const sprites:Container[]=[];
   for(let id=0;id<game.pieceCount;id++){
    const tile=new Container(),image=new Sprite(texture),mask=new Graphics(),outline=new Graphics();
    image.width=960;image.height=600;image.position.set(-(id%game.columns)*cw,-Math.floor(id/game.columns)*ch);
    for(const segment of pieceSegments(id,game.columns,game.rows,cw,ch)){
     const v=segment.values;
     for(const g of [mask,outline]){
      if(segment.kind==='M')g.moveTo(v[0],v[1]);
      else if(segment.kind==='L')g.lineTo(v[0],v[1]);
      else g.bezierCurveTo(v[0],v[1],v[2],v[3],v[4],v[5]);
     }
    }
    mask.closePath().fill(0xffffff);outline.closePath().stroke({color:0xe7d6aa,width:.65,alpha:.7});
    image.mask=mask;tile.addChild(image,mask,outline);stage.addChild(tile);sprites.push(tile);
   }
   pieces.current=sprites;host.dataset.rendererReady='true';
   const current=live.current;
   const g=viewport({width:host.clientWidth,height:host.clientHeight},current.view);
   stage.position.set(g.offsetX*g.fit,g.offsetY*g.fit);
   stage.scale.set(g.fit*current.view.zoom);
   for(let id=0;id<sprites.length;id++){
    const p=current.game.pieces[id],tile=sprites[id];
    tile.visible=p.onBoard;tile.position.set(p.x,p.y);tile.alpha=p.locked?1:.93;
   }
  })().catch(()=>{if(!cancelled)host.textContent='Puzzle renderer unavailable';});
  return()=>{cancelled=true;resize.current?.disconnect();resize.current=null;
   if(instance.current===app){instance.current=null;layer.current=null;pieces.current=[];}
   try{app.destroy(true);}catch{/* async unmount */}};
 },[game.gameId,game.motifId,game.pieceCount,game.columns,game.rows]);
 useLayoutEffect(()=>{
  if(!layer.current||!mount.current)return;
  const g=viewport({width:mount.current.clientWidth,height:mount.current.clientHeight},view);
  layer.current.position.set(g.offsetX*g.fit,g.offsetY*g.fit);
  layer.current.scale.set(g.fit*view.zoom);
  const inFlight=pendingDrag.current;
  if(inFlight&&(game.pieces[inFlight.id].x!==inFlight.originX||game.pieces[inFlight.id].y!==inFlight.originY))pendingDrag.current=null;
  if(layer.current.children[0])layer.current.children[0].alpha=reference?.27:0;
  for(let i=0;i<pieces.current.length;i++){
   const p=game.pieces[i],s=pieces.current[i];
   if(drag.current?.group.includes(i)||pendingDrag.current?.group.includes(i))continue;
   s.visible=p.onBoard;s.position.set(p.x,p.y);s.alpha=p.locked?1:.93;
  }
 },[game,view,reference]);
 const local=(clientX:number,clientY:number)=>{
  const r=mount.current!.getBoundingClientRect();
  return toBoard({width:r.width,height:r.height},live.current.view,clientX-r.left,clientY-r.top);
 };
 const point=(event:ReactPointerEvent<HTMLDivElement>)=>local(event.clientX,event.clientY);
 const pick=(p:{x:number;y:number})=>{
  for(let id=game.pieces.length-1;id>=0;id--){
   const item=game.pieces[id];
   if(item.onBoard&&!item.locked&&p.x>=item.x&&p.y>=item.y
    &&p.x<=item.x+960/game.columns&&p.y<=item.y+600/game.rows)return id;
  }
  return null;
 };
 const down=(event:ReactPointerEvent<HTMLDivElement>)=>{
  if(event.button===1||event.button===2||event.ctrlKey||event.metaKey){
   panning.current={clientX:event.clientX,clientY:event.clientY,view,button:event.button,moved:false};
   event.currentTarget.setPointerCapture(event.pointerId);event.preventDefault();return;
  }
  if(event.button!==0)return;
  const at=point(event),id=pick(at);
  if(id!==null){
   const group=connected(game,id);
   if(group.some(i=>game.pieces[i].locked))return;
   pendingDrag.current=null;
   drag.current={id,start:at,original:game.pieces[id],group};
   onSelect(id);
   event.currentTarget.setPointerCapture(event.pointerId);
  }
 };
 const move=(event:ReactPointerEvent<HTMLDivElement>)=>{
  const pan=panning.current;
  if(pan){
   const r=event.currentTarget.getBoundingClientRect();
   if(Math.hypot(event.clientX-pan.clientX,event.clientY-pan.clientY)>4)pan.moved=true;
   const g=viewport({width:r.width,height:r.height},pan.view);
   onView(boundedView({width:r.width,height:r.height},
    {...pan.view,x:pan.view.x+(event.clientX-pan.clientX)/g.fit,
     y:pan.view.y+(event.clientY-pan.clientY)/g.fit}));return;
  }
  const d=drag.current;if(!d)return;
  const at=point(event);
  const bounded=clampGroupMotion(game.pieces,d.group,d.id,
   d.original.x+at.x-d.start.x,d.original.y+at.y-d.start.y,game.columns,game.rows);
  for(const id of d.group){
   const sprite=pieces.current[id],p=game.pieces[id];
   if(sprite&&!p.locked)sprite.position.set(p.x+bounded.dx,p.y+bounded.dy);
  }
 };
 const up=(event:ReactPointerEvent<HTMLDivElement>)=>{
  const pan=panning.current;
  if(pan){
   panning.current=null;
   if(pan.button===2&&!pan.moved){const id=pick(point(event));if(id!==null)onReturn(id);}
   return;
  }
  const at=point(event),d=drag.current;drag.current=null;
  if(d){
   const bounded=clampGroupMotion(game.pieces,d.group,d.id,
    d.original.x+at.x-d.start.x,d.original.y+at.y-d.start.y,game.columns,game.rows);
   if(Math.hypot(bounded.dx,bounded.dy)>.01){
    pendingDrag.current={id:d.id,originX:d.original.x,originY:d.original.y,group:d.group};
    onMove(d.id,d.original.x+bounded.dx,d.original.y+bounded.dy);
   }
   return;
  }
  if(selected!==null&&!game.pieces[selected].onBoard){
   const spot=clampPiece(at.x-480/game.columns,at.y-300/game.rows,game.columns,game.rows);
   onPlace(selected,spot.x,spot.y);
  }
 };
 const wheel=(event:ReactWheelEvent<HTMLDivElement>)=>{
  event.preventDefault();
  const rect=event.currentTarget.getBoundingClientRect();
  onView(boundedView({width:rect.width,height:rect.height},
   {...view,zoom:view.zoom*(event.deltaY<0?1.25:.8)}));
 };
 return <div ref={mount} className="jigsaw-board" onPointerDown={down}
   onPointerMove={move} onPointerUp={up} onPointerCancel={()=>{drag.current=null;panning.current=null;pendingDrag.current=null;}}
   onWheel={wheel} onContextMenu={e=>e.preventDefault()} role="group" aria-label="Jigsaw board"/>;
}
export function JigsawScreen({state,language,pending,error,command,animations,soundEnabled=false}:Props){
 const t=copy[language],game=state.game;
 const audio=useMemo(()=>new ManorAudio(),[]),previous=useRef({id:'',placed:0,locked:0});
 const [feedback,setFeedback]=useState(false);
 useEffect(()=>{audio.setHostPolicy(soundEnabled,false,0);},[audio,soundEnabled]);
 useEffect(()=>()=>audio.dispose(),[audio]);
 useEffect(()=>{if(!game)return;const last=previous.current;
  if(last.id===game.gameId&&(game.onBoardCount>last.placed||game.lockedCount>last.locked)){
   audio.play(game.lockedCount>last.locked?'checkpoint':'reel');setFeedback(true);
  }
  previous.current={id:game.gameId,placed:game.onBoardCount,locked:game.lockedCount};
 },[game?.gameId,game?.onBoardCount,game?.lockedCount,audio]);
 useEffect(()=>{if(!feedback)return;const id=window.setTimeout(()=>setFeedback(false),650);return()=>window.clearTimeout(id);},[feedback]);
 const [selected,setSelected]=useState<number|null>(null),[page,setPage]=useState(0);
 const boardFrame=useRef<HTMLDivElement>(null);
 const trayDrag=useRef<{id:number;x:number;y:number;moved:boolean}|null>(null);
 const suppressClick=useRef(false);
 const [floating,setFloating]=useState<FloatingPiece|null>(null);
 const [view,setView]=useState<View>({zoom:1,x:0,y:0}),[reference,setReference]=useState(false);
 const available=useMemo(()=>game?.pieces.filter(p=>!p.onBoard).sort((a,b)=>trayRank(a.id,game.gameId)-trayRank(b.id,game.gameId))??[],[game]);
 const pages=Math.max(1,Math.ceil(available.length/32)),currentPage=Math.min(page,pages-1);
 const on=(intent:JigsawIntent)=>{if(!pending)command(intent);};
 const frame=()=>{const r=boardFrame.current?.getBoundingClientRect();return {width:r?.width??960,height:r?.height??600};};
 const zoom=(factor:number)=>setView(v=>boundedView(frame(),{...v,zoom:v.zoom*factor}));
 const motif=state.motifs.find(x=>x.id===(game?.motifId??state.selectedMotifId));
 useEffect(()=>{setPage(0);setSelected(null);setView({zoom:1,x:0,y:0});setReference(false);setFloating(null);},[game?.gameId]);
 const recover=game?.pieces.find(p=>needsRecovery(p,game.columns,game.rows));
 useEffect(()=>{
  if(!game||state.screen!=='game')return;
  const key=(e:KeyboardEvent)=>{
   const target=e.target as HTMLElement|null;
   if(e.altKey||e.ctrlKey||e.metaKey||target?.isContentEditable||['INPUT','TEXTAREA','SELECT'].includes(target?.tagName??''))return;
   const name=e.key.toLowerCase();
   if(name==='arrowleft'||name==='a'){e.preventDefault();setPage(v=>Math.max(0,v-1));}
   else if(name==='arrowright'||name==='d'){e.preventDefault();setPage(v=>Math.min(pages-1,v+1));}
   else if(name==='r'){e.preventDefault();setView({zoom:1,x:0,y:0});}
   else if(name==='h'&&!e.repeat){e.preventDefault();setReference(v=>!v);}
   else if(name==='t'&&!e.repeat){e.preventDefault();if(!pending)command({action:'jigsaw.timer',enabled:!game.timerEnabled});}
  };
  window.addEventListener('keydown',key);return()=>window.removeEventListener('keydown',key);
 },[game?.gameId,game?.timerEnabled,state.screen,pages,pending,command]);
 useEffect(()=>{
  const move=(e:PointerEvent)=>{
   const d=trayDrag.current;if(!d)return;
   if(Math.hypot(e.clientX-d.x,e.clientY-d.y)>5)d.moved=true;
   if(!d.moved)return;
   const r=boardFrame.current?.getBoundingClientRect();
   const overBoard=!!r&&e.clientX>=r.left&&e.clientX<=r.right&&e.clientY>=r.top&&e.clientY<=r.bottom;
   setFloating({id:d.id,x:e.clientX,y:e.clientY,overBoard});
  };
  const finish=(e:PointerEvent)=>{
   const d=trayDrag.current;if(!d)return;
   trayDrag.current=null;setFloating(null);
   if(!d.moved)return;
   suppressClick.current=true;window.setTimeout(()=>{suppressClick.current=false;},0);
   const r=boardFrame.current?.getBoundingClientRect();
   if(!r||e.clientX<r.left||e.clientX>r.right||e.clientY<r.top||e.clientY>r.bottom||!game||pending)return;
   const at=toBoard({width:r.width,height:r.height},view,e.clientX-r.left,e.clientY-r.top);
   const spot=clampPiece(at.x-480/game.columns,at.y-300/game.rows,game.columns,game.rows);
   if(!game.pieces[d.id].onBoard){on({action:'jigsaw.place',index:d.id,x:Math.round(spot.x*100)/100,y:Math.round(spot.y*100)/100});setSelected(null);}
  };
  const cancel=()=>{trayDrag.current=null;setFloating(null);};
  window.addEventListener('pointermove',move);window.addEventListener('pointerup',finish);
  window.addEventListener('pointercancel',cancel);
  return()=>{window.removeEventListener('pointermove',move);window.removeEventListener('pointerup',finish);window.removeEventListener('pointercancel',cancel);};
 },[game,view,pending]);
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
  <div className="jigsaw-gallery">{state.motifs.map(m=><button key={m.id} aria-disabled={pending}
   aria-pressed={state.selectedMotifId===m.id} className={state.selectedMotifId===m.id?'selected':''}
   onClick={()=>on({action:'jigsaw.motif',motifId:m.id})}>
   <img src={images(m.id)} alt=""/><strong>{m.name}</strong><small>{m.category}</small>
  </button>)}</div>
  <h2>{t.size}</h2><div className="jigsaw-sizes">{state.sizes.map(s=><button key={s.count}
   aria-disabled={pending} aria-pressed={state.selectedPieceCount===s.count}
   className={state.selectedPieceCount===s.count?'selected':''}
   onClick={()=>on({action:'jigsaw.size',pieceCount:s.count})}>
   {s.count}<small>{s.columns}×{s.rows} · {s.rewardXp} XP</small></button>)}</div>
  <div className="jigsaw-actions">
   <button aria-disabled={pending} onClick={()=>on({action:'jigsaw.timer',enabled:!state.timerEnabled})}>
    {t.timer}: {state.timerEnabled?t.on:t.off}</button>
   <button onClick={()=>on({action:'jigsaw.achievements'})}>{t.achievements} {state.stats.achievementsUnlocked}/30</button>
   {state.resume&&<button className="jigsaw-primary" aria-disabled={pending}
    onClick={()=>on({action:'jigsaw.resume'})}>{t.resume} · {state.resume.lockedCount}/{state.resume.pieceCount}</button>}
   <button className="jigsaw-primary" aria-disabled={pending} onClick={()=>{
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
  <button className="jigsaw-primary" aria-disabled={pending} onClick={()=>on({action:'jigsaw.back'})}>{t.another}</button>
 </section>;
 return <section className={"jigsaw-shell jigsaw-playing"+(feedback&&animations?" piece-placed":"")} onPointerDownCapture={()=>audio.arm()}>
  <div className="jigsaw-heading"><div><small>CHILL LOUNGE · JIGSAW PUZZLE</small>
   <h1>{motif?.name}</h1><p>{game.lockedCount}/{game.pieceCount} · {game.timerEnabled?fmt(game.elapsedSeconds):t.none}</p></div>
   <div className="jigsaw-actions"><button onClick={()=>zoom(.8)}>{t.zoomOut}</button>
    <button onClick={()=>zoom(1.25)}>{t.zoomIn}</button>
    <button onClick={()=>setView({zoom:1,x:0,y:0})}>{t.reset}</button>
    <button aria-pressed={reference} onClick={()=>setReference(v=>!v)}>{t.reference}</button>
    <button aria-disabled={pending} onClick={()=>on({action:'jigsaw.timer',enabled:!state.timerEnabled})}>
     {t.timer}: {game.timerEnabled?t.on:t.off}</button>
    <button aria-disabled={pending} onClick={()=>on({action:'jigsaw.back'})}>{t.back}</button>
   </div></div>
  <div className="jigsaw-layout"><div className="jigsaw-board-panel">
   <div ref={boardFrame} className="jigsaw-board-frame"><PixiBoard game={game} reference={reference} view={view} onView={setView} selected={selected}
    onSelect={setSelected} onPlace={(index,x,y)=>{
     on({action:'jigsaw.place',index,x:Math.round(x*100)/100,y:Math.round(y*100)/100});setSelected(null);
    }}
    onMove={(index,x,y)=>on({action:'jigsaw.move',index,x:Math.round(x*100)/100,y:Math.round(y*100)/100})}
    onReturn={index=>on({action:'jigsaw.tray',index})}/></div>
   <p className="jigsaw-hint sr-only">{t.guide}</p><p className="jigsaw-placement-notice" role="status">{state.notice}</p>{error&&<p role="alert" className="jigsaw-error">{error}</p>}</div>
   <aside className="jigsaw-tray"><h2>{t.tray}</h2><p>{available.length} {t.left}</p>
    <div className="jigsaw-tray-grid">{available.slice(currentPage*32,(currentPage+1)*32).map(p=><button
     key={p.id} data-piece-id={p.id} aria-disabled={pending} aria-pressed={selected===p.id} className={selected===p.id?'selected':''} draggable={false}
     onPointerDown={e=>{if(e.button===0&&!pending){trayDrag.current={id:p.id,x:e.clientX,y:e.clientY,moved:false};}}}
     title={'Piece '+(p.id+1)} onClick={()=>{if(suppressClick.current)return;setSelected(v=>v===p.id?null:p.id);}}>
     <svg viewBox="-20 -20 140 140" aria-hidden="true"><defs><clipPath id={'tray-piece-'+p.id}><path d={piecePath(p.id,game.columns,game.rows,100,100)}/></clipPath></defs>
      <image href={images(game.motifId)} x={-(p.id%game.columns)*100} y={-Math.floor(p.id/game.columns)*100}
       width={game.columns*100} height={game.rows*100} preserveAspectRatio="none" clipPath={'url(#tray-piece-'+p.id+')'}/>
      <path d={piecePath(p.id,game.columns,game.rows,100,100)} fill="none" stroke="#dfc993" strokeWidth="1.5"/>
     </svg></button>)}</div>
    <div className="jigsaw-pages"><button disabled={currentPage===0} onClick={()=>setPage(v=>Math.max(0,v-1))}>◀</button>
     <span>{t.page} {currentPage+1}/{pages}</span>
     <button disabled={currentPage>=pages-1} onClick={()=>setPage(v=>Math.min(pages-1,v+1))}>▶</button></div>
    {recover&&<button className="jigsaw-recover" disabled={pending} onClick={()=>on({action:'jigsaw.tray',index:recover.id})}>{t.recover} #{recover.id+1}</button>}
    {selected!==null&&<div className="jigsaw-actions">
     {game.pieces[selected].onBoard&&!game.pieces[selected].locked
      &&<button onClick={()=>on({action:'jigsaw.tray',index:selected})}>{t.return}</button>}
     <button onClick={()=>setSelected(null)}>{t.clear}</button></div>}
    <p>{t.saved}</p>
   </aside></div>
  {floating&&game&&(()=>{
   const w=960/game.columns,h=600/game.rows,m=Math.min(w,h)*.2;
   const r=boardFrame.current?.getBoundingClientRect();
   const scale=floating.overBoard&&r?Math.min(r.width/960,r.height/600)*view.zoom:70/(Math.max(w,h)+m*2);
   const width=(w+2*m)*scale,height=(h+2*m)*scale;
   const id=floating.id;
   return <div className={'jigsaw-drag-preview'+(floating.overBoard?' over-board':'')}
    style={{left:floating.x-width/2,top:floating.y-height/2,width,height}} aria-hidden="true">
    <svg viewBox={[-m,-m,w+2*m,h+2*m].join(' ')} width="100%" height="100%">
     <defs><clipPath id="jigsaw-floating-clip"><path d={piecePath(id,game.columns,game.rows,w,h)}/></clipPath></defs>
     <image href={images(game.motifId)} x={-(id%game.columns)*w} y={-Math.floor(id/game.columns)*h}
      width="960" height="600" preserveAspectRatio="none" clipPath="url(#jigsaw-floating-clip)"/>
     <path d={piecePath(id,game.columns,game.rows,w,h)} fill="none" stroke="#ecd99e" strokeWidth={Math.max(.5,1/scale)}/>
    </svg>
   </div>;
  })()}
 </section>;
}
