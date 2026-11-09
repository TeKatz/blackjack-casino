/** Localized presentation names for Sudoku career exclusives. Stable IDs remain unchanged. */
export type SudokuTitleLanguage = 'de' | 'en';
export const sudokuCareerTitleNames: Record<string, Record<SudokuTitleLanguage, string>> = {
 'title-sudoku-10':{de:'Zahlenkenner',en:'Number Adept'},
 'title-sudoku-20':{de:'Logikdetektiv',en:'Logic Detective'},
 'title-sudoku-30':{de:'Rätselstratege',en:'Puzzle Strategist'},
 'title-sudoku-40':{de:'Zahlenvirtuose',en:'Number Virtuoso'},
 'title-sudoku-50':{de:'Meister der Neun',en:'Master of Nine'},
 'title-sudoku-60':{de:'Architekt der Logik',en:'Architect of Logic'},
 'title-sudoku-70':{de:'Sudoku-Großmeister',en:'Sudoku Grandmaster'},
 'title-sudoku-80':{de:'Herrscher der Zahlen',en:'Sovereign of Numbers'},
 'title-sudoku-90':{de:'Zahlenvisionär',en:'Number Visionary'},
 'title-sudoku-90-premium':{de:'Das perfekte Muster',en:'The Perfect Pattern'},
 'title-sudoku-100':{de:'Sudoku-Legende',en:'Sudoku Legend'},
 'title-sudoku-100-ultra':{de:'Jenseits aller Logik',en:'Beyond All Logic'},
};
export function localizedSudokuTitle(id:string, fallback:string, language:SudokuTitleLanguage):string {
 return sudokuCareerTitleNames[id]?.[language] ?? fallback;
}
