import {memo,useEffect,useRef,useState} from 'react';
import {Application,Container,Graphics} from 'pixi.js';
import {boundedPixelRatio} from '../graphics/rendererDiagnostics';
import type {Research} from './empireBridge';

type StageControl={sync:()=>void;paint:()=>void};

export const CommandAtmosphere=memo(function CommandAtmosphere({enabled,earning,focusFloor,burst}:{enabled:boolean;earning:boolean;focusFloor:number|null;burst:number}){
  const hostRef=useRef<HTMLDivElement>(null);
  const stateRef=useRef({enabled,earning,focusFloor,burst});
  const controller=useRef<StageControl|null>(null);
  const [fallback,setFallback]=useState(false);
  stateRef.current={enabled,earning,focusFloor,burst};
  useEffect(()=>{
    const host=hostRef.current;if(!host)return;
    let disposed=false,app:Application|undefined,observer:ResizeObserver|undefined;
    let clock=0,previousBurst=stateRef.current.burst,burstAge=9999;
    let width=1,height=1;
    const waves=new Graphics(),flash=new Graphics(),columns=new Container();
    const dust=Array.from({length:36},(_,index)=>{
      const orb=new Graphics().circle(0,0,1.1+(index%3)*.45).fill({color:index%4===0?0x72d6c9:0xf0c887,alpha:.7});
      orb.alpha=.3;columns.addChild(orb);return orb;
    });
    const paint=()=>{
      if(!app)return;
      width=Math.max(1,host.clientWidth);height=Math.max(1,host.clientHeight);
      app.renderer.resize(width,height,boundedPixelRatio(window.devicePixelRatio));
      waves.clear();flash.clear();
      for(let i=1;i<3;i++)waves.moveTo(width*i/3,12).lineTo(width*i/3,height-12).stroke({color:0x7cb7a2,alpha:.09,width:1});
      app.render();
    };
    const sync=()=>{
      if(!app)return;
      if(stateRef.current.enabled&&!document.hidden)app.ticker.start();
      else{app.ticker.stop();paint();}
      host.dataset.ticker=app.ticker.started?'running':'stopped';
    };
    (async()=>{
      const instance=new Application();
      try{
        await instance.init({width:1,height:1,backgroundAlpha:0,antialias:false,autoStart:false,sharedTicker:false,
          autoDensity:true,resolution:boundedPixelRatio(window.devicePixelRatio),preference:'webgl'});
        if(disposed){instance.destroy(true,{children:true});return;}
        app=instance;host.appendChild(instance.canvas);instance.canvas.setAttribute('aria-hidden','true');
        app.stage.addChild(waves,columns,flash);
        dust.forEach((dot,index)=>dot.position.set((index*193+39)%1000/1000*width,(index*117+81)%1000/1000*height));
        app.ticker.maxFPS=30;
        app.ticker.add(ticker=>{
          const dt=Math.min(ticker.deltaMS,70);clock+=dt;
          const flags=stateRef.current;
          if(flags.burst!==previousBurst){previousBurst=flags.burst;burstAge=0;}
          burstAge+=dt;
          for(let i=0;i<dust.length;i++){
            const dot=dust[i];
            dot.x=(i*193+39)%1000/1000*width+Math.sin(clock*.00022+i)*11;
            dot.y-=dt*(flags.earning?.009:.002)*(1+i%3);
            if(dot.y<0)dot.y=height;
            dot.alpha=(flags.earning?.15:.06)+.17*(1+Math.sin(clock*.001+i*3))*.5;
          }
          flash.clear();
          if(flags.earning){
            for(let i=0;i<3;i++){
              const beam=(Math.sin(clock*.0014+i*1.8)+1)*.5;
              flash.roundRect(i*width/3+5,height-4,width/3-10,3,2).fill({color:0x6edbb0,alpha:.14+beam*.22});
            }
          }
          if(flags.focusFloor!==null){
            const i=Math.max(0,Math.min(2,flags.focusFloor-1));
            flash.roundRect(i*width/3+4,4,width/3-8,height-8,11)
              .stroke({color:0xf2d290,alpha:.24+Math.sin(clock*.003)*.11,width:2});
          }
          if(burstAge<650){
            const progress=burstAge/650,rad=6+progress*22;
            for(let i=0;i<3;i++){
              const cx=(i+.5)*width/3,cy=height-12;
              flash.circle(cx,cy,rad).stroke({color:0xffd783,alpha:(1-progress)*.12,width:1.5});
              for(let j=0;j<5;j++){
                const a=j*Math.PI*2/5+progress*1.2;
                flash.circle(cx+Math.cos(a)*rad,cy+Math.sin(a)*rad,2).fill({color:0xffd783,alpha:(1-progress)*.65});
              }
            }
          }
        });
        observer=new ResizeObserver(paint);observer.observe(host);paint();
        controller.current={sync,paint};document.addEventListener('visibilitychange',sync);sync();
      }catch{
        if(!disposed)setFallback(true);
        app?.destroy(true,{children:true});app=undefined;
      }
    })();
    return()=>{
      disposed=true;controller.current=null;
      document.removeEventListener('visibilitychange',sync);observer?.disconnect();
      app?.destroy(true,{children:true});
    };
  },[]);
  useEffect(()=>controller.current?.sync(),[enabled]);
  return <div ref={hostRef} className="cc-atmosphere" data-testid="cc-pixi-atmosphere" data-fallback={fallback}/>;
});

export const ResearchLattice=memo(function ResearchLattice({items,positions,focus,enabled}:{items:Research[];positions:Record<string,{x:number;y:number}>;focus:string|null;enabled:boolean}){
  const hostRef=useRef<HTMLDivElement>(null);
  const stateRef=useRef({items,positions,focus,enabled});
  const controller=useRef<StageControl|null>(null);
  const [fallback,setFallback]=useState(false);
  stateRef.current={items,positions,focus,enabled};
  useEffect(()=>{
    const host=hostRef.current;if(!host)return;
    let app:Application|undefined,disposed=false,observer:ResizeObserver|undefined,clock=0;
    let width=1,height=1;
    const links=new Graphics(),halos=new Graphics();
    const redraw=()=>{
      if(!app)return;
      const {items,positions,focus}=stateRef.current;
      links.clear();halos.clear();
      const point=(id:string)=>{const p=positions[id];return p?{x:width*p.x/100,y:height*p.y/100}:null;};
      items.forEach(item=>{
        if(!item.prerequisite)return;
        const from=point(item.prerequisite),to=point(item.id);if(!from||!to)return;
        const reached=item.rank>0,highlight=focus===item.id||focus===item.prerequisite;
        const tint=reached?0x7be5b3:highlight?0xe4bc79:0x57796c;
        links.moveTo(from.x,from.y).bezierCurveTo((from.x+to.x)*.5,from.y,(from.x+to.x)*.5,to.y,to.x,to.y)
          .stroke({color:tint,width:highlight?3:2,alpha:highlight?.8:reached?.55:.26});
      });
      items.forEach((item,index)=>{
        const p=point(item.id);if(!p)return;
        const active=focus===item.id,completed=item.rank>=item.maxRank;
        const base=active?0xeec477:completed?0x81e1b9:item.canBuy?0x86d5ad:0x73867e;
        const breath=stateRef.current.enabled?Math.sin(clock*.002+index*1.7)*.5+.5:.35;
        halos.circle(p.x,p.y,24+breath*(active?12:5)).stroke({color:base,alpha:(active?.37:.10)+breath*.08,width:active?3:2});
        if(item.canBuy)halos.circle(p.x,p.y,33+breath*4).stroke({color:base,alpha:.08+breath*.09,width:1});
      });
    };
    const resize=()=>{
      if(!app)return;
      width=Math.max(1,host.clientWidth);height=Math.max(1,host.clientHeight);
      app.renderer.resize(width,height,boundedPixelRatio(window.devicePixelRatio));redraw();app.render();
    };
    const sync=()=>{
      if(!app)return;
      if(stateRef.current.enabled&&!document.hidden)app.ticker.start();
      else{app.ticker.stop();redraw();app.render();}
      host.dataset.ticker=app.ticker.started?'running':'stopped';
    };
    (async()=>{
      const instance=new Application();
      try{
        await instance.init({width:1,height:1,backgroundAlpha:0,antialias:true,autoStart:false,sharedTicker:false,
          autoDensity:true,resolution:boundedPixelRatio(window.devicePixelRatio),preference:'webgl'});
        if(disposed){instance.destroy(true,{children:true});return;}
        app=instance;host.appendChild(instance.canvas);instance.canvas.setAttribute('aria-hidden','true');
        app.stage.addChild(links,halos);
        app.ticker.maxFPS=30;
        app.ticker.add(ticker=>{clock+=Math.min(70,ticker.deltaMS);redraw();});
        observer=new ResizeObserver(resize);observer.observe(host);
        controller.current={sync,paint:()=>{if(!stateRef.current.enabled){redraw();app?.render();}}};
        document.addEventListener('visibilitychange',sync);resize();sync();
      }catch{
        if(!disposed)setFallback(true);
        app?.destroy(true,{children:true});app=undefined;
      }
    })();
    return()=>{
      disposed=true;controller.current=null;observer?.disconnect();
      document.removeEventListener('visibilitychange',sync);app?.destroy(true,{children:true});
    };
  },[]);
  useEffect(()=>controller.current?.sync(),[enabled]);
  useEffect(()=>controller.current?.paint(),[items,positions,focus]);
  return <div ref={hostRef} className="cc-research-lattice" data-testid="cc-pixi-research" data-fallback={fallback}/>;
});
