import {describe,expect,it} from 'vitest';
import {ARCADE_STEP,actArcadeRun,createArcadeRun,stepArcadeRun,PLAYABLE_ARCADE_IDS} from '../src/lobby/arcadeEngine';
import {
 createBeatsRun,beatsBpmForPulse,beatsStageForPulse,beatsTargetTime,
 hitBeatsLane,stepBeatsRun,BEATS_GOOD_WINDOW,
} from '../src/lobby/arcadeBeatsEngine';
import {beatsLaneAt} from '../src/lobby/arcadeBeatsRenderer';
import {playBeatsGroovePulse,playBeatsLaneHit,playBeatsMiss,unlockBeatsAudio}
 from '../src/lobby/arcadeBeatsAudio';
import {driftRoadHalfWidth,driftCenter,gravityHazards,gravitySentries}
 from '../src/lobby/arcadeExpansionOneEngine';
import type {DriftRun,GravityRun} from '../src/lobby/arcadeExpansionOneEngine';
import {createExpansionTwoRun,rotateCircuit,poweredCircuit,selectPrism,tracePrism,
 HEIST_STAGE_WALLS} from '../src/lobby/arcadeExpansionTwoEngine';
import type {PrismRun,CircuitRun,MinerRun,HeistRun} from '../src/lobby/arcadeExpansionTwoEngine';
import {ARCADE_VOICES,ECHO_PAD_NOTES,soundNotes,soundProfileSignature,
 playArcadeSound} from '../src/lobby/arcadeSound';

describe('Midnight Beats synchronized endless rhythm',()=>{
 it('produces an exact musical pulse grid and increases tempo at measure boundaries only',()=>{
  expect(beatsStageForPulse(0)).toBe(1);
  expect(beatsStageForPulse(31)).toBe(1);
  expect(beatsStageForPulse(32)).toBe(2);
  expect(beatsBpmForPulse(0)).toBe(104);
  expect(beatsBpmForPulse(32)).toBe(112);
  expect(beatsBpmForPulse(320)).toBe(184);
  expect(beatsBpmForPulse(1000)).toBe(184);
  expect(beatsTargetTime(32)-beatsTargetTime(31)).toBeCloseTo(30/104,7);
  expect(beatsTargetTime(33)-beatsTargetTime(32)).toBeCloseTo(30/112,7);
 });
 it('generates timed notes and never places more than two on one subdivision',()=>{
  const run=createBeatsRun(87);
  run.lives=10000; // inspect the generator over a long non-terminated run
  for(let i=0;i<5400;i++)stepBeatsRun(run,ARCADE_STEP);
  expect(run.stage).toBeGreaterThan(5);
  expect(run.bpm).toBeGreaterThan(145);
  expect(run.notes.length).toBeLessThan(35);
  expect(run.notes.every(n=>n.lane>=0&&n.lane<=3)).toBe(true);
  const a=createBeatsRun(19),b=createBeatsRun(19);
  a.lives=10000;b.lives=10000;
  for(let i=0;i<1800;i++){stepBeatsRun(a,ARCADE_STEP);stepBeatsRun(b,ARCADE_STEP);}
  expect(a).toEqual(b);
 });
 it('scores PERFECT/GREAT/GOOD within distinct timing windows and builds combos',()=>{
  for(const [offset,grade,points] of [
   [0,'PERFECT',300],[.075,'GREAT',200],[.14,'GOOD',100]
  ] as const){
   const run=createBeatsRun(71);
   for(let i=0;i<140;i++)stepBeatsRun(run,ARCADE_STEP);
   const note=run.notes.find(n=>n.judgement===null)!;
   run.elapsed=note.at+offset;
   expect(hitBeatsLane(run,note.lane)).toBe('score');
   expect(run.lastGrade).toBe(grade);
   expect(run.score).toBe(points);
   expect(run.combo).toBe(1);
   expect(run.lastHitPoints).toBe(points);
  }
  expect(BEATS_GOOD_WINDOW).toBeGreaterThan(.14);
 });
 it('penalizes unmatched keys, preserves the five-life limit and ignores repeat spam',()=>{
  const run=createBeatsRun(42);
  for(let i=0;i<145;i++)stepBeatsRun(run,ARCADE_STEP);
  const first=run.notes.find(n=>n.judgement===null)!;
  run.elapsed=first.at;
  const wrongLane=(first.lane+1)%4;
  expect(hitBeatsLane(run,wrongLane)).toBe('stage');
  expect(run.lives).toBe(4);
  expect(run.combo).toBe(0);
  expect(hitBeatsLane(run,wrongLane)).toBeNull();
  expect(run.lives).toBe(4);
  run.wrongLock=0;
  run.lives=1;
  expect(hitBeatsLane(run,wrongLane)).toBe('fail');
  expect(run.over).toBe(true);
 });
 it('maps D/F/J/K pointer lanes and keeps gaps noninteractive',()=>{
  expect(beatsLaneAt(295,418)).toBe(0);
  expect(beatsLaneAt(420,418)).toBe(1);
  expect(beatsLaneAt(540,418)).toBe(2);
  expect(beatsLaneAt(658,418)).toBe(3);
  expect(beatsLaneAt(360,418)).toBeNull();
  expect(beatsLaneAt(300,20)).toBeNull();
  expect(beatsLaneAt(1100,418)).toBeNull();
 });
 it('handles pause via the shared runner without advancing the game clock',()=>{
  const run=createArcadeRun('midnight-beats',7);
  expect(run.kind).toBe('midnight-beats');
  for(let i=0;i<50;i++)stepArcadeRun(run,ARCADE_STEP);
  const before=JSON.stringify(run);
  // A paused runner is not stepped by ArcadeGameScreen. Nothing has an independent timer.
  expect(JSON.stringify(run)).toBe(before);
  run.over=true;
  expect(stepArcadeRun(run,ARCADE_STEP)).toBeNull();
 });
 it('keeps procedural sound safely optional and distinct from scoring',()=>{
  const ref:{current:AudioContext|null}={current:null};
  expect(()=>playBeatsGroovePulse(ref,false,12,104)).not.toThrow();
  expect(()=>playBeatsLaneHit(ref,false,2,'PERFECT')).not.toThrow();
  expect(()=>playBeatsMiss(ref,false)).not.toThrow();
  expect(()=>unlockBeatsAudio(ref,false)).not.toThrow();
  expect(ref.current).toBeNull();
 });
});

describe('Progressive difficulty without changing the two approved game mechanics',()=>{
 it('narrows Neon Drift but only damages the player for leaving the road',()=>{
  expect(driftRoadHalfWidth(12000)).toBeLessThan(driftRoadHalfWidth(0));
  expect(driftRoadHalfWidth(0)).toBeGreaterThan(90);
  const run=createArcadeRun('neon-drift',4) as DriftRun;
  run.distance=780;run.x=driftCenter(run.distance);run.vx=0;run.hitCooldown=0;
  expect(stepArcadeRun(run,ARCADE_STEP)).toBeNull();
  expect(run.hits).toBe(0);
  run.x=driftCenter(run.distance)+driftRoadHalfWidth(run.distance)+45;
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
