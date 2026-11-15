import {describe,expect,it} from 'vitest';
import {createElement} from 'react';
import {renderToStaticMarkup} from 'react-dom/server';
import CasinoStackArt from '../src/empire/CasinoStackArt';
import BuildingArtwork from '../src/empire/BuildingArtwork';

describe('Casino Empire visuals',()=>{
  it('renders a detailed scalable table with individual chips, not a placeholder glyph',()=>{
    const html=renderToStaticMarkup(createElement(CasinoStackArt,{levels:0,clicks:0}));
    expect(html).toContain('ce-stack-art');
    expect(html).toContain('ce-felt');
    expect(html).toContain('ce-chip-disc');
    expect((html.match(/class="ce-chip-column"/g)??[]).length).toBe(3);
    expect((html.match(/class="ce-chip-disc"/g)??[]).length).toBeGreaterThan(10);
  });

  it('develops additional chip towers with the real saved building progress',()=>{
    const low=renderToStaticMarkup(createElement(CasinoStackArt,{levels:0,clicks:0}));
    const grown=renderToStaticMarkup(createElement(CasinoStackArt,{levels:30,clicks:4000}));
    expect((grown.match(/class="ce-chip-column"/g)??[]).length).toBe(5);
    expect((grown.match(/class="ce-chip-disc"/g)??[]).length)
      .toBeGreaterThan((low.match(/class="ce-chip-disc"/g)??[]).length);
  });

  it('uses packaged casino-room illustrations instead of symbol boxes',()=>{
    const hotel=renderToStaticMarkup(createElement(BuildingArtwork,{id:'cashier',tier:1}));
    const slots=renderToStaticMarkup(createElement(BuildingArtwork,{id:'slots',tier:2}));
    expect(hotel).toContain('empire-art/luxury-hotel.png');
    expect(slots).toContain('empire-art/rooms-atlas.png');
    expect(slots).toContain('ce-scene-tier-2');
  });
});
