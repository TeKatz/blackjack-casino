import type {ReactNode} from 'react';
import type {ExpansionTwoId} from './arcadeExpansionTwoEngine';

/** Five independent miniature gameplay scenes: never animate the entire scene. */
export function ArcadeExpansionTwoCardArt({id}:{id:ExpansionTwoId}){
 const gold='#e8cd93',mint='#98eadc';
 const scene=(elements:ReactNode)=><svg className="arcade-scene" data-arcade-scene={id}
  viewBox="0 0 320 128" preserveAspectRatio="xMidYMid slice" aria-hidden="true" focusable="false">
  <defs><linearGradient id={'two-'+id} x1="0" x2="1" y1="0" y2="1">
   <stop stopColor="#112d44"/><stop offset=".6" stopColor="#132638"/>
   <stop offset="1" stopColor="#2a2140"/>
  </linearGradient></defs>
  <rect width="320" height="128" fill={'url(#two-'+id+')'}/>
  <path d="M0 31H320M0 64H320M0 97H320M39 0V128M93 0V128M147 0V128M201 0V128M255 0V128"
   fill="none" stroke={mint} strokeWidth="1" opacity=".08"/>
  {elements}
 </svg>;
 switch(id){
 case 'prism-lock':return scene(<>
  <rect x="17" y="18" width="285" height="97" rx="12" fill="#102d3a" stroke="#648b91"/>
  {[[80,62],[80,29],[159,29],[159,91],[245,91]].map(([x,y],i)=>
   <rect key={i} x={x-13} y={y-13} width="26" height="26" rx="5"
    fill="#285766" stroke="#c8b589" strokeWidth="1.5"/>)}
  <g className="exp-two-prism-beam" stroke={mint} strokeWidth="3" fill="none" strokeLinecap="round">
   <path d="M28 62H80V29H159V91H245"/>
  </g>
  <g className="exp-two-prism-mirror" fill="none" stroke="#f7e4b6" strokeWidth="3" strokeLinecap="round">
   <path d="M70 52 90 72M70 39 90 19M149 19 169 39M149 101 169 81"/>
  </g>
  <circle cx="28" cy="62" r="9" fill={gold} stroke="#f8ead4"/>
  <rect x="244" y="80" width="31" height="23" rx="5" fill="#377778" stroke={gold} strokeWidth="2"/>
  <circle className="exp-two-prism-target" cx="260" cy="91" r="6" fill={mint}/>
 </>);
 case 'meteor-miner':return scene(<>
  <circle cx="57" cy="86" r="28" fill="#225662" stroke={gold} strokeWidth="2"/>
  <circle cx="57" cy="86" r="17" fill="#123742" stroke={mint} strokeWidth="2"/>
  <path d="M92 5 170 14 223 2M87 123 222 112" fill="none" stroke="#9bdaea" opacity=".2" strokeWidth="2"/>
  {[[232,41,25],[275,89,18],[136,29,16],[155,105,18]].map(([x,y,r],i)=>
   <g key={i}><path d={'M'+(x-r)+' '+y+' '+(x-r*.4)+' '+(y-r)+' '+(x+r*.55)+' '+(y-r*.8)+
    ' '+(x+r)+' '+y+' '+(x+r*.4)+' '+(y+r)+' '+(x-r*.7)+' '+(y+r*.7)+'Z'}
    fill={i===0?'#665783':'#315969'} stroke={i===0?'#dab4ef':'#86dcca'} strokeWidth="2"/>
    <path d={'M'+(x-6)+' '+(y+5)+'l8 -12 10 11'} fill="none" stroke={mint} strokeWidth="2"/></g>)}
  <g className="exp-two-miner-beam" stroke={mint} strokeWidth="2.5" fill="none">
   <path d="M165 68 225 43"/>
  </g>
  <g className="exp-two-miner-ship">
   <path d="M164 51 182 76 164 69 146 76Z" fill="#82e5d8" stroke={gold} strokeWidth="2"/>
   <circle cx="164" cy="62" r="5" fill="#174157"/>
  </g>
  <g className="exp-two-miner-spark" fill="#fff0c1">
   <circle cx="232" cy="41" r="4"/><path d="M232 30V24M241 41h9M232 52v7" stroke="#ffe6a2" strokeWidth="2"/>
  </g>
 </>);
 case 'circuit-rush':return scene(<>
  <rect x="34" y="17" width="252" height="96" rx="12" fill="#0e293c" stroke="#718b8b" strokeWidth="1.7"/>
  {[0,1,2,3,4].map(c=><g key={c}>
   <rect x={48+c*46} y="40" width="37" height="44" rx="6" fill={c<3?'#1f575e':'#253b4f'}
    stroke={c<3?mint:'#5d8290'} strokeWidth="1.6"/>
   <path d={'M'+(66+c*46)+' 62h18'} stroke={c<3?gold:'#689fa2'} strokeWidth="5" strokeLinecap="round"/>
   <circle cx={66+c*46} cy="62" r="5" fill={c<3?mint:'#4b747f'}/>
  </g>)}
  <g className="exp-two-circuit-current" fill="none" stroke="#ffe1aa" strokeWidth="4" strokeLinecap="round">
   <path d="M16 62H67 M84 62H112 M130 62H158"/>
  </g>
  <g className="exp-two-circuit-turn" stroke={gold} strokeWidth="5" strokeLinecap="round" fill="none">
   <path d="M205 62v-14"/>
  </g>
  <circle cx="297" cy="62" r="9" fill="#355665" stroke={gold} strokeWidth="2"/>
 </>);
 case 'shadow-heist':return scene(<>
  <rect x="15" y="10" width="290" height="110" rx="9" fill="#102b35" stroke="#a9ae92" strokeWidth="1.5"/>
  <path d="M23 39H295 M23 76H295M74 17V114 M147 17V114 M221 17V114"
   fill="none" stroke="#7fa799" opacity=".17"/>
  <rect x="130" y="14" width="49" height="18" rx="3" fill="#3d555f" stroke="#9caa93"/>
  <rect x="183" y="83" width="54" height="20" rx="3" fill="#3d555f" stroke="#9caa93"/>
  <g className="exp-two-heist-cone">
   <path d="M155 63 251 19 251 107Z" fill="#edcd85" opacity=".28"/>
   <circle cx="155" cy="63" r="9" fill="#f0cb90" stroke="#6b4b44" strokeWidth="2"/>
   <path d="M155 57 165 63 155 68Z" fill="#64404b"/>
  </g>
  {[[91,32],[211,52],[269,94]].map(([x,y],i)=><g key={i}>
   <rect x={x-7} y={y-7} width="14" height="14" rx="2" fill={mint}
    stroke={gold} strokeWidth="1.6" transform={'rotate(45 '+x+' '+y+')'}/>
  </g>)}
  <g className="exp-two-heist-player">
   <circle cx="75" cy="95" r="9" fill="#7ce3d8" stroke={gold} strokeWidth="1.7"/>
   <circle cx="78" cy="93" r="2" fill="#15394b"/>
  </g>
  <rect x="283" y="43" width="15" height="34" rx="3" fill="#477a77" stroke={gold} strokeWidth="2"/>
 </>);
 case 'pulse-perfect':return scene(<>
  <circle cx="160" cy="64" r="57" fill="#1d2d4a" stroke="#624f88" strokeWidth="1.8"/>
  <circle cx="160" cy="64" r="43" fill="none" stroke="#eacba0" strokeWidth="9"/>
  <circle cx="160" cy="64" r="43" fill="none" stroke="#fff0c9" strokeWidth="1.4"/>
  <g className="exp-two-pulse-ring">
   <circle cx="160" cy="64" r="58" fill="none" stroke={mint} strokeWidth="5"/>
   <circle cx="160" cy="64" r="56" fill="none" stroke="#c9fff3" strokeWidth="1"/>
   {[0,90,180,270].map(d=><circle key={d}
    cx={160+58*Math.cos(d*Math.PI/180)} cy={64+58*Math.sin(d*Math.PI/180)}
    r="3" fill="#fff7d8"/>)}
  </g>
  <circle cx="160" cy="64" r="20" fill="#264f5a" stroke={gold} strokeWidth="2"/>
  <circle cx="160" cy="64" r="11" fill={mint} stroke="#fff0bc" strokeWidth="2"/>
  <g className="exp-two-pulse-glint" fill="none" stroke="#ffefc1" strokeWidth="2">
   <path d="M160 2V12 M160 116V126 M98 64H109M211 64H222"/>
  </g>
 </>);
 }
}
