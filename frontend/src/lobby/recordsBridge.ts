/** Typed, read-only data for React progression and the Collection Album. */
import {isAlbum,isAlbumResult} from './albumBridge';
import type {AlbumSnapshot,AlbumResult} from './albumBridge';
export type ChallengeEntry={
  id:string;name:string;description:string;category:string;isAdventure:boolean;isEndgame:boolean;
  progress:number;goal:number;progressLabel:string;goalLabel:string;fraction:number;
  complete:boolean;claimed:boolean;canClaim:boolean;automaticallyAwarded:boolean;
  coinReward:number;pack:string|null;empireBoost:string|null;
};
export type ChallengesSnapshot={
  total:number;complete:number;claimed:number;ready:number;readyCoins:number;
  categories:string[];entries:ChallengeEntry[];
};
export type StatMetric={label:string;value:string};
export type GameStatistics={
  id:string;title:string;category:'tables'|'slots'|'relaxed';net:string|null;positive:boolean;
  metrics:StatMetric[];
};
export type StatisticsSnapshot={
  overview:{net:string;netPositive:boolean;wagered:string;slotSpins:string;playtime:string;rounds:string;gamesTracked:number};
  games:GameStatistics[];
  session:{rounds:string;hands:string;playtime:string;won:string;lost:string;pushes:string;wagered:string;net:string;blackjacks:string;bestStreak:string};
  tables:{name:string;hands:string;net:string;positive:boolean;rounds:string;winRate:string}[];
};
export type CosmeticIdentityOption={id:string;name:string;description:string;visualVariant:string};
export type AccountSnapshot={
 displayName:string;level:number;prestige:number;coins:string;title:string;portraitId:string;
 titleVariant?:string;portraitVariant?:string;frameVariant?:string;
 equippedTitleId:string;equippedPortraitId:string;
 portraits:CosmeticIdentityOption[];titles:CosmeticIdentityOption[];
 profileExperience:string;playtime:string;ownedCosmetics:number;ownedCards:number;totalCards:number;
 season:string;created:string;
};
export type HistoricalHand={name:string;cards:string;outcome:string;net:string;
 doubled:boolean;split:boolean;surrendered:boolean};
export type HistoryEntry={id:string;completed:string;net:string;positive:boolean;bet:string;
 insurance:string;dealer:string;hands:HistoricalHand[]};
export type HistorySnapshot={total:number;entries:HistoryEntry[]};
export type SeasonTier={tier:number;requiredExperience:number;coins:number;cosmeticId:string|null;bankrollReward:number};
export type SeasonEntry={id:string;name:string;description:string;archived:boolean;rewardsEnabled:boolean;
 tier:number;experience:number;totalTiers:number;maxExperience:number;tiers:SeasonTier[]};
export type SeasonsSnapshot={activeSeasonId:string;seasons:SeasonEntry[]};
export type VaultSnapshot={
 tier:string;tierId:string;accent:string;current:string;capacity:string;progress:number;
 contributionRate:number;bonusRate:number;isFull:boolean;payoutPending:boolean;
 unlockedTier:string;nextTier:string|null;nextTierWager:string|null;
 lifetimeContributions:string;lifetimePayouts:string;lifetimeBonuses:string;
 vaultsOpened:number;biggestPayout:string;lifetimeEligibleWager:string;
};
export type VaultReward={tier:string;baseAmount:string;bonusAmount:string;total:string;pack:string};
export type RecordsPage='challenges'|'statistics'|'profile'|'history'|'season'|'vault'|'album';
export type RecordsMessage=
  {type:'lobby.records.show';data:{page:RecordsPage}}|
  {type:'lobby.records.snapshot';data:{page:'challenges';snapshot:ChallengesSnapshot}|{page:'statistics';snapshot:StatisticsSnapshot}|{page:'profile';snapshot:AccountSnapshot}|{page:'history';snapshot:HistorySnapshot}|{page:'season';snapshot:SeasonsSnapshot}|{page:'vault';snapshot:VaultSnapshot}|{page:'album';snapshot:AlbumSnapshot}}|
  {type:'lobby.records.result';data:{success:boolean;message:string;id:string}}|
  {type:'lobby.records.vault.result';data:{success:boolean;message:string;reward:VaultReward|null}}|
  {type:'lobby.album.result';data:AlbumResult};
export type ChallengeStatus='all'|'ready'|'progress'|'claimed'|'unclaimed';
const obj=(x:unknown):x is Record<string,unknown>=>x!==null&&typeof x==='object'&&!Array.isArray(x);
const str=(x:unknown):x is string=>typeof x==='string';
const num=(x:unknown):x is number=>typeof x==='number'&&Number.isFinite(x)&&x>=0;
const count=(x:unknown):x is number=>num(x)&&Number.isSafeInteger(x);
const pct=(x:unknown)=>num(x)&&x<=1;
export function isChallenges(x:unknown):x is ChallengesSnapshot{
  if(!obj(x)||!count(x.total)||!count(x.complete)||!count(x.claimed)||!count(x.ready)
    ||!count(x.readyCoins)||!Array.isArray(x.categories)||x.categories.length>35
    ||!x.categories.every(str)||!Array.isArray(x.entries)||x.entries.length>1000
    ||x.total!==x.entries.length)return false;
  return x.entries.every((e:unknown)=>obj(e)&&str(e.id)&&/^[a-z0-9-]{1,80}$/.test(e.id)
    &&['name','description','category','progressLabel','goalLabel'].every(k=>str(e[k]))
    &&['isAdventure','isEndgame','complete','claimed','canClaim','automaticallyAwarded'].every(k=>typeof e[k]==='boolean')
    &&num(e.progress)&&num(e.goal)&&pct(e.fraction)&&count(e.coinReward)
    &&(e.pack===null||str(e.pack))&&(e.empireBoost===null||str(e.empireBoost)));
}
export function isStatistics(x:unknown):x is StatisticsSnapshot{
  if(!obj(x)||!obj(x.overview)||!obj(x.session)||!Array.isArray(x.games)||!Array.isArray(x.tables)
    ||x.games.length>60||x.tables.length>200)return false;
  const overview=x.overview as Record<string,unknown>;
  const session=x.session as Record<string,unknown>;
  if(!['net','wagered','slotSpins','playtime','rounds'].every(k=>str(overview[k]))
    ||typeof overview.netPositive!=='boolean'||!count(overview.gamesTracked))return false;
  if(!['rounds','hands','playtime','won','lost','pushes','wagered','net','blackjacks','bestStreak']
    .every(k=>str(session[k])))return false;
  return x.games.every((g:unknown)=>obj(g)&&str(g.id)&&str(g.title)
    &&['tables','slots','relaxed'].includes(String(g.category))
    &&(g.net===null||str(g.net))&&typeof g.positive==='boolean'
    &&Array.isArray(g.metrics)&&g.metrics.length<=40&&g.metrics.every((m:unknown)=>
      obj(m)&&str(m.label)&&str(m.value)))
    &&x.tables.every((t:unknown)=>obj(t)
      &&['name','hands','net','rounds','winRate'].every(k=>str(t[k]))
      &&typeof t.positive==='boolean');
}
export function isVault(x:unknown):x is VaultSnapshot{
 return obj(x)&&['tier','tierId','accent','current','capacity','unlockedTier',
    'lifetimeContributions','lifetimePayouts','lifetimeBonuses','biggestPayout',
    'lifetimeEligibleWager'].every(k=>str(x[k]))
    &&(x.nextTier===null||str(x.nextTier))
    &&(x.nextTierWager===null||str(x.nextTierWager))
    &&pct(x.progress)&&pct(x.bonusRate)&&pct(x.contributionRate)
    &&count(x.vaultsOpened)&&typeof x.isFull==='boolean'
    &&typeof x.payoutPending==='boolean';
}
export function isVaultReward(x:unknown):x is VaultReward{
 return obj(x)&&['tier','baseAmount','bonusAmount','total','pack'].every(k=>str(x[k]));
}
export function isAccount(x:unknown):x is AccountSnapshot{
 if(!obj(x)||!['titleVariant','portraitVariant','frameVariant'].every(k=>x[k]===undefined||
  str(x[k])&&/^[a-z0-9-]{1,100}$/.test(x[k] as string)))return false;
 if(!obj(x)||!['displayName','coins','title','portraitId','equippedTitleId','equippedPortraitId',
    'profileExperience','playtime','season','created'].every(k=>str(x[k]))
    ||!['level','prestige','ownedCosmetics','ownedCards','totalCards'].every(k=>count(x[k])))return false;
 return ['portraits','titles'].every(k=>Array.isArray(x[k])&&(x[k] as unknown[]).length<=500
    &&(x[k] as unknown[]).every(v=>obj(v)&&str(v.id)&&str(v.name)&&str(v.description)&&str(v.visualVariant)));
}
export function isHistory(x:unknown):x is HistorySnapshot{
 return obj(x)&&count(x.total)&&Array.isArray(x.entries)&&x.entries.length<=250
    &&x.entries.every((entry:unknown)=>obj(entry)
      &&['id','completed','net','bet','insurance','dealer'].every(k=>str(entry[k]))
      &&typeof entry.positive==='boolean'&&Array.isArray(entry.hands)&&entry.hands.length<=32
      &&entry.hands.every((hand:unknown)=>obj(hand)
        &&['name','cards','outcome','net'].every(k=>str(hand[k]))
        &&['doubled','split','surrendered'].every(k=>typeof hand[k]==='boolean')));
}
export function isSeasons(x:unknown):x is SeasonsSnapshot{
 return obj(x)&&str(x.activeSeasonId)&&Array.isArray(x.seasons)&&x.seasons.length<=30
 &&x.seasons.every((s:unknown)=>obj(s)&&['id','name','description'].every(k=>str(s[k]))
 &&typeof s.archived==='boolean'&&typeof s.rewardsEnabled==='boolean'
 &&['tier','experience','totalTiers','maxExperience'].every(k=>count(s[k]))
 &&Array.isArray(s.tiers)&&s.tiers.length<=500&&s.tiers.every((t:unknown)=>obj(t)
 &&['tier','requiredExperience','coins','bankrollReward'].every(k=>num(t[k]))
 &&(t.cosmeticId===null||str(t.cosmeticId))));
}
export function onRecordsMessage(handle:(message:RecordsMessage)=>void):()=>void{
  const host=window.chrome?.webview;
  if(!host)return()=>{};
  const listener=(event:MessageEvent)=>{
    const m:unknown=event.data;
    if(!obj(m)||!str(m.type)||!obj(m.data))return;
    if(m.type==='lobby.records.show'&&(m.data.page==='challenges'||m.data.page==='statistics'||m.data.page==='profile'||m.data.page==='history'||m.data.page==='season'||m.data.page==='vault'||m.data.page==='album'))
      handle({type:m.type,data:{page:m.data.page}});
    if(m.type==='lobby.records.snapshot'
      && ((m.data.page==='challenges'&&isChallenges(m.data.snapshot))
        ||(m.data.page==='statistics'&&isStatistics(m.data.snapshot))
        ||(m.data.page==='profile'&&isAccount(m.data.snapshot))
        ||(m.data.page==='history'&&isHistory(m.data.snapshot))
        ||(m.data.page==='season'&&isSeasons(m.data.snapshot))
        ||(m.data.page==='vault'&&isVault(m.data.snapshot))
        ||(m.data.page==='album'&&isAlbum(m.data.snapshot))))
      handle({type:m.type,data:m.data as RecordsMessage & never});
    if(m.type==='lobby.album.result'&&isAlbumResult(m.data)){
      handle({type:'lobby.album.result',data:m.data});
      return;
    }
    if(m.type==='lobby.records.vault.result'
      &&typeof m.data.success==='boolean'&&str(m.data.message)
      &&m.data.message.length<800&&(m.data.reward===null||isVaultReward(m.data.reward)))
      handle({type:m.type,data:{success:m.data.success,message:m.data.message,reward:m.data.reward}});
    if(m.type==='lobby.records.result'
      &&typeof m.data.success==='boolean'&&str(m.data.message)&&m.data.message.length<800&&str(m.data.id))
      handle({type:m.type,data:{success:m.data.success,message:m.data.message,id:m.data.id}});
  };
  host.addEventListener('message',listener);
  return()=>host.removeEventListener('message',listener);
}
export function filterChallenges(entries:ChallengeEntry[],category:string,status:ChallengeStatus,query:string){
  const term=query.trim().toLocaleLowerCase();
  return entries.filter(e=>(category==='All'||e.category===category)
    &&(!term||(e.name+' '+e.description+' '+e.category+' '+(e.isEndgame?'endgame ':'')).toLocaleLowerCase().includes(term))
    &&(status==='all'||(status==='ready'&&e.canClaim)||(status==='progress'&&!e.complete&&!e.claimed)
      ||(status==='claimed'&&e.claimed)||(status==='unclaimed'&&!e.claimed)))
    .sort((a,b)=>(a.claimed?2:a.canClaim?0:1)-(b.claimed?2:b.canClaim?0:1)
      ||a.category.localeCompare(b.category)||a.name.localeCompare(b.name));
}
export const previewChallenges:ChallengesSnapshot={
  total:3,complete:1,claimed:0,ready:1,readyCoins:250,
  categories:['Blackjack','Midnight Manor'],
  entries:[
    {id:'bj-50',name:'A Seat at the Table',description:'Play 50 Blackjack rounds.',category:'Blackjack',
      isAdventure:false,isEndgame:false,progress:50,goal:50,progressLabel:'50',goalLabel:'50',
      fraction:1,complete:true,claimed:false,canClaim:true,automaticallyAwarded:false,coinReward:250,pack:'Common',empireBoost:null},
    {id:'manor-500',name:'After Dark',description:'Play 500 paid spins in Midnight Manor.',category:'Midnight Manor',
      isAdventure:false,isEndgame:false,progress:180,goal:500,progressLabel:'180',goalLabel:'500',
      fraction:.36,complete:false,claimed:false,canClaim:false,automaticallyAwarded:false,coinReward:500,pack:null,empireBoost:null},
    {id:'bj-1000',name:'Master Dealer',description:'Play 1000 rounds.',category:'Blackjack',
      isAdventure:false,isEndgame:true,progress:50,goal:1000,progressLabel:'50',goalLabel:'1,000',
      fraction:.05,complete:false,claimed:false,canClaim:false,automaticallyAwarded:false,coinReward:1200,pack:null,empireBoost:null},
  ],
};
export const previewAccount:AccountSnapshot={
 displayName:'Guest',level:1,prestige:0,coins:'0',title:'Midnight Dealer',
 portraitId:'portrait-classic',equippedTitleId:'title-rookie',equippedPortraitId:'portrait-classic',
 portraits:[{id:'portrait-classic',name:'House Guest',description:'Classic casino portrait',visualVariant:'portrait-house-guest'}],
 titles:[{id:'title-rookie',name:'Rookie',description:'Your first title',visualVariant:'standard'}],
 profileExperience:'0',playtime:'0m',ownedCosmetics:5,
 ownedCards:0,totalCards:136,season:'Neon Dynasty',created:'—'
};
export const previewHistory:HistorySnapshot={total:0,entries:[]};
export const previewVault:VaultSnapshot={
 tier:'Bronze Vault',tierId:'bronze',accent:'#B87947',current:'$2,500.00',
 capacity:'$10,000.00',progress:.25,contributionRate:.02,bonusRate:.10,isFull:false,payoutPending:false,
 unlockedTier:'Bronze Vault',nextTier:'Silver Vault',nextTierWager:'$250,000.00',
 lifetimeContributions:'$2,500.00',lifetimePayouts:'$0.00',lifetimeBonuses:'$0.00',
 vaultsOpened:0,biggestPayout:'$0.00',lifetimeEligibleWager:'$125,000.00',
};
export const previewSeasons:SeasonsSnapshot={
 activeSeasonId:'season-3-neon-dynasty',seasons:[{
 id:'season-3-neon-dynasty',name:'Neon Dynasty',description:'Current Season Pass',
 archived:false,rewardsEnabled:true,tier:0,experience:0,totalTiers:2,maxExperience:1200,
 tiers:[{tier:1,requiredExperience:400,coins:40,cosmeticId:null,bankrollReward:0},
 {tier:2,requiredExperience:1200,coins:60,cosmeticId:null,bankrollReward:0}]
 }]};
export const previewStatistics:StatisticsSnapshot={
  overview:{net:'+$1,230.00',netPositive:true,wagered:'$14,000.00',slotSpins:'592',playtime:'14h 20m',rounds:'59',gamesTracked:2},
  games:[{id:'blackjack',title:'Blackjack',category:'tables',net:'+$300.00',positive:true,metrics:[
    {label:'Rounds',value:'59'},{label:'Hands',value:'60'},{label:'Hand win rate',value:'47.5%'},{label:'Biggest round win',value:'$320.00'}]},
  {id:'midnight-manor',title:'Midnight Manor',category:'slots',net:'+$930.00',positive:true,metrics:[
    {label:'Paid spins',value:'592'},{label:'Free spins',value:'42'},{label:'Total wagered',value:'$12,000.00'}]}],
  session:{rounds:'3',hands:'3',playtime:'19m',won:'2',lost:'1',pushes:'0',wagered:'$120.00',net:'+$30.00',blackjacks:'0',bestStreak:'2'},
  tables:[{name:'Velvet Table',hands:'48',net:'+$300.00',positive:true,rounds:'42',winRate:'50.0%'}],
};
