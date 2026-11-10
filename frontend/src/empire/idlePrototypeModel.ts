import type {Quote,Room} from './empireBridge';

export const prototypePurchaseSizes = [1,5,10,25,50,100] as const;

export function upgradeQuote(room:Room, count:number):Quote|undefined {
  return room.quotes.find(quote=>quote.requestedLevels===count);
}

export function nextMilestone(level:number):number|null {
  if(level>=200)return null;
  return Math.min(200,Math.floor(Math.max(0,level)/10)*10+10);
}

export function milestoneProgress(level:number):number {
  return level>=200?100:Math.max(0,level%10)*10;
}

export function milestoneDistance(level:number):number {
  const next=nextMilestone(level);
  return next===null?0:next-level;
}

export function canPurchase(room:Room,count:number,disabled:boolean):boolean {
  const quote=upgradeQuote(room,count);
  return !disabled && !!quote?.canBuy && quote.levels>0;
}

export function prestigeProgress(current:number,required:number):number {
  if(required<=0)return 100;
  return Math.max(0,Math.min(100,current/required*100));
}
