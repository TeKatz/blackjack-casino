import {useEffect,useRef,useState} from 'react';
import type {PointerEvent as ReactPointerEvent} from 'react';
import {ARCADE_HEIGHT,ARCADE_STEP,ARCADE_WIDTH,actArcadeRun,createArcadeRun,stepArcadeRun,steerSnake} from './arcadeEngine';
import type {PlayableArcadeId,ArcadeEvent,ArcadeRunState} from './arcadeEngine';
import {drawArcadeRun} from './arcadeRenderer';
import {ArcadeHeldKeys,arcadeDirection} from './arcadeControls';
import {readArcadeRecord,saveArcadeRecord} from './arcadeRecords';
import type {ArcadeRecord} from './arcadeRecords';
import {arcadeGames} from './arcadeCatalog';

type Phase='ready'|'playing'|'paused'|'over';
type Language='de'|'en';
const strings={
 de:{back:'← Arcade Games',score:'PUNKTE',best:'REKORD',time:'ZEIT',plays:'VERSUCHE',
  pause:'PAUSE',resume:'WEITER',restart:'NEUSTART',start:'SPIEL STARTEN',
  ready:'BEREIT FÜR DIE HERAUSFORDERUNG?',over:'RUNDE BEENDET',again:'NOCH EIN VERSUCH',
  paused:'PAUSIERT',soundOn:'TON AN',soundOff:'TON AUS',record:'NEUER REKORD!',
  instructions:'Leertaste / Klick = Aktion · P = Pause · R = Neustart · Esc = Pause',
  hold:'Leertaste oder Maustaste gedrückt halten, um zu steigen. Loslassen zum Sinken.',
  blocks:'Leertaste oder Klick zum Platzieren. Genaues Stapeln bewahrt die Breite.',
  knives:'Leertaste oder Klick wirft eine Klinge. Triff keine vorhandene Klinge.',
  wings:'Leertaste oder Klick zum Flügelschlag. Berühre keine Hindernisse.',
  snake:'WASD oder Pfeiltasten steuern. Sammle Futter und meide Wände und den eigenen Schwanz.',
  breaker:'Maus oder ← / → bewegen den Schläger. Leertaste oder Klick startet den Ball.',
  storm:'WASD / Pfeiltasten bewegen dein Schiff. Mit gedrückter Maus kannst du es ziehen.',
  hex:'A / D oder ← / → drehen das Dreieck. Steuere in die Lücken der näherkommenden Ringe.',
  spike:'Leertaste, ↑ oder Klick zum Springen. Weiche Stacheln und Abgründen aus.',
  ascent:'A / D oder ← / → lenken deine Figur. Sie springt beim Landen automatisch weiter.',
  exit:'ZUR SPIELAUSWAHL',local:'Persönliche Rekorde werden auf diesem Gerät gespeichert.',
  pauseHint:'Die Spielzeit wird während der Pause angehalten.'},
 en:{back:'← Arcade Games',score:'SCORE',best:'BEST',time:'TIME',plays:'RUNS',
  pause:'PAUSE',resume:'RESUME',restart:'RESTART',start:'START GAME',
  ready:'READY FOR THE CHALLENGE?',over:'RUN OVER',again:'ONE MORE TRY',
  paused:'PAUSED',soundOn:'SOUND ON',soundOff:'SOUND OFF',record:'NEW BEST!',
  instructions:'Space / Click = Action · P = Pause · R = Restart · Esc = Pause',
  hold:'Hold Space or the mouse button to rise. Release to descend.',
  blocks:'Press Space or click to drop. Perfect alignment saves block width.',
  knives:'Press Space or click to throw a blade. Avoid blades already attached.',
  wings:'Press Space or click to flap. Do not touch the obstacles.',
  snake:'Steer with WASD or arrow keys. Collect food; avoid walls and your own tail.',
  breaker:'Move the paddle with the mouse or ← / →. Space or click launches the ball.',
  storm:'Move the ship with WASD / arrow keys, or hold and drag with the mouse.',
  hex:'A / D or ← / → rotate the triangle. Steer into gaps in the approaching rings.',
  spike:'Press Space, ↑ or click to jump. Avoid spikes and gaps in the floor.',
  ascent:'A / D or ← / → steer your character. Each landing triggers an automatic jump.',
  exit:'BACK TO GAME LIST',local:'Personal bests are saved on this device.',
  pauseHint:'Game time is stopped while the run is paused.'},
};
const instructionsKey={
 'midnight-wings':'wings','cave-escape':'hold','perfect-stack':'blocks','blade-timing':'knives',
 'death-snake':'snake','neon-breaker':'breaker','bullet-storm':'storm',
 'hexagon-hell':'hex','spike-rush':'spike','endless-ascent':'ascent',
} as const;
const formatTime=(s:number)=>{
 const whole=Math.floor(Math.max(0,s));
 return Math.floor(whole/60).toString().padStart(2,'0')+':'+(whole%60).toString().padStart(2,'0');
};
function tone(event:ArcadeEvent,enabled:boolean,ctxRef:React.MutableRefObject<AudioContext|null>){
 if(!enabled||!event)return;
 try{
  const context=ctxRef.current??new AudioContext();
  ctxRef.current=context;
  if(context.state==='suspended')void context.resume();
  const oscillator=context.createOscillator(),gain=context.createGain();
  const start=context.currentTime,frequency={
   flap:440,score:680,fail:180,stage:930,
  }[event];
  oscillator.type=event==='fail'?'sawtooth':'sine';
  oscillator.frequency.setValueAtTime(frequency,start);
  oscillator.frequency.exponentialRampToValueAtTime(event==='fail'?75:frequency*1.2,start+.14);
  gain.gain.setValueAtTime(.0001,start);
  gain.gain.exponentialRampToValueAtTime(.05,start+.012);
  gain.gain.exponentialRampToValueAtTime(.0001,start+.19);
  oscillator.connect(gain).connect(context.destination);
  oscillator.start(start);oscillator.stop(start+.21);
 }catch{/* Some WebView2 installations disallow audio. Game remains fully playable. */}
}
function useCanvasPaint(canvas:HTMLCanvasElement|null,run:ArcadeRunState){
 if(!canvas)return;
 const rect=canvas.getBoundingClientRect();
 if(rect.width<=0||rect.height<=0)return;
 const dpr=Math.min(2,window.devicePixelRatio||1);
 const targetWidth=Math.max(1,Math.round(rect.width*dpr));
 const targetHeight=Math.max(1,Math.round(rect.height*dpr));
 if(canvas.width!==targetWidth)canvas.width=targetWidth;
 if(canvas.height!==targetHeight)canvas.height=targetHeight;
 const ctx=canvas.getContext('2d');if(!ctx)return;
 const scale=Math.min(rect.width/ARCADE_WIDTH,rect.height/ARCADE_HEIGHT);
 ctx.setTransform(dpr,0,0,dpr,0,0);
 ctx.clearRect(0,0,rect.width,rect.height);
 ctx.translate((rect.width-ARCADE_WIDTH*scale)/2,(rect.height-ARCADE_HEIGHT*scale)/2);
 ctx.scale(scale,scale);
 drawArcadeRun(ctx,run);
}
export function ArcadeGameScreen({id,language,onBack}:{
 id:PlayableArcadeId;language:Language;onBack:()=>void;
}){
 const game=arcadeGames.find(g=>g.id===id)!;
 const t=strings[language];
 const [phase,setPhase]=useState<Phase>('ready');
 const [score,setScore]=useState(0);
 const [record,setRecord]=useState<ArcadeRecord>(()=>readArcadeRecord(id));
 const [elapsed,setElapsed]=useState(0);
 const [soundEnabled,setSoundEnabled]=useState(true);
 const [isBest,setIsBest]=useState(false);
 const gameRef=useRef<ArcadeRunState>(createArcadeRun(id));
 const canvasRef=useRef<HTMLCanvasElement>(null);
 const held=useRef(false);
 const keys=useRef(new ArcadeHeldKeys());
 const mouseTarget=useRef<{x:number;y:number}|null>(null);
 const mouseActive=useRef(false);
 const raf=useRef<number>(0);
 const audioRef=useRef<AudioContext|null>(null);
 const phaseRef=useRef<Phase>(phase);
 phaseRef.current=phase;
 // The lobby refreshes its snapshot while playing and creates a fresh onBack prop.
 // Keep that callback current without ever tearing down held-key listeners mid-run.
 const onBackRef=useRef(onBack);
 onBackRef.current=onBack;
 const soundRef=useRef(soundEnabled);
 soundRef.current=soundEnabled;
 const repaint=()=>useCanvasPaint(canvasRef.current,gameRef.current);
 const finish=()=>{
  const s=gameRef.current;
  if(phaseRef.current==='over')return;
  const before=readArcadeRecord(id);
  const next=saveArcadeRecord(id,s.score,s.elapsed);
  setRecord(next);
  setScore(s.score);setElapsed(s.elapsed);
  setIsBest(s.score>before.best&&s.score>0);
  phaseRef.current='over';setPhase('over');
  tone('fail',soundRef.current,audioRef);
 };
 const action=()=>{
  if(phaseRef.current!=='playing')return;
  const s=gameRef.current;
  const event=actArcadeRun(s);
  if(s.over)finish();
  else if(event){
   setScore(s.score);
   tone(event,soundRef.current,audioRef);
  }
  repaint();
 };
 const start=()=>{
  held.current=false;
  keys.current.clear();mouseActive.current=false;mouseTarget.current=null;
  gameRef.current=createArcadeRun(id,Math.floor(Math.random()*0xffffffff));
  setScore(0);setElapsed(0);setIsBest(false);
  phaseRef.current='playing';setPhase('playing');
  if(id==='midnight-wings'||id==='neon-breaker'){
   const event=actArcadeRun(gameRef.current);
   tone(event,soundRef.current,audioRef);
  }
  repaint();
 };
 const pause=()=>{
  held.current=false;keys.current.clear();mouseActive.current=false;
  if(phaseRef.current==='playing'){phaseRef.current='paused';setPhase('paused');}
 };
 const resume=()=>{
  if(phaseRef.current==='paused'){phaseRef.current='playing';setPhase('playing');}
 };
 useEffect(()=>{
  const canvas=canvasRef.current;
  if(!canvas)return;
  const observer=new ResizeObserver(()=>useCanvasPaint(canvas,gameRef.current));
  observer.observe(canvas);
  useCanvasPaint(canvas,gameRef.current);
  return()=>observer.disconnect();
 },[id]);
 useEffect(()=>{
  if(phase!=='playing'){repaint();return;}
  let previous=performance.now(),accumulator=0,active=true;
  const frame=(now:number)=>{
   if(!active)return;
   accumulator+=Math.min(.06,Math.max(0,(now-previous)/1000));
   previous=now;
   let steps=0;
   while(accumulator>=ARCADE_STEP&&steps<5&&!gameRef.current.over){
    const state=gameRef.current,old=state.score;
    const controls=keys.current.controls();
    if((id==='neon-breaker'||id==='bullet-storm')&&mouseTarget.current&&
     (id==='neon-breaker'||mouseActive.current)){
     controls.pointerX=mouseTarget.current.x;
     if(id==='bullet-storm')controls.pointerY=mouseTarget.current.y;
    }
    const event=stepArcadeRun(state,ARCADE_STEP,held.current,controls);
    if(old!==state.score){
     setScore(state.score);
     if(event==='score')tone('score',soundRef.current,audioRef);
    }
    if(event==='fail'){finish();break;}
    if(event==='stage'&&id==='neon-breaker')tone('stage',soundRef.current,audioRef);
    accumulator-=ARCADE_STEP;steps++;
   }
   if(steps===5)accumulator=0;
   setElapsed(old=>Math.floor(old*4)!==Math.floor(gameRef.current.elapsed*4)
    ?gameRef.current.elapsed:old);
   repaint();
   if(active&&!gameRef.current.over)raf.current=requestAnimationFrame(frame);
  };
  raf.current=requestAnimationFrame(frame);
  return()=>{active=false;cancelAnimationFrame(raf.current);};
 },[phase,id]);
 // Keep the entire run truly paused when the app loses focus or goes to the background.
 useEffect(()=>{
  const inactive=()=>{held.current=false;keys.current.clear();mouseActive.current=false;if(phaseRef.current==='playing')pause();};
  const hidden=()=>{if(document.hidden)inactive();};
  window.addEventListener('blur',inactive);
  document.addEventListener('visibilitychange',hidden);
  return()=>{
   window.removeEventListener('blur',inactive);
   document.removeEventListener('visibilitychange',hidden);
  };
 },[]);
 useEffect(()=>{
  const keyDown=(event:KeyboardEvent)=>{
   const target=event.target as HTMLElement|null;
   if(event.ctrlKey||event.altKey||event.metaKey||target?.isContentEditable||
    ['INPUT','TEXTAREA','SELECT'].includes(target?.tagName??''))return;
   const key=event.key.toLowerCase();
   if([' ','arrowup','arrowdown','arrowleft','arrowright','w','a','s','d','p','r','escape'].includes(key))event.preventDefault();
   if(key==='p'||key==='escape'){
    if(event.repeat)return;
    if(phaseRef.current==='playing')pause();
    else if(phaseRef.current==='paused'&&key==='p')resume();
    else if(key==='escape')onBackRef.current();
    return;
   }
   if(key==='r'&&!event.repeat){start();return;}
   const direction=arcadeDirection(event.code,key);
   if(direction&&id==='death-snake'){
    if(phaseRef.current==='playing'&&!event.repeat){
     const state=gameRef.current;
     if(state.kind==='death-snake')steerSnake(state,direction);
    }
    return;
   }
   if(direction&&(id==='neon-breaker'||id==='bullet-storm'||id==='hexagon-hell'||id==='endless-ascent')){
    keys.current.press(event.code,key);
    // A pressed key immediately takes over; only actual mouse motion restores mouse steering.
    if(id==='neon-breaker'&&(direction==='left'||direction==='right'))mouseTarget.current=null;
    if(id==='bullet-storm')mouseActive.current=false;
    return;
   }
   if(key===' '||key==='arrowup'){
    if(id==='cave-escape')held.current=true;
    if(event.repeat)return;
    if(phaseRef.current==='ready'){start();if(id==='cave-escape')held.current=true;}
    else if(phaseRef.current==='playing'&&id!=='cave-escape')action();
    else if(phaseRef.current==='over')start();
   }
  };
  const keyUp=(event:KeyboardEvent)=>{
   const key=event.key.toLowerCase();
   if(id==='cave-escape'&&(key===' '||key==='arrowup'))held.current=false;
   keys.current.release(event.code,key);
  };
  window.addEventListener('keydown',keyDown,true);
  window.addEventListener('keyup',keyUp,true);
  return()=>{
   held.current=false;keys.current.clear();
   window.removeEventListener('keydown',keyDown,true);
   window.removeEventListener('keyup',keyUp,true);
  };
 // Install once per game, not whenever the parent lobby refreshes its callback.
 // Unmount and switching game IDs still release every held physical key.
 },[id]);
 useEffect(()=>()=>{void audioRef.current?.close();audioRef.current=null;},[]);
 const pointerCoordinates=(event:ReactPointerEvent<HTMLCanvasElement>)=>{
  const rect=event.currentTarget.getBoundingClientRect();
  const scale=Math.min(rect.width/ARCADE_WIDTH,rect.height/ARCADE_HEIGHT);
  if(scale<=0)return null;
  return {
   x:Math.max(0,Math.min(ARCADE_WIDTH,(event.clientX-rect.left-(rect.width-ARCADE_WIDTH*scale)/2)/scale)),
   y:Math.max(0,Math.min(ARCADE_HEIGHT,(event.clientY-rect.top-(rect.height-ARCADE_HEIGHT*scale)/2)/scale)),
  };
 };
 const pointerDown=(event:ReactPointerEvent<HTMLCanvasElement>)=>{
  if(event.button!==0)return;
  event.currentTarget.focus({preventScroll:true});
  if(id==='cave-escape'||id==='bullet-storm'){
   if(id==='cave-escape')held.current=true;
   if(id==='bullet-storm')mouseActive.current=true;
   event.currentTarget.setPointerCapture(event.pointerId);
  }
  if(id==='neon-breaker'||id==='bullet-storm')mouseTarget.current=pointerCoordinates(event);
  if(phaseRef.current==='ready'){
   start();
   if(id==='cave-escape')held.current=true;
   if(id==='bullet-storm'){mouseActive.current=true;mouseTarget.current=pointerCoordinates(event);}
   if(id==='neon-breaker')mouseTarget.current=pointerCoordinates(event);
  }
  else if(phaseRef.current==='playing'&&id!=='cave-escape'&&id!=='bullet-storm')action();
 };
 const pointerMove=(event:ReactPointerEvent<HTMLCanvasElement>)=>{
  if(id==='neon-breaker'||(id==='bullet-storm'&&mouseActive.current))
   mouseTarget.current=pointerCoordinates(event);
 };
 const pointerRelease=()=>{held.current=false;mouseActive.current=false;};
 const pointerLeave=()=>{if(id==='neon-breaker')mouseTarget.current=null;};
 return <main className="arcade-play" data-testid="arcade-game-screen" data-game-id={id}>
  <header className="arcade-play-header">
   <div className="arcade-play-heading">
    <button type="button" className="arcade-back" onClick={onBack}>{t.back}</button>
    <span className="arcade-eyebrow">THE MIDNIGHT CLUB · ARCADE</span>
    <h1>{game.name}</h1>
   </div>
   <div className="arcade-play-stats" aria-live="off">
    <div><small>{t.score}</small><strong data-testid="arcade-score">{score}</strong></div>
    <div><small>{t.plays}</small><strong data-testid="arcade-runs">{record.runs}</strong></div>
    <div><small>{t.best}</small><strong data-testid="arcade-best">{record.best}</strong></div>
    <div><small>{t.time}</small><strong>{formatTime(elapsed)}</strong></div>
   </div>
   <div className="arcade-play-actions">
    <button type="button" onClick={()=>setSoundEnabled(v=>!v)}
     aria-pressed={soundEnabled}>{soundEnabled?t.soundOn:t.soundOff}</button>
    <button type="button" onClick={()=>phase==='playing'?pause():phase==='paused'?resume():start()}>
     {phase==='playing'?t.pause:phase==='paused'?t.resume:t.restart}</button>
   </div>
  </header>
  <div className="arcade-play-stage">
   <canvas ref={canvasRef} width={960} height={540} className="arcade-play-canvas"
    data-testid="arcade-canvas" tabIndex={0} role="application"
    aria-label={game.name+': '+game.controls[language]}
    onContextMenu={e=>e.preventDefault()}
    onPointerDown={pointerDown} onPointerMove={pointerMove} onPointerLeave={pointerLeave}
     onPointerUp={pointerRelease} onPointerCancel={pointerRelease}
    onLostPointerCapture={pointerRelease}/>
   {phase!=='playing'&&<div className="arcade-play-overlay" data-phase={phase}>
    <div className="arcade-overlay-symbol" aria-hidden="true">{game.symbol}</div>
    <p className="arcade-eyebrow">{phase==='ready'?t.ready:phase==='paused'?t.pauseHint:game.name}</p>
    <h2>{phase==='ready'?game.name:phase==='paused'?t.paused:t.over}</h2>
    {phase==='over'&&<p className="arcade-final-score">
     {t.score}: <b>{score}</b> {isBest&&<span>{t.record}</span>}
    </p>}
    <p className="arcade-overlay-instruction">{t[instructionsKey[id]]}</p>
    <div className="arcade-overlay-buttons">
     <button type="button" className="arcade-primary-action"
      onClick={()=>phase==='paused'?resume():start()}>
      {phase==='paused'?t.resume:phase==='over'?t.again:t.start}
     </button>
     {phase!=='ready'&&<button type="button" onClick={onBack}>{t.exit}</button>}
    </div>
   </div>}
  </div>
 </main>;
}
