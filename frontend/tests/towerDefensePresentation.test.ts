import {describe,expect,it} from 'vitest';
import {collectNewShots,enemySpeed,interpolateEnemy,positionOnPath,previewDamage,previewRange,shotKey}
 from '../src/lounge/towerDefenseVisuals';
import {BastionAudio} from '../src/lounge/towerDefenseAudio';

const path=[{x:0,y:300},{x:145,y:300},{x:145,y:130}];
const fx={kind:'cannon' as const,fromX:80,fromY:190,toX:145,toY:300,ticks:3};
describe('Midnight Bastion phase 2 presentation',()=>{
 it('reuses one projectile ID across all three native effect ticks',()=>{
  expect(shotKey(101,fx)).toBe(shotKey(102,{...fx,ticks:2}));
  expect(shotKey(101,fx)).toBe(shotKey(103,{...fx,ticks:1}));
  expect(shotKey(105,fx)).not.toBe(shotKey(101,fx));
  const seen=new Set<string>();
  expect(collectNewShots(101,[fx],seen)).toHaveLength(1);
  expect(collectNewShots(102,[{...fx,ticks:2}],seen)).toHaveLength(0);
  expect(collectNewShots(105,[fx],seen)).toHaveLength(1);
 });
 it('keeps cosmetic interpolation out of logical health/distance',()=>{
  const enemy={id:1,kind:'runner' as const,distance:25,hp:24,slow:0};
  const baseline=positionOnPath(path,25);
  const p=interpolateEnemy(enemy,path,.1,1,true);
  expect(p.x).toBeGreaterThan(baseline.x);
  expect(interpolateEnemy(enemy,path,999,1,false)).toEqual(baseline);
  expect(interpolateEnemy(enemy,path,1,2,true).x).toBeLessThan(45);
  expect(enemy.distance).toBe(25);
  expect(enemySpeed.brute).toBeLessThan(enemySpeed.runner);
 });
 it('matches existing C# upgrade damage and range',()=>{
  expect(previewDamage('archer',1)).toBe(22);
  expect(previewDamage('frost',3)).toBe(30);
  expect(previewDamage('cannon',2)).toBe(62);
  expect(previewRange('frost',1)).toBe(161);
  expect(previewRange('cannon',3)).toBe(178);
 });
 it('does not initialise audio without an unlocked user gesture',()=>{
  const audio=new BastionAudio();
  audio.setEnabled(false);
  expect(()=>audio.play('cannon')).not.toThrow();
  expect(()=>audio.dispose()).not.toThrow();
 });
});
