import {describe,it,expect} from 'vitest';
import {SPEED_PRESETS,reelTravel,reactionFromSpin,canQueueAutoSpin} from '../src/reels/reelsMotion';
import type {ReelsSpin} from '../src/reels/bridge';

const spin:ReelsSpin={id:'00000000-0000-4000-8000-000000000001',bet:'100',bankrollBefore:'2000',
 bankrollAfter:'1900',payout:'0',net:'-100',multiplier:0,stops:[0,2,4],
 symbols:['Cherry','Lemon','Bell'],playerXp:20,seasonXp:15,levelUp:false};
const ready={active:true,remaining:10,pending:false,animating:false,
 uncertain:false,canSpin:true,latestId:spin.id,revealedId:spin.id};
describe('Midnight Reels presentation-only speed and autospin safety',()=>{
 it('keeps all original physical stops and only shortens the visual travel',()=>{
  expect(reelTravel(31,0)%32).toBe(1);
  expect(reelTravel(0,31)%32).toBe(31);
  expect(reelTravel(0,0)%32).toBe(0);
  expect(SPEED_PRESETS.normal[2]).toBeGreaterThan(SPEED_PRESETS.fast[2]);
  expect(SPEED_PRESETS.fast[2]).toBeGreaterThan(SPEED_PRESETS.turbo[2]);
 });
 it('does not send a second spin before native acknowledgement and finished animation',()=>{
  expect(canQueueAutoSpin(ready)).toBe(true);
  for(const caseValue of [
   {pending:true},{animating:true},{uncertain:true},{remaining:0},
   {canSpin:false},{revealedId:'00000000-0000-4000-8000-000000000002'},{active:false},
  ])expect(canQueueAutoSpin({...ready,...caseValue})).toBe(false);
 });
 it('uses NET rather than payout to classify pushes, losses and large wins',()=>{
  expect(reactionFromSpin(spin)).toBe('loss');
  expect(reactionFromSpin({...spin,net:'0',multiplier:1,payout:'100'})).toBe('push');
  expect(reactionFromSpin({...spin,net:'400',multiplier:5,payout:'500'})).toBe('win');
  expect(reactionFromSpin({...spin,net:'2400',multiplier:25,payout:'2500'})).toBe('bigwin');
 });
});
