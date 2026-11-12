import {CIRCUIT_TURN_DURATION} from './arcadeExpansionTwoEngine';
import type {CircuitRun,CircuitTile} from './arcadeExpansionTwoEngine';

export function circuitTilePose(run:CircuitRun,tile:CircuitTile){
 const turning=run.turnClock>0&&run.turnCell?.c===tile.c&&run.turnCell?.r===tile.r;
 const progress=turning?Math.max(0,Math.min(1,1-run.turnClock/CIRCUIT_TURN_DURATION)):1;
 // Quintic smoothstep starts and stops without an abrupt mechanical jerk.
 const eased=progress*progress*progress*(progress*(progress*6-15)+10);
 return {turning,progress,eased,mask:turning?run.turnFromMask:tile.mask,
  angle:turning?Math.PI/2*eased:0};
}
