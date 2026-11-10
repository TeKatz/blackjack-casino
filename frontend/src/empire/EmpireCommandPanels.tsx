import {useEffect,useMemo,useState} from 'react';
import type {CSSProperties,FormEvent,FocusEvent,MouseEvent} from 'react';
import {ResearchLattice} from './EmpireCommandPixi';
import {commandProgress,researchCoordinates} from './commandCenterModel';
import {decimalInput,display,fractionToPercent,percentToFraction} from './empireBridge';
import type {Automation,EmpireEconomy,Intent,Manager,Research,Room} from './empireBridge';

export type CommandSection='research'|'projects'|'staff'|'automation'|'expansion'|'opportunities'|'room'|'statistics';
export type HoverSource=MouseEvent<HTMLElement>|FocusEvent<HTMLElement>;
export type ShowHover=(event:HoverSource,intent:Intent|null,title:string,detail?:string,cost?:string)=>void;

type PanelProps={
  section:CommandSection;economy:EmpireEconomy;language:'de'|'en';roomId:string|null;
  disabled:boolean;motion:boolean;t:(text:string)=>string;
  onClose:()=>void;onAction:(intent:Intent)=>void;onHover:ShowHover;onLeave:()=>void;
};

function Bar({value}:{value:number}){
  return <div className="cc-meter" role="progressbar" aria-valuenow={Math.round(value)} aria-valuemin={0} aria-valuemax={100}>
    <div style={{width:Math.max(0,Math.min(100,value))+'%'}}/>
  </div>;
}

function ResearchConsole({economy,language,t,disabled,motion,onAction,onHover,onLeave}:Omit<PanelProps,'section'|'roomId'|'onClose'>){
  const [branch,setBranch]=useState('Operations');
  const [focus,setFocus]=useState<string|null>(null);
  const de=language==='de';
  const items=economy.research.filter(r=>r.branch===branch);
  const positions=useMemo(()=>researchCoordinates(items),[economy.research,branch]);
  const handleEnter=(event:HoverSource,item:Research)=>{
    setFocus(item.id);
    onHover(event,item.canBuy?{action:'research',id:item.id}:null,t(item.name),t(item.description),item.costPerRank+' LP');
  };
  return <div className="cc-research-content">
    <div className="cc-module-status"><div><span className="cc-eyebrow">{de?'FORSCHUNG':'RESEARCH'}</span>
      <strong>{economy.legacyPointsAvailable} <small>LP</small></strong>
      <p>{de?'Jeder Knoten verändert dauerhaft deine Wirtschaftsstrategie.':'Every node permanently changes your economic strategy.'}</p></div>
      <div className="cc-chip-row" role="group" aria-label={de?'Forschungszweige':'Research branches'}>
        {['Operations','Gaming','Hospitality'].map(id=><button key={id} aria-pressed={branch===id} onClick={()=>{setBranch(id);setFocus(null);onLeave();}}>{t(id)}</button>)}
      </div>
    </div>
    <div className="cc-research-graph" data-testid="cc-research-graph">
      <ResearchLattice items={items} positions={positions} focus={focus} enabled={motion}/>
      {items.map(item=>{
        const at=positions[item.id];if(!at)return null;
        const complete=item.rank===item.maxRank;
        return <button type="button" key={item.id} className={'cc-research-node '+(complete?'is-complete ':item.canBuy?'is-available ':'is-locked ')+(focus===item.id?'is-focused':'')}
          style={{left:at.x+'%',top:at.y+'%'}} data-testid={'cc-research-'+item.id}
          disabled={disabled} aria-disabled={!item.canBuy}
          onMouseEnter={event=>handleEnter(event,item)} onMouseLeave={()=>{setFocus(null);onLeave();}}
          onFocus={event=>handleEnter(event,item)} onBlur={()=>{setFocus(null);onLeave();}}
          onClick={()=>{if(item.canBuy){onLeave();onAction({action:'research',id:item.id});}}}>
          <span className="cc-node-emblem">{complete?'✦':item.canBuy?'◇':'◆'}</span>
          <strong>{t(item.name)}</strong>
          <small>{de?'Rang':'Rank'} {item.rank}/{item.maxRank}</small>
          <span className="cc-node-price">{complete?'MAX':item.costPerRank+' LP'}</span>
        </button>;
      })}
    </div>
    <div className="cc-hint">{de?'Über einen Knoten fahren: Auswirkungen ansehen. Anklicken: Forschung freischalten.':'Hover nodes for exact effects. Click to research.'}</div>
  </div>;
}

function ProjectConsole({economy,language,t,disabled,onAction,onHover,onLeave}:Omit<PanelProps,'section'|'roomId'|'onClose'|'motion'>){
  const de=language==='de';
  const count=economy.projects.filter(p=>p.owned).length;
  return <div className="cc-project-content">
    <div className="cc-module-status"><div><span className="cc-eyebrow">{de?'STRATEGISCHE INVESTITIONEN':'STRATEGIC INVESTMENT'}</span>
      <strong>{count}/{economy.projects.length} <small>{de?'Projekte umgesetzt':'completed'}</small></strong></div>
      <p>{de?'Finanziere Projekte direkt aus deinem Baukapital. Erreichte Meilensteine bleiben gespeichert.':'Invest construction credits in lasting strategic improvements.'}</p>
    </div>
    <div className="cc-project-grid">
      {economy.projects.map((project,i)=><button type="button" key={project.id}
        className={'cc-project '+(project.owned?'is-complete':project.canBuy?'is-available':'is-locked')}
        data-testid={'cc-project-'+project.id} disabled={disabled} aria-disabled={!project.canBuy}
        onMouseEnter={e=>onHover(e,project.canBuy?{action:'project',id:project.id}:null,t(project.name),t(project.description)+' · '+t(project.requirement),project.cost+' cr')}
        onMouseLeave={onLeave}
        onFocus={e=>onHover(e,project.canBuy?{action:'project',id:project.id}:null,t(project.name),t(project.description),project.cost+' cr')}
        onBlur={onLeave} onClick={()=>{if(project.canBuy){onLeave();onAction({action:'project',id:project.id});}}}>
        <span className="cc-project-symbol">{project.owned?'✓':i%4===0?'◈':i%4===1?'⬡':i%4===2?'✧':'◇'}</span>
        <span className="cc-project-info"><strong>{t(project.name)}</strong><small>{project.owned?(de?'Fertiggestellt':'Complete'):project.canBuy?(de?'Jetzt finanzierbar':'Ready to fund'):t(project.requirement)|| (de?'Noch nicht verfügbar':'Locked')}</small></span>
        <span className="cc-project-price">{project.owned?'✓':display(project.cost,language)+' cr'}</span>
      </button>)}
    </div>
  </div>;
}

function StaffConsole({economy,language,t,disabled,onAction,onHover,onLeave}:Omit<PanelProps,'section'|'roomId'|'onClose'|'motion'>){
  const de=language==='de';
  const [managerId,setManagerId]=useState<string>(economy.managers[0]?.id??'');
  const selected=economy.managers.find(manager=>manager.id===managerId);
  const assigned=Object.keys(economy.managerAssignments).length;
  const managers=economy.managers;
  return <div className="cc-staff-content">
    <div className="cc-module-status"><div><span className="cc-eyebrow">{de?'PERSONALEINSATZ':'STAFF ASSIGNMENT'}</span>
      <strong>{assigned}/{economy.managerSlots} <small>{de?'Manager aktiv':'managers active'}</small></strong></div>
      <p>{de?'Manager wählen und einem Betrieb zuordnen. Die Vorschau zeigt den wirtschaftlichen Effekt vor der Zuweisung.':'Choose a manager and assign a department. Hover for the true production impact.'}</p>
    </div>
    <div className="cc-staff-workspace">
      <div className="cc-manager-roster" role="group" aria-label={de?'Manager auswählen':'Choose manager'}>
        {managers.map((manager,i)=><button key={manager.id} type="button" className="cc-manager"
          aria-pressed={manager.id===managerId} data-testid={'cc-manager-'+manager.id}
          onClick={()=>{setManagerId(manager.id);onLeave();}}>
          <span className="cc-manager-avatar" style={{'--portrait-accent':['#e6bf82','#88c5b1','#b4a0df','#d8a5b4'][i%4]} as CSSProperties}>
            {manager.name.split(' ').map(v=>v[0]).join('')}</span>
          <span><strong>{manager.name}</strong><small>{t(manager.role)}</small><small className="cc-manager-assigned">{manager.assignment?t(economy.rooms.find(r=>r.id===manager.assignment)?.name??manager.assignment):de?'Nicht zugewiesen':'Unassigned'}</small></span>
          <span className={'cc-mini-dot '+(manager.assignment?'active':'')}/>
        </button>)}
      </div>
      <div className="cc-manager-targets">
        <div className="cc-assignment-head"><strong>{selected?t(selected.name):''}</strong>
          <small>{selected?t(selected.description):''}</small>
          {selected&&!selected.available&&!selected.assignment&&<span className="cc-alert">{de?'Freischaltung: Prestige':'Unlock: prestige'} {selected.requiredPrestige}</span>}</div>
        <div className="cc-target-grid">
          {economy.rooms.map(room=>{
            const current=selected?.assignment===room.id;
            const allowed=!!selected&&(selected.available||!!selected.assignment)&&room.level>0&&!current;
            return <button type="button" key={room.id} data-testid={'cc-target-'+room.id}
              className={'cc-target '+(current?'is-current':'')} disabled={disabled} aria-disabled={!allowed}
              onMouseEnter={event=>onHover(event,allowed?{action:'manager',id:selected?.id,roomId:room.id}:null,
                t(room.name),de?'Manager diesem Betrieb zuweisen':'Assign manager to department')}
              onMouseLeave={onLeave}
              onFocus={event=>onHover(event,allowed?{action:'manager',id:selected?.id,roomId:room.id}:null,t(room.name))}
              onBlur={onLeave} onClick={()=>{if(selected&&allowed){onLeave();onAction({action:'manager',id:selected.id,roomId:room.id});}}}>
              <span>{current?'●':'○'}</span><strong>{t(room.name)}</strong><small>{room.level>0?'Lv. '+room.level:'◆'}</small>
            </button>;
          })}
        </div>
        {selected?.assignment&&<button type="button" className="cc-small-action" disabled={disabled}
          onClick={()=>onAction({action:'manager',id:selected.id})}>{de?'Zuweisung entfernen':'Remove assignment'}</button>}
      </div>
    </div>
  </div>;
}

function AutomationConsole({economy,language,disabled,onAction}:Omit<PanelProps,'section'|'roomId'|'onClose'|'motion'|'t'|'onHover'|'onLeave'>){
  const de=language==='de';
  const initial=economy.automation;
  const [form,setForm]=useState<Automation>(initial);
  const [percent,setPercent]=useState(fractionToPercent(initial.spendPercent));
  const [dirty,setDirty]=useState(false),[error,setError]=useState('');
  const version=JSON.stringify(initial);
  useEffect(()=>{if(!dirty){setForm(initial);setPercent(fractionToPercent(initial.spendPercent));}},[version,dirty]);
  const patch=(v:Partial<Automation>)=>{setForm(x=>({...x,...v}));setDirty(true);setError('');};
  const submit=(event:FormEvent)=>{
    event.preventDefault();
    const reserve=decimalInput(form.reserve),spend=percentToFraction(percent);
    if(reserve===null||spend===null||!Number.isInteger(form.targetLevel)||form.targetLevel<1||form.targetLevel>200
      ||form.priorities.length>15||new Set(form.priorities).size!==form.priorities.length){
      setError(de?'Ungültiger Investitionsplan.':'Invalid investment policy.');return;
    }
    onAction({action:'automation',automation:{...form,reserve,spendPercent:spend}});
    setDirty(false);
  };
  return <form className="cc-auto-content" onSubmit={submit}>
    <div className="cc-module-status"><div><span className="cc-eyebrow">{de?'AUTOMATISCH INVESTIEREN':'AUTO INVEST'}</span>
      <strong>{form.enabled?(de?'Automatik läuft':'Automation running'):(de?'Manuelle Investitionen':'Manual investments')}</strong>
      <p>{de?'Alle Betriebe verdienen immer automatisch Geld. Diese Regeln steuern zusätzliche automatische Upgrades.':'Every business earns automatically; these rules also control automatic upgrades.'}</p></div>
      <span className={'cc-live-pill '+(form.enabled?'is-active':'')}>{form.enabled?'● '+(de?'AKTIV':'ACTIVE'):'○ '+(de?'AUS':'OFF')}</span>
    </div>
    {!economy.automationUnlocked&&<div className="cc-alert">{de?'Erfordert Executive Offices sowie die Forschung Executive Automation. Bis dahin bleiben alle manuellen Käufe verfügbar.':'Requires Executive Offices and Executive Automation research. Manual upgrades remain available.'}</div>}
    <div className="cc-automation-grid">
      <label className="cc-switch-line"><span><strong>{de?'Automatische Käufe':'Enable auto-buy'}</strong><small>{de?'Prüft alle 30 Produktionssekunden':'Runs every 30 production seconds'}</small></span>
        <input type="checkbox" checked={form.enabled} onChange={e=>patch({enabled:e.target.checked})}/><span className="cc-switch" aria-hidden="true"/></label>
      <label className="cc-field"><span>{de?'Sicherheitsreserve':'Never spend below'}</span><input inputMode="decimal" value={form.reserve} onChange={e=>patch({reserve:e.target.value})}/><small>Credits</small></label>
      <label className="cc-field"><span>{de?'Maximales Budget je Entscheidung':'Maximum spend per decision'}</span><div className="cc-percent-field"><input inputMode="decimal" value={percent} onChange={e=>{setPercent(e.target.value);setDirty(true);setError('');}}/><strong>%</strong></div><small>1–50 %</small></label>
      <label className="cc-field"><span>{de?'Bis zu welcher Stufe?':'Maximum target level'}</span><input type="number" value={form.targetLevel} min="1" max="200" onChange={e=>patch({targetLevel:Number(e.target.value)})}/><small>1–200</small></label>
      <label className="cc-switch-line"><span><strong>{de?'Günstigsten Meilenstein zuerst':'Cheapest milestone first'}</strong><small>{de?'Automatische Priorisierung':'Automatic prioritization'}</small></span>
        <input type="checkbox" checked={form.cheapestMilestone} onChange={e=>patch({cheapestMilestone:e.target.checked})}/><span className="cc-switch" aria-hidden="true"/></label>
    </div>
    {!form.cheapestMilestone&&<div className="cc-priority-editor">
      <strong>{de?'Investitionsreihenfolge':'Investment priority'}</strong>
      <div>{form.priorities.map((id,index)=><span className="cc-priority-token" key={id}>
        {index+1}. {economy.rooms.find(room=>room.id===id)?.name??id}
        <button type="button" aria-label={de?'Entfernen':'Remove'} onClick={()=>patch({priorities:form.priorities.filter(x=>x!==id)})}>×</button>
      </span>)}</div>
      <div className="cc-priority-actions">
        <select aria-label={de?'Betrieb hinzufügen':'Add department'} defaultValue="" key={form.priorities.join(',')}
          onChange={e=>{if(e.target.value)patch({priorities:[...form.priorities,e.target.value]});}}>
          <option value="">{de?'Betrieb hinzufügen…':'Add department…'}</option>
          {economy.rooms.filter(room=>!form.priorities.includes(room.id)).map(room=><option key={room.id} value={room.id}>{room.name}</option>)}
        </select>
        {form.priorities.length>0&&<button type="button" onClick={()=>{const p=[...form.priorities];p.push(p.shift()!);patch({priorities:p});}}>{de?'Reihenfolge rotieren':'Rotate priority'}</button>}
      </div>
    </div>}
    {error&&<p className="cc-alert" role="alert">{error}</p>}
    <div className="cc-module-actions"><button type="button" onClick={()=>{setForm(initial);setPercent(fractionToPercent(initial.spendPercent));setDirty(false);setError('');}}>{de?'Zurücksetzen':'Reset'}</button>
      <button type="submit" className="cc-primary-action" data-testid="cc-automation-save" disabled={disabled||!economy.automationUnlocked}>{de?'Regeln übernehmen':'Apply policy'}</button></div>
  </form>;
}

function ExpansionConsole({economy,language,t,disabled,onAction,onHover,onLeave}:Omit<PanelProps,'section'|'roomId'|'onClose'|'motion'>){
  const de=language==='de';
  const [confirm,setConfirm]=useState(false);
  const openMain=economy.rooms.filter(r=>r.floor===1&&r.level>0).length;
  return <div className="cc-expansion-content">
    <div className="cc-expansion-goal">
      <div className="cc-module-status"><div><span className="cc-eyebrow">PRESTIGE</span><strong>P{economy.prestige} → P{economy.prestige+1}</strong>
        <p>{de?'Ein Reset betrifft nur den aktiven Standort; permanente Forschung und andere Immobilien bleiben bestehen.':'A reset affects only the selected property. Research and other estates remain.'}</p></div>
        <button type="button" className="cc-primary-action" data-testid="cc-prestige" disabled={disabled||!economy.canPrestige} onClick={()=>setConfirm(true)}>{de?'Prestige auslösen':'Prestige reset'}</button>
      </div>
      <div className="cc-goal-grid">
        <div><span>{de?'Hauptetagen-Level':'Main floor levels'}</span><strong>{economy.mainFloorLevels}/{economy.prestigeMainFloorLevelRequirement}</strong><Bar value={commandProgress(economy.mainFloorLevels,economy.prestigeMainFloorLevelRequirement)}/></div>
        <div><span>{de?'Einnahmen dieses Durchlaufs':'This run earnings'}</span><strong>{display(economy.statistics.runCredits,language)}/{display(economy.prestigeRunCreditRequirement,language)}</strong><Bar value={commandProgress(Number(economy.statistics.runCredits),Number(economy.prestigeRunCreditRequirement))}/></div>
        <div><span>{de?'Geöffnete Hauptbetriebe':'Open main-floor businesses'}</span><strong>{openMain}/5</strong><Bar value={commandProgress(openMain,5)}/></div>
      </div>
      {!economy.canPrestige&&<small>{t(economy.prestigeRequirement)}</small>}
    </div>
    <div className="cc-section-label">{de?'CASINO-PORTFOLIO':'CASINO PORTFOLIO'}</div>
    <div className="cc-property-grid">
      {economy.properties.map((property,i)=><button key={property.id} type="button" className={'cc-property '+(property.id===economy.activePropertyId?'is-selected':'')}
        disabled={disabled} aria-disabled={property.owned?property.id===economy.activePropertyId:!property.canBuy}
        onMouseEnter={e=>onHover(e,property.canBuy?{action:'property.buy',id:property.id}:null,
          t(property.name),t(property.description)+' · '+t(property.requirement),property.owned?undefined:property.cost+' cr')}
        onMouseLeave={onLeave} onFocus={e=>onHover(e,null,t(property.name),t(property.description))} onBlur={onLeave}
        onClick={()=>{if(property.owned?property.id!==economy.activePropertyId:property.canBuy){onLeave();onAction({action:property.owned?'property.select':'property.buy',id:property.id});}}}>
        <span className="cc-property-monogram">{['♜','◈','♛','✧'][i%4]}</span>
        <span><strong>{t(property.name)}</strong><small>{property.owned?'P'+property.state?.prestige+' · '+(de?'Im Besitz':'Owned'):display(property.cost,language)+' cr'}</small></span>
        <span className="cc-property-verb">{property.id===economy.activePropertyId?'●':property.owned?'↗':'+'}</span>
      </button>)}
    </div>
    <div className="cc-section-label">{de?'AKTIVE SYNERGIEN':'SYNERGY CIRCUITS'}</div>
    <div className="cc-synergy-strip">{economy.synergies.map(s=><span key={s.id} title={t(s.description)}>{t(s.name)} <b>Tier {s.tier}</b></span>)}</div>
    {confirm&&<div className="cc-confirm-mask" role="presentation"><div role="alertdialog" aria-modal="true" className="cc-confirm" aria-label={de?'Prestige bestätigen':'Confirm prestige'}>
      <h3>{de?'Aktuellen Standort zurücksetzen?':'Reset current estate?'}</h3>
      <p>{de?'Du verlierst den Fortschritt dieses Durchlaufs am aktiven Standort. Der dauerhafte Prestige-Bonus bleibt erhalten.':'You lose this run’s progress for the current estate. Permanent prestige bonuses are retained.'}</p>
      <div><button onClick={()=>setConfirm(false)}>{de?'Abbrechen':'Cancel'}</button><button className="cc-primary-action" disabled={disabled||!economy.canPrestige} onClick={()=>{setConfirm(false);onAction({action:'prestige'});}}>{de?'Prestige bestätigen':'Confirm reset'}</button></div>
    </div></div>}
  </div>;
}

function OpportunitiesConsole({economy,language,t,disabled,onAction,onHover,onLeave}:Omit<PanelProps,'section'|'roomId'|'onClose'|'motion'>){
  const de=language==='de';
  return <div className="cc-opportunities-content">
    <div className="cc-opportunity-column">
      <div className="cc-section-label">{de?'VERTRÄGE':'CONTRACTS'}</div>
      {economy.contracts.map(contract=>{
        const current=economy.activeContract===contract.id;
        return <button type="button" key={contract.id} className="cc-opportunity" disabled={disabled||(current?!economy.canClaimContract:!!economy.activeContract)}
          onMouseEnter={e=>onHover(e,null,t(contract.name),t(contract.description)+' · '+t(contract.rewardDescription))}
          onMouseLeave={onLeave} onClick={()=>{onLeave();onAction(current?{action:'contract.claim'}:{action:'contract.accept',id:contract.id});}}>
          <span>▤</span><span><strong>{t(contract.name)}</strong><small>{current?display(economy.contractProgress,language)+' / '+display(contract.goal,language):t(contract.rewardDescription)}</small>
            {current&&<Bar value={commandProgress(Number(economy.contractProgress),Number(contract.goal))}/>}</span>
          <b>{current?economy.canClaimContract?'✓':'…':economy.activeContract?'—':'+'}</b>
        </button>;
      })}
    </div>
    <div className="cc-opportunity-column">
      <div className="cc-section-label">{de?'VERANSTALTUNGEN':'EVENTS'}</div>
      {economy.activeEvent&&<div className="cc-live-pill is-active">{de?'Läuft: ':'Live: '}{t(economy.events.find(e=>e.id===economy.activeEvent?.id)?.name??economy.activeEvent.id)} · {economy.activeEvent.progress}</div>}
      {economy.events.map(event=><button type="button" key={event.id} className="cc-opportunity" disabled={disabled||!event.canBuy}
        onMouseEnter={e=>onHover(e,event.canBuy?{action:'event',id:event.id}:null,t(event.name),t(event.description),event.cost+' cr')}
        onMouseLeave={onLeave} onClick={()=>{onLeave();onAction({action:'event',id:event.id});}}>
        <span>✧</span><span><strong>{t(event.name)}</strong><small>{event.durationMinutes} min · {display(event.cost,language)} cr</small></span><b>+</b>
      </button>)}
    </div>
  </div>;
}

function RoomConsole({economy,language,t,roomId,disabled,onAction,onHover,onLeave}:Omit<PanelProps,'section'|'onClose'|'motion'>){
  const de=language==='de';
  const room=economy.rooms.find(r=>r.id===roomId);
  if(!room)return null;
  return <div className="cc-room-content">
    <div className="cc-module-status"><div><span className="cc-eyebrow">{de?'BETRIEBSBERICHT':'DEPARTMENT BRIEFING'}</span><strong>{t(room.name)}</strong>
      <p>{t(room.description)}</p></div><span className="cc-room-level-tag">LV. {room.level}/200</span></div>
    <div className="cc-room-metrics">
      <div><span>{de?'Produktion':'Production'}</span><strong>{display(room.rate,language)} cr/s</strong></div>
      <div><span>{de?'Basisproduktion':'Base income'}</span><strong>{display(room.breakdown.base,language)} cr/s</strong></div>
      <div><span>{de?'Strategische Boni':'Strategy bonuses'}</span><strong>{display(room.breakdown.strategicBonus,language)}</strong></div>
      <div><span>{de?'Nächster Meilenstein':'Next milestone'}</span><strong>{room.level>=200?'MAX':Math.floor(room.level/10)*10+10}</strong></div>
    </div>
    <div className="cc-section-label">{de?'DIREKT INVESTIEREN':'QUICK INVEST'}</div>
    <div className="cc-room-quote-grid">{room.quotes.map(q=><button key={q.requestedLevels} type="button"
      disabled={disabled} aria-disabled={!q.canBuy} className="cc-quote"
      onMouseEnter={ev=>onHover(ev,{action:'upgrade',id:room.id,levels:q.requestedLevels},t(room.name)+' ×'+q.requestedLevels,undefined,q.cost+' cr')}
      onMouseLeave={onLeave}
      onFocus={ev=>onHover(ev,{action:'upgrade',id:room.id,levels:q.requestedLevels},t(room.name)+' ×'+q.requestedLevels,undefined,q.cost+' cr')}
      onBlur={onLeave}
      onClick={()=>{if(q.canBuy){onLeave();onAction({action:'upgrade',id:room.id,levels:q.requestedLevels});}}}>
      <strong>×{q.requestedLevels}</strong><small>{display(q.cost,language)} cr</small>
      </button>)}</div>
    <div className="cc-hint">{t(room.requirement)|| (de?'Jeder zehnte Level verdoppelt die Grundproduktion.':'Every tenth level doubles base production.')}</div>
  </div>;
}

function StatisticsConsole({economy,language}:Pick<PanelProps,'economy'|'language'>){
  const de=language==='de',stats=economy.statistics;
  const list=[
    [de?'Credits insgesamt':'Lifetime credits',stats.lifetimeCredits],
    [de?'Credits aktives Anwesen':'Current property earned',stats.propertyLifetimeCredits],
    [de?'Credits dieses Durchlaufs':'Current run earned',stats.runCredits],
    [de?'Dividenden':'Dividends paid',stats.lifetimeCashClaimed],
    [de?'Gekaufte Stufen':'Levels purchased',stats.levelsPurchased],
    [de?'Automatisierte Stufen':'Auto-purchased levels',stats.automatedLevels],
    [de?'Erfolgreiche Events':'Completed events',stats.eventsCompleted],
    [de?'Erfüllte Verträge':'Completed contracts',stats.contractsCompleted],
  ];
  return <div className="cc-stat-grid">{list.map(([name,value])=><div key={name}><span>{name}</span><strong>{display(value,language)}</strong></div>)}</div>;
}

export function CommandWindow({section,economy,language,roomId,disabled,motion,t,onClose,onAction,onHover,onLeave}:PanelProps){
  const de=language==='de';
  const titles:Record<CommandSection,[string,string]>={
    research:['Forschungsnetzwerk','Research network'],projects:['Projektportfolio','Projects'],
    staff:['Führungsteam','Management team'],automation:['Automatisierung','Automation'],
    expansion:['Expansion & Prestige','Expansion & prestige'],opportunities:['Aufträge & Events','Contracts & events'],
    room:['Betriebsdetails','Business details'],statistics:['Statistik','Statistics'],
  };
  useEffect(()=>{
    const handler=(event:KeyboardEvent)=>{if(event.key==='Escape'){event.stopPropagation();event.preventDefault();onClose();}};
    document.addEventListener('keydown',handler,true);
    return()=>document.removeEventListener('keydown',handler,true);
  },[onClose]);
  return <div className="cc-window-layer" data-testid="cc-window-layer">
    <button className="cc-window-backdrop" tabIndex={-1} aria-label={de?'Schließen':'Close'} onClick={onClose}/>
    <section className={'cc-window cc-window-'+section} role="dialog" aria-modal="true" aria-label={titles[section][de?0:1]}>
      <header className="cc-window-header"><div><span className="cc-eyebrow">CASINO EMPIRE / COMMAND CENTER</span><h2>{titles[section][de?0:1]}</h2></div>
        <button type="button" className="cc-window-close" data-testid="cc-window-close" onClick={onClose} aria-label={de?'Fenster schließen':'Close window'}>×</button></header>
      <div className="cc-window-body">
        {section==='research'&&<ResearchConsole economy={economy} language={language} t={t} disabled={disabled} motion={motion} onAction={onAction} onHover={onHover} onLeave={onLeave}/>}
        {section==='projects'&&<ProjectConsole economy={economy} language={language} t={t} disabled={disabled} onAction={onAction} onHover={onHover} onLeave={onLeave}/>}
        {section==='staff'&&<StaffConsole economy={economy} language={language} t={t} disabled={disabled} onAction={onAction} onHover={onHover} onLeave={onLeave}/>}
        {section==='automation'&&<AutomationConsole key={economy.activePropertyId} economy={economy} language={language} disabled={disabled} onAction={onAction}/>}
        {section==='expansion'&&<ExpansionConsole economy={economy} language={language} t={t} disabled={disabled} onAction={onAction} onHover={onHover} onLeave={onLeave}/>}
        {section==='opportunities'&&<OpportunitiesConsole economy={economy} language={language} t={t} disabled={disabled} onAction={onAction} onHover={onHover} onLeave={onLeave}/>}
        {section==='room'&&<RoomConsole economy={economy} language={language} t={t} roomId={roomId} disabled={disabled} onAction={onAction} onHover={onHover} onLeave={onLeave}/>}
        {section==='statistics'&&<StatisticsConsole economy={economy} language={language}/>}
      </div>
    </section>
  </div>;
}
