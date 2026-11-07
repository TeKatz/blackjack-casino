import {useState} from 'react';
import {PremiumSelect} from './PremiumSelect';
import type {SelectOption} from './PremiumSelect';
import type {TableState} from './bridge';
import {bjText} from './locale';
import type {BlackjackLanguage} from './locale';

type Rules=TableState['rules'];
export type EditableRules=Omit<Rules,'penetration'> & {penetration:string};

export function CustomRulesEditor({initial,language,onSave,onClose}:{
  initial:Rules;language:BlackjackLanguage;onSave:(rules:EditableRules)=>void;onClose:()=>void;
}){
  const [r,setR]=useState<EditableRules>({...initial,penetration:initial.penetration.toFixed(2)});
  const [error,setError]=useState('');
  const t=bjText[language];
  const update=<K extends keyof EditableRules>(key:K,value:EditableRules[K])=>{
    setR(prev=>({...prev,[key]:value}));setError('');
  };
  const options=<T extends string|number>(values:readonly T[]):SelectOption[] =>
    values.map(value=>({value:String(value),label:String(value)}));
  const checkbox=(key:'dealerHitsSoft17'|'dealerPeeksForBlackjack'|'dealerTakesHoleCard'|
    'allowInsurance'|'allowLateSurrender'|'doubleAfterSplit'|'resplitAces'|'hitSplitAces',
    label:string,disabled=false)=><label className="bj-check">
      <input type="checkbox" checked={r[key]} disabled={disabled}
        onChange={e=>update(key,e.target.checked)}/>{label}</label>;
  const apply=(event:React.FormEvent)=>{
    event.preventDefault();
    const money=/^(?:0|[1-9]\d*)(?:\.\d{1,2})?$/;
    const min=Number(r.minimumBet),max=Number(r.maximumBet);
    if(!money.test(r.minimumBet)||!money.test(r.maximumBet)||min<=0||max<min||max>1_000_000){
      setError(t.invalidLimits);return;
    }
    onSave({...r,dealerPeeksForBlackjack:r.dealerTakesHoleCard&&r.dealerPeeksForBlackjack});
  };
  return <div className="bj-modal-wrap" role="presentation">
    <div className="bj-modal" role="dialog" aria-modal="true" aria-labelledby="bj-rules-title" data-testid="blackjack-custom-rules">
      <form onSubmit={apply}>
        <header><div><small>THE MIDNIGHT CLUB</small><h2 id="bj-rules-title">{t.rulesTitle}</h2>
          <p>{t.rulesDesc}</p></div>
          <button type="button" onClick={onClose} aria-label={t.closeRules}>×</button></header>
        <div className="bj-rule-grid">
          <label>{t.decks}<PremiumSelect label={t.decks} value={String(r.deckCount)}
            onChange={v=>update('deckCount',Number(v))} options={options([1,2,4,6,8])}/></label>
          <label>{t.soft17}<PremiumSelect label={t.soft17} value={String(r.dealerHitsSoft17)}
            onChange={v=>update('dealerHitsSoft17',v==='true')}
            options={[{value:'false',label:t.stand17},{value:'true',label:t.hit17}]}/></label>
          <label>{t.dealerHole}<PremiumSelect label={t.dealerHole}
            value={String(r.dealerTakesHoleCard)}
            onChange={v=>{const hole=v==='true';setR(prev=>({...prev,dealerTakesHoleCard:hole,
              dealerPeeksForBlackjack:hole&&prev.dealerPeeksForBlackjack}));}}
            options={[{value:'true',label:t.usHole},{value:'false',label:t.europeanHole}]}/></label>
          {checkbox('dealerPeeksForBlackjack',t.dealerPeek,!r.dealerTakesHoleCard)}
          {checkbox('allowInsurance',t.insurance)}
          {checkbox('allowLateSurrender',t.lateSurrender)}
          <label>{t.payout}<PremiumSelect label={t.payout} value={r.blackjackPayout}
            onChange={v=>update('blackjackPayout',v)}
            options={[{value:'1.5',label:'3:2'},{value:'1.2',label:'6:5'}]}/></label>
          <label>{t.doubleDown}<PremiumSelect label={t.doubleDown} value={r.doubleRule}
            onChange={v=>update('doubleRule',v)}
            options={[{value:'AnyTwo',label:t.anyTwo},{value:'NineToEleven',label:'9–11'},
              {value:'TenToEleven',label:'10–11'}]}/></label>
          {checkbox('doubleAfterSplit',t.afterSplit)}
          {checkbox('resplitAces',t.resplitAces)}
          {checkbox('hitSplitAces',t.hitSplitAces)}
          <label>{t.maxSplit}<PremiumSelect label={t.maxSplit}
            value={String(r.maxSplitHands)} onChange={v=>update('maxSplitHands',Number(v))}
            options={options([2,3,4,5,6,7,8])}/></label>
          <label>{t.penetration}<PremiumSelect label={t.penetration} value={r.penetration}
            onChange={v=>update('penetration',v)}
            options={options(['0.50','0.55','0.60','0.65','0.70','0.75','0.80','0.85','0.90'])}/></label>
          <label>{t.minBet}<input value={r.minimumBet} inputMode="decimal"
            onChange={e=>update('minimumBet',e.target.value)}/></label>
          <label>{t.maxBet}<input value={r.maximumBet} inputMode="decimal"
            onChange={e=>update('maximumBet',e.target.value)}/></label>
        </div>
        {error&&<p role="alert" className="bj-error">{error}</p>}
        <footer><button type="button" onClick={onClose}>{t.cancel}</button>
          <button className="bj-save" type="submit" data-testid="blackjack-rules-save">{t.useCustom}</button></footer>
      </form>
    </div>
  </div>;
}
