/** All shop prices, permissions and balances are supplied by the native game. */
export type ShopKind = 'Title'|'Frame'|'Portrait'|'CardSet'|'DiceSet';
export type ShopItem = {
  id: string; name: string; description: string; kind: ShopKind; visualVariant: string;
  price: number; shopAvailable: boolean; requiredPrestige: number;
  owned: boolean; equipped: boolean; canBuy: boolean;
};
export type ShopPack = { rarity:string; price:number; owned:number; canBuy:boolean; guaranteed:string; ultraRareChance:number };
export type ShopSnapshot = {
  coins:string; prestige:number; total:number; owned:number; items:ShopItem[]; packs:ShopPack[];
};
export type ShopResult = { id:string; success:boolean; message:string; };
export type ShopMessage = {type:'lobby.shop.snapshot';data:ShopSnapshot}
  | {type:'lobby.shop.result';data:ShopResult}
  | {type:'lobby.shop.show';data:null}
  | {type:'lobby.shop.hide';data:null};

export const previewShop: ShopSnapshot = {
  coins:'10,000',prestige:0,total:5,owned:1,
  items:[
    {id:'cardset-classic',name:'Classic Cards',kind:'CardSet',description:'The familiar clean casino deck.',visualVariant:'card-classic',price:0,shopAvailable:false,requiredPrestige:0,owned:true,equipped:true,canBuy:false},
    {id:'cardset-celestial-opal',name:'Celestial Opal',kind:'CardSet',description:'Premium collection: pearl-lilac faces and luminous star back.',visualVariant:'card-celestial-opal',price:14500,shopAvailable:true,requiredPrestige:0,owned:false,equipped:false,canBuy:false},
    {id:'diceset-aurora',name:'Aurora Prism',kind:'DiceSet',description:'Teal-violet crystal with subtle aurora halo.',visualVariant:'dice-aurora',price:8000,shopAvailable:true,requiredPrestige:0,owned:false,equipped:false,canBuy:true},
    {id:'portrait-eclipse-raven',name:'Eclipse Raven',kind:'Portrait',description:'Velvet Eclipse season reward.',visualVariant:'portrait-eclipse-raven',price:0,shopAvailable:false,requiredPrestige:0,owned:false,equipped:false,canBuy:false},
    {id:'frame-emerald',name:'Emerald Edge',kind:'Frame',description:'A deep green frame with a breathing glow.',visualVariant:'emerald-breathe',price:600,shopAvailable:true,requiredPrestige:0,owned:false,equipped:false,canBuy:true},
  ],
  packs:['Common','Uncommon','Rare','Epic','Legendary'].map((rarity,i)=>({
    rarity,price:[100,200,400,800,1600][i],owned:i===0?3:0,canBuy:true,guaranteed:rarity,ultraRareChance:[.0025,.005,.01,.02,.03][i],
  })),
};
const obj=(x:unknown):x is Record<string,unknown>=>x!==null&&typeof x==='object'&&!Array.isArray(x);
const str=(x:unknown):x is string=>typeof x==='string';
const num=(x:unknown):x is number=>typeof x==='number'&&Number.isSafeInteger(x)&&x>=0;
const kinds:ShopKind[]=['Title','Frame','Portrait','CardSet','DiceSet'];
export function isShopSnapshot(x:unknown):x is ShopSnapshot {
  if(!obj(x)||!str(x.coins)||!num(x.prestige)||!num(x.total)||!num(x.owned)
    ||!Array.isArray(x.items)||x.items.length>300||!Array.isArray(x.packs)||x.packs.length>10)return false;
  return x.items.every((s:unknown)=>obj(s)&&str(s.id)&&/^[a-z0-9-]{1,80}$/.test(s.id)
    &&str(s.name)&&str(s.description)&&str(s.visualVariant)&&kinds.includes(s.kind as ShopKind)
    &&num(s.price)&&num(s.requiredPrestige)&&typeof s.shopAvailable==='boolean'
    &&typeof s.owned==='boolean'&&typeof s.equipped==='boolean'&&typeof s.canBuy==='boolean')
    &&x.packs.every((p:unknown)=>obj(p)&&str(p.rarity)&&/^[A-Za-z]{3,20}$/.test(p.rarity)
      &&num(p.price)&&num(p.owned)&&str(p.guaranteed)
      &&typeof p.canBuy==='boolean'&&typeof p.ultraRareChance==='number'
      &&p.ultraRareChance>=0&&p.ultraRareChance<=1);
}
export function isShopResult(x:unknown):x is ShopResult{
  return obj(x)&&str(x.id)&&str(x.message)&&x.message.length<=600&&typeof x.success==='boolean';
}
export function onShopMessages(handle:(msg:ShopMessage)=>void):()=>void{
  const host=window.chrome?.webview;
  if(!host)return()=>{};
  const listener=(event:MessageEvent)=>{
    const msg:unknown=event.data;
    if(!obj(msg)||!str(msg.type))return;
    if(msg.type==='lobby.shop.snapshot'&&isShopSnapshot(msg.data))handle({type:msg.type,data:msg.data});
    else if(msg.type==='lobby.shop.result'&&isShopResult(msg.data))handle({type:msg.type,data:msg.data});
    else if((msg.type==='lobby.shop.show'||msg.type==='lobby.shop.hide')&&msg.data===null)
      handle({type:msg.type,data:null});
  };
  host.addEventListener('message',listener);
  return()=>host.removeEventListener('message',listener);
}

export type ShopFilter = 'All'|'CardSet'|'DiceSet'|'Portrait'|'Frame'|'Title';
export type ShopStatus = 'all'|'forSale'|'affordable'|'owned'|'equipped'|'unowned'|'locked';
export type ShopSort = 'recommended'|'cheap'|'expensive'|'name';
export function filterShop(items:ShopItem[],type:ShopFilter,status:ShopStatus,query:string,sort:ShopSort):ShopItem[]{
  const search=query.trim().toLowerCase();
  const filtered=items.filter(item=>{
    if(type!=='All'&&item.kind!==type)return false;
    if(search&&!(item.name+' '+item.kind+' '+item.description).toLowerCase().includes(search))return false;
    switch(status){
      case'forSale':return item.shopAvailable&&!item.owned;
      case'affordable':return item.canBuy&&!item.owned;
      case'owned':return item.owned;
      case'equipped':return item.equipped;
      case'unowned':return !item.owned;
      case'locked':return !item.owned&&(!item.shopAvailable||!item.canBuy&&item.requiredPrestige>0);
      default:return true;
    }
  });
  const priority=(item:ShopItem)=>item.equipped?0:item.owned?1:item.shopAvailable&&item.requiredPrestige===0?2:3;
  filtered.sort((a,b)=>sort==='cheap'?a.price-b.price||a.name.localeCompare(b.name):
    sort==='expensive'?b.price-a.price||a.name.localeCompare(b.name):
    sort==='name'?a.name.localeCompare(b.name):
      priority(a)-priority(b)||kinds.indexOf(a.kind)-kinds.indexOf(b.kind)||a.price-b.price||a.name.localeCompare(b.name));
  return filtered;
}
