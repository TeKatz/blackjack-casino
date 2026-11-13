import type {ShopItem,ShopKind} from './shopBridge';

/**
 * Display-only material identities. There is no cosmetic rarity field in the
 * native protocol; tiers here control FX intensity, NEVER price or ownership.
 */
export type ShopMaterial=
 'heritage'|'deco'|'velvet'|'jade'|'ocean'|'celestial'|'solar'|
 'obsidian'|'crystal'|'porcelain'|'metal'|'prism'|'forest'|
 'neon'|'royal'|'ember'|'aurora'|'moonlit';
export type VisualTier='classic'|'signature'|'showpiece';
export function shopMaterial(kind:ShopKind,variant:string):ShopMaterial{
 const v=variant.toLowerCase();
 if(/astral|opal|celestial|starlight/.test(v))return 'celestial';
 if(/solar|gold|sovereign|crown/.test(v))return 'solar';
 if(/neon|circuit|pagoda|dynasty/.test(v))return 'neon';
 if(/aurora|prism/.test(v))return 'aurora';
 if(/eclipse|moon|violet|amethyst/.test(v))return 'moonlit';
 if(/ember|crimson|ruby|burgundy|rose|red/.test(v))return 'ember';
 if(/jade|emerald|sage|forest|green/.test(v))return 'jade';
 if(/obsidian|graphite|charcoal|midnight|noir/.test(v))return 'obsidian';
 if(/sapphire|harbor|ice|blue/.test(v))return 'ocean';
 if(/linen|ivory|porcelain|pearl/.test(v))return 'porcelain';
 if(/copper|brass|bronze|silver/.test(v))return 'metal';
 if(/art-deco|deco|royal/.test(v))return 'deco';
 if(/velvet|rosewood/.test(v))return 'velvet';
 if(/crystal/.test(v))return 'crystal';
 if(kind==='Portrait')return 'royal';
 if(kind==='Frame')return 'royal';
 if(kind==='DiceSet')return 'crystal';
 return 'heritage';
}
export function shopVisualTier(item:ShopItem):VisualTier{
 if(/title-sudoku-(premium|ultra|prism|legend)/.test(item.visualVariant))return 'showpiece';
 if(/classic|static-|linen|porcelain|house-guest/.test(item.visualVariant))return 'classic';
 if(item.requiredPrestige>0||item.price>=5000||/sovereign|dynasty|celestial|astral|solar|eclipse|crown/.test(item.visualVariant))
  return 'showpiece';
 return 'signature';
}
export function shopFxCount(kind:ShopKind,tier:VisualTier):number{
 const base=kind==='Frame'?12:kind==='Portrait'?9:kind==='CardSet'?8:kind==='DiceSet'?9:4;
 return tier==='showpiece'?base+8:tier==='signature'?base:Math.max(3,base-5);
}
