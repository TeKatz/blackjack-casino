import {useEffect,useRef,useState} from 'react';
import type {PointerEvent as ReactPointerEvent} from 'react';
import type {ScratchcardSnapshot,ScratchcardTicket} from './scratchcardsBridge';
import {clearedFraction,SCRATCH_BRUSH_WIDTH,SCRATCH_REVEAL_COVERAGE,winningCellIndexes} from './scratchcardsPresentation';
import {playScratchcardWin,unlockScratchcardAudio} from './scratchcardsAudio';
import './scratchcards.css';

type Props={
  data:ScratchcardSnapshot; loaded:boolean; embedded:boolean; language:'en'|'de';
  pending:'buy'|'collect'|null; notice:{success:boolean;message:string}|null; motionEnabled?:boolean;
  onBack:()=>void;onBuy:()=>void;onCollect:(id:string)=>void;
};
const motifs=['♠','♦','♣','♥','7'];
const prizes=[{amount:0,odds:'50%'},{amount:10,odds:'20%'},{amount:20,odds:'15%'},
  {amount:50,odds:'10%'},{amount:100,odds:'4%'},{amount:250,odds:'0.9%'},
  {amount:1000,odds:'0.1%'}];
const money=(n:number)=>new Intl.NumberFormat('en-US',{style:'currency',currency:'USD'}).format(n);

function ScratchCell({index,symbol,revealed,disabled,winning,onReveal,onScratch,onStart}:{
  index:number;symbol:number;revealed:boolean;disabled:boolean;winning:boolean;
  onReveal:(index:number)=>void;onScratch:(clientX:number,clientY:number)=>void;onStart:()=>void;
}){
  const canvas=useRef<HTMLCanvasElement>(null);
  const dragging=useRef(false);
  const last=useRef<{x:number;y:number}|null>(null);
  const lastScan=useRef(0);
  useEffect(()=>{
    if(revealed)return;
    const surface=canvas.current,context=surface?.getContext('2d');
    if(!surface||!context)return;
    const w=surface.width,h=surface.height;
    const metal=context.createLinearGradient(0,0,w,h);
    metal.addColorStop(0,'#f9e6b9');metal.addColorStop(.28,'#a4b4ac');
    metal.addColorStop(.55,'#d7dfd4');metal.addColorStop(.76,'#91a9a1');
    metal.addColorStop(1,'#f0d6a4');
    context.globalCompositeOperation='source-over';
    context.fillStyle=metal;context.fillRect(0,0,w,h);
    context.strokeStyle='#ffffff44';context.lineWidth=3;
    for(let x=-h;x<w+h;x+=22){
      context.beginPath();context.moveTo(x,0);context.lineTo(x+h,h);context.stroke();
    }
    context.fillStyle='#315047';context.font='bold 18px Segoe UI';
    context.textAlign='center';context.textBaseline='middle';
    context.fillText('✦',w/2,h/2);
    last.current=null;lastScan.current=0;
  },[index,revealed]);
  const checkCoverage=(force=false)=>{
    const surface=canvas.current,context=surface?.getContext('2d',{willReadFrequently:true});
    if(!surface||!context)return;
    const now=performance.now();
    if(!force&&now-lastScan.current<65)return;
    lastScan.current=now;
    const data=context.getImageData(0,0,surface.width,surface.height).data;
    if(clearedFraction(data,surface.width,surface.height)>=SCRATCH_REVEAL_COVERAGE){
      dragging.current=false;
      last.current=null;
      onReveal(index);
    }
  };
  const erase=(event:ReactPointerEvent<HTMLButtonElement>)=>{
    const surface=canvas.current,context=surface?.getContext('2d');
    if(!surface||!context)return;
    const rect=surface.getBoundingClientRect();
    if(!rect.width||!rect.height)return;
    const point={
      x:Math.max(0,Math.min(surface.width,(event.clientX-rect.left)/rect.width*surface.width)),
      y:Math.max(0,Math.min(surface.height,(event.clientY-rect.top)/rect.height*surface.height)),
    };
    context.globalCompositeOperation='destination-out';
    if(last.current){
      context.lineWidth=SCRATCH_BRUSH_WIDTH;context.lineCap='round';context.lineJoin='round';
      context.beginPath();
      context.moveTo(last.current.x,last.current.y);
      context.lineTo(point.x,point.y);
      context.stroke();
    }else{
      context.beginPath();context.arc(point.x,point.y,SCRATCH_BRUSH_WIDTH/2,0,Math.PI*2);
      context.fill();
    }
    last.current=point;
    onScratch(event.clientX,event.clientY);
    checkCoverage();
  };
  const down=(event:ReactPointerEvent<HTMLButtonElement>)=>{
    if(disabled||revealed)return;
    onStart();
    dragging.current=true;last.current=null;
    event.currentTarget.setPointerCapture(event.pointerId);
    erase(event);
  };
  const move=(event:ReactPointerEvent<HTMLButtonElement>)=>{
    if(dragging.current&&!disabled&&!revealed)erase(event);
  };
  const end=()=>{
    if(dragging.current&&!disabled&&!revealed)checkCoverage(true);
    dragging.current=false;last.current=null;
  };
  return <button type="button"
    className={'scratch-cell '+(revealed?'revealed':'covered')+(winning?' winning':'')}
    disabled={disabled||revealed}
    aria-label={revealed?('Field '+(index+1)+': '+motifs[symbol]):('Scratch field '+(index+1))}
    onPointerDown={down} onPointerMove={move} onPointerUp={end}
    onPointerCancel={end} onLostPointerCapture={end}
    onClick={event=>{if(event.detail===0&&!disabled&&!revealed){onStart();onReveal(index);}}}>
    <span className={'scratch-symbol scratch-symbol-'+symbol} aria-hidden={!revealed}>
      {motifs[symbol]}</span>
    <canvas ref={canvas} width={170} height={105} aria-hidden="true"/>
  </button>;
}
const allFields=[0,1,2,3,4,5,6,7,8];
/** Particles are transient DOM visuals, capped by time and cleaned up when the ticket changes. */
function TicketBoard({ticket,revealed,onReveal,ready,winning,motionEnabled,onStart}:{
 ticket:ScratchcardTicket|null;revealed:number[];ready:boolean;winning:number[];
 motionEnabled:boolean;onStart:()=>void;onReveal:(index:number)=>void;
}){
  const particles=useRef<HTMLDivElement>(null);
  const lastParticle=useRef(0);
  useEffect(()=>{
    const layer=particles.current;
    return ()=>{layer?.replaceChildren();};
  },[ticket?.ticketId]);
  const scratchParticles=(clientX:number,clientY:number)=>{
    const layer=particles.current;
    if(!motionEnabled||!layer||typeof Element.prototype.animate!=='function')return;
    const now=performance.now();
    if(now-lastParticle.current<38)return;
    lastParticle.current=now;
    const box=layer.getBoundingClientRect();
    const x=clientX-box.left,y=clientY-box.top;
    if(x<0||x>box.width||y<0||y>box.height)return;
    for(let i=0;i<3;i++){
      const flake=document.createElement('i');
      flake.className='scratch-silver-flake';
      const spread=(Math.random()-.5)*19,fall=24+Math.random()*35;
      const drift=(Math.random()-.5)*58;
      flake.style.left=(x+spread)+'px';flake.style.top=(y-5)+'px';
      layer.append(flake);
      const animation=flake.animate([
        {transform:'translate3d(0,0,0) rotate(0deg) scale(1)',opacity:.96},
        {transform:'translate3d('+drift+'px,'+fall+'px,0) rotate('+((i+1)*95)+'deg) scale(.3)',opacity:0},
      ],{duration:480+Math.random()*310,easing:'cubic-bezier(.17,.7,.36,1)',fill:'forwards'});
      animation.onfinish=()=>flake.remove();
      animation.oncancel=()=>flake.remove();
    }
  };
  return <div className={'scratch-ticket '+(ticket?.collected?'ticket-collected':'')+
    (winning.length?' is-winning':'')} data-testid="scratch-ticket">
    <div className="scratch-ticket-header">
      <span className="scratch-ticket-edition">THE MIDNIGHT CLUB · SERIES 01</span>
      <span className="scratch-ticket-stamp">✦ 07 ✦</span>
    </div>
    <div className="scratch-ticket-title"><span>LUCKY</span><strong>SEVENS</strong></div>
    <p>THREE MATCHING SYMBOLS · ONE PRIZE</p>
    <div className="scratch-ticket-grid">
      {allFields.map(index=><ScratchCell key={(ticket?.ticketId||'empty')+'-'+index}
        index={index} symbol={ticket?.symbols[index]??index%5}
        revealed={ticket===null||revealed.includes(index)}
        disabled={!ticket||!ready||ticket.collected} winning={winning.includes(index)}
        onReveal={onReveal} onScratch={scratchParticles} onStart={onStart}/>)}
    </div>
    <div className="scratch-silver-layer" ref={particles} aria-hidden="true"/>
    <div className="scratch-ticket-footer"><span>NO. {ticket?ticket.ticketId.slice(0,8).toUpperCase():'— — — —'}</span>
      <b>◆</b><span>PLAY MONEY ONLY</span></div>
  </div>;
}
export function ModernScratchcards({data,loaded,embedded,language,pending,notice,
  motionEnabled=true,onBack,onBuy,onCollect}:Props){
  const de=language==='de';
  const ticket=data.ticket;
  const [revealed,setRevealed]=useState<number[]>([]);
  const [soundEnabled,setSoundEnabled]=useState(true);
  const soundPlayedFor=useRef<string|null>(null);
  useEffect(()=>{
    setRevealed(ticket?.collected?[...allFields]:[]);
    soundPlayedFor.current=null;
  },[ticket?.ticketId,ticket?.collected]);
  const allRevealed=!!ticket&&revealed.length===9;
  const winning=allRevealed&&ticket?winningCellIndexes(ticket.symbols,ticket.prize):[];
  const hasWin=winning.length===3;
  const reveal=(index:number)=>{
    if(!ticket||ticket.collected||revealed.includes(index))return;
    const next=[...revealed,index];
    setRevealed(next);
    if(next.length===9 && ticket.prize>0 && soundPlayedFor.current!==ticket.ticketId){
      soundPlayedFor.current=ticket.ticketId;
      playScratchcardWin(soundEnabled);
    }
  };
  const revealAll=()=>{
    if(!ticket||ticket.collected)return;
    unlockScratchcardAudio(soundEnabled);
    if(revealed.length<9 && ticket.prize>0 && soundPlayedFor.current!==ticket.ticketId){
      soundPlayedFor.current=ticket.ticketId;
      playScratchcardWin(soundEnabled);
    }
    setRevealed([...allFields]);
  };
  const canPurchase=embedded&&loaded&&!pending&&!data.recoveryRequired&&
    (!ticket||ticket.collected)&&data.bankroll>=data.price;
  const canCollect=!!ticket&&!ticket.collected&&allRevealed&&!pending&&!data.recoveryRequired;
  const status=ticket?.collected?(ticket.prize>0
    ?de?'Gewinn gutgeschrieben':'Prize paid out'
    :de?'Los abgeschlossen':'Ticket completed')
    :ticket&&allRevealed?(ticket.prize>0
      ?de?'Gewonnen! Jetzt einlösen.':'Winner! Collect your prize.'
      :de?'Leider kein Gewinn. Los abschließen.':'No prize this time. Finish the ticket.')
    :ticket?(de?'Rubbele alle neun Felder frei.':'Scratch all nine fields to reveal your result.')
    :(de?'Kaufe ein Los und entdecke dein Glück.':'Purchase a ticket to begin.');
  return <main className="scratch-page" data-motion={motionEnabled?'on':'off'}
    aria-label={de?'Rubbellose':'Scratchcards'}>
    <div className="scratch-scroll">
      <header className="scratch-hero">
        <div>
          <button type="button" className="shop-back" onClick={onBack}>
            {de?'‹ Zur Lobby':'‹ Back to lobby'}</button>
          <span className="shop-eyebrow">THE MIDNIGHT CLUB / INSTANT WIN</span>
          <h1>{de?'Rubbellose':'Scratchcards'}</h1>
          <p>{de?'Ein kleines Stück Casino-Magie. Ein Los, neun verdeckte Symbole und eine echte Chance auf Spielgeld.':
            'A little casino magic. One ticket, nine concealed symbols and a chance to win play money.'}</p>
        </div>
        <div className="scratch-hero-wallet">
          <span>{de?'DEIN SPIELGELD':'YOUR BANKROLL'}</span>
          <strong>{money(data.bankroll)}</strong><small>{de?'PREIS PRO LOS':'TICKET PRICE'} · {money(data.price)}</small>
        </div>
      </header>
      <div className="scratch-layout">
        <section className={'scratch-stage '+(hasWin?'scratch-stage-win':'')}
          aria-label={de?'Rubbellos':'Scratch ticket'}>
          <div className="scratch-stage-header"><span>01 / LUCKY SEVENS</span>
            <strong>{ticket&&!ticket.collected?(de?'AKTIVES LOS':'ACTIVE TICKET'):'SERIES 01'}</strong></div>
          <TicketBoard ticket={ticket} revealed={revealed} ready={loaded}
            winning={winning} motionEnabled={motionEnabled} onReveal={reveal}
            onStart={()=>unlockScratchcardAudio(soundEnabled)}/>
          <div className={'scratch-status '+(hasWin?'scratch-status-winning':'')} role="status">
            <span className="scratch-status-symbol">{ticket&&allRevealed?(ticket.prize>0?'✦':'◆'):'✧'}</span>
            <div><strong>{ticket&&allRevealed?money(ticket.prize):de?'Verdeckter Gewinn':'Hidden prize'}</strong>
              <p>{status}</p></div>
          </div>
          {notice&&!notice.success&&<p className="scratch-error" role="alert">{notice.message}</p>}
          {data.recoveryRequired&&<p className="scratch-error" role="alert">
            {de?'Speichervorgang unklar. Bitte das Casino neu starten, bevor du weiterspielst.':
              'Save recovery required. Restart the casino before playing again.'}</p>}
          {!embedded&&<p className="scratch-hint">
            {de?'Im Browser nur Vorschau. Kaufen und Gewinne funktionieren im Windows-Programm.':
              'Browser preview only. Purchases and payouts require the Windows app.'}</p>}
          <div className="scratch-actions">
            <button type="button" className="scratch-primary" disabled={!canPurchase} onClick={onBuy}>
              {pending==='buy'?'…':de?'LOS KAUFEN · '+money(data.price):'BUY TICKET · '+money(data.price)}
            </button>
            <button type="button" className="scratch-secondary" disabled={!ticket||allRevealed||!loaded||ticket.collected}
              onClick={revealAll}>
              {de?'Alles aufdecken':'Reveal all'}
            </button>
            <button type="button" className="scratch-sound" aria-pressed={soundEnabled}
              onClick={()=>setSoundEnabled(value=>!value)}>
              {soundEnabled?(de?'♪ Ton an':'♪ Sound on'):(de?'♪ Ton aus':'♪ Sound off')}
            </button>
            {ticket&&!ticket.collected&&<button type="button" className="scratch-collect"
              disabled={!canCollect} onClick={()=>onCollect(ticket.ticketId)}>
              {pending==='collect'?'…':ticket.prize>0
                ?(de?'GEWINN EINLÖSEN':'COLLECT PRIZE')
                :(de?'LOS ABSCHLIESSEN':'FINISH TICKET')}</button>}
          </div>
        </section>
        <aside className="scratch-info">
          <section className="scratch-info-card">
            <span className="shop-eyebrow">HOW TO PLAY</span>
            <h2>{de?'So funktioniert es':'How it works'}</h2>
            <p>{de?'Kaufe ein Lucky-Sevens-Los für 20 $ Spielgeld und rubbele die neun Felder mit der Maus oder per Touch frei.':
              'Buy a Lucky Sevens ticket for $20 of play money and scratch the nine fields with your mouse or touch.'}</p>
            <p>{de?'Drei gleiche Symbole an beliebigen Stellen gewinnen den auf dem Los festgelegten Betrag. Bei einem Gewinn kannst du ihn anschließend einlösen.':
              'Three matching symbols anywhere win the predetermined ticket prize. Collect your payout after revealing every field.'}</p>
          </section>
          <section className="scratch-info-card">
            <span className="shop-eyebrow">PAYOUT TABLE</span>
            <h2>{de?'Gewinne & Chancen':'Prizes & odds'}</h2>
            <div className="scratch-odds">
              <div className="scratch-odds-heading"><span>{de?'Gewinn':'Prize'}</span>
                <span>{de?'Chance':'Chance'}</span></div>
              {prizes.map(entry=><div key={entry.amount} className={(entry.amount===1000?'scratch-jackpot-row ':'')+
                (hasWin&&ticket?.prize===entry.amount?'scratch-odds-winner':'')}>
                <strong>{entry.amount===0?(de?'Niete':'No win'):money(entry.amount)}</strong>
                <span>{entry.odds}</span></div>)}
            </div>
            <p className="scratch-fineprint">
              {de?'Erwartete Rückzahlung: 86,25 %. Nur virtuelles Spielgeld, keine Echtgeldgewinne.':
                'Theoretical return: 86.25%. Virtual play money only; no real-money prizes.'}</p>
          </section>
        </aside>
      </div>
    </div>
  </main>;
}
