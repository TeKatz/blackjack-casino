import {Modal} from './Modal';
import {useEffect,useMemo,useRef,useState} from 'react';
import type {CSSProperties} from 'react';
import {albumArtStyle,albumAtlasUrl} from './albumArt';
import {rarityOrder} from './albumBridge';
import type {AlbumCard,AlbumDraw,AlbumOpening,AlbumRarity,AlbumResult,AlbumSnapshot,AlbumTheme} from './albumBridge';
import './album.css';
import {playAlbumCue} from './albumAudio';

type Props={
 data:AlbumSnapshot;loaded:boolean;language:'en'|'de';animate:boolean;soundEnabled:boolean;result:AlbumResult|null;
 onBack:()=>void;onOpen:(rarity:AlbumRarity)=>void;onComplete:(id:string)=>void;
 onClaim:(id:string)=>void;onShop:()=>void;
};
type Phase='browse'|'stage'|'await'|'reveal'|'summary'|'finished';
const rarityColors:Record<AlbumRarity,string>={
 Common:'#adbec6',Uncommon:'#77deb2',Rare:'#7cbdfa',Epic:'#d2a1ff',Legendary:'#f5d58d',
};
const rarityTitle=(rarity:AlbumRarity,de:boolean)=>de?({
 Common:'Gewöhnlich',Uncommon:'Ungewöhnlich',Rare:'Selten',Epic:'Episch',Legendary:'Legendär',
}[rarity]):rarity;
type CardView={id:string;name:string;rarity:AlbumRarity;owned:boolean;theme:string;
 ultraRare:boolean;badge?:string};
function CardArtwork({id}:{id:string}){
 return <div className="album-card-illustration" style={albumArtStyle(id)} aria-hidden="true"/>;
}
function CollectionCard({card,index,onInspect,de}:{
 card:CardView;index?:number;onInspect?:()=>void;de:boolean;
}){
 const style={'--card-rarity':rarityColors[card.rarity]} as CSSProperties;
 const inner=<>
  <div className="album-card-image-wrap">
    {card.owned?<CardArtwork id={card.id}/>:<span className="album-card-unknown" aria-hidden="true">♠</span>}
    <span className="album-card-foil" aria-hidden="true"/>
  </div>
  <div className="album-card-details"><small>{card.ultraRare?'ULTRA RARE':card.rarity.toUpperCase()}</small>
    <strong>{card.owned?card.name:(de?'Unentdeckt':'Undiscovered')}</strong>
    {card.badge&&<b className="album-card-badge">{card.badge}</b>}</div>
  {index!==undefined&&<span className="album-card-index">#{index.toString().padStart(2,'0')}</span>}
 </>;
 return onInspect?<button type="button" className={'album-collection-card '+(!card.owned?'is-missing ':'')+(card.ultraRare?'is-ultra':'')}
 style={style} onClick={onInspect} aria-label={card.owned?card.name:(de?'Unentdeckte Karte':'Undiscovered card')}>
  {inner}</button>:
  <div className={'album-collection-card '+(card.ultraRare?'is-ultra':'')} style={style}>{inner}</div>;
}
function AlbumBooster({rarity,tearing}:{rarity:AlbumRarity;tearing:boolean}){
 return <div className={'album-booster '+(tearing?'is-tearing':'')} style={{'--card-rarity':rarityColors[rarity]} as CSSProperties} aria-hidden="true">
   <div className="album-booster-light"/><div className="album-booster-back"/>
   <div className="album-booster-body">
     <span className="album-booster-top">THE MIDNIGHT CLUB</span>
     <span className="album-booster-sigil">♠</span>
     <strong>{rarity.toUpperCase()}</strong>
     <span className="album-booster-bottom">COLLECTOR EDITION · FIVE CARDS</span>
   </div>
   <div className="album-booster-tear"/>
   {Array.from({length:18},(_,i)=><i key={i} className="album-booster-spark" style={{'--spark-i':i} as CSSProperties}/>)}
 </div>;
}
function drawCardView(draw:AlbumDraw,de:boolean):CardView{
 return {id:draw.id,name:draw.name,rarity:draw.rarity,owned:true,theme:draw.theme,
 ultraRare:draw.ultraRare,badge:draw.isNew?(de?'NEU':'NEW'):draw.duplicatePoints>0?'+'+draw.duplicatePoints+' DP':undefined};
}
export function ModernAlbum({data,loaded,language,animate,soundEnabled,result,onBack,onOpen,onComplete,onClaim,onShop}:Props){
 const de=language==='de';
 const [themeId,setThemeId]=useState('velvet-table');
 const [filter,setFilter]=useState<'all'|'owned'|'missing'>('all');
 const [query,setQuery]=useState('');
 const [rarity,setRarity]=useState<AlbumRarity>('Common');
 const [phase,setPhase]=useState<Phase>('browse');
 const [revealed,setRevealed]=useState(0);
 const [busy,setBusy]=useState(false);
 const [unresponsive,setUnresponsive]=useState(false);
 const [batchRemaining,setBatchRemaining]=useState(0);
 const [displayOpening,setDisplayOpening]=useState<AlbumOpening|null>(null);
 const [inspectorId,setInspectorId]=useState<string|null>(null);
 const [notice,setNotice]=useState<string|null>(null);
 const lastReceipt=useRef<string|null>(null);
 const artReady=useRef(false);
 const revealSuspended=useRef(false);
 const claimPending=useRef<string|null>(null);
 const [claimedId,setClaimedId]=useState<string|null>(null);
 const themes=data.themes;
 const theme=themes.find(t=>t.id===themeId)??themes[0];
 const currentPack=data.packs.find(p=>p.rarity===rarity);
 const opening=data.pending??displayOpening;
 const openActive=phase!=='browse';
 const fullTitle=theme?.ultraRare?'ULTRA RARE COLLECTION':'THE COLLECTOR ARCHIVE';
 const activeCard=inspectorId?themes.flatMap(t=>t.cards.map(c=>({card:c,theme:t}))).find(x=>x.card.id===inspectorId):undefined;
 const visibleCards=useMemo(()=>theme?.cards.filter(c=>(filter==='all'||(filter==='owned'?c.owned:!c.owned))
 &&(!query.trim()||(c.name+' '+theme.name+' '+c.rarity).toLocaleLowerCase().includes(query.trim().toLocaleLowerCase())))??[],
 [theme,filter,query]);
 const selectedPackCount=data.pending?0:(currentPack?.owned??0);
 // The last pack in an Open All batch must also finish automatically.
 // Zero means a manually opened single pack, which waits for Collect.
 const hasQueuedBatch=batchRemaining>0;

 // A persisted receipt is the only source of revealed cards. Returning from
 // another page or restarting the application resumes the same five results.
 useEffect(()=>{
  if(!data.pending||lastReceipt.current===data.pending.id)return;
  lastReceipt.current=data.pending.id;
  artReady.current=false;
  revealSuspended.current=false;
  setDisplayOpening(data.pending);
  setRarity(data.pending.rarity);
  setRevealed(0);
  setBusy(false);
  // Decode the exact original illustrated atlases before the first flip.
  // A slow PNG must never flash a blank face during the cinematic reveal.
  setPhase('await');
  setUnresponsive(false);
  const urls=Array.from(new Set(data.pending.cards.map(card=>albumAtlasUrl(card.id)).filter(
   (url):url is string=>url!==null)));
  const images=urls.map(url=>{const img=new Image();img.src=url;return img;});
  let cancelled=false;
  let advanced=false;
  const markReady=()=>{
   if(cancelled||advanced)return;
   advanced=true;
   artReady.current=true;
   if(!revealSuspended.current)setPhase('reveal');
  };
  const timeout=window.setTimeout(markReady,4000);
  void Promise.all(images.map(img=>img.decode().catch(()=>{}))).then(()=>{
   if(cancelled)return;
   window.clearTimeout(timeout);
   markReady();
  });
  return()=>{cancelled=true;window.clearTimeout(timeout);};
 },[data.pending?.id]);
 useEffect(()=>{
  if(phase!=='reveal'||!data.pending)return;
  if(revealed>=data.pending.cards.length){setPhase('summary');return;}
  const extra=animate?(revealed===0?830:320+Math.min(revealed,4)*90):15;
  const timer=window.setTimeout(()=>{
   playAlbumCue('flip',soundEnabled,data.pending!.cards[revealed].rarity);
   setRevealed(v=>Math.min(v+1,data.pending!.cards.length));
  },extra);
  return()=>window.clearTimeout(timer);
 },[phase,revealed,data.pending,animate,soundEnabled]);
 // "Open all" is a sequence of *individual* durable native transactions.
 // Never open the next pack before the preceding receipt was completed.
 useEffect(()=>{
  if(phase!=='summary'||!data.pending||!hasQueuedBatch||busy)return;
  const timer=window.setTimeout(()=>{
   setBusy(true);
   onComplete(data.pending!.id);
  },animate?1150:80);
  return()=>window.clearTimeout(timer);
 },[phase,data.pending,hasQueuedBatch,busy,animate,onComplete]);
 useEffect(()=>{
  if(!result)return;
  setUnresponsive(false);
  setNotice(result.message);
  if(result.operation==='claim'){claimPending.current=null;setClaimedId(null);return;}
  if(!result.success){setBusy(false);setBatchRemaining(0);
   if(result.operation==='open'&&!data.pending)setPhase('stage');
   return;
  }
  if(result.operation==='open'){setBusy(false);return;}
  if(batchRemaining<=1)playAlbumCue('finish',soundEnabled,rarity);
  // C# has now durably cleared the receipt. React may request the next pack.
  setBusy(false);
  if(batchRemaining>1){
   setBatchRemaining(n=>Math.max(0,n-1));
   setPhase('await');
   setRevealed(0);
   const next=rarity;
   const timer=window.setTimeout(()=>{setBusy(true);playAlbumCue('seal',soundEnabled,next);onOpen(next);},animate?280:30);
   return()=>window.clearTimeout(timer);
  }
  setBatchRemaining(0);
  setPhase('finished');
 },[result]);
 useEffect(()=>{
  const listener=(event:KeyboardEvent)=>{
   if(event.key!=='Escape')return;
   if(inspectorId){setInspectorId(null);event.stopPropagation();}
   else if(openActive){revealSuspended.current=true;setPhase('browse');setBatchRemaining(0);event.stopPropagation();}
  };
  window.addEventListener('keydown',listener);
  return()=>window.removeEventListener('keydown',listener);
 },[inspectorId,openActive]);
 function stagePack(r:AlbumRarity){
  setNotice(null);
  setRarity(r);
  if(data.pending){
   revealSuspended.current=false;
   setDisplayOpening(data.pending);
   setRevealed(0);
   setPhase(artReady.current?'reveal':'await');
  }else{
   setPhase('stage');
  }
 }
 function startPack(all:boolean){
  if(busy||data.pending||selectedPackCount<=0)return;
  setBatchRemaining(all?selectedPackCount:0);
  setRevealed(0);
  setBusy(true);
  setPhase('await');
  setNotice(null);
  setUnresponsive(false);
  playAlbumCue('seal',soundEnabled,rarity);
  onOpen(rarity);
 }
 // A lost browser reply must never look like an inert button. Do not
 // retry an opening automatically: it may already be durably committed.
 useEffect(()=>{
  if(phase!=='await'||data.pending||unresponsive)return;
  const timeout=window.setTimeout(()=>{
   setUnresponsive(true);
   setBusy(false);
   setNotice(de
    ?'Keine Rückmeldung zur Packöffnung. Die Karten können bereits gespeichert sein. Zur Sammlung zurückkehren und das Pack erneut aufrufen.'
    :'No response to the pack opening. Cards may already be saved. Return to the collection and resume the pack.');
  },8000);
  return()=>window.clearTimeout(timeout);
 },[phase,data.pending,unresponsive,de]);
 function finishPack(){
  if(!data.pending||busy||revealed<5)return;
  setBusy(true);onComplete(data.pending.id);
 }
 const stageHeader=phase==='stage'?(de?'Versiegeltes Pack':'Sealed collector pack'):
  phase==='await'?(de?'Das Siegel bricht…':'Breaking the seal…'):
  phase==='reveal'?(de?'Die Karten erscheinen…':'The cards emerge…'):
  phase==='finished'?(de?'Zum Album hinzugefügt':'Collection updated'):(de?'Deine fünf Karten':'Your five cards');
 return <main className="record-page album-page"><div className="record-page-scroll">
  <header className="record-hero album-hero"><div className="record-hero-copy">
   <button type="button" className="shop-back" onClick={onBack}>{de?'‹ Zur Lobby':'‹ Back to lobby'}</button>
   <span className="shop-eyebrow">THE MIDNIGHT CLUB / COLLECTOR'S ARCHIVE</span>
   <h1>{de?'Das Sammleralbum':'The Collector’s Album'}</h1>
   <p>{de?'136 originale Illustrationen. 16 Themen. Jedes gefundene Motiv erzählt seine eigene Geschichte.':
     '136 original illustrations across 16 themed collections. Every find tells its own story.'}</p>
  </div><div className="record-hero-side"><span>{de?'ENTDECKTE KARTEN':'DISCOVERED CARDS'}</span>
   <strong>{data.ownedCards} <small>/ {data.totalCards||136}</small></strong><small>{data.completedThemes}/16 {de?'Themen abgeschlossen':'themes complete'}</small>
  </div></header>
  {!loaded?<p role="status" className="record-empty">{de?'Sammlung wird geladen…':'Loading your collection…'}</p>:<>
  <div className="album-stats">
   <div><span>{de?'Karten':'Cards'}</span><b>{data.ownedCards}/{data.totalCards}</b></div>
   <div><span>{de?'Geöffnete Packs':'Packs opened'}</span><b>{data.packsOpened.toLocaleString(language)}</b></div>
   <div><span>{de?'Duplikatpunkte':'Duplicate points'}</span><b>{data.duplicatePoints}/{data.duplicatePackCost}</b></div>
   <div><span>{de?'Kosmetikmünzen':'Cosmetic coins'}</span><b>◆ {data.coins}</b></div>
  </div>
  <section className="album-packs" aria-label={de?'Kartenpakete':'Card packs'}>
   <div className="album-section-caption"><div><span className="shop-eyebrow">THE SEALED COLLECTION</span>
   <h2>{de?'Deine Kartenpakete':'Your card packs'}</h2></div>
   <button type="button" className="record-reset" onClick={onShop}>{de?'Neue Packs kaufen':'Buy more packs'} ↗</button></div>
   {data.pending&&<button className="album-resume-banner" type="button" onClick={()=>stagePack(data.pending!.rarity)}>
    ✦ {de?'Eine gespeicherte Öffnung wartet auf dich – mit denselben fünf Karten. Fortsetzen.':
     'A saved pack reveal is waiting. Resume the same five cards.'}</button>}
   <div className="album-pack-strip">
   {data.packs.map(p=><button type="button" key={p.rarity} data-pack-rarity={p.rarity}
    className={'album-pack-choice '+(p.owned>0||data.pending?.rarity===p.rarity?'available':'')}
    style={{'--card-rarity':rarityColors[p.rarity]} as CSSProperties}
    disabled={!!data.pending&&data.pending.rarity!==p.rarity||p.owned===0&&data.pending?.rarity!==p.rarity}
    onClick={()=>stagePack(p.rarity)}>
    <span className="album-pack-glyph">♠</span><span className="album-pack-label">{rarityTitle(p.rarity,de)}</span>
    <b>{data.pending?.rarity===p.rarity?(de?'FORTSETZEN':'RESUME'):p.owned+' ×'}</b>
    <small>{de?'Garantiert':'Guaranteed'} ≥ {p.guaranteed}</small>
   </button>)}
   </div>
   <div className="album-duplicate-meter"><span>{de?'Duplikate werden zu neuen Packs':'Duplicates recycle into fresh packs'}</span>
     <div role="progressbar" aria-valuenow={data.duplicatePoints} aria-valuemin={0} aria-valuemax={data.duplicatePackCost}
      aria-label="Duplicate points"><i style={{width:100*data.duplicatePoints/data.duplicatePackCost+'%'}}/></div>
     <strong>{data.duplicatePoints} / {data.duplicatePackCost}</strong></div>
  </section>
  <section className="album-library">
   <aside className="album-themes" aria-label={de?'Sammlungsthemen':'Collection themes'}>
    <h2>{de?'Kollektionen':'Collections'}</h2>
    {themes.map(t=><button type="button" key={t.id} data-theme-id={t.id} aria-current={t.id===theme?.id?'true':undefined}
    className={'album-theme-item '+(t.id===theme?.id?'selected ':'')+(t.ultraRare?'ultra':'')}
    style={{'--theme-accent':t.accent} as CSSProperties} onClick={()=>{setThemeId(t.id);setFilter('all');setQuery('');}}>
    <span className="album-theme-symbol">{t.symbol}</span><span className="album-theme-name">{t.name}
     <small>{t.ultraRare?'ULTRA RARE':'COLLECTION'}</small></span><strong>{t.owned}/{t.total}</strong></button>)}
   </aside>
   <div className="album-gallery">
   {theme&&<><header className="album-gallery-header" style={{'--theme-accent':theme.accent} as CSSProperties}>
    <div><span className="shop-eyebrow">{fullTitle}</span>
    <h2><span aria-hidden="true">{theme.symbol}</span> {theme.name}</h2>
    <p>{theme.owned}/{theme.total} {de?'Karten entdeckt':'cards discovered'}</p></div>
    <div className="album-theme-progress" role="progressbar" aria-valuemin={0} aria-valuemax={theme.total}
     aria-valuenow={theme.owned}><span style={{width:(theme.owned/theme.total)*100+'%'}}/></div>
   </header>
   <div className="album-gallery-tools"><div className="record-category-list">
   {(['all','owned','missing'] as const).map(k=><button className={'record-category '+(filter===k?'active':'')}
    key={k} onClick={()=>setFilter(k)} type="button">{k==='all'?(de?'Alle':'All'):k==='owned'?(de?'Gefunden':'Owned'):(de?'Fehlend':'Missing')}</button>)}</div>
    <label className="record-search"><span className="sr-only">{de?'Karten suchen':'Search cards'}</span>
     <span aria-hidden="true">⌕</span><input type="search" value={query} onChange={e=>setQuery(e.target.value)}
      placeholder={de?'Motiv suchen…':'Search card titles…'}/></label></div>
   <div className={'album-card-grid '+(theme.ultraRare?'ultra':'')} data-album-theme={theme.id}>
    {visibleCards.map(card=><CollectionCard key={card.id} de={de} index={card.number}
      card={{...card,theme:theme.name,ultraRare:theme.ultraRare}}
      onInspect={()=>card.owned&&setInspectorId(card.id)}/>)}
   </div>
   {visibleCards.length===0&&<p className="record-empty">{de?'Keine passenden Karten.':'No cards match the filter.'}</p>}
   <div className="album-theme-reward" style={{'--theme-accent':theme.accent} as CSSProperties}>
    <div><span className="shop-eyebrow">{de?'KOLLEKTIONSBONUS':'THEME COMPLETION REWARD'}</span>
      <h3>{theme.rewardCoins.toLocaleString(language)} ◆ · {theme.rewardBankroll} · {theme.rewardPack} Pack</h3>
      <p>{theme.rewardClaimed?(de?'Belohnung erhalten.':'Reward claimed.'):
       theme.rewardReady?(de?'Vollständiges Set! Deine Belohnung ist bereit.':'Full set! Your reward is ready.'):
       (de?'Sammle alle Karten dieses Themas.':'Collect all cards in this theme.')}</p></div>
    <button type="button" className="record-claim-all" disabled={!theme.rewardReady||claimedId!==null}
     onClick={()=>{claimPending.current=theme.id;setClaimedId(theme.id);onClaim(theme.id);}}>
     {theme.rewardClaimed?'✓':claimedId===theme.id?'…':de?'BELOHNUNG ABHOLEN':'CLAIM REWARD'}</button>
   </div></>}
   </div>
  </section>
  {notice&&<p className="album-notice" role="status">{notice}</p>}
  </>}
  {loaded&&openActive&&<div className="album-opening-scrim" role="dialog" aria-modal="true" aria-label={stageHeader}>
   <div className={'album-opening-panel phase-'+phase} style={{'--card-rarity':rarityColors[rarity]} as CSSProperties}>
    <button type="button" className="album-opening-exit" onClick={()=>{revealSuspended.current=true;setPhase('browse');setBatchRemaining(0);}}>
     {de?'‹ Zum Album':'‹ Back to album'}</button>
    <div className="album-opening-intro"><span className="shop-eyebrow">MIDNIGHT CLUB · BOOSTER REVEAL</span>
     <h2>{stageHeader}</h2>
     <p>{phase==='stage'
      ?(de?`${selectedPackCount} versiegelte ${rarityTitle(rarity,true)}-Packs verfügbar. Noch wurde keines verbraucht.`:
       `${selectedPackCount} sealed ${rarity} packs. No pack has been consumed yet.`)
      :phase==='finished'?(de?'Alle Ergebnisse sind sicher in deiner Sammlung gespeichert.':'All rewards are already saved in your collection.'):
       (de?'Jede Karte wurde sicher gespeichert, bevor das Siegel geöffnet wurde.':
        'Every card was committed before this reveal began.')}</p></div>
    {(phase==='stage'||phase==='await')?<div className="album-booster-stage">
     {/* Controls appear before decorative artwork, even on a short screen. */}
     <div className="album-opening-actions">
      {phase==='stage'&&<>
       <button type="button" className="album-opening-primary" disabled={busy||selectedPackCount<=0}
        onClick={()=>startPack(false)}>{de?'EIN PACK ÖFFNEN':'OPEN ONE PACK'}</button>
       {selectedPackCount>1&&<button type="button" className="album-opening-secondary"
        disabled={busy} onClick={()=>startPack(true)}>
        {de?`ALLE ${selectedPackCount} PACKS ÖFFNEN`:`OPEN ALL ${selectedPackCount} PACKS`}</button>}
      </>}
      {phase==='await'&&<p role="status">{unresponsive
       ?(de?'Du kannst sicher zum Album zurückkehren.':'You can safely return to the collection.')
       :(de?'Das nächste Pack wird sicher verarbeitet…':'Preparing the next committed pack…')}</p>}
     </div>
     <AlbumBooster rarity={rarity} tearing={phase==='await'}/>
    </div>:<>
    <div className="album-reveal-banner"><span>{rarityTitle(rarity,de).toUpperCase()}</span>
     <span>{phase==='finished'?'✓':Math.min(revealed,5)+'/5'} {de?'Karten':'cards'}</span>
     {batchRemaining>1&&<span>{de?'Öffnungsserie':'Batch sequence'} · {batchRemaining} {de?'übrig':'remaining'}</span>}</div>
    <div className="album-reveal-grid" aria-label={de?'Pack-Ergebnisse':'Pack results'}>
     {opening?.cards.map((card,i)=><div key={opening.id+'-'+i}
      className={'album-reveal-slot '+(i<revealed?'flipped ':'')+(card.ultraRare?'ultra ':'')+card.rarity.toLowerCase()}
      style={{'--card-rarity':rarityColors[card.rarity],'--reveal-i':i} as CSSProperties}>
      <div className="album-reveal-inner"><div className="album-reveal-back">
       <span>♠</span><small>THE MIDNIGHT CLUB</small></div>
       <div className="album-reveal-front">
        <CollectionCard card={drawCardView(card,de)} de={de}/>
       </div></div>
       {i<revealed&&<span className="album-reveal-flare" aria-hidden="true"/>}
     </div>)}
    </div>
    {opening&&revealed===5&&<div className="album-opening-summary" role="status">
     <span><strong>{opening.newCards}</strong> {de?'neue Karten':'new cards'}</span>
     <span><strong>+{opening.pointsEarned}</strong> {de?'Duplikatpunkte':'duplicate points'}</span>
     <span><strong>+{opening.packsEarned}</strong> {de?'Bonus-Packs':'bonus packs'}</span></div>}
    <div className="album-opening-actions">
     {phase==='reveal'&&<button type="button" className="album-opening-secondary"
      onClick={()=>setRevealed(5)}>{de?'ENTHÜLLUNG ÜBERSPRINGEN':'REVEAL ALL NOW'}</button>}
     {phase==='summary'&&<button type="button" className="album-opening-primary"
      disabled={busy||!data.pending} onClick={finishPack}>
      {busy?'…':batchRemaining>1?(de?'SERIE FORTSETZEN':'CONTINUE SEQUENCE'):(de?'KARTEN EINSAMMELN':'COLLECT CARDS')}</button>}
     {phase==='summary'&&batchRemaining>1&&<button type="button" className="album-opening-secondary"
      onClick={()=>setBatchRemaining(1)}>{de?'Nach diesem Pack stoppen':'Stop after this pack'}</button>}
     {phase==='finished'&&<>
      <button type="button" className="album-opening-primary" onClick={()=>setPhase('browse')}>
       {de?'ZUR SAMMLUNG':'BACK TO COLLECTION'}</button>
      {(currentPack?.owned??0)>0&&<button type="button" className="album-opening-secondary"
       onClick={()=>{setPhase('stage');setDisplayOpening(null);}}>{de?'WEITERES PACK ÖFFNEN':'OPEN ANOTHER PACK'}</button>}
     </>}
    </div></>}
    {notice&&<p className="album-opening-notice" role="status">{notice}</p>}
   </div>
  </div>}
  {inspectorId&&activeCard?.card.owned&&<Modal className="album-inspector-backdrop" label={activeCard.card.name} onClose={()=>setInspectorId(null)}>
   <div className="album-inspector" onClick={e=>e.stopPropagation()}>
    <button type="button" className="album-inspector-close" onClick={()=>setInspectorId(null)}
     aria-label={de?'Schließen':'Close'}>×</button>
    <div className="album-inspector-art">
     <CollectionCard de={de} card={{...activeCard.card,theme:activeCard.theme.name,ultraRare:activeCard.theme.ultraRare}}/>
    </div>
    <div className="album-inspector-lore"><span className="shop-eyebrow">THE MIDNIGHT CLUB · ARCHIVE ENTRY</span>
     <h2>{activeCard.card.name}</h2><strong>{activeCard.theme.name} · {activeCard.card.rarity}</strong>
     <p>{activeCard.card.lore|| (de?'Dieser Eintrag ist noch versiegelt.':'This archive entry remains sealed.')}</p>
     <button type="button" className="record-reset" onClick={()=>setInspectorId(null)}>
      {de?'Zurück zum Album':'Return to album'}</button>
    </div>
   </div>
  </Modal>}
 </div></main>;
}
