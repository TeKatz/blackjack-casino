import {useEffect,useRef} from 'react';
import {Application,Container,Graphics,Text} from 'pixi.js';
import type {SweepState} from './minesweeperBridge';

type Particle={x:number;y:number;vx:number;vy:number;life:number;max:number;color:number};
type Props={state:SweepState;animations:boolean};
const palette=[0,0xa6ebed,0x8dc99d,0xffbe87,0xbcaaff,0xf0cf8a,0x94b6ff,0xf9a2ad,0xffe7bc];
const spark=(seed:number)=>{const x=Math.sin(seed*127.17+43.71)*43758.45;return x-Math.floor(x);};

/** Draws original vector tile art, luminous hazard sigils and recovery particles in PixiJS.
 * HTML hit targets live above the canvas for keyboard, touch and screen readers. */
export function SweepField({state,animations}:Props){
 const host=useRef<HTMLDivElement>(null);
 const latest=useRef({state,animations});
 const repaint=useRef<(()=>void)|null>(null);
 latest.current={state,animations};
 useEffect(()=>{repaint.current?.();},[state,animations]);
 useEffect(()=>{
  const mount=host.current;if(!mount)return;
  const app=new Application();
  let disposed=false,ready=false,art:Container|null=null,over:Graphics|null=null;
  let signature='',previous:SweepState|null=null,particles:Particle[]=[];
  let passage=0;
  const reduced=window.matchMedia?.('(prefers-reduced-motion: reduce)')?.matches??false;
  const motion=()=>latest.current.animations&&!reduced;
  function emit(x:number,y:number,color:number,count:number){
   if(!motion())return;
   for(let j=0;j<count&&particles.length<160;j++){
    const angle=spark(j*11+x+y+passage)*Math.PI*2,speed=1.8+spark(j*9+y)*4;
    particles.push({x,y,vx:Math.cos(angle)*speed,vy:Math.sin(angle)*speed-1.7,
     life:.36+spark(x+j*13)*.56,max:.92,color});
   }
  }
  function tileMark(g:Graphics,x:number,y:number,r:number,color:number){
   g.circle(x,y,r*.94).fill({color,alpha:.17});
   g.circle(x,y,r*.69).stroke({color,width:2.5,alpha:.92});
   for(let n=0;n<8;n++){
    const a=n*Math.PI/4,dx=Math.cos(a),dy=Math.sin(a);
    g.moveTo(x+dx*r*.82,y+dy*r*.82).lineTo(x+dx*r*1.16,y+dy*r*1.16)
      .stroke({color,width:Math.max(1.5,r*.13),alpha:.93});
   }
   g.poly([x,y-r*.45,x+r*.32,y,x,y+r*.45,x-r*.32,y])
     .fill({color:0xffdfb5,alpha:.91});
   g.circle(x,y,r*.18).fill({color:0xfff4d7});
  }
  function draw(){
   if(!ready)return;
   const m=latest.current.state,n=m.cells.length;
   const key=m.runId+'|'+m.phase+'|'+m.cells.map(c=>
    (c.revealed?'1':'0')+(c.flagged?'1':'0')+(c.scanned?'1':'0')+
    (c.mine?'1':'0')+c.adjacent.toString()+c.scanCount.toString()).join('');
   if(key===signature)return;
   signature=key;
   if(previous?.runId===m.runId){
    m.cells.forEach((c,i)=>{
     if(c.revealed&&!previous!.cells[i]?.revealed){
      const span=744/m.width,x=28+(i%m.width+.5)*span,y=28+(Math.floor(i/m.width)+.5)*span;
      emit(x,y,c.mine?0xff7b80:0xace8cc,Math.min(5,n>144?2:4));
     }
    });
    if(m.phase!==previous.phase&&m.phase==='won')
      for(let k=0;k<55;k++)emit(40+spark(k*3)*720,90+spark(k*13)*550,0xffd892,2);
    if(m.phase!==previous.phase&&m.phase==='lost')
      for(let k=0;k<30;k++)emit(50+spark(k*5)*700,120+spark(k*7)*560,0xf58291,2);
   }
   previous=m;
   if(art){art.removeFromParent();art.destroy({children:true});}
   art=new Container();app.stage.addChildAt(art,0);
   const g=new Graphics();art.addChild(g);
   g.roundRect(7,7,786,786,36).fill({color:0x050f1d,alpha:.9});
   g.roundRect(8,8,784,784,35).stroke({color:0xb6a882,width:2,alpha:.72});
   g.roundRect(15,15,770,770,30).stroke({color:0x688aa4,width:1.2,alpha:.42});
   g.roundRect(20,20,760,760,25).fill({color:0x0c1e31,alpha:.96});
   for(let k=0;k<110;k++){
    const x=30+spark(k*13+6)*740,y=30+spark(k*17+21)*740;
    g.circle(x,y,k%7===0?2:1).fill({color:k%3===0?0xfce2ae:0xaddfe4,alpha:.06+spark(k*11)*.28});
   }
   const tile=744/m.width;
   for(let i=0;i<n;i++){
    const x=28+(i%m.width)*tile,y=28+Math.floor(i/m.width)*tile;
    const c=m.cells[i],lostMine=m.phase==='lost'&&c.mine,
     detonated=i===m.explodedIndex&&m.phase==='lost';
    g.roundRect(x+2.4,y+5.8,tile-4.8,tile-6,Math.max(3,tile*.13))
      .fill({color:0x020914,alpha:.82});
    if(c.revealed){
     g.roundRect(x+2,y+2,tile-4,tile-4,Math.max(3,tile*.12))
      .fill({color:c.mine?0x693343:0x18374a});
     g.roundRect(x+4,y+4,tile-8,tile-8,Math.max(3,tile*.1))
      .stroke({color:c.mine?0xff788b:0x638b9a,width:1,alpha:c.mine?.85:.3});
    }else{
     const accent=c.flagged?0xc5dc9e:c.scanned?0x95e4f4:0x98c6cc;
     g.roundRect(x+2,y+2,tile-4,tile-4,Math.max(3,tile*.12))
      .fill({color:lostMine?0x503246:0x214054});
     g.roundRect(x+3.5,y+3,tile-7,tile*.48,Math.max(2,tile*.09))
      .fill({color:accent,alpha:.07});
     g.moveTo(x+tile*.17,y+tile*.2).lineTo(x+tile*.83,y+tile*.2)
      .stroke({color:accent,width:1.3,alpha:.22});
     g.roundRect(x+2,y+2,tile-4,tile-4,Math.max(3,tile*.12))
      .stroke({color:accent,width:c.flagged||c.scanned?2:1,alpha:c.flagged||c.scanned?.83:.34});
    }
    const cx=x+tile/2,cy=y+tile/2;
    if(c.flagged&&!c.revealed&&!lostMine){
     g.moveTo(cx-tile*.13,cy+tile*.23).lineTo(cx-tile*.13,cy-tile*.26)
      .stroke({color:0xe9d9ae,width:Math.max(2,tile*.045)});
     g.poly([cx-tile*.1,cy-tile*.26,cx+tile*.24,cy-tile*.14,cx-tile*.1,cy+tile*.05])
      .fill({color:0xe7bb75});
     g.moveTo(cx-tile*.27,cy+tile*.25).lineTo(cx+tile*.15,cy+tile*.25)
      .stroke({color:0xe9d9ae,width:Math.max(2,tile*.05)});
    }else if((c.revealed&&c.mine)||lostMine){
     tileMark(g,cx,cy,tile*.23,detonated?0xff627b:0xe7a2ae);
     if(detonated)g.circle(cx,cy,tile*.36).stroke({color:0xff7891,width:3,alpha:.89});
    }else if(c.revealed&&c.adjacent>0){
     const txt=new Text({text:String(c.adjacent),style:{
      fontFamily:'Georgia, serif',fontWeight:'bold',fontSize:tile*.57,fill:palette[c.adjacent]}});
     txt.anchor.set(.5);txt.position.set(cx,cy);
     art.addChild(txt);
    }else if(c.scanned&&!c.revealed){
     g.circle(cx,cy,tile*.34).stroke({color:0x9ce8ef,width:2,alpha:.65});
     g.circle(cx,cy,tile*.27).fill({color:0x1c6074,alpha:.72});
     const txt=new Text({text:String(c.scanCount),style:{
      fontFamily:'Georgia, serif',fontWeight:'bold',fontSize:tile*.42,fill:0xe2feff}});
     txt.anchor.set(.5);txt.position.set(cx,cy);art.addChild(txt);
    }else if(!c.revealed){
     g.circle(cx,cy,tile*.048).fill({color:0x98d9d7,alpha:.22});
    }
   }
   if(over){over.removeFromParent();app.stage.addChild(over);}
  }
  function tick(){
   if(!ready||!over)return;
   const dt=Math.min(.04,app.ticker.deltaMS/1000);
   over.clear();
   if(!motion()){particles=[];return;}
   passage+=dt;
   for(let i=particles.length-1;i>=0;i--){
    const p=particles[i];p.life-=dt;if(p.life<=0){particles.splice(i,1);continue;}
    p.x+=p.vx;p.y+=p.vy;p.vy+=.085;
    const alpha=Math.min(1,p.life/p.max);
    over.circle(p.x,p.y,1.3+2*alpha).fill({color:p.color,alpha:alpha*.8});
   }
   for(let i=0;i<13;i++){
    const x=42+spark(i*31)*720,y=40+spark(i*67)*720;
    over.circle(x,y,1.3+Math.sin(passage*.5+i)*.6)
     .fill({color:i%2?0xf9dfb0:0xa7ecf1,alpha:.18+.12*Math.sin(passage*.8+i)});
   }
  }
  void app.init({width:800,height:800,antialias:true,backgroundAlpha:0,resolution:Math.min(2,window.devicePixelRatio||1)})
   .then(()=>{
    if(disposed){app.destroy(true,{children:true});return;}
    ready=true;
    mount.appendChild(app.canvas);
    app.canvas.style.width='100%';app.canvas.style.height='100%';
    over=new Graphics();app.stage.addChild(over);
    repaint.current=draw;draw();app.ticker.add(tick);
   }).catch(()=>{/* The accessible HTML controls remain operational without WebGL. */});
  return ()=>{
   disposed=true;repaint.current=null;
   if(ready){app.ticker.remove(tick);app.destroy(true,{children:true});}
  };
 },[]);
 return <div ref={host} className="sweep-pixi" aria-hidden="true"/>;
}
