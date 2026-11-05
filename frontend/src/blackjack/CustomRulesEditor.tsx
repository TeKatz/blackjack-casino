import {useState} from 'react';
import type {TableState} from './bridge';

type Rules=TableState['rules'];
export type EditableRules=Omit<Rules,'penetration'> & {penetration:string};

export function CustomRulesEditor({initial,onSave,onClose}:{
  initial:Rules;onSave:(rules:EditableRules)=>void;onClose:()=>void;
}){
  const [r,setR]=useState<EditableRules>({...initial,penetration:initial.penetration.toFixed(2)});
  const [error,setError]=useState('');
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
      setError('Use valid limits between 0.01 and 1,000,000 (maximum at least minimum).');return;
    }
    onSave({...r,dealerPeeksForBlackjack:r.dealerTakesHoleCard&&r.dealerPeeksForBlackjack});
  };
  return <div className="bj-modal-wrap" role="presentation">
    <div className="bj-modal" role="dialog" aria-modal="true" aria-labelledby="bj-rules-title" data-testid="blackjack-custom-rules">
      <form onSubmit={apply}>
        <header><div><small>THE MIDNIGHT CLUB</small><h2 id="bj-rules-title">Custom table rules</h2>
          <p>Changes apply to the next round. CasinoSession remains the game authority.</p></div>
          <button type="button" onClick={onClose} aria-label="Close rules">×</button></header>
        <div className="bj-rule-grid">
          <label>Decks<select value={r.deckCount} onChange={e=>update('deckCount',Number(e.target.value))}>
            {options([1,2,4,6,8])}</select></label>
          <label>Dealer on soft 17<select value={String(r.dealerHitsSoft17)} onChange={e=>update('dealerHitsSoft17',e.target.value==='true')}>
            <option value="false">Stand (S17)</option><option value="true">Hit (H17)</option></select></label>
          <label>Dealer hole card<select value={String(r.dealerTakesHoleCard)}
            onChange={e=>{const hole=e.target.value==='true';setR(prev=>({...prev,dealerTakesHoleCard:hole,dealerPeeksForBlackjack:hole&&prev.dealerPeeksForBlackjack}));}}>
            <option value="true">US hole card</option><option value="false">European no-hole-card</option></select></label>
          {checkbox('dealerPeeksForBlackjack','Dealer peeks for blackjack',!r.dealerTakesHoleCard)}
          {checkbox('allowInsurance','Insurance available')}
          {checkbox('allowLateSurrender','Late surrender')}
          <label>Blackjack payout<select value={r.blackjackPayout} onChange={e=>update('blackjackPayout',e.target.value)}>
            <option value="1.5">3:2</option><option value="1.2">6:5</option></select></label>
          <label>Double down<select value={r.doubleRule} onChange={e=>update('doubleRule',e.target.value)}>
            <option value="AnyTwo">Any first two</option><option value="NineToEleven">9–11</option>
            <option value="TenToEleven">10–11</option></select></label>
          {checkbox('doubleAfterSplit','Double after split')}
          {checkbox('resplitAces','Resplit aces')}
          {checkbox('hitSplitAces','Hit split aces')}
          <label>Max split hands<select value={r.maxSplitHands} onChange={e=>update('maxSplitHands',Number(e.target.value))}>
            {options([2,3,4,5,6,7,8])}</select></label>
          <label>Deck penetration<select value={r.penetration} onChange={e=>update('penetration',e.target.value)}>
            {options(['0.50','0.55','0.60','0.65','0.70','0.75','0.80','0.85','0.90'])}</select></label>
          <label>Minimum wager<input value={r.minimumBet} inputMode="decimal" onChange={e=>update('minimumBet',e.target.value)}/></label>
          <label>Maximum wager<input value={r.maximumBet} inputMode="decimal" onChange={e=>update('maximumBet',e.target.value)}/></label>
        </div>
        {error&&<p role="alert" className="bj-error">{error}</p>}
        <footer><button type="button" onClick={onClose}>Cancel</button>
          <button className="bj-save" type="submit" data-testid="blackjack-rules-save">Use custom rules</button></footer>
      </form>
    </div>
  </div>;
}
