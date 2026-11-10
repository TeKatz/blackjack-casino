import {useEffect,useState} from 'react';
import {createRoot} from 'react-dom/client';
import {RouletteTable} from '../lobby/RouletteTable';
import {embeddedLobby} from '../lobby/lobbyBridge';
type Snapshot={bankroll:string;bankrollValue:number;language:'de'|'en';motion:boolean;sound:boolean};
function App(){
 const [snapshot,setSnapshot]=useState<Snapshot|null>(embeddedLobby?null:{bankroll:'$100,000',bankrollValue:100000,language:'en',motion:true,sound:true});
 useEffect(()=>{
  const host=window.chrome?.webview;if(!host)return;
  const listener=(e:MessageEvent)=>{const m=e.data,d=m?.data;if(m?.type==='roulette.snapshot'&&typeof d?.bankroll==='string'&&Number.isFinite(d.bankrollValue)&&(d.language==='de'||d.language==='en')&&typeof d.motion==='boolean'&&typeof d.sound==='boolean')setSnapshot(d);};
  host.addEventListener('message',listener);host.postMessage({type:'roulette.ready'});return()=>host.removeEventListener('message',listener);
 },[]);
 useEffect(()=>{if(snapshot){document.documentElement.lang=snapshot.language;window.chrome?.webview?.postMessage({type:'roulette.ui-ready'});}},[snapshot]);
 const close=()=>{if(embeddedLobby)window.chrome?.webview?.postMessage({type:'roulette.close'});else window.location.href='./lobby.html';};
 return <div className="roulette-shell"><div className="roulette-masthead"><span className="roulette-brand-mark">◉</span><div><strong>THE MIDNIGHT CLUB</strong><small>GRAND ROULETTE · HIGH LIMIT</small></div><span className="roulette-masthead-right">EUROPEAN TABLE &nbsp; • &nbsp; 37 POCKETS</span></div>{snapshot?<RouletteTable {...snapshot} motion={snapshot.motion&&!window.matchMedia('(prefers-reduced-motion: reduce)').matches} onBack={close}/>:<div className="roulette-loading">Preparing your table…</div>}</div>;
}
createRoot(document.getElementById('root')!).render(<App/>);
