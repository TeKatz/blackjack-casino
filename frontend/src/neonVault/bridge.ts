// The host owns outcomes, balance, bonuses, payouts and XP. This module accepts display-only snapshots.
export const symbols=['Pulse','Prism','Chip','Crown','Key','Wild','Scatter','VaultKey'] as const;
export type NeonSymbol=typeof symbols[number];
export type Blackout='None'|'Blackout'|'Overload'|'Total';
export type Jackpot='None'|'Mini'|'Minor'|'Major'|'Grand';
export type NearMiss='None'|'Scatter'|'Jackpot'|'Blackout';
export type Tier='Standard'|'HighRoller'|'VaultClass'|'MaxExposure';
export type NeonSpin={
  id:string;bet:string;stake:string;bankrollBefore:string;bankrollAfter:string;
  baseGrid:NeonSymbol[];finalGrid:NeonSymbol[];
  blackout:Blackout;blackoutWildCells:number[];blackoutFullReel:number|null;
  expandedReels:number[];securityOverrideReel:number|null;
  nearMiss:NearMiss;jackpot:Jackpot;
  waysPayout:string;scatterPayout:string;jackpotPayout:string;
  payout:string;net:string;waysWon:number;freeSpinsAwarded:number;freeSpinsRemaining:number;
  vaultEnergyAfter:number;isFreeSpin:boolean;betTier:Tier;totalMultiplier:string;
  playerXp:number;seasonXp:number;levelUp:boolean;
};
export type NeonState={
  language:'de'|'en';bankroll:string;bet:string;canSpin:boolean;bonusActive:boolean;
  freeSpinsRemaining:number;vaultEnergy:number;energyTarget:number;betTier:Tier;
  soundEnabled:boolean;animationsEnabled:boolean;bets:string[];
  paytable:{id:NeonSymbol;name:string;weight:number;threeReels:string;fourReels:string;fiveReels:string}[];
  jackpots:{id:Exclude<Jackpot,'None'>;multiplier:number;amount:string}[];
  profile:{level:number;prestige:number;coins:number};
  stats:{spins:number;wins:number;paidSpins:number;freeSpins:number;
    freeSpinSessions:number;freeSpinsAwarded:number;jackpots:number;grandJackpots:number;
    blackouts:number;totalBlackouts:number;securityOverrides:number;
    netProfit:string;biggestPayout:string;bestPayoutMultiplier:string};
  lastSpin:NeonSpin|null;
};
export type NeonSnapshot={version:1;session:string;revision:number;state:NeonState};
export type NeonReceipt={requestId:string;success:boolean;message:string;revision:number};
export type NeonIntent={action:'spin'|'bet.set'|'sound.set'|'animations.set';value?:string};
const obj=(v:unknown):v is Record<string,unknown>=>v!==null&&typeof v==='object'&&!Array.isArray(v);
const integer=(v:unknown):v is number=>typeof v==='number'&&Number.isSafeInteger(v)&&v>=0;
const money=(v:unknown):v is string=>typeof v==='string'&&/^(?:0|[1-9]\d*)(?:\.\d{1,28})?$/.test(v);
const signed=(v:unknown):v is string=>typeof v==='string'&&/^-?(?:0|[1-9]\d*)(?:\.\d{1,28})?$/.test(v);
const guid=(v:unknown):v is string=>typeof v==='string'&&/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(v);
const pick=<T extends string>(v:unknown,set:readonly T[]):v is T=>typeof v==='string'&&set.includes(v as T);
const sy=(v:unknown):v is NeonSymbol=>pick(v,symbols);
const blackouts=['None','Blackout','Overload','Total'] as const;
const jackpots=['None','Mini','Minor','Major','Grand'] as const;
const misses=['None','Scatter','Jackpot','Blackout'] as const;
const tiers=['Standard','HighRoller','VaultClass','MaxExposure'] as const;
const indexes=(v:unknown,max:number):v is number[]=>Array.isArray(v)
  &&v.every(x=>integer(x)&&x<max)&&new Set(v).size===v.length;
const grid=(v:unknown):v is NeonSymbol[]=>Array.isArray(v)&&v.length===20&&v.every(sy);
export function isNeonSpin(v:unknown):v is NeonSpin{
 if(!obj(v)||!guid(v.id)||!money(v.bet)||!money(v.stake)||!money(v.bankrollBefore)
  ||!money(v.bankrollAfter)||!grid(v.baseGrid)||!grid(v.finalGrid)
  ||!pick(v.blackout,blackouts)||!indexes(v.blackoutWildCells,20)
  ||!(v.blackoutFullReel===null||(integer(v.blackoutFullReel)&&v.blackoutFullReel<5))
  ||!indexes(v.expandedReels,5)
  ||!(v.securityOverrideReel===null||(integer(v.securityOverrideReel)&&v.securityOverrideReel<5))
  ||!pick(v.nearMiss,misses)||!pick(v.jackpot,jackpots)||!pick(v.betTier,tiers)
  ||!money(v.waysPayout)||!money(v.scatterPayout)||!money(v.jackpotPayout)
  ||!money(v.payout)||!signed(v.net)||!money(v.totalMultiplier)
  ||!integer(v.waysWon)||!integer(v.freeSpinsAwarded)||!integer(v.freeSpinsRemaining)
  ||!integer(v.vaultEnergyAfter)||typeof v.isFreeSpin!=='boolean'
  ||!integer(v.playerXp)||!integer(v.seasonXp)||typeof v.levelUp!=='boolean')return false;
 if(v.blackoutWildCells.some(index=>(v.finalGrid as NeonSymbol[])[index]!=='Wild'))return false;
 if(v.expandedReels.some(reel=>[0,1,2,3].some(row=>{
   const symbol=(v.finalGrid as NeonSymbol[])[row*5+reel];
   return symbol!=='Wild'&&symbol!=='Scatter'&&symbol!=='VaultKey';
 })))return false;
 return true;
}
export function isNeonSnapshot(v:unknown):v is NeonSnapshot{
 if(!obj(v)||v.version!==1||!guid(v.session)||!integer(v.revision)||!obj(v.state))return false;
 const s=v.state;
 if(s.language!=='en'&&s.language!=='de')return false;
 if(!money(s.bankroll)||!money(s.bet)||typeof s.canSpin!=='boolean'
   ||typeof s.bonusActive!=='boolean'||!integer(s.freeSpinsRemaining)
   ||s.bonusActive!==(s.freeSpinsRemaining>0)||!integer(s.vaultEnergy)
   ||s.energyTarget!==12||!pick(s.betTier,tiers)
   ||typeof s.soundEnabled!=='boolean'||typeof s.animationsEnabled!=='boolean'
   ||!Array.isArray(s.bets)||s.bets.length!==16||!s.bets.every(money)
   ||!Array.isArray(s.paytable)||s.paytable.length!==8
   ||!s.paytable.every((x:unknown)=>obj(x)&&sy(x.id)&&typeof x.name==='string'
      &&x.name.length<=80&&integer(x.weight)&&money(x.threeReels)
      &&money(x.fourReels)&&money(x.fiveReels))
   ||!Array.isArray(s.jackpots)||s.jackpots.length!==4
   ||!s.jackpots.every((x:unknown)=>obj(x)&&pick(x.id,jackpots.slice(1))
      &&integer(x.multiplier)&&money(x.amount))
   ||!obj(s.profile)||!integer(s.profile.level)||!integer(s.profile.prestige)
   ||!integer(s.profile.coins)||!obj(s.stats))return false;
 const st=s.stats;
 if(!integer(st.spins)||!integer(st.wins)||!integer(st.paidSpins)||!integer(st.freeSpins)
   ||!integer(st.freeSpinSessions)||!integer(st.freeSpinsAwarded)||!integer(st.jackpots)
   ||!integer(st.grandJackpots)||!integer(st.blackouts)||!integer(st.totalBlackouts)
   ||!integer(st.securityOverrides)||!signed(st.netProfit)||!money(st.biggestPayout)
   ||!money(st.bestPayoutMultiplier))return false;
 return s.lastSpin===null||isNeonSpin(s.lastSpin);
}
export function isNeonReceipt(v:unknown):v is NeonReceipt{
 return obj(v)&&guid(v.requestId)&&typeof v.success==='boolean'
   &&typeof v.message==='string'&&v.message.length<=2048&&integer(v.revision);
}
export function sendNeonIntent(snapshot:NeonSnapshot,intent:NeonIntent):string{
 const requestId=crypto.randomUUID();
 window.chrome?.webview?.postMessage({type:'neon-vault.command',version:1,requestId,
   session:snapshot.session,expectedRevision:snapshot.revision,...intent});
 return requestId;
}
export function listenNeon(receive:(message:{type:'snapshot';data:NeonSnapshot}|
 {type:'receipt';data:NeonReceipt})=>void):()=>void{
 const host=window.chrome?.webview;
 const handler=(event:MessageEvent)=>{
  const message:unknown=event.data;
  if(!obj(message))return;
  if(message.type==='neon-vault.snapshot'&&isNeonSnapshot(message.data))
    receive({type:'snapshot',data:message.data});
  if(message.type==='neon-vault.result'&&isNeonReceipt(message.data))
    receive({type:'receipt',data:message.data});
 };
 host?.addEventListener('message',handler);
 return()=>host?.removeEventListener('message',handler);
}
