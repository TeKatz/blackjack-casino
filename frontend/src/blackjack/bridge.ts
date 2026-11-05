import type {} from '../bridge/protocol';

// The browser may request a table action, but cannot supply game outcomes.
export type CardFace = {rank:string|null;suit:string|null;faceDown:boolean};
export type Hand = {index:number;bet:string;value:number;isSoft:boolean;isSplitHand:boolean;
  isDoubled:boolean;isSurrendered:boolean;active:boolean;cards:CardFace[];
  outcome:string|null;net:string|null};
export type TableRound = {phase:string;activeHandIndex:number;initialBet:string;insuranceBet:string;
  dealerCards:CardFace[];dealerValue:number|null;playerHands:Hand[];
  aiSeats:{name:string;hands:{index:number;bet:string;value:number;cards:CardFace[];outcome:string|null}[]}[];
  totalNet:string|null;insuranceNet:string|null};
export type TableState = {bankroll:string;selectedBet:string;shoeNumber:number;cardsRemaining:number;
  fullTable:boolean;aiSeatCount:number;preset:string;theme:string;cardSetId:string;cardVariant:string;
  soundEnabled:boolean;animationsEnabled:boolean;status:string;
  rules:{deckCount:number;dealerHitsSoft17:boolean;dealerPeeksForBlackjack:boolean;
    dealerTakesHoleCard:boolean;allowInsurance:boolean;allowLateSurrender:boolean;
    doubleRule:string;doubleAfterSplit:boolean;resplitAces:boolean;hitSplitAces:boolean;
    maxSplitHands:number;blackjackPayout:string;penetration:number;minimumBet:string;maximumBet:string};
  presets:{id:string;name:string;description:string}[];
  canDeal:boolean;availableActions:string[];round:TableRound|null};
export type TableSnapshot={version:1;session:string;revision:number;state:TableState};
export type TableReply={requestId:string;success:boolean;message:string;revision:number};
export type TableIntent={action:string;value?:string};
const record=(x:unknown):x is Record<string,unknown>=>typeof x==='object'&&x!==null&&!Array.isArray(x);
const integer=(x:unknown):x is number=>typeof x==='number'&&Number.isSafeInteger(x)&&x>=0;
const money=(x:unknown):x is string=>typeof x==='string'&&/^(?:0|[1-9]\d*)(?:\.\d+)?$/.test(x);
const signed=(x:unknown):x is string=>typeof x==='string'&&/^-?(?:0|[1-9]\d*)(?:\.\d+)?$/.test(x);
const nullable=(x:unknown)=>x===null||typeof x==='string';
const cards=(x:unknown):x is CardFace[]=>Array.isArray(x)&&x.every(c=>record(c)&&
  typeof c.faceDown==='boolean'&&(c.faceDown
    ?c.rank===null&&c.suit===null:typeof c.rank==='string'&&typeof c.suit==='string'));
function hand(x:unknown):x is Hand{
  return record(x)&&integer(x.index)&&money(x.bet)&&integer(x.value)&&
    ['isSoft','isSplitHand','isDoubled','isSurrendered','active'].every(k=>typeof x[k]==='boolean')&&
    cards(x.cards)&&nullable(x.outcome)&&(x.net===null||signed(x.net));
}
function round(x:unknown):x is TableRound{
  return record(x)&&typeof x.phase==='string'&&integer(x.activeHandIndex)&&money(x.initialBet)&&
    money(x.insuranceBet)&&cards(x.dealerCards)&&(x.dealerValue===null||integer(x.dealerValue))&&
    Array.isArray(x.playerHands)&&x.playerHands.every(hand)&&
    Array.isArray(x.aiSeats)&&x.aiSeats.every(seat=>record(seat)&&typeof seat.name==='string'&&
      Array.isArray(seat.hands)&&seat.hands.every(h=>record(h)&&integer(h.index)&&money(h.bet)&&
        integer(h.value)&&cards(h.cards)&&nullable(h.outcome)))&&
    (x.totalNet===null||signed(x.totalNet))&&(x.insuranceNet===null||signed(x.insuranceNet));
}
export function isTableSnapshot(x:unknown):x is TableSnapshot{
  if(!record(x)||x.version!==1||typeof x.session!=='string'||!integer(x.revision)||!record(x.state))return false;
  const s=x.state;
  if(!money(s.bankroll)||!money(s.selectedBet)||!integer(s.shoeNumber)||!integer(s.cardsRemaining)||
    typeof s.fullTable!=='boolean'||!integer(s.aiSeatCount)||s.aiSeatCount>5||
    !['preset','theme','cardSetId','cardVariant','status'].every(k=>typeof s[k]==='string')||
    !['soundEnabled','animationsEnabled','canDeal'].every(k=>typeof s[k]==='boolean')||
    !Array.isArray(s.availableActions)||!s.availableActions.every((a:unknown)=>typeof a==='string')||
    !Array.isArray(s.presets)||!s.presets.every((p:unknown)=>record(p)&&
      ['id','name','description'].every(k=>typeof p[k]==='string'))||!record(s.rules))return false;
  const rules=s.rules;
  return integer(rules.deckCount)&&integer(rules.maxSplitHands)&&typeof rules.penetration==='number'&&Number.isFinite(rules.penetration)&&rules.penetration>0&&rules.penetration<1&&
    ['dealerHitsSoft17','dealerPeeksForBlackjack','dealerTakesHoleCard','allowInsurance',
      'allowLateSurrender','doubleAfterSplit','resplitAces','hitSplitAces'].every(k=>typeof rules[k]==='boolean')&&
    typeof rules.doubleRule==='string'&&money(rules.blackjackPayout)&&
    money(rules.minimumBet)&&money(rules.maximumBet)&&(s.round===null||round(s.round));
}
export function isTableReply(x:unknown):x is TableReply{
  return record(x)&&typeof x.requestId==='string'&&typeof x.success==='boolean'&&
    typeof x.message==='string'&&integer(x.revision);
}
export function send(snapshot:TableSnapshot,intent:TableIntent):string{
  const requestId=crypto.randomUUID();
  window.chrome?.webview?.postMessage({type:'blackjack.command',version:1,
    session:snapshot.session,expectedRevision:snapshot.revision,requestId,...intent});
  return requestId;
}
export function listen(receive:(message:{type:'snapshot';data:TableSnapshot}|
  {type:'result';data:TableReply})=>void):()=>void{
  const host=window.chrome?.webview;
  const callback=(event:MessageEvent)=>{
    const m:unknown=event.data;
    if(!record(m))return;
    if(m.type==='blackjack.snapshot'&&isTableSnapshot(m.data))
      receive({type:'snapshot',data:m.data});
    else if(m.type==='blackjack.result'&&isTableReply(m.data))
      receive({type:'result',data:m.data});
  };
  host?.addEventListener('message',callback);
  return()=>host?.removeEventListener('message',callback);
}
