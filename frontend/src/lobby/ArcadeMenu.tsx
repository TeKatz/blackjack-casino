import {useEffect,useState} from 'react';
import type {CSSProperties} from 'react';
import {arcadeCategoryNames,arcadeGames} from './arcadeCatalog';
import {isPlayableArcade} from './arcadeEngine';
import type {PlayableArcadeId} from './arcadeEngine';
import {readArcadeRecord} from './arcadeRecords';
import {ArcadeGameScreen} from './ArcadeGameScreen';
import type {ArcadeCategory,ArcadeGame} from './arcadeCatalog';
import './arcade.css';

type Language='de'|'en';
type CategoryFilter='all'|ArcadeCategory;
type Props={language:Language;onBack:()=>void};
const categories:readonly CategoryFilter[]=['all','one-button','reaction','precision'];
const text={
 de:{back:'← Casino-Lobby',eyebrow:'THE MIDNIGHT CLUB · ARCADE',
  intro:'Kurze Runden. Hohe Schwierigkeit. Ein Versuch geht immer noch.',
  subtitle:'Eine eigene Spielhalle für Geschicklichkeit, Timing und Highscores.',
  games:'SPIELE',coming:'4 SPIELBAR · 6 GEPLANT',details:'DETAILS ANSEHEN',available:'SPIELBAR',play:'JETZT SPIELEN',best:'REKORD',
  records:'HIGHSCORES',soon:'Geplant',difficulty:'SCHWIERIGKEIT',duration:'RUNDE',
  controls:'STEUERUNG',rules:'SPIELPRINZIP',notReady:'Noch nicht spielbar',
  note:'Arcade Update 1: Midnight Wings, Cave Escape, Perfect Stack und Blade Timing sind spielbar. Sechs weitere Spiele folgen.',
  close:'Details schließen',filter:'Arcade-Spiele filtern'},
 en:{back:'← Casino Lobby',eyebrow:'THE MIDNIGHT CLUB · ARCADE',
  intro:'Short runs. High difficulty. Just one more attempt.',
  subtitle:'A dedicated arcade for skill, timing and high scores.',
  games:'GAMES',coming:'4 PLAYABLE · 6 PLANNED',details:'VIEW DETAILS',available:'PLAYABLE',play:'PLAY NOW',best:'BEST',
  records:'HIGH SCORES',soon:'Planned',difficulty:'DIFFICULTY',duration:'RUN',
  controls:'CONTROLS',rules:'HOW TO PLAY',notReady:'Not playable yet',
  note:'Arcade Update 1: Midnight Wings, Cave Escape, Perfect Stack and Blade Timing are playable. Six more games are planned.',
  close:'Close details',filter:'Filter arcade games'},
};
const duration=(game:ArcadeGame)=>game.seconds[0]+'–'+game.seconds[1]+' s';
const tint=(game:ArcadeGame)=>({'--arcade-accent':game.accent} as CSSProperties);

export function ArcadeMenu({language,onBack}:Props){
 const [category,setCategory]=useState<CategoryFilter>('all');
 const [selectedId,setSelectedId]=useState<string|null>(null);
 const [playingId,setPlayingId]=useState<PlayableArcadeId|null>(null);
 const t=text[language],selected=arcadeGames.find(x=>x.id===selectedId);
 const shown=arcadeGames.filter(x=>category==='all'||x.category===category);
 useEffect(()=>{
  const key=(event:KeyboardEvent)=>{
   if(event.key!=='Escape')return;
   event.preventDefault();
   if(playingId)return; // The active game handles Escape and pause.
   if(selectedId)setSelectedId(null);else onBack();
  };
  window.addEventListener('keydown',key);
  return()=>window.removeEventListener('keydown',key);
 },[selectedId,playingId,onBack]);
 if(playingId)return <ArcadeGameScreen id={playingId} language={language}
  onBack={()=>{setPlayingId(null);setSelectedId(null);}}/>;
 return <main className="arcade-menu" data-testid="arcade-menu">
  <header className="arcade-menu-header">
   <div className="arcade-heading-copy">
    <button className="arcade-back" type="button" onClick={onBack}>{t.back}</button>
    <p className="arcade-eyebrow">{t.eyebrow}</p>
    <h1>Arcade <em>Games</em></h1>
    <p className="arcade-intro">{t.intro}</p>
    <p className="arcade-subtitle">{t.subtitle}</p>
   </div>
   <div className="arcade-header-visual" aria-hidden="true">
    <span className="arcade-visual-ring"/>
    <span className="arcade-visual-hex">⬡</span>
    <span className="arcade-visual-orb one"/>
    <span className="arcade-visual-orb two"/>
    <span className="arcade-visual-orb three"/>
   </div>
   <div className="arcade-summary" aria-label={t.games+': '+arcadeGames.length}>
    <strong>{arcadeGames.length.toString().padStart(2,'0')}</strong><span>{t.games}</span>
   </div>
  </header>
  <div className="arcade-toolbar">
   <div className="arcade-filters" role="group" aria-label={t.filter}>
    {categories.map(id=><button key={id} type="button" aria-pressed={category===id}
     className={category===id?'active':''} onClick={()=>setCategory(id)}>
     {arcadeCategoryNames[id][language]}
    </button>)}
   </div>
   <span className="arcade-status"><span aria-hidden="true" className="arcade-status-dot"/>{t.coming}</span>
  </div>
  <section className="arcade-games-grid" aria-label={t.games}>
   {shown.map((game,index)=><button type="button" className="arcade-game-card"
    key={game.id} data-arcade-id={game.id} style={tint(game)}
    aria-label={game.name+' · '+t.details} onClick={()=>setSelectedId(game.id)}>
    <div className="arcade-game-art" aria-hidden="true">
     <div className="arcade-orbit"><span className="arcade-art-symbol">{game.symbol}</span></div>
     <span className="arcade-art-line first"/><span className="arcade-art-line second"/>
     <span className="arcade-art-index">{String(index+1).padStart(2,'0')}</span>
    </div>
    <div className="arcade-card-content">
     <div className="arcade-card-category">
      <span>{arcadeCategoryNames[game.category][language]}</span>
      <span className={'arcade-card-soon'+(isPlayableArcade(game.id)?' arcade-card-live':'')}>
       {isPlayableArcade(game.id)?t.available:t.soon}
      </span>
     </div>
     <h2>{game.name}</h2>
     <p>{game.description[language]}</p>
     <div className="arcade-card-footer">
      <span>{isPlayableArcade(game.id)?t.best+': '+readArcadeRecord(game.id).best:duration(game)}</span>
      <span>{t.details} <span aria-hidden="true">›</span></span>
     </div>
    </div>
   </button>)}
  </section>
  <p className="arcade-note">{t.note}</p>
  {selected&&<div className="arcade-detail-backdrop" onMouseDown={e=>{
   if(e.target===e.currentTarget)setSelectedId(null);
  }}>
   <section className="arcade-detail" role="dialog" aria-modal="true" aria-label={selected.name}
    style={tint(selected)}>
    <button type="button" className="arcade-detail-close" autoFocus
     aria-label={t.close} onClick={()=>setSelectedId(null)}>×</button>
    <div className="arcade-detail-emblem" aria-hidden="true">{selected.symbol}</div>
    <p className="arcade-eyebrow">{arcadeCategoryNames[selected.category][language]}</p>
    <h2>{selected.name}</h2>
    <p className="arcade-detail-intro">{selected.description[language]}</p>
    <p className="arcade-detail-rules">{selected.detail[language]}</p>
    <dl className="arcade-detail-facts">
     <div><dt>{t.difficulty}</dt><dd>{'◆'.repeat(selected.difficulty)}<span className="arcade-empty">{'◇'.repeat(5-selected.difficulty)}</span></dd></div>
     <div><dt>{t.duration}</dt><dd>{duration(selected)}</dd></div>
     <div><dt>{t.controls}</dt><dd>{selected.controls[language]}</dd></div>
    </dl>
    {isPlayableArcade(selected.id)?
      <>
       <div className="arcade-detail-record" aria-label={t.best}>
        <span>{t.best}: <strong>{readArcadeRecord(selected.id).best}</strong></span>
        <span>{duration(selected)}</span>
       </div>
       <button type="button" className="arcade-detail-play"
        data-testid="arcade-play-button"
        onClick={()=>{setPlayingId(selected.id as PlayableArcadeId);setSelectedId(null);}}>
        {t.play}
       </button>
      </>
      :<div className="arcade-detail-unavailable" role="status">{t.notReady}</div>}
   </section>
  </div>}
 </main>;
}
