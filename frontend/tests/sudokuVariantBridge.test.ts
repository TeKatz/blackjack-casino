import {describe,it,expect} from 'vitest';
import {isSudokuState,isVariantGame} from '../src/lounge/sudokuBridge';
const givens='1234341221434321';
const mini={id:'variant-mini4-easy-0000',mode:'mini',difficulty:'Easy',miniSize:4,width:4,digits:4,
  givens,board:givens,notes:Array(16).fill(0),regions:Array.from({length:16},(_,i)=>Math.floor(i/8)*2+Math.floor(i%4/2)),
  cages:[],incorrect:[],conflicts:[],elapsedSeconds:8,hintsUsed:0,completed:true,canUndo:false,canRedo:false,
  completionXp:62,careerXp:13,notice:'',rewardNotice:''};
const keys=['x','killer','jigsaw','samurai'].flatMap(mode=>['Easy','Medium','Hard'].map(difficulty=>({
 key:mode+':'+difficulty,mode,difficulty,miniSize:9,canResume:false,elapsedSeconds:0,completed:0,
}))).concat(['Easy','Medium','Hard'].flatMap(difficulty=>[4,6].map(miniSize=>({
 key:'mini:'+difficulty+':'+miniSize,mode:'mini',difficulty,miniSize,canResume:false,elapsedSeconds:0,completed:0,
}))));
describe('native variant Sudoku bridge',()=>{
 it('accepts a typed board without leaking an answer',()=>{
  expect(isVariantGame(mini)).toBe(true);
  expect(isVariantGame({...mini,solution:givens})).toBe(false);
  expect(isVariantGame({...mini,board:'9'+givens.slice(1)})).toBe(false);
  expect(isVariantGame({...mini,notes:[1,...Array(15).fill(0)]})).toBe(false);
  expect(isVariantGame({...mini,width:21})).toBe(false);
 });
 it('accepts all eighteen offline choices and rejects malformed catalog entries',()=>{
  const entries=['Relaxed','Easy','Medium','Hard','Expert','Master'].map(id=>({id,canResume:false,elapsedSeconds:0,completed:0}));
  const base={screen:'library',showTimer:true,showGuides:true,
   entries,normalCompleted:0,normalTotal:100,dailyCompleted:0,
   daily:{date:'2026-11-09',difficulty:'Easy',completed:false,canResume:false},
   game:null,notice:'',rewardNotice:'',variant:mini,variantEntries:keys};
  expect(isSudokuState(base)).toBe(true);
  expect(isSudokuState({...base,variantEntries:keys.slice(1)})).toBe(false);
  expect(isSudokuState({...base,variant:{...mini,board:'6'+givens.slice(1)}})).toBe(false);
 });
});
