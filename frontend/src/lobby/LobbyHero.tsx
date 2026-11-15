import {useEffect,useRef} from 'react';
// The native AVIF stays on a stable texture for the lifetime of the banner.

/**
 * The club banner uses three independently authored image plates, not three
 * copies of one photograph. The plates share one coordinate system so their
 * subject boundaries stay aligned even when the banner is cropped.
 */
export function LobbyHero({de,motionEnabled}:{de:boolean;motionEnabled:boolean}) {
  const host=useRef<HTMLDivElement>(null);
  useEffect(()=>{
    const element=host.current;
    if(!element)return;
    let frame=0,x=0,y=0,targetX=0,targetY=0;
    const paint=()=>{
      frame=0;
      x+=(targetX-x)*.16;
      y+=(targetY-y)*.16;
      element.style.setProperty('--hero-back-x',(-x*.12).toFixed(2)+'px');
      element.style.setProperty('--hero-back-y',(-y*.10).toFixed(2)+'px');
      element.style.setProperty('--hero-mid-x',(x*.27).toFixed(2)+'px');
      element.style.setProperty('--hero-mid-y',(y*.20).toFixed(2)+'px');
      element.style.setProperty('--hero-front-x',(x*.56).toFixed(2)+'px');
      element.style.setProperty('--hero-front-y',(y*.38).toFixed(2)+'px');
      element.style.setProperty('--hero-light-x',(x*.45).toFixed(2)+'px');
      if(Math.abs(targetX-x)+Math.abs(targetY-y)>.06)frame=requestAnimationFrame(paint);
    };
    const schedule=()=>{if(!frame)frame=requestAnimationFrame(paint);};
    const move=(event:PointerEvent)=>{
      if(!motionEnabled||event.pointerType==='touch')return;
      const rect=element.getBoundingClientRect();
      targetX=((event.clientX-rect.left)/Math.max(1,rect.width)-.5)*16;
      targetY=((event.clientY-rect.top)/Math.max(1,rect.height)-.5)*12;
      schedule();
    };
    const reset=()=>{targetX=0;targetY=0;schedule();};
    const size=()=>{
      const rect=element.getBoundingClientRect();
      // All plates are 896 × 299. Overscan makes movement safe at any aspect.
      const width=Math.ceil(Math.max(rect.width+34,(rect.height+34)*896/299));
      element.style.setProperty('--hero-art-width',width+'px');
    };
    const observer=new ResizeObserver(size);
    observer.observe(element);
    size();
    element.addEventListener('pointermove',move);
    element.addEventListener('pointerleave',reset);
    if(!motionEnabled)reset();
    return ()=>{
      observer.disconnect();
      element.removeEventListener('pointermove',move);
      element.removeEventListener('pointerleave',reset);
      cancelAnimationFrame(frame);
      for(const name of ['--hero-back-x','--hero-back-y','--hero-mid-x','--hero-mid-y',
        '--hero-front-x','--hero-front-y','--hero-light-x','--hero-art-width'])
        element.style.removeProperty(name);
    };
  },[motionEnabled]);
  useEffect(()=>{
    const element=host.current;
    if(!element)return;
    // Do not display the planes until their shared texture is decoded.
    // The static banner beneath remains visible if the codec is unavailable.
    let live=true;
    const image=new Image();
    image.decoding='async';
    image.src='/lobby/art/hero-parallax.avif';
    void image.decode().then(()=>{
      if(live)element.dataset.artReady='true';
    }).catch(()=>{/* Keep the static fallback without a black replacement frame. */});
    return ()=>{live=false;delete element.dataset.artReady;};
  },[]);
  return <div ref={host} className="lobby-hero">
    <div className="lobby-hero-art" aria-hidden="true">
      <div className="lobby-hero-layer lobby-hero-layer-back"/>
      <div className="lobby-hero-layer lobby-hero-layer-mid"/>
      <div className="lobby-hero-layer lobby-hero-layer-front"/>
      <div className="lobby-hero-light"/>
      <div className="lobby-hero-sheen"/>
    </div>
    <div className="hero-copy">
      <span className="lobby-eyebrow">— &nbsp; THE MIDNIGHT CLUB &nbsp; —</span>
      <h1>{de?'Eine Nacht wie keine andere.':'A different kind of night.'}</h1>
      <p>{de?'Wähle deinen Tisch. Finde deinen Rhythmus. Fühl dich zu Hause.':
        'Choose your table. Find your rhythm. Make yourself at home.'}</p>
    </div>
  </div>;
}
