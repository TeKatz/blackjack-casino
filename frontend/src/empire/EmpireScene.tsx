import {memo,useEffect,useRef,useState} from 'react';
import {Application,Graphics,Container} from 'pixi.js';
import {EmpireArtwork} from './EmpireArtwork';
export {EmpireArtwork,roomArt} from './EmpireArtwork';
import {boundedPixelRatio,summarizeFrameTimes} from '../graphics/rendererDiagnostics';
import type {RendererDiagnostics} from '../graphics/rendererDiagnostics';

export const EmpireScene=memo(function EmpireScene({roomId,enabled,diagnostics,onReady}:{roomId:string;enabled:boolean;diagnostics:boolean;onReady:()=>void}) {
  const target=useRef<HTMLDivElement>(null);
  const control=useRef<{sync:()=>void}|null>(null);
  const flags=useRef({enabled,diagnostics});flags.current={enabled,diagnostics};
  const ready=useRef(onReady);ready.current=onReady;
  const [sample,setSample]=useState<RendererDiagnostics|null>(null);
  const [fallback,setFallback]=useState(false);
  useEffect(()=>{
    const host=target.current;if(!host)return;
    let disposed=false,app:Application|undefined,observer:ResizeObserver|undefined,ratioMedia:MediaQueryList|undefined;
    let previous=0,lastReport=0,clock=0;const samples:number[]=[];
    const sync=()=>{
      if(!app)return;
      previous=0;samples.length=0;
      if(flags.current.enabled&&!document.hidden)app.ticker.start();
      else {app.ticker.stop();app.render();setSample(null);}
      host.dataset.ticker=app.ticker.started?'running':'stopped';
    };
    const resize=()=>{
      if(!app)return;
      const w=Math.max(1,host.clientWidth),h=Math.max(1,host.clientHeight);
      app.renderer.resize(w,h,boundedPixelRatio(window.devicePixelRatio));
      app.stage.scale.set(w/960,h/440);app.render();
    };
    const bindRatio=()=>{
      ratioMedia?.removeEventListener('change',ratioChanged);
      ratioMedia=matchMedia(`(resolution: ${window.devicePixelRatio}dppx)`);
      ratioMedia.addEventListener('change',ratioChanged);
    };
    const ratioChanged=()=>{resize();bindRatio();};
    (async()=>{
      const instance=new Application();
      try{
        await instance.init({width:1,height:1,backgroundAlpha:0,antialias:false,autoStart:false,sharedTicker:false,
          autoDensity:true,resolution:boundedPixelRatio(window.devicePixelRatio),preference:'webgl'});
        if(disposed){instance.destroy(true,{children:true});return;}
        app=instance;host.appendChild(instance.canvas);instance.canvas.setAttribute('aria-hidden','true');
        const lights=new Container();
        // Retained translucent spotlights and dust. Nothing is redrawn or allocated per frame.
        for(let i=0;i<5;i++)lights.addChild(new Graphics().poly([100+i*185,0,150+i*185,0,310+i*150,440,-70+i*230,440]).fill({color:0xffdf9d,alpha:.035}));
        instance.stage.addChild(lights);
        const particles=Array.from({length:28},(_,i)=>{
          const dot=new Graphics().circle(0,0,.9+i%3*.45).fill({color:i%4?0xf7db9e:0x8ce8cc,alpha:.5});
          dot.position.set(i*137%960,i*83%440);instance.stage.addChild(dot);return dot;
        });
        instance.ticker.maxFPS=30;
        instance.ticker.add(ticker=>{
          clock+=ticker.deltaMS;
          lights.alpha=.55+Math.sin(clock*.00035)*.2;
          for(let i=0;i<particles.length;i++){
            const dot=particles[i];dot.y-=ticker.deltaMS*(.003+i%4*.001);if(dot.y<0)dot.y=440;
            dot.alpha=.2+.35*(1+Math.sin(clock*.0008+i))*.5;
          }
          if(flags.current.diagnostics){
            const now=performance.now();if(previous>0)samples.push(now-previous);previous=now;
            if(samples.length>300)samples.shift();
            if(now-lastReport>2000){lastReport=now;setSample(summarizeFrameTimes(samples,host.clientWidth,host.clientHeight,window.devicePixelRatio));}
          }
        });
        observer=new ResizeObserver(resize);observer.observe(host);bindRatio();resize();sync();
        document.addEventListener('visibilitychange',sync);control.current={sync};ready.current();
      }catch{
        if(!disposed){setFallback(true);ready.current();}
        // Gameplay remains available when GPU initialization is unavailable.
        if(app){app.destroy(true,{children:true});app=undefined;}
      }
    })();
    return()=>{
      disposed=true;control.current=null;observer?.disconnect();ratioMedia?.removeEventListener('change',ratioChanged);
      document.removeEventListener('visibilitychange',sync);app?.destroy(true,{children:true});
    };
  },[]);
  useEffect(()=>{control.current?.sync();},[enabled,diagnostics]);
  return <div className="empire-scene" data-room={roomId}>
    <EmpireArtwork className="empire-scene-art" roomId={roomId}/><div className="empire-scene-shade"/>
    <div ref={target} className="empire-pixi" data-fallback={fallback}/>
    {diagnostics&&<output className="empire-diagnostics">{!enabled?'Ticker paused':fallback?'Static artwork':sample?
      `Ticker ${sample.fpsEstimate}/s · mean ${sample.averageMs} ms · p95 ${sample.p95Ms} ms · ${sample.width}×${sample.height} @${sample.pixelRatio}`:'Sampling ticker intervals…'}</output>}
  </div>;
});
