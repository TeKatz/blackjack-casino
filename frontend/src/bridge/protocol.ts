/** V2 WebView2 bridge. Browser sends intent only; C# owns rules and settlement. */
export const PROTOCOL_VERSION = 2 as const;

export type ManorRoom = {
  id: number;
  name: string;
  x: number;
  y: number;
  area: string;
  visited: boolean;
  available: boolean;
  locked: boolean;
};

export type ManorPassage = { from: number; to: number; requiresKey: boolean };
export type ManorWinningLine = { number: number; count: number; rows: number[]; amount: string };
export type ManorPaytableEntry = {
  symbol: string; name: string; three: number; four: number; five: number;
};
export type ManorRulesInfo = {
  version: number; symbols: ManorPaytableEntry[]; paylines: number[][];
};

export type ManorRun = {
  status: string;
  message: string;
  ghostClue: string;
  playerRoom: number;
  turn: number;
  exitTarget: number;
  current: string;
  banked: string;
  hasCandle: boolean;
  hasKey: boolean;
  hasCharm: boolean;
  rooms: ManorRoom[];
  passages: ManorPassage[];
};

export type ManorRunConclusion = {
  status: 'Escaped' | 'Caught' | 'CashedOut' | 'Exhausted';
  message: string;
  multiplier: string;
  award: string;
};

export type ManorSnapshot = {
  bankroll: string;
  bet: string;
  selectedBet: string;
  cycleReturned: string;
  payout: string;
  cells: string[];
  winningCells: number[];
  cycleActive: boolean;
  freeSpinsRemaining: number;
  feature: string;
  animationsEnabled: boolean;
  hasCommittedSpin: boolean;
  revision: string;
  canSpin: boolean;
  canFreeSpin: boolean;
  lastSpinFree: boolean;
  scatters: number;
  doors: number;
  winningLines: number;
  winningLineDetails: ManorWinningLine[];
  language: 'en' | 'de';
  rules: ManorRulesInfo;
  /** Read-only desktop-level audio constraints, not a separate persisted preference. */
  globalSoundEnabled: boolean;
  globalMusicEnabled: boolean;
  globalMusicVolume: number;
  run: ManorRun | null;
  /** Ephemeral C# ManorRun: never affects bankroll or persistent journal. */
  practiceRun?: ManorRun | null;
  /** Last already-committed real run settlement, no client-side payout math. */
  lastRun?: ManorRunConclusion | null;
  saveNotice: string | null;
};

export type HostEnvelope =
  | { protocolVersion: 2; type: 'snapshot'; requestId: string; payload: ManorSnapshot }
  | { protocolVersion: 2; type: 'error'; requestId: string; payload: {
    message: string; fatal: boolean; snapshot: ManorSnapshot;
  } };

export type GameAction = 'spin.paid' | 'spin.free' | 'run.move' | 'run.candle'
  | 'run.cashout' | 'run.follow' | 'run.auto'
  | 'practice.start' | 'practice.restart' | 'practice.exit'
  | 'practice.move' | 'practice.candle' | 'practice.cashout'
  | 'practice.follow' | 'practice.auto';

export type ActionArguments = {
  expectedRevision: string;
  bet?: string;
  room?: number;
};

type WebView = {
  postMessage(message: object): void;
  addEventListener(name: 'message', cb: (event: MessageEvent) => void): void;
  removeEventListener(name: 'message', cb: (event: MessageEvent) => void): void;
};

declare global {
  interface Window {
    chrome?: { webview?: WebView };
  }
}

const text = (value: unknown): value is string => typeof value === 'string';
const flag = (value: unknown): value is boolean => typeof value === 'boolean';
const nonnegative = (value: unknown): value is number =>
  Number.isInteger(value) && typeof value === 'number' && value >= 0;
const dictionary = (value: unknown): value is Record<string, unknown> =>
  value !== null && typeof value === 'object' && !Array.isArray(value);

function isRun(value: unknown): value is ManorRun {
  if (!dictionary(value)) return false;
  return ['status', 'message', 'ghostClue', 'current', 'banked'].every(k => text(value[k])) &&
    nonnegative(value.playerRoom) && nonnegative(value.turn) && nonnegative(value.exitTarget) &&
    flag(value.hasCandle) && flag(value.hasKey) && flag(value.hasCharm) &&
    Array.isArray(value.rooms) && value.rooms.every(room =>
      dictionary(room) && nonnegative(room.id) && text(room.name) &&
      Number.isInteger(room.x) && Number.isInteger(room.y) && text(room.area) &&
      flag(room.visited) && flag(room.available) && flag(room.locked)) &&
    Array.isArray(value.passages) && value.passages.length <= 30 &&
    value.passages.every(edge => dictionary(edge) && nonnegative(edge.from) &&
      edge.from < 17 && nonnegative(edge.to) && edge.to < 17 &&
      edge.from !== edge.to && flag(edge.requiresKey));
}

export function isSnapshot(value: unknown): value is ManorSnapshot {
  if (!dictionary(value)) return false;
  return ['bankroll', 'bet', 'selectedBet', 'cycleReturned', 'payout', 'feature', 'revision']
    .every(k => text(value[k])) &&
    /^[0-9]{1,19}$/.test(value.revision as string) &&
    Array.isArray(value.cells) && value.cells.length === 15 && value.cells.every(text) &&
    Array.isArray(value.winningCells) &&
    value.winningCells.every(x => nonnegative(x) && x < 15) &&
    ['cycleActive', 'animationsEnabled', 'hasCommittedSpin', 'canSpin', 'canFreeSpin', 'lastSpinFree']
      .every(k => flag(value[k])) &&
    ['freeSpinsRemaining', 'scatters', 'doors', 'winningLines'].every(k => nonnegative(value[k])) &&
    Array.isArray(value.winningLineDetails) && value.winningLineDetails.length <= 20 &&
    value.winningLineDetails.every(line => dictionary(line) &&
      nonnegative(line.number) && line.number >= 1 && line.number <= 20 &&
      nonnegative(line.count) && line.count >= 3 && line.count <= 5 &&
      text(line.amount) && Array.isArray(line.rows) && line.rows.length === 5 &&
      line.rows.every(row => nonnegative(row) && row <= 2)) &&
    flag(value.globalSoundEnabled) && flag(value.globalMusicEnabled) &&
    typeof value.globalMusicVolume === 'number' &&
    Number.isFinite(value.globalMusicVolume) &&
    value.globalMusicVolume >= 0 && value.globalMusicVolume <= 1 &&
    (value.language === 'en' || value.language === 'de') &&
    dictionary(value.rules) && (value.rules.version === 1 || value.rules.version === 2) &&
    Array.isArray(value.rules.symbols) && value.rules.symbols.length === 14 &&
    value.rules.symbols.every(entry => dictionary(entry) && text(entry.symbol) &&
      text(entry.name) && ['three', 'four', 'five'].every(k => nonnegative(entry[k]))) &&
    Array.isArray(value.rules.paylines) && value.rules.paylines.length === 20 &&
    value.rules.paylines.every(line => Array.isArray(line) && line.length === 5 &&
      line.every(row => nonnegative(row) && row <= 2)) &&
    (value.run === null || isRun(value.run)) &&
    (value.practiceRun === undefined || value.practiceRun === null || isRun(value.practiceRun)) &&
    (value.lastRun === undefined || value.lastRun === null ||
      (dictionary(value.lastRun) && text(value.lastRun.status) &&
       ['Escaped', 'Caught', 'CashedOut', 'Exhausted'].includes(value.lastRun.status) &&
       text(value.lastRun.message) && text(value.lastRun.multiplier) &&
       text(value.lastRun.award))) &&
    (value.saveNotice === null || text(value.saveNotice));
}

export function parseHostMessage(input: unknown): HostEnvelope | null {
  let data: unknown = input;
  if (typeof input === 'string') {
    try { data = JSON.parse(input) as unknown; } catch { return null; }
  }
  if (!dictionary(data) || data.protocolVersion !== PROTOCOL_VERSION ||
    !text(data.requestId) || data.requestId.length > 64) return null;
  if (data.type === 'snapshot' && isSnapshot(data.payload)) {
    return {
      protocolVersion: 2, type: 'snapshot', requestId: data.requestId,
      payload: data.payload,
    };
  }
  if (data.type === 'error' && dictionary(data.payload) &&
    text(data.payload.message) && flag(data.payload.fatal) &&
    isSnapshot(data.payload.snapshot)) {
    return {
      protocolVersion: 2, type: 'error', requestId: data.requestId,
      payload: {
        message: data.payload.message, fatal: data.payload.fatal,
        snapshot: data.payload.snapshot,
      },
    };
  }
  return null;
}

let counter = 0;
function nextId(): string {
  // Unique across reopens; the C# journal stores the last committed mutation ID.
  return window.crypto?.randomUUID?.() ?? 'local-' + String(++counter);
}

export function sendCommand(type: 'client.ready' | 'snapshot.request' | 'ui.close' | 'ui.ready'): string {
  const requestId = nextId();
  window.chrome?.webview?.postMessage({ protocolVersion: 2, type, requestId });
  return requestId;
}

export function sendAction(type: GameAction, args: ActionArguments): string {
  const requestId = nextId();
  window.chrome?.webview?.postMessage({ protocolVersion: 2, type, requestId, ...args });
  return requestId;
}

export function subscribeToHost(handler: (message: HostEnvelope) => void): () => void {
  const webview = window.chrome?.webview;
  if (!webview) return () => {};
  const listener = (event: MessageEvent) => {
    const message = parseHostMessage(event.data);
    if (message) handler(message);
  };
  webview.addEventListener('message', listener);
  return () => webview.removeEventListener('message', listener);
}

// Never simulate paid rounds in an ordinary browser: the standalone dev build
// is only a visual preview until the real C# WebView2 host is connected.
export const browserPreview: ManorSnapshot = {
  bankroll: '—', bet: '—', selectedBet: '$25.00', cycleReturned: '—', payout: '—',
  cells: ['Rose', 'Watch', 'Raven', 'Queen', 'Wild', 'Portrait', 'Scatter', 'Lady', 'Ace',
    'Candelabrum', 'Key', 'Jack', 'Bonus', 'King', 'Rose'],
  winningCells: [], cycleActive: false, freeSpinsRemaining: 0,
  feature: 'Browser design preview · no wagers executed',
  animationsEnabled: true, hasCommittedSpin: false, revision: '0', canSpin: false,
  canFreeSpin: false, lastSpinFree: false, scatters: 0, doors: 0, winningLines: 0,
  run: null, practiceRun: null, lastRun: null,
  saveNotice: null, winningLineDetails: [],
  language: 'en',
  globalSoundEnabled: false, globalMusicEnabled: false, globalMusicVolume: 0,
  rules: {
    version: 2,
    symbols: [
      ['Jack', 'Victorian J', 14, 36, 90], ['Queen', 'Victorian Q', 18, 45, 112],
      ['King', 'Victorian K', 22, 54, 135], ['Ace', 'Victorian A', 27, 68, 180],
      ['Candelabrum', 'Candelabrum', 36, 90, 270], ['Watch', 'Pocket watch', 45, 112, 360],
      ['Raven', 'Raven', 68, 180, 540], ['Key', 'Antique key', 81, 225, 675],
      ['Portrait', 'Haunted portrait', 90, 270, 900], ['Rose', 'Black rose', 112, 338, 1125],
      ['Lady', 'Lady of the Manor', 180, 675, 2700], ['Wild', 'Haunted mirror / Wild', 0, 0, 0],
      ['Scatter', 'Manor moon / Scatter', 0, 0, 0], ['Bonus', 'Manor door / Bonus', 0, 0, 0],
    ].map(([symbol, name, three, four, five]) => ({
      symbol: String(symbol), name: String(name), three: Number(three),
      four: Number(four), five: Number(five),
    })),
    paylines: [
      '11111', '00000', '22222', '01210', '21012', '00100', '22122', '11011',
      '11211', '01110', '21112', '10001', '12221', '01010', '21212', '10101',
      '12121', '02020', '20202', '02120',
    ].map(line => [...line].map(Number)),
  },
};

export function availableActions(snapshot: ManorSnapshot): GameAction[] {
  if (snapshot.run) {
    return snapshot.run.hasCandle
      ? ['run.move', 'run.candle', 'run.cashout', 'run.follow', 'run.auto']
      : ['run.move', 'run.cashout', 'run.follow', 'run.auto'];
  }
  if (snapshot.practiceRun) {
    if (snapshot.practiceRun.status !== 'Hunting') return ['practice.restart', 'practice.exit'];
    return snapshot.practiceRun.hasCandle
      ? ['practice.move', 'practice.candle', 'practice.cashout',
        'practice.follow', 'practice.auto', 'practice.restart', 'practice.exit']
      : ['practice.move', 'practice.cashout', 'practice.follow',
        'practice.auto', 'practice.restart', 'practice.exit'];
  }
  if (snapshot.canFreeSpin) return ['spin.free'];
  if (snapshot.canSpin) return ['spin.paid', 'practice.start'];
  return [];
}
