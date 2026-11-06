export const variants = ['Klondike','FreeCell','Spider','Pyramid','Golf','TriPeaks'] as const;
export const suits = ['Clubs','Diamonds','Hearts','Spades'] as const;
export type SolitaireVariant = typeof variants[number];
export type CardSuit = typeof suits[number];
export type SolitaireCard = {rank:number;suit:CardSuit;faceUp:boolean;selectable:boolean};
export type SolitaireOption = {variant:SolitaireVariant;spiderSuits:number;
 canResume:boolean;won:boolean;dealNumber:number;moves:number;elapsedSeconds:number};
export type SolitaireAchievement = {id:string;name:string;description:string;complete:boolean};
export type SolitaireGame = {gameId:string;variant:SolitaireVariant;dealNumber:number;spiderSuits:number;
 moves:number;elapsedSeconds:number;completedRuns:number;won:boolean;
 piles:Record<string,SolitaireCard[]>;rewardXp:number};
export type SolitaireState = {screen:'library'|'game';cardSetId:string;nextDealNumber:number;
 options:SolitaireOption[];stats:{gamesStarted:number;wins:number;gamesAbandoned:number;
 bestWinStreak:number;currentWinStreak:number;winRate:number;achievements:SolitaireAchievement[]};
 game:SolitaireGame|null;notice:string;rewardNotice:string};
export type SolitaireIntent =
 | {action:'solitaire.new'|'solitaire.resume';variant:SolitaireVariant;spiderSuits:number;dealNumber?:number}
 | {action:'solitaire.draw'|'solitaire.back'|'solitaire.exit'}
 | {action:'solitaire.move';source:string;index:number;target:string}
 | {action:'solitaire.activate'|'solitaire.auto';source:string;index:number};
export type SolitaireReply = {requestId:string;success:boolean;message:string;revision:number};
const record=(v:unknown):v is Record<string,unknown>=>v!==null&&typeof v==='object'&&!Array.isArray(v);
const integer=(v:unknown,limit=Number.MAX_SAFE_INTEGER):v is number=>
 typeof v==='number'&&Number.isSafeInteger(v)&&v>=0&&v<=limit;
const guid=(v:unknown)=>typeof v==='string'&&/^[\da-f]{8}-[\da-f]{4}-[\da-f]{4}-[\da-f]{4}-[\da-f]{12}$/i.test(v);
const variant=(v:unknown):v is SolitaireVariant=>typeof v==='string'&&variants.some(x=>x===v);
const suit=(v:unknown):v is CardSuit=>typeof v==='string'&&suits.some(x=>x===v);
const validPile=(id:string)=>/^(Stock|Waste|F-(Clubs|Diamonds|Hearts|Spades)|Cell[0-3]|[TCGP](?:[0-9]|1[0-9]|2[0-7]))$/.test(id);
const option=(v:unknown):v is SolitaireOption=>record(v)&&variant(v.variant)&&
 (v.spiderSuits===1||v.variant==='Spider'&&(v.spiderSuits===2||v.spiderSuits===4))&&
 typeof v.canResume==='boolean'&&typeof v.won==='boolean'&&integer(v.dealNumber,2_000_000_000)&&
 integer(v.moves)&&integer(v.elapsedSeconds);
const card=(v:unknown):v is SolitaireCard=>record(v)&&integer(v.rank,13)&&v.rank>=1&&
 suit(v.suit)&&typeof v.faceUp==='boolean'&&typeof v.selectable==='boolean';
const achievement=(v:unknown):v is SolitaireAchievement=>record(v)&&
 typeof v.id==='string'&&v.id.length<=80&&
 typeof v.name==='string'&&v.name.length<=200&&
 typeof v.description==='string'&&v.description.length<=500&&typeof v.complete==='boolean';
const validGame=(v:unknown):v is SolitaireGame=>{
 if(!record(v)||!guid(v.gameId)||!variant(v.variant)||
 !integer(v.dealNumber,2_000_000_000)||v.dealNumber<1||
 ![1,2,4].includes(v.spiderSuits as number)||!integer(v.moves)||!integer(v.elapsedSeconds)||
 !integer(v.completedRuns,8)||typeof v.won!=='boolean'||!integer(v.rewardXp,100000)||
 !record(v.piles))return false;
 const entries=Object.entries(v.piles);
 if(entries.length>44||entries.length<2||entries.some(([id,pile])=>
  !validPile(id)||!Array.isArray(pile)||pile.length>104||!pile.every(card)))return false;
 if(entries.reduce((sum,[,pile])=>sum+(pile as SolitaireCard[]).length,0)>104)return false;
 return true;
};
export function isSolitaireState(v:unknown):v is SolitaireState{
 if(!record(v)||(v.screen!=='library'&&v.screen!=='game')||
 typeof v.cardSetId!=='string'||v.cardSetId.length>100||
 !integer(v.nextDealNumber,2_147_483_647)||!Array.isArray(v.options)||
 v.options.length!==8||!v.options.every(option)||
 new Set(v.options.map((o:SolitaireOption)=>o.variant+':'+o.spiderSuits)).size!==8||
 !record(v.stats)||!integer(v.stats.gamesStarted)||!integer(v.stats.wins)||
 !integer(v.stats.gamesAbandoned)||!integer(v.stats.bestWinStreak)||
 !integer(v.stats.currentWinStreak)||typeof v.stats.winRate!=='number'||
 !Number.isFinite(v.stats.winRate)||!Array.isArray(v.stats.achievements)||
 v.stats.achievements.length>32||!v.stats.achievements.every(achievement)||
 typeof v.notice!=='string'||v.notice.length>8192||
 typeof v.rewardNotice!=='string'||v.rewardNotice.length>8192)return false;
 return v.screen==='game'?validGame(v.game):v.game===null;
}
export function isSolitaireReply(v:unknown):v is SolitaireReply{
 return record(v)&&guid(v.requestId)&&typeof v.success==='boolean'&&
 typeof v.message==='string'&&v.message.length<=8192&&integer(v.revision);
}
export function issueSolitaireIntent(session:string,revision:number,intent:SolitaireIntent):string{
 const requestId=crypto.randomUUID();
 window.chrome?.webview?.postMessage({type:'lounge.solitaire.command',version:1,
  requestId,session,expectedRevision:revision,...intent});
 return requestId;
}
