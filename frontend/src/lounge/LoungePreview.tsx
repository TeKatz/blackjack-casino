import {piecePath} from './puzzleGeometry';
import type {LoungeId} from './bridge';
export function LoungePreview({id}:{id:LoungeId}){
 if(id==='sudoku'||id==='word-search')return <span className={'lounge-preview-grid '+id}>{(id==='sudoku'?'193628574':'MIDNIGHTCLUBWORD').split('').map((n,i)=><i key={i}>{n}</i>)}</span>;
 if(id==='solitaire')return <span className="lounge-preview-hand">{['A♠','Q♥','K♣'].map((n,i)=><i key={n} style={{rotate:(i*15-15)+'deg'}}>{n}</i>)}</span>;
 if(id==='memory')return <span className="lounge-preview-memory">{['☾','✦','☾','✦'].map((n,i)=><i key={i}>{n}</i>)}</span>;
 if(id==='tower-defense')return <svg className="lounge-bastion-preview" viewBox="0 0 520 230" role="img" aria-label="Illustrated midnight fortress under siege">
  <defs><linearGradient id="bastion-night" x1="0" y1="0" x2=".8" y2="1">
   <stop stopColor="#152e3e"/><stop offset="1" stopColor="#0e251c"/></linearGradient>
   <linearGradient id="bastion-gold" x1="0" y1="0" x2="0" y2="1">
    <stop stopColor="#f4d59d"/><stop offset="1" stopColor="#8a6742"/></linearGradient></defs>
  <rect width="520" height="230" fill="url(#bastion-night)"/>
  <circle cx="395" cy="44" r="29" fill="#d7d6af" opacity=".75"/>
  <circle cx="408" cy="36" r="27" fill="#162d38"/>
  <g opacity=".35" fill="#b8ccad"><circle cx="25" cy="23" r="2"/><circle cx="118" cy="57" r="2"/>
   <circle cx="230" cy="26" r="1.8"/><circle cx="486" cy="81" r="2"/></g>
  <path d="M0 175 L115 160 L205 193 L315 138 L520 168 V230 H0Z" fill="#183d32"/>
  <path d="M-15 199 C125 210 137 100 238 117 S390 190 532 140" stroke="#9f896b"
   strokeWidth="46" fill="none" strokeLinecap="round"/>
  <path d="M-15 199 C125 210 137 100 238 117 S390 190 532 140" stroke="#e0c28c"
   strokeWidth="1.5" fill="none" strokeDasharray="7 10" opacity=".5"/>
  <g transform="translate(80 69)">
   <rect x="7" y="56" width="100" height="83" rx="4" fill="#6f7c76" stroke="#d6b17e" strokeWidth="2"/>
   <rect x="37" y="12" width="44" height="127" rx="3" fill="#9aa6a0" stroke="#ddc28d" strokeWidth="3"/>
   <path d="M29 22 L58 -1 L89 22Z" fill="#d6a76d"/>
   <rect x="-7" y="42" width="33" height="97" fill="#81928b" stroke="#d0b487" strokeWidth="2"/>
   <rect x="89" y="42" width="33" height="97" fill="#81928b" stroke="#d0b487" strokeWidth="2"/>
   <rect x="46" y="88" width="26" height="51" rx="12" fill="#142d2b"/>
   <path d="M58 2V-23" stroke="#efcf8b" strokeWidth="3"/>
   <path d="M58 -23 L82 -14 L58 -7Z" fill="#d6a46f"/>
   <rect x="43" y="38" width="32" height="19" rx="8" fill="#e8c17a"/>
  </g>
  <g fill="url(#bastion-gold)" stroke="#3c2919" strokeWidth="2">
   <circle cx="287" cy="91" r="18"/><circle cx="441" cy="143" r="20"/></g>
  <g stroke="#bde9f4" strokeWidth="4" fill="none">
   <path d="M286 90 L335 52 L372 88 L421 55" />
   <circle cx="335" cy="52" r="5" fill="#ffffff"/>
   <circle cx="372" cy="88" r="4" fill="#ffffff"/>
  </g>
  <g fill="#c99c7e"><circle cx="346" cy="162" r="8"/><circle cx="365" cy="171" r="7"/>
   <circle cx="387" cy="170" r="9"/></g>
  <path d="M14 218H505" stroke="#d6ad78" opacity=".55" strokeWidth="2"/>
 </svg>;
 if(id==='jigsaw')return <svg className="lounge-preview-puzzle" viewBox="0 0 160 110"><defs><clipPath id="lounge-puzzle"><path d={piecePath(4,3,3,100,100)}/></clipPath></defs><image href="/album/atlas/enchanted-forest.png" width="160" height="110" opacity=".4"/><g transform="translate(35 6)"><image href="/album/atlas/enchanted-forest.png" width="100" height="100" clipPath="url(#lounge-puzzle)"/><path d={piecePath(4,3,3,100,100)} fill="none" stroke="#ead4a1" strokeWidth="2"/></g></svg>;
 return <span className="lounge-preview-story"><i>✦</i><b>TALES<br/>FROM THE<br/>BACKROOM</b><em>20</em></span>;
}
