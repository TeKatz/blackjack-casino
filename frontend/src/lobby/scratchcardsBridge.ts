/** Lobby scratchcards: Windows owns tickets, prices, RNG, wallet and payouts. */
export type ScratchcardGame = {
  id:string;name:string;price:number;prizeMultiplier:number;
};
export type ScratchcardTicket = {
  ticketId:string;gameId:string;symbols:number[];prize:number;collected:boolean;bankrollAfter:number;
};
export type ScratchcardSnapshot = {
  /** Original entry price, retained for compatibility with older builds. */
  price:number;games:ScratchcardGame[];
  bankroll:number;ticket:ScratchcardTicket|null;recoveryRequired:boolean;
};
export type ScratchcardMessage =
  | {type:'lobby.scratchcards.snapshot';data:ScratchcardSnapshot}
  | {type:'lobby.scratchcards.result';data:{action:'buy'|'collect';success:boolean;message:string}};

const isRecord=(value:unknown):value is Record<string,unknown> =>
  typeof value==='object'&&value!==null&&!Array.isArray(value);
const amount=(value:unknown)=>typeof value==='number'&&Number.isFinite(value)&&value>=0;
const expectedPrizes=[0,10,20,50,100,250,1000];
export function isScratchcardSnapshot(value:unknown):value is ScratchcardSnapshot {
  if(!isRecord(value)||value.price!==20||!amount(value.bankroll)||
    typeof value.recoveryRequired!=='boolean'||
    !Array.isArray(value.games)||value.games.length<1||value.games.length>12)return false;
  const games=value.games;
  const ids=new Set<string>();
  for(const raw of games){
    if(!isRecord(raw)||typeof raw.id!=='string'||!(/^[a-z0-9-]{1,48}$/).test(raw.id)||
      ids.has(raw.id)||typeof raw.name!=='string'||raw.name.length<1||raw.name.length>80||
      !amount(raw.price)||raw.price===0||!Number.isInteger(raw.prizeMultiplier)||
      (raw.prizeMultiplier as number)<1||(raw.prizeMultiplier as number)>1000)return false;
    ids.add(raw.id);
  }
  if(value.ticket===null)return true;
  const ticket=value.ticket;
  if(!isRecord(ticket)||typeof ticket.ticketId!=='string'||
    !/^[a-f0-9-]{36}$/i.test(ticket.ticketId)||typeof ticket.gameId!=='string'||
    !Array.isArray(ticket.symbols)||ticket.symbols.length!==9||
    !ticket.symbols.every(symbol=>Number.isInteger(symbol)&&symbol>=0&&symbol<5)||
    typeof ticket.collected!=='boolean'||!amount(ticket.bankrollAfter))return false;
  const game=games.find(candidate=>candidate.id===ticket.gameId);
  return !!game&&expectedPrizes.some(prize=>prize*game.prizeMultiplier===ticket.prize);
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
  price:20,
  games:[
    {id:'lucky-sevens',name:'Lucky Sevens',price:20,prizeMultiplier:1},
    {id:'gold-rush',name:'Gold Rush',price:100,prizeMultiplier:5},
    {id:'platinum-vault',name:'Platinum Vault',price:500,prizeMultiplier:25},
    {id:'diamond-royale',name:'Diamond Royale',price:2500,prizeMultiplier:125},
  ],
  bankroll:1000,ticket:null,recoveryRequired:false,
};
