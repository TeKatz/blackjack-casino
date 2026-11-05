import {useEffect,useState} from 'react';
import type {AccountSnapshot} from './recordsBridge';
const jpgPortraits = new Set(['house-guest','eclipse-raven','eclipse-lady','eclipse-moon',
 'eclipse-sovereign','dynasty-heiress','neon-overseer','neon-emperor','neon-croupier']);
function portraitImage(variant:string):string|null {
 const id=variant.replace(/^portrait-/,'');
 if(!/^[a-z0-9-]+$/.test(id))return null;
 return '/shop/portraits/'+id+'.'+(jpgPortraits.has(id)?'jpg':'png');
}
type Props={
 data:AccountSnapshot;loaded:boolean;language:'en'|'de';pending:string|null;
 onBack:()=>void;onRoute:(route:string)=>void;
 onEdit:()=>void;onRename:(name:string)=>void;onEquip:(id:string)=>void;
};
export function ModernAccount({data,loaded,language,pending,onBack,onRoute,onEdit,onRename,onEquip}:Props){
 const de=language==='de';
 const [editorOpen,setEditorOpen]=useState(false);
 const [name,setName]=useState(data.displayName);
 const [search,setSearch]=useState('');
 const [group,setGroup]=useState<'portraits'|'titles'>('portraits');
 useEffect(()=>setName(data.displayName),[data.displayName]);
 const routes=[['season','✦','Season Pass'],['shop','♢',de?'Kosmetik-Shop':'Cosmetic Shop'],
 ['album','▤',de?'Sammleralbum':'Collector’s Album'],['challenges','♛','Challenges'],
 ['statistics','▥',de?'Statistiken':'Statistics'],['history','▧',de?'Spielverlauf':'Hand History']];
 const options=(group==='portraits'?data.portraits:data.titles).filter(item=>
   item.name.toLocaleLowerCase().includes(search.trim().toLocaleLowerCase()));
 return <main className="record-page account-page"><div className="record-page-scroll">
 <header className="record-hero"><div className="record-hero-copy">
 <button className="shop-back" onClick={onBack}>{de?'‹ Zur Lobby':'‹ Back to lobby'}</button>
 <span className="shop-eyebrow">THE MIDNIGHT CLUB / PLAYER</span>
 <h1>{de?'Spielerprofil':'Player profile'}</h1>
 <p>{de?'Identität, Sammlung und Fortschritt an einem Ort.':'Identity, collection and progression in one place.'}</p>
 </div><div className="record-hero-side"><span>{de?'SPIELER':'PLAYER'}</span>
 <strong className="account-name">{loaded?data.displayName:'…'}</strong><small>{data.title} · P{data.prestige}</small></div></header>
 {!loaded?<p className="record-empty" role="status">{de?'Profil wird geladen…':'Loading profile…'}</p>:<>
 <div className="record-summary account-summary">
 <div><span>LEVEL</span><strong>{data.level}</strong></div>
 <div><span>{de?'MÜNZEN':'COINS'}</span><strong>{data.coins}</strong></div>
 <div><span>{de?'SPIELZEIT':'PLAYTIME'}</span><strong>{data.playtime}</strong></div>
 <div><span>XP</span><strong>{data.profileExperience}</strong></div></div>
 <section className="account-panel">
 <h2>{de?'Dein Fortschritt':'Your progression'}</h2>
 <p>{data.ownedCosmetics} {de?'Kosmetikartikel':'cosmetics'} · {data.ownedCards}/{data.totalCards}
 {de?' Albumkarten':' album cards'} · {data.season}</p>
 <div className="account-routes">{routes.map(([id,icon,title])=>
 <button type="button" key={id} onClick={()=>onRoute(id)}><span>{icon}</span><strong>{title}</strong></button>)}
 <button type="button" className="account-edit-route" onClick={()=>setEditorOpen(v=>!v)}>
 <span>✎</span><strong>{de?'Identität bearbeiten':'Edit identity'}</strong></button>
 <button type="button" onClick={onEdit}><span>⇩</span><strong>{de?'Export & erweiterte Optionen':'Export & advanced options'}</strong></button>
 </div></section>
 {editorOpen&&<section className="account-panel identity-editor" aria-label={de?'Profil bearbeiten':'Edit identity'}>
 <h2>{de?'Identität gestalten':'Customize identity'}</h2>
 <form className="identity-rename" onSubmit={event=>{event.preventDefault();if(!pending)onRename(name);}}>
 <label htmlFor="identity-name">{de?'Anzeigename':'Display name'}</label>
 <div><input id="identity-name" type="text" maxLength={80} value={name}
 onChange={e=>setName(e.target.value)} autoComplete="off"/>
 <button className="record-claim-all" disabled={pending!==null||name.trim().length===0}
 type="submit">{pending==='rename'?'…':(de?'Speichern':'Save name')}</button></div>
 <small>{de?'Maximal 24 sichtbare Zeichen. Wird im lokalen Profil gespeichert.':
 'Maximum 24 visible characters. Saved to your local profile.'}</small>
 </form>
 <div className="identity-controls"><div className="record-category-list">
 <button type="button" className={'record-category '+(group==='portraits'?'active':'')}
 onClick={()=>setGroup('portraits')}>{de?'Porträts':'Portraits'} · {data.portraits.length}</button>
 <button type="button" className={'record-category '+(group==='titles'?'active':'')}
 onClick={()=>setGroup('titles')}>{de?'Titel':'Titles'} · {data.titles.length}</button></div>
 <label className="record-search"><span className="sr-only">{de?'Suchen':'Search'}</span>
 <span aria-hidden="true">⌕</span><input type="search" value={search} onChange={e=>setSearch(e.target.value)}
 placeholder={de?'Sammlung durchsuchen…':'Search collection…'}/></label></div>
 <div className="identity-cosmetics">
 {options.map(item=>{
 const active=item.id===(group==='portraits'?data.equippedPortraitId:data.equippedTitleId);
 return <article className={'identity-cosmetic '+(active?'selected':'')} key={item.id}>
 <div className="identity-cosmetic-symbol" aria-hidden="true">
 {group==='portraits'&&portraitImage(item.visualVariant)?
 <img alt="" src={portraitImage(item.visualVariant)!} loading="lazy"
 onError={e=>{e.currentTarget.style.display='none';}}/>:group==='portraits'?'♠':'✦'}
 </div>
 <div><strong>{item.name}</strong><p>{item.description}</p></div>
 <button type="button" disabled={active||pending!==null} onClick={()=>onEquip(item.id)}>
 {active?(de?'AUSGERÜSTET':'EQUIPPED'):pending===item.id?'…':(de?'AUSRÜSTEN':'EQUIP')}</button>
 </article>;
 })}
 {options.length===0&&<p className="record-empty">{de?'Keine passenden Gegenstände.':'No matching items.'}</p>}
 </div>
 </section>}
 </>}</div></main>;
}
