import {useMemo,useState} from 'react';
import type {HistorySnapshot} from './recordsBridge';
type Props={data:HistorySnapshot;loaded:boolean;language:'en'|'de';onBack:()=>void};
export function ModernHistory({data,loaded,language,onBack}:Props){
 const de=language==='de';
 const [query,setQuery]=useState('');
 const [result,setResult]=useState<'all'|'won'|'lost'>('all');
 const [limit,setLimit]=useState(25);
 const entries=useMemo(()=>data.entries.filter(entry=>
 (result==='all'||(result==='won'&&entry.positive)||(result==='lost'&&!entry.positive))
 &&(!query||[entry.completed,entry.dealer,entry.net,
 ...entry.hands.map(hand=>hand.cards+' '+hand.outcome)].join(' ').toLocaleLowerCase().includes(query.toLocaleLowerCase())))
 ,[data.entries,query,result]);
 return <main className="record-page history-page"><div className="record-page-scroll">
 <header className="record-hero"><div className="record-hero-copy">
 <button className="shop-back" onClick={onBack}>{de?'‹ Zur Lobby':'‹ Back to lobby'}</button>
 <span className="shop-eyebrow">THE MIDNIGHT CLUB / THE VELVET TABLES</span>
 <h1>{de?'Deine Blackjack-Chronik':'Blackjack Hand History'}</h1>
 <p>{de?'Die letzten 250 gespeicherten Runden, direkt aus deinem lokalen Spielverlauf.':
 'Your 250 latest rounds, loaded from the authoritative local hand history.'}</p></div>
 <div className="record-hero-side"><span>{de?'GESPICHERTE RUNDEN':'SAVED ROUNDS'}</span>
 <strong>{data.total.toLocaleString(language)}</strong><small>{de?'Neueste zuerst':'Newest first'}</small></div></header>
 <section className="record-filters history-filters"><div className="record-filter-bottom">
 <div className="record-category-list">
 {(['all','won','lost'] as const).map(kind=><button type="button" key={kind}
 className={'record-category '+(result===kind?'active':'')} onClick={()=>{setResult(kind);setLimit(25)}}>
 {kind==='all'?(de?'Alle':'All'):kind==='won'?(de?'Nicht negativ':'Non-negative'):(de?'Verlust':'Losses')}</button>)}</div>
 <label className="record-search"><span className="sr-only">{de?'Suchen':'Search'}</span>
 <span aria-hidden="true">⌕</span><input type="search" value={query} onChange={e=>{setQuery(e.target.value);setLimit(25)}}
 placeholder={de?'Karten, Ergebnis oder Datum…':'Cards, outcome or date…'}/></label></div>
 </section>
 {!loaded?<p className="record-empty" role="status">{de?'Runden werden geladen…':'Loading your rounds…'}</p>:
 entries.length===0?<p className="record-empty">{de?'Keine passenden Runden gespeichert.':'No matching rounds saved yet.'}</p>:
 <section className="history-list" aria-label={de?'Rundenverlauf':'Round history'}>
 {entries.slice(0,limit).map((entry,index)=><article className="history-round" key={entry.id}>
 <div className="history-round-top"><span className="history-index">#{index+1}</span>
 <strong>{entry.completed}</strong><span className={'history-net '+(entry.positive?'positive':'negative')}>{entry.net}</span></div>
 <div className="history-round-meta"><span>{de?'Einsatz':'Bet'} {entry.bet}</span>
 <span>Dealer · {entry.dealer||'—'}</span></div>
 <div className="history-hands">{entry.hands.map((hand,i)=><div key={i}>
 <strong>{hand.name}</strong><span>{hand.cards}</span><b>{hand.outcome}</b>
 <em>{hand.net}</em>
 {(hand.doubled||hand.split||hand.surrendered)&&<small>
 {[hand.doubled?'Double':'',hand.split?'Split':'',hand.surrendered?'Surrender':''].filter(Boolean).join(' · ')}</small>}
 </div>)}</div></article>)}
 {limit<entries.length&&<button type="button" className="record-reset history-more"
 onClick={()=>setLimit(x=>x+25)}>{de?'Weitere Runden anzeigen':'Show more rounds'} · {entries.length-limit}</button>}
 </section>}
 </div></main>;
}
