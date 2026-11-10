import {useCallback,useEffect,useRef,useState} from 'react';
import type {FocusEvent,MouseEvent} from 'react';
import {CommandAtmosphere} from './EmpireCommandPixi';
import {CommandWindow} from './EmpireCommandPanels';
import type {CommandSection,HoverSource} from './EmpireCommandPanels';
import {EmpireArtwork} from './EmpireArtwork';
import {commandCanBuy,commandImpact,commandMilestone,commandProgress,commandPurchaseSizes,commandQuote} from './commandCenterModel';
import {display} from './empireBridge';
import type {EmpireSnapshot,Intent,Preview,Room} from './empireBridge';
import './empireCommandCenter.css';

type Props={
  snapshot:EmpireSnapshot;disabled:boolean;pending:boolean;notice:string|null;preview:Preview|null;motion:boolean;
  t:(value:string)=>string;onAction:(intent:Intent)=>void;onPreview:(intent:Intent)=>void;
  onClearPreview:()=>void;onClassic:()=>void;onClose:()=>void;onDismissOffline:()=>void;
};
type Hover={title:string;detail?:string;cost?:string;x:number;y:number;intent:boolean;action:Intent['action']|null};

export function EmpireCommandCenter({snapshot,disabled,pending,notice,preview,motion,t,onAction,onPreview,onClearPreview,onClassic,onClose,onDismissOffline}:Props){
  const economy=snapshot.state.economy,language=snapshot.state.language,de=language==='de';
  const l=(german:string,english:string)=>de?german:english;
  const fmt=(s:string)=>display(s,language);
  const [section,setSection]=useState<CommandSection|null>(null);
  const [roomId,setRoomId]=useState<string|null>(null);
  const [quantity,setQuantity]=useState<number>(10);
  const [hover,setHover]=useState<Hover|null>(null);
  const [highlightFloor,setHighlightFloor]=useState<number|null>(null);
  const [burst,setBurst]=useState(0);
  const [recent,setRecent]=useState<string|null>(null);
  const prevLevels=useRef<Record<string,number>|null>(null);
  const lastTrigger=useRef<HTMLElement|null>(null);
  const dismissRef=useRef<ReturnType<typeof setTimeout>|null>(null);
  const clearHover=useCallback(()=>{
    setHover(null);setHighlightFloor(null);onClearPreview();
  },[onClearPreview]);
  const openSection=(next:CommandSection,trigger?:HTMLElement,room?:string)=>{
    clearHover();if(trigger)lastTrigger.current=trigger;setRoomId(room??null);setSection(next);
  };
  const closeSection=useCallback(()=>{setSection(null);clearHover();},[clearHover]);
  useEffect(()=>{
    if(!section)return;
    const active=window.setTimeout(()=>document.querySelector<HTMLButtonElement>('[data-testid=cc-window-close]')?.focus(),0);
    return()=>{window.clearTimeout(active);lastTrigger.current?.focus();};
  },[section]);
  useEffect(()=>{
    const current=Object.fromEntries(economy.rooms.map(room=>[room.id,room.level]));
    if(prevLevels.current){
      const changed=economy.rooms.find(room=>room.level>(prevLevels.current?.[room.id]??room.level));
      if(changed){
        setRecent(changed.id);setBurst(v=>v+1);
        if(dismissRef.current)clearTimeout(dismissRef.current);
        dismissRef.current=setTimeout(()=>setRecent(null),1300);
      }
    }
    prevLevels.current=current;
  },[economy.activePropertyId,economy.rooms]);
  useEffect(()=>()=>{if(dismissRef.current)clearTimeout(dismissRef.current);},[]);
  useEffect(()=>{clearHover();setSection(null);setRoomId(null);prevLevels.current=null;},[economy.activePropertyId]);
  const showHover=(event:HoverSource,intent:Intent|null,title:string,detail?:string,cost?:string)=>{
    const box=event.currentTarget.getBoundingClientRect();
    const width=336;
    const x=Math.max(10,Math.min(window.innerWidth-width-10,box.right+10+width<window.innerWidth?box.right+10:box.left-width-10));
    const y=Math.max(72,Math.min(window.innerHeight-350,box.top+Math.min(box.height,30)));
    setHover({title,detail,cost,x,y,intent:!!intent&&!disabled,action:intent?.action??null});
    if(intent&&!disabled)onPreview(intent);
    else onClearPreview();
  };
  const handleRoomEnter=(event:HoverSource,room:Room)=>{
    setHighlightFloor(room.floor);
    const q=commandQuote(room,quantity);
    showHover(event,q?.unlocked&&q.levels>0?{action:'upgrade',id:room.id,levels:quantity}:null,t(room.name),
      q?.unlocked?t(room.description):t(room.requirement),q?.unlocked?fmt(q.cost)+' cr':undefined);
  };
  const property=economy.properties.find(p=>p.id===economy.activePropertyId);
  const availableResearch=economy.research.filter(item=>item.canBuy).length;
  const availableProjects=economy.projects.filter(item=>item.canBuy).length;
  const hasAvailableEvent=economy.events.some(item=>item.canBuy);
  const staffCount=Object.keys(economy.managerAssignments).length;
  const mainCount=economy.rooms.filter(r=>r.floor===1&&r.level>0).length;
  const goals=[commandProgress(economy.mainFloorLevels,economy.prestigeMainFloorLevelRequirement),
    commandProgress(Number(economy.statistics.runCredits),Number(economy.prestigeRunCreditRequirement)),
    commandProgress(mainCount,5)];
  const prestigeProgress=Math.min(...goals);
  const dock:{id:CommandSection;icon:string;de:string;en:string;status:string;ready?:boolean}[]=[
    {id:'research',icon:'✧',de:'Forschung',en:'Research',status:availableResearch+' '+l('verfügbar','available'),ready:availableResearch>0},
    {id:'projects',icon:'⬡',de:'Projekte',en:'Projects',status:availableProjects+' '+l('finanzierbar','affordable'),ready:availableProjects>0},
    {id:'staff',icon:'♟',de:'Personal',en:'Staff',status:staffCount+'/'+economy.managerSlots+' '+l('Manager','managers')},
    {id:'automation',icon:'↻',de:'Automation',en:'Automation',status:economy.automation.enabled?l('Aktiv','Running'):economy.automationUnlocked?l('Bereit','Ready'):l('Gesperrt','Locked'),ready:economy.automation.enabled},
    {id:'expansion',icon:'♜',de:'Expansion',en:'Expansion',status:economy.canPrestige?l('Prestige bereit','Prestige ready'):'P'+economy.prestige,ready:economy.canPrestige},
    {id:'opportunities',icon:'◇',de:'Aufträge',en:'Contracts',status:economy.canClaimContract?l('Belohnung bereit','Reward ready'):hasAvailableEvent?l('Events verfügbar','Events available'):l('Verwalten','Manage'),ready:economy.canClaimContract},
  ];
  const impact=hover&&preview?.success?commandImpact(preview):null;
  const upgradeImpact=!section&&hover?.action==='upgrade'?impact:null;
  const signed=(amount:string)=>(amount.startsWith('-')?'−':'+')+fmt(amount.replace(/^-/ ,''));
  const gradient=(amount:string)=>amount.startsWith('-')?'cc-negative':amount==='0'?'cc-neutral':'cc-positive';
  const isProducing=Number(economy.portfolioRate)>0;
  return <div className="empire-command" data-testid="empire-command-center" data-motion={motion} data-modal-open={!!section}>
    <header className="cc-header">
      <div className="cc-brand"><span>THE MIDNIGHT CLUB <b> / COMMAND CENTER</b></span><strong>CASINO <em>EMPIRE</em></strong></div>
      <div className="cc-header-property">
        <span>{l('STANDORT','PROPERTY')}</span>
        <select value={economy.activePropertyId} aria-label={l('Aktive Immobilie','Active property')} disabled={disabled}
          onChange={e=>onAction({action:'property.select',id:e.target.value})}>
          {economy.properties.filter(p=>p.owned).map(p=><option key={p.id} value={p.id}>{t(p.name)}</option>)}
        </select>
      </div>
      <div className="cc-header-actions">
        <span className="cc-prototype-flag">CONCEPT / 03</span>
        <button type="button" data-testid="cc-classic" onClick={onClassic}>{l('Klassisch','Classic')}</button>
        <button type="button" className="cc-exit" data-testid="cc-back" onClick={onClose}>← {l('Lobby','Lobby')}</button>
      </div>
    </header>
    <main className="cc-dashboard" aria-busy={pending}>
      <section className="cc-finance-strip" aria-label={l('Wirtschaftskennzahlen','Economy metrics')}>
        <div className="cc-finance-item cc-capital"><span>{l('BAUKAPITAL','CAPITAL')}</span><strong className="cc-kpi-value" title={economy.credits}><span>{fmt(economy.credits)} <small>cr</small></span>{upgradeImpact&&<span data-testid="cc-capital-delta" className={"cc-kpi-delta "+gradient(upgradeImpact.capital)}>{signed(upgradeImpact.capital)}</span>}</strong><div>{l('Verfügbares Budget','Available to invest')}</div></div>
        <div className={'cc-finance-item cc-production '+(isProducing?'earning':'')}><span>{l('EINKOMMEN / SEKUNDE','INCOME / SECOND')}</span>
          <strong className="cc-kpi-value" title={economy.portfolioRate}><span>+{fmt(economy.portfolioRate)} <small>cr/s</small></span>{upgradeImpact&&<span data-testid="cc-income-delta" className={"cc-kpi-delta "+gradient(upgradeImpact.portfolioIncome)} title={l("Zusätzliches Portfolioeinkommen pro Sekunde","Additional portfolio income per second")}>{signed(upgradeImpact.portfolioIncome)} <small>cr/s</small></span>}</strong><div><i className="cc-status-dot"/>{l('Portfolio produziert','Portfolio producing')}</div></div>
        <div className="cc-finance-item"><span>{l('AKTIVER STANDORT','CURRENT PROPERTY')}</span><strong title={economy.rate}>{fmt(economy.rate)} <small>cr/s</small></strong><div>{t(property?.name??'Casino Empire')}</div></div>
        <div className="cc-finance-item cc-prestige-progress">
          <span>PRESTIGE · P{economy.prestige}</span><strong>{economy.canPrestige?l('Bereit','Ready'):Math.round(prestigeProgress)+' %'}</strong>
          <div className="cc-meter"><div style={{width:prestigeProgress+'%'}}/></div>
        </div>
        <div className="cc-finance-item cc-dividend">
          <span>{l('INHABERDIVIDENDE','OWNER DIVIDEND')}</span><strong>{'$'}{fmt(economy.dividend)}</strong>
          <button type="button" data-testid="cc-claim-dividend" disabled={disabled||!snapshot.state.canClaimDividend}
            onClick={()=>onAction({action:'dividend'})}>{l('Abholen','Claim')}</button>
        </div>
      </section>
      <section className="cc-enterprise" aria-label={l('Betriebsübersicht','Business overview')}>
        <CommandAtmosphere enabled={motion&&!section} earning={isProducing} focusFloor={null} burst={burst}/>
        {economy.floors.map(floor=>{
          const rooms=economy.rooms.filter(r=>r.floor===floor.id);
          return <section className={'cc-floor '+(!floor.unlocked?'cc-floor-locked':'')} key={floor.id} data-testid={'cc-floor-'+floor.id}>
            <header className="cc-floor-header">
              <div><span className="cc-floor-number">0{floor.id}</span><strong>{t(floor.displayName)}</strong></div>
              <span className={'cc-floor-status '+(floor.unlocked?'is-active':'')}>{floor.unlocked?l('IN BETRIEB','OPERATING'):l('GESPERRT','LOCKED')}</span>
            </header>
            <div className="cc-room-stack">
              {rooms.map(room=>{
                const quote=commandQuote(room,quantity),milestone=commandMilestone(room.level),locked=!quote?.unlocked;
                const buying=commandCanBuy(room,quantity,disabled);
                const managers=economy.managers.filter(m=>m.assignment===room.id);
                return <article className={'cc-business '+(locked?'is-locked ':'')+(room.id===recent?'is-burst ':'')+(milestone.remaining===1?'is-near-milestone':'')}
                  key={room.id} data-testid={'cc-business-'+room.id}
                  onMouseLeave={clearHover}>
                  <button type="button" className="cc-business-detail"
                    aria-label={t(room.name)}
                    onClick={event=>openSection('room',event.currentTarget,room.id)}>
                    <EmpireArtwork roomId={room.id} className="cc-room-art"/>
                    <span className="cc-room-text"><strong>{t(room.name)}</strong>
                      <span>LV {room.level} <b>·</b> <em>{fmt(room.rate)} cr/s</em></span>
                      <span className="cc-room-milestone"><span className="cc-room-track"><span style={{width:milestone.progress+'%'}}/></span>
                        {milestone.next===null?'MAX':milestone.remaining+' → '+milestone.next}</span>
                      <span className={"cc-room-manager "+(managers.length?"is-managed":"")} data-testid={"cc-manager-status-"+room.id} title={managers.map(m=>m.name+" · "+t(m.description)).join("\n")}><i aria-hidden="true">♟</i>{managers.length?managers.map(m=>m.name).join(", "):l("Kein Manager","No manager")}</span>
                    </span>
                  </button>
                  <button type="button" className={'cc-room-buy '+(buying?'is-affordable':'')} data-testid={'cc-upgrade-'+room.id}
                    aria-disabled={!buying} onMouseEnter={e=>handleRoomEnter(e,room)} onMouseLeave={clearHover} onFocus={e=>{if(e.currentTarget.matches(":focus-visible"))handleRoomEnter(e,room);}} onBlur={clearHover}
                    onClick={()=>{if(buying){clearHover();onAction({action:'upgrade',id:room.id,levels:quantity});}}}>
                    <span>{room.level>=200?'MAX':locked?'◆':(quote?.levels??quantity)?'+'+(quote?.levels??quantity):'—'}</span>
                    <small>{quote?.unlocked?fmt(quote.cost):'—'}</small>
                  </button>
                  {room.id===recent&&<span className="cc-upgrade-flash" aria-hidden="true">✦</span>}
                </article>;
              })}
            </div>
            {!floor.unlocked&&<p className="cc-floor-requirement">◆ {t(floor.requirement)}</p>}
          </section>;
        })}
      </section>
      <section className="cc-command-bar" aria-label={l('Management','Management')}>
        <div className="cc-buy-control">
          <span>{l('KAUFMENGE','BUY LEVELS')}</span>
          <div role="group" aria-label={l('Kaufmenge','Purchase quantity')}>
            {commandPurchaseSizes.map(amount=><button type="button" key={amount} aria-pressed={quantity===amount}
              data-testid={'cc-buy-qty-'+amount} onClick={()=>{setQuantity(amount);clearHover();}}>×{amount}</button>)}
          </div>
        </div>
        <div className="cc-command-dock">
          {dock.map(item=><button type="button" className={'cc-module '+(item.ready?'is-ready':'')} key={item.id}
            data-testid={'cc-open-'+item.id} onClick={e=>openSection(item.id,e.currentTarget)}>
            <span className="cc-module-icon">{item.icon}</span><span><strong>{de?item.de:item.en}</strong><small>{item.status}</small></span>
            {item.ready&&<span className="cc-module-ready-dot"/>}
          </button>)}
        </div>
        <button type="button" className="cc-insights" data-testid="cc-open-statistics" onClick={e=>openSection('statistics',e.currentTarget)}
          title={l('Statistik öffnen','Open statistics')}>▥</button>
      </section>
    </main>
    {notice&&<div className="cc-notice" role="status">{t(notice)}</div>}
    {pending&&<div className="cc-saving" role="status">{l('Transaktion wird gespeichert…','Saving transaction…')}</div>}
    {snapshot.offline&&<div className="cc-offline" role="status"><span><strong>{l('WILLKOMMEN ZURÜCK','WELCOME BACK')}</strong> +{fmt(snapshot.offline.credits)} cr · {'$'}{fmt(snapshot.offline.dividend)}</span>
      <button onClick={onDismissOffline}>{l('Schließen','Dismiss')} ×</button></div>}
    {section&&<CommandWindow section={section} economy={economy} language={language} roomId={roomId}
      disabled={disabled} motion={motion} t={t} onClose={closeSection} onAction={onAction} onHover={showHover} onLeave={clearHover}/>}
    {hover&&section!=='research'&&<div className="cc-hover" role="status" data-testid="cc-hover" style={{left:hover.x,top:hover.y}}>
      <div className="cc-hover-heading"><span>◈ {hover.title}</span><small>{l('AKTIONSVORSCHAU','ACTION PREVIEW')}</small></div>
      {hover.detail&&<p>{hover.detail}</p>}
      {hover.cost&&!impact&&<div className="cc-impact-line"><span>{l('Investition','Investment')}</span><strong className="cc-negative">−{hover.cost}</strong></div>}
      {hover.intent&&(!preview||!preview.success)&&<span className="cc-preview-pending">
        {preview&&!preview.success?t(preview.message):l('Berechne tatsächliche Auswirkungen…','Calculating authoritative impact…')}</span>}
      {impact&&preview&&<>
        <div className="cc-impact-line"><span>{l('Kapitalveränderung','Capital change')}</span><strong className={gradient(impact.capital)}>{signed(impact.capital)} cr</strong></div>
        <div className="cc-impact-line"><span>{l('Betriebseinkommen','Business income')}</span><strong className={gradient(impact.propertyIncome)}>{signed(impact.propertyIncome)} cr/s</strong></div>
        <div className="cc-impact-line"><span>{l('Portfolioeinkommen','Portfolio income')}</span><strong className={gradient(impact.portfolioIncome)}>{signed(impact.portfolioIncome)} cr/s</strong></div>
        {preview.levelAfter!==preview.levelBefore&&<div className="cc-impact-line"><span>{l('Stufe','Level')}</span><strong>{preview.levelBefore} → {preview.levelAfter}</strong></div>}
        {impact.incomePercent!==null&&<div className="cc-impact-line"><span>{l('Portfolio-Veränderung','Portfolio change')}</span><strong className={gradient(impact.portfolioIncome)}>{impact.incomePercent>=0?'+':''}{impact.incomePercent.toFixed(2)}%</strong></div>}
        {impact.paybackSeconds!==null&&['upgrade','project','property.buy'].includes(hover.action??'')&&<div className="cc-impact-line"><span>{l('Amortisation (ca.)','Payback (approx.)')}</span><strong>{impact.paybackSeconds<60?impact.paybackSeconds.toFixed(1)+' s':impact.paybackSeconds<3600?(impact.paybackSeconds/60).toFixed(1)+' min':(impact.paybackSeconds/3600).toFixed(1)+' h'}</strong></div>}
      </>}
      {!hover.intent&&<span className="cc-preview-pending">{l('Weitere Informationen im jeweiligen Modul.','Additional information is available in this module.')}</span>}
    </div>}
  </div>;
}
