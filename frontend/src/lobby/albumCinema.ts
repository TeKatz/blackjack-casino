import type {AlbumDraw} from './albumBridge';
import type {AlbumCue} from './albumAudio';

/** Rarity changes only the spectacle; outcomes are committed by C# before it runs. */
export function albumRevealDelay(index:number,card:AlbumDraw|undefined,animations:boolean):number{
 if(!animations)return 15;
 const suspense=card?.ultraRare?530:card?.rarity==='Legendary'?430:
  card?.rarity==='Epic'?260:card?.rarity==='Rare'?145:0;
 return (index===0?670:335+index*60)+suspense;
}
export function albumRevealSound(card:AlbumDraw):AlbumCue{
 return card.ultraRare?'ultra':card.rarity==='Legendary'||card.rarity==='Epic'?'rare':'flip';
}
export const packRuptureTime=(animations:boolean)=>animations?1180:15;
