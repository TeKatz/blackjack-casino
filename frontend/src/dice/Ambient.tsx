import {useEffect,useRef} from 'react';
import {Application,Graphics} from 'pixi.js';
/** Decorative low-frequency glints. Never influences dice or scores. */
export function DiceAmbient({enabled}:{enabled:boolean}){
 const mount=useRef<HTMLDivElement>(null);
 useEffect(()=>{
  const element=mount.current;if(!enabled||!element)return;
  let ended=false,app:Application|undefined;
  let onVisibility=()=>{};
  (async()=>{
   const scene=new Application();
   try{
    await scene.init({resizeTo:element,backgroundAlpha:0,antialias:false,
      autoDensity:true,resolution:Math.min(window.devicePixelRatio||1,1.5),preference:'webgl'});
    if(ended){scene.destroy(true);return;}
    app=scene;element.appendChild(scene.canvas);scene.canvas.setAttribute('aria-hidden','true');
    const sparks=Array.from({length:42},(_,i)=>{
      const dot=new Graphics().circle(0,0,1.2+(i%4)*.5)
        .fill({color:i%3===0?0x8fceb6:0xe7c88d,alpha:.27});
      dot.x=(i*53.77%100)/100*element.clientWidth;
      dot.y=(i*27.13%100)/100*element.clientHeight;
      scene.stage.addChild(dot);
      return {dot,rate:.025+(i%6)*.013,phase:i*1.5};
    });
    let frame=0;scene.ticker.maxFPS=30;
    scene.ticker.add(t=>{
      frame+=t.deltaTime;
      for(const s of sparks){
        s.dot.y-=s.rate*t.deltaTime;
        s.dot.alpha=.17+.14*Math.sin(frame*.012+s.phase);
        if(s.dot.y<0)s.dot.y=element.clientHeight;
      }
    });
    onVisibility=()=>document.hidden?scene.ticker.stop():scene.ticker.start();
    document.addEventListener('visibilitychange',onVisibility);
   }catch{scene.destroy(true);}
  })();
  return()=>{ended=true;document.removeEventListener('visibilitychange',onVisibility);app?.destroy(true);};
 },[enabled]);
 return <div className="dice-ambient" ref={mount} aria-hidden="true"/>;
}
