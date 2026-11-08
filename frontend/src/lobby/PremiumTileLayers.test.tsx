import {describe,it,expect} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {PremiumTileLayers} from './PremiumTileLayers';
import {ANIMATED_TILE_IDS,tileColors} from './TileFxEngine';

describe('premium animated lobby tiles',()=>{
 it('gives seven games independent visual signatures and color palettes',()=>{
  const scenes=ANIMATED_TILE_IDS.map(id=>{
   const html=renderToStaticMarkup(<PremiumTileLayers id={id} enabled/>);
   expect(html).toContain('data-tile-motion="'+id+'"');
   expect(html).toContain('data-running="true"');
   expect(html).toContain('tile-signature-'+id);
   expect(html).toContain('aria-hidden="true"');
   expect(tileColors(id).length).toBeGreaterThanOrEqual(3);
   return html;
  });
  expect(new Set(scenes).size).toBe(ANIMATED_TILE_IDS.length);
 });
 it('handles motion settings and avoids duplicates over other game key art',()=>{
  const reduced=renderToStaticMarkup(<PremiumTileLayers id="neon-vault" enabled={false}/>);
  expect(reduced).toContain('data-running="false"');
  expect(renderToStaticMarkup(<PremiumTileLayers id="blackjack" enabled/>)).toBe('');
  expect(renderToStaticMarkup(<PremiumTileLayers id="midnight-merge" enabled/>)).toBe('');
 });
});
