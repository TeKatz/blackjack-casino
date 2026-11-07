import {describe,it,expect} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import type {TableSnapshot,TableRound} from './bridge';
import {tableVisualEvent,totalVisibleCards} from './TablePresentation';
import {PlayingCard} from './PlayingCard';
import {WinCelebration} from './WinCelebration';
import {resolveCardSkin} from './cardSkins';
import {SoftTableAudio} from './SoftTableAudio';

const card=(rank:string|null,suit:string|null,faceDown=false)=>({rank,suit,faceDown});
const start:TableRound={
 phase:'PlayerTurn',activeHandIndex:0,initialBet:'25',insuranceBet:'0',
 dealerCards:[card('Ace','Clubs'),card(null,null,true)],dealerValue:null,
 playerHands:[{index:0,bet:'25',value:16,isSoft:false,isSplitHand:false,
  isDoubled:false,isSurrendered:false,active:true,
  cards:[card('Ten','Spades'),card('Six','Hearts')],outcome:null,net:null}],
 aiSeats:[],totalNet:null,insuranceNet:null,
};
function snap(revision:number,round:TableRound|null,cardsRemaining=100):TableSnapshot{
 return {version:1,session:'session-1',revision,
  state:{round,cardsRemaining,shoeNumber:1}} as TableSnapshot;
}
describe('Blackjack presentation linked to native snapshots',()=>{
 it('never celebrates or plays cards on first load or duplicate revisions',()=>{
  expect(tableVisualEvent(null,snap(1,start))).toEqual({newRound:false,newCards:0,revealedCards:0,win:null});
  expect(tableVisualEvent(snap(1,start),snap(1,start)).newCards).toBe(0);
 });
 it('stages a new deal, incremental hit, and dealer card flip without inventing outcomes',()=>{
  const dealt=tableVisualEvent(snap(1,null),snap(2,start));
  expect(dealt.newRound).toBe(true);
  expect(dealt.newCards).toBe(4);
  expect(totalVisibleCards(start)).toBe(4);
  const hit={...start,playerHands:[{...start.playerHands[0],
    cards:[...start.playerHands[0].cards,card('Two','Diamonds')]}]};
  expect(tableVisualEvent(snap(2,start),snap(3,hit)).newCards).toBe(1);
  const reveal={...hit,dealerCards:[card('Ace','Clubs'),card('Seven','Spades')]};
  const e=tableVisualEvent(snap(3,hit),snap(4,reveal));
  expect(e.revealedCards).toBe(1);
  expect(e.newCards).toBe(0);
  expect(e.win).toBeNull();
 });
 it('fires exactly once for an actual positive native settlement',()=>{
  const won={...start,phase:'Settled',totalNet:'37.50',
   dealerCards:[card('Ace','Clubs'),card('Seven','Spades')],
   playerHands:[{...start.playerHands[0],active:false,
     outcome:'Blackjack',net:'37.50'}]};
  const e=tableVisualEvent(snap(3,start),snap(4,won));
  expect(e.win).toEqual({net:'37.50',blackjack:true});
  expect(tableVisualEvent(snap(4,won),snap(4,won)).win).toBeNull();
  const loss={...won,totalNet:'-25',playerHands:[{...won.playerHands[0],net:'-25',outcome:'Loss'}]};
  expect(tableVisualEvent(snap(3,start),snap(4,loss)).win).toBeNull();
 });
 it('reopens a previously settled table without awarding any client side result',()=>{
  const ended={...start,phase:'Settled',totalNet:'15'};
  expect(tableVisualEvent(null,snap(2,ended)).win).toBeNull();
 });
});
describe('premium card and win presentation',()=>{
 it('keeps hole-card values hidden and renders a real flip structure',()=>{
  const hidden=renderToStaticMarkup(<PlayingCard card={card(null,null,true)}
    skin={resolveCardSkin('card-celestial-opal')}/>);
  expect(hidden).toContain('data-facedown="true"');
  expect(hidden).toContain('bj-card-flip');
  expect(hidden).not.toContain('Ace of Clubs');
  const face=renderToStaticMarkup(<PlayingCard card={card('Ace','Clubs')}
    skin={resolveCardSkin('card-classic')} dealIndex={2}/>);
  expect(face).toContain('Ace of Clubs');
  expect(face).toContain('--card-arrival-delay:150ms');
 });
 it('localizes the win plaque and shows only supplied C# net',()=>{
  const html=renderToStaticMarkup(<WinCelebration moment={{id:7,net:'50',blackjack:true}} language="de"/>);
  expect(html).toContain('BLACKJACK!');
  expect(html).toContain('+$50');
  expect(html).toContain('blackjack-win-celebration');
  expect(renderToStaticMarkup(<WinCelebration moment={null} language="en"/>)).toBe('');
 });
 it('allows silent fallback when no audio output device is available',()=>{
  const audio=new SoftTableAudio();
  expect(()=>{audio.arm(false);audio.cards(2,false);audio.chip(false);audio.win(false,false);audio.dispose();}).not.toThrow();
 });
});
