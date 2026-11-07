import {useState} from 'react';
import type {SeasonsSnapshot,SeasonTier} from './recordsBridge';
import './season.css';

type Props={data:SeasonsSnapshot;loaded:boolean;language:'en'|'de';pending:string|null;
 onBack:()=>void;onActivate:(id:string)=>void};
type TierView='nearby'|'highlights'|'all';

export function seasonTheme(id:string,name:string):'midnight'|'eclipse'|'neon'{
 const value=(id+' '+name).toLowerCase();
 if(value.includes('neon')||value.includes('dynasty'))return 'neon';
 if(value.includes('eclipse')||value.includes('velvet'))return 'eclipse';
 return 'midnight';
}
function rewardText(t:SeasonTier,de:boolean,rewardsEnabled:boolean):string[]{
 if(!rewardsEnabled)return [de?'Fortschritt':'Progress'];
 const rewards:string[]=[];
 if(t.coins>0)rewards.push('◆ '+t.coins.toLocaleString(de?'de':'en')+' '+(de?'Münzen':'coins'));
 if(t.bankrollReward>0)rewards.push('$'+t.bankrollReward.toLocaleString(de?'de':'en'));
 if(t.cosmeticId)rewards.push(t.cosmeticId.replace(/[-_]/g,' ').replace(/^\s*title\s*/i,''));
 return rewards.length?rewards:[de?'Freigeschaltet durch Fortschritt':'Progress milestone'];
}
function isHighlight(t:SeasonTier):boolean{
 return t.cosmeticId!==null||t.bankrollReward>0||t.tier%10===0;
}
export function ModernSeasons({data,loaded,language,pending,onBack,onActivate}:Props){
 const de=language==='de';
 const [id,setId]=useState<string|null>(null);
 const [tierView,setTierView]=useState<TierView>('nearby');
 const selected=data.seasons.find(s=>s.id===id)??data.seasons.find(s=>s.id===data.activeSeasonId)??data.seasons[0];
 const active=selected?.id===data.activeSeasonId;
 const theme=selected?seasonTheme(selected.id,selected.name):'midnight';
 const progress=selected?.maxExperience?Math.max(0,Math.min(100,selected.experience/selected.maxExperience*100)):0;
 const nextTier=selected?.tiers.find(t=>t.tier>selected.tier);
 const nextHighlight=selected?.tiers.find(t=>t.tier>selected.tier&&isHighlight(t));
 const recentHighlight=selected?.tiers.filter(isHighlight).at(-1);
 const feature=nextHighlight??nextTier??recentHighlight;
 const highlightCount=selected?.tiers.filter(isHighlight).length??0;
 const nearbyStart=selected?Math.max(1,Math.min(selected.tier-2,selected.totalTiers-11)):1;
 const nearbyEnd=Math.min(selected?.totalTiers??1,nearbyStart+11);
 const visible=selected?.tiers.filter(t=>tierView==='all'
  ||(tierView==='highlights'&&isHighlight(t))
  ||(tierView==='nearby'&&t.tier>=nearbyStart&&t.tier<=nearbyEnd))??[];
 return <main className="record-page season-page" data-season-theme={theme}>
 <div className="record-page-scroll">
 <header className="record-hero season-hero"><div className="record-hero-copy">
  <button className="shop-back" onClick={onBack}>{de?'‹ Zur Lobby':'‹ Back to lobby'}</button>
  <span className="shop-eyebrow">THE MIDNIGHT CLUB / SEASON PASS LIBRARY</span>
  <h1>Season Pass</h1>
  <p>{de?'Jeder Pass behält seinen eigenen Fortschritt. Neue Saison-XP erhält nur der aktive Pass.':
   'Every pass retains its own progress. Only the active pass earns new Season XP.'}</p>
 </div>
 <div className="record-hero-side"><span>{de?'AKTIVER PASS':'ACTIVE PASS'}</span>
  <strong className="season-title">{data.seasons.find(s=>s.id===data.activeSeasonId)?.name??'—'}</strong>
  <small>{de?'Deine laufende Saison':'Your current season'}</small>
 </div>
 <span className="season-hero-emblem" aria-hidden="true">✦</span>
 </header>
 {!loaded||!selected?<p role="status" className="record-empty">{de?'Pass wird geladen…':'Loading pass…'}</p>:<>
 <div className="season-library" role="tablist" aria-label={de?'Saisons auswählen':'Choose season'}>
 {data.seasons.map(s=>{
  const itemTheme=seasonTheme(s.id,s.name);
  return <button type="button" role="tab" aria-selected={s.id===selected.id}
    key={s.id} data-season-theme={itemTheme} className={s.id===selected.id?'selected':''}
    onClick={()=>{setId(s.id);setTierView('nearby');}}>
   <span className="season-library-icon" aria-hidden="true">{itemTheme==='eclipse'?'☾':itemTheme==='neon'?'✧':'✦'}</span>
   <strong>{s.name}</strong>
   <span>{de?'Stufe':'Tier'} {s.tier}/{s.totalTiers}</span>
   <span className="season-library-state">{s.id===data.activeSeasonId?(de?'AKTIV':'ACTIVE'):s.archived?(de?'ARCHIV':'ARCHIVE'):'PASS'}</span>
  </button>;
 })}</div>
 <section className="account-panel season-pass-panel">
 <div className="season-heading">
  <div className="season-heading-copy">
   <span className="shop-eyebrow">{selected.archived?'LEGACY PASS':'SEASON PASS'}</span>
   <h2>{selected.name}</h2><p>{selected.description}</p>
  </div>
  <button type="button" className="record-claim-all season-activate"
   disabled={active||pending!==null} onClick={()=>onActivate(selected.id)}>
   {active?(de?'AKTIVER PASS':'ACTIVE PASS'):pending===selected.id?'…':
   (de?'PASS AKTIVIEREN':'ACTIVATE PASS')}
  </button>
 </div>
 <div className="season-progress">
  <div className="season-progress-heading">
   <strong>{de?'Stufe':'Tier'} <b>{selected.tier}</b><span>/{selected.totalTiers}</span></strong>
   <span>{selected.experience.toLocaleString(language)} / {selected.maxExperience.toLocaleString(language)} XP</span>
  </div>
  <div className="season-progress-track" role="progressbar" aria-label={de?'Saisonfortschritt':'Season progress'}
   aria-valuemin={0} aria-valuemax={100} aria-valuenow={Math.round(progress)}>
   <span style={{width:progress+'%'}}/>
  </div>
  <div className="season-progress-caption"><span>{selected.archived?(de?'Archivierter Fortschritt':'Archived progress'):(de?'Dein Fortschritt':'Your progression')}</span>
   <span>{Math.round(progress)}%</span></div>
 </div>
 {feature&&<div className="season-featured">
  <span className="season-featured-sigil" aria-hidden="true">✦</span>
  <div><span className="shop-eyebrow">{de?'NÄCHSTE BESONDERE BELOHNUNG':'NEXT FEATURED REWARD'}</span>
   <strong>{de?'Stufe':'Tier'} {feature.tier}</strong>
   <p>{rewardText(feature,de,selected.rewardsEnabled).join(' · ')}</p></div>
  <span className="season-featured-xp">{feature.requiredExperience.toLocaleString(language)} XP</span>
 </div>}
 <div className="season-rewards-heading">
  <div><span className="shop-eyebrow">{de?'BELOHNUNGSWEG':'REWARD JOURNEY'}</span>
   <h2>{de?'Belohnungsstufen':'Reward tiers'}</h2>
   <p>{de?'Abgeschlossene Stufen, kommende Belohnungen und besondere Meilensteine.':
    'Completed tiers, upcoming rewards and special milestones.'}</p></div>
  <div className="season-view-tabs" role="group" aria-label={de?'Belohnungen filtern':'Filter rewards'}>
   {(['nearby','highlights','all'] as const).map(view=><button type="button" key={view}
    className={tierView===view?'selected':''} aria-pressed={tierView===view}
    onClick={()=>setTierView(view)}>{view==='nearby'?(de?'Um mich':'In focus'):
     view==='highlights'?(de?'Highlights':'Highlights'):(de?'Alle':'All')}</button>)}
  </div>
 </div>
 <p className="season-view-count" role="status">{de?'Gezeigt':'Showing'}: {visible.length} / {selected.tiers.length}
  {tierView==='highlights'?' · '+highlightCount+' '+(de?'Meilensteine':'milestones'):''}</p>
 <div className="season-tiers">
 {visible.map(t=>{
  const earned=t.tier<=selected.tier;
  const upcoming=t.tier===selected.tier+1;
  const standout=isHighlight(t);
  return <article key={t.tier} className={'season-tier-card '+(earned?'earned ':'')
   +(upcoming?'is-next ':'')+(standout?'milestone ':'')}
   aria-label={(de?'Stufe ':'Tier ')+t.tier+(earned?(de?', abgeschlossen':', completed'):'')}>
   <div className="season-tier-top"><span>{de?'STUFE':'TIER'} {t.tier}</span>
    <span aria-hidden="true">{earned?'✓':upcoming?'✦':standout?'◆':'·'}</span></div>
   <span className="season-tier-symbol" aria-hidden="true">{t.cosmeticId?'✧':t.bankrollReward>0?'◇':t.tier%10===0?'✦':'◆'}</span>
   <strong>{rewardText(t,de,selected.rewardsEnabled).join(' · ')}</strong>
   <small>{t.requiredExperience.toLocaleString(language)} XP</small>
   <span className="season-tier-state">{earned?(de?'ABGESCHLOSSEN':'COLLECTED'):upcoming?(de?'ALS NÄCHSTES':'UP NEXT'):
    standout?(de?'MEILENSTEIN':'MILESTONE'):''}</span>
  </article>;
 })}
 </div>
 </section></>}
 </div></main>;
}
