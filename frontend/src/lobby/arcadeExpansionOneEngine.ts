import {createPinballRun,launchPinball,stepPinball} from './arcadePinballV2';
import type {PinballRun} from './arcadePinballV2';
export {pinBumpers,pinballFlipper,pinballGeometry} from './arcadePinballV2';
export type {PinballRun} from './arcadePinballV2';
/**
 * Arcade Expansion I — five independent, deterministic, offline minigames.
 * Pure 60 Hz simulation. The React runner owns user inputs, pause and high scores.
 */
export const EXPANSION_ONE_IDS=[
 'midnight-pinball','phantom-hook','neon-drift','gravity-chamber','echo-sequence',
] as const;
export type ExpansionOneId=typeof EXPANSION_ONE_IDS[number];
export const isExpansionOneId=(id:string):id is ExpansionOneId=>
 (EXPANSION_ONE_IDS as readonly string[]).includes(id);
type Base={score:number;elapsed:number;over:boolean;seed:number};
export type ExpansionEvent='flap'|'score'|'fail'|'stage'|null;
export type ExpansionControls={left?:boolean;right?:boolean;up?:boolean;down?:boolean;pointerX?:number;pointerY?:number};
const W=960,H=540,STEP=1/60;
const clamp=(v:number,a:number,b:number)=>Math.max(a,Math.min(b,v));
const rand=(s:Base)=>{s.seed=(Math.imul(s.seed,1664525)+1013904223)>>>0;return s.seed/4294967296;};
export type HookAnchor={x:number;y:number};
export type HookRun=Base&{kind:'phantom-hook';x:number;y:number;vx:number;vy:number;
 anchored:number;rope:number;anchors:HookAnchor[];camera:number;lastAnchor:number};
export type DriftRun=Base&{kind:'neon-drift';x:number;vx:number;distance:number;speed:number;
 combo:number;driftPoints:number;hits:number;hitCooldown:number;checkpoint:number};
export type GravityRun=Base&{kind:'gravity-chamber';x:number;y:number;vx:number;vy:number;
 gravity:1|-1;stage:number;lives:number;invulnerable:number};
export type EchoRun=Base&{kind:'echo-sequence';round:number;lives:number;
 sequence:number[];phase:'watch'|'input'|'celebrate';clock:number;index:number;
 active:number;inputClock:number;lastChoice:number;feedback:number;lastGood:boolean};
export type ExpansionRun=PinballRun|HookRun|DriftRun|GravityRun|EchoRun;
export const isExpansionRun=(s:{kind:string}):s is ExpansionRun=>isExpansionOneId(s.kind);
export const driftCenter=(distance:number)=>
 480+118*Math.sin(distance*.00215)+57*Math.sin(distance*.0047);
export const gravityGoalY=(stage:number)=>stage%2===0?93:438;
export const gravityHazards=(stage:number)=>[
 {x:260+(stage%3)*22,y:444,w:84,h:25},
 {x:480-(stage%3)*16,y:67,w:92,h:19},
 {x:645+(stage%2)*12,y:444,w:79,h:25},
];
export function createExpansionRun(kind:ExpansionOneId,seed=0x51eab141):ExpansionRun{
 const base={score:0,elapsed:0,over:false,seed:(seed>>>0)||0x51eab141};
 switch(kind){
  case 'midnight-pinball':return createPinballRun(seed);
  case 'phantom-hook':return {...base,kind,x:145,y:302,vx:164,vy:-90,
   anchored:-1,rope:185,camera:0,lastAnchor:0,
   anchors:[{x:226,y:128},{x:475,y:180},{x:735,y:111},{x:984,y:172}]};
  case 'neon-drift':return {...base,kind,x:480,vx:0,distance:0,speed:260,
   combo:0,driftPoints:0,hits:0,hitCooldown:0,checkpoint:0};
  case 'gravity-chamber':return {...base,kind,x:132,y:438,vx:0,vy:0,
   gravity:1,stage:1,lives:3,invulnerable:0};
  case 'echo-sequence':{
   const run:EchoRun={...base,kind,round:1,lives:3,sequence:[],phase:'watch',
    clock:0,index:0,active:-1,inputClock:0,lastChoice:-1,feedback:0,lastGood:true};
   run.sequence.push(Math.floor(rand(run)*6));return run;
  }
 }
}
function fail(s:Base):ExpansionEvent{s.over=true;return 'fail';}
export function chooseEchoPad(s:EchoRun,pad:number):ExpansionEvent{
 if(s.over||s.phase!=='input'||!Number.isSafeInteger(pad)||pad<0||pad>=6)return null;
 s.lastChoice=pad;s.feedback=.28;s.lastGood=s.sequence[s.index]===pad;
 if(!s.lastGood){
  s.lives--;
  if(s.lives<=0)return fail(s);
  s.phase='watch';s.clock=0;s.index=0;s.active=-1;s.inputClock=0;
  return 'stage';
 }
 s.index++;s.inputClock=0;
 if(s.index<s.sequence.length)return 'flap';
 s.score+=100*s.round;
 s.round++;s.sequence.push(Math.floor(rand(s)*6));
 s.phase='celebrate';s.clock=0;s.index=0;s.active=-1;
 return 'score';
}
export function actExpansionRun(s:ExpansionRun):ExpansionEvent{
 if(s.over)return null;
 switch(s.kind){
 case 'midnight-pinball':return launchPinball(s);
 case 'phantom-hook':
  if(s.anchored>=0){
   s.anchored=-1;s.vy=Math.min(s.vy,-155);return 'flap';
  }
  {
   let best=-1,dist=240;
   for(let i=0;i<s.anchors.length;i++){
    const a=s.anchors[i],d=Math.hypot(s.x-a.x,s.y-a.y);
    if(d<dist&&a.x>s.x-115){dist=d;best=i;}
   }
   if(best<0)return null;
   s.anchored=best;s.rope=Math.max(94,dist);return 'flap';
  }
 case 'gravity-chamber':
  s.gravity=s.gravity===1?-1:1;
  s.vy=s.gravity*90;return 'flap';
 case 'neon-drift':case 'echo-sequence':return null;
 }
}
function hookStep(s:HookRun,dt:number):ExpansionEvent{
 s.vy=clamp(s.vy+835*dt,-900,920);
 s.vx=clamp(s.vx+24*dt,-110,540);
 s.x+=s.vx*dt;s.y+=s.vy*dt;
 if(s.anchored>=0){
  const a=s.anchors[s.anchored];
  if(!a){s.anchored=-1;}
  else{
   const dx=s.x-a.x,dy=s.y-a.y,d=Math.hypot(dx,dy);
   if(d>s.rope){
    const nx=dx/d,ny=dy/d;
    s.x=a.x+nx*s.rope;s.y=a.y+ny*s.rope;
    const outward=s.vx*nx+s.vy*ny;
    if(outward>0){s.vx-=outward*nx;s.vy-=outward*ny;}
    // Powered swing preserves momentum without allowing runaway speed.
    s.vx=clamp(s.vx+56*dt, -240,580);
   }
  }
 }
 if(s.y>H+24||s.x<s.camera-85)return fail(s);
 s.camera=Math.max(0,s.x-270);
 while(s.anchors[s.anchors.length-1].x<s.camera+1080){
  const x=s.anchors[s.anchors.length-1].x+215+rand(s)*90;
  s.anchors.push({x,y:110+rand(s)*128});
 }
 if(s.anchors.length>14){
  const removed=s.anchors.splice(0,s.anchors.length-14).length;
  s.anchored=s.anchored<0?-1:Math.max(-1,s.anchored-removed);
 }
 const nextScore=Math.max(s.score,Math.floor(Math.max(0,s.x-145)/22));
 if(nextScore!==s.score){s.score=nextScore;return 'score';}
 return null;
}
function driftStep(s:DriftRun,dt:number,holding:boolean,c:ExpansionControls):ExpansionEvent{
 const steer=Number(!!c.right)-Number(!!c.left);
 const brake=holding;
 s.hitCooldown=Math.max(0,s.hitCooldown-dt);
 const target=brake?208:365;
 s.speed+=(target-s.speed)*dt*(brake?1.6:.7);
 s.speed=clamp(s.speed,160,410);
 const sideTarget=steer*(brake?325:255);
 s.vx+=(sideTarget-s.vx)*dt*(brake?1.3:2.8);
 s.vx=clamp(s.vx,-330,330);
 s.x+=s.vx*dt;
 s.distance+=s.speed*dt;
 const road=driftCenter(s.distance),limit=169;
 if(Math.abs(s.x-road)>limit&&s.hitCooldown===0){
  s.hits++;s.hitCooldown=1.4;s.combo=0;s.speed=190;
  s.x=road+Math.sign(s.x-road)*limit;
  if(s.hits>=3)return fail(s);
  return 'stage';
 }
 s.x=clamp(s.x,40,W-40);
 if(brake&&steer&&Math.abs(s.vx)>75&&Math.abs(s.x-road)<limit-12){
  s.combo=Math.min(8,s.combo+dt*1.5);
  s.driftPoints+=dt*(10+s.combo*9);
 }else s.combo=Math.max(0,s.combo-dt*2.4);
 const nextScore=Math.floor(s.distance/12)+Math.floor(s.driftPoints)+s.checkpoint*100;
 s.score=nextScore;
 const checkpoint=Math.floor(s.distance/1200);
 if(checkpoint>s.checkpoint){s.checkpoint=checkpoint;s.score+=100;return 'stage';}
 return null;
}
function gravityStep(s:GravityRun,dt:number,c:ExpansionControls):ExpansionEvent{
 s.invulnerable=Math.max(0,s.invulnerable-dt);
 const dir=Number(!!c.right)-Number(!!c.left);
 s.vx+=(dir*285-s.vx)*dt*8;
 s.x+=s.vx*dt;
 s.vy=clamp(s.vy+s.gravity*1050*dt,-630,630);
 s.y+=s.vy*dt;
 s.x=clamp(s.x,102,859);s.y=clamp(s.y,93,438);
 if((s.y===93&&s.vy<0)||(s.y===438&&s.vy>0))s.vy=0;
 const hazards=gravityHazards(s.stage);
 if(s.invulnerable===0&&hazards.some(h=>s.x+13>h.x&&s.x-13<h.x+h.w&&
   s.y+13>h.y&&s.y-13<h.y+h.h)){
  s.lives--;
  if(s.lives<=0)return fail(s);
  s.x=132;s.y=438;s.vx=0;s.vy=0;s.gravity=1;s.invulnerable=.85;
  return 'stage';
 }
 if(s.x>807&&Math.abs(s.y-gravityGoalY(s.stage))<33){
  s.score+=150+s.stage*25;
  s.stage++;s.x=132;s.y=438;s.vx=0;s.vy=0;s.gravity=1;s.invulnerable=.6;
  return 'score';
 }
 return null;
}
function echoStep(s:EchoRun,dt:number):ExpansionEvent{
 s.feedback=Math.max(0,s.feedback-dt);
 if(s.phase==='celebrate'){
  s.clock+=dt;
  if(s.clock>.7){s.phase='watch';s.clock=0;s.index=0;s.active=-1;}
  return null;
 }
 if(s.phase==='watch'){
  s.clock+=dt;
  const position=Math.floor(s.clock/.79);
  if(position>=s.sequence.length){
   s.phase='input';s.clock=0;s.inputClock=0;s.index=0;s.active=-1;return 'stage';
  }
  s.active=s.clock% .79<.51?s.sequence[position]:-1;
  return null;
 }
 s.inputClock+=dt;
 const maxTime=6+s.sequence.length*1.6;
 if(s.inputClock>maxTime){
  s.lives--;
  if(s.lives<=0)return fail(s);
  s.phase='watch';s.clock=0;s.index=0;s.active=-1;s.inputClock=0;
  return 'stage';
 }
 return null;
}
export function stepExpansionRun(s:ExpansionRun,dt:number,holding=false,
 controls:ExpansionControls={}):ExpansionEvent{
 if(s.over||!Number.isFinite(dt)||dt<=0)return null;
 const step=Math.min(dt,STEP);
 s.elapsed+=step;
 switch(s.kind){
  case 'midnight-pinball':return stepPinball(s,step,controls);
  case 'phantom-hook':return hookStep(s,step);
  case 'neon-drift':return driftStep(s,step,holding,controls);
  case 'gravity-chamber':return gravityStep(s,step,controls);
  case 'echo-sequence':return echoStep(s,step);
 }
}
