import {useEffect,useRef} from 'react';
import {Application,Graphics} from 'pixi.js';

/** Purely decorative GPU particles; all accessibility, values and controls stay in React. */
export function RooftopAmbient({enabled}:{enabled:boolean}){
  const host=useRef<HTMLDivElement>(null);
  useEffect(()=>{
    const target=host.current;
    if(!target||!enabled)return;
    let disposed=false,app:Application|undefined;
    let visible=()=>{};
    (async()=>{
      const stage=new Application();
      try{
        await stage.init({resizeTo:target,backgroundAlpha:0,antialias:false,
          resolution:Math.min(window.devicePixelRatio||1,1.5),autoDensity:true,preference:'webgl'});
        if(disposed){stage.destroy(true);return;}
        app=stage;stage.canvas.setAttribute('aria-hidden','true');target.appendChild(stage.canvas);
        const stars=Array.from({length:38},(_,i)=>{
          const dot=new Graphics().circle(0,0,i%6===0?2:1).fill({
            color:i%3===0?0xe5bd74:0xc18bf9,alpha:.2+(i%5)*.065});
          dot.x=(i*83.29%100)*target.clientWidth/100;
          dot.y=(i*41.63%100)*target.clientHeight/100;
          stage.stage.addChild(dot);
          return {dot,phase:i*1.96,rate:.06+(i%6)*.019};
        });
        stage.ticker.maxFPS=30;
        let tick=0;
        stage.ticker.add(clock=>{
          tick+=clock.deltaTime;
          for(const star of stars){
            star.dot.y-=clock.deltaTime*star.rate;
            if(star.dot.y<0)star.dot.y=target.clientHeight;
            star.dot.alpha=.28+.36*Math.sin(tick*.013+star.phase);
          }
        });
        visible=()=>document.hidden?stage.ticker.stop():stage.ticker.start();
        document.addEventListener('visibilitychange',visible);
      }catch{stage.destroy(true);}
    })();
    return()=>{disposed=true;document.removeEventListener('visibilitychange',visible);app?.destroy(true);};
  },[enabled]);
  return <div className="nine-pixi" ref={host} aria-hidden="true"/>;
}
