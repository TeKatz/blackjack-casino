import {ARCADE_HEIGHT as H,ARCADE_WIDTH as W} from './arcadeEngine';
import {SNAKE_COLS,SNAKE_ROWS} from './arcadeEngine';
import type {ArcadeRunState,CaveRun,StackRun,BladeRun,WingsRun,SnakeRun,BreakerRun,StormRun} from './arcadeEngine';

type Context=CanvasRenderingContext2D;
const TAU=Math.PI*2;
function background(ctx:Context,state:ArcadeRunState){
 const tones:Record<ArcadeRunState['kind'],[string,string,string]>={
  'midnight-wings':['#061b25','#16283a','#244541'],
  'cave-escape':['#071b23','#13272b','#0b151d'],
  'perfect-stack':['#12292b','#172739','#171d2b'],
  'blade-timing':['#141c2e','#202537','#0d1f27'],
  'death-snake':['#101e24','#142b29','#1b3024'],
  'neon-breaker':['#071a2c','#15253b','#17312f'],
  'bullet-storm':['#081426','#161b3a','#0c2731'],
 };
 const colors=tones[state.kind],g=ctx.createLinearGradient(0,0,W,H);
 g.addColorStop(0,colors[0]);g.addColorStop(.56,colors[1]);g.addColorStop(1,colors[2]);
 ctx.fillStyle=g;ctx.fillRect(0,0,W,H);
 const t=state.elapsed;
 for(let i=0;i<55;i++){
  const x=(i*227.51+73)%W,y=(i*149.73+31)%H;
  const alpha=.25+.3*(.5+.5*Math.sin(t*.8+i*2.2));
  ctx.globalAlpha=alpha;
  ctx.fillStyle=i%7===0?'#e9ce96':'#a0efdb';
  ctx.beginPath();ctx.arc(x,y,i%11===0?1.8:.85,0,TAU);ctx.fill();
 }
 ctx.globalAlpha=1;
 ctx.strokeStyle='#b6cfa010';ctx.lineWidth=1;
 for(let x=0;x<W;x+=48){ctx.beginPath();ctx.moveTo(x,0);ctx.lineTo(x,H);ctx.stroke();}
 for(let y=0;y<H;y+=48){ctx.beginPath();ctx.moveTo(0,y);ctx.lineTo(W,y);ctx.stroke();}
 const vignette=ctx.createRadialGradient(W/2,H/2,160,W/2,H/2,620);
 vignette.addColorStop(0,'#00000000');vignette.addColorStop(1,'#00000088');
 ctx.fillStyle=vignette;ctx.fillRect(0,0,W,H);
}
function mountain(ctx:Context,t:number,depth:number,base:number,color:string){
 ctx.beginPath();ctx.moveTo(0,H);
 for(let x=0;x<=W+32;x+=32){
  const z=x+t*depth,noise=Math.sin(z*.008+depth)*26+Math.sin(z*.022+2*depth)*14;
  ctx.lineTo(x,base+noise);
 }
 ctx.lineTo(W,H);ctx.closePath();ctx.fillStyle=color;ctx.fill();
}
function wingPipe(ctx:Context,p:WingsRun['pipes'][number]){
 const w=88,edge=p.center-p.gap/2,bottom=p.center+p.gap/2;
 const fill=ctx.createLinearGradient(p.x,0,p.x+w,0);
 fill.addColorStop(0,'#123837');fill.addColorStop(.48,'#36716d');fill.addColorStop(1,'#122b32');
 ctx.fillStyle=fill;ctx.strokeStyle='#c8a76a';ctx.lineWidth=3;
 ctx.fillRect(p.x,0,w,edge-16);ctx.strokeRect(p.x+2,0,w-4,edge-16);
 ctx.fillRect(p.x,bottom+16,w,H-bottom-16);ctx.strokeRect(p.x+2,bottom+16,w-4,H-bottom-16);
 for(const y of [edge-30,bottom+16]){
  ctx.fillStyle='#182b31';ctx.fillRect(p.x-9,y,w+18,30);
  ctx.strokeStyle='#e3c37c';ctx.strokeRect(p.x-9,y,w+18,30);
  ctx.fillStyle='#91ebe0';ctx.fillRect(p.x+36,y+6,16,18);
  ctx.fillStyle='#f0d99d';ctx.fillRect(p.x+42,y+9,4,12);
 }
 for(let x=p.x+15;x<p.x+w;x+=28){
  ctx.strokeStyle='#d7b56855';ctx.beginPath();ctx.moveTo(x,0);ctx.lineTo(x,edge-30);
  ctx.moveTo(x,bottom+46);ctx.lineTo(x,H);ctx.stroke();
 }
}
function wings(ctx:Context,state:WingsRun){
 mountain(ctx,state.elapsed*30,.7,395,'#143c41');
 mountain(ctx,state.elapsed*65,1.1,468,'#0a272f');
 ctx.fillStyle='#e7d29b13';ctx.beginPath();ctx.arc(715,115,86,0,TAU);ctx.fill();
 ctx.strokeStyle='#e7d29b28';ctx.lineWidth=2;ctx.beginPath();ctx.arc(715,115,105,0,TAU);ctx.stroke();
 state.pipes.forEach(p=>wingPipe(ctx,p));
 ctx.save();ctx.translate(210,state.y);ctx.rotate(Math.max(-.5,Math.min(.9,state.vy/700)));
 ctx.shadowColor='#efc583';ctx.shadowBlur=22;
 ctx.fillStyle='#1d414a';ctx.strokeStyle='#f7d99c';ctx.lineWidth=3;
 ctx.beginPath();ctx.ellipse(0,0,26,17,0,0,TAU);ctx.fill();ctx.stroke();
 ctx.shadowBlur=0;
 ctx.fillStyle='#74ddda';ctx.beginPath();ctx.moveTo(-16,-4);ctx.lineTo(-38,-26-Math.sin(state.elapsed*23)*7);
 ctx.lineTo(-29,9);ctx.closePath();ctx.fill();ctx.stroke();
 ctx.fillStyle='#f1ce84';ctx.beginPath();ctx.moveTo(20,1);ctx.lineTo(38,7);ctx.lineTo(20,12);ctx.fill();
 ctx.fillStyle='#f6eace';ctx.beginPath();ctx.arc(9,-6,4,0,TAU);ctx.fill();
 ctx.fillStyle='#10212c';ctx.beginPath();ctx.arc(10,-6,2,0,TAU);ctx.fill();
 ctx.restore();
}
function cave(ctx:Context,state:CaveRun){
 const seg=state.segments;
 const top=new Path2D();top.moveTo(0,0);top.lineTo(W,0);
 for(let i=seg.length-1;i>=0;i--)top.lineTo(seg[i].x,seg[i].center-seg[i].gap/2);
 top.closePath();
 const bottom=new Path2D();bottom.moveTo(0,H);bottom.lineTo(W,H);
 for(let i=seg.length-1;i>=0;i--)bottom.lineTo(seg[i].x,seg[i].center+seg[i].gap/2);
 bottom.closePath();
 const grad=ctx.createLinearGradient(0,0,0,H);
 grad.addColorStop(0,'#14252c');grad.addColorStop(.48,'#2c4b49');grad.addColorStop(1,'#0b1c27');
 ctx.fillStyle=grad;ctx.fill(top);ctx.fill(bottom);
 ctx.strokeStyle='#d1ae6c';ctx.lineWidth=3;ctx.beginPath();
 seg.forEach((s,i)=>{if(i===0)ctx.moveTo(s.x,s.center-s.gap/2);
  else ctx.lineTo(s.x,s.center-s.gap/2);});ctx.stroke();
 ctx.beginPath();seg.forEach((s,i)=>{if(i===0)ctx.moveTo(s.x,s.center+s.gap/2);
  else ctx.lineTo(s.x,s.center+s.gap/2);});ctx.stroke();
 for(let i=1;i<seg.length;i+=3){
  const s=seg[i],upper=s.center-s.gap/2,lower=s.center+s.gap/2;
  ctx.strokeStyle='#82d7c144';ctx.lineWidth=2;
  ctx.beginPath();ctx.moveTo(s.x,upper);ctx.lineTo(s.x+5,upper-22);ctx.moveTo(s.x,lower);
  ctx.lineTo(s.x-6,lower+23);ctx.stroke();
  ctx.fillStyle='#6ddcc27c';ctx.beginPath();ctx.arc(s.x,upper-7,3,0,TAU);ctx.fill();
 }
 ctx.save();ctx.translate(225,state.y);
 ctx.shadowColor='#8af6dc';ctx.shadowBlur=20;
 ctx.fillStyle='#85e7dc';ctx.strokeStyle='#f8dfac';ctx.lineWidth=2;
 ctx.beginPath();ctx.moveTo(25,0);ctx.lineTo(-12,-15);ctx.lineTo(-23,0);ctx.lineTo(-12,15);
 ctx.closePath();ctx.fill();ctx.stroke();ctx.shadowBlur=0;
 ctx.fillStyle='#11333b';ctx.beginPath();ctx.arc(1,0,6,0,TAU);ctx.fill();
 ctx.strokeStyle='#5addbd99';ctx.lineWidth=3;
 for(let j=0;j<4;j++){
  ctx.beginPath();ctx.moveTo(-26-j*12,(j%2?1:-1)*5);ctx.lineTo(-37-j*12,(j%2?1:-1)*10);ctx.stroke();
 }
 ctx.restore();
}
function roundedRect(ctx:Context,x:number,y:number,w:number,h:number,r:number){
 const radius=Math.min(r,h/2,w/2);
 ctx.beginPath();ctx.roundRect(x,y,w,h,radius);
}
function stack(ctx:Context,state:StackRun){
 const blocks=state.blocks;
 const start=Math.max(0,blocks.length-7);
 ctx.fillStyle='#e2be7e30';ctx.fillRect(0,505,W,3);
 const projectedY=(i:number)=>490-(i-start)*48;
 for(let i=start;i<blocks.length;i++){
  const p=blocks[i],y=projectedY(i);
  const light=ctx.createLinearGradient(p.x,y,p.x+p.width,y+40);
  light.addColorStop(0,i%2===0?'#316775':'#9d665b');
  light.addColorStop(.55,i%2===0?'#7fc4be':'#e9b28c');
  light.addColorStop(1,i%2===0?'#1f4a61':'#885249');
  roundedRect(ctx,p.x,y,p.width,38,5);ctx.fillStyle=light;ctx.fill();
  ctx.strokeStyle='#eedaa4';ctx.lineWidth=1.5;ctx.stroke();
  ctx.fillStyle='#fbe8c340';ctx.fillRect(p.x+5,y+4,Math.max(0,p.width-10),4);
 }
 const last=blocks[blocks.length-1];
 const movingY=projectedY(blocks.length);
 const guideY=movingY+41;
 ctx.strokeStyle='#e9cc7d44';ctx.setLineDash([9,9]);ctx.lineWidth=2;
 ctx.beginPath();ctx.moveTo(last.x,guideY);ctx.lineTo(last.x+last.width,guideY);ctx.stroke();
 ctx.setLineDash([]);
 const glow=ctx.createLinearGradient(state.movingX,movingY,state.movingX+last.width,movingY);
 glow.addColorStop(0,'#8c6dca');glow.addColorStop(.45,'#f5d7a8');glow.addColorStop(1,'#8264bb');
 roundedRect(ctx,state.movingX,movingY,last.width,38,5);
 ctx.fillStyle=glow;ctx.fill();ctx.strokeStyle='#fff1bf';ctx.lineWidth=2;ctx.stroke();
 ctx.fillStyle='#ecce8966';ctx.beginPath();ctx.arc(last.x+last.width/2,510,18,0,TAU);ctx.fill();
 ctx.fillStyle='#fff4d7a6';ctx.font='15px Segoe UI, sans-serif';ctx.textAlign='center';
 ctx.fillText('◆',last.x+last.width/2,526);
}
function bladeShape(ctx:Context,angle:number,radius:number){
 ctx.save();ctx.rotate(angle);
 ctx.shadowColor='#eec37c';ctx.shadowBlur=9;
 ctx.fillStyle='#f9dfa3';ctx.strokeStyle='#593d32';ctx.lineWidth=2;
 ctx.beginPath();ctx.moveTo(-5,radius-12);ctx.lineTo(-5,radius+40);
 ctx.lineTo(0,radius+62);ctx.lineTo(5,radius+40);ctx.lineTo(5,radius-12);
 ctx.closePath();ctx.fill();ctx.stroke();
 ctx.shadowBlur=0;
 ctx.fillStyle='#314952';ctx.fillRect(-8,radius+12,16,14);
 ctx.fillStyle='#d2a466';ctx.fillRect(-4,radius+32,8,5);
 ctx.restore();
}
function blades(ctx:Context,state:BladeRun){
 const cx=W/2,cy=236,radius=113;
 ctx.save();ctx.translate(cx,cy);
 ctx.shadowColor='#cfa77b';ctx.shadowBlur=18;
 const rim=ctx.createRadialGradient(0,0,12,0,0,radius+14);
 rim.addColorStop(0,'#34535b');rim.addColorStop(.65,'#102d3a');rim.addColorStop(.9,'#b68b59');rim.addColorStop(1,'#1d3340');
 ctx.fillStyle=rim;ctx.beginPath();ctx.arc(0,0,radius,0,TAU);ctx.fill();ctx.shadowBlur=0;
 ctx.strokeStyle='#e2c189';ctx.lineWidth=7;ctx.beginPath();ctx.arc(0,0,radius,0,TAU);ctx.stroke();
 ctx.rotate(state.rotation);
 ctx.strokeStyle='#f9e6ae52';ctx.lineWidth=4;
 for(let i=0;i<12;i++){
  const a=i*TAU/12;
  ctx.beginPath();ctx.moveTo(Math.cos(a)*32,Math.sin(a)*32);
  ctx.lineTo(Math.cos(a)*96,Math.sin(a)*96);ctx.stroke();
 }
 ctx.strokeStyle='#8cdbc6';ctx.lineWidth=3;ctx.beginPath();ctx.arc(0,0,67,0,TAU);ctx.stroke();
 ctx.fillStyle='#e3c481';ctx.beginPath();ctx.arc(0,0,26,0,TAU);ctx.fill();
 ctx.fillStyle='#1d3339';ctx.beginPath();ctx.arc(0,0,13,0,TAU);ctx.fill();
 ctx.restore();
 ctx.save();ctx.translate(cx,cy);
 for(const angle of state.blades)bladeShape(ctx,angle+state.rotation,radius);
 ctx.restore();
 // Ready knife at the launch position (below the spinning target).
 ctx.save();ctx.translate(cx,431);ctx.rotate(Math.PI);
 ctx.fillStyle='#e1c68a';ctx.strokeStyle='#fff1c2';ctx.lineWidth=2;
 ctx.beginPath();ctx.moveTo(0,39);ctx.lineTo(-9,-7);ctx.lineTo(0,-27);ctx.lineTo(9,-7);ctx.closePath();
 ctx.fill();ctx.stroke();
 ctx.fillStyle='#597e83';ctx.fillRect(-6,32,12,32);
 ctx.restore();
 ctx.strokeStyle='#edd4a655';ctx.lineWidth=2;ctx.setLineDash([5,9]);
 ctx.beginPath();ctx.moveTo(cx,362);ctx.lineTo(cx,408);ctx.stroke();ctx.setLineDash([]);
 ctx.fillStyle='#c4e4d5';ctx.font='700 14px Segoe UI, sans-serif';ctx.textAlign='center';
 ctx.fillText('STAGE '+state.stage,cx,40);
 ctx.fillStyle='#e4c289';ctx.fillText(state.hits+' / '+state.needed,cx,60);
}

function deathSnake(ctx:Context,state:SnakeRun){
 const cell=24,ox=(W-SNAKE_COLS*cell)/2,oy=(H-SNAKE_ROWS*cell)/2;
 ctx.save();
 const panel=ctx.createLinearGradient(ox,oy,ox+SNAKE_COLS*cell,oy+SNAKE_ROWS*cell);
 panel.addColorStop(0,'#091a1c');panel.addColorStop(1,'#14302b');
 ctx.fillStyle=panel;ctx.fillRect(ox-11,oy-11,SNAKE_COLS*cell+22,SNAKE_ROWS*cell+22);
 ctx.strokeStyle='#dfc283';ctx.lineWidth=3;ctx.strokeRect(ox-10,oy-10,SNAKE_COLS*cell+20,SNAKE_ROWS*cell+20);
 ctx.strokeStyle='#66867530';ctx.lineWidth=1;
 for(let i=0;i<=SNAKE_COLS;i++){
  ctx.beginPath();ctx.moveTo(ox+i*cell,oy);ctx.lineTo(ox+i*cell,oy+SNAKE_ROWS*cell);ctx.stroke();
 }
 for(let i=0;i<=SNAKE_ROWS;i++){
  ctx.beginPath();ctx.moveTo(ox,oy+i*cell);ctx.lineTo(ox+SNAKE_COLS*cell,oy+i*cell);ctx.stroke();
 }
 for(const wall of state.walls){
  const x=ox+wall.x*cell,y=oy+wall.y*cell;
  ctx.shadowColor='#eab56e';ctx.shadowBlur=8;ctx.fillStyle='#a16f49';
  roundedRect(ctx,x+3,y+3,cell-6,cell-6,3);ctx.fill();
  ctx.shadowBlur=0;ctx.strokeStyle='#edd69e';ctx.lineWidth=1.5;ctx.stroke();
  ctx.fillStyle='#f8d59f';ctx.fillRect(x+9,y+8,6,6);
 }
 const foodX=ox+(state.food.x+.5)*cell,foodY=oy+(state.food.y+.5)*cell;
 ctx.shadowColor='#ffd18b';ctx.shadowBlur=22;
 ctx.fillStyle='#f8cf86';ctx.beginPath();ctx.arc(foodX,foodY,8,0,TAU);ctx.fill();
 ctx.shadowBlur=0;ctx.strokeStyle='#ffe5b2';ctx.lineWidth=2;
 ctx.beginPath();ctx.arc(foodX,foodY,11+Math.sin(state.elapsed*8)*1.5,0,TAU);ctx.stroke();
 state.body.forEach((segment,index)=>{
  const x=ox+segment.x*cell,y=oy+segment.y*cell;
  const brightness=Math.max(.35,1-index/Math.max(9,state.body.length+4));
  ctx.globalAlpha=brightness;
  ctx.shadowColor=index===0?'#a2faac':'#65db9b';
  ctx.shadowBlur=index===0?19:6;
  ctx.fillStyle=index===0?'#b1f69a':index%2===0?'#5cd5a5':'#2da88a';
  roundedRect(ctx,x+1.5,y+1.5,cell-3,cell-3,index===0?7:5);ctx.fill();
  ctx.shadowBlur=0;ctx.strokeStyle='#e8ffe287';ctx.lineWidth=1;ctx.stroke();
  if(index===0){
   ctx.fillStyle='#17362c';ctx.globalAlpha=1;
   const dx=state.direction==='right'?5:state.direction==='left'?-5:0;
   const dy=state.direction==='down'?5:state.direction==='up'?-5:0;
   for(const side of [-1,1]){
    ctx.beginPath();
    ctx.arc(x+12+dx+(dy?side*5:0),y+12+dy+(dx?side*5:0),2.3,0,TAU);ctx.fill();
   }
  }
 });
 ctx.globalAlpha=1;ctx.restore();
 ctx.fillStyle='#e9d299';ctx.font='700 15px Segoe UI, sans-serif';ctx.textAlign='left';
 ctx.fillText('SPEED '+Math.round(1000/state.speed)/100,ox,oy-24);
 ctx.textAlign='right';ctx.fillText('FOOD '+state.score,ox+SNAKE_COLS*cell,oy-24);
}
function neonBreaker(ctx:Context,state:BreakerRun){
 ctx.save();
 ctx.fillStyle='#d7b67929';ctx.fillRect(22,506,W-44,2);
 ctx.strokeStyle='#66828f50';ctx.lineWidth=2;
 ctx.strokeRect(21,36,W-42,470);
 ctx.fillStyle='#f7dfaf';ctx.textAlign='left';ctx.font='700 15px Segoe UI, sans-serif';
 ctx.fillText('WAVE '+state.wave,43,26);
 ctx.textAlign='right';
 ctx.fillText('◆ '.repeat(state.lives),W-45,26);
 const colors=[
  ['#77e6d3','#245d69'],['#eab88f','#8d597f'],['#b8acfa','#574b92'],
  ['#f1d68d','#977044'],['#86bfff','#355e96'],['#aaf19b','#427854'],['#f2a3c9','#8d4763']
 ];
 for(const brick of state.bricks){
  if(!brick.alive)continue;
  const row=Math.round((brick.y-78)/31),pair=colors[row%colors.length];
  const g=ctx.createLinearGradient(brick.x,brick.y,brick.x+72,brick.y+23);
  g.addColorStop(0,pair[0]);g.addColorStop(1,pair[1]);
  ctx.shadowColor=pair[0];ctx.shadowBlur=10;
  roundedRect(ctx,brick.x,brick.y,72,23,4);ctx.fillStyle=g;ctx.fill();
  ctx.shadowBlur=0;ctx.lineWidth=1;ctx.strokeStyle='#fff2c57d';ctx.stroke();
  ctx.fillStyle='#fff9f077';ctx.fillRect(brick.x+6,brick.y+4,60,3);
 }
 const half=state.paddleWidth/2;
 ctx.shadowColor='#7feadb';ctx.shadowBlur=19;
 const paddle=ctx.createLinearGradient(state.paddleX-half,0,state.paddleX+half,0);
 paddle.addColorStop(0,'#236e76');paddle.addColorStop(.5,'#d2f1de');paddle.addColorStop(1,'#237c8a');
 roundedRect(ctx,state.paddleX-half,483,state.paddleWidth,17,7);
 ctx.fillStyle=paddle;ctx.fill();ctx.strokeStyle='#f8dda2';ctx.lineWidth=2;ctx.stroke();
 ctx.shadowBlur=0;
 ctx.fillStyle='#a6ffea';ctx.shadowColor='#9efce0';ctx.shadowBlur=25;
 ctx.beginPath();ctx.arc(state.ballX,state.ballY,9,0,TAU);ctx.fill();
 ctx.fillStyle='#fff3c7';ctx.beginPath();ctx.arc(state.ballX-2,state.ballY-2,4,0,TAU);ctx.fill();
 ctx.shadowBlur=0;
 if(state.serve){
  ctx.fillStyle='#e3d6b7';
  ctx.textAlign='center';ctx.font='700 17px Segoe UI, sans-serif';
  ctx.fillText('SPACE / CLICK TO LAUNCH',W/2,441);
 }
 ctx.restore();
}
function bulletStorm(ctx:Context,state:StormRun){
 ctx.save();
 const cx=W/2,cy=H/2;
 ctx.strokeStyle='#9dbaeb24';ctx.lineWidth=2;
 for(let r=80;r<=550;r+=77){
  ctx.beginPath();ctx.arc(cx,cy,r,0,TAU);ctx.stroke();
 }
 ctx.strokeStyle='#c7a4f42c';ctx.lineWidth=1;ctx.beginPath();
 ctx.moveTo(cx,0);ctx.lineTo(cx,H);ctx.moveTo(0,cy);ctx.lineTo(W,cy);ctx.stroke();
 for(const bullet of state.bullets){
  const color=['#f3b47c','#bf96ff','#83e8e2'][bullet.style%3];
  ctx.shadowColor=color;ctx.shadowBlur=13;
  ctx.fillStyle=color;ctx.beginPath();ctx.arc(bullet.x,bullet.y,bullet.r,0,TAU);ctx.fill();
  ctx.strokeStyle='#fff5d4';ctx.lineWidth=1;
  ctx.beginPath();ctx.arc(bullet.x,bullet.y,bullet.r+2,0,TAU);ctx.stroke();
 }
 ctx.shadowColor='#8ef3dc';ctx.shadowBlur=24;
 ctx.fillStyle='#d0fbe2';ctx.strokeStyle='#d9ba8d';ctx.lineWidth=2;
 ctx.beginPath();ctx.moveTo(state.x,state.y-19);ctx.lineTo(state.x-15,state.y+14);
 ctx.lineTo(state.x,state.y+7);ctx.lineTo(state.x+15,state.y+14);ctx.closePath();
 ctx.fill();ctx.stroke();ctx.shadowBlur=0;
 ctx.fillStyle='#3a7291';ctx.beginPath();ctx.arc(state.x,state.y,4,0,TAU);ctx.fill();
 ctx.lineWidth=1.5;ctx.strokeStyle='#88edd2';
 ctx.beginPath();ctx.arc(state.x,state.y,19,0,TAU);ctx.stroke();
 ctx.font='700 15px Segoe UI, sans-serif';ctx.fillStyle='#e9d5a7';
 ctx.textAlign='left';ctx.fillText('WAVE '+state.wave,24,32);
 ctx.textAlign='right';ctx.fillText('SURVIVE',W-24,32);
 ctx.restore();
}

export function drawArcadeRun(ctx:Context,state:ArcadeRunState){
 ctx.save();
 ctx.clearRect(0,0,W,H);
 background(ctx,state);
 switch(state.kind){
  case 'midnight-wings':wings(ctx,state);break;
  case 'cave-escape':cave(ctx,state);break;
  case 'perfect-stack':stack(ctx,state);break;
  case 'blade-timing':blades(ctx,state);break;
  case 'death-snake':deathSnake(ctx,state);break;
  case 'neon-breaker':neonBreaker(ctx,state);break;
  case 'bullet-storm':bulletStorm(ctx,state);break;
 }
 ctx.strokeStyle='#c6a46a';ctx.lineWidth=3;ctx.strokeRect(1.5,1.5,W-3,H-3);
 ctx.restore();
}
