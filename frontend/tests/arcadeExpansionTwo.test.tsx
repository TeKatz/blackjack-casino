import {describe,expect,it} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {ArcadeGameScreen} from '../src/lobby/ArcadeGameScreen';
import {ArcadeMenu} from '../src/lobby/ArcadeMenu';
import {arcadeGames,arcadeCategoryNames} from '../src/lobby/arcadeCatalog';
import {ARCADE_STEP,PLAYABLE_ARCADE_IDS,actArcadeRun,createArcadeRun,
 stepArcadeRun,isPlayableArcade} from '../src/lobby/arcadeEngine';
import {EXPANSION_TWO_IDS,PRISM_COLS,PRISM_ROWS,CIRCUIT_COLS,CIRCUIT_ROWS,
 createExpansionTwoRun,selectPrism,tracePrism,rotatePorts,rotateCircuit,poweredCircuit,
 circuitSolved,movePuzzleCursor,guardSees,pulseStrike,DIRS} from '../src/lobby/arcadeExpansionTwoEngine';
import type {PrismRun,MinerRun,CircuitRun,HeistRun,PulseRun,PrismMirror,CircuitTile}
 from '../src/lobby/arcadeExpansionTwoEngine';
import {prismCellAt,circuitCellAt} from '../src/lobby/arcadeExpansionTwoRenderer';
import {readArcadeRecord,saveArcadeRecord} from '../src/lobby/arcadeRecords';

describe('Arcade Expansion II — five independently playable games',()=>{
 it('preserves the previous fifteen IDs and adds five unique, fully localized modes',()=>{
  expect(PLAYABLE_ARCADE_IDS).toHaveLength(20);
  expect(PLAYABLE_ARCADE_IDS.slice(-5)).toEqual([...EXPANSION_TWO_IDS]);
  expect(new Set(PLAYABLE_ARCADE_IDS).size).toBe(20);
  expect(arcadeGames).toHaveLength(20);
  for(const id of EXPANSION_TWO_IDS){
   expect(isPlayableArcade(id)).toBe(true);
   expect(createArcadeRun(id).kind).toBe(id);
   const entry=arcadeGames.find(g=>g.id===id);
   expect(entry?.detail.en.length).toBeGreaterThan(40);
   expect(entry?.detail.de.length).toBeGreaterThan(40);
   expect(arcadeCategoryNames[entry!.category]).toBeTruthy();
   for(const language of ['de','en'] as const){
    const html=renderToStaticMarkup(<ArcadeGameScreen id={id} language={language} onBack={()=>{}}/>);
    expect(html).toContain('data-game-id="'+id+'"');
    expect(html).toContain('data-testid="arcade-canvas"');
    expect(html).toContain('data-testid="arcade-best"');
   }
  }
  const menu=renderToStaticMarkup(<ArcadeMenu language="en" onBack={()=>{}}/>);
  expect((menu.match(/data-arcade-id=/g)||[]).length).toBe(20);
  for(const id of EXPANSION_TWO_IDS){
   expect(menu).toContain('data-arcade-id="'+id+'"');
   expect(menu).toContain('data-arcade-scene="'+id+'"');
  }
 });
 it('keeps local record keys separate from the existing fifteen modes',()=>{
  const data=new Map<string,string>();
  const store={getItem:(k:string)=>data.get(k)??null,setItem:(k:string,v:string)=>{data.set(k,v);}};
  saveArcadeRecord('midnight-wings',29,22,store);
  saveArcadeRecord('echo-sequence',800,60,store);
  for(const id of EXPANSION_TWO_IDS){
   saveArcadeRecord(id,450,44,store);
   saveArcadeRecord(id,300,19,store);
   expect(readArcadeRecord(id,store)).toEqual({best:450,last:300,runs:2,bestSeconds:44});
  }
  expect(readArcadeRecord('midnight-wings',store).best).toBe(29);
  expect(readArcadeRecord('echo-sequence',store).best).toBe(800);
  expect(data.size).toBe(7);
 });
});

describe('Prism Lock — physically reflecting laser',()=>{
 function solvedMirrors(s:PrismRun):PrismMirror[]{
  const upper=s.mirrors.find(m=>m.c===1&&m.r!==2)!.r;
  const lower=s.targetRow;
  return s.mirrors.map(m=>{
   if(!m.required)return {...m};
   if(m.c===1)return {...m,slash:upper<2};
   return {...m,slash:lower<upper};
  });
 }
 it('generates a guaranteed optical solution for many seeds and a non-solved starting board',()=>{
  for(let seed=1;seed<=65;seed++){
   const s=createExpansionTwoRun('prism-lock',seed) as PrismRun;
   expect(s.mirrors.filter(m=>m.required)).toHaveLength(4);
   expect(s.targetRow).toBeGreaterThanOrEqual(0);
   expect(s.targetRow).toBeLessThan(PRISM_ROWS);
   expect(s.beam.hit).toBe(false);
   expect(tracePrism(solvedMirrors(s),s.targetRow).hit).toBe(true);
   expect(tracePrism(s.mirrors,s.targetRow)).toEqual(s.beam);
  }
 });
 it('recalculates the beam after mirror rotations, scoring and advancing on success',()=>{
  const s=createExpansionTwoRun('prism-lock',17) as PrismRun;
  const solved=solvedMirrors(s);
  for(const m of solved)if(m.required){
   const actual=s.mirrors.find(x=>x.c===m.c&&x.r===m.r)!;
   if(actual.slash!==m.slash){
    const old=s.stage;
    const result=selectPrism(s,m.c,m.r);
    if(s.stage!==old){expect(result).toBe('score');break;}
    expect(result).toBe('flap');
   }
  }
  expect(s.stage).toBe(2);
  expect(s.score).toBeGreaterThan(250);
  expect(selectPrism(s,-10,2)).toBeNull();
  expect(prismCellAt(180,102)).toEqual({c:0,r:0});
  expect(prismCellAt(5,5)).toBeNull();
  expect(PRISM_COLS).toBe(7);
 });
 it('allows keyboard navigation and charges a life when the timer expires',()=>{
  const s=createExpansionTwoRun('prism-lock',9) as PrismRun;
  movePuzzleCursor(s,100,100);
  expect(s.cursor).toEqual({c:6,r:4});
  s.timeLeft=.001;
  expect(stepArcadeRun(s,ARCADE_STEP)).toBe('stage');
  expect(s.lives).toBe(2);
  s.timeLeft=.001;stepArcadeRun(s,ARCADE_STEP);
  s.timeLeft=.001;expect(stepArcadeRun(s,ARCADE_STEP)).toBe('fail');
  expect(s.over).toBe(true);
 });
});

describe('Meteor Miner — deposits, oxygen, collisions',()=>{
 it('mines actual ore with held input and scores only after the ship docks',()=>{
  const s=createArcadeRun('meteor-miner',31) as MinerRun;
  expect(s.rocks).toHaveLength(9);
  expect(s.cargo).toBe(0);expect(s.score).toBe(0);
  const rock=s.rocks[0];
  s.x=rock.x+rock.r+35;s.y=rock.y;s.vx=0;s.vy=0;
  for(let i=0;i<82;i++)stepArcadeRun(s,ARCADE_STEP,true);
  expect(s.cargo).toBeGreaterThan(0);
  expect(s.score).toBe(0);
  const collected=s.cargo;
  s.x=115;s.y=435;s.vx=0;s.vy=0;
  expect(stepArcadeRun(s,ARCADE_STEP)).toBe('score');
  expect(s.score).toBeGreaterThanOrEqual(collected*20);
  expect(s.cargo).toBe(0);expect(s.deposits).toBe(1);
 });
 it('moves in two axes, expires oxygen and respects finished-run freeze',()=>{
  const s=createArcadeRun('meteor-miner',22) as MinerRun;
  const x=s.x,y=s.y;
  for(let i=0;i<10;i++)stepArcadeRun(s,ARCADE_STEP,false,{right:true,up:true});
  expect(s.x).toBeGreaterThan(x);
  expect(s.y).toBeLessThan(y);
  s.oxygen=.001;s.x=500;s.y=270;
  expect(stepArcadeRun(s,ARCADE_STEP)).toBe('fail');
  const frozen=JSON.stringify(s);
  expect(stepArcadeRun(s,ARCADE_STEP)).toBeNull();
  expect(JSON.stringify(s)).toBe(frozen);
 });
});

describe('Circuit Rush — powered tile connectivity',()=>{
 function solution(s:CircuitRun):CircuitTile[]{
  const other=s.tiles.find(t=>t.path&&t.c===2&&t.r!==2)!.r;
  const path=[{c:0,r:2},{c:1,r:2},{c:2,r:2},{c:2,r:other},
   {c:3,r:other},{c:4,r:other},{c:4,r:2},{c:5,r:2}];
  const bit=(a:{c:number;r:number},b:{c:number;r:number})=>
   b.c>a.c?2:b.c<a.c?8:b.r>a.r?4:1;
  return s.tiles.map(tile=>{
   const i=path.findIndex(p=>p.c===tile.c&&p.r===tile.r);
   if(i<0)return {...tile};
   const before=i===0?{c:-1,r:2}:path[i-1];
   const after=i===path.length-1?{c:6,r:2}:path[i+1];
   return {...tile,mask:bit(tile,before)|bit(tile,after)};
  });
 }
 it('models true four-way rotations and a generated solvable two-way network',()=>{
  expect(rotatePorts(1)).toBe(2);
  expect(rotatePorts(2)).toBe(4);
  expect(rotatePorts(4)).toBe(8);
  expect(rotatePorts(8)).toBe(1);
  expect(DIRS).toHaveLength(4);
  for(let seed=1;seed<55;seed++){
   const s=createArcadeRun('circuit-rush',seed) as CircuitRun;
   expect(s.tiles).toHaveLength(CIRCUIT_COLS*CIRCUIT_ROWS);
   expect(circuitSolved(s)).toBe(false);
   s.tiles=solution(s);s.powered=poweredCircuit(s.tiles);
   expect(circuitSolved(s)).toBe(true);
  }
 });
 it('energizes connected nodes, advances after rotation and consumes timer lives',()=>{
  const s=createArcadeRun('circuit-rush',13) as CircuitRun;
  const complete=solution(s);
  s.tiles=complete;s.powered=poweredCircuit(s.tiles);
  const bad=s.tiles.find(t=>t.c===2&&t.r===2)!;
  bad.mask=rotatePorts(bad.mask);
  s.powered=poweredCircuit(s.tiles);
  expect(circuitSolved(s)).toBe(false);
  for(let i=0;i<4&&s.stage===1;i++)rotateCircuit(s,2,2);
  expect(s.stage).toBe(2);
  expect(s.score).toBeGreaterThan(300);
  expect(circuitCellAt(190,90)).toEqual({c:0,r:0});
  expect(circuitCellAt(0,0)).toBeNull();
  expect(CIRCUIT_ROWS).toBe(5);
  s.timeLeft=.001;expect(stepArcadeRun(s,ARCADE_STEP)).toBe('stage');
  expect(s.lives).toBe(2);
 });
});

describe('Shadow Heist — cover and guard vision',()=>{
 it('honors directional vision cones and wall occlusion',()=>{
  const s=createArcadeRun('shadow-heist',8) as HeistRun;
  const g=s.guards[0];
  s.x=g.x+90;s.y=g.y;s.invincible=0;
  expect(guardSees(s,g)).toBe(true);
  s.x=g.x-110;
  expect(guardSees(s,g)).toBe(false);
 });
 it('collects loot, unlocks exits, and advances to a new guarded room',()=>{
  const s=createArcadeRun('shadow-heist',17) as HeistRun;
  s.invincible=3;
  const gem=s.loot[0];
  s.x=gem.x;s.y=gem.y;
  expect(stepArcadeRun(s,ARCADE_STEP)).toBe('score');
  expect(s.loot[0].taken).toBe(true);
  s.loot.forEach(g=>{g.taken=true;});
  s.x=885;s.y=452;
  expect(stepArcadeRun(s,ARCADE_STEP)).toBe('score');
  expect(s.stage).toBe(2);
  expect(s.score).toBeGreaterThan(100);
  expect(s.loot.every(g=>!g.taken)).toBe(true);
 });
 it('raises an alarm after exposure and ends after three detections',()=>{
  const s=createArcadeRun('shadow-heist',22) as HeistRun;
  for(let lost=1;lost<=3;lost++){
   const g=s.guards[0];
   s.invincible=0;s.x=g.x+18;s.y=g.y;s.alert=.998;
   expect(stepArcadeRun(s,ARCADE_STEP)).toBe(lost===3?'fail':'stage');
   expect(s.lives).toBe(3-lost);
  }
  expect(s.over).toBe(true);
 });
});

describe('Pulse Perfect — bounded timing window, combos and misses',()=>{
 it('rewards precise strikes with more points than ordinary good strikes',()=>{
  const perfect=createArcadeRun('pulse-perfect',5) as PulseRun;
  perfect.radius=perfect.target+2;
  expect(actArcadeRun(perfect)).toBe('score');
  expect(perfect.feedback).toBe('perfect');
  expect(perfect.score).toBe(100);
  expect(actArcadeRun(perfect)).toBeNull();
  const good=createArcadeRun('pulse-perfect',5) as PulseRun;
  good.radius=good.target+10;
  expect(pulseStrike(good)).toBe('score');
  expect(good.feedback).toBe('good');
  expect(good.score).toBe(55);
  // Check at the precise transition frame; later frames advance the next pulse.
  for(let i=0;i<32;i++)stepArcadeRun(perfect,ARCADE_STEP);
  expect(perfect.radius).toBe(223);
  expect(perfect.round).toBe(2);
 });
 it('counts three mistimed attempts and automatic ring expiry as misses',()=>{
  const s=createArcadeRun('pulse-perfect',29) as PulseRun;
  for(let lost=1;lost<=3;lost++){
   s.feedbackClock=0;s.radius=s.target+58;
   expect(actArcadeRun(s)).toBe(lost===3?'fail':'stage');
   expect(s.lives).toBe(3-lost);
  }
  expect(s.over).toBe(true);
  const expiry=createArcadeRun('pulse-perfect',7) as PulseRun;
  expiry.radius=expiry.target-16;
  expect(stepArcadeRun(expiry,ARCADE_STEP)).toBe('stage');
  expect(expiry.lives).toBe(2);
 });
});

describe('Safe fixed-step cross-game behavior',()=>{
 it.each(EXPANSION_TWO_IDS)('%s is deterministic for a fixed seed and inputs',id=>{
  const a=createExpansionTwoRun(id,314),b=createExpansionTwoRun(id,314);
  for(let i=0;i<180;i++){
   const controls={left:i%31<8,right:i%31>20,up:i%40<13,down:false};
   stepArcadeRun(a,ARCADE_STEP,i%50<18,controls);
   stepArcadeRun(b,ARCADE_STEP,i%50<18,controls);
  }
  expect(a).toEqual(b);
 });
 it.each(EXPANSION_TWO_IDS)('%s is bounded and frozen after a terminal run',id=>{
  const run=createArcadeRun(id,61);
  stepArcadeRun(run,12);
  expect(run.elapsed).toBeCloseTo(ARCADE_STEP,7);
  run.over=true;
  const before=JSON.stringify(run);
  expect(stepArcadeRun(run,ARCADE_STEP)).toBeNull();
  expect(actArcadeRun(run)).toBeNull();
  expect(JSON.stringify(run)).toBe(before);
 });
});
