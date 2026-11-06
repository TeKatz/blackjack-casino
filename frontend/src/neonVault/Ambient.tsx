import {useEffect,useRef} from 'react';
import {Application,Graphics} from 'pixi.js';
export function VaultAmbient({enabled}:{enabled:boolean}){
 const host=useRef<HTMLDivElement>(null);
 useEffect(()=>{
   const target=host.current;
   if(!target||!enabled)return;
   let disposed=false,renderer:Application|undefined,visibility=()=>{};
   (async()=>{
     const app=new Application();
     try{
       await app.init({resizeTo:target,backgroundAlpha:0,antialias:false,
         autoDensity:true,resolution:Math.min(window.devicePixelRatio||1,1.5),preference:'webgl'});
       if(disposed){app.destroy(true);return;}
       renderer=app;target.appendChild(app.canvas);app.canvas.setAttribute('aria-hidden','true');
       const lines=Array.from({length:40},(_,i)=>{
         const sprite=new Graphics().rect(0,0,i%7===0?2:1,8+(i%5)*5).fill({
           color:i%3===0?0x986aff:0x41d6f6,alpha:.08+(i%5)*.03});
         sprite.x=(i*71.71%100)*target.clientWidth/100;
         sprite.y=(i*39.17%100)*target.clientHeight/100;
         app.stage.addChild(sprite);
         return {sprite,speed:.13+(i%7)*.035};
       });
       app.ticker.maxFPS=30;
       app.ticker.add(time=>{
         for(const line of lines){
           line.sprite.y+=time.deltaTime*line.speed;
           if(line.sprite.y>target.clientHeight+40)line.sprite.y=-40;
         }
       });
       visibility=()=>{if(document.hidden)app.ticker.stop();else app.ticker.start();};
       document.addEventListener('visibilitychange',visibility);
     }catch{app.destroy(true);}
   })();
   return()=>{disposed=true;document.removeEventListener('visibilitychange',visibility);renderer?.destroy(true);};
 },[enabled]);
 return <div ref={host} className="vault-ambient" aria-hidden="true"/>;
}
