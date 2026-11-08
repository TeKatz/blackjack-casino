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
// C# decimal values loaded from older saves can contain fractional cents.
// These are read-only display amounts; the host remains authoritative for
 // wager validation and settlement. Do not reject a valid saved balance.
const money=(v:unknown):v is string=>typeof v==='string'&&/^(?:0|[1-9]\d*)(?:\.\d{1,28})?$/.test(v);
const signed=(v:unknown):v is string=>typeof v==='string'&&/^-?(?:0|[1-9]\d*)(?:\.\d{1,28})?$/.test(v);
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
// Return only the offending field path, never a wallet amount or player data.
// A rejected snapshot must remain non-playable, but the recovery screen can
 // now identify precisely which native/browser contract field needs repair.
export function reelsSnapshotIssue(v:unknown):string|null{
  if(!record(v))return 'envelope';
  if(v.version!==1)return 'version';
  if(!guid(v.session))return 'session';
  if(!integer(v.revision))return 'revision';
  if(!record(v.state))return 'state';
  const s=v.state;
  if(s.language!=='en'&&s.language!=='de')return 'state.language';
  if(!money(s.bankroll))return 'state.bankroll';
  if(!money(s.bet))return 'state.bet';
  if(typeof s.canSpin!=='boolean')return 'state.canSpin';
  if(typeof s.soundEnabled!=='boolean')return 'state.soundEnabled';
  if(typeof s.animationsEnabled!=='boolean')return 'state.animationsEnabled';
  if(!Array.isArray(s.reelStrip)||s.reelStrip.length!==32||!s.reelStrip.every(symbol))
    return 'state.reelStrip';
  if(!Array.isArray(s.paytable)||s.paytable.length!==5
    ||!s.paytable.every((x:unknown)=>record(x)&&symbol(x.id)&&typeof x.name==='string'
       &&integer(x.stops)&&integer(x.tripleMultiplier)))return 'state.paytable';
  if(!record(s.profile))return 'state.profile';
  if(!integer(s.profile.level))return 'state.profile.level';
  if(!integer(s.profile.prestige))return 'state.profile.prestige';
  if(!integer(s.profile.coins))return 'state.profile.coins';
  if(!record(s.stats))return 'state.stats';
  if(!integer(s.stats.spins))return 'state.stats.spins';
  if(!integer(s.stats.wins))return 'state.stats.wins';
  if(!integer(s.stats.pushes))return 'state.stats.pushes';
  if(!signed(s.stats.netProfit))return 'state.stats.netProfit';
  if(!money(s.stats.biggestPayout))return 'state.stats.biggestPayout';
  if(s.lastSpin!==null){
    if(!isReelsSpin(s.lastSpin))return 'state.lastSpin';
    const spin=s.lastSpin as ReelsSpin;
    const strip=s.reelStrip as ReelSymbol[];
    if(spin.stops.some((stop,i)=>strip[stop]!==spin.symbols[i]))
      return 'state.lastSpin.symbols';
  }
  return null;
}
export function isReelsSnapshot(v:unknown):v is ReelsSnapshot{
  return reelsSnapshotIssue(v)===null;
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
    if(payload.type==='reels.snapshot'){
      if(isReelsSnapshot(payload.data))receive({type:'snapshot',data:payload.data});
      else host?.postMessage({type:'reels.snapshot-invalid',field:reelsSnapshotIssue(payload.data)});
    }
    if(payload.type==='reels.result'&&isReelsReply(payload.data))
      receive({type:'result',data:payload.data});
  };
  host?.addEventListener('message',handler);
  return()=>host?.removeEventListener('message',handler);
}
