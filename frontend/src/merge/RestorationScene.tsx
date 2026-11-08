import {useId} from 'react';
import type {ReactNode} from 'react';
import type {MergeRoom} from './bridge';

type Palette={wall:string;deep:string;metal:string;light:string;fabric:string};
const palettes:Record<string,Palette>={
 lobby:{wall:'#293a42',deep:'#101b27',metal:'#cbb176',light:'#f6daab',fabric:'#593c51'},
 bar:{wall:'#222f40',deep:'#111d2a',metal:'#dbb27a',light:'#f2d0a5',fabric:'#5b3549'},
 'gaming-floor':{wall:'#25383e',deep:'#101c27',metal:'#d4bc83',light:'#f8dcb3',fabric:'#326057'},
 conservatory:{wall:'#23504d',deep:'#122d35',metal:'#c2d6a7',light:'#e7f2cc',fabric:'#285b4b'},
 lounge:{wall:'#403544',deep:'#1e202d',metal:'#e2c18c',light:'#ffe1b1',fabric:'#67506f'},
 gallery:{wall:'#323943',deep:'#141b28',metal:'#d8bf91',light:'#f1e7cd',fabric:'#3e626c'},
 theater:{wall:'#342b39',deep:'#140f22',metal:'#e8c186',light:'#fff0be',fabric:'#843f5e'},
 terrace:{wall:'#224251',deep:'#102237',metal:'#dfc28a',light:'#ffe9bf',fabric:'#426d66'},
 'vip-suites':{wall:'#493e49',deep:'#171f2d',metal:'#f0cd97',light:'#ffedce',fabric:'#7f4967'},
 observatory:{wall:'#1c3149',deep:'#0d1529',metal:'#bed6ed',light:'#e2f1fc',fabric:'#3b567a'},
};
function Column({x,clean,metal}:{x:number;clean:boolean;metal:string}){
 return <g>
   <path d={'M'+(x-27)+' 20 H'+(x+27)+' V38 H'+(x-27)+'Z'} fill={clean?metal:'#596466'} opacity=".72"/>
   <rect x={x-16} y="39" width="32" height="167" fill={clean?'#32434a':'#273038'} stroke={metal} strokeOpacity={clean?.62:.22} strokeWidth="2"/>
   <path d={'M'+(x-10)+' 43 V200'} stroke="#fff7df" strokeOpacity={clean?.15:.03} strokeWidth="4"/>
   <path d={'M'+(x-30)+' 207 H'+(x+30)+' V215 H'+(x-30)+'Z'} fill={metal} opacity={clean?.8:.28}/>
 </g>;
}
function Lamp({x,y,lit,color}:{x:number;y:number;lit:boolean;color:string}){
 return <g>
   {lit&&<ellipse className="merge-scene-light" cx={x} cy={y+31} rx="85" ry="66" fill={color} opacity=".16"/>}
   <path d={'M'+x+' 0 V'+(y-10)} stroke="#a88b62" strokeWidth="3"/>
   <path d={'M'+(x-33)+' '+y+' Q'+x+' '+(y-34)+' '+(x+33)+' '+y+'L'+(x+21)+' '+(y+13)+'H'+(x-21)+'Z'}
     fill={lit?'#f4d39b':'#6e6864'} stroke="#e9c789" strokeWidth="2"/>
   <ellipse cx={x} cy={y+13} rx="21" ry="5" fill={lit?'#fff1c7':'#49494c'} opacity=".85"/>
 </g>;
}
function Plant({x,y=207,full}:{x:number;y?:number;full:boolean}){
 return <g>
  <path d={'M'+(x-16)+' '+(y-12)+'H'+(x+17)+'L'+(x+12)+' '+(y+18)+'H'+(x-11)+'Z'} fill="#8e674d" stroke="#c6ae79" strokeWidth="2"/>
  {full&&<g>
   <path d={'M'+x+' '+(y-10)+'V'+(y-94)} stroke="#668d66" strokeWidth="4"/>
   {[-1,1].map(side=><g key={side}>
    <ellipse cx={x+side*24} cy={y-67} rx="14" ry="33" transform={'rotate('+(side*40)+' '+(x+side*24)+' '+(y-67)+')'} fill="#376e57" stroke="#9fcb8e" strokeWidth="1.5"/>
    <ellipse cx={x+side*34} cy={y-42} rx="12" ry="30" transform={'rotate('+(side*63)+' '+(x+side*34)+' '+(y-42)+')'} fill="#4f8564"/>
   </g>)}
   <ellipse cx={x} cy={y-89} rx="14" ry="33" fill="#5b9971"/>
  </g>}
 </g>;
}
function Sofa({x,y=189,shade}:{x:number;y?:number;shade:string}){
 return <g>
   <ellipse cx={x+80} cy={y+35} rx="113" ry="12" fill="#030d17" opacity=".55"/>
   <rect x={x} y={y-28} width="160" height="48" rx="17" fill={shade} stroke="#d1b784" strokeWidth="3"/>
   <rect x={x+8} y={y-18} width="144" height="32" rx="9" fill="#977a78" opacity=".24"/>
   <rect x={x-13} y={y-9} width="29" height="35" rx="10" fill={shade} stroke="#d1b784" strokeWidth="2"/>
   <rect x={x+145} y={y-9} width="29" height="35" rx="10" fill={shade} stroke="#d1b784" strokeWidth="2"/>
   <path d={'M'+(x+23)+' '+(y+3)+'H'+(x+140)} stroke="#edd7b5" strokeOpacity=".5"/>
   {[x+18,x+137].map(p=><rect key={p} x={p} y={y+22} width="8" height="11" fill="#9b7957"/>)}
 </g>;
}
function ArtFrame({x,y=75,color,lit}:{x:number;y?:number;color:string;lit:boolean}){
 return <g>
  <rect x={x} y={y} width="145" height="108" rx="4" fill="#2a2d36" stroke="#a48e6d" strokeWidth="8"/>
  <rect x={x+10} y={y+10} width="125" height="88" fill={color}/>
  <circle cx={x+90} cy={y+39} r="25" fill="#e7c08b" opacity=".7"/>
  <path d={'M'+(x+10)+' '+(y+94)+'L'+(x+61)+' '+(y+28)+'L'+(x+97)+' '+(y+63)+'L'+(x+131)+' '+(y+20)+'V'+(y+98)+'H'+(x+10)+'Z'} fill="#263d55" opacity=".8"/>
  {lit&&<path d={'M'+(x+11)+' '+(y+11)+'L'+(x+123)+' '+(y+11)+'L'+(x+43)+' '+(y+95)+'Z'} fill="#fff7cb" opacity=".12"/>}
 </g>;
}
function RoomDetails({kind,phase,p}:{kind:string;phase:number;p:Palette}):ReactNode{
 const built=phase>=1,furnished=phase>=2,lit=phase>=3;
 switch(kind){
  case 'lobby':return <>
   <path d="M397 214 V75 Q600 -50 803 75 V214" fill="#111d29" stroke={p.metal} strokeWidth={built?12:5}/>
   <path d="M425 208 V82 Q600 -18 775 82 V208" fill="#1b3339" stroke="#668184" strokeWidth="3"/>
   <path d="M451 120 Q600 18 749 120" fill="none" stroke="#8b987f" opacity=".36" strokeWidth="4"/>
   <rect x="465" y="111" width="270" height="66" rx="12" fill="#0f252b" stroke={p.metal} strokeWidth="3"/>
   <text x="600" y="150" textAnchor="middle" fontFamily="Georgia,serif" fontSize="25"
     fill={lit?p.light:'#9c9c94'} letterSpacing="5">MIDNIGHT CLUB</text>
   {furnished&&<><Sofa x={122} shade={p.fabric}/><Sofa x={914} shade={p.fabric}/>
    <Plant x={380} full/><Plant x={820} full/>
    <path d="M448 231 L548 197 M752 231 L652 197" stroke="#d5b97f" strokeWidth="4"/>
    <circle cx="448" cy="230" r="7" fill="#f6dbaa"/><circle cx="752" cy="230" r="7" fill="#f6dbaa"/></>}
   {lit&&<><Lamp x={250} y={85} lit color={p.light}/><Lamp x={950} y={85} lit color={p.light}/></>}
  </>;
  case 'bar':return <>
   <rect x="285" y="67" width="630" height="143" rx="15" fill="#0e1b25" stroke={p.metal} strokeWidth="9"/>
   {[93,135,177].map(y=><path key={y} d={'M307 '+y+'H892'} stroke={p.metal} strokeWidth="3" opacity={built?.7:.22}/>)}
   {furnished&&Array.from({length:27},(_,i)=>{
     const x=326+(i%9)*63,y=72+Math.floor(i/9)*43;
     return <g key={i}>
      <rect x={x+4} y={y} width="19" height="25" rx="4" fill={['#6695a4','#a87ca2','#d0ae67','#77aa89'][i%4]} stroke="#ddd1a5" strokeWidth="1"/>
      <rect x={x+9} y={y-6} width="9" height="8" fill="#dbc999"/>
      {lit&&<rect x={x+8} y={y+5} width="3" height="14" fill="#fff2c5" opacity=".65"/>}
     </g>})}
   <path d="M220 198 L978 198 L1024 230 H174Z" fill={built?'#725039':'#37373b'} stroke={p.metal} strokeWidth="5"/>
   <path d="M174 230 H1024 L990 280 H206Z" fill={built?'#362b30':'#24262a'} stroke={p.metal} strokeWidth="3"/>
   {furnished&&[312,485,700,885].map(x=><g key={x}>
    <ellipse cx={x} cy="250" rx="28" ry="9" fill="#9e7057" stroke="#dfbd85" strokeWidth="3"/>
    <path d={'M'+x+' 250 V289'} stroke="#aa8e64" strokeWidth="8"/>
   </g>)}
   {lit&&<Lamp x={600} y={65} lit color={p.light}/>}
  </>;
  case 'gaming-floor':return <>
   <path d="M310 65 H890 V211 H310Z" fill="#182d32" stroke={p.metal} strokeWidth="7"/>
   <path d="M349 73 H851 M349 205 H851" stroke="#d2b784" strokeWidth="3"/>
   {furnished&&[260,590,920].map((x,i)=><g key={x} transform={'translate('+x+' '+(i===1?155:179)+')'}>
    <ellipse cy="38" rx="147" ry="45" fill="#081922" opacity=".68"/>
    <ellipse cy="4" rx="133" ry="46" fill="#5e3c28" stroke={p.metal} strokeWidth="8"/>
    <ellipse cy="2" rx="111" ry="33" fill="#1f6159" stroke="#90ae88" strokeWidth="2"/>
    <path d="M-82 -4 H82" stroke="#e4d49f" strokeWidth="2" strokeDasharray="8 7"/>
    {i===1&&<circle cy="3" r="24" fill="#6b4433" stroke="#cbb584" strokeWidth="3"/>}
    {[-100,0,100].map(cx=><g key={cx}><rect x={cx-13} y="-58" width="26" height="16" rx="5" fill="#693e48" stroke="#ac957b"/></g>)}
   </g>)}
   {lit&&<><Lamp x={355} y={64} lit color={p.light}/><Lamp x={850} y={64} lit color={p.light}/></>}
  </>;
  case 'conservatory':return <>
   <path d="M125 212 V60 L340 8 H858 L1075 60 V212" fill="#21444c" fillOpacity=".48" stroke={p.metal} strokeWidth="8"/>
   {[205,365,525,675,835,995].map(x=><path key={x} d={'M'+x+' 30 V215'} stroke="#93b9ad" strokeWidth="5" opacity=".65"/>)}
   <path d="M340 8 L525 74 M858 8 L675 74" stroke="#d0d9b3" strokeWidth="4"/>
   {built&&<path d="M145 78 H1055" stroke="#bcdeca" strokeWidth="3"/>}
   {furnished&&<><Plant x={195} full/><Plant x={300} full/><Plant x={895} full/><Plant x={1005} full/>
    <ellipse cx="602" cy="228" rx="115" ry="34" fill="#163f43" stroke={p.metal} strokeWidth="7"/>
    <ellipse cx="602" cy="223" rx="83" ry="22" fill="#74a7a7" opacity=".7"/>
    <path d="M602 225 Q574 147 602 115 Q630 147 602 225" fill="#80c4cd" opacity=".65"/>
    <ellipse cx="602" cy="185" rx="42" ry="5" fill="#b8e1de" opacity=".6"/></>}
   {lit&&[430,775].map(x=><circle className="merge-scene-spark" key={x} cx={x} cy="108" r="7" fill={p.light}/>)}
  </>;
  case 'lounge':return <>
   <path d="M451 78 H751 V216 H451Z" fill="#241e2d" stroke={p.metal} strokeWidth="6"/>
   <path d="M471 100 H731 V204 H471Z" fill="#3e2d36" stroke="#735f58" strokeWidth="3"/>
   {lit&&<path d="M558 195 Q536 152 570 131 Q580 158 599 118 Q654 166 635 192 Z" fill="#f3a35d" opacity=".88"/>}
   <path d="M439 212 H764 V230 H439Z" fill="#73544c" stroke={p.metal} strokeWidth="4"/>
   {furnished&&<><Sofa x={206} y={186} shade={p.fabric}/><Sofa x={838} y={186} shade={p.fabric}/>
    {[391,801].map(x=><g key={x}><rect x={x-26} y="189" width="54" height="12" fill="#8d674c" stroke={p.metal}/><path d={'M'+(x-18)+' 202 V231 M'+(x+18)+' 202 V231'} stroke="#bfa16b" strokeWidth="4"/></g>)}
    <ellipse cx="602" cy="252" rx="190" ry="28" fill="#4d3c4d" stroke={p.metal} strokeOpacity=".45" strokeWidth="3"/></>}
   {lit&&<Lamp x={602} y={66} lit color={p.light}/>}
  </>;
  case 'gallery':return <>
   {[128,352,704,928].map((x,i)=><ArtFrame key={x} x={x} y={i%2?71:91}
    color={['#436478','#6f5167','#4e786b','#837259'][i]} lit={lit}/>)}
   {furnished&&[325,872].map(x=><g key={x}>
    <rect x={x-48} y="197" width="96" height="13" fill="#d3c3a1"/>
    <path d={'M'+(x-37)+' 209 H'+(x+37)+'L'+(x+31)+' 259 H'+(x-31)+'Z'} fill="#78818a" stroke={p.metal} strokeWidth="3"/>
    <path d={'M'+x+' 192 Q'+(x-25)+' 162 '+x+' 147 Q'+(x+27)+' 167 '+x+' 192'} fill="#cfc4ad" stroke="#a9b3ad" strokeWidth="2"/>
   </g>)}
   {lit&&[200,600,1000].map(x=><Lamp key={x} x={x} y={62} lit color={p.light}/>)}
  </>;
  case 'theater':return <>
   <path d="M230 42 H970 V232 H230Z" fill="#0b1122" stroke={p.metal} strokeWidth="13"/>
   <path d="M260 76 H940 V222 H260Z" fill="#1e2840" stroke="#96816b" strokeWidth="3"/>
   <path d="M255 212 H945 L1000 246 H200Z" fill="#4a3139" stroke={p.metal} strokeWidth="6"/>
   <path d={'M255 48 Q340 146 405 208 L380 226 H255Z M945 48 Q860 146 795 208 L820 226 H945Z'}
     fill={built?'#803e58':'#523e54'} stroke="#d0a379" strokeWidth="6"/>
   {furnished&&<><path d="M415 158 L550 80 L675 147 L790 72 V211 H415Z" fill="#344b59" stroke="#c7b17e" strokeWidth="3"/>
    <path d="M455 197 H749" stroke="#e3bc83" strokeWidth="4"/>
    <text x="601" y="110" textAnchor="middle" fill="#e6cfa2" fontSize="23" letterSpacing="6" fontFamily="Georgia,serif">MIDNIGHT</text></>}
   {lit&&<><path d="M310 25 L440 219 L500 219Z M885 25 L705 219 L770 219Z" fill="#fff0bc" opacity=".17"/>
    <Lamp x={603} y={65} lit color={p.light}/></>}
  </>;
  case 'terrace':return <>
   <rect x="0" y="38" width="1200" height="153" fill="#152b46" opacity=".8"/>
   {[58,122,192,275,340,422,775,835,902,991,1077,1141].map((x,i)=><rect key={x} x={x} y={130-i%4*18}
    width={35+i%3*19} height="75" fill="#202e42" stroke="#557084" strokeWidth="1"/>)}
   <circle cx="854" cy="73" r="23" fill="#fff2c5" opacity=".75"/>
   {built&&<path d="M92 219 V100 H1108 V219 M92 101 H1108 M274 101 V219 M924 101 V219" stroke={p.metal} strokeWidth="9" fill="none"/>}
   <path d="M90 209 H1110 M90 230 H1110" stroke="#c8b589" strokeWidth="8"/>
   {[120,250,380,520,660,810,955,1090].map(x=><path key={x} d={'M'+x+' 211 V231'} stroke={p.metal} strokeWidth="5"/>)}
   {furnished&&<><Plant x={230} full/><Plant x={961} full/>
    <path d="M474 222 L514 196 H679 L721 222Z" fill="#716252" stroke={p.metal} strokeWidth="5"/>
    <path d="M520 219 V255 M672 219 V255" stroke="#b6a279" strokeWidth="7"/>
    <Sofa x={160} y={221} shade={p.fabric}/><Sofa x={875} y={221} shade={p.fabric}/></>}
   {lit&&[150,325,875,1050].map(x=><circle key={x} className="merge-scene-light" cx={x} cy="101" r="7" fill={p.light}/>)}
  </>;
  case 'vip-suites':return <>
   <path d="M448 58 H753 V197 H448Z" fill="#25374c" stroke={p.metal} strokeWidth="8"/>
   <path d="M600 60 V193 M452 112 H750" stroke="#a6b5ad" strokeWidth="5"/>
   <circle cx="667" cy="88" r="24" fill="#f0e8c4" opacity=".62"/>
   {furnished&&<><path d="M365 181 V127 Q601 68 838 127 V181Z" fill={p.fabric} stroke={p.metal} strokeWidth="8"/>
    <path d="M335 192 Q604 153 865 192 L905 257 H297Z" fill="#cdb99c" stroke={p.metal} strokeWidth="5"/>
    <path d="M366 209 H831 L858 245 H340Z" fill="#927889" opacity=".85"/>
    {[363,840].map(x=><g key={x}><rect x={x-60} y="203" width="65" height="35" fill="#755e56" stroke={p.metal} strokeWidth="3"/>
     <Lamp x={x-30} y={125} lit={lit} color={p.light}/></g>)}
    <Plant x={160} full/><Plant x={1060} full/></>}
   {lit&&<circle className="merge-scene-light" cx="601" cy="110" r="70" fill="#f5d9ad" opacity=".09"/>}
  </>;
  case 'observatory':return <>
   <path d="M240 211 V110 Q601 -107 960 110 V211" fill="#172c47" stroke={p.metal} strokeWidth="11"/>
   <path d="M275 201 V115 Q601 -64 925 115 V201" fill="#101c35" stroke="#5e7b99" strokeWidth="4"/>
   {[0,1,2,3,4,5,6,7,8].map(i=><circle key={i} cx={317+(i*71)%595} cy={70+(i*41)%94}
     r={i%3+1.4} fill="#e4f0ff" opacity=".65" className={lit?'merge-scene-spark':''}/>)}
   {built&&<path d="M600 8 V215 M300 156 H900 M395 60 Q600 160 804 60" fill="none" stroke="#6c95aa" strokeWidth="2" opacity=".46"/>}
   {furnished&&<g transform="translate(605 181) rotate(-24)">
      <path d="M-156 -28 H78 L120 1 L78 29 H-156 Z" fill="#49637a" stroke={p.metal} strokeWidth="8"/>
      <rect x="-180" y="-39" width="38" height="79" rx="8" fill="#283848" stroke="#bcd3e5" strokeWidth="5"/>
      <ellipse cx="120" cy="0" rx="18" ry="32" fill="#a9c0d5" stroke={p.metal} strokeWidth="6"/>
      <path d="M-40 29 V101 M15 29 V101" stroke="#a8bdc8" strokeWidth="9"/>
    </g>}
   {lit&&<><path d="M660 124 L1020 14 L1052 70Z" fill="#d7e9ff" opacity=".12"/>
      <circle cx="1078" cy="66" r="8" fill="#c3e4ff" className="merge-scene-spark"/></>}
  </>;
  default:return null;
 }
}

export function RestorationScene({stage,animated=true}:{stage:MergeRoom;animated?:boolean}){
 const unique=useId().replaceAll(':','');
 const id=(key:string)=>'merge-'+key+'-'+unique;
 const url=(key:string)=>'url(#'+id(key)+')';
 const p=palettes[stage.areaId]??palettes.lobby;
 const phase=Math.max(0,stage.milestones.findIndex(ms=>ms.id===stage.current.id));
 const cleaned=phase>=1,furnished=phase>=2,illuminated=phase>=3;
 const outside=stage.areaId==='terrace'||stage.areaId==='observatory';
 const glass=stage.areaId==='conservatory';
 return <svg viewBox="0 0 1200 280" className={'merge-room-art'+(animated?'':' static')}
   data-scene={stage.areaId} data-phase={phase} preserveAspectRatio="xMidYMid slice"
   role="img" aria-label={stage.areaName+' – '+stage.current.name}>
  <defs>
   <linearGradient id={id('wall')} x1="0" y1="0" x2="0" y2="1">
    <stop stopColor={cleaned?p.wall:'#222b35'}/>
    <stop offset="1" stopColor={cleaned?p.deep:'#131b25'}/>
   </linearGradient>
   <linearGradient id={id('floor')} x1="0" y1="0" x2="0" y2="1">
    <stop stopColor={cleaned?'#4d4d48':'#30383d'}/><stop offset=".36" stopColor="#283239"/>
    <stop offset="1" stopColor="#0c1921"/>
   </linearGradient>
   <radialGradient id={id('glow')}>
    <stop stopColor={p.light} stopOpacity={illuminated?.36:.08}/><stop offset="1" stopColor={p.light} stopOpacity="0"/>
   </radialGradient>
   <linearGradient id={id('vignette')} x1="0" y1="0" x2="1" y2=".3">
    <stop stopColor="#02080c" stopOpacity=".42"/><stop offset=".18" stopColor="#07111c" stopOpacity="0"/>
    <stop offset=".82" stopColor="#07111c" stopOpacity="0"/><stop offset="1" stopColor="#02080c" stopOpacity=".4"/>
   </linearGradient>
   <pattern id={id('grain')} width="32" height="32" patternUnits="userSpaceOnUse">
    <path d="M0 32 L32 0" stroke="#faf1cd" strokeWidth="1" opacity={cleaned?.025:.08}/>
   </pattern>
  </defs>
  <rect width="1200" height="280" fill={url('wall')}/>
  {outside&&<><path d="M0 0 H1200 V196 H0Z" fill="#10203a"/>
    <circle cx="995" cy="51" r="31" fill="#e7e6d2" opacity=".85"/>
    {Array.from({length:32},(_,i)=><circle key={i} cx={i*113%1200} cy={i*47%162}
      r={1+i%3*.7} fill="#e2eef9" opacity=".65"/>)}</>}
  {glass&&<path d="M0 0 H1200 V185 H0Z" fill="#235c5b" opacity=".3"/>}
  {!outside&&!glass&&[150,360,850,1050].map(x=><g key={x}>
    <path d={'M'+(x-62)+' 40 V202 H'+(x+62)+' V40 Q'+x+' -16 '+(x-62)+' 40Z'}
      fill="#172833" stroke={p.metal} strokeWidth="4" opacity=".4"/>
    <path d={'M'+x+' 25 V200'} stroke="#c5bb9e" strokeOpacity=".2" strokeWidth="3"/>
  </g>)}
  <path d="M0 205 H1200 V280 H0Z" fill={url('floor')}/>
  <path d="M0 205 H1200" stroke={p.metal} strokeWidth={cleaned?6:3} strokeOpacity={cleaned?.85:.3}/>
  {[0,240,480,720,960,1200].map(x=><path key={x} d={'M600 204 L'+x+' 280'}
    stroke={p.metal} strokeWidth="1.5" opacity={cleaned?.19:.07}/>)}
  {[234,254,277].map(y=><path key={y} d={'M0 '+y+'H1200'} stroke="#aeb3a3" opacity=".1"/>)}
  {!outside&&!glass&&<g>
   <Column x={55} clean={cleaned} metal={p.metal}/><Column x={1145} clean={cleaned} metal={p.metal}/>
   {cleaned&&<><path d="M80 22 H1120" stroke={p.metal} strokeWidth="6" opacity=".56"/>
    <path d="M85 35 H1115" stroke="#e4d5b5" strokeWidth="2" opacity=".3"/></>}
  </g>}
  <RoomDetails kind={stage.areaId} phase={phase} p={p}/>
  {!cleaned&&<g opacity=".56">
   <path d="M55 213 L160 213 L178 231 H44Z M899 216 L1036 216 L1075 232 H873Z" fill="#858987" opacity=".45"/>
   {[154,232,412,625,788,990].map((x,i)=><ellipse key={x} cx={x} cy={242+i%3*7} rx="38" ry="4"
     fill="#b1aaa0" opacity=".55"/>)}
   <path d="M77 202 L95 147 L112 202 M96 145 V102" stroke="#afa79a" strokeWidth="5" fill="none"/>
   <path d="M1003 204 L1042 136 L1068 204" stroke="#b5a99a" strokeWidth="6" fill="none"/>
  </g>}
  {cleaned&&<path d="M115 207 H1085" stroke={p.metal} strokeWidth="2" opacity=".4"/>}
  {furnished&&<ellipse cx="600" cy="241" rx="310" ry="30" fill={p.fabric} opacity=".16"/>}
  {illuminated&&<g>
    <ellipse cx="600" cy="93" rx="410" ry="170" fill={url('glow')} className="merge-scene-light"/>
    {[132,444,612,782,1065].map((x,i)=><circle key={x} cx={x} cy={59+(i%2)*18}
      r={2+(i%3)} fill={p.light} opacity=".82" className="merge-scene-spark"/>)}
  </g>}
  <rect width="1200" height="280" fill={url('grain')}/>
  <rect width="1200" height="280" fill={url('vignette')}/>
  <path d="M0 2 H1200 M0 278 H1200" stroke={p.metal} strokeWidth="5" opacity={illuminated?.84:.4}/>
 </svg>;
}
