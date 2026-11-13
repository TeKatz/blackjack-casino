import {finishModeBoot} from '../boot/finishModeBoot';
import { useEffect, useState } from 'react';
import type {PointerEvent as ReactPointerEvent} from 'react';
import { createRoot } from 'react-dom/client';
import {PlayerPortrait,CosmeticTitle,VaultIcon} from './Cosmetics';
import {localizedSudokuTitleName} from './sudokuTitleLocalization';
import {Modal} from './Modal';
import {ExitPrompt} from './ExitPrompt';
import {NavIcon} from './NavIcon';
import './rouletteTile.css';
import {ArcadeMenu} from './ArcadeMenu';
import {ARCADE_MODE_ID,arcadeLobbyMode} from './arcadeCatalog';
import { LobbyAmbient } from './LobbyAmbient';
import {stableLobbyTiles} from './lobbyTiles';
import { ModernShop } from './ModernShop';
import { ModernChallenges } from './ModernChallenges';
import { ModernStatistics } from './ModernStatistics';
import { ModernAccount } from './ModernAccount';
import { ModernHistory } from './ModernHistory';
import { ModernVault } from './ModernVault';
import { ModernAlbum } from './ModernAlbum';
import {previewAlbum} from './albumBridge';
import type {AlbumSnapshot,AlbumResult,AlbumRarity} from './albumBridge';
import { ModernSeasons } from './ModernSeasons';
import { ModernUpdates } from './ModernUpdates';
import {fallbackUpdates,onUpdateSnapshot} from './updatesBridge';
import type {LobbyUpdatesSnapshot} from './updatesBridge';
import { onRecordsMessage, previewChallenges, previewStatistics, previewAccount, previewHistory, previewVault, previewSeasons } from './recordsBridge';
import type { ChallengesSnapshot, StatisticsSnapshot, AccountSnapshot, HistorySnapshot, VaultSnapshot, VaultReward, SeasonsSnapshot, RecordsPage } from './recordsBridge';
import { onShopMessages, previewShop } from './shopBridge';
import type { ShopSnapshot } from './shopBridge';
import { embeddedLobby, fallbackLobby, lobbyCommand, onLobbySnapshot, renamePlayer } from './lobbyBridge';
import type { LobbyMode, LobbySnapshot } from './lobbyBridge';
import './lobby.css';

type Filter = 'all'|'tables'|'slots'|'relax'|'empire'|'collections'|'shop'|'stats'|'season'|'challenges'|'vault'|'daily'|'profile';
const artOrder = ['blackjack','roulette','kniffel','chill-lounge','casino-empire','midnight-reels',
  'neon-vault','nine-lives','midnight-manor','midnight-merge',ARCADE_MODE_ID,'midnight-descent'];
const categoryById: Record<string, string> = {
  blackjack:'TABLE GAMES',roulette:'TABLE GAMES · HIGH LIMIT',kniffel:'TABLE GAMES','chill-lounge':'RELAXED',
  'casino-empire':'EMPIRE','midnight-reels':'SLOTS','neon-vault':'SLOTS',
  'nine-lives':'SLOTS','midnight-manor':'SLOTS','midnight-merge':'CASINO RESTORATION',
  'arcade-games':'ARCADE CHALLENGES',
  'midnight-descent':'ACTION ROGUELITE',
};
const navigation: {id: Filter; en: string; de: string}[] = [
  {id:'all',en:'Lobby',de:'Lobby'}, {id:'tables',en:'Table Games',de:'Tischspiele'},
  {id:'slots',en:'Slots',de:'Slots'}, {id:'relax',en:'Relaxed',de:'Entspannt'},
  {id:'empire',en:'Empire',de:'Empire'}, {id:'collections',en:'Collections',de:'Sammlungen'},
  {id:'shop',en:'Shop',de:'Shop'}, {id:'stats',en:'Statistics',de:'Statistik'},
  // Remain accessible when the compact viewport hides the topbar and overview widgets.
  {id:'season',en:'Season Pass',de:'Saisonpass'},
  {id:'challenges',en:'Challenges',de:'Aufgaben'},
  {id:'vault',en:'Vault',de:'Tresor'},
  {id:'daily',en:'Daily Login',de:'Täglicher Login'},
  {id:'profile',en:'Player Profile',de:'Spielerprofil'},
];
function percent(n: number): string { return Math.round(n * 100) + '%'; }
function Progress({value, tone = 'green'}:{value:number; tone?:'green'|'gold'}) {
  return <div className={'lobby-meter '+tone} role="progressbar" aria-valuemin={0}
    aria-valuemax={100} aria-valuenow={Math.round(value*100)}>
      <span style={{width: percent(value)}}/>
    </div>;
}
function GameTile({ mode, hidden, language, motionEnabled, onOpen, onSecondary }:{
  mode:LobbyMode; hidden:boolean; language:'de'|'en'; motionEnabled:boolean;
  onOpen:(id:string)=>void; onSecondary:(id:string)=>void;
}) {
  const move=(event:ReactPointerEvent<HTMLElement>)=>{
    if(!motionEnabled||event.pointerType==='touch')return;
    const card=event.currentTarget,rect=card.getBoundingClientRect();
    const x=Math.max(-1,Math.min(1,(event.clientX-rect.left)/Math.max(rect.width,1)*2-1));
    const y=Math.max(-1,Math.min(1,(event.clientY-rect.top)/Math.max(rect.height,1)*2-1));
    card.style.setProperty('--motion-x',(x*6).toFixed(2)+'px');
    card.style.setProperty('--motion-y',(y*5).toFixed(2)+'px');
  };
  const leave=(event:ReactPointerEvent<HTMLElement>)=>{
    event.currentTarget.style.setProperty('--motion-x','0px');
    event.currentTarget.style.setProperty('--motion-y','0px');
  };
  return <article hidden={hidden} className={'lobby-game game-'+mode.id+
      (mode.enabled?'':' unavailable')} onPointerMove={move} onPointerLeave={leave}>
    <div className="game-art-window" data-mode-art={mode.id} aria-hidden="true">
      {mode.id==='roulette'&&<div className="roulette-tile-art"><span>✦</span><i/></div>}
      {mode.id===ARCADE_MODE_ID&&<div className="arcade-tile-scene">
        <span className="arcade-tile-spark one"/><span className="arcade-tile-spark two"/>
        <span className="arcade-tile-spark three"/>
        <span className="arcade-tile-machine"><span>✦</span></span>
      </div>}
    </div>
    <div className="game-info">
      <div className="game-category">{mode.id==='midnight-descent'?(language==='de'?'ACTION-ROGUELITE · NEUES FENSTER':'ACTION ROGUELITE · NEW WINDOW'):(categoryById[mode.id] || mode.category.toUpperCase())}</div>
      <h3>{mode.title}</h3>
      <p>{mode.description}</p>
      <div className="game-actions">
        <button type="button" className="lobby-gold-button" disabled={!mode.enabled}
          onClick={()=>onOpen(mode.id)}>
          <span aria-hidden="true">{mode.id==='casino-empire'||mode.id==='midnight-descent'?'↗':'▶'}</span>
          {mode.id==='midnight-descent'?(language==='de'?'Öffnen':'Open'):mode.id==='casino-empire'? (language==='de'?'Öffnen':'Enter') :
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
  const [shop,setShop] = useState<ShopSnapshot>(previewShop);
  const [recordsVisible,setRecordsVisible] = useState<RecordsPage|null>(null);
  const [challenges,setChallenges] = useState<ChallengesSnapshot>(previewChallenges);
  const [statistics,setStatistics] = useState<StatisticsSnapshot>(previewStatistics);
  const [account,setAccount] = useState<AccountSnapshot>(previewAccount);
  const [history,setHistory] = useState<HistorySnapshot>(previewHistory);
  const [vault,setVault] = useState<VaultSnapshot>(previewVault);
  const [album,setAlbum] = useState<AlbumSnapshot>(previewAlbum);
  const [albumResult,setAlbumResult] = useState<AlbumResult|null>(null);
  const [vaultReward,setVaultReward] = useState<VaultReward|null>(null);
  const [vaultNotice,setVaultNotice] = useState<string|null>(null);
  const [seasons,setSeasons] = useState<SeasonsSnapshot>(previewSeasons);
  const [challengesLoaded,setChallengesLoaded] = useState(!embeddedLobby);
  const [statisticsLoaded,setStatisticsLoaded] = useState(!embeddedLobby);
  const [accountLoaded,setAccountLoaded] = useState(!embeddedLobby);
  const [historyLoaded,setHistoryLoaded] = useState(!embeddedLobby);
  const [vaultLoaded,setVaultLoaded] = useState(!embeddedLobby);
  const [albumLoaded,setAlbumLoaded] = useState(!embeddedLobby);
  const [seasonsLoaded,setSeasonsLoaded] = useState(!embeddedLobby);
  const [recordsPending,setRecordsPending] = useState<string|null>(null);
  const [shopVisible,setShopVisible] = useState(false);
  const [shopLoaded,setShopLoaded] = useState(!embeddedLobby);
  const [shopPending,setShopPending] = useState<string|null>(null);
  const [shopNotice,setShopNotice] = useState<{success:boolean;message:string}|null>(null);
  const [filter,setFilter] = useState<Filter>('all');
  const [arcadeOpen,setArcadeOpen] = useState(false);
  const [updates,setUpdates] = useState<LobbyUpdatesSnapshot>(fallbackUpdates);
  const [updatesOpen,setUpdatesOpen] = useState(false);
  const [exitOpen,setExitOpen] = useState(false);
  const [newsOpen,setNewsOpen]=useState(false);
  const [newsShown,setNewsShown]=useState(false);
  const [settingsOpen,setSettingsOpen] = useState(false);
  const [dailyOpen,setDailyOpen] = useState(false);
  const [lowMotion,setLowMotion] = useState(window.matchMedia('(prefers-reduced-motion: reduce)').matches);
  const [nativeSnapshotReceived,setNativeSnapshotReceived] = useState(!embeddedLobby);
  const [nativePreviewNotice,setNativePreviewNotice] = useState<string|null>(null);
  useEffect(()=>{
    const media = window.matchMedia('(prefers-reduced-motion: reduce)');
    const listener = () => setLowMotion(media.matches);
    media.addEventListener('change',listener);
    return ()=>media.removeEventListener('change',listener);
  },[]);
  useEffect(()=>{
    const stop = onRecordsMessage(message=>{
      if(message.type==='lobby.records.show'){
        setRecordsVisible(message.data.page);setShopVisible(false);setDailyOpen(false);setArcadeOpen(false);
      }else if(message.type==='lobby.records.snapshot'){
        if(message.data.page==='challenges'){setChallenges(message.data.snapshot);setChallengesLoaded(true);}
        else if(message.data.page==='statistics'){setStatistics(message.data.snapshot);setStatisticsLoaded(true);}
        else if(message.data.page==='profile'){setAccount(message.data.snapshot);setAccountLoaded(true);}
        else if(message.data.page==='history'){setHistory(message.data.snapshot);setHistoryLoaded(true);}
        else if(message.data.page==='vault'){setVault(message.data.snapshot);setVaultLoaded(true);}
        else if(message.data.page==='album'){setAlbum(message.data.snapshot);setAlbumLoaded(true);}
        else {setSeasons(message.data.snapshot);setSeasonsLoaded(true);}
      }else if(message.type==='lobby.album.result'){
        // The native reply includes the durable opening receipt. Consume it
        // directly so slow/invalid full archive snapshots cannot strand a
        // committed pack on the "breaking seal" screen.
        if(message.data.pending!==undefined)
          setAlbum(previous=>({...previous,pending:message.data.pending!}));
        setAlbumResult(message.data);
      }else if(message.type==='lobby.records.vault.result'){
        setRecordsPending(null);
        setVaultNotice(message.data.message);
        if(message.data.success)setVaultReward(message.data.reward);
      }else if(message.type==='lobby.records.result'){
        setRecordsPending(null);
        setShopNotice({success:message.data.success,message:message.data.message});
      }
    });
    return stop;
  },[]);
  useEffect(()=>{
    const stop = onShopMessages(message=>{
      if(message.type==='lobby.shop.snapshot'){setShop(message.data);setShopLoaded(true);}
      else if(message.type==='lobby.shop.result'){
        setShopPending(null);
        setShopNotice({success:message.data.success,message:message.data.message});
      }
      else if(message.type==='lobby.shop.show'){setShopVisible(true);setRecordsVisible(null);setDailyOpen(false);setArcadeOpen(false);}
      else if(message.type==='lobby.shop.hide'){setShopVisible(false);}
    });
    return stop;
  },[]);
  useEffect(()=>onUpdateSnapshot(setUpdates),[]);
  // Native window-close actions reuse this already-mounted React surface.
  // No second WebView2 controller is needed while the lobby is visible.
  useEffect(()=>{
    const host=window.chrome?.webview;
    const onHostMessage=(event:MessageEvent)=>{
      const value:unknown=event.data;
      if(value&&typeof value==='object'
        &&(value as {type?:unknown}).type==='lobby.exit.show')setExitOpen(true);
    };
    host?.addEventListener('message',onHostMessage);
    return()=>host?.removeEventListener('message',onHostMessage);
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
    if (nativeSnapshotReceived) { finishModeBoot(); lobbyCommand('lobby.ui-ready'); }
  },[nativeSnapshotReceived]);
  useEffect(()=>{
    document.documentElement.lang=data.language;
    document.title='Blackjack Casino · Lobby';
  },[data.language]);
  useEffect(()=>{if(nativeSnapshotReceived&&!newsShown){if(!data.nativePreview)setNewsOpen(true);setNewsShown(true);}},[nativeSnapshotReceived,newsShown]);
  const de = data.language==='de';
  const nativePreview = data.nativePreview===true;
  const previewUnavailable = () => setNativePreviewNotice(de
    ? 'Diese Funktion ist noch nicht mit der NativeShell-Vorschau verbunden. Spielstände werden nicht verändert.'
    : 'This feature is not connected to the NativeShell preview yet. Player data has not changed.');
  const closeRecords = () => {
    setRecordsVisible(null);setRecordsPending(null);setShopNotice(null);setVaultReward(null);setVaultNotice(null);setAlbumResult(null);
    lobbyCommand('lobby.records.close');
  };
  const openRecords = (page:RecordsPage) => {
    setArcadeOpen(false);
    if(shopVisible)closeShop();
    setRecordsVisible(page);setDailyOpen(false);setShopNotice(null);setRecordsPending(null);
    if(page==='vault'){setVaultReward(null);setVaultNotice(null);}
    if(page==='album')setAlbumResult(null);
    if(page==='challenges')setChallengesLoaded(!embeddedLobby);
    else if(page==='statistics')setStatisticsLoaded(!embeddedLobby);
    else if(page==='profile')setAccountLoaded(!embeddedLobby);
    else if(page==='history')setHistoryLoaded(!embeddedLobby);
    else if(page==='vault')setVaultLoaded(!embeddedLobby);
    else if(page==='album')setAlbumLoaded(!embeddedLobby);
    else setSeasonsLoaded(!embeddedLobby);
    lobbyCommand('lobby.records.open',page);
  };
  const openShop = () => {
    setArcadeOpen(false);
    if(recordsVisible)closeRecords();
    setShopNotice(null);
    setShopVisible(true);setShopLoaded(!embeddedLobby);setDailyOpen(false);
    lobbyCommand('lobby.shop.open');
  };
  const closeShop = () => {
    setShopVisible(false);setShopNotice(null);setShopPending(null);
    lobbyCommand('lobby.shop.close');
  };
  const openMode = (id:string)=>{
    if(nativePreview&&id!==ARCADE_MODE_ID&&!data.modes.some(mode=>mode.id===id&&mode.enabled)){
      previewUnavailable();return;
    }
    if(shopVisible)closeShop();
    if(recordsVisible)closeRecords();
    if(id==='roulette'&&!embeddedLobby){window.location.href='./roulette.html';return;}
    if(id===ARCADE_MODE_ID){
      setDailyOpen(false);setFilter('all');setArcadeOpen(true);
      return;
    }
    setArcadeOpen(false);
    lobbyCommand('lobby.open',id);
  };
  const openSecondary = (id:string)=>{
    if(nativePreview&&!data.modes.some(mode=>mode.id===id&&mode.secondary)){previewUnavailable();return;}
    lobbyCommand('lobby.open-secondary',id);
  };
  const openProgression = (id:string)=>{
    if(id==='Cosmetic Shop'){openShop();return;}
    if(id==="Collector's Album"||id==='Album'){openRecords('album');return;}
    if(id==='Challenges'||id==='Statistics'||id==='Season Pass'){
      openRecords(id==='Challenges'?'challenges':id==='Season Pass'?'season':'statistics');return;
    }
    if(shopVisible)closeShop();
    if(recordsVisible)closeRecords();
    lobbyCommand('lobby.progression',id);
  };
  const chooseNav = (id:Filter)=>{
    setArcadeOpen(false);
    if(id==='collections') {openProgression("Collector's Album");return;}
    if(id==='shop') {openShop();return;}
    if(shopVisible)closeShop();
    if(recordsVisible)closeRecords();
    if(id==='stats') {openProgression('Statistics');return;}
    if(id==='season') {openProgression('Season Pass');return;}
    if(id==='challenges') {openProgression('Challenges');return;}
    if(id==='vault') {openRecords('vault');return;}
    if(id==='daily') {setDailyOpen(true);return;}
    if(id==='profile') {openRecords('profile');return;}
    setDailyOpen(false);
    setFilter(id);
  };
  const openUpdates = () => {
    setSettingsOpen(false);
    setUpdatesOpen(true);
    lobbyCommand('lobby.updates.open');
  };
  const cancelExit=()=>{
    setExitOpen(false);
    lobbyCommand('lobby.exit.cancel');
  };
  const confirmExit=()=>{
    setExitOpen(false);
    lobbyCommand('lobby.exit');
  };
  const closeUpdates = () => {
    if(updates.busy)return;
    setUpdatesOpen(false);
    lobbyCommand('lobby.updates.close');
  };
  // Arcade is a local selection screen; it does not issue an unknown native game route.
  // Preserve the artwork DOM and its Pixi scene objects while a subpage is
  // open. Hiding instead of unmounting prevents delayed icon reconstruction.
  const lobbyContentVisible=recordsVisible===null&&!shopVisible&&!arcadeOpen;
  // Keep the same keyed tile elements across category changes; their PixiJS
  // sprites and first-frame readiness must not reset when returning to Lobby.
  const modes = stableLobbyTiles(
    [...data.modes.filter(x=>x.id!==ARCADE_MODE_ID),arcadeLobbyMode(data.language)],
    filter,artOrder);
  const hasVisibleModes=modes.some(({visible})=>visible);
  if(embeddedLobby&&!nativeSnapshotReceived){
    return <div className="casino-lobby-shell native-lobby-boot" role="status" aria-live="polite">
      <div className="native-lobby-loading">THE MIDNIGHT CLUB · {data.language==='de'?'Lade Spielerdaten…':'Loading player data…'}</div>
    </div>;
  }
  return <div className="casino-lobby-shell" data-reduced={lowMotion || !data.animationsEnabled}>
    {nativePreviewNotice&&<div className="native-preview-notice" role="status">
      <strong>{de?'NativeShell-Vorschau':'NativeShell Preview'}</strong>
      <span>{nativePreviewNotice}</span>
      <button type="button" onClick={()=>setNativePreviewNotice(null)}
        aria-label={de?'Hinweis schließen':'Dismiss notice'}>×</button>
    </div>}
    <LobbyAmbient motionEnabled={!lowMotion && data.animationsEnabled}/>
    <header className="lobby-topbar">
      <button className="lobby-brand" type="button"
        onPointerDown={event=>{if(event.pointerType!=='touch')event.preventDefault();}}
        onClick={event=>{if(event.detail>0)event.currentTarget.blur();if(shopVisible)closeShop();if(recordsVisible)closeRecords();setArcadeOpen(false);setFilter('all');}}>
        <span className="brand-spade" aria-hidden="true">♠</span>
        <span><b>BLACKJACK CASINO</b><i>THE MIDNIGHT CLUB</i></span>
      </button>
      <div className="lobby-top-status">
        <button className="header-widget header-compact" onClick={()=>openProgression('Challenges')}>
          <span className="header-emoji">♛</span><span><small>{de?'AUFGABEN':'CHALLENGES'}</small>
          <b>{data.challengeCompleted} / {data.challengeTotal} · {percent(data.challengeOverallFraction)}</b><Progress value={data.challengeOverallFraction}/></span>
        </button>
        <button className="header-widget header-compact" onClick={()=>openProgression("Collector's Album")}>
          <span className="header-emoji">▤</span><span><small>ALBUM</small><b>{data.albumOwned} / {data.albumTotal}</b></span>
        </button>
        <button className="header-widget profile-widget" onClick={()=>openRecords('profile')}>
          <PlayerPortrait portrait={data.portraitVariant} frame={data.frameVariant}/><span><b>{data.displayName}</b>
          <small>LV {data.level} · ◆ {data.coins}</small><em><CosmeticTitle variant={data.titleVariant}>{localizedSudokuTitleName(data.title,data.language)}</CosmeticTitle></em></span>
          {data.prestige>0&&<span className="prestige-badge">✦<br/>P{data.prestige}</span>}
        </button>
        <button className="header-widget vault-widget" onClick={()=>openRecords('vault')}>
          <VaultIcon tier={data.vaultTier}/><span>
            <small>VAULT · {data.vaultTier}</small><b>{data.vaultAmount} / {data.vaultCapacity}</b>
            <Progress value={data.vaultFraction} tone="gold"/></span>
        </button>
      </div>
      <div className="lobby-finances">
        <span><small>BANKROLL</small><strong>{data.bankroll}</strong></span>
        {data.showEmpireBalance&&<span className="empire-wallet"><small>EMPIRE CREDITS</small><strong>{data.empireCredits}</strong></span>}
      </div>
      <button type="button" className="lobby-exit" onClick={()=>setExitOpen(true)}>
        ↪ <span>{de?'Beenden':'Exit'}</span>
      </button>
    </header>
    <div className={'lobby-body '+(shopVisible?'shop-active':recordsVisible?'records-active':arcadeOpen?'arcade-active':'')}>
      <nav className="lobby-nav" aria-label={de?'Casino-Navigation':'Casino navigation'}>
        <div className="lobby-nav-scroll">
        <button className="lobby-nav-item" onClick={()=>setNewsOpen(true)}><NavIcon name="news"/><span>News</span></button>
        {navigation.map(n=><button type="button" key={n.id}
          aria-current={(shopVisible?n.id==='shop':recordsVisible?n.id===(recordsVisible==='challenges'?'challenges':recordsVisible==='statistics'?'stats':recordsVisible==='history'?'profile':recordsVisible==='vault'?'vault':recordsVisible==='album'?'collections':recordsVisible==='season'?'season':'profile'):n.id===filter)?'page':undefined}
          className={'lobby-nav-item '+((shopVisible?n.id==='shop':recordsVisible?n.id===(recordsVisible==='challenges'?'challenges':recordsVisible==='statistics'?'stats':recordsVisible==='vault'?'vault':recordsVisible==='album'?'collections':recordsVisible==='season'?'season':'profile'):n.id===filter)?'selected':'')+(n.id==='daily'?' daily-nav':'')+(n.id==='collections'?' nav-section-start':'')}
          onClick={()=>chooseNav(n.id)}>
          <NavIcon name={n.id}/>
          <span className="lobby-nav-label">{de?n.de:n.en}</span>
          {((n.id==='challenges'&&data.challengesReady>0)||
            (n.id==='collections'&&data.albumPacksReady>0))&&
            <span className="nav-ready" aria-label={
              (n.id==='collections'
                ?(de?'Ungeöffnete Packs: ':'Unopened packs: ')
                :(de?'Abholbereite Belohnungen: ':'Claimable rewards: '))+
              (n.id==='collections'?data.albumPacksReady:data.challengesReady)}>
              +{Math.min(99,n.id==='collections'?data.albumPacksReady:data.challengesReady)}
            </span>}
        </button>)}
        <div className="lobby-nav-decoration" aria-hidden="true">♣</div>
        </div>
        <button type="button" className="lobby-updates-entry lobby-settings-entry" data-testid="lobby-settings-entry"
          aria-haspopup="dialog" aria-expanded={settingsOpen}
          onClick={()=>{setUpdatesOpen(false);setSettingsOpen(true);}}>
          <NavIcon name="settings"/>
          <span>{de?'Einstellungen':'Settings'}</span>
        </button>
        <button type="button" className="lobby-updates-entry" data-testid="lobby-updates-entry"
          aria-haspopup="dialog" aria-expanded={updatesOpen}
          onClick={openUpdates}>
          <NavIcon name="updates"/>
          <span>{de?'Updates':'Updates'}</span>
          {updates.updateAvailable&&<span className="lobby-updates-dot" aria-label={de?'Update verfügbar':'Update available'}/>}
        </button>
      </nav>
      {recordsVisible==='challenges'?<ModernChallenges data={challenges}
        language={data.language} loaded={challengesLoaded} pending={recordsPending}
        onBack={()=>{closeRecords();setFilter('all');}}
        onClaim={id=>{setRecordsPending(id);lobbyCommand('lobby.records.claim',id);}}
        onClaimAll={()=>{setRecordsPending('all');lobbyCommand('lobby.records.claim-all');}}/>:
      recordsVisible==='statistics'?<ModernStatistics data={statistics}
        language={data.language} loaded={statisticsLoaded}
        onBack={()=>{closeRecords();setFilter('all');}}/>:
      recordsVisible==='profile'?<ModernAccount data={account} loaded={accountLoaded} pending={recordsPending}
        language={data.language} onBack={()=>{closeRecords();setFilter('all');}}
        onRoute={route=>route==='shop'?openShop():route==='album'?openProgression("Collector's Album"):
          openRecords(route as RecordsPage)}
        onRename={name=>{setRecordsPending('rename');renamePlayer(name);}}
        onEquip={id=>{setRecordsPending(id);lobbyCommand('lobby.profile.equip',id);}}
        onEdit={()=>lobbyCommand('lobby.profile.native')}/>:
      recordsVisible==='history'?<ModernHistory data={history} loaded={historyLoaded}
        language={data.language} onBack={()=>{closeRecords();setFilter('all');}}/>:
      recordsVisible==='album'?<ModernAlbum data={album} loaded={albumLoaded}
        language={data.language} animate={!lowMotion&&data.animationsEnabled}
        soundEnabled={data.albumSoundEnabled} result={albumResult}
        onBack={()=>{closeRecords();setFilter('all');}}
        onOpen={(rarity:AlbumRarity)=>lobbyCommand('lobby.album.open',rarity)}
        onComplete={id=>lobbyCommand('lobby.album.complete',id)}
        onClaim={id=>lobbyCommand('lobby.album.claim',id)}
        onShop={openShop}/>:
      recordsVisible==='vault'?<ModernVault data={vault} loaded={vaultLoaded}
        language={data.language} pending={recordsPending==='vault'} reward={vaultReward} notice={vaultNotice}
        onBack={()=>{closeRecords();setFilter('all');}}
        onOpen={()=>{setRecordsPending('vault');lobbyCommand('lobby.records.vault.open');}}
        onAlbum={()=>openProgression("Collector's Album")}/>:
      recordsVisible==='season'?<ModernSeasons data={seasons} loaded={seasonsLoaded}
        language={data.language} pending={recordsPending} onBack={()=>{closeRecords();setFilter('all');}}
        onActivate={id=>{setRecordsPending(id);lobbyCommand('lobby.records.season.activate',id);}}/>:
      shopVisible?<ModernShop shop={shop} loaded={shopLoaded} language={data.language} pending={shopPending}
        motionEnabled={!lowMotion && data.animationsEnabled}
        onBack={()=>{closeShop();setFilter('all');}}
        onCosmetic={id=>{setShopPending(id);lobbyCommand('lobby.shop.action',id);}}
        onPack={rarity=>{setShopPending(rarity);lobbyCommand('lobby.shop.pack',rarity);}}
        onAlbum={()=>openProgression("Collector's Album")}/>:
      arcadeOpen?<ArcadeMenu language={data.language} onBack={()=>setArcadeOpen(false)}/>:null}
      <main className="lobby-main" style={{display:lobbyContentVisible?undefined:'none'}}>
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
          {modes.map(({mode,visible})=><GameTile key={mode.id} mode={mode}
            hidden={!visible} language={data.language}
            motionEnabled={!lowMotion && data.animationsEnabled}
            onOpen={openMode} onSecondary={openSecondary}/>)}
          {!hasVisibleModes&&<p className="empty-mode">{de?'Keine Spiele in dieser Kategorie.':'No games in this category.'}</p>}
        </section>
        {!embeddedLobby&&<small className="browser-note">Browser preview · navigation requires the Windows client.</small>}
      </main>
      {lobbyContentVisible&&<DailyLogin data={data} onClaim={()=>lobbyCommand('lobby.daily.claim')}
        onClose={()=>setDailyOpen(false)} mobileOpen={dailyOpen}/>}
    </div>
    {newsOpen&&<Modal className="casino-news" label="News" onClose={()=>setNewsOpen(false)}>
      <header><div><small>THE MIDNIGHT CLUB · {data.installedVersion??updates.installedVersion}</small><h2>{de?'Neuigkeiten':'News'}</h2></div>
       <button onClick={()=>setNewsOpen(false)} aria-label={de?'Schließen':'Close'}>×</button></header>
      <div className="casino-news-items">{(data.news??[]).map((note,i)=><article key={i} style={{borderColor:note.accent}}><h3>{note.title}</h3><p>{note.description}</p></article>)}
       {!data.news?.length&&<p>{de?'Für diese Vorschau sind keine Versionshinweise geladen.':'Release notes are unavailable in this preview.'}</p>}</div>
      <button className="lobby-gold-button" onClick={()=>setNewsOpen(false)}>{de?'ZUM CASINO':'ENTER CASINO'}</button>
    </Modal>}
    {settingsOpen&&<div className="lobby-updates-backdrop">
      <section className="lobby-updates-panel lobby-settings-panel" role="dialog" aria-modal="true"
        aria-label={de?'Casino-Einstellungen':'Casino settings'}>
        <header className="lobby-updates-header">
          <div><small>THE MIDNIGHT CLUB</small>
            <h2>{de?'Einstellungen':'Settings'}</h2>
            <p>{de?'Musik, Sprache und Casino-Anzeige verwalten.':'Manage language, music and the shared casino display.'}</p>
          </div>
          <button type="button" className="lobby-updates-close" aria-label={de?'Schließen':'Close'}
            onClick={()=>setSettingsOpen(false)}>×</button>
        </header>
        <label className="lobby-settings-language">
          <span>{de?'Sprache':'Language'}</span>
          <select data-testid="lobby-language-select" value={data.language}
            onChange={e=>lobbyCommand('lobby.language.set',e.target.value)}>
            <option value="de">Deutsch</option>
            <option value="en">English</option>
          </select>
        </label>

        <label className="lobby-settings-check">
          <input type="checkbox" data-testid="lobby-music-enabled" checked={data.musicEnabled}
            onChange={e=>lobbyCommand('lobby.settings.music',String(e.target.checked))}/>
          <span>{de?'Hintergrundmusik':'Background music'}</span>
        </label>
        <label className="lobby-settings-volume">
          <span>{de?'Musiklautstärke':'Music volume'}: <strong>{Math.round(data.musicVolume*100)}%</strong></span>
          <input type="range" data-testid="lobby-music-volume" min={0} max={32} step={2}
            value={Math.round(data.musicVolume*100)}
            onChange={e=>lobbyCommand('lobby.settings.volume',e.target.value)}/>
        </label>
        <label className="lobby-settings-check">
          <input type="checkbox" data-testid="lobby-empire-balance" checked={data.showEmpireBalance}
            onChange={e=>lobbyCommand('lobby.settings.empire',String(e.target.checked))}/>
          <span>{de?'Empire-Guthaben in der Kopfzeile anzeigen':'Show Empire balance in header'}</span>
        </label>
        <button type="button" onClick={()=>lobbyCommand('lobby.settings.fullscreen')}>
          {de?'Vollbild umschalten (F11)':'Toggle fullscreen (F11)'}
        </button>
      </section>
    </div>}
    {updatesOpen&&<ModernUpdates state={updates} language={data.language}
      onClose={closeUpdates} onChannel={value=>lobbyCommand('lobby.updates.channel.set',value)}
      onCheck={()=>lobbyCommand('lobby.updates.check')}
      onInstall={()=>lobbyCommand('lobby.updates.install')}/>}
    {exitOpen&&<ExitPrompt language={data.language} onStay={cancelExit} onExit={confirmExit}/>}
    {shopNotice&&<div role="status" className={'shop-notice '+(shopNotice.success?'success':'error')}>
      <span>{shopNotice.message}</span>
      <button type="button" onClick={()=>setShopNotice(null)} aria-label="Dismiss message">×</button>
    </div>}
  </div>;
}

createRoot(document.getElementById('root')!).render(<LobbyApp/>);
