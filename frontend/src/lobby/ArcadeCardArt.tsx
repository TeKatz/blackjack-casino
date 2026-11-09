import type {ReactNode} from 'react';

/** Ten original, resolution-independent arcade illustrations. Hover movement is CSS-only. */
export function ArcadeCardArt({id}:{id:string}){
 const frame=(children:ReactNode)=><svg className="arcade-scene" viewBox="0 0 320 128"
  preserveAspectRatio="xMidYMid slice" focusable="false" aria-hidden="true">
  <defs>
   <linearGradient id="arcade-gold" x1="0" y1="0" x2="1" y2="1">
    <stop stopColor="#fff0c5"/><stop offset="1" stopColor="#c99e5e"/>
   </linearGradient>
   <linearGradient id="arcade-teal" x1="0" x2="1" y1="0" y2="1">
    <stop stopColor="#b0fff0"/><stop offset="1" stopColor="#358d95"/>
   </linearGradient>
  </defs>
  <g className="arcade-scene-stars" fill="#b5ede1" opacity=".5">
   <circle cx="22" cy="15" r="1.4"/><circle cx="58" cy="104" r="1"/><circle cx="250" cy="15" r="2"/>
   <circle cx="288" cy="93" r="1.5"/><circle cx="105" cy="30" r="1"/>
  </g>{children}</svg>;
 switch(id){
 case 'midnight-wings':return frame(<>
  <g className="arcade-art-motion" fill="#317e73" stroke="#e5c889" strokeWidth="2">
   <rect x="52" y="-45" width="32" height="92"/><rect x="52" y="92" width="32" height="80"/>
   <rect x="236" y="-26" width="32" height="70"/><rect x="236" y="85" width="32" height="110"/>
  </g><g className="arcade-art-fly" transform="translate(158 63)">
   <ellipse rx="20" ry="12" fill="url(#arcade-teal)" stroke="#ebd09d" strokeWidth="2"/>
   <path d="M-3 -3 L-23 -25 L-25 5" fill="#7debd9" stroke="#e8d5af" strokeWidth="2"/>
   <path d="M19 0 L35 7 L18 13" fill="#f7d58d"/><circle cx="10" cy="-4" r="3" fill="#14252a"/>
  </g></>);
 case 'cave-escape':return frame(<>
  <path d="M0 0 H320 V36 L278 47 L241 36 L191 55 L141 30 L91 46 L48 34 L0 49Z" fill="#496065" stroke="#cfaf78" strokeWidth="2"/>
  <path d="M0 128 H320 V92 L273 104 L228 85 L185 107 L133 82 L87 100 L42 88 L0 97Z" fill="#3e5960" stroke="#d1b37d" strokeWidth="2"/>
  <g className="arcade-art-fly" transform="translate(150 65)">
   <path d="M30 0 L-19 -15 L-24 0 L-19 15Z" fill="url(#arcade-teal)" stroke="#f0d9a4" strokeWidth="2"/>
   <circle cx="-2" r="6" fill="#102833"/><path d="M-24 -4 L-48 -8 M-24 4 L-52 8" stroke="#a2efd5" strokeWidth="3"/>
  </g></>);
 case 'hexagon-hell':return frame(<>
  <g className="arcade-art-spin" transform="translate(160 65)" fill="none" strokeWidth="12">
   <path d="M0 -58 L50 -29 L50 29 L0 58 L-50 29 L-50 -29Z" stroke="#9570d7"/>
   <path d="M0 -41 L35 -20 L35 20 L0 41 L-35 20 L-35 -20Z" stroke="#d4b1fa" strokeDasharray="148 50"/>
   <path d="M0 -18 L16 -9 L16 9 L0 18 L-16 9 L-16 -9Z" stroke="#e8d4ff" strokeWidth="5"/>
  </g><path d="M210 62 L235 53 L223 77Z" fill="#aaf3ec" stroke="#eed9a5" strokeWidth="2"/></>);
 case 'perfect-stack':return frame(<>
  <g className="arcade-art-stack" stroke="#f6e2b3" strokeWidth="1.5">
   <rect x="100" y="88" width="120" height="19" rx="3" fill="#4b9f9e"/>
   <rect x="110" y="67" width="107" height="19" rx="3" fill="#bc8392"/>
   <rect x="126" y="46" width="91" height="19" rx="3" fill="#77c9c0"/>
   <rect x="137" y="24" width="80" height="19" rx="3" fill="#ebc192" className="arcade-art-drop"/>
  </g><path d="M91 110 H228" stroke="#e5c78e" strokeWidth="3"/></>);
 case 'spike-rush':return frame(<>
  <path d="M0 103 H320" stroke="#d5b578" strokeWidth="4"/>
  <g className="arcade-art-obstacle" fill="#e7a2a0" stroke="#f9dcb1" strokeWidth="2">
   <path d="M180 103 L197 65 L213 103Z M215 103 L231 65 L247 103Z"/>
   <circle cx="278" cy="82" r="17" fill="#ae6b87" strokeDasharray="6 5"/>
  </g><g className="arcade-art-jump"><rect x="117" y="51" width="29" height="29" rx="5"
    fill="url(#arcade-teal)" stroke="#f5d8a4" strokeWidth="2"/><circle cx="136" cy="61" r="2" fill="#193b48"/></g></>);
 case 'blade-timing':return frame(<>
  <g className="arcade-art-spin" transform="translate(157 60)">
   <circle r="47" fill="#304650" stroke="#eacb91" strokeWidth="7"/>
   <circle r="31" fill="none" stroke="#72cfc5" strokeWidth="3"/>
   <path d="M0 -42 V-21 M42 0 H23 M-40 0 H-22" stroke="#f5e3b7" strokeWidth="6"/>
   <circle r="9" fill="#e7c38b"/>
  </g><g className="arcade-art-throw"><path d="M154 127 L155 88 L160 75 L165 88 L166 127" fill="#f4dfad" stroke="#f8f1d2" strokeWidth="2"/></g>
  <circle cx="160" cy="109" r="10" fill="none" stroke="#86efd7" strokeDasharray="3 4" strokeWidth="2"/></>);
 case 'bullet-storm':return frame(<>
  <g className="arcade-art-bullets">
   <path d="M65 18 L84 52 M141 9 L151 42 M248 11 L233 49 M287 32 L271 62" stroke="#75dfe4" opacity=".7" strokeWidth="4"/>
   <circle cx="85" cy="55" r="10" fill="#f7ae81"/><path d="M151 36 L162 50 L151 64 L140 50Z" fill="#cba9fe"/>
   <circle cx="230" cy="53" r="10" fill="#75ece0"/><circle cx="272" cy="71" r="8" fill="#fab38a"/>
  </g><path className="arcade-art-dodge" d="M155 95 L144 121 L156 114 L168 121Z" fill="#bbf8e4" stroke="#e4c392" strokeWidth="2"/></>);
 case 'neon-breaker':return frame(<>
  <g className="arcade-art-bricks" stroke="#e2d6c2" strokeWidth="1.4">
   {Array.from({length:4},(_,row)=>Array.from({length:7},(_,col)=>
    <rect key={row*7+col} x={44+col*34} y={14+row*18} width="30" height="14" rx="2"
     fill={['#87decf','#f0bc8b','#a99cfa','#d49fbc'][row]}/>))}
  </g><rect x="120" y="110" width="82" height="9" rx="4" fill="#91e4d3" stroke="#ffe1a4" strokeWidth="2"/>
  <circle className="arcade-art-ball" cx="180" cy="91" r="8" fill="#fff3c7" stroke="#65e5d5" strokeWidth="2"/></>);
 case 'death-snake':return frame(<>
  <g className="arcade-art-snake" stroke="#b4faac" strokeWidth="2">
   <rect x="88" y="79" width="30" height="30" rx="6" fill="#367d6a"/>
   <rect x="118" y="79" width="30" height="30" rx="6" fill="#409d79"/>
   <rect x="148" y="79" width="30" height="30" rx="6" fill="#59bd8d"/>
   <rect x="148" y="49" width="30" height="30" rx="6" fill="#70d3a3"/>
   <rect x="178" y="49" width="30" height="30" rx="7" fill="#a3ef9e"/>
   <circle cx="197" cy="58" r="2.8" fill="#1c3730"/>
  </g><circle cx="248" cy="62" r="11" fill="#ffd38c" stroke="#fbeab9" strokeWidth="3"/></>);
 case 'endless-ascent':return frame(<>
  <g className="arcade-art-platforms" fill="#7dbab5" stroke="#e6d1a3" strokeWidth="2">
   <rect x="42" y="100" width="104" height="12" rx="5"/><rect x="182" y="74" width="92" height="12" rx="5"/>
   <rect x="84" y="45" width="91" height="12" rx="5"/><rect x="203" y="17" width="78" height="12" rx="5" fill="#e6c585"/>
  </g><g className="arcade-art-jump">
   <rect x="154" y="30" width="24" height="32" rx="9" fill="#b4e8d9" stroke="#f7dfae" strokeWidth="2"/>
   <path d="M154 57 L151 66 M174 57 L180 64" stroke="#e6cd92" strokeWidth="3"/>
  </g><path d="M247 69 V39 M237 50 L247 39 L257 50" fill="none" stroke="#e9d4a5" strokeWidth="3"/></>);
 default:return frame(<circle cx="160" cy="64" r="32" fill="url(#arcade-teal)"/>);
 }
}
