// The host sends fully committed results; no wager, randomness or XP lives here.
export const reelSymbols=['Cherry','Lemon','Bell','Bar','Seven'] as const;
export type ReelSymbol=typeof reelSymbols[number];
export type ReelsSpin={id:string;bet:string;bankrollBefore:string;bankrollAfter:string;
  payout:string;net:string;multiplier:number;stops:number[];symbols:ReelSymbol[];
  playerXp:number;seasonXp:number;levelUp:boolean};
export type ReelsState={
  language:'en'|'de';bankroll:string;bet:string;canSpin:boolean;
  soundEnabled:boolean;animationsEnabled:boolean;
  reelStrip:ReelSymbol[];
  paytable:{id:ReelSymbol;name:string;stops:number;tripleMultiplier:number}[];
  profile:{level:number;prestige:number;coins:number};
  stats:{spins:number;wins:number;pushes:number;netProfit:string;biggestPayout:string};
  lastSpin:ReelsSpin|null;
};
export type ReelsSnapshot={version:1;session:string;revision:number;state:ReelsState};
export type ReelsReply={requestId:string;success:boolean;message:string;revision:number};
export type ReelsIntent={action:'spin'|'bet.set'|'sound.set'|'animations.set';value?:string};

const record=(v:unknown):v is Record<string,unknown>=>v!==null&&typeof v==='object'&&!Array.isArray(v);
const integer=(v:unknown):v is number=>typeof v==='number'&&Number.isSafeInteger(v)&&v>=0;
const money=(v:unknown):v is string=>typeof v==='string'&&/^(?:0|[1-9]\d*)(?:\.\d{1,2})?$/.test(v);
const signed=(v:unknown):v is string=>typeof v==='string'&&/^-?(?:0|[1-9]\d*)(?:\.\d{1,2})?$/.test(v);
const symbol=(v:unknown):v is ReelSymbol=>reelSymbols.some(s=>s===v);
const guid=(v:unknown):v is string=>typeof v==='string'&&/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(v);

export function isReelsSpin(v:unknown):v is ReelsSpin{
  if(!record(v))return false;
  if(!guid(v.id)||!money(v.bet)||!money(v.bankrollBefore)||!money(v.bankrollAfter)
    ||!money(v.payout)||!signed(v.net)||!integer(v.multiplier)
    ||!integer(v.playerXp)||!integer(v.seasonXp)||typeof v.levelUp!=='boolean'
    ||!Array.isArray(v.stops)||v.stops.length!==3
    ||!v.stops.every((n:unknown)=>integer(n)&&n<32)
    ||!Array.isArray(v.symbols)||v.symbols.length!==3||!v.symbols.every(symbol))return false;
  return true;
}
export function isReelsSnapshot(v:unknown):v is ReelsSnapshot{
  if(!record(v)||v.version!==1||!guid(v.session)||!integer(v.revision)||!record(v.state))return false;
  const s=v.state;
  if(s.language!=='en'&&s.language!=='de')return false;
  if(!money(s.bankroll)||!money(s.bet)||typeof s.canSpin!=='boolean'
    ||typeof s.soundEnabled!=='boolean'||typeof s.animationsEnabled!=='boolean'
    ||!Array.isArray(s.reelStrip)||s.reelStrip.length!==32||!s.reelStrip.every(symbol)
    ||!Array.isArray(s.paytable)||s.paytable.length!==5
    ||!s.paytable.every((x:unknown)=>record(x)&&symbol(x.id)&&typeof x.name==='string'
       &&integer(x.stops)&&integer(x.tripleMultiplier))
    ||!record(s.profile)||!integer(s.profile.level)||!integer(s.profile.prestige)
    ||!integer(s.profile.coins)||!record(s.stats)||!integer(s.stats.spins)
    ||!integer(s.stats.wins)||!integer(s.stats.pushes)||!signed(s.stats.netProfit)
    ||!money(s.stats.biggestPayout))return false;
  if(s.lastSpin!==null&&!isReelsSpin(s.lastSpin))return false;
  // A browser snapshot must never depict different symbols at the given stops.
  if(s.lastSpin!==null){
    const spin=s.lastSpin as ReelsSpin;
    if(spin.stops.some((stop,i)=>s.reelStrip[stop]!==spin.symbols[i]))return false;
  }
  return true;
}
export function isReelsReply(v:unknown):v is ReelsReply{
  return record(v)&&guid(v.requestId)&&typeof v.success==='boolean'
    &&typeof v.message==='string'&&v.message.length<=2048&&integer(v.revision);
}
export function sendReelsIntent(snapshot:ReelsSnapshot,intent:ReelsIntent):string{
  const requestId=crypto.randomUUID();
  window.chrome?.webview?.postMessage({type:'reels.command',version:1,
    session:snapshot.session,expectedRevision:snapshot.revision,requestId,...intent});
  return requestId;
}
export function listenReels(receive:(message:{type:'snapshot';data:ReelsSnapshot}|
  {type:'result';data:ReelsReply})=>void):()=>void{
  const host=window.chrome?.webview;
  const handler=(event:MessageEvent)=>{
    const payload:unknown=event.data;
    if(!record(payload))return;
    if(payload.type==='reels.snapshot'&&isReelsSnapshot(payload.data))
      receive({type:'snapshot',data:payload.data});
    if(payload.type==='reels.result'&&isReelsReply(payload.data))
      receive({type:'result',data:payload.data});
  };
  host?.addEventListener('message',handler);
  return()=>host?.removeEventListener('message',handler);
}
