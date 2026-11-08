import {describe,expect,it} from 'vitest';
import {difficulties,isSudokuState,isSudokuReply,sudokuEditIntent} from '../src/lounge/sudokuBridge';

const givens='256000703000000010000200084000360020000800000498072000060007300102043508080005900';
const board=givens;
const game={puzzleId:'normal-relaxed-001',difficulty:'Relaxed',isDaily:false,givens,board,
 notes:Array(81).fill(0),incorrect:[],conflicts:[],completedDigits:[],
 elapsedSeconds:12,hintsUsed:0,completed:false,canUndo:true,canRedo:false,
 completionXp:150,completionSeasonXp:75};
const entries=difficulties.map(id=>({id,canResume:false,elapsedSeconds:0,completed:0}));
const base={screen:'library',showTimer:true,showGuides:true,entries,normalCompleted:0,
 normalTotal:9000,dailyCompleted:0,daily:{date:'2026-11-06',difficulty:'Relaxed',
 completed:false,canResume:false},game:null,notice:'',rewardNotice:''};
describe('C#-authored Sudoku snapshot',()=>{
 it('accepts the full six-level library and a valid 81-cell board',()=>{
  expect(isSudokuState(base)).toBe(true);
  expect(isSudokuState({...base,screen:'game',game})).toBe(true);
 });
 it('rejects missing difficulty, forged givens, malformed notes, noncanonical cells',()=>{
  expect(isSudokuState({...base,entries:entries.slice(1)})).toBe(false);
  expect(isSudokuState({...base,entries:[...entries.slice(0,5),entries[0]]})).toBe(false);
  expect(isSudokuState({...base,screen:'game',game:{...game,board:'9'+board.slice(1)}})).toBe(false);
  expect(isSudokuState({...base,screen:'game',game:{...game,notes:Array(81).fill(1023)}})).toBe(false);
  expect(isSudokuState({...base,screen:'game',game:{...game,incorrect:[0,0]}})).toBe(false);
  expect(isSudokuState({...base,screen:'game',game:{...game,completedDigits:[10]}})).toBe(false);
  expect(isSudokuState({...base,screen:'game',game:{...game,elapsedSeconds:-1}})).toBe(false);
 });
 it('rejects malformed command receipts',()=>{
  const reply={requestId:'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',success:true,revision:2,message:'Saved'};
  expect(isSudokuReply(reply)).toBe(true);
  expect(isSudokuReply({...reply,revision:-1})).toBe(false);
  expect(isSudokuReply({...reply,requestId:'not-a-guid'})).toBe(false);
 });
});

describe('selected Sudoku editing cell',()=>{
 it('erases the selected cell rather than the hint auto-selection sentinel',()=>{
  for(let i=0;i<81;i++)expect(sudokuEditIntent('sudoku.erase',i)).toEqual({action:'sudoku.erase',index:i});
  expect(sudokuEditIntent('sudoku.erase',-1)).toBeNull();
  expect(sudokuEditIntent('sudoku.hint',-1)).toEqual({action:'sudoku.hint',index:-1});
  expect(sudokuEditIntent('sudoku.erase',81)).toBeNull();
 });
});
