import type {NeonSpin} from './bridge';

/** Presentation is derived from the host receipt. It never invents a bonus or payout. */
export type NeonImpactKind=
  'idle'|'blackout'|'overload'|'total-blackout'|'override'|'jackpot'|
  'mega-win'|'big-win'|'free-spins'|'wild';
export type NeonWinTier='none'|'big'|'mega';
export type NeonImpact={
  id:string;kind:NeonImpactKind;winTier:NeonWinTier;
  strength:1|2|3;multiplier:number;alarm:boolean;payout:string;
};
const ratio=(value:string):number=>{
  const parsed=Number(value);
  return Number.isFinite(parsed)&&parsed>0?parsed:0;
};
export function neonImpact(spin:NeonSpin):NeonImpact{
  // Native totalMultiplier is already settled in C#. Thresholds are visual only.
  const multiplier=ratio(spin.totalMultiplier);
  const winTier:NeonWinTier=spin.jackpot==='Grand'||multiplier>=50?'mega'
    :spin.jackpot!=='None'||multiplier>=10?'big':'none';
  const kind:NeonImpactKind=spin.blackout==='Total'?'total-blackout'
    :spin.blackout==='Overload'?'overload'
    :spin.blackout==='Blackout'?'blackout'
    :spin.securityOverrideReel!==null?'override'
    :spin.jackpot!=='None'?'jackpot'
    :winTier==='mega'?'mega-win'
    :winTier==='big'?'big-win'
    :spin.freeSpinsAwarded>0?'free-spins'
    :spin.expandedReels.length>0?'wild':'idle';
  const alarm=kind==='blackout'||kind==='overload'||kind==='total-blackout';
  const strength:1|2|3=kind==='total-blackout'||winTier==='mega'?3
    :kind==='overload'||kind==='jackpot'||winTier==='big'?2:1;
  return {id:spin.id,kind,winTier,strength,multiplier,alarm,payout:spin.payout};
}
export const isImpactful=(impact:NeonImpact):boolean=>impact.kind!=='idle';
export function impactColor(kind:NeonImpactKind):number{
  switch(kind){
    case 'total-blackout': return 0xff475f;
    case 'overload': return 0xff5aa8;
    case 'blackout': return 0xf73c7a;
    case 'override': return 0xffc870;
    case 'jackpot': return 0xffdf84;
    case 'mega-win': return 0xffd477;
    case 'big-win': return 0x77ffe5;
    case 'free-spins': return 0xb4a6ff;
    case 'wild': return 0xf77cf0;
    default: return 0x42eaff;
  }
}
