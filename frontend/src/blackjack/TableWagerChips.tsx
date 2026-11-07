import type {CSSProperties} from 'react';

export function stackHeights(wager:string):number[]{
 const amount=Number(wager);
 if(!Number.isFinite(amount)||amount<=0)return [];
 // Visual-only logarithmic growth: 1 chip at $1, capped stacks for large bets.
 const height=Math.max(1,Math.min(12,1+Math.floor(Math.log10(Math.max(1,amount))*2)));
 if(height<=3)return [height];
 if(height<=7)return [Math.min(height,5),height-3];
 return [Math.min(6,height-3),Math.min(5,height-5),Math.min(5,height-7)];
}
export function TableWagerChips({wager,animationsEnabled}:{
 wager:string;animationsEnabled:boolean;
}){
 const stacks=stackHeights(wager);
 if(stacks.length===0)return null;
 return <div className="bj-wager-display" aria-hidden="true"
   data-testid="blackjack-table-wager-chips" data-chip-count={stacks.reduce((a,b)=>a+b,0)}
   data-animation={animationsEnabled}>
   <div className="bj-wager-shadows"/>
   <div className="bj-wager-stacks">
     {stacks.map((height,stack)=><div key={stack} className={'bj-wager-stack stack-'+stack}
       style={{'--stack-index':stack} as CSSProperties}>
       {Array.from({length:height},(_,index)=><span key={index}
         className={'bj-wager-chip token-'+((stack+index)%3)}
         style={{'--chip-height':index,'--chip-delay':(stack*95+index*45)+'ms'} as CSSProperties}>
         <i aria-hidden="true">✦</i>
       </span>)}
     </div>)}
   </div>
   <span className="bj-wager-display-label">WAGER</span>
 </div>;
}
