import {describe,expect,it} from 'vitest';
import {isJigsawReply,isJigsawState,motifs,sizes} from '../src/lounge/jigsawBridge';
const dims=[[6,4],[8,6],[12,8],[15,10],[20,15],[25,20],[30,25],[40,25]];
const images=motifs.map(id=>({id,name:id,category:'Fantasy',asset:id+'.png'}));
const scale=sizes.map((count,i)=>({count,columns:dims[i][0],rows:dims[i][1],rewardXp:100}));
const achievements=Array.from({length:30},(_,i)=>({id:'milestone-'+i,name:'Achievement '+i,
 description:'Unlock',tier:'Bronze',goal:1,current:0,unlocked:false,rewardXp:100,rewardCoins:50}));
const saved={screen:'library',selectedMotifId:'enchanted-forest',selectedPieceCount:96,
 timerEnabled:true,game:null,resume:null,motifs:images,sizes:scale,
 stats:{completedGames:0,piecesCompleted:0,largestPuzzle:0,achievementsUnlocked:0},
 achievements,notice:'',rewardNotice:''};
const board={gameId:'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
 motifId:'enchanted-forest',pieceCount:24,columns:6,rows:4,width:960,height:600,
 elapsedSeconds:0,timerEnabled:true,completed:false,lockedCount:0,onBoardCount:0,
 pieces:Array.from({length:24},(_,id)=>({id,x:0,y:0,onBoard:false,locked:false}))};
describe('Jigsaw native gameplay contract',()=>{
 it('accepts full 12-scene, eight-size catalog and thirty achievements',()=>{
  expect(isJigsawState(saved)).toBe(true);
  expect(isJigsawState({...saved,screen:'game',game:board})).toBe(true);
  expect(isJigsawState({...saved,screen:'achievements'})).toBe(true);
  expect(isJigsawState({...saved,screen:'complete',game:{...board,completed:true,
   lockedCount:24,onBoardCount:24,pieces:board.pieces.map(p=>({...p,onBoard:true,locked:true}))}})).toBe(true);
 });
 it('rejects forged game results, invalid pieces and invalid image references',()=>{
  expect(isJigsawState({...saved,game:board})).toBe(false);
  expect(isJigsawState({...saved,motifs:[...images.slice(0,11),{...images[11],asset:'https://remote.example/art.png'}]})).toBe(false);
  expect(isJigsawState({...saved,screen:'game',game:{...board,pieceCount:1000}})).toBe(false);
  expect(isJigsawState({...saved,screen:'game',game:{...board,pieces:[...board.pieces.slice(0,23),{...board.pieces[23],id:1}]}})).toBe(false);
  expect(isJigsawState({...saved,screen:'game',game:{...board,pieces:[{...board.pieces[0],x:Number.NaN},...board.pieces.slice(1)]}})).toBe(false);
  expect(isJigsawState({...saved,screen:'game',game:{...board,lockedCount:1}})).toBe(false);
 });
 it('validates bounded native receipts',()=>{
  const good={requestId:'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',success:true,message:'saved',revision:4};
  expect(isJigsawReply(good)).toBe(true);
  expect(isJigsawReply({...good,revision:-1})).toBe(false);
  expect(isJigsawReply({...good,requestId:'not-a-guid'})).toBe(false);
 });
});
