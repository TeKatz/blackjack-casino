import type {CSSProperties} from 'react';
import {ProfileFrame,DicePreview,CosmeticTitle,portraitPath} from './Cosmetics';
import {resolveCardSkin} from '../blackjack/cardSkins';
import type {ShopItem} from './shopBridge';
import {shopMaterial,shopVisualTier} from './ShopVisualModel';
import type {ShopMaterial} from './ShopVisualModel';

/** Focused object-centered art. Host data remains the sole source of permissions and prices. */
const palette=(variant:string)=>{
 const v=variant.toLowerCase();
 if(/obsidian|graphite|charcoal|noir/.test(v))return ['#96c6da','#253948','#e5f5ff'];
 if(/pearl|linen|ivory|porcelain/.test(v))return ['#e4cfa2','#8c7b69','#fff9e7'];
 if(/ruby|crimson|rose|burgundy|ember|copper/.test(v))return ['#ed8e85','#64263c','#ffe0b7'];
 if(/opal|aurora|celestial|astral|violet|moon|eclipse/.test(v))return ['#cda4f9','#473668','#f3e1ff'];
 if(/neon|cyan|sapphire|ice|blue|midnight/.test(v))return ['#76e3ef','#224465','#ecfaff'];
 if(/forest|emerald|jade|sage/.test(v))return ['#91e6b8','#244f42','#ecf8d8'];
 if(/gold|solar|king|sovereign|deco|brass|dynasty/.test(v))return ['#f6d68b','#785328','#fff1c2'];
 return ['#d9c5a0','#655343','#fff2d1'];
};
function Corners(){return <span className="showcase-corners" aria-hidden="true">
 <i/><i/><i/><i/></span>;}
function Ornament({material}:{material:ShopMaterial}){
 return <span className={'showcase-ornament ornament-'+material} aria-hidden="true">
   <i/><i/><i/><i/><b>✦</b></span>;
}
function CardArtwork({item}:{item:ShopItem}){
 const skin=resolveCardSkin(item.visualVariant);
 return <div className="showcase-card-pair">
   <div className="showcase-card card-back" style={{
     '--card-a':skin.backA,'--card-b':skin.backB,
     '--card-edge':skin.accent
   } as CSSProperties}>
     <div className="card-back-linework"><Ornament material={shopMaterial(item.kind,item.visualVariant)}/></div>
     <span className="card-back-corner">✦</span><span className="card-back-corner bottom">✦</span>
     <span className="card-metal-foil"/>
   </div>
   <div className="showcase-card card-face" style={{
     '--card-a':skin.faceA,'--card-b':skin.faceB,
     '--card-edge':skin.border,'--card-ink':skin.black
   } as CSSProperties}>
     <span className="card-face-corner">A<small>♠</small></span>
     <span className="card-face-center">{skin.mark}</span>
     <span className="card-face-corner bottom">A<small>♠</small></span>
     <span className="card-metal-foil"/>
   </div>
 </div>;
}
function FrameArtwork({item}:{item:ShopItem}){
 return <div className="showcase-frame-stage">
   <div className="frame-aura"/>
   <ProfileFrame variant={item.visualVariant} large>
     <span className="frame-inner-crest" aria-hidden="true">♠</span>
   </ProfileFrame>
   <Corners/>
   <span className="frame-gems" aria-hidden="true"><i/><i/><i/><i/></span>
   <span className="frame-orbit" aria-hidden="true"><i/><i/><i/></span>
 </div>;
}
function DiceArtwork({item}:{item:ShopItem}){
 return <div className="showcase-dice-stage">
   <span className="dice-facet-back"/>
   <DicePreview variant={item.visualVariant}/>
   <span className="dice-facet-glow"/>
 </div>;
}
function SecretGuestArtwork({item,motionEnabled}:{item:ShopItem;motionEnabled:boolean}){
 return <div className="secret-guest-showcase" data-testid="secret-guest-showcase">
   <span className="secret-guest-halo" aria-hidden="true"/>
   <span className="secret-guest-aura" aria-hidden="true"/>
   <span className="secret-guest-frame" aria-hidden="true"/>
   <picture>
     {motionEnabled&&<source media="(prefers-reduced-motion: reduce)"
       srcSet="/shop/portraits/secret-guest.jpg" type="image/jpeg"/>}
     <img src={motionEnabled?'/shop/portraits/secret-guest.webp':portraitPath(item.visualVariant)}
       alt={item.name} loading="lazy" decoding="async"/>
   </picture>
   <span className="secret-guest-sweep" aria-hidden="true"/>
   <span className="secret-guest-crown-gem" aria-hidden="true"/>
   <span className="secret-guest-glint left" aria-hidden="true">✧</span>
   <span className="secret-guest-glint right" aria-hidden="true">✧</span>
   <span className="secret-guest-embers" aria-hidden="true">
     <i/><i/><i/><i/><i/><i/>
   </span>
   <span className="secret-guest-sigil" aria-hidden="true">✦ EXCLUSIVE ✦</span>
 </div>;
}
function PortraitArtwork({item}:{item:ShopItem}){
 return <div className="showcase-portrait-stage">
   <span className="portrait-arc"/><span className="portrait-arc secondary"/>
   <div className="portrait-image-rim">
     <img src={portraitPath(item.visualVariant)} alt={item.name} loading="lazy"/>
   </div>
   <span className="portrait-crown" aria-hidden="true">✦</span>
   <span className="portrait-ambient"/>
 </div>;
}
export function ShopShowcase({item,motionEnabled=true}:{item:ShopItem;motionEnabled?:boolean}){
 const material=shopMaterial(item.kind,item.visualVariant);
 const tier=shopVisualTier(item);
 const skin=item.kind==='CardSet'?resolveCardSkin(item.visualVariant):null;
 const [accent,dark,light]=skin?[skin.accent,skin.backA,skin.faceA]:palette(item.visualVariant);
 const style={'--preview-accent':accent,'--preview-dark':dark,
  '--preview-light':light} as CSSProperties;
 const secret=item.id==='portrait-secret-guest';
 return <div className={'cosmetic-visual premium-showcase showcase-'+item.kind.toLowerCase()}
   data-shop-fx={item.id} data-shop-kind={item.kind} data-material={material}
   data-quality={tier} data-secret-guest={secret?'true':undefined} style={style}>
   <div className="showcase-atmosphere" aria-hidden="true"/>
   <div className="showcase-plinth" aria-hidden="true"/>
   <div className="showcase-object">
     {item.kind==='Frame'?<FrameArtwork item={item}/>:
      item.kind==='CardSet'?<CardArtwork item={item}/>:
      item.kind==='DiceSet'?<DiceArtwork item={item}/>:
      item.kind==='Portrait'?(secret?<SecretGuestArtwork item={item} motionEnabled={motionEnabled}/>:
        <PortraitArtwork item={item}/>):
      <div className="showcase-title-art">
       <span>THE MIDNIGHT CLUB</span><strong><CosmeticTitle variant={item.visualVariant}>{item.name}</CosmeticTitle></strong>
       <i aria-hidden="true">✦ &nbsp; ✦ &nbsp; ✦</i>
      </div>}
   </div>
   <span className="showcase-material-glow" aria-hidden="true"/>
 </div>;
}
