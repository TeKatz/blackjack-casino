/** Versioned read-only bridge contract for the first Midnight Manor migration slice. */
export const PROTOCOL_VERSION = 1 as const;

export type ManorSnapshot = {
  bankroll: string;
  bet: string;
  cycleReturned: string;
  payout: string;
  cells: string[];
  winningCells: number[];
  cycleActive: boolean;
  freeSpinsRemaining: number;
  feature: string;
  animationsEnabled: boolean;
  hasCommittedSpin: boolean;
};

export type ServerEnvelope =
  | { protocolVersion: 1; type: 'snapshot'; requestId: string; payload: ManorSnapshot }
  | { protocolVersion: 1; type: 'error'; requestId: string; payload: { message: string } };

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

export function isSnapshot(value: unknown): value is ManorSnapshot {
  if (typeof value !== 'object' || value === null) return false;
  const s = value as Record<string, unknown>;
  return (
    ['bankroll', 'bet', 'cycleReturned', 'payout', 'feature'].every(k => typeof s[k] === 'string') &&
    Array.isArray(s.cells) && s.cells.length === 15 && s.cells.every(x => typeof x === 'string') &&
    Array.isArray(s.winningCells) && s.winningCells.every(x => Number.isInteger(x) && x >= 0 && x < 15) &&
    typeof s.cycleActive === 'boolean' &&
    typeof s.hasCommittedSpin === 'boolean' &&
    typeof s.animationsEnabled === 'boolean' &&
    Number.isInteger(s.freeSpinsRemaining) && Number(s.freeSpinsRemaining) >= 0
  );
}

export function parseServerMessage(input: unknown): ServerEnvelope | null {
  let value: unknown = input;
  if (typeof input === 'string') {
    try { value = JSON.parse(input) as unknown; } catch { return null; }
  }
  if (typeof value !== 'object' || value === null) return null;
  const record = value as Record<string, unknown>;
  if (record.protocolVersion !== PROTOCOL_VERSION || typeof record.requestId !== 'string') return null;
  if (record.type === 'snapshot' && isSnapshot(record.payload)) {
    return { protocolVersion: 1, type: 'snapshot', requestId: record.requestId, payload: record.payload };
  }
  if (record.type === 'error' &&
      typeof record.payload === 'object' && record.payload !== null &&
      typeof (record.payload as Record<string, unknown>).message === 'string') {
    return { protocolVersion: 1, type: 'error', requestId: record.requestId, payload: { message: (record.payload as { message: string }).message } };
  }
  return null;
}

let counter = 0;
export function sendCommand(type: 'client.ready' | 'snapshot.request' | 'ui.close'): void {
  window.chrome?.webview?.postMessage({
    protocolVersion: PROTOCOL_VERSION, type, requestId: String(++counter),
  });
}

export function subscribeToHost(handler: (message: ServerEnvelope) => void): () => void {
  const webview = window.chrome?.webview;
  if (!webview) return () => {};
  const listener = (event: MessageEvent) => {
    const message = parseServerMessage(event.data);
    if (message) handler(message);
  };
  webview.addEventListener('message', listener);
  return () => webview.removeEventListener('message', listener);
}

export const browserPreview: ManorSnapshot = {
  bankroll: '—', bet: '—', cycleReturned: '—', payout: '—',
  cells: [
    'Rose','Watch','Raven', 'Queen','Wild','Portrait', 'Scatter','Lady','Ace',
    'Candelabrum','Key','Jack', 'Bonus','King','Rose',
  ],
  winningCells: [], cycleActive: false, freeSpinsRemaining: 0,
  feature: 'Browser design preview · no game actions',
  animationsEnabled: true, hasCommittedSpin: false,
};
