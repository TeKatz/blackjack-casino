import {useState} from 'react';
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
  const options=<T extends string|number>(values:readonly T[])=>
    values.map(v=><option key={v} value={v}>{v}</option>);
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
          <label>{t.decks}<select value={r.deckCount} onChange={e=>update('deckCount',Number(e.target.value))}>
            {options([1,2,4,6,8])}</select></label>
          <label>{t.soft17}<select value={String(r.dealerHitsSoft17)} onChange={e=>update('dealerHitsSoft17',e.target.value==='true')}>
            <option value="false">{t.stand17}</option><option value="true">{t.hit17}</option></select></label>
          <label>{t.dealerHole}<select value={String(r.dealerTakesHoleCard)}
            onChange={e=>{const hole=e.target.value==='true';setR(prev=>({...prev,dealerTakesHoleCard:hole,dealerPeeksForBlackjack:hole&&prev.dealerPeeksForBlackjack}));}}>
            <option value="true">{t.usHole}</option><option value="false">{t.europeanHole}</option></select></label>
          {checkbox('dealerPeeksForBlackjack',t.dealerPeek,!r.dealerTakesHoleCard)}
          {checkbox('allowInsurance',t.insurance)}
          {checkbox('allowLateSurrender',t.lateSurrender)}
          <label>{t.payout}<select value={r.blackjackPayout} onChange={e=>update('blackjackPayout',e.target.value)}>
            <option value="1.5">3:2</option><option value="1.2">6:5</option></select></label>
          <label>{t.doubleDown}<select value={r.doubleRule} onChange={e=>update('doubleRule',e.target.value)}>
            <option value="AnyTwo">{t.anyTwo}</option><option value="NineToEleven">9–11</option>
            <option value="TenToEleven">10–11</option></select></label>
          {checkbox('doubleAfterSplit',t.afterSplit)}
          {checkbox('resplitAces',t.resplitAces)}
          {checkbox('hitSplitAces',t.hitSplitAces)}
          <label>{t.maxSplit}<select value={r.maxSplitHands} onChange={e=>update('maxSplitHands',Number(e.target.value))}>
            {options([2,3,4,5,6,7,8])}</select></label>
          <label>{t.penetration}<select value={r.penetration} onChange={e=>update('penetration',e.target.value)}>
            {options(['0.50','0.55','0.60','0.65','0.70','0.75','0.80','0.85','0.90'])}</select></label>
          <label>{t.minBet}<input value={r.minimumBet} inputMode="decimal" onChange={e=>update('minimumBet',e.target.value)}/></label>
          <label>{t.maxBet}<input value={r.maximumBet} inputMode="decimal" onChange={e=>update('maximumBet',e.target.value)}/></label>
        </div>
        {error&&<p role="alert" className="bj-error">{error}</p>}
        <footer><button type="button" onClick={onClose}>{t.cancel}</button>
          <button className="bj-save" type="submit" data-testid="blackjack-rules-save">{t.useCustom}</button></footer>
      </form>
    </div>
  </div>;
}
