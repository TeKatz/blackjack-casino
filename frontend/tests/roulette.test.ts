import {describe,it,expect} from 'vitest';
import {WHEEL,RED,covered,insideKey,settlePreview,pocketAngle} from '../src/lobby/rouletteModel';
describe('European roulette',()=>{
 it('has all 37 pockets and 18 red numbers',()=>{expect(new Set(WHEEL).size).toBe(37);expect(RED.size).toBe(18);expect(pocketAngle(0)).toBe(-Math.PI/2);});
 it('pays straight 35:1 net and zero loses outside',()=>{expect(settlePreview('x',[{key:'n0',amount:100},{key:'red',amount:100}],1000,0)).toMatchObject({success:true,return:3600,net:3400,bankroll:4400});});
 it('validates adjacent combinations',()=>{expect(insideKey([1,2])).toBe('i1,2');expect(insideKey([3,4])).toBeNull();expect(insideKey([0,1,2,3])).toBe('i0,1,2,3');expect(covered('i1,2,4,5')).toEqual([1,2,4,5]);});
 it('rejects invalid stakes and duplicate bets',()=>{expect(settlePreview('x',[{key:'red',amount:101000}],200000,1).success).toBe(false);expect(settlePreview('x',[{key:'red',amount:100},{key:'red',amount:100}],1000,1).success).toBe(false);});
});
