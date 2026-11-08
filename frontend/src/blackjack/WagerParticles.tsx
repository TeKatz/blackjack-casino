import {useEffect,useRef} from 'react';

/** A single, small, lazy PixiJS canvas for localized wager FX.
 * Decorative only: the authoritative wager is always provided by C#.
 */
export function WagerParticles({enabled}:{enabled:boolean}){
 const host=useRef<HTMLSpanElement>(null);
 useEffect(()=>{
  const element=host.current;
  if(!enabled||!element||typeof window==='undefined'||
    window.matchMedia('(prefers-reduced-motion: reduce)').matches)return;
  let disposed=false;
  let instance:import('pixi.js').Application|undefined;
  let visibility=()=>{};
  void (async()=>{
   try{
    const {Application,Graphics}=await import('pixi.js');
    if(disposed)return;
    const app=new Application();
    await app.init({resizeTo:element,backgroundAlpha:0,preference:'webgl',
      antialias:true,autoDensity:true,resolution:Math.min(1.5,window.devicePixelRatio||1)});
    if(disposed){app.destroy(true);return;}
    instance=app;
    element.appendChild(app.canvas);
    app.ticker.maxFPS=30;
    let time=0;
    const nextRandom=(()=>{let state=0x6f14ae1;return()=>{
      state=(Math.imul(state,1664525)+1013904223)>>>0;
      return state/4294967296;
    };})();
    const specs=Array.from({length:32},(_,i)=>{
      const seed=nextRandom(),angle=nextRandom()*Math.PI*2,
        phase=nextRandom()*Math.PI*2,speed=.22+nextRandom()*.38,
        radius=.55+nextRandom()*1.6;
      const color=i%7===0?0xb2f8d9:i%4===0?0xfff6d1:0xeab66b;
      const node=new Graphics().circle(0,0,radius).fill({color});
      app.stage.addChild(node);
      return {node,seed,angle,phase,speed,radius};
    });
    const arcs=[0,1].map((index)=>{
      const g=new Graphics().ellipse(0,0,72+index*20,24+index*11).stroke({
        color:index===0?0xfbe1a0:0x70d6ab,width:index===0?1.4:1,
        alpha:index===0?.5:.3
      });
      app.stage.addChild(g);
      return g;
    });
    app.ticker.add(ticker=>{
      time+=ticker.deltaTime/60;
      const w=element.clientWidth,h=element.clientHeight;
      const unit=Math.min(w/320,h/238);
      for(const [i,ring] of arcs.entries()){
        ring.position.set(w*.49,h*.57);
        ring.scale.set(unit);
        ring.rotation=-.16+Math.sin(time*.32+i*1.4)*.09;
        ring.alpha=.2+.19*(.5+.5*Math.sin(time*.75+i));
      }
      for(const p of specs){
        const angle=p.angle+time*p.speed*.28;
        const x=w*.49+Math.cos(angle)*w*(.25+p.seed*.11)+Math.sin(time*.66+p.phase)*w*.014;
        const y=h*.56+Math.sin(angle)*h*(.11+p.seed*.12)-
          (time*.023*p.speed+p.seed)%1*h*.17;
        p.node.position.set(x,y);
        p.node.alpha=.13+.58*Math.pow(Math.sin(time*1.5+p.phase),2);
        p.node.scale.set(unit*(.75+.25*Math.sin(time+p.phase)));
      }
    });
    visibility=()=>{if(document.hidden)app.ticker.stop();else app.ticker.start();};
    document.addEventListener('visibilitychange',visibility);
    if(document.hidden)app.ticker.stop();
   }catch{
    // Static SVG and its CSS lighting remain usable on older WebView2/GPU setups.
   }
  })();
  return()=>{
   disposed=true;
   document.removeEventListener('visibilitychange',visibility);
   if(instance){instance.destroy(true);instance=undefined;}
  };
 },[enabled]);
 return <span ref={host} className="bj-wager-pixi" aria-hidden="true"/>;
}
