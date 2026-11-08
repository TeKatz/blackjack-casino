import {useEffect,useState} from 'react';
import type {LobbyUpdatesSnapshot} from './updatesBridge';

export function ModernUpdates({state,language,onClose,onChannel,onCheck,onInstall}:{
  state:LobbyUpdatesSnapshot;language:'de'|'en';
  onClose:()=>void;onChannel:(value:'stable'|'development')=>void;
  onCheck:()=>void;onInstall:()=>void;
}){
  const [confirm,setConfirm]=useState(false);
  const de=language==='de';
  useEffect(()=>{
    const key=(e:KeyboardEvent)=>{
      if(e.key==='Escape'&&!state.busy){e.preventDefault();onClose();}
    };
    document.addEventListener('keydown',key);
    return()=>document.removeEventListener('keydown',key);
  },[onClose,state.busy]);
  return <div className="lobby-updates-backdrop" role="presentation">
    <section className="lobby-updates-panel" role="dialog" aria-modal="true"
      aria-labelledby="lobby-updates-title" data-testid="lobby-updates-panel">
      <header className="lobby-updates-header">
        <div>
          <small>THE MIDNIGHT CLUB</small>
          <h2 id="lobby-updates-title">{de?'Updates verwalten':'Manage updates'}</h2>
          <p>{de?'Installierte Version':'Installed version'}: <b>{state.installedVersion}</b></p>
        </div>
        <button type="button" className="lobby-updates-close"
          aria-label={de?'Updates schließen':'Close updates'} onClick={onClose} disabled={state.busy}>×</button>
      </header>
      <label>{de?'Update-Kanal':'Release channel'}
        <select data-testid="lobby-update-channel" value={state.channel} disabled={state.busy||confirm}
          onChange={e=>{setConfirm(false);onChannel(e.target.value==='development'?'development':'stable');}}>
          <option value="stable">Stable</option><option value="development">Development</option>
        </select>
      </label>
      <div className="lobby-updates-status" role="status" aria-live="polite">
        <strong>{state.updateAvailable?(de?'UPDATE VERFÜGBAR':'UPDATE AVAILABLE'):
          state.busy?(de?'AKTUALISIERUNG LÄUFT':'WORKING'):
            (de?'UPDATE-STATUS':'UPDATE STATUS')}</strong>
        <span>{state.status}</span>
      </div>
      <div className="lobby-updates-actions">
        <button type="button" data-testid="lobby-update-check"
          disabled={!state.canCheck||confirm} onClick={onCheck}>
          {de?'Auf Updates prüfen':'Check for updates'}
        </button>
        <button type="button" className="install" data-testid="lobby-update-install"
          disabled={!state.canInstall||confirm} onClick={()=>setConfirm(true)}>
          {de?'Update installieren':'Install update'}
        </button>
      </div>
      {confirm&&<div className="lobby-updates-confirm" role="group" aria-label={de?'Update bestätigen':'Confirm update'}>
        <p>{de?'Das verifizierte Update wird heruntergeladen. Anschließend startet die Anwendung neu. Fortfahren?':
          'Download and verify this update, then restart the application?'} </p>
        <div><button type="button" onClick={()=>setConfirm(false)}>{de?'Abbrechen':'Cancel'}</button>
          <button type="button" className="confirm" data-testid="lobby-update-confirm"
            onClick={()=>{setConfirm(false);onInstall();}}>
            {de?'Installieren':'Install'}
          </button></div>
      </div>}
      {state.busy&&<small className="lobby-updates-progress">
        {de?'Bitte die Anwendung während des Updates geöffnet lassen.':'Keep the application open during the update.'}
      </small>}
    </section>
  </div>;
}
