import {drawBeatsRun} from './arcadeBeatsRenderer';
import {driftCenter,driftRoadHalfWidth,driftTrafficAt,gravityGoalY,gravityHazards,gravitySentries} from './arcadeExpansionOneEngine';
import type {ExpansionRun,BeatsRun,HookRun,DriftRun,GravityRun,EchoRun} from './arcadeExpansionOneEngine';
const W=960,H=540,TAU=Math.PI*2;
type Ctx=CanvasRenderingContext2D;
const clamp=(v:number,a:number,b:number)=>Math.max(a,Math.min(b,v));
function round(ctx:Ctx,x:number,y:number,w:number,h:number,r:number,fill:string|CanvasGradient|CanvasPattern,stroke?:string){
 ctx.beginPath();ctx.roundRect(x,y,w,h,r);ctx.fillStyle=fill;ctx.fill();
 if(stroke){ctx.lineWidth=2;ctx.strokeStyle=stroke;ctx.stroke();}
}
function line(ctx:Ctx,points:readonly (readonly [number,number])[],color:string,width=2){
 ctx.beginPath();
 points.forEach(([x,y],i)=>i?ctx.lineTo(x,y):ctx.moveTo(x,y));
 ctx.strokeStyle=color;ctx.lineWidth=width;ctx.stroke();
}
function text(ctx:Ctx,value:string,x:number,y:number,size=15,color='#eed7ae',align:CanvasTextAlign='left'){
 ctx.fillStyle=color;ctx.font='700 '+size+'px Segoe UI, sans-serif';ctx.textAlign=align;
 ctx.fillText(value,x,y);
}
function backdrop(ctx:Ctx,from:string,to:string){
 const g=ctx.createLinearGradient(0,0,W,H);g.addColorStop(0,from);g.addColorStop(1,to);
 ctx.fillStyle=g;ctx.fillRect(0,0,W,H);
 ctx.fillStyle='#acdeda';
 for(let i=0;i<45;i++){
  const x=(i*227+17)%W,y=(i*139+61)%H;
  ctx.globalAlpha=.1+(i%5)*.05;ctx.fillRect(x,y,1.2,1.2);
 }
 ctx.globalAlpha=1;
}
function orb(ctx:Ctx,x:number,y:number,r:number,color:string){
 ctx.save();ctx.shadowColor=color;ctx.shadowBlur=17;
 ctx.fillStyle=color;ctx.beginPath();ctx.arc(x,y,r,0,TAU);ctx.fill();ctx.restore();
}
function beats(ctx:Ctx,s:BeatsRun){drawBeatsRun(ctx,s);}
function hook(ctx:Ctx,s:HookRun){
 backdrop(ctx,'#081c30','#2c2250');
 const cam=s.camera;
 for(let i=0;i<55;i++){
  const wx=(i*173+73)%(W+900),x=((wx-s.camera*.13)%(W+900)+W+900)%(W+900)-180;
  const y=20+(i*71)%340;
  ctx.globalAlpha=.16+(i%4)*.08;
  orb(ctx,x,y,i%5===0?2.1:.9,i%3===0?'#f8daa8':'#a9e9f3');
 }
 ctx.globalAlpha=1;
 for(let depth=0;depth<3;depth++){
  ctx.fillStyle=['#20334d','#16293e','#0e273b'][depth];
  const slide=cam*(.12+depth*.18);
  ctx.beginPath();ctx.moveTo(0,H);
  for(let x=0;x<=W+40;x+=35){
   const t=x+slide;
   const y=350+depth*46+Math.sin(t*.009+depth)*41+Math.sin(t*.021)*18;
   ctx.lineTo(x,y);
  }
  ctx.lineTo(W,H);ctx.closePath();ctx.fill();
 }
 for(let j=0;j<7;j++){
  const x=120+j*350-s.camera*.23;
  if(x>-80&&x<W+80){
   ctx.save();ctx.globalAlpha=.19;
   round(ctx,x,280+(j%3)*25,30,210,10,'#43718a');
   round(ctx,x-14,275+(j%3)*25,58,12,6,'#aec6b7');
   ctx.restore();
  }
 }
 // Bright fixed anchor nodes with a crosshair and an attached physics rope.
 for(const a of s.anchors){
  const x=a.x-cam;
  if(x<-40||x>W+40)continue;
  ctx.strokeStyle='#70f0d35f';ctx.lineWidth=1.5;
  ctx.beginPath();ctx.arc(x,a.y,24,0,TAU);ctx.stroke();
  line(ctx,[[x,a.y-36],[x,a.y+36]],'#83ebca66');
  line(ctx,[[x-36,a.y],[x+36,a.y]],'#83ebca66');
  orb(ctx,x,a.y,9,'#f3d39d');
  orb(ctx,x,a.y,3,'#17313c');
 }
 if(s.anchored>=0){
  const a=s.anchors[s.anchored];
  if(a){
   ctx.save();ctx.shadowColor='#8bf1df';ctx.shadowBlur=14;
   line(ctx,[[s.x-cam,s.y],[a.x-cam,a.y]],'#9de6d7',7);
   line(ctx,[[s.x-cam,s.y],[a.x-cam,a.y]],'#fff0c7',2.4);
   ctx.restore();
   for(let k=0;k<4;k++){
    const t=k/5;
    orb(ctx,(s.x-cam)*(1-t)+(a.x-cam)*t,s.y*(1-t)+a.y*t,2,'#ffe5ad');
   }
  }
 }
 const x=s.x-cam,y=s.y;
 ctx.save();ctx.translate(x,y);ctx.rotate(clamp(s.vx/600,-.45,.45));
 ctx.shadowColor='#8ce5e0';ctx.shadowBlur=18;
 ctx.beginPath();ctx.moveTo(0,-21);ctx.lineTo(17,-7);ctx.lineTo(15,12);
 ctx.lineTo(0,19);ctx.lineTo(-15,12);ctx.lineTo(-17,-7);ctx.closePath();
 ctx.fillStyle='#7dcaca';ctx.fill();ctx.strokeStyle='#ffdfae';ctx.lineWidth=2.5;ctx.stroke();
 round(ctx,-11,-9,22,13,6,'#16394b','#b0fff3');
 ctx.restore();
 orb(ctx,x+4,y-4,2,'#d9ffff');
 for(const side of [-1,1]){
  ctx.save();ctx.globalAlpha=.7;orb(ctx,x+side*11,y+17,4,'#f7c694');ctx.restore();
 }
 for(let i=0;i<5;i++){
  const trailX=x-(i+1)*11,trailY=y+Math.sin(i*.9+s.elapsed*8)*5;
  orb(ctx,trailX,trailY,Math.max(1,4-i*.7),'#4db7b8');
 }
 text(ctx,'DISTANCE '+s.score*22+' m',26,36,17,'#f2d29b');
 text(ctx,s.anchored>=0?'SPACE · RELEASE':'SPACE · GRAPPLE',W-26,36,13,'#9fefdf','right');
 if(s.anchored<0){
  // Guide the eye to the nearest possible anchor.
  const a=s.anchors.find(a=>Math.hypot(a.x-s.x,a.y-s.y)<240);
  if(a){
   ctx.setLineDash([7,7]);line(ctx,[[x,y],[a.x-cam,a.y]],'#a4e8d080',2);ctx.setLineDash([]);
  }
 }
}
function drift(ctx:Ctx,s:DriftRun){
 backdrop(ctx,'#11172d','#172939');
 // Road recedes into the horizon; all offsets derive from one deterministic track function.
 for(let y=65;y<560;y+=10){
  const t=clamp((y-65)/475,0,1);
  const world=s.distance+(420-y)*1.75;
  const roadWidth=driftRoadHalfWidth(world)*(.68+.39*t);
  const center=driftCenter(world);
  ctx.fillStyle=Math.floor(y/18)%2?'#213648':'#263c4f';
  ctx.fillRect(center-roadWidth,y,roadWidth*2,11);
  ctx.fillStyle='#e4c28f';
  ctx.fillRect(center-roadWidth-4,y,4,11);
  ctx.fillRect(center+roadWidth,y,4,11);
  if(Math.floor((s.distance/34+(540-y)/17))%4<2){
   ctx.fillStyle='#b6fff0b0';ctx.fillRect(center-1.5,y,3,9);
  }
 }
 // Perspective beacon gates and roadside greenery.
 for(let j=0;j<8;j++){
  const d=(j*260-(s.distance%260)+260)%2060;
  const y=500-d*.2;
  if(y<40||y>H)continue;
  const cx=driftCenter(s.distance+d);
  const rw=driftRoadHalfWidth(s.distance+d)*(.68+.39*clamp((y-65)/475,0,1));
  for(const side of [-1,1]){
   const x=cx+side*(rw+32);
   orb(ctx,x,y,Math.max(2,8-d/400),'#56aaa0');
  }
 }
 for(let i=Math.max(0,Math.floor((s.distance-900)/425)-1);
 i<Math.floor((s.distance+1400)/425)+3;i++){
  const obstacle=driftTrafficAt(i),ahead=obstacle.distance-s.distance;
  if(ahead<-110||ahead>1320)continue;
  const y=422-ahead*.31,size=14+16*clamp(1-ahead/1300,0,1);
  // These are actual oncoming vehicles, not ambiguous red hazard triangles.
  ctx.save();ctx.translate(obstacle.x,y);
  ctx.shadowColor='#ff958f';ctx.shadowBlur=16;
  round(ctx,-size*.74,-size*1.15,size*1.48,size*2.3,size*.32,'#aa465b','#ffd3ad');
  round(ctx,-size*.55,-size*.57,size*1.1,size*.95,size*.22,'#21364f','#ffdaaa');
  round(ctx,-size*.6,size*.75,size*.38,size*.23,3,'#ffb1aa');
  round(ctx,size*.22,size*.75,size*.38,size*.23,3,'#ffb1aa');
  line(ctx,[[-size*.39,-size*.88],[size*.39,-size*.88]],'#ffe8ba',2);
  ctx.restore();
 }
 ctx.save();ctx.translate(s.x,422);ctx.rotate(-s.vx/900);
 ctx.shadowColor=s.combo>2?'#a4fff1':'#e9b68e';ctx.shadowBlur=21;
 round(ctx,-23,-44,46,88,10,'#7ed8cd','#ffddac');
 round(ctx,-17,-24,34,33,7,'#112e41','#b8fff1');
 round(ctx,-17,19,34,14,4,'#193646');
 round(ctx,-26,-28,6,19,2,'#e6b78e');
 round(ctx,20,-28,6,19,2,'#e6b78e');
 ctx.restore();
 if(s.combo>0){
  for(let i=0;i<4;i++)orb(ctx,s.x+(i-2)*9,470+i%2*6,3,'#b5f4db');
 }
 text(ctx,'DRIFT ×'+Math.max(1,Math.floor(s.combo)+1),25,36,18,'#ffe4a9');
 text(ctx,'DISTANCE '+Math.floor(s.distance/10)+' m',25,61,12,'#a9cfcc');
 text(ctx,'AVOID TRAFFIC · STAY ON THE ROAD',25,83,11,'#e2d2ad');
 text(ctx,'DAMAGE '+s.hits+' / 3',W-25,36,15,s.hits?'#f7a9a2':'#a6e4d5','right');
 text(ctx,'HOLD SPACE FOR DRIFT',W-25,61,12,'#a5ded7','right');
}
function gravity(ctx:Ctx,s:GravityRun){
 backdrop(ctx,'#0b1b32','#20274a');
 for(let i=0;i<9;i++)line(ctx,[[95+i*93,64],[95+i*93,480]],'#b1badd18',1);
 for(let i=0;i<5;i++)line(ctx,[[95,75+i*94],[865,75+i*94]],'#b1badd18',1);
 round(ctx,84,53,792,434,10,'#0e293abb','#597a86');
 round(ctx,92,59,776,21,4,'#42667a','#b8e8d6');
 round(ctx,92,462,776,21,4,'#42667a','#b8e8d6');
 // Hazard stripes are lethal only on contact; level geometry mirrors gravityHazards().
 for(const h of gravityHazards(s.stage)){
  ctx.save();ctx.shadowColor='#ff8c9e';ctx.shadowBlur=15;
  round(ctx,h.x,h.y,h.w,h.h,3,'#ad576a','#ffd0aa');
  for(let x=h.x+7;x<h.x+h.w-5;x+=15){
   ctx.fillStyle='#f9c1a1';ctx.beginPath();
   if(h.y<100){ctx.moveTo(x,h.y+h.h);ctx.lineTo(x+6,h.y-11);ctx.lineTo(x+12,h.y+h.h);}
   else{ctx.moveTo(x,h.y);ctx.lineTo(x+6,h.y+28);ctx.lineTo(x+12,h.y);}
   ctx.closePath();ctx.fill();
  }ctx.restore();
 }
 for(const h of gravitySentries(s.stage,s.elapsed)){
  ctx.save();ctx.shadowColor='#ff9eaa';ctx.shadowBlur=30;
  orb(ctx,h.x,h.y,h.r+8,'#663b6d');
  orb(ctx,h.x,h.y,h.r,'#d88aab');
  orb(ctx,h.x,h.y,Math.max(4,h.r-8),'#ffe1bc');
  ctx.restore();
  for(let i=0;i<6;i++){
   const a=s.elapsed*2.4+i*TAU/6;
   orb(ctx,h.x+Math.cos(a)*(h.r+14),h.y+Math.sin(a)*(h.r+14),2,'#ffb9bc');
  }
 }
 const goalY=gravityGoalY(s.stage);
 ctx.save();ctx.shadowColor='#ffdda4';ctx.shadowBlur=25;
 round(ctx,802,goalY-29,54,58,7,'#1a4f54','#f0c790');
 ctx.restore();
 text(ctx,'EXIT',829,goalY+5,13,'#ffe7bc','center');
 ctx.fillStyle=s.gravity===1?'#9ce9d5':'#e6b9ff';
 ctx.beginPath();ctx.moveTo(s.x,s.y+s.gravity*33);
 ctx.lineTo(s.x-9,s.y+s.gravity*19);ctx.lineTo(s.x+9,s.y+s.gravity*19);ctx.closePath();ctx.fill();
 ctx.save();ctx.globalAlpha=s.invulnerable>0?.45+Math.sin(s.elapsed*20)*.28:1;
 ctx.shadowColor='#8cf0dc';ctx.shadowBlur=18;
 round(ctx,s.x-14,s.y-14,28,28,6,'#8cebd8','#ffdfad');
 ctx.restore();
 text(ctx,'CHAMBER '+s.stage,24,35,17,'#f1d5a5');
 text(ctx,'LIVES '+s.lives,W-25,35,15,'#b7f3db','right');
 text(ctx,s.stage<4?'SPACE · FLIP GRAVITY':'WATCH FOR MOVING SENTRIES',W/2,35,12,'#c8c7eb','center');
 if(s.stage>=3)text(ctx,'SECURITY LEVEL '+Math.min(5,Math.floor(s.stage/2)),W/2,514,13,'#f1b5ba','center');
}
const padColors=['#5dd8c8','#eca873','#bfa5f5','#f08ba5','#8fbfff','#c7e78a'] as const;
export function echoPadAt(x:number,y:number):number|null{
 const col=Math.floor((x-220)/175),row=Math.floor((y-130)/170);
 if(col<0||col>=3||row<0||row>=2)return null;
 const localX=x-220-col*175,localY=y-130-row*170;
 return localX>=0&&localX<=155&&localY>=0&&localY<=145?row*3+col:null;
}
function echo(ctx:Ctx,s:EchoRun){
 backdrop(ctx,'#0b1830','#292042');
 ctx.save();ctx.shadowColor='#e2c28c';ctx.shadowBlur=27;
 round(ctx,199,99,565,391,22,'#112934','#bd9e75');ctx.restore();
 for(let pad=0;pad<6;pad++){
  const col=pad%3,row=Math.floor(pad/3),x=220+col*175,y=130+row*170;
  const active=(s.phase==='watch'&&s.active===pad)||
   (s.phase==='input'&&s.feedback>0&&s.lastGood&&s.lastChoice===pad);
  ctx.save();ctx.shadowColor=active?'#fdf4e6':padColors[pad];
  ctx.shadowBlur=active?38:10;
  const c=padColors[pad];round(ctx,x,y,155,145,17,active?'#ecf9e9':c+'55',c);
  round(ctx,x+11,y+12,133,121,10,active?c:'#0c2939','#cfe8df33');
  text(ctx,String(pad+1),x+77,y+88,43,active?'#132e3a':c,'center');
  ctx.restore();
 }
 text(ctx,'ECHO '+s.round,24,37,18,'#f6d6ab');
 text(ctx,'LIVES '+s.lives,W-24,37,14,'#c5efdb','right');
 text(ctx,s.phase==='watch'?'WATCH THE LIGHTS':s.phase==='input'?
  'REPEAT THE PATTERN':'PERFECT SEQUENCE',W/2,65,15,
  s.phase==='input'?'#a4f0dc':'#f3d7a0','center');
 if(s.feedback>0&&!s.lastGood)
  text(ctx,'MISSED!',W/2,95,19,'#ffb2a0','center');
 if(s.phase==='input'){
  const limit=6+s.sequence.length*1.6;
  round(ctx,222,499,520,9,4,'#2c4a59');
  round(ctx,222,499,520*clamp(1-s.inputClock/limit,0,1),9,4,'#8ce5d4');
  text(ctx,(s.index)+' / '+s.sequence.length,751,508,12,'#e5d4b1');
 }
}
export function drawExpansionRun(ctx:Ctx,s:ExpansionRun):void{
 ctx.save();ctx.clearRect(0,0,W,H);
 switch(s.kind){
  case 'midnight-beats':beats(ctx,s);break;
  case 'phantom-hook':hook(ctx,s);break;
  case 'neon-drift':drift(ctx,s);break;
  case 'gravity-chamber':gravity(ctx,s);break;
  case 'echo-sequence':echo(ctx,s);break;
 }
 ctx.strokeStyle='#c6a46a';ctx.lineWidth=3;ctx.strokeRect(1.5,1.5,W-3,H-3);
 ctx.restore();
}
