import {useEffect,useRef} from 'react';

/** Layered hero remains a native React surface; only its decorative image moves. */
export function LobbyHero({de,motionEnabled}:{de:boolean;motionEnabled:boolean}) {
  const host=useRef<HTMLDivElement>(null);
  useEffect(()=>{
    const element=host.current;
    if(!element)return;
    let frame=0;
    let x=0,y=0,targetX=0,targetY=0;
    const paint=()=>{
      frame=0;
      x+=(targetX-x)*.16;
      y+=(targetY-y)*.16;
      element.style.setProperty('--hero-back-x',(-x*.35).toFixed(2)+'px');
      element.style.setProperty('--hero-back-y',(-y*.25).toFixed(2)+'px');
      element.style.setProperty('--hero-front-x',(x*.65).toFixed(2)+'px');
      element.style.setProperty('--hero-front-y',(y*.42).toFixed(2)+'px');
      element.style.setProperty('--hero-light-x',(x*.95).toFixed(2)+'px');
      if(Math.abs(targetX-x)+Math.abs(targetY-y)>.08)frame=requestAnimationFrame(paint);
    };
    const schedule=()=>{if(!frame)frame=requestAnimationFrame(paint);};
    const move=(event:PointerEvent)=>{
      if(!motionEnabled||event.pointerType==='touch')return;
      const bounds=element.getBoundingClientRect();
      targetX=((event.clientX-bounds.left)/Math.max(1,bounds.width)-.5)*12;
      targetY=((event.clientY-bounds.top)/Math.max(1,bounds.height)-.5)*8;
      schedule();
    };
    const reset=()=>{targetX=0;targetY=0;schedule();};
    element.addEventListener('pointermove',move);
    element.addEventListener('pointerleave',reset);
    if(!motionEnabled)reset();
    return ()=>{
      element.removeEventListener('pointermove',move);
      element.removeEventListener('pointerleave',reset);
      cancelAnimationFrame(frame);
      element.style.removeProperty('--hero-back-x');
      element.style.removeProperty('--hero-back-y');
      element.style.removeProperty('--hero-front-x');
      element.style.removeProperty('--hero-front-y');
      element.style.removeProperty('--hero-light-x');
    };
  },[motionEnabled]);
  return <div ref={host} className="lobby-hero">
    <div className="lobby-hero-picture" aria-hidden="true"/>
    <div className="lobby-hero-foreground" aria-hidden="true"/>
    <div className="lobby-hero-light" aria-hidden="true"/>
    <div className="lobby-hero-sheen" aria-hidden="true"/>
    <div className="hero-copy">
      <span className="lobby-eyebrow">— &nbsp; THE MIDNIGHT CLUB &nbsp; —</span>
      <h1>{de?'Eine Nacht wie keine andere.':'A different kind of night.'}</h1>
      <p>{de?'Wähle deinen Tisch. Finde deinen Rhythmus. Fühl dich zu Hause.':
        'Choose your table. Find your rhythm. Make yourself at home.'}</p>
    </div>
  </div>;
}
