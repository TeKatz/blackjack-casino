export type TowerKind='archer'|'frost'|'cannon'|'ballista'|'flame';
export type EnemyKind='grunt'|'runner'|'brute'|'shield';
export type TdPoint={x:number;y:number};
export type TdTower={pad:number;kind:TowerKind;level:number;cooldown:number};
export type TdEnemy={id:number;kind:EnemyKind;distance:number;hp:number;slow:number};
export type TdEffect={fromX:number;fromY:number;toX:number;toY:number;kind:TowerKind;ticks:number};
export type BastionProgress={level:number;experience:number;skills:Record<string,number>;
  completedStages:string[];bestWaves:Record<string,number>;enemiesDefeated:number;enemiesEscaped:number;
  wavesCleared:number;victories:number;defeats:number;runsStarted:number;pointsAvailable:number};
export type TowerDefenceData={stage?:'forest'|'frost';endless?:boolean;progress?:BastionProgress;
  screen?:'menu'|'skills'|'game';stageRuns?:Record<string,unknown>;
  version:1;wave:number;coins:number;lives:number;
  phase:'planning'|'running'|'won'|'lost';paused:boolean;speed:1|2;
  nextEnemyId:number;spawned:number;spawnTotal:number;waveKills:number;waveEscapes:number;spawnClock:number;ticks:number;
  towers:TdTower[];enemies:TdEnemy[];effects:TdEffect[];
};
export type TowerDefenseState={screen:'menu'|'skills'|'game';mapPath:TdPoint[];pads:TdPoint[];maxWaves:15|20;state:TowerDefenceData};
export type TowerIntent=
  |{action:'tower.build';pad:number;kind:TowerKind}
  |{action:'tower.upgrade'|'tower.sell';pad:number}
  |{action:'tower.play';kind:'forest'|'frost'}
  |{action:'tower.skill.buy';kind:string}
  |{action:'tower.start'|'tower.pause'|'tower.speed'|'tower.restart'|'tower.exit'|'tower.menu'|'tower.skills'|'tower.endless'|'tower.skill.reset'};
export type TowerReply={requestId:string;success:boolean;message:string;revision:number};
const record=(v:unknown):v is Record<string,unknown>=>v!==null&&typeof v==='object'&&!Array.isArray(v);
const int=(v:unknown,max=Number.MAX_SAFE_INTEGER):v is number=>
  typeof v==='number'&&Number.isSafeInteger(v)&&v>=0&&v<=max;
const finite=(v:unknown,max:number):v is number=>
  typeof v==='number'&&Number.isFinite(v)&&v>=0&&v<=max;
const point=(v:unknown):v is TdPoint=>record(v)&&finite(v.x,960)&&finite(v.y,600);
const towerKinds=['archer','frost','cannon','ballista','flame'] as const;
const enemyKinds=['grunt','runner','brute','shield'] as const;
const kind=(v:unknown):v is TowerKind=>towerKinds.some(k=>k===v);
const enemyKind=(v:unknown):v is EnemyKind=>enemyKinds.some(k=>k===v);
const guid=(v:unknown)=>typeof v==='string'&&/^[\da-f]{8}-[\da-f]{4}-[\da-f]{4}-[\da-f]{4}-[\da-f]{12}$/i.test(v);
const tower=(v:unknown):v is TdTower=>record(v)&&int(v.pad,9)&&kind(v.kind)
  &&int(v.level,3)&&v.level>=1&&finite(v.cooldown,10);
const enemy=(v:unknown):v is TdEnemy=>record(v)&&int(v.id,100000)&&v.id>0
  &&enemyKind(v.kind)&&finite(v.distance,5000)&&finite(v.hp,1000000)&&v.hp>0&&finite(v.slow,10);
const effect=(v:unknown):v is TdEffect=>record(v)&&finite(v.fromX,960)&&finite(v.fromY,600)
  &&finite(v.toX,960)&&finite(v.toY,600)&&kind(v.kind)&&int(v.ticks,3);
function validProgress(p:unknown):p is BastionProgress{
 return record(p)&&int(p.level,100)&&p.level>=1&&int(p.experience,100000000)&&
  record(p.skills)&&Object.entries(p.skills).every(([k,v])=>['capital','refund','fortify','engineer','scouting','focus','last-stand'].includes(k)&&int(v,5))&&
  Array.isArray(p.completedStages)&&p.completedStages.length<=2&&p.completedStages.every(x=>x==='forest'||x==='frost')&&
  record(p.bestWaves)&&Object.entries(p.bestWaves).every(([k,v])=>['forest','frost'].includes(k)&&int(v,250))&&
  int(p.enemiesDefeated)&&int(p.enemiesEscaped)&&int(p.wavesCleared)&&int(p.victories)&&int(p.defeats)&&int(p.runsStarted)&&int(p.pointsAvailable,100);
}
export function isTowerDefenseState(v:unknown):v is TowerDefenseState{
 if(!record(v)||!['game','menu','skills'].includes(String(v.screen))||(v.maxWaves!==15&&v.maxWaves!==20)||
  !Array.isArray(v.mapPath)||v.mapPath.length!==10||!v.mapPath.every(point)||
  !Array.isArray(v.pads)||v.pads.length!==10||!v.pads.every(point))return false;
 const s=v.state;
 if(!record(s)||s.version!==1||!int(s.wave,250)||!int(s.coins,1000000)||
  (s.stage!==undefined&&s.stage!=='forest'&&s.stage!=='frost')||
  (s.endless!==undefined&&typeof s.endless!=='boolean')||
  (s.progress!==undefined&&!validProgress(s.progress))||
  !int(s.lives,30)||!['planning','running','won','lost'].includes(String(s.phase))||
  typeof s.paused!=='boolean'||(s.speed!==1&&s.speed!==2)||
  !int(s.spawned,100)||!int(s.spawnTotal,100)||s.spawned>s.spawnTotal||
  !int(s.waveKills,100)||!int(s.waveEscapes,100)||s.waveKills+s.waveEscapes>s.spawned||
  !finite(s.spawnClock,10)||
  !int(s.nextEnemyId,100000)||!int(s.ticks,100000000)||
  !Array.isArray(s.towers)||s.towers.length>10||!s.towers.every(tower)||
  new Set(s.towers.map((t:TdTower)=>t.pad)).size!==s.towers.length||
  !Array.isArray(s.enemies)||s.enemies.length>100||!s.enemies.every(enemy)||
  !Array.isArray(s.effects)||s.effects.length>100||!s.effects.every(effect))return false;
 return true;
}
export function isTowerReply(v:unknown):v is TowerReply{
 return record(v)&&guid(v.requestId)&&typeof v.success==='boolean'&&
  typeof v.message==='string'&&v.message.length<=8192&&int(v.revision);
}
export function issueTowerIntent(session:string,revision:number,intent:TowerIntent):string{
 const requestId=crypto.randomUUID();
 window.chrome?.webview?.postMessage({type:'lounge.tower-defense.command',
  version:1,session,expectedRevision:revision,requestId,...intent});
 return requestId;
}
