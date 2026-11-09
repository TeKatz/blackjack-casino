import {useEffect,useRef,useState} from 'react';
import type {TowerDefenseState,TowerIntent,TowerKind} from './towerDefenseBridge';
import {BastionBattlefield} from './TowerDefenseBattlefield';
import {BastionAudio} from './towerDefenseAudio';
import type {BastionCue} from './towerDefenseAudio';
import {previewDamage,previewRange} from './towerDefenseVisuals';
import './towerDefense.css';

type Props={state:TowerDefenseState;language:'en'|'de';pending:boolean;error:string;
  animations:boolean;soundEnabled:boolean;command:(intent:TowerIntent)=>void};
const costs:Record<TowerKind,number>={archer:70,frost:105,cannon:135,ballista:170,flame:150};
const locks:Record<TowerKind,number>={archer:1,frost:1,cannon:1,ballista:2,flame:4};
type SkillId='capital'|'refund'|'fortify'|'engineer'|'scouting'|'focus'|'last-stand';
const skills:{id:SkillId;max:number;icon:string;group:string;en:[string,string];de:[string,string]}[]=[
 {id:'capital',max:5,icon:'◆',group:'Economy',
  en:['Starting treasury','Start each run with 16 more coins per rank.'],de:['Startkapital','Jede Stufe bringt 16 zusätzliche Startmünzen.']},
 {id:'refund',max:5,icon:'↺',group:'Economy',
  en:['Careful trade','Sell towers for 7 percentage points more per rank, up to 95%.'],
  de:['Geschickter Handel','7 Prozentpunkte mehr Verkaufswert pro Stufe, bis zu 95 %.']},
 {id:'engineer',max:5,icon:'⚙',group:'Economy',
  en:['Efficient construction','Tower builds and upgrades cost 2% less per rank.'],
  de:['Effiziente Bauweise','Türme und Upgrades kosten 2 % weniger je Stufe.']},
 {id:'fortify',max:5,icon:'♥',group:'Defence',
  en:['Fortified walls','Start with 2 extra lives per rank.'],
  de:['Befestigte Mauern','Jede Stufe gibt 2 zusätzliche Startleben.']},
 {id:'last-stand',max:1,icon:'✦',group:'Defence',
  en:['Last stand','Once per run, prevent defeat and remain at one life.'],
  de:['Letzte Verteidigung','Einmal pro Partie eine Niederlage abfangen und mit einem Leben weiterspielen.']},
 {id:'focus',max:5,icon:'⌖',group:'Warfare',
  en:['Tower discipline','All towers deal 2% more damage per rank.'],
  de:['Turmdisziplin','Alle Türme verursachen 2 % mehr Schaden je Stufe.']},
 {id:'scouting',max:1,icon:'◇',group:'Warfare',
  en:['Scouting','See the next wave’s enemy composition before starting.'],
  de:['Aufklärung','Sieh vor dem Start die Zusammensetzung der nächsten Welle.']},
];
const stages=[
 {id:'forest' as const,en:'Forest Border',de:'Waldgrenze',level:1,waves:15,number:'01',img:'battlefield'},
 {id:'frost' as const,en:'Frostpass',de:'Frostpass',level:3,waves:20,number:'02',img:'frostpass'},
];
const towerNames={en:{archer:'Archer',frost:'Frost',cannon:'Cannon',ballista:'Ballista',flame:'Flame Spire'},
  de:{archer:'Bogenturm',frost:'Frostturm',cannon:'Kanone',ballista:'Balliste',flame:'Flammenturm'}};
const towerDescription={
 en:{archer:'Fast precision fire. Excellent against runners.',
  frost:'Ice magic that slows enemies in its range.',
  cannon:'Heavy shells and splash damage against groups.',
  ballista:'Long-range siege bolts with devastating single-target damage.',
  flame:'Rapid bursts that scorch clustered enemies.'},
 de:{archer:'Schnelle Präzisionsschüsse gegen flinke Feinde.',
  frost:'Eismagie, die getroffene Gegner verlangsamt.',
  cannon:'Schwere Geschosse mit Flächenschaden gegen Gruppen.',
  ballista:'Langsame, gewaltige Bolzen mit besonders hoher Reichweite.',
  flame:'Schnelle Flammenstöße gegen Gruppen von Feinden.'},
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
 const progress=game.progress??{level:1,experience:0,skills:{},completedStages:[],bestWaves:{},pointsAvailable:0,enemiesDefeated:0,enemiesEscaped:0,wavesCleared:0,victories:0,defeats:0,runsStarted:0};
 const rank=(skill:SkillId)=>progress.skills[skill]??0;
 const price=(base:number)=>Math.max(1,Math.floor(base*(100-rank('engineer')*2)/100));
 const stage=game.stage??'forest';
 const [selected,setSelected]=useState<number|null>(0);
 const tower=selected===null?undefined:game.towers.find(x=>x.pad===selected);
 const canAct=game.phase!=='won'&&game.phase!=='lost';
 const levelCost=tower&&tower.level<3?price(Math.floor(costs[tower.kind]*(tower.level+1)/2)):null;
 const listPrice=tower?costs[tower.kind]+Array.from({length:tower.level-1},(_,i)=>
  Math.floor(costs[tower.kind]*(i+2)/2)).reduce((a,b)=>a+b,0):0;
 const sellAmount=Math.floor((tower?.spent||listPrice)*(60+rank('refund')*7)/100);
 // Deployment does not equal wave progress: only confirmed enemy kills advance this bar.
 const killPct=game.spawnTotal>0?Math.round(game.waveKills/game.spawnTotal*100):0;
 const confirmRestart=()=>{
  if(window.confirm(t.restartConfirm))issue({action:'tower.restart'});
 };
 if(state.screen==='menu')return <section className="td-shell td-phase2 td-menu" data-testid="midnight-bastion">
  <div className="td-menu-head">
   <div><p className="td-eyebrow">THE MIDNIGHT CLUB · DEFENCE WING</p><h1>Midnight <span>Bastion</span></h1>
    <p>{language==='de'?'Wähle deine Front. Jede Karte speichert ihre Verteidigung.':'Choose your front. Each map keeps its own defence.'}</p></div>
   <div className="td-mastery-summary">
    <small>{language==='de'?'BASTION-LEVEL':'BASTION LEVEL'}</small><strong>{progress.level}</strong>
    <div className="td-xp-rail"><span style={{width:(progress.level>=100?100:progress.experience/(110+progress.level*70)*100)+'%'}}/></div>
    <small>{progress.experience} / {110+progress.level*70} XP</small>
    <button className="td-primary" disabled={pending} onClick={()=>issue({action:'tower.skills'})}>
     {language==='de'?'SKILLTREE ÖFFNEN':'OPEN SKILL TREE'} · {progress.pointsAvailable} ◆</button>
   </div>
  </div>
  <div className="td-menu-title"><h2>{language==='de'?'KARTENAUSWAHL':'SELECT A BATTLEFIELD'}</h2>
   <span>{progress.completedStages.length} / {stages.length} {language==='de'?'abgeschlossen':'completed'}</span></div>
  <div className="td-stage-grid">
   {stages.map(item=>{const locked=progress.level<item.level;
    const inProgress=item.id===stage?game:game.stageRuns?.[item.id] as {wave?:number;phase?:string}|undefined;
    const best=progress.bestWaves[item.id]??0;
    return <article key={item.id} className={'td-stage-card '+(locked?'locked':'')} data-stage={item.id}>
     <div className="td-stage-art" style={{backgroundImage:'linear-gradient(0deg,#0b1b24 4%,transparent 90%),url(/tower-defense/'+item.img+'.svg)'}}>
      <span>CHAPTER {item.number}</span>{locked&&<strong>◈ LV {item.level}</strong>}</div>
     <div className="td-stage-body"><h3>{item[language]}</h3>
      <p>{item.waves} {language==='de'?'Kampagnenwellen':'campaign waves'} · {language==='de'?'Bestleistung':'Best'} {best}</p>
      <p className="td-stage-status">{progress.completedStages.includes(item.id)?'◆ '+(language==='de'?'ABGESCHLOSSEN':'CLEARED'):
       inProgress?.wave?(language==='de'?'Welle ':'Wave ')+inProgress.wave:'◇ '+(language==='de'?'NEUE VERTEIDIGUNG':'NEW DEFENCE')}</p>
      <button className="td-primary" disabled={locked||pending}
       onClick={()=>issue({action:'tower.play',kind:item.id})}>
       {locked?(language==='de'?'AB LEVEL ':'UNLOCKS AT LV ')+item.level:
        inProgress?.wave?(language==='de'?'FORTSETZEN':'CONTINUE'):(language==='de'?'SPIELEN':'PLAY')}</button>
     </div></article>;
   })}
  </div>
  <div className="td-menu-foot"><span>{language==='de'?'Erhalte XP für abgeschlossene Wellen und schalte dauerhaft neue Möglichkeiten frei.':'Earn XP from completed waves to unlock more options permanently.'}</span>
   <button disabled={pending} onClick={()=>issue({action:'tower.skills'})}>{language==='de'?'SKILLTREE':'SKILL TREE'} →</button></div>
  {(game.phase==='won'||game.phase==='lost')&&<div className="td-end-screen" role="dialog"
   aria-label={game.phase==='won'?t.victory:t.defeat}>
    <div className="td-end-content">
     <p className="td-eyebrow">THE MIDNIGHT CLUB · {stage==='frost'?'FROSTPASS':'FOREST BORDER'}</p>
     <div className="td-end-emblem">{game.phase==='won'?'♜':'⚑'}</div>
     <h2>{game.phase==='won'?t.victory:t.defeat}</h2>
     <p>{game.phase==='won'?(language==='de'?'Alle '+state.maxWaves+' Wellen überstanden.':'All '+state.maxWaves+' waves cleared.'):t.lost}</p>
     <div className="td-end-numbers"><div><small>{t.wave}</small><strong>{game.wave}</strong></div>
      <div><small>{language==='de'?'BESIEGT':'DEFEATED'}</small><strong>{game.waveKills}</strong></div>
      <div><small>BASTION LV</small><strong>{progress.level}</strong></div></div>
     <div className="td-end-actions">
      {game.phase==='won'&&<button className="td-primary" disabled={pending}
       onClick={()=>issue({action:'tower.endless'})}>{language==='de'?'ENDLOS WEITERSPIELEN':'CONTINUE ENDLESS'}</button>}
      <button disabled={pending} onClick={()=>issue({action:'tower.menu'})}>{language==='de'?'ZUR KARTENAUSWAHL':'MAP SELECTION'}</button>
      <button disabled={pending} onClick={confirmRestart}>{language==='de'?'ERNEUT SPIELEN':'RETRY MAP'}</button>
     </div></div>
   </div>}
  {error&&<p className="td-error" role="alert">{error}</p>}
 </section>;
 if(state.screen==='skills')return <section className="td-shell td-phase2 td-skills" data-testid="midnight-bastion-skills">
  <div className="td-skill-header">
   <div><p className="td-eyebrow">THE MIDNIGHT CLUB · MASTERY</p><h1>{language==='de'?'Bastion-':'Bastion '}<span>{language==='de'?'Meisterschaft':'Mastery'}</span></h1>
    <p>{language==='de'?'Alle Fähigkeiten lassen sich langfristig freischalten.':'All skills can eventually be mastered.'}</p></div>
   <div className="td-skill-points"><small>{language==='de'?'VERFÜGBARE PUNKTE':'AVAILABLE POINTS'}</small><strong>{progress.pointsAvailable}</strong>
    <span>LV {progress.level}</span></div>
  </div>
  <div className="td-skill-grid">
   {skills.map(skill=><article className="td-skill-card" key={skill.id} data-skill={skill.id}>
    <div className="td-skill-top"><span className="td-skill-icon">{skill.icon}</span><span className="td-skill-category">{skill.group}</span></div>
    <h3>{skill[language][0]}</h3><p>{skill[language][1]}</p>
    <div className="td-skill-ranks" aria-label={rank(skill.id)+' / '+skill.max}>
     {Array.from({length:skill.max},(_,i)=><span key={i} className={i<rank(skill.id)?'filled':''}/>)}
    </div>
    <button disabled={pending||rank(skill.id)>=skill.max||progress.pointsAvailable<1}
     onClick={()=>issue({action:'tower.skill.buy',kind:skill.id})}>
     {rank(skill.id)>=skill.max?(language==='de'?'MAXIMUM':'MAXIMUM'):
      (language==='de'?'FREISCHALTEN':'UPGRADE')+' · 1 ◆'}</button>
   </article>)}
  </div>
  <div className="td-skills-footer">
  <button className="td-skill-back" disabled={pending} onClick={()=>issue({action:'tower.menu'})}>
   ← {language==='de'?'ZUR KARTENAUSWAHL':'BACK TO MAPS'}</button>
   <button disabled={pending||Object.values(progress.skills).every(v=>v===0)}
    onClick={()=>{if(window.confirm(language==='de'?'Skillpunkte kostenlos zurücksetzen?':'Reset all skill points for free?'))issue({action:'tower.skill.reset'});}}>
    {language==='de'?'PUNKTE NEU VERTEILEN':'RESET SKILL POINTS'}</button></div>
  {error&&<p className="td-error" role="alert">{error}</p>}
 </section>;
 return <section className="td-shell td-phase2" data-testid="midnight-bastion">
  <div className="td-atmosphere" aria-hidden="true"/>
  <div className="td-heading">
   <div><p className="td-eyebrow">THE MIDNIGHT CLUB · DEFENCE WING <span className="td-edition">CHAPTER {stage==='frost'?'II':'I'}</span></p>
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
    ?game.waveKills+' / '+game.spawnTotal+' '+(language==='de'?'besiegt':'defeated')+
      (game.waveEscapes>0?' · '+game.waveEscapes+' '+(language==='de'?'entkommen':'escaped'):'')
    :game.phase==='won'?t.won:game.phase==='lost'?t.lost:t.intro}</span>
  </div>
  <div className="td-layout">
   <div className="td-main">
    <div className="td-board-frame">
      <div className="td-board-top"><span>✦ &nbsp; {stage==='frost'?'THE FROZEN PASS':'THE NORTHERN APPROACH'}</span><span className="td-stage">{game.phase==='running'?'● LIVE':'◇ READY'}</span></div>
      <BastionBattlefield model={state} selected={selected} onSelect={setSelected}
       onDropTower={(pad,kind)=>{if(canAct&&!pending&&!game.towers.some(t=>t.pad===pad)
        &&progress.level>=locks[kind]&&game.coins>=price(costs[kind]))
        issue({action:'tower.build',pad,kind});}}
       animations={animations} onCue={playCue}/>
      <div className="td-board-bottom"><span>✦ &nbsp; MIDNIGHT BASTION</span><span>{stage==='frost'?'02 / FROSTPASS':'01 / NORTH GATE'}</span></div>
    </div>
    {game.phase==='running'&&<div className="td-wave-progress" aria-label={killPct+'% defeated'}>
      <span>{language==='de'?'GEGNER BESIEGT':'ENEMIES DEFEATED'}</span>
      <div className="td-wave-rail"><div style={{width:killPct+'%'}}/></div>
      <strong>{game.waveKills} / {game.spawnTotal}</strong>
    </div>}
    <p className="td-caption">{language==='de'?'Türme per Drag-and-drop auf freie Bauplätze ziehen oder Bauplatz anklicken.':
  'Drag a tower onto an empty building site, or click a site to build.'}</p>
    {game.phase==='planning'&&rank('scouting')>0&&<p className="td-scout">
     ◈ {language==='de'?'AUFKLÄRUNG':'SCOUTING'} · {language==='de'?'Nächste Welle:':'Next wave:'}
     {' '+Math.min(96,7+(game.wave+1)*2)+' '+(language==='de'?'Gegner':'enemies')}
     {game.wave+1>=3?' · '+(language==='de'?'schwere Einheiten':'heavy units'):''}
     {game.wave+1>=5?' · '+(language==='de'?'Schilde':'shields'):''}</p>}
    <div className="td-actions">
     {game.phase==='planning'&&<button className="td-primary" disabled={pending}
      onClick={()=>issue({action:'tower.start'})}>{t.start}</button>}
     {game.phase==='running'&&<button disabled={pending} onClick={()=>issue({action:'tower.pause'})}>
      {game.paused?t.resume:t.pause}</button>}
     <button disabled={pending} onClick={()=>issue({action:'tower.speed'})}>
      {t.speed} ×{game.speed}</button>
     <button onClick={confirmRestart} disabled={pending}>{t.restart}</button>
     <button disabled={pending} onClick={()=>issue({action:'tower.menu'})}>← {language==='de'?'KARTEN':'MAPS'}</button>
    </div>
    {false&&(game.phase==='won'||game.phase==='lost')&&<div className="td-conclusion">
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
        draggable={canAct&&!pending&&progress.level>=locks[kind]&&game.coins>=price(costs[kind])}
        onDragStart={event=>{event.dataTransfer.effectAllowed='copy';event.dataTransfer.setData('text/plain',kind);}}
        disabled={selected===null||!canAct||pending||progress.level<locks[kind]||game.coins<price(costs[kind])}
        onClick={()=>{if(selected!==null)issue({action:'tower.build',pad:selected,kind});}} data-tower-kind={kind}>
        <span className={'td-tower-icon '+kind}><img src={'/tower-defense/'+kind+'.svg'} alt=""/></span>
        <span><strong>{towerNames[language][kind]}</strong><small>{progress.level<locks[kind]?'LV '+locks[kind]+' · '+(language==='de'?'GESPERRT':'LOCKED'):t.build+' · '+price(costs[kind])}</small></span>
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
