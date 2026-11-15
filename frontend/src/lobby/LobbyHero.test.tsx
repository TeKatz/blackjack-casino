import {describe,it,expect} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {createElement} from 'react';
import {LobbyHero} from './LobbyHero';

describe('independent banner art plates',()=>{
  it('renders one background, one transparent midground and one transparent foreground',()=>{
    const html=renderToStaticMarkup(createElement(LobbyHero,{de:false,motionEnabled:true}));
    expect(html).toContain('lobby-hero-layer-back');
    expect(html).toContain('lobby-hero-layer-mid');
    expect(html).toContain('lobby-hero-layer-front');
    // Keep the non-animated static fallback on the first server/client paint.
    expect(html).toContain('class="lobby-hero"');
    expect(html).not.toContain('data-art-ready');
    expect(html).not.toContain('lobby-hero-picture');
    expect(html).not.toContain('lobby-hero-foreground');
  });
});
