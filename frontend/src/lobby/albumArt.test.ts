import {describe,it,expect} from 'vitest';
import manifest from './albumArtManifest.json';
import {albumArtStyle,hasAlbumArt} from './albumArt';
import {isAlbum,isAlbumResult} from './albumBridge';
import type {AlbumSnapshot} from './albumBridge';

describe('Original Collector Album atlas',()=>{
 it('maps each of the 136 stable cards to exactly one original full-resolution crop',()=>{
  expect(manifest.Cards).toHaveLength(136);
  expect(manifest.Atlases).toHaveLength(16);
  expect(new Set(manifest.Cards.map(c=>c.Id)).size).toBe(136);
  for(const card of manifest.Cards){
   expect(hasAlbumArt(card.Id)).toBe(true);
   const style=albumArtStyle(card.Id);
   expect(style.backgroundImage).toContain('/album/atlas/'+card.Atlas+'.png');
   expect(style.backgroundSize).toContain('%');
   expect(style.backgroundPosition).toContain('%');
  }
  expect(hasAlbumArt('../../secret')).toBe(false);
  expect(albumArtStyle('../../secret')).toEqual({});
 });
 it('validates catalogue-backed native snapshots and one pending five-card receipt',()=>{
  const themes=manifest.Atlases.map(atlas=>{
   const cards=manifest.Cards.filter(card=>card.Atlas===atlas.Id).map(card=>({
    id:card.Id,name:card.Title,number:Number(card.Id.slice(-2)),rarity:'Common' as const,
    owned:false,lore:'',
   }));
   return{id:atlas.Id,name:atlas.Id,symbol:'♠',accent:'#efc681',ultraRare:false,
    owned:0,total:cards.length,rewardReady:false,rewardClaimed:false,
    rewardCoins:300,rewardBankroll:'$2,500.00',rewardPack:'Rare' as const,cards};
  });
  const base:AlbumSnapshot={
   totalCards:136,ownedCards:0,completedThemes:0,packsOpened:0,duplicatePoints:0,
   duplicatePackCost:800,coins:'100',packs:['Common','Uncommon','Rare','Epic','Legendary'].map(rarity=>({
    rarity:rarity as AlbumSnapshot['packs'][number]['rarity'],price:100,owned:3,
    guaranteed:rarity as AlbumSnapshot['packs'][number]['rarity'],ultraRareChance:.01,
   })),themes,pending:null,
  };
  expect(isAlbum(base)).toBe(true);
  const sample=manifest.Cards.slice(0,5);
  const opening={
   id:'0123456789abcdef0123456789abcdef',rarity:'Common' as const,pointsEarned:5,
   packsEarned:0,newCards:4,cards:sample.map((c,i)=>({
    id:c.Id,name:c.Title,number:i+1,rarity:'Common' as const,themeId:c.Atlas,
    theme:c.Atlas,accent:'#eecc99',ultraRare:false,isNew:i!==4,duplicatePoints:i===4?5:0,
   })),
  };
  expect(isAlbum({...base,pending:opening})).toBe(true);
  expect(isAlbum({...base,pending:{...opening,id:'not-a-guid'}})).toBe(false);
  expect(isAlbum({...base,pending:{...opening,newCards:5}})).toBe(false);
  expect(isAlbum({...base,pending:{...opening,cards:opening.cards.slice(0,4)}})).toBe(false);
  expect(isAlbumResult({operation:'complete',id:opening.id,success:true,message:'Saved'})).toBe(true);
  expect(isAlbumResult({operation:'reroll',id:opening.id,success:true,message:'No'})).toBe(false);
 });
});
