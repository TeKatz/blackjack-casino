/**
 * Deterministic, dependency-free simulation for the first four Arcade Games.
 * Every game uses the same logical 960×540 coordinate space and fixed 60Hz steps.
 * Rendering is intentionally separate from input and persistence.
 */
export const ARCADE_WIDTH=960;
export const ARCADE_HEIGHT=540;
export const ARCADE_STEP=1/60;
export const PLAYABLE_ARCADE_IDS=[
 'midnight-wings','cave-escape','perfect-stack','blade-timing',
] as const;
export type PlayableArcadeId=typeof PLAYABLE_ARCADE_IDS[number];
export const isPlayableArcade=(id:string):id is PlayableArcadeId=>
 (PLAYABLE_ARCADE_IDS as readonly string[]).includes(id);
export type ArcadeEvent='flap'|'score'|'fail'|'stage'|null;
type Base={score:number;elapsed:number;over:boolean;seed:number};
export type WingPipe={x:number;center:number;gap:number;passed:boolean};
export type WingsRun=Base&{kind:'midnight-wings';y:number;vy:number;spawn:number;pipes:WingPipe[]};
export type CaveSegment={x:number;center:number;gap:number};
export type CaveRun=Base&{kind:'cave-escape';y:number;vy:number;segments:CaveSegment[];distance:number};
export type StackBlock={x:number;width:number};
export type StackRun=Base&{kind:'perfect-stack';blocks:StackBlock[];movingX:number;direction:number;perfect:number};
export type BladeRun=Base&{kind:'blade-timing';rotation:number;spin:number;blades:number[];stage:number;hits:number;needed:number};
export type ArcadeRunState=WingsRun|CaveRun|StackRun|BladeRun;

const clamp=(value:number,min:number,max:number)=>Math.max(min,Math.min(max,value));
const random=(state:Base)=>{
 state.seed=(Math.imul(state.seed,1664525)+1013904223)>>>0;
 return state.seed/4294967296;
};
const seedValue=(seed:number)=>((seed>>>0)||0x51c3a9e1);
export function createArcadeRun(kind:PlayableArcadeId,seed=0x543fa1d2):ArcadeRunState{
 const base={score:0,elapsed:0,over:false,seed:seedValue(seed)};
 switch(kind){
  case 'midnight-wings':return {...base,kind,y:265,vy:0,spawn:1.4,
   pipes:[{x:1020,center:265,gap:208,passed:false}]};
  case 'cave-escape':return {...base,kind,y:270,vy:0,distance:0,
   segments:Array.from({length:26},(_,i)=>({x:i*42-42,center:270,gap:292}))};
  case 'perfect-stack':return {...base,kind,blocks:[{x:340,width:280}],
   movingX:0,direction:1,perfect:0};
  case 'blade-timing':return {...base,kind,rotation:0,spin:1.4,
   blades:[-Math.PI/2,0],stage:1,hits:0,needed:5};
 }
}
function fail(state:Base):ArcadeEvent{state.over=true;return 'fail';}
export function isInsideCave(run:CaveRun,x:number,y:number,radius=13){
 const segments=run.segments;
 let i=0;
 while(i<segments.length-2&&segments[i+1].x<x)i++;
 const a=segments[i],b=segments[i+1];
 const t=clamp((x-a.x)/Math.max(1,b.x-a.x),0,1);
 const center=a.center+(b.center-a.center)*t;
 const gap=a.gap+(b.gap-a.gap)*t;
 return y-radius>center-gap/2&&y+radius<center+gap/2;
}
export function angularDistance(a:number,b:number){
 return Math.abs(Math.atan2(Math.sin(a-b),Math.cos(a-b)));
}
export function actArcadeRun(state:ArcadeRunState):ArcadeEvent{
 if(state.over)return null;
 switch(state.kind){
 case 'midnight-wings':
  state.vy=-335;
  return 'flap';
 case 'cave-escape':return null; // held input controls lift on fixed steps
 case 'perfect-stack':{
  const last=state.blocks[state.blocks.length-1];
  const width=last.width;
  const exact=Math.abs(state.movingX-last.x)<=5;
  const left=exact?last.x:Math.max(last.x,state.movingX);
  const right=exact?last.x+width:Math.min(last.x+width,state.movingX+width);
  if(right-left<4)return fail(state);
  state.blocks.push({x:left,width:right-left});
  state.score++;
  if(exact)state.perfect++;
  else state.perfect=0;
  const nextWidth=right-left;
  const fromRight=state.blocks.length%2===0;
  state.movingX=fromRight?ARCADE_WIDTH-nextWidth:0;
  state.direction=fromRight?-1:1;
  return 'score';
 }
 case 'blade-timing':{
  const injection=Math.PI/2;
  for(const blade of state.blades){
   if(angularDistance(blade+state.rotation,injection)<.21)return fail(state);
  }
  state.blades.push(injection-state.rotation);
  state.score++;
  state.hits++;
  if(state.hits>=state.needed){
   state.stage++;
   state.hits=0;
   state.needed=Math.min(10,5+Math.floor(state.stage/2));
   state.blades=[-Math.PI/2-state.rotation,0-state.rotation];
   return 'stage';
  }
  return 'score';
 }
 }
}
export function stepArcadeRun(state:ArcadeRunState,dt:number,holding=false):ArcadeEvent{
 if(state.over||!Number.isFinite(dt)||dt<=0)return null;
 // Hard cap prevents simulation jumps on tab resumes; the React runner uses a fixed step.
 const step=Math.min(dt,ARCADE_STEP);
 state.elapsed+=step;
 switch(state.kind){
 case 'midnight-wings':{
  state.vy+=990*step;
  state.y+=state.vy*step;
  if(state.y<18||state.y>ARCADE_HEIGHT-20)return fail(state);
  const speed=Math.min(355,220+state.score*3.3);
  state.spawn-=step;
  if(state.spawn<=0){
   const center=160+random(state)*220;
   state.pipes.push({x:1020,center,gap:Math.max(152,208-state.score*2),passed:false});
   state.spawn+=Math.max(1.05,1.55-state.score*.013);
  }
  for(const pipe of state.pipes){
   pipe.x-=speed*step;
   if(pipe.x<231&&pipe.x+88>191 &&
    (state.y-16<pipe.center-pipe.gap/2||state.y+16>pipe.center+pipe.gap/2))
    return fail(state);
   if(!pipe.passed&&pipe.x+88<210){pipe.passed=true;state.score++;}
  }
  state.pipes=state.pipes.filter(p=>p.x+88>-20);
  return null;
 }
 case 'cave-escape':{
  const speed=Math.min(335,195+state.elapsed*1.6);
  state.vy=clamp(state.vy+(holding?-790:650)*step,-320,330);
  state.y+=state.vy*step;
  state.distance+=speed*step;
  state.score=Math.floor(state.distance/20);
  for(const segment of state.segments)segment.x-=speed*step;
  while(state.segments[1].x<0)state.segments.shift();
  while(state.segments[state.segments.length-1].x<ARCADE_WIDTH+55){
   const last=state.segments[state.segments.length-1];
   const nextCenter=clamp(last.center+(random(state)-.5)*53,162,378);
   state.segments.push({x:last.x+42,center:nextCenter,
    gap:Math.max(148,290-state.elapsed*1.65)});
  }
  return isInsideCave(state,225,state.y,14)?null:fail(state);
 }
 case 'perfect-stack':{
  const width=state.blocks[state.blocks.length-1].width;
  const speed=Math.min(590,260+(state.blocks.length-1)*22);
  state.movingX+=state.direction*speed*step;
  if(state.movingX<=0){state.movingX=0;state.direction=1;}
  if(state.movingX>=ARCADE_WIDTH-width){
   state.movingX=ARCADE_WIDTH-width;state.direction=-1;
  }
  return null;
 }
 case 'blade-timing':{
  // Smooth deterministic rotation; reverse direction between stages.
  const speed=1.4+Math.min(2.3,state.stage*.2);
  const pulse=.8+.2*Math.sin(state.elapsed*1.4+state.stage);
  state.spin=(state.stage%2===0?-1:1)*speed*pulse;
  state.rotation+=state.spin*step;
  return null;
 }
 }
}
