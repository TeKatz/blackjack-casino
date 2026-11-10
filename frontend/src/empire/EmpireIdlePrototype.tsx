import {useEffect,useState} from 'react';
import type {FormEvent} from 'react';
import {EmpireArtwork} from './EmpireArtwork';
import {decimalInput,display,fractionToPercent,percentToFraction} from './empireBridge';
import type {Automation,EmpireSnapshot,Intent,Manager,Preview,Room} from './empireBridge';
import {canPurchase,milestoneDistance,milestoneProgress,nextMilestone,prestigeProgress,prototypePurchaseSizes,upgradeQuote} from './idlePrototypeModel';
import './idlePrototype.css';

type Tab = 'business'|'strategy'|'expansion'|'statistics';
type StrategyTab = 'research'|'projects'|'staff'|'automation'|'opportunities';

type Props = {
  snapshot:EmpireSnapshot;
  disabled:boolean;
  pending:boolean;
  notice:string|null;
  preview:Preview|null;
  t:(value:string)=>string;
  onAction:(intent:Intent)=>void;
  onPreview:(intent:Intent)=>void;
  onClearPreview:()=>void;
  onClassic:()=>void;
  onClose:()=>void;
  onDismissOffline:()=>void;
};

function ManagerAssignment({manager,rooms,disabled,de,t,onAction}:{manager:Manager;rooms:Room[];disabled:boolean;de:boolean;t:(value:string)=>string;onAction:(intent:Intent)=>void}){
  const [assignment,setAssignment]=useState(manager.assignment??'');
  return <article className="idle-panel idle-manager">
    <div className="idle-tag">{t(manager.role)}</div>
    <h3>{manager.name}</h3>
    <p>{t(manager.description)}</p>
    <label>{de?'Einsatzbereich':'Assignment'}
      <select aria-label={manager.name} value={assignment} onChange={event=>setAssignment(event.target.value)} disabled={disabled||(!manager.available&&!manager.assignment)}>
        <option value="">{de?'Nicht zugewiesen':'Unassigned'}</option>
        {rooms.filter(room=>room.level>0||room.id===assignment).map(room=><option key={room.id} value={room.id}>{t(room.name)}</option>)}
      </select>
    </label>
    <button className="idle-action" disabled={disabled||(!manager.available&&!manager.assignment)||assignment===(manager.assignment??'')}
      onClick={()=>onAction({action:'manager',id:manager.id,roomId:assignment||undefined})}>{de?'Zuweisung speichern':'Save assignment'}</button>
    {!manager.available&&!manager.assignment&&<small>{de?'Benötigt Prestige':'Requires prestige'} {manager.requiredPrestige}</small>}
  </article>;
}

function AutomationEditor({initial,rooms,unlocked,disabled,de,t,onAction}:{initial:Automation;rooms:Room[];unlocked:boolean;disabled:boolean;de:boolean;t:(value:string)=>string;onAction:(intent:Intent)=>void}){
  const [form,setForm]=useState<Automation>(initial);
  const [percent,setPercent]=useState(fractionToPercent(initial.spendPercent));
  const [dirty,setDirty]=useState(false);
  const [error,setError]=useState('');
  const serialized=JSON.stringify(initial);
  useEffect(()=>{
    if(!dirty){setForm(initial);setPercent(fractionToPercent(initial.spendPercent));}
  },[serialized,dirty]);
  const update=(next:Partial<Automation>)=>{setForm(old=>({...old,...next}));setDirty(true);setError('');};
  const save=(event:FormEvent)=>{
    event.preventDefault();
    const reserve=decimalInput(form.reserve);
    const spend=percentToFraction(percent);
    if(reserve===null||spend===null||form.targetLevel<1||form.targetLevel>200||!Number.isInteger(form.targetLevel)){
      setError(de?'Ungültige Reserve, Budgetprozent oder Zielstufe.':'Invalid reserve, budget percentage or target level.');return;
    }
    onAction({action:'automation',automation:{...form,reserve,spendPercent:spend}});
    setDirty(false);
  };
  return <form className="idle-panel idle-automation" onSubmit={save}>
    <div className="idle-card-heading"><div><div className="idle-tag">{de?'AUTOMATISIERUNG':'AUTOMATION'}</div><h3>{de?'Dein Investitionsplan':'Your investment policy'}</h3></div>
      <span className={'idle-state '+(form.enabled?'idle-state-on':'')}>{form.enabled?(de?'Aktiv':'Active'):(de?'Pausiert':'Paused')}</span></div>
    <p>{de?'Der Betrieb produziert immer automatisch. Diese Funktion investiert zusätzlich alle 30 Produktionssekunden nach deinen Regeln.':'Businesses always produce automatically. This feature also invests every 30 production seconds using your rules.'}</p>
    {!unlocked&&<div className="idle-locked">{de?'Freischaltung: Executive Offices und Executive Automation.':'Unlock: Executive Offices and Executive Automation.'}</div>}
    <label className="idle-inline-check"><input type="checkbox" checked={form.enabled} onChange={e=>update({enabled:e.target.checked})}/>{de?'Automatisch ausbauen':'Automatic upgrades'}</label>
    <div className="idle-fields">
      <label>{de?'Sicherheitsreserve (cr)':'Credit reserve'}<input inputMode="decimal" value={form.reserve} onChange={e=>update({reserve:e.target.value})}/></label>
      <label>{de?'Budget je Prüfung (%)':'Budget per decision (%)'}<input inputMode="decimal" value={percent} onChange={e=>{setPercent(e.target.value);setDirty(true);setError('');}}/></label>
      <label>{de?'Zielstufe':'Target level'}<input type="number" min={1} max={200} value={form.targetLevel} onChange={e=>update({targetLevel:Number(e.target.value)})}/></label>
    </div>
    <label className="idle-inline-check"><input type="checkbox" checked={form.cheapestMilestone} onChange={e=>update({cheapestMilestone:e.target.checked})}/>{de?'Günstigsten Meilenstein zuerst':'Cheapest milestone first'}</label>
    {!form.cheapestMilestone&&<div className="idle-priorities">
      <strong>{de?'Investitionsreihenfolge':'Investment priority'}</strong>
      {form.priorities.map((id,index)=><div className="idle-priority" key={id}>
        <select aria-label={(de?'Priorität ':'Priority ')+(index+1)} value={id} onChange={e=>update({priorities:form.priorities.map((old,i)=>i===index?e.target.value:old)})}>
          {rooms.map(room=><option key={room.id} value={room.id} disabled={room.id!==id&&form.priorities.includes(room.id)}>{t(room.name)}</option>)}
        </select>
        <button type="button" disabled={index===0} onClick={()=>{const p=[...form.priorities];[p[index-1],p[index]]=[p[index],p[index-1]];update({priorities:p});}} aria-label={de?'Nach oben':'Move up'}>↑</button>
        <button type="button" disabled={index===form.priorities.length-1} onClick={()=>{const p=[...form.priorities];[p[index],p[index+1]]=[p[index+1],p[index]];update({priorities:p});}} aria-label={de?'Nach unten':'Move down'}>↓</button>
        <button type="button" onClick={()=>update({priorities:form.priorities.filter(value=>value!==id)})} aria-label={de?'Entfernen':'Remove'}>×</button>
      </div>)}
      <button type="button" disabled={form.priorities.length===rooms.length} onClick={()=>{const next=rooms.find(room=>!form.priorities.includes(room.id));if(next)update({priorities:[...form.priorities,next.id]});}}>{de?'+ Betrieb hinzufügen':'+ Add business'}</button>
    </div>}
    {error&&<p className="idle-error" role="alert">{error}</p>}
    <div className="idle-form-actions">
      <button type="button" disabled={disabled} onClick={()=>{setForm(initial);setPercent(fractionToPercent(initial.spendPercent));setDirty(false);setError('');}}>{de?'Zurücksetzen':'Reset edits'}</button>
      <button className="idle-action" type="submit" disabled={disabled||!unlocked}>{de?'Investitionsplan speichern':'Save investment policy'}</button>
    </div>
  </form>;
}

function ProgressBar({value}:{value:number}){
  return <div className="idle-progress-track" role="progressbar" aria-valuenow={Math.round(value)} aria-valuemin={0} aria-valuemax={100}>
    <div style={{width:Math.max(0,Math.min(100,value))+'%'}}/>
  </div>;
}

export function EmpireIdlePrototype({snapshot,disabled,pending,notice,preview,t,onAction,onPreview,onClearPreview,onClassic,onClose,onDismissOffline}:Props){
  const [tab,setTab]=useState<Tab>('business');
  const [strategyTab,setStrategyTab]=useState<StrategyTab>('research');
  const [floor,setFloor]=useState(0);
  const [quantity,setQuantity]=useState<number>(1);
  const [researchBranch,setResearchBranch]=useState('Operations');
  const [prestigeConfirm,setPrestigeConfirm]=useState(false);
  const economy=snapshot.state.economy;
  const de=snapshot.state.language==='de';
  const label=(german:string,english:string)=>de?german:english;
  const fmt=(value:string)=>display(value,snapshot.state.language);
  const currency=(value:string)=>'$'+display(value,snapshot.state.language,false);
  const property=economy.properties.find(item=>item.id===economy.activePropertyId);
  const floorInfo=economy.floors.find(item=>item.id===floor);
  const rooms=economy.rooms.filter(room=>floor===0||room.floor===floor);
  const allOpen=economy.rooms.filter(room=>room.level>0).length;
  const runProgress=prestigeProgress(Number(economy.statistics.runCredits),Number(economy.prestigeRunCreditRequirement));
  const levelProgress=prestigeProgress(economy.mainFloorLevels,economy.prestigeMainFloorLevelRequirement);
  const mainRoomsOpen=economy.rooms.filter(room=>room.floor===1&&room.level>0).length;
  const tabs:{id:Tab;de:string;en:string;number?:string}[]=[
    {id:'business',de:'Betriebe',en:'Businesses',number:String(allOpen)},
    {id:'strategy',de:'Management',en:'Management'},
    {id:'expansion',de:'Expansion',en:'Expansion'},
    {id:'statistics',de:'Statistik',en:'Statistics'},
  ];
  const strategyTabs:{id:StrategyTab;de:string;en:string}[]=[
    {id:'research',de:'Forschung',en:'Research'},
    {id:'projects',de:'Projekte',en:'Projects'},
    {id:'staff',de:'Personal',en:'Staff'},
    {id:'automation',de:'Automatisierung',en:'Automation'},
    {id:'opportunities',de:'Aufträge & Events',en:'Contracts & events'},
  ];
  return <div className="empire-proto" data-testid="empire-idle-prototype">
    <header className="idle-topbar">
      <div className="idle-brand"><span>THE MIDNIGHT CLUB / EXPERIMENTAL</span><strong>CASINO <em>EMPIRE</em></strong></div>
      <div className="idle-top-actions">
        <span className="idle-prototype-tag">{label('IDLE-PROTOTYP','IDLE PROTOTYPE')}</span>
        <button type="button" data-testid="empire-idle-classic" onClick={onClassic}>{label('Klassische Ansicht','Classic view')}</button>
        <button type="button" className="idle-back" onClick={onClose}>← {label('Lobby','Lobby')}</button>
      </div>
    </header>
    <div className="idle-shell">
      <aside className="idle-nav" aria-label={label('Empire-Navigation','Empire navigation')}>
        <div className="idle-nav-title">{label('DEINE WIRTSCHAFT','YOUR ECONOMY')}</div>
        {tabs.map(item=><button key={item.id} type="button" data-testid={'idle-nav-'+item.id} aria-current={tab===item.id?'page':undefined} onClick={()=>{setTab(item.id);onClearPreview();}}>
          <span>{de?item.de:item.en}</span>{item.number&&<b>{item.number}</b>}</button>)}
        <div className="idle-nav-bottom">
          <span>{label('PRESTIGE-STUFE','PRESTIGE LEVEL')}</span>
          <strong>{economy.prestige}</strong>
          <small>{economy.reputation.toLocaleString(snapshot.state.language)} {label('Ruf','reputation')}</small>
          <p>{label('Alle Betriebe produzieren automatisch – auch während anderer Spiele.','Businesses earn automatically – even while playing other games.')}</p>
        </div>
      </aside>
      <main className="idle-main">
        <div className="idle-heading-row">
          <div><span className="idle-kicker">{property?.name??'Casino Empire'}</span>
            <h1>{tab==='business'?label('Deine Betriebe','Your businesses'):tab==='strategy'?label('Management','Management'):tab==='expansion'?label('Dein Imperium','Your empire'):label('Geschäftsbericht','Business report')}</h1>
          </div>
          <label className="idle-property-picker">{label('IMMOBILIE','PROPERTY')}
            <select aria-label={label('Immobilie wechseln','Change property')} value={economy.activePropertyId} disabled={disabled} onChange={event=>{onClearPreview();onAction({action:'property.select',id:event.target.value});}}>
              {economy.properties.filter(item=>item.owned).map(item=><option key={item.id} value={item.id}>{t(item.name)}</option>)}
            </select>
          </label>
        </div>
        <section className="idle-kpis" aria-label={label('Kennzahlen','Key metrics')}>
          <div><span>{label('BAUKAPITAL','CONSTRUCTION CAPITAL')}</span><strong title={economy.credits}>{fmt(economy.credits)} <small>cr</small></strong><small>{label('Verfügbar für Investitionen','Available to invest')}</small></div>
          <div><span>{label('EINKOMMEN / SEK.','INCOME / SEC.')}</span><strong className="idle-income" title={economy.rate}>+{fmt(economy.rate)} <small>cr</small></strong><small>{label('Aktuelle Immobilie','Current property')}</small></div>
          <div><span>{label('PORTFOLIO / SEK.','PORTFOLIO / SEC.')}</span><strong title={economy.portfolioRate}>{fmt(economy.portfolioRate)} <small>cr</small></strong><small>{label('Alle Immobilien','All properties')}</small></div>
          <div className="idle-kpi-dividend"><span>{label('INHABERDIVIDENDE','OWNER DIVIDEND')}</span><strong>{currency(economy.dividend)}</strong><button className="idle-action" data-testid="idle-dividend" disabled={disabled||!snapshot.state.canClaimDividend||Number(economy.dividend)<.01} onClick={()=>onAction({action:'dividend'})}>{label('Abholen','Collect')}</button></div>
        </section>
        {snapshot.offline&&<section className="idle-offline">
          <div><span className="idle-kicker">{label('WILLKOMMEN ZURÜCK','WELCOME BACK')}</span>
            <strong>{label('Dein Empire hat weitergearbeitet.','Your empire kept running.')}</strong>
            <p>+{fmt(snapshot.offline.credits)} cr · {currency(snapshot.offline.dividend)} · {Math.round(snapshot.offline.seconds/60)} min{snapshot.offline.capped?' · '+label('Speicherlimit erreicht','storage limit reached'):''}</p>
          </div><button onClick={onDismissOffline}>{label('Weiter','Continue')} →</button>
        </section>}
        {notice&&<div className="idle-message" role="status">{t(notice)}</div>}
        {pending&&<div className="idle-processing" role="status">{label('Aktion wird gespeichert…','Saving action…')}</div>}
        {tab==='business'&&<>
          <div className="idle-section-head"><div><span className="idle-kicker">{label('LAUFENDER BETRIEB','OPERATIONS')}</span><h2>{label('Investieren und wachsen','Invest and grow')}</h2><p>{label('Jede Abteilung erwirtschaftet automatisch Credits. Kaufe Stufen, um neue Produktionsmeilensteine zu erreichen.','Every department produces credits automatically. Buy levels to reach new production milestones.')}</p></div></div>
          <div className="idle-controls">
            <div className="idle-floor-filter" role="group" aria-label={label('Etage filtern','Filter floor')}>
              <button aria-pressed={floor===0} onClick={()=>setFloor(0)}>{label('Alle Etagen','All floors')}</button>
              {economy.floors.map(item=><button key={item.id} aria-pressed={floor===item.id} onClick={()=>setFloor(item.id)}>{t(item.displayName)} {!item.unlocked?'◆':''}</button>)}
            </div>
            <div className="idle-quantity" role="group" aria-label={label('Kaufmenge','Purchase quantity')}>
              <span>{label('KAUFEN','BUY')}</span>
              {prototypePurchaseSizes.map(count=><button key={count} type="button" aria-pressed={quantity===count} onClick={()=>setQuantity(count)}>×{count}</button>)}
            </div>
          </div>
          {floorInfo&&!floorInfo.unlocked&&<div className="idle-locked">◆ {t(floorInfo.requirement)}</div>}
          <div className="idle-business-list">{rooms.map(room=>{
            const quote=upgradeQuote(room,quantity);
            const milestone=nextMilestone(room.level);
            const locked=!quote?.unlocked;
            return <article className={'idle-business '+(locked?'idle-business-locked':'')} key={room.id} data-testid={'idle-room-'+room.id}>
              <div className="idle-business-art"><EmpireArtwork className="idle-business-image" roomId={room.id}/></div>
              <div className="idle-business-data">
                <div className="idle-business-title">
                  <div><span className="idle-business-category">{t(room.category)} · {label('ETAGE','FLOOR')} {room.floor}</span><h3>{t(room.name)}</h3></div>
                  <span className={'idle-state '+(!locked&&room.level>0?'idle-state-on':'')}>{locked?label('Gesperrt','Locked'):room.level>0?label('Produziert','Producing'):label('Bereit','Available')}</span>
                </div>
                <div className="idle-business-numbers"><strong>{label('Stufe','Level')} {room.level} <small>/ 200</small></strong><strong className="idle-income">+{fmt(room.rate)} <small>cr/s</small></strong></div>
                <div className="idle-milestone"><div><span>{milestone===null?label('Maximale Stufe erreicht','Maximum level reached'):label('Nächster Meilenstein','Next milestone')+' · '+milestone}</span><span>{milestone===null?'MAX':label('Noch ','')+milestoneDistance(room.level)+label(' Stufen',' levels')}</span></div>
                  <ProgressBar value={milestoneProgress(room.level)}/></div>
                {locked&&<div className="idle-requirement">◆ {t(room.requirement)}</div>}
              </div>
              <div className="idle-business-buy">
                <span>{label('INVESTITION','INVESTMENT')} ×{quote?.levels??quantity}</span>
                <strong>{quote?fmt(quote.cost):'—'} <small>cr</small></strong>
                <button type="button" className="idle-action" data-testid={'idle-upgrade-'+room.id} disabled={!canPurchase(room,quantity,disabled)}
                  onClick={()=>onAction({action:'upgrade',id:room.id,levels:quantity})}>{locked?label('Gesperrt','Locked'):room.level>=200?'MAX':label('Ausbauen','Upgrade')}</button>
                <button type="button" className="idle-preview-trigger" disabled={disabled||!quote?.unlocked||quote.levels===0}
                  onClick={()=>onPreview({action:'upgrade',id:room.id,levels:quantity})}>{label('Auswirkung ansehen','Inspect impact')}</button>
              </div>
            </article>;
          })}</div>
        </>}
        {tab==='strategy'&&<>
          <div className="idle-subnav" role="group" aria-label={label('Managementbereich','Management section')}>
            {strategyTabs.map(item=><button key={item.id} aria-pressed={strategyTab===item.id} onClick={()=>{setStrategyTab(item.id);onClearPreview();}}>{de?item.de:item.en}</button>)}
          </div>
          {strategyTab==='research'&&<>
            <div className="idle-section-head"><div><span className="idle-kicker">{economy.legacyPointsAvailable} {label('VERFÜGBARE LEGACY-PUNKTE','AVAILABLE LEGACY POINTS')}</span><h2>{label('Permanente Forschung','Permanent research')}</h2></div></div>
            <div className="idle-floor-filter" role="group" aria-label={label('Forschungszweig','Research branch')}>{['Operations','Gaming','Hospitality'].map(branch=><button key={branch} aria-pressed={researchBranch===branch} onClick={()=>setResearchBranch(branch)}>{t(branch)}</button>)}</div>
            <div className="idle-card-grid">{economy.research.filter(item=>item.branch===researchBranch).map(item=><article className="idle-panel" key={item.id}>
              <span className="idle-tag">{t(item.branch)} · {label('RANG','RANK')} {item.rank}/{item.maxRank}</span><h3>{t(item.name)}</h3><p>{t(item.description)}</p>
              {item.prerequisite&&<small>{label('Voraussetzung','Prerequisite')}: {t(economy.research.find(r=>r.id===item.prerequisite)?.name??item.prerequisite)} ({item.prerequisiteRank})</small>}
              <button className="idle-action" disabled={disabled||!item.canBuy} onClick={()=>onAction({action:'research',id:item.id})}>{label('Erforschen','Research')} · {item.costPerRank} LP</button>
            </article>)}</div>
          </>}
          {strategyTab==='projects'&&<><div className="idle-section-head"><div><span className="idle-kicker">{label('LANGFRISTIGE INVESTITIONEN','LONG-TERM INVESTMENT')}</span><h2>{label('Kapitalprojekte','Capital projects')}</h2></div></div>
            <div className="idle-card-grid">{economy.projects.map(item=><article className="idle-panel" key={item.id}><span className="idle-tag">{item.owned?label('ABGESCHLOSSEN','COMPLETED'):label('PROJEKT','PROJECT')}</span><h3>{t(item.name)}</h3><p>{t(item.description)}</p><small>{t(item.requirement)}</small><strong>{fmt(item.cost)} cr</strong>
              <button className="idle-action" disabled={disabled||!item.canBuy} onClick={()=>onAction({action:'project',id:item.id})}>{item.owned?'✓ '+label('Abgeschlossen','Completed'):label('Investieren','Invest')}</button></article>)}</div></>}
          {strategyTab==='staff'&&<><div className="idle-section-head"><div><span className="idle-kicker">{Object.keys(economy.managerAssignments).length}/{economy.managerSlots} {label('MANAGERPLÄTZE','MANAGER SLOTS')}</span><h2>{label('Dein Führungsteam','Your management team')}</h2></div></div>
            <div className="idle-card-grid">{economy.managers.map(item=><ManagerAssignment key={economy.activePropertyId+item.id+(item.assignment??'')} manager={item} rooms={economy.rooms} disabled={disabled} de={de} t={t} onAction={onAction}/>)}</div></>}
          {strategyTab==='automation'&&<AutomationEditor key={economy.activePropertyId} initial={economy.automation} rooms={economy.rooms} unlocked={economy.automationUnlocked} disabled={disabled} de={de} t={t} onAction={onAction}/>}
          {strategyTab==='opportunities'&&<>
            <div className="idle-section-head"><div><span className="idle-kicker">{label('ZUSÄTZLICHE ERTRÄGE','ADDITIONAL REVENUE')}</span><h2>{label('Verträge & Veranstaltungen','Contracts & events')}</h2></div></div>
            <h3 className="idle-group-label">{label('Verträge','Contracts')}</h3>
            <div className="idle-card-grid">{economy.contracts.map(item=><article className="idle-panel" key={item.id}><span className="idle-tag">{label('OHNE FRIST','NO DEADLINE')}</span><h3>{t(item.name)}</h3><p>{t(item.description)}</p><p>{t(item.rewardDescription)}</p>
              {economy.activeContract===item.id&&<><div className="idle-contract-progress">{fmt(economy.contractProgress)} / {fmt(item.goal)}</div><ProgressBar value={prestigeProgress(Number(economy.contractProgress),Number(item.goal))}/></>}
              <button className="idle-action" disabled={disabled||(economy.activeContract===item.id?!economy.canClaimContract:!!economy.activeContract)}
                onClick={()=>onAction(economy.activeContract===item.id?{action:'contract.claim'}:{action:'contract.accept',id:item.id})}>
                {economy.activeContract===item.id?label('Belohnung abholen','Claim reward'):label('Annehmen','Accept')}</button>
            </article>)}</div>
            <h3 className="idle-group-label">{label('Veranstaltungen','Events')}</h3>
            {economy.activeEvent&&<div className="idle-message">{label('Laufendes Event','Active event')}: {t(economy.events.find(item=>item.id===economy.activeEvent?.id)?.name??'')} · {economy.activeEvent.progress} {label('Fortschritt','progress')}</div>}
            <div className="idle-card-grid">{economy.events.map(item=><article className="idle-panel" key={item.id}><span className="idle-tag">{item.durationMinutes} MIN</span><h3>{t(item.name)}</h3><p>{t(item.description)}</p><small>{t(item.requirement)}</small><strong>{fmt(item.cost)} cr</strong>
              <button className="idle-action" disabled={disabled||!item.canBuy} onClick={()=>onAction({action:'event',id:item.id})}>{label('Starten','Start event')}</button></article>)}</div>
          </>}
        </>}
        {tab==='expansion'&&<>
          <section className="idle-prestige idle-panel">
            <div className="idle-section-head"><div><span className="idle-kicker">{label('LANGFRISTIGER FORTSCHRITT','LONG-TERM PROGRESSION')}</span><h2>{label('Nächster Prestigeaufstieg','Next prestige')}</h2><p>{label('Ein Reset betrifft nur die ausgewählte Immobilie. Permanente Forschung und andere Immobilien bleiben erhalten.','A reset affects only the selected property. Permanent research and other properties remain.')}</p></div>
              <strong className="idle-prestige-badge">P{economy.prestige} → P{Math.min(economy.prestige+1,100)}</strong></div>
            <div className="idle-prestige-goals">
              <div><span>{label('Hauptetagen-Stufen','Main floor levels')}</span><strong>{economy.mainFloorLevels}/{economy.prestigeMainFloorLevelRequirement}</strong><ProgressBar value={levelProgress}/></div>
              <div><span>{label('Verdiente Credits im Durchlauf','Credits earned this run')}</span><strong>{fmt(economy.statistics.runCredits)} / {fmt(economy.prestigeRunCreditRequirement)}</strong><ProgressBar value={runProgress}/></div>
              <div><span>{label('Geöffnete Hauptbetriebe','Operating main floor rooms')}</span><strong>{mainRoomsOpen}/5</strong><ProgressBar value={mainRoomsOpen/5*100}/></div>
            </div>
            <button className="idle-action idle-prestige-button" disabled={disabled||!economy.canPrestige} onClick={()=>setPrestigeConfirm(true)}>{label('Prestige durchführen','Advance prestige')}</button>
            {!economy.canPrestige&&<small>{t(economy.prestigeRequirement)}</small>}
          </section>
          <div className="idle-section-head"><div><span className="idle-kicker">{label('PORTFOLIO','PORTFOLIO')}</span><h2>{label('Deine Immobilien','Your properties')}</h2></div></div>
          <div className="idle-card-grid">{economy.properties.map(item=><article className="idle-panel" key={item.id}><span className="idle-tag">{item.owned?label('IM BESITZ','OWNED'):label('NEUER STANDORT','NEW PROPERTY')}</span><h3>{t(item.name)}</h3><p>{t(item.description)}</p>{item.state&&<p>{fmt(item.state.credits)} cr · Prestige {item.state.prestige} · {item.state.totalLevels} {label('Stufen','levels')}</p>}
            <small>{t(item.requirement)}</small><strong>{fmt(item.cost)} cr</strong>
            <button className="idle-action" disabled={disabled||(item.owned?item.id===economy.activePropertyId:!item.canBuy)}
              onClick={()=>{onClearPreview();onAction({action:item.owned?'property.select':'property.buy',id:item.id});}}>
              {item.id===economy.activePropertyId?label('Aktive Immobilie','Active property'):item.owned?label('Verwalten','Manage'):label('Kaufen','Purchase')}</button></article>)}</div>
          <div className="idle-section-head"><div><span className="idle-kicker">{label('PRODUKTIONSBONI','PRODUCTION BONUSES')}</span><h2>{label('Abteilungssynergien','Department synergies')}</h2></div></div>
          <div className="idle-card-grid">{economy.synergies.map(item=><article className="idle-panel" key={item.id}><span className="idle-tag">{label('STUFE','TIER')} {item.tier}</span><h3>{t(item.name)}</h3><p>{t(item.description)}</p><strong>+{(Number(item.bonus)*100).toLocaleString(snapshot.state.language)}%</strong><small>{label('Nächste Stufe','Next level')}: {item.nextLevel??'MAX'}</small></article>)}</div>
        </>}
        {tab==='statistics'&&<><div className="idle-section-head"><div><span className="idle-kicker">{label('UNTERNEHMENSBILANZ','BUSINESS PERFORMANCE')}</span><h2>{label('Deine Zahlen','Your numbers')}</h2></div></div>
          <div className="idle-card-grid">
            {[
              [label('Gesamte Credits','Lifetime credits'),fmt(economy.statistics.lifetimeCredits)],
              [label('Credits dieser Immobilie','Property lifetime'),fmt(economy.statistics.propertyLifetimeCredits)],
              [label('Credits in diesem Durchlauf','Current run'),fmt(economy.statistics.runCredits)],
              [label('Ausgezahlte Dividenden','Dividends collected'),currency(economy.statistics.lifetimeCashClaimed)],
              [label('Gekaufte Stufen','Purchased levels'),economy.statistics.levelsPurchased],
              [label('Automatisch gekaufte Stufen','Automated levels'),economy.statistics.automatedLevels],
              [label('Abgeschlossene Veranstaltungen','Completed events'),economy.statistics.eventsCompleted],
              [label('Abgeschlossene Verträge','Completed contracts'),economy.statistics.contractsCompleted],
              [label('Offline-Kapazität','Offline capacity'),economy.offlineHours+' h']
            ].map(([name,value])=><div key={name} className="idle-stat"><span>{name}</span><strong>{value}</strong></div>)}
          </div></>}
        <footer className="idle-footer">{label('Casino Empire · Management-Prototyp · Gemeinsamer Spielstand mit der klassischen Ansicht','Casino Empire · Management prototype · Shared save with classic view')}</footer>
      </main>
    </div>
    {preview&&<aside className="idle-preview-panel" role="status"><div className="idle-card-heading"><span className="idle-tag">{label('UNVERBINDLICHE VORSCHAU','READ-ONLY PREVIEW')}</span><button onClick={onClearPreview} aria-label={label('Schließen','Close')}>×</button></div>
      {preview.success?<><h3>{label('Auswirkung der Investition','Investment impact')}</h3>
        <p>{label('Baukapital','Capital')}: {fmt(preview.budgetBefore)} → {fmt(preview.budgetAfter)} cr</p>
        <p>{label('Betriebsstufe','Business level')}: {preview.levelBefore} → {preview.levelAfter}</p>
        <p>{label('Betriebseinkommen','Business income')}: {fmt(preview.roomBefore.final)} → {fmt(preview.roomAfter.final)} cr/s</p>
        <p>{label('Portfolioeinkommen','Portfolio income')}: {fmt(preview.portfolioBefore)} → {fmt(preview.portfolioAfter)} cr/s</p>
      </>:<p>{t(preview.message)}</p>}
    </aside>}
    {prestigeConfirm&&<div className="idle-modal-overlay" role="presentation"><div className="idle-panel idle-prestige-dialog" role="dialog" aria-modal="true" aria-label={label('Prestige bestätigen','Confirm prestige')}>
      <h2>{label('Neustart für dauerhaften Wachstumsschub?','Reset for permanent growth?')}</h2>
      <p>{label('Nur das aktuelle Anwesen verliert Baukapital, Abteilungsstufen und das aktive Event. Prestige, Forschung, Projekte und andere Anwesen bleiben erhalten.','Only the current property loses construction credits, department levels and its active event. Prestige, research, projects and other properties are preserved.')}</p>
      <div className="idle-form-actions"><button onClick={()=>setPrestigeConfirm(false)}>{label('Abbrechen','Cancel')}</button>
        <button className="idle-action" disabled={disabled||!economy.canPrestige} onClick={()=>{setPrestigeConfirm(false);onAction({action:'prestige'});}}>{label('Prestige bestätigen','Confirm prestige')}</button></div>
    </div></div>}
  </div>;
}
