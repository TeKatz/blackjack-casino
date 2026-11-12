import {describe,expect,it} from 'vitest';
import sudokuSvg from '../public/lounge/art/sudoku.svg?raw';
import wordSvg from '../public/lounge/art/word-search.svg?raw';
import jigsawSvg from '../public/lounge/art/jigsaw.svg?raw';
import defenseSvg from '../public/lounge/art/tower-defense.svg?raw';
import seasonViewText from '../src/lobby/ModernSeasons.tsx?raw';
import {actArcadeRun,angularDistance,BLADE_HIT_ARC,BLADE_IMPACT_ANGLE,createArcadeRun} from '../src/lobby/arcadeEngine';
import type {BladeRun} from '../src/lobby/arcadeEngine';
import {BLADE_PENETRATION,BLADE_RIM_RADIUS,bladePaintedTip} from '../src/lobby/bladeGeometry';

const assets:Record<string,string>={sudoku:sudokuSvg,'word-search':wordSvg,jigsaw:jigsawSvg,'tower-defense':defenseSvg};
const asset=(name:string)=>assets[name];
const seasonView=()=>seasonViewText;

describe('Screenshot feedback: exact visual alignment without game-logic changes',()=>{
 it('the aiming reticle projects the very same painted tip as a newly placed knife',()=>{
  expect(BLADE_PENETRATION).toBe(32);
  expect(BLADE_RIM_RADIUS).toBe(113);
  const cx=480,cy=236;
  const aiming=bladePaintedTip(cx,cy,BLADE_IMPACT_ANGLE);
  expect(aiming.x).toBeCloseTo(cx-BLADE_RIM_RADIUS+BLADE_PENETRATION,8);
  expect(aiming.y).toBeCloseTo(cy,8);
  for(const rotation of [-2.9,-1.5,0,.4,2.8]){
   const run=createArcadeRun('blade-timing',123) as BladeRun;
   run.rotation=rotation;
   run.blades=[];
   expect(actArcadeRun(run)).toBe('score');
   const renderedAngle=run.blades[0]+run.rotation;
   const landed=bladePaintedTip(cx,cy,renderedAngle);
   expect(landed.x).toBeCloseTo(aiming.x,8);
   expect(landed.y).toBeCloseTo(aiming.y,8);
   expect(angularDistance(renderedAngle,BLADE_IMPACT_ANGLE)).toBeLessThan(BLADE_HIT_ARC);
   // Existing engine collision criterion is intentionally unchanged.
   run.over=false;
   expect(actArcadeRun(run)).toBe('fail');
  }
 });
 it('clips the continuous Season Pass sheen to the actual percentage fill',()=>{
  const view=seasonView();
  expect(view).toContain('<span className="season-progress-fill" style={{width:progress+\'%\'}}>');
  expect(view).toContain('className="season-progress-sheen"');
 });
 it('centers each Sudoku digit inside a legal cell without repeats in its row or column',()=>{
  const art=asset('sudoku');
  expect(art).toContain('text-anchor="middle" dominant-baseline="middle"');
  const digits=[...art.matchAll(/<text x="(\d+)" y="(\d+)">(\d)<\/text>/g)]
   .map(m=>({x:Number(m[1]),y:Number(m[2]),n:Number(m[3])}));
  expect(digits).toHaveLength(29);
  for(const digit of digits){
   expect((digit.x-15)%30).toBe(0);
   expect((digit.y-15)%30).toBe(0);
   const row=(digit.y-15)/30,col=(digit.x-15)/30;
   expect(row).toBeGreaterThanOrEqual(0);expect(row).toBeLessThan(9);
   expect(col).toBeGreaterThanOrEqual(0);expect(col).toBeLessThan(9);
   expect(digits.filter(d=>d.y===digit.y&&d.n===digit.n)).toHaveLength(1);
   expect(digits.filter(d=>d.x===digit.x&&d.n===digit.n)).toHaveLength(1);
  }
 });
 it('rings precisely the MOONLIGHT row in the word search',()=>{
  const art=asset('word-search');
  const letters=[...art.matchAll(/<text x="([^"]+)" y="([^"]+)">([A-Z])<\/text>/g)]
   .map(m=>({x:Number(m[1]),y:Number(m[2]),letter:m[3]}));
  expect(letters).toHaveLength(63);
  const rowY=3.5*29.7;
  const word=letters.filter(p=>Math.abs(p.y-rowY)<.001).sort((a,b)=>a.x-b.x);
  expect(word.map(p=>p.letter).join('')).toBe('MOONLIGHT');
  const outline=art.match(/<ellipse cx="([^"]+)" cy="([^"]+)" rx="([^"]+)" ry="20"/);
  expect(outline).not.toBeNull();
  expect(Number(outline![1])).toBeCloseTo((word[0].x+word[8].x)/2,5);
  expect(Number(outline![2])).toBeCloseTo(rowY,5);
  expect(Number(outline![3])).toBeGreaterThan((word[8].x-word[0].x)/2);
 });
 it('renders contiguous shared jigsaw joints and a path with towers firing on enemies',()=>{
  const puzzle=asset('jigsaw');
  // One continuous horizontal interlocking boundary is shared by both rows.
  expect(puzzle).toContain('M160 140H196C190 128');
  expect(puzzle).toContain('M268 22V70');
  expect(puzzle).toContain('M376 22V67');
  const defense=asset('tower-defense');
  expect(defense).toContain('stroke-width="91"');
  expect(defense).toContain('stroke-width="79"');
  expect(defense).toContain('translate(175 184)');
  expect(defense).toContain('translate(365 78)');
  expect(defense).toContain('translate(293 135)');
  expect(defense).toContain('translate(489 163)');
  expect(defense).toContain('stroke-dasharray="2 12"');
 });
});
