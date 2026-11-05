import {memo,useCallback,useEffect,useRef,useState} from 'react';
import type {FormEvent,ReactNode} from 'react';
import {createRoot} from 'react-dom/client';
import {EmpireScene,roomArt} from './EmpireScene';
import {decimalInput,display,fractionToPercent,listen,percentToFraction,post,retainRooms,send} from './empireBridge';
import type {Automation,EmpireSnapshot,Intent,Manager,Preview,Reply,Room} from './empireBridge';
import translations from './empireTranslations.json';
import './empire.css';

type Page='estate'|'synergies'|'research'|'projects'|'properties'|'team'|'automation'|'events'|'contracts'|'statistics';
const pages:{id:Page;group:string;en:string;de:string;icon:string}[]=[
  {id:'estate',group:'ESTATE',en:'Your estate',de:'Dein Anwesen',icon:'♜'},
  {id:'synergies',group:'ESTATE',en:'Circuits',de:'Synergien',icon:'◇'},
  {id:'research',group:'DEVELOPMENT',en:'Research',de:'Forschung',icon:'✧'},
  {id:'projects',group:'DEVELOPMENT',en:'Projects',de:'Projekte',icon:'⌂'},
  {id:'properties',group:'DEVELOPMENT',en:'Properties',de:'Immobilien',icon:'▦'},
  {id:'team',group:'OPERATIONS',en:'Specialists',de:'Spezialisten',icon:'♟'},
  {id:'automation',group:'OPERATIONS',en:'Automation',de:'Automatisierung',icon:'↻'},
  {id:'events',group:'OPPORTUNITIES',en:'Events',de:'Veranstaltungen',icon:'✦'},
  {id:'contracts',group:'OPPORTUNITIES',en:'Contracts',de:'Verträge',icon:'▤'},
  {id:'statistics',group:'INSIGHTS',en:'Statistics',de:'Statistik',icon:'▥'},
];
const groupDe:Record<string,string>={ESTATE:'ANWESEN',DEVELOPMENT:'ENTWICKLUNG',OPERATIONS:'BETRIEB',OPPORTUNITIES:'GELEGENHEITEN',INSIGHTS:'EINBLICKE'};
const dictionary=translations as Record<string,string>;
const RoomCard=memo(function RoomCard({room,selected,onSelect,language}:{room:Room;selected:boolean;onSelect:(id:string)=>void;language:'de'|'en'}){
  const de=language==='de',q=room.quotes[0];
  return <button className={'empire-room '+(selected?'selected ':'')+(q.unlocked?'':'locked')} type="button"
    data-testid={'room-'+room.id} aria-pressed={selected} onClick={()=>onSelect(room.id)}>
    <div className="empire-room-art" style={roomArt(room.id)}/>
    <div className="empire-room-top"><span>{q.unlocked?(room.level?de?'IN BETRIEB':'OPERATING':de?'VERFÜGBAR':'AVAILABLE'):'◆ '+(de?'GESPERRT':'LOCKED')}</span><b>{String(room.floor).padStart(2,'0')}</b></div>
    <div className="empire-room-copy"><span>{de?dictionary[room.category]??room.category:room.category}</span>
      <h3>{de?dictionary[room.name]??room.name:room.name}</h3><div><b>{de?'Stufe':'Level'} {room.level}</b><span>{display(room.rate,language)} cr/s</span></div>
      <div className="empire-room-meter"><i style={{width:`${room.level/2}%`}}/></div>
    </div>
  </button>;
});
function Confirm({children,onConfirm,onCancel,label}:{children:ReactNode;onConfirm:()=>void;onCancel:()=>void;label:string}){
  const dialog=useRef<HTMLDialogElement>(null);
  useEffect(()=>{dialog.current?.showModal();return()=>dialog.current?.close();},[]);
  return <dialog ref={dialog} className="empire-confirm" onCancel={e=>{e.preventDefault();onCancel();}} aria-label={label}>
    <h2>{label}</h2>{children}<div className="empire-actions"><button onClick={onCancel}>←</button><button className="empire-gold" onClick={onConfirm}>{label}</button></div>
  </dialog>;
}
function AutomationForm({initial,unlocked,disabled,language,onSave,onPreview,onClear}:{initial:Automation;unlocked:boolean;disabled:boolean;language:'de'|'en';onSave:(a:Automation)=>void;onPreview:(a:Automation)=>void;onClear:()=>void}){
  const de=language==='de';
  const [form,setForm]=useState(initial),[dirty,setDirty]=useState(false),[error,setError]=useState('');
  useEffect(()=>{if(!dirty)setForm(initial);},[initial,dirty]);
  const update=(values:Partial<Automation>)=>{setForm(f=>({...f,...values}));setDirty(true);setError('');onClear();};
  const [percent,setPercent]=useState(fractionToPercent(initial.spendPercent));
  const [priorities,setPriorities]=useState(initial.priorities.join(', '));
  useEffect(()=>{if(!dirty){setPercent(fractionToPercent(initial.spendPercent));setPriorities(initial.priorities.join(', '));}},[initial,dirty]);
  const parse=():Automation|null=>{
    const reserve=decimalInput(form.reserve),budget=decimalInput(percent);
    if(!reserve||!budget||Number(budget)<1||Number(budget)>50||!Number.isInteger(form.targetLevel)||form.targetLevel<1||form.targetLevel>200){
      setError(de?'Reserve, Budget (1–50 %) und Zielstufe (1–200) prüfen.':'Check reserve, budget (1–50%) and target level (1–200).');return null;
    }
    // Policy percentage only. Construction money remains an exact decimal string.
    return {...form,reserve,spendPercent:percentToFraction(budget)!,priorities:priorities.split(',').map(s=>s.trim()).filter(Boolean)};
  };
  const submit=(e:FormEvent)=>{e.preventDefault();const next=parse();if(next)onSave(next);};
  return <form className="empire-panel empire-form" onSubmit={submit} data-testid="automation-form">
    <h2>{de?'Deine Baupolitik':'Your construction policy'}</h2>
    <p>{de?'Alle 30 Produktionssekunden werden begrenzte Upgrades geprüft. Es entsteht kein zusätzlicher Produktionsbonus.':'Bounded upgrades are evaluated every 30 production seconds. Automation does not add a production multiplier.'}</p>
    {!unlocked&&<p className="empire-lock">{de?'Benötigt: Executive Offices und Automatisierungsforschung.':'Requires Executive Offices and Executive Automation research.'}</p>}
    <label className="empire-check"><input type="checkbox" checked={form.enabled} onChange={e=>update({enabled:e.target.checked})}/>{de?'Automatisierung aktivieren':'Enable automation'}</label>
    <label>{de?'Reserve in Credits':'Credit reserve'}<input name="reserve" inputMode="decimal" value={form.reserve} onChange={e=>update({reserve:e.target.value})}/></label>
    <label>{de?'Budget pro Prüfung (%)':'Budget per evaluation (%)'}<input name="percent" inputMode="decimal" value={percent} onChange={e=>{setPercent(e.target.value);update({});}}/></label>
    <label>{de?'Zielstufe':'Target level'}<input name="target" type="number" min={1} max={200} value={form.targetLevel} onChange={e=>update({targetLevel:Number(e.target.value)})}/></label>
    <label className="empire-check"><input type="checkbox" checked={form.cheapestMilestone} onChange={e=>update({cheapestMilestone:e.target.checked})}/>{de?'Günstigsten Meilenstein bevorzugen':'Prefer cheapest milestone'}</label>
    <label>{de?'Prioritäten (Raum-IDs, durch Komma getrennt)':'Priorities (room IDs, comma separated)'}<input name="priorities" value={priorities} onChange={e=>{setPriorities(e.target.value);update({});}}/></label>
    <p>{de?'Raum-IDs findest du im Rauminspektor.':'Room IDs appear in the room inspector.'}</p>
    {error&&<p role="alert">{error}</p>}
    <button type="button" disabled={disabled} onClick={()=>{setForm(initial);setPercent(fractionToPercent(initial.spendPercent));setPriorities(initial.priorities.join(', '));setDirty(false);setError('');onClear();}}>{de?'Gespeicherte Werte laden':'Restore saved values'}</button>
    <div className="empire-actions"><button type="button" disabled={!unlocked||disabled} onClick={()=>{const a=parse();if(a)onPreview(a);}}>{de?'Vorschau':'Preview'}</button>
      <button className="empire-gold" disabled={!unlocked||disabled} type="submit" data-testid="automation-save">{de?'Baupolitik speichern':'Save policy'}</button></div>
  </form>;
}
function ManagerCard({manager,rooms,disabled,onAction,onPreview,language}:{manager:Manager;rooms:Room[];disabled:boolean;onAction:(id:string,room?:string)=>void;onPreview:(id:string,room?:string)=>void;language:'de'|'en'}){
  const [room,setRoom]=useState(manager.assignment??'');const de=language==='de';
  return <article className="empire-panel"><span className="empire-eyebrow">{de?dictionary[manager.role]??manager.role:manager.role}</span><h2>{manager.name}</h2>
    <p>{de?dictionary[manager.description]??manager.description:manager.description}</p><p>{de?'Benötigt Prestige':'Requires Prestige'} {manager.requiredPrestige}</p>
    <label>{de?'Abteilung':'Department'}<select value={room} onChange={e=>setRoom(e.target.value)} aria-label={manager.name}>
      <option value="">{de?'Inaktiv':'Inactive'}</option>{rooms.filter(r=>r.level>0).map(r=><option key={r.id} value={r.id}>{de?dictionary[r.name]??r.name:r.name}</option>)}</select></label>
    <div className="empire-actions"><button disabled={disabled||(!manager.available&&!manager.assignment)} onClick={()=>onPreview(manager.id,room||undefined)}>{de?'Vorschau':'Preview'}</button>
    <button className="empire-gold" data-testid={'assign-'+manager.id} disabled={disabled||(!manager.available&&!manager.assignment)} onClick={()=>onAction(manager.id,room||undefined)}>{de?'Zuweisung speichern':'Save assignment'}</button></div>
  </article>;
}

function EmpireApp(){
  const [snapshot,setSnapshot]=useState<EmpireSnapshot|null>(null),[page,setPage]=useState<Page>('estate');
  const [selected,setSelected]=useState<string|null>('slots'),[floor,setFloor]=useState(1),[filter,setFilter]=useState('all'),[bulk,setBulk]=useState(1);
  const [pending,setPending]=useState<string|null>(null),[uncertain,setUncertain]=useState(false),[notice,setNotice]=useState<string|null>(null);
  const [preview,setPreview]=useState<Preview|null>(null),[visible,setVisible]=useState(true),[motion,setMotion]=useState(true),[diagnostics,setDiagnostics]=useState(false),[rendererReady,setRendererReady]=useState(false);
  const [lowMotion,setLowMotion]=useState(matchMedia('(prefers-reduced-motion: reduce)').matches),[confirmation,setConfirmation]=useState(false);
  const [researchBranch,setResearchBranch]=useState('Operations');
  const live=useRef(snapshot);live.current=snapshot;
  const pageRef=useRef(page);pageRef.current=page;
  const pendingRef=useRef<string|null>(null),previewId=useRef<string|null>(null),previewTimer=useRef<ReturnType<typeof setTimeout>|undefined>(undefined);
  const clearPreview=useCallback(()=>{clearTimeout(previewTimer.current);previewId.current=null;setPreview(null);},[]);
  const selectRoom=useCallback((id:string)=>{setSelected(old=>old===id?null:id);clearPreview();},[clearPreview]);
  const sceneReady=useCallback(()=>setRendererReady(true),[]);
  const go=(next:Page)=>{setPage(next);clearPreview();};
  const close=()=>post('empire.close');
  useEffect(()=>{
    const accept=(reply:Reply)=>{
      if(reply.requestId===previewId.current){setPreview(reply.preview);return;}
      if(reply.requestId===pendingRef.current){pendingRef.current=null;setPending(null);setUncertain(false);setNotice(reply.message);clearPreview();}
    };
    const stop=listen(message=>{
      if(message.type==='snapshot'){
        const next=message.data;
        if(next.lastReply)accept(next.lastReply);
        setSnapshot(previous=>{
          if(previous&&previous.session===next.session&&next.revision<previous.revision)return previous;
          if(previous)next.state.economy.rooms=retainRooms(previous.state.economy.rooms,next.state.economy.rooms);
          return next;
        });
      }else if(message.type==='result')accept(message.data);
      else if(message.type==='visibility'){setVisible(message.visible);if(!message.visible)clearPreview();}
      else if(message.type==='escape'){
        clearPreview();if(pageRef.current!=='estate')setPage('estate');else post('empire.close');
      }else if(message.type==='invalid'){setNotice('Invalid command. Reopen Empire before continuing.');setUncertain(true);}
    });
    post('empire.ready');
    const media=matchMedia('(prefers-reduced-motion: reduce)'),change=()=>setLowMotion(media.matches);
    media.addEventListener('change',change);
    return()=>{stop();media.removeEventListener('change',change);clearTimeout(previewTimer.current);};
  },[clearPreview]);
  useEffect(()=>{
    if(snapshot&&rendererReady)post('empire.ui-ready');
  },[!!snapshot,rendererReady]);
  useEffect(()=>{
    if(!pending)return;
    const timer=setTimeout(()=>{setUncertain(true);post('empire.refresh');},8000);
    return()=>clearTimeout(timer);
  },[pending]);
  useEffect(()=>{
    const key=(e:KeyboardEvent)=>{
      if(e.key!=='Escape'||confirmation)return;
      e.preventDefault();clearPreview();if(page!=='estate')setPage('estate');else close();
    };document.addEventListener('keydown',key);return()=>document.removeEventListener('keydown',key);
  },[page,confirmation,clearPreview]);
  useEffect(()=>{document.documentElement.lang=snapshot?.state.language??'en';},[snapshot?.state.language]);
  const act=(intent:Intent)=>{
    if(!live.current||pendingRef.current||uncertain)return;
    clearPreview();const id=send(live.current,intent);pendingRef.current=id;setPending(id);setNotice(null);
  };
  const showPreview=(intent:Intent)=>{
    if(!live.current||pendingRef.current||uncertain)return;
    clearPreview();previewTimer.current=setTimeout(()=>{
      if(live.current&&!pendingRef.current)previewId.current=send(live.current,{...intent,preview:true,roomId:intent.roomId??selected??'slots'});
    },120);
  };
  if(!snapshot)return <main className="empire-loading"><span>♜</span><h1>CASINO EMPIRE</h1><p>{window.chrome?.webview?'Opening your estate…':'Open Casino Empire in the desktop application.'}</p><button onClick={()=>post('empire.close')}>← Casino lobby</button></main>;
  const {state}=snapshot,e=state.economy,de=state.language==='de',language=state.language;
  const t=(source:string):string=>{
    if(!de)return source;if(dictionary[source])return dictionary[source];
    return source.split(' · ').map(part=>{
      if(dictionary[part])return dictionary[part];
      return part.replace(/^Empire Prestige /,'Empire-Prestige ').replace(/^Reputation /,'Ruf ')
        .replace(/^Project: (.+)$/,(_,name:string)=>'Projekt: '+(dictionary[name]??name))
        .replace(/^(\d+) estate levels$/,'$1 Anwesenstufen')
        .replace(/^(.+) departments at level (\d+)$/,(_,tags:string,level:string)=>tags.split(', ').map(s=>dictionary[s]??s).join(', ')+'-Abteilungen auf Stufe '+level);
    }).join(' · ');
  };
  const n=(v:string)=>display(v,language),money=(v:string)=>'$'+display(v,language,false);
  const disabled=!!pending||uncertain;
  const room=e.rooms.find(r=>r.id===selected),quote=room?.quotes.find(q=>q.requestedLevels===bulk);
  const property=e.properties.find(p=>p.id===e.activePropertyId)!;
  const currentPage=pages.find(p=>p.id===page)!;
  const enabled=state.animationsEnabled&&motion&&!lowMotion&&visible;
  const previewBox=preview&&<aside className="empire-preview" role="status"><span className="empire-eyebrow">{de?'VORSCHAU · NICHT GESPEICHERT':'PREVIEW · NOT SAVED'}</span>
    {!preview.success?<p>{t(preview.message)}</p>:<><div>{de?'Budget':'Budget'}<strong>{n(preview.budgetBefore)} → {n(preview.budgetAfter)} cr</strong></div>
      <div>{de?'Portfolio / Sekunde':'Portfolio / second'}<strong>{n(preview.portfolioBefore)} → {n(preview.portfolioAfter)} cr/s</strong></div>
      <div>{de?'Raumstufe':'Room level'}<strong>{preview.levelBefore} → {preview.levelAfter}</strong></div>
      <div>{de?'Raumproduktion':'Room production'}<strong>{n(preview.roomBefore.final)} → {n(preview.roomAfter.final)} cr/s</strong></div>
      <details><summary>{de?'Synergien nach der Aktion':'Circuits after action'}</summary>{preview.synergiesAfter.map(s=><p key={s.id}>{t(e.synergies.find(y=>y.id===s.id)?.name??s.id)}: {s.tier} · {Number(s.bonus)*100}%</p>)}</details></>}
    <button onClick={clearPreview}>{de?'Vorschau schließen':'Close preview'}</button></aside>;
  return <div className="empire-shell" data-motion={enabled} data-page={page} data-testid="empire-ready">
    <header className="empire-header"><button className="empire-back" data-testid="back" onClick={close}>← <span>{state.detached?(de?'Fenster schließen':'Close window'):'Casino lobby'}</span></button>
      <div className="empire-wordmark"><span>THE MIDNIGHT CLUB</span><h1>CASINO EMPIRE</h1></div>
      <label className="empire-property"><span>{de?'ANWESEN':'PROPERTY'}</span><select aria-label={de?'Anwesen':'Property'} value={e.activePropertyId} disabled={disabled} onChange={event=>{setSelected('slots');clearPreview();act({action:'property.select',id:event.target.value});}}>
        {e.properties.filter(p=>p.owned).map(p=><option key={p.id} value={p.id}>{p.name}</option>)}</select></label>
    </header>
    <aside className="empire-sidebar"><nav aria-label={de?'Empire-Bereiche':'Empire sections'}>{pages.map((p,i)=><div key={p.id}>
      {(i===0||pages[i-1].group!==p.group)&&<span className="empire-nav-group">{de?groupDe[p.group]:p.group}</span>}
      <button data-testid={'nav-'+p.id} aria-current={page===p.id?'page':undefined} onClick={()=>go(p.id)}><i aria-hidden="true">{p.icon}</i>{de?p.de:p.en}</button></div>)}</nav>
      <div className="empire-sidebar-foot"><b>{de?'Prestige':'Prestige'} {e.prestige}</b><span>{e.reputation.toLocaleString(language)} {de?'Ruf':'reputation'}</span>
        <label className="empire-check"><input type="checkbox" checked={motion} onChange={event=>setMotion(event.target.checked)}/>{de?'Atmosphäre':'Atmosphere'}</label>
        <label className="empire-check"><input type="checkbox" checked={diagnostics} onChange={event=>setDiagnostics(event.target.checked)}/>{de?'Bilddiagnose':'Frame diagnostics'}</label></div>
    </aside>
    <main className="empire-main" aria-busy={!!pending}>
      <section className="empire-metrics" aria-label={de?'Wirtschaft':'Economy'}>
        <div><span>{de?'BAUKAPITAL':'CONSTRUCTION CAPITAL'}</span><strong title={e.credits}>{n(e.credits)} <small>cr</small></strong></div>
        <div><span>{de?'ANWESEN / SEK.':'ESTATE / SEC'}</span><strong title={e.rate}>+{n(e.rate)} <small>cr/s</small></strong></div>
        <div><span>{de?'PORTFOLIO / SEK.':'PORTFOLIO / SEC'}</span><strong title={e.portfolioRate}>{n(e.portfolioRate)} <small>cr/s</small></strong></div>
        <div className="empire-dividend"><span>{de?'INHABERRESERVE':'OWNER RESERVE'}</span><strong>{money(e.dividend)} <small>/ {money(e.dividendCapacity)}</small></strong>
          <button className="empire-gold" data-testid="claim-dividend" disabled={disabled||!state.canClaimDividend||Number(e.dividend)<.01} onClick={()=>act({action:'dividend'})}>{de?'Abholen':'Collect'}</button>
          {!state.canClaimDividend&&<small>{de?'Zuerst das aktive Spiel beenden.':'Finish the active game first.'}</small>}</div>
      </section>
      {uncertain&&<div className="empire-notice" role="alert">{de?'Die Bestätigung fehlt. Empire schließen und neu öffnen, um den gespeicherten Stand zu prüfen.':'Confirmation is unavailable. Close and reopen Empire to review the saved state.'}</div>}
      {(notice||snapshot.notice)&&<div className="empire-notice" role="status">{t(notice??snapshot.notice??'')}</div>}
      {snapshot.offline&&<section className="empire-offline"><div><span className="empire-eyebrow">{de?'WILLKOMMEN ZURÜCK':'WELCOME BACK'}</span><h2>{de?'Dein Empire hat weitergearbeitet.':'Your empire kept working.'}</h2>
        <p>{n(snapshot.offline.credits)} cr · {money(snapshot.offline.dividend)} · {Math.round(snapshot.offline.seconds/60)} min {snapshot.offline.capped?(de?'(Speichergrenze erreicht)':'(storage cap reached)'):''}</p>
        <details><summary>{de?'Ertrag nach Räumen und Immobilien':'Room and property earnings'}</summary>{Object.entries(snapshot.offline.rooms).map(([id,v])=><p key={id}>{t(e.rooms.find(r=>r.id===id)?.name??id)}: {n(v)} cr</p>)}{Object.entries(snapshot.offline.properties).map(([id,v])=><p key={id}>{e.properties.find(p=>p.id===id)?.name??id}: {n(v)} cr</p>)}</details></div>
        <button onClick={()=>post('empire.dismiss-offline')}>{de?'Weiter':'Continue'} →</button></section>}
      <div className="empire-section-heading"><div><span className="empire-eyebrow">{property.name} · {de?groupDe[currentPage.group]:currentPage.group}</span><h2>{de?currentPage.de:currentPage.en}</h2></div>
        <span>{e.openRooms}/15 {de?'Räume':'rooms'} · {e.totalLevels} {de?'Stufen':'levels'}</span></div>
      {/* Keep the sole Pixi application mounted across all management routes. */}
      <section className="empire-estate" hidden={page!=='estate'}>
        <div className="empire-scene-frame"><EmpireScene roomId={room?.id??'slots'} enabled={enabled&&page==='estate'} diagnostics={diagnostics} onReady={sceneReady}/>
          <div className="empire-scene-title"><span className="empire-eyebrow">{room?t(room.category):de?'DEIN WAHRZEICHEN':'YOUR MIDNIGHT LANDMARK'}</span><h2>{room?t(room.name):property.name}</h2><p>{room?t(room.description):t(property.description)}</p></div>
          <span className="empire-scene-badge">{de?'PRESTIGE':'PRESTIGE'} {e.prestige}</span></div>
        <div className="empire-floorbar"><div role="group" aria-label={de?'Etagen':'Floors'}>{e.floors.map(f=><button key={f.id} data-testid={'floor-'+f.id} aria-pressed={floor===f.id} title={t(f.requirement)} onClick={()=>{setFloor(f.id);clearPreview();}}>{f.unlocked?'':'◆ '}{t(f.displayName)}</button>)}</div>
          <label className="empire-filter"><span>{de?'Zeigen':'Show'}</span><select value={filter} onChange={event=>{setFilter(event.target.value);clearPreview();}} aria-label={de?'Raumfilter':'Room filter'}>
            <option value="all">{de?'Alle Räume':'All rooms'}</option><option value="open">{de?'In Betrieb':'Operating'}</option><option value="locked">{de?'Noch geschlossen':'Not operating'}</option></select></label>
        </div>
        {!e.floors.find(f=>f.id===floor)?.unlocked&&<p className="empire-lock">◆ {t(e.floors.find(f=>f.id===floor)?.requirement??'')}</p>}
        <div className="empire-workspace"><div><div className="empire-gallery">{e.rooms.filter(r=>r.floor===floor&&(filter==='all'||(filter==='open'?r.level>0:r.level===0))).map(r=><RoomCard key={r.id} room={r} selected={selected===r.id} onSelect={selectRoom} language={language}/>)}</div>
          <button className="empire-overview" data-testid="overview" onClick={()=>{setSelected(null);clearPreview();}}>{de?'Anwesenübersicht':'Estate overview'} ↗</button>
          {room&&<section className="empire-panel empire-intelligence"><h2>{de?'Raumwissen':'Room intelligence'}</h2><p>{room.id} · {t(room.tags)} · {t(room.requirement)}</p>
            <div className="empire-breakdown">{Object.entries(room.breakdown).map(([key,value])=><div key={key}><span>{t(key[0].toUpperCase()+key.slice(1))}</span><strong>{key==='base'||key==='final'?n(value)+' cr/s':key==='prestige'||key==='achievements'?'×'+value:'+'+display(String(Number(value)*100),language)+'%'}</strong></div>)}</div>
            <h3>{de?'Aktive Spezialisten':'Active specialists'}</h3>{e.managers.filter(m=>m.assignment!==null).map(m=><p key={m.id}>{m.name} → {t(e.rooms.find(r=>r.id===m.assignment)?.name??'')} · {t(m.description)}</p>)}
            <button onClick={()=>go('team')}>{de?'Team verwalten':'Manage team'}</button><button onClick={()=>go('synergies')}>{de?'Synergien prüfen':'Inspect circuits'}</button>
          </section>}
        </div>
        <aside className="empire-inspector empire-panel">{room&&quote?<><span className="empire-eyebrow">{de?'RAUMINSPEKTOR':'ROOM INSPECTOR'}</span><h2>{t(room.name)}</h2><p>{t(room.description)}</p>
          <div className="empire-inspector-numbers"><div><span>{de?'Stufe':'Level'}</span><strong>{room.level}<small> / 200</small></strong></div><div><span>{de?'Produktion':'Production'}</span><strong>{n(room.rate)}<small> cr/s</small></strong></div></div>
          <div className="empire-bulk" role="group" aria-label={de?'Upgrade-Menge':'Upgrade quantity'}>{room.quotes.map(q=><button key={q.requestedLevels} aria-pressed={bulk===q.requestedLevels} onClick={()=>{setBulk(q.requestedLevels);clearPreview();}} onPointerEnter={()=>showPreview({action:'upgrade',id:room.id,levels:q.requestedLevels})} onPointerLeave={clearPreview} onFocus={()=>showPreview({action:'upgrade',id:room.id,levels:q.requestedLevels})} onBlur={clearPreview}>×{q.requestedLevels}</button>)}</div>
          <p>{de?'Nächster Meilenstein':'Next milestone'}: {room.level<200?Math.min(200,(Math.floor(room.level/10)+1)*10):'MAX'}</p>
          <button className="empire-gold empire-upgrade" data-testid="upgrade" disabled={disabled||!quote.canBuy} onClick={()=>act({action:'upgrade',id:room.id,levels:bulk})}
            onPointerEnter={()=>showPreview({action:'upgrade',id:room.id,levels:bulk})} onPointerLeave={clearPreview} onFocus={()=>showPreview({action:'upgrade',id:room.id,levels:bulk})} onBlur={clearPreview}>
            <span>{de?'Ausbauen':'Upgrade'} +{quote.levels}</span><strong>{n(quote.cost)} cr</strong></button>
          {!quote.unlocked&&<p className="empire-lock">◆ {t(room.requirement)}</p>}{room.level===200&&<p>{de?'Maximale Stufe erreicht.':'Maximum level reached.'}</p>}
          <button onClick={()=>showPreview({action:'upgrade',id:room.id,levels:bulk})} disabled={disabled}>{de?'Vorschau öffnen':'Open preview'}</button>
        </>:<><span className="empire-eyebrow">{de?'ANWESENÜBERSICHT':'ESTATE OVERVIEW'}</span><h2>{property.name}</h2><p>{t(property.description)}</p><p>{e.totalLevels} {de?'Stufen':'levels'} · {e.reputation} {de?'Ruf':'reputation'}</p>
          <h3>{de?'Das nächste Kapitel':'Your next chapter'}</h3><p>{de?'Alle fünf Räume der Hauptetage in Betrieb.':'All five Main Floor rooms operating.'} {e.mainFloorLevels}/{e.prestigeMainFloorLevelRequirement} {de?'Hauptetagenstufen':'Main Floor levels'} · {n(e.statistics.runCredits)}/{n(e.prestigeRunCreditRequirement)} cr {de?'im Durchlauf produziert':'produced this run'}.</p><button className="empire-gold" data-testid="prestige" disabled={disabled||!e.canPrestige} onClick={()=>setConfirmation(true)}>{de?'Prestige aufsteigen':'Advance prestige'}</button>
          <p>{de?'Baukapital, Raumstufen und das aktive Event werden zurückgesetzt. Permanente Projekte und Forschung bleiben erhalten.':'Construction capital, room levels and the active event reset. Permanent projects and research are retained.'}</p>
          <button onClick={()=>go('research')}>{e.legacyPointsAvailable} {de?'Forschungspunkte verfügbar':'Legacy Points available'}</button></>}
        </aside></div>
      </section>
      {page==='research'&&<><div className="empire-tabs">{['Operations','Gaming','Hospitality'].map(branch=><button key={branch} aria-pressed={researchBranch===branch} onClick={()=>{setResearchBranch(branch);clearPreview();}}>{t(branch)}</button>)}<span>{e.legacyPointsAvailable} {de?'Forschungspunkte':'Legacy Points'}</span></div>
        <div className="empire-catalog">{e.research.filter(r=>r.branch===researchBranch).map(r=><article key={r.id} className="empire-panel" data-prerequisite={r.prerequisite??''}><span className="empire-eyebrow">{t(r.branch)} · {r.rank}/{r.maxRank}</span><h2>{t(r.name)}</h2><p>{t(r.description)}</p>
          {r.prerequisite&&<p className="empire-prerequisite">↳ {t(e.research.find(n=>n.id===r.prerequisite)?.name??'')} · {de?'Rang':'rank'} {r.prerequisiteRank}</p>}
          <button className="empire-gold" data-testid={'research-'+r.id} disabled={disabled||!r.canBuy} onClick={()=>act({action:'research',id:r.id})}>{de?'Erforschen':'Research'} · {r.costPerRank} LP</button></article>)}</div></>}
      {(page==='projects'||page==='properties')&&<div className="empire-catalog">{(page==='projects'?e.projects:e.properties).map(p=><article key={p.id} className="empire-panel"><span className="empire-eyebrow">{p.owned?(de?'IM BESITZ':'OWNED'):de?'ERWEITERUNG':'EXPANSION'}</span><h2>{t(p.name)}</h2><p>{t(p.description)}</p><p>{t(p.requirement)}</p><strong title={p.cost}>{n(p.cost)} cr</strong>
        {page==='properties'&&'state' in p&&(p as typeof property).state&&<p>{n((p as typeof property).state!.credits)} cr · Prestige {(p as typeof property).state!.prestige}</p>}
        <div className="empire-actions"><button disabled={disabled||!!p.owned} onClick={()=>showPreview({action:page==='projects'?'project':'property.buy',id:p.id})}>{de?'Vorschau':'Preview'}</button>
          <button className="empire-gold" data-testid={(page==='projects'?'project-':'property-')+p.id} disabled={disabled||(page==='properties'&&p.owned?p.id===e.activePropertyId:!p.canBuy)} onClick={()=>act({action:page==='projects'?'project':p.owned?'property.select':'property.buy',id:p.id})}>{p.owned?(page==='projects'?'✓':de?'Verwalten':'Manage'):de?'Kaufen':'Purchase'}</button></div></article>)}</div>}
      {page==='synergies'&&<div className="empire-catalog">{e.synergies.map(s=><article key={s.id} className="empire-panel"><span className="empire-eyebrow">{de?'SYNERGIESTUFE':'CIRCUIT TIER'} {s.tier}</span><h2>{t(s.name)}</h2><p>{t(s.description)}</p><strong>+{Number(s.bonus)*100}%</strong><p>{de?'Nächste Raumstufe':'Next room level'}: {s.nextLevel??'MAX'}</p>
        <ul>{s.members.map(m=><li key={m.id}><button onClick={()=>{setFloor(e.rooms.find(r=>r.id===m.id)!.floor);setSelected(m.id);go('estate');}}>{t(m.name)} <b>{m.level}</b>{m.level===s.minimumLevel?' ◇':''}</button></li>)}</ul></article>)}</div>}
      {page==='team'&&<><p>{Object.keys(e.managerAssignments).length}/{e.managerSlots} {de?'Managerplätze belegt':'manager slots occupied'}</p><div className="empire-catalog">{e.managers.map(m=><ManagerCard key={e.activePropertyId+m.id+(m.assignment??'')} manager={m} rooms={e.rooms} disabled={disabled} language={language} onAction={(id,roomId)=>act({action:'manager',id,roomId})} onPreview={(id,roomId)=>showPreview({action:'manager',id,roomId})}/>)}</div></>}
      {page==='automation'&&<AutomationForm key={e.activePropertyId} initial={e.automation} unlocked={e.automationUnlocked} disabled={disabled} language={language} onSave={automation=>act({action:'automation',automation})} onPreview={automation=>showPreview({action:'automation',automation})} onClear={clearPreview}/>}
      {page==='events'&&<><p>{de?'Ein Event je Anwesen. Freiwillige Spielziele verlängern es, ohne Spielregeln zu verändern.':'One event per property. Optional game goals extend it without changing game rules.'}</p>
        {e.activeEvent&&<aside className="empire-notice">{t(e.events.find(v=>v.id===e.activeEvent?.id)?.name??'')} · {e.activeEvent.progress}/{e.events.find(v=>v.id===e.activeEvent?.id)?.activityGoal} · {de?'Endet':'Ends'} {new Date(e.activeEvent.endsAt).toLocaleString(language)} {e.activeEvent.encore?'✦ Encore':''}</aside>}
        <div className="empire-catalog">{e.events.map(v=><article key={v.id} className="empire-panel"><span className="empire-eyebrow">{v.durationMinutes} min</span><h2>{t(v.name)}</h2><p>{t(v.description)}</p><p>{t(v.requirement)}</p><button className="empire-gold" data-testid={'event-'+v.id} disabled={disabled||!v.canBuy} onClick={()=>act({action:'event',id:v.id})}>{de?'Starten':'Host event'} · {n(v.cost)} cr</button></article>)}</div></>}
      {page==='contracts'&&<>{e.contractBuff&&<p className="empire-notice">{t(e.contracts.find(c=>c.id===e.contractBuff?.contractId)?.rewardDescription??'')} · {Math.ceil(Number(e.contractBuff.remainingSeconds)/60)} min</p>}
        <div className="empire-catalog">{e.contracts.map(c=><article key={c.id} className="empire-panel"><span className="empire-eyebrow">{de?'OHNE FRIST':'NO DEADLINE'}</span><h2>{t(c.name)}</h2><p>{t(c.description)}</p><p>{t(c.rewardDescription)}</p>
          {e.activeContract===c.id?<><p>{n(e.contractProgress)} / {n(c.goal)}</p><progress value={Number(e.contractProgress)} max={Number(c.goal)} aria-label={t(c.name)}/><button className="empire-gold" data-testid="contract-claim" disabled={disabled||!e.canClaimContract} onClick={()=>act({action:'contract.claim'})}>{de?'Abschließen':'Complete contract'}</button></>:<button data-testid={'contract-'+c.id} disabled={disabled||!!e.activeContract} onClick={()=>act({action:'contract.accept',id:c.id})}>{de?'Annehmen':'Accept brief'}</button>}</article>)}</div></>}
      {page==='statistics'&&<div className="empire-catalog"><article className="empire-panel"><h2>{de?'Portfolio & Produktion':'Portfolio & production'}</h2><dl className="empire-stat-list">{[
        [de?'Lebenszeit-Credits':'Lifetime credits',n(e.statistics.lifetimeCredits)], [de?'Anwesen-Lebenszeit':'Property lifetime',n(e.statistics.propertyLifetimeCredits)],
        [de?'Aktueller Durchlauf':'Current run',n(e.statistics.runCredits)],[de?'Ausgezahlte Dividenden':'Dividends collected',money(e.statistics.lifetimeCashClaimed)],
        [de?'Gekaufte Stufen':'Levels purchased',n(e.statistics.levelsPurchased)],[de?'Automatisierte Stufen':'Automated levels',n(e.statistics.automatedLevels)],
        [de?'Abgeschlossene Events':'Completed events',e.statistics.eventsCompleted],[de?'Zugaben':'Encores',e.statistics.eventEncores],
        [de?'Abgeschlossene Verträge':'Completed contracts',e.statistics.contractsCompleted],[de?'Offline-Speicher':'Offline capacity',e.offlineHours+' h'],
        [de?'Dividende pro Stunde':'Dividend per hour',money(e.cashPerHour)],
      ].map(([label,value])=><div key={label}><dt>{label}</dt><dd>{value}</dd></div>)}</dl></article>
        <article className="empire-panel"><h2>{de?'Produktion nach Tags':'Production by tag'}</h2><dl className="empire-stat-list">{Object.entries(e.statistics.producedByTag).map(([tag,value])=><div key={tag}><dt>{t(tag)}</dt><dd>{n(value)} cr</dd></div>)}</dl><h3>{de?'Aktive Erfolgsboni':'Active achievement boosts'}</h3>{e.achievementBoosts.map((b,i)=><p key={i}>{t(b.name)} +{Number(b.percent)*100}% {b.roomId?t(e.rooms.find(r=>r.id===b.roomId)?.name??''):''}</p>)}</article>
        <article className="empire-panel"><h2>{de?'Etagen & Immobilien':'Floors & properties'}</h2>{e.floors.map(f=><p key={f.id}>{t(f.displayName)} · {e.rooms.filter(r=>r.floor===f.id&&r.level>0).length}/5</p>)}{e.properties.filter(p=>p.owned).map(p=><p key={p.id}>{p.name} · Prestige {p.state?.prestige} · {p.state?.totalLevels} {de?'Stufen':'levels'}</p>)}</article></div>}
      {previewBox}
    </main>
    {confirmation&&<Confirm label={de?'Prestige bestätigen':'Confirm prestige'} onCancel={()=>setConfirmation(false)} onConfirm={()=>{setConfirmation(false);act({action:'prestige'});}}><p>{de?'Nur dieses Anwesen startet neu. Baukapital, Raumstufen und das aktive Event werden zurückgesetzt.':'Only this estate starts again. Construction capital, room levels and the active event reset.'}</p></Confirm>}
  </div>;
}
createRoot(document.getElementById('root')!).render(<EmpireApp/>);
