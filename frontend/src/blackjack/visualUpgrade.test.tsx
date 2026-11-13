import {describe,it,expect} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {PlayingCard} from './PlayingCard';
import {TableWagerChips,stackHeights} from './TableWagerChips';
import {PremiumSelect} from './PremiumSelect';
import {resolveCardSkin} from './cardSkins';

const ace={rank:'Ace',suit:'Spades',faceDown:false};
const hearts={rank:'Queen',suit:'Hearts',faceDown:false};
const render=(variant:string)=>renderToStaticMarkup(<PlayingCard
 card={ace} skin={resolveCardSkin(variant)} variant={variant}/>);
describe('real collector-card alignment',()=>{
 it('centers the suit separately from rank and ornamental filigree in premium sets',()=>{
  for(const variant of ['card-midnight-sovereign','card-solar-eclipse']){
   const html=render(variant);
   expect(html).toContain('bj-card-engraving');
   expect(html).toContain('bj-card-skin-crest');
   expect(html).toContain('bj-rank');
   expect(html).toContain('bj-index-pip');
   expect(html).toContain('bj-suit');
   expect(html).toContain('bj-corner-rank');
   expect(html).toContain('bj-corner-suit');
   expect(html).toContain('data-card-set="'+variant+'"');
   expect(html).toContain('role="img"');
   expect(html).toContain('Ace of Spades');
  }
  expect(render('card-midnight-sovereign')).toContain('bj-card-engraving-sovereign');
  expect(render('card-solar-eclipse')).toContain('bj-card-engraving-solar');
 });
 it('keeps red suits readable and never exposes dealer hole cards',()=>{
  const red=renderToStaticMarkup(<PlayingCard card={hearts}
    skin={resolveCardSkin('card-midnight-sovereign')}
    variant="card-midnight-sovereign"/>);
  expect(red).toContain('bj-card');
  expect(red).toContain('red');
  const hidden=renderToStaticMarkup(<PlayingCard card={{rank:null,suit:null,faceDown:true}}
    skin={resolveCardSkin('card-midnight-sovereign')}
    variant="card-midnight-sovereign"/>);
  expect(hidden).toContain('Face-down card');
  expect(hidden).not.toContain('Ace of Spades');
 });
});
describe('premium wager sculpture',()=>{
 it('uses 3D-look engraved SVG chips without interactive controls',()=>{
  const html=renderToStaticMarkup(<TableWagerChips wager="1000" animationsEnabled/>);
  expect(html).toContain('data-testid="blackjack-table-wager-chips"');
  expect(html).toContain('bj-wager-illustration');
  expect(html).toContain('bj-chip-face-emerald');
  expect(html).toContain('bj-chip-face-ruby');
  expect(html).toContain('bj-wager-facing-coin');
  expect(html).not.toContain('bj-wager-pixi');
  expect(html).toContain('aria-hidden="true"');
  expect(html).not.toContain('<button');
  expect(html).not.toContain('<input');
  expect((html.match(/bj-coin-layer/g)||[]).length).toBe(
   stackHeights('1000').reduce((n,k)=>n+k,0)
  );
 });
 it('keeps visual budget capped for extreme bets and supports reduced motion',()=>{
  expect(stackHeights('0')).toEqual([]);
  expect(stackHeights('-10')).toEqual([]);
  expect(stackHeights('banana')).toEqual([]);
  expect(stackHeights('Infinity')).toEqual([]);
  const total=(v:string)=>stackHeights(v).reduce((n,k)=>n+k,0);
  expect(total('5')).toBeLessThan(total('500'));
  expect(total('500')).toBeLessThan(total('500000'));
  expect(total('9999999999999')).toBeLessThanOrEqual(16);
  const still=renderToStaticMarkup(<TableWagerChips wager="2500" animationsEnabled={false}/>);
  expect(still).toContain('data-animation="false"');
 });
});
describe('pending action cannot visually disable the rest of the table',()=>{
 it('keeps a live dropdown trigger enabled while reporting its busy state to AT',()=>{
  const html=renderToStaticMarkup(<PremiumSelect label="Table preset" busy
    options={[{value:'x',label:'Casual 3:2'}]} value="x" onChange={()=>{}}/>);
  expect(html).toContain('aria-disabled="true"');
  expect(html).not.toContain('disabled=""');
  expect(html).toContain('Casual 3:2');
 });
});
