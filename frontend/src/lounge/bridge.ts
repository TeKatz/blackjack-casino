export const loungeIds = ['sudoku','solitaire','word-search','memory','jigsaw','tales-backroom'] as const;
export type LoungeId = typeof loungeIds[number];
export type LoungeGame = { id: LoungeId; status: string | null };
import { isSudokuState } from './sudokuBridge';
import { isSolitaireState } from './solitaireBridge';
import { isWordSearchState } from './wordSearchBridge';
import type { SudokuState } from './sudokuBridge';
import type { SolitaireState } from './solitaireBridge';
import type { WordSearchState } from './wordSearchBridge';
export type LoungeSnapshot = { version: 1; language: 'en' | 'de';
  session:string;revision:number;sudoku:SudokuState|null;
  solitaireSession:string;solitaireRevision:number;solitaire:SolitaireState|null;
  wordSearchSession:string;wordSearchRevision:number;wordSearch:WordSearchState|null;
  musicEnabled: boolean; soundEnabled: boolean; animationsEnabled: boolean; games: LoungeGame[] };
const record = (v: unknown): v is Record<string, unknown> => v !== null && typeof v === 'object' && !Array.isArray(v);
export const isLoungeId = (value: unknown): value is LoungeId =>
  typeof value === 'string' && loungeIds.some(id => id === value);
export function isLoungeSnapshot(value: unknown): value is LoungeSnapshot {
  if (!record(value) || value.version !== 1 || (value.language !== 'de' && value.language !== 'en')
    || typeof value.session!=='string' || !/^[\da-f]{8}-[\da-f]{4}-[\da-f]{4}-[\da-f]{4}-[\da-f]{12}$/i.test(value.session)
    || typeof value.revision!=='number'||!Number.isSafeInteger(value.revision)||value.revision<0
    || !(value.sudoku===null||isSudokuState(value.sudoku))
    || typeof value.solitaireSession!=='string'||!/^([\da-f]{8}-[\da-f]{4}-[\da-f]{4}-[\da-f]{4}-[\da-f]{12})$/i.test(value.solitaireSession)
    || typeof value.solitaireRevision!=='number'||!Number.isSafeInteger(value.solitaireRevision)||value.solitaireRevision<0
    || !(value.solitaire===null||isSolitaireState(value.solitaire))
    || typeof value.wordSearchSession!=='string'||!/^([\da-f]{8}-[\da-f]{4}-[\da-f]{4}-[\da-f]{4}-[\da-f]{12})$/i.test(value.wordSearchSession)
    || typeof value.wordSearchRevision!=='number'||!Number.isSafeInteger(value.wordSearchRevision)||value.wordSearchRevision<0
    || !(value.wordSearch===null||isWordSearchState(value.wordSearch))
    || typeof value.musicEnabled !== 'boolean' || typeof value.soundEnabled !== 'boolean'
    || typeof value.animationsEnabled !== 'boolean' || !Array.isArray(value.games)
    || value.games.length !== loungeIds.length) return false;
  const ids = new Set<string>();
  for (const game of value.games) {
    if (!record(game) || !isLoungeId(game.id) || ids.has(game.id)
      || !(game.status === null || (typeof game.status === 'string' && game.status.length <= 240))) return false;
    ids.add(game.id);
  }
  return loungeIds.every(id => ids.has(id));
}
export function readLoungeSnapshot(message: unknown): LoungeSnapshot | null {
  return record(message) && message.type === 'lounge.snapshot' && isLoungeSnapshot(message.data)
    ? message.data : null;
}
export function sendLoungeCommand(type: 'lounge.ready' | 'lounge.ui-ready' | 'lounge.close'): void;
export function sendLoungeCommand(type: 'lounge.open', id: LoungeId): void;
export function sendLoungeCommand(type: string, id?: LoungeId): void {
  const webview = window.chrome?.webview;
  if (!webview) return;
  if (type === 'lounge.open' && !isLoungeId(id)) return;
  if (type !== 'lounge.open' && !['lounge.ready','lounge.ui-ready','lounge.close'].includes(type)) return;
  webview.postMessage(id ? { type, version: 1, id } : { type, version: 1 });
}
