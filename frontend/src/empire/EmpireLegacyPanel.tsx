import {useState} from 'react';
import {fmt} from './empireBridge';
import type {EmpireLegacy,LegacyMilestone,LegacyUpgrade} from './empireBridge';

type Props = {
  legacy:EmpireLegacy;prestige:number;de:boolean;disabled:boolean;
  buy:(id:string)=>void;
};

const paths = [
  {id:'active',en:'High Roller',de:'High Roller',icon:'♠',
    hintEn:'Click power & lucky payouts',hintDe:'Klickstärke und Glückstreffer'},
  {id:'passive',en:'Tycoon',de:'Tycoon',icon:'◆',
    hintEn:'Production & steady growth',hintDe:'Produktion und stetiges Wachstum'},
  {id:'hybrid',en:'Inner Circle',de:'Inner Circle',icon:'✦',
    hintEn:'Strengthen both income types',hintDe:'Aktive und passive Synergien'},
] as const;

const groups = [
  {id:'clicks',en:'Active play',de:'Aktives Spielen',icon:'♠'},
  {id:'credits',en:'Lifetime wealth',de:'Gesamtvermögen',icon:'◇'},
  {id:'levels',en:'Construction',de:'Gebäudeentwicklung',icon:'⌂'},
  {id:'prestige',en:'Reopenings',de:'Neueröffnungen',icon:'✧'},
] as const;

const namesDe:Record<string,string>={
  'active-1':'Polierte Chips','active-2':'Glückliche 25','active-3':'High-Stakes-Hände',
  'active-4':'Schneller Jackpot','active-5':'Goldene Berührung',
  'passive-1':'Zuverlässiger Betrieb','passive-2':'Erfahrenes Personal',
  'passive-3':'Premium-Tische','passive-4':'Luxusbetrieb','passive-5':'Empire-Franchise',
  'hybrid-1':'Hausvorteil','hybrid-2':'Verbundene Bereiche',
  'hybrid-3':'Große Partnerschaft','hybrid-4':'Privates Syndikat','hybrid-5':'Midnight-Erbe',
};
const descriptionsDe:Record<string,string>={
  'active-1':'+15 % dauerhafte Klickstärke.',
  'active-2':'Kritische Klicks bringen das Vierfache statt des Dreifachen.',
  'active-3':'+25 % dauerhafte Klickstärke.',
  'active-4':'Jeder 20. statt jeder 25. Klick ist kritisch.',
  'active-5':'+50 % dauerhafte Klickstärke.',
  'passive-1':'+15 % dauerhafte Gebäudeproduktion.',
  'passive-2':'+10 % dauerhafte Gebäudeproduktion.',
  'passive-3':'+22 % dauerhafte Gebäudeproduktion.',
  'passive-4':'+25 % dauerhafte Gebäudeproduktion.',
  'passive-5':'+40 % dauerhafte Gebäudeproduktion.',
  'hybrid-1':'+8 % Klicks und Gebäudeproduktion.',
  'hybrid-2':'+12 % Klicks und Gebäudeproduktion.',
  'hybrid-3':'+15 % Klicks und Gebäudeproduktion.',
  'hybrid-4':'+18 % Klicks und Gebäudeproduktion.',
  'hybrid-5':'+25 % Klicks und Gebäudeproduktion.',
};
const milestoneNamesDe:Record<string,string>={
  'click-50':'Der erste Stapel','click-250':'Warme Hände','click-1000':'Im Rhythmus',
  'click-5000':'Chipmeister','click-20000':'Endloser Stapel',
  'wealth-1000':'Erste Einnahmen','wealth-50000':'Profitabler Abend',
  'wealth-1000000':'Millionär','wealth-50000000':'Casino-Vermögen',
  'wealth-2000000000':'Empire-Vermögen',
  'build-5':'Das Fundament','build-12':'Wachsendes Casino',
  'build-20':'Etabliertes Casino','build-32':'Luxuskomplex',
  'build-48':'Mitternachts-Wahrzeichen',
  'prestige-1':'Neuanfang','prestige-2':'Zweite Eröffnung',
  'prestige-3':'Steigender Ruf','prestige-5':'Casino-Dynastie',
  'prestige-10':'Mitternachtslegende',
};

const displayName=(item:LegacyUpgrade|LegacyMilestone,de:boolean)=>
  de?(namesDe[item.id]??milestoneNamesDe[item.id]??item.name):item.name;

function ProgressDots({count}:{count:number}){
  return <span className="ce-legacy-dots" aria-label={count+' / 5'}>
    {Array.from({length:5},(_,i)=><span key={i} className={i<count?'is-complete':''}/>)}
  </span>;
}

function UpgradePath({id,legacy,prestige,de,disabled,buy,path,setPath}:{
  id:string;legacy:EmpireLegacy;prestige:number;de:boolean;disabled:boolean;
  buy:(id:string)=>void;path:string;setPath:(id:string)=>void;
}){
    const config=paths.find(p=>p.id===id)!;
    const track=legacy.upgrades.filter(u=>u.path===id);
    const purchased=track.filter(u=>u.owned).length;
    const next=track.find(u=>!u.owned);
    const selected=path===id;
    return <div className={'ce-legacy-track'+(selected?' is-selected':'')}>
      <button className="ce-legacy-track-button" type="button"
        aria-expanded={selected} onClick={()=>setPath(id)}>
        <span className="ce-legacy-track-icon" aria-hidden="true">{config.icon}</span>
        <span className="ce-legacy-track-summary">
          <strong>{de?config.de:config.en}</strong>
          <small>{de?config.hintDe:config.hintEn}</small>
          <ProgressDots count={purchased}/>
        </span>
        <span className="ce-legacy-chevron" aria-hidden="true">{selected?'−':'+'}</span>
      </button>
      <div className="ce-legacy-track-detail" data-open={selected} inert={!selected}>
        <div className="ce-legacy-track-detail-inner">
          {next?<>
            <small className="ce-legacy-overline">{de?'NÄCHSTE FREISCHALTUNG':'NEXT UNLOCK'}</small>
            <strong className="ce-legacy-next-name">{displayName(next,de)}</strong>
            <p>{de?(descriptionsDe[next.id]??next.description):next.description}</p>
            {prestige<next.minimumPrestige?
              <span className="ce-legacy-requirement">⌑ {de?'Benötigt Prestige':'Requires Prestige'} {next.minimumPrestige}</span>:
              <button type="button" className="ce-legacy-purchase"
                disabled={disabled||!next.canBuy} onClick={()=>buy(next.id)}>
                <span>{de?'Dauerhaft freischalten':'Unlock permanently'}</span>
                <strong>{next.cost} ✦</strong>
              </button>}
            {prestige>=next.minimumPrestige&&!next.canBuy&&
              <small className="ce-legacy-requirement">{de?'Nächste Stufe nach genügend Punkten':'Earn more points to unlock'}</small>}
          </>:<div className="ce-legacy-complete">✦ {de?'Pfad vollständig ausgebaut':'Path completed'}</div>}
        </div>
      </div>
    </div>;
  }


function MilestoneGroup({id,legacy,de,category,setCategory}:{
  id:string;legacy:EmpireLegacy;de:boolean;category:string;setCategory:(id:string)=>void;
}){
  const language=de?'de':'en';
    const group=groups.find(g=>g.id===id)!;
    const entries=legacy.milestones.filter(m=>m.category===id);
    const earned=entries.filter(m=>m.earned).length;
    const next=entries.find(m=>!m.earned);
    const selected=category===id;
    return <div className={'ce-legacy-milestone'+(selected?' is-selected':'')}>
      <button className="ce-legacy-milestone-button" type="button"
        aria-expanded={selected} onClick={()=>setCategory(id)}>
        <span aria-hidden="true">{group.icon}</span>
        <strong>{de?group.de:group.en}</strong>
        <small>{earned}/5</small>
        <span aria-hidden="true">{selected?'−':'+'}</span>
      </button>
      <div className="ce-legacy-milestone-detail" data-open={selected} inert={!selected}>
        <div className="ce-legacy-milestone-inner">
          {next?<>
            <div className="ce-legacy-milestone-name"><span>{de?milestoneNamesDe[next.id]??next.name:next.name}</span>
              <small>+1 ✦</small></div>
            <div className="ce-legacy-progress" role="progressbar"
              aria-label={de?milestoneNamesDe[next.id]??next.name:next.name}
              aria-valuemin={0} aria-valuemax={100}
              aria-valuenow={Math.round(next.progress*100)}>
              <span style={{width:Math.round(next.progress*100)+'%'}}/>
            </div>
            <div className="ce-legacy-values">
              <span>{fmt(next.current,language)} / {fmt(next.target,language)}</span>
              <span>{Math.round(next.progress*100)}%</span>
            </div>
          </>:<div className="ce-legacy-complete">✦ {de?'Alle Ziele erreicht':'All goals completed'}</div>}
          <div className="ce-legacy-history">
            {entries.filter(m=>m.earned).slice(-2).map(m=>
              <span key={m.id}>✓ {de?milestoneNamesDe[m.id]??m.name:m.name}</span>)}
          </div>
        </div>
      </div>
    </div>;
  }

export function EmpireLegacyPanel({legacy,prestige,de,disabled,buy}:Props){
  const [section,setSection]=useState<'upgrades'|'milestones'|null>('upgrades');
  const [path,setPath]=useState<string>('active');
  const [category,setCategory]=useState<string>('clicks');
  const owned=legacy.upgrades.filter(u=>u.owned).length;
  const visibleMilestones=legacy.milestones.filter(m=>m.earned).length;
  const language=de?'de':'en';


  return <div className="ce-legacy-panel">
    <div className="ce-legacy-header">
      <div className="ce-legacy-emblem" aria-hidden="true">✧</div>
      <div><small>{de?'AB PRESTIGE I':'FROM PRESTIGE I'}</small>
        <h2>{de?'Dein Vermächtnis':'Your Legacy'}</h2>
        <p>{de?'Dauerhafter Fortschritt zwischen den Neueröffnungen.':
          'Permanent progress between reopenings.'}</p>
      </div>
    </div>
    <div className="ce-legacy-currency">
      <div><small>{de?'VERFÜGBARE PUNKTE':'AVAILABLE POINTS'}</small>
        <strong>{legacy.tokens} <span>✦</span></strong></div>
      <span>{de?'Pro Prestige +2':'Each prestige +2'}<small>{de?'Meilensteine +1':'Milestones +1'}</small></span>
    </div>

    <div className="ce-legacy-section">
      <button className="ce-legacy-section-toggle" type="button" aria-expanded={section==='upgrades'}
        onClick={()=>setSection(x=>x==='upgrades'?null:'upgrades')}>
        <span>◆ {de?'DAUERHAFTE UPGRADES':'PERMANENT UPGRADES'}</span>
        <small>{owned}/15</small><span aria-hidden="true">{section==='upgrades'?'−':'+'}</span>
      </button>
      <div className="ce-legacy-section-content" data-open={section==='upgrades'} inert={section!=='upgrades'}>
        <div className="ce-legacy-section-inner">
          {paths.map(p=><UpgradePath key={p.id} id={p.id} legacy={legacy} prestige={prestige} de={de}
              disabled={disabled} buy={buy} path={path} setPath={setPath}/>)}
        </div>
      </div>
    </div>

    <div className="ce-legacy-section">
      <button className="ce-legacy-section-toggle" type="button" aria-expanded={section==='milestones'}
        onClick={()=>setSection(x=>x==='milestones'?null:'milestones')}>
        <span>✧ {de?'MEILENSTEINE':'MILESTONES'}</span>
        <small>{visibleMilestones}/{legacy.total}</small><span aria-hidden="true">{section==='milestones'?'−':'+'}</span>
      </button>
      <div className="ce-legacy-section-content" data-open={section==='milestones'} inert={section!=='milestones'}>
        <div className="ce-legacy-section-inner">
          <p className="ce-legacy-explainer">{de?'Jedes Ziel vergibt einmalig einen Prestige-Punkt.':
            'Each milestone awards one permanent point, only once.'}</p>
          {groups.map(g=><MilestoneGroup key={g.id} id={g.id} legacy={legacy} de={de}
              category={category} setCategory={setCategory}/>)}
        </div>
      </div>
    </div>
  </div>;
}
