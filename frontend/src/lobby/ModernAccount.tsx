import type {AccountSnapshot} from './recordsBridge';
type Props={data:AccountSnapshot;loaded:boolean;language:'en'|'de';onBack:()=>void;onRoute:(route:string)=>void;onEdit:()=>void};
export function ModernAccount({data,loaded,language,onBack,onRoute,onEdit}:Props){
 const de=language==='de';
 const routes=[['season','✦','Season Pass'],['shop','♢',de?'Kosmetik-Shop':'Cosmetic Shop'],
 ['album','▤',de?'Sammleralbum':'Collector’s Album'],['challenges','♛','Challenges'],
 ['statistics','▥',de?'Statistiken':'Statistics']];
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
 <section className="account-panel"><h2>{de?'Dein Fortschritt':'Your progression'}</h2>
 <p>{data.ownedCosmetics} {de?'Kosmetikartikel':'cosmetics'} · {data.ownedCards}/{data.totalCards}
 {de?' Albumkarten':' album cards'} · {data.season}</p>
 <div className="account-routes">{routes.map(([id,icon,title])=>
 <button key={id} onClick={()=>onRoute(id)}><span>{icon}</span><strong>{title}</strong></button>)}
 <button onClick={onEdit}><span>♠</span><strong>{de?'Profil bearbeiten / Export':'Edit profile / Export'}</strong></button>
 </div></section></>}</div></main>;
}
