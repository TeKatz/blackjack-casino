import {useEffect,useRef,useState} from 'react';
import {createRoot} from 'react-dom/client';
import './exit.css';

type Language='en'|'de';
type HostMessage={type:string;language?:string};
const post=(type:string)=>window.chrome?.webview?.postMessage({type});
function ExitConfirmation(){
 const [language,setLanguage]=useState<Language>('en');
 const [open,setOpen]=useState(false);
 const cancelRef=useRef<HTMLButtonElement>(null);
 useEffect(()=>{
  const host=window.chrome?.webview;
  const handler=(event:MessageEvent)=>{
   const data:unknown=event.data;
   if(!data||typeof data!=='object')return;
   const message=data as HostMessage;
   if(message.type!=='casino.exit.show')return;
   setLanguage(message.language==='de'?'de':'en');
   setOpen(true);
  };
  host?.addEventListener('message',handler);
  post('casino.exit.ready');
  return()=>host?.removeEventListener('message',handler);
 },[]);
 useEffect(()=>{
  if(!open)return;
  cancelRef.current?.focus();
  const key=(event:KeyboardEvent)=>{
   if(event.key==='Escape'){event.preventDefault();post('casino.exit.cancel');setOpen(false);}
   if(event.key==='Tab'){
    const buttons=Array.from(document.querySelectorAll<HTMLButtonElement>('.exit-action'));
    const current=buttons.indexOf(document.activeElement as HTMLButtonElement);
    if(event.shiftKey&&current===0){event.preventDefault();buttons.at(-1)?.focus();}
    else if(!event.shiftKey&&current===buttons.length-1){event.preventDefault();buttons[0]?.focus();}
   }
  };
  document.addEventListener('keydown',key);
  return()=>document.removeEventListener('keydown',key);
 },[open]);
 if(!open)return <div className="exit-shell" aria-hidden="true"/>;
 const de=language==='de';
 const choose=(type:'confirm'|'cancel')=>{post('casino.exit.'+type);setOpen(false);};
 return <main className="exit-shell">
  <section className="exit-dialog" role="alertdialog" aria-modal="true"
   aria-labelledby="exit-heading" aria-describedby="exit-description">
    <div className="exit-crest"><span className="exit-crest-mark" aria-hidden="true">✦</span>
      <span>THE MIDNIGHT CLUB</span><small>{de?'SITZUNG BEENDEN':'END SESSION'}</small>
    </div>
    <h1 id="exit-heading">{de?'Den Club verlassen?':'Leaving the club?'}</h1>
    <p id="exit-description">{de
     ?'Möchtest du deine Casino-Sitzung wirklich beenden? Deine bereits gespeicherten Fortschritte bleiben erhalten.'
     :'Are you sure you want to end your casino session? Your previously saved progress will be retained.'}</p>
    <div className="exit-divider" aria-hidden="true"/>
    <div className="exit-actions">
      <button type="button" ref={cancelRef} className="exit-action exit-stay"
        onClick={()=>choose('cancel')}>{de?'Im Casino bleiben':'Stay in the casino'}</button>
      <button type="button" className="exit-action exit-leave"
        onClick={()=>choose('confirm')}>{de?'Casino beenden':'Exit casino'}</button>
    </div>
    <p className="exit-keyboard">{de?'ESC zum Zurückkehren':'ESC to return'}</p>
  </section>
 </main>;
}
createRoot(document.getElementById('root')!).render(<ExitConfirmation/>);
