import {useEffect,useRef} from 'react';
import {Application,Container,Graphics} from 'pixi.js';
import {impactColor} from './presentation';
import type {NeonImpact} from './presentation';

type Props={enabled:boolean;spinning:boolean;energy:number;bonus:boolean;impact:NeonImpact|null};
type Particle={shape:Graphics;u:number;v:number;phase:number;speed:number};
type Point={x:number;y:number};
const TAU=Math.PI*2;
const rng=(initial:number)=>{
 let seed=initial>>>0;
 return ()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296;};
};
const within=(x:number,min:number,max:number)=>Math.max(min,Math.min(max,x));
const perimeter=(distance:number,x:number,y:number,w:number,h:number):Point=>{
 const length=2*(w+h),d=((distance%length)+length)%length;
 if(d<w)return {x:x+d,y};
 if(d<w+h)return {x:x+w,y:y+d-w};
 if(d<2*w+h)return {x:x+w-(d-w-h),y:y+h};
 return {x,y:y+h-(d-2*w-h)};
};

/**
 * One PixiJS 8 renderer draws both the living ambient field and the full
 * reactive cabinet. React only changes semantic event state; Pixi owns motion.
 * All coordinates are derived from actual DOM bounds so DPI/window resizing
 * never changes how the frame hugs the cabinet.
 */
export function VaultAmbient({enabled,spinning,energy,bonus,impact}:Props){
 const host=useRef<HTMLDivElement>(null);
 const live=useRef({spinning,energy,bonus,impact});
 live.current={spinning,energy,bonus,impact};

 useEffect(()=>{
  const target=host.current;
  const root=target?.parentElement;
  if(!target||!root||!enabled)return;
  let disposed=false;
  let app:Application|null=null;
  let visibility=()=>{};
  const random=rng(0x09fafe12);
  const particles:Particle[]=[];
  const railNodes:Graphics[]=[];
  const flareNodes:Graphics[]=[];

  void (async()=>{
   const instance=new Application();
   try{
    await instance.init({
      resizeTo:target,backgroundAlpha:0,antialias:false,
      autoDensity:true,resolution:Math.min(window.devicePixelRatio||1,1.4),
      preference:'webgl',
    });
    if(disposed){instance.destroy(true);return;}
    app=instance;
    target.appendChild(instance.canvas);
    instance.canvas.setAttribute('aria-hidden','true');
    const ambience=new Container();
    const electro=new Container();
    electro.blendMode='add';
    const frame=new Graphics();
    const conduits=new Graphics();
    const scanner=new Graphics();
    const burst=new Graphics();
    instance.stage.addChild(ambience,electro);
    electro.addChild(frame,conduits,scanner,burst);
    for(let i=0;i<44;i++){
      const shape=new Graphics().circle(0,0,i%11===0?1.9:.7+random()*1.1)
        .fill({color:i%4===0?0xeb8eff:0x6bdfff,alpha:.7});
      ambience.addChild(shape);
      particles.push({shape,u:random(),v:random(),phase:random()*TAU,speed:.09+random()*.29});
    }
    for(let i=0;i<20;i++){
      const shape=new Graphics().circle(0,0,i%5===0?3:1.5)
        .fill({color:i%3===0?0xc97cff:0x78ecff});
      electro.addChild(shape);
      railNodes.push(shape);
    }
    for(let i=0;i<36;i++){
      const shape=new Graphics().circle(0,0,i%6===0?3:1.5+random()*1.6)
        .fill({color:i%4===0?0xffffff:0xffd386});
      electro.addChild(shape);
      flareNodes.push(shape);
    }
    const machine=root.querySelector<HTMLElement>('.vault-machine');
    const board=root.querySelector<HTMLElement>('.vault-board');
    let seconds=0,arrival=0,lastImpactId='';
    instance.ticker.maxFPS=30;
    instance.ticker.add(ticker=>{
      seconds+=within(ticker.deltaMS,0,100)/1000;
      const state=live.current;
      const bounds=target.getBoundingClientRect();
      const area=machine?.getBoundingClientRect();
      const screen=board?.getBoundingClientRect();
      if(!area||!screen||area.width<80||area.height<80)return;
      const x=area.left-bounds.left,y=area.top-bounds.top;
      const w=area.width,h=area.height;
      const bx=screen.left-bounds.left,by=screen.top-bounds.top;
      const bw=screen.width,bh=screen.height;
      const key=state.impact?.id??'';
      if(key!==lastImpactId){arrival=seconds;lastImpactId=key;}
      const age=seconds-arrival;
      const impact=state.impact;
      const impactAmount=impact?Math.max(0,1-age/(impact.strength===3?3.2:2.6)):0;
      const force=impactAmount*(impact?.strength??0)/3;
      const color=impact&&force>0?impactColor(impact.kind):
        state.bonus?0xb8a3ff:0x46ddfa;
      const scanPower=state.spinning?1:.25;
      for(const p of particles){
        const drift=(seconds*p.speed+p.v)%1;
        p.shape.position.set(
          bounds.width*(p.u+.014*Math.sin(seconds*.48+p.phase)),
          bounds.height*(1-drift));
        p.shape.alpha=.05+.18*Math.pow(Math.sin(seconds*.9+p.phase),2);
        p.shape.scale.set(.65+(.3+.4*Math.sin(seconds*.7+p.phase)));
      }
      // Several interlocking glow rails and a breathing perimeter follow the
      // entire machine, not just the 20 symbol cells.
      const pulse=.5+.5*Math.sin(seconds*2.15);
      frame.clear()
        .roundRect(x+3,y+3,w-6,h-6,18)
        .stroke({color,width:2.4+force*7,alpha:.31+.21*pulse+.45*force})
        .roundRect(x+10,y+10,w-20,h-20,14)
        .stroke({color:0x84dcff,width:1.3+force*1.8,alpha:.24+.35*force})
        .roundRect(bx-4,by-4,bw+8,bh+8,16)
        .stroke({color,width:1.7+force*4.5,alpha:.27+.3*scanPower+.4*force});
      conduits.clear();
      for(let i=0;i<4;i++){
        const d=(seconds*(state.spinning?110:65)+i*(w+h)*.48)%(2*(w+h));
        const a=perimeter(d,x+7,y+7,w-14,h-14);
        const b=perimeter(d+Math.min(44,w*.075),x+7,y+7,w-14,h-14);
        // Tiny luminous PCB traces glide along the real cabinet edges.
        if(Math.hypot(b.x-a.x,b.y-a.y)<60)
          conduits.moveTo(a.x,a.y).lineTo(b.x,b.y).stroke({
            color:i%2===0?0x82fdff:0xe29cff,width:2.8+force*2,
            alpha:.48+.3*Math.sin(seconds*1.6+i)**2});
      }
      // A restrained translucent CRT sweep and short diagnostic side bars.
      const scanY=by+((seconds*(state.spinning?165:45))%Math.max(1,bh));
      scanner.clear().rect(bx+4,scanY,bw-8,state.spinning?4:2)
        .fill({color:state.spinning?0x7af6ff:0x65a9f7,alpha:state.spinning?.23:.065});
      for(let i=0;i<12;i++){
        const px=x+18+(i+.5)*(w-36)/12,py=y+10;
        scanner.circle(px,py,1.5+(i<state.energy?1.5:0))
          .fill({color:i<state.energy?0xb8ffe6:0x368aaf,
            alpha:i<state.energy?.5+.3*pulse:.20});
      }
      for(let i=0;i<railNodes.length;i++){
        const node=railNodes[i],distance=seconds*(state.spinning?125:52)
          +i*(2*(w+h)/railNodes.length);
        const p=perimeter(distance,x+7,y+7,w-14,h-14);
        node.position.set(p.x,p.y);
        node.tint=color;
        node.alpha=.2+.35*Math.sin(seconds*.85+i*.35)**2+.4*force;
        node.scale.set(1+force*.9);
      }
      burst.clear();
      if(impact&&force>0){
        const cx=bx+bw/2,cy=by+bh/2;
        const radius=Math.min(bw,bh)*(.15+.78*within(age/2,0,1));
        for(let i=0;i<3;i++)
          burst.circle(cx,cy,radius*(.7+i*.23)).stroke({
            color,width:1.3+i*.7,alpha:force*(.36-i*.09)});
        // Visible energy travels from the reel board into all four cabinet edges.
        for(let i=0;i<4;i++){
          const rim=perimeter((i+.5)*(w+h)/2,x+7,y+7,w-14,h-14);
          burst.moveTo(cx,cy).lineTo(rim.x,rim.y)
            .stroke({color,width:1.6,alpha:force*.19});
        }
      }
      for(let i=0;i<flareNodes.length;i++){
        const node=flareNodes[i],angle=i*2.399963229728653;
        const direction=.72+(i%7)*.13;
        node.position.set(bx+bw/2+Math.cos(angle)*direction*(16+age*138),
          by+bh/2+Math.sin(angle)*direction*(12+age*100));
        node.tint=i%3===0?0xffffff:color;
        node.alpha=force*Math.max(0,1-age/2.5)*(.4+(i%4)*.14);
        node.scale.set(.7+force*1.65);
      }
    });
    visibility=()=>{if(document.hidden)instance.ticker.stop();else instance.ticker.start();};
    document.addEventListener('visibilitychange',visibility);
    if(document.hidden)instance.ticker.stop();
   }catch{
    try{instance.destroy(true);}catch{/* WebGL unavailable: DOM cabinet still works. */}
   }
  })();
  return()=>{
    disposed=true;
    document.removeEventListener('visibilitychange',visibility);
    try{app?.destroy(true);}catch{/* Context already lost. */}
  };
 },[enabled]);
 return <div ref={host} className="vault-ambient" data-neon-engine="pixi-cabinet-v2"
   aria-hidden="true"/>;
}
