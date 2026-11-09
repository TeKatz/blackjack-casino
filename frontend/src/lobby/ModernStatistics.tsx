import {useMemo,useState} from 'react';
import type {StatisticsSnapshot} from './recordsBridge';
import './records.css';
type Props={data:StatisticsSnapshot;loaded:boolean;language:'en'|'de';onBack:()=>void};
type GameGroup='all'|'tables'|'slots'|'relaxed';
const tabs:{id:GameGroup;de:string;en:string}[]=[
  {id:'all',de:'Alle Spiele',en:'All games'},
  {id:'tables',de:'Tischspiele',en:'Table games'},
  {id:'slots',de:'Slots',en:'Slots'},
  {id:'relaxed',de:'Entspannt',en:'Relaxed'},
];
const symbols:Record<string,string>={
  blackjack:'♠','midnight-dice':'⚄','tower-defense':'♜',sudoku:'▦','midnight-reels':'▣',
  'midnight-manor':'☽','neon-vault':'◇','nine-lives':'●',
  'neon-features':'◇','nine-features':'●','legacy-slots':'▣',
};

const germanMetrics:Record<string,string>={
  "Mastery level": "Bastion-Meisterschaft",
  "Maps completed": "Abgeschlossene Karten",
  "Victories / defeats": "Siege / Niederlagen",
  "Waves cleared": "Abgeschlossene Wellen",
  "Enemies defeated": "Besiegte Gegner",
  "Enemies escaped": "Entkommene Gegner",
  "Best endless wave": "Höchste Endloswelle",
  "Games started": "Begonnene Partien",
  "Games won": "Gewonnene Partien",
  "Win rate": "Siegquote",
  "Abandoned games": "Abgebrochene Partien",
  "Best win streak": "Beste Siegesserie",
  "Current win streak": "Aktuelle Siegesserie",
  "Klondike / FreeCell wins": "Klondike / FreeCell · Siege",
  "Spider wins (1 / 2 / 4 suits)": "Spider-Siege (1 / 2 / 4 Farben)",
  "Pyramid / Golf / TriPeaks": "Pyramid / Golf / TriPeaks",
  "Best completion time": "Schnellster Sieg",
  "Fewest moves in a win": "Wenigste Züge bei Sieg",
  "Total time in winning games": "Gesamtdauer gewonnener Partien",
  "Completed puzzles": "Abgeschlossene Rätsel",
  "Words found": "Gefundene Wörter",
  "Completions without hints": "Ohne Hinweise gelöst",
  "Expert / Master": "Experte / Meister",
  "Best solve time": "Schnellste Lösung",
  "Total solve time": "Gesamte Lösungszeit",
  "Completed tables": "Abgeschlossene Spielfelder",
  "Pairs found": "Gefundene Paare",
  "Perfect tables": "Fehlerfreie Partien",
  "Cards flipped": "Aufgedeckte Karten",
  "Wrong matches": "Fehlversuche",
  "Best match streak": "Längste Trefferfolge",
  "Best accuracy": "Beste Trefferquote",
  "Pieces placed": "Gesetzte Puzzleteile",
  "Largest puzzle": "Größtes Puzzle",
  "Different motifs": "Verschiedene Motive",
  "Puzzle achievements": "Puzzle-Erfolge",
  "1,000-piece puzzles": "1.000-Teile-Puzzles",
  "Completed orders": "Erfüllte Aufträge",
  "Total merges": "Kombinierte Gegenstände",
  "Renovation points": "Renovierungspunkte",
  "Areas restored": "Restaurierte Bereiche",
  "Milestones reached": "Erreichte Meilensteine",
  "Highest item tier": "Höchste Gegenstandsstufe",
  "Items spawned": "Erzeugte Gegenstände",
  "Completed adventures": "Abgeschlossene Abenteuer",
  "Different endings": "Verschiedene Enden",
  "Classes completed": "Abgeschlossene Klassen",
  "Successful / failed checks": "Erfolgreiche / gescheiterte Proben",
  "Natural 20s / 1s": "Natürliche 20er / 1er",
  "Items found": "Gefundene Gegenstände",
  "Encounters won / lost": "Begegnungen gewonnen / verloren",
  "Blackjack / five-of-a-kind victories": "Blackjack- / Fünferpasch-Siege",
  "Rounds": "Runden",
  "Hands": "Hände",
  "Hand win rate": "Gewinnquote der Hände",
  "Won / lost / push": "Gewonnen / verloren / unentschieden",
  "Total wagered": "Gesamteinsätze",
  "Biggest round win": "Größter Rundengewinn",
  "Blackjacks": "Blackjacks",
  "Doubles / splits": "Verdoppelt / gesplittet",
  "Surrenders / insurance": "Aufgaben / Versicherung",
  "Double wins": "Gewonnene Verdopplungen",
  "Completed games": "Abgeschlossene Spiele",
  "Casual / casino": "Freizeit / Casino",
  "Best / average score": "Bestwert / Durchschnitt",
  "Casino wins / top two": "Casino-Siege / Top 2",
  "Casino win rate": "Casino-Siegquote",
  "Entries / forfeits": "Teilnahmen / Aufgaben",
  "Total entry fees": "Gesamte Teilnahmegebühren",
  "Total returned": "Gesamte Auszahlungen",
  "Biggest payout": "Größte Auszahlung",
  "Upper bonuses": "Obere Boni",
  "Five of a kind": "Fünferpasch",
  "Full house / large straight": "Full House / Große Straße",
  "Free Play / Daily": "Freies Spiel / Tagesrätsel",
  "No-hint solves": "Ohne Hinweis gelöst",
  "Hints used": "Genutzte Hinweise",
  "Average solve": "Durchschnittliche Lösungszeit",
  "Fastest solve": "Schnellste Lösung",
  "Fastest clean solve": "Schnellste Lösung ohne Hinweise",
  "Relaxed / Easy / Medium": "Entspannt / Leicht / Mittel",
  "Hard / Expert / Master": "Schwer / Experte / Meister",
  "Spins": "Spins",
  "Winning spins": "Gewonnene Spins",
  "Spin win rate": "Spin-Gewinnquote",
  "Pushes": "Unentschieden",
  "Losing spins": "Verlorene Spins",
  "Return / wagered": "Rücklaufquote",
  "Paid spins": "Bezahlte Spins",
  "Free spins": "Freispiele",
  "Big wins": "Große Gewinne",
  "Biggest win": "Größter Gewinn",
  "Free spin triggers": "Freispiel-Auslösungen",
  "Manor bonuses": "Manor-Boni",
  "Best run": "Bester Durchlauf",
  "Escapes / max exits": "Entkommen / Maximale Ausgänge",
  "Caught / cash out": "Erwischt / Ausgezahlt",
  "Interactive / auto": "Interaktiv / Automatisch",
  "Paid / free spins": "Bezahlte / Freispiele",
  "Free spin sessions": "Freispielserien",
  "Spins awarded": "Vergebene Spins",
  "Jackpots / grand": "Jackpots / Grand",
  "Blackouts / total": "Blackouts / Total",
  "Security overrides": "Sicherheits-Overrides",
  "Best payout multiplier": "Bester Gewinnmultiplikator",
  "Paid / bonus spins": "Bezahlte / Bonus-Spins",
  "Bonus sessions": "Bonusrunden",
  "Pounce Wilds": "Pounce-Wilds",
  "Full Grid Frenzies": "Full-Grid-Frenzies"
};
export function ModernStatistics({data,loaded,language,onBack}:Props){
  const de=language==='de';
  const [group,setGroup]=useState<GameGroup>('all');
  const [search,setSearch]=useState('');
  const [expanded,setExpanded]=useState<Record<string,boolean>>({});
  const [showSession,setShowSession]=useState(true);
  const games=useMemo(()=>data.games.filter(game=>(group==='all'||game.category===group)
    &&game.title.toLocaleLowerCase().includes(search.trim().toLocaleLowerCase())),[data.games,group,search]);
  const overview=data.overview;
  return <main className="record-page statistics-page" aria-label={de?'Statistiken':'Statistics'}>
    <div className="record-page-scroll">
      <header className="record-hero statistics-hero">
        <div className="record-hero-copy">
          <button className="shop-back" type="button" onClick={onBack}>‹ {de?'Zurück zur Lobby':'Back to lobby'}</button>
          <span className="shop-section-label">THE MIDNIGHT CLUB · YOUR CASINO HISTORY</span>
          <h1>{de?'Deine Zahlen. Deine Geschichte.':'The numbers tell a story.'}</h1>
          <p>{de?'Alle aufgezeichneten Ergebnisse – von Blackjack und Würfeln bis zu den Spielautomaten und Rätseln.':
            'Every hand, every spin, every quiet moment. Your casino career in one place.'}</p>
        </div>
        <div className="record-hero-symbol" aria-hidden="true">♠</div>
        <div className="record-hero-side record-net-hero">
          <span>{de?'CASINO-GESAMTERGEBNIS':'CASINO LIFETIME NET'}</span>
          <strong className={overview.netPositive?'positive':'negative'}>{overview.net}</strong>
          <small>{de?'Erfasstes Nettoergebnis':'Recorded net performance'}</small>
        </div>
      </header>
      <div className="record-summary stats-summary">
        <div><span>{de?'GESAMTGESETZT':'TOTAL WAGERED'}</span><strong>{overview.wagered}</strong></div>
        <div><span>{de?'SLOT-SPINS · GESAMT':'LIFETIME SLOT SPINS'}</span><strong>{overview.slotSpins}</strong></div>
        <div><span>{de?'GESAMTSPIELZEIT':'TOTAL PLAYTIME'}</span><strong>{overview.playtime}</strong></div>
        <div><span>{de?'BLACKJACK-RUNDEN':'BLACKJACK ROUNDS'}</span><strong>{overview.rounds}</strong></div>
      </div>
      <section className="record-filters">
        <div className="record-filter-title"><div><span className="shop-section-label">YOUR GAME ARCHIVE</span>
          <h2>{de?'Leistung nach Spiel':'Performance by game'}</h2></div>
          <span className="stat-game-count">{games.length} {de?'Bereiche':'sections'}</span>
        </div>
        <div className="record-filter-bottom stats-filter-line">
          <div className="record-category-list" role="group" aria-label={de?'Spieltyp':'Game type'}>
            {tabs.map(t=><button type="button" key={t.id} className={'record-category '+(group===t.id?'active':'')}
              aria-pressed={group===t.id} onClick={()=>setGroup(t.id)}>{de?t.de:t.en}</button>)}
          </div>
          <label className="record-search"><span className="sr-only">{de?'Spiel suchen':'Search games'}</span>
            <span aria-hidden="true">⌕</span>
            <input type="search" value={search} onChange={e=>setSearch(e.target.value)}
              placeholder={de?'Spiel suchen…':'Find a game…'}/></label>
        </div>
      </section>
      {!loaded?<p role="status" className="record-empty">{de?'Statistiken werden geladen…':'Loading your lifetime statistics…'}</p>:
      games.length===0?<p className="record-empty">{de?'Keine Spiele gefunden.':'No games match your search.'}</p>:
      <section className="record-stat-grid" aria-label={de?'Spielstatistiken':'Game statistics'}>
        {games.map(game=>{
          const show=expanded[game.id]??false;
          const metrics=show?game.metrics:game.metrics.slice(0,6);
          return <article key={game.id} data-stat-game={game.id} className={'stat-game-card '+(show?'expanded':'')}>
            <header className="stat-card-head">
              <span className="stat-game-symbol" aria-hidden="true">{symbols[game.id]||'♣'}</span>
              <div><span>{game.category==='tables'?(de?'TISCHSPIELE':'TABLE GAMES'):
                game.category==='slots'?'VIDEO SLOTS':(de?'CHILL LOUNGE':'RELAXED GAMES')}</span>
                <h3>{game.title}</h3></div>
              {game.net&&<strong className={'stat-net '+(game.positive?'positive':'negative')}>{game.net}</strong>}
            </header>
            <div className="stat-metrics">
              {metrics.map((metric,i)=><div className="stat-metric" key={metric.label+'-'+i}>
                <span>{de?(germanMetrics[metric.label]||metric.label):metric.label}</span><strong>{metric.value}</strong>
              </div>)}
            </div>
            {game.metrics.length>6&&<button className="stat-expand" type="button"
              aria-expanded={show} onClick={()=>setExpanded(x=>({...x,[game.id]:!show}))}>
              {show?(de?'Weniger anzeigen':'Show less'):(de?'Alle Kennzahlen anzeigen':'Explore all metrics')}
              <span>{show?'⌃':'⌄'}</span></button>}
          </article>;
        })}
      </section>}
      <section className="record-extra-section">
        <div className="record-extra-title"><span className="shop-section-label">BLACKJACK DESK</span>
          <h2>{de?'Aktuelle Sitzung':'This Session'}</h2>
          <button type="button" aria-expanded={showSession} onClick={()=>setShowSession(v=>!v)}>
            {showSession?(de?'Einklappen':'Collapse'):(de?'Anzeigen':'Expand')} {showSession?'⌃':'⌄'}</button>
        </div>
        {showSession&&<div className="session-metrics">
          {([
            ['Rounds',data.session.rounds],['Hands',data.session.hands],['Session time',data.session.playtime],
            ['Won',data.session.won],['Lost',data.session.lost],['Pushes',data.session.pushes],
            ['Wagered',data.session.wagered],['Net',data.session.net],['Blackjacks',data.session.blackjacks],
            ['Best win streak',data.session.bestStreak],
          ] as [string,string][]).map(([label,value])=><div key={label}><span>{de?(germanMetrics[label]||label):label}</span><strong>{value}</strong></div>)}
        </div>}
      </section>
      <section className="record-extra-section table-archive">
        <div className="record-extra-title"><span className="shop-section-label">THE VELVET TABLES</span>
          <h2>{de?'Blackjack-Tischübersicht':'Blackjack Table Performance'}</h2></div>
        {data.tables.length===0?<p className="stat-no-tables">{de?'Noch keine abgeschlossenen Blackjack-Runden.':
          'No completed Blackjack rounds yet. Tables appear after your first round.'}</p>:
          <div className="stat-table-scroll"><table className="stat-table"><thead><tr>
            <th>{de?'Tisch':'Table'}</th><th>{de?'Runden':'Rounds'}</th>
            <th>{de?'Hände':'Hands'}</th><th>{de?'Gewinnrate':'Win rate'}</th>
            <th>{de?'Netto':'Net'}</th>
          </tr></thead><tbody>{data.tables.map((table,index)=><tr key={table.name+'-'+index}>
            <th scope="row">{table.name}</th><td>{table.rounds}</td><td>{table.hands}</td>
            <td>{table.winRate}</td><td className={table.positive?'positive':'negative'}>{table.net}</td>
          </tr>)}</tbody></table></div>}
      </section>
    </div>
  </main>;
}
