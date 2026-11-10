import type {Preview,Quote,Research,Room} from './empireBridge';

export const commandPurchaseSizes=[1,5,10,25,50,100] as const;

export const commandQuote=(room:Room,count:number):Quote|undefined=>
  room.quotes.find(quote=>quote.requestedLevels===count);

export function commandMilestone(level:number):{next:number|null;remaining:number;progress:number}{
  if(level>=200)return {next:null,remaining:0,progress:100};
  const next=Math.min(200,Math.floor(Math.max(0,level)/10)*10+10);
  return {next,remaining:next-level,progress:Math.max(0,level%10)*10};
}

export function commandCanBuy(room:Room,count:number,disabled:boolean):boolean{
  const quote=commandQuote(room,count);
  return !disabled&&!!quote?.canBuy&&quote.levels>0;
}

export function commandProgress(current:number,required:number):number{
  if(required<=0)return 100;
  return Math.max(0,Math.min(100,current/required*100));
}

// Exact subtraction of Core decimal strings. No JS floating-point arithmetic is
// used to determine whether an investment is positive or negative.
export function decimalDifference(after:string,before:string):string{
  const a=after.split('.'),b=before.split('.');
  const precision=Math.max((a[1]??'').length,(b[1]??'').length);
  const read=(p:string[])=>BigInt(p[0]+(p[1]??'').padEnd(precision,'0'));
  const delta=read(a)-read(b),negative=delta<0n,abs=negative?-delta:delta;
  const raw=abs.toString().padStart(precision+1,'0');
  const whole=precision?raw.slice(0,-precision):raw;
  const fractional=precision?raw.slice(-precision).replace(/0+$/,''):'';
  return (negative?'-':'')+whole+(fractional?'.'+fractional:'');
}

export function commandImpact(preview:Preview):{
  capital:string;propertyIncome:string;portfolioIncome:string;
  paybackSeconds:number|null;incomePercent:number|null;
}{
  const capital=decimalDifference(preview.budgetAfter,preview.budgetBefore);
  const propertyIncome=decimalDifference(preview.roomAfter.final,preview.roomBefore.final);
  const portfolioIncome=decimalDifference(preview.portfolioAfter,preview.portfolioBefore);
  const cost=Math.max(0,-Number(capital)),gain=Number(portfolioIncome);
  const before=Number(preview.portfolioBefore);
  return {
    capital,propertyIncome,portfolioIncome,
    paybackSeconds:gain>0&&Number.isFinite(cost/gain)?cost/gain:null,
    incomePercent:before>0&&Number.isFinite(gain/before)?gain/before*100:null,
  };
}

// Dependencies are authoritative. Coordinates are presentation-only and stable
// across production snapshots so Pixi links and accessible buttons never jump.
export function researchCoordinates(items:Research[]):Record<string,{x:number;y:number}>{
  const depths=new Map<string,number>();
  const findDepth=(item:Research,seen=new Set<string>()):number=>{
    if(seen.has(item.id))return 0;
    if(depths.has(item.id))return depths.get(item.id)!;
    seen.add(item.id);
    const prerequisite=items.find(other=>other.id===item.prerequisite);
    const depth=prerequisite?findDepth(prerequisite,seen)+1:0;
    depths.set(item.id,depth);
    return depth;
  };
  items.forEach(item=>findDepth(item));
  const maxDepth=Math.max(0,...depths.values());
  const groups=new Map<number,Research[]>();
  items.forEach(item=>groups.set(depths.get(item.id)??0,[...(groups.get(depths.get(item.id)??0)??[]),item]));
  const result:Record<string,{x:number;y:number}>={};
  groups.forEach((group,depth)=>group.forEach((item,index)=>{
    result[item.id]={x:items.length===1?50:15+depth/Math.max(1,maxDepth)*70,
      y:group.length===1?50:18+index/(group.length-1)*64};
  }));
  return result;
}


// Mirrors the catalog-driven Core CanResearch prerequisites without overriding
// its authoritative canBuy flag. Distinguish affordability from dependencies.
export function researchRequirements(node:Research,all:Research[],points:number){
  const prerequisite=node.prerequisite?all.find(r=>r.id===node.prerequisite):undefined;
  return {maxed:node.rank>=node.maxRank,missingPoints:Math.max(0,node.costPerRank-points),
    prerequisite:node.prerequisite?{name:prerequisite?.name??node.prerequisite,
      current:prerequisite?.rank??0,required:node.prerequisiteRank,
      met:(prerequisite?.rank??0)>=node.prerequisiteRank}:null};
}
