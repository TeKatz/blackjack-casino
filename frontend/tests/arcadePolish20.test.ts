import {describe,expect,it} from 'vitest';
import {ARCADE_STEP,actArcadeRun,createArcadeRun,stepArcadeRun,PLAYABLE_ARCADE_IDS} from '../src/lobby/arcadeEngine';
import {pinballGeometry,pinballFlipper,launchPinball,createPinballRun,
 stepPinball} from '../src/lobby/arcadePinballV2';
import {driftRoadHalfWidth,driftTrafficAt,gravityHazards,gravitySentries}
 from '../src/lobby/arcadeExpansionOneEngine';
import type {DriftRun,GravityRun} from '../src/lobby/arcadeExpansionOneEngine';
import {createExpansionTwoRun,rotateCircuit,poweredCircuit,selectPrism,tracePrism,
 HEIST_STAGE_WALLS} from '../src/lobby/arcadeExpansionTwoEngine';
import type {PrismRun,CircuitRun,MinerRun,HeistRun} from '../src/lobby/arcadeExpansionTwoEngine';
import {ARCADE_VOICES,ECHO_PAD_NOTES,soundNotes,soundProfileSignature,
 playArcadeSound} from '../src/lobby/arcadeSound';

describe('Midnight Pinball physical rebuild',()=>{
 it('launches from a proper channel into the playable table rather than falling outside',()=>{
  const run=createPinballRun(123);
  expect(run.serve).toBe(true);
  expect(run.x).toBe(pinballGeometry.shooterX);
  expect(launchPinball(run)).toBe('flap');
  let entered=false;
  for(let n=0;n<70&&!run.over;n++){
   stepPinball(run,ARCADE_STEP);
   if(!run.inShooter){entered=true;break;}
  }
  expect(entered).toBe(true);
  expect(run.serve).toBe(false);
  expect(run.x).toBeLessThan(pinballGeometry.shooterX);
  expect(run.vx).toBeLessThan(0);
  expect(run.y).toBeLessThan(160);
 });
 it('kicks a descending ball with a raised left or right flipper',()=>{
  for(const side of ['left','right'] as const){
   const run=createPinballRun(7);
   launchPinball(run);
   run.inShooter=false;
   run.x=side==='left'?426:534;run.y=439;run.vx=0;run.vy=620;
   for(let i=0;i<4;i++)stepPinball(run,ARCADE_STEP,{[side]:true});
   expect(run.vy).toBeLessThan(-200);
   expect(side==='left'?run.leftLift:run.rightLift).toBeGreaterThan(0);
  }
 });
 it('shares exact flipper coordinates with the renderer and preserves a centre drain',()=>{
  const left=pinballFlipper('left',1),right=pinballFlipper('right',1);
  expect(left.pivotY).toBe(right.pivotY);
  expect(left.tipY).toBe(right.tipY);
  expect(left.tipX).toBeLessThan(right.tipX);
  expect(left.tipY).toBeLessThan(pinballFlipper('left',0).tipY);
  const run=createPinballRun(4);
  launchPinball(run);run.inShooter=false;run.x=480;run.y=532;run.vy=650;
  expect(stepPinball(run,ARCADE_STEP)).toBe('stage');
  expect(run.balls).toBe(2);
 });
 it('awards combo bumper scores without per-substep farming and drains three balls',()=>{
  const run=createPinballRun(6);launchPinball(run);
  run.inShooter=false;run.x=414;run.y=216;run.vx=-90;run.vy=0;
  expect(stepPinball(run,ARCADE_STEP)).toBe('score');
  expect(run.score).toBeGreaterThan(0);
  const score=run.score;
  stepPinball(run,ARCADE_STEP);
  expect(run.score).toBe(score);
  for(let n=1;n<=3;n++){
   run.serve=false;run.inShooter=false;run.x=480;run.y=560;run.vy=20;
   expect(stepPinball(run,ARCADE_STEP)).toBe(n===3?'fail':'stage');
  }
  expect(run.over).toBe(true);
 });
 it('stays deterministic under alternating flipper controls',()=>{
  const a=createPinballRun(33),b=createPinballRun(33);
  launchPinball(a);launchPinball(b);
  for(let i=0;i<500;i++){
   const controls={left:i%41<15,right:i%53<16};
   stepPinball(a,ARCADE_STEP,controls);stepPinball(b,ARCADE_STEP,controls);
   if(a.serve&&b.serve){launchPinball(a);launchPinball(b);}
  }
  expect(a).toEqual(b);
 });
});

describe('Progressive difficulty without changing the two approved game mechanics',()=>{
 it('narrows Neon Drift and makes crossing oncoming traffic penalize a collision',()=>{
  expect(driftRoadHalfWidth(12000)).toBeLessThan(driftRoadHalfWidth(0));
  expect(driftRoadHalfWidth(0)).toBeGreaterThan(90);
  const run=createArcadeRun('neon-drift',4) as DriftRun;
  const obstacle=driftTrafficAt(0);
  run.distance=obstacle.distance-1;run.x=obstacle.x;run.vx=0;run.hitCooldown=0;
  expect(stepArcadeRun(run,ARCADE_STEP)).toBe('stage');
  expect(run.hits).toBe(1);
 });
 it('adds moving lethal sentries only in advanced Gravity Chamber rooms',()=>{
  expect(gravitySentries(1,10)).toHaveLength(0);
  expect(gravityHazards(8).length).toBeGreaterThan(gravityHazards(1).length);
  expect(gravitySentries(8,5).length).toBeGreaterThan(gravitySentries(4,5).length);
  const run=createArcadeRun('gravity-chamber',5) as GravityRun;
  run.stage=4;run.invulnerable=0;
  const sentry=gravitySentries(run.stage,run.elapsed+ARCADE_STEP)[0];
  run.x=sentry.x;run.y=sentry.y;
  expect(stepArcadeRun(run,ARCADE_STEP)).toBe('stage');
  expect(run.lives).toBe(2);
 });
 it('spawns actual collision-capable pursuit drones after mining deliveries',()=>{
  const run=createArcadeRun('meteor-miner',5) as MinerRun;
  expect(run.drones).toHaveLength(0);
  run.cargo=2;run.cargoValue=3;
  run.x=115;run.y=435;
  expect(stepArcadeRun(run,ARCADE_STEP)).toBe('score');
  expect(run.deposits).toBe(1);
  expect(run.drones).toHaveLength(1);
  run.rocks=[];
  run.x=460;run.y=260;run.vx=0;run.vy=0;run.invincible=0;
  run.drones[0].x=460;run.drones[0].y=260;
  const oxygen=run.oxygen;
  expect(stepArcadeRun(run,ARCADE_STEP)).toBeNull();
  expect(run.hull).toBe(2);
  expect(run.invincible).toBeGreaterThan(1);
  expect(run.oxygen).toBeLessThan(oxygen);
 });
 it('cycles through four genuinely different Heist rooms with escalating patrol counts',()=>{
  expect(HEIST_STAGE_WALLS).toHaveLength(4);
  expect(new Set(HEIST_STAGE_WALLS.map(w=>JSON.stringify(w))).size).toBe(4);
  const run=createArcadeRun('shadow-heist',7) as HeistRun;
  const firstWalls=run.walls;
  expect(run.guards).toHaveLength(3);
  run.loot.forEach(g=>g.taken=true);
  run.x=885;run.y=452;
  expect(stepArcadeRun(run,ARCADE_STEP)).toBe('score');
  expect(run.walls).not.toEqual(firstWalls);
  expect(run.guards).toHaveLength(4);
  expect(run.guards.some(g=>g.axis==='y')).toBe(true);
  for(let i=0;i<3;i++){
   run.loot.forEach(g=>g.taken=true);
   run.x=885;run.y=452;run.invincible=2;
   stepArcadeRun(run,ARCADE_STEP);
  }
  expect(run.stage).toBe(5);
  expect(run.guards.length).toBeGreaterThanOrEqual(5);
 });
});

describe('Prism Lock and Circuit Rush readable failure states',()=>{
 function solvedPrism(s:PrismRun){
  const first=s.mirrors.find(m=>m.c===1&&m.r!==2)!.r;
  const second=s.targetRow;
  return s.mirrors.filter(m=>m.required).map(m=>({
   c:m.c,r:m.r,slash:m.c===1?first<2:second<first,
  }));
 }
 it('freezes Prism Lock on a visible level-cleared screen before next stage',()=>{
  const run=createExpansionTwoRun('prism-lock',17) as PrismRun;
  const solution=solvedPrism(run);
  expect(tracePrism(run.mirrors,run.targetRow).hit).toBe(false);
  for(const m of solution){
   const actual=run.mirrors.find(a=>a.c===m.c&&a.r===m.r)!;
   if(actual.slash!==m.slash)selectPrism(run,m.c,m.r);
  }
  expect(run.stage).toBe(2);
  expect(run.transition).toBe('clear');
  expect(run.transitionClock).toBeGreaterThan(2);
  const time=run.timeLeft;
  expect(selectPrism(run,1,2)).toBeNull();
  for(let i=0;i<60;i++)stepArcadeRun(run,ARCADE_STEP);
  expect(run.timeLeft).toBe(time);
  expect(run.transition).toBe('clear');
  for(let i=0;i<80&&run.transition;i++)stepArcadeRun(run,ARCADE_STEP);
  expect(run.transition).toBeNull();
  expect(run.stage).toBe(2);
  expect(run.timeLeft).toBeGreaterThan(15);
 });
 it('generates red circuit hazards away from guaranteed solution and penalizes powering them',()=>{
  for(let seed=1;seed<=35;seed++){
   const s=createExpansionTwoRun('circuit-rush',seed) as CircuitRun;
   expect(s.tiles.some(t=>t.danger)).toBe(true);
   expect(s.tiles.every(t=>!t.danger||!t.path)).toBe(true);
   expect(s.tiles.every(t=>!t.danger||!s.powered.some(p=>p.c===t.c&&p.r===t.r))).toBe(true);
  }
  const s=createExpansionTwoRun('circuit-rush',6) as CircuitRun;
  const hazard=s.tiles.find(t=>t.danger)!;
  const path=[{c:0,r:2}];
  let c=0,r=2;
  while(c!==hazard.c){c+=Math.sign(hazard.c-c);path.push({c,r});}
  while(r!==hazard.r){r+=Math.sign(hazard.r-r);path.push({c,r});}
  const port=(a:{c:number;r:number},b:{c:number;r:number})=>
   b.c>a.c?2:b.c<a.c?8:b.r>a.r?4:1;
  for(const t of s.tiles)t.mask=0;
  for(let i=0;i<path.length;i++){
   const curr=path[i],t=s.tiles.find(q=>q.c===curr.c&&q.r===curr.r)!;
   const before=i===0?{c:-1,r:2}:path[i-1];
   const after=i===path.length-1?null:path[i+1];
   t.mask=port(curr,before)|(after?port(curr,after):0);
  }
  hazard.mask=((hazard.mask<<1)&15)|((hazard.mask&8)>>3);
  s.powered=poweredCircuit(s.tiles);
  expect(s.powered.some(p=>p.c===hazard.c&&p.r===hazard.r)).toBe(false);
  let result=null;
  for(let i=0;i<4&&!s.shockClock;i++)result=rotateCircuit(s,hazard.c,hazard.r);
  expect(result).toBe('stage');
  expect(s.shockClock).toBeGreaterThan(1);
  expect(s.lives).toBe(2);
  expect(rotateCircuit(s,hazard.c,hazard.r)).toBeNull();
 });
});

describe('Individual arcade sounds and preserved Pulse Perfect rules',()=>{
 it('provides a unique synthesized sound signature for each of twenty games',()=>{
  expect(Object.keys(ARCADE_VOICES)).toHaveLength(20);
  expect(new Set(PLAYABLE_ARCADE_IDS.map(id=>soundProfileSignature(id))).size).toBe(20);
  for(const id of PLAYABLE_ARCADE_IDS)expect(soundNotes(id,'score').length).toBeGreaterThan(0);
 });
 it('maps six Echo Sequence fields to six reproducible, individual notes',()=>{
  expect(ECHO_PAD_NOTES).toHaveLength(6);
  const notes=ECHO_PAD_NOTES.map((_,i)=>soundNotes('echo-sequence','pad',i)[0]);
  expect(new Set(notes).size).toBe(6);
  expect(notes.every(n=>n>220&&n<900)).toBe(true);
 });
 it('distinguishes Pulse Perfect hits without modifying its scoring model',()=>{
  const perfect=soundNotes('pulse-perfect','perfect');
  const good=soundNotes('pulse-perfect','good');
  const miss=soundNotes('pulse-perfect','miss');
  expect(perfect.length).toBeGreaterThan(good.length);
  expect(good).not.toEqual(miss);
  const run=createArcadeRun('pulse-perfect',99);
  if(run.kind!=='pulse-perfect')throw Error('Unreachable');
  run.radius=run.target+2;
  expect(actArcadeRun(run)).toBe('score');
  expect(run.score).toBe(100);
 });
 it('does not create audio in muted mode, including a WebView2-less test environment',()=>{
  const context={current:null};
  expect(()=>playArcadeSound('pulse-perfect','perfect',false,context)).not.toThrow();
  expect(context.current).toBeNull();
 });
});
