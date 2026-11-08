import {ShopShowcase} from './ShopShowcase';
import {useMemo,useState} from 'react';
import {filterShop} from './shopBridge';
import type {ShopSnapshot,ShopItem,ShopKind,ShopFilter,ShopStatus,ShopSort} from './shopBridge';
import './shop.css';
import './shopPremium.css';

type Props={
  shop:ShopSnapshot;language:'en'|'de';loaded:boolean;pending:string|null;motionEnabled?:boolean;
  onBack:()=>void;onCosmetic:(id:string)=>void;onPack:(rarity:string)=>void;onAlbum:()=>void;
};
const groups:{id:ShopFilter;en:string;de:string;icon:string}[]=[
  {id:'All',en:'Everything',de:'Alles',icon:'✦'},
  {id:'CardSet',en:'Card Sets',de:'Kartensets',icon:'♠'},
  {id:'DiceSet',en:'Dice Sets',de:'Würfelsets',icon:'⚄'},
  {id:'Portrait',en:'Portraits',de:'Porträts',icon:'◈'},
  {id:'Frame',en:'Frames',de:'Rahmen',icon:'▣'},
  {id:'Title',en:'Titles',de:'Titel',icon:'♛'},
];
const labelKind=(kind:ShopKind,de:boolean)=>({
  CardSet:de?'Kartenset':'Card Set',DiceSet:de?'Würfelset':'Dice Set',
  Portrait:de?'Porträt':'Portrait',Frame:de?'Rahmen':'Frame',Title:'Title',
}[kind]);
function CosmeticCard({item,de,pending,motionEnabled,onPick}:{
  item:ShopItem;de:boolean;pending:boolean;motionEnabled:boolean;onPick:(item:ShopItem)=>void;
}){
  const locked=!item.owned&&(!item.shopAvailable||item.requiredPrestige>0&&!item.canBuy);
  const cta=item.equipped?(de?'Ausgerüstet':'Equipped'):item.owned?(de?'Ausrüsten':'Equip'):
    !item.shopAvailable?(de?'Belohnung':'Reward only'):
    item.requiredPrestige>0&&!item.canBuy?(de?'Prestige benötigt':'Prestige required'):
    !item.canBuy?(de?'Zu wenig Münzen':'Not enough coins'):(de?'Kaufen & ausrüsten':'Buy & equip');
  const disabled=item.equipped||pending||(!item.owned&&!item.canBuy);
  return <article className={'shop-product '+(item.equipped?'is-equipped ':'')+(locked?'is-locked':'')+
      (item.id==='portrait-secret-guest'?' secret-guest-product':'')}
      data-cosmetic-id={item.id}>
    <div className="shop-product-top">
      <span>{labelKind(item.kind,de)}</span>
      {item.equipped?<b className="shop-tag equipped">✓ {de?'AKTIV':'EQUIPPED'}</b>:
      item.owned?<b className="shop-tag owned">✓ {de?'BESITZ':'OWNED'}</b>:
      !item.shopAvailable?<b className="shop-tag locked">✦ {de?'BELOHNUNG':'REWARD'}</b>:
      item.requiredPrestige>0?<b className="shop-tag locked">P{item.requiredPrestige}</b>:null}
    </div>
    <ShopShowcase item={item} motionEnabled={motionEnabled}/>
    <div className="shop-product-content">
      <h3 title={item.name}>{item.name}</h3>
      <p>{item.description}</p>
      <div className="shop-product-footer">
        <span className="shop-price">{item.owned?(de?'In Sammlung':'In collection'):
          !item.shopAvailable?(de?'Nicht käuflich':'Not for sale'):
          '◆ '+item.price.toLocaleString('en-US')}</span>
        <button className="shop-card-action" type="button" disabled={disabled}
          onClick={()=>onPick(item)}>{pending?'…':cta}</button>
      </div>
    </div>
  </article>;
}
export function ModernShop({shop,language,loaded,pending,motionEnabled=true,onBack,onCosmetic,onPack,onAlbum}:Props){
  const de=language==='de';
  const [kind,setKind]=useState<ShopFilter>('All');
  const [status,setStatus]=useState<ShopStatus>('all');
  const [sort,setSort]=useState<ShopSort>('recommended');
  const [query,setQuery]=useState('');
  const [confirm,setConfirm]=useState<{id:string;name:string;price:number;pack:boolean}|null>(null);
  const filtered=useMemo(()=>filterShop(shop.items,kind,status,query,sort),[shop.items,kind,status,query,sort]);
  const pick=(item:ShopItem)=>{
    if(item.owned){onCosmetic(item.id);return;}
    setConfirm({id:item.id,name:item.name,price:item.price,pack:false});
  };
  const accept=()=>{
    if(!confirm)return;
    if(confirm.pack)onPack(confirm.id);else onCosmetic(confirm.id);
    setConfirm(null);
  };
  return <main className="modern-shop" data-shop-motion={motionEnabled?'on':'off'} aria-label={de?'Kosmetik-Shop':'Cosmetic Shop'}>
    <div className="shop-scroll">
      <div className="shop-hero">
        <div className="shop-hero-copy">
          <button className="shop-back" type="button" onClick={onBack}>‹ {de?'Zurück zur Lobby':'Back to lobby'}</button>
          <span className="shop-eyebrow">THE MIDNIGHT CLUB / COSMETIC COLLECTION</span>
          <h1>{de?'Dein Stil. Deine Geschichte.':'Dress for the night.'}</h1>
          <p>{de?'Sammle unverwechselbare Karten, Würfel, Porträts, Rahmen und Titel. Die Wirkung bleibt kosmetisch.':
            'Own the table with signature cards, dice, portraits, frames and titles. Every upgrade is purely cosmetic.'}</p>
        </div>
        <div className="shop-hero-art" aria-hidden="true">♠</div>
        <div className="shop-wallet">
          <span>{de?'KOSMETIK-MÜNZEN':'COSMETIC COINS'}</span>
          <strong>◆ {shop.coins}</strong>
          <small>PRESTIGE {shop.prestige} &nbsp; · &nbsp; {shop.owned}/{shop.total} {de?'in Sammlung':'collected'}</small>
        </div>
      </div>
      <div className="shop-featured">
        <span className="shop-section-label">{de?'EXKLUSIV FÜR DICH':'CURATED FOR YOU'}</span>
        <strong>{de?'Jede Sammlung hat ihren eigenen Charakter.':'Your collection. Your signature.'}</strong>
        <span>{de?'Alle Werte und Freischaltungen stammen aus deinem Spielstand.':
          'All unlocks and prices reflect your current saved profile.'}</span>
      </div>
      <div className="shop-filter-panel">
        <div className="shop-categories" role="group" aria-label={de?'Kosmetiktyp':'Cosmetic category'}>
          {groups.map(g=><button key={g.id} type="button" aria-pressed={kind===g.id}
            className={'shop-category '+(kind===g.id?'active':'')} onClick={()=>setKind(g.id)}>
            <span aria-hidden="true">{g.icon}</span>{de?g.de:g.en}
          </button>)}
        </div>
        <div className="shop-filter-row">
          <label className="shop-search"><span className="sr-only">{de?'Kosmetik suchen':'Search cosmetics'}</span>
            <span aria-hidden="true">⌕</span><input value={query} onChange={e=>setQuery(e.target.value)}
              placeholder={de?'Kosmetik durchsuchen…':'Search the collection…'} type="search"/></label>
          <label className="shop-control-label">{de?'Status':'Status'}<select value={status} onChange={e=>setStatus(e.target.value as ShopStatus)}>
            <option value="all">{de?'Alle':'All items'}</option>
            <option value="forSale">{de?'Im Verkauf':'For sale'}</option>
            <option value="affordable">{de?'Bezahlbar':'Affordable'}</option>
            <option value="owned">{de?'In Besitz':'Owned'}</option>
            <option value="equipped">{de?'Ausgerüstet':'Equipped'}</option>
            <option value="unowned">{de?'Noch nicht im Besitz':'Unowned'}</option>
            <option value="locked">{de?'Gesperrte Belohnungen':'Locked rewards'}</option>
          </select></label>
          <label className="shop-control-label">{de?'Sortierung':'Sort'}<select value={sort} onChange={e=>setSort(e.target.value as ShopSort)}>
            <option value="recommended">{de?'Empfohlen':'Recommended'}</option>
            <option value="cheap">{de?'Preis: aufsteigend':'Price: low to high'}</option>
            <option value="expensive">{de?'Preis: absteigend':'Price: high to low'}</option>
            <option value="name">{de?'Name':'Name'}</option>
          </select></label>
          <button className="shop-reset" type="button" onClick={()=>{setKind('All');setStatus('all');setSort('recommended');setQuery('');}}>
            {de?'Zurücksetzen':'Reset'}</button>
        </div>
      </div>
      <div className="shop-grid-caption">
        <h2>{de?'Kosmetik-Kollektion':'The Cosmetic Collection'}</h2>
        <span>{loaded?filtered.length:0} {de?'Gegenstände':'items'}</span>
      </div>
      {!loaded?<div className="shop-loading" role="status">{de?'Sammlung wird geladen…':'Loading your collection…'}</div>:
      filtered.length===0?<div className="shop-empty">{de?'Keine Gegenstände gefunden. Passe die Filter an.':
        'No cosmetics found. Try a different category or search.'}</div>:
      <section className="shop-product-grid" aria-label={de?'Verfügbare Kosmetik':'Available cosmetics'}>
        {filtered.map(item=><CosmeticCard key={item.id} item={item} de={de}
          pending={pending===item.id} motionEnabled={motionEnabled} onPick={pick}/>)}
      </section>}
      <div className="shop-packs-section">
        <div className="shop-pack-heading"><span className="shop-section-label">COLLECTOR'S CORNER</span>
          <h2>{de?'Kartenpakete':'Collector Card Packs'}</h2>
          <p>{de?'Jedes Paket enthält fünf Sammelkarten. Die Seltenheitsstufe garantiert mindestens eine Karte dieser Stufe.':
            'Five cards per pack, with one guaranteed card of the featured rarity. Open purchased packs in the Album.'}</p></div>
        <div className="shop-pack-grid">
          {shop.packs.map(pack=><article className={'shop-pack pack-'+pack.rarity.toLowerCase()} key={pack.rarity}>
            <div className="pack-art" aria-hidden="true"><span>✦</span><i>♠</i><b>✦</b></div>
            <h3>{pack.rarity}</h3>
            <span className="pack-odds">{de?'Ultra-selten':'Ultra rare'} {(pack.ultraRareChance*100).toFixed(2)}%</span>
            <div className="pack-owned">{de?'Vorhanden':'Owned'}: <strong>{pack.owned}</strong></div>
            <button className="shop-pack-buy" type="button" disabled={!pack.canBuy||pending!==null}
              onClick={()=>setConfirm({id:pack.rarity,name:pack.rarity+' Pack',price:pack.price,pack:true})}>
              ◆ {pack.price.toLocaleString('en-US')} · {de?'Kaufen':'Buy'}</button>
          </article>)}
        </div>
        <button className="shop-album-link" type="button" onClick={onAlbum}>▤ {de?'Zum Sammelalbum':'Open Collector’s Album'} ›</button>
      </div>
    </div>
    {confirm&&<div className="shop-confirm-overlay" role="presentation">
      <section className="shop-confirm" role="dialog" aria-modal="true" aria-labelledby="shop-confirm-heading">
        <span className="shop-section-label">THE MIDNIGHT CLUB</span>
        <h2 id="shop-confirm-heading">{de?'Kauf bestätigen':'Confirm purchase'}</h2>
        <p><strong>{confirm.name}</strong></p>
        <p>{de?'Der Kauf wird mit Kosmetik-Münzen aus deinem Spielstand bezahlt.':
          'This purchase uses cosmetic coins from your saved profile.'}</p>
        <div className="shop-confirm-price">◆ {confirm.price.toLocaleString('en-US')}</div>
        <div className="shop-confirm-actions">
          <button type="button" onClick={()=>setConfirm(null)}>{de?'Abbrechen':'Cancel'}</button>
          <button type="button" onClick={accept}>{de?'Kaufen':'Purchase'}</button>
        </div>
      </section>
    </div>}
  </main>;
}
