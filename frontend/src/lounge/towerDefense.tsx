import {useEffect,useRef,useState} from 'react';
import type {TowerDefenseState,TowerIntent,TowerKind} from './towerDefenseBridge';
import {BastionBattlefield} from './TowerDefenseBattlefield';
import {BastionAudio} from './towerDefenseAudio';
import type {BastionCue} from './towerDefenseAudio';
import {previewDamage,previewRange} from './towerDefenseVisuals';
import './towerDefense.css';

type Props={state:TowerDefenseState;language:'en'|'de';pending:boolean;error:string;
  animations:boolean;soundEnabled:boolean;command:(intent:TowerIntent)=>void};
const costs:Record<TowerKind,number>={archer:70,frost:105,cannon:135};
const towerNames={en:{archer:'Archer',frost:'Frost',cannon:'Cannon'},
  de:{archer:'Bogenturm',frost:'Frostturm',cannon:'Kanone'}};
const towerDescription={
 en:{archer:'Fast precision fire. Excellent against runners.',
  frost:'Ice magic that slows enemies in its range.',
  cannon:'Heavy shells and splash damage against groups.'},
 de:{archer:'Schnelle Präzisionsschüsse gegen flinke Feinde.',
  frost:'Eismagie, die getroffene Gegner verlangsamt.',
  cannon:'Schwere Geschosse mit Flächenschaden gegen Gruppen.'},
};
const copy={
 en:{sub:'A quiet defence of the midnight keep',wave:'WAVE',lives:'HEARTS',coins:'COINS',active:'IN THE FIELD',
  intro:'Choose a building site and prepare your towers. Start each wave when you are ready.',
  start:'START NEXT WAVE',pause:'PAUSE',resume:'RESUME',speed:'SPEED',restart:'NEW DEFENCE',
  restartConfirm:'Replace your current defence and start from wave one?',
  build:'BUILD',upgrade:'UPGRADE',sell:'SELL',level:'LEVEL',empty:'Choose a tower to build on this site.',
  selected:'BUILDING SITE',max:'MAX LEVEL',notEnough:'Not enough coins',select:'Select a building pad',
  victory:'THE KEEP STANDS',won:'All 15 waves defeated. Your fortress is safe.',
  defeat:'THE KEEP HAS FALLEN',lost:'The invaders breached the last gate.',
  planning:'PREPARATION',running:'UNDER ATTACK',paused:'PAUSED',hint:'Click a glowing construction circle or use the site selector.',
  save:'Progress is saved locally. No virtual credits are wagered or won.'},
 de:{sub:'Eine ruhige Verteidigung der Mitternachtsfeste',wave:'WELLE',lives:'LEBEN',coins:'MÜNZEN',active:'AUF DEM FELD',
  intro:'Wähle einen Bauplatz und errichte deine Türme. Du entscheidest, wann die nächste Welle beginnt.',
  start:'NÄCHSTE WELLE STARTEN',pause:'PAUSE',resume:'WEITER',speed:'TEMPO',restart:'NEUE VERTEIDIGUNG',
  restartConfirm:'Aktuellen Spielstand ersetzen und von Welle eins neu starten?',
  build:'BAUEN',upgrade:'VERBESSERN',sell:'VERKAUFEN',level:'STUFE',empty:'Wähle einen Turm für diesen Bauplatz.',
  selected:'BAUPLATZ',max:'MAXIMALE STUFE',notEnough:'Nicht genug Münzen',select:'Bauplatz wählen',
  victory:'DIE FESTUNG STEHT',won:'Alle 15 Wellen besiegt. Deine Festung ist sicher.',
  defeat:'DIE FESTUNG IST GEFALLEN',lost:'Die Angreifer haben das letzte Tor überwunden.',
  planning:'VORBEREITUNG',running:'UNTER ANGRIFF',paused:'PAUSIERT',hint:'Klicke auf einen leuchtenden Bauplatz oder wähle einen aus der Liste.',
  save:'Dein Fortschritt wird lokal gespeichert. Keine Einsätze oder Gewinne von Casino-Credits.'}
};
export function TowerDefenseScreen({state,language,pending,error,animations,soundEnabled,command}:Props){
 const audio=useRef<BastionAudio|null>(null);
 if(!audio.current)audio.current=new BastionAudio();
 useEffect(()=>audio.current?.setEnabled(soundEnabled),[soundEnabled]);
 useEffect(()=>()=>audio.current?.dispose(),[]);
 const issue=(intent:TowerIntent)=>{audio.current?.arm();command(intent);};
 const playCue=(cue:BastionCue)=>audio.current?.play(cue);

 const t=copy[language],game=state.state;
 const [selected,setSelected]=useState<number|null>(0);
 const tower=selected===null?undefined:game.towers.find(x=>x.pad===selected);
 const canAct=game.phase!=='won'&&game.phase!=='lost';
 const levelCost=tower&&tower.level<3?Math.floor(costs[tower.kind]*(tower.level+1)/2):null;
 const spent=tower?costs[tower.kind]+Array.from({length:tower.level-1},(_,i)=>
  Math.floor(costs[tower.kind]*(i+2)/2)).reduce((a,b)=>a+b,0):0;
 const sellAmount=Math.floor(spent*.6);
 const spawnPct=game.spawnTotal>0?Math.round(game.spawned/game.spawnTotal*100):0;
 const confirmRestart=()=>{
  if(window.confirm(t.restartConfirm))issue({action:'tower.restart'});
 };
 return <section className="td-shell td-phase2" data-testid="midnight-bastion">
  <div className="td-atmosphere" aria-hidden="true"/>
  <div className="td-heading">
   <div><p className="td-eyebrow">THE MIDNIGHT CLUB · DEFENCE WING <span className="td-edition">CHAPTER I</span></p>
    <h1>Midnight <span>Bastion</span></h1><p>{t.sub}</p></div>
   <div className="td-stats">
    <div><small>{t.wave}</small><strong>{game.wave}<span> / {state.maxWaves}</span></strong></div>
    <div><small>{t.lives}</small><strong>{game.lives}</strong></div>
    <div><small>{t.coins}</small><strong>{game.coins}</strong></div>
    <div><small>{t.active}</small><strong>{game.enemies.length}</strong></div>
   </div>
  </div>
  <div className="td-status">
   <span className={'td-state td-'+game.phase}>
    {game.phase==='running'&&game.paused?t.paused:t[game.phase]}
   </span>
   <span>{game.phase==='running'
    ?game.spawned+' / '+game.spawnTotal+' '+(language==='de'?'Gegner erschienen':'enemies deployed')
    :game.phase==='won'?t.won:game.phase==='lost'?t.lost:t.intro}</span>
  </div>
  <div className="td-layout">
   <div className="td-main">
    <div className="td-board-frame">
      <div className="td-board-top"><span>✦ &nbsp; THE NORTHERN APPROACH</span><span className="td-stage">{game.phase==='running'?'● LIVE':'◇ READY'}</span></div>
      <BastionBattlefield model={state} selected={selected} onSelect={setSelected} animations={animations} onCue={playCue}/>
      <div className="td-board-bottom"><span>✦ &nbsp; MIDNIGHT BASTION</span><span>01 / NORTH GATE</span></div>
    </div>
    {game.phase==='running'&&<div className="td-wave-progress" aria-label={spawnPct+'% deployed'}>
      <span>{language==='de'?'WELLENFORTSCHRITT':'WAVE DEPLOYMENT'}</span>
      <div className="td-wave-rail"><div style={{width:spawnPct+'%'}}/></div>
      <strong>{spawnPct}%</strong>
    </div>}
    <p className="td-caption">{t.hint}</p>
    <div className="td-actions">
     {game.phase==='planning'&&<button className="td-primary" disabled={pending}
      onClick={()=>issue({action:'tower.start'})}>{t.start}</button>}
     {game.phase==='running'&&<button disabled={pending} onClick={()=>issue({action:'tower.pause'})}>
      {game.paused?t.resume:t.pause}</button>}
     <button disabled={pending} onClick={()=>issue({action:'tower.speed'})}>
      {t.speed} ×{game.speed}</button>
     <button onClick={confirmRestart} disabled={pending}>{t.restart}</button>
    </div>
    {(game.phase==='won'||game.phase==='lost')&&<div className="td-conclusion">
     <h2>{game.phase==='won'?t.victory:t.defeat}</h2>
     <p>{game.phase==='won'?t.won:t.lost}</p>
    </div>}
   </div>
   <aside className="td-sidebar">
    <div className="td-sidebar-kicker">⚔ &nbsp; {language==='de'?'VERTEIDIGUNGSWERKSTATT':'DEFENCE WORKSHOP'}</div>
    <h2>{t.selected} <span>{selected===null?'—':String(selected+1).padStart(2,'0')}</span></h2>
    <label htmlFor="td-pad">{t.select}</label>
    <select id="td-pad" value={selected??''} onChange={e=>setSelected(Number(e.target.value))}>
     {state.pads.map((_,i)=><option key={i} value={i}>{t.selected} {i+1}</option>)}
    </select>
    {tower?<div className="td-equipped">
     <div className="td-tower-portrait">
       <span className={'td-portrait-halo '+tower.kind}/>
       <img src={'/tower-defense/'+tower.kind+'.svg'} alt=""/>
       <div className="td-rank">{'◆ '.repeat(tower.level).trim()}</div>
     </div>
     <h3>{towerNames[language][tower.kind]}</h3>
     <p>{t.level} {tower.level} / 3</p>
     <p className="td-tower-description">{towerDescription[language][tower.kind]}</p>
     <div className="td-readouts">
       <span><small>{language==='de'?'SCHADEN':'DAMAGE'}</small><strong>{previewDamage(tower.kind,tower.level)}</strong></span>
       <span><small>{language==='de'?'REICHWEITE':'RANGE'}</small><strong>{previewRange(tower.kind,tower.level)}</strong></span>
     </div>
     <button className="td-primary" disabled={!canAct||pending||levelCost===null||game.coins<levelCost}
      onClick={()=>{if(selected!==null)issue({action:'tower.upgrade',pad:selected});}}>
      {levelCost===null?t.max:t.upgrade+' · '+levelCost}
     </button>
     <button disabled={!canAct||pending} onClick={()=>{
      if(selected!==null)issue({action:'tower.sell',pad:selected});
     }}>{t.sell} · +{sellAmount}</button>
    </div>:<>
     <p className="td-help">{t.empty}</p>
     <div className="td-builds">
      {(Object.keys(costs) as TowerKind[]).map(kind=><button key={kind}
        disabled={selected===null||!canAct||pending||game.coins<costs[kind]}
        onClick={()=>{if(selected!==null)issue({action:'tower.build',pad:selected,kind});}}>
        <span className={'td-tower-icon '+kind}><img src={'/tower-defense/'+kind+'.svg'} alt=""/></span>
        <span><strong>{towerNames[language][kind]}</strong><small>{t.build} · {costs[kind]}</small></span>
       </button>)}
     </div>
    </>}
    <div className="td-sfx-label"><span className={soundEnabled?'td-sound-on':'td-sound-off'}>●</span> {language==='de'?'EFFEKTE':'SOUND FX'}: {soundEnabled?'ON':'OFF'}</div>
    <p className="td-save-note">{t.save}</p>
   </aside>
  </div>
  {error&&<p className="td-error" role="alert">{error}</p>}
 </section>;
}
