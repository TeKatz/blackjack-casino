import {describe,it,expect} from 'vitest';
import {ArcadeHeldKeys,arcadeDirection} from '../src/lobby/arcadeControls';
import {BLADE_HIT_ARC,ARCADE_STEP,actArcadeRun,angularDistance,createArcadeRun,stepArcadeRun} from '../src/lobby/arcadeEngine';
import type {SpikeRun,AscentRun,StackRun,BladeRun,BreakerRun} from '../src/lobby/arcadeEngine';

describe('Arcade controls: reliable simultaneous keys and dynamic steering',()=>{
 it('recognizes physical WASD and arrow keys, with character fallback',()=>{
  expect(arcadeDirection('KeyA','a')).toBe('left');
  expect(arcadeDirection('KeyD','d')).toBe('right');
  expect(arcadeDirection('ArrowUp','ArrowUp')).toBe('up');
  expect(arcadeDirection('','s')).toBe('down');
  expect(arcadeDirection('','ArrowLeft')).toBe('left');
  expect(arcadeDirection('Space',' ')).toBeNull();
 });
 it('does not lose a direction when its other physical key is released',()=>{
  const input=new ArcadeHeldKeys();
  input.press('KeyA','a');
  input.press('ArrowLeft','ArrowLeft');
  expect(input.controls().left).toBe(true);
  input.release('KeyA','a');
  expect(input.controls().left).toBe(true);
  input.release('ArrowLeft','ArrowLeft');
  expect(input.controls().left).toBe(false);
 });
 it('keeps the remaining direction after one opposite key is released',()=>{
  const input=new ArcadeHeldKeys();
  input.press('KeyA','a');input.press('KeyD','d');
  expect(input.controls().left).toBe(true);
  expect(input.controls().right).toBe(true);
  input.release('KeyA','a');
  expect(input.controls()).toMatchObject({left:false,right:true});
  input.clear();
  expect(input.controls()).toEqual({left:false,right:false,up:false,down:false});
 });
 it('can steer Neon Breaker with keyboard immediately after mouse-controlled frames',()=>{
  const run=createArcadeRun('neon-breaker',17) as BreakerRun;
  const input=new ArcadeHeldKeys();
  stepArcadeRun(run,ARCADE_STEP,false,{pointerX:800});
  const afterMouse=run.paddleX;
  input.press('KeyA','a');
  for(let i=0;i<3;i++)stepArcadeRun(run,ARCADE_STEP,false,input.controls());
  expect(run.paddleX).toBeLessThan(afterMouse);
  const afterKeyboard=run.paddleX;
  stepArcadeRun(run,ARCADE_STEP,false,{pointerX:800});
  expect(run.paddleX).toBeGreaterThan(afterKeyboard);
  input.release('KeyA','a');
 });
 it('keeps movement in Hexagon Hell and Endless Ascent with mixed held keys',()=>{
  const input=new ArcadeHeldKeys();
  input.press('KeyD','d');input.press('ArrowRight','ArrowRight');
  const hex=createArcadeRun('hexagon-hell') ;
  const ascent=createArcadeRun('endless-ascent');
  input.release('KeyD','d');
  stepArcadeRun(hex,ARCADE_STEP,false,input.controls());
  stepArcadeRun(ascent,ARCADE_STEP,false,input.controls());
  expect(hex.kind==='hexagon-hell'&&hex.angle).toBeGreaterThan(0);
  expect(ascent.kind==='endless-ascent'&&ascent.x).toBeGreaterThan(480);
 });
});

describe('Arcade polish: challenge balance and truthful feedback',()=>{
 it('gives Spike Rush a shorter jump arc with a hard ceiling below 90 px',()=>{
  const run=createArcadeRun('spike-rush',2) as SpikeRun;
  run.obstacles=[];run.spawn=5;
  expect(actArcadeRun(run)).toBe('flap');
  let highest=run.feet;
  for(let i=0;i<43;i++){
   stepArcadeRun(run,ARCADE_STEP);
   highest=Math.min(highest,run.feet);
  }
  const height=440-highest;
  expect(height).toBeGreaterThan(54);
  expect(height).toBeLessThan(90);
  expect(run.grounded).toBe(true);
 });
 it('distinguishes real collision and safe jumps on a moving saw',()=>{
  const hit=createArcadeRun('spike-rush',7) as SpikeRun;
  hit.obstacles=[{x:200,width:46,type:'saw',passed:false,phase:0}];
  expect(stepArcadeRun(hit,ARCADE_STEP)).toBe('fail');
  const safe=createArcadeRun('spike-rush',7) as SpikeRun;
  safe.obstacles=[{x:200,width:46,type:'saw',passed:false,phase:0}];
  safe.feet=345;safe.vy=-80;safe.grounded=false;
  expect(stepArcadeRun(safe,ARCADE_STEP)).toBeNull();
  expect(safe.over).toBe(false);
 });
 it('makes tall spike towers dangerous until the runner clears their full height',()=>{
  const low=createArcadeRun('spike-rush',3) as SpikeRun;
  low.obstacles=[{x:200,width:43,type:'tower',passed:false}];
  low.feet=402;low.vy=-60;low.grounded=false;
  expect(stepArcadeRun(low,ARCADE_STEP)).toBe('fail');
  const high=createArcadeRun('spike-rush',3) as SpikeRun;
  high.obstacles=[{x:200,width:43,type:'tower',passed:false}];
  high.feet=350;high.vy=-30;high.grounded=false;
  expect(stepArcadeRun(high,ARCADE_STEP)).toBeNull();
 });
 it('launches higher from a spring and destroys fragile platforms after contact',()=>{
  const spring=createArcadeRun('endless-ascent',1) as AscentRun;
  spring.platforms=[{x:400,y:401,width:135,vx:0,kind:'spring'}];
  spring.feet=398;spring.vy=350;
  stepArcadeRun(spring,ARCADE_STEP);
  expect(spring.vy).toBe(-705);
  expect(spring.jumps).toBe(1);
  const fragile=createArcadeRun('endless-ascent',1) as AscentRun;
  fragile.platforms=[{x:400,y:401,width:135,vx:0,kind:'fragile'}];
  fragile.feet=398;fragile.vy=350;
  stepArcadeRun(fragile,ARCADE_STEP);
  expect(fragile.platforms[0].crumble).toBeCloseTo(.2);
  for(let i=0;i<15;i++)stepArcadeRun(fragile,ARCADE_STEP);
  expect(fragile.platforms.some(p=>p.kind==='fragile')).toBe(false);
 });
 it('makes the early moving platforms substantially faster',()=>{
  const ascent=createArcadeRun('endless-ascent',5) as AscentRun;
  const moving=ascent.platforms.filter(p=>p.vx!==0);
  expect(moving.length).toBeGreaterThanOrEqual(2);
  expect(moving.every(p=>Math.abs(p.vx)>=75)).toBe(true);
  const previous=ascent.platforms[1].x;
  stepArcadeRun(ascent,ARCADE_STEP);
  expect(ascent.platforms[1].x).not.toBe(previous);
 });
 it('keeps Perfect Stack exact placement and shows a timed drop event',()=>{
  const stack=createArcadeRun('perfect-stack') as StackRun;
  stack.movingX=340;
  expect(actArcadeRun(stack)).toBe('score');
  expect(stack.perfect).toBe(1);
  expect(stack.lastDropAt).toBe(0);
  expect(stack.lastDropX).toBe(480);
  expect(stack.lastDropWidth).toBe(280);
  expect(stack.blocks[1]).toEqual({x:340,width:280});
 });
 it('uses the exact Blade Timing collision threshold for the landing reticle',()=>{
  const run=createArcadeRun('blade-timing') as BladeRun;
  expect(BLADE_HIT_ARC).toBeGreaterThan(.1);
  expect(BLADE_HIT_ARC).toBeLessThan(.3);
  run.blades=[Math.PI/2-run.rotation];
  expect(angularDistance(run.blades[0]+run.rotation,Math.PI/2)).toBeLessThan(BLADE_HIT_ARC);
  expect(actArcadeRun(run)).toBe('fail');
 });
});
