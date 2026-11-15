import {describe,expect,it} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {RubyReelStage} from '../src/indianRuby/RubyReelStage';
import {baseReelDuration,baseReelTape,bonusCellDuration,bonusCellTape,
 bonusKeyframes,reelKeyframes} from '../src/indianRuby/reelMotion';
import type {SymbolId} from '../src/indianRuby/bridge';

const spin='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
const from:SymbolId[]=['Ace','King','Queen'];
const to:SymbolId[]=['Elephant','Ruby','Spices'];
const grid:SymbolId[]=[
 'Ace','Queen','Vase','Ten','Elephant',
 'Necklace','Ruby','King','Jack','Queen',
 'Ten','Spices','Ace','Vase','King',
];

describe('Indian Ruby premium reel compositor',()=>{
 it('uses true five-reel, staggered deterministic spin tapes that land on native targets',()=>{
  const durations=Array.from({length:5},(_,reel)=>baseReelDuration(reel));
  expect(durations).toEqual([850,1140,1430,1720,2010]);
  expect(()=>baseReelDuration(5)).toThrow();
  for(let reel=0;reel<5;reel++){
   const strip=baseReelTape(spin,reel,from,to);
   expect(strip.slice(1,4)).toEqual(from);
   expect(strip.slice(-4,-1)).toEqual(to);
   expect(strip).toEqual(baseReelTape(spin,reel,from,to));
   const frames=reelKeyframes(80,strip.length);
   expect(frames[0].transform).toBe('translate3d(0,-80px,0)');
   expect(frames.at(-1)?.transform)
    .toBe(`translate3d(0,${-(strip.length-4)*80}px,0)`);
  }
 });
 it('animates only unlocked bonus cells to the authoritative final symbol',()=>{
  const strip=bonusCellTape(spin,6,'Queen','Elephant');
  expect(strip[0]).toBe('Queen');
  expect(strip.slice(-2)).toEqual(['Elephant','Elephant']);
  expect(strip).toEqual(bonusCellTape(spin,6,'Queen','Elephant'));
  expect(bonusCellDuration(0)).toBeLessThan(bonusCellDuration(14));
  expect(()=>bonusCellDuration(-1)).toThrow();
  expect(bonusKeyframes(65,strip.length).at(-1)?.transform)
   .toBe(`translate3d(0,${-(strip.length-2)*65}px,0)`);
 });
 it('keeps fifteen illustrated cells in five stable columns with explicit held positions',()=>{
  const html=renderToStaticMarkup(<RubyReelStage displayGrid={grid}
   targetBase={null} targetBonus={null} spinId={null}
   rollingReels={[]} rollingCells={[]} heldCells={[4,6]} winningLines={[]}
   showWins={false} anticipation={false} reducedMotion={false}
   onBaseStop={()=>{}} onBonusStop={()=>{}}/>);
  expect(html).toContain('data-reel-engine="five-reel-compositor"');
  expect((html.match(/class="ruby-reel-column"/g)??[]).toHaveLength(5);
  expect((html.match(/class="ruby-cell /g)??[]).toHaveLength(15);
  expect((html.match(/data-held="true"/g)??[])).toHaveLength(2);
  expect(html).not.toContain('🐘');
 });
});
