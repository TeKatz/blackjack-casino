import type { GameAction, ManorSnapshot } from '../bridge/protocol';

/** Host responses are committed synchronously. The animation is not a financial lock:
 * this pure helper decides whether any result may enter visible UI before reel stop.
 */
export function isFreshOwnSpin(
  requestId: string, awaitingId: string | null, requestType: GameAction | null,
  previousRevision: string, incomingRevision: string,
): boolean {
  return awaitingId !== null &&
    (requestId === awaitingId || awaitingId === 'recovering') &&
    requestType !== null && requestType.startsWith('spin.') &&
    BigInt(incomingRevision) > BigInt(previousRevision);
}

/** The financial result, trigger counts, bonus and bankroll stay at the last
 * presented state until the five reels have actually stopped.
 */
export function visibleDuringSpin(
  alreadyShown: ManorSnapshot, latestCommitted: ManorSnapshot, awaiting: boolean,
): ManorSnapshot {
  return awaiting ? alreadyShown : latestCommitted;
}
