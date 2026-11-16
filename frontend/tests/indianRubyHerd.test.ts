import {describe,expect,it} from 'vitest';
import {herdPosition} from '../src/indianRuby/rubyHerdScene';
import {bonusStartPause} from '../src/indianRuby/reelMotion';

const members=[0,1,2,3,4];

describe('Sunset Ruby bonus herd',()=>{
 it.each(['normal','fast','turbo'] as const)('crosses the reel window once at %s speed',speed=>{
  const duration=bonusStartPause(speed)/1000;
  for(const width of [300,900,1550]){
   for(const member of members){
    const at=(fraction:number)=>herdPosition('intro',member,width,330,duration*fraction,duration);
    expect(at(0).x).toBeLessThan(0);
    expect(at(.5).x).toBeGreaterThan(0);
    expect(at(.5).x).toBeLessThan(width);
    expect(at(1).x).toBeGreaterThan(width);
    expect(at(2)).toEqual(at(1));
   }
  }
 });

 it('starts in view across the landscape and keeps moving through the respins',()=>{
  const width=1920,height=240;
  const at=(age:number)=>members.map(i=>herdPosition('background',i,width,height,age,2.45));
  const entering=at(0);
  expect(entering.every(({x})=>x>0&&x<width)).toBe(true);
  expect(new Set(entering.map(({scale})=>scale)).size).toBe(members.length);
  expect(at(1).some(({x})=>x>0&&x<width)).toBe(true);
  expect(at(2).some(({x},i)=>x>0&&x<width&&x!==entering[i].x)).toBe(true);
  expect(at(12).some(({x})=>x>0&&x<width)).toBe(true);
 });
});
