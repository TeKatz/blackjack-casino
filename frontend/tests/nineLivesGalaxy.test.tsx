import {describe,expect,it} from 'vitest';
import {createElement} from 'react';
import {renderToStaticMarkup} from 'react-dom/server';
import {GalaxyBackdrop,galaxySignature} from '../src/nineLives/GalaxyBackdrop';
import {ReelStage} from '../src/nineLives/ReelStage';
import type {NineLivesSymbol} from '../src/nineLives/bridge';

describe('Nine Lives nine unique celestial reel cells',()=>{
 it('creates nine separate, stable star maps and independent cosmic landmarks',()=>{
  const fingerprints=Array.from({length:9},(_,cell)=>galaxySignature(cell));
  expect(new Set(fingerprints).size).toBe(9);
  for(let cell=0;cell<9;cell++)
   expect(galaxySignature(cell)).toBe(fingerprints[cell]);
  const sceneNames=Array.from({length:9},(_,cell)=>{
   const html=renderToStaticMarkup(createElement(GalaxyBackdrop,{cell}));
   expect(html).toContain(`data-galaxy-cell="${cell}"`);
   expect(html).not.toContain('repeating-linear-gradient');
   expect(html).not.toContain('nine-cell-nightlights');
   return html.match(/data-galaxy-name="([^"]+)"/)?.[1];
  });
  expect(new Set(sceneNames).size).toBe(9);
 });
 it('places every galaxy behind the original symbol without any overlaying star layers',()=>{
  const displayGrid: NineLivesSymbol[]=[
   'BlackCat','Yarn','Fishbone',
   'WhiteCat','Bonus','Bell',
   'Fishbone','TabbyCat','Wild',
  ];
  const html=renderToStaticMarkup(createElement(ReelStage,{
   displayGrid,result:null,spinning:[],activeWin:false,speed:'normal',
   reducedMotion:false,onStop:()=>{},
  }));
  expect(html.match(/class="nine-galaxy-scene"/g)).toHaveLength(9);
  expect(html.match(/class="nine-symbol-art"/g)).toHaveLength(9);
  for(let cell=0;cell<9;cell++){
   const bg=html.indexOf(`data-galaxy-cell="${cell}"`);
   expect(bg).toBeGreaterThan(-1);
   expect(html.slice(bg).indexOf('class="nine-symbol-art"')).toBeGreaterThan(0);
  }
  expect(html).toContain('data-symbol="BlackCat"');
  expect(html).toContain('data-galaxy-name="Amethyst Rift"');
 });
});
