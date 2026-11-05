/** Update actions are hints to the native update manager, never download URLs. */
export type LobbyUpdatesSnapshot={
  installedVersion:string;
  channel:'stable'|'development';
  status:string;
  busy:boolean;
  canCheck:boolean;
  canInstall:boolean;
  updateAvailable:boolean;
};
const record=(value:unknown):value is Record<string,unknown>=>
  value!==null&&typeof value==='object'&&!Array.isArray(value);
export function isLobbyUpdatesSnapshot(value:unknown):value is LobbyUpdatesSnapshot{
  if(!record(value))return false;
  return typeof value.installedVersion==='string'&&value.installedVersion.length<=80
    &&(value.channel==='stable'||value.channel==='development')
    &&typeof value.status==='string'&&value.status.length<=2048
    &&['busy','canCheck','canInstall','updateAvailable'].every(k=>typeof value[k]==='boolean')
    &&(!value.canInstall||value.updateAvailable&&!value.busy)
    &&(!value.busy||!value.canCheck);
}
export const fallbackUpdates:LobbyUpdatesSnapshot={
  installedVersion:'—',channel:'stable',status:'Not checked yet',
  busy:false,canCheck:false,canInstall:false,updateAvailable:false,
};
export function onUpdateSnapshot(onSnapshot:(value:LobbyUpdatesSnapshot)=>void):()=>void{
  const host=window.chrome?.webview;
  if(!host)return()=>{};
  const handler=(event:MessageEvent)=>{
    const data:unknown=event.data;
    if(record(data)&&data.type==='lobby.updates.snapshot'&&isLobbyUpdatesSnapshot(data.data))
      onSnapshot(data.data);
  };
  host.addEventListener('message',handler);
  return()=>host.removeEventListener('message',handler);
}
