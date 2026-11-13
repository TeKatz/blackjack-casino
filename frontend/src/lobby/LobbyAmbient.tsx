import {useEffect,useRef} from 'react';
import {Application,Graphics} from 'pixi.js';
import {createModeScenes} from './ModeScenes';
import {createShopFx} from './ShopFxEngine';

/** Single shared, transparent WebGL scene behind accessible React text/controls. */
export function LobbyAmbient({motionEnabled}:{motionEnabled:boolean}){
  const target=useRef<HTMLDivElement>(null);
  const motionRef=useRef(motionEnabled);
  motionRef.current=motionEnabled;
  useEffect(()=>{
    const host=target.current;
    if(!host)return;
    let disposed=false;
    let app:Application|undefined;
    let disposeScenes=()=>{};
    let syncVisibility=()=>{};
    let hostVisible=true;
    const hostMessage=(event:MessageEvent)=>{
      const value:unknown=event.data;
      if(!value||typeof value!=='object')return;
      const message=value as {type?:unknown;data?:{visible?:unknown}};
      if(message.type!=='lobby.visibility'||typeof message.data?.visible!=='boolean')return;
      hostVisible=message.data.visible;
      syncVisibility();
    };
    window.chrome?.webview?.addEventListener('message',hostMessage);
    (async()=>{
      const instance=new Application();
      try{
        await instance.init({
          resizeTo:host,backgroundAlpha:0,antialias:true,
          resolution:Math.min(window.devicePixelRatio||1,1.5),
          autoDensity:true,preference:'webgl',
        });
        if(disposed){instance.destroy(true);return;}
        app=instance;
        instance.canvas.setAttribute('aria-hidden','true');
        host.appendChild(instance.canvas);
        const root=host.parentElement??host;
        const scene=createModeScenes(instance.stage,root,()=>motionRef.current);
        const shop=createShopFx(instance.stage,root);
        disposeScenes=()=>{shop?.destroy();scene.destroy();};
        const dust=Array.from({length:16},(_,i)=>{
          const dot=new Graphics().circle(0,0,.85+(i%3)*.4)
            .fill({color:i%5===0?0x87d7b1:0xe1be82,alpha:.15});
          dot.x=((i*191.7)%100)*host.clientWidth/100;
          dot.y=((i*37.3)%100)*host.clientHeight/100;
          instance.stage.addChild(dot);
          return {dot,phase:i*2.21,speed:.08+(i%5)*.025};
        });
        let clock=0;
        // Disabled animations still render illustrations, but at a reduced tick
        // rate to keep scroll/resize geometry correct without ambient movement.
        instance.ticker.maxFPS=30;
        instance.ticker.add(ticker=>{
          const motion=motionRef.current;
          instance.ticker.maxFPS=motion?30:12;
          clock+=ticker.deltaTime/60;
          scene.tick(clock);
          if(motion)shop.tick(clock,ticker.deltaTime);
          for(const {dot,phase,speed} of dust){
            dot.visible=motion;
            if(!motion)continue;
            dot.y-=speed*ticker.deltaTime;
            if(dot.y < -7)dot.y=host.clientHeight+7;
            dot.x+=Math.sin(clock*.46+phase)*.05*ticker.deltaTime;
            dot.alpha=.32+.22*Math.sin(clock*.8+phase);
          }
        });
        syncVisibility=()=>{
          if(document.hidden||!hostVisible)instance.ticker.stop();
          else instance.ticker.start();
        };
        document.addEventListener('visibilitychange',syncVisibility);
        syncVisibility();
        // Asset errors are isolated per game mode and never block navigation.
        void scene.ready;
      }catch(error){
        console.warn('Lobby GPU presentation unavailable',error);
        try{instance.destroy(true);}catch{/* partial initialization cleanup */}
      }
    })();
    return()=>{
      disposed=true;
      document.removeEventListener('visibilitychange',syncVisibility);
      window.chrome?.webview?.removeEventListener('message',hostMessage);
      disposeScenes();
      app?.destroy(true);
    };
  },[]);
  return <div ref={target} className="lobby-pixi" aria-hidden="true"/>;
}
