import type {TableSnapshot,TableRound} from './bridge';

/** Visual/sound events are derived from accepted native snapshots only.
 * No client-side prediction of cards, payout, or round outcomes.
 */
export type TableVisualEvent={
  newRound:boolean;newCards:number;revealedCards:number;
  win:null|{net:string;blackjack:boolean};
};
export function totalVisibleCards(round:TableRound|null):number{
  if(!round)return 0;
  return round.dealerCards.length+
    round.playerHands.reduce((n,h)=>n+h.cards.length,0)+
    round.aiSeats.reduce((n,seat)=>n+seat.hands.reduce((count,h)=>count+h.cards.length,0),0);
}
export function isFreshRound(before:TableSnapshot,after:TableSnapshot):boolean{
  const prev=before.state.round,next=after.state.round;
  if(!next)return false;
  if(!prev)return true;
  // Dealer can resolve a natural blackjack in the same command as Deal.
  return prev.phase==='Settled'&&(
    next.phase!=='Settled'||before.state.shoeNumber!==after.state.shoeNumber
    ||before.state.cardsRemaining!==after.state.cardsRemaining
  );
}
export function tableVisualEvent(before:TableSnapshot|null,after:TableSnapshot):TableVisualEvent{
  const silent:TableVisualEvent={newRound:false,newCards:0,revealedCards:0,win:null};
  if(!before||before.session!==after.session||before.revision>=after.revision)return silent;
  const prev=before.state.round,next=after.state.round;
  const newRound=isFreshRound(before,after);
  if(!next)return silent;
  const revealedCards=newRound||!prev?0:next.dealerCards.filter((c,i)=>
    !c.faceDown&&prev.dealerCards[i]?.faceDown===true).length;
  const newCards=newRound?totalVisibleCards(next):
    Math.max(0,totalVisibleCards(next)-totalVisibleCards(prev));
  const settled=next.phase==='Settled'&&(newRound||prev?.phase!=='Settled');
  const positive=next.totalNet!==null&&Number(next.totalNet)>0;
  const win=settled&&positive?{
    net:next.totalNet!,
    blackjack:next.playerHands.some(h=>h.outcome==='Blackjack'),
  }:null;
  return {newRound,newCards,revealedCards,win};
}
