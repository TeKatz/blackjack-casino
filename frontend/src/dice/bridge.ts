export type DiceCategory='Ones'|'Twos'|'Threes'|'Fours'|'Fives'|'Sixes'|
 'ThreeOfAKind'|'FourOfAKind'|'FullHouse'|'SmallStraight'|'LargeStraight'|'Kniffel'|'Chance';
export const categories:DiceCategory[]=['Ones','Twos','Threes','Fours','Fives','Sixes',
 'ThreeOfAKind','FourOfAKind','FullHouse','SmallStraight','LargeStraight','Kniffel','Chance'];
export type DiceScore={id:DiceCategory;name:string;stored:number|null;preview:number|null;upper:boolean};
export type DiceGame={gameId:string;dice:number[];held:boolean[];rollsUsed:number;filledCount:number;
 isComplete:boolean;canScore:boolean;canRoll:boolean;isAdditionalKniffel:boolean;kniffelsScored:number;
 upperSubtotal:number;upperBonus:number;lowerSubtotal:number;additionalKniffelBonus:number;total:number;
 categories:DiceScore[]};
export type DiceOpponent={name:string;style:string;game:DiceGame};
export type DiceTable={stake:string;settled:boolean;forfeited:boolean;payout:string;net:string;placement:number;
 waiting:boolean;lastMoves:string[];tied:boolean;opponents:DiceOpponent[]};
export type DicePalette={top:string;bottom:string;pips:string;edge:string;glow:string|null;engraved:boolean};
export type DiceCelebration={id:number;player:string;category:DiceCategory;points:number;bonus:number;face:number};
export type DiceState={language:'en'|'de';mode:'casual'|'casino';bankroll:string;
 stakeOptions:string[];stakePrizes:{stake:string;prizes:string[]}[];
 casualHasSaved:boolean;casualCompleted:boolean;game:DiceGame;table:DiceTable|null;
 profile:{level:number;prestige:number;coins:number}|null;diceVariant:string;dicePalette:DicePalette;
 soundEnabled:boolean;animationsEnabled:boolean;musicEnabled:boolean;musicVolume:number;
 notice:string;completionNotice:string;events:DiceCelebration[];rollToken:number;rollingIndices:number[];
 completionXp:number;completionSeasonXp:number};
export type DiceSnapshot={version:1;session:string;revision:number;state:DiceState};
export type DiceReply={requestId:string;success:boolean;message:string;revision:number};
export type DiceIntent=
 |{action:'roll'|'casual.start'|'casual.resume'|'casino.resume'|'casino.forfeit'}
 |{action:'hold';index:number}
 |{action:'score'|'scratch';category:DiceCategory}
 |{action:'casino.start';value:string}
 |{action:'sound.set'|'animations.set'|'music.set';value:'true'|'false'};
const obj=(v:unknown):v is Record<string,unknown>=>v!==null&&typeof v==='object'&&!Array.isArray(v);
const count=(v:unknown,max=1000000000):v is number=>typeof v==='number'&&Number.isSafeInteger(v)&&v>=0&&v<=max;
const txt=(v:unknown):v is string=>typeof v==='string'&&v.length<=8192;
const guid=(v:unknown):v is string=>typeof v==='string'&&/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(v);
const cash=(v:unknown):v is string=>typeof v==='string'&&/^(0|[1-9]\d{0,14})(\.\d{1,4})?$/.test(v);
const signedCash=(v:unknown):v is string=>typeof v==='string'&&/^-?(0|[1-9]\d{0,14})(\.\d{1,4})?$/.test(v);
const color=(v:unknown):v is string=>typeof v==='string'&&/^#[a-f0-9]{6}$/i.test(v);
const score=(v:unknown):v is DiceScore=>obj(v)&&categories.includes(v.id as DiceCategory)&&txt(v.name)
 &&(v.stored===null||count(v.stored,1000))&&(v.preview===null||count(v.preview,1000))&&typeof v.upper==='boolean';
const game=(v:unknown):v is DiceGame=>obj(v)&&guid(v.gameId)
 &&Array.isArray(v.dice)&&v.dice.length===5&&v.dice.every(x=>count(x,6))
 &&Array.isArray(v.held)&&v.held.length===5&&v.held.every(x=>typeof x==='boolean')
 &&count(v.rollsUsed,3)&&count(v.filledCount,13)
 &&typeof v.isComplete==='boolean'&&typeof v.canScore==='boolean'&&typeof v.canRoll==='boolean'
 &&typeof v.isAdditionalKniffel==='boolean'
 &&[v.kniffelsScored,v.upperSubtotal,v.upperBonus,v.lowerSubtotal,v.additionalKniffelBonus,v.total].every(x=>count(x,3000))
 &&Array.isArray(v.categories)&&v.categories.length===13&&v.categories.every(score)
 &&new Set(v.categories.map(x=>(x as DiceScore).id)).size===13
 &&((v.rollsUsed===0&&v.dice.every(x=>x===0)&&v.held.every(x=>x===false))
   ||(v.rollsUsed>0&&v.dice.every(x=>x>=1&&x<=6)))
 &&v.filledCount===v.categories.filter(x=>(x as DiceScore).stored!==null).length
 &&v.isComplete===(v.filledCount===13);
const table=(v:unknown):v is DiceTable=>obj(v)&&cash(v.stake)&&typeof v.settled==='boolean'
 &&typeof v.forfeited==='boolean'&&signedCash(v.net)&&cash(v.payout)
 &&count(v.placement,4)&&typeof v.waiting==='boolean'&&typeof v.tied==='boolean'
 &&Array.isArray(v.lastMoves)&&v.lastMoves.length<=3&&v.lastMoves.every(txt)
 &&Array.isArray(v.opponents)&&v.opponents.length===3&&v.opponents.every(x=>
  obj(x)&&txt(x.name)&&txt(x.style)&&game(x.game));
const palette=(v:unknown):v is DicePalette=>obj(v)&&[v.top,v.bottom,v.pips,v.edge].every(color)
 &&(v.glow===null||color(v.glow))&&typeof v.engraved==='boolean';
const celebration=(v:unknown):v is DiceCelebration=>obj(v)&&count(v.id)
 &&txt(v.player)&&categories.includes(v.category as DiceCategory)&&count(v.points,1000)
 &&count(v.bonus,1000)&&count(v.face,6);
export function isDiceSnapshot(payload:unknown):payload is DiceSnapshot{
 if(!obj(payload)||payload.version!==1||!guid(payload.session)||!count(payload.revision)||!obj(payload.state))return false;
 const v=payload.state;
 return (v.language==='en'||v.language==='de')&&(v.mode==='casual'||v.mode==='casino')
 &&cash(v.bankroll)&&Array.isArray(v.stakeOptions)&&v.stakeOptions.length===6&&v.stakeOptions.every(cash)
 &&Array.isArray(v.stakePrizes)&&v.stakePrizes.length===6&&v.stakePrizes.every(x=>obj(x)&&cash(x.stake)
 &&Array.isArray(x.prizes)&&x.prizes.length===4&&x.prizes.every(cash))
 &&typeof v.casualHasSaved==='boolean'&&typeof v.casualCompleted==='boolean'&&game(v.game)
 &&(v.table===null||table(v.table))&&(v.mode!=='casino'||v.table!==null)
 &&(v.profile===null||(obj(v.profile)&&count(v.profile.level)&&count(v.profile.prestige)&&count(v.profile.coins)))
 &&txt(v.diceVariant)&&palette(v.dicePalette)&&typeof v.soundEnabled==='boolean'
 &&typeof v.animationsEnabled==='boolean'&&typeof v.musicEnabled==='boolean'
 &&typeof v.musicVolume==='number'&&v.musicVolume>=0&&v.musicVolume<=1
 &&txt(v.notice)&&txt(v.completionNotice)
 &&Array.isArray(v.events)&&v.events.length<=24&&v.events.every(celebration)
 &&count(v.rollToken)&&Array.isArray(v.rollingIndices)&&v.rollingIndices.length<=5
 &&v.rollingIndices.every(x=>count(x,4))&&new Set(v.rollingIndices).size===v.rollingIndices.length
 &&count(v.completionXp)&&count(v.completionSeasonXp);
}
export function isDiceReply(v:unknown):v is DiceReply{
 return obj(v)&&guid(v.requestId)&&typeof v.success==='boolean'&&txt(v.message)&&count(v.revision);
}
export function issueDiceIntent(snapshot:DiceSnapshot,intent:DiceIntent):string{
 const requestId=crypto.randomUUID();
 window.chrome?.webview?.postMessage({type:'midnight-dice.command',version:1,requestId,
   session:snapshot.session,expectedRevision:snapshot.revision,...intent});
 return requestId;
}
export function listenDice(onEvent:(event:{type:'snapshot';data:DiceSnapshot}|
 {type:'reply';data:DiceReply})=>void):()=>void{
 const browser=window.chrome?.webview;
 const handler=(event:MessageEvent)=>{
   const msg:unknown=event.data;
   if(!obj(msg))return;
   if(msg.type==='midnight-dice.snapshot'){
     const valid=isDiceSnapshot(msg.data);
     const envelope=obj(msg.data)?msg.data:null;
     const s=envelope&&obj(envelope.state)?envelope.state:null;
     const checks:Record<string,boolean>={
       envelope:!!envelope&&envelope.version===1&&guid(envelope.session)&&count(envelope.revision),
       state:!!s,
       language:!!s&&(s.language==='en'||s.language==='de'),
       mode:!!s&&(s.mode==='casual'||s.mode==='casino'),
       money:!!s&&cash(s.bankroll),
       stakes:!!s&&Array.isArray(s.stakeOptions)&&s.stakeOptions.length===6&&s.stakeOptions.every(cash),
       stakePrizes:!!s&&Array.isArray(s.stakePrizes)&&s.stakePrizes.length===6&&s.stakePrizes.every(x=>
         obj(x)&&cash(x.stake)&&Array.isArray(x.prizes)&&x.prizes.length===4&&x.prizes.every(cash)),
       casual:!!s&&typeof s.casualHasSaved==='boolean'&&typeof s.casualCompleted==='boolean',
       game:!!s&&game(s.game),
       table:!!s&&(s.table===null||table(s.table))&&(s.mode!=='casino'||s.table!==null),
       profile:!!s&&(s.profile===null||(obj(s.profile)&&count(s.profile.level)&&count(s.profile.prestige)&&count(s.profile.coins))),
       variant:!!s&&txt(s.diceVariant),
       palette:!!s&&palette(s.dicePalette),
       preferences:!!s&&typeof s.soundEnabled==='boolean'&&typeof s.animationsEnabled==='boolean'
         &&typeof s.musicEnabled==='boolean'&&typeof s.musicVolume==='number'&&s.musicVolume>=0&&s.musicVolume<=1,
       strings:!!s&&txt(s.notice)&&txt(s.completionNotice),
       events:!!s&&Array.isArray(s.events)&&s.events.length<=24&&s.events.every(celebration),
       roll:!!s&&count(s.rollToken)&&Array.isArray(s.rollingIndices)&&s.rollingIndices.length<=5
        &&s.rollingIndices.every(x=>count(x,4))&&new Set(s.rollingIndices).size===s.rollingIndices.length,
       rewards:!!s&&count(s.completionXp)&&count(s.completionSeasonXp),
     };
     (window as Window & {__diceBridgeDiagnostics?:string}).__diceBridgeDiagnostics=JSON.stringify({
       valid,failed:Object.entries(checks).filter(([,pass])=>!pass).map(([key])=>key),
       stateKeys:s?Object.keys(s):[],gameKeys:s&&obj(s.game)?Object.keys(s.game):[],
       die:s&&obj(s.game)?s.game.dice:null,
       stakeCount:s&&Array.isArray(s.stakeOptions)?s.stakeOptions.length:null,
       prizeCount:s&&Array.isArray(s.stakePrizes)?s.stakePrizes.length:null,
       mode:s?.mode,version:envelope?.version,
     });
     if(valid)onEvent({type:'snapshot',data:msg.data as DiceSnapshot});
   }
   if(msg.type==='midnight-dice.result'&&isDiceReply(msg.data))
     onEvent({type:'reply',data:msg.data});
 };
 browser?.addEventListener('message',handler);
 return()=>browser?.removeEventListener('message',handler);
}
