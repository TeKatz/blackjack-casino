import {memo,useEffect,useRef} from 'react';
import {Application,Graphics} from 'pixi.js';
function MergeAmbientCanvas({enabled}:{enabled:boolean}){
 const host=useRef<HTMLDivElement>(null);
 useEffect(()=>{
   const node=host.current;
   if(!node||!enabled)return;
   let stopped=false,app:Application|undefined,visibility=()=>{};
   (async()=>{
    const scene=new Application();
    try{
     await scene.init({resizeTo:node,backgroundAlpha:0,antialias:false,
       autoDensity:true,resolution:Math.min(window.devicePixelRatio||1,1.5),preference:'webgl'});
     if(stopped){scene.destroy(true);return;}
     app=scene;scene.canvas.setAttribute('aria-hidden','true');node.appendChild(scene.canvas);
     const specks=Array.from({length:45},(_,i)=>{
       const graphic=new Graphics().circle(0,0,1+(i%4)*.45).fill({color:i%3?0xdbb77d:0x9adbc1,alpha:.25});
       graphic.x=(i*71.4%100)*node.clientWidth/100;
       graphic.y=(i*27.73%100)*node.clientHeight/100;
       scene.stage.addChild(graphic);return {graphic,speed:.04+(i%7)*.014,phase:i*1.13};
     });
     let clock=0;scene.ticker.maxFPS=30;
     scene.ticker.add(delta=>{
       clock+=delta.deltaTime;
       for(const s of specks){
         s.graphic.y-=s.speed*delta.deltaTime;
         s.graphic.alpha=.16+.23*Math.sin(clock*.011+s.phase);
         if(s.graphic.y<0)s.graphic.y=node.clientHeight;
       }
     });
     visibility=()=>document.hidden?scene.ticker.stop():scene.ticker.start();
     document.addEventListener('visibilitychange',visibility);
    }catch{scene.destroy(true);}
   })();
   return()=>{stopped=true;document.removeEventListener('visibilitychange',visibility);app?.destroy(true);};
 },[enabled]);
 return <div ref={host} className="merge-ambient" aria-hidden="true"/>;
}

export const MergeAmbient=memo(MergeAmbientCanvas);
