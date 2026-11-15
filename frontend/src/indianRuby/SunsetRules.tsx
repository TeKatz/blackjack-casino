import {memo} from 'react';
import {RubySymbolArt} from './SymbolArt';
import type {RubyState,SymbolId} from './bridge';

type Props={state:RubyState|null;language:'de'|'en';close:()=>void};
const titles:Record<SymbolId,{de:string;en:string}>={
 Elephant:{de:'Elefant',en:'Elephant'},Ruby:{de:'Rubin-Wild',en:'Ruby Wild'},
 Necklace:{de:'Halskette',en:'Necklace'},Vase:{de:'Vase',en:'Vase'},
 Spices:{de:'Gewürze',en:'Spices'},Ace:{de:'Ass',en:'Ace'},King:{de:'König',en:'King'},
 Queen:{de:'Dame',en:'Queen'},Jack:{de:'Bube',en:'Jack'},Ten:{de:'Zehn',en:'Ten'},
 Empty:{de:'Leer',en:'Blank'},
};
const pct=(n:number)=>n.toLocaleString('de-DE',{maximumFractionDigits:3})+' %';
const factor=(n:number)=>n===0?'—':'×'+n.toLocaleString('de-DE');
const upper=(text:string)=>text.toLocaleUpperCase('de-DE');
function SunsetRulesView({state,language,close}:Props){
 const de=language==='de';
 const t=de?{
  title:'SUNSET RUBY · SPIELREGELN',sub:'Symbole, Gewinnplan & tatsächliche Test-Wahrscheinlichkeiten',
  symbol:'SYMBOL',two:'2',three:'3',four:'4',five:'5',outer:'WALZEN 1 & 5',inner:'WALZEN 2–4',
  table:'GEWINNPLAN',chance:'SYMBOLWAHRSCHEINLICHKEITEN',bonus:'ELEFANTEN-BONUS',
  odds:'Chance auf mindestens 2 Elefanten im Basisspin',bonusElephant:'Elefant pro Bonussymbol',
  bonusRuby:'Rubin pro freiem Bonusfeld (nur Walzen 2–4)',
  explanation:'Alle Faktoren gelten pro Gewinnlinie. Der Einsatz wird durch 10 Linien geteilt. Rubine ersetzen reguläre Gewinnsymbole und verdoppeln den entsprechenden Liniengewinn.',
  activation:'Mindestens zwei echte Elefanten auf der ursprünglichen 5×3-Matrix lösen den Bonus aus. Bereits angezeigte Elefanten und Rubine bleiben stehen. Alle anderen Felder drehen einmal erneut und zeigen nur Elefant, Rubin oder Leer. Ein Rubin im Bonus kann Elefantenlinien vervollständigen.',
  note:'Wichtig: Das sind Wahrscheinlichkeiten pro Symbolposition der aktuell implementierten Test-Gewichtung, nicht die offizielle Original-Slot-RTP. Gewinnkombinationen und Bonuswahrscheinlichkeit hängen von allen 15 unabhängigen Symbolziehungen ab.',
  near:'Near Miss: Nur die Animation reagiert auf einen einzelnen tatsächlich gezogenen Elefanten. Das Ergebnis und die Chance auf den Bonus bleiben unverändert.',
  close:'Zurück zum Spiel',heads:'TREFFER VON LINKS',tips:'WILD ×2 · 10 LINIEN · 5 WALZEN'
 }:{
  title:'SUNSET RUBY · GAME RULES',sub:'Symbols, paytable & actual prototype probabilities',
  symbol:'SYMBOL',two:'2',three:'3',four:'4',five:'5',outer:'REELS 1 & 5',inner:'REELS 2–4',
  table:'PAYTABLE',chance:'SYMBOL PROBABILITIES',bonus:'ELEPHANT BONUS',
  odds:'Chance of at least 2 elephants on a base spin',bonusElephant:'Elephant per bonus cell',
  bonusRuby:'Ruby per free bonus cell (reels 2–4 only)',
  explanation:'All multipliers apply per winning line. The total stake is divided over 10 lines. A ruby replaces regular paying symbols and doubles that line win.',
  activation:'At least two real elephants on the original 5×3 grid trigger the bonus. Existing elephants and rubies are held. Other positions respin once and show only Elephant, Ruby or Blank. A bonus Ruby can help complete elephant lines.',
  note:'These are per-cell odds from this prototype’s implemented symbol weights, NOT the retail game’s RTP. Winning patterns and bonus odds depend on all 15 independent symbol draws.',
  near:'Near misses only affect animation after a genuinely drawn single elephant. Game outcomes and bonus probability are unchanged.',
  close:'Back to game',heads:'MATCHES FROM LEFT',tips:'WILD ×2 · 10 LINES · 5 REELS'
 };
 return <div className="ruby-rules-backdrop" onMouseDown={event=>{
  if(event.target===event.currentTarget)close();
 }}>
  <section className="ruby-rules" role="dialog" aria-modal="true" aria-label={t.title}>
   <header className="ruby-rules-head">
    <div><small>{t.tips}</small><h2>{t.title}</h2><p>{t.sub}</p></div>
    <button type="button" onClick={close} aria-label={t.close}>×</button>
   </header>
   <div className="ruby-rules-scroll">
    <div className="ruby-rules-section">
     <h3>{t.table}<span>{t.heads}</span></h3>
     <div className="ruby-rules-table-wrap"><table className="ruby-paytable"><thead>
      <tr><th>{t.symbol}</th><th>{t.two}</th><th>{t.three}</th><th>{t.four}</th><th>{t.five}</th></tr>
     </thead><tbody>{state?.paytable.map(item=><tr key={item.symbol}>
      <th><span className="ruby-rule-symbol"><RubySymbolArt symbol={item.symbol}/></span>
       <span>{titles[item.symbol][language]}</span></th>
      <td>{factor(item.two)}</td><td>{factor(item.three)}</td>
      <td>{factor(item.four)}</td><td>{factor(item.five)}</td>
     </tr>)}</tbody></table></div>
     <p className="ruby-rules-explain">{t.explanation}</p>
    </div>
    <div className="ruby-rules-section">
     <h3>{t.chance}</h3>
     <div className="ruby-rules-table-wrap"><table className="ruby-paytable ruby-chance-table"><thead>
      <tr><th>{t.symbol}</th><th>{t.outer}</th><th>{t.inner}</th></tr>
     </thead><tbody>{state?.paytable.map(item=><tr key={item.symbol}>
      <th><span className="ruby-rule-symbol"><RubySymbolArt symbol={item.symbol}/></span>
       <span>{titles[item.symbol][language]}</span></th>
      <td>{pct(item.outerPercent)}</td><td>{pct(item.innerPercent)}</td>
     </tr>)}</tbody></table></div>
    </div>
    <div className="ruby-rules-section ruby-rule-feature">
     <h3>{t.bonus}</h3>
     <p>{t.activation}</p>
     <div className="ruby-odds-grid">
      <div><span>{t.odds}</span><strong>{pct(state?.bonusTriggerPercent??0)}</strong></div>
      <div><span>{t.bonusElephant}</span><strong>{pct(state?.bonusElephantPercent??0)}</strong></div>
      <div><span>{t.bonusRuby}</span><strong>{pct(state?.bonusRubyInnerPercent??0)}</strong></div>
     </div>
     <p className="ruby-rules-explain">{t.near}</p>
    </div>
    <p className="ruby-rules-disclaimer">{t.note}</p>
   </div>
   <footer className="ruby-rules-footer">
    <span>{upper(de?'NUR SPIELGELD':'PLAY MONEY ONLY')}</span>
    <button type="button" onClick={close}>{t.close}</button>
   </footer>
  </section>
 </div>;
}
export const SunsetRules=memo(SunsetRulesView);
