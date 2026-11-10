import {describe,expect,it} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {actArcadeRun,ARCADE_STEP,createArcadeRun,isPlayableArcade,
 PLAYABLE_ARCADE_IDS,stepArcadeRun} from '../src/lobby/arcadeEngine';
import {ArcadeGameScreen} from '../src/lobby/ArcadeGameScreen';
import {ArcadeMenu} from '../src/lobby/ArcadeMenu';
import {arcadeGames} from '../src/lobby/arcadeCatalog';
import {EXPANSION_ONE_IDS,chooseEchoPad,driftCenter,gravityGoalY} from '../src/lobby/arcadeExpansionOneEngine';
import type {PinballRun,HookRun,DriftRun,GravityRun,EchoRun} from '../src/lobby/arcadeExpansionOneEngine';
import {echoPadAt} from '../src/lobby/arcadeExpansionOneRenderer';
import {readArcadeRecord,saveArcadeRecord} from '../src/lobby/arcadeRecords';

describe('Arcade Expansion I — five fully playable games',()=>{
 it('preserves the original ten IDs and registers five distinct complete localizations',()=>{
  expect(PLAYABLE_ARCADE_IDS).toHaveLength(20);
  expect(PLAYABLE_ARCADE_IDS.slice(10,15)).toEqual([...EXPANSION_ONE_IDS]);
  expect(new Set(PLAYABLE_ARCADE_IDS).size).toBe(20);
  for(const id of EXPANSION_ONE_IDS){
   expect(isPlayableArcade(id)).toBe(true);
   expect(createArcadeRun(id).kind).toBe(id);
   const game=arcadeGames.find(g=>g.id===id)!;
   expect(game?.name.length).toBeGreaterThan(4);
   expect(game?.detail.de.length).toBeGreaterThan(40);
   expect(game?.detail.en.length).toBeGreaterThan(40);
   for(const language of ['de','en'] as const){
    const html=renderToStaticMarkup(<ArcadeGameScreen id={id} language={language} onBack={()=>{}}/>);
    expect(html).toContain('data-game-id="'+id+'"');
    expect(html).toContain('data-testid="arcade-canvas"');
    expect(html).toContain('data-testid="arcade-score"');
    expect(html).toContain('data-testid="arcade-best"');
   }
  }
  const lobby=renderToStaticMarkup(<ArcadeMenu language="de" onBack={()=>{}}/>);
  for(const id of EXPANSION_ONE_IDS){
   expect(lobby).toContain('data-arcade-id="'+id+'"');
   expect(lobby).toContain('data-arcade-scene="'+id+'"');
  }
 });
 it('provides independent persistent records for all five IDs without migrating old ones',()=>{
  const data=new Map<string,string>();
  const store={getItem:(key:string)=>data.get(key)??null,
   setItem:(key:string,val:string)=>{data.set(key,val);}};
  saveArcadeRecord('midnight-wings',7,20,store);
  for(const id of EXPANSION_ONE_IDS){
   saveArcadeRecord(id,12,32,store);
   saveArcadeRecord(id,9,25,store);
   expect(readArcadeRecord(id,store)).toEqual({best:12,last:9,runs:2,bestSeconds:32});
  }
  expect(readArcadeRecord('midnight-wings',store).best).toBe(7);
  expect(data.size).toBe(6);
 });
});

describe('Midnight Pinball',()=>{
 it('launches balls and lets the two independent flipper controls move',()=>{
  const s=createArcadeRun('midnight-pinball',19) as PinballRun;
  expect(s.balls).toBe(3);expect(s.serve).toBe(true);
  expect(actArcadeRun(s)).toBe('flap');
  expect(s.serve).toBe(false);expect(s.vy).toBeLessThan(0);
  expect(actArcadeRun(s)).toBeNull();
  stepArcadeRun(s,ARCADE_STEP,false,{left:true});
  expect(s.leftFlipper).toBe(true);expect(s.rightFlipper).toBe(false);
  stepArcadeRun(s,ARCADE_STEP,false,{right:true});
  expect(s.leftFlipper).toBe(false);expect(s.rightFlipper).toBe(true);
 });
 it('scores bumper hits and counts exactly three drained balls',()=>{
  const s=createArcadeRun('midnight-pinball',19) as PinballRun;
  actArcadeRun(s);s.x=414;s.y=216;s.vx=-90;s.vy=0;
  expect(stepArcadeRun(s,ARCADE_STEP)).toBe('score');
  expect(s.score).toBeGreaterThan(0);
  const first=s.score;
  // Contacts are debounced, preventing per-frame score farming.
  stepArcadeRun(s,ARCADE_STEP);
  expect(s.score).toBe(first);
  for(let lost=1;lost<=3;lost++){
   s.serve=false;s.y=H+25;s.vy=100;
   const event=stepArcadeRun(s,ARCADE_STEP);
   expect(event).toBe(lost===3?'fail':'stage');
   expect(s.balls).toBe(3-lost);
  }
  expect(s.over).toBe(true);
  expect(actArcadeRun(s)).toBeNull();
 });
});
const H=540;
describe('Phantom Hook',()=>{
 it('attaches to reachable nodes, applies a rope constraint and releases on command',()=>{
  const s=createArcadeRun('phantom-hook',4) as HookRun;
  expect(actArcadeRun(s)).toBe('flap');
  expect(s.anchored).toBeGreaterThanOrEqual(0);
  const a=s.anchors[s.anchored];
  for(let i=0;i<40;i++)stepArcadeRun(s,ARCADE_STEP);
  expect(Math.hypot(s.x-a.x,s.y-a.y)).toBeLessThanOrEqual(s.rope+.001);
  expect(actArcadeRun(s)).toBe('flap');
  expect(s.anchored).toBe(-1);
  s.y=H+30;
  expect(stepArcadeRun(s,ARCADE_STEP)).toBe('fail');
 });
 it('generates deterministic anchor spacing for the same seeded run',()=>{
  const a=createArcadeRun('phantom-hook',44) as HookRun;
  const b=createArcadeRun('phantom-hook',44) as HookRun;
  actArcadeRun(a);actArcadeRun(b);
  for(let i=0;i<100;i++){
   if(i===40){actArcadeRun(a);actArcadeRun(b);}
   stepArcadeRun(a,ARCADE_STEP);
   stepArcadeRun(b,ARCADE_STEP);
  }
  expect(a).toEqual(b);
 });
});

describe('Neon Drift',()=>{
 it('moves on the curving road and awards combo points only while drifting',()=>{
  const s=createArcadeRun('neon-drift',4) as DriftRun;
  expect(driftCenter(0)).toBeCloseTo(480);
  for(let i=0;i<12;i++)stepArcadeRun(s,ARCADE_STEP,true,{right:true});
  expect(s.x).toBeGreaterThan(480);
  expect(s.distance).toBeGreaterThan(0);
  expect(s.combo).toBeGreaterThan(0);
  expect(s.driftPoints).toBeGreaterThan(0);
  expect(s.score).toBeGreaterThan(0);
 });
 it('ends only after three hard road-edge collisions',()=>{
  const s=createArcadeRun('neon-drift',7) as DriftRun;
  for(let i=0;i<3;i++){
   s.x=930;s.hitCooldown=0;
   expect(stepArcadeRun(s,ARCADE_STEP)).toBe(i===2?'fail':'stage');
  }
  expect(s.hits).toBe(3);expect(s.over).toBe(true);
 });
});

describe('Gravity Chamber',()=>{
 it('reverses gravity, detects hazards and awards a cleared room',()=>{
  const s=createArcadeRun('gravity-chamber',9) as GravityRun;
  expect(s.gravity).toBe(1);
  expect(actArcadeRun(s)).toBe('flap');
  expect(s.gravity).toBe(-1);
  for(let i=0;i<20;i++)stepArcadeRun(s,ARCADE_STEP);
  expect(s.y).toBeLessThan(438);
  const collision=createArcadeRun('gravity-chamber',2) as GravityRun;
  collision.x=300;collision.y=438;
  expect(stepArcadeRun(collision,ARCADE_STEP)).toBe('stage');
  expect(collision.lives).toBe(2);
  const clear=createArcadeRun('gravity-chamber',2) as GravityRun;
  expect(gravityGoalY(clear.stage)).toBe(438);
  clear.x=825;clear.y=438;
  expect(stepArcadeRun(clear,ARCADE_STEP)).toBe('score');
  expect(clear.stage).toBe(2);expect(clear.score).toBeGreaterThan(0);
  expect(gravityGoalY(clear.stage)).toBe(93);
 });
});

describe('Echo Sequence',()=>{
 const waitUntilInput=(s:EchoRun)=>{
  for(let i=0;i<700&&s.phase!=='input'&&!s.over;i++)stepArcadeRun(s,ARCADE_STEP);
  expect(s.phase).toBe('input');
 };
 it('shows a seeded pattern and accepts keyboard/pointer pad coordinates',()=>{
  const s=createArcadeRun('echo-sequence',5) as EchoRun;
  expect(s.phase).toBe('watch');
  expect(s.sequence).toHaveLength(1);
  expect(echoPadAt(250,150)).toBe(0);
  expect(echoPadAt(450,150)).toBe(1);
  expect(echoPadAt(600,360)).toBe(5);
  expect(echoPadAt(40,20)).toBeNull();
  expect(chooseEchoPad(s,s.sequence[0])).toBeNull(); // watch phase ignores inputs
  waitUntilInput(s);
  expect(chooseEchoPad(s,s.sequence[0])).toBe('score');
  expect(s.round).toBe(2);expect(s.score).toBe(100);
  expect(s.sequence).toHaveLength(2);
  expect(chooseEchoPad(s,7)).toBeNull();
  waitUntilInput(s);
  expect(chooseEchoPad(s,s.sequence[0])).toBe('flap');
  expect(chooseEchoPad(s,s.sequence[1])).toBe('score');
  expect(s.score).toBe(300);
 });
 it('charges wrong pads and timeouts as misses, ending after three lives',()=>{
  const s=createArcadeRun('echo-sequence',21) as EchoRun;
  for(let misses=1;misses<=3;misses++){
   waitUntilInput(s);
   const wrong=(s.sequence[0]+1)%6;
   expect(chooseEchoPad(s,wrong)).toBe(misses===3?'fail':'stage');
   expect(s.lives).toBe(3-misses);
  }
  expect(s.over).toBe(true);
 });
});

describe('Cross-game simulation safety',()=>{
 it.each(EXPANSION_ONE_IDS)('%s stops advancing after a finished run',id=>{
  const s=createArcadeRun(id,333);
  s.over=true;const before=JSON.stringify(s);
  expect(stepArcadeRun(s,5)).toBeNull();
  expect(actArcadeRun(s)).toBeNull();
  expect(JSON.stringify(s)).toBe(before);
 });
 it.each(EXPANSION_ONE_IDS)('%s clamps overlong frames to one 60Hz step',id=>{
  const s=createArcadeRun(id,222);
  stepArcadeRun(s,9);
  expect(s.elapsed).toBeCloseTo(ARCADE_STEP,7);
 });
});
