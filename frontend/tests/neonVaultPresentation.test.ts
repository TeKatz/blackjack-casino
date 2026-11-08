import {describe,it,expect} from 'vitest';
import type {NeonSpin} from '../src/neonVault/bridge';
import {neonImpact,isImpactful,impactColor} from '../src/neonVault/presentation';

const plain:NeonSpin={
 id:'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',bet:'100',stake:'100',
 bankrollBefore:'10000',bankrollAfter:'9900',
 baseGrid:Array.from({length:20},()=> 'Pulse'),
 finalGrid:Array.from({length:20},()=> 'Pulse'),
 blackout:'None',blackoutWildCells:[],blackoutFullReel:null,
 expandedReels:[],securityOverrideReel:null,
 nearMiss:'None',jackpot:'None',waysPayout:'0',scatterPayout:'0',
 jackpotPayout:'0',payout:'0',net:'-100',waysWon:0,freeSpinsAwarded:0,
 freeSpinsRemaining:0,vaultEnergyAfter:0,isFreeSpin:false,
 betTier:'HighRoller',totalMultiplier:'0',
 playerXp:20,seasonXp:15,levelUp:false,
};
describe('Neon Vault cabinet reactions',()=>{
 it('does not invent wins, jackpot or warnings for a normal result',()=>{
  expect(neonImpact(plain)).toMatchObject({kind:'idle',winTier:'none',alarm:false});
  expect(isImpactful(neonImpact(plain))).toBe(false);
 });
 it('assigns a graded full-cabinet reaction to real win multipliers',()=>{
  expect(neonImpact({...plain,totalMultiplier:'9.99',payout:'999'}).kind).toBe('idle');
  expect(neonImpact({...plain,totalMultiplier:'10',payout:'1000'})).toMatchObject({
   kind:'big-win',strength:2,winTier:'big'});
  expect(neonImpact({...plain,totalMultiplier:'50',payout:'5000'})).toMatchObject({
   kind:'mega-win',strength:3,winTier:'mega'});
  expect(neonImpact({...plain,jackpot:'Grand',payout:'100000'})).toMatchObject({
   kind:'jackpot',winTier:'mega',strength:3});
 });
 it('keeps security events prioritized even if the host also paid a huge win',()=>{
  const total=neonImpact({...plain,blackout:'Total',totalMultiplier:'75',payout:'7500'});
  expect(total).toMatchObject({kind:'total-blackout',winTier:'mega',alarm:true,strength:3});
  expect(neonImpact({...plain,blackout:'Overload',jackpot:'Major'}).kind).toBe('overload');
  expect(neonImpact({...plain,blackout:'Blackout'}).alarm).toBe(true);
  expect(impactColor(total.kind)).not.toBe(impactColor('jackpot'));
 });
 it('distinguishes override, free spins and wild from a jackpot',()=>{
  expect(neonImpact({...plain,securityOverrideReel:2}).kind).toBe('override');
  expect(neonImpact({...plain,freeSpinsAwarded:8}).kind).toBe('free-spins');
  expect(neonImpact({...plain,expandedReels:[1]}).kind).toBe('wild');
  expect(neonImpact({...plain,jackpot:'Minor'}).kind).toBe('jackpot');
 });
 it('uses no random choices and does not mutate native receipt',()=>{
  const receipt={...plain,blackout:'Blackout' as const};
  const before=JSON.stringify(receipt);
  expect(neonImpact(receipt)).toEqual(neonImpact(receipt));
  expect(JSON.stringify(receipt)).toBe(before);
 });
});
