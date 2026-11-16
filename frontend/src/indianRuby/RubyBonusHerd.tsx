import {useEffect,useRef} from 'react';
import type {RefObject} from 'react';
import type {Application} from 'pixi.js';
import type {HerdPhase,createRubyHerd} from './rubyHerdScene';

type Props={
 phase:HerdPhase|'prepare';
 introDuration:number;
 foreground:RefObject<HTMLDivElement|null>;
 background:RefObject<HTMLDivElement|null>;
};

/** A single decorative canvas follows the existing bonus phases. */
export function RubyBonusHerd({phase,introDuration,foreground,background}:Props){
 const scene=useRef<{sync:()=>void}|null>(null);
 const shot=useRef({phase,introDuration,started:0});
 useEffect(()=>{
  shot.current={phase,introDuration,started:performance.now()};
  scene.current?.sync();
 },[phase,introDuration]);

 useEffect(()=>{
  let disposed=false;
  let app:Application|undefined;
  let observer:ResizeObserver|undefined;
  let host:HTMLDivElement|null=null;
  let paint:ReturnType<typeof createRubyHerd>|undefined;
  const draw=()=>{
   const {phase,started,introDuration}=shot.current;
   if(phase!=='prepare')paint?.(phase,(performance.now()-started)/1000,introDuration/1000);
  };
  const resize=()=>{
   if(!app||!host||!host.clientWidth||!host.clientHeight)return;
   app.renderer.resize(host.clientWidth,host.clientHeight,Math.min(window.devicePixelRatio||1,1.5));
   draw();
   if(!document.hidden)app.render();
  };
  const sync=()=>{
   if(!app)return;
   const phase=shot.current.phase;
   const next=phase==='intro'?foreground.current:phase==='background'?background.current:null;
   if(next!==host){
    observer?.disconnect();
    host=next;
    if(host){
     host.appendChild(app.canvas);
     observer?.observe(host);
     resize();
    }else app.canvas.remove();
   }
   if(host&&!document.hidden)app.start();
   else app.stop();
  };
  document.addEventListener('visibilitychange',sync);

  // Warm the renderer while the native bonus-bearing base result is still spinning.
  // Missing graphics support must never hold up a committed spin.
  void (async()=>{
   let instance:Application|undefined;
   try{
    const [{Application},{createRubyHerd}]=await Promise.all([import('pixi.js'),import('./rubyHerdScene')]);
    if(disposed)return;
    instance=new Application();
    // The herd uses vector paths only. An isolated Canvas renderer avoids
    // a new WebGL context during the paid spin in WebView2.
    await instance.init({width:1,height:1,preference:['canvas'],backgroundAlpha:0,
     antialias:true,autoDensity:true,autoStart:false});
    if(disposed){instance.destroy(true,{children:true});return;}
    instance.canvas.dataset.testid='ruby-herd-canvas';
    paint=createRubyHerd(instance);
    instance.ticker.maxFPS=60;
    instance.ticker.add(draw);
    observer=new ResizeObserver(resize);
    app=instance;
    scene.current={sync};
    sync();
   }catch(error){
    observer?.disconnect();
    scene.current=null;
    app=undefined;
    if(instance?.renderer)instance.destroy(true,{children:true});
    console.warn('Sunset Ruby herd effects unavailable',error);
   }
  })();

  return()=>{
   disposed=true;
   scene.current=null;
   document.removeEventListener('visibilitychange',sync);
   observer?.disconnect();
   app?.destroy(true,{children:true});
  };
 },[foreground,background]);

 return null;
}
