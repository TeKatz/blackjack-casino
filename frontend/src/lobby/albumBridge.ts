/** The album is presentation-only. C# commits every draw and reward first. */
export const rarityOrder=['Common','Uncommon','Rare','Epic','Legendary'] as const;
export type AlbumRarity=typeof rarityOrder[number];
export type AlbumCard={id:string;name:string;number:number;rarity:AlbumRarity;owned:boolean;lore:string};
export type AlbumTheme={
 id:string;name:string;symbol:string;accent:string;ultraRare:boolean;
 owned:number;total:number;rewardReady:boolean;rewardClaimed:boolean;
 rewardCoins:number;rewardBankroll:string;rewardPack:AlbumRarity;cards:AlbumCard[];
};
export type AlbumPack={rarity:AlbumRarity;price:number;owned:number;guaranteed:AlbumRarity;ultraRareChance:number};
export type AlbumDraw={id:string;name:string;number:number;rarity:AlbumRarity;
 themeId:string;theme:string;accent:string;ultraRare:boolean;isNew:boolean;duplicatePoints:number};
export type AlbumOpening={id:string;rarity:AlbumRarity;pointsEarned:number;packsEarned:number;
 newCards:number;cards:AlbumDraw[]};
export type AlbumSnapshot={totalCards:number;ownedCards:number;completedThemes:number;
 packsOpened:number;duplicatePoints:number;duplicatePackCost:number;coins:string;
 packs:AlbumPack[];themes:AlbumTheme[];pending:AlbumOpening|null};
export type AlbumOperation='open'|'complete'|'claim';
export type AlbumResult={operation:AlbumOperation;id:string;success:boolean;message:string};

const obj=(x:unknown):x is Record<string,unknown>=>x!==null&&typeof x==='object'&&!Array.isArray(x);
const text=(x:unknown):x is string=>typeof x==='string'&&x.length<=3000;
const integer=(x:unknown)=>typeof x==='number'&&Number.isSafeInteger(x)&&x>=0;
const fraction=(x:unknown)=>typeof x==='number'&&Number.isFinite(x)&&x>=0&&x<=1;
const rarity=(x:unknown):x is AlbumRarity=>rarityOrder.some(r=>r===x);
const card=(v:unknown):v is AlbumCard=>obj(v)&&text(v.id)&&text(v.name)&&integer(v.number)
 &&rarity(v.rarity)&&typeof v.owned==='boolean'&&text(v.lore);
const theme=(v:unknown):v is AlbumTheme=>obj(v)&&['id','name','symbol','accent','rewardBankroll']
 .every(k=>text(v[k]))&&typeof v.ultraRare==='boolean'&&typeof v.rewardReady==='boolean'
 &&typeof v.rewardClaimed==='boolean'&&integer(v.owned)&&integer(v.total)
 &&integer(v.rewardCoins)&&rarity(v.rewardPack)&&Array.isArray(v.cards)
 &&v.cards.length===v.total&&v.cards.length<=10&&Number(v.owned)<=Number(v.total)&&v.cards.every(card);
const pack=(v:unknown):v is AlbumPack=>obj(v)&&rarity(v.rarity)&&rarity(v.guaranteed)
 &&integer(v.price)&&integer(v.owned)&&fraction(v.ultraRareChance);
const draw=(v:unknown):v is AlbumDraw=>obj(v)
 &&['id','name','themeId','theme','accent'].every(k=>text(v[k]))
 &&integer(v.number)&&rarity(v.rarity)&&typeof v.ultraRare==='boolean'
 &&typeof v.isNew==='boolean'&&integer(v.duplicatePoints);
export function isAlbum(x:unknown):x is AlbumSnapshot{
 if(!obj(x)||!['totalCards','ownedCards','completedThemes','packsOpened','duplicatePoints','duplicatePackCost']
 .every(k=>integer(x[k]))||!text(x.coins)
 ||!Array.isArray(x.packs)||x.packs.length!==5||!x.packs.every(pack)
 ||!Array.isArray(x.themes)||x.themes.length!==16||!x.themes.every(theme)
 ||x.totalCards!==x.themes.reduce((n:number,t:AlbumTheme)=>n+t.total,0)
 ||Number(x.ownedCards)>Number(x.totalCards)||Number(x.completedThemes)>x.themes.length)return false;
 if(x.pending===null)return true;
 const p=x.pending;
 return obj(p)&&typeof p.id==='string'&&/^[a-f0-9]{32}$/.test(p.id)
 &&rarity(p.rarity)&&integer(p.pointsEarned)&&integer(p.packsEarned)&&integer(p.newCards)
 &&Array.isArray(p.cards)&&p.cards.length===5&&p.cards.every(draw)
 &&p.newCards===p.cards.filter((d:AlbumDraw)=>d.isNew).length;
}
export function isAlbumResult(x:unknown):x is AlbumResult{
 return obj(x)&&['open','complete','claim'].includes(String(x.operation))
 &&text(x.id)&&x.id.length<=80&&typeof x.success==='boolean'
 &&text(x.message)&&x.message.length<=800;
}
export const previewAlbum:AlbumSnapshot={
 totalCards:0,ownedCards:0,completedThemes:0,packsOpened:0,duplicatePoints:0,
 duplicatePackCost:800,coins:'0',packs:[],themes:[],pending:null,
};
