export type MergeItem={id:string;definitionId:string;position:number;status:string};
export type MergeDefinition={id:string;name:string;lineId:string;tier:number;maxTier:number;
 mergeResult:string|null;color:string;mark:string;stackCount:number;tags:string[]};
export type MergeProducer={id:string;name:string;outputDefinitionId:string;unlockPoints:number;
 unlocked:boolean;available:boolean;spawnCount:number};
export type MergeOrder={id:string;definitionId:string;areaId:string;reward:number;
 available:boolean;visible:boolean;requirements:{definitionId:string;count:number;available:number}[]};
export type MergeMilestone={id:string;name:string;points:number;unlocked:boolean};
export type MergeRestoration={areaId:string;areaName:string;areaIndex:number;areaCount:number;
 current:{id:string;name:string;points:number};
 next:{id:string;name:string;points:number}|null;
 pointsUntilNext:number;upgradeProgress:number;milestones:MergeMilestone[]};
export type MergeAction={outcome:string;position:number;fromPosition:number;definitionId:string|null;
 sourceDefinitionId:string|null;reward:number;milestones:string[]|null};
export type MergeLabels={title:string;eyebrow:string;back:string;producer:string;noEnergy:string;
 producerHint:string;orders:string;deliver:string;waiting:string;howToPlay:string;
 selectionHelp:string;selectFirst:string;globalProgress:string;nextUpgrade:string;
 areaComplete:string;saveFailed:string;progressionSaveFailed:string;xpHint:string;profileUnavailable:string};
export type MergeState={
 language:'en'|'de';schemaVersion:number;revision:number;columns:7;rows:6;capacity:42;
 renovationPoints:number;totalMerges:number;highestTierCreated:number;unlockedMilestones:string[];
 items:MergeItem[];definitions:MergeDefinition[];producers:MergeProducer[];
 orders:MergeOrder[];restoration:MergeRestoration;
 profile:{level:number;levelExperience:number;prestige:number;coins:number;nextLevelExperience:number}|null;
 soundEnabled:boolean;animationsEnabled:boolean;feedback:string;playerXp:number;seasonXp:number;
 progressionSaveFailed:boolean;lastAction:MergeAction|null;labels:MergeLabels;
};
export type MergeSnapshot={version:1;session:string;revision:number;state:MergeState};
export type MergeReceipt={requestId:string;success:boolean;message:string;revision:number};
export type MergeIntent=
 |{action:'spawn';producerId:string}
 |{action:'drop';itemId:string;target:number}
 |{action:'deliver';orderId:string}
 |{action:'sound.set'|'animations.set';value:'true'|'false'};
const obj=(v:unknown):v is Record<string,unknown>=>typeof v==='object'&&v!==null&&!Array.isArray(v);
const int=(v:unknown):v is number=>typeof v==='number'&&Number.isSafeInteger(v)&&v>=0;
const guid=(v:unknown):v is string=>typeof v==='string'&&/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(v);
const str=(v:unknown):v is string=>typeof v==='string'&&v.length<=4096;
const arr=(v:unknown):v is unknown[]=>Array.isArray(v)&&v.length<=8192;
const req=(v:unknown):v is MergeOrder['requirements'][number]=>obj(v)&&str(v.definitionId)&&int(v.count)&&int(v.available);
const item=(v:unknown):v is MergeItem=>obj(v)&&guid(v.id)&&str(v.definitionId)&&int(v.position)&&v.position<42&&str(v.status);
const definition=(v:unknown):v is MergeDefinition=>obj(v)&&str(v.id)&&str(v.name)&&str(v.lineId)
 &&int(v.tier)&&v.tier>0&&int(v.maxTier)&&v.maxTier>=v.tier
 &&(v.mergeResult===null||str(v.mergeResult))&&str(v.color)&&/^#[0-9a-f]{6}$/i.test(v.color)
 &&str(v.mark)&&int(v.stackCount)&&arr(v.tags)&&v.tags.every(str);
const producer=(v:unknown):v is MergeProducer=>obj(v)&&str(v.id)&&str(v.name)&&str(v.outputDefinitionId)
 &&int(v.unlockPoints)&&typeof v.unlocked==='boolean'&&typeof v.available==='boolean'&&int(v.spawnCount);
const order=(v:unknown):v is MergeOrder=>obj(v)&&guid(v.id)&&str(v.definitionId)&&str(v.areaId)
 &&int(v.reward)&&typeof v.available==='boolean'&&typeof v.visible==='boolean'
 &&arr(v.requirements)&&v.requirements.length>0&&v.requirements.every(req);
const milestone=(v:unknown):v is MergeMilestone=>obj(v)&&str(v.id)&&str(v.name)
 &&int(v.points)&&typeof v.unlocked==='boolean';
const restoration=(v:unknown):v is MergeRestoration=>obj(v)&&str(v.areaId)&&str(v.areaName)
 &&int(v.areaIndex)&&int(v.areaCount)&&v.areaIndex>0&&v.areaIndex<=v.areaCount
 &&obj(v.current)&&str(v.current.id)&&str(v.current.name)&&int(v.current.points)
 &&(v.next===null||(obj(v.next)&&str(v.next.id)&&str(v.next.name)&&int(v.next.points)))
 &&int(v.pointsUntilNext)&&typeof v.upgradeProgress==='number'&&Number.isFinite(v.upgradeProgress)
 &&v.upgradeProgress>=0&&v.upgradeProgress<=1
 &&arr(v.milestones)&&v.milestones.every(milestone);
const action=(v:unknown):v is MergeAction=>obj(v)&&str(v.outcome)
 &&typeof v.position==='number'&&Number.isSafeInteger(v.position)&&v.position>=-1&&v.position<42
 &&typeof v.fromPosition==='number'&&Number.isSafeInteger(v.fromPosition)&&v.fromPosition>=-1&&v.fromPosition<42
 &&(v.definitionId===null||str(v.definitionId))&&(v.sourceDefinitionId===null||str(v.sourceDefinitionId))
 &&int(v.reward)&&(v.milestones===null||(arr(v.milestones)&&v.milestones.every(str)));
const labelKeys=['title','eyebrow','back','producer','noEnergy','producerHint','orders','deliver','waiting',
 'howToPlay','selectionHelp','selectFirst','globalProgress','nextUpgrade','areaComplete',
 'saveFailed','progressionSaveFailed','xpHint','profileUnavailable'] as const;
export function isMergeSnapshot(v:unknown):v is MergeSnapshot{
 if(!obj(v)||v.version!==1||!guid(v.session)||!int(v.revision)||!obj(v.state))return false;
 const s=v.state;
 if(s.language!=='en'&&s.language!=='de'||!int(s.schemaVersion)||s.schemaVersion>2
   ||!int(s.revision)||s.revision!==v.revision||s.columns!==7||s.rows!==6||s.capacity!==42
   ||!int(s.renovationPoints)||!int(s.totalMerges)||!int(s.highestTierCreated)
   ||!arr(s.unlockedMilestones)||!s.unlockedMilestones.every(str)
   ||!arr(s.items)||!s.items.every(item)||new Set(s.items.map(x=>(x as MergeItem).position)).size!==s.items.length
   ||new Set(s.items.map(x=>(x as MergeItem).id)).size!==s.items.length
   ||!arr(s.definitions)||!s.definitions.every(definition)
   ||!arr(s.producers)||!s.producers.every(producer)||!arr(s.orders)||!s.orders.every(order)
   ||!restoration(s.restoration)||typeof s.soundEnabled!=='boolean'||typeof s.animationsEnabled!=='boolean'
   ||!str(s.feedback)||!int(s.playerXp)||!int(s.seasonXp)||typeof s.progressionSaveFailed!=='boolean'
   ||!(s.lastAction===null||action(s.lastAction))
   ||!obj(s.labels)||!labelKeys.every(k=>str((s.labels as Record<string,unknown>)[k])))return false;
 if(s.profile!==null&&!(obj(s.profile)&&int(s.profile.level)&&int(s.profile.levelExperience)
  &&int(s.profile.prestige)&&int(s.profile.coins)&&int(s.profile.nextLevelExperience)))return false;
 const defs=new Set((s.definitions as MergeDefinition[]).map(x=>x.id));
 if((s.items as MergeItem[]).some(x=>!defs.has(x.definitionId))
  ||(s.producers as MergeProducer[]).some(x=>!defs.has(x.outputDefinitionId))
  ||(s.orders as MergeOrder[]).some(x=>x.requirements.some(r=>!defs.has(r.definitionId))))return false;
 return true;
}
export function isMergeReceipt(v:unknown):v is MergeReceipt{
 return obj(v)&&guid(v.requestId)&&typeof v.success==='boolean'
   &&str(v.message)&&int(v.revision);
}
export function sendMergeIntent(snapshot:MergeSnapshot,intent:MergeIntent):string{
 const requestId=crypto.randomUUID();
 window.chrome?.webview?.postMessage({type:'midnight-merge.command',version:1,
   requestId,session:snapshot.session,expectedRevision:snapshot.revision,...intent});
 return requestId;
}
export function listenMerge(onEvent:(event:{type:'snapshot';data:MergeSnapshot}|
 {type:'receipt';data:MergeReceipt})=>void):()=>void{
 const browser=window.chrome?.webview;
 const handler=(event:MessageEvent)=>{
   const payload:unknown=event.data;
   if(!obj(payload))return;
   if(payload.type==='midnight-merge.snapshot'&&isMergeSnapshot(payload.data))
     onEvent({type:'snapshot',data:payload.data});
   if(payload.type==='midnight-merge.result'&&isMergeReceipt(payload.data))
     onEvent({type:'receipt',data:payload.data});
 };
 browser?.addEventListener('message',handler);
 return()=>browser?.removeEventListener('message',handler);
}
