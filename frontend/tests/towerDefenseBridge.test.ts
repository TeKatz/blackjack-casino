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
  nextEnemyId:2,spawned:1,spawnTotal:9,waveKills:0,waveEscapes:0,spawnClock:.7,ticks:1,
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
  expect(isTowerDefenseState({...game,state:{...game.state,spawnClock:-1}})).toBe(false);
  expect(isTowerDefenseState({...game,state:{...game.state,waveKills:2}})).toBe(false);
  expect(isTowerDefenseState({...game,state:{...game.state,spawned:10}})).toBe(false);
 });
 it('accepts all-deployed running waves while no enemies have yet been killed',()=>{
  const afterDeployment={...game,state:{...game.state,spawned:9,spawnClock:0,waveKills:0,
   enemies:game.state.enemies}};
  expect(isTowerDefenseState(afterDeployment)).toBe(true);
  expect(isTowerDefenseState({...afterDeployment,state:{...afterDeployment.state,
   waveKills:8,waveEscapes:1,enemies:[]}})).toBe(true);
 });
 it('validates map selection, Frostpass and mastery snapshots',()=>{
  const progress={level:4,experience:88,skills:{capital:2,scouting:1},
   completedStages:['forest'],bestWaves:{forest:15,frost:0},enemiesDefeated:180,
   enemiesEscaped:4,wavesCleared:18,victories:1,defeats:0,runsStarted:2,pointsAvailable:0};
  const menu={...game,screen:'menu',state:{...game.state,screen:'menu',stage:'forest',progress,
   stageRuns:{forest:{wave:1}}}};
  expect(isTowerDefenseState(menu)).toBe(true);
  expect(isTowerDefenseState({...menu,screen:'skills'})).toBe(true);
  expect(isTowerDefenseState({...menu,state:{...menu.state,progress:{...progress,level:0}}})).toBe(false);
  expect(isTowerDefenseState({...menu,state:{...menu.state,progress:{...progress,
   skills:{unknown:4}}}})).toBe(false);
  const frost={...menu,screen:'game',maxWaves:20,state:{...menu.state,stage:'frost',
   towers:[{pad:1,kind:'ballista',level:1,cooldown:0},
    {pad:2,kind:'flame',level:1,cooldown:0}],
   effects:[{...game.state.effects[0],kind:'flame'}]}};
  expect(isTowerDefenseState(frost)).toBe(true);
 });
 it('only acknowledges valid native command receipts',()=>{
  const reply={requestId:'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',success:true,message:'saved',revision:2};
  expect(isTowerReply(reply)).toBe(true);
  expect(isTowerReply({...reply,revision:-1})).toBe(false);
  expect(isTowerReply({...reply,requestId:'wrong'})).toBe(false);
 });
});
