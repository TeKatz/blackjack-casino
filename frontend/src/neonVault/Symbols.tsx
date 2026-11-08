import {useId} from 'react';
import type {NeonSymbol} from './bridge';

const themes:Record<NeonSymbol,{a:string;b:string;c:string;name:string}>={
 Pulse:{a:'#72f8ff',b:'#176eab',c:'#0a284b',name:'Pulse'},
 Prism:{a:'#d9a5ff',b:'#7954df',c:'#251a58',name:'Prism'},
 Chip:{a:'#87ffe5',b:'#138c89',c:'#093e50',name:'High Roller'},
 Crown:{a:'#ffebb1',b:'#d79a44',c:'#603321',name:'Crown'},
 Key:{a:'#ffd4a1',b:'#cc7648',c:'#642a35',name:'Vault Key'},
 Wild:{a:'#ffc6fa',b:'#ed52cf',c:'#651682',name:'Wild'},
 Scatter:{a:'#ffecab',b:'#bf8f47',c:'#463c2d',name:'Vault Break'},
 VaultKey:{a:'#eec3ff',b:'#b17aff',c:'#42266f',name:'Access Key'},
};

/**
 * Full-bleed 200×136 portraiture composed for the reel window's landscape
 * cell aspect. The old 160×160 letterboxed square left wide black gutters.
 * Separate core, foil, energy and sparkle layers allow symbol-specific idle
 * motion and amplified winning feedback without rerendering any game state.
 */
export function SymbolArt({symbol,compact=false,animate=true,emphasis=false}:{
 symbol:NeonSymbol;compact?:boolean;animate?:boolean;emphasis?:boolean;
}){
 const id=useId().replaceAll(':','');
 const theme=themes[symbol];
 const metal='m'+id,shadow='s'+id,glow='g'+id,glass='v'+id,clip='c'+id,gem='q'+id;
 return <svg className={'vault-symbol vault-symbol-'+symbol.toLowerCase()
   +(animate&&!compact?' vault-symbol-animated':'')
   +(emphasis?' vault-symbol-celebrate':'')+(compact?' vault-symbol-compact':'')}
   viewBox="0 0 200 136" preserveAspectRatio="xMidYMid meet"
   data-symbol={symbol} role="img" aria-label={theme.name}>
   <defs>
     <linearGradient id={metal} x1="0" y1="0" x2=".85" y2="1">
       <stop offset="0" stopColor="#fff8e9"/>
       <stop offset=".16" stopColor={theme.a}/>
       <stop offset=".38" stopColor={theme.b}/>
       <stop offset=".68" stopColor={theme.a}/>
       <stop offset="1" stopColor={theme.b}/>
     </linearGradient>
     <linearGradient id={shadow} x1="0" y1="0" x2=".55" y2="1">
       <stop stopColor={theme.b}/><stop offset=".6" stopColor={theme.c}/>
       <stop offset="1" stopColor="#030b1d"/>
     </linearGradient>
     <radialGradient id={glow} cx=".5" cy=".5" r=".65">
       <stop offset="0" stopColor={theme.a} stopOpacity=".65"/>
       <stop offset=".55" stopColor={theme.b} stopOpacity=".23"/>
       <stop offset="1" stopColor={theme.c} stopOpacity="0"/>
     </radialGradient>
     <linearGradient id={glass} x1="0" y1="0" x2="0" y2="1">
       <stop stopColor="#ecfaff" stopOpacity=".19"/>
       <stop offset=".48" stopColor="#24466c" stopOpacity=".025"/>
       <stop offset="1" stopColor="#050a22" stopOpacity=".55"/>
     </linearGradient>
     <linearGradient id={gem} x1="0" y1="0" x2="1" y2="1">
       <stop stopColor="#ffffff"/>
       <stop offset=".34" stopColor={theme.a}/>
       <stop offset="1" stopColor={theme.b}/>
     </linearGradient>
     <clipPath id={clip}><rect x="2" y="2" width="196" height="132" rx="12"/></clipPath>
   </defs>
   <rect x="0" y="0" width="200" height="136" rx="13" fill="#050d22"/>
   <rect x="2" y="2" width="196" height="132" rx="12" fill={'url(#'+shadow+')'}/>
   <rect x="2" y="2" width="196" height="132" rx="12" fill={'url(#'+glow+')'}/>
   <g className="vault-symbol-circuit" fill="none" stroke={theme.a} strokeWidth="1" opacity=".24">
     <path d="M0 26 H28 L44 42 H67 M200 108 H172 L155 94 H134"/>
     <path d="M18 132 V115 L37 96 H54 M183 3 V22 L166 39 H147"/>
     <circle cx="27" cy="26" r="3" fill={theme.a}/>
     <circle cx="174" cy="109" r="3" fill={theme.a}/>
   </g>
   <g className="vault-symbol-energy" transform="translate(100 69)">
     <ellipse rx="79" ry="52" fill={'url(#'+glow+')'}/>
     <ellipse rx="70" ry="47" fill="none" stroke={theme.a} strokeWidth="1.4" strokeDasharray="13 22" opacity=".4"/>
   </g>
   <g className="vault-symbol-core" strokeLinejoin="round" strokeLinecap="round">
     {symbol==='Pulse'&&<>
       <g className="vault-symbol-pulse-rings" fill="none" stroke={theme.a} strokeWidth="2.7">
         <ellipse cx="100" cy="68" rx="73" ry="50" opacity=".7"/>
         <ellipse cx="100" cy="68" rx="61" ry="40" opacity=".36"/>
       </g>
       <path d="M22 70 H49 L66 49 L84 96 L102 25 L117 74 L131 58 L144 70 H178"
         fill="none" stroke="#042e46" strokeWidth="17"/>
       <path className="vault-symbol-pulse-line" d="M22 70 H49 L66 49 L84 96 L102 25 L117 74 L131 58 L144 70 H178"
         fill="none" stroke={'url(#'+metal+')'} strokeWidth="9"/>
       <circle cx="102" cy="25" r="5.5" fill="#e8fdff" stroke={theme.a} strokeWidth="3"/>
     </>}
     {symbol==='Prism'&&<>
       <path d="M100 7 L160 46 L143 109 L100 132 L56 109 L39 46 Z"
         fill="#170d36" stroke={theme.a} strokeWidth="5.5"/>
       <g className="vault-symbol-prism-facets">
         <path d="M100 7 L39 46 L100 69 Z" fill="#b9d4ff"/>
         <path d="M100 7 L160 46 L100 69 Z" fill="#b272ff"/>
         <path d="M39 46 L56 109 L100 69 Z" fill="#4523a9"/>
         <path d="M160 46 L143 109 L100 69 Z" fill="#8c45d3"/>
         <path d="M56 109 L100 132 L100 69 Z" fill="#6435ac"/>
         <path d="M143 109 L100 132 L100 69 Z" fill="#34206d"/>
       </g>
       <path d="M100 7 L100 132 M39 46 L160 46 M39 46 L100 69 L160 46 M56 109 L100 69 L143 109"
         fill="none" stroke="#f6d8ff" strokeWidth="2" opacity=".87"/>
       <path d="M94 17 L58 45 L96 63" fill="none" stroke="white" strokeWidth="4" opacity=".72"/>
     </>}
     {symbol==='Chip'&&<>
       <g className="vault-symbol-chip-spin">
         <circle cx="100" cy="68" r="57" fill="#062b40" stroke="#102b3e" strokeWidth="9"/>
         <circle cx="100" cy="68" r="54" fill="none" stroke={'url(#'+metal+')'} strokeWidth="13"/>
         {Array.from({length:12},(_,i)=><path key={i} d="M100 10 V28"
           transform={'rotate('+(i*30)+' 100 68)'}
           stroke={i%3===0?'#f6ffed':theme.a} strokeWidth="9"/>)}
         <circle cx="100" cy="68" r="40" fill="#0a4d58" stroke="#d0ffef" strokeWidth="3"/>
         <circle cx="100" cy="68" r="33" fill="none" stroke={theme.a} strokeWidth="2" strokeDasharray="5 7"/>
       </g>
       <text x="100" y="92" textAnchor="middle" fontSize="64" fontWeight="900"
         fontFamily="Georgia,serif" fill={'url(#'+metal+')'} stroke="#e8fff1" strokeWidth=".7">$</text>
     </>}
     {symbol==='Crown'&&<>
       <path d="M25 37 L53 75 L79 20 L105 77 L144 30 L172 47 L155 112 L42 112 Z"
         fill="#4b241f" stroke="#5a311a" strokeWidth="9"/>
       <path d="M25 37 L53 75 L79 20 L105 77 L144 30 L172 47 L155 112 L42 112 Z"
         fill={'url(#'+metal+')'} stroke="#fff2bf" strokeWidth="3.3"/>
       <path d="M46 88 Q102 107 159 84 L154 113 H43 Z" fill="#9f571f" opacity=".55"/>
       <rect x="42" y="106" width="116" height="18" rx="5" fill={'url(#'+shadow+')'}
         stroke="#ffe6a1" strokeWidth="3"/>
       <g className="vault-symbol-crown-gems">
         <path d="M100 76 L113 89 L100 101 L87 89 Z" fill="#ff467e" stroke="#fff1b1" strokeWidth="3"/>
         <ellipse cx="61" cy="93" rx="8" ry="6" fill="#9cddff" stroke="#fff4c2" strokeWidth="2"/>
         <ellipse cx="141" cy="93" rx="8" ry="6" fill="#9cddff" stroke="#fff4c2" strokeWidth="2"/>
       </g>
       <g className="vault-symbol-sparkles" fill="#fff6ce">
         <circle cx="25" cy="37" r="6"/><circle cx="79" cy="20" r="7"/>
         <circle cx="144" cy="30" r="7"/><circle cx="172" cy="47" r="6"/>
       </g>
     </>}
     {symbol==='Key'&&<>
       <path d="M76 69 L161 69 L161 82 L180 82 L180 100 L156 100 L156 91 L135 91 L135 105 L115 105 L115 90 L75 90"
         fill="#48292b" stroke="#321628" strokeWidth="16"/>
       <path d="M76 69 L161 69 L161 82 L180 82 L180 100 L156 100 L156 91 L135 91 L135 105 L115 105 L115 90 L75 90"
         fill="none" stroke={'url(#'+metal+')'} strokeWidth="11"/>
       <circle className="vault-symbol-key-crest" cx="62" cy="72" r="44" fill={'url(#'+metal+')'}
         stroke="#ffcba1" strokeWidth="4"/>
       <circle cx="62" cy="72" r="31" fill="#582b35" stroke="#ffd2a0" strokeWidth="5"/>
       <circle cx="62" cy="72" r="15" fill="#080e29" stroke="#ffdaae" strokeWidth="5"/>
       <path d="M86 59 L113 63 L140 63" stroke="#fff0d4" strokeWidth="3" fill="none" opacity=".8"/>
     </>}
     {symbol==='Wild'&&<>
       <g className="vault-symbol-wild-plasma">
         <path d="M102 2 L117 29 L148 15 L146 48 L177 48 L155 73 L180 100 L148 102 L143 127 L117 113 L101 134 L85 112 L54 130 L53 103 L22 103 L45 72 L25 43 L57 45 L60 16 L85 29 Z"
           fill={'url(#'+shadow+')'} stroke={'url(#'+metal+')'} strokeWidth="4"/>
         <path d="M95 14 L68 63 L89 63 L68 111 L127 52 L105 53 L128 17"
           fill="#fcb8ff" stroke="#fff0ff" strokeWidth="2"/>
         <circle cx="100" cy="68" r="50" fill="#a54bd4" opacity=".24"/>
       </g>
       <text x="100" y="83" textAnchor="middle" fontSize="46" letterSpacing="3"
         fontWeight="1000" fontFamily="Arial Black,Impact,sans-serif"
         fill="#fff2fe" stroke="#8a2ca8" strokeWidth="5" paintOrder="stroke">WILD</text>
       <path d="M43 92 H158" stroke={theme.a} strokeWidth="2.5" opacity=".8"/>
     </>}
     {symbol==='Scatter'&&<>
       <g className="vault-symbol-vault-wheel">
         <circle cx="100" cy="67" r="58" fill={'url(#'+metal+')'} stroke="#fff2bd" strokeWidth="4"/>
         {Array.from({length:12},(_,i)=><path key={i} d="M100 11 V26"
           transform={'rotate('+(i*30)+' 100 67)'} stroke="#31272b" strokeWidth="6"/>)}
         <circle cx="100" cy="67" r="45" fill={'url(#'+shadow+')'} stroke="#ffe8a4" strokeWidth="4"/>
         <circle cx="100" cy="67" r="30" fill="none" stroke="#ffdf93" strokeWidth="5"/>
         {Array.from({length:6},(_,i)=><path key={i} d="M100 34 V49"
           transform={'rotate('+(i*60)+' 100 67)'} stroke="#ffe6a1" strokeWidth="7"/>)}
         <circle cx="100" cy="67" r="14" fill="#1b2039" stroke="#fff4bf" strokeWidth="5"/>
         <path d="M98 61 H106 L102 78 H96 Z" fill="#cbeeff"/>
       </g>
       <rect x="140" y="58" width="27" height="22" rx="5" fill="#8c632c"
         stroke="#fff1b6" strokeWidth="3"/>
     </>}
     {symbol==='VaultKey'&&<>
       <g className="vault-symbol-access-halo">
         <path d="M100 3 L155 32 L166 87 L100 133 L34 87 L45 32 Z"
           fill={'url(#'+shadow+')'} stroke={'url(#'+metal+')'} strokeWidth="5"/>
         <path d="M100 18 L142 43 L149 84 L100 115 L51 84 L58 43 Z"
           fill="#201541" stroke="#f1c5ff" strokeWidth="3"/>
         <path d="M100 21 V111 M52 65 H148" stroke="#b77fe9" strokeWidth="1" opacity=".5"/>
       </g>
       <circle cx="100" cy="56" r="24" fill={'url(#'+gem+')'} stroke="#fcdeff" strokeWidth="3"/>
       <circle cx="100" cy="56" r="12" fill="#301453" stroke="#ffedff" strokeWidth="4"/>
       <path d="M91 74 L91 106 L109 106 L109 74" fill="#a279db" stroke="#ffe4ff" strokeWidth="4"/>
       <path d="M92 94 H109" stroke="#2f194e" strokeWidth="4"/>
     </>}
   </g>
   <g className="vault-symbol-glass" clipPath={'url(#'+clip+')'}>
     <path d="M4 4 H196 V47 Q104 32 4 61 Z" fill={'url(#'+glass+')'} opacity=".85"/>
     <path className="vault-symbol-foil-sweep" d="M-55 -12 L-7 -12 L76 147 L25 147 Z"
       fill="#ffffff" opacity=".19"/>
   </g>
   <g className="vault-symbol-corner-details" fill="none" stroke={theme.a} strokeWidth="1.7" opacity=".75">
     <path d="M8 35 V13 Q8 8 14 8 H35 M165 128 H186 Q192 128 192 122 V105"/>
     <path d="M8 111 V122 Q8 128 14 128 H32 M167 8 H186 Q192 8 192 14 V31"/>
   </g>
   {!compact&&<g fill="#eaffff" className="vault-symbol-sparkles" opacity=".9">
     <circle cx="20" cy="18" r="1.6"/><circle cx="182" cy="117" r="1.6"/>
   </g>}
 </svg>;
}
