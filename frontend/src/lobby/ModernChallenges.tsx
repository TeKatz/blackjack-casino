import {useMemo,useState} from 'react';
import type {ChallengeStatus,ChallengesSnapshot,ChallengeEntry} from './recordsBridge';
import './records.css';
import {portraitPath} from './Cosmetics';

type Props={
  data:ChallengesSnapshot;loaded:boolean;language:'en'|'de';pending:string|null;
  onBack:()=>void;onClaim:(id:string)=>void;onClaimAll:()=>void;
};
const statuses:{id:ChallengeStatus;en:string;de:string}[]=[
  {id:'all',en:'All',de:'Alle'},
  {id:'ready',en:'Ready to claim',de:'Abholbereit'},
  {id:'progress',en:'In progress',de:'In Arbeit'},
  {id:'claimed',en:'Claimed',de:'Abgeholt'},
  {id:'unclaimed',en:'Unclaimed',de:'Nicht abgeholt'},
];
const categoryIcons:Record<string,string>={
  Blackjack:'♠','Midnight Reels':'▣','Midnight Manor':'☽','Neon Vault':'◇','Nine Lives':'●',
  'Midnight Dice':'⚄',Sudoku:'▦','Word Search':'⌕',Memory:'✦',Solitaire:'♣',
  Jigsaw:'◈','Midnight Merge':'♛','Midnight Bastion':'♜',Adventure:'⚔',Lifetime:'♜',Prestige:'♕',
};
const pct=(v:number)=>Math.round(Math.max(0,Math.min(1,v))*100);
function Card({entry,de,pending,onClaim}:{
  entry:ChallengeEntry;de:boolean;pending:boolean;onClaim:(id:string)=>void;
}){
  const ready=entry.canClaim;
  const label=entry.claimed?(entry.automaticallyAwarded?(de?'Vergeben':'Awarded'):(de?'Abgeholt':'Claimed'))
    :ready?(de?'Bereit':'Ready'):(de?'In Arbeit':'In progress');
  return <article data-challenge-id={entry.id}
    className={'record-challenge '+(ready?'record-ready ':entry.claimed?'record-claimed ':'')+(entry.isEndgame?'record-endgame':'')}>
    <header className="record-challenge-head">
      <span>{categoryIcons[entry.category]||'✦'} &nbsp; {entry.isAdventure?'Adventure':entry.category}</span>
      <span className={'challenge-state '+(ready?'ready':entry.claimed?'claimed':'')}>{entry.isEndgame?'✧ ENDGAME · ':''}{label}</span>
    </header>
    <h3>{entry.name}</h3>
    <p className="challenge-description">{entry.description}</p>
    <div className="challenge-progress">
      <div><span>{de?'Fortschritt':'Progress'}</span><strong>{entry.progressLabel} / {entry.goalLabel}</strong></div>
      <div className="challenge-progress-track" role="progressbar" aria-valuemin={0} aria-valuemax={100}
        aria-valuenow={pct(entry.fraction)}><span style={{width:pct(entry.fraction)+'%'}}/></div>
    </div>
    <footer className="challenge-reward">
      <div>
        <strong>◆ {entry.coinReward.toLocaleString('en-US')}</strong>
        {entry.pack&&<small>+ {entry.pack} {de?'Paket':'pack'}</small>}
        {entry.cosmeticReward&&<span className="challenge-portrait-reward">
          {entry.cosmeticRewardVariant&&<img src={portraitPath(entry.cosmeticRewardVariant)} alt="" loading="lazy"/>}
          <small>+ {de?'Exklusives Porträt':'Exclusive portrait'}: {entry.cosmeticReward}</small>
        </span>}
        {entry.empireBoost&&<small className="empire-reward">EMPIRE {entry.empireBoost}</small>}
        {entry.automaticallyAwarded&&<small>{de?'Automatisch beim Puzzeln vergeben':'Automatically awarded in Jigsaw Puzzle'}</small>}
      </div>
      <button type="button" disabled={!ready||pending} onClick={()=>onClaim(entry.id)}
        className={'record-claim '+(ready?'available':'')} aria-label={label+': '+entry.name}>
        {pending?'…':ready?(de?'Abholen':'Claim'):entry.claimed?'✓ '+(de?'Erhalten':'Claimed'):'—'}
      </button>
    </footer>
  </article>;
}
export function ModernChallenges({data,loaded,language,pending,onBack,onClaim,onClaimAll}:Props){
  const de=language==='de';
  const [category,setCategory]=useState('All');
  const [status,setStatus]=useState<ChallengeStatus>('all');
  const [query,setQuery]=useState('');
  const [page,setPage]=useState(0);
  const all=useMemo(()=>{
    const text=query.trim().toLocaleLowerCase();
    return data.entries.filter(e=>(category==='All'||(category==='Adventure'?e.isAdventure:e.category===category))
      &&(!text||(e.name+' '+e.description+' '+e.category+(e.isEndgame?' endgame':'')).toLocaleLowerCase().includes(text))
      &&(status==='all'||status==='ready'&&e.canClaim||status==='progress'&&!e.complete&&!e.claimed
        ||status==='claimed'&&e.claimed||status==='unclaimed'&&!e.claimed))
      .sort((a,b)=>(a.claimed?2:a.canClaim?0:1)-(b.claimed?2:b.canClaim?0:1)
        ||a.category.localeCompare(b.category)||a.name.localeCompare(b.name));
  },[data.entries,category,status,query]);
  const pageSize=12;
  const maxPage=Math.max(0,Math.ceil(all.length/pageSize)-1);
  const current=Math.min(page,maxPage);
  const shown=all.slice(current*pageSize,(current+1)*pageSize);
  const resetPage=()=>setPage(0);
  return <main className="record-page" aria-label={de?'Challenges':'Challenges'}>
    <div className="record-page-scroll">
      <header className="record-hero challenge-hero">
        <div className="record-hero-copy">
          <button className="shop-back" type="button" onClick={onBack}>‹ {de?'Zurück zur Lobby':'Back to lobby'}</button>
          <span className="shop-section-label">THE MIDNIGHT CLUB · YOUR ACHIEVEMENTS</span>
          <h1>{de?'Jede Nacht zählt.':'Make every night count.'}</h1>
          <p>{de?'Neue Meilensteine, seltene Belohnungen und große Ziele in all deinen Spielen.':
            'Every table, every game, every milestone. Your rewards are waiting.'}</p>
        </div>
        <div className="record-hero-symbol" aria-hidden="true">♛</div>
        <div className="record-hero-side">
          <span>{de?'BEREIT ZUM ABHOLEN':'READY TO CLAIM'}</span>
          <strong>{data.ready}</strong>
          <small>◆ {data.readyCoins.toLocaleString('en-US')} {de?'Münzen verfügbar':'coins available'}</small>
        </div>
      </header>
      <div className="record-summary">
        <div><span>{de?'ERFOLGE':'ALL CHALLENGES'}</span><strong>{data.total}</strong></div>
        <div><span>{de?'ABGESCHLOSSEN':'COMPLETED'}</span><strong>{data.complete}</strong></div>
        <div><span>{de?'BELOHNUNGEN ERHALTEN':'REWARDS CLAIMED'}</span><strong>{data.claimed}</strong></div>
        <div><span>{de?'GESAMTFORTSCHRITT':'COMPLETION'}</span>
          <strong>{data.total?Math.round(data.complete/data.total*100):0}%</strong></div>
      </div>
      <section className="record-filters" aria-label={de?'Challenges filtern':'Challenge filters'}>
        <div className="record-filter-title">
          <div><span className="shop-section-label">YOUR CHALLENGE BOARD</span>
            <h2>{de?'Deine Aufgaben':'The Challenge Board'}</h2></div>
          <button type="button" className="record-claim-all" disabled={!loaded||data.ready===0||pending!==null}
            onClick={onClaimAll}>✦ {de?'Alle abholen':'Claim all'} · ◆ {data.readyCoins.toLocaleString('en-US')}</button>
        </div>
        <p className="record-help">{de?
          '„Alle abholen“ berücksichtigt auch Aufgaben außerhalb deiner aktuellen Filter.':
          'Claim all includes every ready reward, even those hidden by your filters.'}</p>
        <div className="record-category-list">
          {['All','Adventure',...data.categories.filter(c=>c!=='Adventure')].map(c=>
            <button type="button" key={c} aria-pressed={category===c}
              className={'record-category '+(category===c?'active':'')}
              onClick={()=>{setCategory(c);resetPage();}}>
              <span aria-hidden="true">{categoryIcons[c]||'✦'}</span> {c==='All'?(de?'Alle Spiele':'All games'):c==='Lifetime'&&de?'Lebenszeit':c==='Prestige'?'Prestige':c}
            </button>)}
        </div>
        <div className="record-filter-bottom">
          <label className="record-search"><span className="sr-only">{de?'Aufgaben suchen':'Search challenges'}</span>
            <span aria-hidden="true">⌕</span><input type="search" value={query} onChange={e=>{setQuery(e.target.value);resetPage();}}
              placeholder={de?'Aufgaben durchsuchen…':'Search challenges…'}/></label>
          <label className="record-select">{de?'Status':'Status'}<select value={status}
            onChange={e=>{setStatus(e.target.value as ChallengeStatus);resetPage();}}>
            {statuses.map(s=><option key={s.id} value={s.id}>{de?s.de:s.en}</option>)}
          </select></label>
          <button type="button" className="record-reset" onClick={()=>{setCategory('All');setQuery('');setStatus('all');setPage(0);}}>
            {de?'Zurücksetzen':'Reset filters'}</button>
        </div>
      </section>
      <div className="record-list-head"><h2>{de?'Meilensteine':'Milestones'}</h2>
        <span>{all.length} {de?'Ergebnisse':'results'}</span></div>
      {!loaded?<p className="record-empty" role="status">{de?'Aufgaben werden geladen…':'Loading your challenges…'}</p>:
      all.length===0?<p className="record-empty">{de?'Keine passenden Aufgaben gefunden.':'No challenges match those filters.'}</p>:
      <section className="record-challenge-grid">
        {shown.map(entry=><Card key={entry.id} entry={entry} de={de}
          pending={pending===entry.id||pending==='all'} onClaim={onClaim}/>)}
      </section>}
      {all.length>pageSize&&<nav className="record-pagination" aria-label="Challenge pages">
        <span>{de?'Seite':'Page'} {current+1} / {maxPage+1}</span>
        <button type="button" disabled={current===0} onClick={()=>setPage(p=>Math.max(0,p-1))}>
          ‹ {de?'Zurück':'Previous'}</button>
        <button type="button" disabled={current>=maxPage} onClick={()=>setPage(p=>Math.min(maxPage,p+1))}>
          {de?'Weiter':'Next'} ›</button>
      </nav>}
    </div>
  </main>;
}
