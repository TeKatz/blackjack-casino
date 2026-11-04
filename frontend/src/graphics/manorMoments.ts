import type { GameAction, ManorSnapshot } from '../bridge/protocol';
import type { ManorCue } from '../audio/manorAudio';
import { localizeFeature, localizeRoom, localizeRunText, t } from '../i18n';
import type { ManorLanguage } from '../i18n';

export type ManorMoment = {
  key: 'jackpot' | 'big' | 'win' | 'door' | 'moon' | 'run';
  heading: string;
  detail: string;
  secondary?: string;
};

function readDollarAmount(amount: string): number {
  const digits = amount.replace(/[$,\s]/g, '');
  if (!/^\d+(?:\.\d{1,2})?$/.test(digits)) return 0;
  const value = Number(digits);
  return Number.isFinite(value) && value > 0 ? value : 0;
}

/** Thresholds are *presentation-only*; payout amounts always come from C#. */
export function manorWinTier(payout: string, stake: string): 'none' | 'win' | 'big' | 'jackpot' {
  const amount = readDollarAmount(payout);
  if (amount === 0) return 'none';
  const bet = readDollarAmount(stake);
  if (bet > 0 && amount >= bet * 100) return 'jackpot';
  if (bet > 0 && amount >= bet * 25) return 'big';
  return 'win';
}

/** Multiple simultaneous door + moon rewards remain visible, without simulating bonuses. */
export function spinMoment(snapshot: ManorSnapshot): ManorMoment | null {
  if (!snapshot.hasCommittedSpin) return null;
  const openedDoors = snapshot.doors >= 2;
  const newNight = snapshot.scatters >= 3 && !snapshot.lastSpinFree;
  const detail = openedDoors ? 'Escape the Manor has opened' :
    newNight ? 'Haunted Night free spins unlocked' : '';
  const secondary = openedDoors && newNight ? 'Haunted Night free spins also unlocked' : undefined;
  if (openedDoors) return { key: 'door', heading: 'THE MANOR OPENS', detail, secondary };
  if (newNight) return {
    key: 'moon', heading: 'HAUNTED NIGHT', detail,
    secondary: snapshot.freeSpinsRemaining > 0
      ? snapshot.freeSpinsRemaining + ' saved free spins' : undefined,
  };
  const tier = manorWinTier(snapshot.payout, snapshot.bet);
  if (tier === 'none') return null;
  return {
    key: tier,
    heading: tier === 'jackpot' ? 'LEGENDARY WIN' : tier === 'big' ? 'BIG WIN' : 'WIN',
    detail: snapshot.payout + ' · ' + snapshot.winningLines + ' winning ' +
      (snapshot.winningLines === 1 ? 'line' : 'lines'),
  };
}

export function spinCue(snapshot: ManorSnapshot): ManorCue | null {
  if (snapshot.doors >= 2) return 'door';
  if (snapshot.scatters >= 3 && !snapshot.lastSpinFree) return 'moon';
  const tier = manorWinTier(snapshot.payout, snapshot.bet);
  return tier === 'jackpot' ? 'epicWin' : tier === 'big' ? 'bigWin' :
    tier === 'win' ? 'smallWin' : null;
}

/** Only called for an acknowledged, newer C# revision after a run action. */
export function runMoment(snapshot: ManorSnapshot, action: GameAction): ManorMoment {
  const active = snapshot.run;
  const room = active?.rooms.find(item => item.id === active.playerRoom)?.name;
  if (!active) {
    return {
      key: 'run', heading: action === 'run.cashout' ? 'REWARD SECURED' : 'THE RUN ENDS',
      detail: snapshot.feature,
    };
  }
  return {
    key: 'run',
    heading: action === 'run.candle' ? 'CANDLE LIT' :
      room ? room.toUpperCase() : 'A NEW CHAMBER',
    detail: active.message,
    secondary: active.ghostClue,
  };
}

export function runCue(snapshot: ManorSnapshot, action: GameAction): ManorCue {
  if (action === 'run.candle') return 'candle';
  if (action === 'run.cashout' || (action === 'run.auto' && !snapshot.run)) return 'exit';
  if (snapshot.run?.message.includes('Checkpoint secured')) return 'checkpoint';
  if (snapshot.run?.ghostClue.includes('CLOSE')) return 'threat';
  return 'footstep';
}

/** Translate a presentation event at render time, never replaying its audio. */
export function localizeMoment(moment: ManorMoment, language: ManorLanguage): ManorMoment {
  if (language === 'en') return moment;
  const headings: Record<string, string> = {
    WIN: t(language, 'momentWin'),
    'BIG WIN': t(language, 'momentBig'),
    'LEGENDARY WIN': t(language, 'momentEpic'),
    'THE MANOR OPENS': t(language, 'momentDoor'),
    'HAUNTED NIGHT': t(language, 'momentMoon'),
    'A NEW CHAMBER': t(language, 'momentRun'),
    'CANDLE LIT': t(language, 'momentCandle'),
    'REWARD SECURED': t(language, 'momentCashout'),
    'THE RUN ENDS': t(language, 'momentRunEnd'),
  };
  const name = moment.heading.toLowerCase().replace(/\b[a-z]/g, char => char.toUpperCase());
  const translated = localizeRoom(name, language);
  const heading = headings[moment.heading] ??
    (translated !== name ? translated.toUpperCase() : moment.heading);
  let detail = localizeRunText(moment.detail, language);
  if (detail === moment.detail) detail = localizeFeature(detail, language);
  const win = /^(.*) · (\d+) winning (line|lines)$/.exec(detail);
  if (win) detail = t(language, 'winDetail', win[1], win[2],
    t(language, win[3] === 'line' ? 'lineSingular' : 'linePlural'));
  if (detail === t('en', 'doorDetail')) detail = t(language, 'doorDetail');
  if (detail === t('en', 'moonDetail')) detail = t(language, 'moonDetail');
  let secondary = moment.secondary ? localizeRunText(moment.secondary, language) : undefined;
  if (secondary === t('en', 'comboDetail')) secondary = t(language, 'comboDetail');
  if (secondary) {
    const saved = /^(\d+) saved free spins$/.exec(secondary);
    if (saved) secondary = t(language, 'savedSpins', saved[1]);
  }
  return { ...moment, heading, detail, secondary };
}
