// A read-only projection of C# outcomes. No client-side RNG, odds or payouts.
export const symbols=['Yarn','Fishbone','Bell','TabbyCat','WhiteCat','BlackCat','Wild','Bonus'] as const;
export type NineLivesSymbol=typeof symbols[number];
export type NineLivesSpin={
  id:string;bet:string;stake:string;bankrollBefore:string;bankrollAfter:string;
  grid:NineLivesSymbol[];winningLines:number[];winningCells:number[];
  pounceWildCell:number|null;bonusCount:number;bonusSpinsAwarded:number;
  bonusSpinsRemaining:number;isBonusSpin:boolean;fullGridFrenzy:boolean;
  payout:string;net:string;totalMultiplier:number;
  playerXp:number;seasonXp:number;levelUp:boolean;
};
export type NineLivesState={
  language:'en'|'de';bankroll:string;bet:string;canSpin:boolean;
  bonusActive:boolean;bonusSpinsRemaining:number;
  soundEnabled:boolean;animationsEnabled:boolean;bets:string[];
  paylines:number[][];
  paytable:{id:NineLivesSymbol;name:string;weight:number;lineMultiplier:number}[];
  profile:{level:number;prestige:number;coins:number};
  stats:{spins:number;wins:number;paidSpins:number;bonusSpins:number;
    bonusSessions:number;pounceWilds:number;fullGridFrenzies:number;
    netProfit:string;biggestPayout:string;bestPayoutMultiplier:number};
  lastSpin:NineLivesSpin|null;
};
export type NineLivesSnapshot={version:1;session:string;revision:number;state:NineLivesState};
export type NineLivesReceipt={requestId:string;success:boolean;message:string;revision:number};
export type NineLivesIntent={action:'spin'|'bet.set'|'sound.set'|'animations.set';value?:string};

const obj=(v:unknown):v is Record<string,unknown>=>typeof v==='object'&&v!==null&&!Array.isArray(v);
const int=(v:unknown):v is number=>typeof v==='number'&&Number.isSafeInteger(v)&&v>=0;
const decimal=(v:unknown):v is string=>typeof v==='string'&&/^(?:0|[1-9]\d*)(?:\.\d{1,28})?$/.test(v);
const signed=(v:unknown):v is string=>typeof v==='string'&&/^-?(?:0|[1-9]\d*)(?:\.\d{1,28})?$/.test(v);
const finite=(v:unknown):v is number=>typeof v==='number'&&Number.isFinite(v)&&v>=0;
const symbol=(v:unknown):v is NineLivesSymbol=>symbols.some(id=>id===v);
const guid=(v:unknown):v is string=>typeof v==='string'&&/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(v);
const cells=(v:unknown):v is number[]=>Array.isArray(v)&&v.every(x=>int(x)&&x<9)&&new Set(v).size===v.length;
const lines=(v:unknown):v is number[]=>Array.isArray(v)&&v.every(x=>int(x)&&x<5)&&new Set(v).size===v.length;
export const paylineCells=[[0,1,2],[3,4,5],[6,7,8],[0,4,8],[2,4,6]] as const;
export function isNineLivesSpin(v:unknown):v is NineLivesSpin{
  if(!obj(v)||!guid(v.id)||!decimal(v.bet)||!decimal(v.stake)
    ||!decimal(v.bankrollBefore)||!decimal(v.bankrollAfter)||!decimal(v.payout)
    ||!signed(v.net)||!finite(v.totalMultiplier)||!int(v.playerXp)||!int(v.seasonXp)
    ||typeof v.levelUp!=='boolean'||typeof v.isBonusSpin!=='boolean'
    ||typeof v.fullGridFrenzy!=='boolean'||!int(v.bonusCount)||v.bonusCount>9
    ||!int(v.bonusSpinsRemaining)||!int(v.bonusSpinsAwarded)
    ||(v.bonusSpinsAwarded!==0&&v.bonusSpinsAwarded!==9)
    ||!Array.isArray(v.grid)||v.grid.length!==9||!v.grid.every(symbol)
    ||!lines(v.winningLines)||!cells(v.winningCells)
    ||!(v.pounceWildCell===null||(int(v.pounceWildCell)&&v.pounceWildCell<9)))return false;
  if(v.pounceWildCell!==null&&v.grid[v.pounceWildCell]!=='Wild')return false;
  if(v.bonusCount!==v.grid.filter(x=>x==='Bonus').length)return false;
  if(v.fullGridFrenzy&&v.grid.some(x=>x==='Bonus'))return false;
  if(v.winningLines.some(line=>paylineCells[line].some(index=>!(v.winningCells as number[]).includes(index))))return false;
  return true;
}
export function isNineLivesSnapshot(v:unknown):v is NineLivesSnapshot{
  if(!obj(v)||v.version!==1||!guid(v.session)||!int(v.revision)||!obj(v.state))return false;
  const s=v.state;
  if(s.language!=='de'&&s.language!=='en')return false;
  if(!decimal(s.bankroll)||!decimal(s.bet)||typeof s.canSpin!=='boolean'
    ||typeof s.bonusActive!=='boolean'||!int(s.bonusSpinsRemaining)
    ||(s.bonusActive!==(s.bonusSpinsRemaining>0))
    ||typeof s.soundEnabled!=='boolean'||typeof s.animationsEnabled!=='boolean'
    ||!Array.isArray(s.bets)||s.bets.length!==13||!s.bets.every(decimal)
    ||!Array.isArray(s.paylines)||s.paylines.length!==5
    ||!s.paylines.every((line:unknown,i:number)=>Array.isArray(line)
      &&line.length===3&&line.every((n:unknown,j:number)=>n===paylineCells[i][j]))
    ||!Array.isArray(s.paytable)||s.paytable.length!==8
    ||!s.paytable.every((item:unknown)=>obj(item)&&symbol(item.id)
      &&typeof item.name==='string'&&item.name.length<=80
      &&int(item.weight)&&finite(item.lineMultiplier))
    ||!obj(s.profile)||!int(s.profile.level)||!int(s.profile.prestige)||!int(s.profile.coins)
    ||!obj(s.stats)||!int(s.stats.spins)||!int(s.stats.wins)||!int(s.stats.paidSpins)
    ||!int(s.stats.bonusSpins)||!int(s.stats.bonusSessions)||!int(s.stats.pounceWilds)
    ||!int(s.stats.fullGridFrenzies)||!signed(s.stats.netProfit)
    ||!decimal(s.stats.biggestPayout)||!finite(s.stats.bestPayoutMultiplier))return false;
  return s.lastSpin===null||isNineLivesSpin(s.lastSpin);
}
export function isNineLivesReceipt(v:unknown):v is NineLivesReceipt{
  return obj(v)&&guid(v.requestId)&&typeof v.success==='boolean'
    &&typeof v.message==='string'&&v.message.length<=2048&&int(v.revision);
}
export function sendNineLivesIntent(snapshot:NineLivesSnapshot,intent:NineLivesIntent):string{
  const requestId=crypto.randomUUID();
  window.chrome?.webview?.postMessage({type:'nine-lives.command',version:1,
    requestId,session:snapshot.session,expectedRevision:snapshot.revision,...intent});
  return requestId;
}
export function listenNineLives(receive:(event:{type:'snapshot';data:NineLivesSnapshot}|
  {type:'receipt';data:NineLivesReceipt})=>void):()=>void{
  const browser=window.chrome?.webview;
  const onMessage=(event:MessageEvent)=>{
    const payload:unknown=event.data;
    if(!obj(payload))return;
    if(payload.type==='nine-lives.snapshot'&&isNineLivesSnapshot(payload.data))
      receive({type:'snapshot',data:payload.data});
    if(payload.type==='nine-lives.result'&&isNineLivesReceipt(payload.data))
      receive({type:'receipt',data:payload.data});
  };
  browser?.addEventListener('message',onMessage);
  return()=>browser?.removeEventListener('message',onMessage);
}
