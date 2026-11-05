import {describe,expect,it} from 'vitest';
import {isTableReply,isTableSnapshot} from '../src/blackjack/bridge';

const face={rank:'Ten',suit:'Spades',faceDown:false};
const concealed={rank:null,suit:null,faceDown:true};
const snapshot={
  version:1,session:'epoch',revision:0,state:{
    bankroll:'975',selectedBet:'25',shoeNumber:1,cardsRemaining:307,
    fullTable:false,aiSeatCount:0,preset:'Casual',theme:'ClassicGreen',
    cardSetId:'card-classic',soundEnabled:true,animationsEnabled:true,status:'Ready',
    rules:{deckCount:6,dealerHitsSoft17:false,dealerPeeksForBlackjack:true,
      dealerTakesHoleCard:true,allowInsurance:true,allowLateSurrender:true,
      doubleRule:'AnyTwo',doubleAfterSplit:true,resplitAces:false,hitSplitAces:false,
      maxSplitHands:4,blackjackPayout:'1.5',minimumBet:'1',maximumBet:'5000'},
    presets:[{id:'Casual',name:'Casual',description:'Table'}],
    canDeal:false,availableActions:['Hit','Stand'],
    round:{phase:'PlayerTurn',activeHandIndex:0,initialBet:'25',insuranceBet:'0',
      dealerCards:[face,concealed],dealerValue:null,
      playerHands:[{index:0,bet:'25',value:20,isSoft:false,isSplitHand:false,
        isDoubled:false,isSurrendered:false,active:true,cards:[face,face],outcome:null,net:null}],
      aiSeats:[],totalNet:null,insuranceNet:null},
  },
};
describe('Blackjack modern transport',()=>{
  it('validates snapshot content and refuses a face-down card with disclosed rank',()=>{
    expect(isTableSnapshot(snapshot)).toBe(true);
    expect(isTableSnapshot({...snapshot,version:2})).toBe(false);
    expect(isTableSnapshot({...snapshot,state:{...snapshot.state,bankroll:975}})).toBe(false);
    expect(isTableSnapshot({...snapshot,state:{...snapshot.state,round:{...snapshot.state.round,
      dealerCards:[face,{rank:'King',suit:'Clubs',faceDown:true}]}}})).toBe(false);
  });
  it('requires well-formed native acknowledgements',()=>{
    expect(isTableReply({requestId:'id',success:true,message:'ok',revision:1})).toBe(true);
    expect(isTableReply({requestId:'id',success:true,message:'ok',revision:-1})).toBe(false);
    expect(isTableReply({requestId:'id',success:true,message:'ok',revision:1.5})).toBe(false);
  });
});
