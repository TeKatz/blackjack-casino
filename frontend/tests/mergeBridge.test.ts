import {describe,expect,it} from 'vitest';
import {isMergeReceipt,isMergeSnapshot} from '../src/merge/bridge';
const guid='aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
const labels=Object.fromEntries(['title','eyebrow','back','producer','noEnergy','producerHint',
 'orders','deliver','waiting','howToPlay','selectionHelp','selectFirst','globalProgress',
 'nextUpgrade','areaComplete','saveFailed','progressionSaveFailed','xpHint','profileUnavailable'].map(key=>[key,key]));
const defs=[{id:'chip-1',name:'Chip',lineId:'casino-chips',tier:1,maxTier:2,mergeResult:'chip-2',
 color:'#00ffaa',mark:'I',stackCount:1,tags:['chip']},{id:'chip-2',name:'Chip 2',lineId:'casino-chips',
 tier:2,maxTier:2,mergeResult:null,color:'#00ffaa',mark:'II',stackCount:1,tags:['chip']}];
const state={language:'en',schemaVersion:2,revision:2,columns:7,rows:6,capacity:42,
 renovationPoints:0,totalMerges:0,highestTierCreated:1,unlockedMilestones:[],
 items:[{id:guid,definitionId:'chip-1',position:0,status:'ready'}],
 definitions:defs,producers:[{id:'chip-case',name:'Chip case',outputDefinitionId:'chip-1',
 unlockPoints:0,unlocked:true,available:true,spawnCount:1}],
 orders:[{id:guid,definitionId:'first-stack',areaId:'lobby',reward:10,available:false,visible:true,
 requirements:[{definitionId:'chip-2',count:1,available:0}]}],
 restoration:{areaId:'lobby',areaName:'Lobby restoration',areaIndex:1,areaCount:1,
 current:{id:'dusty',name:'Dusty lobby',points:0},
 next:{id:'cleanup',name:'First cleanup',points:20},pointsUntilNext:20,
 upgradeProgress:0,milestones:[{id:'dusty',name:'Dusty lobby',points:0,unlocked:true}],
 areas:[{areaId:'lobby',areaName:'Lobby restoration',areaIndex:1,
  current:{id:'dusty',name:'Dusty lobby',points:0},
  next:{id:'cleanup',name:'First cleanup',points:20},pointsUntilNext:20,
  upgradeProgress:0,milestones:[{id:'dusty',name:'Dusty lobby',points:0,unlocked:true}]}]},
 profile:{level:1,levelExperience:2,prestige:0,coins:0,nextLevelExperience:100},
 soundEnabled:true,animationsEnabled:true,feedback:'Saved',playerXp:0,seasonXp:0,
 progressionSaveFailed:false,lastAction:null,labels};
const snapshot={version:1,session:guid,revision:2,state};
describe('Midnight Merge strict WebView2 projection',()=>{
 it('accepts a consistent 7x6 durable C# board',()=>{
   expect(isMergeSnapshot(snapshot)).toBe(true);
   expect(isMergeSnapshot({...snapshot,revision:1})).toBe(false);
   expect(isMergeSnapshot({...snapshot,state:{...state,capacity:43}})).toBe(false);
   expect(isMergeSnapshot({...snapshot,state:{...state,restoration:{...state.restoration,areas:[]}}})).toBe(false);
   expect(isMergeSnapshot({...snapshot,state:{...state,restoration:{...state.restoration,areaCount:2}}})).toBe(false);
   expect(isMergeSnapshot({...snapshot,state:{...state,items:[{...state.items[0],position:42}]}})).toBe(false);
   expect(isMergeSnapshot({...snapshot,state:{...state,items:[state.items[0],{...state.items[0],id:'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb'}]}})).toBe(false);
   expect(isMergeSnapshot({...snapshot,state:{...state,orders:[{...state.orders[0],
     requirements:[{definitionId:'fake-reward',count:100,available:100}]}]}})).toBe(false);
 });
 it('rejects malformed acknowledgments',()=>{
  expect(isMergeReceipt({requestId:guid,success:true,message:'saved',revision:3})).toBe(true);
  expect(isMergeReceipt({requestId:guid,success:true,message:'saved',revision:-1})).toBe(false);
  expect(isMergeReceipt({requestId:'invalid',success:true,message:'saved',revision:3})).toBe(false);
 });
});