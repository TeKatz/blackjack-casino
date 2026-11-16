/** Lobby scratchcards: the Windows host owns the ticket, RNG, wallet and payout. */
export type ScratchcardTicket = {
  ticketId:string; symbols:number[]; prize:number; collected:boolean; bankrollAfter:number;
};
export type ScratchcardSnapshot = {
  price:number; bankroll:number; ticket:ScratchcardTicket|null; recoveryRequired:boolean;
};
export type ScratchcardMessage =
  | {type:'lobby.scratchcards.snapshot';data:ScratchcardSnapshot}
  | {type:'lobby.scratchcards.result';data:{action:'buy'|'collect';success:boolean;message:string}};

const isRecord=(value:unknown):value is Record<string,unknown> =>
  typeof value==='object'&&value!==null&&!Array.isArray(value);
const amount=(value:unknown)=>typeof value==='number'&&Number.isFinite(value)&&value>=0;
export function isScratchcardSnapshot(value:unknown):value is ScratchcardSnapshot {
  if(!isRecord(value)||value.price!==20||!amount(value.bankroll)||
    typeof value.recoveryRequired!=='boolean')return false;
  if(value.ticket===null)return true;
  const ticket=value.ticket;
  return isRecord(ticket)&&typeof ticket.ticketId==='string'&&
    /^[a-f0-9-]{36}$/i.test(ticket.ticketId)&&
    Array.isArray(ticket.symbols)&&ticket.symbols.length===9&&
    ticket.symbols.every(symbol=>Number.isInteger(symbol)&&symbol>=0&&symbol<5)&&
    [0,10,20,50,100,250,1000].includes(ticket.prize as number)&&
    typeof ticket.collected==='boolean'&&amount(ticket.bankrollAfter);
}
export function isScratchcardMessage(value:unknown):value is ScratchcardMessage {
  if(!isRecord(value))return false;
  if(value.type==='lobby.scratchcards.snapshot')return isScratchcardSnapshot(value.data);
  if(value.type!=='lobby.scratchcards.result'||!isRecord(value.data))return false;
  const result=value.data;
  return (result.action==='buy'||result.action==='collect')&&
    typeof result.success==='boolean'&&typeof result.message==='string'&&
    result.message.length<=300;
}
export function onScratchcardMessages(callback:(message:ScratchcardMessage)=>void):()=>void {
  const host=window.chrome?.webview;
  if(!host)return ()=>{};
  const listener=(event:MessageEvent)=>{
    if(isScratchcardMessage(event.data))callback(event.data);
  };
  host.addEventListener('message',listener);
  return ()=>host.removeEventListener('message',listener);
}
export const previewScratchcards:ScratchcardSnapshot={
  price:20,bankroll:1000,ticket:null,recoveryRequired:false,
};
