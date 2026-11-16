import {useEffect,useRef,useState} from 'react';
import type {PointerEvent as ReactPointerEvent} from 'react';
import type {ScratchcardGame,ScratchcardSnapshot,ScratchcardTicket} from './scratchcardsBridge';
import {clearedFraction,revealedIndexesForTicket,SCRATCH_BRUSH_WIDTH,SCRATCH_REVEAL_COVERAGE,strokeOverlapsRect,winningCellIndexes} from './scratchcardsPresentation';
import {playScratchcardWin,unlockScratchcardAudio} from './scratchcardsAudio';
import {ScratchcardMotif,scratchSymbolLabel} from './ScratchcardMotifs';
import './scratchcards.css';

type Props={
  data:ScratchcardSnapshot; loaded:boolean; embedded:boolean; language:'en'|'de';
  pending:'buy'|'collect'|null; notice:{success:boolean;message:string}|null; motionEnabled?:boolean;
  onBack:()=>void;onBuy:(gameId:string)=>void;onCollect:(id:string)=>void;
};
const tiers:Record<string,{series:string;stamp:string;rarity:string}>={
  'lucky-sevens':{series:'01',stamp:'✦ 07 ✦',rarity:'SERIES ONE'},
  'gold-rush':{series:'02',stamp:'✦ AU ✦',rarity:'GOLD EDITION'},
  'platinum-vault':{series:'03',stamp:'✧ PT ✧',rarity:'PLATINUM EDITION'},
  'diamond-royale':{series:'04',stamp:'✧ DR ✧',rarity:'ROYAL DIAMOND EDITION'},
};
const foilFinishes:Record<string,readonly string[]>={
  'lucky-sevens':['#f9e6b9','#a4b4ac','#d7dfd4','#91a9a1','#f0d6a4','#315047'],
  'gold-rush':['#fff2b5','#b89044','#ebcb7a','#9c7132','#fff0ad','#6d4b18'],
  'platinum-vault':['#fcffff','#8babb9','#e9f5f4','#94a9bf','#f6fcff','#44606f'],
  'diamond-royale':['#ffffff','#98bbd9','#dfedf9','#b3b1e0','#e8ffff','#4b5d94'],
};
const basePrizes=[{amount:0,odds:'50%'},{amount:10,odds:'20%'},{amount:20,odds:'15%'},
  {amount:50,odds:'10%'},{amount:100,odds:'4%'},{amount:250,odds:'0.9%'},
  {amount:1000,odds:'0.1%'}];
const money=(n:number)=>new Intl.NumberFormat('en-US',{style:'currency',currency:'USD'}).format(n);

function ScratchCell({gameId,index,symbol,revealed,disabled,winning,onReveal,onStart}:{
  gameId:string;index:number;symbol:number;revealed:boolean;disabled:boolean;winning:boolean;
  onReveal:(index:number)=>void;onStart:()=>void;
}){
  const canvas=useRef<HTMLCanvasElement>(null);
  useEffect(()=>{
    if(revealed)return;
    const surface=canvas.current,context=surface?.getContext('2d',{willReadFrequently:true});
    if(!surface||!context)return;
    const w=surface.width,h=surface.height;
    const metal=context.createLinearGradient(0,0,w,h);
    const finish=foilFinishes[gameId]??foilFinishes['lucky-sevens'];
    metal.addColorStop(0,finish[0]);metal.addColorStop(.28,finish[1]);
    metal.addColorStop(.55,finish[2]);metal.addColorStop(.76,finish[3]);
    metal.addColorStop(1,finish[4]);
    context.globalCompositeOperation='source-over';
    context.fillStyle=metal;context.fillRect(0,0,w,h);
    context.strokeStyle='#ffffff44';context.lineWidth=3;
    for(let x=-h;x<w+h;x+=22){
      context.beginPath();context.moveTo(x,0);context.lineTo(x+h,h);context.stroke();
    }
    context.fillStyle=finish[5];context.font='bold 18px Segoe UI';
    context.textAlign='center';context.textBaseline='middle';
    context.fillText('✦',w/2,h/2);
  },[gameId,index,revealed]);
  return <button type="button"
    className={'scratch-cell '+(revealed?'revealed':'covered')+(winning?' winning':'')}
    disabled={disabled||revealed}
    aria-label={revealed?('Field '+(index+1)+': '+scratchSymbolLabel(gameId,symbol)):('Scratch field '+(index+1))}
    onClick={event=>{if(event.detail===0&&!disabled&&!revealed){onStart();onReveal(index);}}}>
    <span className={'scratch-symbol scratch-symbol-'+symbol} aria-hidden="true">
      <ScratchcardMotif gameId={gameId} symbol={symbol}/>
    </span>
    <canvas ref={canvas} data-scratch-index={index} width={170} height={105} aria-hidden="true"/>
  </button>;
}
const allFields=[0,1,2,3,4,5,6,7,8];
/** Pointer debris is temporary and cleaned up when the ticket changes. */
function TicketBoard({game,ticket,revealed,onReveal,ready,winning,motionEnabled,onStart}:{
 game:ScratchcardGame;ticket:ScratchcardTicket|null;revealed:number[];ready:boolean;winning:number[];
 motionEnabled:boolean;onStart:()=>void;onReveal:(index:number)=>void;
}){
  const grid=useRef<HTMLDivElement>(null);
  const particles=useRef<HTMLDivElement>(null);
  const lastParticle=useRef(0);
  const activePointers=useRef<Map<number,{x:number;y:number}>>(new Map());
  const scratchedCells=useRef<Set<number>>(new Set());
  const lastScan=useRef(0);
  useEffect(()=>{
    const layer=particles.current;
    activePointers.current.clear();
    scratchedCells.current.clear();
    lastScan.current=0;
    return ()=>{layer?.replaceChildren();activePointers.current.clear();};
  },[ticket?.ticketId,ticket?.collected]);

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

  const scanCoverage=(force:boolean)=>{
    const surface=grid.current;
    if(!surface||!scratchedCells.current.size)return;
    const now=performance.now();
    if(!force&&now-lastScan.current<65)return;
    lastScan.current=now;
    for(const index of [...scratchedCells.current]){
      if(revealed.includes(index)){scratchedCells.current.delete(index);continue;}
      const canvas=surface.querySelector<HTMLCanvasElement>('canvas[data-scratch-index="'+index+'"]');
      const context=canvas?.getContext('2d',{willReadFrequently:true});
      if(!canvas||!context)continue;
      if(clearedFraction(context.getImageData(0,0,canvas.width,canvas.height).data,
        canvas.width,canvas.height)>=SCRATCH_REVEAL_COVERAGE){
        scratchedCells.current.delete(index);
        onReveal(index);
      }
    }
  };
  /** Global stroke endpoints are mapped separately into each cell's canvas.
      This also paints a cell crossed in a fast sweep, even if no pointer event lands in it. */
  const scratchSegment=(from:{x:number;y:number},to:{x:number;y:number})=>{
    const surface=grid.current;
    if(!surface)return;
    let painted=false;
    surface.querySelectorAll<HTMLCanvasElement>('canvas[data-scratch-index]').forEach(canvas=>{
      const index=Number(canvas.dataset.scratchIndex);
      if(!Number.isInteger(index)||revealed.includes(index)||!ticket||ticket.collected)return;
      const rect=canvas.getBoundingClientRect();
      if(rect.width<=0||rect.height<=0)return;
      const margin=Math.max(SCRATCH_BRUSH_WIDTH*rect.width/canvas.width,
        SCRATCH_BRUSH_WIDTH*rect.height/canvas.height)/2;
      if(!strokeOverlapsRect(from,to,rect,margin))return;
      const context=canvas.getContext('2d',{willReadFrequently:true});
      if(!context)return;
      const x=(p:{x:number;y:number})=>(p.x-rect.left)/rect.width*canvas.width;
      const y=(p:{x:number;y:number})=>(p.y-rect.top)/rect.height*canvas.height;
      context.globalCompositeOperation='destination-out';
      context.lineWidth=SCRATCH_BRUSH_WIDTH;
      context.lineCap='round';
      context.lineJoin='round';
      if(from.x===to.x&&from.y===to.y){
        context.beginPath();context.arc(x(to),y(to),SCRATCH_BRUSH_WIDTH/2,0,Math.PI*2);
        context.fill();
      }else{
        context.beginPath();context.moveTo(x(from),y(from));context.lineTo(x(to),y(to));
        context.stroke();
      }
      painted=true;
      scratchedCells.current.add(index);
    });
    if(painted)scratchParticles(to.x,to.y);
  };
  const pointerDown=(event:ReactPointerEvent<HTMLDivElement>)=>{
    if(!ready||!ticket||ticket.collected||event.pointerType==='mouse'&&event.button!==0)return;
    onStart();
    const current={x:event.clientX,y:event.clientY};
    activePointers.current.set(event.pointerId,current);
    event.currentTarget.setPointerCapture(event.pointerId);
    event.preventDefault();
    scratchSegment(current,current);
    scanCoverage(false);
  };
  const pointerMove=(event:ReactPointerEvent<HTMLDivElement>)=>{
    const previous=activePointers.current.get(event.pointerId);
    if(!previous)return;
    const current={x:event.clientX,y:event.clientY};
    scratchSegment(previous,current);
    activePointers.current.set(event.pointerId,current);
    scanCoverage(false);
  };
  const pointerUp=(event:ReactPointerEvent<HTMLDivElement>)=>{
    const previous=activePointers.current.get(event.pointerId);
    if(!previous)return;
    scratchSegment(previous,{x:event.clientX,y:event.clientY});
    scanCoverage(true);
    activePointers.current.delete(event.pointerId);
    if(event.currentTarget.hasPointerCapture(event.pointerId))
      event.currentTarget.releasePointerCapture(event.pointerId);
  };
  const pointerCancel=(event:ReactPointerEvent<HTMLDivElement>)=>{
    if(!activePointers.current.has(event.pointerId))return;
    scanCoverage(true);
    activePointers.current.delete(event.pointerId);
    if(event.currentTarget.hasPointerCapture(event.pointerId))
      event.currentTarget.releasePointerCapture(event.pointerId);
  };
  const lostCapture=(event:ReactPointerEvent<HTMLDivElement>)=>{
    if(!activePointers.current.has(event.pointerId))return;
    scanCoverage(true);
    activePointers.current.delete(event.pointerId);
  };
  const [titleTop,...titleRest]=game.name.toUpperCase().split(' ');
  const theme=tiers[game.id]??{series:'00',stamp:'✦',rarity:'COLLECTOR SERIES'};
  return <div className={'scratch-ticket scratch-ticket-'+game.id+' '+
    (ticket?.collected?'ticket-collected':'')+
    (winning.length?' is-winning':'')} data-testid="scratch-ticket">
    {game.id!=='lucky-sevens'&&<div className="scratch-tier-sheen" aria-hidden="true"/>}
    {(game.id==='platinum-vault'||game.id==='diamond-royale')&&
      <div className="scratch-ticket-crest" aria-hidden="true">
        <ScratchcardMotif gameId={game.id} symbol={0}/>
      </div>}
    <div className="scratch-ticket-header">
      <span className="scratch-ticket-edition">THE MIDNIGHT CLUB · SERIES {theme.series}</span>
      <span className="scratch-ticket-stamp">{theme.stamp}</span>
    </div>
    <div className="scratch-ticket-title">
      <span>{titleTop}</span><strong>{titleRest.join(' ')}</strong>
    </div>
    <p>{game.id==='lucky-sevens'?'THREE MATCHING SYMBOLS · ONE PRIZE':
      theme.rarity+' · MATCH THREE SYMBOLS'}</p>
    <div className="scratch-ticket-grid" ref={grid} data-testid="scratch-ticket-grid"
      onPointerDown={pointerDown} onPointerMove={pointerMove} onPointerUp={pointerUp}
      onPointerCancel={pointerCancel} onLostPointerCapture={lostCapture}>
      {allFields.map(index=><ScratchCell key={(ticket?.ticketId||'empty')+'-'+index}
        gameId={game.id} index={index} symbol={ticket?.symbols[index]??index%5}
        revealed={!!ticket&&(ticket.collected||revealed.includes(index))}
        disabled={!ticket||!ready||ticket.collected} winning={winning.includes(index)}
        onReveal={onReveal} onStart={onStart}/>)}
    </div>
    <div className="scratch-silver-layer" ref={particles} aria-hidden="true"/>
    <div className="scratch-ticket-footer"><span>NO. {ticket?ticket.ticketId.slice(0,8).toUpperCase():'— — — —'}</span>
      <b>◆</b><span>{game.id==='diamond-royale'?'DIAMOND EXCLUSIVE':
        game.id==='platinum-vault'?'PLATINUM COLLECTION':'PLAY MONEY ONLY'}</span></div>
  </div>;
}
type RevealSession={ticketId:string|null;indexes:number[]};

export function ModernScratchcards({data,loaded,embedded,language,pending,notice,
  motionEnabled=true,onBack,onBuy,onCollect}:Props){
  const de=language==='de';
  const ticket=data.ticket;
  const games=data.games;
  const [selectedId,setSelectedId]=useState<string>(ticket?.gameId??games[0]?.id??'lucky-sevens');
  const [direction,setDirection]=useState<'next'|'previous'|null>(null);
  const [revealSession,setRevealSession]=useState<RevealSession>({ticketId:null,indexes:[]});
  const revealRef=useRef<RevealSession>({ticketId:null,indexes:[]});
  const [soundEnabled,setSoundEnabled]=useState(true);
  const soundPlayedFor=useRef<string|null>(null);

  // Fresh snapshots must never render the previous ticket's nine revealed fields,
  // even for the single render preceding this effect. That caused the odds flash.
  useEffect(()=>{
    const next={ticketId:ticket?.ticketId??null,
      indexes:ticket?.collected?[...allFields]:[]};
    revealRef.current=next;
    setRevealSession(next);
    soundPlayedFor.current=null;
    if(ticket&&!ticket.collected&&games.some(game=>game.id===ticket.gameId))
      setSelectedId(ticket.gameId);
  },[ticket?.ticketId,ticket?.collected]);

  const selectedIndex=Math.max(0,games.findIndex(game=>game.id===selectedId));
  const selected=games[selectedIndex]??games[0];
  const activeTicket=ticket?.gameId===selected.id?ticket:null;
  const activeElsewhere=!!ticket&&!ticket.collected&&ticket.gameId!==selected.id;
  const revealed=revealedIndexesForTicket(activeTicket?.ticketId??null,
    activeTicket?.collected===true,revealSession);
  const allRevealed=!!activeTicket&&revealed.length===9;
  const winning=allRevealed?winningCellIndexes(activeTicket!.symbols,activeTicket!.prize):[];
  const hasWin=winning.length===3;
  const prizes=basePrizes.map(entry=>({
    amount:entry.amount*selected.prizeMultiplier,odds:entry.odds,
  }));

  const navigate=(step:number)=>{
    const next=selectedIndex+step;
    if(next<0||next>=games.length)return;
    setDirection(step>0?'next':'previous');
    setSelectedId(games[next].id);
  };
  const reveal=(index:number)=>{
    if(!activeTicket||activeTicket.collected)return;
    const current=revealRef.current.ticketId===activeTicket.ticketId?
      revealRef.current.indexes:[];
    if(current.includes(index))return;
    const next=[...current,index];
    const session={ticketId:activeTicket.ticketId,indexes:next};
    revealRef.current=session;
    setRevealSession(session);
    if(next.length===9&&activeTicket.prize>0&&soundPlayedFor.current!==activeTicket.ticketId){
      soundPlayedFor.current=activeTicket.ticketId;
      playScratchcardWin(soundEnabled);
    }
  };
  const revealAll=()=>{
    if(!activeTicket||activeTicket.collected)return;
    unlockScratchcardAudio(soundEnabled);
    const current=revealRef.current.ticketId===activeTicket.ticketId?
      revealRef.current.indexes:[];
    if(current.length<9&&activeTicket.prize>0&&soundPlayedFor.current!==activeTicket.ticketId){
      soundPlayedFor.current=activeTicket.ticketId;
      playScratchcardWin(soundEnabled);
    }
    const session={ticketId:activeTicket.ticketId,indexes:[...allFields]};
    revealRef.current=session;
    setRevealSession(session);
  };
  const canPurchase=embedded&&loaded&&!pending&&!data.recoveryRequired&&
    (!ticket||ticket.collected)&&data.bankroll>=selected.price;
  const canCollect=!!activeTicket&&!activeTicket.collected&&allRevealed&&!pending&&!data.recoveryRequired;
  const status=activeElsewhere?(de
    ?'Schließe zuerst dein anderes aktives Los ab.':'Finish your other active ticket first.'):
    activeTicket?.collected?(activeTicket.prize>0
      ?de?'Gewinn gutgeschrieben':'Prize paid out'
      :de?'Los abgeschlossen':'Ticket completed')
    :activeTicket&&allRevealed?(activeTicket.prize>0
      ?de?'Gewonnen! Jetzt einlösen.':'Winner! Collect your prize.'
      :de?'Leider kein Gewinn. Los abschließen.':'No prize this time. Finish the ticket.')
    :activeTicket?(de?'Rubbele alle neun Felder frei.':'Scratch all nine fields to reveal your result.')
    :(de?'Wähle dieses Los und versuche dein Glück.':'Choose this ticket and try your luck.');
  return <main className="scratch-page" data-game={selected.id}
    data-motion={motionEnabled?'on':'off'} aria-label={de?'Rubbellose':'Scratchcards'}>
    <div className="scratch-scroll">
      <header className="scratch-hero">
        <div>
          <button type="button" className="shop-back" onClick={onBack}>
            {de?'‹ Zur Lobby':'‹ Back to lobby'}</button>
          <span className="shop-eyebrow">THE MIDNIGHT CLUB / INSTANT WIN</span>
          <h1>{de?'Rubbellose':'Scratchcards'}</h1>
          <p>{de?'Vier exklusive Losserien, neun Rubbel-Felder und große Spielgeldgewinne.':
            'Four exclusive ticket series, nine scratch fields and high-roller play-money prizes.'}</p>
        </div>
        <div className="scratch-hero-wallet">
          <span>{de?'DEIN SPIELGELD':'YOUR BANKROLL'}</span>
          <strong>{money(data.bankroll)}</strong>
          <small>{de?'PREIS PRO LOS':'TICKET PRICE'} · {money(selected.price)}</small>
        </div>
      </header>
      <div className="scratch-layout">
        <section className={'scratch-stage '+(hasWin?'scratch-stage-win':'')}
          aria-label={de?'Rubbellos':'Scratch ticket'}>
          <div className="scratch-stage-header">
            <span>{String(selectedIndex+1).padStart(2,'0')} / {selected.name.toUpperCase()}</span>
            <strong>{activeTicket&&!activeTicket.collected?
              (de?'AKTIVES LOS':'ACTIVE TICKET'):
              (de?'LOS '+(selectedIndex+1)+' / '+games.length:
                'TICKET '+(selectedIndex+1)+' / '+games.length)}</strong>
          </div>
          <div className="scratch-ticket-switcher">
            <div className="scratch-switch-side scratch-switch-left">
              {selectedIndex>0&&<button type="button" className="scratch-switch-arrow"
                onClick={()=>navigate(-1)} disabled={pending!==null}
                aria-label={(de?'Vorheriges Los: ':'Previous ticket: ')+games[selectedIndex-1].name}>
                <span aria-hidden="true">‹</span>
              </button>}
            </div>
            <div key={selected.id} className={'scratch-ticket-entrance '+
              (motionEnabled?(direction==='next'?'slide-next':
                direction==='previous'?'slide-previous':''):'')}>
              <TicketBoard game={selected} ticket={activeTicket} revealed={revealed}
                ready={loaded} winning={winning} motionEnabled={motionEnabled}
                onReveal={reveal} onStart={()=>unlockScratchcardAudio(soundEnabled)}/>
            </div>
            <div className="scratch-switch-side scratch-switch-right">
              {selectedIndex<games.length-1&&<button type="button" className="scratch-switch-arrow"
                onClick={()=>navigate(1)} disabled={pending!==null}
                aria-label={(de?'Nächstes Los: ':'Next ticket: ')+games[selectedIndex+1].name}>
                <span aria-hidden="true">›</span>
              </button>}
            </div>
          </div>
          {hasWin&&<div className="scratch-win-banner" role="status" aria-live="polite">
            <span aria-hidden="true">✦</span>
            <strong>{activeTicket?.prize===selected.price*50?'JACKPOT':de?'GEWINN!':'WINNER!'}</strong>
            <b>{money(activeTicket?.prize??0)}</b>
            <span aria-hidden="true">✦</span>
          </div>}
          <div className={'scratch-status '+(hasWin?'scratch-status-winning':'')} role="status">
            <span className="scratch-status-symbol">{hasWin?'✦':activeTicket&&allRevealed?'◆':'✧'}</span>
            <div><strong>{activeTicket&&allRevealed?money(activeTicket.prize):
              de?'Verdeckter Gewinn':'Hidden prize'}</strong>
              <p>{status}</p></div>
          </div>
          {activeElsewhere&&<button type="button" className="scratch-return-active"
            onClick={()=>navigate(games.findIndex(game=>game.id===ticket!.gameId)-selectedIndex)}>
            {de?'‹ Zurück zum aktiven Los: ':'‹ Back to active ticket: '}
            {games.find(game=>game.id===ticket!.gameId)?.name??ticket!.gameId}
          </button>}
          {notice&&!notice.success&&<p className="scratch-error" role="alert">{notice.message}</p>}
          {data.recoveryRequired&&<p className="scratch-error" role="alert">
            {de?'Speichervorgang unklar. Bitte das Casino neu starten, bevor du weiterspielst.':
              'Save recovery required. Restart the casino before playing again.'}</p>}
          {!embedded&&<p className="scratch-hint">
            {de?'Im Browser nur Vorschau. Kaufen und Gewinne funktionieren im Windows-Programm.':
              'Browser preview only. Purchases and payouts require the Windows app.'}</p>}
          <div className="scratch-actions">
            <button type="button" className="scratch-primary" disabled={!canPurchase}
              onClick={()=>onBuy(selected.id)}>
              {pending==='buy'?'…':de?'LOS KAUFEN · '+money(selected.price):
                'BUY TICKET · '+money(selected.price)}
            </button>
            <button type="button" className="scratch-secondary"
              disabled={!activeTicket||allRevealed||!loaded||activeTicket.collected}
              onClick={revealAll}>
              {de?'Alles aufdecken':'Reveal all'}
            </button>
            <button type="button" className="scratch-sound" aria-pressed={soundEnabled}
              onClick={()=>setSoundEnabled(value=>!value)}>
              {soundEnabled?(de?'♪ Ton an':'♪ Sound on'):(de?'♪ Ton aus':'♪ Sound off')}
            </button>
            {activeTicket&&!activeTicket.collected&&<button type="button" className="scratch-collect"
              disabled={!canCollect} onClick={()=>onCollect(activeTicket.ticketId)}>
              {pending==='collect'?'…':activeTicket.prize>0
                ?(de?'GEWINN EINLÖSEN':'COLLECT PRIZE')
                :(de?'LOS ABSCHLIESSEN':'FINISH TICKET')}</button>}
          </div>
        </section>
        <aside className="scratch-info">
          <section className="scratch-info-card">
            <span className="shop-eyebrow">HOW TO PLAY</span>
            <h2>{de?'So funktioniert es':'How it works'}</h2>
            <p>{de?'Kaufe ein '+selected.name+'-Los für '+money(selected.price)+
              ' Spielgeld und rubbele die neun Felder mit der Maus oder per Touch frei.':
              'Buy a '+selected.name+' ticket for '+money(selected.price)+
              ' of play money and scratch all nine fields with mouse or touch.'}</p>
            <p>{de?'Drei gleiche Symbole an beliebigen Stellen gewinnen den auf dem Los festgelegten Betrag. Bei einem Gewinn kannst du ihn anschließend einlösen.':
              'Three matching symbols anywhere win the predetermined ticket prize. Collect your payout after revealing every field.'}</p>
            <div className="scratch-symbol-preview" aria-label={de?'Symbole dieser Serie':'Symbols in this series'}>
              {[0,1,2,3,4].map(symbol=><span className="scratch-symbol-preview-item"
                key={symbol} title={scratchSymbolLabel(selected.id,symbol)}>
                <ScratchcardMotif gameId={selected.id} symbol={symbol}/>
              </span>)}
            </div>
          </section>
          <section className="scratch-info-card">
            <span className="shop-eyebrow">PAYOUT TABLE · {selected.name.toUpperCase()}</span>
            <h2>{de?'Gewinne & Chancen':'Prizes & odds'}</h2>
            <div className="scratch-odds">
              <div className="scratch-odds-heading"><span>{de?'Gewinn':'Prize'}</span>
                <span>{de?'Chance':'Chance'}</span></div>
              {prizes.map(entry=><div key={entry.amount} className={
                (entry.amount===selected.price*50?'scratch-jackpot-row ':'')+
                (hasWin&&activeTicket?.prize===entry.amount?'scratch-odds-winner':'')}>
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
