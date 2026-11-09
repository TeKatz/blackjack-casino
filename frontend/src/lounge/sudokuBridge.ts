export const difficulties = ['Relaxed','Easy','Medium','Hard','Expert','Master'] as const;
export type Difficulty = typeof difficulties[number];
export type SudokuEntry = {id:Difficulty;canResume:boolean;elapsedSeconds:number;completed:number};
export type SudokuGame = {puzzleId:string;difficulty:Difficulty;isDaily:boolean;
  givens:string;board:string;notes:number[];incorrect:number[];conflicts:number[];completedDigits:number[];
  elapsedSeconds:number;hintsUsed:number;completed:boolean;canUndo:boolean;canRedo:boolean;
  completionXp:number;completionSeasonXp:number};
export type SudokuCareer = {level:number;experience:number;nextLevelXp:number;lifetimeExperience:number;cosmeticPointsEarned:number;pointsPerLevel:number};
export type VariantMode = 'x'|'killer'|'jigsaw'|'samurai'|'mini';
export type VariantDifficulty = 'Easy'|'Medium'|'Hard';
export type VariantEntry = {key:string;mode:VariantMode;difficulty:VariantDifficulty;miniSize:number;canResume:boolean;elapsedSeconds:number;completed:number};
export type VariantCage = {sum:number;cells:number[]};
export type VariantGame = {id:string;mode:VariantMode;difficulty:VariantDifficulty;miniSize:number;width:number;digits:number;
 givens:string;board:string;notes:number[];regions:number[];cages:VariantCage[];
 incorrect:number[];conflicts:number[];elapsedSeconds:number;hintsUsed:number;completed:boolean;
 canUndo:boolean;canRedo:boolean;completionXp:number;careerXp:number;notice:string;rewardNotice:string};
export type SudokuState = {screen:'library'|'game';career?:SudokuCareer;
 variant?:VariantGame|null;variantEntries?:VariantEntry[];showTimer:boolean;showGuides:boolean;entries:SudokuEntry[];
 normalCompleted:number;normalTotal:number;dailyCompleted:number;
 daily:{date:string;difficulty:Difficulty;completed:boolean;canResume:boolean};
 game:SudokuGame|null;notice:string;rewardNotice:string};
export type SudokuIntent =
 | {action:'sudoku.new'|'sudoku.resume';value:Difficulty}
 | {action:'sudoku.daily'|'sudoku.back'|'sudoku.exit'|'sudoku.undo'|'sudoku.redo'}
 | {action:'sudoku.value'|'sudoku.note';index:number;digit:number}
 | {action:'sudoku.erase'|'sudoku.hint';index:number}
 | {action:'sudoku.guides'|'sudoku.timer';value:'true'|'false'}
 | {action:'sudoku.variant.new'|'sudoku.variant.resume';value:string}
 | {action:'sudoku.variant.back'|'sudoku.variant.undo'|'sudoku.variant.redo'}
 | {action:'sudoku.variant.value'|'sudoku.variant.note';index:number;digit:number}
 | {action:'sudoku.variant.erase'|'sudoku.variant.hint';index:number};
export type SudokuReply = {requestId:string;success:boolean;message:string;revision:number};

const obj=(v:unknown):v is Record<string,unknown>=>v!==null&&typeof v==='object'&&!Array.isArray(v);
const integer=(v:unknown,limit=2_000_000_000):v is number=>
 typeof v==='number'&&Number.isSafeInteger(v)&&v>=0&&v<=limit;
const name=(v:unknown):v is Difficulty=>typeof v==='string'&&
 difficulties.some(d=>d===v);
const countArray=(v:unknown,min:number,max:number,length?:number):v is number[]=>
 Array.isArray(v)&&(length===undefined||v.length===length)&&v.length<=81&&
 v.every(n=>integer(n,max)&&n>=min)&&new Set(v).size===v.length;
const puzzleGrid=(v:unknown):v is string=>typeof v==='string'&&/^[0-9]{81}$/.test(v);
const game=(v:unknown):v is SudokuGame=>{
 if(!obj(v)||typeof v.puzzleId!=='string'||!/^(normal|daily)-[a-z]+-\d{3,4}$/.test(v.puzzleId)
  ||!name(v.difficulty)||typeof v.isDaily!=='boolean'
  ||!puzzleGrid(v.givens)||!puzzleGrid(v.board)
  ||!Array.isArray(v.notes)||v.notes.length!==81||!v.notes.every(n=>integer(n,1022)&&!(n&1))
  ||!countArray(v.incorrect,0,80)||!countArray(v.conflicts,0,80)
  ||!countArray(v.completedDigits,1,9)||!integer(v.elapsedSeconds,9_000_000_000)
  ||!integer(v.hintsUsed)||typeof v.completed!=='boolean'
  ||typeof v.canUndo!=='boolean'||typeof v.canRedo!=='boolean'
  ||!integer(v.completionXp)||!integer(v.completionSeasonXp))return false;
 if(v.isDaily!==v.puzzleId.startsWith('daily-'))return false;
 for(let i=0;i<81;i++){
  if(v.givens[i]!=='0'&&v.givens[i]!==v.board[i])return false;
  if(v.board[i]!=='0'&&v.notes[i]!==0)return false;
 }
 return true;
};
const variantMode=(v:unknown):v is VariantMode=>v==='x'||v==='killer'||v==='jigsaw'||v==='samurai'||v==='mini';
const variantDifficulty=(v:unknown):v is VariantDifficulty=>v==='Easy'||v==='Medium'||v==='Hard';
const variantKey=(v:unknown):v is string=>typeof v==='string'&&/^(x|killer|jigsaw|samurai):(Easy|Medium|Hard)$|^mini:(Easy|Medium|Hard):(4|6)$/.test(v);
const variantEntry=(v:unknown):v is VariantEntry=>obj(v)&&variantKey(v.key)&&variantMode(v.mode)&&
 variantDifficulty(v.difficulty)&&integer(v.miniSize,9)&&typeof v.canResume==='boolean'&&
 integer(v.elapsedSeconds,9_000_000_000)&&integer(v.completed);
export function isVariantGame(v:unknown):v is VariantGame{
 if(!obj(v)||!variantMode(v.mode)||!variantDifficulty(v.difficulty)||
  !integer(v.width,21)||![4,6,9,21].includes(v.width)||!integer(v.digits,9)||
  v.digits!==(v.mode==='mini'?v.width:9)||v.width!==(v.mode==='samurai'?21:v.mode==='mini'?v.digits:9)||
  !integer(v.miniSize,9)||!(v.mode==='mini'?[4,6].includes(v.miniSize):v.miniSize===9)||
  typeof v.id!=='string'||!/^(variant-)(x|killer|jigsaw|samurai|mini[46])-(easy|medium|hard)-\d{4}$/.test(v.id)||
  typeof v.board!=='string'||typeof v.givens!=='string'||v.board.length!==(v.width as number)*(v.width as number)||
  v.givens.length!==v.board.length||!/^[0-9-]+$/.test(v.board)||!/^[0-9-]+$/.test(v.givens)||
  !Array.isArray(v.notes)||v.notes.length!==v.board.length||!v.notes.every(n=>integer(n,1022))||
  !Array.isArray(v.regions)||v.regions.length!==v.board.length||
  !v.regions.every(n=>Number.isSafeInteger(n)&&n>=-1&&n<=44)||
  !Array.isArray(v.cages)||v.cages.length>100||!v.cages.every(c=>obj(c)&&integer(c.sum,45)&&
   Array.isArray(c.cells)&&c.cells.length>=1&&c.cells.length<=9&&
   c.cells.every(n=>integer(n,(v.width as number)*(v.width as number)-1))&&new Set(c.cells).size===c.cells.length)||
  !Array.isArray(v.incorrect)||!Array.isArray(v.conflicts)||
  !v.incorrect.every(n=>integer(n,(v.width as number)*(v.width as number)-1))||
  !v.conflicts.every(n=>integer(n,(v.width as number)*(v.width as number)-1))||
  !integer(v.elapsedSeconds,9_000_000_000)||!integer(v.hintsUsed)||typeof v.completed!=='boolean'||
  typeof v.canUndo!=='boolean'||typeof v.canRedo!=='boolean'||!integer(v.completionXp)||
  !integer(v.careerXp)||typeof v.notice!=='string'||typeof v.rewardNotice!=='string')return false;
 for(let i=0;i<v.board.length;i++){
  if(v.givens[i]==='-'&&v.board[i]!=='-')return false;
  if(v.givens[i]!=='-'&&v.board[i]==='-')return false;
  if(v.givens[i]!=='0'&&v.givens[i]!==v.board[i])return false;
  if(v.board[i]!=='0'&&v.notes[i]!==0)return false;
  if(v.board[i]!=='-'&&Number(v.board[i])>v.digits)return false;
 }
 return true;
}
export function isSudokuState(v:unknown):v is SudokuState{
 if(!obj(v)||(v.screen!=='library'&&v.screen!=='game')||typeof v.showTimer!=='boolean'
  ||typeof v.showGuides!=='boolean'||!Array.isArray(v.entries)||v.entries.length!==6
  ||!v.entries.every(e=>obj(e)&&name(e.id)&&typeof e.canResume==='boolean'
     &&integer(e.elapsedSeconds,9_000_000_000)&&integer(e.completed))
  ||new Set(v.entries.map(e=>(e as SudokuEntry).id)).size!==6
  ||!integer(v.normalCompleted)||!integer(v.normalTotal)
  ||v.normalCompleted>v.normalTotal||!integer(v.dailyCompleted)
  ||!obj(v.daily)||typeof v.daily.date!=='string'
  ||!/^\d{4}-\d{2}-\d{2}$/.test(v.daily.date)||!name(v.daily.difficulty)
  ||typeof v.daily.completed!=='boolean'||typeof v.daily.canResume!=='boolean'
  ||typeof v.notice!=='string'||v.notice.length>8192
  ||typeof v.rewardNotice!=='string'||v.rewardNotice.length>8192)return false;
 if(v.variantEntries!==undefined&&(!Array.isArray(v.variantEntries)||v.variantEntries.length!==18||
  !v.variantEntries.every(variantEntry)||new Set(v.variantEntries.map(e=>(e as VariantEntry).key)).size!==18))return false;
 if(v.variant!==undefined&&v.variant!==null&&!isVariantGame(v.variant))return false;
 if(v.career!==undefined && (!obj(v.career)||!integer(v.career.level,100)||v.career.level<1
   ||!integer(v.career.experience,9_000_000_000)||!integer(v.career.nextLevelXp,9_000_000_000)
   ||!integer(v.career.lifetimeExperience,9_000_000_000)
   ||!integer(v.career.cosmeticPointsEarned,9_000_000_000)||!integer(v.career.pointsPerLevel,1_000_000)))return false;
 return v.screen==='game'?game(v.game):v.game===null;
}
export function isSudokuReply(v:unknown):v is SudokuReply{
 return obj(v)&&typeof v.requestId==='string'&&
 /^[\da-f]{8}-[\da-f]{4}-[\da-f]{4}-[\da-f]{4}-[\da-f]{12}$/i.test(v.requestId)&&
 typeof v.success==='boolean'&&typeof v.message==='string'&&v.message.length<=8192&&integer(v.revision);
}
export function issueSudokuIntent(session:string,revision:number,intent:SudokuIntent):string {
 const requestId=crypto.randomUUID();
 window.chrome?.webview?.postMessage({type:'lounge.sudoku.command',version:1,requestId,
   session,expectedRevision:revision,...intent});
 return requestId;
}

export function sudokuEditIntent(action:'sudoku.erase'|'sudoku.hint',selected:number):SudokuIntent|null {
 if(!Number.isInteger(selected)||selected < (action==='sudoku.hint'?-1:0)||selected>80)return null;
 return {action,index:selected};
}
