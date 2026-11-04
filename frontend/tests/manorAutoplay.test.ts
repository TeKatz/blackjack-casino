import { describe, expect, it } from 'vitest';
import { browserPreview } from '../src/bridge/protocol';
import { AUTO_PRESETS, BET_PRESETS, canAffordBet, shouldContinueAuto, validManorBet } from '../src/graphics/manorAutoplay';

describe('Manor preset stakes and safe automatic spins', () => {
  it('provides the six fixed bets and four automatic session lengths', () => {
    expect(BET_PRESETS).toEqual([50, 100, 250, 1000, 2500, 5000]);
    expect(AUTO_PRESETS).toEqual([10, 25, 50, 100]);
    expect(validManorBet('5000')).toBe(true);
    expect(validManorBet('2500')).toBe(true);
    for (const wrong of ['0', '5001', '1.5', '-1', '', '1e3', '99999'])
      expect(validManorBet(wrong)).toBe(false);
  });
  it('compares against the actual saved balance, never the displayed stake', () => {
    expect(canAffordBet('$5,010.20', '5000')).toBe(true);
    expect(canAffordBet('$4,999.99', '5000')).toBe(false);
    expect(canAffordBet('—', '50')).toBe(false);
    expect(canAffordBet('$250.00', '250')).toBe(true);
  });
  it('stops at zero spins, active bonuses, saved free spins and low funds', () => {
    const clean = {...browserPreview, bankroll:'$5,000.00', canSpin:true };
    expect(shouldContinueAuto(clean,'50',2)).toBe(true);
    expect(shouldContinueAuto(clean,'50',0)).toBe(false);
    expect(shouldContinueAuto({...clean, bankroll:'$49.99'},'50',2)).toBe(false);
    expect(shouldContinueAuto({...clean, cycleActive:true},'50',2)).toBe(false);
    expect(shouldContinueAuto({...clean, canFreeSpin:true},'50',2)).toBe(false);
    expect(shouldContinueAuto({...clean, canSpin:false},'50',2)).toBe(false);
    expect(shouldContinueAuto({...clean, run:{
      status:'Hunting',message:'',ghostClue:'',playerRoom:0,turn:0,exitTarget:250,
      current:'0×',banked:'0×',hasCandle:false,hasKey:false,hasCharm:false,
      rooms:[],passages:[],
    }},'50',2)).toBe(false);
  });
});
