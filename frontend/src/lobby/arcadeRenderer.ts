import {isExpansionRun} from './arcadeExpansionOneEngine';
import {isExpansionTwoRun} from './arcadeExpansionTwoEngine';
import {drawExpansionTwoRun} from './arcadeExpansionTwoRenderer';
import {drawExpansionRun} from './arcadeExpansionOneRenderer';
import {ARCADE_HEIGHT as H,ARCADE_WIDTH as W} from './arcadeEngine';
import {SNAKE_COLS,SNAKE_ROWS,BLADE_HIT_ARC,angularDistance} from './arcadeEngine';
import type {ArcadeRunState,CaveRun,StackRun,BladeRun,WingsRun,SnakeRun,BreakerRun,StormRun,HexRun,SpikeRun,AscentRun} from './arcadeEngine';

type Context=CanvasRenderingContext2D;
const TAU=Math.PI*2;
function background(ctx:Context,state:ArcadeRunState){
 const tones:Record<ArcadeRunState['kind'],[string,string,string]>={
  'midnight-pinball':['#061822','#1e1933','#132a33'],
  'phantom-hook':['#081c30','#2c2250','#152d3c'],
  'neon-drift':['#11172d','#172939','#122e38'],
  'gravity-chamber':['#0b1b32','#20274a','#152f3e'],
  'echo-sequence':['#0b1830','#292042','#162c3d'],
  'prism-lock':['#061b2a','#242444','#142b3a'],
  'meteor-miner':['#061525','#192949','#203348'],
  'circuit-rush':['#091a2b','#19303b','#12394a'],
  'shadow-heist':['#081827','#222b39','#193444'],
  'pulse-perfect':['#0c1730','#302044','#172b39'],
  'midnight-wings':['#061b25','#16283a','#244541'],
  'cave-escape':['#071b23','#13272b','#0b151d'],
  'perfect-stack':['#12292b','#172739','#171d2b'],
  'blade-timing':['#141c2e','#202537','#0d1f27'],
  'death-snake':['#101e24','#142b29','#1b3024'],
  'neon-breaker':['#071a2c','#15253b','#17312f'],
  'bullet-storm':['#081426','#161b3a','#0c2731'],
  'hexagon-hell':['#0e142c','#252041','#111d30'],
  'spike-rush':['#192b34','#182c40','#241e32'],
  'endless-ascent':['#102937','#163e50','#13243d'],
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
 // A restrained animated ion trail: particles stream behind the craft with speed and flicker.
 for(let i=0;i<16;i++){
  const progress=((i*17+state.elapsed*125)%143)/143;
  const x=-25-progress*134,y=Math.sin(i*2.37+state.elapsed*7)*(2+progress*11);
  ctx.globalAlpha=(1-progress)*(.28+.26*Math.sin(state.elapsed*11+i)**2);
  ctx.fillStyle=i%4===0?'#ffe1a2':'#80f3da';
  ctx.shadowColor='#8ef5d8';ctx.shadowBlur=7;
  ctx.beginPath();ctx.arc(x,y,Math.max(.7,3-progress*2),0,TAU);ctx.fill();
 }
 ctx.shadowBlur=0;ctx.globalAlpha=1;
 ctx.shadowColor='#8af6dc';ctx.shadowBlur=20;
 ctx.fillStyle='#85e7dc';ctx.strokeStyle='#f8dfac';ctx.lineWidth=2;
 ctx.beginPath();ctx.moveTo(25,0);ctx.lineTo(-12,-15);ctx.lineTo(-23,0);ctx.lineTo(-12,15);
 ctx.closePath();ctx.fill();ctx.stroke();ctx.shadowBlur=0;
 ctx.fillStyle='#11333b';ctx.beginPath();ctx.arc(1,0,6,0,TAU);ctx.fill();
 ctx.strokeStyle='#5addbd99';ctx.lineWidth=2;
 for(let j=0;j<3;j++){
  const flicker=Math.sin(state.elapsed*19+j*2)*4;
  ctx.globalAlpha=.67-j*.13;
  ctx.beginPath();ctx.moveTo(-24-j*13,(j%2?1:-1)*3);
  ctx.lineTo(-43-j*15-flicker,(j%2?1:-1)*(6+j*2));ctx.stroke();
 }
 ctx.globalAlpha=1;
 ctx.restore();
}
function roundedRect(ctx:Context,x:number,y:number,w:number,h:number,r:number){
 const radius=Math.min(r,h/2,w/2);
 ctx.beginPath();ctx.roundRect(x,y,w,h,radius);
}
function stack(ctx:Context,state:StackRun){
 const blocks=state.blocks;
 const start=Math.max(0,blocks.length-7);
 const projectedY=(i:number)=>490-(i-start)*48;
 const towerCenter=blocks[blocks.length-1].x+blocks[blocks.length-1].width/2;
 const time=state.elapsed;
 // Architectural columns, soft perspective shafts and skyline.
 for(let i=0;i<9;i++){
  const x=100+i*100;
  const shine=.08+.05*Math.sin(time*.75+i*.8);
  ctx.fillStyle='rgba(139,196,193,'+shine+')';
  ctx.fillRect(x,44,1,455);
 }
 const halo=ctx.createRadialGradient(towerCenter,318,25,towerCenter,318,290);
 halo.addColorStop(0,'#e4b56a32');halo.addColorStop(.7,'#7cd5c30b');halo.addColorStop(1,'#00000000');
 ctx.fillStyle=halo;ctx.fillRect(0,0,W,H);
 for(let i=0;i<24;i++){
  const drift=((time*(8+i%6)+i*93)%590);
  const x=(i*193+17)%W, y=H-drift;
  ctx.globalAlpha=.13+.19*Math.sin(time+i)*Math.sin(time+i);
  ctx.fillStyle=i%3===0?'#fae3ad':'#a0f5e2';
  ctx.beginPath();ctx.arc(x,y,1.3+i%3*.45,0,TAU);ctx.fill();
 }
 ctx.globalAlpha=1;
 // Reinforced golden floor and the plinth supporting the tower.
 const floor=ctx.createLinearGradient(0,491,0,H);
 floor.addColorStop(0,'#ecd09a');floor.addColorStop(.17,'#446365');floor.addColorStop(1,'#091c27');
 ctx.fillStyle=floor;ctx.fillRect(0,505,W,35);
 ctx.fillStyle='#102832';ctx.fillRect(0,501,W,4);
 ctx.fillStyle='#efcc8766';ctx.fillRect(0,504,W,2);
 ctx.shadowBlur=23;ctx.shadowColor='#e4c28c';
 ctx.fillStyle='#9c9679';ctx.beginPath();ctx.roundRect(towerCenter-162,487,324,20,6);ctx.fill();
 ctx.shadowBlur=0;
 ctx.fillStyle='#eedca2';ctx.fillRect(towerCenter-154,487,308,4);
 ctx.fillStyle='#9bdbc788';ctx.fillRect(towerCenter-125,502,250,3);
 // Each block has a luminous top, a bevel and an embossed side panel.
 for(let i=start;i<blocks.length;i++){
  const p=blocks[i],y=projectedY(i),w=p.width;
  const cyan=i%2===0;
  const light=ctx.createLinearGradient(p.x,y,p.x+w,y+38);
  light.addColorStop(0,cyan?'#276878':'#795270');
  light.addColorStop(.42,cyan?'#83ccc3':'#e7b48b');
  light.addColorStop(1,cyan?'#183b56':'#764d68');
  ctx.shadowColor=cyan?'#73dec4':'#f3ad9e';ctx.shadowBlur=9;
  roundedRect(ctx,p.x,y,w,38,5);ctx.fillStyle=light;ctx.fill();
  ctx.shadowBlur=0;ctx.strokeStyle='#f2d8aa';ctx.lineWidth=1.5;ctx.stroke();
  ctx.fillStyle='#fff0be79';ctx.fillRect(p.x+4,y+4,Math.max(0,w-8),4);
  ctx.fillStyle='#081b2866';ctx.fillRect(p.x+4,y+29,Math.max(0,w-8),5);
  if(w>42){
   ctx.strokeStyle='#ffffff3c';ctx.lineWidth=1;
   for(let j=1;j<=3;j++){
    const gx=p.x+w*j/4;
    ctx.beginPath();ctx.moveTo(gx,y+12);ctx.lineTo(gx,y+26);ctx.stroke();
   }
  }
  ctx.fillStyle='#fbe7bc';ctx.font='700 10px Segoe UI, sans-serif';ctx.textAlign='center';
  if(w>75)ctx.fillText(String(i+1).padStart(2,'0'),p.x+w/2,y+24);
 }
 const last=blocks[blocks.length-1],movingY=projectedY(blocks.length),movingW=last.width;
 // Landing guide and a translucent afterimage make precision easy to read.
 ctx.setLineDash([6,7]);ctx.strokeStyle='#a6f5d16b';ctx.lineWidth=2;
 ctx.strokeRect(last.x,movingY,Math.max(1,last.width),38);ctx.setLineDash([]);
 ctx.globalAlpha=.14;ctx.fillStyle='#e8c992';
 roundedRect(ctx,state.movingX-state.direction*17,movingY,movingW,38,5);ctx.fill();ctx.globalAlpha=1;
 const glow=ctx.createLinearGradient(state.movingX,movingY,state.movingX+movingW,movingY+38);
 glow.addColorStop(0,'#8064b8');glow.addColorStop(.5,'#fff0c5');glow.addColorStop(1,'#7668ba');
 ctx.shadowColor='#eac888';ctx.shadowBlur=18;
 roundedRect(ctx,state.movingX,movingY,movingW,38,5);
 ctx.fillStyle=glow;ctx.fill();ctx.shadowBlur=0;ctx.strokeStyle='#fff1bf';ctx.lineWidth=2;ctx.stroke();
 ctx.fillStyle='#ffffff6a';ctx.fillRect(state.movingX+5,movingY+4,Math.max(0,movingW-10),4);
 // A short sparkling impact burst follows each successful drop.
 const age=time-state.lastDropAt;
 if(age>=0&&age<.78){
  const progress=age/.78,px=state.lastDropX,py=projectedY(blocks.length-1);
  const particles=state.perfect>0?20:10;
  for(let j=0;j<particles;j++){
   const a=j*Math.PI*2/particles;
   const speed=29+(j%4)*15,rad=6+progress*speed;
   const x=px+Math.cos(a)*rad,y=py+Math.sin(a)*rad*.63;
   ctx.globalAlpha=(1-progress)*.95;
   ctx.fillStyle=j%3===0?'#fff2ba':j%3===1?'#89e3d0':'#dfb6ff';
   ctx.beginPath();ctx.arc(x,y,Math.max(.6,2.8-progress*1.9),0,TAU);ctx.fill();
  }
  ctx.globalAlpha=1;
 }
 ctx.fillStyle='#f8dfac';ctx.font='700 16px Segoe UI, sans-serif';ctx.textAlign='left';
 ctx.fillText('FLOOR '+(blocks.length-1),28,34);
 if(state.perfect>0){
  ctx.textAlign='right';ctx.fillStyle='#95f2cc';ctx.font='700 17px Segoe UI, sans-serif';
  ctx.fillText('PERFECT  ×'+state.perfect,W-28,34);
 }
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
 // The impact reticle is the actual knife collision angle (Math.PI/2),
 // not a decorative guess. It turns red precisely when the throw would collide.
 const blocked=state.blades.some(angle=>angularDistance(angle+state.rotation,Math.PI/2)<BLADE_HIT_ARC);
 const markerColor=blocked?'#ff927e':'#a4f1d2';
 const impactY=cy+radius;
 ctx.save();
 ctx.strokeStyle=markerColor;ctx.fillStyle=markerColor;ctx.lineWidth=2;
 ctx.shadowColor=markerColor;ctx.shadowBlur=14;
 ctx.beginPath();ctx.arc(cx,impactY,19+Math.sin(state.elapsed*7)*2,0,TAU);ctx.stroke();
 ctx.beginPath();ctx.moveTo(cx-29,impactY);ctx.lineTo(cx-10,impactY);
 ctx.moveTo(cx+10,impactY);ctx.lineTo(cx+29,impactY);ctx.stroke();
 ctx.beginPath();ctx.moveTo(cx,impactY-28);ctx.lineTo(cx,impactY-11);ctx.stroke();
 ctx.shadowBlur=0;ctx.setLineDash([5,6]);
 ctx.beginPath();ctx.moveTo(cx,impactY+30);ctx.lineTo(cx,408);ctx.stroke();ctx.setLineDash([]);
 ctx.font='700 12px Segoe UI, sans-serif';ctx.textAlign='left';
 ctx.fillText(blocked?'BLOCKED':'LANDING ZONE',cx+37,impactY+5);
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
 const t=state.elapsed;
 for(const bullet of state.bullets){
  const style=bullet.style%3;
  const color=['#ffbc7d','#c59bff','#7df0e1'][style];
  ctx.save();ctx.translate(bullet.x,bullet.y);
  // Subtle velocity-aligned energy trail – gameplay hitboxes remain unchanged.
  const speed=Math.max(1,Math.hypot(bullet.vx,bullet.vy));
  const tailX=-bullet.vx/speed*35,tailY=-bullet.vy/speed*35;
  const trail=ctx.createLinearGradient(tailX,tailY,0,0);
  trail.addColorStop(0,'#ffffff00');trail.addColorStop(1,color);
  ctx.strokeStyle=trail;ctx.globalAlpha=.48;ctx.lineWidth=style===2?7:5;
  ctx.beginPath();ctx.moveTo(tailX,tailY);ctx.lineTo(0,0);ctx.stroke();ctx.globalAlpha=1;
  ctx.shadowColor=color;ctx.shadowBlur=17;
  if(style===0){
   const plasma=ctx.createRadialGradient(-3,-4,1,0,0,bullet.r+2);
   plasma.addColorStop(0,'#fff7dc');plasma.addColorStop(.45,'#ffd09b');
   plasma.addColorStop(1,'#d66f64');
   ctx.fillStyle=plasma;ctx.beginPath();ctx.arc(0,0,bullet.r,0,TAU);ctx.fill();
   ctx.strokeStyle='#ffe9bd';ctx.lineWidth=1.5;
   ctx.beginPath();ctx.arc(0,0,bullet.r+2,0,TAU);ctx.stroke();
  }else if(style===1){
   ctx.rotate(t*2.4+bullet.x*.014);
   const diamond=bullet.r+2;
   ctx.fillStyle='#a78af0';ctx.strokeStyle='#fff1d2';ctx.lineWidth=1.7;
   ctx.beginPath();ctx.moveTo(0,-diamond);ctx.lineTo(diamond,0);
   ctx.lineTo(0,diamond);ctx.lineTo(-diamond,0);ctx.closePath();ctx.fill();ctx.stroke();
   ctx.fillStyle='#ffedda';ctx.beginPath();ctx.arc(0,0,3.5,0,TAU);ctx.fill();
  }else{
   ctx.rotate(-t*3.7+bullet.y*.01);
   ctx.fillStyle='#4ebbaa';ctx.strokeStyle='#d4fff2';ctx.lineWidth=1.7;
   ctx.beginPath();
   for(let i=0;i<8;i++){
    const a=i*Math.PI/4,rad=i%2===0?bullet.r+2:bullet.r*.52;
    const x=Math.cos(a)*rad,y=Math.sin(a)*rad;
    if(i===0)ctx.moveTo(x,y);else ctx.lineTo(x,y);
   }
   ctx.closePath();ctx.fill();ctx.stroke();
   ctx.fillStyle='#fff7cd';ctx.beginPath();ctx.arc(0,0,3.5,0,TAU);ctx.fill();
  }
  ctx.shadowBlur=0;ctx.restore();
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

const easeAngle=(a:number)=>a;
function hexagonHell(ctx:Context,state:HexRun){
 const cx=W/2,cy=H/2;
 ctx.save();ctx.translate(cx,cy);
 const pulse=state.elapsed;
 ctx.strokeStyle='#947dde33';ctx.lineWidth=1;
 for(let r=50;r<=340;r+=54){
  ctx.beginPath();
  for(let i=0;i<7;i++){
   const a=i*Math.PI/3+state.rotation*.2;
   const x=Math.cos(a)*r,y=Math.sin(a)*r;
   if(i===0)ctx.moveTo(x,y);else ctx.lineTo(x,y);
  }
  ctx.stroke();
 }
 for(let i=0;i<12;i++){
  const a=i*Math.PI/6+state.rotation*.1;
  ctx.beginPath();ctx.moveTo(Math.cos(a)*44,Math.sin(a)*44);
  ctx.lineTo(Math.cos(a)*340,Math.sin(a)*340);ctx.stroke();
 }
 for(const wall of state.walls){
  if(wall.radius<=0)continue;
  const opening=wall.opening+state.rotation;
  const start=opening+wall.gap/2;
  const end=start+TAU-wall.gap;
  ctx.shadowColor='#b887ff';ctx.shadowBlur=20;
  ctx.strokeStyle='#bea3ff';ctx.lineWidth=wall.thickness+5;
  ctx.beginPath();ctx.arc(0,0,wall.radius,start,end);ctx.stroke();
  ctx.shadowBlur=0;
  ctx.strokeStyle='#49365f';ctx.lineWidth=wall.thickness-2;
  ctx.beginPath();ctx.arc(0,0,wall.radius,start,end);ctx.stroke();
  ctx.strokeStyle='#d5beff';ctx.lineWidth=3;
  for(const radius of [wall.radius-wall.thickness/2+2,wall.radius+wall.thickness/2-2]){
   ctx.beginPath();ctx.arc(0,0,Math.max(0,radius),start,end);ctx.stroke();
  }
  for(const angle of [start,end]){
   const vx=Math.cos(angle),vy=Math.sin(angle);
   ctx.strokeStyle='#fae2ad';ctx.lineWidth=3;
   ctx.beginPath();ctx.moveTo(vx*(wall.radius-wall.thickness/2),vy*(wall.radius-wall.thickness/2));
   ctx.lineTo(vx*(wall.radius+wall.thickness/2),vy*(wall.radius+wall.thickness/2));ctx.stroke();
  }
 }
 // Center reactor.
 ctx.save();ctx.rotate(state.rotation*1.8);
 ctx.shadowColor='#bca0ff';ctx.shadowBlur=27;
 const grad=ctx.createRadialGradient(0,0,5,0,0,46);
 grad.addColorStop(0,'#d4edeb');grad.addColorStop(.28,'#726baa');grad.addColorStop(1,'#2a2b50');
 ctx.fillStyle=grad;ctx.beginPath();
 for(let i=0;i<6;i++){
  const a=i*TAU/6;
  const x=Math.cos(a)*44,y=Math.sin(a)*44;
  if(i===0)ctx.moveTo(x,y);else ctx.lineTo(x,y);
 }
 ctx.closePath();ctx.fill();ctx.lineWidth=4;ctx.strokeStyle='#efd5a2';ctx.stroke();ctx.restore();
 ctx.strokeStyle='#ffe3ae70';ctx.lineWidth=2;
 ctx.beginPath();ctx.arc(0,0,126,0,TAU);ctx.stroke();
 // Player marker on a fixed orbital radius, oriented away from the center.
 ctx.save();ctx.rotate(easeAngle(state.angle));
 ctx.translate(126,0);ctx.rotate(Math.PI/2);
 ctx.shadowColor='#8fe9e8';ctx.shadowBlur=22;
 ctx.fillStyle='#aafbf0';ctx.strokeStyle='#fff0c0';ctx.lineWidth=2;
 ctx.beginPath();ctx.moveTo(0,-17);ctx.lineTo(-13,11);ctx.lineTo(0,6);ctx.lineTo(13,11);ctx.closePath();
 ctx.fill();ctx.stroke();
 ctx.shadowBlur=0;ctx.restore();
 ctx.restore();
 ctx.fillStyle='#eacaa7';ctx.font='700 13px Segoe UI, sans-serif';ctx.textAlign='left';
 ctx.fillText('SURVIVED '+state.score+' WALLS',22,32);
 ctx.fillStyle='#b8d7df';ctx.textAlign='right';ctx.fillText('← A / D →',W-25,32);
 ctx.fillStyle='#9f94cf50';ctx.fillRect(30,H-30,Math.max(1,120+10*Math.sin(pulse)),2);
}
function spikeRush(ctx:Context,state:SpikeRun){
 const ground=440,offset=state.distance%75;
 ctx.save();
 // Moving distant skyline and clouds.
 for(let layer=0;layer<3;layer++){
  ctx.fillStyle=['#2a364b','#25354d','#182f3c'][layer];
  const slide=(state.distance*(.12+layer*.14))%155;
  for(let i=-1;i<9;i++){
   const x=i*155-slide;
   const height=75+25*Math.sin(i*7+layer*1.9);
   ctx.fillRect(x,ground-height-35-layer*14,106,height);
   ctx.fillStyle='#e6cda024';
   for(let j=0;j<4;j++)ctx.fillRect(x+14+j*19,ground-height-25-layer*14,6,5);
   ctx.fillStyle=['#2a364b','#25354d','#182f3c'][layer];
  }
 }
 ctx.fillStyle='#0a242c';ctx.fillRect(0,ground,W,H-ground);
 ctx.fillStyle='#2d5056';ctx.fillRect(0,ground,W,12);
 ctx.fillStyle='#cfad76';ctx.fillRect(0,ground,W,4);
 ctx.strokeStyle='#92c2b02a';ctx.lineWidth=2;
 for(let x=-offset;x<W;x+=75){
  ctx.beginPath();ctx.moveTo(x,ground+15);ctx.lineTo(x-22,H);ctx.stroke();
 }
 for(const o of state.obstacles){
  if(o.type==='gap'){
   ctx.fillStyle='#070d1a';ctx.fillRect(o.x,ground-1,o.width,H-ground+2);
   ctx.fillStyle='#f4c788';ctx.fillRect(o.x-5,ground,5,5);ctx.fillRect(o.x+o.width,ground,5,5);
   ctx.strokeStyle='#c89df1aa';ctx.lineWidth=2;
   for(let i=0;i<4;i++){
    const x=o.x+10+i*22;
    ctx.beginPath();ctx.moveTo(x,ground+21);ctx.lineTo(x-10,H);ctx.stroke();
   }
   continue;
  }
  if(o.type==='saw'){
   const x=o.x+o.width/2,y=ground-26+Math.sin(state.elapsed*5.5+(o.phase??0))*7;
   ctx.save();ctx.translate(x,y);ctx.rotate(state.elapsed*6.8);
   ctx.shadowColor='#ffac9d';ctx.shadowBlur=16;
   ctx.fillStyle='#cf7a72';ctx.strokeStyle='#ffe6be';ctx.lineWidth=1.8;
   ctx.beginPath();
   for(let k=0;k<20;k++){
    const a=k*Math.PI/10,r=k%2===0?21:13;
    if(k===0)ctx.moveTo(Math.cos(a)*r,Math.sin(a)*r);
    else ctx.lineTo(Math.cos(a)*r,Math.sin(a)*r);
   }
   ctx.closePath();ctx.fill();ctx.stroke();
   ctx.shadowBlur=0;ctx.fillStyle='#1a3343';
   ctx.beginPath();ctx.arc(0,0,7,0,TAU);ctx.fill();
   ctx.strokeStyle='#fff0c5';ctx.beginPath();ctx.arc(0,0,4,0,TAU);ctx.stroke();
   ctx.restore();
   ctx.strokeStyle='#d89baf66';ctx.lineWidth=1.5;
   ctx.beginPath();ctx.moveTo(x,ground);ctx.lineTo(x,y+17);ctx.stroke();
   continue;
  }
  if(o.type==='tower'){
   const x=o.x,y=ground-54;
   const metal=ctx.createLinearGradient(x,y,x+o.width,y);
   metal.addColorStop(0,'#584772');metal.addColorStop(.5,'#c17ca8');metal.addColorStop(1,'#443b67');
   ctx.fillStyle=metal;ctx.shadowColor='#e19ab8';ctx.shadowBlur=13;
   ctx.fillRect(x+3,y+8,o.width-6,46);ctx.shadowBlur=0;
   ctx.strokeStyle='#ffe1ba';ctx.lineWidth=2;ctx.strokeRect(x+3,y+8,o.width-6,46);
   ctx.fillStyle='#ffdac1';ctx.beginPath();
   ctx.moveTo(x,y+10);ctx.lineTo(x+o.width/2,y-7);ctx.lineTo(x+o.width,y+10);
   ctx.closePath();ctx.fill();ctx.stroke();
   ctx.fillStyle='#fff3cc';ctx.fillRect(x+o.width/2-3,y+23,6,12);
   continue;
  }
  const count=o.type==='double'?2:1;
  for(let i=0;i<count;i++){
   const x=o.x+i*38+2,y=ground;
   const g=ctx.createLinearGradient(x,y-37,x+38,y);
   g.addColorStop(0,'#f4dba7');g.addColorStop(.55,'#ce8fa9');g.addColorStop(1,'#7b639c');
   ctx.shadowColor='#eeb1ad';ctx.shadowBlur=11;
   ctx.fillStyle=g;ctx.strokeStyle='#fff1d0';ctx.lineWidth=2;
   ctx.beginPath();ctx.moveTo(x,y);ctx.lineTo(x+18,y-38);ctx.lineTo(x+37,y);ctx.closePath();
   ctx.fill();ctx.stroke();ctx.shadowBlur=0;
  }
 }
 ctx.save();
 ctx.translate(210,state.feet-17);
 ctx.rotate(state.grounded?0:state.elapsed*4.9);
 ctx.shadowColor='#7be9d2';ctx.shadowBlur=18;
 const cube=ctx.createLinearGradient(-17,-17,17,17);
 cube.addColorStop(0,'#f8e5bc');cube.addColorStop(.4,'#9ee8d7');cube.addColorStop(1,'#425d8b');
 ctx.fillStyle=cube;ctx.strokeStyle='#f8dda1';ctx.lineWidth=2.5;
 ctx.beginPath();ctx.roundRect(-17,-17,34,34,5);ctx.fill();ctx.stroke();
 ctx.shadowBlur=0;ctx.fillStyle='#1c3347';
 ctx.fillRect(-6,-4,4,5);ctx.fillRect(4,-4,4,5);
 ctx.restore();
 ctx.fillStyle='#e8d3a4';ctx.font='700 14px Segoe UI, sans-serif';ctx.textAlign='left';
 ctx.fillText('GATES '+state.score,24,30);
 ctx.textAlign='right';ctx.fillText(state.grounded?'READY':'AIRBORNE',W-24,30);
 ctx.restore();
}
function endlessAscent(ctx:Context,state:AscentRun){
 ctx.save();
 const h=state.climbed;
 // Ascending bands and light shafts convey motion without distracting from platforms.
 for(let i=0;i<9;i++){
  const y=((i*110+h*.22)%660)-60;
  ctx.strokeStyle='#b6d4ed1e';ctx.lineWidth=1;
  ctx.beginPath();ctx.moveTo(0,y);ctx.lineTo(W,y);ctx.stroke();
 }
 for(let i=0;i<15;i++){
  const x=(i*169+57)%W,y=(i*111+h*.43)%600-30;
  ctx.fillStyle='#b8e2e52a';
  ctx.beginPath();ctx.ellipse(x,y,44,9,-.09,0,TAU);ctx.fill();
 }
 for(let i=0;i<state.platforms.length;i++){
  const p=state.platforms[i];
  if(p.y<-35||p.y>H+35)continue;
  const spring=p.kind==='spring',fragile=p.kind==='fragile';
  const gradient=ctx.createLinearGradient(p.x,p.y,p.x+p.width,p.y+22);
  gradient.addColorStop(0,spring?'#5c806e':fragile?'#7d567c':'#315963');
  gradient.addColorStop(.48,spring?'#f6df9b':fragile?'#e4a9b8':'#80c7c1');
  gradient.addColorStop(1,spring?'#6f8f73':fragile?'#6c416f':'#324a79');
  ctx.globalAlpha=p.crumble!==undefined?Math.max(.3,p.crumble/.2):1;
  ctx.shadowColor=spring?'#f9dd88':fragile?'#ffabc3':p.vx!==0?'#ba9cff':'#8de2ce';
  ctx.shadowBlur=15;
  ctx.fillStyle=gradient;ctx.strokeStyle='#f2d69b';ctx.lineWidth=2;
  ctx.beginPath();ctx.roundRect(p.x,p.y,p.width,18,5);ctx.fill();ctx.stroke();
  ctx.shadowBlur=0;
  ctx.fillStyle='#fff1c45c';ctx.fillRect(p.x+7,p.y+4,Math.max(0,p.width-14),3);
  ctx.fillStyle='#243a56';ctx.beginPath();ctx.moveTo(p.x+15,p.y+19);
  ctx.lineTo(p.x+p.width/2,p.y+31);ctx.lineTo(p.x+p.width-15,p.y+19);ctx.closePath();ctx.fill();
  if(spring){
   ctx.lineWidth=3;ctx.strokeStyle='#fff1b7';ctx.beginPath();
   for(let k=0;k<=8;k++){
    const x=p.x+p.width/2-24+k*6;
    const y=p.y-3+(k%2===0?-7:3);
    if(k===0)ctx.moveTo(x,y);else ctx.lineTo(x,y);
   }
   ctx.stroke();ctx.fillStyle='#ffeac3';ctx.font='700 12px Segoe UI, sans-serif';
   ctx.textAlign='center';ctx.fillText('BOOST',p.x+p.width/2,p.y+14);
  }
  if(fragile){
   ctx.strokeStyle='#583e5e';ctx.lineWidth=2;ctx.beginPath();
   ctx.moveTo(p.x+p.width*.35,p.y+2);ctx.lineTo(p.x+p.width*.42,p.y+10);
   ctx.lineTo(p.x+p.width*.38,p.y+18);
   ctx.moveTo(p.x+p.width*.7,p.y+2);ctx.lineTo(p.x+p.width*.59,p.y+12);
   ctx.lineTo(p.x+p.width*.65,p.y+18);ctx.stroke();
  }
  if(p.vx!==0){
   ctx.fillStyle='#e9c8ff';ctx.font='700 13px Segoe UI, sans-serif';ctx.textAlign='center';
   ctx.fillText('‹  ›',p.x+p.width/2,p.y-9);
  }
  ctx.globalAlpha=1;
 }
 ctx.save();ctx.translate(state.x,state.feet-18);
 ctx.shadowColor='#e1d3a4';ctx.shadowBlur=22;
 const player=ctx.createLinearGradient(-15,-18,15,18);
 player.addColorStop(0,'#fff0bc');player.addColorStop(.45,'#8bdfdf');player.addColorStop(1,'#7283d1');
 ctx.fillStyle=player;ctx.strokeStyle='#f9dda5';ctx.lineWidth=3;
 ctx.beginPath();ctx.roundRect(-16,-18,32,36,11);ctx.fill();ctx.stroke();
 ctx.shadowBlur=0;ctx.fillStyle='#18394a';
 ctx.beginPath();ctx.arc(-5,-4,2.2,0,TAU);ctx.arc(5,-4,2.2,0,TAU);ctx.fill();
 ctx.fillStyle='#e7c68d';ctx.fillRect(-10,16,8,7);ctx.fillRect(2,16,8,7);
 ctx.restore();
 ctx.fillStyle='#f1d8a1';ctx.font='700 14px Segoe UI, sans-serif';ctx.textAlign='left';
 ctx.fillText('HEIGHT '+Math.floor(state.climbed)+' m',24,30);
 ctx.textAlign='right';ctx.fillText('JUMPS '+state.jumps,W-24,30);
 ctx.restore();
}

export function drawArcadeRun(ctx:Context,state:ArcadeRunState){
 if(isExpansionRun(state)){drawExpansionRun(ctx,state);return;}
 if(isExpansionTwoRun(state)){drawExpansionTwoRun(ctx,state);return;}
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
  case 'hexagon-hell':hexagonHell(ctx,state);break;
  case 'spike-rush':spikeRush(ctx,state);break;
  case 'endless-ascent':endlessAscent(ctx,state);break;
 }
 ctx.strokeStyle='#c6a46a';ctx.lineWidth=3;ctx.strokeRect(1.5,1.5,W-3,H-3);
 ctx.restore();
}
