import {describe,expect,it} from 'vitest';
import {commandCanBuy,commandImpact,commandMilestone,commandProgress,commandPurchaseSizes,commandQuote,decimalDifference,researchCoordinates} from '../src/empire/commandCenterModel';
import {isEmpireSnapshot} from '../src/empire/empireBridge';
import type {Preview,Research} from '../src/empire/empireBridge';
import fixture from './fixtures/empire-snapshot.json';

describe('Command Center rules',()=>{
  it('uses only authoritative Core quotes and never invents buy-max levels',()=>{
    expect(isEmpireSnapshot(fixture)).toBe(true);
    if(!isEmpireSnapshot(fixture))return;
    const room=fixture.state.economy.rooms[0];
    expect(commandPurchaseSizes).toEqual([1,5,10,25,50,100]);
    expect(commandQuote(room,10)).toBe(room.quotes.find(q=>q.requestedLevels===10));
    expect(commandQuote(room,9)).toBeUndefined();
    expect(commandCanBuy(room,9,false)).toBe(false);
    expect(commandCanBuy(room,10,true)).toBe(false);
    expect(commandCanBuy(room,10,false)).toBe(!!room.quotes.find(q=>q.requestedLevels===10)?.canBuy);
  });
  it('makes level-200 and ten-level milestones explicit',()=>{
    expect(commandMilestone(0)).toEqual({next:10,remaining:10,progress:0});
    expect(commandMilestone(19)).toEqual({next:20,remaining:1,progress:90});
    expect(commandMilestone(200)).toEqual({next:null,remaining:0,progress:100});
    expect(commandProgress(3,4)).toBe(75);
    expect(commandProgress(4,0)).toBe(100);
  });
  it('subtracts huge and fractional Core decimals without rounding errors',()=>{
    expect(decimalDifference('10000000000000000000000000.03','9999999999999999999999999.99')).toBe('0.04');
    expect(decimalDifference('0.01','1.10')).toBe('-1.09');
    expect(decimalDifference('2','2.000')).toBe('0');
  });
  it('computes ROI from the native preview; non-productive actions do not invent ROI',()=>{
    const base={budgetBefore:'2000',budgetAfter:'1800',portfolioBefore:'100',portfolioAfter:'120',
      levelBefore:20,levelAfter:30,roomBefore:{final:'40'},roomAfter:{final:'60'}} as Preview;
    expect(commandImpact(base)).toMatchObject({capital:'-200',propertyIncome:'20',portfolioIncome:'20',paybackSeconds:10,incomePercent:20});
    expect(commandImpact({...base,portfolioAfter:'100'}).paybackSeconds).toBeNull();
  });
  it('positions research nodes deterministically and respects prerequisite links',()=>{
    const research=[{id:'first',prerequisite:null},{id:'second',prerequisite:'first'},{id:'third',prerequisite:'second'}] as Research[];
    const a=researchCoordinates(research),b=researchCoordinates(research);
    expect(a).toEqual(b);
    expect(a.first.x).toBeLessThan(a.second.x);
    expect(a.second.x).toBeLessThan(a.third.x);
    expect(a.third.y).toBe(50);
  });
});
