import {EXPANSION_TWO_IDS,isExpansionTwoId,isExpansionTwoRun,createExpansionTwoRun,actExpansionTwoRun,stepExpansionTwoRun} from './arcadeExpansionTwoEngine';
import type {ExpansionTwoRun} from './arcadeExpansionTwoEngine';
export {selectPrism,rotateCircuit,movePuzzleCursor} from './arcadeExpansionTwoEngine';
import {EXPANSION_ONE_IDS,isExpansionOneId,isExpansionRun,createExpansionRun,actExpansionRun,stepExpansionRun} from './arcadeExpansionOneEngine';
import type {ExpansionRun} from './arcadeExpansionOneEngine';
export {chooseEchoPad} from './arcadeExpansionOneEngine';
/**
 * Deterministic, dependency-free simulation for the Arcade Games.
 * Every game uses the same logical 960×540 coordinate space and fixed 60Hz steps.
 * Rendering is intentionally separate from input and persistence.
 */
export const ARCADE_WIDTH=960;
export const ARCADE_HEIGHT=540;
export const ARCADE_STEP=1/60;
export const PLAYABLE_ARCADE_IDS=[
 'midnight-wings','cave-escape','perfect-stack','blade-timing',
 'death-snake','neon-breaker','bullet-storm',
 'hexagon-hell','spike-rush','endless-ascent',
 ...EXPANSION_ONE_IDS,
 ...EXPANSION_TWO_IDS,
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
export type StackRun=Base&{kind:'perfect-stack';blocks:StackBlock[];movingX:number;direction:number;
 perfect:number;lastDropAt:number;lastDropX:number;lastDropWidth:number};
export type BladeRun=Base&{kind:'blade-timing';rotation:number;spin:number;blades:number[];stage:number;hits:number;needed:number};
export const SNAKE_COLS=30,SNAKE_ROWS=18;
export type ArcadeDirection='up'|'down'|'left'|'right';
export type ArcadeControls={left?:boolean;right?:boolean;up?:boolean;down?:boolean;pointerX?:number;pointerY?:number};
export type SnakeCell={x:number;y:number};
export type SnakeRun=Base&{kind:'death-snake';body:SnakeCell[];direction:ArcadeDirection;
 nextDirection:ArcadeDirection;turned:boolean;food:SnakeCell;walls:SnakeCell[];clock:number;speed:number};
export type BreakerBrick={x:number;y:number;alive:boolean};
export type BreakerRun=Base&{kind:'neon-breaker';ballX:number;ballY:number;vx:number;vy:number;
 paddleX:number;paddleWidth:number;serve:boolean;lives:number;wave:number;bricks:BreakerBrick[]};
export type StormBullet={x:number;y:number;vx:number;vy:number;r:number;style:number};
export type StormRun=Base&{kind:'bullet-storm';x:number;y:number;bullets:StormBullet[];
 spawn:number;wave:number};
export type HexWall={radius:number;opening:number;gap:number;thickness:number;passed:boolean};
export type HexRun=Base&{kind:'hexagon-hell';angle:number;rotation:number;walls:HexWall[];spawn:number;lastOpening:number};
export type SpikeObstacle={x:number;width:number;type:'spike'|'double'|'gap'|'saw'|'tower';passed:boolean;phase?:number};
export type SpikeRun=Base&{kind:'spike-rush';feet:number;vy:number;grounded:boolean;spawn:number;
 distance:number;obstacles:SpikeObstacle[]};
export type AscentPlatform={x:number;y:number;width:number;vx:number;kind?:'normal'|'spring'|'fragile';crumble?:number};
export type AscentRun=Base&{kind:'endless-ascent';x:number;feet:number;vy:number;
 climbed:number;platforms:AscentPlatform[];jumps:number};
export type ArcadeRunState=WingsRun|CaveRun|StackRun|BladeRun|SnakeRun|BreakerRun|StormRun|HexRun|SpikeRun|AscentRun|ExpansionRun|ExpansionTwoRun;

const clamp=(value:number,min:number,max:number)=>Math.max(min,Math.min(max,value));
const random=(state:Base)=>{
 state.seed=(Math.imul(state.seed,1664525)+1013904223)>>>0;
 return state.seed/4294967296;
};
const seedValue=(seed:number)=>((seed>>>0)||0x51c3a9e1);
const sameCell=(a:SnakeCell,b:SnakeCell)=>a.x===b.x&&a.y===b.y;
export const SNAKE_START:readonly SnakeCell[]=[{x:14,y:9},{x:13,y:9},{x:12,y:9}];
function freeSnakeCell(run:SnakeRun,forWall=false):SnakeCell|null{
 const taken=new Set([...run.body,...run.walls,...(forWall?[run.food]:[])].map(c=>c.y*SNAKE_COLS+c.x));
 const candidates:SnakeCell[]=[];
 for(let y=0;y<SNAKE_ROWS;y++)for(let x=0;x<SNAKE_COLS;x++){
  const cell={x,y};
  if(taken.has(y*SNAKE_COLS+x))continue;
  // Never surprise a player with a wall immediately next to the head.
  if(forWall&&Math.abs(x-run.body[0].x)+Math.abs(y-run.body[0].y)<5)continue;
  candidates.push(cell);
 }
 return candidates.length?candidates[Math.floor(random(run)*candidates.length)]:null;
}
function newBreakerBricks(wave:number):BreakerBrick[]{
 const rows=Math.min(7,4+wave);
 return Array.from({length:rows*10},(_,i)=>({x:94+(i%10)*78,y:78+Math.floor(i/10)*31,alive:true}));
}
export function steerSnake(run:SnakeRun,direction:ArcadeDirection):boolean{
 if(run.over||run.turned)return false;
 const opposite:Record<ArcadeDirection,ArcadeDirection>={
  left:'right',right:'left',up:'down',down:'up',
 };
 if(opposite[run.direction]===direction||run.direction===direction)return false;
 run.nextDirection=direction;run.turned=true;
 return true;
}
export function createArcadeRun(kind:PlayableArcadeId,seed=0x543fa1d2):ArcadeRunState{
 if(isExpansionOneId(kind))return createExpansionRun(kind,seed);
 if(isExpansionTwoId(kind))return createExpansionTwoRun(kind,seed);
 const base={score:0,elapsed:0,over:false,seed:seedValue(seed)};
 switch(kind){
  case 'midnight-wings':return {...base,kind,y:265,vy:0,spawn:1.4,
   pipes:[{x:1020,center:265,gap:208,passed:false}]};
  case 'cave-escape':return {...base,kind,y:270,vy:0,distance:0,
   segments:Array.from({length:26},(_,i)=>({x:i*42-42,center:270,gap:292}))};
  case 'perfect-stack':return {...base,kind,blocks:[{x:340,width:280}],
   movingX:0,direction:1,perfect:0,lastDropAt:-10,lastDropX:480,lastDropWidth:280};
  case 'blade-timing':return {...base,kind,rotation:0,spin:1.4,
   blades:[-Math.PI/2,0],stage:1,hits:0,needed:5};
  case 'death-snake':return {...base,kind,body:SNAKE_START.map(c=>({...c})),
   direction:'right',nextDirection:'right',turned:false,food:{x:21,y:9},
   walls:[],clock:0,speed:.16};
  case 'neon-breaker':return {...base,kind,ballX:480,ballY:460,vx:168,vy:-265,
   paddleX:480,paddleWidth:130,serve:true,lives:3,wave:1,bricks:newBreakerBricks(1)};
  case 'bullet-storm':return {...base,kind,x:480,y:422,bullets:[],spawn:.65,wave:1};
  case 'hexagon-hell':return {...base,kind,angle:0,rotation:0,spawn:1.6,
   lastOpening:0,walls:[{radius:465,opening:0,gap:1.65,thickness:34,passed:false}]};
  case 'spike-rush':return {...base,kind,feet:440,vy:0,grounded:true,
   spawn:1.43,distance:0,
   obstacles:[{x:1000,width:42,type:'spike',passed:false}]};
  case 'endless-ascent':return {...base,kind,x:480,feet:470,vy:-550,climbed:0,jumps:0,
   platforms:[
    {x:390,y:471,width:180,vx:0,kind:'normal'},
    {x:411,y:368,width:132,vx:78,kind:'normal'},
    {x:323,y:263,width:120,vx:-96,kind:'spring'},
    {x:475,y:156,width:116,vx:0,kind:'fragile'},
    {x:346,y:48,width:118,vx:108,kind:'normal'},
   ]};
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
export const BLADE_HIT_ARC=.21;
export function angularDistance(a:number,b:number){
 return Math.abs(Math.atan2(Math.sin(a-b),Math.cos(a-b)));
}
export function actArcadeRun(state:ArcadeRunState):ArcadeEvent{
 if(isExpansionRun(state))return actExpansionRun(state);
 if(isExpansionTwoRun(state))return actExpansionTwoRun(state);
 if(state.over)return null;
 switch(state.kind){
 case 'midnight-wings':
  state.vy=-335;
  return 'flap';
 case 'cave-escape':return null; // held input controls lift on fixed steps
 case 'death-snake':return null; // discrete turns are queued with steerSnake
 case 'bullet-storm':return null; // movement is directional or pointer controlled
 case 'hexagon-hell':return null; // orbit is controlled continuously
 case 'endless-ascent':return null; // jumping is automatic on platform contact
 case 'spike-rush':
  if(!state.grounded)return null;
  state.vy=-435;
  state.grounded=false;
  return 'flap';
 case 'neon-breaker':
  if(!state.serve)return null;
  state.serve=false;
  // Launch always heads upwards; all subsequent hits use paddle aim.
  return 'flap';
 case 'perfect-stack':{
  const last=state.blocks[state.blocks.length-1];
  const width=last.width;
  const exact=Math.abs(state.movingX-last.x)<=5;
  const left=exact?last.x:Math.max(last.x,state.movingX);
  const right=exact?last.x+width:Math.min(last.x+width,state.movingX+width);
  if(right-left<4)return fail(state);
  state.blocks.push({x:left,width:right-left});
  state.lastDropAt=state.elapsed;
  state.lastDropX=left+(right-left)/2;
  state.lastDropWidth=right-left;
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
   if(angularDistance(blade+state.rotation,injection)<BLADE_HIT_ARC)return fail(state);
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
export function stepArcadeRun(state:ArcadeRunState,dt:number,holding=false,controls:ArcadeControls={}):ArcadeEvent{
 if(isExpansionRun(state))return stepExpansionRun(state,dt,holding,controls);
 if(isExpansionTwoRun(state))return stepExpansionTwoRun(state,dt,holding,controls);
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
 case 'death-snake':{
  state.clock+=step;
  if(state.clock+1e-9<state.speed)return null;
  state.clock-=state.speed;
  state.direction=state.nextDirection;
  state.turned=false;
  const old=state.body[0];
  const delta:Record<ArcadeDirection,SnakeCell>={up:{x:0,y:-1},down:{x:0,y:1},
   left:{x:-1,y:0},right:{x:1,y:0}};
  const next={x:old.x+delta[state.direction].x,y:old.y+delta[state.direction].y};
  const eating=sameCell(next,state.food);
  if(next.x<0||next.x>=SNAKE_COLS||next.y<0||next.y>=SNAKE_ROWS||
   state.walls.some(c=>sameCell(c,next))||
   state.body.slice(0,eating?undefined:-1).some(c=>sameCell(c,next)))return fail(state);
  state.body.unshift(next);
  if(!eating){state.body.pop();return null;}
  state.score++;
  state.speed=Math.max(.068,.16-Math.floor(state.score/5)*.009);
  // Introduce only a small number of obstacles, well away from the snake's head.
  if(state.score%5===0&&state.walls.length<18){
   const wall=freeSnakeCell(state,true);
   if(wall)state.walls.push(wall);
  }
  const food=freeSnakeCell(state);
  if(food)state.food=food;else return fail(state);
  return 'score';
 }
 case 'neon-breaker':{
  const direction=(controls.right?1:0)-(controls.left?1:0);
  if(Number.isFinite(controls.pointerX)){
   const target=clamp(controls.pointerX!,state.paddleWidth/2+13,ARCADE_WIDTH-state.paddleWidth/2-13);
   const delta=clamp(target-state.paddleX,-640*step,640*step);
   state.paddleX+=delta;
  }else state.paddleX+=direction*570*step;
  state.paddleX=clamp(state.paddleX,state.paddleWidth/2+13,ARCADE_WIDTH-state.paddleWidth/2-13);
  if(state.serve){state.ballX=state.paddleX;state.ballY=464;return null;}
  const oldX=state.ballX,oldY=state.ballY;
  state.ballX+=state.vx*step;state.ballY+=state.vy*step;
  const radius=9;
  if(state.ballX<radius+10){state.ballX=radius+10;state.vx=Math.abs(state.vx);}
  if(state.ballX>ARCADE_WIDTH-radius-10){state.ballX=ARCADE_WIDTH-radius-10;state.vx=-Math.abs(state.vx);}
  if(state.ballY<radius+10){state.ballY=radius+10;state.vy=Math.abs(state.vy);}
  if(state.vy>0&&oldY+radius<=483&&state.ballY+radius>=483&&
   Math.abs(state.ballX-state.paddleX)<=state.paddleWidth/2+radius){
   state.ballY=483-radius;
   const angle=clamp((state.ballX-state.paddleX)/(state.paddleWidth/2),-1,1);
   const speed=Math.min(525,Math.hypot(state.vx,state.vy)*1.02);
   state.vx=speed*Math.sin(angle*1.07);
   state.vy=-speed*Math.cos(angle*1.07);
  }
  if(state.ballY>ARCADE_HEIGHT+radius){
   state.lives--;
   if(state.lives<=0)return fail(state);
   state.serve=true;
   state.ballX=state.paddleX;state.ballY=464;
   const speed=300+Math.min(180,state.wave*22);
   state.vx=(random(state)<.5?-1:1)*speed*.48;state.vy=-speed*.88;
   return 'stage';
  }
  for(const brick of state.bricks){
   if(!brick.alive)continue;
   const l=brick.x,r=brick.x+72,t=brick.y,b=brick.y+23;
   const nearX=clamp(state.ballX,l,r),nearY=clamp(state.ballY,t,b);
   if((state.ballX-nearX)**2+(state.ballY-nearY)**2>radius*radius)continue;
   brick.alive=false;
   const passedSide=oldX+radius<=l||oldX-radius>=r;
   if(passedSide){state.vx=-state.vx;state.ballX=oldX;}
   else{state.vy=-state.vy;state.ballY=oldY;}
   state.score+=10;
   if(state.bricks.every(x=>!x.alive)){
    state.wave++;
    state.score+=50;
    state.paddleWidth=Math.max(64,130-(state.wave-1)*10);
    state.bricks=newBreakerBricks(state.wave);
    state.serve=true;
    state.ballX=state.paddleX;state.ballY=464;
    const speed=Math.min(510,300+state.wave*22);
    state.vx=(random(state)<.5?-1:1)*speed*.5;state.vy=-speed*.866;
    return 'stage';
   }
   return 'score';
  }
  return null;
 }
 case 'bullet-storm':{
  const moveX=(controls.right?1:0)-(controls.left?1:0);
  const moveY=(controls.down?1:0)-(controls.up?1:0);
  const distance=Math.hypot(moveX,moveY);
  if(Number.isFinite(controls.pointerX)&&Number.isFinite(controls.pointerY)){
   const dx=controls.pointerX!-state.x,dy=controls.pointerY!-state.y;
   const d=Math.hypot(dx,dy);
   if(d>1){
    const stepMove=Math.min(d,360*step);
    state.x+=dx/d*stepMove;state.y+=dy/d*stepMove;
   }
  }else if(distance>0){
   state.x+=moveX/distance*345*step;state.y+=moveY/distance*345*step;
  }
  state.x=clamp(state.x,26,ARCADE_WIDTH-26);
  state.y=clamp(state.y,32,ARCADE_HEIGHT-26);
  state.wave=Math.max(1,1+Math.floor(state.elapsed/15));
  // Fair, seeded projectile patterns. All bullets enter from outside the arena.
  state.spawn-=step;
  if(state.spawn<=0){
   const count=state.wave>=3?3:state.wave===2?2:1;
   const speed=Math.min(320,152+state.elapsed*2.2);
   const center=80+random(state)*(ARCADE_WIDTH-160);
   for(let i=0;i<count;i++){
    const x=clamp(center+(i-(count-1)/2)*82,25,ARCADE_WIDTH-25);
    const drift=(random(state)-.5)*85;
    state.bullets.push({x,y:-22-i*16,vx:drift,vy:speed+i*11,r:10,style:(state.wave+i)%3});
   }
   state.spawn+=Math.max(.19,.68-state.elapsed*.006);
  }
  for(const bullet of state.bullets){
   bullet.x+=bullet.vx*step;bullet.y+=bullet.vy*step;
   if(Math.hypot(state.x-bullet.x,state.y-bullet.y)<bullet.r+11)return fail(state);
  }
  state.bullets=state.bullets.filter(b=>b.y<ARCADE_HEIGHT+30&&b.x>-35&&b.x<ARCADE_WIDTH+35);
  state.score=Math.floor(state.elapsed*10);
  return null;
 }
 case 'hexagon-hell':{
  const turn=(controls.right?1:0)-(controls.left?1:0);
  state.angle+=turn*3.65*step;
  state.rotation+=Math.sin(state.elapsed*.4)*.14*step;
  const velocity=Math.min(310,166+state.elapsed*2.3);
  state.spawn-=step;
  if(state.spawn<=0){
   // Keep consecutive openings reachable. All geometry is determined by the run's seed.
   state.lastOpening+=(random(state)-.5)*1.65;
   state.walls.push({radius:480,opening:state.lastOpening,
    gap:Math.max(.86,1.65-state.score*.065),thickness:34,passed:false});
   state.spawn+=Math.max(1.14,1.7-state.score*.025);
  }
  const playerRadius=126;
  for(const wall of state.walls){
   wall.radius-=velocity*step;
   if(Math.abs(wall.radius-playerRadius)<wall.thickness/2+11 &&
    angularDistance(state.angle,wall.opening+state.rotation)>wall.gap/2-.12)
    return fail(state);
   if(!wall.passed&&wall.radius+wall.thickness/2<playerRadius-12){
    wall.passed=true;
    state.score++;
    return 'score';
   }
  }
  state.walls=state.walls.filter(w=>w.radius+w.thickness/2>20);
  return null;
 }
 case 'spike-rush':{
  const speed=Math.min(390,285+state.score*4);
  const playerX=210,ground=440;
  state.distance+=speed*step;
  state.spawn-=step;
  if(state.spawn<=0){
   const choice=random(state);
   const kind:SpikeObstacle['type']=state.score<2?'spike':
    choice<.17?'gap':choice<.38?'double':choice<.59?'saw':
    choice<.76?'tower':'spike';
   const width=kind==='gap'?100:kind==='double'?78:kind==='saw'?46:kind==='tower'?43:42;
   state.obstacles.push({x:1010,width,type:kind,passed:false,phase:random(state)*Math.PI*2});
   state.spawn+=Math.max(1.1,1.55-state.score*.018);
  }
  for(const obstacle of state.obstacles)obstacle.x-=speed*step;
  const oldFeet=state.feet;
  const floorMissing=state.obstacles.some(o=>o.type==='gap'&&
   playerX>o.x+8&&playerX<o.x+o.width-8);
  if(state.grounded&&!floorMissing){
   state.feet=ground;
   state.vy=0;
  }else{
   state.grounded=false;
   state.vy+=1320*step;
   state.feet+=state.vy*step;
   if(!floorMissing&&state.vy>=0&&oldFeet<=ground&&state.feet>=ground){
    state.feet=ground;state.vy=0;state.grounded=true;
   }
  }
  for(const obstacle of state.obstacles){
   if(obstacle.type!=='gap' &&
    obstacle.x<playerX+14&&obstacle.x+obstacle.width>playerX-14){
    if(obstacle.type==='saw'){
     const sawY=ground-26+Math.sin(state.elapsed*5.5+(obstacle.phase??0))*7;
     if(Math.hypot(playerX-(obstacle.x+obstacle.width/2),state.feet-17-sawY)<30)
      return fail(state);
    }else if(state.feet>ground-(obstacle.type==='tower'?54:29))return fail(state);
   }
   if(!obstacle.passed&&obstacle.x+obstacle.width<playerX-15){
    obstacle.passed=true;
    state.score++;
   }
  }
  if(state.feet>ARCADE_HEIGHT+30)return fail(state);
  state.obstacles=state.obstacles.filter(o=>o.x+o.width>-20);
  return null;
 }
 case 'endless-ascent':{
  const direction=(controls.right?1:0)-(controls.left?1:0);
  state.x=clamp(state.x+direction*355*step,18,ARCADE_WIDTH-18);
  const oldFeet=state.feet;
  state.vy+=1110*step;
  state.feet+=state.vy*step;
  for(const platform of state.platforms){
   if(platform.crumble!==undefined){
    platform.crumble-=step;
    if(platform.crumble<=0)continue;
   }
   if(platform.vx!==0){
    platform.x+=platform.vx*step;
    if(platform.x<24){platform.x=24;platform.vx=Math.abs(platform.vx);}
    if(platform.x+platform.width>ARCADE_WIDTH-24){
     platform.x=ARCADE_WIDTH-24-platform.width;platform.vx=-Math.abs(platform.vx);
    }
   }
   if(platform.crumble!==undefined)continue;
   if(state.vy>0&&oldFeet<=platform.y+1&&state.feet>=platform.y&&
    state.x+13>platform.x&&state.x-13<platform.x+platform.width){
    state.feet=platform.y;
    state.vy=platform.kind==='spring'?-705:-555;
    if(platform.kind==='fragile')platform.crumble=.20;
    state.jumps++;
    break;
   }
  }
  const shift=Math.max(0,240-state.feet);
  if(shift>0){
   state.feet+=shift;
   for(const p of state.platforms)p.y+=shift;
   state.climbed+=shift;
   state.score=Math.floor(state.climbed/20);
  }
  // Every generated platform is reachable from the preceding one with one automatic bounce.
  let minY=Math.min(...state.platforms.map(p=>p.y));
  while(minY>-85){
   const top=state.platforms.reduce((a,b)=>a.y<b.y?a:b);
   const width=clamp(138-state.climbed/110,92,138);
   const center=clamp(top.x+top.width/2+(random(state)-.5)*260,95,ARCADE_WIDTH-95);
   const y=top.y-(90+random(state)*23);
   const kind:NonNullable<AscentPlatform['kind']>=state.climbed>220&&random(state)<.18?
    'spring':state.climbed>350&&random(state)<.22?'fragile':'normal';
   const moving=state.climbed>240&&random(state)<Math.min(.72,.32+state.climbed/6000);
   const vx=moving?(random(state)<.5?-1:1)*(90+random(state)*75):0;
   state.platforms.push({x:clamp(center-width/2,24,ARCADE_WIDTH-width-24),y,width,vx,kind});
   minY=y;
  }
  state.platforms=state.platforms.filter(p=>p.y<ARCADE_HEIGHT+80&&(p.crumble===undefined||p.crumble>0));
  if(state.feet>ARCADE_HEIGHT+42)return fail(state);
  return null;
 }
 }
}
