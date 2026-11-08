import {MergeItemArt} from '../merge/ItemArt';
import './gamePreview.css';

/** Overlay game controls only where they add context; photographic key art owns all other tiles. */
export function GamePreview({id}:{id:string}){
 if(id==='blackjack')return <div className="native-preview preview-blackjack">
  <div className="preview-blackjack-hand"><span>A<big>♠</big></span><span>K<big>♥</big></span><b>21</b></div>
 </div>;
 if(id==='midnight-merge')return <div className="native-preview preview-midnight-merge">
  <div className="preview-merge-grid">
   {Array.from({length:12},(_,i)=><span key={i}>
    <MergeItemArt small definition={{id:'preview-'+i,name:'',lineId:'casino-chips',
      tier:i%3+1,maxTier:6,mergeResult:null,color:['#BBD0C4','#7FB59C','#73ADCE'][i%3],
      mark:['I','II','III'][i%3],stackCount:1,tags:['chip']}}/>
   </span>)}<b>✦</b>
  </div>
 </div>;
 // In particular, never cover real Dice artwork with a second set of dice,
 // or obscure the individual photographic/cinematic Slot and Club key-art.
 return null;
}
