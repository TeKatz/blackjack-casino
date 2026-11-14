import {describe,expect,it} from 'vitest';
import {filterMotionPhases} from './LobbyFilterMotion';
import {isConfirmedDailyClaim} from './DailyClaimEffects';
import {fallbackLobby} from './lobbyBridge';

describe('lobby filter transitions',()=>{
  const ids=['blackjack','roulette','kniffel','chill-lounge','casino-empire',
    'midnight-reels','neon-vault','nine-lives','midnight-manor','midnight-merge'];
  it('partitions visible elements without remounting their identities',()=>{
    const change=filterMotionPhases(ids,'all','slots');
    expect(change.staying).toEqual(['midnight-reels','neon-vault','nine-lives','midnight-manor']);
    expect(change.entering).toEqual([]);
    expect(change.leaving).toContain('blackjack');
    expect([...change.staying,...change.leaving].sort()).toEqual([...ids].sort());
  });
  it('knows which tiles appear when returning to the lobby',()=>{
    const change=filterMotionPhases(ids,'tables','all');
    expect(change.leaving).toEqual([]);
    expect(change.staying).toEqual(['blackjack','roulette','kniffel']);
    expect(change.entering).toContain('midnight-manor');
  });
});
describe('daily claim confirmation',()=>{
  const initial=fallbackLobby.dailyLogin;
  const pending={totalClaims:initial.totalClaims,day:initial.activeStep,bankrollValue:1000};
  it('never celebrates a click, stale snapshot, or failed claim',()=>{
    expect(isConfirmedDailyClaim(null,initial)).toBe(false);
    expect(isConfirmedDailyClaim(pending,initial)).toBe(false);
    expect(isConfirmedDailyClaim(pending,{...initial,canClaim:false,claimedToday:true})).toBe(false);
    expect(isConfirmedDailyClaim(pending,{...initial,totalClaims:1,canClaim:true})).toBe(false);
  });
  it('celebrates only a host-confirmed increase with claimedToday',()=>{
    expect(isConfirmedDailyClaim(pending,{...initial,totalClaims:1,canClaim:false,claimedToday:true})).toBe(true);
  });
});
