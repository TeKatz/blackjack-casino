import type {} from '../bridge/protocol';
export type Building = {
  id:string;name:string;description:string;level:number;maxLevel:number;
  unlocked:boolean;visible:boolean;canBuy:boolean;cost:string;rate:string;
  visualTier:number;requirement:string;nextUnlock:string|null;
};
export type Economy = {
  credits:string;rate:string;tapValue:string;clicks:number;prestige:number;
  totalLevels:number;runCredits:string;prestigeRequirement:string;canPrestige:boolean;
  buildings:Building[];
  goal:{id:string|null;title:string;description:string;current:string;target:string;progress:number};
};
export type Reply={requestId:string;success:boolean;message:string;revision:number};
export type EmpireSnapshot={
  version:2;session:string;revision:number;visitId?:number;notice:string|null;
  lastReply:Reply|null;offline:unknown|null;
  state:{language:'de'|'en';animationsEnabled:boolean;soundEnabled:boolean;detached:boolean;economy:Economy};
};
export type Command={action:'tap'|'building.upgrade'|'prestige';id?:string;count?:number};
const record=(x:unknown):x is Record<string,unknown>=>x!==null&&typeof x==='object'&&!Array.isArray(x);
const decimal=(x:unknown):x is string=>typeof x==='string'&&/^\d+(?:\.\d+)?$/.test(x)&&x.length<=60;
const int=(x:unknown)=>typeof x==='number'&&Number.isSafeInteger(x)&&x>=0;
export function isReply(x:unknown):x is Reply{
  return record(x)&&typeof x.requestId==='string'&&typeof x.success==='boolean'
    &&typeof x.message==='string'&&int(x.revision);
}
export function isEmpireSnapshot(x:unknown):x is EmpireSnapshot{
  if(!record(x)||x.version!==2||typeof x.session!=='string'||!int(x.revision)
    ||(x.visitId!==undefined&&(!int(x.visitId)||x.visitId<1))
    ||!(x.notice===null||typeof x.notice==='string')
    ||!(x.lastReply===null||isReply(x.lastReply))||!record(x.state))return false;
  const s=x.state,e=s.economy;
  if(!['de','en'].includes(String(s.language))||typeof s.animationsEnabled!=='boolean'
    ||typeof s.soundEnabled!=='boolean'||typeof s.detached!=='boolean'||!record(e))return false;
  if(!['credits','rate','tapValue','runCredits','prestigeRequirement'].every(k=>decimal(e[k]))
    ||!['clicks','prestige','totalLevels'].every(k=>int(e[k]))
    ||typeof e.canPrestige!=='boolean'||!Array.isArray(e.buildings)||e.buildings.length!==12)return false;
  if(!e.buildings.every((b:unknown)=>record(b)&&['id','name','description','requirement'].every(k=>typeof b[k]==='string')
    &&(b.nextUnlock===null||typeof b.nextUnlock==='string')
    &&['level','maxLevel','visualTier'].every(k=>int(b[k]))
    &&b.level<=b.maxLevel&&b.maxLevel===8&&b.visualTier<=4
    &&['unlocked','visible','canBuy'].every(k=>typeof b[k]==='boolean')
    &&decimal(b.cost)&&decimal(b.rate)))return false;
  const g=e.goal;
  return record(g)&&(g.id===null||typeof g.id==='string')
    &&typeof g.title==='string'&&typeof g.description==='string'
    &&decimal(g.current)&&decimal(g.target)&&typeof g.progress==='number'
    &&Number.isFinite(g.progress)&&g.progress>=0&&g.progress<=1;
}
export function isEmpireActivation(x:unknown):x is {type:'empire.activated';data:{visitId:number}}{
  return record(x)&&x.type==='empire.activated'&&record(x.data)&&int(x.data.visitId)&&x.data.visitId>0;
}
export type EmpireMessage={type:'snapshot';data:EmpireSnapshot}|{type:'result';data:Reply}
  |{type:'activated';visitId:number}|{type:'visibility';visible:boolean}|{type:'escape'}|{type:'invalid'};
export function listen(receive:(message:EmpireMessage)=>void){
  const host=window.chrome?.webview;
  const handler=(event:MessageEvent)=>{
    const m:unknown=event.data;if(!record(m))return;
    if(m.type==='empire.snapshot'&&isEmpireSnapshot(m.data))receive({type:'snapshot',data:m.data});
    else if(m.type==='empire.result'&&isReply(m.data))receive({type:'result',data:m.data});
    else if(isEmpireActivation(m))receive({type:'activated',visitId:m.data.visitId});
    else if(m.type==='empire.visibility'&&record(m.data)&&typeof m.data.visible==='boolean')
      receive({type:'visibility',visible:m.data.visible});
    else if(m.type==='empire.escape')receive({type:'escape'});
    else if(m.type==='empire.invalid')receive({type:'invalid'});
  };
  host?.addEventListener('message',handler);
  return()=>host?.removeEventListener('message',handler);
}
export function post(type:string,fields:Record<string,unknown>={}){
  window.chrome?.webview?.postMessage({type,...fields});
}
export function send(snapshot:EmpireSnapshot,command:Command){
  const requestId=crypto.randomUUID();
  post('empire.command',{version:2,session:snapshot.session,expectedRevision:snapshot.revision,requestId,...command});
  return requestId;
}
export function fmt(value:string|number,language:'de'|'en'='de'){
  const n=Number(value);
  if(!Number.isFinite(n))return String(value);
  const steps=['','K','M','B','T','Qa','Qi','Sx','Sp'];
  const tier=Math.min(steps.length-1,Math.max(0,Math.floor(Math.log10(Math.max(1,n))/3)));
  return new Intl.NumberFormat(language==='de'?'de-DE':'en-US',{maximumFractionDigits:tier?2:0})
    .format(n/Math.pow(1000,tier))+(tier?' '+steps[tier]:'');
}
