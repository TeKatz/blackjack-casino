import type { ManorSnapshot } from '../bridge/protocol';

export const BET_PRESETS = [50, 100, 250, 1000, 2500, 5000] as const;
export const AUTO_PRESETS = [10, 25, 50, 100] as const;
export const MANOR_MAX_BET = 5000;

export function validManorBet(bet: string): boolean {
  return /^[1-9][0-9]{0,3}$/.test(bet) && Number(bet) <= MANOR_MAX_BET;
}

export function canAffordBet(bankroll: string, bet: string): boolean {
  if (!validManorBet(bet)) return false;
  const amount = Number(bankroll.replace(/[$,\s]/g, ''));
  return Number.isFinite(amount) && amount >= Number(bet);
}

/** Automatic spins never skip a pending bonus, use free spins, or issue an
 * extra wager when the saved bankroll can no longer cover the selected bet. */
export function shouldContinueAuto(snapshot: ManorSnapshot, bet: string,
  remaining: number): boolean {
  return remaining > 0 && snapshot.canSpin && !snapshot.cycleActive &&
    !snapshot.run && !snapshot.canFreeSpin && canAffordBet(snapshot.bankroll, bet);
}
