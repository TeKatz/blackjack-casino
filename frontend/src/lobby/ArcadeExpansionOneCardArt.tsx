import type {ReactNode} from 'react';
import type {ExpansionOneId} from './arcadeExpansionOneEngine';
/** Compact illustrations of gameplay, using the same visual language as their 960×540 canvases. */
export function ArcadeExpansionOneCardArt({id}:{id:ExpansionOneId}){
 const gold='#eed09a',mint='#8ee9d5';
 const bg=(inner:ReactNode)=><svg className="arcade-scene" data-arcade-scene={id}
  viewBox="0 0 320 128" preserveAspectRatio="xMidYMid slice" aria-hidden="true" focusable="false">
  <defs>
   <linearGradient id={'exp-'+id} x1="0" x2="1" y1="0" y2="1">
    <stop stopColor="#142f46"/><stop offset=".6" stopColor="#102433"/>
    <stop offset="1" stopColor="#291e3c"/>
   </linearGradient>
  </defs>
  <rect width="320" height="128" fill={'url(#exp-'+id+')'}/>
  <path d="M0 24H320M0 56H320M0 88H320M0 120H320M32 0V128M96 0V128M160 0V128M224 0V128M288 0V128"
   stroke="#92e4d7" opacity=".07" strokeWidth="1"/>
  {inner}
 </svg>;
 switch(id){
  case 'midnight-beats':return bg(<>
   <rect x="47" y="-11" width="226" height="153" rx="15" fill="#0e253a" stroke="#c6ab83" strokeWidth="2"/>
   {['#7de8de','#f1c18c','#c4a2fb','#f58eb0'].map((color,i)=>{
    const x=67+i*48;
    return <g key={color}>
     <rect x={x} y="0" width="43" height="127" rx="4" fill={i%2?'#26334e':'#19384b'} stroke={color} strokeOpacity=".4"/>
     <rect x={x+4} y="99" width="35" height="25" rx="4" fill="#15283c" stroke={color} strokeWidth="1.5"/>
     <text x={x+21} y="117" textAnchor="middle" fontSize="14" fontWeight="700" fill={color}>{['D','F','J','K'][i]}</text>
    </g>;
   })}
   <path d="M62 95H262" stroke="#fff1c8" strokeWidth="4"/>
   <g className="exp-beats-notes">
    {[
     {x:75,y:13,c:'#7de8de'},{x:171,y:43,c:'#c4a2fb'},
     {x:123,y:65,c:'#f1c18c'},{x:219,y:26,c:'#f58eb0'},
     {x:75,y:78,c:'#7de8de'},
    ].map((n,i)=><rect key={i} x={n.x} y={n.y} width="27" height="10" rx="3"
     fill={n.c} stroke="#fff5d9" strokeOpacity=".65"/>)}
   </g>
   <g className="exp-beats-lights">
    <circle cx="27" cy="43" r="16" fill="#274962" stroke={mint} strokeWidth="2"/>
    <circle cx="293" cy="43" r="16" fill="#493a60" stroke={gold} strokeWidth="2"/>
    <circle cx="27" cy="43" r="7" fill={mint}/><circle cx="293" cy="43" r="7" fill={gold}/>
   </g>
  </>);
  case 'phantom-hook':return bg(<>
   <path d="M0 111 60 93 120 111 165 87 210 107 263 88 320 103V128H0Z" fill="#25415d" opacity=".8"/>
   <g className="exp-hook-anchors">
    <circle cx="130" cy="20" r="11" fill="#17434d" stroke={mint} strokeWidth="2"/>
    <circle cx="254" cy="33" r="11" fill="#17434d" stroke={mint} strokeWidth="2"/>
    <circle cx="130" cy="20" r="4" fill={gold}/>
    <circle cx="254" cy="33" r="4" fill={gold}/>
   </g>
   <g className="exp-hook-swing">
    <path d="M130 20 180 90" stroke={gold} strokeWidth="2.6"/>
    <rect x="168" y="78" width="24" height="24" rx="7" fill={mint} stroke={gold} strokeWidth="2"/>
    <circle cx="184" cy="86" r="2.8" fill="#123d48"/>
   </g>
  </>);
  case 'neon-drift':return bg(<>
   <path d="M119-10 201-10 265 140H55Z" fill="#2d4057" stroke={gold} strokeWidth="2"/>
   <path d="M158-7 160 16 M160 35 161 57 M162 80 164 102" stroke={mint}
    strokeDasharray="12 9" strokeWidth="2.4" opacity=".65"/>
   <g className="exp-drift-tire" opacity=".9">
    <path d="M98 89 81 107 M220 99 244 110" stroke="#8df4db" strokeWidth="3"/>
   </g>
   <g className="exp-drift-car">
    <rect x="148" y="59" width="28" height="51" rx="9" fill={mint} stroke={gold} strokeWidth="2"/>
    <rect x="153" y="70" width="18" height="16" rx="4" fill="#164257"/>
    <path d="M151 95H173" stroke="#fbd4a4" strokeWidth="3"/>
   </g>
  </>);
  case 'gravity-chamber':return bg(<>
   <rect x="44" y="12" width="232" height="103" rx="12" fill="#162c42" stroke="#6d8f98" strokeWidth="2"/>
   <rect x="50" y="16" width="220" height="9" rx="3" fill="#769eb0"/>
   <rect x="50" y="102" width="220" height="9" rx="3" fill="#769eb0"/>
   <path d="M106 102 115 83 124 102 M187 25 196 44 205 25" stroke="#ffb2af" strokeWidth="2" fill="#ac6685"/>
   <g className="exp-gravity-player">
    <rect x="145" y="71" width="27" height="27" rx="6" fill={mint} stroke={gold} strokeWidth="2"/>
    <circle cx="153" cy="82" r="2" fill="#174257"/><circle cx="164" cy="82" r="2" fill="#174257"/>
   </g>
   <g className="exp-gravity-arrow" stroke={gold} fill="none" strokeWidth="3" strokeLinecap="round">
    <path d="M185 86V41 M176 50 185 41 194 50"/>
   </g>
   <rect x="247" y="40" width="16" height="44" rx="4" stroke={gold} fill="#327878"/>
  </>);
  case 'echo-sequence':return bg(<>
   <rect x="55" y="8" width="210" height="114" rx="15" fill="#0f2a38" stroke={gold} strokeWidth="2.2"/>
   {['#5dd8c8','#eca873','#bfa5f5','#f08ba5','#8fbfff','#c7e78a'].map((color,i)=>{
    const x=69+(i%3)*64,y=20+Math.floor(i/3)*48;
    return <g key={i} className={i===2?'exp-echo-highlight':undefined}>
     <rect x={x} y={y} width="54" height="39" rx="7" fill={color} opacity=".65" stroke={color} strokeWidth="2"/>
     <rect x={x+5} y={y+5} width="44" height="29" rx="4" fill="#123044"/>
     <text x={x+27} y={y+26} textAnchor="middle" fontSize="17" fontFamily="Georgia"
      fill={color}>{i+1}</text>
    </g>;
   })}
  </>);
 }
}
