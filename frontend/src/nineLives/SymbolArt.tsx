import {memo} from 'react';
import type {NineLivesSymbol} from './bridge';

/** Original packaged artwork, composited without its opaque square edges. */
const ART:Record<NineLivesSymbol,string>={
 Yarn:'yarn.jpg',Fishbone:'fishbone.jpg',Bell:'bell.jpg',TabbyCat:'tabby-cat.jpg',
 WhiteCat:'white-cat.jpg',BlackCat:'black-cat.jpg',Wild:'wild.jpg',Bonus:'bonus.jpg',
};
export const NineSymbolArt=memo(function NineSymbolArt({symbol}:{symbol:NineLivesSymbol}){
 return <img className="nine-symbol-art" draggable={false}
   src={'/nine-lives-art/'+ART[symbol]} alt={symbol} loading="eager"/>;
});
