import {useEffect,useRef,useState} from 'react';
import {Application,Container,Graphics,Text} from 'pixi.js';
import {WHEEL,color,pocketAngle} from './rouletteModel';
const TAU=Math.PI*2;
const ease=(t:number)=>1-Math.pow(1-t,3);
export function RouletteWheel({number,spinId,motion,onStart,onDone}:{number:number|null;spinId:string;motion:boolean;onStart:()=>void;onDone:()=>void}){
 const host=useRef<HTMLDivElement>(null), scene=useRef<{wheel:Container;ball:Graphics;app:Application}|null>(null);
 const done=useRef(onDone);done.current=onDone;
 const begin=useRef(onStart);begin.current=onStart;
 const [ready,setReady]=useState(false);
 const [failed,setFailed]=useState(false);
 useEffect(()=>{
  let disposed=false, initialized=false;const app=new Application();
  async function init(){
   try{
    await app.init({width:520,height:520,backgroundAlpha:0,antialias:true,resolution:Math.min(window.devicePixelRatio||1,2),autoDensity:true,preference:'webgl'});
    initialized=true;if(disposed){app.destroy(true,{children:true});return;}
    const root=new Container();root.position.set(260,260);app.stage.addChild(root);
    const base=new Graphics();
    base.circle(0,0,250).fill(0x17110e).circle(0,0,246).stroke({color:0x8d5126,width:7}).circle(0,0,231).fill(0x492314).circle(0,0,224).stroke({color:0xe5b96a,width:3}).circle(0,0,214).fill(0x171718).circle(0,0,197).stroke({color:0xb58a47,width:2});for(let i=0;i<18;i++){const radius=230+i*.85;base.circle(0,0,radius).stroke({color:i%3===0?0x9a6035:0x4b2b1c,width:.65,alpha:.55});}
    base.circle(0,0,218).stroke({color:0xf3d08d,width:2}).circle(0,0,211).stroke({color:0x513c25,width:4});root.addChild(base);
    const wheel=new Container();root.addChild(wheel);
    const pockets=new Graphics();const step=TAU/37;
    WHEEL.forEach((n,i)=>{
     const a=-Math.PI/2+i*step-step/2,b=a+step;
     pockets.moveTo(Math.cos(a)*194,Math.sin(a)*194).arc(0,0,194,a,b).lineTo(Math.cos(b)*126,Math.sin(b)*126).arc(0,0,126,b,a,true).closePath().fill(color(n)==='red'?0xa42636:color(n)==='green'?0x13775a:0x20242d).stroke({color:0xc4a267,width:1.4});
    });wheel.addChild(pockets);
    WHEEL.forEach((n,i)=>{const a=-Math.PI/2+i*step;const t=new Text({text:String(n),style:{fontFamily:'Arial',fontSize:16,fontWeight:'bold',fill:0xfff5dd}});t.anchor.set(.5);t.position.set(Math.cos(a)*166,Math.sin(a)*166);t.rotation=a+Math.PI/2;wheel.addChild(t);});
    const hub=new Graphics().circle(0,0,125).fill(0x573321).circle(0,0,122).stroke({color:0xe3b76a,width:3}).circle(0,0,99).fill(0x211a18).circle(0,0,97).stroke({color:0x90693a,width:2});
    for(let i=0;i<8;i++){const a=i*TAU/8;hub.moveTo(Math.cos(a)*34,Math.sin(a)*34).lineTo(Math.cos(a)*96,Math.sin(a)*96).stroke({color:0x8b6138,width:3});}
    hub.moveTo(0,-29).lineTo(8,-8).lineTo(29,0).lineTo(8,8).lineTo(0,29).lineTo(-8,8).lineTo(-29,0).lineTo(-8,-8).closePath().fill(0xc99b57).stroke({color:0xf5d895,width:1.5});
    hub.moveTo(0,-16).lineTo(5,0).lineTo(0,16).lineTo(-5,0).closePath().fill(0xf7db9e);hub.circle(0,0,112).stroke({color:0xc09a5b,width:1.5}).circle(0,0,105).stroke({color:0x77502d,width:2});wheel.addChild(hub);
    const ball=new Graphics().circle(2,4,8).fill({color:0x000000,alpha:.5}).circle(0,0,8).fill(0xf7e9c9).circle(-2,-3,3).fill(0xffffff);ball.position.set(0,-207);root.addChild(ball);
    app.canvas.style.width='100%';app.canvas.style.height='auto';host.current?.appendChild(app.canvas);scene.current={wheel,ball,app};setReady(true);
   }catch{if(!disposed)setFailed(true);if(initialized&&!disposed){app.destroy(true,{children:true});initialized=false;}}
  }void init();return()=>{disposed=true;scene.current=null;if(initialized)app.destroy(true,{children:true});};
 },[]);
 useEffect(()=>{
  if(number===null||!spinId)return;
  let frame=0;let cancelled=false;
  const s=scene.current;
  if(!s||!ready){if(failed){const timer=window.setTimeout(()=>done.current(),0);return()=>clearTimeout(timer);}return;}
  begin.current();
  const start=performance.now(),duration=motion?6100:1, initial=s.wheel.rotation, final=initial+TAU*4+1.2;
  const target=pocketAngle(number)+final;
  const ballStart=Math.atan2(s.ball.y,s.ball.x), diff=((target-ballStart)%TAU+TAU)%TAU-TAU*7;
  function draw(now:number){
   if(cancelled||!s)return;const t=Math.min(1,(now-start)/duration), e=ease(t);
   s.wheel.rotation=initial+(final-initial)*e;
   const angle=ballStart+diff*ease(Math.min(1,t*1.03));
   const drop=Math.max(0,(t-.57)/.43),rad=207-43*ease(drop)+Math.sin(drop*5*Math.PI)*5*(1-drop);
   s.ball.position.set(Math.cos(angle)*rad,Math.sin(angle)*rad);
   if(t<1)frame=requestAnimationFrame(draw);else done.current();
  }frame=requestAnimationFrame(draw);return()=>{cancelled=true;cancelAnimationFrame(frame);};
 },[number,spinId,ready,failed,motion]);
 return <div className="roulette-wheel" ref={host} aria-label="Roulette wheel">{!ready&&<div className="roulette-wheel-fallback"><span>✦</span>{failed&&number!==null&&<b>{number}</b>}</div>}</div>;
}
