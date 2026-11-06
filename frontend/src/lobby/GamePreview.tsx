import {ReelIcon} from '../reels/symbols';
import {SymbolArt} from '../neonVault/Symbols';
import {manorSymbolSvg} from '../graphics/manorArtwork';
import {MergeItemArt} from '../merge/ItemArt';
import {DicePreview} from './Cosmetics';
import './gamePreview.css';
export function GamePreview({id}:{id:string}){
 if(id==='nine-lives')return null;
 return <div className={'native-preview preview-'+id}>
 {id==='blackjack'?<div className="preview-blackjack-hand"><span>A<big>♠</big></span><span>K<big>♥</big></span><b>21</b></div>
 :id==='kniffel'?<DicePreview variant="dice-obsidian"/>
 :id==='midnight-reels'?<div className="preview-reel-strip">{(['Cherry','Seven','Bar'] as const).map(symbol=><span key={symbol}><ReelIcon symbol={symbol}/></span>)}</div>
 :id==='neon-vault'?<div className="preview-reel-strip neon-preview">{(['Prism','Wild','VaultKey'] as const).map(symbol=><span key={symbol}><SymbolArt symbol={symbol}/></span>)}</div>
 :id==='midnight-manor'?<div className="preview-reel-strip manor-preview">{(['Candelabrum','Lady','Bonus'] as const).map(symbol=><span key={symbol}><img alt="" src={'data:image/svg+xml;charset=utf-8,'+encodeURIComponent(manorSymbolSvg(symbol))}/></span>)}</div>
 :id==='midnight-merge'?<div className="preview-merge-grid">{Array.from({length:12},(_,i)=><span key={i}><MergeItemArt small definition={{id:'preview-'+i,name:'',lineId:'casino-chips',tier:i%3+1,maxTier:6,mergeResult:null,color:['#BBD0C4','#7FB59C','#73ADCE'][i%3],mark:['I','II','III'][i%3],stackCount:1,tags:['chip']}}/></span>)}<b>✦</b></div>
 :id==='casino-empire'?<div className="preview-empire"><span>♜</span><span>♜</span><strong>♛</strong><span>♜</span><span>♜</span></div>
 :<div className="preview-lounge-cards"><span>♠</span><span>1 9<br/>3 6</span><span>✧</span></div>}
 </div>;
}
