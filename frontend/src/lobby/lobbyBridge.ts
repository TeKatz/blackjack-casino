/** Presentation-only lobby bridge. The native shell owns navigation and all player state. */
export type LobbyMode = {
  id: string; title: string; category: string; description: string;
  enabled: boolean; secondary: boolean;
};
export type LobbySnapshot = {
  displayName: string; level: number; coins: string; prestige: number; title: string;
  bankroll: string; empireCredits: string; albumOwned: number; albumTotal: number;
  seasonTier: number; seasonNextTier: number; seasonExperience: number; seasonRequired: number;
  seasonFraction: number; seasonComplete: boolean; challengeName: string;
  challengeFraction: number; challengesReady: number; vaultTier: string;
  vaultAmount: string; vaultCapacity: string; vaultFraction: number;
  animationsEnabled: boolean; language: 'de' | 'en'; modes: LobbyMode[];
};

const object = (x: unknown): x is Record<string, unknown> =>
  x !== null && typeof x === 'object' && !Array.isArray(x);
const string = (x: unknown): x is string => typeof x === 'string';
const numeric = (x: unknown): x is number => typeof x === 'number' && Number.isFinite(x);
const fraction = (x: unknown) => numeric(x) && x >= 0 && x <= 1;

export function isLobbySnapshot(x: unknown): x is LobbySnapshot {
  if (!object(x)) return false;
  if (!['displayName','coins','title','bankroll','empireCredits','challengeName',
    'vaultTier','vaultAmount','vaultCapacity'].every(k => string(x[k]))) return false;
  if (!['level','prestige','albumOwned','albumTotal','seasonTier','seasonNextTier',
    'seasonExperience','seasonRequired','challengesReady'].every(k => numeric(x[k]) && Number(x[k]) >= 0)) return false;
  if (!['seasonFraction','challengeFraction','vaultFraction'].every(k => fraction(x[k]))) return false;
  if (typeof x.seasonComplete !== 'boolean' || typeof x.animationsEnabled !== 'boolean'
    || (x.language !== 'de' && x.language !== 'en')
    || !Array.isArray(x.modes) || x.modes.length > 40) return false;
  return x.modes.every(m => object(m) && string(m.id) && /^[a-z0-9-]{1,48}$/.test(m.id)
    && string(m.title) && string(m.category) && string(m.description)
    && typeof m.enabled === 'boolean' && typeof m.secondary === 'boolean');
}

export const fallbackLobby: LobbySnapshot = {
  displayName: 'Guest', level: 1, coins: '0', prestige: 0, title: 'Midnight Dealer',
  bankroll: '$1,000.00', empireCredits: '0 cr', albumOwned: 0, albumTotal: 136,
  seasonTier: 0, seasonNextTier: 1, seasonExperience: 0, seasonRequired: 2000,
  seasonFraction: 0, seasonComplete: false, challengeName: 'Unbroken',
  challengeFraction: 0, challengesReady: 0, vaultTier: 'BRONZE',
  vaultAmount: '$0.00', vaultCapacity: '$100,000.00', vaultFraction: 0,
  animationsEnabled: true, language: 'en', modes: [
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
export const embeddedLobby = Boolean(window.chrome?.webview);
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
    'lobby.progression','lobby.profile','lobby.vault','lobby.exit','lobby.refresh'];
  if (!allowed.includes(type)) return;
  window.chrome?.webview?.postMessage(id === undefined ? { type } : { type, id });
}
