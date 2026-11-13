import {describe,it,expect} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {LoungePreview} from '../src/lounge/LoungePreview';
import {MemoryIllustration} from '../src/lounge/MemoryIllustration';
import {BackroomItemIcon} from '../src/lounge/BackroomItemIcon';
import {loungeIds} from '../src/lounge/bridge';
import {galaxySignature} from '../src/nineLives/GalaxyBackdrop';
import {Die,DiceCube} from '../src/dice/DiceCube';
import {DICE_SET_SKINS,diceSkin} from '../src/dice/diceSkins';
import {createExpansionTwoRun,rotateCircuit} from '../src/lobby/arcadeExpansionTwoEngine';

const palette={top:'#e0b38b',bottom:'#87503b',pips:'#302a29',
 edge:'#f2ce9e',glow:null,engraved:true};
describe('Quality package 3 visual contracts',()=>{
 it('gives each of the eight Lounge games its own vector illustration',()=>{
  const paths=loungeIds.map(id=>{
   const html=renderToStaticMarkup(<LoungePreview id={id}/>);
   expect(html).toContain('src="/lounge/art/'+id+'.svg"');
   expect(html).toContain('aria-hidden="true"');
   return html;
  });
  expect(new Set(paths).size).toBe(8);
  expect(paths.find(x=>x.includes('tower-defense'))).toContain('lounge-bastion-preview');
  expect(paths.find(x=>x.includes('minesweeper'))).toContain('lounge-sweep-preview');
 });
 it('renders SVG material illustrations rather than platform emoji on both Memory decks',()=>{
  for(const [deck,names] of [
   ['animals',['Cat','Dog','Rabbit','Butterfly','Whale','Snake','Penguin','Crab']],
   ['fruits',['Apple','Banana','Grapes','Strawberry','Pineapple','Cupcake','Coffee','Cheese']]
  ] as const){
   for(const name of names){
    const html=renderToStaticMarkup(<MemoryIllustration deck={deck} name={name} accent="#bd8459"/>);
    expect(html).toContain('class="memo-illustration"');
    expect(html).toContain('aria-label="'+name+'"');
    expect(html).toContain('<path');
   }
  }
 });
 it('links all thirteen cosmetic D6 sets to recognizable material styling',()=>{
  expect(Object.keys(DICE_SET_SKINS)).toHaveLength(13);
  const htmls=Object.keys(DICE_SET_SKINS).map(variant=>{
   const html=renderToStaticMarkup(<DiceCube value={6} palette={palette}
    rolling={false} index={0} token={1} variant={variant}/>);
   expect(html).toContain('data-skin="'+variant+'"');
   expect(html).toContain('data-material="'+diceSkin(variant).surface+'"');
   expect(html).toContain('md-die-flat-face');
   expect(html).not.toContain('md-die-roll-3d');
   return html;
  });
  expect(new Set(htmls).size).toBe(13);
  expect(renderToStaticMarkup(<Die value={5} palette={palette} variant="dice-copper"/>))
   .toContain('data-skin="dice-copper"');
 });
 it('keeps the Nine Lives backdrop uniform while retaining individual celestial signatures',()=>{
  expect(galaxySignature(0).split('|')[0]).not.toBe(galaxySignature(1).split('|')[0]);
  expect(galaxySignature(0)).not.toBe(galaxySignature(1));
 });
 it('provides usable miniature inventory icons for all native item kinds',()=>{
  for(const kind of ['Quest','Consumable','Utility','Relic']){
   const html=renderToStaticMarkup(<BackroomItemIcon kind={kind} name="Example"/>);
   expect(html).toContain('class="backroom-inventory-icon"');
   expect(html).toContain('<path');
  }
 });
 it('starts a visually eased Circuit Rush tile turn without changing puzzle mechanics',()=>{
  const run=createExpansionTwoRun('circuit-rush',731);
  if(run.kind!=='circuit-rush')throw new Error('Expected Circuit Rush');
  const tile=run.tiles[0];
  rotateCircuit(run,tile.c,tile.r);
  expect(run.turnClock).toBe(.32);
  expect(run.turnCell).toEqual({c:tile.c,r:tile.r});
 });
});
