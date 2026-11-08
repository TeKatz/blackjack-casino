import {describe,expect,it} from 'vitest';
import {isTowerDefenseState,isTowerReply} from '../src/lounge/towerDefenseBridge';

const points=[
 {x:0,y:300},{x:145,y:300},{x:145,y:130},{x:340,y:130},{x:340,y:450},
 {x:590,y:450},{x:590,y:210},{x:810,y:210},{x:810,y:325},{x:960,y:325},
];
const pads=[
 {x:80,y:190},{x:242,y:242},{x:245,y:365},{x:255,y:60},{x:440,y:245},
 {x:465,y:535},{x:690,y:345},{x:700,y:130},{x:880,y:125},{x:886,y:425},
];
const game={
 screen:'game',mapPath:points,pads,maxWaves:15,
 state:{version:1,wave:1,coins:250,lives:20,phase:'running',paused:false,speed:1,
  nextEnemyId:2,spawned:1,spawnTotal:9,spawnClock:.7,ticks:1,
  towers:[{pad:0,kind:'archer',level:1,cooldown:0}],
  enemies:[{id:1,kind:'runner',distance:10,hp:24,slow:0}],
  effects:[{fromX:80,fromY:190,toX:10,toY:300,kind:'archer',ticks:3}]},
};
describe('Tower Defence native snapshot validation',()=>{
 it('accepts authoritative bounded frames',()=>{
  expect(isTowerDefenseState(game)).toBe(true);
  expect(isTowerDefenseState({...game,state:{...game.state,phase:'planning',enemies:[]}})).toBe(true);
 });
 it('rejects impossible coins, duplicate pads, NaN, fabricated enemies and map corruption',()=>{
  expect(isTowerDefenseState({...game,state:{...game.state,coins:-1}})).toBe(false);
  expect(isTowerDefenseState({...game,state:{...game.state,towers:[...game.state.towers,...game.state.towers]}})).toBe(false);
  expect(isTowerDefenseState({...game,state:{...game.state,enemies:[{...game.state.enemies[0],distance:NaN}]}})).toBe(false);
  expect(isTowerDefenseState({...game,state:{...game.state,enemies:[{...game.state.enemies[0],kind:'unknown'}]}})).toBe(false);
  expect(isTowerDefenseState({...game,pads:pads.slice(1)})).toBe(false);
 });
 it('only acknowledges valid native command receipts',()=>{
  const reply={requestId:'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',success:true,message:'saved',revision:2};
  expect(isTowerReply(reply)).toBe(true);
  expect(isTowerReply({...reply,revision:-1})).toBe(false);
  expect(isTowerReply({...reply,requestId:'wrong'})).toBe(false);
 });
});
