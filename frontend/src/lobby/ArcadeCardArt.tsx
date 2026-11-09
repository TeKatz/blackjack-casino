import type {ReactNode} from 'react';
import {gameplayAccurateArcadeArt} from './ArcadeCardScenesV2';

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

 const revised=gameplayAccurateArcadeArt(id,gradient);
 if(revised!==null)return frame(revised);
 switch(id){






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

 default:return frame(<circle cx="160" cy="64" r="30" fill={gradient('energy')}/>);
 }
}
