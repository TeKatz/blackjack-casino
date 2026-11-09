import {describe,expect,it} from 'vitest';
import {createArcadeRun,actArcadeRun,stepArcadeRun,angularDistance,isInsideCave,
 PLAYABLE_ARCADE_IDS,isPlayableArcade,ARCADE_STEP} from '../src/lobby/arcadeEngine';
import type {WingsRun,CaveRun,StackRun,BladeRun} from '../src/lobby/arcadeEngine';
import {readArcadeRecord,saveArcadeRecord} from '../src/lobby/arcadeRecords';

describe('Arcade Update 1: deterministic playable engines',()=>{
 it('contains exactly the first four playable games',()=>{
  expect(PLAYABLE_ARCADE_IDS).toEqual([
   'midnight-wings','cave-escape','perfect-stack','blade-timing',
  ]);
  for(const id of PLAYABLE_ARCADE_IDS){
   expect(isPlayableArcade(id)).toBe(true);
   expect(createArcadeRun(id).kind).toBe(id);
  }
  expect(isPlayableArcade('hexagon-hell')).toBe(false);
  expect(isPlayableArcade('../missing')).toBe(false);
 });
 it('implements bird flap physics, pipe scoring and both collision edges',()=>{
  const bird=createArcadeRun('midnight-wings',123) as WingsRun;
  expect(actArcadeRun(bird)).toBe('flap');
  expect(bird.vy).toBeLessThan(0);
  const oldY=bird.y;
  stepArcadeRun(bird,ARCADE_STEP);
  expect(bird.y).toBeLessThan(oldY);
  bird.pipes=[{x:220,center:250,gap:180,passed:false}];
  bird.y=250;bird.vy=0;
  expect(stepArcadeRun(bird,ARCADE_STEP)).toBeNull();
  bird.y=80;
  expect(stepArcadeRun(bird,ARCADE_STEP)).toBe('fail');
  expect(bird.over).toBe(true);
  expect(actArcadeRun(bird)).toBeNull();
  const clean=createArcadeRun('midnight-wings',123) as WingsRun;
  clean.pipes=[{x:80,center:250,gap:200,passed:false}];
  expect(stepArcadeRun(clean,ARCADE_STEP)).toBeNull();
  expect(clean.score).toBe(1);
 });
 it('models a continuous cave corridor and pressure-to-rise controls',()=>{
  const rising=createArcadeRun('cave-escape',887) as CaveRun;
  const falling=createArcadeRun('cave-escape',887) as CaveRun;
  expect(isInsideCave(rising,225,270)).toBe(true);
  for(let i=0;i<10;i++){
   stepArcadeRun(rising,ARCADE_STEP,true);
   stepArcadeRun(falling,ARCADE_STEP,false);
  }
  expect(rising.y).toBeLessThan(270);
  expect(falling.y).toBeGreaterThan(270);
  expect(rising.score).toBeGreaterThan(0);
  expect(falling.score).toBeGreaterThan(0);
  falling.y=0;
  expect(stepArcadeRun(falling,ARCADE_STEP,false)).toBe('fail');
 });
 it('is seeded and reproducible across runs',()=>{
  const a=createArcadeRun('cave-escape',42) as CaveRun;
  const b=createArcadeRun('cave-escape',42) as CaveRun;
  for(let i=0;i<120;i++){
   const hold=i%40<20;
   stepArcadeRun(a,ARCADE_STEP,hold);
   stepArcadeRun(b,ARCADE_STEP,hold);
  }
  expect(a).toEqual(b);
 });
 it('trims imperfect stacks and fails when no block remains',()=>{
  const stack=createArcadeRun('perfect-stack',5) as StackRun;
  stack.movingX=340;
  expect(actArcadeRun(stack)).toBe('score');
  expect(stack.score).toBe(1);
  expect(stack.perfect).toBe(1);
  expect(stack.blocks[1]).toEqual({x:340,width:280});
  stack.movingX=390;
  expect(actArcadeRun(stack)).toBe('score');
  expect(stack.blocks[2].width).toBe(230);
  stack.movingX=0;
  expect(actArcadeRun(stack)).toBe('fail');
  expect(stack.over).toBe(true);
 });
 it('rotates the blade wheel and rejects collisions with embedded blades',()=>{
  const run=createArcadeRun('blade-timing',99) as BladeRun;
  expect(angularDistance(Math.PI-.1,-Math.PI+.1)).toBeCloseTo(.2);
  expect(actArcadeRun(run)).toBe('score');
  expect(run.score).toBe(1);
  expect(actArcadeRun(run)).toBe('fail');
  const next=createArcadeRun('blade-timing',22) as BladeRun;
  for(let i=0;i<60;i++)stepArcadeRun(next,ARCADE_STEP);
  expect(next.rotation).toBeGreaterThan(.5);
  // Simulate successful spaced throws to verify the next difficulty stage.
  next.blades=[];
  for(let i=0;i<5;i++){
   next.blades=[];
   actArcadeRun(next);
  }
  expect(next.stage).toBe(2);
  expect(next.needed).toBe(6);
  expect(next.hits).toBe(0);
  expect(next.score).toBe(5);
  expect(next.spin).toBeGreaterThan(0); // updated with the next simulation step
  stepArcadeRun(next,ARCADE_STEP);
  expect(next.spin).toBeLessThan(0);
 });
 it('does not advance a finished game or simulate overlong frames',()=>{
  const run=createArcadeRun('perfect-stack') as StackRun;
  run.over=true;
  expect(stepArcadeRun(run,3)).toBeNull();
  expect(run.elapsed).toBe(0);
  run.over=false;
  stepArcadeRun(run,3);
  expect(run.elapsed).toBeCloseTo(ARCADE_STEP);
 });
});

describe('Arcade personal record persistence',()=>{
 const createStore=()=>{
  const data=new Map<string,string>();
  return {
   getItem:(key:string)=>data.get(key)??null,
   setItem:(key:string,value:string)=>{data.set(key,value);},
   data,
  };
 };
 it('keeps independent highest scores, last score and run counts',()=>{
  const store=createStore();
  expect(readArcadeRecord('midnight-wings',store).best).toBe(0);
  saveArcadeRecord('midnight-wings',10,18.5,store);
  saveArcadeRecord('midnight-wings',7,35,store);
  saveArcadeRecord('cave-escape',2,9,store);
  expect(readArcadeRecord('midnight-wings',store)).toEqual({
   best:10,last:7,runs:2,bestSeconds:35,
  });
  expect(readArcadeRecord('cave-escape',store).best).toBe(2);
 });
 it('rejects malformed, huge or invalid stored data',()=>{
  const store=createStore();
  const key='midnight-club.arcade.v1.perfect-stack';
  store.setItem(key,'{"best":-4}');
  expect(readArcadeRecord('perfect-stack',store).best).toBe(0);
  store.setItem(key,'['+'x'.repeat(520)+']');
  expect(readArcadeRecord('perfect-stack',store).runs).toBe(0);
  expect(saveArcadeRecord('perfect-stack',Number.NaN,-1,store).best).toBe(0);
 });
 it('continues playing when browser storage is unavailable',()=>{
  const broken={getItem:(_key:string):string|null=>{throw Error('disabled');},
   setItem:(_key:string,_val:string):void=>{throw Error('full');}};
  expect(saveArcadeRecord('blade-timing',15,12,broken).best).toBe(15);
  expect(readArcadeRecord('blade-timing',broken).best).toBe(0);
 });
});
