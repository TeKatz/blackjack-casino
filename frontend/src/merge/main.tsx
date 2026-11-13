import {finishModeBoot} from '../boot/finishModeBoot';
import {useCallback,useEffect,useMemo,useRef,useState} from 'react';
import {createRoot} from 'react-dom/client';
import {isMergeSnapshot,listenMerge,sendMergeIntent} from './bridge';
import type {MergeDefinition,MergeIntent,MergeItem,MergeSnapshot} from './bridge';
import {MergeItemArt} from './ItemArt';
import {RestorationScene} from './RestorationScene';
import {MergeAmbient} from './Ambient';
import './merge.css';

const dictionary={
 en:{space:'FREE CELLS',restoration:'RENOVATION POINTS',chapter:'RESTORATION AREA',
  sound:'Sound effects',motion:'Animations',settings:'SETTINGS',level:'PLAYER LEVEL',
  tier:'TIER',record:'LIFETIME MERGES',board:'RESTORATION WORKBENCH',materials:'MATERIALS',
  completed:'AREA COMPLETE',unlocked:'UNLOCKED',locked:'LOCKED',waiting:'NEED ITEMS',
  next:'NEXT UPGRADE',area:'AREA',earned:'PLAYER XP',season:'SEASON XP',
  saved:'AUTOSAVED · NO ENERGY · NO COOLDOWNS',requirements:'REQUIRED SUPPLIES',
  estate:'CASINO RESTORATION ROADMAP',returnCurrent:'CURRENT ROOM',preview:'VIEWING FINISHED ROOM',
  expand:'VIEW FULL PANORAMA',shrink:'CLOSE PANORAMA',
  tierOne:'TIER I · FREE',unlockedAt:'UNLOCK AT',fullyRestored:'FULLY RESTORED',
  order:'ORDER',remaining:'TO GO',help:'Select an item, then a free tile to move or an identical tile to merge.',
  empty:'Empty space', selected:'SELECTED',deselect:'Click again to deselect',
  blocked:'Another operation is being saved.',restoring:'Restoring your workshop…',
  saveUncertain:'No save confirmation arrived. Reopen the workshop to safely reload the authoritative board.',
  recover:'Recover view',status:'WORKSHOP STATUS',noOrders:'No active deliveries in this area.',
  category:'RESTORATION SUPPLIES',profile:'PROFILE',xp:'XP',
  hints:'Drag items, or click one and then its destination. ESC returns to the lobby.',
  disk:'All moves are written to your local save before their results appear.',
  placed:'PLACED',merged:'MERGED',moved:'MOVED',milestone:'UPGRADE UNLOCKED',
  soundHint:'Preferences are scoped to Midnight Merge.'},
 de:{space:'FREIE FELDER',restoration:'RENOVIERUNGSPUNKTE',chapter:'RESTAURIERUNGSBEREICH',
  sound:'Spielgeräusche',motion:'Animationen',settings:'EINSTELLUNGEN',level:'SPIELERLEVEL',
  tier:'STUFE',record:'KOMBINATIONEN GESAMT',board:'RESTAURIERUNGSWERKBANK',materials:'MATERIALIEN',
  completed:'BEREICH ABGESCHLOSSEN',unlocked:'FREIGESCHALTET',locked:'GESPERRT',waiting:'ITEMS FEHLEN',
  next:'NÄCHSTES UPGRADE',area:'BEREICH',earned:'SPIELER-XP',season:'SAISON-XP',
  saved:'AUTOMATISCH GESPEICHERT · KEINE ENERGIE · KEINE WARTEZEIT',
  estate:'AUSBAUPLAN DES CASINOS',returnCurrent:'AKTUELLER RAUM',preview:'ABGESCHLOSSENER RAUM',
  expand:'PANORAMA ANSEHEN',shrink:'PANORAMA SCHLIESSEN',
  tierOne:'STUFE I · GRATIS',unlockedAt:'FREI AB',fullyRestored:'VOLLSTÄNDIG RESTAURIERT',
  requirements:'BENÖTIGTE VORRÄTE',order:'AUFTRAG',remaining:'NOCH',
  help:'Wähle ein Item und klicke dann auf ein freies Feld oder auf ein gleiches Item.',
  empty:'Freies Feld',selected:'AUSGEWÄHLT',deselect:'Zum Abwählen erneut klicken',
  blocked:'Eine Aktion wird noch gespeichert.',restoring:'Werkbank wird wiederhergestellt…',
  saveUncertain:'Keine Speicherbestätigung. Öffne die Werkbank erneut, um den C#-Spielstand sicher zu laden.',
  recover:'Ansicht wiederherstellen',status:'WERKSTATTSTATUS',noOrders:'Keine offenen Lieferungen in diesem Bereich.',
  category:'RESTAURIERUNGSVORRÄTE',profile:'PROFIL',xp:'XP',
  hints:'Ziehe Items, oder wähle erst eines und dann das Zielfeld. ESC führt zur Lobby.',
  disk:'Jede Aktion wird vor der Anzeige des Ergebnisses lokal gespeichert.',
  placed:'PLATZIERT',merged:'KOMBINIERT',moved:'VERSCHOBEN',milestone:'UPGRADE FREIGESCHALTET',
  soundHint:'Die Einstellungen gelten nur für Midnight Merge.'},
};
function App(){
 const [snap,setSnap]=useState<MergeSnapshot|null>(null);
 const [selected,setSelected]=useState<string|null>(null);
 const [pending,setPending]=useState<string|null>(null);
 const [uncertain,setUncertain]=useState(false);
 const [error,setError]=useState<string|null>(null);
 const [settings,setSettings]=useState(false);
 const [animCell,setAnimCell]=useState(-1);
 const [celebration,setCelebration]=useState(false);
 const [previewArea,setPreviewArea]=useState<string|null>(null);
 const [cinematic,setCinematic]=useState(false);
 const activeSnap=useRef<MergeSnapshot|null>(null),activePending=useRef<string|null>(null);
 const lastRevision=useRef(-1);
 const selectedRef=useRef<string|null>(null);
 const soundNotice=useRef<ReturnType<typeof setTimeout>|null>(null);
 const celebrateTimer=useRef<ReturnType<typeof setTimeout>|null>(null);
 const send=useCallback((command:MergeIntent)=>{
   const state=activeSnap.current;
   if(!state||activePending.current||uncertain)return;
   const request=sendMergeIntent(state,command);
   activePending.current=request;setPending(request);setError(null);
 },[uncertain]);
 useEffect(()=>{
   const detach=listenMerge(message=>{
     if(message.type==='receipt'){
       if(message.data.requestId!==activePending.current)return;
       activePending.current=null;setPending(null);
       if(!message.data.success)setError(message.data.message);
       return;
     }
     const next=message.data;
     if(activeSnap.current?.session===next.session
       &&next.revision<activeSnap.current.revision)return;
     if(lastRevision.current>=0&&next.revision>lastRevision.current&&next.state.lastAction){
       const result=next.state.lastAction;
       setAnimCell(result.position);
       if(result.milestones?.length){setCelebration(true);setPreviewArea(null);}
       if(soundNotice.current)clearTimeout(soundNotice.current);
       if(celebrateTimer.current)clearTimeout(celebrateTimer.current);
       soundNotice.current=setTimeout(()=>setAnimCell(-1),820);
       celebrateTimer.current=setTimeout(()=>setCelebration(false),2300);
     }
     lastRevision.current=next.revision;
     activeSnap.current=next;setSnap(next);
     if(selectedRef.current&&!next.state.items.some(x=>x.id===selectedRef.current)){
       selectedRef.current=null;setSelected(null);
     }
   });
   window.chrome?.webview?.postMessage({type:'midnight-merge.ready'});
   return()=>{
     detach();if(soundNotice.current)clearTimeout(soundNotice.current);
     if(celebrateTimer.current)clearTimeout(celebrateTimer.current);
   };
 },[]);
 useEffect(()=>{
   if(snap){finishModeBoot();window.chrome?.webview?.postMessage({type:'midnight-merge.ui-ready'});}
 },[!!snap]);
 useEffect(()=>{
   if(!pending)return;
   const t=setTimeout(()=>{
     setUncertain(true);setError(dictionary[activeSnap.current?.state.language??'en'].saveUncertain);
   },12000);
   return()=>clearTimeout(t);
 },[pending]);
 const close=useCallback(()=>{
   if(uncertain){window.chrome?.webview?.postMessage({type:'midnight-merge.recover'});return;}
   if(!activePending.current)window.chrome?.webview?.postMessage({type:'midnight-merge.close'});
 },[uncertain]);
 useEffect(()=>{
   const key=(event:KeyboardEvent)=>{
     if(event.key!=='Escape')return;
     event.preventDefault();
     if(cinematic){setCinematic(false);return;}
     if(settings){setSettings(false);return;}
     if(selectedRef.current){selectedRef.current=null;setSelected(null);return;}
     close();
   };
   document.addEventListener('keydown',key);
   return()=>document.removeEventListener('keydown',key);
 },[settings,cinematic,close]);
 const onCell=useCallback((position:number,item:MergeItem|null)=>{
   if(activePending.current||uncertain)return;
   const current=selectedRef.current;
   if(current){
     if(item?.id===current){selectedRef.current=null;setSelected(null);return;}
     selectedRef.current=null;setSelected(null);
     send({action:'drop',itemId:current,target:position});
   }else if(item){
     selectedRef.current=item.id;setSelected(item.id);
   }
 },[send,uncertain]);
 const onDrag=useCallback((event:React.DragEvent<HTMLElement>,item:MergeItem)=>{
   if(activePending.current||uncertain){event.preventDefault();return;}
   selectedRef.current=item.id;setSelected(item.id);
   event.dataTransfer.effectAllowed='move';
   event.dataTransfer.setData('text/plain',item.id);
 },[uncertain]);
 const onDrop=useCallback((event:React.DragEvent<HTMLElement>,position:number)=>{
   event.preventDefault();
   if(activePending.current||uncertain)return;
   const item=event.dataTransfer.getData('text/plain');
   if(!/^[0-9a-f-]{36}$/i.test(item))return;
   selectedRef.current=null;setSelected(null);
   send({action:'drop',itemId:item,target:position});
 },[send,uncertain]);
 const state=snap?.state;
 const byPosition=useMemo(()=>new Map(state?.items.map(item=>[item.position,item])??[]),[state?.items]);
 const definitions=useMemo(()=>new Map(state?.definitions.map(d=>[d.id,d])??[]),[state?.definitions]);
 const selectedItem=state?.items.find(x=>x.id===selected);
 const selectedDef=selectedItem&&definitions.get(selectedItem.definitionId);
 const labels=state?.labels;
 const lang=state?.language??'en',t=dictionary[lang];
 // In-flight requests are already guarded by send, onCell and onDrop.
 // Disabling every button during a save caused the global disabled opacity flicker.
 const disabled=uncertain;
 const available=state?.capacity!==undefined?state.capacity-state.items.length:42;
 const displayedRoom=state
   ?state.restoration.areas.find(area=>area.areaId===previewArea)??state.restoration
   :undefined;
 const viewingPrevious=!!state&&displayedRoom?.areaId!==state.restoration.areaId;
 const next=displayedRoom?.next;
 const canMerge=(item:MergeItem|null):boolean=>
   !!item&&!!selectedItem&&selectedItem.id!==item.id&&selectedDef?.id===item.definitionId
   &&selectedDef.mergeResult!==null;
 return <div className="merge-app" data-testid="midnight-merge-app"
   data-revision={snap?.revision??-1} data-language={lang} lang={lang}>
   <MergeAmbient enabled={!!state?.animationsEnabled&&!window.matchMedia?.('(prefers-reduced-motion: reduce)').matches}/>
   <header className="merge-header">
     <div className="merge-brand">
       <div className="merge-eyebrow">{labels?.eyebrow??'THE MIDNIGHT CLUB · RESTORATION'}</div>
       <h1>MIDNIGHT <em>MERGE</em></h1>
       <small>{t.saved}</small>
     </div>
     <div className="merge-top-stats" aria-label={lang==='de'?'Spielfortschritt':'Game progress'}>
       <div className="merge-stat-card stat-renovation">
         <span className="merge-stat-emblem" aria-hidden="true">
           <svg viewBox="0 0 32 32"><path d="M5 25V13L16 5l11 8v12H5ZM12 25V15h8v10M8 13h16"/></svg>
         </span>
         <span className="merge-stat-content">
           <small>{t.restoration}</small>
           <strong data-testid="merge-points">{(state?.renovationPoints??0).toLocaleString(lang)}</strong>
           <span className="merge-stat-meter"><i style={{width:((state?.restoration.upgradeProgress??0)*100)+'%'}}/></span>
         </span>
       </div>
       <div className="merge-stat-card stat-level">
         <span className="merge-stat-emblem" aria-hidden="true">
           <svg viewBox="0 0 32 32"><path d="M16 3l4 9 10 1-7.5 7 2 10-8.5-5-9 5 2-10L2 13l10-1 4-9Z"/></svg>
         </span>
         <span className="merge-stat-content">
           <small>{t.level}</small><strong>{state?.profile?.level??'—'}</strong>
           <span className="merge-stat-meter"><i style={{width:(state?.profile?.nextLevelExperience
             ?Math.min(100,100*state.profile.levelExperience/state.profile.nextLevelExperience):0)+'%'}}/></span>
         </span>
       </div>
       <div className="merge-stat-card stat-merges">
         <span className="merge-stat-emblem" aria-hidden="true">
           <svg viewBox="0 0 32 32"><path d="M7 9a12 12 0 0 1 19 4M26 6v7h-7M25 23A12 12 0 0 1 6 19M6 26v-7h7"/></svg>
         </span>
         <span className="merge-stat-content">
           <small>{t.record}</small><strong>{(state?.totalMerges??0).toLocaleString(lang)}</strong>
           <span className="merge-stat-merges-line" aria-hidden="true">◆ ◇ ◆</span>
         </span>
       </div>
     </div>
     <div className="merge-header-tools">
       <details className="merge-header-help" data-testid="merge-how-to-play">
         <summary>{labels?.howToPlay??(lang==='de'?'Spielhilfe':'How to play')}</summary>
         <p>{t.help}</p>
       </details>
       <div className="merge-settings">
         <button type="button" data-testid="merge-settings" aria-expanded={settings}
           onClick={()=>setSettings(x=>!x)}>⚙ {t.settings}</button>
         {settings&&<div className="merge-settings-panel" data-testid="merge-settings-panel">
           <label><input type="checkbox" checked={state?.soundEnabled??true} disabled={disabled}
             onChange={e=>send({action:'sound.set',value:e.target.checked?'true':'false'})}/>{t.sound}</label>
           <label><input type="checkbox" checked={state?.animationsEnabled??true} disabled={disabled}
             onChange={e=>send({action:'animations.set',value:e.target.checked?'true':'false'})}/>{t.motion}</label>
           <small>{t.soundHint}</small>
         </div>}
       </div>
     </div>
     <button className="merge-back" data-testid="merge-back" disabled={disabled}
       onClick={close}>{labels?.back??'← Casino lobby'}</button>
   </header>
   {!snap||!state||!isMergeSnapshot(snap)?<div className="merge-loading">
     <div className="merge-loading-symbol">◈</div><strong>{t.restoring}</strong></div>:
   <div className="merge-scroll" aria-busy={!!pending}>
     <section className={'merge-restoration'+(celebration?' celebrating':'')}
       aria-label={t.estate}>
       <div className="merge-room-head">
         <div>
           <small>{t.chapter} · {displayedRoom!.areaIndex}/{state.restoration.areaCount}
             {viewingPrevious?' · '+t.preview:''}</small>
           <h2 data-testid="merge-area">{displayedRoom!.areaName}</h2>
         </div>
         <div className="merge-room-stage">
           <small>{displayedRoom!.current.id.toUpperCase()}</small>
           <strong>{displayedRoom!.current.name}</strong>
         </div>
         {viewingPrevious&&<button type="button" className="merge-room-return"
           onClick={()=>setPreviewArea(null)}>{t.returnCurrent}</button>}
       </div>
       <div className={'merge-scene'+(cinematic?' is-expanded':'')+(displayedRoom!.next===null?' fully-restored':'')+(!state.animationsEnabled?' motion-off':'')}
         role={cinematic?'dialog':undefined} aria-modal={cinematic?true:undefined}
         aria-label={cinematic?displayedRoom!.areaName:undefined}
         data-testid="merge-panorama-container">
         {cinematic&&<div className="merge-cinema-caption">
           <small>{t.chapter} {displayedRoom!.areaIndex} / {state.restoration.areaCount}</small>
           <strong>{displayedRoom!.areaName}</strong><span>{displayedRoom!.current.name}</span>
         </div>}
         <RestorationScene stage={displayedRoom!} animated={state.animationsEnabled} fit={cinematic?'contain':'crop'}/>
         {displayedRoom!.next===null&&<div className="merge-complete-seal" aria-label={t.fullyRestored}>
           <span aria-hidden="true">✦</span> {t.fullyRestored}</div>}
         {celebration&&!viewingPrevious&&<div className="merge-unlock" aria-live="polite">
           <span>✦</span>{t.milestone}<span>✦</span></div>}
         <button type="button" className="merge-scene-expand" data-testid="merge-scene-expand"
           aria-label={cinematic?t.shrink:t.expand}
           onClick={()=>setCinematic(value=>!value)}>
           {cinematic?t.shrink:t.expand} <span aria-hidden="true">{cinematic?'×':'⤢'}</span>
         </button>
       </div>
       <div className="merge-milestone-bar">
         <div className="merge-next"><div><small>{t.next}</small>
           <strong data-testid="merge-next-upgrade">{next
             ?lang==='de'? 'Noch '+displayedRoom!.pointsUntilNext.toLocaleString(lang)+' Punkte bis '+next.name
               :displayedRoom!.pointsUntilNext.toLocaleString(lang)+' points until '+next.name
             :labels?.areaComplete}</strong></div>
           <small>{next?state.renovationPoints.toLocaleString(lang)+' / '+next.points.toLocaleString(lang):t.completed}</small></div>
         <div className="merge-progress" role="progressbar" aria-label={t.next}
           aria-valuenow={Math.round(100*displayedRoom!.upgradeProgress)}
           aria-valuemin={0} aria-valuemax={100}>
           <span style={{width:displayedRoom!.upgradeProgress*100+'%'}}/></div>
         <div className="merge-milestone-labels">{displayedRoom!.milestones.map(ms=>
           <span key={ms.id} className={ms.unlocked?'unlocked':''}>{ms.name}</span>)}</div>
       </div>
       <nav className="merge-estate-roadmap" aria-label={t.estate} data-testid="merge-estate-roadmap">
         {state.restoration.areas.map(room=>{
           const unlocked=state.renovationPoints>=room.milestones[0].points;
           const completed=state.renovationPoints>=room.milestones[room.milestones.length-1].points;
           const showing=room.areaId===displayedRoom!.areaId;
           return <button type="button" key={room.areaId} disabled={!unlocked}
             data-testid={'merge-room-'+room.areaId} aria-pressed={showing}
             className={'merge-estate-stop'+(showing?' active':'')+
               (completed?' complete':'')+(unlocked?' unlocked':' locked')}
             onClick={()=>setPreviewArea(room.areaId===state.restoration.areaId?null:room.areaId)}>
             <span className="merge-estate-number">{completed?'✓':String(room.areaIndex).padStart(2,'0')}</span>
             <span className="merge-estate-name">{room.areaName}</span>
           </button>;
         })}
       </nav>
     </section>
     <div className="merge-workspace">
       <section className="merge-board-panel">
         <div className="merge-section-heading"><h2><span>✦</span> {t.board}</h2>
          <strong>{available} / 42 {t.space}</strong></div>
         <div className="merge-board-section">
           <div className="merge-board" data-testid="merge-board" aria-label={t.board}>
             {Array.from({length:42},(_,position)=>{
               const item=byPosition.get(position)??null;
               const definition=item?definitions.get(item.definitionId):null;
               const own=!!item&&item.id===selected;
               const target=!!selected&&(!item||canMerge(item));
               return <button key={position} type="button" data-testid={'merge-cell-'+position}
                 data-definition={item?.definitionId??''} data-position={position}
                 disabled={disabled}
                 className={'merge-cell'+(item?' filled':' empty')+(own?' selected':'')
                   +(target?' merge-target':'')+(canMerge(item)?' compatible':'')
                   +(animCell===position&&state.animationsEnabled?' created':'')}
                 aria-label={definition?definition.name+' · '+(position+1)+'/42':
                   t.empty+' · '+(position+1)+'/42'}
                 draggable={!!item&&!disabled}
                 onDragStart={event=>item&&onDrag(event,item)}
                 onDragOver={event=>event.preventDefault()}
                 onDrop={event=>onDrop(event,position)}
                 onClick={()=>onCell(position,item)}>
                 {definition&&<MergeItemArt definition={definition}/>}
                 {definition&&<span className="merge-tier">{definition.tier}</span>}
               </button>;
             })}
           </div>
           <div className="merge-supply">
             <h3>{labels?.producer}</h3>
             {state.producers.map(producer=><button key={producer.id}
               data-testid={'merge-producer-'+producer.id}
               className={'merge-producer'+(producer.unlocked?' unlocked':' locked')}
               disabled={disabled||!producer.available}
               onClick={()=>send({action:'spawn',producerId:producer.id})}>
               <span className="merge-producer-art"><MergeItemArt
                 definition={definitions.get(producer.outputDefinitionId)!} small/></span>
               <span className="merge-producer-text"><strong>{producer.name}</strong>
                 <small>{producer.unlocked?t.tierOne:
                  t.unlockedAt+' '+producer.unlockPoints.toLocaleString(lang)}</small></span>
               <span className="merge-producer-icon">{producer.unlocked?'+':'⌑'}</span>
             </button>)}
             {selectedDef&&<div className="merge-selection" data-testid="merge-selected">
               <span>{t.selected}</span><strong>{selectedDef.name}</strong><small>{t.deselect}</small>
             </div>}
           </div>
         </div>
       </section>
       <aside className="merge-orders-panel">
         <div className="merge-section-heading"><h2><span>◇</span> {labels?.orders}</h2>
          <small>{state.restoration.areaName}</small></div>
         <div className="merge-orders" data-testid="merge-orders">
           {state.orders.filter(order=>order.visible).map(order=><div key={order.id}
             className={'merge-order'+(order.available?' ready':'')}
             data-definition={order.definitionId} data-testid={'merge-order-'+order.definitionId}>
             <div className="merge-order-header"><small>{t.order}</small>
               <strong>+{order.reward.toLocaleString(lang)} ◈</strong></div>
             <div className="merge-requirements">
               {order.requirements.map(req=>{
                 const def=definitions.get(req.definitionId)!;
                 return <div key={req.definitionId} className={'merge-requirement'+(req.available>=req.count?' ready':'')}>
                   <span className="merge-required-art"><MergeItemArt definition={def} small/></span>
                   <span className="merge-required-name">{def.name}</span>
                   <strong>{Math.min(req.available,req.count)} / {req.count}</strong>
                 </div>;
               })}
             </div>
             <button data-testid={'merge-deliver-'+order.definitionId}
               disabled={disabled||!order.available}
               onClick={()=>send({action:'deliver',orderId:order.id})}>
               {order.available?labels?.deliver:labels?.waiting}</button>
           </div>)}
           {!state.orders.some(o=>o.visible)&&<p className="merge-no-orders">{t.noOrders}</p>}
         </div>
       </aside>
     </div>
     {(error||state.progressionSaveFailed)&&<div className="merge-save-alert" role="alert"
       data-testid="merge-save-alert">
       {error??labels?.progressionSaveFailed}
     </div>}
   </div>}
  </div>;
}
createRoot(document.getElementById('root')!).render(<App/>);
