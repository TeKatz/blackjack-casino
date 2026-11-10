import {CIRCUIT_COLS,CIRCUIT_ROWS,PRISM_COLS,PRISM_ROWS,
 DIRS} from './arcadeExpansionTwoEngine';
import type {ExpansionTwoRun,PrismRun,MinerRun,CircuitRun,HeistRun,PulseRun}
 from './arcadeExpansionTwoEngine';

const W=960,H=540,TAU=Math.PI*2;
type Ctx=CanvasRenderingContext2D;
const clamp=(v:number,min:number,max:number)=>Math.max(min,Math.min(max,v));
const px=(c:number)=>180+c*90,py=(r:number)=>102+r*80;
const cx=(c:number)=>190+c*118,cy=(r:number)=>90+r*88;
export function prismCellAt(x:number,y:number):{c:number;r:number}|null{
 const c=Math.round((x-180)/90),r=Math.round((y-102)/80);
 if(c<0||c>=PRISM_COLS||r<0||r>=PRISM_ROWS||Math.abs(x-px(c))>34||
  Math.abs(y-py(r))>34)return null;
 return {c,r};
}
export function circuitCellAt(x:number,y:number):{c:number;r:number}|null{
 const c=Math.round((x-190)/118),r=Math.round((y-90)/88);
 if(c<0||c>=CIRCUIT_COLS||r<0||r>=CIRCUIT_ROWS||
  Math.abs(x-cx(c))>40||Math.abs(y-cy(r))>39)return null;
 return {c,r};
}
function panel(ctx:Ctx,x:number,y:number,w:number,h:number,r:number,
 fill:string|CanvasGradient,stroke?:string){
 ctx.beginPath();ctx.roundRect(x,y,w,h,r);ctx.fillStyle=fill;ctx.fill();
 if(stroke){ctx.lineWidth=2;ctx.strokeStyle=stroke;ctx.stroke();}
}
function label(ctx:Ctx,s:string,x:number,y:number,size=15,color='#e8d4ad',
 align:CanvasTextAlign='left'){
 ctx.textAlign=align;ctx.fillStyle=color;ctx.font='700 '+size+'px Segoe UI, sans-serif';ctx.fillText(s,x,y);
}
function segment(ctx:Ctx,a:[number,number],b:[number,number],stroke:string,width=2){
 ctx.strokeStyle=stroke;ctx.lineWidth=width;ctx.beginPath();ctx.moveTo(...a);ctx.lineTo(...b);ctx.stroke();
}
function circle(ctx:Ctx,x:number,y:number,r:number,fill:string,stroke?:string){
 ctx.beginPath();ctx.arc(x,y,r,0,TAU);ctx.fillStyle=fill;ctx.fill();
 if(stroke){ctx.lineWidth=2;ctx.strokeStyle=stroke;ctx.stroke();}
}
function background(ctx:Ctx,from:string,to:string){
 const g=ctx.createLinearGradient(0,0,W,H);g.addColorStop(0,from);g.addColorStop(1,to);
 ctx.fillStyle=g;ctx.fillRect(0,0,W,H);
 for(let i=0;i<55;i++){
  const x=(i*227+17)%W,y=(i*149+31)%H;
  ctx.globalAlpha=.08+(i%5)*.048;
  circle(ctx,x,y,1+(i%3)*.4,'#ccf9eb');
 }
 ctx.globalAlpha=1;
}
function statBar(ctx:Ctx,x:number,y:number,w:number,ratio:number,color:string){
 panel(ctx,x,y,w,10,5,'#263f48','#42616a');
 if(ratio>0)panel(ctx,x,y,Math.max(1,w*clamp(ratio,0,1)),10,4,color);
}
function beam(ctx:Ctx,points:readonly {c:number;r:number}[],glow:string,bright:string){
 if(points.length<2)return;
 ctx.save();ctx.beginPath();
 points.forEach((p,i)=>i?ctx.lineTo(px(p.c),py(p.r)):ctx.moveTo(px(p.c),py(p.r)));
 ctx.lineJoin='round';ctx.lineCap='round';ctx.strokeStyle=glow;
 ctx.shadowColor=bright;ctx.shadowBlur=23;ctx.lineWidth=12;ctx.stroke();
 ctx.shadowBlur=0;ctx.strokeStyle=bright;ctx.lineWidth=3;ctx.stroke();ctx.restore();
}
function prism(ctx:Ctx,s:PrismRun){
 background(ctx,'#061b2a','#242444');
 panel(ctx,116,54,707,441,22,'#102a3b','#69858b');
 for(let r=0;r<PRISM_ROWS;r++)for(let c=0;c<PRISM_COLS;c++){
  const x=px(c),y=py(r);
  panel(ctx,x-31,y-31,62,62,9,(c+r)%2?'#193748':'#183344','#416376');
  circle(ctx,x,y,3,'#80cbbd');
 }
 // A real optical path, recalculated from the current mirror orientations.
 beam(ctx,s.beam.points,'#16bcd066',s.beam.hit?'#d8fff1':'#6eeedc');
 ctx.save();ctx.shadowColor='#fff0b1';ctx.shadowBlur=28;
 circle(ctx,91,py(2),19,'#eac886','#fff1d0');
 circle(ctx,91,py(2),9,'#f8f3dd');
 ctx.restore();
 const targetY=py(s.targetRow);
 ctx.save();ctx.shadowColor=s.beam.hit?'#e0fffa':'#edc484';ctx.shadowBlur=25;
 panel(ctx,825,targetY-26,43,52,9,s.beam.hit?'#64bdb5':'#37616a','#f3ce93');
 panel(ctx,835,targetY-16,23,32,5,'#0e2835','#96e7d5');
 ctx.restore();
 s.mirrors.forEach(m=>{
  const x=px(m.c),y=py(m.r);
  ctx.save();ctx.shadowColor=m.required?'#ecd09f':'#789aaa';ctx.shadowBlur=9;
  circle(ctx,x,y,23,m.required?'#365b65':'#2c475b','#d4b17e');
  segment(ctx,[x-14,y+(m.slash?14:-14)],[x+14,y+(m.slash?-14:14)],
   '#d6f5df',5);
  circle(ctx,x,y,4,'#f1deb7');ctx.restore();
 });
 ctx.save();ctx.strokeStyle='#ffe8b5';ctx.lineWidth=3;ctx.setLineDash([7,4]);
 const x=px(s.cursor.c),y=py(s.cursor.r);
 ctx.strokeRect(x-37,y-37,74,74);ctx.restore();
 label(ctx,'PRISM LOCK · STAGE '+s.stage,24,32,17);
 label(ctx,'LIVES '+s.lives,710,32,15,'#e9c6a5');
 label(ctx,'TIME '+Math.ceil(s.timeLeft),W-23,32,16,s.timeLeft<9?'#ffafa8':'#a6eed9','right');
 label(ctx,'CLICK MIRRORS OR USE ARROWS + SPACE',480,526,13,'#9debdc','center');
 if(s.transition){
  const won=s.transition==='clear';
  ctx.save();ctx.fillStyle='rgba(7,16,29,.91)';ctx.fillRect(0,0,W,H);
  ctx.shadowColor=won?'#97ffe2':'#ffb4aa';ctx.shadowBlur=30;
  panel(ctx,205,119,550,300,23,'#173242',won?'#8ef6d4':'#eaa08d');
  ctx.restore();
  label(ctx,won?'LEVEL COMPLETE':'LIFE LOST',W/2,205,33,
   won?'#c4ffeb':'#ffd4c8','center');
  label(ctx,won?'STAGE '+(s.stage-1)+' CLEARED':'TIME EXPIRED',
   W/2,252,19,'#eeddb6','center');
  label(ctx,won?'NEXT: STAGE '+s.stage+' · GET READY':'LIVES REMAINING: '+s.lives,
   W/2,311,20,won?'#a2eddb':'#ffc7ba','center');
  statBar(ctx,302,353,356,s.transitionClock/(won?2.15:1.85),
   won?'#9aecd5':'#f3ab9b');
 }
}
function miner(ctx:Ctx,s:MinerRun){
 background(ctx,'#061525','#192949');
 // Subtle orbital fields reinforce depth without obscuring the mining targets.
 ctx.strokeStyle='#8bbcd122';ctx.lineWidth=1;
 for(let r=115;r<620;r+=95){
  ctx.beginPath();ctx.ellipse(480,275,r,r*.48,-.16,0,TAU);ctx.stroke();
 }
 ctx.save();ctx.shadowColor='#88ead2';ctx.shadowBlur=23;
 circle(ctx,116,435,66,'#123943','#b1d4b8');
 circle(ctx,116,435,49,'#1f6164','#f4d39a');
 circle(ctx,116,435,32,'#103643','#8ae4d0');
 ctx.restore();
 label(ctx,'BASE',116,441,15,'#ffe8b5','center');
 for(const drone of s.drones){
  ctx.save();ctx.shadowColor='#fbabae';ctx.shadowBlur=21;
  circle(ctx,drone.x,drone.y,drone.r+7,'#3f435d','#d79e9a');
  circle(ctx,drone.x,drone.y,drone.r,'#a35e77','#ffe5bb');
  circle(ctx,drone.x,drone.y,5,'#fff2d2');
  for(let k=0;k<4;k++){
   const a=s.elapsed*2+k*TAU/4;
   circle(ctx,drone.x+Math.cos(a)*(drone.r+8),
    drone.y+Math.sin(a)*(drone.r+8),2.2,'#e0a9b4');
  }
  ctx.restore();
 }
 for(const rock of s.rocks){
  ctx.save();ctx.translate(rock.x,rock.y);
  ctx.shadowColor=rock.grade===2?'#d5a5ff':'#81d9c4';ctx.shadowBlur=rock.flash>0?36:12;
  ctx.beginPath();
  for(let j=0;j<9;j++){
   const angle=j*TAU/9,rad=rock.r*(.85+.13*Math.sin(j*2.1+rock.x));
   const x=Math.cos(angle)*rad,y=Math.sin(angle)*rad;
   if(j)ctx.lineTo(x,y);else ctx.moveTo(x,y);
  }
  ctx.closePath();ctx.fillStyle=rock.grade===2?'#554975':'#345966';ctx.fill();
  ctx.strokeStyle=rock.grade===2?'#dbb1ef':'#a6d9ce';ctx.lineWidth=2;ctx.stroke();
  ctx.beginPath();ctx.moveTo(-rock.r*.5,rock.r*.15);ctx.lineTo(-4,-rock.r*.45);
  ctx.lineTo(rock.r*.45,rock.r*.15);ctx.strokeStyle=rock.grade===2?'#c89bfa':'#74e7d9';ctx.lineWidth=3;ctx.stroke();
  ctx.restore();
  label(ctx,String(rock.ore),rock.x,rock.y+6,13,'#fff5d6','center');
 }
 if(s.mining&&s.mineIndex>=0&&s.rocks[s.mineIndex]){
  const a=s.rocks[s.mineIndex];
  ctx.save();ctx.shadowColor='#8efae5';ctx.shadowBlur=16;
  segment(ctx,[s.x,s.y],[a.x,a.y],'#80e9d6',5);
  ctx.restore();
  statBar(ctx,a.x-25,a.y-a.r-17,50,s.mineProgress/(.53+a.r/110),'#f0d39a');
 }
 ctx.save();ctx.translate(s.x,s.y);
 const heading=Math.atan2(s.vy,s.vx)+Math.PI/2;
 if(Math.hypot(s.vx,s.vy)>8)ctx.rotate(heading);
 ctx.globalAlpha=s.invincible>0?.55+.35*Math.sin(s.elapsed*22):1;
 ctx.shadowColor='#8ce8da';ctx.shadowBlur=15;
 ctx.beginPath();ctx.moveTo(0,-21);ctx.lineTo(16,16);ctx.lineTo(0,9);ctx.lineTo(-16,16);ctx.closePath();
 ctx.fillStyle='#8ae5d8';ctx.fill();ctx.lineWidth=2;ctx.strokeStyle='#ffe1a8';ctx.stroke();
 circle(ctx,0,-3,5,'#194659');
 ctx.restore();
 label(ctx,'OXYGEN',22,30,12,'#e9d6ac');statBar(ctx,22,38,190,s.oxygen/80,'#83e7da');
 label(ctx,'HULL '+s.hull,W-23,30,15,s.hull===1?'#f4a19e':'#e9d3a2','right');
 label(ctx,'CARGO '+s.cargo+'/'+s.capacity,24,68,13,'#d0b9f8');
 label(ctx,'DEPOSITS '+s.deposits,W-23,59,12,'#afddd7','right');
 label(ctx,'THREATS '+s.drones.length,W-23,82,12,
  s.drones.length?'#f5b0b3':'#9fdfcf','right');
 if(s.oxygen<20)label(ctx,'LOW OXYGEN',480,74,20,'#ffb0ab','center');
 label(ctx,s.atBase?'DOCKED · OXYGEN REFILL':
  s.cargo>=s.capacity?'CARGO FULL · RETURN TO BASE':'HOLD SPACE TO MINE NEAR ASTEROIDS',
  480,520,14,s.atBase?'#a7f2d7':'#f1d9a8','center');
}
function circuit(ctx:Ctx,s:CircuitRun){
 background(ctx,'#091a2b','#19303b');
 panel(ctx,100,38,764,474,19,'#102b3a','#729082');
 const live=new Set(s.powered.map(t=>t.c+':'+t.r));
 for(const tile of s.tiles){
  const x=cx(tile.c),y=cy(tile.r),powered=live.has(tile.c+':'+tile.r);
  ctx.save();ctx.shadowColor=powered?'#70f0ce':'#527f90';ctx.shadowBlur=powered?21:3;
  panel(ctx,x-39,y-37,78,74,10,powered?'#1c5d62':'#193647',
   powered?'#9af3d5':'#477985');
  ctx.lineCap='round';
  for(const d of DIRS){
   if(!(tile.mask&d.bit))continue;
   segment(ctx,[x,y],[x+d.dc*31,y+d.dr*30],
    powered?'#f5dfa7':'#7bb3ba',12);
   segment(ctx,[x,y],[x+d.dc*31,y+d.dr*30],
    powered?'#c7ffe4':'#2f6474',4);
  }
  circle(ctx,x,y,9,powered?'#f3d6a1':'#4b8491','#d5debd');
  ctx.restore();
 }
 ctx.save();ctx.strokeStyle='#fce7a8';ctx.lineWidth=3;
 ctx.setLineDash([6,4]);ctx.strokeRect(cx(s.cursor.c)-45,cy(s.cursor.r)-43,90,86);
 ctx.setLineDash([]);ctx.restore();
 circle(ctx,80,cy(2),20,'#e4c58e','#ffe9ad');
 segment(ctx,[93,cy(2)],[150,cy(2)],s.powered.length?'#a5f5d6':'#657e86',6);
 circle(ctx,881,cy(2),24,circuitExit(s)?'#7cd7b8':'#385e68','#f1d29e');
 circle(ctx,881,cy(2),9,'#e1deb3');
 label(ctx,'CIRCUIT RUSH · STAGE '+s.stage,25,26,15);
 label(ctx,'LIVES '+s.lives,715,26,14);
 label(ctx,'TIME '+Math.ceil(s.timeLeft),W-24,26,15,s.timeLeft<9?'#ffaca5':'#b1f2e0','right');
 label(ctx,'ROTATE TILES WITH CLICK OR ARROWS + SPACE',480,529,13,'#a2efdd','center');
}
function circuitExit(s:CircuitRun){
 const tile=s.tiles.find(t=>t.c===5&&t.r===2);
 return !!tile&&(tile.mask&2)!==0&&s.powered.some(t=>t.c===5&&t.r===2);
}
function heist(ctx:Ctx,s:HeistRun){
 background(ctx,'#081827','#222b39');
 panel(ctx,31,49,899,442,13,'#122d35','#8e9374');
 for(let x=62;x<940;x+=50)segment(ctx,[x,52],[x,485],'#65999b14',1);
 for(let y=77;y<480;y+=50)segment(ctx,[36,y],[930,y],'#65999b14',1);
 for(const guard of s.guards){
  const angle=guard.direction===1?0:Math.PI;
  ctx.save();ctx.translate(guard.x,guard.y);ctx.rotate(angle);
  ctx.fillStyle='#e7bf7677';ctx.beginPath();ctx.moveTo(0,0);
  ctx.arc(0,0,190+s.stage*8,-.52,.52);ctx.closePath();ctx.fill();
  ctx.strokeStyle='#f3cb9266';ctx.lineWidth=1.5;ctx.stroke();
  ctx.restore();
 }
 // Obstacles cover the guard vision cone visually as well as mechanically.
 for(const w of HEIST_WALLS){
  ctx.save();ctx.shadowColor='#030c19';ctx.shadowBlur=12;
  panel(ctx,w.x,w.y,w.w,w.h,8,'#384a51','#a4ab86');
  ctx.restore();
  panel(ctx,w.x+9,w.y+9,w.w-18,w.h-18,5,'#243b47');
 }
 for(const item of s.loot){
  if(item.taken)continue;
  ctx.save();ctx.translate(item.x,item.y);ctx.rotate(Math.PI/4);
  ctx.shadowColor='#b4f1d7';ctx.shadowBlur=25;
  panel(ctx,-11,-11,22,22,3,'#7ddcd2','#ffebbb');
  ctx.restore();
  circle(ctx,item.x,item.y,3,'#ffffff');
 }
 const open=s.loot.every(g=>g.taken);
 ctx.save();ctx.shadowColor=open?'#9fffd1':'#f0c68c';ctx.shadowBlur=21;
 panel(ctx,854,415,62,74,8,open?'#3f887a':'#453c41','#f2d6a4');
 ctx.restore();
 label(ctx,open?'EXIT':'LOCKED',885,454,12,'#f7eac4','center');
 for(const g of s.guards){
  ctx.save();ctx.translate(g.x,g.y);ctx.rotate(g.direction===1?Math.PI/2:-Math.PI/2);
  circle(ctx,0,0,15,'#e7bf8b','#553f3a');
  ctx.beginPath();ctx.moveTo(0,-18);ctx.lineTo(7,-5);ctx.lineTo(-7,-5);ctx.closePath();
  ctx.fillStyle='#512f3d';ctx.fill();ctx.restore();
 }
 ctx.save();ctx.globalAlpha=s.invincible>0?.56+.3*Math.sin(s.elapsed*19):1;
 ctx.shadowColor='#8ef2dc';ctx.shadowBlur=16;
 circle(ctx,s.x,s.y,14,'#80e6d6','#f7dfaf');
 circle(ctx,s.x+4,s.y-3,3,'#23455b');ctx.restore();
 label(ctx,'SHADOW HEIST · FLOOR '+s.stage,24,28,15);
 label(ctx,'LOOT '+s.loot.filter(g=>g.taken).length+'/'+s.loot.length,350,28,14,'#d4ffec');
 label(ctx,'LIVES '+s.lives,660,28,14);
 label(ctx,'TIME '+Math.ceil(s.timeLeft),W-23,28,15,s.timeLeft<20?'#ffaba3':'#a9e9d8','right');
 statBar(ctx,366,506,230,s.alert,'#f5a27c');
 label(ctx,'ALERT',357,517,12,'#edd1a2','right');
 if(s.lastCaught>0)label(ctx,'DETECTED!',480,80,23,'#ffaea2','center');
}
function pulse(ctx:Ctx,s:PulseRun){
 background(ctx,'#0c1730','#302044');
 const x=480,y=285,glow=s.feedback==='perfect'?'#c8ffe6':s.feedback==='miss'?'#ff9c9b':'#bdc5f4';
 ctx.save();ctx.strokeStyle='#5e5d8380';ctx.lineWidth=1;
 for(let r=44;r<285;r+=38){ctx.beginPath();ctx.arc(x,y,r,0,TAU);ctx.stroke();}
 for(let i=0;i<16;i++){
  const a=i*TAU/16,r=250;
  segment(ctx,[x+Math.cos(a)*(r-9),y+Math.sin(a)*(r-9)],
   [x+Math.cos(a)*(r+9),y+Math.sin(a)*(r+9)],'#9985b766',2);
 }
 ctx.shadowColor='#e7cdab';ctx.shadowBlur=28;
 ctx.beginPath();ctx.arc(x,y,s.target,0,TAU);
 ctx.strokeStyle='#e7c791';ctx.lineWidth=11;ctx.stroke();ctx.shadowBlur=0;
 ctx.beginPath();ctx.arc(x,y,s.target,0,TAU);
 ctx.strokeStyle='#fff1cb';ctx.lineWidth=2;ctx.stroke();
 ctx.shadowColor=glow;ctx.shadowBlur=32;
 ctx.beginPath();ctx.arc(x,y,s.radius,0,TAU);
 ctx.strokeStyle=glow;ctx.lineWidth=6;ctx.stroke();ctx.restore();
 ctx.save();ctx.shadowColor='#a7ead9';ctx.shadowBlur=30;
 circle(ctx,x,y,31,'#284c59','#e9d0a4');
 circle(ctx,x,y,18,'#89e7d7','#f9e9bf');ctx.restore();
 for(let i=0;i<10;i++){
  const angle=i*TAU/10+s.elapsed*.5;
  const r=s.radius;
  circle(ctx,x+Math.cos(angle)*r,y+Math.sin(angle)*r,3,'#adf2e6');
 }
 if(s.feedback){
  const word=s.feedback==='perfect'?'PERFECT!':s.feedback==='good'?'GOOD!':'MISSED!';
  label(ctx,word,x,112,33,s.feedback==='miss'?'#ffaaa5':'#c7ffdf','center');
  if(s.lastPoints>0)label(ctx,'+'+s.lastPoints,x,150,19,'#ffe1ad','center');
 }
 label(ctx,'PULSE PERFECT',24,31,18);
 label(ctx,'COMBO ×'+s.combo,24,58,16,'#b7f3df');
 label(ctx,'LIVES '+s.lives,W-24,30,16,s.lives===1?'#ffaca5':'#e7d2ae','right');
 label(ctx,'ROUND '+s.round,W-24,57,13,'#c3c3e9','right');
 label(ctx,'PRESS SPACE WHEN THE RINGS ALIGN',480,523,14,'#e4d5b7','center');
}
export function drawExpansionTwoRun(ctx:Ctx,run:ExpansionTwoRun):void{
 ctx.save();ctx.clearRect(0,0,W,H);
 switch(run.kind){
 case 'prism-lock':prism(ctx,run);break;
 case 'meteor-miner':miner(ctx,run);break;
 case 'circuit-rush':circuit(ctx,run);break;
 case 'shadow-heist':heist(ctx,run);break;
 case 'pulse-perfect':pulse(ctx,run);break;
 }
 ctx.strokeStyle='#c6a46a';ctx.lineWidth=3;ctx.strokeRect(1.5,1.5,W-3,H-3);
 ctx.restore();
}
