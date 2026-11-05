import type {} from '../bridge/protocol';

export type Automation = {enabled:boolean;reserve:string;spendPercent:string;targetLevel:number;cheapestMilestone:boolean;priorities:string[]};
export type Quote = {requestedLevels:number;levels:number;targetLevel:number;cost:string;unlocked:boolean;canBuy:boolean};
export type Breakdown = Record<'base'|'prestige'|'achievements'|'strategy'|'synergies'|'projects'|'managers'|'events'|'contracts'|'strategicBonus'|'final',string>;
export type Room = {id:string;name:string;description:string;floor:number;category:string;tags:string;accent:string;level:number;rate:string;requirement:string;breakdown:Breakdown;quotes:Quote[]};
export type CatalogItem = {id:string;name:string;description:string;cost:string;requirement:string;owned?:boolean;canBuy:boolean};
export type Research = {id:string;name:string;description:string;branch:string;rank:number;maxRank:number;costPerRank:number;prerequisite:string|null;prerequisiteRank:number;canBuy:boolean};
export type Synergy = {id:string;name:string;description:string;tier:number;minimumLevel:number;bonus:string;nextLevel:number|null;members:{id:string;name:string;level:number}[]};
export type Manager = {id:string;name:string;role:string;description:string;requiredPrestige:number;assignment:string|null;available:boolean};
export type Property = CatalogItem & {state:{credits:string;prestige:number;lifetimeCredits:string;totalLevels:number}|null};
export type EmpireEconomy = {
  activePropertyId:string;credits:string;dividend:string;dividendCapacity:string;rate:string;portfolioRate:string;cashPerHour:string;
  prestige:number;totalPrestige:number;reputation:number;totalLevels:number;openRooms:number;legacyPointsAvailable:number;managerSlots:number;
  automationUnlocked:boolean;automation:Automation;managerAssignments:Record<string,string>;canPrestige:boolean;prestigeRequirement:string;mainFloorLevels:number;prestigeMainFloorLevelRequirement:number;prestigeRunCreditRequirement:string;offlineHours:number;
  floors:{id:number;displayName:string;accent:string;unlocked:boolean;requirement:string}[];rooms:Room[];research:Research[];projects:CatalogItem[];
  properties:Property[];synergies:Synergy[];managers:Manager[];events:(CatalogItem & {durationMinutes:number;activityGoal:number})[];
  activeEvent:{id:string;endsAt:string;encore:boolean;progress:number}|null;
  contracts:{id:string;name:string;description:string;rewardDescription:string;goal:string}[];activeContract:string|null;contractProgress:string;canClaimContract:boolean;
  contractBuff:{contractId:string;remainingSeconds:string}|null;
  statistics:{lifetimeCredits:string;runCredits:string;lifetimeCashClaimed:string;propertyLifetimeCredits:string;levelsPurchased:string;automatedLevels:string;eventsCompleted:string;eventEncores:string;contractsCompleted:string;producedByTag:Record<string,string>};
  achievementBoosts:{name:string;roomId:string|null;percent:string}[];
};
export type EmpireSnapshot = {version:1;session:string;revision:number;notice:string|null;lastReply:Reply|null;
  offline:{credits:string;dividend:string;seconds:number;capped:boolean;rooms:Record<string,string>;properties:Record<string,string>}|null;
  state:{language:'de'|'en';animationsEnabled:boolean;soundEnabled:boolean;detached:boolean;canClaimDividend:boolean;economy:EmpireEconomy}};
export type Preview = {success:boolean;message:string;budgetBefore:string;budgetAfter:string;portfolioBefore:string;portfolioAfter:string;levelBefore:number;levelAfter:number;roomBefore:Breakdown;roomAfter:Breakdown;synergiesAfter:{id:string;tier:number;minimumLevel:number;bonus:string}[];automationAfter:Automation};
export type Reply = {requestId:string;success:boolean;message:string;revision:number;preview:Preview|null};
export type Intent = {action:'upgrade'|'research'|'project'|'property.buy'|'property.select'|'manager'|'automation'|'event'|'contract.accept'|'contract.claim'|'prestige'|'dividend';id?:string;roomId?:string;levels?:number;automation?:Automation;preview?:boolean};

const record = (x:unknown):x is Record<string,unknown> => !!x && typeof x==='object' && !Array.isArray(x);
const decimal = (x:unknown):x is string => typeof x==='string' && /^\d+(?:\.\d+)?$/.test(x) && x.length<=60;
const integer = (x:unknown):x is number => typeof x==='number' && Number.isSafeInteger(x) && x>=0;
const strings = (x:Record<string,unknown>,keys:string[]) => keys.every(k=>typeof x[k]==='string');
const bools = (x:Record<string,unknown>,keys:string[]) => keys.every(k=>typeof x[k]==='boolean');
const list = (x:unknown, check:(v:unknown)=>boolean) => Array.isArray(x) && x.length<=100 && x.every(check);
const nullableString = (x:unknown) => x===null || typeof x==='string';
const decimalMap = (x:unknown) => record(x) && Object.values(x).every(decimal);
function automation(x:unknown):boolean {return record(x)&&bools(x,['enabled','cheapestMilestone'])&&decimal(x.reserve)&&decimal(x.spendPercent)&&integer(x.targetLevel)&&list(x.priorities,v=>typeof v==='string');}
function breakdown(x:unknown):boolean {return record(x)&&['base','prestige','achievements','strategy','synergies','projects','managers','events','contracts','strategicBonus','final'].every(k=>decimal(x[k]));}
function catalog(x:unknown):boolean {return record(x)&&strings(x,['id','name','description','requirement'])&&decimal(x.cost)&&typeof x.canBuy==='boolean';}
function room(x:unknown):boolean {return record(x)&&strings(x,['id','name','description','category','tags','accent','requirement'])&&integer(x.floor)&&integer(x.level)&&x.level<=200&&decimal(x.rate)&&breakdown(x.breakdown)&&list(x.quotes,q=>record(q)&&['requestedLevels','levels','targetLevel'].every(k=>integer(q[k]))&&decimal(q.cost)&&bools(q,['unlocked','canBuy']));}
export function isEmpireSnapshot(x:unknown):x is EmpireSnapshot {
  if(!record(x)||x.version!==1||typeof x.session!=='string'||!integer(x.revision)||!nullableString(x.notice)||(x.lastReply!==null&&!isReply(x.lastReply))||!record(x.state))return false;
  const s=x.state,e=s.economy;
  if(!['en','de'].includes(String(s.language))||!bools(s,['animationsEnabled','soundEnabled','detached','canClaimDividend'])||!record(e))return false;
  if(!strings(e,['activePropertyId','prestigeRequirement'])||!['credits','dividend','dividendCapacity','rate','portfolioRate','cashPerHour','contractProgress','prestigeRunCreditRequirement'].every(k=>decimal(e[k]))
    ||!['prestige','totalPrestige','reputation','totalLevels','openRooms','legacyPointsAvailable','managerSlots','offlineHours','mainFloorLevels','prestigeMainFloorLevelRequirement'].every(k=>integer(e[k]))
    ||!bools(e,['automationUnlocked','canPrestige','canClaimContract'])||!automation(e.automation)||!record(e.managerAssignments)||!Object.values(e.managerAssignments).every(v=>typeof v==='string'))return false;
  if(!list(e.rooms,room)||!list(e.floors,f=>record(f)&&integer(f.id)&&strings(f,['displayName','accent','requirement'])&&typeof f.unlocked==='boolean')
    ||!list(e.research,r=>record(r)&&strings(r,['id','name','description','branch'])&&['rank','maxRank','costPerRank','prerequisiteRank'].every(k=>integer(r[k]))&&nullableString(r.prerequisite)&&typeof r.canBuy==='boolean')
    ||!list(e.projects,p=>catalog(p)&&record(p)&&typeof p.owned==='boolean')
    ||!list(e.properties,p=>catalog(p)&&record(p)&&typeof p.owned==='boolean'&&(p.state===null||record(p.state)&&decimal(p.state.credits)&&decimal(p.state.lifetimeCredits)&&integer(p.state.prestige)&&integer(p.state.totalLevels)))
    ||!list(e.managers,m=>record(m)&&strings(m,['id','name','role','description'])&&integer(m.requiredPrestige)&&nullableString(m.assignment)&&typeof m.available==='boolean')
    ||!list(e.events,v=>catalog(v)&&record(v)&&integer(v.durationMinutes)&&integer(v.activityGoal))
    ||!list(e.contracts,c=>record(c)&&strings(c,['id','name','description','rewardDescription'])&&decimal(c.goal))
    ||!nullableString(e.activeContract))return false;
  if(!list(e.synergies,y=>record(y)&&strings(y,['id','name','description'])&&integer(y.tier)&&integer(y.minimumLevel)&&decimal(y.bonus)&&(y.nextLevel===null||integer(y.nextLevel))&&list(y.members,m=>record(m)&&strings(m,['id','name'])&&integer(m.level)))
    ||!list(e.achievementBoosts,b=>record(b)&&typeof b.name==='string'&&nullableString(b.roomId)&&decimal(b.percent)))return false;
  if(e.activeEvent!==null&&(!record(e.activeEvent)||!strings(e.activeEvent,['id','endsAt'])||typeof e.activeEvent.encore!=='boolean'||!integer(e.activeEvent.progress)))return false;
  if(e.contractBuff!==null&&(!record(e.contractBuff)||typeof e.contractBuff.contractId!=='string'||!decimal(e.contractBuff.remainingSeconds)))return false;
  const st=e.statistics;
  if(!record(st)||!['lifetimeCredits','runCredits','lifetimeCashClaimed','propertyLifetimeCredits','levelsPurchased','automatedLevels'].every(k=>decimal(st[k]))||!['eventsCompleted','eventEncores','contractsCompleted'].every(k=>decimal(st[k]))||!decimalMap(st.producedByTag))return false;
  if(x.offline!==null&&(!record(x.offline)||!decimal(x.offline.credits)||!decimal(x.offline.dividend)||typeof x.offline.seconds!=='number'||!Number.isFinite(x.offline.seconds)||x.offline.seconds<0||typeof x.offline.capped!=='boolean'||!decimalMap(x.offline.rooms)||!decimalMap(x.offline.properties)))return false;
  return true;
}
export function isReply(x:unknown):x is Reply {
  if(!record(x)||typeof x.requestId!=='string'||typeof x.success!=='boolean'||typeof x.message!=='string'||!integer(x.revision))return false;
  const p=x.preview;
  return p===null || record(p)&&typeof p.success==='boolean'&&typeof p.message==='string'&&['budgetBefore','budgetAfter','portfolioBefore','portfolioAfter'].every(k=>decimal(p[k]))&&integer(p.levelBefore)&&integer(p.levelAfter)&&breakdown(p.roomBefore)&&breakdown(p.roomAfter)&&automation(p.automationAfter)&&list(p.synergiesAfter,s=>record(s)&&typeof s.id==='string'&&integer(s.tier)&&integer(s.minimumLevel)&&decimal(s.bonus));
}
export function retainRooms(previous:Room[],next:Room[]):Room[] {
  const old=new Map(previous.map(r=>[r.id,r]));
  return next.map(r=>{const before=old.get(r.id);return before&&JSON.stringify(before)===JSON.stringify(r)?before:r;});
}
export function post(type:string):void {window.chrome?.webview?.postMessage({type});}
export function send(snapshot:EmpireSnapshot,intent:Intent):string {
  const requestId=crypto.randomUUID();
  window.chrome?.webview?.postMessage({type:'empire.command',version:1,session:snapshot.session,expectedRevision:snapshot.revision,requestId,...intent});
  return requestId;
}
export type EmpireMessage = {type:'snapshot';data:EmpireSnapshot}|{type:'result';data:Reply}|{type:'visibility';visible:boolean}|{type:'escape'}|{type:'invalid'};
export function listen(receive:(message:EmpireMessage)=>void):()=>void {
  const host=window.chrome?.webview;
  const handler=(event:MessageEvent)=>{
    const m:unknown=event.data;if(!record(m))return;
    if(m.type==='empire.snapshot'&&isEmpireSnapshot(m.data))receive({type:'snapshot',data:m.data});
    else if(m.type==='empire.result'&&isReply(m.data))receive({type:'result',data:m.data});
    else if(m.type==='empire.visibility'&&record(m.data)&&typeof m.data.visible==='boolean')receive({type:'visibility',visible:m.data.visible});
    else if(m.type==='empire.escape')receive({type:'escape'});
    else if(m.type==='empire.invalid')receive({type:'invalid'});
  };
  host?.addEventListener('message',handler);
  return()=>host?.removeEventListener('message',handler);
}

export function display(value:string,language:'de'|'en',compact=true):string {
  const amount=Number(value);if(!Number.isFinite(amount))return value;
  const tier=compact?Math.min(9,Math.max(0,Math.floor(Math.log10(Math.max(1,amount))/3))):0;
  const suffix=['','K','M','B','T','Qa','Qi','Sx','Sp','Oc'][tier];
  return new Intl.NumberFormat(language==='de'?'de-DE':'en-US',{maximumFractionDigits:2}).format(amount/10**(tier*3))+(tier?' '+suffix:'');
}
export function decimalInput(value:string):string|null {
  const normalized=value.trim().replace(',','.');
  return /^\d+(?:\.\d+)?$/.test(normalized)&&normalized.length<=40?normalized:null;
}

// Exact decimal shifts for policy editing, independent of JavaScript float rounding.
export function percentToFraction(value:string):string|null {
  const parsed=decimalInput(value);if(!parsed||Number(parsed)<1||Number(parsed)>50)return null;
  const [whole,fraction='']=parsed.split('.');
  return '0.'+whole.padStart(2,'0')+fraction;
}
export function fractionToPercent(value:string):string {
  const [whole,fraction='']=value.split('.');
  const digits=fraction.padEnd(2,'0');
  const integer=String(Number(whole)*100+Number(digits.slice(0,2)));
  return integer+(digits.slice(2)?'.'+digits.slice(2):'');
}
