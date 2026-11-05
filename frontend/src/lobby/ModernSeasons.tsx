import {useState} from 'react';
import type {SeasonsSnapshot} from './recordsBridge';
type Props={data:SeasonsSnapshot;loaded:boolean;language:'en'|'de';pending:string|null;onBack:()=>void;onActivate:(id:string)=>void};
export function ModernSeasons({data,loaded,language,pending,onBack,onActivate}:Props){
 const de=language==='de';const [id,setId]=useState<string|null>(null);
 const selected=data.seasons.find(s=>s.id===id)??data.seasons.find(s=>s.id===data.activeSeasonId)??data.seasons[0];
 const active=selected?.id===data.activeSeasonId;
 const progress=selected?.maxExperience?Math.max(0,Math.min(100,selected.experience/selected.maxExperience*100)):0;
 return <main className="record-page season-page"><div className="record-page-scroll">
 <header className="record-hero"><div className="record-hero-copy">
 <button className="shop-back" onClick={onBack}>{de?'‹ Zur Lobby':'‹ Back to lobby'}</button>
 <span className="shop-eyebrow">THE MIDNIGHT CLUB / SEASON PASS LIBRARY</span>
 <h1>Season Pass</h1>
 <p>{de?'Jeder Pass hat einen eigenen Fortschritt. Nur der aktive Pass sammelt neue Saison-XP.':
 'Each pass preserves its own progress. Only the active pass receives new Season XP.'}</p></div>
 <div className="record-hero-side"><span>{de?'AKTIVER PASS':'ACTIVE PASS'}</span>
 <strong className="season-title">{data.seasons.find(s=>s.id===data.activeSeasonId)?.name??'—'}</strong></div></header>
 {!loaded||!selected?<p role="status" className="record-empty">{de?'Pass wird geladen…':'Loading pass…'}</p>:<>
 <div className="season-library" role="tablist" aria-label="Season Pass">
 {data.seasons.map(s=><button role="tab" aria-selected={s.id===selected.id} key={s.id}
 className={s.id===selected.id?'selected':''} onClick={()=>setId(s.id)}>
 <strong>{s.name}</strong><span>Tier {s.tier}/{s.totalTiers}</span>
 {s.id===data.activeSeasonId&&<small>{de?'AKTIV':'ACTIVE'}</small>}</button>)}</div>
 <section className="account-panel"><div className="season-heading">
 <div><span className="shop-eyebrow">{selected.archived?'LEGACY PASS':'SEASON PASS'}</span>
 <h2>{selected.name}</h2><p>{selected.description}</p></div>
 <button className="record-claim-all" disabled={active||pending!==null}
 onClick={()=>onActivate(selected.id)}>{active?(de?'AKTIVER PASS':'ACTIVE PASS'):pending===selected.id?'…':
 (de?'PASS AKTIVIEREN':'ACTIVATE PASS')}</button></div>
 <div className="season-meta"><strong>Tier {selected.tier}/{selected.totalTiers}</strong>
 <span>{selected.experience.toLocaleString(language)} / {selected.maxExperience.toLocaleString(language)} XP</span></div>
 <div className="lobby-meter gold season-meter"><span style={{width:progress+'%'}}/></div>
 <h2>{de?'Belohnungsstufen':'Reward tiers'}</h2>
 <div className="season-tiers">{selected.tiers.map(t=><article key={t.tier} className={t.tier<=selected.tier?'earned':''}>
 <b>Tier {t.tier}</b><small>{t.requiredExperience.toLocaleString(language)} XP</small>
 <span>{selected.rewardsEnabled?[
 t.coins>0?'◆ '+t.coins.toLocaleString(language):'',
 t.cosmeticId?t.cosmeticId.replace(/-/g,' '):'',
 t.bankrollReward>0?'$'+t.bankrollReward.toLocaleString(language):''
 ].filter(Boolean).join(' · ')||'—':(de?'Fortschritt':'Progress')}</span>
 {t.tier<=selected.tier&&<em>✓</em>}</article>)}</div></section></>}</div></main>;
}
