import { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { LobbyAmbient } from './LobbyAmbient';
import { embeddedLobby, fallbackLobby, lobbyCommand, onLobbySnapshot } from './lobbyBridge';
import type { LobbyMode, LobbySnapshot } from './lobbyBridge';
import './lobby.css';

type Filter = 'all'|'tables'|'slots'|'relax'|'empire'|'collections'|'shop'|'stats'|'season'|'challenges'|'vault'|'daily';
const artOrder = ['blackjack','kniffel','chill-lounge','casino-empire','midnight-reels',
  'neon-vault','nine-lives','midnight-manor','midnight-merge'];
const categoryById: Record<string, string> = {
  blackjack:'TABLE GAMES',kniffel:'TABLE GAMES','chill-lounge':'RELAXED',
  'casino-empire':'EMPIRE','midnight-reels':'SLOTS','neon-vault':'SLOTS',
  'nine-lives':'SLOTS','midnight-manor':'SLOTS','midnight-merge':'CASINO RESTORATION',
};
const navigation: {id: Filter; icon: string; en: string; de: string}[] = [
  {id:'all',icon:'⌂',en:'Lobby',de:'Lobby'}, {id:'tables',icon:'♠',en:'Table Games',de:'Tischspiele'},
  {id:'slots',icon:'▣',en:'Slots',de:'Slots'}, {id:'relax',icon:'☕',en:'Relaxed',de:'Entspannt'},
  {id:'empire',icon:'♛',en:'Empire',de:'Empire'}, {id:'collections',icon:'▤',en:'Collections',de:'Sammlungen'},
  {id:'shop',icon:'♢',en:'Shop',de:'Shop'}, {id:'stats',icon:'▥',en:'Statistics',de:'Statistik'},
  // Remain accessible when the compact viewport hides the topbar and overview widgets.
  {id:'season',icon:'✦',en:'Season Pass',de:'Saisonpass'},
  {id:'challenges',icon:'♛',en:'Challenges',de:'Aufgaben'},
  {id:'vault',icon:'⬡',en:'Vault',de:'Tresor'},
  {id:'daily',icon:'✦',en:'Daily Login',de:'Täglicher Login'},
];
function percent(n: number): string { return Math.round(n * 100) + '%'; }
function matchesFilter(id: string, filter: Filter): boolean {
  if (filter === 'all') return true;
  if (filter === 'tables') return id === 'blackjack' || id === 'kniffel';
  if (filter === 'slots') return ['midnight-reels','neon-vault','nine-lives','midnight-manor'].includes(id);
  if (filter === 'relax') return id === 'chill-lounge';
  if (filter === 'empire') return ['casino-empire','midnight-merge'].includes(id);
  return true;
}
function Progress({value, tone = 'green'}:{value:number; tone?:'green'|'gold'}) {
  return <div className={'lobby-meter '+tone} role="progressbar" aria-valuemin={0}
    aria-valuemax={100} aria-valuenow={Math.round(value*100)}>
      <span style={{width: percent(value)}}/>
    </div>;
}
function GameTile({ mode, language, onOpen, onSecondary }:{
  mode:LobbyMode; language:'de'|'en'; onOpen:(id:string)=>void; onSecondary:(id:string)=>void;
}) {
  const [hover, setHover] = useState(false);
  const wide = mode.id === 'midnight-merge';
  return <article className={'lobby-game '+(wide?'lobby-merge ':'')+'game-'+mode.id+
      (hover?' hovered':'')+(mode.enabled?'':' unavailable')}
      onPointerEnter={()=>setHover(true)} onPointerLeave={()=>setHover(false)}>
    <div className="game-art-window" aria-hidden="true">
      <div className={'game-art-image art-'+mode.id}/>
      <div className="art-shimmer"/>
    </div>
    <div className="game-info">
      <div className="game-category">{categoryById[mode.id] || mode.category.toUpperCase()}</div>
      <h3>{mode.title}</h3>
      <p>{mode.description}</p>
      <div className="game-actions">
        <button type="button" className="lobby-gold-button" disabled={!mode.enabled}
          onClick={()=>onOpen(mode.id)}>
          <span aria-hidden="true">{wide || mode.id==='casino-empire'?'↗':'▶'}</span>
          {wide || mode.id==='casino-empire'? (language==='de'?'Öffnen':'Enter') :
            (language==='de'?'Spielen':'Play')}
        </button>
        {mode.secondary && <button className="lobby-secondary" type="button"
          onClick={()=>onSecondary(mode.id)} disabled={!mode.enabled}
          title={language==='de'?'Neues Fenster':'New window'} aria-label="New window">↗</button>}
      </div>
    </div>
  </article>;
}
function DailyLogin({data,onClaim,onClose,mobileOpen}:{data:LobbySnapshot;onClaim:()=>void;onClose:()=>void;mobileOpen:boolean}) {
  const de = data.language==='de';
  const daily = data.dailyLogin;
  return <aside className={"lobby-daily "+(mobileOpen?"mobile-open":"")} aria-label={de?'Tägliche Belohnungen':'Daily rewards'}>
    <div className="daily-header">
      <button className="daily-close" type="button" onClick={onClose} aria-label={de?"Tägliche Belohnungen schließen":"Close daily rewards"}>×</button>
      <span className="lobby-eyebrow">✦ {de?'DEIN MIDNIGHT CLUB':'THE MIDNIGHT CLUB'}</span>
      <h2>{de?'Täglicher Login':'Daily Login'}</h2>
      <p>{de?'Eine kleine Belohnung für deinen Besuch. Ohne Verlust von Serien: Die nächste Belohnung wartet auf dich.':
        'A little something for stopping by. No streaks, no pressure. Your next reward always waits.'}</p>
    </div>
    <div className="daily-visual" aria-hidden="true">
      <div className="daily-visual-copy">
        <span>{de?'DEIN HEUTIGER BONUS':'TODAY AT THE CLUB'}</span>
        <strong>{daily.canClaim?daily.activeReward:(de?'Bis morgen':'See you tomorrow')}</strong>
      </div>
    </div>
    <div className="daily-cycle-heading">
      <strong>{de?'7-Tage-Belohnungen':'7-day rewards'}</strong>
      <span>{daily.totalClaims} {de?'Besuche belohnt':'claims earned'}</span>
    </div>
    <div className="daily-rewards">
      {daily.rewards.map(reward=>{
        const completed = reward.day <= daily.cycleCompletedSteps;
        const active = daily.canClaim && reward.day === daily.activeStep;
        const claimedToday = daily.claimedToday && reward.day === daily.activeStep;
        return <div key={reward.day} className={'daily-reward '+(active?'current ':completed?'claimed ':'upcoming ')+(reward.pack?'has-pack':'')}
          aria-current={active?'step':undefined}>
          <span className="daily-reward-top">
            <b>{de?'Tag':'Day'} {reward.day}</b>
            <span aria-hidden="true">{completed?'✓':active?'✦':reward.pack?'◆':'◇'}</span>
          </span>
          <strong className="daily-reward-bankroll">{reward.bankroll}</strong>
          <small>◆ {reward.coins} {de?'Münzen':'coins'}</small>
          {reward.pack&&<span className="daily-pack">{reward.pack} {de?'Paket':'pack'}</span>}
          <span className="daily-reward-state">
            {active?(de?'ABHOLBEREIT':'READY'):claimedToday?(de?'HEUTE ERHALTEN':'CLAIMED TODAY'):
              completed?(de?'ERHALTEN':'CLAIMED'):(de?'ALS NÄCHSTES':'UP NEXT')}
          </span>
        </div>;
      })}
    </div>
    <div className="daily-footer">
      <p>{daily.canClaim?
        (de?'Tag '+daily.activeStep+' wartet auf dich.':'Day '+daily.activeStep+' is ready for you.'):
        (de?'Für heute bereits abgeholt. Nächste Belohnung: '+daily.nextReward:
          'Claimed today. Next visit: '+daily.nextReward)}</p>
      <button type="button" className={'daily-claim '+(daily.canClaim?'ready':'claimed')}
        onClick={onClaim} disabled={!daily.canClaim}>
        <span aria-hidden="true">{daily.canClaim?'✦':'✓'}</span>
        {daily.canClaim?(de?'BELOHNUNG ABHOLEN':'CLAIM REWARD'):
          (de?'HEUTE ABGEHOLT':'CLAIMED TODAY')}
      </button>
    </div>
  </aside>;
}
function LobbyApp() {
  const [data,setData] = useState<LobbySnapshot>(fallbackLobby);
  const [filter,setFilter] = useState<Filter>('all');
  const [dailyOpen,setDailyOpen] = useState(false);
  const [lowMotion,setLowMotion] = useState(window.matchMedia('(prefers-reduced-motion: reduce)').matches);
  const [nativeSnapshotReceived,setNativeSnapshotReceived] = useState(!embeddedLobby);
  useEffect(()=>{
    const media = window.matchMedia('(prefers-reduced-motion: reduce)');
    const listener = () => setLowMotion(media.matches);
    media.addEventListener('change',listener);
    return ()=>media.removeEventListener('change',listener);
  },[]);
  useEffect(()=>{
    // The native WebView2 host starts hidden. Hidden webviews can suspend
    // requestAnimationFrame indefinitely, so a frame-based ready handshake
    // would deadlock the lobby and leave the old WPF fallback visible.
    const stop = onLobbySnapshot(snapshot=>{
      setData(snapshot);
      setNativeSnapshotReceived(true);
    });
    lobbyCommand('lobby.ready');
    return stop;
  },[]);
  useEffect(()=>{
    // Announce readiness only after React committed the real player snapshot.
    // This effect does not depend on visibility, timers or animation frames.
    if (nativeSnapshotReceived) lobbyCommand('lobby.ui-ready');
  },[nativeSnapshotReceived]);
  useEffect(()=>{
    document.documentElement.lang=data.language;
    document.title='Blackjack Casino · Lobby';
  },[data.language]);
  const de = data.language==='de';
  const openMode = (id:string)=>lobbyCommand('lobby.open',id);
  const openSecondary = (id:string)=>lobbyCommand('lobby.open-secondary',id);
  const openProgression = (id:string)=>lobbyCommand('lobby.progression',id);
  const chooseNav = (id:Filter)=>{
    if(id==='collections') {openProgression("Collector's Album");return;}
    if(id==='shop') {openProgression('Cosmetic Shop');return;}
    if(id==='stats') {openProgression('Statistics');return;}
    if(id==='season') {openProgression('Season Pass');return;}
    if(id==='challenges') {openProgression('Challenges');return;}
    if(id==='vault') {lobbyCommand('lobby.vault');return;}
    if(id==='daily') {setDailyOpen(true);return;}
    setDailyOpen(false);
    setFilter(id);
  };
  const modes = data.modes
    .filter(x=>matchesFilter(x.id,filter))
    .sort((a,b)=>artOrder.indexOf(a.id)-artOrder.indexOf(b.id));
  return <div className="casino-lobby-shell" data-reduced={lowMotion || !data.animationsEnabled}>
    <LobbyAmbient enabled={!lowMotion && data.animationsEnabled}/>
    <header className="lobby-topbar">
      <button className="lobby-brand" type="button" onClick={()=>setFilter('all')}>
        <span className="brand-spade" aria-hidden="true">♠</span>
        <span><b>BLACKJACK CASINO</b><i>THE MIDNIGHT CLUB</i></span>
      </button>
      <div className="lobby-top-status">
        <button className="header-widget header-compact" onClick={()=>openProgression('Challenges')}>
          <span className="header-emoji">♛</span><span><small>{de?'AUFGABEN':'CHALLENGES'}</small>
          <b>{percent(data.challengeFraction)}</b><Progress value={data.challengeFraction}/></span>
        </button>
        <button className="header-widget header-compact" onClick={()=>openProgression("Collector's Album")}>
          <span className="header-emoji">▤</span><span><small>ALBUM</small><b>{data.albumOwned} / {data.albumTotal}</b></span>
        </button>
        <button className="header-widget profile-widget" onClick={()=>lobbyCommand('lobby.profile')}>
          <span className="header-avatar">♠</span><span><b>{data.displayName}</b>
          <small>LV {data.level} · ◆ {data.coins}</small><em>{data.title}</em></span>
          {data.prestige>0&&<span className="prestige-badge">✦<br/>P{data.prestige}</span>}
        </button>
        <button className="header-widget vault-widget" onClick={()=>lobbyCommand('lobby.vault')}>
          <span className="header-emoji">⬡</span><span>
            <small>VAULT · {data.vaultTier}</small><b>{data.vaultAmount} / {data.vaultCapacity}</b>
            <Progress value={data.vaultFraction} tone="gold"/></span>
        </button>
      </div>
      <div className="lobby-finances">
        <span><small>BANKROLL</small><strong>{data.bankroll}</strong></span>
        <span className="empire-wallet"><small>EMPIRE CREDITS</small><strong>{data.empireCredits}</strong></span>
      </div>
      <button type="button" className="lobby-exit" onClick={()=>lobbyCommand('lobby.exit')}>
        ↪ <span>{de?'Beenden':'Exit'}</span>
      </button>
    </header>
    <div className="lobby-body">
      <nav className="lobby-nav" aria-label={de?'Casino-Navigation':'Casino navigation'}>
        {navigation.map(n=><button type="button" key={n.id}
          aria-current={n.id===filter?'page':undefined}
          className={'lobby-nav-item '+(n.id===filter?'selected':'')+(n.id==='daily'?' daily-nav':'')}
          onClick={()=>chooseNav(n.id)}>
          <span className="nav-icon" aria-hidden="true">{n.icon}</span>
          <span>{de?n.de:n.en}</span>
        </button>)}
        <div className="lobby-nav-decoration" aria-hidden="true">♣</div>
      </nav>
      <main className="lobby-main">
        <div className="lobby-hero">
          <div className="lobby-hero-picture" aria-hidden="true"/>
          <div className="hero-copy">
            <span className="lobby-eyebrow">— &nbsp; THE MIDNIGHT CLUB &nbsp; —</span>
            <h1>{de?'Eine Nacht wie keine andere.':'A different kind of night.'}</h1>
            <p>{de?'Wähle deinen Tisch. Finde deinen Rhythmus. Fühl dich zu Hause.':
              'Choose your table. Find your rhythm. Make yourself at home.'}</p>
          </div>
        </div>
        <section className={'lobby-game-grid '+(filter!=='all'?'filtered':'')}
          aria-label={de?'Spiele und Aktivitäten':'Games and activities'}>
          {modes.map(mode=><GameTile key={mode.id} mode={mode} language={data.language}
            onOpen={openMode} onSecondary={openSecondary}/>)}
          {modes.length===0&&<p className="empty-mode">{de?'Keine Spiele in dieser Kategorie.':'No games in this category.'}</p>}
        </section>
        {!embeddedLobby&&<small className="browser-note">Browser preview · navigation requires the Windows client.</small>}
      </main>
      <DailyLogin data={data} onClaim={()=>lobbyCommand('lobby.daily.claim')}
        onClose={()=>setDailyOpen(false)} mobileOpen={dailyOpen}/>
    </div>
  </div>;
}

createRoot(document.getElementById('root')!).render(<LobbyApp/>);
