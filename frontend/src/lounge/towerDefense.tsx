import {useEffect,useRef,useState} from 'react';
import {Application,Graphics} from 'pixi.js';
import type {TdPoint,TowerDefenseState,TowerIntent,TowerKind} from './towerDefenseBridge';
import './towerDefense.css';

type Props={state:TowerDefenseState;language:'en'|'de';pending:boolean;error:string;
  animations:boolean;command:(intent:TowerIntent)=>void};
const costs:Record<TowerKind,number>={archer:70,frost:105,cannon:135};
const towerNames={en:{archer:'Archer',frost:'Frost',cannon:'Cannon'},
  de:{archer:'Bogenturm',frost:'Frostturm',cannon:'Kanone'}};
const towerColors:Record<TowerKind,number>={archer:0xe9bf6d,frost:0x84dafa,cannon:0xf3896f};
const enemyColors={grunt:0xb1cf82,runner:0xf8cb80,brute:0xc88881,shield:0x9cbad8};
const copy={
 en:{sub:'A quiet defence of the midnight keep',wave:'WAVE',lives:'HEARTS',coins:'COINS',active:'IN THE FIELD',
  intro:'Choose a building site and prepare your towers. Start each wave when you are ready.',
  start:'START NEXT WAVE',pause:'PAUSE',resume:'RESUME',speed:'SPEED',restart:'NEW DEFENCE',
  restartConfirm:'Replace your current defence and start from wave one?',
  build:'BUILD',upgrade:'UPGRADE',sell:'SELL',level:'LEVEL',empty:'Choose a tower to build on this site.',
  selected:'BUILDING SITE',max:'MAX LEVEL',notEnough:'Not enough coins',select:'Select a building pad',
  victory:'THE KEEP STANDS',won:'All 15 waves defeated. Your fortress is safe.',
  defeat:'THE KEEP HAS FALLEN',lost:'The invaders breached the last gate.',
  planning:'PREPARATION',running:'UNDER ATTACK',paused:'PAUSED',hint:'Click a glowing construction circle or use the site selector.',
  save:'Progress is saved locally. No virtual credits are wagered or won.'},
 de:{sub:'Eine ruhige Verteidigung der Mitternachtsfeste',wave:'WELLE',lives:'LEBEN',coins:'MÜNZEN',active:'AUF DEM FELD',
  intro:'Wähle einen Bauplatz und errichte deine Türme. Du entscheidest, wann die nächste Welle beginnt.',
  start:'NÄCHSTE WELLE STARTEN',pause:'PAUSE',resume:'WEITER',speed:'TEMPO',restart:'NEUE VERTEIDIGUNG',
  restartConfirm:'Aktuellen Spielstand ersetzen und von Welle eins neu starten?',
  build:'BAUEN',upgrade:'VERBESSERN',sell:'VERKAUFEN',level:'STUFE',empty:'Wähle einen Turm für diesen Bauplatz.',
  selected:'BAUPLATZ',max:'MAXIMALE STUFE',notEnough:'Nicht genug Münzen',select:'Bauplatz wählen',
  victory:'DIE FESTUNG STEHT',won:'Alle 15 Wellen besiegt. Deine Festung ist sicher.',
  defeat:'DIE FESTUNG IST GEFALLEN',lost:'Die Angreifer haben das letzte Tor überwunden.',
  planning:'VORBEREITUNG',running:'UNTER ANGRIFF',paused:'PAUSIERT',hint:'Klicke auf einen leuchtenden Bauplatz oder wähle einen aus der Liste.',
  save:'Dein Fortschritt wird lokal gespeichert. Keine Einsätze oder Gewinne von Casino-Credits.'}
};
function pos(path:TdPoint[],distance:number):TdPoint{
 let left=Math.max(0,distance);
 for(let i=1;i<path.length;i++){
  const a=path[i-1],b=path[i],length=Math.hypot(b.x-a.x,b.y-a.y);
  if(left<=length)return{x:a.x+(b.x-a.x)*left/length,y:a.y+(b.y-a.y)*left/length};
  left-=length;
 }
 return path[path.length-1];
}
function MapBoard({model,selected,onSelect,animations}:{model:TowerDefenseState;selected:number|null;
 onSelect:(pad:number)=>void;animations:boolean}){
 const mount=useRef<HTMLDivElement>(null);
 const live=useRef({model,selected,onSelect,animations,received:performance.now()});
 if(live.current.model.state.ticks!==model.state.ticks)live.current.received=performance.now();
 live.current={...live.current,model,selected,onSelect,animations};
 useEffect(()=>{
  const host=mount.current;
  if(!host)return;
  let disposed=false;
  const app=new Application();
  let canvas:HTMLCanvasElement|null=null;
  const frame=()=>{
   const layer=dynamic;
   if(!layer)return;
   layer.clear();
   const {model:selectedModel,selected:chosen,animations:animate,received}=live.current;
   const game=selectedModel.state;
   const pads=selectedModel.pads;
   const towers=game.towers;
   const circle=(x:number,y:number,r:number,color:number,alpha=1)=>{
    layer.circle(x,y,r).fill({color,alpha});
   };
   // Animated tower foundations and generous selected build radius.
   for(let i=0;i<pads.length;i++){
    const p=pads[i], tower=towers.find(t=>t.pad===i);
    if(chosen===i){
     layer.circle(p.x,p.y,44).fill({color:0xf0c979,alpha:.09});
     layer.circle(p.x,p.y,43).stroke({color:0xf5d791,width:2,alpha:.9});
    }
    circle(p.x,p.y,26,0x07171b,.9);
    layer.circle(p.x,p.y,25).stroke({color:tower?0xd2ba79:0x7d9e8a,width:2,alpha:.9});
    if(!tower){
     circle(p.x,p.y,18,0x25483d,.75);
     layer.moveTo(p.x-9,p.y).lineTo(p.x+9,p.y).stroke({color:0xb2d4ab,width:2});
     layer.moveTo(p.x,p.y-9).lineTo(p.x,p.y+9).stroke({color:0xb2d4ab,width:2});
     continue;
    }
    const c=towerColors[tower.kind];
    circle(p.x,p.y,19,0x1d2e30);
    layer.circle(p.x,p.y,16).stroke({color:c,width:3});
    if(tower.kind==='archer'){
     layer.moveTo(p.x-9,p.y+8).lineTo(p.x+10,p.y-10).stroke({color:c,width:5});
     circle(p.x+9,p.y-9,3,0xffe8a6);
    }else if(tower.kind==='frost'){
     layer.moveTo(p.x,p.y-13).lineTo(p.x+11,p.y).lineTo(p.x,p.y+13)
      .lineTo(p.x-11,p.y).closePath().fill({color:c,alpha:.95});
     circle(p.x,p.y,4,0xffffff);
    }else{
     layer.roundRect(p.x-11,p.y-7,22,14,3).fill({color:c});
     layer.rect(p.x+3,p.y-5,20,10).fill({color:0xf9c6a6});
    }
    for(let j=0;j<tower.level;j++)circle(p.x-8+j*8,p.y+20,2.7,0xffe2a6);
   }
   // Native C# supplies damage, enemy positions, lives and effects.
   const extra=animate&&game.phase==='running'&&!game.paused
    ?Math.min(.095,Math.max(0,(performance.now()-received)/1000))*game.speed:0;
   for(const enemy of game.enemies){
    const speed=enemy.kind==='runner'?96:enemy.kind==='brute'?32:enemy.kind==='shield'?43:56;
    const p=pos(selectedModel.mapPath,enemy.distance+extra*speed*(enemy.slow>0?.5:1));
    const r=enemy.kind==='brute'?17:enemy.kind==='runner'?10:13;
    circle(p.x+2,p.y+4,r+3,0x000000,.35);
    circle(p.x,p.y,r,enemyColors[enemy.kind]);
    layer.circle(p.x,p.y,r-3).stroke({color:0x203437,width:1.6,alpha:.75});
    if(enemy.kind==='shield')layer.circle(p.x,p.y,r+3).stroke({color:0xb7e1f4,width:3});
    if(enemy.kind==='runner'){
     layer.moveTo(p.x-6,p.y-5).lineTo(p.x+8,p.y).lineTo(p.x-6,p.y+5)
      .closePath().fill({color:0x503629});
    }
    if(enemy.slow>0)layer.circle(p.x,p.y,r+6).stroke({color:0x7bdbff,width:2,alpha:.7});
    const max=enemy.kind==='runner'?18+game.wave*6:enemy.kind==='brute'?72+game.wave*20:
     enemy.kind==='shield'?40+game.wave*12:27+game.wave*10;
    layer.roundRect(p.x-17,p.y-r-12,34,5,2).fill({color:0x172c2c,alpha:.96});
    layer.roundRect(p.x-16,p.y-r-11,Math.max(0,32*enemy.hp/max),3,2)
     .fill({color:enemy.hp/max<.35?0xef8c7f:0x93daa7});
   }
   for(const fx of game.effects){
    const c=towerColors[fx.kind];
    layer.moveTo(fx.fromX,fx.fromY).lineTo(fx.toX,fx.toY)
     .stroke({color:c,width:fx.kind==='cannon'?6:3,alpha:fx.ticks/3});
    if(fx.kind==='cannon'){
     layer.circle(fx.toX,fx.toY,55+(3-fx.ticks)*4)
      .stroke({color:c,width:4,alpha:fx.ticks/5});
    }else circle(fx.toX,fx.toY,5+(3-fx.ticks)*3,c,fx.ticks/3);
   }
  };
  let dynamic:Graphics|null=null;
  const onPointer=(ev:PointerEvent)=>{
   if(!canvas)return;
   const rect=canvas.getBoundingClientRect();
   const x=(ev.clientX-rect.left)/rect.width*960,y=(ev.clientY-rect.top)/rect.height*600;
   const {pads}=live.current.model;
   let closest=-1,distance=42;
   for(let i=0;i<pads.length;i++){
    const d=Math.hypot(pads[i].x-x,pads[i].y-y);
    if(d<distance){distance=d;closest=i;}
   }
   if(closest>=0)live.current.onSelect(closest);
  };
  (async()=>{
   await app.init({width:960,height:600,backgroundColor:0x0b1d1d,antialias:true,
    resolution:Math.min(window.devicePixelRatio||1,2),autoDensity:true});
   if(disposed){app.destroy(true);return;}
   canvas=app.canvas;
   canvas.addEventListener('pointerdown',onPointer);
   host.appendChild(canvas);
   const bg=new Graphics();
   bg.rect(0,0,960,600).fill({color:0x102d29});
   for(let x=0;x<=960;x+=48)bg.moveTo(x,0).lineTo(x,600)
    .stroke({color:0x527564,width:1,alpha:.14});
   for(let y=0;y<=600;y+=48)bg.moveTo(0,y).lineTo(960,y)
    .stroke({color:0x527564,width:1,alpha:.14});
   // Warm stone pathway, outlines and center line.
   const trace=(width:number,color:number,alpha:number)=>{
    const path=live.current.model.mapPath;
    bg.moveTo(path[0].x,path[0].y);
    for(const p of path.slice(1))bg.lineTo(p.x,p.y);
    bg.stroke({color,width,alpha,cap:'round',join:'round'});
   };
   trace(66,0x06191a,.95);trace(57,0x81745b,1);trace(47,0xa08b65,.85);
   trace(2,0xf0d5a0,.17);
   // End gate, arrowheads and towers are rendered as shapes, not emoji assets.
   bg.roundRect(932,272,24,108,4).fill({color:0x243b40})
    .roundRect(926,262,34,12,3).fill({color:0xb49a65});
   for(let i=0;i<4;i++)bg.rect(936+i*5,277,2,93).fill({color:0x9d855a});
   app.stage.addChild(bg);
   dynamic=new Graphics();app.stage.addChild(dynamic);
   app.ticker.add(frame);
  })().catch(()=>{if(!disposed)host.setAttribute('data-error','render');});
  return()=>{disposed=true;if(canvas)canvas.removeEventListener('pointerdown',onPointer);
   app.ticker.remove(frame);app.destroy(true);};
 },[]);
 return <div className="td-map" ref={mount} role="img"
  aria-label="Tower defense map with winding enemy path and ten buildable sites"/>;
}
export function TowerDefenseScreen({state,language,pending,error,animations,command}:Props){
 const t=copy[language],game=state.state;
 const [selected,setSelected]=useState<number|null>(0);
 const tower=selected===null?undefined:game.towers.find(x=>x.pad===selected);
 const canAct=game.phase!=='won'&&game.phase!=='lost';
 const levelCost=tower&&tower.level<3?Math.floor(costs[tower.kind]*(tower.level+1)/2):null;
 const spent=tower?costs[tower.kind]+Array.from({length:tower.level-1},(_,i)=>
  Math.floor(costs[tower.kind]*(i+2)/2)).reduce((a,b)=>a+b,0):0;
 const sellAmount=Math.floor(spent*.6);
 const confirmRestart=()=>{
  if(window.confirm(t.restartConfirm))command({action:'tower.restart'});
 };
 return <section className="td-shell">
  <div className="td-heading">
   <div><p className="td-eyebrow">THE MIDNIGHT CLUB · DEFENCE WING</p>
    <h1>Midnight Bastion</h1><p>{t.sub}</p></div>
   <div className="td-stats">
    <div><small>{t.wave}</small><strong>{game.wave}<span> / {state.maxWaves}</span></strong></div>
    <div><small>{t.lives}</small><strong>{game.lives}</strong></div>
    <div><small>{t.coins}</small><strong>{game.coins}</strong></div>
    <div><small>{t.active}</small><strong>{game.enemies.length}</strong></div>
   </div>
  </div>
  <div className="td-status">
   <span className={'td-state td-'+game.phase}>
    {game.phase==='running'&&game.paused?t.paused:t[game.phase]}
   </span>
   <span>{game.phase==='running'
    ?game.spawned+' / '+game.spawnTotal+' '+(language==='de'?'Gegner erschienen':'enemies deployed')
    :game.phase==='won'?t.won:game.phase==='lost'?t.lost:t.intro}</span>
  </div>
  <div className="td-layout">
   <div className="td-main">
    <MapBoard model={state} selected={selected} onSelect={setSelected} animations={animations}/>
    <p className="td-caption">{t.hint}</p>
    <div className="td-actions">
     {game.phase==='planning'&&<button className="td-primary" disabled={pending}
      onClick={()=>command({action:'tower.start'})}>{t.start}</button>}
     {game.phase==='running'&&<button disabled={pending} onClick={()=>command({action:'tower.pause'})}>
      {game.paused?t.resume:t.pause}</button>}
     <button disabled={pending} onClick={()=>command({action:'tower.speed'})}>
      {t.speed} ×{game.speed}</button>
     <button onClick={confirmRestart} disabled={pending}>{t.restart}</button>
    </div>
    {(game.phase==='won'||game.phase==='lost')&&<div className="td-conclusion">
     <h2>{game.phase==='won'?t.victory:t.defeat}</h2>
     <p>{game.phase==='won'?t.won:t.lost}</p>
    </div>}
   </div>
   <aside className="td-sidebar">
    <h2>{t.selected} <span>{selected===null?'—':String(selected+1).padStart(2,'0')}</span></h2>
    <label htmlFor="td-pad">{t.select}</label>
    <select id="td-pad" value={selected??''} onChange={e=>setSelected(Number(e.target.value))}>
     {state.pads.map((_,i)=><option key={i} value={i}>{t.selected} {i+1}</option>)}
    </select>
    {tower?<div className="td-equipped">
     <span className={'td-tower-icon '+tower.kind}>{tower.kind==='archer'?'⌖':tower.kind==='frost'?'❄':'✹'}</span>
     <h3>{towerNames[language][tower.kind]}</h3>
     <p>{t.level} {tower.level} / 3</p>
     <button className="td-primary" disabled={!canAct||pending||levelCost===null||game.coins<levelCost}
      onClick={()=>{if(selected!==null)command({action:'tower.upgrade',pad:selected});}}>
      {levelCost===null?t.max:t.upgrade+' · '+levelCost}
     </button>
     <button disabled={!canAct||pending} onClick={()=>{
      if(selected!==null)command({action:'tower.sell',pad:selected});
     }}>{t.sell} · +{sellAmount}</button>
    </div>:<>
     <p className="td-help">{t.empty}</p>
     <div className="td-builds">
      {(Object.keys(costs) as TowerKind[]).map(kind=><button key={kind}
        disabled={selected===null||!canAct||pending||game.coins<costs[kind]}
        onClick={()=>{if(selected!==null)command({action:'tower.build',pad:selected,kind});}}>
        <span className={'td-tower-icon '+kind}>{kind==='archer'?'⌖':kind==='frost'?'❄':'✹'}</span>
        <span><strong>{towerNames[language][kind]}</strong><small>{t.build} · {costs[kind]}</small></span>
       </button>)}
     </div>
    </>}
    <p className="td-save-note">{t.save}</p>
   </aside>
  </div>
  {error&&<p className="td-error" role="alert">{error}</p>}
 </section>;
}
