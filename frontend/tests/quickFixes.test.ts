import {describe,expect,it} from 'vitest';
import {createExpansionRun,driftCenter,driftRoadHalfWidth,stepExpansionRun}
 from '../src/lobby/arcadeExpansionOneEngine';

describe('Paket 1: Neon Drift without automatic red traffic',()=>{
 it('does not inflict automatic traffic damage while driving in the former center lane',()=>{
  const run=createExpansionRun('neon-drift');
  if(run.kind!=='neon-drift')throw new Error('Expected Neon Drift');
  run.distance=780;
  run.x=driftCenter(run.distance);
  run.vx=0;
  expect(stepExpansionRun(run,1/60)).toBeNull();
  expect(run.hits).toBe(0);
  expect(run.over).toBe(false);
 });
 it('still penalizes leaving the drivable road',()=>{
  const run=createExpansionRun('neon-drift');
  if(run.kind!=='neon-drift')throw new Error('Expected Neon Drift');
  run.distance=400;
  run.x=driftCenter(run.distance)+driftRoadHalfWidth(run.distance)+50;
  expect(stepExpansionRun(run,1/60)).toBe('stage');
  expect(run.hits).toBe(1);
  expect(run.over).toBe(false);
 });
});
