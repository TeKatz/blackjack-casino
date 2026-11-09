import {describe,it,expect} from 'vitest';
import {ARCADE_STEP,PLAYABLE_ARCADE_IDS,actArcadeRun,createArcadeRun,
 isPlayableArcade,steerSnake,stepArcadeRun,SNAKE_COLS,SNAKE_ROWS} from '../src/lobby/arcadeEngine';
import type {SnakeRun,BreakerRun,StormRun} from '../src/lobby/arcadeEngine';
import {readArcadeRecord,saveArcadeRecord} from '../src/lobby/arcadeRecords';

describe('Arcade Update 2: three complete arcade simulations',()=>{
 it('preserves the first seven titles of the playable arcade catalog',()=>{
  expect(PLAYABLE_ARCADE_IDS.slice(0,7)).toEqual([
   'midnight-wings','cave-escape','perfect-stack','blade-timing',
   'death-snake','neon-breaker','bullet-storm',
  ]);
  for(const id of PLAYABLE_ARCADE_IDS){
   expect(isPlayableArcade(id)).toBe(true);
   expect(createArcadeRun(id).kind).toBe(id);
  }
  for(const id of ['hexagon-hell','spike-rush','endless-ascent'])
   expect(isPlayableArcade(id)).toBe(true);
 });
 describe('Death Snake',()=>{
  it('steers on grid ticks, rejects reversals and prevents two turns within one tick',()=>{
   const run=createArcadeRun('death-snake',12) as SnakeRun;
   expect(run.body).toEqual([{x:14,y:9},{x:13,y:9},{x:12,y:9}]);
   expect(steerSnake(run,'left')).toBe(false);
   expect(steerSnake(run,'up')).toBe(true);
   expect(steerSnake(run,'left')).toBe(false);
   for(let i=0;i<10;i++)stepArcadeRun(run,ARCADE_STEP);
   expect(run.body[0]).toEqual({x:14,y:8});
   expect(run.direction).toBe('up');
   expect(steerSnake(run,'down')).toBe(false);
   expect(steerSnake(run,'left')).toBe(true);
  });
  it('grows on food, places new food in a free cell and safely adds distant obstacles',()=>{
   const run=createArcadeRun('death-snake',9) as SnakeRun;
   run.food={x:15,y:9};
   run.score=4;
   run.clock=run.speed-ARCADE_STEP/2;
   expect(stepArcadeRun(run,ARCADE_STEP)).toBe('score');
   expect(run.score).toBe(5);
   expect(run.body).toHaveLength(4);
   expect(run.walls).toHaveLength(1);
   expect(Math.abs(run.walls[0].x-run.body[0].x)+Math.abs(run.walls[0].y-run.body[0].y)).toBeGreaterThanOrEqual(5);
   expect(run.body.some(c=>c.x===run.food.x&&c.y===run.food.y)).toBe(false);
   expect(run.speed).toBeLessThan(.16);
  });
  it('fails on borders, its body and spawned barriers but permits entering its vacating tail cell',()=>{
   const edge=createArcadeRun('death-snake') as SnakeRun;
   edge.body=[{x:0,y:8},{x:1,y:8}];edge.direction='left';edge.nextDirection='left';edge.clock=edge.speed;
   expect(stepArcadeRun(edge,ARCADE_STEP)).toBe('fail');
   const wall=createArcadeRun('death-snake') as SnakeRun;
   wall.walls=[{x:15,y:9}];wall.clock=wall.speed;
   expect(stepArcadeRun(wall,ARCADE_STEP)).toBe('fail');
   const tail=createArcadeRun('death-snake') as SnakeRun;
   tail.body=[{x:3,y:2},{x:3,y:3},{x:2,y:3},{x:2,y:2}];
   tail.direction='left';tail.nextDirection='left';tail.clock=tail.speed;
   expect(stepArcadeRun(tail,ARCADE_STEP)).toBeNull();
   expect(tail.body[0]).toEqual({x:2,y:2});
   expect(tail.over).toBe(false);
   expect(tail.body.every(c=>c.x>=0&&c.x<SNAKE_COLS&&c.y>=0&&c.y<SNAKE_ROWS)).toBe(true);
  });
 });
 describe('Neon Breaker',()=>{
  it('serves a ball and allows continuous keyboard and mouse paddle movement',()=>{
   const run=createArcadeRun('neon-breaker',5) as BreakerRun;
   expect(run.serve).toBe(true);
   expect(run.bricks.length).toBe(50);
   expect(actArcadeRun(run)).toBe('flap');
   expect(run.serve).toBe(false);
   const x=run.paddleX;
   stepArcadeRun(run,ARCADE_STEP,false,{right:true});
   expect(run.paddleX).toBeGreaterThan(x);
   stepArcadeRun(run,ARCADE_STEP,false,{pointerX:120});
   expect(run.paddleX).toBeLessThan(x+10);
  });
  it('reflects from the paddle and removes bricks exactly once',()=>{
   const run=createArcadeRun('neon-breaker',8) as BreakerRun;
   actArcadeRun(run);
   run.ballX=run.paddleX;run.ballY=472;run.vx=0;run.vy=320;
   stepArcadeRun(run,ARCADE_STEP);
   expect(run.vy).toBeLessThan(0);
   run.ballX=130;run.ballY=108;run.vx=0;run.vy=-300;
   expect(stepArcadeRun(run,ARCADE_STEP)).toBe('score');
   expect(run.bricks[0].alive).toBe(false);
   expect(run.score).toBe(10);
  });
  it('advances through waves, narrows the paddle and only ends after all lives are spent',()=>{
   const run=createArcadeRun('neon-breaker',7) as BreakerRun;
   actArcadeRun(run);
   run.bricks.forEach(b=>b.alive=false);run.bricks[0].alive=true;
   run.ballX=130;run.ballY=108;run.vx=0;run.vy=-300;
   expect(stepArcadeRun(run,ARCADE_STEP)).toBe('stage');
   expect(run.wave).toBe(2);
   expect(run.paddleWidth).toBe(120);
   expect(run.bricks.every(b=>b.alive)).toBe(true);
   expect(run.score).toBe(60);
   for(let i=0;i<3;i++){
    actArcadeRun(run);
    run.ballY=555;run.vy=300;
    const event=stepArcadeRun(run,ARCADE_STEP);
    if(i<2){expect(event).toBe('stage');expect(run.serve).toBe(true);}
    else expect(event).toBe('fail');
   }
   expect(run.lives).toBe(0);
   expect(run.over).toBe(true);
  });
 });
 describe('Bullet Storm',()=>{
  it('normalizes diagonal movement, clamps borders and builds seeded bullet waves',()=>{
   const run=createArcadeRun('bullet-storm',123) as StormRun;
   const before={x:run.x,y:run.y};
   stepArcadeRun(run,ARCADE_STEP,false,{left:true,up:true});
   expect(run.x).toBeLessThan(before.x);
   expect(run.y).toBeLessThan(before.y);
   expect(Math.hypot(run.x-before.x,run.y-before.y)).toBeCloseTo(345*ARCADE_STEP,4);
   for(let i=0;i<50;i++)stepArcadeRun(run,ARCADE_STEP);
   expect(run.bullets.length).toBeGreaterThan(0);
   expect(run.score).toBeGreaterThan(0);
   run.x=26;run.y=32;
   stepArcadeRun(run,ARCADE_STEP,false,{left:true,up:true});
   expect(run.x).toBe(26);expect(run.y).toBe(32);
  });
  it('has fair offscreen spawns, pointer guidance and deterministic replay',()=>{
   const a=createArcadeRun('bullet-storm',77) as StormRun;
   const b=createArcadeRun('bullet-storm',77) as StormRun;
   for(let i=0;i<55;i++){
    stepArcadeRun(a,ARCADE_STEP,false,{pointerX:650,pointerY:300});
    stepArcadeRun(b,ARCADE_STEP,false,{pointerX:650,pointerY:300});
   }
   expect(a).toEqual(b);
   expect(a.x).toBeGreaterThan(480);
   expect(a.y).toBeLessThan(422);
   expect(a.bullets.some(p=>p.y>0)).toBe(true);
  });
  it('detects an actual projectile collision without mutating past a game over',()=>{
   const run=createArcadeRun('bullet-storm') as StormRun;
   run.bullets=[{x:run.x,y:run.y,vx:0,vy:0,r:10,style:0}];
   expect(stepArcadeRun(run,ARCADE_STEP)).toBe('fail');
   const past=run.elapsed;
   expect(stepArcadeRun(run,ARCADE_STEP)).toBeNull();
   expect(run.elapsed).toBe(past);
  });
 });
 it('stores independent validated records for the three additional games',()=>{
  const memory=new Map<string,string>();
  const storage={getItem:(k:string)=>memory.get(k)??null,setItem:(k:string,v:string)=>{memory.set(k,v);}};
  for(const id of ['death-snake','neon-breaker','bullet-storm'] as const){
   saveArcadeRecord(id,17,24.8,storage);
   saveArcadeRecord(id,11,20.1,storage);
   expect(readArcadeRecord(id,storage)).toEqual({best:17,last:11,runs:2,bestSeconds:24});
  }
  expect(memory.size).toBe(3);
 });
});
