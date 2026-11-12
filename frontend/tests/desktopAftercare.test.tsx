import {describe,expect,it} from 'vitest';
import {createElement} from 'react';
import {renderToStaticMarkup} from 'react-dom/server';
import {Die,DiceCube} from '../src/dice/DiceCube';
import {BackroomD20} from '../src/lounge/BackroomD20';
import {createExpansionTwoRun,rotateCircuit,rotatePorts,CIRCUIT_TURN_DURATION}
 from '../src/lobby/arcadeExpansionTwoEngine';
import {circuitTilePose} from '../src/lobby/circuitVisual';
import {ReelStage} from '../src/nineLives/ReelStage';
import type {NineLivesSymbol} from '../src/nineLives/bridge';

const palette={top:'#bcecd5',bottom:'#386477',pips:'#253d38',
 edge:'#d9c595',glow:'#afeac8',engraved:true};

describe('Desktop aftercare: render and input regression contracts',()=>{
 it('isolates paint server IDs per die and every rolling cube face',()=>{
  const html=renderToStaticMarkup(createElement('div',{},
   ...Array.from({length:5},(_,i)=>createElement(DiceCube,{
    key:i,value:i+1,palette,variant:'dice-astral-opal',
    rolling:i===0,index:i,token:3,
   }))));
  const ids=[...html.matchAll(/\sid="([^"]+)"/g)].map(x=>x[1]);
  expect(ids.length).toBeGreaterThan(30);
  expect(new Set(ids).size).toBe(ids.length);
  const refs=[...html.matchAll(/url\(#([^\)]+)\)/g)].map(x=>x[1]);
  expect(refs.length).toBeGreaterThan(30);
  for(const id of refs)expect(ids).toContain(id);
  // Repainting or switching skins does not change authoritative dice values.
  expect(renderToStaticMarkup(createElement(Die,{value:5,palette,variant:'dice-copper'})))
   .toContain('data-skin="dice-copper"');
 });
 it('starts mechanical Circuit Rush rotation at the previous port mask and ends exactly at its new mask',()=>{
  const run=createExpansionTwoRun('circuit-rush',51);
  if(run.kind!=='circuit-rush')throw new Error('Expected circuit run');
  const tile=run.tiles.find(t=>!t.danger)!;
  const initial=tile.mask;
  rotateCircuit(run,tile.c,tile.r);
  const final=tile.mask;
  expect(final).toBe(rotatePorts(initial));
  const start=circuitTilePose(run,tile);
  expect(start.turning).toBe(true);
  expect(start.mask).toBe(initial);
  expect(start.angle).toBeCloseTo(0,7);
  run.turnClock=CIRCUIT_TURN_DURATION/2;
  const middle=circuitTilePose(run,tile);
  expect(middle.angle).toBeCloseTo(Math.PI/4,7);
  run.turnClock=0;run.turnCell=null;
  const complete=circuitTilePose(run,tile);
  expect(complete.turning).toBe(false);
  expect(complete.mask).toBe(final);
  expect(complete.angle).toBe(0);
 });
 it('preserves the retained nine-cell grid until reel tape readiness; there is no implicit overlay before a spin',()=>{
  const grid:NineLivesSymbol[]=[
   'Yarn','Bell','BlackCat','WhiteCat','Yarn','Bell','Wild','Bonus','Fishbone',
  ];
  const html=renderToStaticMarkup(createElement(ReelStage,{
   displayGrid:grid,result:null,spinning:[],activeWin:false,speed:'normal',
   reducedMotion:false,onStop:()=>{},
  }));
  expect((html.match(/class="nine-cell"/g)||[]).length).toBe(9);
  expect(html).toContain('data-reel-state="settled"');
  expect(html).not.toContain('data-compositor-ready="true"');
 });
 it('renders a true projected icosahedron canvas for D20, never the old flat rotating decal',()=>{
  const html=renderToStaticMarkup(createElement(BackroomD20,{
   value:17,rolling:false,resolved:true,motion:true,
  }));
  expect(html).toContain('<canvas');
  expect(html).toContain('class="backroom-d20-stage"');
  expect(html).not.toContain('<svg');
 });
});
