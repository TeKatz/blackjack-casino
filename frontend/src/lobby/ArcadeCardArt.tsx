import type {ReactNode} from 'react';

/**
 * Ten bespoke miniature gameplay illustrations, not interchangeable symbols.
 * A scene's moving pieces are separate SVG layers so hover/focus can demonstrate
 * its actual mechanic. All art is bundled offline and has no pointer handlers.
 */
const palettes:Record<string,readonly [string,string,string]>={
 'midnight-wings':['#071d32','#264058','#efc27c'],
 'cave-escape':['#082b34','#1c5559','#76e5ca'],
 'hexagon-hell':['#15142f','#47336e','#cc9cff'],
 'perfect-stack':['#182635','#4e5360','#f2bd87'],
 'spike-rush':['#271a31','#593442','#ff9b82'],
 'blade-timing':['#112631','#3a515b','#f6d79c'],
 'bullet-storm':['#111b3c','#2b4867','#82e4ff'],
 'neon-breaker':['#101b39','#26496d','#80f6e1'],
 'death-snake':['#102731','#2a523f','#c5f782'],
 'endless-ascent':['#102942','#47627c','#c5d7ff'],
};

export function ArcadeCardArt({id}:{id:string}){
 const [dark,light,highlight]=palettes[id]??palettes['midnight-wings'];
 const gradient=(name:string)=>'url(#arcade-'+id+'-'+name+')';
 const frame=(children:ReactNode)=><svg
  className="arcade-scene" data-arcade-scene={id} viewBox="0 0 320 128"
  preserveAspectRatio="xMidYMid slice" aria-hidden="true" focusable="false">
  <defs>
   <linearGradient id={'arcade-'+id+'-sky'} x1="0" y1="0" x2="1" y2="1">
    <stop stopColor={light}/><stop offset=".48" stopColor={dark}/><stop offset="1" stopColor="#06191f"/>
   </linearGradient>
   <linearGradient id={'arcade-'+id+'-metal'} x1="0" y1="0" x2="1" y2="1">
    <stop stopColor="#fdf0c9"/><stop offset=".36" stopColor={highlight}/>
    <stop offset="1" stopColor="#957252"/>
   </linearGradient>
   <linearGradient id={'arcade-'+id+'-energy'} x1="0" y1="0" x2="1" y2="1">
    <stop stopColor="#d5fff6"/><stop offset=".52" stopColor={highlight}/>
    <stop offset="1" stopColor="#2c8a9a"/>
   </linearGradient>
   <radialGradient id={'arcade-'+id+'-glow'}>
    <stop stopColor={highlight} stopOpacity=".29"/><stop offset="1" stopColor={highlight} stopOpacity="0"/>
   </radialGradient>
   <pattern id={'arcade-'+id+'-grid'} width="24" height="24" patternUnits="userSpaceOnUse">
    <path d="M24 0H0V24" fill="none" stroke="#c2ebdd" strokeOpacity=".17" strokeWidth=".6"/>
   </pattern>
  </defs>
  <rect width="320" height="128" fill={gradient('sky')}/>
  <ellipse cx="165" cy="62" rx="190" ry="105" fill={gradient('glow')}/>
  <rect width="320" height="128" fill={gradient('grid')} opacity=".34"/>
  <g fill="#d7e9db" opacity=".62">
   <circle cx="23" cy="19" r=".8"/><circle cx="83" cy="29" r="1"/>
   <circle cx="293" cy="23" r="1"/><circle cx="262" cy="109" r=".8"/>
  </g>
  {children}
 </svg>;

 switch(id){
 case 'midnight-wings':return frame(<>
  <circle cx="265" cy="30" r="23" fill="#ffd997" opacity=".23"/>
  <path d="M0 107 29 90 45 98 91 66 120 92 155 76 205 112 251 88 320 102V128H0Z"
   fill="#102637" opacity=".85"/>
  <g className="arcade-wings-course">
   {[44,196,348].map((x,i)=><g key={x}>
    <path d={'M'+x+' -12 h35 V'+(i%2===0?42:57)+' h-35Z'}
     fill="#235e60" stroke="#e9c88b" strokeWidth="2"/>
    <path d={'M'+x+' '+(i%2===0?85:101)+' h35 v58 h-35Z'}
     fill="#286b68" stroke="#e9c88b" strokeWidth="2"/>
    <path d={'M'+(x+5)+' 0v'+(i%2===0?35:50)+' M'+(x+5)+' '+(i%2===0?90:106)+'v35'}
     stroke="#9ce4c6" strokeWidth="2" opacity=".8"/>
    <circle cx={x+17} cy={i%2===0?57:72} r="2.3" fill="#ffde98" opacity=".85"/>
   </g>)}
  </g>
  <g className="arcade-wings-trail" stroke="#a4ffed" fill="none" strokeLinecap="round">
   <path d="M66 66 101 66 M75 73 95 73" strokeWidth="2.2" opacity=".8"/>
   <path d="M52 61 86 61" strokeWidth="1.2" opacity=".5"/>
  </g>
  <g className="arcade-wings-bird">
   <path d="M118 67Q101 47 123 39L145 60Z" fill="#4ccbbb" stroke="#ffe3b0" strokeWidth="1.5"
    className="arcade-wings-flap"/>
   <path d="M115 64 99 75 125 70" fill="#5bc5bb"/>
   <path d="M111 62Q131 49 150 66Q131 83 111 70Z" fill={gradient('energy')}
    stroke="#ffe2ac" strokeWidth="2"/>
   <path d="M143 65 157 68 143 73" fill={gradient('metal')}/>
   <circle cx="138" cy="63" r="3" fill="#0a2735"/>
   <path d="M116 72Q103 90 129 86L140 71" className="arcade-wings-flap-bottom"
    fill="#2d988f" stroke="#ade9d2" strokeWidth="1.5"/>
  </g>
 </>);
 case 'cave-escape':return frame(<>
  <path d="M0 0H320V41L297 36 280 54 259 34 235 42 208 29 190 49 165 37 134 51 108 30 82 50 63 35 35 53 0 40Z"
   fill="#19363c" stroke="#b2a07b" strokeWidth="2"/>
  <path d="M0 128H320V99L299 82 268 98 240 88 218 105 183 86 155 101 128 81 97 100 69 89 40 105 0 92Z"
   fill="#214247" stroke="#c6af85" strokeWidth="2"/>
  <g className="arcade-cave-depth">
   <path d="M0 29 28 35 48 29 72 39 101 29 126 41 155 29 177 38 205 25 240 36 275 24 320 35"
    stroke="#77b6af" strokeWidth="2" fill="none" opacity=".6"/>
   <path d="M0 104 37 94 65 99 101 91 126 101 163 89 202 100 234 91 273 100 320 94"
    stroke="#75aaa2" strokeWidth="2" fill="none" opacity=".7"/>
   <path d="M49 41 62 61 76 45 M229 99 238 79 248 97" fill="#8ce7d3" opacity=".5"/>
   <circle cx="289" cy="66" r="3" fill="#80e5ce" opacity=".7"/>
  </g>
  <path d="M0 65H320" stroke="#9cf5e0" opacity=".18" strokeDasharray="9 17"/>
  <g className="arcade-cave-thrust">
   <path d="M117 63 79 55 91 65 78 76Z" fill="#6ef0d9" opacity=".6"/>
   <path d="M105 63 62 63" stroke="#a4f8e6" strokeWidth="2" opacity=".8"/>
  </g>
  <g className="arcade-cave-glider">
   <path d="M111 64 139 51 175 65 138 79Z" fill={gradient('energy')}
    stroke="#f3dfb4" strokeWidth="2"/>
   <path d="M131 56 114 47 119 64 114 81 131 74" fill="#338a92" stroke="#ffe4bd" strokeWidth="1.5"/>
   <path d="M158 63 174 65 158 69" fill={gradient('metal')}/>
   <ellipse cx="139" cy="64" rx="7" ry="6" fill="#0c313a"/>
  </g>
 </>);
 case 'hexagon-hell':return frame(<>
  <path d="M0 110 72 80 128 93 184 64 250 83 320 51" stroke="#ae7dd9" strokeOpacity=".22"
   strokeWidth="3" fill="none"/>
  <g className="arcade-hex-ring-out" transform="translate(155 65)" fill="none">
   <path d="M0-59 51-30 51 30 0 59-51 30-51-30Z" stroke="#9368d3"
    strokeWidth="12" strokeDasharray="224 36 82 20" strokeLinejoin="bevel"/>
   <path d="M0-59 51-30 51 30 0 59-51 30-51-30Z" stroke="#dfb7ff"
    strokeWidth="2.4" strokeDasharray="38 292" opacity=".8"/>
  </g>
  <g className="arcade-hex-ring-mid" transform="translate(155 65)" fill="none">
   <path d="M0-41 35-20 35 20 0 41-35 20-35-20Z" stroke="#d1aaf4"
    strokeWidth="10" strokeDasharray="83 45 107 8"/>
   <path d="M0-41 35-20 35 20 0 41-35 20-35-20Z" stroke="#eee2ff"
    strokeWidth="2" strokeDasharray="12 199"/>
  </g>
  <path d="M155 49 169 57V73L155 81 141 73V57Z" fill="#1a2444"
   stroke="#e6d3ff" strokeWidth="3.5"/>
  <g className="arcade-hex-pilot" transform="translate(155 65)">
   <path d="M58-8 79-17 69 8Z" fill="#9ff2e3" stroke="#ffe8bd" strokeWidth="2"/>
   <circle cx="68" cy="-3" r="2" fill="#4b3298"/>
  </g>
  <circle cx="155" cy="65" r="4" fill="#efddff"/>
 </>);
 case 'perfect-stack':return frame(<>
  <path d="M20 112 157 76 303 112 M45 125 157 91 279 125" fill="none" stroke="#cbb7a2"
   strokeOpacity=".25" strokeWidth="1.5"/>
  <g className="arcade-stack-tower" stroke="#f6dfb5" strokeWidth="1.4">
   <rect x="99" y="87" width="122" height="18" rx="2" fill="#377e85"/>
   <path d="M99 105 106 111H221V105" fill="#1a5258" stroke="#b2c8ac"/>
   <rect x="109" y="67" width="112" height="18" rx="2" fill="#b87d93"/>
   <path d="M109 85 115 91H221V85" fill="#794b70"/>
   <rect x="123" y="47" width="98" height="18" rx="2" fill="#6abdb4"/>
   <path d="M123 65 129 71H221V65" fill="#317b81"/>
   <rect x="136" y="27" width="85" height="18" rx="2" fill="#eac193"/>
   <path d="M136 45 143 50H221V45" fill="#b58264"/>
  </g>
  <path d="M92 113H228" stroke="#ffe0a5" strokeWidth="3" opacity=".7"/>
  <g className="arcade-stack-active" stroke="#ffe2b3" strokeWidth="1.5">
   <rect x="147" y="8" width="85" height="16" rx="2" fill={gradient('metal')}/>
   <path d="M147 24 154 28H232V24" fill="#ab795f"/>
   <path d="M151 11H224" stroke="#fff9e2" opacity=".8"/>
  </g>
  <g className="arcade-stack-sparks" fill="#ffe7b0">
   <circle cx="131" cy="30" r="2"/><circle cx="224" cy="32" r="2"/>
   <path d="M128 23v-6 M231 34l5 5" stroke="#f8e5ad" strokeWidth="2"/>
  </g>
 </>);
 case 'spike-rush':return frame(<>
  <path d="M0 109H320V128H0Z" fill="#152d36"/>
  <path d="M0 103H320" stroke="#ffd093" strokeWidth="4"/>
  <path d="M0 108H320" stroke="#ee8b84" strokeWidth="1.4" opacity=".7"/>
  <g className="arcade-spike-speed" stroke="#ffdfb1" strokeWidth="2" opacity=".6">
   <path d="M12 72H64 M30 82H83 M2 93H42 M241 54H283" />
  </g>
  <g className="arcade-spike-hazards" fill="#a95a7c" stroke="#ffcb9f" strokeWidth="2">
   <path d="M200 102 218 61 236 102Z M241 102 259 61 277 102Z"/>
   <path d="M220 89 220 79 M260 90 260 79" stroke="#ffe7c3" strokeWidth="1.5"/>
   <circle cx="302" cy="83" r="17" fill="#683c65" strokeDasharray="5 4"/>
   <path d="M301 62v-7 M301 110v-7 M281 83h-7" strokeWidth="2"/>
  </g>
  <ellipse cx="129" cy="103" rx="19" ry="4" fill="#f7c991" opacity=".3"/>
  <g className="arcade-spike-runner">
   <rect x="116" y="61" width="29" height="29" rx="6" fill={gradient('energy')}
    stroke="#ffe4af" strokeWidth="2"/>
   <path d="M121 66H139" stroke="#fff0ca" opacity=".9"/>
   <circle cx="137" cy="72" r="2.6" fill="#132d3d"/>
   <path d="M120 92 116 99 M141 92 145 99" stroke="#b6f4da" strokeWidth="3"/>
  </g>
  <path className="arcade-spike-landing" d="M112 100Q131 91 150 100" stroke="#fff3ba"
   strokeWidth="2" strokeDasharray="3 4" fill="none"/>
 </>);
 case 'blade-timing':return frame(<>
  <circle cx="158" cy="62" r="59" fill="#d7bb8a" opacity=".07"/>
  <g className="arcade-blade-wheel" transform="translate(158 62)">
   <circle r="49" fill="#172e3b" stroke={gradient('metal')} strokeWidth="6"/>
   <circle r="36" fill="#335764" stroke="#78c5bd" strokeWidth="2.5"/>
   <circle r="25" fill="#1d3b47" stroke="#d8d8b6" strokeDasharray="6 9" strokeWidth="2"/>
   {Array.from({length:8},(_,i)=><path key={i} d="M0-29V-39" stroke="#e9cfa0"
     strokeWidth="3" transform={'rotate('+(i*45)+' 0 0)'}/>)}
   <g fill="#ebdab3" stroke="#fff4d5" strokeWidth="1">
    <path d="M-4-48-4-63 0-70 4-63 4-48Z"/>
    <path d="M39 28 51 35 59 36 53 29 44 21Z"/>
    <path d="M-40 27-52 36-60 36-53 29-45 20Z"/>
   </g>
   <circle r="10" fill={gradient('metal')} stroke="#102d37" strokeWidth="3"/>
   <circle r="3" fill="#5d4b43"/>
  </g>
  <g className="arcade-blade-shot">
   <path d="M154 124 154 99 158 80 162 99 162 124Z" fill={gradient('metal')}
    stroke="#fef4d2" strokeWidth="1.5"/>
   <path d="M154 108H162" stroke="#4a6d72" strokeWidth="3"/>
   <path d="M158 126V135" stroke="#85e7d4" strokeWidth="3"/>
  </g>
  <g className="arcade-blade-impact" fill="none" stroke="#9ee7da" strokeWidth="2">
   <circle cx="158" cy="105" r="12" strokeDasharray="4 5"/>
   <path d="M147 94 141 87 M168 94 174 87"/>
  </g>
 </>);
 case 'bullet-storm':return frame(<>
  <path d="M0 115 45 87 91 112 137 85 177 113 239 79 320 112" fill="none"
   stroke="#89c0f9" strokeOpacity=".25" strokeWidth="2"/>
  <g className="arcade-bullet-wave-a">
   {[[57,24],[103,45],[150,15],[204,47],[253,21],[297,56]].map(([x,y],i)=>
    <g key={i}>
     <path d={'M'+(x-7)+' '+(y-18)+'l6 11'} stroke="#79dbe8" strokeWidth="2.5" opacity=".75"/>
     <circle cx={x} cy={y} r={i%2===0?7:9} fill={i%3===0?'#ffac8e':'#87e8eb'}
      stroke="#ddf9e7" strokeOpacity=".45" strokeWidth="1.5"/>
     <circle cx={x-2} cy={y-2} r="2" fill="#fff4d8" opacity=".8"/>
    </g>)}
  </g>
  <g className="arcade-bullet-wave-b">
   {[[32,71],[84,86],[145,65],[210,86],[279,91]].map(([x,y],i)=>
    <g key={i}>
     <path d={'M'+(x-10)+' '+(y-18)+'l6 10'} stroke="#cb9fff" strokeWidth="2" opacity=".45"/>
     <path d={'M'+x+' '+(y-9)+'l8 9-8 9-8-9Z'} fill="#c6abfa" stroke="#e8dcff" strokeWidth="1"/>
    </g>)}
  </g>
  <g className="arcade-bullet-dodger">
   <path d="M161 90 143 123 161 114 179 123Z" fill={gradient('energy')}
    stroke="#f9d9a2" strokeWidth="2"/>
   <path d="M161 100V115" stroke="#e7fff7" strokeWidth="2"/>
   <path d="M151 121 160 128 170 121" fill="none" stroke="#87e7e8" strokeWidth="2"/>
  </g>
  <path className="arcade-bullet-nearmiss" d="M120 102Q135 90 141 75" fill="none"
   stroke="#b4efff" strokeDasharray="3 6" strokeWidth="2"/>
 </>);
 case 'neon-breaker':return frame(<>
  <path d="M15 123H305 M27 117 112 91 M293 117 208 91" stroke="#8bdadf"
   strokeWidth="1.5" strokeOpacity=".35" fill="none"/>
  <g className="arcade-breaker-bricks" stroke="#dbf9ec" strokeWidth="1.5">
   {Array.from({length:3},(_,row)=>Array.from({length:7},(_,col)=>{
    const x=39+col*35,y=13+row*19;
    return <g key={row*7+col} className={row===2&&col===4?'arcade-breaker-target':undefined}>
     <rect x={x} y={y} width="31" height="15" rx="2.5"
      fill={['#52c7c3','#e8b887','#9c8ee5'][row]}/>
     <path d={'M'+(x+3)+' '+(y+3)+'h24'} stroke="#fff8d8" strokeWidth="1" opacity=".65"/>
    </g>;
   }))}
  </g>
  <g className="arcade-breaker-shards" fill="#a2fff3">
   <path d="M188 63 193 72 183 70Z M208 65 216 60 215 72Z M201 78 207 86 196 83Z"/>
  </g>
  <path className="arcade-breaker-ray" d="M145 114 198 66" stroke="#e4fff5"
   strokeWidth="2" strokeDasharray="4 6" opacity=".6"/>
  <g className="arcade-breaker-paddle">
   <rect x="108" y="112" width="90" height="10" rx="5" fill={gradient('energy')}
    stroke="#ffe7b6" strokeWidth="2"/>
   <rect x="121" y="114" width="64" height="2" rx="1" fill="#e2fff0"/>
  </g>
  <g className="arcade-breaker-ball">
   <circle cx="154" cy="102" r="8" fill="#fdf4cb" stroke="#64e7dc" strokeWidth="2"/>
   <circle cx="151" cy="99" r="2" fill="white"/>
  </g>
 </>);
 case 'death-snake':return frame(<>
  <rect x="23" y="8" width="274" height="111" rx="7" fill="#0c2b2d" stroke="#9ab677"
   strokeOpacity=".5"/>
  <path d="M23 34H297 M23 60H297 M23 86H297 M55 8V119 M87 8V119 M119 8V119 M151 8V119 M183 8V119 M215 8V119 M247 8V119 M279 8V119"
   stroke="#7aa988" strokeWidth=".8" opacity=".19"/>
  <path d="M262 34h17v17" fill="none" stroke="#eac28b" strokeWidth="3" opacity=".6"/>
  <path d="M78 105H111V81H156V55H195" fill="none" stroke="#214b42"
   strokeWidth="31" strokeLinecap="round" strokeLinejoin="round"/>
  <path d="M78 105H111V81H156V55H195" fill="none" stroke="#5cbd8b"
   strokeWidth="27" strokeLinecap="round" strokeLinejoin="round"/>
  <path className="arcade-snake-current" d="M78 105H111V81H156V55H195" fill="none"
   stroke="#b8f9a6" strokeWidth="4" strokeLinecap="round" strokeDasharray="15 18" opacity=".65"/>
  <g className="arcade-snake-face">
   <rect x="178" y="41" width="31" height="28" rx="9" fill={gradient('energy')}
    stroke="#e1ffc0" strokeWidth="1.8"/>
   <circle cx="201" cy="49" r="2.4" fill="#19392f"/>
   <path d="M209 56h9" stroke="#ffb9b4" strokeWidth="2"/>
  </g>
  <g className="arcade-snake-food">
   <circle cx="247" cy="55" r="11" fill="#ffdb93" stroke="#fff5c8" strokeWidth="2"/>
   <path d="M247 43V38" stroke="#b9f3a6" strokeWidth="2.5"/>
   <ellipse cx="251" cy="40" rx="5" ry="2.5" fill="#a0ed9b" transform="rotate(-30 251 40)"/>
  </g>
 </>);
 case 'endless-ascent':return frame(<>
  <path d="M0 101 51 82 90 96 137 61 182 93 234 59 320 80V128H0Z"
   fill="#17344b" opacity=".65"/>
  <g className="arcade-ascent-clouds" fill="#b7d7fc" opacity=".18">
   <ellipse cx="48" cy="36" rx="44" ry="11"/><ellipse cx="270" cy="58" rx="52" ry="10"/>
  </g>
  <g className="arcade-ascent-platforms" stroke="#f4d9a9" strokeWidth="1.6">
   <rect x="31" y="110" width="100" height="11" rx="5" fill="#376d78"/>
   <rect x="175" y="85" width="96" height="11" rx="5" fill="#4a9da5"/>
   <rect x="66" y="58" width="91" height="11" rx="5" fill="#65c5c2"/>
   <rect x="206" y="29" width="85" height="11" rx="5" fill={gradient('metal')}/>
   <path d="M45 112h69 M191 87h60 M81 60h59 M223 31h52" stroke="#c9fff0" opacity=".6"/>
  </g>
  <g className="arcade-ascent-trail" stroke="#b4eaff" strokeWidth="2.5" opacity=".65">
   <path d="M160 93v18 M153 100v10 M168 96v11" strokeLinecap="round"/>
  </g>
  <g className="arcade-ascent-jumper">
   <rect x="150" y="43" width="25" height="32" rx="9" fill={gradient('energy')}
    stroke="#ffe0af" strokeWidth="2"/>
   <path d="M154 73 149 81 M171 73 177 81" stroke="#b4fff2" strokeWidth="3" strokeLinecap="round"/>
   <circle cx="167" cy="54" r="2.2" fill="#172e4b"/>
   <path d="M152 48q10-8 20 0" stroke="#fff3d2" fill="none" opacity=".7"/>
  </g>
  <path className="arcade-ascent-arrow" d="M292 71V46 M283 55 292 46 301 55" stroke="#e7d9af"
   strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" fill="none"/>
 </>);
 default:return frame(<circle cx="160" cy="64" r="30" fill={gradient('energy')}/>);
 }
}
