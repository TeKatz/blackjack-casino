import {PINBALL_BALL_RADIUS,pinBumpers,pinballFlipper,pinballGeometry}
 from './arcadePinballV2';
import type {PinballRun} from './arcadePinballV2';
type Ctx=CanvasRenderingContext2D;
const TAU=Math.PI*2;
function circle(ctx:Ctx,x:number,y:number,r:number,fill:string,stroke?:string,width=2){
 ctx.beginPath();ctx.arc(x,y,r,0,TAU);
 ctx.fillStyle=fill;ctx.fill();
 if(stroke){ctx.strokeStyle=stroke;ctx.lineWidth=width;ctx.stroke();}
}
function stroke(ctx:Ctx,points:[number,number][],color:string,w:number){
 ctx.beginPath();
 points.forEach(([x,y],i)=>i?ctx.lineTo(x,y):ctx.moveTo(x,y));
 ctx.lineWidth=w;ctx.lineCap='round';ctx.lineJoin='round';ctx.strokeStyle=color;ctx.stroke();
}
function text(ctx:Ctx,str:string,x:number,y:number,size=15,color='#eddbba',align:CanvasTextAlign='left'){
 ctx.font='700 '+size+'px Segoe UI, sans-serif';ctx.fillStyle=color;ctx.textAlign=align;ctx.fillText(str,x,y);
}
function rr(ctx:Ctx,x:number,y:number,w:number,h:number,r:number,fill:string,edge?:string){
 ctx.beginPath();ctx.roundRect(x,y,w,h,r);ctx.fillStyle=fill;ctx.fill();
 if(edge){ctx.strokeStyle=edge;ctx.lineWidth=2;ctx.stroke();}
}
function star(ctx:Ctx,x:number,y:number,r:number,strokeColor:string){
 ctx.beginPath();
 for(let i=0;i<8;i++){
  const angle=i*Math.PI/4-Math.PI/2,dist=i%2?r*.32:r;
  const xx=x+Math.cos(angle)*dist,yy=y+Math.sin(angle)*dist;
  if(i)ctx.lineTo(xx,yy);else ctx.moveTo(xx,yy);
 }
 ctx.closePath();ctx.fillStyle=strokeColor;ctx.fill();
}
export function drawPinballV2(ctx:Ctx,s:PinballRun):void{
 const G=pinballGeometry;
 const backdrop=ctx.createLinearGradient(0,0,960,540);
 backdrop.addColorStop(0,'#07131f');backdrop.addColorStop(.5,'#101e2e');
 backdrop.addColorStop(1,'#1c1731');
 ctx.fillStyle=backdrop;ctx.fillRect(0,0,960,540);
 // A single coherent table silhouette, not a collection of floating shapes.
 ctx.save();ctx.shadowColor='#bc9565';ctx.shadowBlur=27;
 rr(ctx,159,12,642,520,38,'#20333b','#e3bc7f');ctx.restore();
 rr(ctx,175,26,610,494,28,'#081b2b','#627983');
 const board=ctx.createLinearGradient(190,40,738,522);
 board.addColorStop(0,'#15354a');board.addColorStop(.5,'#112939');
 board.addColorStop(1,'#24273c');
 rr(ctx,191,43,578,465,24,board,'#b6a16f');
 // Rails, lanes, guides, switchlights and the visible plunger channel.
 ctx.save();ctx.shadowColor='#8adbd3';ctx.shadowBlur=12;
 stroke(ctx,[[214,410],[214,154],[238,105],[320,69],[650,69],[730,113],[746,410]],
  '#7eb9b1',5);
 stroke(ctx,[[214,390],[330,450]],'#d6ba8d',7);
 stroke(ctx,[[746,390],[630,450]],'#d6ba8d',7);
 ctx.restore();
 rr(ctx,703,76,48,426,14,'#142532','#d2b681');
 stroke(ctx,[[724,115],[724,462]],'#385d67',2);
 rr(ctx,696,80,55,22,6,'#263f4a','#f3d6a4');
 star(ctx,724,91,8,'#e8cd8f');
 for(let i=0;i<5;i++){
  const x=269+i*82;
  rr(ctx,x-24,69,48,13,5,'#29515b','#baaa80');
  circle(ctx,x,75,3,'#d5ffde');
 }
 // Decorative scoring emblem behind the bumpers.
 ctx.save();ctx.globalAlpha=.12;
 circle(ctx,480,237,160,'#8bd1c1');
 circle(ctx,480,237,145,'#152f47');
 star(ctx,480,237,115,'#9bd4c8');
 ctx.restore();
 pinBumpers.forEach((b,i)=>{
  const active=s.flashes[i]>0;
  ctx.save();ctx.shadowColor=active?'#fff0b3':'#89e8d5';
  ctx.shadowBlur=active?35:17;
  circle(ctx,b.x,b.y,b.r+9,'#123b4b','#a3bfa7',3);
  circle(ctx,b.x,b.y,b.r+3,'#c4ad82','#f7dfa4',2);
  circle(ctx,b.x,b.y,b.r-6,active?'#e8e9bf':'#4a9896','#daeed1',2);
  circle(ctx,b.x,b.y,b.r-14,'#1d5564');
  star(ctx,b.x,b.y,Math.max(8,b.r-14),'#f3dfa8');
  ctx.restore();
 });
 // Two genuine flippers in exact shared simulation geometry.
 for(const side of ['left','right'] as const){
  const lift=side==='left'?s.leftLift:s.rightLift;
  const pressed=side==='left'?s.leftFlipper:s.rightFlipper;
  const f=pinballFlipper(side,lift);
  ctx.save();ctx.shadowBlur=pressed?20:10;ctx.shadowColor='#ffe4ae';
  stroke(ctx,[[f.pivotX,f.pivotY],[f.tipX,f.tipY]],'#735f61',23);
  stroke(ctx,[[f.pivotX,f.pivotY],[f.tipX,f.tipY]],
   pressed?'#f8d99f':'#cba98c',17);
  stroke(ctx,[[f.pivotX,f.pivotY],[f.tipX,f.tipY]],'#ffefcb',4);
  circle(ctx,f.pivotX,f.pivotY,12,'#314857','#fae2a9',3);
  circle(ctx,f.pivotX,f.pivotY,4,'#d7dbc3');
  ctx.restore();
 }
 // Outer drains and central open pocket stay visible to the player.
 stroke(ctx,[[259,457],[323,486],[340,508]],'#c9aa7c',5);
 stroke(ctx,[[701,457],[637,486],[620,508]],'#c9aa7c',5);
 rr(ctx,459,496,42,26,8,'#061520','#c19578');
 text(ctx,'DRAIN',480,513,10,'#e8b59b','center');
 // Light trail and specular steel ball.
 if(!s.serve){
  for(let i=s.trail.length-1;i>=0;i--){
   const point=s.trail[i];
   ctx.globalAlpha=.25*(1-i/(s.trail.length+1));
   circle(ctx,point.x,point.y,Math.max(1,7-i*.4),'#a1e6d7');
  }
  ctx.globalAlpha=1;
 }
 ctx.save();ctx.shadowColor='#fff2da';ctx.shadowBlur=19;
 circle(ctx,s.x,s.y,PINBALL_BALL_RADIUS+1,'#fff1d3','#b5cad4',2);
 circle(ctx,s.x-3,s.y-3,3,'#ffffff');ctx.restore();
 if(s.hitTimer>0&&s.hitValue>0){
  ctx.globalAlpha=Math.min(1,s.hitTimer*2.6);
  text(ctx,'+'+s.hitValue,s.hitX,s.hitY-47,18,'#ffebb1','center');
  ctx.globalAlpha=1;
 }
 text(ctx,'MIDNIGHT PINBALL',24,37,18,'#f7d9a0');
 text(ctx,'COMBO ×'+Math.max(1,s.combo+1),25,63,14,'#9af0dc');
 text(ctx,'BALLS',826,37,12,'#e5caa1');
 for(let i=0;i<3;i++){
  circle(ctx,848+i*23,32,7,i<s.balls?'#eac995':'#354252','#dfd4b2');
 }
 text(ctx,'BUMPER • RAMP • MULTIPLIER',480,35,12,'#a3d8cf','center');
 if(s.serve){
  ctx.save();ctx.globalAlpha=.92;
  rr(ctx,333,478,294,39,11,'#102936','#d7b780');
  text(ctx,'SPACE  ·  LAUNCH BALL',480,504,16,'#f9e6c0','center');ctx.restore();
 }else if(s.inShooter){
  text(ctx,'LAUNCH LANE',724,124,12,'#e5dcaa','center');
 }
}
