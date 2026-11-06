import {piecePath} from './puzzleGeometry';
import type {LoungeId} from './bridge';
export function LoungePreview({id}:{id:LoungeId}){
 if(id==='sudoku'||id==='word-search')return <span className={'lounge-preview-grid '+id}>{(id==='sudoku'?'193628574':'MIDNIGHTCLUBWORD').split('').map((n,i)=><i key={i}>{n}</i>)}</span>;
 if(id==='solitaire')return <span className="lounge-preview-hand">{['A♠','Q♥','K♣'].map((n,i)=><i key={n} style={{rotate:(i*15-15)+'deg'}}>{n}</i>)}</span>;
 if(id==='memory')return <span className="lounge-preview-memory">{['☾','✦','☾','✦'].map((n,i)=><i key={i}>{n}</i>)}</span>;
 if(id==='jigsaw')return <svg className="lounge-preview-puzzle" viewBox="0 0 160 110"><defs><clipPath id="lounge-puzzle"><path d={piecePath(4,3,3,100,100)}/></clipPath></defs><image href="/album/atlas/enchanted-forest.png" width="160" height="110" opacity=".4"/><g transform="translate(35 6)"><image href="/album/atlas/enchanted-forest.png" width="100" height="100" clipPath="url(#lounge-puzzle)"/><path d={piecePath(4,3,3,100,100)} fill="none" stroke="#ead4a1" strokeWidth="2"/></g></svg>;
 return <span className="lounge-preview-story"><i>✦</i><b>TALES<br/>FROM THE<br/>BACKROOM</b><em>20</em></span>;
}
