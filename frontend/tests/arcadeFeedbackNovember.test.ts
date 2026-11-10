import {describe,expect,it} from 'vitest';
import {
 ARCADE_STEP,BLADE_IMPACT_ANGLE,BLADE_HIT_ARC,actArcadeRun,
 angularDistance,createArcadeRun,stepArcadeRun,
} from '../src/lobby/arcadeEngine';
import type {BladeRun} from '../src/lobby/arcadeEngine';
import type {CircuitRun,MinerRun} from '../src/lobby/arcadeExpansionTwoEngine';
import type {EchoRun} from '../src/lobby/arcadeExpansionOneEngine';
import {beatsMelodyFrequencies,playBeatsGroovePulse} from '../src/lobby/arcadeBeatsAudio';
import {createBeatsRun,beatsTargetTime,stepBeatsRun} from '../src/lobby/arcadeBeatsEngine';
import {soundNotes} from '../src/lobby/arcadeSound';
import {drawArcadeRun} from '../src/lobby/arcadeRenderer';

describe('Arcade November feedback regressions',()=>{
 it('uses exactly the visible six-o-clock blade entry point for collisions',()=>{
  expect(BLADE_IMPACT_ANGLE).toBeCloseTo(Math.PI/2,9);
  expect(angularDistance(BLADE_IMPACT_ANGLE,BLADE_IMPACT_ANGLE)).toBeLessThan(BLADE_HIT_ARC);
  const blocked=createArcadeRun('blade-timing',17) as BladeRun;
  blocked.rotation=0;blocked.blades=[BLADE_IMPACT_ANGLE];
  expect(actArcadeRun(blocked)).toBe('fail');
  const clear=createArcadeRun('blade-timing',17) as BladeRun;
  clear.rotation=0;clear.blades=[-Math.PI/2];
  expect(actArcadeRun(clear)).toBe('score');
 });
 it('provides recognizably separate jump, grapple, mining, and win cues',()=>{
  expect(soundNotes('spike-rush','jump')).not.toEqual(soundNotes('spike-rush','flap'));
  expect(soundNotes('phantom-hook','hook')).not.toEqual(soundNotes('phantom-hook','release'));
  expect(soundNotes('meteor-miner','mineTick')).not.toEqual(soundNotes('meteor-miner','drill'));
  expect(soundNotes('prism-lock','victory')).not.toEqual(soundNotes('prism-lock','solve'));
  expect(soundNotes('prism-lock','victory').length).toBeGreaterThan(3);
 });
 it('moves exactly one clicked circuit tile through a bounded ninety-degree turn',()=>{
  const run=createArcadeRun('circuit-rush',13) as CircuitRun;
  // Keep this test independent of random network topology and hazard locations.
  for(const tile of run.tiles)tile.mask=0;
  const chosen=run.tiles.find(t=>t.c===2&&t.r===0)!;
  chosen.mask=1;
  expect(actArcadeRun(run)).toBe('flap');
  // The cursor starts at 2,2; that is the only rotated tile.
  expect(run.turnCell).toEqual({c:2,r:2});
  expect(run.turnClock).toBeGreaterThan(0);
  expect(run.turnClock).toBeLessThanOrEqual(.18);
  for(let i=0;i<12;i++)stepArcadeRun(run,ARCADE_STEP);
  expect(run.turnClock).toBe(0);
  expect(run.turnCell).toBeNull();
 });
 it('plays the visible four chart pitches, never an unrelated note chart',()=>{
  expect(beatsMelodyFrequencies([0,1,2,3])).toEqual([293.66,349.23,440,523.25]);
  expect(beatsMelodyFrequencies([3,1,99,-1,1.5])).toEqual([523.25,349.23]);
  const run=createBeatsRun(42);
  for(let i=0;i<155;i++)stepBeatsRun(run,ARCADE_STEP);
  const note=run.notes.find(n=>n.judgement===null)!;
  expect(note.at).toBeCloseTo(beatsTargetTime(Math.floor(note.id/4)),7);
  expect(beatsMelodyFrequencies([note.lane])).toHaveLength(1);
  const ref:{current:AudioContext|null}={current:null};
  expect(()=>playBeatsGroovePulse(ref,false,8,104,[note.lane])).not.toThrow();
  expect(ref.current).toBeNull();
 });
 it('preserves the deterministic Echo and Meteor gameplay while feedback changes',()=>{
  const echo=createArcadeRun('echo-sequence',19) as EchoRun;
  expect(echo.phase).toBe('watch');
  expect(echo.sequence).toHaveLength(1);
  const miner=createArcadeRun('meteor-miner',8) as MinerRun;
  const rock=miner.rocks[0];
  miner.x=rock.x+rock.r+45;miner.y=rock.y;
  miner.vx=0;miner.vy=0;
  for(let i=0;i<70;i++)stepArcadeRun(miner,ARCADE_STEP,true);
  expect(miner.cargo).toBeGreaterThan(0);
 });
 it('can paint every retouched arcade canvas mode without throwing',()=>{
  const gradient={addColorStop:()=>{}};
  const noOp=()=>{};
  const ctx=new Proxy({
   createLinearGradient:()=>gradient,
   createRadialGradient:()=>gradient,
   measureText:()=>({width:80}),
  } as Record<string,unknown>,{
   get:(obj,key)=>obj[String(key)]??noOp,
   set:(obj,key,value)=>{obj[String(key)]=value;return true;},
  }) as unknown as CanvasRenderingContext2D;
  for(const id of ['spike-rush','blade-timing','phantom-hook','neon-drift',
   'echo-sequence','prism-lock','meteor-miner','circuit-rush',
   'shadow-heist','pulse-perfect','midnight-beats'] as const){
    expect(()=>drawArcadeRun(ctx,createArcadeRun(id,17))).not.toThrow();
  }
 });
});
