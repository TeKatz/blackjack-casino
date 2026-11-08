import type {MergeRestoration} from './bridge';
export function RestorationScene({stage}:{stage:MergeRestoration}){
 const bar=stage.areaId==='bar';
 const points=stage.current.points;
 const cleaned=bar?points>=500:points>=20;
 const lit=bar?points>=1200:points>=50;
 const ready=bar?points>=2500:points>=100;
 return <svg viewBox="0 0 1200 224" className={'merge-room-art'+(ready?' completed':'')} aria-hidden="true"
   preserveAspectRatio="xMidYMid slice">
  <defs>
   <linearGradient id="room-walls" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stopColor={bar?'#1d2736':'#26343a'}/>
    <stop offset="1" stopColor={cleaned?'#324e4a':'#2c3336'}/>
   </linearGradient>
   <linearGradient id="room-floor" x1="0" y1="0" x2="0" y2="1">
    <stop stopColor="#1d2d2f"/><stop offset="1" stopColor="#09161b"/>
   </linearGradient>
   <radialGradient id="room-spot"><stop stopColor={lit?'#d6b175':'#546f65'} stopOpacity={lit?.7:.14}/>
    <stop offset="1" stopColor="#2f3436" stopOpacity="0"/></radialGradient>
   <pattern id="room-grain" width="38" height="38" patternUnits="userSpaceOnUse">
    <path d="M0 38 L38 0" stroke="#d5c69e" strokeWidth=".6" opacity=".08"/>
   </pattern>
  </defs>
  <rect width="1200" height="224" fill="url(#room-walls)"/>
  <path d="M0 156 L1200 156 V224 H0Z" fill="url(#room-floor)"/>
  <path d="M0 155 H1200" stroke={lit?'#c5ab75':'#526465'} strokeWidth="3"/>
  <path d="M0 184 H1200 M0 208 H1200 M200 156 L175 224 M420 156 L385 224 M640 156 L605 224 M850 156 L840 224 M1080 156 L1094 224"
    stroke="#8c9479" strokeOpacity=".2" strokeWidth="1"/>
  <rect width="1200" height="224" fill="url(#room-grain)"/>
  <g stroke={cleaned?'#bdab7b':'#627073'} strokeWidth="5" fill="#101e26">
   {[76,306,542,766,1001].map(x=><g key={x}>
    <path d={'M'+x+' 16 V148'}/>
    <path d={'M'+(x-34)+' 16 V143 H'+(x+35)+' V16'} fill="#122b31"/>
    <path d={'M'+(x-16)+' 47 H'+(x+16)} strokeWidth="2" strokeOpacity=".5"/>
   </g>)}
  </g>
  {bar?<g>
    <rect x="380" y="52" width="440" height="104" rx="7" fill="#0a1721" stroke="#987b59" strokeWidth="5"/>
    <rect x="398" y="62" width="404" height="83" fill="#122b30" stroke="#675d49" strokeWidth="2"/>
    {[425,470,515,560,605,650,695,740,785].map((x,i)=><g key={x} opacity={cleaned?1:.28}>
      <rect x={x} y={75+(i%3)*7} width="17" height={46-(i%3)*7} rx="4" fill={['#86a6a2','#ddba78','#9d7fbb'][i%3]}/>
      <rect x={x+3} y={68+(i%3)*7} width="11" height="9" fill="#d6c295"/>
     </g>)}
    <path d="M265 150 H935 L963 174 H230Z" fill={cleaned?'#78543e':'#34343a'} stroke="#c6a470" strokeWidth="4"/>
    <path d="M230 174 H963 L930 216 H266Z" fill="#2b292c" stroke="#8f7858" strokeWidth="4"/>
   </g>:<g>
    <rect x="415" y="42" width="365" height="118" rx="26" stroke={lit?'#cba36a':'#626f72'}
     strokeWidth="8" fill="#14272b"/>
    <rect x="430" y="57" width="335" height="88" rx="20" stroke="#9e9270" strokeWidth="2" fill="#1e3938"/>
    <path d="M465 115 Q600 35 730 115" stroke="#809c8d" strokeWidth="4" fill="none" opacity=".3"/>
    <text x="598" y="103" fontFamily="Georgia" textAnchor="middle"
      fill={lit?'#efddb6':'#85948f'} fontSize="23" letterSpacing="7">MIDNIGHT CLUB</text>
    <path d="M455 129 H742" stroke="#ab8f65" strokeOpacity=".7" strokeWidth="2"/>
    <path d="M218 168 L290 110 L365 168 Z M836 168 L913 112 L982 168 Z" fill="#14272b" stroke="#6a786f" strokeWidth="3"/>
    <path d="M247 164 H336 M863 164 H954" stroke="#ae9167" strokeWidth="5"/>
   </g>}
  {lit&&<g>
    {[160,620,1050].map(x=><g key={x}>
      <ellipse cx={x} cy="9" rx="110" ry="90" fill="url(#room-spot)"/>
      <path d={'M'+(x-22)+' 0 H'+(x+22)+' L'+(x+14)+' 17 H'+(x-14)+'Z'} fill="#d7be85"/>
      <ellipse cx={x} cy="17" rx="17" ry="6" fill="#f5dcab"/>
    </g>)}
   </g>}
  {ready&&<g>
   {[130,340,860,1080].map(x=><g key={x}>
    <circle cx={x} cy="133" r="13" fill="#3d7b5f"/><circle cx={x-9} cy="124" r="10" fill="#628c5d"/>
    <path d={'M'+x+' 135 V166'} stroke="#9e764d" strokeWidth="5"/>
    <rect x={x-17} y="160" width="35" height="12" rx="3" fill="#72563c"/>
   </g>)}
   </g>}
  {!cleaned&&<g opacity=".47" fill="#a9ad98">
   {[110,205,330,610,781,953,1085].map((x,i)=><ellipse key={x} cx={x} cy={183+i%2*9} rx="34" ry="5"/>)}
   </g>}
  <rect width="1200" height="224" fill="url(#room-spot)" opacity=".16"/>
 </svg>;
}
