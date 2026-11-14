import {useEffect,useLayoutEffect,useRef} from 'react';
import {matchesLobbyFilter} from './lobbyTiles';

/** Pure filter partition shared by the transition planner and regression tests. */
export function filterMotionPhases(ids:readonly string[],previous:string,next:string) {
  return {
    leaving:ids.filter(id=>matchesLobbyFilter(id,previous)&&!matchesLobbyFilter(id,next)),
    entering:ids.filter(id=>!matchesLobbyFilter(id,previous)&&matchesLobbyFilter(id,next)),
    staying:ids.filter(id=>matchesLobbyFilter(id,previous)&&matchesLobbyFilter(id,next)),
  };
}

/**
 * The actual game articles remain keyed and mounted. Animate their DOM geometry,
 * never clone a Pixi texture or reconstruct a canvas during category changes.
 */
export function useLobbyFilterMotion<T extends string>(
  filter:T,setFilter:(next:T)=>void,enabled:boolean,
) {
  const gridRef=useRef<HTMLElement>(null);
  const before=useRef<Map<string,DOMRect>|null>(null);
  const timer=useRef<number|null>(null);
  const running=useRef<Animation[]>([]);
  const cancel=()=>{
    if(timer.current!==null){clearTimeout(timer.current);timer.current=null;}
    for(const animation of running.current)animation.cancel();
    running.current=[];
    before.current=null;
  };
  const request=(next:T)=>{
    cancel();
    if(next===filter)return;
    const grid=gridRef.current;
    if(!enabled||!grid||typeof Element.prototype.animate!=='function'){
      setFilter(next);return;
    }
    const nodes=[...grid.querySelectorAll<HTMLElement>('.lobby-game[data-lobby-id]')];
    const active=nodes.filter(node=>!node.hidden);
    before.current=new Map(active.map(node=>[node.dataset.lobbyId||'',node.getBoundingClientRect()]));
    const phases=filterMotionPhases(nodes.map(node=>node.dataset.lobbyId||''),filter,next);
    for(const node of active)if(phases.leaving.includes(node.dataset.lobbyId||'')){
      running.current.push(node.animate([{opacity:1},{opacity:0}],{
        duration:105,easing:'ease-out',fill:'forwards',
      }));
    }
    if(phases.leaving.length){
      timer.current=window.setTimeout(()=>{
        timer.current=null;
        // Keep the outgoing artwork faded until React commits hidden=true.
        setFilter(next);
      },105);
    }else setFilter(next);
  };
  useLayoutEffect(()=>{
    const boxes=before.current;
    before.current=null;
    // This runs after React hid departing cards but before the next paint:
    // cancelling fill:forwards here cannot flash a departing Pixi sprite.
    for(const animation of running.current)animation.cancel();
    running.current=[];
    const grid=gridRef.current;
    if(!boxes||!grid||!enabled)return;
    const elements=[...grid.querySelectorAll<HTMLElement>('.lobby-game[data-lobby-id]')];
    for(const node of elements){
      if(node.hidden)continue;
      const old=boxes.get(node.dataset.lobbyId||'');
      const current=node.getBoundingClientRect();
      if(old) {
        const dx=old.left-current.left,dy=old.top-current.top;
        if(Math.abs(dx)+Math.abs(dy)>.6)running.current.push(node.animate([
          {translate:dx.toFixed(2)+'px '+dy.toFixed(2)+'px'},
          {translate:'0px 0px'},
        ],{duration:290,easing:'cubic-bezier(.2,.8,.2,1)'}));
      }else running.current.push(node.animate([
        {opacity:0,translate:'0px 7px'},
        {opacity:1,translate:'0px 0px'},
      ],{duration:230,easing:'ease-out'}));
    }
  },[filter,enabled]);
  useEffect(()=>{if(!enabled)cancel();},[enabled]);
  useEffect(()=>()=>cancel(),[]);
  return {gridRef,requestFilter:request,cancelFilterMotion:cancel};
}
