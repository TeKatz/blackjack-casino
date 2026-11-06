export type JigsawMotif={id:string;name:string;category:string;asset:string};
export type JigsawSize={count:number;columns:number;rows:number;rewardXp:number};
export type JigsawPiece={id:number;x:number;y:number;onBoard:boolean;locked:boolean};
export type JigsawGame={gameId:string;motifId:string;pieceCount:number;columns:number;rows:number;
 width:number;height:number;elapsedSeconds:number;timerEnabled:boolean;completed:boolean;
 lockedCount:number;onBoardCount:number;pieces:JigsawPiece[]};
export type JigsawResume={motifId:string;pieceCount:number;lockedCount:number;elapsedSeconds:number};
export type JigsawAchievement={id:string;name:string;description:string;tier:string;goal:number;current:number;
 unlocked:boolean;rewardXp:number;rewardCoins:number};
export type JigsawState={screen:'library'|'game'|'complete'|'achievements';selectedMotifId:string;
 selectedPieceCount:number;timerEnabled:boolean;game:JigsawGame|null;resume:JigsawResume|null;
 motifs:JigsawMotif[];sizes:JigsawSize[];
 stats:{completedGames:number;piecesCompleted:number;largestPuzzle:number;achievementsUnlocked:number};
 achievements:JigsawAchievement[];notice:string;rewardNotice:string};
export type JigsawIntent=
 | {action:'jigsaw.motif';motifId:string}
 | {action:'jigsaw.size';pieceCount:number}
 | {action:'jigsaw.timer';enabled:boolean}
 | {action:'jigsaw.start';confirmReplace?:true}
 | {action:'jigsaw.place'|'jigsaw.move';index:number;x:number;y:number}
 | {action:'jigsaw.tray';index:number}
 | {action:'jigsaw.resume'|'jigsaw.back'|'jigsaw.exit'|'jigsaw.achievements'};
export type JigsawReply={requestId:string;success:boolean;message:string;revision:number};
const record=(v:unknown):v is Record<string,unknown>=>v!==null&&typeof v==='object'&&!Array.isArray(v);
const int=(v:unknown,max=Number.MAX_SAFE_INTEGER):v is number=>
 typeof v==='number'&&Number.isSafeInteger(v)&&v>=0&&v<=max;
const number=(v:unknown):v is number=>typeof v==='number'&&Number.isFinite(v);
const guid=(v:unknown)=>typeof v==='string'&&/^[\da-f]{8}-[\da-f]{4}-[\da-f]{4}-[\da-f]{4}-[\da-f]{12}$/i.test(v);
const label=(v:unknown,max=240)=>typeof v==='string'&&v.length<=max;
export const sizes=[24,48,96,150,300,500,750,1000] as const;
export const motifs=['enchanted-forest','sakura-garden','ocean-depths','frost-kingdom',
 'celestial-voyage','pirate-cove','midnight-manor','neon-city','eternal-casino',
 'clockwork','desert-gold','primordial-dragons'] as const;
const hasMotif=(v:unknown)=>typeof v==='string'&&motifs.some(id=>id===v);
const hasSize=(v:unknown):v is number=>typeof v==='number'&&sizes.some(s=>s===v);
const image=(v:unknown):v is JigsawMotif=>record(v)&&hasMotif(v.id)
 &&label(v.name)&&label(v.category)&&v.asset===v.id+'.png';
const size=(v:unknown):v is JigsawSize=>record(v)&&hasSize(v.count)
 &&int(v.rows,40)&&int(v.columns,40)&&v.rows*v.columns===v.count&&int(v.rewardXp,10000);
const piece=(v:unknown):v is JigsawPiece=>record(v)&&int(v.id,999)
 &&number(v.x)&&Math.abs(v.x)<=5000&&number(v.y)&&Math.abs(v.y)<=5000
 &&typeof v.onBoard==='boolean'&&typeof v.locked==='boolean'&&(!v.locked||v.onBoard);
const board=(v:unknown):v is JigsawGame=>{
 if(!record(v)||!guid(v.gameId)||!hasMotif(v.motifId)||!hasSize(v.pieceCount)
 ||!int(v.rows,40)||!int(v.columns,40)||v.rows*v.columns!==v.pieceCount
 ||v.width!==960||v.height!==600||!int(v.elapsedSeconds,20000000)
 ||typeof v.timerEnabled!=='boolean'||typeof v.completed!=='boolean'
 ||!int(v.lockedCount,1000)||!int(v.onBoardCount,1000)
 ||!Array.isArray(v.pieces)||v.pieces.length!==v.pieceCount
 ||!v.pieces.every(piece)||!v.pieces.every((p:JigsawPiece,i:number)=>p.id===i))return false;
 const ps=v.pieces as JigsawPiece[];
 return ps.filter(p=>p.locked).length===v.lockedCount
 &&ps.filter(p=>p.onBoard).length===v.onBoardCount
 &&v.completed===(v.lockedCount===v.pieceCount);
};
const resume=(v:unknown):v is JigsawResume=>record(v)&&hasMotif(v.motifId)
 &&hasSize(v.pieceCount)&&int(v.lockedCount,1000)&&v.lockedCount<=v.pieceCount
 &&int(v.elapsedSeconds,20000000);
const achievement=(v:unknown):v is JigsawAchievement=>record(v)
 &&label(v.id,100)&&label(v.name)&&label(v.description,500)&&label(v.tier,30)
 &&int(v.goal)&&int(v.current)&&v.current<=v.goal
 &&typeof v.unlocked==='boolean'&&int(v.rewardXp,10000)&&int(v.rewardCoins,10000);
export function isJigsawState(v:unknown):v is JigsawState{
 if(!record(v)||!['library','game','complete','achievements'].includes(String(v.screen))
 ||!hasMotif(v.selectedMotifId)||!hasSize(v.selectedPieceCount)||typeof v.timerEnabled!=='boolean'
 ||!Array.isArray(v.motifs)||v.motifs.length!==12||!v.motifs.every(image)
 ||new Set(v.motifs.map((m:JigsawMotif)=>m.id)).size!==12
 ||!Array.isArray(v.sizes)||v.sizes.length!==8||!v.sizes.every(size)
 ||new Set(v.sizes.map((s:JigsawSize)=>s.count)).size!==8
 ||!(v.resume===null||resume(v.resume))||!record(v.stats)
 ||!['completedGames','piecesCompleted','largestPuzzle'].every(k=>int((v.stats as Record<string,unknown>)[k]))
 ||!int(v.stats.achievementsUnlocked,30)||!Array.isArray(v.achievements)
 ||v.achievements.length!==30||!v.achievements.every(achievement)
 ||!label(v.notice,8192)||!label(v.rewardNotice,8192))return false;
 return v.screen==='library'||v.screen==='achievements'
  ? v.game===null : board(v.game)
    &&(v.screen==='game'?!v.game.completed:v.game.completed);
}
export function isJigsawReply(v:unknown):v is JigsawReply{
 return record(v)&&guid(v.requestId)&&typeof v.success==='boolean'
  &&label(v.message,8192)&&int(v.revision);
}
export function issueJigsawIntent(session:string,revision:number,intent:JigsawIntent):string{
 const requestId=crypto.randomUUID();
 window.chrome?.webview?.postMessage({type:'lounge.jigsaw.command',version:1,
  session,expectedRevision:revision,requestId,...intent});
 return requestId;
}
