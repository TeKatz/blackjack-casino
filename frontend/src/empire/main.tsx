import {useCallback,useEffect,useRef,useState} from 'react';
import {createRoot} from 'react-dom/client';
import {finishModeBoot} from '../boot/finishModeBoot';
import {fmt,listen,post,send} from './empireBridge';
import type {Building,Command,EmpireSnapshot} from './empireBridge';
import CasinoStackArt from './CasinoStackArt';
import BuildingArtwork from './BuildingArtwork';
import {EmpireLegacyPanel} from './EmpireLegacyPanel';
import './empire.css';

const names:Record<string,string>={
  cashier:'Casino-Kasse',slots:'Automatenhalle',tables:'Kartensalon',bar:'Midnight Bar',
  roulette:'Roulette-Galerie',poker:'Poker Club',stage:'Grand Stage',vip:'VIP Lounge',
  'high-limit':'High Limit Floor',hotel:'Grand Hotel',vault:'Casino Vault',penthouse:'Penthouse Club',
};
const descriptions:Record<string,string>={
  cashier:'Verbessert die Einnahmen pro Klick.',
  slots:'Die ersten Automaten sorgen für passives Einkommen.',
  tables:'Kartenspiele stärken Klicks und passives Einkommen.',
  bar:'Mehr Gäste und ein größerer laufender Ertrag.',
  roulette:'Die Spieltische verstärken die Automatenhalle.',
  poker:'Verbessert die Karten- und Poker-Einnahmen.',
  stage:'Die Bühne verstärkt die Bar und den Casino-Betrieb.',
  vip:'Exklusive Gäste verstärken deine Klicks.',
  'high-limit':'Hohe Einsätze eröffnen neue Einnahmequellen.',
  hotel:'Das Hotel stärkt die Produktion deines Casinos.',
  vault:'Ein großer Tresor verbessert deine Klickwerte.',
  penthouse:'Das Penthouse ist die Krönung deines Casinos.',
};
function localizedBuilding(building:Building,de:boolean){return de?(names[building.id]??building.name):building.name;}
function BuildingTile({building,de,active,disabled,onSelect,onUpgrade}:{
  building:Building;de:boolean;active:boolean;disabled:boolean;onSelect:()=>void;onUpgrade:()=>void;
}){
  const title=localizedBuilding(building,de);
  const visualNext=building.level<8&&[2,4,7].includes(building.level);
  const maxed=building.level===building.maxLevel;
  return <article className={'ce-building ce-tier-'+building.visualTier+
    (active?' is-expanded':'')+(!building.unlocked?' is-locked':'')+
    (building.canBuy?' is-affordable':'')}>
    <button className="ce-building-trigger" type="button" aria-expanded={active}
      aria-label={title+' · '+(building.unlocked?(de?'Stufe ':'Level ')+building.level+'/8':(de?'Gesperrt':'Locked'))}
      onClick={onSelect} title={building.unlocked?title:building.requirement}>
      <BuildingArtwork id={building.id} tier={building.visualTier}/>
      <span className="ce-building-label">
        <strong>{title}</strong>
        <span className="ce-building-subtitle">{building.unlocked?
          (de?'Stufe ':'Level ')+building.level+' / 8':(de?'Noch gesperrt':'Locked')}</span>
        <span className="ce-level-track"><i style={{width:building.level*12.5+'%'}}/></span>
      </span>
      <span className="ce-building-toggle" aria-hidden="true">{active?'−':building.unlocked?'+':'⌑'}</span>
    </button>
    <div className="ce-building-details" data-open={active}>
      <div className="ce-building-details-inner">
        <p className="ce-building-description">{de?descriptions[building.id]:building.description}</p>
        {building.unlocked?
          <>
            <div className="ce-building-stat"><span>{de?'Laufender Ertrag':'Passive income'}</span>
              <b>+{fmt(building.rate,de?'de':'en')} cr/s</b></div>
            {building.nextUnlock&&<p className="ce-unlock-hint">✦ {de?'Nächste Stufe schaltet frei: ':'Next level unlocks: '}{building.nextUnlock}</p>}
            {visualNext&&<p className="ce-unlock-hint">✧ {de?'Nächster Ausbau: neue Optik':'Next level: visual evolution'}</p>}
            <button className="ce-upgrade" type="button" disabled={disabled||!building.canBuy}
              onClick={onUpgrade}><span aria-hidden="true">♦</span>
              {maxed?(de?'Vollständig ausgebaut':'Fully upgraded'):
                (de?'Ausbauen':'Upgrade')+' · '+fmt(building.cost,de?'de':'en')+' cr'}</button>
          </>:
          <p className="ce-unlock-hint ce-locked-requirement">⌑ {de?'Freischalten durch: ':'Unlock by: '}{building.requirement}</p>}
      </div>
    </div>
  </article>;
}

function EmpireApp(){
  const [snapshot,setSnapshot]=useState<EmpireSnapshot|null>(null);
  const [notice,setNotice]=useState<string|null>(null);
  const [selected,setSelected]=useState<string|null>(null);
  const [buildingsOpen,setBuildingsOpen]=useState(true);
  const [prestigeOpen,setPrestigeOpen]=useState(false);
  const [legacyOpen,setLegacyOpen]=useState(false);
  const [compactLegacy,setCompactLegacy]=useState(matchMedia('(max-width: 1220px)').matches);
  const [confirm,setConfirm]=useState(false);
  const confirmRef=useRef(false);confirmRef.current=confirm;
  const [pulse,setPulse]=useState(0);
  const [visible,setVisible]=useState(true);
  const [activeVisitId,setActiveVisitId]=useState<number|null>(null);
  const [pending,setPending]=useState(false);
  const [lowMotion,setLowMotion]=useState(matchMedia('(prefers-reduced-motion: reduce)').matches);
  const live=useRef<EmpireSnapshot|null>(null);
  const activeVisit=useRef<number|null>(null);
  const pendingId=useRef<string|null>(null);
  const queuedTaps=useRef(0);
  const sentAt=useRef(0);
  const visibleRef=useRef(true);
  live.current=snapshot;

  const ready=()=>{
    const current=live.current;
    return visibleRef.current&&current!==null&&current.visitId!==undefined
      &&activeVisit.current===current.visitId;
  };
  const command=useCallback((action:Command)=>{
    if(!ready()||pendingId.current)return false;
    const id=send(live.current!,action);
    pendingId.current=id;sentAt.current=Date.now();setPending(true);
    return true;
  },[]);

  useEffect(()=>{
    const stop=listen(message=>{
      if(message.type==='snapshot'){
        if(message.data.lastReply?.requestId===pendingId.current){
          pendingId.current=null;setPending(false);
        }
        setSnapshot(previous=>previous&&previous.session===message.data.session
          &&message.data.revision<previous.revision?previous:message.data);
      }else if(message.type==='result'){
        if(message.data.requestId===pendingId.current){
          pendingId.current=null;setPending(false);
          if(!message.data.success)setNotice(message.data.message);
        }
      }else if(message.type==='activated'){activeVisit.current=message.visitId;setActiveVisitId(message.visitId);}
      else if(message.type==='visibility'){
        visibleRef.current=message.visible;setVisible(message.visible);
        if(!message.visible){activeVisit.current=null;setActiveVisitId(null);queuedTaps.current=0;}
      }else if(message.type==='escape'){if(confirmRef.current)setConfirm(false);else post('empire.close');}
      else if(message.type==='invalid')setNotice('Empire command rejected. Reopen the game.');
    });
    post('empire.ready');
    const compact=matchMedia('(max-width: 1220px)');
    const updateCompact=()=>setCompactLegacy(compact.matches);
    compact.addEventListener('change',updateCompact);
    const media=matchMedia('(prefers-reduced-motion: reduce)');
    const update=()=>setLowMotion(media.matches);
    media.addEventListener('change',update);
    return()=>{stop();media.removeEventListener('change',update);
      compact.removeEventListener('change',updateCompact);};
  },[]);

  useEffect(()=>{
    if(snapshot?.visitId!==undefined){
      finishModeBoot();
      post('empire.ui-ready',{visitId:snapshot.visitId});
    }
  },[snapshot?.visitId]);

  useEffect(()=>{
    const timer=window.setInterval(()=>{
      if(pendingId.current&&Date.now()-sentAt.current>8000){
        setNotice('Connection delayed. Refreshing the saved state.');
        post('empire.refresh');
      }
      if(queuedTaps.current<1||pendingId.current||!ready())return;
      const count=Math.min(20,queuedTaps.current);
      if(command({action:'tap',count}))queuedTaps.current-=count;
    },350);
    return()=>window.clearInterval(timer);
  },[command]);

  const click=()=>{
    if(!ready())return;
    queuedTaps.current=Math.min(200,queuedTaps.current+1);
    setPulse(v=>v+1);
  };
  if(!snapshot)return <main className="ce-loading"><h1>CASINO EMPIRE</h1>
    <p>Opening your casino…</p><button onClick={()=>post('empire.close')}>← Lobby</button></main>;

  const {state}=snapshot;
  const e=state.economy,de=state.language==='de',language=state.language;
  const legacyUnlocked=e.legacy.unlocked;
  const shown=e.buildings.filter(b=>b.visible);
  const disabled=!visible||pending||activeVisitId!==snapshot.visitId||!ready();
  const goal=e.goal;
  const name=(id:string|null)=>id?(de?names[id]??id:e.buildings.find(b=>b.id===id)?.name??id):'';
  const goalTitle=goal.id?(de?'Nächster Ausbau: ':'Next upgrade: ')+name(goal.id):
    (de?'Alle Gebäude ausgebaut':'All buildings upgraded');
  const goalBuilding=e.buildings.find(b=>b.id===goal.id);
  const goalReward=goalBuilding?.nextUnlock
    ? (de?'Schaltet frei: ':'Unlocks: ')+(de?names[e.buildings.find(b=>b.name===goalBuilding.nextUnlock)?.id??'']??goalBuilding.nextUnlock:goalBuilding.nextUnlock)
    :de?'Mehr Leistung und sichtbarer Fortschritt':'More production and visible progress';

  return <div className={'ce-shell'+(legacyUnlocked?' ce-has-legacy':'')}
    data-motion={state.animationsEnabled&&!lowMotion}>
    <div className="ce-scenery" aria-hidden="true"/>
    <header className="ce-header">
      <div className="ce-header-inner">
        <div className="ce-header-left">
          <button className="ce-back" type="button" onClick={()=>post('empire.close')}>
            <span className="ce-back-arrow" aria-hidden="true">←</span> Lobby
          </button>
          {legacyUnlocked&&<button className="ce-legacy-launch" type="button"
            aria-expanded={legacyOpen} aria-controls="empire-legacy"
            onClick={()=>setLegacyOpen(v=>!v)}>
            ✧ {de?'Vermächtnis':'Legacy'} <span aria-hidden="true">{legacyOpen?'−':'+'}</span>
          </button>}
        </div>
        <div className="ce-wordmark">
          <span className="ce-wordmark-insignia" aria-hidden="true">♠</span>
          <small>THE MIDNIGHT CLUB</small>
          <h1>CASINO <em>EMPIRE</em></h1>
          <span className="ce-wordmark-rule" aria-hidden="true"/>
        </div>
        <div className="ce-prestige-badge" title={de?'Prestige-Stufe':'Prestige level'}>
          <span aria-hidden="true">✧</span> P{e.prestige}
        </div>
      </div>
    </header>

    {legacyUnlocked&&compactLegacy&&legacyOpen&&
      <button type="button" className="ce-legacy-scrim"
        onClick={()=>setLegacyOpen(false)} aria-label={de?'Vermächtnis schließen':'Close legacy'}/>}
    <main className={'ce-main'+(legacyUnlocked?' ce-main-with-legacy':'')}>
      {legacyUnlocked&&<aside id="empire-legacy" className="ce-progress-rail"
        data-open={legacyOpen} inert={compactLegacy&&!legacyOpen}
        aria-label={de?'Dauerhafter Prestige-Fortschritt':'Permanent prestige progression'}>
        <EmpireLegacyPanel legacy={e.legacy} prestige={e.prestige} de={de}
          disabled={disabled} buy={id=>command({action:'legacy.buy',id})}/>
      </aside>}
      <section className="ce-primary" aria-label={de?'Casino-Spielbereich':'Casino play area'}>
        <section className="ce-stage-panel" aria-label={de?'Chips stapeln':'Stack chips'}>
          <div className="ce-balance">
            <span className="ce-balance-title">{de?'CASINO-VERMÖGEN':'CASINO BALANCE'}</span>
            <strong title={e.credits}>{fmt(e.credits,language)} <small>cr</small></strong>
            <p><span className="ce-rate">+{fmt(e.rate,language)} cr/s</span>
              <span className="ce-rate-separator">·</span>
              <span>{de?'Klick: ':'Click: '}+{fmt(e.tapValue,language)} cr</span>
            </p>
          </div>
          <div className="ce-stack-zone">
            <span className="ce-stage-sheen" aria-hidden="true"/>
            <span className="ce-stage-mote ce-mote-one" aria-hidden="true"/>
            <span className="ce-stage-mote ce-mote-two" aria-hidden="true"/>
            <span className="ce-stage-mote ce-mote-three" aria-hidden="true"/>
            <button className="ce-stack-button" type="button" onClick={click} disabled={!ready()}
              aria-label={de?'Chips stapeln und Credits verdienen':'Stack chips to earn credits'}>
              <CasinoStackArt levels={e.totalLevels} clicks={e.clicks}/>
              {pulse>0&&<span key={pulse} className="ce-click-pop" aria-hidden="true">+{fmt(e.tapValue,language)}</span>}
              {pulse>0&&<span key={'r'+pulse} className="ce-click-ripple" aria-hidden="true"/>}
              <span className="ce-stack-hint"><span aria-hidden="true">♠</span>
                {de?'CHIPS STAPELN':'STACK CHIPS'}<span aria-hidden="true">♠</span></span>
            </button>
          </div>
          <div className="ce-stats-row">
            <span>{de?'Gesamtklicks':'Total clicks'} <b>{fmt(e.clicks,language)}</b></span>
            <span className="ce-stats-separator" aria-hidden="true">◆</span>
            <span>{de?'Gebäudestufen':'Building levels'} <b>{e.totalLevels}</b></span>
          </div>
        </section>

        <section className="ce-goal" aria-label={de?'Nächstes Ziel':'Next goal'}>
          <div className="ce-goal-crest" aria-hidden="true"><span>✦</span></div>
          <div className="ce-goal-content">
            <div className="ce-goal-heading">
              <small>{de?'NÄCHSTER MEILENSTEIN':'NEXT MILESTONE'}</small>
              <strong>{goalTitle}</strong>
            </div>
            <div className="ce-goal-track" role="progressbar"
              aria-label={goalTitle} aria-valuemin={0} aria-valuemax={100}
              aria-valuenow={Math.round(goal.progress*100)}>
              <i style={{width:Math.round(goal.progress*100)+'%'}}/>
            </div>
            <div className="ce-goal-meta">
              <span>{goal.id?fmt(goal.current,language)+' / '+fmt(goal.target,language)+' cr':
                (de?'Abgeschlossen':'Complete')}</span>
              <span>{Math.round(goal.progress*100)}%</span>
            </div>
            <p className="ce-goal-reward"><span aria-hidden="true">↗</span> {goalReward}</p>
          </div>
        </section>
      </section>

      <aside className="ce-side" aria-label={de?'Casino-Ausbau':'Casino buildings'}>
        <div className="ce-side-frame">
          <button className="ce-accordion-head ce-side-heading" type="button"
            onClick={()=>setBuildingsOpen(v=>!v)} aria-expanded={buildingsOpen}>
            <span>{de?'DEIN CASINO':'YOUR CASINO'}
              <small>{shown.filter(b=>b.level>0).length}/{e.buildings.length} {de?'eröffnet':'opened'}</small>
            </span>
            <span aria-hidden="true">{buildingsOpen?'−':'+'}</span>
          </button>
          <div className="ce-building-region" data-open={buildingsOpen} inert={!buildingsOpen}>
            <div className="ce-building-scroll" role="region"
              aria-label={de?'Gebäudeliste':'Building list'} tabIndex={0}>
              <div className="ce-buildings">
                {shown.map(building=><BuildingTile key={building.id} building={building} de={de}
                  active={selected===building.id} disabled={disabled}
                  onSelect={()=>setSelected(v=>v===building.id?null:building.id)}
                  onUpgrade={()=>command({action:'building.upgrade',id:building.id})}/>)}
              </div>
            </div>
          </div>
          {(e.totalLevels>=12||e.prestige>0)&&
            <section className="ce-prestige-panel" aria-label="Prestige">
              <button className="ce-accordion-head ce-prestige-heading" type="button"
                onClick={()=>setPrestigeOpen(v=>!v)} aria-expanded={prestigeOpen}>
                <span>✧ PRESTIGE
                  <small>{e.canPrestige?(de?'Bereit':'Ready'):(de?'Langzeitziel':'Long-term goal')}</small>
                </span>
                <span aria-hidden="true">{prestigeOpen?'−':'+'}</span>
              </button>
              <div className="ce-prestige-disclosure" data-open={prestigeOpen} inert={!prestigeOpen}>
                <div className="ce-prestige-content">
                  <p>{de?'Baue dein Casino neu auf und erhöhe Klick- und Gebäudeerträge dauerhaft.':
                    'Rebuild the casino with a permanent boost to clicks and building income.'}</p>
                  <div className="ce-goal-track" role="progressbar"
                    aria-label={de?'Prestige-Fortschritt':'Prestige progress'}
                    aria-valuemin={0} aria-valuemax={100}
                    aria-valuenow={Math.min(100,Math.floor(Number(e.runCredits)/
                      Math.max(1,Number(e.prestigeRequirement))*100))}>
                    <i style={{width:Math.min(100,Math.floor(Number(e.runCredits)/
                      Math.max(1,Number(e.prestigeRequirement))*100))+'%'}}/>
                  </div>
                  <small>{fmt(e.runCredits,language)} / {fmt(e.prestigeRequirement,language)} cr
                    {' · '}{e.totalLevels}/20 {de?'Stufen':'levels'}</small>
                  <button className="ce-upgrade" disabled={!e.canPrestige||disabled}
                    onClick={()=>setConfirm(true)}>{de?'Prestige starten':'Start prestige'}</button>
                </div>
              </div>
            </section>}
        </div>
      </aside>
    </main>
    {notice&&<div className="ce-notice" role="status">{notice}
      <button onClick={()=>setNotice(null)} aria-label="Dismiss">×</button></div>}
    {confirm&&<div className="ce-modal-shade"><section className="ce-modal" role="dialog" aria-modal="true">
      <h2>{de?'Casino neu eröffnen?':'Reopen your casino?'}</h2>
      <p>{de?'Gebäudestufen und aktuelle Credits werden zurückgesetzt. Dein Prestige- und Klickfortschritt bleibt erhalten.':
        'Building levels and current credits reset; your prestige and click history stay.'}</p>
      <div><button onClick={()=>setConfirm(false)}>{de?'Abbrechen':'Cancel'}</button>
        <button className="ce-upgrade" onClick={()=>{if(command({action:'prestige'}))setConfirm(false);}}>
          {de?'Prestige bestätigen':'Confirm prestige'}</button></div>
    </section></div>}
  </div>;
}

createRoot(document.getElementById('root')!).render(<EmpireApp/>);
