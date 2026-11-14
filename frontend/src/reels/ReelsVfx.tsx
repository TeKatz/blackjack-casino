import {useEffect,useRef} from 'react';
import {Application,Graphics} from 'pixi.js';
import type {ReelsSpeed} from './reelsMotion';

type Props={enabled:boolean;spinning:boolean;celebrationId:string|null;jackpot:boolean;speed:ReelsSpeed};

/** Decorative GPU effects. Never calculates stops, winnings, XP or player data.
 * Reels and their CSS timing stay in the authoritative React/NativeShell flow. */
export function ReelsVfx({enabled,spinning,celebrationId,jackpot,speed}:Props){
 const mount=useRef<HTMLDivElement>(null);
 const state=useRef({spinning,celebrationId,jackpot,speed});
 state.current={spinning,celebrationId,jackpot,speed};
 useEffect(()=>{
  const host=mount.current;
  if(!host||!enabled)return;
  const reduced=window.matchMedia?.('(prefers-reduced-motion: reduce)');
  if(reduced?.matches)return;
  let disposed=false,app:Application|undefined,clock=0,seen:string|null=null;
  let burstAge=100,visible=!document.hidden;
  const motes:{art:Graphics;phase:number;depth:number;speed:number}[]=[];
  const burst:{art:Graphics;angle:number;radius:number}[]=[];
  const halos:Graphics[]=[];
  const columns:{art:Graphics;phase:number}[]=[];
  const syncVisibility=()=>{
   visible=!document.hidden;
   if(!app)return;
   if(visible)app.ticker.start();else app.ticker.stop();
  };
  const onReduced=()=>{
   if(reduced?.matches)app?.ticker.stop();
   else syncVisibility();
  };
  document.addEventListener('visibilitychange',syncVisibility);
  reduced?.addEventListener('change',onReduced);
  void (async()=>{
   const instance=new Application();
   try{
    await instance.init({resizeTo:host,backgroundAlpha:0,antialias:false,
      resolution:Math.min(window.devicePixelRatio||1,1.35),
      autoDensity:true,preference:'webgl'});
    if(disposed){instance.destroy(true);return;}
    app=instance;
    instance.stage.eventMode='none';
    instance.canvas.setAttribute('aria-hidden','true');
    instance.canvas.setAttribute('data-testid','reels-pixi-canvas');
    host.appendChild(instance.canvas);
    // Reusable geometric Art Deco glows create spatial depth in the empty
    // centre without filling it with flat panels or textures.
    for(let i=0;i<3;i++){
      const halo=new Graphics().circle(0,0,88+i*48)
        .stroke({width:i===0?1.5:1,color:i===1?0xa8e3c1:0xd6af69,alpha:i===0 ? 0.29 : 0.12})
        .circle(0,0,118+i*43)
        .stroke({width:1,color:0xe3c48c,alpha:.10});
      instance.stage.addChild(halo);halos.push(halo);
    }
    for(let i=0;i<8;i++){
      const art=new Graphics().moveTo(0,0).lineTo(0,115+(i%4)*35)
        .stroke({color:i%3===0?0xbce8d1:0xe9c184,alpha:.12,width:1});
      art.alpha=.25;
      instance.stage.addChild(art);
      columns.push({art,phase:i*2.6});
    }
    // Small, reusable geometry: no texture churn, no new Graphics during spins.
    for(let i=0;i<47;i++){
     const art=new Graphics().circle(0,0,.75+(i%4)*.33)
       .fill({color:i%7===0?0x90ecd1:i%3===0?0xffffff:0xe3b869,alpha:.7});
     instance.stage.addChild(art);
     motes.push({art,phase:i*2.399,depth:.28+(i%5)*.15,speed:1.8+(i%7)*.64});
    }
    for(let i=0;i<30;i++){
     const art=new Graphics()
       .moveTo(0,-6).lineTo(1.6,-1).lineTo(0,5).lineTo(-1.6,1)
       .closePath().fill({color:i%4===0?0xa9ffdf:0xffd78d});
     art.visible=false;
     instance.stage.addChild(art);
     burst.push({art,angle:(i/30)*Math.PI*2,radius:48+(i%6)*19});
    }
    instance.ticker.maxFPS=25;
    instance.ticker.add(tick=>{
     if(!visible||reduced?.matches)return;
     const d=Math.min(tick.deltaMS/1000,.05);
     clock+=d;
     const {spinning,celebrationId:effectId,jackpot:bigWin,speed:motionSpeed}=state.current;
     // Animated reel motion and result UI remain browser-compositor driven at
     // 60 FPS; idle accents are deliberately only 25 FPS.
     instance.ticker.maxFPS=spinning&&motionSpeed!=='turbo'?60:25;
     const w=host.clientWidth||1,h=host.clientHeight||1;
     halos.forEach((halo,i)=>{
       halo.position.set(w*.51,h*.48);
       halo.scale.set(Math.max(.75,w/1200)*(.92+.05*Math.sin(clock*.16+i)),.7);
       halo.rotation=clock*(i%2?-0.035:0.045);
       halo.alpha=.36+.1*Math.sin(clock*.38+i);
     });
     columns.forEach(({art,phase},i)=>{
       art.position.set((i+.5)/8*w, .13*h);
       art.rotation=Math.sin(clock*.17+phase)*.13;
       art.alpha=.14+.09*Math.sin(clock*.45+phase);
     });
     // Low-amplitude gold/teal ambience, speed responding to reel motion.
     motes.forEach((m,i)=>{
      const y=((i*47.3-clock*m.speed*(spinning?5:1))%(h+30)+(h+30))%(h+30)-15;
      const x=(i*111.7+Math.sin(clock*.5+m.phase)*26*m.depth)%w;
      m.art.position.set(x,y);
      const intensity=motionSpeed==='turbo' ? 0.72 : spinning ? 1.45 : 1;
      m.art.alpha=(.11+.22*(.5+.5*Math.sin(clock*1.1+m.phase)))*intensity;
      m.art.scale.set(spinning?(motionSpeed==='turbo'?1:1.45):.85+m.depth*.35);
     });
     if(effectId!==seen){
      seen=effectId;
      if(effectId)burstAge=0;
     }
     if(burstAge<1.7)burstAge+=d*(motionSpeed==='turbo'?2.4:1);
     for(let i=0;i<burst.length;i++){
      const {art,angle,radius}=burst[i];
      const t=burstAge/1.7;
      art.visible=t<1;
      if(!art.visible)continue;
      const spread=radius*(bigWin?1.55:1);
      const dist=8+spread*(1-Math.pow(1-t,2));
      art.position.set(w*.5+Math.cos(angle)*dist,h*.48+Math.sin(angle)*dist*.72);
      art.rotation=angle+clock*.6;
      art.alpha=Math.pow(1-t,1.6)*.88;
      art.scale.set((bigWin?1.5:1)*(.6+.55*t));
     }
    });
    syncVisibility();
   }catch(error){
    // A missing GPU/driver cannot block the existing playable React reels.
    console.warn('Midnight Reels GPU accents unavailable',error);
    try{instance.destroy(true);}catch{/* Partial GPU initialization. */}
   }
  })();
  return()=>{
   disposed=true;
   document.removeEventListener('visibilitychange',syncVisibility);
   reduced?.removeEventListener('change',onReduced);
   app?.ticker.stop();
   app?.destroy(true);
  };
 },[enabled]);
 return <div ref={mount} className="reels-pixi-vfx" aria-hidden="true"/>;
}
