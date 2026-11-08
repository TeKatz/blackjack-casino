import {useId} from 'react';
import type {MergeDefinition} from './bridge';

export function MergeItemArt({definition,small=false}:{definition:MergeDefinition;small?:boolean}){
 const uid=useId().replaceAll(':',''),color=definition.color;
 const fill='fill'+uid,shine='shine'+uid,glow='glow'+uid;
 const drink=definition.tags.includes('drink');
 const decor=definition.tags.includes('decor');
 const garden=definition.tags.includes('garden');
 const stage=definition.tags.includes('stage');
 const sky=definition.tags.includes('sky');
 const supply=decor||garden||stage||sky;
 return <svg className={'merge-item-art'+(drink?' drink':supply?' supplies':' chip')} viewBox="0 0 112 112"
   role="img" aria-label={definition.name}>
  <defs>
   <linearGradient id={fill} x1="0" y1="0" x2="1" y2="1">
    <stop offset="0" stopColor="#fffbeb"/><stop offset=".31" stopColor={color}/><stop offset="1" stopColor="#584266"/>
   </linearGradient>
   <linearGradient id={shine} x1="0" y1="0" x2=".9" y2="1">
    <stop stopColor="#fff" stopOpacity=".65"/><stop offset=".4" stopColor="#fff" stopOpacity="0"/>
    <stop offset="1" stopColor="#b2e5cc" stopOpacity=".25"/>
   </linearGradient>
   <radialGradient id={glow}><stop stopColor={color} stopOpacity=".32"/><stop offset="1" stopColor={color} stopOpacity="0"/></radialGradient>
  </defs>
  <circle cx="56" cy="59" r="49" fill={'url(#'+glow+')'}/>
  {drink?<g>
    <ellipse cx="56" cy="95" rx="25" ry="5" fill="#020b14" opacity=".5"/>
    {definition.tier<=2?<g>
      <path d="M36 23 L76 23 L71 81 Q56 91 41 81 Z" fill="#122f34" stroke={color} strokeWidth="4"/>
      <path d="M39 30 L73 30 L69 77 Q55 85 43 77 Z" fill={color} opacity={definition.tier===1?.14:.5}/>
      <ellipse cx="56" cy="27" rx="19" ry="4" fill="#eaf8e8" opacity=".55"/>
      {definition.tier===2&&<><circle cx="49" cy="55" r="8" fill="#b1e8ee" opacity=".8"/>
       <circle cx="60" cy="66" r="9" fill="#dae2ff" opacity=".5"/><path d="M62 15 L80 62" stroke="#e3cdaa" strokeWidth="4"/></>}
    </g>:<g>
      <path d="M29 34 L83 34 L67 71 L60 76 L60 88 H77 V94 H35 V88 H52 V76 L45 71 Z"
       fill="#123046" stroke={'url(#'+fill+')'} strokeWidth="3.5" strokeLinejoin="round"/>
      <path d="M35 38 H77 L64 68 H48 Z" fill={color} opacity=".75"/>
      <ellipse cx="56" cy="36" rx="25" ry="5" fill="#ffeec7" opacity=".54"/>
      <path d="M71 30 L84 13" stroke="#d7c5a0" strokeWidth="3"/>
      {definition.tier>=5&&<><circle cx="79" cy="26" r="9" fill="#d5ad59"/>
       <path d="M77 19 L84 11" stroke="#c3ed93" strokeWidth="4"/></>}
      {definition.tier>=7&&<path d="M30 35 L47 11 L66 35" fill="none" stroke="#f3daa1" strokeWidth="3" opacity=".85"/>}
    </g>}
   </g>:supply?<g>
    <ellipse cx="56" cy="93" rx="39" ry="8" fill="#030b14" opacity=".55"/>
    <path d="M20 83 L26 38 L56 18 L86 38 L92 83 Q56 101 20 83Z" fill="#172936" stroke={'url(#'+fill+')'} strokeWidth="3.5"/>
    <path d="M25 78 L30 40 L56 24 L82 40 L87 78 Q56 91 25 78Z" fill={color} opacity=".31"/>
    {decor&&<g>
      {definition.tier>=4?<><path d="M32 75 H80 L74 52 H38Z" fill="#d4b377" stroke="#faf0c4" strokeWidth="2"/>
        <path d="M56 48 V28" stroke="#e5c989" strokeWidth="5"/><circle cx="56" cy="26" r="7" fill="#ffeed1"/>
        {definition.tier>=6&&[35,77].map(x=><path key={x} d={'M56 31 L'+x+' 50'} stroke="#f0dca5" strokeWidth="3"/> )}</>:
       <><rect x="35" y="41" width="42" height="29" rx="5" fill="#9a7589" stroke="#f6e0b1" strokeWidth="3"/>
        <path d="M35 56 H77" stroke="#eed5a7" strokeWidth="2"/></>}
     </g>}
    {garden&&<g>
     <path d="M37 67 H75 L68 85 H45Z" fill="#9e7055" stroke="#dfc290" strokeWidth="2"/>
     <path d="M56 67 V37" stroke="#a9db92" strokeWidth="4"/>
     {[40,72].map((x,i)=><ellipse key={x} cx={x} cy={46} rx={definition.tier>=4?16:12} ry="10"
       transform={'rotate('+(i?37:-37)+' '+x+' 46)'} fill={i?'#70b47b':'#3c8e65'} stroke="#d0ecab" strokeWidth="1"/>)}
     {definition.tier>=5&&<circle cx="56" cy="32" r="11" fill="#dceca2"/>}
    </g>}
    {stage&&<g>
     <path d="M38 31 H74 V77 H38Z" fill="#352239" stroke="#e0af8f" strokeWidth="3"/>
     <path d="M40 35 Q55 68 40 73 M72 35 Q57 68 72 73" fill="none" stroke="#d780aa" strokeWidth="8"/>
     <circle cx="56" cy="39" r="7" fill="#fff0b4"/>
     {definition.tier>=4&&<path d="M56 38 L36 82 H76Z" fill="#fff2b9" opacity=".33"/>}
    </g>}
    {sky&&<g>
     <path d="M31 72 L67 35 L81 44 L46 79Z" fill="#587d99" stroke="#d7e8f4" strokeWidth="4"/>
     <ellipse cx="70" cy="39" rx="13" ry="9" transform="rotate(42 70 39)" fill="#9cc4dd" stroke="#f8e6bc" strokeWidth="3"/>
     <path d="M55 59 L72 83 M56 60 L42 83" stroke="#e0cd9f" strokeWidth="5"/>
     {definition.tier>=5&&<circle cx="85" cy="26" r="4" fill="#f4e6be"/>}
    </g>}
    <path d="M28 81 Q56 95 85 81" fill="none" stroke="#fff4cc" strokeWidth="2" opacity=".45"/>
   </g>:<g>
    {[0,1].slice(0,definition.stackCount).map(layer=>
      <ellipse key={layer} cx="56" cy={72+layer*6} rx="38" ry="18"
        fill="#182832" stroke={color} strokeWidth="3"/>)}
    <ellipse cx="56" cy="54" rx="39" ry="35" fill="#101a29" stroke={'url(#'+fill+')'} strokeWidth="7"/>
    <ellipse cx="56" cy="54" rx="30" ry="26" fill={color} opacity=".3" stroke="#fff7d8" strokeWidth="1.8"/>
    <ellipse cx="56" cy="54" rx="24" ry="21" fill={'url(#'+shine+')'} opacity=".64"/>
    {Array.from({length:10},(_,i)=><path key={i} d="M56 19 V25"
      stroke="#efdeb5" strokeWidth="3" transform={'rotate('+(i*36)+' 56 54)'}/>)}
    <text x="56" y="64" fontSize={definition.mark.length>2?25:30} textAnchor="middle"
      fill="#fff3d7" fontFamily="Georgia" fontWeight="bold">{definition.mark}</text>
   </g>}
   {!small&&<path d="M20 30 Q27 13 42 11" fill="none" stroke="#fff" strokeOpacity=".35" strokeWidth="3"/>}
 </svg>;
}
