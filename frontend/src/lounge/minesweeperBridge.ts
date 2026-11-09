/** WebView2 intent-only contract. Hidden mine positions never cross into React. */
export type SweepCell={revealed:boolean;flagged:boolean;scanned:boolean;adjacent:number;scanCount:number;mine:boolean};
export type SweepState={
 screen:'menu'|'game';runId:string;difficulty:'easy'|'standard'|'expert';
 width:number;height:number;mines:number;phase:'ready'|'active'|'won'|'lost';
 seconds:number;scansUsed:number;safeRevealed:number;explodedIndex:number;flagged:number;
 cells:SweepCell[];
};
export type SweepIntent=
 |{action:'mine.new';difficulty:'easy'|'standard'|'expert'}
 |{action:'mine.reveal'|'mine.flag'|'mine.chord'|'mine.scan';index:number}
 |{action:'mine.exit'|'mine.menu'|'mine.resume'};
export type SweepReply={requestId:string;success:boolean;message:string;revision:number};
const rec=(v:unknown):v is Record<string,unknown>=>v!==null&&typeof v==='object'&&!Array.isArray(v);
const int=(v:unknown,min=0,max=Number.MAX_SAFE_INTEGER)=>typeof v==='number'&&Number.isSafeInteger(v)&&v>=min&&v<=max;
const guid=(v:unknown)=>typeof v==='string'&&/^[\da-f]{8}-[\da-f]{4}-[\da-f]{4}-[\da-f]{4}-[\da-f]{12}$/i.test(v);
const cell=(v:unknown):v is SweepCell=>rec(v)
 &&typeof v.revealed==='boolean'&&typeof v.flagged==='boolean'
 &&typeof v.scanned==='boolean'&&typeof v.mine==='boolean'
 &&int(v.adjacent,-1,8)&&int(v.scanCount,-1,9);
export function isSweepState(v:unknown):v is SweepState{
 if(!rec(v)||!['menu','game'].includes(String(v.screen))||!guid(v.runId)
  ||!['ready','active','won','lost'].includes(String(v.phase))||
  !['easy','standard','expert'].includes(String(v.difficulty))||
  !int(v.seconds,0,100000000)||!int(v.scansUsed,0,1)||!int(v.safeRevealed,0,256)
  ||!int(v.flagged,0,256)||!int(v.explodedIndex,-1,255)
  ||!Array.isArray(v.cells)||!v.cells.every(cell))return false;
 const expected=v.difficulty==='easy'?[8,8,10]:v.difficulty==='standard'?[12,12,24]:[16,16,40];
 if(v.width!==expected[0]||v.height!==expected[1]||v.mines!==expected[2]||
  v.cells.length!==expected[0]*expected[1]||Number(v.safeRevealed)>v.cells.length-v.mines||
  v.flagged!==v.cells.filter((c:SweepCell)=>c.flagged).length)return false;
 return v.cells.every((c:SweepCell)=>
  (!c.mine||v.phase==='won'||v.phase==='lost') &&
  (c.revealed ? c.adjacent>=0 : c.adjacent===-1) &&
  (c.scanned ? !c.revealed&&c.scanCount>=0 : c.scanCount===-1) &&
  !(c.revealed&&c.flagged)) &&
  v.safeRevealed===v.cells.filter((c:SweepCell)=>c.revealed&&!c.mine).length;
}
export function isSweepReply(v:unknown):v is SweepReply{
 return rec(v)&&guid(v.requestId)&&typeof v.success==='boolean'
  &&typeof v.message==='string'&&v.message.length<=8192&&int(v.revision);
}
export function issueSweepIntent(session:string,revision:number,intent:SweepIntent):string{
 const requestId=crypto.randomUUID();
 window.chrome?.webview?.postMessage({type:'lounge.minesweeper.command',version:1,
  session,expectedRevision:revision,requestId,...intent});
 return requestId;
}
