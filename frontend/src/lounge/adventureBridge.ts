export type TaleScreen='library'|'story'|'board'|'encounter'|'complete';
export type TaleCatalog={id:string;title:string;subtitle:string;acts:number;scenes:number;
 status:'unplayed'|'active'|'completed';runId:string|null;lastSceneId:string|null};
export type TaleClass={id:string;name:string;description:string;maxHitPoints:number;armor:number;
 special:string;abilities:{id:string;name:string;score:number}[]};
export type TaleChoice={id:string;text:string;check:null|{ability:string;difficulty:number;skillId:string|null};
 encounter:null|string};
export type TaleBoardNode={id:string;category:string;title:string;description:string;
 subtitle:string;reliability:string;lead:string;theory:string;facts:string[];
 quotes:string[];x:number;y:number};
export type TaleBoard={title:string;nodes:TaleBoardNode[];connections:{
 id:string;from:string;to:string;type:string;status:string;label:string}[];
 zoom:number;panX:number;panY:number};
export type TaleRun={runId:string;adventureId:string;completed:boolean;endingId:string|null;
 rollsMade:number;scene:{id:string;title:string;body:string;speaker:string|null;location:string;act:string};
 choices:TaleChoice[];pending:null|{ability:string;difficulty:number;skillId:string|null;
 mode:'Normal'|'Advantage'|'Disadvantage';modifier:number};
 lastRoll:null|{firstRoll:number;secondRoll:number|null;chosenRoll:number;
 abilityModifier:number;skillBonus:number;additionalModifier:number;
 total:number;difficulty:number;outcome:string};
 character:{name:string;classId:string;level:number;experience:number;maxHitPoints:number;
 currentHitPoints:number;armor:number;coins:number;
 abilities:{id:string;name:string;score:number;modifier:number}[];
 skills:{id:string;name:string;bonus:number}[]};
 inventory:{itemId:string;quantity:number;name:string;description:string;kind:string;
 useChoices:{id:string;text:string}[]}[];
 conditions:{conditionId:string;remaining:number;stacks:number;name:string}[];
 feedback:{type:string;title:string;description:string}[];
 board:TaleBoard|null;statistics:{scenesVisited:number;choicesMade:number;
 checksSucceeded:number;checksFailed:number;encountersWon:number;encountersLost:number}};
export type TaleCard={hidden:boolean;rank:string|null;suit:string|null};
export type TaleEncounter={id:string;type:'Blackjack'|'FiveOfAKind';title:string;
 opponentName:string;intro:string;stakes:string;outcome:'Win'|'Loss'|'Draw'|null;
 blackjack:null|{phase:string;dealer:TaleCard[];player:TaleCard[];
 playerTotal:number;dealerTotal:number|null;canHit:boolean;canStand:boolean;net:number|null};
 dice:null|{player:number[];held:boolean[];opponent:number[];
 rollsUsed:number;canRoll:boolean;canBank:boolean;bestCategory:string|null;bestPoints:number|null;
 playerScore:number|null;opponentScore:number|null;
 playerCategory:string|null;opponentCategory:string|null}};
export type TaleState={screen:TaleScreen;selectedAdventureId:string;adventures:TaleCatalog[];
 classes:TaleClass[];defaultPlayerName:string;started:number;completed:number;
 run:TaleRun|null;encounter:TaleEncounter|null;notice:string;metaNotice:string};
export type TaleIntent=
 |{action:'adventure.select'|'adventure.resume';adventureId:string}
 |{action:'adventure.start';adventureId:string;classId:string;confirmReplace?:true}
 |{action:'adventure.restart';adventureId:string;confirmReplace?:true}
 |{action:'adventure.choice';choiceId:string}
 |{action:'adventure.board.move';nodeId:string;x:number;y:number}
 |{action:'adventure.board.view';zoom:number;panX:number;panY:number}
 |{action:'adventure.encounter.hold';index:number}
 |{action:'adventure.roll'|'adventure.board.open'|'adventure.board.close'
  |'adventure.encounter.hit'|'adventure.encounter.stand'
  |'adventure.encounter.roll'|'adventure.encounter.bank'|'adventure.encounter.finish'
  |'adventure.back'|'adventure.exit'};
export type TaleReply={requestId:string;success:boolean;message:string;revision:number};

const record=(v:unknown):v is Record<string,unknown>=>v!==null&&typeof v==='object'&&!Array.isArray(v);
const label=(v:unknown,max=16384)=>typeof v==='string'&&v.length<=max;
const textOrNull=(v:unknown)=>v===null||label(v);
const int=(v:unknown,max=Number.MAX_SAFE_INTEGER):v is number=>typeof v==='number'
 &&Number.isSafeInteger(v)&&v>=0&&v<=max;
const num=(v:unknown):v is number=>typeof v==='number'&&Number.isFinite(v);
const guid=(v:unknown):v is string=>typeof v==='string'&&v.length<=64&&/^[\da-f]{8}-[\da-f]{4}-[\da-f]{4}-[\da-f]{4}-[\da-f]{12}$/i.test(v);
const words=(v:unknown,max=500)=>Array.isArray(v)&&v.length<=max&&v.every(x=>label(x,1000));
const ability=(v:unknown)=>record(v)&&label(v.id,60)&&label(v.name,180)&&int(v.score,100);
const classInfo=(v:unknown)=>record(v)&&label(v.id,100)&&label(v.name,200)
 &&label(v.description,5000)&&int(v.maxHitPoints,10000)&&int(v.armor,1000)
 &&label(v.special,2000)&&Array.isArray(v.abilities)&&v.abilities.length===6
 &&v.abilities.every(ability);
const catalog=(v:unknown)=>record(v)&&label(v.id,100)&&label(v.title,300)
 &&label(v.subtitle,3000)&&int(v.acts,100)&&int(v.scenes,10000)
 &&['unplayed','active','completed'].includes(String(v.status))&&textOrNull(v.runId)
 &&textOrNull(v.lastSceneId);
const choice=(v:unknown)=>record(v)&&label(v.id,128)&&label(v.text,8000)
 &&textOrNull(v.encounter)&&(v.check===null||(record(v.check)
 &&label(v.check.ability,200)&&int(v.check.difficulty,10000)&&textOrNull(v.check.skillId)));
const card=(v:unknown)=>record(v)&&typeof v.hidden==='boolean'
 &&textOrNull(v.rank)&&textOrNull(v.suit)&&(!v.hidden||(v.rank===null&&v.suit===null));
const board=(v:unknown):v is TaleBoard=>record(v)&&label(v.title,500)
 &&Array.isArray(v.nodes)&&v.nodes.length<=250&&v.nodes.every(n=>record(n)
  &&label(n.id,128)&&label(n.category,100)&&label(n.title,1000)
  &&label(n.description,10000)&&label(n.subtitle,1000)
  &&label(n.reliability,100)&&label(n.lead,100)&&label(n.theory,100)
  &&words(n.facts,100)&&words(n.quotes,100)
  &&num(n.x)&&Math.abs(n.x)<=5000&&num(n.y)&&Math.abs(n.y)<=5000)
 &&Array.isArray(v.connections)&&v.connections.length<=500&&v.connections.every(e=>record(e)
  &&label(e.id,128)&&label(e.from,128)&&label(e.to,128)&&label(e.type,100)
  &&label(e.status,100)&&label(e.label,1000))
 &&num(v.zoom)&&v.zoom>=.2&&v.zoom<=4
 &&num(v.panX)&&Math.abs(v.panX)<=5000&&num(v.panY)&&Math.abs(v.panY)<=5000;
const encounter=(v:unknown):v is TaleEncounter=>{
 if(!record(v)||!label(v.id,128)||!['Blackjack','FiveOfAKind'].includes(String(v.type))
 ||!label(v.title,2000)||!label(v.opponentName,300)||!label(v.intro,8000)
 ||!label(v.stakes,2000)||!(v.outcome===null||['Win','Loss','Draw'].includes(String(v.outcome))))return false;
 if(v.type==='Blackjack')return v.dice===null&&record(v.blackjack)&&label(v.blackjack.phase,100)
  &&Array.isArray(v.blackjack.dealer)&&v.blackjack.dealer.every(card)
  &&Array.isArray(v.blackjack.player)&&v.blackjack.player.every(card)
  &&int(v.blackjack.playerTotal,100)&&
    (v.blackjack.dealerTotal===null||int(v.blackjack.dealerTotal,100))
  &&typeof v.blackjack.canHit==='boolean'&&typeof v.blackjack.canStand==='boolean'
  &&(v.blackjack.net===null||num(v.blackjack.net));
 return v.blackjack===null&&record(v.dice)&&Array.isArray(v.dice.player)
  &&v.dice.player.length===5&&v.dice.player.every((n:unknown)=>int(n,6))
  &&Array.isArray(v.dice.held)&&v.dice.held.length===5
  &&v.dice.held.every((b:unknown)=>typeof b==='boolean')
  &&Array.isArray(v.dice.opponent)&&v.dice.opponent.length<=5
  &&v.dice.opponent.every((n:unknown)=>int(n,6))
  &&int(v.dice.rollsUsed,3)&&typeof v.dice.canRoll==='boolean'
  &&typeof v.dice.canBank==='boolean'&&textOrNull(v.dice.bestCategory)
  &&(v.dice.bestPoints===null||int(v.dice.bestPoints,1000))
  &&(v.dice.playerScore===null||int(v.dice.playerScore,1000))
  &&(v.dice.opponentScore===null||int(v.dice.opponentScore,1000))
  &&textOrNull(v.dice.playerCategory)&&textOrNull(v.dice.opponentCategory);
};
const game=(v:unknown):v is TaleRun=>{
 if(!record(v)||!label(v.runId,128)||!label(v.adventureId,100)
 ||typeof v.completed!=='boolean'||!textOrNull(v.endingId)||!int(v.rollsMade)
 ||!record(v.scene)||!label(v.scene.id,128)||!label(v.scene.title,1000)
 ||!label(v.scene.body,50000)||!textOrNull(v.scene.speaker)
 ||!label(v.scene.location,2000)||!label(v.scene.act,2000)
 ||!Array.isArray(v.choices)||v.choices.length>100||!v.choices.every(choice)
 ||!(v.pending===null||(record(v.pending)&&label(v.pending.ability,300)
  &&int(v.pending.difficulty,10000)&&textOrNull(v.pending.skillId)
  &&['Normal','Advantage','Disadvantage'].includes(String(v.pending.mode))
  &&num(v.pending.modifier)))
 ||!(v.lastRoll===null||(record(v.lastRoll)&&int(v.lastRoll.firstRoll,20)
  &&(v.lastRoll.secondRoll===null||int(v.lastRoll.secondRoll,20))
  &&int(v.lastRoll.chosenRoll,20)&&num(v.lastRoll.abilityModifier)
  &&num(v.lastRoll.skillBonus)&&num(v.lastRoll.additionalModifier)
  &&num(v.lastRoll.total)&&int(v.lastRoll.difficulty,10000)
  &&label(v.lastRoll.outcome,100)))
 ||!record(v.character)||!label(v.character.name,300)
 ||!label(v.character.classId,100)||!int(v.character.level,100)
 ||!int(v.character.experience)||!int(v.character.maxHitPoints,10000)
 ||!int(v.character.currentHitPoints,10000)||!int(v.character.armor,10000)
 ||!int(v.character.coins)
 ||!Array.isArray(v.character.abilities)||v.character.abilities.length!==6
 ||!v.character.abilities.every(a=>ability(a)&&num(a.modifier))
 ||!Array.isArray(v.character.skills)||v.character.skills.length>100
 ||!v.character.skills.every(s=>record(s)&&label(s.id,128)&&label(s.name,300)&&num(s.bonus))
 ||!Array.isArray(v.inventory)||v.inventory.length>200||!v.inventory.every(i=>record(i)
  &&label(i.itemId,128)&&int(i.quantity)&&label(i.name,300)
  &&label(i.description,5000)&&label(i.kind,100)
  &&Array.isArray(i.useChoices)&&i.useChoices.every(c=>record(c)
   &&label(c.id,128)&&label(c.text,5000)))
 ||!Array.isArray(v.conditions)||v.conditions.length>100
 ||!v.conditions.every(c=>record(c)&&label(c.conditionId,128)
  &&int(c.remaining)&&int(c.stacks)&&label(c.name,500))
 ||!Array.isArray(v.feedback)||v.feedback.length>8||!v.feedback.every(f=>record(f)
  &&label(f.type,100)&&label(f.title,1000)&&label(f.description,4000))
 ||!(v.board===null||board(v.board))
 ||!record(v.statistics)||!['scenesVisited','choicesMade','checksSucceeded',
  'checksFailed','encountersWon','encountersLost'].every(k=>int((v.statistics as Record<string,unknown>)[k])))return false;
 return true;
};
export function isTaleState(v:unknown):v is TaleState{
 if(!record(v)||!['library','story','board','encounter','complete'].includes(String(v.screen))
 ||!label(v.selectedAdventureId,100)||!Array.isArray(v.adventures)
 ||v.adventures.length<1||v.adventures.length>100||!v.adventures.every(catalog)
 ||!v.adventures.some((a:TaleCatalog)=>a.id===v.selectedAdventureId)
 ||!Array.isArray(v.classes)||v.classes.length<1||v.classes.length>100
 ||!v.classes.every(classInfo)||!label(v.defaultPlayerName,300)
 ||!int(v.started,10000)||!int(v.completed,10000)
 ||!label(v.notice,8192)||!label(v.metaNotice,8192))return false;
 if(v.screen==='library')return v.run===null&&v.encounter===null;
 if(!game(v.run))return false;
 if(v.screen==='encounter')return encounter(v.encounter)&&!v.run.completed;
 if(v.encounter!==null)return false;
 if(v.screen==='complete')return v.run.completed;
 if(v.screen==='board')return v.run.board!==null&&!v.run.completed;
 return !v.run.completed;
}
export function isTaleReply(v:unknown):v is TaleReply{
 return record(v)&&guid(v.requestId)&&typeof v.success==='boolean'
  &&label(v.message,8192)&&int(v.revision);
}
export function issueTaleIntent(session:string,revision:number,intent:TaleIntent):string{
 const requestId=crypto.randomUUID();
 window.chrome?.webview?.postMessage({type:'lounge.adventure.command',version:1,
  session,expectedRevision:revision,requestId,...intent});
 return requestId;
}
