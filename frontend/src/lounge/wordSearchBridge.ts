export const difficulties=['Relaxed','Easy','Medium','Hard','Expert','Master'] as const;
export type WordSearchDifficulty=typeof difficulties[number];
export type WordSearchTier={difficulty:WordSearchDifficulty;size:number;wordCount:number;
 total:number;completed:number;rewardXp:number};
export type WordSearchPath={word:string;startRow:number;startColumn:number;endRow:number;endColumn:number};
export type WordSearchHint={word:string;startRow:number;startColumn:number};
export type WordSearchPuzzle={id:string;difficulty:WordSearchDifficulty;theme:string;
 size:number;grid:string;words:string[];found:string[];foundPaths:WordSearchPath[];
 hint:WordSearchHint|null;hintsUsed:number;elapsedSeconds:number;completed:boolean;rewardXp:number};
export type WordSearchResume={id:string;difficulty:WordSearchDifficulty;theme:string;
 found:number;total:number;elapsedSeconds:number};
export type WordSearchState={screen:'library'|'game';catalogTotal:number;completedTotal:number;
 themeTotal:number;difficulties:WordSearchTier[];
 stats:{completedPuzzles:number;wordsFound:number;noHintCompletions:number;
 expertCompleted:number;masterCompleted:number};
 resume:WordSearchResume|null;game:WordSearchPuzzle|null;notice:string;rewardNotice:string};
export type WordSearchIntent =
 | {action:'word-search.start';difficulty:WordSearchDifficulty}
 | {action:'word-search.resume'|'word-search.hint'|'word-search.back'|'word-search.exit'}
 | {action:'word-search.select';startRow:number;startColumn:number;endRow:number;endColumn:number};
export type WordSearchReply={requestId:string;success:boolean;message:string;revision:number};
const record=(v:unknown):v is Record<string,unknown>=>v!==null&&typeof v==='object'&&!Array.isArray(v);
const integer=(v:unknown,max=Number.MAX_SAFE_INTEGER):v is number=>
 typeof v==='number'&&Number.isSafeInteger(v)&&v>=0&&v<=max;
const guid=(v:unknown)=>typeof v==='string'&&/^[\da-f]{8}-[\da-f]{4}-[\da-f]{4}-[\da-f]{4}-[\da-f]{12}$/i.test(v);
const difficulty=(v:unknown):v is WordSearchDifficulty=>
 typeof v==='string'&&difficulties.some(d=>d===v);
const word=(v:unknown)=>typeof v==='string'&&/^[A-Z]{3,13}$/.test(v);
const count=(v:unknown,max=288)=>integer(v,max);
const coordinates=(v:Record<string,unknown>,size:number)=>['startRow','startColumn','endRow','endColumn']
 .every(k=>integer(v[k],size-1))&&
 ((v.startRow===v.endRow||v.startColumn===v.endColumn)||
 Math.abs((v.startRow as number)-(v.endRow as number))===
 Math.abs((v.startColumn as number)-(v.endColumn as number)))&&
 (v.startRow!==v.endRow||v.startColumn!==v.endColumn);
const path=(v:unknown,size:number):v is WordSearchPath=>
 record(v)&&word(v.word)&&coordinates(v,size);
const puzzle=(v:unknown):v is WordSearchPuzzle=>{
 if(!record(v)||typeof v.id!=='string'||!/^word-search-(relaxed|easy|medium|hard|expert|master)-\d{3}$/.test(v.id)||
 !difficulty(v.difficulty)||typeof v.theme!=='string'||v.theme.length>100||
 !integer(v.size,13)||v.size<8||typeof v.grid!=='string'||
 v.grid.length!==v.size*v.size||!/^[A-Z]+$/.test(v.grid)||
 !Array.isArray(v.words)||v.words.length>14||v.words.length<6||!v.words.every(word)||
 new Set(v.words).size!==v.words.length||
 !Array.isArray(v.found)||v.found.length>v.words.length||!v.found.every(word)||
 new Set(v.found).size!==v.found.length||
 !(v.found as string[]).every(w=>(v.words as string[]).includes(w))||
 !Array.isArray(v.foundPaths)||v.foundPaths.length!==v.found.length||
 !v.foundPaths.every(p=>path(p,v.size as number))||
 new Set(v.foundPaths.map((p:WordSearchPath)=>p.word)).size!==v.found.length||
 !(v.foundPaths as WordSearchPath[]).every(p=>{
  if(!(v.found as string[]).includes(p.word))return false;
  const deltaR=Math.sign(p.endRow-p.startRow),deltaC=Math.sign(p.endColumn-p.startColumn);
  const len=Math.max(Math.abs(p.endRow-p.startRow),Math.abs(p.endColumn-p.startColumn))+1;
  if(len!==p.word.length)return false;
  for(let i=0;i<len;i++)if((v.grid as string)[(p.startRow+i*deltaR)*(v.size as number)+p.startColumn+i*deltaC]!==p.word[i])return false;
  return true;
 })||
 !(v.hint===null||(record(v.hint)&&word(v.hint.word)&&
  (v.words as string[]).includes(v.hint.word as string)&&
  integer(v.hint.startRow,v.size-1)&&integer(v.hint.startColumn,v.size-1)))||
 !integer(v.hintsUsed)||!integer(v.elapsedSeconds)||typeof v.completed!=='boolean'||
 !integer(v.rewardXp,10000))return false;
 return v.completed===((v.found as string[]).length===(v.words as string[]).length);
};
const tier=(v:unknown):v is WordSearchTier=>record(v)&&difficulty(v.difficulty)&&
 integer(v.size,13)&&v.size>=8&&integer(v.wordCount,14)&&v.wordCount>=6&&
 count(v.total)&&v.total>0&&count(v.completed)&&v.completed<=v.total&&
 integer(v.rewardXp,10000);
const resume=(v:unknown):v is WordSearchResume=>record(v)&&typeof v.id==='string'&&
 /^word-search-(relaxed|easy|medium|hard|expert|master)-\d{3}$/.test(v.id)&&
 difficulty(v.difficulty)&&typeof v.theme==='string'&&v.theme.length<=100&&
 integer(v.found,14)&&integer(v.total,14)&&v.found<=v.total&&integer(v.elapsedSeconds);
export function isWordSearchState(v:unknown):v is WordSearchState{
 return record(v)&&(v.screen==='library'||v.screen==='game')&&
 count(v.catalogTotal)&&integer(v.themeTotal,100)&&count(v.completedTotal)&&v.completedTotal<=v.catalogTotal&&
 Array.isArray(v.difficulties)&&v.difficulties.length===6&&v.difficulties.every(tier)&&
 new Set(v.difficulties.map((t:WordSearchTier)=>t.difficulty)).size===6&&
 (v.difficulties as WordSearchTier[]).reduce((sum,t)=>sum+t.total,0)===v.catalogTotal&&
 record(v.stats)&&integer(v.stats.completedPuzzles)&&integer(v.stats.wordsFound)&&
 integer(v.stats.noHintCompletions)&&integer(v.stats.expertCompleted)&&integer(v.stats.masterCompleted)&&
 (v.resume===null||resume(v.resume))&&
 (v.screen==='game'?puzzle(v.game):v.game===null)&&
 typeof v.notice==='string'&&v.notice.length<=8192&&
 typeof v.rewardNotice==='string'&&v.rewardNotice.length<=8192;
}
export function isWordSearchReply(v:unknown):v is WordSearchReply{
 return record(v)&&guid(v.requestId)&&typeof v.success==='boolean'&&
 typeof v.message==='string'&&v.message.length<=8192&&integer(v.revision);
}
export function issueWordSearchIntent(session:string,revision:number,intent:WordSearchIntent):string{
 const requestId=crypto.randomUUID();
 window.chrome?.webview?.postMessage({type:'lounge.word-search.command',version:1,
  requestId,session,expectedRevision:revision,...intent});
 return requestId;
}
