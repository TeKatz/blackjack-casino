import {useEffect,useRef} from 'react';
import {Application,Assets,Container,Graphics,Sprite,Texture} from 'pixi.js';
import type {TdEnemy,TdPoint,TdTower,TowerDefenseState,TowerKind} from './towerDefenseBridge';
import type {BastionCue} from './towerDefenseAudio';
import {collectNewShots,enemySpeed,interpolateEnemy,positionOnPath,previewRange} from './towerDefenseVisuals';

type Props={
 model:TowerDefenseState;selected:number|null;onSelect:(pad:number)=>void;
 animations:boolean;onCue:(cue:BastionCue)=>void;
};
type TowerVisual={sprite:Sprite;kind:TowerKind;level:number;firedAt:number};
type EnemyVisual={sprite:Sprite;hp:number;kind:TdEnemy['kind'];flashUntil:number};
type Flight={kind:TowerKind;from:TdPoint;to:TdPoint;age:number;life:number};
type Burst={x:number;y:number;kind:'build'|'upgrade'|'kill'|'gate'|'impact';age:number;life:number;seed:number};
const art='/tower-defense/';
const assets=['battlefield','archer','frost','cannon','grunt','runner','brute','shield'] as const;
const color:{[K in TowerKind]:number}={archer:0xf0ca83,frost:0xa9eafd,cannon:0xfbb18b};
const mkGraphics=()=>new Graphics();
const phaseReady=(s:TowerDefenseState)=>s.state.phase==='running'&&!s.state.paused;
const dimFor=(kind:TdEnemy['kind'])=>kind==='brute'?74:kind==='runner'?51:kind==='shield'?64:58;
const maxHp=(kind:TdEnemy['kind'],wave:number)=>kind==='runner'?18+wave*6:
 kind==='brute'?72+wave*20:kind==='shield'?40+wave*12:27+wave*10;
const seedBurst=(x:number,y:number,kind:Burst['kind'],seed:number):Burst=>
 ({x,y,kind,age:0,life:kind==='gate'?.65:kind==='upgrade'?.75:.47,seed});
const bColor=(kind:Burst['kind'])=>kind==='gate'?0xf2a07c:kind==='kill'?0xb8e4aa:
 kind==='upgrade'?0xf5dca3:kind==='impact'?0xefbf89:0xe6d4a2;
const prng=(n:number)=>{const v=Math.sin(n*127.7+78.23)*43758.5453123;return v-Math.floor(v);};
const range=(n:number,a:number,b:number)=>a+(b-a)*prng(n);
const easeOut=(t:number)=>1-(1-t)*(1-t);

export function BastionBattlefield({model,selected,onSelect,animations,onCue}:Props){
 const mount=useRef<HTMLDivElement|null>(null);
 const last=useRef({model,selected,onSelect,animations,onCue,received:performance.now()});
 if(last.current.model.state.ticks!==model.state.ticks)last.current.received=performance.now();
 last.current={...last.current,model,selected,onSelect,animations,onCue};
 useEffect(()=>{
  const host=mount.current;if(!host)return;
  let disposed=false,ready=false,canvas:HTMLCanvasElement|null=null,sceneTime=0;
  const reducedMotion=window.matchMedia?.('(prefers-reduced-motion: reduce)').matches??false;
  const app=new Application();
  const towerVisuals=new Map<number,TowerVisual>(),enemyVisuals=new Map<number,EnemyVisual>();
  const seenShots=new Set<string>(),flights:Flight[]=[],bursts:Burst[]=[];
  let previous:TowerDefenseState|null=null;
  let bg:Sprite|Graphics|null=null,pads:Graphics|null=null,weather:Graphics|null=null,
   groundLights:Graphics|null=null,overlay:Graphics|null=null,shots:Graphics|null=null,hud:Graphics|null=null;
  let towerLayer:Container|null=null,enemyLayer:Container|null=null;
  let textures:Partial<Record<(typeof assets)[number],Texture>>={};
  let lastLife=20;
  function emit(x:number,y:number,kind:Burst['kind'],seed:number){
   if(!last.current.animations||reducedMotion)return;
   if(bursts.length>=48)bursts.shift();
   bursts.push(seedBurst(x,y,kind,seed));
  }
  const cue=(sound:BastionCue)=>last.current.onCue(sound);
  const position=(path:TdPoint[],distance:number)=>positionOnPath(path,distance);
  function makeTower(tower:TdTower,p:TdPoint){
   const sprite=new Sprite(textures[tower.kind]??Texture.WHITE);
   sprite.anchor.set(.5);
   sprite.width=93;sprite.height=93;
   sprite.x=p.x;sprite.y=p.y-7;
   if(!textures[tower.kind])sprite.tint=color[tower.kind];
   towerLayer?.addChild(sprite);
   towerVisuals.set(tower.pad,{sprite,kind:tower.kind,level:tower.level,firedAt:-100});
  }
  function removeTower(pad:number){
   const visual=towerVisuals.get(pad);
   if(visual){visual.sprite.removeFromParent();visual.sprite.destroy();towerVisuals.delete(pad);}
  }
  function makeEnemy(enemy:TdEnemy,p:TdPoint){
   const sprite=new Sprite(textures[enemy.kind]??Texture.WHITE);
   sprite.anchor.set(.5);
   sprite.width=dimFor(enemy.kind);sprite.height=dimFor(enemy.kind);
   sprite.x=p.x;sprite.y=p.y;
   if(!textures[enemy.kind])sprite.tint=0xbbd3ad;
   enemyLayer?.addChild(sprite);
   enemyVisuals.set(enemy.id,{sprite,kind:enemy.kind,hp:enemy.hp,flashUntil:0});
  }
  function removeEnemy(id:number){
   const visual=enemyVisuals.get(id);
   if(visual){visual.sprite.removeFromParent();visual.sprite.destroy();enemyVisuals.delete(id);}
  }
  function reconcile(){
   const next=last.current.model,game=next.state;
   if(previous===next)return;
   const prior=previous?.state;
   const reset=prior!==undefined&&game.ticks<prior.ticks;
   if(reset){seenShots.clear();flights.length=0;bursts.length=0;}
   // Native snapshots can repeat at the same tick (language/settings/host refresh).
   if(prior && !reset){
    if(game.wave>prior.wave){cue('wave');emit(478,255,'upgrade',game.wave+17);}
    if(game.lives<prior.lives){cue('gate');emit(926,325,'gate',game.ticks);}
    if(game.phase!==prior.phase){
     if(game.phase==='won'){cue('victory');emit(939,326,'upgrade',55);}
     if(game.phase==='lost'){cue('defeat');emit(939,326,'gate',56);}
    }
    const oldTowers=new Map(prior.towers.map(t=>[t.pad,t]));
    for(const t of game.towers){
     const old=oldTowers.get(t.pad),p=next.pads[t.pad];
     if(!old){cue('build');emit(p.x,p.y,'build',t.pad+game.ticks);}
     else if(t.level>old.level){cue('upgrade');emit(p.x,p.y,'upgrade',t.pad+game.ticks);}
    }
    if(game.towers.some(t=>!oldTowers.has(t.pad))===false &&
      prior.towers.some(t=>!game.towers.some(n=>n.pad===t.pad)))cue('sell');
    const remaining=new Set(game.enemies.map(e=>e.id));
    let kills=0;
    for(const e of prior.enemies)if(!remaining.has(e.id)){
     // Enemies at the gate have escaped; show a gate flash instead of a reward burst.
     const p=position(next.mapPath,e.distance);
     if(p.x<920 || Math.abs(p.y-325)>48){kills++;emit(p.x,p.y,'kill',e.id);}
    }
    if(kills>0)cue('kill');
   }
   const towerIds=new Set(game.towers.map(t=>t.pad));
   for(const pad of Array.from(towerVisuals.keys()))if(!towerIds.has(pad))removeTower(pad);
   for(const tower of game.towers){
    const existing=towerVisuals.get(tower.pad);
    if(!existing||existing.kind!==tower.kind){
     if(existing)removeTower(tower.pad);
     makeTower(tower,next.pads[tower.pad]);
    }else existing.level=tower.level;
   }
   const enemies=new Set(game.enemies.map(e=>e.id));
   for(const id of Array.from(enemyVisuals.keys()))if(!enemies.has(id))removeEnemy(id);
   for(const enemy of game.enemies){
    const found=enemyVisuals.get(enemy.id);
    if(!found)makeEnemy(enemy,position(next.mapPath,enemy.distance));
    else if(found.hp>enemy.hp)found.flashUntil=sceneTime+.13;
    const visual=enemyVisuals.get(enemy.id);
    if(visual)visual.hp=enemy.hp;
   }
   if(reset)seenShots.clear();
   const fresh=collectNewShots(game.ticks,game.effects,seenShots);
   if(seenShots.size>6000)seenShots.clear();
   for(const fx of fresh){
    if(!last.current.animations||reducedMotion)continue;
    if(flights.length>=90)flights.shift();
    const flight:Flight={kind:fx.kind,from:{x:fx.fromX,y:fx.fromY},
     to:{x:fx.toX,y:fx.toY},age:0,life:fx.kind==='cannon'?.32:fx.kind==='frost'?.27:.19};
    flights.push(flight);
    cue(fx.kind);
    const matching=next.pads.findIndex(p=>p.x===fx.fromX&&p.y===fx.fromY);
    if(matching>=0){
     const visual=towerVisuals.get(matching);
     if(visual)visual.firedAt=sceneTime;
    }
   }
   lastLife=game.lives;
   previous=next;
  }
  function render(){
   if(!overlay||!shots||!hud||!weather||!groundLights)return;
   reconcile();
   const current=last.current,game=current.model.state;
   const motion=current.animations&&!reducedMotion;
   const dt=Math.min(.05,Math.max(0,app.ticker.deltaMS/1000));
   const frozen=game.phase==='running'&&game.paused;
   sceneTime+=motion?dt:0;
   overlay.clear();shots.clear();hud.clear();weather.clear();groundLights.clear();
   // Subtle environmental life, intentionally unaffected by battle speed.
   for(let i=0;i<20;i++){
    const x=range(i*31+4,10,950),y=range(i*37+6,15,585);
    const phase=motion?sceneTime*.36+i*.7:0;
    const px=x+(motion?Math.cos(phase)*10:0),py=y+(motion?Math.sin(phase)*9:0);
    const opacity=motion?.17+.18*(.5+.5*Math.sin(phase*1.4)):.18;
    weather.circle(px,py,range(i*3+9,1,2.6)).fill({color:i%3===0?0xf6d59f:0xa1eac4,alpha:opacity});
   }
   // Individual pads retain a solid hit area, with a wide halo for the chosen pad.
   for(let i=0;i<current.model.pads.length;i++){
    const p=current.model.pads[i],tower=game.towers.find(t=>t.pad===i),chosen=current.selected===i;
    if(chosen){
     const radius=tower?previewRange(tower.kind,tower.level):54;
     overlay.circle(p.x,p.y,radius).fill({color:0xe4d097,alpha:.028});
     overlay.circle(p.x,p.y,radius).stroke({color:0xf2d69c,width:1.6,alpha:.23});
     overlay.circle(p.x,p.y,35+(motion?2*Math.sin(sceneTime*3):0))
      .stroke({color:0xf5da9b,width:2,alpha:.91});
    }
    const glimmer=(motion?.65+.35*Math.sin(sceneTime*2.5+i):.78);
    groundLights.circle(p.x,p.y,17).fill({color:tower?0xe6b971:0x88c7aa,alpha:glimmer*.13});
   }
   const elapsed=Math.max(0,(performance.now()-current.received)/1000);
   const path=current.model.mapPath;
   for(const e of game.enemies){
    const sprite=enemyVisuals.get(e.id)?.sprite;if(!sprite)continue;
    const p=interpolateEnemy(e,path,elapsed,game.speed,phaseReady(current.model)&&motion);
    const bounce=motion&&!frozen?Math.sin(sceneTime*(e.kind==='runner'?17:9)+e.id)*1.5:0;
    sprite.x=p.x;sprite.y=p.y-2+bounce;
    const wobble=motion&&!frozen?Math.sin(sceneTime*5+e.id)*.04:0;
    sprite.rotation=wobble;
    sprite.alpha=enemyVisuals.get(e.id)!.flashUntil>sceneTime? .77:1;
    sprite.zIndex=p.y;
    const r=e.kind==='brute'?27:e.kind==='runner'?18:21;
    hud.roundRect(p.x-23,p.y-r-17,46,6,3).fill({color:0x091c20,alpha:.96});
    const percentage=Math.min(1,Math.max(0,e.hp/maxHp(e.kind,game.wave)));
    hud.roundRect(p.x-22,p.y-r-16,44*percentage,4,2)
     .fill({color:percentage<.34?0xf2917f:e.slow>0?0x85d9ee:0x99e0ae});
    if(e.slow>0){
     hud.circle(p.x,p.y,24).stroke({color:0x8ce7ff,width:1.8,alpha:.35});
     for(let n=0;n<3;n++){
      const a=n*Math.PI*2/3+(motion?sceneTime*.4:0);
      hud.circle(p.x+Math.cos(a)*25,p.y+Math.sin(a)*25,2.2)
       .fill({color:0xc0f3ff,alpha:.62});
     }
    }
   }
   for(const tower of game.towers){
    const visual=towerVisuals.get(tower.pad);if(!visual)continue;
    const p=current.model.pads[tower.pad];
    const recentlyFired=motion?Math.max(0,1-(sceneTime-visual.firedAt)/.19):0;
    visual.sprite.x=p.x;visual.sprite.y=p.y-7+recentlyFired*3;
    visual.sprite.scale.set((.59+.018*(tower.level-1))*(1-recentlyFired*.05));
    // Gold and coloured upgrade accents follow the native tower level.
    const c=color[tower.kind];
    for(let i=0;i<tower.level;i++){
     const x=p.x+(i-(tower.level-1)/2)*10,y=p.y+28;
     hud.circle(x,y,3.4).fill({color:0x152c33});
     hud.circle(x,y,2.4).fill({color:c,alpha:.95});
    }
    if(tower.level===3)hud.circle(p.x,p.y,30).stroke({color:c,width:2,alpha:.42});
   }
   if(motion&&!frozen){
    for(const flight of flights)flight.age+=dt*(game.phase==='running'?game.speed:1);
    for(const burst of bursts)burst.age+=dt;
   }else if(game.phase!=='running'){
    for(const flight of flights)flight.age+=dt;
    for(const burst of bursts)burst.age+=dt;
   }
   for(let i=flights.length-1;i>=0;i--){
    const f=flights[i];
    if(f.age>f.life){emit(f.to.x,f.to.y,'impact',i+Math.floor(sceneTime*60));flights.splice(i,1);continue;}
    const t=Math.max(0,Math.min(1,f.age/f.life)),p=easeOut(t);
    const x=f.from.x+(f.to.x-f.from.x)*p,
     y=f.from.y+(f.to.y-f.from.y)*p-(f.kind==='cannon'?32*Math.sin(Math.PI*p):0);
    const clr=color[f.kind];
    if(f.kind==='archer'){
     const angle=Math.atan2(f.to.y-f.from.y,f.to.x-f.from.x);
     const dx=Math.cos(angle)*13,dy=Math.sin(angle)*13;
     shots.moveTo(x-dx,y-dy).lineTo(x+dx,y+dy).stroke({color:0xf8e9c2,width:3.6,alpha:.95});
     shots.moveTo(x-dx*1.5,y-dy*1.5).lineTo(x-dx*.4,y-dy*.4)
      .stroke({color:0xe6bc76,width:6,alpha:.25});
    }else if(f.kind==='frost'){
     shots.circle(x,y,13).fill({color:0x75d8f4,alpha:.15});
     shots.circle(x,y,7).fill({color:clr,alpha:.67});
     shots.circle(x,y,3).fill({color:0xf1ffff});
     for(let j=0;j<4;j++){
      const a=j*Math.PI/2+sceneTime*6;
      shots.circle(x+Math.cos(a)*13,y+Math.sin(a)*13,2.2)
       .fill({color:0xd2f9ff,alpha:.82});
     }
    }else{
     shots.circle(x,y,13).fill({color:0xfb854b,alpha:.20});
     shots.circle(x,y,8).fill({color:0x563c33});
     shots.circle(x,y,5).fill({color:0xf6bd89});
     shots.circle(x,y,2).fill({color:0xfff1bc});
    }
   }
   for(let i=bursts.length-1;i>=0;i--){
    const b=bursts[i];if(b.age>b.life){bursts.splice(i,1);continue;}
    const t=b.age/b.life,fade=1-t,c=bColor(b.kind);
    const segments=b.kind==='gate'?18:b.kind==='upgrade'?16:10;
    if(b.kind==='upgrade'||b.kind==='impact'||b.kind==='gate')
     shots.circle(b.x,b.y,7+t*(b.kind==='gate'?75:46))
      .stroke({color:c,width:3*(1-t)+.3,alpha:fade*.74});
    for(let j=0;j<segments;j++){
     const a=Math.PI*2*j/segments+range(b.seed+j,0,.4);
     const d=(8+range(j*17+b.seed,15,65))*easeOut(t);
     const px=b.x+Math.cos(a)*d,py=b.y+Math.sin(a)*d;
     shots.circle(px,py,Math.max(.5,3.3*(1-t))).fill({color:j%3===0?0xffffff:c,alpha:fade*.85});
    }
   }
  }
  const handleClick=(event:PointerEvent)=>{
   if(!canvas)return;
   const rect=canvas.getBoundingClientRect();
   const x=(event.clientX-rect.left)/rect.width*960;
   const y=(event.clientY-rect.top)/rect.height*600;
   let chosen=-1,min=41;
   for(let i=0;i<last.current.model.pads.length;i++){
    const p=last.current.model.pads[i],dist=Math.hypot(x-p.x,y-p.y);
    if(dist<min){min=dist;chosen=i;}
   }
   if(chosen!==-1)last.current.onSelect(chosen);
  };
  const visibility=()=>{
   if(!ready)return;
   if(document.hidden)app.ticker.stop();
   else app.ticker.start();
  };
  (async()=>{
   await app.init({width:960,height:600,backgroundColor:0x162d2d,antialias:true,
    resolution:Math.min(2,window.devicePixelRatio||1),autoDensity:true});
   if(disposed){app.destroy(true);return;}
   app.ticker.maxFPS=60;
   canvas=app.canvas;canvas.addEventListener('pointerdown',handleClick);
   host.appendChild(canvas);
   const results=await Promise.allSettled(assets.map(name=>
    Assets.load<Texture>(art+name+'.svg')));
   if(disposed){app.destroy(true);return;}
   results.forEach((result,i)=>{
    if(result.status==='fulfilled')textures[assets[i]]=result.value;
   });
   if(textures.battlefield){
    bg=new Sprite(textures.battlefield);bg.width=960;bg.height=600;
   }else{
    bg=new Graphics().rect(0,0,960,600).fill({color:0x24463d});
   }
   app.stage.addChild(bg);
   pads=mkGraphics();
   const model=last.current.model;
   for(const p of model.pads){
    pads.ellipse(p.x+2,p.y+9,34,14).fill({color:0x071e26,alpha:.68});
    pads.circle(p.x,p.y,31).fill({color:0x122b32,alpha:.94});
    pads.circle(p.x,p.y,30).stroke({color:0xc7ab73,width:3,alpha:.8});
    pads.circle(p.x,p.y,23).fill({color:0x335349,alpha:.88});
    pads.circle(p.x,p.y,22).stroke({color:0xe6c98d,width:1.3,alpha:.56});
    for(let k=0;k<6;k++){
     const a=Math.PI*k/3;
     pads.circle(p.x+Math.cos(a)*26,p.y+Math.sin(a)*26,2.4)
      .fill({color:0xeccf97,alpha:.78});
    }
    pads.moveTo(p.x-8,p.y).lineTo(p.x+8,p.y).stroke({color:0xb4dfc5,width:2.6,alpha:.72});
    pads.moveTo(p.x,p.y-8).lineTo(p.x,p.y+8).stroke({color:0xb4dfc5,width:2.6,alpha:.72});
   }
   app.stage.addChild(pads);
   groundLights=mkGraphics();app.stage.addChild(groundLights);
   towerLayer=new Container();app.stage.addChild(towerLayer);
   enemyLayer=new Container();enemyLayer.sortableChildren=true;app.stage.addChild(enemyLayer);
   weather=mkGraphics();app.stage.addChild(weather);
   overlay=mkGraphics();app.stage.addChild(overlay);
   shots=mkGraphics();app.stage.addChild(shots);
   hud=mkGraphics();app.stage.addChild(hud);
   ready=true;
   host.dataset.renderer=results.every(r=>r.status==='fulfilled')?'illustrated':'fallback';
   reconcile();
   app.ticker.add(render);
   document.addEventListener('visibilitychange',visibility);
   visibility();
  })().catch(()=>{
   if(!disposed)host.dataset.renderer='error';
  });
  return()=>{
   disposed=true;
   document.removeEventListener('visibilitychange',visibility);
   if(canvas)canvas.removeEventListener('pointerdown',handleClick);
   if(ready)app.ticker.remove(render);
   for(const visual of towerVisuals.values())visual.sprite.removeFromParent();
   for(const visual of enemyVisuals.values())visual.sprite.removeFromParent();
   towerVisuals.clear();enemyVisuals.clear();flights.length=0;bursts.length=0;
   if(ready)app.destroy(true);
  };
 },[]);
 return <div ref={mount} className="td-map td-illustrated" data-testid="tower-defense-map"
  role="img" aria-label="Illustrated moonlit fortress battlefield with ten buildable tower sites"/>;
}
