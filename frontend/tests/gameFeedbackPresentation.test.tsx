import {createElement} from 'react';
import {renderToStaticMarkup} from 'react-dom/server';
import {describe,expect,it} from 'vitest';
import {PuzzleCompletion} from '../src/lounge/PuzzleCompletion';
import {DiceCube} from '../src/dice/DiceCube';
import type {DicePalette} from '../src/dice/bridge';

const palette:DicePalette={top:'#f5ece0',bottom:'#aa9781',pips:'#1c2930',edge:'#3c3934',glow:null,engraved:true};
describe('Lounge completion screens',()=>{
 const dialog=()=>renderToStaticMarkup(createElement(PuzzleCompletion,{
  testId:'word-search-completion',kind:'words',title:'RÄTSEL GELÖST',
  subtitle:'Alle Wörter gefunden.',metrics:[
   {label:'WÖRTER',value:12},{label:'ZEIT',value:'02:34'},{label:'HINWEISE',value:0},
  ],
  rewardXp:50,seasonXp:25,rewardNotice:'Saved',xpLabel:'SPIELER-XP',
  seasonLabel:'SAISON-XP',viewLabel:'SPIELFELD ANSEHEN',backLabel:'ZUR AUSWAHL',
  onView:()=>{},onBack:()=>{},animated:false,
 }));
 it('displays a prominent bilingual-ready modal instead of a footer box',()=>{
  const markup=dialog();
  expect(markup).toContain('data-testid="word-search-completion"');
  expect(markup).toContain('role="dialog"');
  expect(markup).toContain('aria-modal="true"');
  expect(markup).toContain('RÄTSEL GELÖST');
  expect(markup).toContain('SPIELFELD ANSEHEN');
  expect(markup).toContain('ZUR AUSWAHL');
  expect(markup).toContain('+50');
  expect(markup).toContain('+25');
  expect(markup).toContain('02:34');
  expect(markup).toContain('is-static');
 });
});
describe('Midnight Dice 3D throw',()=>{
 it('renders all six real pip faces while showing the committed result',()=>{
  const markup=renderToStaticMarkup(createElement(DiceCube,{
   value:6,palette,rolling:true,index:2,token:5,
  }));
  expect(markup.match(/class="md-cube-face/g)??[]).toHaveLength(6);
  expect(markup).toContain('md-face-front');
  expect(markup).toContain('md-face-back');
  expect(markup).toContain('md-face-right');
  expect(markup).toContain('md-face-left');
  expect(markup).toContain('md-face-top');
  expect(markup).toContain('md-face-bottom');
  expect(markup).toContain('--die-final-y:180deg');
  expect(markup).toContain('--die-tilt-x:');
  expect(markup).toContain('md-cube-face-material');
  expect(markup).toContain('md-cube-bevel-light');
  expect(markup).toContain('is-rolling');
 });
 it('does not animate held dice or alter pip colours',()=>{
  const markup=renderToStaticMarkup(createElement(DiceCube,{
   value:3,palette,rolling:false,index:0,token:1,
  }));
  expect(markup).toContain('--die-final-x:-90deg');
  expect(markup).not.toContain('is-rolling');
  expect(markup).toContain('#1c2930');
 });
});
