// Native C# is solely responsible for RNG, wagers, payouts, rewards and persistence.
export const symbols=['Elephant','Ruby','Necklace','Vase','Spices','Ace','King','Queen','Jack','Ten','Empty'] as const;
export type SymbolId=typeof symbols[number];
export type RubySpin={
 id:string;bet:string;bankrollBefore:string;bankrollAfter:string;
 baseGrid:SymbolId[];finalGrid:SymbolId[];heldCells:number[];winningLines:number[];
 elephantBonus:boolean;basePayout:string;bonusPayout:string;payout:string;net:string;
 playerXp:number;seasonXp:number;levelUp:boolean;
};
export type RubyState={
 language:'de'|'en';bankroll:string;bet:string;canSpin:boolean;soundEnabled:boolean;
 animationsEnabled:boolean;bets:string[];paylines:number[][];
 profile:{level:number;prestige:number;coins:number};
 stats:{spins:number;wins:number;netProfit:string;biggestPayout:string};
 lastSpin:RubySpin|null;
};
export type RubySnapshot={version:1;session:string;revision:number;state:RubyState};
export type RubyReceipt={requestId:string;success:boolean;message:string;revision:number};
export type RubyIntent={action:'spin'|'bet.set'|'sound.set'|'animations.set';value?:string};

const obj=(x:unknown):x is Record<string,unknown>=>typeof x==='object'&&x!==null&&!Array.isArray(x);
const integer=(x:unknown):x is number=>typeof x==='number'&&Number.isSafeInteger(x)&&x>=0;
const money=(x:unknown):x is string=>typeof x==='string'&&/^(?:0|[1-9]\d*)(?:\.\d{1,28})?$/.test(x);
const signed=(x:unknown):x is string=>typeof x==='string'&&/^-?(?:0|[1-9]\d*)(?:\.\d{1,28})?$/.test(x);
const guid=(x:unknown):x is string=>typeof x==='string'&&/^[a-f\d]{8}-[a-f\d]{4}-[a-f\d]{4}-[a-f\d]{4}-[a-f\d]{12}$/i.test(x);
const symbol=(x:unknown):x is SymbolId=>symbols.some(s=>s===x);
const grid=(x:unknown):x is SymbolId[]=>Array.isArray(x)&&x.length===15&&x.every(symbol);
const indices=(x:unknown,max:number):x is number[]=>Array.isArray(x)&&x.every(n=>integer(n)&&n<max)&&new Set(x).size===x.length;

export function isRubySpin(x:unknown):x is RubySpin {
 if(!obj(x)||!guid(x.id)||!money(x.bet)||!money(x.bankrollBefore)||!money(x.bankrollAfter)
  ||!money(x.basePayout)||!money(x.bonusPayout)||!money(x.payout)||!signed(x.net)
  ||!grid(x.baseGrid)||!grid(x.finalGrid)||!indices(x.heldCells,15)||!indices(x.winningLines,10)
  ||typeof x.elephantBonus!=='boolean'||!integer(x.playerXp)||!integer(x.seasonXp)
  ||typeof x.levelUp!=='boolean')return false;
 if(x.elephantBonus!==(x.baseGrid.filter(s=>s==='Elephant').length>=2))return false;
 if(!x.elephantBonus&&x.heldCells.length!==0)return false;
 if(x.heldCells.some(i=>x.baseGrid[i]!==x.finalGrid[i]||
     (x.baseGrid[i]!=='Elephant'&&x.baseGrid[i]!=='Ruby')))return false;
 return true;
}
export function isRubySnapshot(x:unknown):x is RubySnapshot {
 if(!obj(x)||x.version!==1||!guid(x.session)||!integer(x.revision)||!obj(x.state))return false;
 const s=x.state;
 return (s.language==='de'||s.language==='en')&&money(s.bankroll)&&money(s.bet)
  &&typeof s.canSpin==='boolean'&&typeof s.soundEnabled==='boolean'
  &&typeof s.animationsEnabled==='boolean'
  &&Array.isArray(s.bets)&&s.bets.length===9&&s.bets.every(money)
  &&Array.isArray(s.paylines)&&s.paylines.length===10
  &&s.paylines.every((line:unknown)=>Array.isArray(line)&&line.length===5&&line.every((v:unknown)=>integer(v)&&v<15))
  &&obj(s.profile)&&integer(s.profile.level)&&integer(s.profile.prestige)&&integer(s.profile.coins)
  &&obj(s.stats)&&integer(s.stats.spins)&&integer(s.stats.wins)
  &&signed(s.stats.netProfit)&&money(s.stats.biggestPayout)
  &&(s.lastSpin===null||isRubySpin(s.lastSpin));
}
export function isRubyReceipt(x:unknown):x is RubyReceipt {
 return obj(x)&&guid(x.requestId)&&typeof x.success==='boolean'
  &&typeof x.message==='string'&&x.message.length<=2048&&integer(x.revision);
}
export function sendRuby(snapshot:RubySnapshot,intent:RubyIntent):string {
 const requestId=crypto.randomUUID();
 window.chrome?.webview?.postMessage({type:'indian-ruby.command',version:1,requestId,
  session:snapshot.session,expectedRevision:snapshot.revision,...intent});
 return requestId;
}
export function onRubyMessage(receiver:(message:{type:'snapshot';data:RubySnapshot}|
 {type:'receipt';data:RubyReceipt})=>void):()=>void {
 const host=window.chrome?.webview;
 const handler=(event:MessageEvent)=>{
  const message:unknown=event.data;
  if(!obj(message))return;
  if(message.type==='indian-ruby.snapshot'&&isRubySnapshot(message.data))
   receiver({type:'snapshot',data:message.data});
  else if(message.type==='indian-ruby.result'&&isRubyReceipt(message.data))
   receiver({type:'receipt',data:message.data});
 };
 host?.addEventListener('message',handler);
 return()=>host?.removeEventListener('message',handler);
}
