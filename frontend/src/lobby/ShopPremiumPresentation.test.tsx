import {describe,it,expect} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {ShopShowcase} from './ShopShowcase';
import {previewShop} from './shopBridge';
import {shopMaterial,shopVisualTier,shopFxCount} from './ShopVisualModel';
import type {ShopItem} from './shopBridge';

const item=(kind:ShopItem['kind'],variant:string,price=6500):ShopItem=>({
 id:'test-'+kind.toLowerCase(),name:'Test Cosmetic',description:'Preview only',kind,
 visualVariant:variant,price,shopAvailable:true,requiredPrestige:0,
 owned:false,equipped:false,canBuy:true,
});

describe('premium shop object materials',()=>{
 it('renders one unique material treatment per category without affecting shop ownership',()=>{
  const samples=[
   item('CardSet','card-celestial-opal'),
   item('DiceSet','dice-obsidian'),
   item('Portrait','portrait-eclipse-raven'),
   item('Frame','dynasty-crown'),
  ];
  const html=samples.map(entry=>renderToStaticMarkup(<ShopShowcase item={entry}/>));
  expect(new Set(html).size).toBe(4);
  for(const output of html){
   expect(output).toContain('class="cosmetic-visual premium-showcase');
   expect(output).toContain('class="showcase-object"');
   expect(output).toContain('data-shop-fx="test-');
  }
  expect(html[0]).toContain('card-back-linework');
  expect(html[1]).toContain('cosmetic-d6');
  expect(html[2]).toContain('portrait-image-rim');
  expect(html[3]).toContain('frame-gems');
  expect(html[3]).toContain('frame-orbit');
 });
 it('distinguishes materials beyond color variations and limits classic effects',()=>{
  expect(shopMaterial('CardSet','card-art-deco')).toBe('deco');
  expect(shopMaterial('CardSet','card-celestial-opal')).toBe('celestial');
  expect(shopMaterial('DiceSet','dice-obsidian')).toBe('obsidian');
  expect(shopMaterial('DiceSet','dice-aurora')).toBe('aurora');
  expect(shopMaterial('Frame','neon-pagoda')).toBe('neon');
  expect(shopMaterial('Frame','eclipse-halo')).toBe('moonlit');
  expect(shopVisualTier(item('Frame','static-slate',100))).toBe('classic');
  expect(shopVisualTier(item('Frame','dynasty-crown'))).toBe('showpiece');
  expect(shopFxCount('Frame','showpiece')).toBeGreaterThan(shopFxCount('Frame','classic'));
  expect(shopFxCount('CardSet','signature')).toBeGreaterThan(shopFxCount('Title','classic'));
 });
 it('supports representative native preview data',()=>{
  for(const entry of previewShop.items){
   const html=renderToStaticMarkup(<ShopShowcase item={entry}/>);
   expect(html).toContain('data-shop-kind="'+entry.kind+'"');
   expect(html).toContain('data-shop-fx="'+entry.id+'"');
  }
 });
});
