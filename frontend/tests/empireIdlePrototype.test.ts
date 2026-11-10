import {describe,expect,it} from 'vitest';
import {canPurchase,milestoneDistance,milestoneProgress,nextMilestone,prestigeProgress,prototypePurchaseSizes,upgradeQuote} from '../src/empire/idlePrototypeModel';
import {isEmpireSnapshot} from '../src/empire/empireBridge';
import fixture from './fixtures/empire-snapshot.json';

describe('Empire idle-management prototype',()=>{
  it('uses only authoritative native purchase quotes and supported bulk quantities',()=>{
    expect(isEmpireSnapshot(fixture)).toBe(true);
    if(!isEmpireSnapshot(fixture))return;
    const room=fixture.state.economy.rooms[0];
    expect(prototypePurchaseSizes).toEqual([1,5,10,25,50,100]);
    expect(upgradeQuote(room,1)).toEqual(room.quotes.find(q=>q.requestedLevels===1));
    expect(upgradeQuote(room,3)).toBeUndefined();
    expect(canPurchase(room,1,true)).toBe(false);
    expect(canPurchase(room,3,false)).toBe(false);
    expect(canPurchase(room,1,false)).toBe(!!room.quotes[0]?.canBuy);
  });
  it('shows accurate milestone boundaries and a capped maximum',()=>{
    expect(nextMilestone(0)).toBe(10);
    expect(nextMilestone(9)).toBe(10);
    expect(nextMilestone(10)).toBe(20);
    expect(nextMilestone(199)).toBe(200);
    expect(nextMilestone(200)).toBeNull();
    expect(milestoneDistance(199)).toBe(1);
    expect(milestoneDistance(200)).toBe(0);
    expect(milestoneProgress(0)).toBe(0);
    expect(milestoneProgress(9)).toBe(90);
    expect(milestoneProgress(200)).toBe(100);
  });
  it('clamps prestige progress without granting fictional levels',()=>{
    expect(prestigeProgress(250,500)).toBe(50);
    expect(prestigeProgress(700,500)).toBe(100);
    expect(prestigeProgress(0,0)).toBe(100);
    expect(prestigeProgress(-1,500)).toBe(0);
  });
});
