import {describe,it,expect} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import type {TableState} from './bridge';
import {PremiumSelect,moveSelectIndex,placeSelect} from './PremiumSelect';
import {TableWagerChips,stackHeights} from './TableWagerChips';
import {PlayingCard} from './PlayingCard';
import {CustomRulesEditor} from './CustomRulesEditor';
import {resolveCardSkin} from './cardSkins';

const sum=(numbers:number[])=>numbers.reduce((n,i)=>n+i,0);
describe('Blackjack premium dropdowns',()=>{
 it('renders a keyboard-operable custom trigger instead of an OS select',()=>{
  const html=renderToStaticMarkup(<PremiumSelect value="s17" label="Table preset"
    onChange={()=>{}} options={[{value:'s17',label:'Casual 3:2'},{value:'h17',label:'Vegas'}]}/>);
  expect(html).toContain('aria-haspopup="listbox"');
  expect(html).toContain('aria-expanded="false"');
  expect(html).toContain('Casual 3:2');
  expect(html).not.toContain('<select');
  expect(html).not.toContain('<option');
 });
 it('opens above the trigger when necessary, keeps a visible popup, and skips disabled options',()=>{
  const above=placeSelect({left:123,right:330,width:207,top:650,bottom:690},
   {width:900,height:720},8);
  expect(above.above).toBe(true);
  expect(above.top).toBeLessThan(650);
  expect(above.maxHeight).toBeGreaterThan(60);
  const below=placeSelect({left:20,right:260,width:240,top:28,bottom:68},
   {width:900,height:720},5);
  expect(below.above).toBe(false);
  expect(below.top).toBeGreaterThan(68);
  const options=[{value:'a',label:'A'},{value:'b',label:'B',disabled:true},{value:'c',label:'C'}];
  expect(moveSelectIndex(0,1,options)).toBe(2);
  expect(moveSelectIndex(2,-1,options)).toBe(0);
 });
 it('replaces every native select in the full custom-rules editor',()=>{
  const rules={
    deckCount:6,dealerHitsSoft17:false,dealerPeeksForBlackjack:true,
    dealerTakesHoleCard:true,allowInsurance:true,allowLateSurrender:true,
    doubleRule:'AnyTwo',doubleAfterSplit:true,resplitAces:false,hitSplitAces:false,
    maxSplitHands:4,blackjackPayout:'1.5',penetration:.75,
    minimumBet:'1',maximumBet:'5000',
  } satisfies TableState['rules'];
  const html=renderToStaticMarkup(<CustomRulesEditor initial={rules} language="en"
   onClose={()=>{}} onSave={()=>{}}/>);
  expect(html.match(/aria-haspopup="listbox"/g)?.length).toBe(7);
  expect(html).not.toContain('<select');
  expect(html).toContain('blackjack-rules-save');
 });
});
describe('decorative native-wager chips',()=>{
 it('scales monotonically with the confirmed wager but stays bounded',()=>{
  expect(stackHeights('0')).toEqual([]);
  expect(stackHeights('-3')).toEqual([]);
  expect(stackHeights('garbage')).toEqual([]);
  const small=sum(stackHeights('5')),mid=sum(stackHeights('250')),large=sum(stackHeights('5000'));
  expect(small).toBeGreaterThan(0);
  expect(mid).toBeGreaterThan(small);
  expect(large).toBeGreaterThan(mid);
  expect(sum(stackHeights('1000000000'))).toBeLessThanOrEqual(16);
 });
 it('creates neither input nor buttons, and allows disabling visual motion',()=>{
  const html=renderToStaticMarkup(<TableWagerChips wager="1000" animationsEnabled={false}/>);
  expect(html).toContain('data-testid="blackjack-table-wager-chips"');
  expect(html).toContain('data-animation="false"');
  expect(html).toContain('aria-hidden="true"');
  expect(html).not.toContain('<button');
  expect(html).not.toContain('<input');
  expect(renderToStaticMarkup(<TableWagerChips wager="0" animationsEnabled/>)).toBe('');
 });
});
describe('distinct collector cards',()=>{
 it('renders different structural identities for Midnight Sovereign and Solar Eclipse',()=>{
  const card={rank:'Ace',suit:'Spades',faceDown:false};
  const sovereign=renderToStaticMarkup(<PlayingCard card={card}
    variant="card-midnight-sovereign" skin={resolveCardSkin('card-midnight-sovereign')}/>);
  const eclipse=renderToStaticMarkup(<PlayingCard card={card}
    variant="card-solar-eclipse" skin={resolveCardSkin('card-solar-eclipse')}/>);
  expect(sovereign).toContain('data-card-set="card-midnight-sovereign"');
  expect(eclipse).toContain('data-card-set="card-solar-eclipse"');
  expect(sovereign).toContain('bj-card-skin-crest');
  expect(eclipse).toContain('bj-card-skin-crest');
  expect(sovereign).not.toEqual(eclipse);
  expect(resolveCardSkin('card-solar-eclipse').accent).not.toBe(resolveCardSkin('card-midnight-sovereign').accent);
 });
});
