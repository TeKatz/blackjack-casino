import {piecePath} from './puzzleGeometry';
import type {LoungeId} from './bridge';
export function LoungePreview({id}:{id:LoungeId}){
 if(id==='sudoku'||id==='word-search')return <span className={'lounge-preview-grid '+id}>{(id==='sudoku'?'193628574':'MIDNIGHTCLUBWORD').split('').map((n,i)=><i key={i}>{n}</i>)}</span>;
 if(id==='solitaire')return <span className="lounge-preview-hand">{['A♠','Q♥','K♣'].map((n,i)=><i key={n} style={{rotate:(i*15-15)+'deg'}}>{n}</i>)}</span>;
 if(id==='memory')return <span className="lounge-preview-memory">{['☾','✦','☾','✦'].map((n,i)=><i key={i}>{n}</i>)}</span>;
 if(id==='minesweeper')return <svg className="lounge-sweep-preview" viewBox="0 0 520 230" role="img" aria-label="Moonlit grid with luminous mines and crystal markers">
  <defs>
   <radialGradient id="sweep-preview-night"><stop stopColor="#355e78"/><stop offset="1" stopColor="#102334"/></radialGradient>
   <linearGradient id="sweep-preview-tile" x1="0" x2=".9" y1="0" y2="1"><stop stopColor="#56869c"/><stop offset="1" stopColor="#1c344c"/></linearGradient>
  </defs>
  <rect width="520" height="230" fill="url(#sweep-preview-night)"/>
  <g fill="#d5e5e1" opacity=".46">{Array.from({length:32},(_,i)=><circle key={i}
   cx={12+(i*137)%510} cy={11+(i*79)%209} r={i%5===0?2:1}/>)}</g>
  <circle cx="112" cy="55" r="45" fill="#b2dce7" opacity=".12"/>
  <circle cx="112" cy="55" r="38" stroke="#c0e1e2" opacity=".54" fill="none"/>
  <circle cx="112" cy="55" r="27" stroke="#f0d79c" opacity=".4" fill="none"/>
  <path d="M112 11V100M69 55H156" stroke="#a3dce7" opacity=".4"/>
  <g transform="translate(232 17) rotate(-7 110 99)">
   {Array.from({length:35},(_,i)=>{
    const x=(i%7)*39,y=Math.floor(i/7)*39;
    const opened=[5,11,12,18,19,20,26,27,33,34].includes(i);
    const mine=[9,23].includes(i);
    const flag=[2,30].includes(i);
    return <g key={i} transform={`translate(${x} ${y})`}>
     <rect width="34" height="34" rx="5" fill={opened?"#1c394e":"url(#sweep-preview-tile)"}
      stroke={flag?"#f3d18e":"#8ac0c5"} strokeOpacity={opened?.3:.68}/>
     {opened&&<text x="17" y="24" fontSize="20" textAnchor="middle" fontWeight="bold"
      fill="#b1e4dd">{i%4===0?'2':'1'}</text>}
     {mine&&<g><circle cx="17" cy="17" r="8" fill="#e897a6"/><circle cx="17" cy="17" r="4" fill="#fce1ba"/>
      <path d="M17 3V9M17 25V31M3 17H9M25 17H31" stroke="#f7c8b3" strokeWidth="3"/></g>}
     {flag&&<path d="M13 26V8L25 14L13 18" fill="#f5d29a" stroke="#f5d29a" strokeWidth="2"/>}
    </g>;
   })}
  </g>
  <path d="M0 201 Q70 174 150 197T310 193T520 182" stroke="#a7dee8" strokeWidth="1.4" opacity=".3" fill="none"/>
 </svg>;
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
 if(id==='jigsaw')return <svg className="lounge-preview-puzzle" viewBox="0 0 160 110"><defs><clipPath id="lounge-puzzle"><path d={piecePath(4,3,3,100,100)}/></clipPath></defs><image href="/jigsaw/scenes/enchanted-forest.jpg" width="160" height="110" preserveAspectRatio="xMidYMid slice" opacity=".48"/><g transform="translate(35 6)"><image href="/jigsaw/scenes/enchanted-forest.jpg" width="100" height="100" preserveAspectRatio="xMidYMid slice" clipPath="url(#lounge-puzzle)"/><path d={piecePath(4,3,3,100,100)} fill="none" stroke="#ead4a1" strokeWidth="2"/></g></svg>;
 return <span className="lounge-preview-story"><i>✦</i><b>TALES<br/>FROM THE<br/>BACKROOM</b><em>20</em></span>;
}
