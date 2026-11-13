import {describe,expect,it} from 'vitest';
import {fallbackLobby,isLobbySnapshot} from '../src/lobby/lobbyBridge';
import {createExpansionTwoRun,stepExpansionTwoRun} from '../src/lobby/arcadeExpansionTwoEngine';
import {symbols} from '../src/nineLives/bridge';
import {reelFrames,reelStrip} from '../src/nineLives/reelMotion';

describe('Paket 2: UI and arcade regression contracts',()=>{
 it('validates overall challenge completion independently of a single challenge',()=>{
  expect(isLobbySnapshot(fallbackLobby)).toBe(true);
  expect(isLobbySnapshot({...fallbackLobby,challengeCompleted:4})).toBe(false);
  expect(isLobbySnapshot({...fallbackLobby,challengeTotal:-1})).toBe(false);
  expect(isLobbySnapshot({...fallbackLobby,challengeOverallFraction:1.2})).toBe(false);
  expect(isLobbySnapshot({...fallbackLobby,challengeOverallFraction:.5,
   challengeTotal:10,challengeCompleted:5})).toBe(true);
 });
 it('keeps Shadow Heist within the visible playable rectangle',()=>{
  const a=createExpansionTwoRun('shadow-heist',32);
  const b=createExpansionTwoRun('shadow-heist',32);
  if(a.kind!=='shadow-heist'||b.kind!=='shadow-heist')throw new Error('Expected Shadow Heist');
  for(const run of [a,b]){run.guards=[];run.invincible=999;run.timeLeft=1000;}
  for(let i=0;i<600;i++){
   stepExpansionTwoRun(a,1/60,false,{left:true,up:true});
   stepExpansionTwoRun(b,1/60,false,{right:true,down:true});
   expect(a.x).toBeGreaterThanOrEqual(48);
   expect(a.y).toBeGreaterThanOrEqual(66);
   expect(b.x).toBeLessThanOrEqual(913);
   expect(b.y).toBeLessThanOrEqual(474);
  }
 });
 it('starts each Nine Lives reel with the prior visible cells, not the strip guard',()=>{
  const visible=[symbols[0],symbols[1],symbols[2]];
  const target=[symbols[3],symbols[4],symbols[5]];
  const strip=reelStrip('package-two-test-spin',0,visible,target);
  expect(strip.slice(1,4)).toEqual(visible);
  expect(strip.slice(-4,-1)).toEqual(target);
  expect(reelFrames(80,0)[0].transform).toBe('translate3d(0,-80px,0)');
 });
});
