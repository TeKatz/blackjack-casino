export const WHEEL=[0,32,15,19,4,21,2,25,17,34,6,27,13,36,11,30,8,23,10,5,24,16,33,1,20,14,31,9,22,18,29,7,28,12,35,3,26] as const;
export const RED=new Set([1,3,5,7,9,12,14,16,18,19,21,23,25,27,30,32,34,36]);
export const LIMIT=100000;
export type Bet={key:string;amount:number};
export type Receipt={roundId:string;success:true;number:number;stake:number;return:number;net:number;bankroll:number}|{roundId:string;success:false;message:string};
export function color(n:number){return n===0?'green':RED.has(n)?'red':'black'}
export function insideKey(numbers:number[]):string|null{
 const n=[...new Set(numbers)].sort((a,b)=>a-b), a=n[0];
 if(n.some(v=>!Number.isInteger(v)||v<0||v>36))return null;
 const same=(x:number[])=>x.length===n.length&&x.every((v,i)=>v===n[i]);
 const valid=n.length===2?(a===0?n[1]<=3:n[1]===a+3||(a%3!==0&&n[1]===a+1)):
 n.length===3?(a===0?same([0,1,2])||same([0,2,3]):a%3===1&&same([a,a+1,a+2])):
 n.length===4?(a===0?same([0,1,2,3]):a%3!==0&&same([a,a+1,a+3,a+4])):
 n.length===6&&a>0&&a%3===1&&same(Array.from({length:6},(_,i)=>a+i));
 return valid?'i'+n.join(','):null;
}
export function covered(key:string):number[]{
 if(/^n(?:[0-9]|[12][0-9]|3[0-6])$/.test(key))return [+key.slice(1)];
 if(key[0]==='i'){const n=key.slice(1).split(',').map(Number);return insideKey(n)===key?n:[];}
 const all=Array.from({length:36},(_,i)=>i+1);
 switch(key){case 'red':return all.filter(n=>RED.has(n));case 'black':return all.filter(n=>!RED.has(n));case 'even':return all.filter(n=>n%2===0);case 'odd':return all.filter(n=>n%2!==0);case 'low':return all.slice(0,18);case 'high':return all.slice(18);}
 if(/^[dc][123]$/.test(key)){const i=+key[1]-1;return key[0]==='d'?all.slice(i*12,i*12+12):all.filter(n=>(n-1)%3===i);}
 return [];
}
export function settlePreview(roundId:string,bets:Bet[],bankroll:number,number:number):Receipt{
 const stake=bets.reduce((s,b)=>s+b.amount,0);
 if(!Number.isInteger(number)||number<0||number>36||stake>bankroll||stake>LIMIT||!bets.length||new Set(bets.map(b=>b.key)).size!==bets.length||bets.some(b=>!Number.isInteger(b.amount)||b.amount<100||!covered(b.key).length))return {roundId,success:false,message:'Invalid bets'};
 const returned=bets.reduce((s,b)=>{const n=covered(b.key);return s+(n.includes(number)?b.amount*36/n.length:0)},0);
 return {roundId,success:true,number,stake,return:returned,net:returned-stake,bankroll:bankroll-stake+returned};
}
export function randomNumber(){const a=new Uint32Array(1);do{crypto.getRandomValues(a)}while(a[0]>=4294967289);return a[0]%37;}
export function pocketAngle(n:number){return WHEEL.indexOf(n as typeof WHEEL[number])*Math.PI*2/37-Math.PI/2;}
