import {useId} from 'react';
import type {NeonSymbol} from './bridge';

const palette:Record<NeonSymbol,string>={
 Pulse:'#54ecff',Prism:'#ad7aff',Chip:'#58f2d5',Crown:'#ffd47a',
 Key:'#ffad70',Wild:'#ff70e9',Scatter:'#ffe88d',VaultKey:'#dba6ff',
};
export function SymbolArt({symbol,compact=false}:{symbol:NeonSymbol;compact?:boolean}){
 const uuid=useId().replaceAll(':','');
 const color=palette[symbol];
 const grad='g'+uuid,halo='h'+uuid,glow='l'+uuid;
 return <svg className="vault-symbol" data-symbol={symbol} viewBox="0 0 160 160" role="img"
   aria-label={symbol==='VaultKey'?'Access key':symbol==='Scatter'?'Vault Break':symbol}>
   <defs>
     <linearGradient id={grad} x1="0" y1="0" x2="1" y2="1">
       <stop offset="0" stopColor="#ffffff"/><stop offset=".35" stopColor={color}/><stop offset="1" stopColor="#6b367b"/>
     </linearGradient>
     <radialGradient id={halo}><stop stopColor={color} stopOpacity=".45"/><stop offset="1" stopColor={color} stopOpacity="0"/></radialGradient>
     <filter id={glow} x="-40%" y="-40%" width="180%" height="180%">
       <feGaussianBlur stdDeviation="3.4" result="b"/>
       <feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge>
     </filter>
   </defs>
   <rect width="160" height="160" rx="16" fill="#091126"/>
   <circle cx="80" cy="77" r="76" fill={'url(#'+halo+')'}/>
   <g stroke={color} fill="none" opacity=".22" strokeWidth=".7">
     {[24,50,76,102,128].map(i=><g key={i}><path d={'M'+i+' 10 V150'}/><path d={'M10 '+i+' H150'}/></g>)}
   </g>
   <rect x="7" y="7" width="146" height="146" rx="13" stroke={color} strokeWidth="3" fill="none" opacity=".55"/>
   <path d="M19 40 V20 H40 M120 140 H140 V120" stroke="#9ef7ff" strokeWidth="2" fill="none" opacity=".6"/>
   <g stroke={'url(#'+grad+')'} strokeWidth="5" strokeLinejoin="round"
     strokeLinecap="round" filter={'url(#'+glow+')'}>
     {symbol==='Pulse'&&<><circle cx="80" cy="82" r="43" fill="none" strokeWidth="3" opacity=".55"/>
       <path d="M20 86 H50 L63 62 L77 113 L92 52 L104 86 H140" fill="none" strokeWidth="8"/></>}
     {symbol==='Prism'&&<><path d="M80 21 L128 72 L109 129 L51 129 L32 72 Z" fill="#23183d" strokeWidth="5"/>
       <path d="M80 21 V129 M32 72 H128 M80 21 L51 129 M80 21 L109 129" fill="none" strokeWidth="3"/>
       <path d="M32 72 L80 90 L128 72" fill="none" strokeWidth="2"/></>}
     {symbol==='Chip'&&<><circle cx="80" cy="80" r="53" fill="#132d3a" strokeWidth="9"/>
       <circle cx="80" cy="80" r="39" strokeWidth="4" fill="#193e48"/>
       {Array.from({length:8},(_,i)=><path key={i} d="M80 18 V40" transform={'rotate('+(i*45)+' 80 80)'} strokeWidth="8"/>)}
       <text x="80" y="94" textAnchor="middle" fill={color} stroke="none" fontSize="43" fontFamily="Georgia" fontWeight="bold">$</text></>}
     {symbol==='Crown'&&<><path d="M27 59 L48 98 L80 42 L112 98 L133 59 L121 121 H39 Z" fill="#4a2635" strokeWidth="6"/>
       <circle cx="27" cy="59" r="7" fill="#ffe39b"/><circle cx="80" cy="42" r="8" fill="#ffe39b"/>
       <circle cx="133" cy="59" r="7" fill="#ffe39b"/><path d="M42 109 H118" fill="none" strokeWidth="4"/></>}
     {symbol==='Key'&&<><circle cx="59" cy="54" r="25" strokeWidth="9" fill="#3b1a27"/>
       <circle cx="59" cy="54" r="8" strokeWidth="3" fill="none"/>
       <path d="M78 74 L121 117 L110 129 L97 118 L89 127 L77 116 L86 106 L68 86" strokeWidth="13" fill="none"/></>}
     {symbol==='Wild'&&<><path d="M80 23 L92 54 L122 38 L117 69 L139 80 L117 92 L122 122 L93 109 L80 139 L67 110 L38 122 L44 92 L22 80 L45 66 L38 39 L68 54 Z" strokeWidth="4" fill="#4e1453"/>
       <text x="80" y="94" textAnchor="middle" fontFamily="Georgia" fontWeight="900" fontSize="34" strokeWidth="1" fill="#fff0fc">WILD</text></>}
     {symbol==='Scatter'&&<><circle cx="80" cy="80" r="52" fill="#473523" strokeWidth="7"/>
       <circle cx="80" cy="80" r="37" fill="#191a2d" strokeWidth="5"/>
       <circle cx="80" cy="80" r="13" fill="#73513a" strokeWidth="5"/>
       {Array.from({length:6},(_,i)=><path key={i} d="M80 43 V65" transform={'rotate('+(i*60)+' 80 80)'} strokeWidth="5"/>)}
       <rect x="113" y="73" width="18" height="15" rx="4" fill="#e4af62" strokeWidth="3"/></>}
     {symbol==='VaultKey'&&<><path d="M80 16 L133 54 V112 L80 143 L27 112 V54 Z" fill="#241a4d" strokeWidth="5"/>
       <circle cx="77" cy="64" r="18" fill="#4c2873" strokeWidth="6"/>
       <path d="M78 83 V120 M78 103 L95 103 M78 114 L94 114" strokeWidth="10" fill="none"/></>}
   </g>
   {!compact&&<g fill="#e8f3ff" opacity=".7"><circle cx="27" cy="27" r="2"/><circle cx="134" cy="134" r="2"/></g>}
 </svg>;
}
