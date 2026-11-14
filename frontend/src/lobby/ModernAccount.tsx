import {useEffect,useState} from 'react';
import {PlayerPortrait,CosmeticTitle,portraitPath} from './Cosmetics';
import {NavIcon} from './NavIcon';
import {Modal} from './Modal';
import {localizedSudokuTitle} from './sudokuTitleLocalization';
import type {AccountSnapshot,PrestigeOutcome} from './recordsBridge';
import './account.css';

type Props={
 data:AccountSnapshot;loaded:boolean;language:'en'|'de';pending:string|null;
 notice:{success:boolean;message:string}|null;prestigeOutcome:PrestigeOutcome|null;
 motionEnabled:boolean;
 onBack:()=>void;onRoute:(route:string)=>void;
 onRename:(name:string)=>void;onEquip:(id:string)=>void;
 onPrestige:()=>void;onExport:()=>void;onFolder:()=>void;
};
type Ceremony='confirm'|'ascending'|'complete'|null;

/** Shared readable, high-contrast prestige heraldry for the profile and the lobby header. */
export function PrestigeMark({prestige,size='large'}:{prestige:number;size?:'compact'|'large'|'ceremony'}){
 return <span className={'prestige-seal prestige-seal--'+size} aria-label={'Prestige '+prestige}>
   <span className="prestige-seal__crown" aria-hidden="true">♛</span>
   <span className="prestige-seal__plate">
     {size!=='compact'&&<span className="prestige-seal__label">PRESTIGE</span>}
     <b>P{prestige}</b>
   </span>
   {size!=='compact'&&<span className="prestige-seal__laurel" aria-hidden="true">✦</span>}
 </span>;
}

const routeItems=[
 {id:'season',icon:'✦',en:'Season Pass',de:'Season Pass'},
 {id:'shop',icon:'♢',en:'Cosmetic Shop',de:'Kosmetik-Shop'},
 {id:'album',icon:'▤',en:'Collector’s Album',de:'Sammleralbum'},
 {id:'challenges',icon:'♛',en:'Challenges',de:'Herausforderungen'},
 {id:'statistics',icon:'▥',en:'Statistics',de:'Statistiken'},
 {id:'history',icon:'♠',en:'Hand History',de:'Spielverlauf'},
];

export function ModernAccount({data,loaded,language,pending,notice,prestigeOutcome,motionEnabled,
 onBack,onRoute,onRename,onEquip,onPrestige,onExport,onFolder}:Props){
 const de=language==='de';
 const [editorOpen,setEditorOpen]=useState(false);
 const [detailsOpen,setDetailsOpen]=useState(false);
 const [name,setName]=useState(data.displayName);
 const [search,setSearch]=useState('');
 const [group,setGroup]=useState<'portraits'|'titles'>('portraits');
 const [ceremony,setCeremony]=useState<Ceremony>(null);
 const [ceremonyError,setCeremonyError]=useState<string|null>(null);
 const nextPrestige=data.prestige+1;
 const eligible=loaded&&data.level>=100;
 const busy=pending!==null;
 useEffect(()=>setName(data.displayName),[data.displayName]);
 useEffect(()=>{
   if(!prestigeOutcome)return;
   if(prestigeOutcome.success){setCeremonyError(null);setCeremony('ascending');}
   else{setCeremonyError(prestigeOutcome.message);setCeremony('confirm');}
 },[prestigeOutcome]);
 useEffect(()=>{
   if(ceremony!=='ascending')return;
   if(!motionEnabled){setCeremony('complete');return;}
   const timer=window.setTimeout(()=>setCeremony('complete'),3300);
   return()=>window.clearTimeout(timer);
 },[ceremony,motionEnabled]);
 const options=(group==='portraits'?data.portraits:data.titles).filter(item=>
   (group==='titles'?localizedSudokuTitle(item.id,item.name,language):item.name)
     .toLocaleLowerCase().includes(search.trim().toLocaleLowerCase()));
 const title=<CosmeticTitle variant={data.titleVariant}>
   {localizedSudokuTitle(data.equippedTitleId,data.title,language)}
 </CosmeticTitle>;
 return <main className="record-page account-page premium-account">
 <div className="record-page-scroll account-scroll">
 <header className="account-cinematic-header">
   <div className="account-header-copy">
     <button type="button" className="shop-back account-back" onClick={onBack}>
       {de?'‹ Zur Lobby':'‹ Back to lobby'}</button>
     <span className="shop-eyebrow">{de?'THE MIDNIGHT CLUB / SPIELER':'THE MIDNIGHT CLUB / PLAYER'}</span>
     <h1>{de?'Spielerprofil':'Player Profile'}</h1>
     <p>{de?'Identität, Sammlung und Fortschritt an einem Ort.':'Identity, collection and progression in one place.'}</p>
   </div>
   <span className="account-header-monogram" aria-hidden="true">♠</span>
 </header>
 {!loaded?<p className="record-empty" role="status">{de?'Profil wird geladen…':'Loading profile…'}</p>:<>
 <section className="account-identity-stage" aria-label={de?'Spieleridentität und Prestige':'Player identity and prestige'}>
   <div className="account-identity-core">
     <div className="account-portrait-showcase">
       <PlayerPortrait portrait={data.portraitVariant??data.portraits.find(p=>p.id===data.equippedPortraitId)?.visualVariant}
         frame={data.frameVariant} large/>
     </div>
     <div className="account-identity-text">
       <span className="account-kicker">{de?'MITGLIED DER MIDNIGHT CLUB':'THE MIDNIGHT CLUB MEMBER'}</span>
       <h2>{data.displayName}</h2>
       <div className="account-player-title">{title}</div>
       <div className="account-identity-ornament" aria-hidden="true">✦ ───── ✦</div>
     </div>
     <PrestigeMark prestige={data.prestige}/>
   </div>
   <div className={'account-ascend-card '+(eligible?'is-eligible':'is-locked')}>
     <div className="account-ascend-copy">
       <span className="account-kicker">{eligible?(de?'✦ PRESTIGE-AUFSTIEG VERFÜGBAR ✦':'✦ PRESTIGE UPGRADE AVAILABLE ✦'):
         (de?'DER NÄCHSTE MEILENSTEIN':'YOUR NEXT MILESTONE')}</span>
       <h2>{eligible?(de?'Aufstieg zu Prestige '+nextPrestige:'Ascend to Prestige '+nextPrestige):
         (de?'Nächstes Prestige freischalten':'Unlock your next Prestige')}</h2>
       <p>{eligible?(de?'Level 100 erreicht. Dein nächster Aufstieg wartet.':'Level 100 reached. Your next ascension awaits.'):
         (de?'Erreiche Level 100, um deinen nächsten Prestige-Rang zu erhalten.':
           'Reach level 100 to earn your next Prestige rank.')}</p>
       <div className="account-prestige-rewards" aria-label={de?'Prestige-Belohnungen':'Prestige rewards'}>
         <span>+500 {de?'Münzen':'Coins'}</span><span>{de?'1 legendäres Pack':'1 Legendary pack'}</span>
         <span>{de?'2 epische Packs':'2 Epic packs'}</span>
       </div>
       {eligible?<button type="button" className="account-ascend-button" disabled={busy}
         onClick={()=>{setCeremonyError(null);setCeremony('confirm');}}>
         <span aria-hidden="true">♛</span> {de?'ZU PRESTIGE '+nextPrestige+' AUFSTEIGEN':'ASCEND TO PRESTIGE '+nextPrestige}
         <span aria-hidden="true">›</span></button>:
         <div className="account-level-goal" role="progressbar" aria-valuemin={0} aria-valuemax={100}
           aria-valuenow={Math.min(100,data.level)}>
           <span style={{width:Math.min(100,data.level)+'%'}}/>
           <b>{de?'LEVEL':'LEVEL'} {data.level} / 100</b>
         </div>}
     </div>
     <div className="account-ascend-art" aria-hidden="true">
       <span className="account-art-orbit account-art-orbit--outer"/>
       <span className="account-art-orbit account-art-orbit--inner"/>
       <PrestigeMark prestige={nextPrestige} size="ceremony"/>
       <span className="account-art-pedestal"/>
       <span className="account-art-spark account-art-spark--one">✦</span>
       <span className="account-art-spark account-art-spark--two">✧</span>
     </div>
   </div>
 </section>
 <section className="account-stat-grid" aria-label={de?'Spielerstatistik':'Player summary'}>
   {[['♛',de?'LEVEL':'LEVEL',String(data.level)],['◈',de?'MÜNZEN':'COINS',data.coins],
     ['⌛',de?'SPIELZEIT':'PLAYTIME',data.playtime],['✦','XP',data.profileExperience]]
     .map(([symbol,label,value])=><article key={label} className="account-stat-card">
       <span className="account-stat-icon" aria-hidden="true">{symbol}</span>
       <span className="account-stat-copy"><small>{label}</small><strong>{value}</strong></span>
     </article>)}
 </section>
 <section className="account-progression account-panel">
   <header className="account-section-header">
     <div><span className="account-kicker">THE MIDNIGHT CLUB / {de?'FORTSCHRITT':'PROGRESSION'}</span>
       <h2>{de?'Dein Fortschritt':'Your Progression'}</h2>
       <p>{data.ownedCosmetics} {de?'Kosmetikartikel':'cosmetics'} · {data.ownedCards}/{data.totalCards}
         {de?' Albumkarten':' album cards'} · <strong>{data.season}</strong></p>
     </div><span className="account-section-crest" aria-hidden="true">♠</span>
   </header>
   <div className="account-route-grid">
     {routeItems.map(item=><button type="button" className={'account-route-card account-route--'+item.id}
       key={item.id} onClick={()=>onRoute(item.id)}>
       <span className="account-route-art" aria-hidden="true">{item.icon}</span>
       <strong>{de?item.de:item.en}</strong><span className="account-route-arrow" aria-hidden="true">›</span>
     </button>)}
     <button type="button" className="account-route-card account-route--edit"
       onClick={()=>setEditorOpen(v=>!v)} aria-expanded={editorOpen} aria-controls="account-identity-editor">
       <span className="account-route-art" aria-hidden="true">✎</span>
       <strong>{de?'Identität bearbeiten':'Edit Identity'}</strong>
       <span className="account-route-arrow" aria-hidden="true">›</span>
     </button>
   </div>
   <button type="button" className="account-options-toggle" onClick={()=>setDetailsOpen(v=>!v)}
     aria-expanded={detailsOpen} aria-controls="account-data-options">
     <span aria-hidden="true">⚙</span>{de?'Daten & Profiloptionen':'Data & profile options'}
     <span aria-hidden="true">›</span>
   </button>
 </section>
 {detailsOpen&&<section id="account-data-options" className="account-panel account-data-options">
   <div><span className="account-kicker">{de?'LOKALE SPEICHERDATEN':'LOCAL PLAYER DATA'}</span>
     <h2>{de?'Deine Daten verwalten':'Manage your player data'}</h2>
     <p>{de?'Exportiere dein Spielerprofil als JSON oder öffne den zugehörigen Speicherordner.':
       'Export your player profile as JSON, or open its local storage folder.'}</p>
     <p className="account-data-warning">{de?'Ein JSON-Export ist kein vollständiges Backup aller Spieldaten.':
       'A JSON profile export is not a complete backup of every game save.'}</p>
   </div>
   <div className="account-data-buttons">
     <button type="button" disabled={busy} onClick={onExport}>
       <span aria-hidden="true">⇩</span>{de?'Profil als JSON exportieren':'Export profile JSON'}</button>
     <button type="button" disabled={busy} onClick={onFolder}>
       <span aria-hidden="true">▣</span>{de?'Speicherordner öffnen':'Open save folder'}</button>
   </div>
   {notice&&<p role="status" className={'account-data-notice '+(notice.success?'success':'error')}>
     {notice.message}</p>}
 </section>}
 {editorOpen&&<section id="account-identity-editor" className="account-panel identity-editor" aria-label={de?'Profil bearbeiten':'Edit identity'}>
   <h2>{de?'Identität gestalten':'Customize identity'}</h2>
   <form className="identity-rename" onSubmit={event=>{event.preventDefault();if(!pending)onRename(name);}}>
     <label htmlFor="identity-name">{de?'Anzeigename':'Display name'}</label>
     <div><input id="identity-name" type="text" maxLength={80} value={name}
       onChange={e=>setName(e.target.value)} autoComplete="off"/>
       <button className="record-claim-all" disabled={busy||name.trim().length===0}
         type="submit">{pending==='rename'?'…':(de?'Namen speichern':'Save name')}</button></div>
     <small>{de?'Maximal 24 sichtbare Zeichen. Wird im lokalen Profil gespeichert.':
       'Maximum 24 visible characters. Saved to your local profile.'}</small>
   </form>
   <div className="identity-controls"><div className="record-category-list">
     <button type="button" className={'record-category '+(group==='portraits'?'active':'')}
       onClick={()=>setGroup('portraits')}>{de?'Porträts':'Portraits'} · {data.portraits.length}</button>
     <button type="button" className={'record-category '+(group==='titles'?'active':'')}
       onClick={()=>setGroup('titles')}>{de?'Titel':'Titles'} · {data.titles.length}</button>
   </div><label className="record-search"><span className="sr-only">{de?'Suchen':'Search'}</span>
     <span aria-hidden="true">⌕</span><input type="search" value={search} onChange={e=>setSearch(e.target.value)}
       placeholder={de?'Sammlung durchsuchen…':'Search collection…'}/></label></div>
   <div className="identity-cosmetics">
     {options.map(item=>{
       const active=item.id===(group==='portraits'?data.equippedPortraitId:data.equippedTitleId);
       return <article className={'identity-cosmetic '+(active?'selected':'')} key={item.id}>
         <div className="identity-cosmetic-symbol" aria-hidden="true">
           {group==='portraits'&&portraitPath(item.visualVariant)?
             <img alt="" src={portraitPath(item.visualVariant)} loading="lazy"
               onError={e=>{e.currentTarget.style.display='none';}}/>:group==='portraits'?'♠':'✦'}
         </div><div><strong>{group==='titles'?<CosmeticTitle variant={item.visualVariant}>
           {localizedSudokuTitle(item.id,item.name,language)}</CosmeticTitle>:item.name}</strong>
           <p>{item.description}</p></div>
         <button type="button" disabled={active||busy} onClick={()=>onEquip(item.id)}>
           {active?(de?'AUSGERÜSTET':'EQUIPPED'):pending===item.id?'…':(de?'AUSRÜSTEN':'EQUIP')}</button>
       </article>;
     })}
     {options.length===0&&<p className="record-empty">{de?'Keine passenden Gegenstände.':'No matching items.'}</p>}
   </div>
 </section>}
 </>}
 </div>
 {ceremony&&<Modal className={'account-ceremony-modal '+(ceremony!=='confirm'?'ceremony-playing':'')}
   label={de?'Prestige-Aufstieg':'Prestige ascension'}
   onClose={()=>{if(pending==='prestige')return;setCeremony(ceremony==='ascending'?'complete':null);}}>
   {ceremony==='confirm'?<div className="account-ceremony-confirm">
     <span className="account-kicker">{de?'EIN SELTENER MEILENSTEIN':'A RARE MILESTONE'}</span>
     <PrestigeMark prestige={nextPrestige} size="ceremony"/>
     <h2>{de?'Bereit für Prestige '+nextPrestige+'?':'Ready for Prestige '+nextPrestige+'?'}</h2>
     <p>{de?'Dein Level wird auf 0 zurückgesetzt. Erhalten bleiben dein gesammelter Fortschritt, Coins, Statistiken und Kosmetik. Du bekommst 500 Coins, ein legendäres und zwei epische Kartenpacks.':
       'Your level resets to 0. Your collected progress, coins, statistics and cosmetics are kept. You earn 500 coins, one Legendary and two Epic card packs.'}</p>
     {ceremonyError&&<p className="account-ceremony-error" role="alert">{ceremonyError}</p>}
     <div className="account-ceremony-actions">
       <button type="button" disabled={pending==='prestige'} onClick={()=>setCeremony(null)}>
         {de?'Abbrechen':'Cancel'}</button>
       <button type="button" className="account-ascend-button" disabled={busy} onClick={onPrestige}>
         {pending==='prestige'?'…':(de?'AUFSTIEG BESTÄTIGEN':'CONFIRM ASCENSION')}</button>
     </div>
   </div>:<div className={'account-ceremony-reveal '+(ceremony==='complete'?'ceremony-done':'')}>
     <div className="account-ceremony-rings" aria-hidden="true"><i/><i/><i/></div>
     <div className="account-ceremony-flare" aria-hidden="true">✦</div>
     <PrestigeMark prestige={prestigeOutcome?.prestige??nextPrestige} size="ceremony"/>
     <span className="account-kicker">{de?'DER NÄCHSTE ABSCHNITT BEGINNT':'A NEW CHAPTER BEGINS'}</span>
     <h2>{de?'Prestige '+(prestigeOutcome?.prestige??nextPrestige)+' erreicht!':
       'Prestige '+(prestigeOutcome?.prestige??nextPrestige)+' achieved!'}</h2>
     <p>{de?'Dein neuer Rang wurde sicher gespeichert.':'Your new rank has been saved.'}</p>
     <div className="account-ceremony-reward-row">+500 {de?'Münzen':'Coins'} · 1 {de?'legendäres':'Legendary'} + 2 {de?'epische Kartenpacks':'Epic packs'}</div>
     {ceremony==='ascending'&&motionEnabled?<button className="account-ceremony-skip" type="button"
       onClick={()=>setCeremony('complete')}>{de?'Animation überspringen':'Skip animation'}</button>:
       <button type="button" className="account-ascend-button account-ceremony-finish"
         onClick={()=>setCeremony(null)}>{de?'WEITER ZUM PROFIL':'RETURN TO PROFILE'}</button>}
   </div>}
 </Modal>}
 </main>;
}
