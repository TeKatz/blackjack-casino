import {describe,expect,it} from 'vitest';
import {ARCADE_STEP,PLAYABLE_ARCADE_IDS,actArcadeRun,createArcadeRun,
 angularDistance,isPlayableArcade,stepArcadeRun} from '../src/lobby/arcadeEngine';
import type {HexRun,SpikeRun,AscentRun} from '../src/lobby/arcadeEngine';
import {readArcadeRecord,saveArcadeRecord} from '../src/lobby/arcadeRecords';

describe('Arcade Update 3: original ten-game collection remains stable',()=>{
 it('preserves ten original games and five playable expansions without duplicate ids',()=>{
  expect(PLAYABLE_ARCADE_IDS).toHaveLength(15);
  expect(new Set(PLAYABLE_ARCADE_IDS).size).toBe(15);
  expect(PLAYABLE_ARCADE_IDS.slice(7,10)).toEqual([
   'hexagon-hell','spike-rush','endless-ascent',
  ]);
  for(const id of PLAYABLE_ARCADE_IDS){
   expect(isPlayableArcade(id)).toBe(true);
   expect(createArcadeRun(id).kind).toBe(id);
  }
 });
 describe('Hexagon Hell',()=>{
  it('rotates the player only on input, with symmetric speed',()=>{
   const left=createArcadeRun('hexagon-hell',52) as HexRun;
   const right=createArcadeRun('hexagon-hell',52) as HexRun;
   const idle=createArcadeRun('hexagon-hell',52) as HexRun;
   stepArcadeRun(left,ARCADE_STEP,false,{left:true});
   stepArcadeRun(right,ARCADE_STEP,false,{right:true});
   stepArcadeRun(idle,ARCADE_STEP);
   expect(left.angle).toBeLessThan(0);
   expect(right.angle).toBeGreaterThan(0);
   expect(left.angle).toBeCloseTo(-right.angle,9);
   expect(idle.angle).toBe(0);
   expect(actArcadeRun(left)).toBeNull();
  });
  it('collides with solid wall sectors and permits passage through the angular opening',()=>{
   const blocked=createArcadeRun('hexagon-hell') as HexRun;
   blocked.walls=[{radius:126,opening:0,gap:1.5,thickness:34,passed:false}];
   blocked.angle=Math.PI;
   expect(stepArcadeRun(blocked,ARCADE_STEP)).toBe('fail');
   expect(blocked.over).toBe(true);
   const safe=createArcadeRun('hexagon-hell') as HexRun;
   safe.walls=[{radius:126,opening:0,gap:1.5,thickness:34,passed:false}];
   safe.angle=0;
   expect(stepArcadeRun(safe,ARCADE_STEP)).toBeNull();
   expect(safe.over).toBe(false);
   expect(angularDistance(-Math.PI+.1,Math.PI-.1)).toBeCloseTo(.2,7);
  });
  it('scores completed walls and generates continuous, progressively narrower gaps',()=>{
   const run=createArcadeRun('hexagon-hell',15) as HexRun;
   run.walls=[{radius:92,opening:0,gap:1.5,thickness:34,passed:false}];
   expect(stepArcadeRun(run,ARCADE_STEP)).toBe('score');
   expect(run.score).toBe(1);
   expect(run.walls[0].passed).toBe(true);
   expect(stepArcadeRun(run,ARCADE_STEP)).toBeNull();
   expect(run.score).toBe(1);
   run.spawn=0;
   stepArcadeRun(run,ARCADE_STEP);
   expect(run.walls).toHaveLength(2);
   expect(run.walls[1].gap).toBeLessThan(1.65);
   expect(run.walls[1].gap).toBeGreaterThanOrEqual(.86);
   expect(Math.abs(run.walls[1].opening)).toBeLessThanOrEqual(.825);
  });
 });
 describe('Spike Rush',()=>{
  it('auto-scrolls and allows one jump only when grounded',()=>{
   const run=createArcadeRun('spike-rush',34) as SpikeRun;
   expect(run.grounded).toBe(true);
   expect(actArcadeRun(run)).toBe('flap');
   expect(actArcadeRun(run)).toBeNull();
   const initial=run.feet;
   stepArcadeRun(run,ARCADE_STEP);
   expect(run.feet).toBeLessThan(initial);
   expect(run.distance).toBeGreaterThan(0);
   expect(run.obstacles[0].x).toBeLessThan(1000);
  });
  it('hits spikes when on the ground and clears them on an airborne jump',()=>{
   const hit=createArcadeRun('spike-rush') as SpikeRun;
   hit.obstacles=[{x:199,width:42,type:'spike',passed:false}];
   expect(stepArcadeRun(hit,ARCADE_STEP)).toBe('fail');
   const jump=createArcadeRun('spike-rush') as SpikeRun;
   jump.obstacles=[{x:199,width:42,type:'spike',passed:false}];
   jump.feet=385;jump.vy=-100;jump.grounded=false;
   expect(stepArcadeRun(jump,ARCADE_STEP)).toBeNull();
   expect(jump.over).toBe(false);
  });
  it('falls through gaps without a floor, and only counts obstacles after passing them',()=>{
   const gap=createArcadeRun('spike-rush') as SpikeRun;
   gap.obstacles=[{x:182,width:95,type:'gap',passed:false}];
   for(let i=0;i<40&&!gap.over;i++)stepArcadeRun(gap,ARCADE_STEP);
   expect(gap.over).toBe(true);
   const passed=createArcadeRun('spike-rush') as SpikeRun;
   passed.obstacles=[{x:140,width:42,type:'spike',passed:false}];
   stepArcadeRun(passed,ARCADE_STEP);
   expect(passed.score).toBe(1);
   for(let i=0;i<4;i++)stepArcadeRun(passed,ARCADE_STEP);
   expect(passed.score).toBe(1);
  });
  it('generates deterministic spaced obstacles and unlocks gaps only after the introduction',()=>{
   const first=createArcadeRun('spike-rush',44) as SpikeRun;
   const second=createArcadeRun('spike-rush',44) as SpikeRun;
   first.spawn=0;second.spawn=0;
   stepArcadeRun(first,ARCADE_STEP);
   stepArcadeRun(second,ARCADE_STEP);
   expect(first).toEqual(second);
   expect(first.obstacles.at(-1)?.type).toBe('spike');
   first.score=8;first.spawn=0;
   stepArcadeRun(first,ARCADE_STEP);
   expect(['spike','double','gap','saw','tower']).toContain(first.obstacles.at(-1)?.type);
  });
 });
 describe('Endless Ascent',()=>{
  it('begins with an automatic upward leap and permits responsive horizontal movement',()=>{
   const run=createArcadeRun('endless-ascent',17) as AscentRun;
   expect(run.vy).toBeLessThan(0);
   expect(actArcadeRun(run)).toBeNull();
   const x=run.x,feet=run.feet;
   stepArcadeRun(run,ARCADE_STEP,false,{left:true});
   expect(run.x).toBeLessThan(x);
   expect(run.feet).toBeLessThan(feet);
   stepArcadeRun(run,ARCADE_STEP,false,{right:true});
   expect(run.x).toBeCloseTo(x);
  });
  it('automatically bounces only when descending onto a platform',()=>{
   const run=createArcadeRun('endless-ascent') as AscentRun;
   run.feet=398;run.vy=350;
   run.platforms=[{x:400,y:401,width:160,vx:0}];
   expect(stepArcadeRun(run,ARCADE_STEP)).toBeNull();
   expect(run.feet).toBe(401);
   expect(run.vy).toBeLessThan(-500);
   expect(run.jumps).toBe(1);
  });
  it('scrolls the world, tracks height and generates reachable platforms',()=>{
   const run=createArcadeRun('endless-ascent',881) as AscentRun;
   const originalY=run.platforms[0].y;
   run.feet=200;run.vy=-200;
   stepArcadeRun(run,ARCADE_STEP);
   expect(run.feet).toBe(240);
   expect(run.climbed).toBeGreaterThan(35);
   expect(run.score).toBeGreaterThan(0);
   expect(run.platforms[0].y).toBeGreaterThan(originalY);
   expect(Math.min(...run.platforms.map(x=>x.y))).toBeLessThanOrEqual(-85);
   for(const platform of run.platforms){
    expect(platform.x).toBeGreaterThanOrEqual(20);
    expect(platform.x+platform.width).toBeLessThanOrEqual(940);
   }
  });
  it('ends a run when the player falls below the viewport and remains frozen afterward',()=>{
   const run=createArcadeRun('endless-ascent',99) as AscentRun;
   run.feet=590;run.vy=250;run.x=25;
   expect(stepArcadeRun(run,ARCADE_STEP)).toBe('fail');
   const snapshot=JSON.stringify(run);
   expect(stepArcadeRun(run,ARCADE_STEP)).toBeNull();
   expect(JSON.stringify(run)).toBe(snapshot);
  });
 });
 it('persists independent best scores for all three new games',()=>{
  const data=new Map<string,string>();
  const store={
   getItem:(key:string)=>data.get(key)??null,
   setItem:(key:string,value:string)=>{data.set(key,value);},
  };
  for(const id of ['hexagon-hell','spike-rush','endless-ascent'] as const){
   saveArcadeRecord(id,19,42,store);
   saveArcadeRecord(id,12,23,store);
   expect(readArcadeRecord(id,store)).toEqual({
    best:19,last:12,runs:2,bestSeconds:42,
   });
  }
  expect(data.size).toBe(3);
 });
});
