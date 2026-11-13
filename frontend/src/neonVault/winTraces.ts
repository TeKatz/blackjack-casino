import {symbols} from './bridge';
import type {NeonSymbol} from './bridge';

export type WinTrace={symbol:NeonSymbol;cells:number[]};
/** Only highlight confirmed ways on reels which have already stopped. */
export function winningTraces(grid:readonly NeonSymbol[],reelsSettled:number):WinTrace[]{
 if(grid.length!==20||reelsSettled<3)return [];
 const count=Math.min(5,Math.floor(reelsSettled));
 const traces:WinTrace[]=[];
 for(const symbol of symbols.slice(0,5)){
  const cells:number[]=[];
  let previousRow=1;
  for(let reel=0;reel<count;reel++){
   const rows=[0,1,2,3].filter(row=>{
    const cell=grid[row*5+reel];return cell===symbol||cell==='Wild';
   });
   if(!rows.length)break;
   const row=rows.reduce((best,candidate)=>
    Math.abs(candidate-previousRow)<Math.abs(best-previousRow)?candidate:best,rows[0]);
   cells.push(row*5+reel);previousRow=row;
  }
  if(cells.length>=3)traces.push({symbol,cells});
 }
 return traces.sort((a,b)=>b.cells.length-a.cells.length).slice(0,3);
}
