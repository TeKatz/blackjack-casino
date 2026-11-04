import { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { LobbyAmbient } from './LobbyAmbient';
import { embeddedLobby, fallbackLobby, lobbyCommand, onLobbySnapshot } from './lobbyBridge';
import type { LobbyMode, LobbySnapshot } from './lobbyBridge';
import './lobby.css';

type Filter = 'all'|'tables'|'slots'|'relax'|'empire'|'collections'|'shop'|'stats';
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
];
const progression: {id: string; icon: string; de: string; en: string; detail: string}[] = [
  {id:'Season Pass',icon:'✦',en:'Season Pass',de:'Season Pass',detail:'season'},
  {id:'Cosmetic Shop',icon:'◈',en:'Cosmetic Shop',de:'Kosmetik-Shop',detail:'shop'},
  {id:"Collector's Album",icon:'▤',en:"Collector’s Album",de:'Sammelalbum',detail:'album'},
  {id:'Challenges',icon:'♛',en:'Challenges',de:'Herausforderungen',detail:'challenges'},
  {id:'Statistics',icon:'▥',en:'Statistics',de:'Statistiken',detail:'stats'},
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
function Overview({data, open}:{data:LobbySnapshot;open:(id:string)=>void}) {
  const de = data.language==='de';
  return <aside className="lobby-overview">
    <span className="lobby-eyebrow">{de?'DEIN MIDNIGHT CLUB':'YOUR MIDNIGHT CLUB'}</span>
    <h2>{de?'Auf einen Blick.':'At a glance.'}</h2>
    <p className="overview-intro">{de?'Belohnungen, Stil und Fortschritt – alles an einem Ort.':
      'Rewards, style, goals and your casino career — all in one place.'}</p>
    <div className="overview-items">
      {progression.map((item,i)=><button className={'overview-tile overview-'+item.detail}
        type="button" key={item.id} onClick={()=>open(item.id)}>
        <span className={'overview-icon overview-icon-'+i} aria-hidden="true">{item.icon}</span>
        <span className="overview-copy">
          <strong>{de?item.de:item.en}</strong>
          {item.detail==='season'?<>
            <small>{de?'Verfolge deine Saison und sichere Belohnungen.':'Follow your season track and claim rewards.'}</small>
            <Progress value={data.seasonFraction} tone="gold"/>
            <em>{de?'Stufe':'Tier'} {data.seasonTier} → {data.seasonNextTier} · {data.seasonExperience.toLocaleString()} / {data.seasonRequired.toLocaleString()} XP</em>
          </>:item.detail==='challenges'?<>
            <small>{data.challengesReady>0?
              (de?'Belohnungen verfügbar: ':'Rewards ready: ')+data.challengesReady:
              data.challengeName}</small>
            <Progress value={data.challengeFraction}/>
          </>:item.detail==='album'?<small>{data.albumOwned} / {data.albumTotal} {de?'Karten entdeckt':'cards discovered'}</small>:
          item.detail==='shop'?<small>{de?'Karten, Porträts und neue Looks.':'Card sets, portraits and more ways to customize.'}</small>:
          <small>{de?'Deine Ergebnisse und dein Spielerprofil.':'Lifetime results and your casino story.'}</small>}
        </span>
        <span className="overview-arrow" aria-hidden="true">›</span>
      </button>)}
    </div>
  </aside>;
}
function LobbyApp() {
  const [data,setData] = useState<LobbySnapshot>(fallbackLobby);
  const [filter,setFilter] = useState<Filter>('all');
  const [lowMotion,setLowMotion] = useState(window.matchMedia('(prefers-reduced-motion: reduce)').matches);
  useEffect(()=>{
    const media = window.matchMedia('(prefers-reduced-motion: reduce)');
    const listener = () => setLowMotion(media.matches);
    media.addEventListener('change',listener);
    return ()=>media.removeEventListener('change',listener);
  },[]);
  useEffect(()=>{
    const stop = onLobbySnapshot(setData);
    lobbyCommand('lobby.ready');
    const frame = window.requestAnimationFrame(()=>lobbyCommand('lobby.ui-ready'));
    return ()=>{stop();window.cancelAnimationFrame(frame);};
  },[]);
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
          className={'lobby-nav-item '+(n.id===filter?'selected':'')}
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
      <Overview data={data} open={openProgression}/>
    </div>
  </div>;
}

createRoot(document.getElementById('root')!).render(<LobbyApp/>);
