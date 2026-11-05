import type {CSSProperties} from 'react';
import manifest from './albumArtManifest.json';

// Original audited 16 atlases. Each card has an independent crop; no low-res
// placeholder is upscaled and no client-side card data is synthesized.
const crops=new Map(manifest.Cards.map(c=>[c.Id,c]));
const atlases=new Map(manifest.Atlases.map(a=>[a.Id,a]));
export function albumArtStyle(id:string):CSSProperties{
 const crop=crops.get(id);
 const atlas=crop?atlases.get(crop.Atlas):undefined;
 if(!crop||!atlas)return{};
 const left=atlas.Width===crop.Width?0:(crop.X/(atlas.Width-crop.Width))*100;
 const top=atlas.Height===crop.Height?0:(crop.Y/(atlas.Height-crop.Height))*100;
 return{
  backgroundImage:'url(/album/atlas/'+encodeURIComponent(atlas.File)+')',
  backgroundSize:(atlas.Width/crop.Width*100)+'% '+(atlas.Height/crop.Height*100)+'%',
  backgroundPosition:left+'% '+top+'%',
  aspectRatio:String(crop.Width)+' / '+String(crop.Height),
 };
}
export function hasAlbumArt(id:string):boolean{return crops.has(id);}
