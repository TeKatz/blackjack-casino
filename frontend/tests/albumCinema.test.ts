import {describe,expect,it} from 'vitest';
import {albumRevealDelay,albumRevealSound,packRuptureTime} from '../src/lobby/albumCinema';
import type {AlbumDraw} from '../src/lobby/albumBridge';
const draw=(rarity:AlbumDraw['rarity'],ultraRare=false):AlbumDraw=>({
 id:'velvet-club-1',name:'Ace of Night',number:1,rarity,
 ultraRare,themeId:'velvet-table',theme:'Velvet Table',accent:'#e5d49a',isNew:true,duplicatePoints:0,
});
describe('Receipt-backed pack opening presentation',()=>{
 it('dramatically paces rarer committed cards without touching a roll',()=>{
  const common=draw('Common'),rare=draw('Rare'),legendary=draw('Legendary'),ultra=draw('Legendary',true);
  expect(albumRevealDelay(0,common,true)).toBeGreaterThan(600);
  expect(albumRevealDelay(2,legendary,true)).toBeGreaterThan(albumRevealDelay(2,rare,true));
  expect(albumRevealDelay(2,ultra,true)).toBeGreaterThan(albumRevealDelay(2,legendary,true));
  expect(albumRevealDelay(2,common,false)).toBe(15);
  expect(packRuptureTime(false)).toBe(15);
 });
 it('adds special sounds only for existing drawn rarities',()=>{
  expect(albumRevealSound(draw('Common'))).toBe('flip');
  expect(albumRevealSound(draw('Rare'))).toBe('flip');
  expect(albumRevealSound(draw('Epic'))).toBe('rare');
  expect(albumRevealSound(draw('Legendary'))).toBe('rare');
  expect(albumRevealSound(draw('Legendary',true))).toBe('ultra');
 });
});
