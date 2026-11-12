/**
 * Arcade Expansion II — five deterministic, self-contained 60 Hz games.
 * Simulation owns collisions, scoring and loss; canvas rendering is presentation only.
 */
export const EXPANSION_TWO_IDS=[
 'prism-lock','meteor-miner','circuit-rush','shadow-heist','pulse-perfect',
] as const;
export type ExpansionTwoId=typeof EXPANSION_TWO_IDS[number];
export const isExpansionTwoId=(id:string):id is ExpansionTwoId=>
 (EXPANSION_TWO_IDS as readonly string[]).includes(id);
export type ExpansionTwoEvent='flap'|'score'|'fail'|'stage'|null;
export type ExpansionTwoControls={left?:boolean;right?:boolean;up?:boolean;down?:boolean};
type Base={score:number;elapsed:number;over:boolean;seed:number};
const STEP=1/60,W=960,H=540;
const clamp=(value:number,low:number,high:number)=>Math.max(low,Math.min(high,value));
const random=(run:Base)=>{
 run.seed=(Math.imul(run.seed,1664525)+1013904223)>>>0;
 return run.seed/4294967296;
};
type Point={c:number;r:number};
export type PrismMirror=Point&{slash:boolean;required:boolean};
export type PrismBeam={points:Point[];hit:boolean};
export type PrismRun=Base&{kind:'prism-lock';mirrors:PrismMirror[];
 stage:number;lives:number;timeLeft:number;targetRow:number;cursor:Point;
 beam:PrismBeam;flash:number;transition:'clear'|'lost'|null;transitionClock:number};
export const PRISM_COLS=7,PRISM_ROWS=5;
const same=(a:Point,b:Point)=>a.c===b.c&&a.r===b.r;
const mirrorReflect=(dc:number,dr:number,slash:boolean):[number,number]=>
 slash?[-dr,-dc]:[dr,dc];
export function tracePrism(mirrors:readonly PrismMirror[],targetRow:number):PrismBeam{
 const points:Point[]=[{c:-1,r:2}],seen=new Set<string>();
 let c=-1,r=2,dc=1,dr=0;
 for(let step=0;step<PRISM_COLS*PRISM_ROWS*4;step++){
  c+=dc;r+=dr;points.push({c,r});
  if(c===PRISM_COLS&&r===targetRow)return {points,hit:true};
  if(c<0||c>=PRISM_COLS||r<0||r>=PRISM_ROWS)return {points,hit:false};
  const key=c+':'+r+':'+dc+':'+dr;
  if(seen.has(key))return {points,hit:false};
  seen.add(key);
  const mirror=mirrors.find(m=>m.c===c&&m.r===r);
  if(mirror)[dc,dr]=mirrorReflect(dc,dr,mirror.slash);
 }
 return {points,hit:false};
}
function makePrism(run:PrismRun):void{
 const first=[0,1,3,4][Math.floor(random(run)*4)];
 const second=[0,1,2,3,4].filter(r=>r!==first)[Math.floor(random(run)*4)];
 const required:PrismMirror[]=[
  {c:1,r:2,slash:first<2,required:true},
  {c:1,r:first,slash:first<2,required:true},
  {c:4,r:first,slash:second<first,required:true},
  {c:4,r:second,slash:second<first,required:true},
 ];
 const occupied=new Set<string>();
 const mark=(c:number,r:number)=>occupied.add(c+':'+r);
 for(let c=0;c<=1;c++)mark(c,2);
 for(let r=Math.min(2,first);r<=Math.max(2,first);r++)mark(1,r);
 for(let c=1;c<=4;c++)mark(c,first);
 for(let r=Math.min(first,second);r<=Math.max(first,second);r++)mark(4,r);
 for(let c=4;c<PRISM_COLS;c++)mark(c,second);
 const mirrors=required.map(m=>({...m,slash:random(run)<.5?m.slash:!m.slash}));
 // Force at least one genuine adjustment, even if seeded guesses happen to match.
 mirrors[0].slash=!required[0].slash;
 for(let i=0;i<Math.min(7,3+run.stage);i++){
  const available:Point[]=[];
  for(let r=0;r<PRISM_ROWS;r++)for(let c=0;c<PRISM_COLS;c++)
   if(!occupied.has(c+':'+r)&&!mirrors.some(m=>m.c===c&&m.r===r))
    available.push({c,r});
  if(!available.length)break;
  const p=available[Math.floor(random(run)*available.length)];
  mirrors.push({...p,slash:random(run)<.5,required:false});
 }
 run.mirrors=mirrors;run.targetRow=second;run.cursor={c:1,r:2};
 run.beam=tracePrism(mirrors,second);run.timeLeft=Math.max(19,42-run.stage*1.8);
 run.flash=0;run.transition=null;run.transitionClock=0;
}
export function selectPrism(run:PrismRun,c:number,r:number):ExpansionTwoEvent{
 if(run.over||run.transition||!Number.isInteger(c)||!Number.isInteger(r)||c<0||c>=PRISM_COLS||
  r<0||r>=PRISM_ROWS)return null;
 run.cursor={c,r};
 const mirror=run.mirrors.find(m=>m.c===c&&m.r===r);
 if(!mirror)return null;
 mirror.slash=!mirror.slash;
 run.beam=tracePrism(run.mirrors,run.targetRow);run.flash=.25;
 if(!run.beam.hit)return 'flap';
 run.score+=250+run.stage*40+Math.floor(run.timeLeft*5);
 run.stage++;run.transition='clear';run.transitionClock=2.15;return 'score';
}
export type MinerRock={x:number;y:number;r:number;ore:number;grade:number;flash:number};
export type MinerDrone={x:number;y:number;vx:number;vy:number;r:number};
export type MinerRun=Base&{kind:'meteor-miner';x:number;y:number;vx:number;vy:number;
 rocks:MinerRock[];oxygen:number;hull:number;invincible:number;cargo:number;
 cargoValue:number;capacity:number;mineIndex:number;mineProgress:number;
 atBase:boolean;deposits:number;mining:boolean;drones:MinerDrone[]};
function minerRock(run:MinerRun):MinerRock{
 for(let attempts=0;attempts<100;attempts++){
  const x=245+random(run)*630,y=75+random(run)*378;
  if(run.rocks.every(a=>Math.hypot(a.x-x,a.y-y)>a.r+57)&&Math.hypot(x-116,y-438)>140)
   return {x,y,r:20+random(run)*17,ore:2+Math.floor(random(run)*3),
    grade:random(run)<.23?2:1,flash:0};
 }
 return {x:290+random(run)*500,y:90+random(run)*320,r:23,ore:3,grade:1,flash:0};
}
export type CircuitTile=Point&{mask:number;path:boolean;danger:boolean};
export type CircuitRun=Base&{kind:'circuit-rush';tiles:CircuitTile[];powered:Point[];
 stage:number;lives:number;timeLeft:number;cursor:Point;flash:number;solved:boolean;shockClock:number;
 turnCell:Point|null;turnClock:number;turnFromMask:number};
export const CIRCUIT_COLS=6,CIRCUIT_ROWS=5;
export const DIRS=[{dc:0,dr:-1,bit:1,opposite:4},
 {dc:1,dr:0,bit:2,opposite:8},{dc:0,dr:1,bit:4,opposite:1},
 {dc:-1,dr:0,bit:8,opposite:2}] as const;
export const rotatePorts=(mask:number)=>((mask<<1)&15)|((mask&8)>>3);
export function poweredCircuit(tiles:readonly CircuitTile[]):Point[]{
 const get=(c:number,r:number)=>tiles.find(t=>t.c===c&&t.r===r);
 const start=get(0,2);
 if(!start||(start.mask&8)===0)return [];
 const queue:Point[]=[{c:0,r:2}],seen=new Set<string>(['0:2']);
 for(let i=0;i<queue.length;i++){
  const p=queue[i],tile=get(p.c,p.r)!;
  for(const dir of DIRS){
   if(!(tile.mask&dir.bit))continue;
   const c=p.c+dir.dc,r=p.r+dir.dr,neighbor=get(c,r),key=c+':'+r;
   if(!neighbor||(neighbor.mask&dir.opposite)===0||seen.has(key))continue;
   seen.add(key);queue.push({c,r});
  }
 }
 return queue;
}
export function circuitSolved(run:CircuitRun):boolean{
 const sink=run.tiles.find(t=>t.c===5&&t.r===2);
 return !!sink&&(sink.mask&2)!==0&&run.powered.some(p=>same(p,{c:5,r:2}));
}
function makeCircuit(run:CircuitRun):void{
 const upper=random(run)<.5?1:3;
 const path:Point[]=[{c:0,r:2},{c:1,r:2},{c:2,r:2},{c:2,r:upper},
  {c:3,r:upper},{c:4,r:upper},{c:4,r:2},{c:5,r:2}];
 const tiles:CircuitTile[]=[];
 const direction=(a:Point,b:Point)=>{
  if(b.c>a.c)return 2;if(b.c<a.c)return 8;
  if(b.r>a.r)return 4;return 1;
 };
 for(let r=0;r<CIRCUIT_ROWS;r++)for(let c=0;c<CIRCUIT_COLS;c++){
  const index=path.findIndex(p=>p.c===c&&p.r===r);
  let mask:number;
  if(index<0)mask=random(run)<.28?5:3;
  else{
   const p=path[index],before=index===0?{c:-1,r:2}:path[index-1];
   const after=index===path.length-1?{c:6,r:2}:path[index+1];
   mask=direction(p,before)|direction(p,after);
  }
  const rotations=Math.floor(random(run)*4);
  for(let k=0;k<rotations;k++)mask=rotatePorts(mask);
  tiles.push({c,r,mask,path:index>=0,danger:false});
 }
 run.tiles=tiles;run.cursor={c:2,r:2};
 run.powered=poweredCircuit(tiles);
 if(circuitSolved(run)){
  const turn=run.tiles.find(t=>t.c===2&&t.r===2)!;
  turn.mask=rotatePorts(turn.mask);
  run.powered=poweredCircuit(tiles);
 }
 // Red short-circuit tiles are never part of the guaranteed solution path,
 // and never powered on spawn. Later circuits have more forbidden contacts.
 const safe=tiles.filter(t=>!t.path&&!run.powered.some(p=>same(p,t)));
 for(let i=0;i<Math.min(5,1+Math.floor((run.stage-1)/2))&&safe.length;i++){
  const index=Math.floor(random(run)*safe.length);
  safe.splice(index,1)[0].danger=true;
 }
 run.solved=false;run.flash=0;run.shockClock=0;
 run.turnCell=null;run.turnClock=0;run.turnFromMask=0;
 run.timeLeft=Math.max(21,46-run.stage*1.9);
}
export function rotateCircuit(run:CircuitRun,c:number,r:number):ExpansionTwoEvent{
 if(run.over||run.shockClock>0||!Number.isInteger(c)||!Number.isInteger(r)||c<0||c>=CIRCUIT_COLS||
  r<0||r>=CIRCUIT_ROWS)return null;
 run.cursor={c,r};const tile=run.tiles.find(t=>t.c===c&&t.r===r);
 if(!tile)return null;
 run.turnFromMask=tile.mask;tile.mask=rotatePorts(tile.mask);
 run.turnCell={c,r};run.turnClock=.32;
 run.powered=poweredCircuit(run.tiles);run.flash=.2;
 if(run.tiles.some(t=>t.danger&&run.powered.some(p=>same(p,t)))){
  run.lives--;run.shockClock=1.3;
  if(run.lives<=0){run.over=true;return 'fail';}
  return 'stage';
 }
 if(!circuitSolved(run))return 'flap';
 run.score+=300+run.stage*50+Math.floor(run.timeLeft*5);
 run.stage++;makeCircuit(run);return 'score';
}
export type Guard={x:number;y:number;min:number;max:number;direction:1|-1;speed:number;axis?:'x'|'y'};
export type Loot={x:number;y:number;taken:boolean};
export type Wall={x:number;y:number;w:number;h:number};
export const HEIST_WALLS:readonly Wall[]=[
 {x:275,y:93,w:108,h:42},{x:430,y:267,w:132,h:50},
 {x:680,y:270,w:102,h:45},{x:159,y:299,w:78,h:48},
];
export const HEIST_STAGE_WALLS:readonly (readonly Wall[])[]=[
 HEIST_WALLS,
 [{x:255,y:125,w:102,h:44},{x:460,y:180,w:65,h:141},
  {x:682,y:307,w:106,h:47},{x:175,y:326,w:74,h:50}],
 [{x:208,y:191,w:75,h:144},{x:387,y:83,w:117,h:48},
  {x:589,y:249,w:133,h:55},{x:731,y:101,w:70,h:61}],
 [{x:310,y:92,w:90,h:75},{x:465,y:287,w:85,h:121},
  {x:640,y:105,w:115,h:48},{x:184,y:342,w:78,h:43}],
];
const HEIST_STAGE_LOOT:readonly (readonly {x:number;y:number}[])[]=[
 [{x:245,y:166},{x:445,y:166},{x:650,y:171},{x:773,y:390}],
 [{x:235,y:213},{x:414,y:114},{x:645,y:192},{x:814,y:385},{x:412,y:403}],
 [{x:305,y:121},{x:475,y:196},{x:663,y:405},{x:815,y:193},{x:365,y:397}],
 [{x:228,y:191},{x:431,y:215},{x:601,y:404},{x:816,y:263},{x:300,y:404}],
];
export type HeistRun=Base&{kind:'shadow-heist';x:number;y:number;
 guards:Guard[];loot:Loot[];walls:readonly Wall[];alert:number;
 lives:number;invincible:number;stage:number;timeLeft:number;lastCaught:number};
function makeHeist(run:HeistRun):void{
 run.x=94;run.y=438;run.alert=0;run.invincible=.8;
 const variant=(run.stage-1)%HEIST_STAGE_WALLS.length;
 run.walls=HEIST_STAGE_WALLS[variant];
 run.timeLeft=Math.max(49,116-run.stage*5);
 run.loot=HEIST_STAGE_LOOT[variant].map(p=>({...p,taken:false}));
 const speedUp=Math.min(125,run.stage*12);
 run.guards=[
  {x:195,y:245,min:142,max:410,direction:1,speed:83+speedUp,axis:'x'},
  {x:623,y:218,min:533,max:828,direction:-1,speed:105+speedUp,axis:'x'},
  {x:515,y:415,min:364,max:747,direction:1,speed:83+speedUp,axis:'x'},
 ];
 if(run.stage>=2)run.guards.push({
  x:414,y:193,min:104,max:429,direction:1,speed:95+speedUp,axis:'y'});
 if(run.stage>=4)run.guards.push({
  x:738,y:183,min:99,max:440,direction:-1,speed:106+speedUp,axis:'y'});
 if(run.stage>=7)run.guards.push({
  x:293,y:381,min:200,max:715,direction:1,speed:110+speedUp,axis:'x'});
}
function blockSight(x1:number,y1:number,x2:number,y2:number,walls:readonly Wall[]):boolean{
 for(const wall of walls)for(let k=1;k<16;k++){
  const t=k/16,x=x1+(x2-x1)*t,y=y1+(y2-y1)*t;
  if(x>wall.x&&x<wall.x+wall.w&&y>wall.y&&y<wall.y+wall.h)return true;
 }
 return false;
}
export function guardSees(run:HeistRun,guard:Guard):boolean{
 const dx=run.x-guard.x,dy=run.y-guard.y,distance=Math.hypot(dx,dy);
 if(distance<22)return true;
 const forward=(guard.axis==='y'?dy:dx)*guard.direction;
 const lateral=guard.axis==='y'?dx:dy;
 return distance<205+run.stage*9&&forward>0&&
  Math.abs(lateral)<forward*.72&&
  !blockSight(guard.x,guard.y,run.x,run.y,run.walls);
}
export type PulseRun=Base&{kind:'pulse-perfect';radius:number;target:number;
 speed:number;combo:number;bestCombo:number;lives:number;round:number;
 feedback:'perfect'|'good'|'miss'|null;feedbackClock:number;lastPoints:number};
function nextPulse(run:PulseRun):void{
 run.round++;run.radius=223;run.target=68+Math.floor(random(run)*48);
 run.speed=Math.min(310,125+run.round*9);
}
export function pulseStrike(run:PulseRun):ExpansionTwoEvent{
 if(run.over||run.feedbackClock>0)return null;
 const accuracy=Math.abs(run.radius-run.target);
 if(accuracy<=15){
  const perfect=accuracy<=5;
  run.combo++;run.bestCombo=Math.max(run.bestCombo,run.combo);
  run.lastPoints=(perfect?100:55)*(1+Math.min(7,Math.floor((run.combo-1)/4)));
  run.score+=run.lastPoints;run.feedback=perfect?'perfect':'good';
  run.feedbackClock=.53;return 'score';
 }
 run.lives--;run.combo=0;run.lastPoints=0;run.feedback='miss';run.feedbackClock=.53;
 return run.lives<=0?(run.over=true,'fail'):'stage';
}
export type ExpansionTwoRun=PrismRun|MinerRun|CircuitRun|HeistRun|PulseRun;
export const isExpansionTwoRun=(state:{kind:string}):state is ExpansionTwoRun=>
 isExpansionTwoId(state.kind);
export function createExpansionTwoRun(kind:ExpansionTwoId,seed=0x297b51cd):ExpansionTwoRun{
 const base={score:0,elapsed:0,over:false,seed:(seed>>>0)||0x297b51cd};
 switch(kind){
 case 'prism-lock':{
  const run:PrismRun={...base,kind,mirrors:[],targetRow:2,stage:1,lives:3,
   timeLeft:40,cursor:{c:1,r:2},beam:{points:[],hit:false},flash:0,transition:null,transitionClock:0};
  makePrism(run);return run;
 }
 case 'meteor-miner':{
  const run:MinerRun={...base,kind,x:125,y:422,vx:0,vy:0,rocks:[],
   oxygen:79,hull:3,invincible:0,cargo:0,cargoValue:0,capacity:6,
   mineIndex:-1,mineProgress:0,atBase:true,deposits:0,mining:false,drones:[]};
  for(let i=0;i<9;i++)run.rocks.push(minerRock(run));
  return run;
 }
 case 'circuit-rush':{
  const run:CircuitRun={...base,kind,tiles:[],powered:[],stage:1,lives:3,
   timeLeft:40,cursor:{c:2,r:2},flash:0,solved:false,shockClock:0,
   turnCell:null,turnClock:0,turnFromMask:0};
  makeCircuit(run);return run;
 }
 case 'shadow-heist':{
  const run:HeistRun={...base,kind,x:94,y:438,guards:[],loot:[],
   walls:HEIST_WALLS,alert:0,lives:3,invincible:1,stage:1,timeLeft:126,lastCaught:0};
  makeHeist(run);return run;
 }
 case 'pulse-perfect':return {...base,kind,radius:223,target:94,speed:140,
  combo:0,bestCombo:0,lives:3,round:1,feedback:null,feedbackClock:0,lastPoints:0};
 }
}
export function actExpansionTwoRun(run:ExpansionTwoRun):ExpansionTwoEvent{
 if(run.over)return null;
 switch(run.kind){
 case 'prism-lock':return selectPrism(run,run.cursor.c,run.cursor.r);
 case 'circuit-rush':return rotateCircuit(run,run.cursor.c,run.cursor.r);
 case 'pulse-perfect':return pulseStrike(run);
 case 'meteor-miner':case 'shadow-heist':return null;
 }
}
export function movePuzzleCursor(run:PrismRun|CircuitRun,dc:number,dr:number):void{
 const cols=run.kind==='prism-lock'?PRISM_COLS:CIRCUIT_COLS;
 const rows=run.kind==='prism-lock'?PRISM_ROWS:CIRCUIT_ROWS;
 run.cursor={c:clamp(run.cursor.c+dc,0,cols-1),r:clamp(run.cursor.r+dr,0,rows-1)};
}
function losePuzzleLife(run:PrismRun|CircuitRun):ExpansionTwoEvent{
 run.lives--;
 if(run.lives<=0){run.over=true;return 'fail';}
 if(run.kind==='prism-lock'){
  run.transition='lost';run.transitionClock=1.85;run.timeLeft=0;
 }else makeCircuit(run);
 return 'stage';
}
function prismStep(run:PrismRun,dt:number):ExpansionTwoEvent{
 if(run.transition){
  run.transitionClock=Math.max(0,run.transitionClock-dt);
  if(run.transitionClock<=0){makePrism(run);return 'stage';}
  return null;
 }
 run.timeLeft-=dt;run.flash=Math.max(0,run.flash-dt);
 if(run.timeLeft<=0)return losePuzzleLife(run);
 return null;
}
function minerStep(run:MinerRun,dt:number,holding:boolean,controls:ExpansionTwoControls):ExpansionTwoEvent{
 const dx=Number(!!controls.right)-Number(!!controls.left);
 const dy=Number(!!controls.down)-Number(!!controls.up);
 const length=Math.hypot(dx,dy)||1;
 const acceleration=620;
 run.vx=clamp((run.vx+dx/length*acceleration*dt)*Math.pow(.94,dt*60),-240,240);
 run.vy=clamp((run.vy+dy/length*acceleration*dt)*Math.pow(.94,dt*60),-240,240);
 run.x=clamp(run.x+run.vx*dt,24,W-24);
 run.y=clamp(run.y+run.vy*dt,40,H-30);
 run.invincible=Math.max(0,run.invincible-dt);
 run.oxygen=Math.max(0,run.oxygen-dt*(1.32+run.deposits*.26));
 if(run.oxygen<=0){run.over=true;return 'fail';}
 for(const rock of run.rocks){
  rock.flash=Math.max(0,rock.flash-dt);
  const dist=Math.hypot(run.x-rock.x,run.y-rock.y);
  if(dist<rock.r+15){
   if(run.invincible===0&&Math.hypot(run.vx,run.vy)>85){
    run.hull--;run.invincible=1.25;
    if(run.hull<=0){run.over=true;return 'fail';}
   }
   const push=Math.max(.1,dist),nx=(run.x-rock.x)/push,ny=(run.y-rock.y)/push;
   run.x=clamp(rock.x+nx*(rock.r+15),24,W-24);
   run.y=clamp(rock.y+ny*(rock.r+15),40,H-30);
   run.vx*=.35;run.vy*=.35;
  }
 }
 run.atBase=Math.hypot(run.x-115,run.y-435)<90;
 if(run.atBase){
  run.oxygen=Math.min(80,run.oxygen+dt*14);
  if(run.cargo>0){
   run.score+=run.cargoValue*80+run.cargo*20;
   run.cargo=0;run.cargoValue=0;run.deposits++;
   run.capacity=Math.min(8,6+Math.floor(run.deposits/4));
   // Security drones appear after the first successful delivery. Unlike
   // cosmetic asteroids they pursue the player and have true collision.
   const targetCount=Math.min(4,run.deposits);
   while(run.drones.length<targetCount){
    const n=run.drones.length;
    run.drones.push({x:755-(n%2)*145,y:112+n*82,vx:0,vy:0,r:13+n%2*3});
   }
   return 'score';
  }
 }
 // Drones chase outside the protected base ring; an occupied drone has to
 // be dodged, and cannot repeatedly damage the ship during invulnerability.
 for(const drone of run.drones){
  const dx=run.x-drone.x,dy=run.y-drone.y,d=Math.max(1,Math.hypot(dx,dy));
  const speed=55+Math.min(70,run.deposits*9);
  drone.vx+=(dx/d*speed-drone.vx)*dt*1.8;
  drone.vy+=(dy/d*speed-drone.vy)*dt*1.8;
  drone.x=clamp(drone.x+drone.vx*dt,230,W-35);
  drone.y=clamp(drone.y+drone.vy*dt,52,H-35);
  if(!run.atBase&&run.invincible<=0&&d<drone.r+16){
   run.hull--;run.invincible=1.55;run.vx+=(dx/d)*165;run.vy+=(dy/d)*165;
   if(run.hull<=0){run.over=true;return 'fail';}
  }
 }
 run.mining=holding&&!run.atBase&&run.cargo<run.capacity;
 const closest=run.mining?run.rocks
  .map((rock,i)=>({i,dist:Math.hypot(run.x-rock.x,run.y-rock.y)}))
  .filter(entry=>entry.dist<112).sort((a,b)=>a.dist-b.dist)[0]:undefined;
 if(!closest){run.mineIndex=-1;run.mineProgress=0;return null;}
 if(run.mineIndex!==closest.i){run.mineIndex=closest.i;run.mineProgress=0;}
 run.mineProgress+=dt;
 const rock=run.rocks[closest.i];
 if(run.mineProgress<(.53+rock.r/110))return null;
 run.mineProgress=0;rock.ore--;rock.flash=.26;
 run.cargo++;run.cargoValue+=rock.grade;
 if(rock.ore<=0){
  run.rocks.splice(closest.i,1);
  run.mineIndex=-1;
  run.rocks.push(minerRock(run));
 }
 return 'flap';
}
function circuitStep(run:CircuitRun,dt:number):ExpansionTwoEvent{
 run.turnClock=Math.max(0,run.turnClock-dt);
 if(run.turnClock===0)run.turnCell=null;
 if(run.shockClock>0){
  run.shockClock=Math.max(0,run.shockClock-dt);
  if(run.shockClock===0)makeCircuit(run);
  return null;
 }
 run.timeLeft-=dt;run.flash=Math.max(0,run.flash-dt);
 if(run.timeLeft<=0)return losePuzzleLife(run);
 return null;
}
function heistStep(run:HeistRun,dt:number,controls:ExpansionTwoControls):ExpansionTwoEvent{
 run.timeLeft-=dt;run.invincible=Math.max(0,run.invincible-dt);
 run.lastCaught=Math.max(0,run.lastCaught-dt);
 if(run.timeLeft<=0){run.over=true;return 'fail';}
 const dx=Number(!!controls.right)-Number(!!controls.left);
 const dy=Number(!!controls.down)-Number(!!controls.up);
 const d=Math.hypot(dx,dy)||1,move=220*dt;
 const collision=(x:number,y:number)=>run.walls.some(w=>
  x+13>w.x&&x-13<w.x+w.w&&y+13>w.y&&y-13<w.y+w.h);
 const nx=clamp(run.x+dx/d*move,48,913);
 if(!collision(nx,run.y))run.x=nx;
 const ny=clamp(run.y+dy/d*move,66,474);
 if(!collision(run.x,ny))run.y=ny;
 for(const g of run.guards){
  if(g.axis==='y'){
   g.y+=g.direction*g.speed*dt;
   if(g.y<g.min){g.y=g.min;g.direction=1;}
   if(g.y>g.max){g.y=g.max;g.direction=-1;}
  }else{
   g.x+=g.direction*g.speed*dt;
   if(g.x<g.min){g.x=g.min;g.direction=1;}
   if(g.x>g.max){g.x=g.max;g.direction=-1;}
  }
 }
 if(run.invincible===0&&run.guards.some(g=>guardSees(run,g)))
  run.alert=Math.min(1,run.alert+dt*(1.13+run.stage*.18));
 else run.alert=Math.max(0,run.alert-dt*.7);
 if(run.alert>=1){
  run.lives--;run.alert=0;run.lastCaught=1;
  if(run.lives<=0){run.over=true;return 'fail';}
  run.x=94;run.y=438;run.invincible=1.2;
  return 'stage';
 }
 for(const gem of run.loot){
  if(!gem.taken&&Math.hypot(run.x-gem.x,run.y-gem.y)<22){
   gem.taken=true;run.score+=85+run.stage*15;return 'score';
  }
 }
 if(run.loot.every(g=>g.taken)&&Math.hypot(run.x-885,run.y-452)<46){
  run.score+=300+run.stage*70+Math.floor(run.timeLeft*4);
  run.stage++;makeHeist(run);return 'score';
 }
 return null;
}
function pulseStep(run:PulseRun,dt:number):ExpansionTwoEvent{
 if(run.feedbackClock>0){
  run.feedbackClock-=dt;
  if(run.feedbackClock<=0){
   if(!run.over){nextPulse(run);run.feedback=null;}
   run.feedbackClock=0;
  }
  return null;
 }
 run.radius-=run.speed*dt;
 if(run.radius<run.target-17)return pulseStrike(run);
 return null;
}
export function stepExpansionTwoRun(run:ExpansionTwoRun,dt:number,holding=false,
 controls:ExpansionTwoControls={}):ExpansionTwoEvent{
 if(run.over||!Number.isFinite(dt)||dt<=0)return null;
 const step=Math.min(dt,STEP);
 run.elapsed+=step;
 switch(run.kind){
 case 'prism-lock':return prismStep(run,step);
 case 'meteor-miner':return minerStep(run,step,holding,controls);
 case 'circuit-rush':return circuitStep(run,step);
 case 'shadow-heist':return heistStep(run,step,controls);
 case 'pulse-perfect':return pulseStep(run,step);
 }
}
