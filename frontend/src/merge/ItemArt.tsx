import {useId} from 'react';
import type {MergeDefinition} from './bridge';

export function MergeItemArt({definition,small=false}:{definition:MergeDefinition;small?:boolean}){
 const uid=useId().replaceAll(':',''),color=definition.color;
 const fill='fill'+uid,shine='shine'+uid,glow='glow'+uid;
 const drink=definition.tags.includes('drink');
 return <svg className={'merge-item-art'+(drink?' drink':' chip')} viewBox="0 0 112 112"
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
