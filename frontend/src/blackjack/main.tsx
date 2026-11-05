import {useCallback,useEffect,useRef,useState} from 'react';
import {createRoot} from 'react-dom/client';
import type {CSSProperties} from 'react';
import {CustomRulesEditor} from './CustomRulesEditor';
import {resolveCardSkin} from './cardSkins';
import type {CardSkin} from './cardSkins';
import {listen,send} from './bridge';
import type {CardFace,TableIntent,TableReply,TableSnapshot} from './bridge';
import './table.css';

const rank:Record<string,string>={Ace:'A',Jack:'J',Queen:'Q',King:'K',Two:'2',Three:'3',Four:'4',Five:'5',Six:'6',Seven:'7',Eight:'8',Nine:'9',Ten:'10'};
const suit:Record<string,string>={Clubs:'♣',Diamonds:'♦',Hearts:'♥',Spades:'♠'};
const money=(s:string)=>s.replace(/\B(?=(\d{3})+(?!\d))/g,',');
const showMoney=(s:string)=>'$'+money(s);
function PlayingCard({card,skin}:{card:CardFace;skin:CardSkin}){
  const styles={
    '--card-face-a':skin.faceA,'--card-face-b':skin.faceB,'--card-border':skin.border,
    '--card-black':skin.black,'--card-red':skin.red,'--card-back-a':skin.backA,
    '--card-back-b':skin.backB,'--card-accent':skin.accent,
  } as CSSProperties;
  if(card.faceDown)return <div className={'bj-card back'+(skin.premium?' premium':'')}
    style={styles} role="img" aria-label="Face-down card"><span>{skin.mark}</span></div>;
  return <div style={styles} className={'bj-card'+(card.suit==='Hearts'||card.suit==='Diamonds'?' red':'')+(skin.dark?' dark':'')+(skin.premium?' premium':'')}
    role="img" aria-label={(card.rank??'')+' of '+(card.suit??'')}>
    <span className="bj-rank">{rank[card.rank??'']??card.rank}</span>
    <span className="bj-suit">{suit[card.suit??'']}</span>
    {skin.premium&&<span className="bj-ornament" aria-hidden="true">{skin.mark}</span>}
  </div>;
}
function BlackjackApp(){
  const [snapshot,setSnapshot]=useState<TableSnapshot|null>(null);
  const [pending,setPending]=useState<string|null>(null);
  const [message,setMessage]=useState<string|null>(null);
  const [uncertain,setUncertain]=useState(false);
  const [bet,setBet]=useState('25');
  const [rulesOpen,setRulesOpen]=useState(false);
  const [resetOpen,setResetOpen]=useState(false);
  const current=useRef<TableSnapshot|null>(null),pendingRef=useRef<string|null>(null);
  const accept=useCallback((reply:TableReply)=>{
    if(reply.requestId!==pendingRef.current)return;
    pendingRef.current=null;setPending(null);setUncertain(false);setMessage(reply.message);
  },[]);
  useEffect(()=>{
    const stop=listen(event=>{
      if(event.type==='result')accept(event.data);
      else{
        if(current.current&&current.current.session===event.data.session&&event.data.revision<current.current.revision)return;
        current.current=event.data;setSnapshot(event.data);
        if(document.activeElement?.getAttribute('data-wager')!=='true')setBet(event.data.state.selectedBet);
      }
    });
    window.chrome?.webview?.postMessage({type:'blackjack.ready'});
    return stop;
  },[accept]);
  useEffect(()=>{if(snapshot)window.chrome?.webview?.postMessage({type:'blackjack.ui-ready'});},[!!snapshot]);
  useEffect(()=>{
    if(!pending)return;
    const id=setTimeout(()=>{setUncertain(true);setMessage('Confirmation missing. Reopen the table before any further action.');},8000);
    return()=>clearTimeout(id);
  },[pending]);
  const act=useCallback((intent:TableIntent)=>{
    const state=current.current;if(!state||pendingRef.current||uncertain)return;
    const id=send(state,intent);pendingRef.current=id;setPending(id);setMessage(null);
  },[uncertain]);
  const close=useCallback(()=>{
    if(current.current?.state.round?.phase==='PlayerTurn'||current.current?.state.round?.phase==='InsuranceDecision'){
      setMessage('Finish the current hand before returning to the lobby.');return;
    }
    window.chrome?.webview?.postMessage({type:'blackjack.close'});
  },[]);
  useEffect(()=>{
    const keyboard=(event:KeyboardEvent)=>{
      if(event.repeat||event.altKey||event.ctrlKey||event.metaKey)return;
      if(document.activeElement instanceof HTMLInputElement||document.activeElement instanceof HTMLSelectElement)return;
      if(event.key==='Escape'){event.preventDefault();close();return;}
      if(event.key==='Enter'&&current.current?.state.canDeal){event.preventDefault();act({action:'deal'});return;}
      const map:Record<string,[string,string]>={h:['hit','Hit'],s:['stand','Stand'],d:['double','DoubleDown'],p:['split','Split'],r:['surrender','Surrender']};
      const entry=map[event.key.toLowerCase()];
      if(entry&&current.current?.state.availableActions.includes(entry[1])){event.preventDefault();act({action:entry[0]});}
    };
    document.addEventListener('keydown',keyboard);
    return()=>document.removeEventListener('keydown',keyboard);
  },[act,close]);
  if(!snapshot)return <div className="bj-loading"><span>♠</span><h1>BLACKJACK</h1>
    <p>{window.chrome?.webview?'Opening your table…':'Open Blackjack in the desktop application.'}</p>
    <button onClick={close}>← Casino lobby</button></div>;
  const s=snapshot.state,round=s.round,locked=!!pending||uncertain,inHand=!!round&&round.phase!=='Settled';
  const skin=resolveCardSkin(s.cardVariant);
  const allowed=(key:string)=>!locked&&s.availableActions.includes(key);
  const commitBet=()=>{
    const value=bet.trim().replace(',','.');
    if(!/^\d+(?:\.\d{1,2})?$/.test(value)){setMessage('Enter a valid wager.');return;}
    if(Number(value)>Number(s.bankroll)||Number(value)>Number(s.rules.maximumBet)){setMessage('Wager exceeds the table limit or available bankroll.');return;}
    if(value!==s.selectedBet)act({action:'bet.set',value});
  };
  return <div className="bj-app" data-theme={s.theme} data-motion={s.animationsEnabled} data-phase={round?.phase??"none"}>
    <header><button onClick={close} className="bj-back">← Casino lobby</button>
      <div className="bj-logo"><small>THE MIDNIGHT CLUB</small><h1>BLACKJACK</h1></div>
      <div className="bj-money"><small>BANKROLL</small><strong>{showMoney(s.bankroll)}</strong></div>
    </header>
    <div className="bj-layout">
      <main className="bj-table">
        <div className="bj-felt">
          <section className="bj-dealer"><small>DEALER · SHOE {s.shoeNumber} · {s.cardsRemaining} CARDS</small>
            <div className="bj-cards">{round?.dealerCards.map((card,i)=><PlayingCard card={card} skin={skin} key={i}/>)??<em>Waiting for your bet</em>}</div>
            {round?.dealerValue!=null&&<span>Dealer {round.dealerValue}</span>}
          </section>
          {round&&round.aiSeats.length>0&&<div className="bj-ai">
            {round.aiSeats.map((seat,i)=><div className="bj-ai-seat" key={i}><small>{seat.name}</small>
              {seat.hands.map((hand,j)=><div key={j}><div className="bj-cards compact">
                {hand.cards.map((card,k)=><PlayingCard card={card} skin={skin} key={k}/>)}</div>
                <small>{showMoney(hand.bet)} · {hand.value} {hand.outcome??''}</small></div>)}</div>)}
          </div>}
          <section className="bj-player"><small>YOUR SEAT</small><div className="bj-hands">
            {round?round.playerHands.map(hand=><article key={hand.index} className={'bj-hand'+(hand.active?' active':'')}>
              <div className="bj-hand-heading"><span>{round.playerHands.length>1?'HAND '+(hand.index+1):'YOUR HAND'}</span>
                <strong>{showMoney(hand.bet)}</strong></div>
              <div className="bj-cards">{hand.cards.map((card,j)=><PlayingCard key={j} card={card} skin={skin}/>)}</div>
              <p>{hand.value}{hand.isSoft?' (soft)':''} · {hand.outcome??(hand.active?'Your move':'')}
                {hand.net!==null?' · '+showMoney(hand.net):''}</p>
            </article>):<div className="bj-empty">Place a wager and deal to begin.</div>}
          </div></section>
          <div className="bj-status" role="status">{message??s.status}
            {round?.totalNet!=null&&<strong> · NET {showMoney(round.totalNet)}</strong>}</div>
          <div className="bj-actions">
            {round?.phase==='InsuranceDecision'?<>
              <button disabled={locked} onClick={()=>act({action:'insurance.take'})}>TAKE INSURANCE</button>
              <button disabled={locked} onClick={()=>act({action:'insurance.decline'})}>NO INSURANCE</button>
            </>:<>
              <button disabled={!allowed('Hit')} onClick={()=>act({action:'hit'})}>HIT <kbd>H</kbd></button>
              <button disabled={!allowed('Stand')} onClick={()=>act({action:'stand'})}>STAND <kbd>S</kbd></button>
              <button disabled={!allowed('DoubleDown')} onClick={()=>act({action:'double'})}>DOUBLE <kbd>D</kbd></button>
              <button disabled={!allowed('Split')} onClick={()=>act({action:'split'})}>SPLIT <kbd>P</kbd></button>
              <button disabled={!allowed('Surrender')} onClick={()=>act({action:'surrender'})}>SURRENDER <kbd>R</kbd></button>
            </>}
          </div>
        </div>
      </main>
      <aside className="bj-sidebar">
        <section><small>YOUR WAGER</small><strong className="bj-bet">{showMoney(s.selectedBet)}</strong>
          <div className="bj-chips">{[5,25,100,500].map(amount=>
            <button key={amount} disabled={locked||inHand}
              onClick={()=>act({action:'bet.set',value:String(Math.min(Number(s.selectedBet)+amount,Number(s.bankroll),Number(s.rules.maximumBet)))})}>+{amount}</button>)}</div>
          <label>Exact wager<input data-wager="true" value={bet} inputMode="decimal" disabled={locked||inHand}
            onChange={e=>setBet(e.target.value)} onBlur={commitBet}
            onKeyDown={e=>{if(e.key==='Enter'){e.preventDefault();commitBet();e.currentTarget.blur();}}}/></label>
          <button disabled={locked||inHand} onClick={()=>act({action:'bet.clear'})}>Clear wager</button>
          <button className="bj-deal" data-testid="blackjack-deal" disabled={locked||!s.canDeal} onClick={()=>act({action:'deal'})}>DEAL <kbd>ENTER</kbd></button>
          <small>LIMITS {showMoney(s.rules.minimumBet)} – {showMoney(s.rules.maximumBet)}</small>
        </section>
        <section><small>TABLE CONFIGURATION</small>
          <label>Game mode<select disabled={locked||inHand} value={s.fullTable?'full':'solo'}
            onChange={e=>act({action:'mode.set',value:e.target.value})}>
            <option value="solo">Solo</option><option value="full">Full table (AI)</option></select></label>
          {s.fullTable&&<label>AI seats<select disabled={locked||inHand} value={s.aiSeatCount}
            onChange={e=>act({action:'seats.set',value:e.target.value})}>{[1,2,3,4,5].map(n=><option value={n} key={n}>{n}</option>)}</select></label>}
          <label>Table preset<select disabled={locked||inHand} value={s.preset}
            onChange={e=>act({action:'preset.set',value:e.target.value})}>
            {s.presets.map(p=><option key={p.id} value={p.id}>{p.name}</option>)}
            {s.preset==='Custom'&&<option value="Custom">Custom</option>}</select></label>
          <div className="bj-rule">{s.rules.deckCount}D · {s.rules.dealerHitsSoft17?'H17':'S17'} · {s.rules.blackjackPayout}:1 · {s.rules.dealerTakesHoleCard?'Hole card':'ENHC'}</div>
          <button type="button" data-testid="blackjack-edit-rules" disabled={locked||inHand}
            onClick={()=>setRulesOpen(true)}>Edit custom rules</button>
          <div className="bj-rule">Equipped cards: {s.cardVariant.replace(/^card-/,'').replace(/-/g,' ')}</div>
          <label>Felt<select value={s.theme} disabled={locked} onChange={e=>act({action:'theme.set',value:e.target.value})}>
            <option value="ClassicGreen">Green</option><option value="MidnightBlue">Midnight blue</option><option value="Burgundy">Burgundy</option></select></label>
          <label className="bj-check"><input type="checkbox" checked={s.soundEnabled} disabled={locked}
            onChange={e=>act({action:'sound.set',value:String(e.target.checked)})}/> Sound</label>
          <label className="bj-check"><input type="checkbox" checked={s.animationsEnabled} disabled={locked}
            onChange={e=>act({action:'animations.set',value:String(e.target.checked)})}/> Animations</label>
          <button type="button" disabled={locked||inHand}
            onClick={()=>setResetOpen(true)}>Reset play-money bankroll</button>
        </section>
      </aside>
    </div>
    {rulesOpen&&<CustomRulesEditor initial={s.rules}
      onSave={rules=>{setRulesOpen(false);act({action:'rules.custom.set',value:JSON.stringify(rules)});}}
      onClose={()=>setRulesOpen(false)}/>}
    {resetOpen&&<div className="bj-modal-wrap"><div className="bj-confirm" role="dialog" aria-modal="true" aria-labelledby="bj-reset-title">
      <h2 id="bj-reset-title">Reset local bankroll?</h2>
      <p>Your play-money bankroll will return to $1,000. This cannot be undone.</p>
      <div><button onClick={()=>setResetOpen(false)}>Cancel</button>
      <button data-testid="blackjack-reset-confirm" onClick={()=>{
        setResetOpen(false);act({action:'bankroll.reset'});}}>Reset bankroll</button></div></div></div>}
  </div>;
}
createRoot(document.getElementById('root')!).render(<BlackjackApp/>);
