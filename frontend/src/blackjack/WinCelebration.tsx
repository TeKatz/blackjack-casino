import type {CSSProperties} from 'react';

export type WinMoment={id:number;net:string;blackjack:boolean};
const sparks=Array.from({length:24},(_,i)=>{
 const a=i*Math.PI*2/24;
 const distance=105+(i%4)*28;
 return {x:Math.cos(a)*distance,y:Math.sin(a)*distance,
   delay:(i%5)*36,rotation:i*57};
});
export function WinCelebration({moment,language}:{moment:WinMoment|null;language:'en'|'de'}){
 if(!moment)return null;
 const de=language==='de';
 return <div key={moment.id} className="bj-win-celebration" aria-live="polite" role="status"
    data-testid="blackjack-win-celebration">
   <div className="bj-win-glow" aria-hidden="true"/>
   <div className="bj-win-sparks" aria-hidden="true">
    {sparks.map((p,i)=><span key={i} style={{
      '--spark-x':p.x+'px','--spark-y':p.y+'px',
      '--spark-delay':p.delay+'ms','--spark-turn':p.rotation+'deg'
    } as CSSProperties}/>)}
   </div>
   <div className="bj-win-plaque">
    <small>{moment.blackjack?'NATURAL 21':de?'GLÜCKWUNSCH':'CONGRATULATIONS'}</small>
    <strong>{moment.blackjack?'BLACKJACK!':de?'GEWONNEN!':'YOU WIN!'}</strong>
    <span>{'+$'+moment.net.replace(/\B(?=(\d{3})+(?!\d))/g,',')}</span>
   </div>
 </div>;
}
