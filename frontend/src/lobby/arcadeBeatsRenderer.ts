import {
 BEATS_LANE_COLORS,BEATS_KEYS,BEATS_HIT_LINE_Y,BEATS_START_Y,
} from './arcadeBeatsEngine';
import type {BeatsRun} from './arcadeBeatsEngine';

type Ctx=CanvasRenderingContext2D;
const W=960,H=540,TAU=Math.PI*2;
const X=246,LANE_W=108,LANE_GAP=10;
const clamp=(v:number,a:number,b:number)=>Math.max(a,Math.min(b,v));
const laneX=(lane:number)=>X+lane*(LANE_W+LANE_GAP);
function rect(ctx:Ctx,x:number,y:number,w:number,h:number,r:number,
 color:string|CanvasGradient,edge?:string){
 ctx.beginPath();ctx.roundRect(x,y,w,h,r);
 ctx.fillStyle=color;ctx.fill();
 if(edge){ctx.strokeStyle=edge;ctx.lineWidth=2;ctx.stroke();}
}
function text(ctx:Ctx,value:string,x:number,y:number,size=16,
 color='#e9d9c2',align:CanvasTextAlign='left'){
 ctx.font='700 '+size+'px Segoe UI, sans-serif';
 ctx.fillStyle=color;ctx.textAlign=align;ctx.fillText(value,x,y);
}
function glowCircle(ctx:Ctx,x:number,y:number,r:number,color:string){
 ctx.save();ctx.shadowBlur=24;ctx.shadowColor=color;
 ctx.fillStyle=color;ctx.beginPath();ctx.arc(x,y,r,0,TAU);ctx.fill();
 ctx.restore();
}
export function beatsLaneAt(x:number,y:number):number|null{
 if(!Number.isFinite(x)||!Number.isFinite(y)||y<85||y>506)return null;
 const candidate=Math.floor((x-X)/(LANE_W+LANE_GAP));
 if(candidate<0||candidate>3)return null;
 const local=x-laneX(candidate);
 return local>=0&&local<=LANE_W?candidate:null;
}
function drawBackdrop(ctx:Ctx,s:BeatsRun){
 const gradient=ctx.createLinearGradient(0,0,960,540);
 gradient.addColorStop(0,'#090f21');gradient.addColorStop(.46,'#16172e');
 gradient.addColorStop(1,'#261a36');ctx.fillStyle=gradient;ctx.fillRect(0,0,W,H);
 const pulse=s.lastGroovePulse>=0?Math.max(0,1-(s.nextGrooveAt-s.elapsed)*s.bpm/30):0;
 // A coherent club stage: speaker stacks, side equalizers and muted spotlights.
 for(let j=0;j<5;j++){
  const x=74+j*200;
  ctx.save();ctx.globalAlpha=.035+.035*pulse;
  ctx.fillStyle=j%2===0?'#a7e8e0':'#e5b9ed';
  ctx.beginPath();ctx.moveTo(x,-12);ctx.lineTo(x+120,525);
  ctx.lineTo(x-120,525);ctx.closePath();ctx.fill();ctx.restore();
 }
 for(const side of [0,1]){
  const sx=side===0?44:806;
  rect(ctx,sx,118,102,305,16,'#152c3d','#496575');
  for(let i=0;i<3;i++){
   const y=168+i*92;
   glowCircle(ctx,sx+51,y,29,'#254b59');
   glowCircle(ctx,sx+51,y,19,i===1?'#514267':'#467c85');
   glowCircle(ctx,sx+51,y,8,'#102737');
  }
  for(let col=0;col<9;col++){
   const h=22+20*Math.abs(Math.sin(s.elapsed*2+col*.67+(side?2:0)));
   const v=h*(.58+.42*pulse);
   rect(ctx,sx+8+col*10,446-v,6,v,3,col%3===0?'#b6a1ef':'#6fcabd');
  }
 }
 for(let i=0;i<35;i++){
  const x=(i*283+37)%W,y=(i*137+23)%H;
  ctx.globalAlpha=.07+(i%6)*.025;
  glowCircle(ctx,x,y,1.2,i%3===0?'#c9a9fc':'#a2e9dd');
 }
 ctx.globalAlpha=1;
}
function drawHighway(ctx:Ctx,s:BeatsRun){
 rect(ctx,X-17,78,4*(LANE_W+LANE_GAP)-LANE_GAP+34,418,18,'#101e32','#9d92a1');
 for(let lane=0;lane<4;lane++){
  const x=laneX(lane),color=BEATS_LANE_COLORS[lane];
  const background=ctx.createLinearGradient(x,90,x,485);
  background.addColorStop(0,lane%2?'#17253c':'#172c3d');
  background.addColorStop(1,lane%2?'#243147':'#1b3544');
  rect(ctx,x,88,LANE_W,392,8,background,color+'44');
  if(s.laneGlow[lane]>0){
   ctx.save();ctx.globalAlpha=clamp(s.laneGlow[lane]*2.6,0,.57);
   rect(ctx,x,101,LANE_W,366,8,color);ctx.restore();
  }
  // A colored, stable target button with its physical keyboard label.
  rect(ctx,x+8,437,LANE_W-16,42,10,'#142438',color);
  rect(ctx,x+12,443,LANE_W-24,30,8,'#1a3246');
  text(ctx,BEATS_KEYS[lane],x+LANE_W/2,467,23,color,'center');
  ctx.save();ctx.globalAlpha=.35;
  for(let j=0;j<8;j++)rect(ctx,x+10+j*12,96,3,7,1,color);
  ctx.restore();
 }
 // Musical beat subdivisions drift toward the target in the same time frame.
 ctx.save();ctx.beginPath();
 ctx.rect(X,90,4*(LANE_W+LANE_GAP)-LANE_GAP,344);ctx.clip();
 for(let pulse=Math.max(0,s.nextGroovePulse-2);pulse<=s.nextGroovePulse+8;pulse++){
  // Period-local scrollgrid. Decorative beat markers, not alternate note times.
  const target=s.nextGrooveAt+(pulse-s.nextGroovePulse)*(30/s.bpm);
  const progress=1-(target-s.elapsed)/1.95;
  const y=BEATS_START_Y+progress*(BEATS_HIT_LINE_Y-BEATS_START_Y);
  if(y<90||y>427)continue;
  ctx.globalAlpha=pulse%2===0?.19:.08;
  ctx.strokeStyle='#bcd5cc';ctx.lineWidth=pulse%2===0?2:1;
  ctx.beginPath();ctx.moveTo(X,y);ctx.lineTo(X+4*(LANE_W+LANE_GAP)-LANE_GAP,y);ctx.stroke();
 }
 ctx.restore();ctx.globalAlpha=1;
}
function drawNotes(ctx:Ctx,s:BeatsRun){
 const top=90,bottom=435;
 ctx.save();ctx.beginPath();ctx.rect(X,top,462,bottom-top);ctx.clip();
 const glow=Math.max(0,Math.sin(s.elapsed*9)*.1+.9);
 for(const note of s.notes){
  const passed=s.elapsed-note.at;
  const progress=1+(passed/note.travel);
  const y=BEATS_START_Y+progress*(BEATS_HIT_LINE_Y-BEATS_START_Y);
  if(y<70||y>445)continue;
  const color=BEATS_LANE_COLORS[note.lane],x=laneX(note.lane);
  if(note.judgement){
   if(note.judgement==='MISS')continue;
   const fade=clamp(1-(s.elapsed-note.judgedAt)/.26,0,1);
   ctx.save();ctx.globalAlpha=fade;
   ctx.strokeStyle=color;ctx.lineWidth=4;
   ctx.beginPath();ctx.arc(x+LANE_W/2,BEATS_HIT_LINE_Y,27+(1-fade)*24,0,TAU);ctx.stroke();
   ctx.restore();
   continue;
  }
  ctx.save();ctx.shadowColor=color;ctx.shadowBlur=18*glow;
  rect(ctx,x+9,y-13,LANE_W-18,25,7,color,'#fff2dc');
  rect(ctx,x+17,y-9,LANE_W-34,6,3,'#ffffffbb');
  ctx.restore();
 }
 ctx.restore();
 // The hit line is intentionally drawn in FRONT of notes.
 ctx.save();ctx.shadowColor='#fff0c0';ctx.shadowBlur=24;
 rect(ctx,X-8,BEATS_HIT_LINE_Y-3,480,7,3,'#fff2ce');ctx.restore();
 ctx.save();ctx.globalAlpha=.33;
 rect(ctx,X-8,BEATS_HIT_LINE_Y-21,480,42,4,'#b5e5da');
 ctx.restore();
 for(let lane=0;lane<4;lane++){
  const x=laneX(lane);
  if(s.laneGlow[lane]<=0)continue;
  ctx.save();ctx.globalAlpha=clamp(s.laneGlow[lane]*3.4,0,1);
  ctx.shadowColor=BEATS_LANE_COLORS[lane];ctx.shadowBlur=26;
  rect(ctx,x+7,BEATS_HIT_LINE_Y-17,LANE_W-14,35,8,BEATS_LANE_COLORS[lane]);
  ctx.restore();
 }
}
function drawFeedback(ctx:Ctx,s:BeatsRun){
 if(s.feedbackClock>0&&s.lastGrade){
  const color=s.lastGrade==='PERFECT'?'#9dffe7':s.lastGrade==='GREAT'?
   '#f4d59e':s.lastGrade==='GOOD'?'#c0b8ef':'#ffacbb';
  const y=230-13*(1-s.feedbackClock/.6);
  ctx.save();ctx.globalAlpha=clamp(s.feedbackClock*3.4,0,1);
  ctx.shadowColor=color;ctx.shadowBlur=22;
  text(ctx,s.lastGrade,480,y,29,color,'center');
  if(s.lastHitPoints>0)text(ctx,'+'+s.lastHitPoints,480,y+31,18,'#fff1d1','center');
  ctx.restore();
 }
 if(s.stageClock>0){
  ctx.save();ctx.globalAlpha=clamp(s.stageClock*1.3,0,1);
  rect(ctx,317,147,326,51,13,'#2c4051','#c4aa87');
  text(ctx,'STAGE '+s.stage+'  ·  '+s.bpm+' BPM',480,181,20,'#ffebb6','center');
  ctx.restore();
 }
 if(s.elapsed<2.52){
  const count=4-Math.floor(Math.max(0,s.elapsed-BEATS_FIRST_PULSE_AT)*s.bpm/60);
  if(count>0&&count<=4){
   ctx.save();ctx.globalAlpha=.89;
   text(ctx,count===1?'GO!':String(count-1),480,300,49,'#e9ffdf','center');
   ctx.restore();
  }
 }
}
export function drawBeatsRun(ctx:Ctx,s:BeatsRun):void{
 ctx.save();ctx.clearRect(0,0,W,H);
 drawBackdrop(ctx,s);drawHighway(ctx,s);drawNotes(ctx,s);drawFeedback(ctx,s);
 text(ctx,'MIDNIGHT BEATS',30,40,24,'#f9dfac');
 text(ctx,'ENDLESS RHYTHM',31,61,12,'#9de7d8');
 text(ctx,'STAGE '+s.stage,807,36,18,'#eacb9d');
 text(ctx,s.bpm+' BPM',807,60,15,'#a1e7dd');
 text(ctx,'COMBO',28,96,14,'#a1dcd2');
 text(ctx,String(s.combo),27,123,33,'#fbe2b4');
 text(ctx,'BEST COMBO '+s.bestCombo,25,151,13,'#a7c8d6');
 text(ctx,'LIVES',812,96,14,'#e6c99a');
 for(let i=0;i<5;i++){
  const x=824+(i%3)*28,y=123+Math.floor(i/3)*28;
  ctx.save();ctx.globalAlpha=i<s.lives?1:.2;
  glowCircle(ctx,x,y,9,'#f69fae');ctx.restore();
 }
 text(ctx,'PERFECT '+s.perfect,28,500,12,'#9eead5');
 text(ctx,'GREAT '+s.great,175,500,12,'#f9dea8');
 text(ctx,'MISSES '+s.misses,805,500,12,'#ffb2b8');
 ctx.fillStyle='#c9b08b';ctx.fillRect(1.5,1.5,957,1.5);
 ctx.strokeStyle='#c6a46a';ctx.lineWidth=3;ctx.strokeRect(1.5,1.5,W-3,H-3);
 ctx.restore();
}
