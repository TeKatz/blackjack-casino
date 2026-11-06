export const difficulties=['Easy','Normal','Hard','Expert','Master'] as const;
export type MemoryDifficulty=typeof difficulties[number];
export const deckIds=['casino-icons','card-suits','animals','fruits'] as const;
export type MemoryDeckId=typeof deckIds[number];
export type MemorySymbol={glyph:string;name:string;accent:string};
export type MemoryDeck={id:MemoryDeckId;name:string;description:string;preview:MemorySymbol[]};
export type MemoryTier={difficulty:MemoryDifficulty;rows:number;columns:number;pairs:number;rewardXp:number;bestMoves:number};
export type MemoryCard={index:number;matched:boolean;faceUp:boolean;glyph:string|null;name:string|null;accent:string|null};
export type MemoryTable={gameId:string;difficulty:MemoryDifficulty;deckId:MemoryDeckId;deckName:string;
 rows:number;columns:number;cards:MemoryCard[];pairsFound:number;pairCount:number;moves:number;
 cardsFlipped:number;wrongMatches:number;currentStreak:number;bestStreak:number;accuracy:number;
 elapsedSeconds:number;completed:boolean;perfect:boolean;resolvingMismatch:boolean;rewardXp:number};
export type MemoryResume={difficulty:MemoryDifficulty;deckId:MemoryDeckId;pairsFound:number;pairCount:number;
 moves:number;elapsedSeconds:number};
export type MemoryState={screen:'library'|'game'|'complete';preferredDeckId:MemoryDeckId;
 decks:MemoryDeck[];difficulties:MemoryTier[];
 stats:{completedGames:number;pairsFound:number;cardsFlipped:number;wrongMatches:number;
 perfectGames:number;expertCompleted:number;masterCompleted:number;bestStreak:number;bestAccuracy:number};
 resume:MemoryResume|null;game:MemoryTable|null;notice:string;rewardNotice:string};
export type MemoryIntent=
 | {action:'memory.deck';deckId:MemoryDeckId}
 | {action:'memory.start';difficulty:MemoryDifficulty}
 | {action:'memory.flip';index:number}
 | {action:'memory.resume'|'memory.again'|'memory.back'|'memory.exit'};
export type MemoryReply={requestId:string;success:boolean;message:string;revision:number};
const record=(v:unknown):v is Record<string,unknown>=>v!==null&&typeof v==='object'&&!Array.isArray(v);
const int=(v:unknown,max=Number.MAX_SAFE_INTEGER):v is number=>
 typeof v==='number'&&Number.isSafeInteger(v)&&v>=0&&v<=max;
const numberBetween=(v:unknown,max:number)=>typeof v==='number'&&Number.isFinite(v)&&v>=0&&v<=max;
const guid=(v:unknown)=>typeof v==='string'&&/^[\da-f]{8}-[\da-f]{4}-[\da-f]{4}-[\da-f]{4}-[\da-f]{12}$/i.test(v);
const difficulty=(v:unknown):v is MemoryDifficulty=>typeof v==='string'&&difficulties.some(x=>x===v);
const deck=(v:unknown):v is MemoryDeckId=>typeof v==='string'&&deckIds.some(x=>x===v);
const label=(v:unknown,max=100)=>typeof v==='string'&&v.length<=max;
const color=(v:unknown)=>typeof v==='string'&&/^#[\da-f]{6}$/i.test(v);
const symbol=(v:unknown):v is MemorySymbol=>record(v)&&label(v.glyph,12)&&label(v.name)&&color(v.accent);
const deckInfo=(v:unknown):v is MemoryDeck=>record(v)&&deck(v.id)&&label(v.name)&&label(v.description,300)
 &&Array.isArray(v.preview)&&v.preview.length===4&&v.preview.every(symbol);
const specs:Record<MemoryDifficulty,[number,number,number]>={
 Easy:[4,4,8],Normal:[4,5,10],Hard:[6,6,18],Expert:[6,8,24],Master:[8,8,32]};
const tier=(v:unknown):v is MemoryTier=>record(v)&&difficulty(v.difficulty)
 &&int(v.rows,8)&&int(v.columns,8)&&int(v.pairs,32)
 &&[v.rows,v.columns,v.pairs].every((n,i)=>n===specs[v.difficulty as MemoryDifficulty][i])
 &&int(v.rewardXp,10000)&&int(v.bestMoves);
const card=(v:unknown):v is MemoryCard=>{
 if(!record(v)||!int(v.index,63)||typeof v.faceUp!=='boolean'||typeof v.matched!=='boolean'
 ||(v.matched&&!v.faceUp))return false;
 return v.faceUp ? label(v.glyph,12)&&label(v.name)&&color(v.accent)
  : v.glyph===null&&v.name===null&&v.accent===null;
};
const table=(v:unknown):v is MemoryTable=>{
 if(!record(v)||!guid(v.gameId)||!difficulty(v.difficulty)||!deck(v.deckId)
 ||!label(v.deckName)||!int(v.rows,8)||!int(v.columns,8)
 ||v.rows!==specs[v.difficulty][0]||v.columns!==specs[v.difficulty][1]
 ||!Array.isArray(v.cards)||v.cards.length!==v.rows*v.columns||!v.cards.every(card)
 ||!v.cards.every((c:MemoryCard,i:number)=>c.index===i)
 ||!int(v.pairCount,32)||v.pairCount!==specs[v.difficulty][2]
 ||!int(v.pairsFound,32)||v.pairsFound>v.pairCount
 ||v.cards.filter((c:MemoryCard)=>c.matched).length!==v.pairsFound*2
 ||!int(v.moves)||!int(v.cardsFlipped)||!int(v.wrongMatches)||v.wrongMatches>v.moves
 ||!int(v.currentStreak)||!int(v.bestStreak)||v.bestStreak<v.currentStreak
 ||!numberBetween(v.accuracy,1)||!int(v.elapsedSeconds)
 ||typeof v.completed!=='boolean'||v.completed!==(v.pairsFound===v.pairCount)
 ||typeof v.perfect!=='boolean'||v.perfect!==(v.completed&&v.wrongMatches===0)
 ||typeof v.resolvingMismatch!=='boolean'||v.resolvingMismatch&&v.completed
 ||!int(v.rewardXp,10000))return false;
 const open=(v.cards as MemoryCard[]).filter(c=>c.faceUp&&!c.matched);
 return open.length<=2 && (!v.resolvingMismatch||open.length===2);
};
const resume=(v:unknown):v is MemoryResume=>record(v)&&difficulty(v.difficulty)&&deck(v.deckId)
 &&int(v.pairCount,32)&&v.pairCount===specs[v.difficulty][2]
 &&int(v.pairsFound,32)&&v.pairsFound<=v.pairCount&&int(v.moves)&&int(v.elapsedSeconds);
export function isMemoryState(v:unknown):v is MemoryState{
 if(!record(v)||!['library','game','complete'].includes(String(v.screen))||!deck(v.preferredDeckId)
 ||!Array.isArray(v.decks)||v.decks.length!==4||!v.decks.every(deckInfo)
 ||new Set(v.decks.map((d:MemoryDeck)=>d.id)).size!==4
 ||!Array.isArray(v.difficulties)||v.difficulties.length!==5||!v.difficulties.every(tier)
 ||new Set(v.difficulties.map((d:MemoryTier)=>d.difficulty)).size!==5
 ||!record(v.stats)||!['completedGames','pairsFound','cardsFlipped','wrongMatches',
  'perfectGames','expertCompleted','masterCompleted','bestStreak'].every(k=>int(v.stats[k]))
 ||!numberBetween(v.stats.bestAccuracy,1)
 ||!(v.resume===null||resume(v.resume))
 ||typeof v.notice!=='string'||v.notice.length>8192
 ||typeof v.rewardNotice!=='string'||v.rewardNotice.length>8192)return false;
 return v.screen==='library' ? v.game===null
  : table(v.game)&&(v.screen==='complete' ? v.game.completed&&!v.resume
     : !v.game.completed);
}
export function isMemoryReply(v:unknown):v is MemoryReply{
 return record(v)&&guid(v.requestId)&&typeof v.success==='boolean'
 &&label(v.message,8192)&&int(v.revision);
}
export function issueMemoryIntent(session:string,revision:number,intent:MemoryIntent):string{
 const requestId=crypto.randomUUID();
 window.chrome?.webview?.postMessage({type:'lounge.memory.command',version:1,
  requestId,session,expectedRevision:revision,...intent});
 return requestId;
}
