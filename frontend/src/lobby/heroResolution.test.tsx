import {describe,expect,it} from 'vitest';
import {heroAtlasPlaneCount,heroAtlasScale} from './heroResolution';
import {renderToStaticMarkup} from 'react-dom/server';
import {createElement} from 'react';
import {LobbyHero} from './LobbyHero';

describe('high-DPI hero atlas enhancement',()=>{
 it('keeps the three physically separate parallax planes in one sheet',()=>{
  expect(heroAtlasPlaneCount()).toBe(3);
  expect(heroAtlasScale()).toBe(2);
  const markup=renderToStaticMarkup(createElement(LobbyHero,{de:false,motionEnabled:false}));
  expect(markup).toContain('lobby-hero-layer-back');
  expect(markup).toContain('lobby-hero-layer-mid');
  expect(markup).toContain('lobby-hero-layer-front');
 });
});
