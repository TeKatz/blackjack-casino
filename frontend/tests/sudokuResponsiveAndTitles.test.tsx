import {describe,it,expect} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {SudokuModeMenu} from '../src/lounge/SudokuModeMenu';
import {SudokuVariantScreen} from '../src/lounge/SudokuVariantScreen';
import type {VariantGame} from '../src/lounge/sudokuBridge';
import {localizedSudokuTitle,localizedSudokuTitleName,sudokuCareerTitleNames} from '../src/lobby/sudokuTitleLocalization';

const noop=()=>{};
describe('Sudoku desktop presentation and title localization',()=>{
 it('uses canonical career IDs for all twelve titles in German and English',()=>{
  expect(Object.keys(sudokuCareerTitleNames)).toHaveLength(12);
  for(const [id,names] of Object.entries(sudokuCareerTitleNames)){
   expect(localizedSudokuTitle(id,'fallback','de')).toBe(names.de);
   expect(localizedSudokuTitle(id,'fallback','en')).toBe(names.en);
   expect(names.de).not.toBe(names.en);
  }
  expect(localizedSudokuTitle('title-unknown','Keep me','en')).toBe('Keep me');
  expect(localizedSudokuTitleName('Zahlenkenner','en')).toBe('Number Adept');
  expect(localizedSudokuTitleName('Number Adept','de')).toBe('Zahlenkenner');
  expect(localizedSudokuTitleName('Midnight Dealer','en')).toBe('Midnight Dealer');
 });
 it('renders the reward overview in the selected language, without cross-language labels',()=>{
  const props={selectedMode:null,select:noop,entries:[],pending:false,command:noop};
  // The actual modal is interactive; its names are keyed to the same centralized title data
  // used by Shop and Profile, rather than a second German-only array.
  const english=renderToStaticMarkup(<SudokuModeMenu {...props} language="en"/>);
  const german=renderToStaticMarkup(<SudokuModeMenu {...props} language="de"/>);
  expect(english).toContain('Six ways to solve.');
  expect(german).toContain('Sechs Wege zur Lösung.');
  expect(english).toContain('SUDOKU CAREER');
  expect(german).toContain('SUDOKU-KARRIERE');
 });
 it('renders high-contrast Jigsaw region edges with per-edge colors',()=>{
  const regions=Array.from({length:81},(_,i)=>Math.floor(i/27)*3+Math.floor(i%9/3));
  const game:VariantGame={
   id:'variant-jigsaw-easy-0000',mode:'jigsaw',difficulty:'Easy',miniSize:9,width:9,digits:9,
   givens:'0'.repeat(81),board:'0'.repeat(81),notes:Array(81).fill(0),regions,cages:[],
   incorrect:[],conflicts:[],elapsedSeconds:0,hintsUsed:0,completed:false,
   canUndo:false,canRedo:false,completionXp:250,careerXp:60,notice:'',rewardNotice:'',
  };
  const html=renderToStaticMarkup(<SudokuVariantScreen game={game} language="en" pending={false}
   error="" command={noop}/>);
  expect(html).toContain('data-mode="jigsaw"');
  expect(html).toContain('border-top-color:#dfa8ff');
  expect(html).toContain('border-left-color:#dfa8ff');
  expect(html).toContain('border-top-width:4px');
  expect(html).toContain('border-top-color:#355960');
  expect(html).toContain('--variant-board-cap:790px');
 });
});
