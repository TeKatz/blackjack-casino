import {describe,expect,it} from 'vitest';
import {isTaleReply,isTaleState} from '../src/lounge/adventureBridge';

const abilities=['Strength','Dexterity','Constitution','Intelligence','Wisdom','Charisma']
 .map(id=>({id,name:id,score:12}));
const adventures=['black-stag','drowned-bell','dead-mans-hand']
 .map(id=>({id,title:id,subtitle:'A tale with consequences',acts:3,scenes:20,
 status:'unplayed',runId:null,lastSceneId:null}));
const classes=[{id:'rogue',name:'Rogue',description:'A resourceful adventurer',
 maxHitPoints:16,armor:14,special:'',abilities}];
const library={screen:'library',selectedAdventureId:'black-stag',
 adventures,classes,defaultPlayerName:'Player',started:0,completed:0,
 run:null,encounter:null,notice:'',metaNotice:'',musicEnabled:true,musicVolume:.24};
const run={runId:'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',adventureId:'black-stag',
 completed:false,endingId:null,rollsMade:0,scene:{id:'opening',title:'The path',
 body:'The door creaks.',speaker:null,location:'Forest',act:'Act I'},
 choices:[{id:'go',text:'Open the door',check:null,encounter:null}],
 pending:null,lastRoll:null,
 character:{name:'Player',classId:'rogue',level:1,experience:0,
 maxHitPoints:16,currentHitPoints:16,armor:14,coins:20,
 abilities:abilities.map(a=>({...a,modifier:1})),
 skills:[{id:'stealth',name:'Stealth',bonus:3}]},
 inventory:[],conditions:[],feedback:[],board:null,
 statistics:{scenesVisited:1,choicesMade:0,checksSucceeded:0,checksFailed:0,
 encountersWon:0,encountersLost:0}};
const story={...library,screen:'story',run};
const encounter={id:'ashen-stranger-blackjack',type:'Blackjack',title:'The Wager',
 opponentName:'The Stranger',intro:'One hand.',stakes:'One wager.',
 outcome:null,blackjack:{phase:'PlayerTurn',dealer:[{hidden:false,rank:'King',suit:'Hearts'},
 {hidden:true,rank:null,suit:null}],player:[{hidden:false,rank:'Ace',suit:'Spades'},
 {hidden:false,rank:'Six',suit:'Diamonds'}],playerTotal:17,
 dealerTotal:null,canHit:true,canStand:true,net:null},dice:null};

describe('React Tales native snapshot guard',()=>{
 it('accepts catalog, character roster and native scene without changing save schema',()=>{
  expect(isTaleState(library)).toBe(true);
  expect(isTaleState(story)).toBe(true);
  expect(isTaleState({...story,run:{...run,pending:{
   ability:'Dexterity',difficulty:13,skillId:'stealth',mode:'Advantage',modifier:4},choices:[]}})).toBe(true);
 });
 it('validates board discovery and native-only encounter outputs',()=>{
  const board={title:'The Case',zoom:1,panX:0,panY:0,nodes:[{
   id:'clue',category:'Evidence',title:'Letter',description:'Evidence',subtitle:'',
   reliability:'Credible',lead:'Unresolved',theory:'Proposed',facts:[],quotes:[],
   x:100,y:120}],connections:[]};
  expect(isTaleState({...story,screen:'board',run:{...run,board}})).toBe(true);
  expect(isTaleState({...story,screen:'board'})).toBe(false);
  expect(isTaleState({...story,screen:'board',
   run:{...run,board:{...board,nodes:[{...board.nodes[0],x:Number.NaN}]}}})).toBe(false);
  expect(isTaleState({...story,screen:'encounter',encounter})).toBe(true);
  expect(isTaleState({...story,screen:'encounter',encounter:{...encounter,
   blackjack:{...encounter.blackjack,dealer:[{hidden:true,rank:'King',suit:'Hearts'}]}}})).toBe(false);
 });
 it('allows completed runs only on the native completion route and rejects bad receipts',()=>{
  expect(isTaleState({...story,screen:'complete',
   run:{...run,completed:true,endingId:'ending'}})).toBe(true);
  expect(isTaleState({...story,screen:'complete'})).toBe(false);
  const reply={requestId:'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
   success:true,message:'Saved',revision:3};
  expect(isTaleReply(reply)).toBe(true);
  expect(isTaleReply({...reply,requestId:'not-guid'})).toBe(false);
  expect(isTaleReply({...reply,revision:-1})).toBe(false);
 });
});
