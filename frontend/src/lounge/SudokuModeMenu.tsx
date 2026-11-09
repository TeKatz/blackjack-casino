import {useEffect,useState} from 'react';
import type {SudokuCareer,SudokuIntent,VariantEntry} from './sudokuBridge';
import './sudokuModes.css';
import {localizedSudokuTitle} from '../lobby/sudokuTitleLocalization';

export type ModeId='classic'|'x'|'killer'|'jigsaw'|'samurai'|'mini';
type Lang='de'|'en';
type Props={language:Lang;selectedMode:ModeId|null;select:(mode:ModeId|null)=>void;
 career?:SudokuCareer;entries:VariantEntry[];pending:boolean;command:(intent:SudokuIntent)=>void};
const modes:{id:ModeId;en:string;de:string;descEn:string;descDe:string}[]=[
 {id:'classic',en:'Classic Sudoku',de:'Classic Sudoku',descEn:'The timeless 9×9 grid.',descDe:'Das klassische 9×9-Raster.'},
 {id:'x',en:'Sudoku X',de:'Sudoku X',descEn:'Two diagonals add a new rule.',descDe:'Zwei zusätzliche Diagonalen.'},
 {id:'killer',en:'Killer Sudoku',de:'Killer Sudoku',descEn:'Sum cages, without repeated digits.',descDe:'Summenkäfige ohne doppelte Zahlen.'},
 {id:'jigsaw',en:'Jigsaw Sudoku',de:'Jigsaw Sudoku',descEn:'Nine irregular puzzle regions.',descDe:'Neun unregelmäßige Gebiete.'},
 {id:'samurai',en:'Samurai Sudoku',de:'Samurai Sudoku',descEn:'Five overlapping 9×9 boards.',descDe:'Fünf überlappende 9×9-Raster.'},
 {id:'mini',en:'Mini Sudoku',de:'Mini Sudoku',descEn:'Four-by-four or six-by-six.',descDe:'Vier mal vier oder sechs mal sechs.'},
];
const rewards:[number,string,'normal'|'premium'|'ultra'][]=[
 [10,'title-sudoku-10','normal'],[20,'title-sudoku-20','normal'],
 [30,'title-sudoku-30','normal'],[40,'title-sudoku-40','normal'],
 [50,'title-sudoku-50','normal'],[60,'title-sudoku-60','normal'],
 [70,'title-sudoku-70','normal'],[80,'title-sudoku-80','normal'],
 [90,'title-sudoku-90','normal'],[90,'title-sudoku-90-premium','premium'],
 [100,'title-sudoku-100','normal'],[100,'title-sudoku-100-ultra','ultra'],
];
const levelNames:Record<Lang,Record<string,string>>={
 de:{Easy:'Leicht',Medium:'Mittel',Hard:'Schwer'},
 en:{Easy:'Easy',Medium:'Medium',Hard:'Hard'},
};
export function SudokuModeMenu({language,selectedMode,select,career:careerRaw,entries,pending,command}:Props){
 const de=language==='de';
 const [careerOpen,setCareerOpen]=useState(false);
 const [replace,setReplace]=useState<VariantEntry|null>(null);
 useEffect(()=>{if(!careerOpen)return;const key=(e:KeyboardEvent)=>{if(e.key==='Escape')setCareerOpen(false);};window.addEventListener('keydown',key);return()=>window.removeEventListener('keydown',key);},[careerOpen]);
 const career=careerRaw??{level:1,experience:0,nextLevelXp:100,lifetimeExperience:0,cosmeticPointsEarned:0,pointsPerLevel:25};
 const percent=career.nextLevelXp>0?Math.min(100,career.experience/career.nextLevelXp*100):100;
 const nextLevel=career.level>=100?100:Math.ceil((career.level+1)/10)*10;
 const current=modes.find(m=>m.id===selectedMode);
 return <section className={'sudoku-mode-menu'+(selectedMode!==null?' is-selection':'')} data-testid="sudoku-mode-menu">
  <div className="sudoku-mode-hero">
   <div><span className="sudoku-eyebrow">THE MIDNIGHT CLUB · SUDOKU LOUNGE</span>
    <h1>{de?'Sechs Wege zur Lösung.':'Six ways to solve.'}</h1>
    <p>{de?'Wähle deine Variante. Alle zählen zu deiner Sudoku-Karriere.':
       'Choose a variation. Every solved board advances your Sudoku career.'}</p></div>
   <button type="button" className="sudoku-career-overview" data-testid="sudoku-career" onClick={()=>setCareerOpen(true)}
     aria-haspopup="dialog" aria-label={de?'Karriere und Belohnungen öffnen':'Open career and rewards'}>
    <span>{de?'SUDOKU-KARRIERE':'SUDOKU CAREER'} · {de?'ANSEHEN ↗':'VIEW ↗'}</span>
    <strong>LV {career.level}<small> / 100</small></strong>
    <div className="sudoku-career-track" role="progressbar" aria-valuemin={0}
      aria-valuemax={career.nextLevelXp||1} aria-valuenow={career.experience}
      aria-label={de?'Karriere-Erfahrung':'Career experience'}><i style={{width:percent+'%'}}/></div>
    <small>{career.level===100?'MAX':career.experience.toLocaleString(language)+' / '+career.nextLevelXp.toLocaleString(language)+' XP'}</small>
    <div className="sudoku-career-rewards"><span>◆ {career.cosmeticPointsEarned.toLocaleString(language)} CP</span>
      <span>{de?'Nächster Titel':'Next title'} · LV {nextLevel}</span></div>
   </button>
  </div>
  {selectedMode===null?
   <div className="sudoku-mode-grid">
    {modes.map(mode=><button type="button" className="sudoku-mode-tile available"
      data-testid={'sudoku-mode-'+mode.id} key={mode.id} onClick={()=>select(mode.id)}>
      <img src={'/lounge/sudoku-'+mode.id+'.svg'} alt={(de?'Vorschau ':'Preview ')+(de?mode.de:mode.en)} loading="eager"/>
      <span className="sudoku-mode-tile-body">
       <span className="sudoku-mode-kicker">{de?'JETZT SPIELBAR':'PLAY NOW'}</span>
       <strong>{de?mode.de:mode.en}</strong>
       <span>{de?mode.descDe:mode.descEn}</span>
       <b>{de?'SCHWIERIGKEIT WÄHLEN →':'CHOOSE DIFFICULTY →'}</b>
      </span>
     </button>)}
   </div>:
   <div className="sudoku-mode-coming sudoku-variant-select" data-testid="sudoku-variant-selection">
    <button type="button" className="sudoku-mode-back" onClick={()=>select(null)}>← {de?'Alle Modi':'All modes'}</button>
    <div className="sudoku-variant-select-head">
     <img src={'/lounge/sudoku-'+selectedMode+'.svg'} alt="" aria-hidden="true"/>
     <div><span className="sudoku-eyebrow">{de?'SPIELMODUS':'GAME MODE'}</span>
      <h2>{de?current?.de:current?.en}</h2>
      <p>{de?current?.descDe:current?.descEn}</p></div>
    </div>
    <h3>{de?'Wähle dein Rätsel':'Choose your puzzle'}</h3>
    <div className="sudoku-variant-options">
     {entries.filter(entry=>entry.mode===selectedMode).map(entry=><article className="sudoku-variant-option"
       key={entry.key} data-testid={'sudoku-entry-'+entry.key.replaceAll(':','-')}>
      <strong>{selectedMode==='mini'?entry.miniSize+'×'+entry.miniSize+' · ':''}{levelNames[language][entry.difficulty]}</strong>
      <span>{entry.completed} {de?'gelöst':'solved'}</span>
      <div><button type="button" disabled={!entry.canResume||pending}
        onClick={()=>command({action:'sudoku.variant.resume',value:entry.key})}>{de?'FORTSETZEN':'RESUME'}</button>
       <button type="button" className="primary" disabled={pending} onClick={()=>
        entry.canResume?setReplace(entry):command({action:'sudoku.variant.new',value:entry.key})}>
        {de?'NEUES RÄTSEL':'NEW PUZZLE'}</button></div>
     </article>)}
    </div>
   </div>}
  {careerOpen&&<div className="sudoku-career-overlay" role="presentation"
    onMouseDown={e=>{if(e.target===e.currentTarget)setCareerOpen(false);}}>
   <section className="sudoku-career-modal" role="dialog" aria-modal="true" aria-labelledby="career-modal-title">
    <div className="sudoku-career-modal-head">
     <div><span className="sudoku-eyebrow">SUDOKU · CAREER</span>
      <h2 id="career-modal-title">{de?'Die 100-Level-Karriere':'The 100-level career'}</h2>
      <p>{de?'25 Cosmetic Points pro Levelaufstieg, exklusive Titel alle zehn Level.':
       '25 cosmetic points per level-up; exclusive titles every ten levels.'}</p></div>
     <button type="button" onClick={()=>setCareerOpen(false)} aria-label={de?'Schließen':'Close'}>✕</button>
    </div>
    <div className="sudoku-career-modal-track"><span>LV {career.level}</span><div><i style={{width:percent+'%'}}/></div>
      <span>{career.level===100?'MAX':career.experience.toLocaleString(language)+' / '+career.nextLevelXp.toLocaleString(language)+' XP'}</span>
    </div>
    <div className="sudoku-career-milestones">
     {rewards.map(([level,titleId,tier])=><div key={titleId}
      className={'sudoku-career-milestone '+(level<=career.level?'unlocked':'locked')+' '+tier}>
      <span className="sudoku-milestone-level">LV {level}</span>
      <span className="sudoku-milestone-name">{localizedSudokuTitle(titleId,titleId,language)}</span>
      <span className="sudoku-milestone-status">{level<=career.level?'✓ '+(de?'FREI':'UNLOCKED'):
        (de?'GESPERRT':'LOCKED')}</span>
     </div>)}
    </div>
    <p>{de?'Im Cosmetic Shop sichtbar, aber nur durch die Sudoku-Karriere freischaltbar.':
      'Visible in the Cosmetic Shop but only unlockable through Sudoku career progress.'}</p>
   </section>
  </div>}
  {replace&&<div className="sudoku-dialog" role="dialog" aria-modal="true">
    <div><h2>{de?'Begonnenes Rätsel ersetzen?':'Replace unfinished puzzle?'}</h2>
     <p>{de?'Der Spielstand dieses Schwierigkeitsgrades wird ersetzt. Andere Rätsel bleiben erhalten.':
      'Only this difficulty’s unfinished board will be replaced.'}</p>
     <div className="sudoku-dialog-actions">
      <button onClick={()=>setReplace(null)}>{de?'ABBRECHEN':'CANCEL'}</button>
      <button className="primary" onClick={()=>{command({action:'sudoku.variant.new',value:replace.key});setReplace(null);}}>{de?'ERSETZEN':'REPLACE'}</button>
     </div></div>
   </div>}
  <p className="sudoku-career-note">{de?'Alle Modi sind offline spielbar. Erreiche Level 90 und 100 für die besonders animierten Prestige-Titel.':
    'All six modes work offline. Reach levels 90 and 100 for the exclusive animated prestige titles.'}</p>
 </section>;
}
