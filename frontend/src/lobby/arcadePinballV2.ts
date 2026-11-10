/**
 * Midnight Pinball — deterministic, self-contained mechanical table.
 * All collision geometry is shared with the painter. No animation controls physics.
 * Stable legacy run shape (score, balls, serve, flippers) is preserved for receipts.
 */
export type PinballControls={left?:boolean;right?:boolean};
export const PINBALL_BALL_RADIUS=10;
export const pinBumpers=[
 {x:380,y:216,r:34,points:120},
 {x:580,y:213,r:34,points:120},
 {x:480,y:306,r:29,points:180},
 {x:305,y:334,r:24,points:90},
 {x:652,y:331,r:24,points:90},
] as const;
export type PinballTrail={x:number;y:number};
export type PinballRun={
 kind:'midnight-pinball';score:number;elapsed:number;over:boolean;seed:number;
 x:number;y:number;vx:number;vy:number;balls:number;serve:boolean;
 combo:number;contact:number;flashes:number[];
 leftFlipper:boolean;rightFlipper:boolean;leftLift:number;rightLift:number;
 inShooter:boolean;trail:PinballTrail[];ballTime:number;
 hitTimer:number;hitX:number;hitY:number;hitValue:number;
 lanes:number;lastFlipper:'left'|'right'|null;
};
export type PinballEvent='flap'|'score'|'fail'|'stage'|null;
const clamp=(n:number,a:number,b:number)=>Math.max(a,Math.min(b,n));
const BALL=PINBALL_BALL_RADIUS;
export const pinballGeometry={
 leftWall:201,rightWall:761,top:63,shooterX:724,shooterGateY:109,
 drainY:523,flipperY:453,leftPivotX:391,rightPivotX:569,
};
export type Flipper={pivotX:number;pivotY:number;tipX:number;tipY:number};
export function pinballFlipper(side:'left'|'right',lift:number):Flipper{
 const raised=clamp(lift,0,1);
 return side==='left'
 ?{pivotX:391,pivotY:454,tipX:472,tipY:466-35*raised}
 :{pivotX:569,pivotY:454,tipX:488,tipY:466-35*raised};
}
export function createPinballRun(seed=0x51eab141):PinballRun{
 return {kind:'midnight-pinball',score:0,elapsed:0,over:false,
  seed:(seed>>>0)||0x51eab141,x:724,y:478,vx:0,vy:0,balls:3,serve:true,
  combo:0,contact:0,flashes:pinBumpers.map(()=>0),
  leftFlipper:false,rightFlipper:false,leftLift:0,rightLift:0,
  inShooter:true,trail:[],ballTime:0,hitTimer:0,hitX:480,hitY:245,
  hitValue:0,lanes:0,lastFlipper:null};
}
export function launchPinball(run:PinballRun):PinballEvent{
 if(run.over||!run.serve)return null;
 run.serve=false;run.inShooter=true;
 run.x=pinballGeometry.shooterX;run.y=478;
 run.vx=0;run.vy=-980;run.ballTime=0;run.trail=[];
 run.lastFlipper=null;
 return 'flap';
}
function loseBall(run:PinballRun):PinballEvent{
 run.balls--;
 if(run.balls<=0){run.over=true;return 'fail';}
 run.serve=true;run.inShooter=true;run.x=724;run.y=478;
 run.vx=0;run.vy=0;run.combo=0;run.contact=0;
 run.trail=[];run.ballTime=0;run.lastFlipper=null;
 return 'stage';
}
function bumper(run:PinballRun,x:number,y:number,r:number,points:number,index:number):PinballEvent{
 const dx=run.x-x,dy=run.y-y;
 const d=Math.hypot(dx,dy),minimum=r+BALL;
 if(d>=minimum)return null;
 // When spawned inside a bumper, choose an upwards normal (never NaN).
 const nx=d>.0001?dx/d:0,ny=d>.0001?dy/d:-1;
 run.x=x+nx*(minimum+.3);
 run.y=y+ny*(minimum+.3);
 const normal=run.vx*nx+run.vy*ny;
 // Strong radial bumper impulse, plus bounded tangential momentum.
 const speed=clamp(Math.hypot(run.vx,run.vy)+115,500,940);
 const tangential=(run.vx*(-ny)+run.vy*nx)*.21;
 run.vx=clamp(nx*speed-ny*tangential,-1000,1000);
 run.vy=clamp(ny*speed+nx*tangential,-1000,1030);
 if(normal>0)run.vy-=Math.min(70,normal*.07);
 if(run.contact>0)return null;
 run.score+=points*(1+Math.min(3,run.combo));
 run.hitValue=points*(1+Math.min(3,run.combo));
 run.combo=Math.min(7,run.combo+1);run.contact=.085;
 run.flashes[index]=.24;run.hitTimer=.65;run.hitX=x;run.hitY=y;
 return 'score';
}
function wallSegment(run:PinballRun,
 ax:number,ay:number,bx:number,by:number,bounce=.83):void{
 const ux=bx-ax,uy=by-ay,len=ux*ux+uy*uy;
 const p=clamp(((run.x-ax)*ux+(run.y-ay)*uy)/len,0,1);
 const x=ax+ux*p,y=ay+uy*p;
 const dx=run.x-x,dy=run.y-y,d=Math.hypot(dx,dy);
 if(d>=BALL+4)return;
 const nx=d>.01?dx/d:0,ny=d>.01?dy/d:-1;
 run.x=x+nx*(BALL+4+.15);run.y=y+ny*(BALL+4+.15);
 const v=run.vx*nx+run.vy*ny;
 if(v<0){
  run.vx-=nx*(1+bounce)*v;
  run.vy-=ny*(1+bounce)*v;
 }
}
function flipperCollision(run:PinballRun,side:'left'|'right',dt:number):PinballEvent{
 const lift=side==='left'?run.leftLift:run.rightLift;
 const pressed=side==='left'?run.leftFlipper:run.rightFlipper;
 const f=pinballFlipper(side,lift),dx=f.tipX-f.pivotX,dy=f.tipY-f.pivotY;
 const p=clamp(((run.x-f.pivotX)*dx+(run.y-f.pivotY)*dy)/(dx*dx+dy*dy),0,1);
 const x=f.pivotX+dx*p,y=f.pivotY+dy*p;
 const dist=Math.hypot(run.x-x,run.y-y);
 if(dist>BALL+10||run.y>y+13)return null;
 // A rising flipper delivers a mechanical upward impulse. Idle rubber returns
 // balls weakly; it never generates free energy like a powered kick.
 if(run.vy<-95&&!pressed)return null;
 run.y=y-(BALL+11);
 const outgoing=pressed?800+125*p:355+75*p;
 run.vy=-outgoing;
 const direction=side==='left'?1:-1;
 run.vx=clamp(run.vx*.35+direction*(pressed?115+235*p:75+100*p),-850,850);
 run.hitTimer=pressed?.14:0;run.hitX=x;run.hitY=y;run.hitValue=0;
 if(pressed&&run.lastFlipper!==side){
  run.lastFlipper=side;run.combo=Math.max(0,run.combo-1);
  return 'flap';
 }
 return null;
}
export function stepPinball(run:PinballRun,dt:number,controls:PinballControls={}):PinballEvent{
 if(run.over||!Number.isFinite(dt)||dt<=0)return null;
 const step=Math.min(1/60,dt);
 run.elapsed+=step;
 run.leftFlipper=!!controls.left;run.rightFlipper=!!controls.right;
 run.leftLift=clamp(run.leftLift+(run.leftFlipper?1:-1)*step*14,0,1);
 run.rightLift=clamp(run.rightLift+(run.rightFlipper?1:-1)*step*14,0,1);
 run.contact=Math.max(0,run.contact-step);
 run.hitTimer=Math.max(0,run.hitTimer-step);
 for(let i=0;i<run.flashes.length;i++)run.flashes[i]=Math.max(0,run.flashes[i]-step);
 if(run.serve)return null;
 run.ballTime+=step;
 let result:PinballEvent=null;
 // Five substeps plus continuous separation prevent high-velocity tunnelling.
 for(let i=0;i<5;i++){
  const t=step/5;
  if(!run.inShooter)run.vy=clamp(run.vy+735*t,-1100,1180);
  run.x+=run.vx*t;run.y+=run.vy*t;
  if(run.inShooter){
   run.x=pinballGeometry.shooterX;
   if(run.y<=pinballGeometry.shooterGateY){
    // One-way entrance curves the ball into the central playfield.
    run.inShooter=false;run.x=708;run.y=pinballGeometry.shooterGateY;
    run.vx=-370;run.vy=175;
    run.lanes++;run.score+=25;result='score';
   }
   continue;
  }
  if(run.x<pinballGeometry.leftWall+BALL){
   run.x=pinballGeometry.leftWall+BALL;run.vx=Math.abs(run.vx)*.84;
  }
  if(run.x>pinballGeometry.rightWall-BALL){
   run.x=pinballGeometry.rightWall-BALL;run.vx=-Math.abs(run.vx)*.84;
  }
  if(run.y<pinballGeometry.top+BALL){
   run.y=pinballGeometry.top+BALL;run.vy=Math.max(150,Math.abs(run.vy)*.8);
   run.vx+=run.x<480?100:-100;
  }
  // Real sling rails: balls fall toward the flipper pockets, not an imaginary
  // hard wall; if either flipper misses, the centre and outer drains stay open.
  wallSegment(run,215,390,330,450);
  wallSegment(run,746,390,630,450);
  wallSegment(run,225,130,264,85);
  wallSegment(run,736,130,696,85);
  for(let j=0;j<pinBumpers.length;j++){
   const b=pinBumpers[j],hit=bumper(run,b.x,b.y,b.r,b.points,j);
   if(hit==='score')result='score';
  }
  const left=flipperCollision(run,'left',t);
  const right=flipperCollision(run,'right',t);
  if(result===null&&(left||right))result=left||right;
  run.vx=clamp(run.vx,-1050,1050);run.vy=clamp(run.vy,-1100,1180);
  if(run.y>pinballGeometry.drainY)return loseBall(run);
 }
 if(!Number.isFinite(run.x)||!Number.isFinite(run.y)){
  run.over=true;return 'fail';
 }
 if(!run.serve){
  run.trail.unshift({x:run.x,y:run.y});
  if(run.trail.length>12)run.trail.length=12;
 }
 if(run.y<415)run.lastFlipper=null;
 return result;
}
