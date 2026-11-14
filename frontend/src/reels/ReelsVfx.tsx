import {useEffect,useRef} from 'react';
import {Application,Graphics} from 'pixi.js';

type Props={enabled:boolean;spinning:boolean;celebrationId:string|null;jackpot:boolean};

/** Decorative GPU effects. Never calculates stops, winnings, XP or player data.
 * Reels and their CSS timing stay in the authoritative React/NativeShell flow. */
export function ReelsVfx({enabled,spinning,celebrationId,jackpot}:Props){
 const mount=useRef<HTMLDivElement>(null);
 const state=useRef({spinning,celebrationId,jackpot});
 state.current={spinning,celebrationId,jackpot};
 useEffect(()=>{
  const host=mount.current;
  if(!host||!enabled)return;
  const reduced=window.matchMedia?.('(prefers-reduced-motion: reduce)');
  if(reduced?.matches)return;
  let disposed=false,app:Application|undefined,clock=0,seen:string|null=null;
  let burstAge=100,visible=!document.hidden;
  const motes:{art:Graphics;phase:number;depth:number;speed:number}[]=[];
  const burst:{art:Graphics;angle:number;radius:number}[]=[];
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
    // Small, reusable geometry: no texture churn, no new Graphics during spins.
    for(let i=0;i<31;i++){
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
    instance.ticker.maxFPS=30;
    instance.ticker.add(tick=>{
     if(!visible||reduced?.matches)return;
     const d=Math.min(tick.deltaMS/1000,.05);
     clock+=d;
     const {spinning,celebrationId:effectId,jackpot:bigWin}=state.current;
     const w=host.clientWidth||1,h=host.clientHeight||1;
     // Low-amplitude gold/teal ambience, speed responding to reel motion.
     motes.forEach((m,i)=>{
      const y=((i*47.3-clock*m.speed*(spinning?5:1))%(h+30)+(h+30))%(h+30)-15;
      const x=(i*111.7+Math.sin(clock*.5+m.phase)*26*m.depth)%w;
      m.art.position.set(x,y);
      m.art.alpha=(.12+.20*(.5+.5*Math.sin(clock*1.1+m.phase)))*(spinning?1.45:1);
      m.art.scale.set(spinning?1.35:.85+m.depth*.35);
     });
     if(effectId!==seen){
      seen=effectId;
      if(effectId)burstAge=0;
     }
     if(burstAge<1.7)burstAge+=d;
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
