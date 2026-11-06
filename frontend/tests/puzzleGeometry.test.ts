import {describe,it,expect} from 'vitest';
import {pieceSegments,piecePath,trayRank} from '../src/lounge/puzzleGeometry';
describe('puzzle piece presentation',()=>{
 it('uses curved tabs inside the grid and straight outer boundaries',()=>{
  const corner=pieceSegments(0,6,4,160,150),middle=pieceSegments(7,6,4,160,150);
  expect(corner.filter(s=>s.kind==='C')).toHaveLength(4);
  expect(middle.filter(s=>s.kind==='C')).toHaveLength(8);
  expect(corner[1]).toEqual({kind:'L',values:[160,0]});
  expect(piecePath(7,6,4,160,150)).toMatch(/^M0 0.*Z$/);
 });
 it('keeps complementary horizontal tabs on exactly the same boundary',()=>{
  // The bottom of the upper piece and top of the lower piece share their normal displacement.
  const upper=pieceSegments(0,6,4,160,150).filter(s=>s.kind==='C');
  const lower=pieceSegments(6,6,4,160,150).filter(s=>s.kind==='C');
  const bottomCenter=upper[2].values.slice(-2),topCenter=lower[0].values.slice(-2);
  expect(bottomCenter[0]).toBe(topCenter[0]);
  expect(bottomCenter[1]).toBeCloseTo(topCenter[1]+150);
 });
 it('has a stable mixed order scoped to the saved game',()=>{
  const ids=Array.from({length:24},(_,i)=>i),order=[...ids].sort((a,b)=>trayRank(a,'table-a')-trayRank(b,'table-a'));
  expect(new Set(order).size).toBe(24);expect(order).not.toEqual(ids);
  expect(trayRank(3,'table-a')).toBe(trayRank(3,'table-a'));expect(trayRank(3,'table-a')).not.toBe(trayRank(3,'table-b'));
 });
});
