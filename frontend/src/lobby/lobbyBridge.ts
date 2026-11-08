/** Presentation-only lobby bridge. The native shell owns navigation and all player state. */
export type LobbyMode = {
  id: string; title: string; category: string; description: string;
  enabled: boolean; secondary: boolean;
};
export type DailyLoginSnapshot = {
  canClaim: boolean; claimedToday: boolean; totalClaims: number;
  activeStep: number; nextStep: number; cycleCompletedSteps: number;
  activeReward: string; nextReward: string;
  rewards: {day:number;bankroll:string;coins:number;pack:string|null}[];
};
export type LobbySnapshot = {
  dailyLogin: DailyLoginSnapshot;
 titleVariant?:string;portraitVariant?:string;frameVariant?:string;
  displayName: string; level: number; coins: string; prestige: number; title: string;
  bankroll: string; empireCredits: string; albumOwned: number; albumTotal: number; albumPacksReady: number;
  seasonTier: number; seasonNextTier: number; seasonExperience: number; seasonRequired: number;
  seasonFraction: number; seasonComplete: boolean; challengeName: string;
  challengeFraction: number; challengesReady: number; vaultTier: string;
  vaultAmount: string; vaultCapacity: string; vaultFraction: number;
  animationsEnabled: boolean; albumSoundEnabled: boolean;
  musicEnabled: boolean; musicVolume: number; showEmpireBalance: boolean;
  news?:{title:string;description:string;accent:string}[];installedVersion?:string;
  language: 'de' | 'en'; modes: LobbyMode[];
};

const object = (x: unknown): x is Record<string, unknown> =>
  x !== null && typeof x === 'object' && !Array.isArray(x);
const string = (x: unknown): x is string => typeof x === 'string';
const numeric = (x: unknown): x is number => typeof x === 'number' && Number.isFinite(x);
const fraction = (x: unknown) => numeric(x) && x >= 0 && x <= 1;

export function isLobbySnapshot(x: unknown): x is LobbySnapshot {
  if (!object(x) || !object(x.dailyLogin)) return false;
  if (!['titleVariant','portraitVariant','frameVariant'].every(k=>x[k]===undefined||
    typeof x[k]==='string'&&/^[a-z0-9-]{1,100}$/.test(x[k] as string)))return false;
  if(x.installedVersion!==undefined&&(!string(x.installedVersion)||x.installedVersion.length>80))return false;
  if(x.news!==undefined&&(!Array.isArray(x.news)||x.news.length>24||!x.news.every(n=>object(n)&&
    string(n.title)&&n.title.length<=2000&&string(n.description)&&n.description.length<=12000&&
    string(n.accent)&&/^#[0-9a-f]{6}$/i.test(n.accent))))return false;
  const daily = x.dailyLogin;
  if (typeof daily.canClaim !== 'boolean' || typeof daily.claimedToday !== 'boolean'
    || !['totalClaims','activeStep','nextStep','cycleCompletedSteps'].every(k => numeric(daily[k]) && Number(daily[k]) >= 0)
    || !string(daily.activeReward) || !string(daily.nextReward)
    || !Array.isArray(daily.rewards) || daily.rewards.length !== 7
    || !daily.rewards.every((r:unknown,i:number)=>object(r)&&r.day===i+1
      && string(r.bankroll)&&numeric(r.coins)&&r.coins>=0
      && (r.pack===null||string(r.pack)))) return false;
  if (!['displayName','coins','title','bankroll','empireCredits','challengeName',
    'vaultTier','vaultAmount','vaultCapacity'].every(k => string(x[k]))) return false;
  if (!['level','prestige','albumOwned','albumTotal','seasonTier','seasonNextTier',
    'seasonExperience','seasonRequired','challengesReady','albumPacksReady'].every(k => numeric(x[k]) && Number(x[k]) >= 0)) return false;
  if (!['seasonFraction','challengeFraction','vaultFraction'].every(k => fraction(x[k]))) return false;
  if (typeof x.seasonComplete !== 'boolean' || typeof x.albumSoundEnabled !== 'boolean'
    || typeof x.animationsEnabled !== 'boolean'
    || typeof x.musicEnabled !== 'boolean' || typeof x.showEmpireBalance !== 'boolean'
    || !numeric(x.musicVolume) || x.musicVolume < 0 || x.musicVolume > 0.32
    || (x.language !== 'de' && x.language !== 'en')
    || !Array.isArray(x.modes) || x.modes.length > 40) return false;
  return x.modes.every(m => object(m) && string(m.id) && /^[a-z0-9-]{1,48}$/.test(m.id)
    && string(m.title) && string(m.category) && string(m.description)
    && typeof m.enabled === 'boolean' && typeof m.secondary === 'boolean');
}

export const fallbackLobby: LobbySnapshot = {
  dailyLogin: {
    canClaim:true,claimedToday:false,totalClaims:0,activeStep:1,nextStep:1,
    cycleCompletedSteps:0,activeReward:'$1,000.00',nextReward:'$1,000.00',
    rewards:[
      {day:1,bankroll:'$1,000.00',coins:40,pack:'Common'},
      {day:2,bankroll:'$1,500.00',coins:40,pack:null},
      {day:3,bankroll:'$2,000.00',coins:40,pack:null},
      {day:4,bankroll:'$2,500.00',coins:40,pack:'Rare'},
      {day:5,bankroll:'$3,000.00',coins:40,pack:null},
      {day:6,bankroll:'$3,500.00',coins:40,pack:null},
      {day:7,bankroll:'$4,000.00',coins:80,pack:'Epic'},
    ],
  },
  displayName: 'Guest', level: 1, coins: '0', prestige: 0, title: 'Midnight Dealer',
  bankroll: '$1,000.00', empireCredits: '0 cr', albumOwned: 0, albumTotal: 136, albumPacksReady: 0,
  seasonTier: 0, seasonNextTier: 1, seasonExperience: 0, seasonRequired: 2000,
  seasonFraction: 0, seasonComplete: false, challengeName: 'Unbroken',
  challengeFraction: 0, challengesReady: 0, vaultTier: 'BRONZE',
  vaultAmount: '$0.00', vaultCapacity: '$100,000.00', vaultFraction: 0,
  animationsEnabled: true, albumSoundEnabled:true,
  musicEnabled:true, musicVolume:0.10, showEmpireBalance:true,
  language: 'en', modes: [
    {id:'blackjack',title:'Blackjack',category:'Table games',description:'A quiet seat, a fresh shoe and your next decision.',enabled:true,secondary:false},
    {id:'kniffel',title:'Midnight Dice',category:'Table games',description:'Five dice and your score sheet. Play solo or with AI opponents.',enabled:true,secondary:false},
    {id:'chill-lounge',title:'Chill Lounge',category:'Relaxed games',description:'Offline games at your own pace: Sudoku and Solitaire.',enabled:true,secondary:false},
    {id:'casino-empire',title:'Casino Empire',category:'Idle casino',description:'Build your midnight landmark with lasting upgrades.',enabled:true,secondary:true},
    {id:'midnight-reels',title:'Midnight Reels',category:'Classic slots',description:'Three reels, a centre payline and old-school charm.',enabled:true,secondary:false},
    {id:'neon-vault',title:'Neon Vault',category:'Video slots',description:'Five reels, 1,024 ways and Vault Break Free Spins.',enabled:true,secondary:false},
    {id:'nine-lives',title:'Nine Lives',category:'Video slots',description:'Five lines, Pounce Wilds and nine bonus spins.',enabled:true,secondary:false},
    {id:'midnight-manor',title:'Midnight Manor',category:'Video slots',description:'Five haunted reels, Free Spins and rooms that remember.',enabled:true,secondary:false},
    {id:'midnight-merge',title:'Midnight Merge',category:'Casino restoration',description:'Bring the Midnight Club back room by room.',enabled:true,secondary:false},
  ],
};

type HostMessage = { type: string; data: unknown };
export const embeddedLobby = typeof window !== 'undefined' && Boolean(window.chrome?.webview);
export function onLobbySnapshot(onSnapshot: (snapshot: LobbySnapshot) => void): () => void {
  const host = window.chrome?.webview;
  if (!host) return () => {};
  const handler = (event: MessageEvent) => {
    const data: unknown = event.data;
    if (!object(data) || data.type !== 'lobby.snapshot' || !isLobbySnapshot(data.data)) return;
    onSnapshot(data.data);
  };
  host.addEventListener('message', handler);
  return () => host.removeEventListener('message', handler);
}
export function lobbyCommand(type: string, id?: string): void {
  const allowed = ['lobby.ready','lobby.ui-ready','lobby.open','lobby.open-secondary',
    'lobby.progression','lobby.profile','lobby.profile.native','lobby.profile.equip','lobby.vault','lobby.daily.claim','lobby.language.set','lobby.exit','lobby.refresh',
    'lobby.shop.open','lobby.shop.close','lobby.shop.action','lobby.shop.pack',
    'lobby.records.open','lobby.records.close','lobby.records.claim','lobby.records.claim-all','lobby.records.season.activate',
    'lobby.records.vault.open','lobby.album.open','lobby.album.complete','lobby.album.claim',
    'lobby.updates.open','lobby.updates.close','lobby.updates.channel.set',
    'lobby.updates.check','lobby.updates.install',
    'lobby.settings.music','lobby.settings.volume','lobby.settings.empire','lobby.settings.fullscreen'];
  if (!allowed.includes(type)) return;
  window.chrome?.webview?.postMessage(id === undefined ? { type } : { type, id });
}

/** A profile name is user text, never a command id. The native host validates and saves it. */
export function renamePlayer(value:string):void {
  if(value.length>256)return;
  window.chrome?.webview?.postMessage({type:'lobby.profile.rename',value});
}
