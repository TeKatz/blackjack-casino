import {driftCenter,gravityGoalY,gravityHazards,pinBumpers} from './arcadeExpansionOneEngine';
import type {ExpansionRun,PinballRun,HookRun,DriftRun,GravityRun,EchoRun} from './arcadeExpansionOneEngine';
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
function pinball(ctx:Ctx,s:PinballRun){
 backdrop(ctx,'#061822','#1e1933');
 ctx.save();ctx.shadowColor='#dba971';ctx.shadowBlur=24;
 round(ctx,155,22,650,526,38,'#122d35','#d6b47e');ctx.restore();
 round(ctx,175,45,610,470,27,'#082632','#518a88');
 const field=ctx.createLinearGradient(205,55,725,505);
 field.addColorStop(0,'#123b49');field.addColorStop(.55,'#162d42');field.addColorStop(1,'#27223c');
 round(ctx,191,54,578,451,24,field,'#b2a277');
 line(ctx,[[310,80],[210,173],[210,395],[308,450]],'#b5ddca66',4);
 line(ctx,[[650,80],[749,173],[749,395],[652,450]],'#b5ddca66',4);
 // Animated pinball scoring lanes.
 for(let i=0;i<4;i++){
  const x=315+i*109;
  round(ctx,x,69,52,13,6,i%2?'#729f9d':'#dbb17b','#f6d7a0');
  text(ctx,String(i+1),x+26,80,10,'#112a31','center');
 }
 pinBumpers.forEach((b,i)=>{
  ctx.save();ctx.shadowColor=s.flashes[i]>0?'#fff4ac':'#88ead6';
  ctx.shadowBlur=s.flashes[i]>0?38:15;
  orb(ctx,b.x,b.y,b.r+8,'#1c5d67');
  orb(ctx,b.x,b.y,b.r,'#e8c28e');
  orb(ctx,b.x,b.y,b.r-8,s.flashes[i]>0?'#fff7ca':'#4a9c9e');
  ctx.restore();
  text(ctx,'✦',b.x,b.y+8,23,'#fce6bb','center');
 });
 for(const [side,pressed] of [['left',s.leftFlipper],['right',s.rightFlipper]] as const){
  const x=side==='left'?386:574,tip=side==='left'?(pressed?489:474):(pressed?471:486);
  const y=pressed?429:468;
  ctx.save();ctx.shadowBlur=15;ctx.shadowColor='#edc281';
  line(ctx,[[x,454],[tip,y]],pressed?'#fff1c4':'#ebc18a',16);
  ctx.restore();
  orb(ctx,x,454,10,'#b9d6ca');
 }
 round(ctx,682,64,67,426,13,'#10232f','#bf9d71');
 line(ctx,[[716,81],[716,467]],'#5b9f9c99',3);
 // Plunger channel / remaining balls.
 text(ctx,'BALLS',740,35,12,'#dfd0a5','right');
 for(let i=0;i<3;i++)orb(ctx,702+i*22,35,5,i<s.balls?'#f2cf91':'#40525c');
 if(s.serve){
  orb(ctx,718,478,12,'#f4f0da');
  text(ctx,'SPACE TO LAUNCH',480,510,13,'#91e5d9','center');
 }else{
  ctx.save();ctx.shadowColor='#fff6ce';ctx.shadowBlur=16;
  orb(ctx,s.x,s.y,11,'#f8f1d6');ctx.restore();
  orb(ctx,s.x-3,s.y-4,2,'#ffffff');
 }
 text(ctx,'COMBO ×'+(s.combo+1),220,35,14,'#a5f4d9');
}
function hook(ctx:Ctx,s:HookRun){
 backdrop(ctx,'#081c30','#2c2250');
 const cam=s.camera;
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
  if(a)line(ctx,[[s.x-cam,s.y],[a.x-cam,a.y]],'#e8d4af',3);
 }
 const x=s.x-cam,y=s.y;
 ctx.save();ctx.translate(x,y);ctx.rotate(clamp(s.vx/600,-.45,.45));
 ctx.shadowColor='#8ce5e0';ctx.shadowBlur=18;
 round(ctx,-15,-15,30,30,8,'#9fefdf','#f4d49c');
 ctx.restore();
 orb(ctx,x+5,y-5,3,'#173949');
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
  const roadWidth=115+165*t;
  const world=s.distance+(420-y)*1.75;
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
  const rw=115+165*clamp((y-65)/475,0,1);
  for(const side of [-1,1]){
   const x=cx+side*(rw+32);
   orb(ctx,x,y,Math.max(2,8-d/400),'#56aaa0');
  }
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
 text(ctx,'SPACE · FLIP GRAVITY',W/2,35,12,'#c8c7eb','center');
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
  const active=s.phase==='watch'&&s.active===pad;
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
  case 'midnight-pinball':pinball(ctx,s);break;
  case 'phantom-hook':hook(ctx,s);break;
  case 'neon-drift':drift(ctx,s);break;
  case 'gravity-chamber':gravity(ctx,s);break;
  case 'echo-sequence':echo(ctx,s);break;
 }
 ctx.strokeStyle='#c6a46a';ctx.lineWidth=3;ctx.strokeRect(1.5,1.5,W-3,H-3);
 ctx.restore();
}
