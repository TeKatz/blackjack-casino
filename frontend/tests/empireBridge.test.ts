import {describe,it,expect} from 'vitest';
import {decimalInput,fractionToPercent,isEmpireSnapshot,isReply,percentToFraction,retainRooms} from '../src/empire/empireBridge';
import fixture from './fixtures/empire-snapshot.json';

describe('Empire bridge contract',()=>{
  it('accepts the complete native snapshot and rejects incomplete or unsupported state',()=>{
    expect(isEmpireSnapshot(fixture)).toBe(true);
    for(const value of [null,{}, {...fixture,version:2},{...fixture,revision:-1},
      {...fixture,state:{...fixture.state,economy:{...fixture.state.economy,credits:1e27}}},
      {...fixture,state:{...fixture.state,economy:{...fixture.state.economy,rooms:[{}]}}},
      {...fixture,state:{...fixture.state,canClaimDividend:undefined}},
      {...fixture,lastReply:{requestId:'wrong'}}])expect(isEmpireSnapshot(value)).toBe(false);
  });
  it('rejects malformed receipts without unlocking purchases',()=>{
    expect(isReply({requestId:'a',success:true,message:'Saved',revision:1,preview:null})).toBe(true);
    expect(isReply({requestId:'a',success:true,message:'Saved',revision:NaN,preview:null})).toBe(false);
    expect(isReply({requestId:'a',success:true,message:'Saved',revision:1,preview:{}})).toBe(false);
  });
  it('preserves exact construction reserve and German decimals',()=>{
    expect(decimalInput('1000000000000000000000000000')).toBe('1000000000000000000000000000');
    expect(decimalInput('1000000,50')).toBe('1000000.50');
    expect(decimalInput('1.000,50')).toBeNull();
    expect(decimalInput('-1')).toBeNull();
    expect(decimalInput('NaN')).toBeNull();
  });
  it('converts automation percentages with decimal shifts',()=>{
    expect(percentToFraction('12,5')).toBe('0.125');
    expect(percentToFraction('1.1')).toBe('0.011');
    expect(percentToFraction('50')).toBe('0.50');
    expect(percentToFraction('51')).toBeNull();
    expect(percentToFraction('0')).toBeNull();
    expect(fractionToPercent('0.125')).toBe('12.5');
    expect(fractionToPercent('0.011')).toBe('1.1');
  });
  it('keeps unchanged room objects across production ticks',()=>{
    if(!isEmpireSnapshot(fixture))throw new Error('fixture');
    const rooms=fixture.state.economy.rooms;
    const next=structuredClone(rooms);
    expect(retainRooms(rooms,next)[0]).toBe(rooms[0]);
    next[0].level++;
    expect(retainRooms(rooms,next)[0]).not.toBe(rooms[0]);
  });
});
