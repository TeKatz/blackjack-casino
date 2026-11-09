import type {ReactNode} from 'react';

/** Gameplay-faithful replacements. Kept separate from the three accepted scenes. */
export function gameplayAccurateArcadeArt(id:string,gradient:(name:string)=>string):ReactNode|null{
 switch(id){
 case 'midnight-wings':return <>
  <circle cx="265" cy="30" r="23" fill="#ebd29b" opacity=".13"/>
  <path d="M0 108 28 96 65 106 108 81 148 96 187 84 236 106 277 88 320 102V128H0Z"
   fill="#13323b" opacity=".8"/>
  <g className="preview-wings-pipes">
   {[44,196,348].map((x,i)=>{
    const top=i%2===0?31:38,bottom=i%2===0?86:93;
    return <g key={x}>
     <rect x={x} y="-12" width="34" height={top+9} fill="#285f60" stroke="#bd9d6b" strokeWidth="1.5"/>
     <rect x={x} y={bottom} width="34" height={140-bottom} fill="#2d6662" stroke="#bd9d6b" strokeWidth="1.5"/>
     <path d={'M'+(x+8)+' 0V'+(top-10)+'M'+(x+23)+' 0V'+(top-10)+
       'M'+(x+8)+' '+(bottom+16)+'V128M'+(x+23)+' '+(bottom+16)+'V128'}
      fill="none" stroke="#c49e67" strokeWidth="1.4" opacity=".5"/>
     <rect x={x-8} y={top-10} width="50" height="18" rx="1" fill="#182b31"
      stroke="#e3c37c" strokeWidth="1.8"/>
     <rect x={x-8} y={bottom-7} width="50" height="18" rx="1" fill="#182b31"
      stroke="#e3c37c" strokeWidth="1.8"/>
     <rect x={x+11} y={top-6} width="12" height="10" fill="#91ebe0"/>
     <rect x={x+11} y={bottom-3} width="12" height="10" fill="#91ebe0"/>
     <path d={'M'+(x+16)+' '+(top-4)+'v6M'+(x+16)+' '+(bottom-1)+'v6'}
      stroke="#f0d99d" strokeWidth="2.5"/>
    </g>;
   })}
  </g>
  <g className="preview-wings-trail" fill="none" stroke="#9be6d5" strokeWidth="2"
   strokeLinecap="round" opacity=".7">
   <path d="M68 64H100 M77 73H98"/>
  </g>
  {/* Same oval, back-wing, gold beak and pale eye proportions as wings(). */}
  <g className="preview-wings-bird">
   <ellipse cx="127" cy="67" rx="22" ry="14" fill="#1d414a" stroke="#f7d99c" strokeWidth="2.4"/>
   <path className="preview-wings-flap" d="M113 62 94 39 101 73Z"
    fill="#74ddda" stroke="#e1d4ab" strokeWidth="1.6"/>
   <path d="M146 66 160 73 146 78Z" fill="#f1ce84"/>
   <circle cx="135" cy="61" r="3.8" fill="#f6eace"/>
   <circle cx="135" cy="61" r="1.9" fill="#10212c"/>
  </g>
 </>;
 case 'cave-escape':return <>
  {/* Entrance clearance is intentionally broad, following the initial game segments. */}
  <path d="M0 0H320V24L293 20 270 31 239 22 213 29 183 19 153 28 125 23 94 30 66 20 37 28 0 21Z"
   fill="#243d40" stroke="#d3ae74" strokeWidth="1.8"/>
  <path d="M0 128H320V107L293 112 267 102 238 111 207 103 179 114 148 104 120 112 92 103 61 113 33 104 0 110Z"
   fill="#273f42" stroke="#d3ae74" strokeWidth="1.8"/>
  <g className="preview-cave-parallax" fill="none" stroke="#91d9c5" strokeWidth="1.7" opacity=".48">
   <path d="M-20 17 12 20 35 13 69 18 96 13 123 20 152 14 189 19 218 13 246 18 275 12 336 19"/>
   <path d="M-20 118 14 115 47 120 76 116 110 122 145 117 181 122 218 118 252 123 287 115 336 121"/>
  </g>
  <path d="M0 66H320" stroke="#6ad6c2" strokeWidth="1" opacity=".2" strokeDasharray="9 18"/>
  <g className="preview-cave-trail" fill="none" stroke="#80f0dc" strokeWidth="2" strokeLinecap="round">
   <path d="M111 64 70 56 M111 70 64 79" opacity=".7"/>
  </g>
  <g className="preview-cave-ship">
   <path d="M176 66 140 51 112 66 140 81Z" fill="#85e7dc" stroke="#f8dfac" strokeWidth="2"/>
   <path d="M131 56 116 48 120 66 116 83 131 75" fill="#338a92"
    stroke="#e1d5b2" strokeWidth="1.2"/>
   <circle cx="143" cy="66" r="5.7" fill="#11333b"/>
  </g>
 </>;
 case 'hexagon-hell':return <>
  {/* Actual Hexagon Hell uses circular wall arcs, a central hexagon and a fixed player orbit. */}
  <g fill="none" stroke="#b899ed" opacity=".22">
   <circle cx="160" cy="64" r="60"/><circle cx="160" cy="64" r="31"/>
   {[0,60,120,180,240,300].map(deg=><path key={deg} d="M160 64H219"
    transform={'rotate('+deg+' 160 64)'} strokeWidth=".8"/>)}
  </g>
  <g className="preview-hex-outer" fill="none">
   <circle cx="160" cy="64" r="55" stroke="#b99bf3" strokeWidth="11"
    strokeDasharray="247 99"/>
   <circle cx="160" cy="64" r="55" stroke="#ead8ff" strokeWidth="2"
    strokeDasharray="247 99" opacity=".8"/>
  </g>
  <g className="preview-hex-inner" fill="none">
   <circle cx="160" cy="64" r="45" stroke="#8061bd" strokeWidth="8"
    strokeDasharray="184 99"/>
   <circle cx="160" cy="64" r="45" stroke="#e1c7ff" strokeWidth="2"
    strokeDasharray="184 99"/>
  </g>
  <path d="M160 46 175.6 55V73L160 82 144.4 73V55Z"
   fill="#312b57" stroke="#d4b5fa" strokeWidth="3"/>
  <path d="M160 54 169 59V69L160 74 151 69V59Z"
   fill="#6d5d9a" stroke="#f6e6ff" strokeWidth="1.4"/>
  <circle cx="160" cy="64" r="3.5" fill="#fff0d3"/>
  <circle cx="160" cy="64" r="33" fill="none" stroke="#f8e4bf" strokeWidth="1" opacity=".32"/>
  <g className="preview-hex-player">
   <path d="M194 54 183 71 201 66Z" fill="#aafbf0" stroke="#fff0c0" strokeWidth="1.8"/>
  </g>
 </>;
 case 'perfect-stack':return <>
  <path d="M14 120 158 82 307 120" fill="none" stroke="#d7c9ad" opacity=".3"/>
  <path d="M92 116H229" stroke="#ffe0a5" strokeWidth="3" opacity=".7"/>
  <g className="preview-stack-tower" stroke="#f6dfb5" strokeWidth="1.5">
   <rect x="100" y="97" width="124" height="18" rx="2" fill="#377e85"/>
   <path d="M100 115 106 120H224V115" fill="#194a55"/>
   <rect x="113" y="76" width="111" height="18" rx="2" fill="#b87d93"/>
   <path d="M113 94 119 99H224V94" fill="#794b70"/>
   <rect x="129" y="55" width="95" height="18" rx="2" fill="#6abdb4"/>
   <path d="M129 73 135 78H224V73" fill="#317b81"/>
   <path d="M134 59H219" stroke="#fff6d5" strokeWidth="2" opacity=".7"/>
  </g>
  <g className="preview-stack-drop" stroke="#ffe2b3" strokeWidth="1.6">
   <rect x="129" y="5" width="95" height="18" rx="2" fill={gradient('metal')}/>
   <path d="M129 23 136 27H224V23" fill="#ab795f"/>
   <path d="M136 9H216" stroke="#fff9e2" strokeWidth="2" opacity=".85"/>
  </g>
  <g className="preview-stack-hit" fill="#ffe7b0">
   <circle cx="127" cy="54" r="2"/><circle cx="226" cy="54" r="2"/>
   <path d="M123 48 117 42M231 48 237 42" stroke="#f8e5ad" strokeWidth="2"/>
  </g>
 </>;
 case 'spike-rush':return <>
  <path d="M0 105H320V128H0Z" fill="#122934"/>
  <path d="M0 105H320" stroke="#cfad76" strokeWidth="3.5"/>
  <path d="M0 110H320" stroke="#518f91" strokeWidth="3" opacity=".7"/>
  <g className="preview-spike-speed" fill="none" stroke="#ffdfb1" strokeWidth="1.6" opacity=".5">
   <path d="M4 81H49 M18 93H76 M265 57H304"/>
  </g>
  <g className="preview-spike-hazard">
   <path d="M207 105 225 68 244 105Z" fill="#c58da7" stroke="#fff1d0" strokeWidth="2"/>
   <path d="M211 102 225 78 239 102Z" fill="#f4dba7" opacity=".45"/>
  </g>
  <ellipse className="preview-spike-shadow" cx="131" cy="104" rx="17" ry="3"
   fill="#f9cd99" opacity=".37"/>
  {/* 34×34 rounded square, cream/mint/blue body and two pixel eyes exactly like spikeRush(). */}
  <g className="preview-spike-cube">
   <rect x="114" y="71" width="34" height="34" rx="5" fill={gradient('energy')}
    stroke="#f8dda1" strokeWidth="2.5"/>
   <path d="M117 74H143" stroke="#fff4cf" opacity=".7" strokeWidth="2"/>
   <rect x="125" y="84" width="4" height="5" fill="#1c3347"/>
   <rect x="135" y="84" width="4" height="5" fill="#1c3347"/>
  </g>
  <g className="preview-spike-dust" fill="none" stroke="#fff0c9" strokeWidth="2" opacity=".7">
   <path d="M109 104 103 100 M151 104 157 100"/>
  </g>
 </>;
 case 'blade-timing':return <>
  {/* Rim never moves; only the inner rotor and its attached blades rotate at (159,55). */}
  <circle cx="159" cy="55" r="46" fill="#203947" stroke={gradient('metal')} strokeWidth="6"/>
  <circle cx="159" cy="55" r="38" fill="#244650" stroke="#8cdbc6" strokeWidth="2.4"/>
  <g className="preview-blade-rotor">
   {[0,45,90,135,180,225,270,315].map(deg=>
    <path key={deg} d="M159 26V44" stroke="#e7d29d" strokeWidth="3"
     transform={'rotate('+deg+' 159 55)'}/>)}
   <circle cx="159" cy="55" r="24" fill="none" stroke="#c6e5cb"
    strokeWidth="1.5" strokeDasharray="5 6"/>
   {[35,145,255].map(deg=><g key={deg} transform={'rotate('+deg+' 159 55)'}>
    <path d="M155 12 155 -2 159 -11 163 -2 163 12Z"
     fill="#f9dfa3" stroke="#fff0c0" strokeWidth="1.3"/>
    <rect x="155" y="4" width="8" height="8" fill="#385662"/>
   </g>)}
  </g>
  <circle cx="159" cy="55" r="9" fill="#e3c481" stroke="#324952" strokeWidth="2.5"/>
  <circle cx="159" cy="55" r="3.5" fill="#20313b"/>
  <g className="preview-blade-guide" fill="none" stroke="#8fdfc8" strokeWidth="1.5">
   <path d="M141 101H152 M166 101H177"/>
   <circle cx="159" cy="101" r="8" strokeDasharray="2 4" opacity=".45"/>
  </g>
  <g className="preview-blade-throw">
   <path d="M155 116 159 107 163 116 163 136H155Z"
    fill={gradient('metal')} stroke="#fff1c2" strokeWidth="1.8"/>
   <rect x="153" y="125" width="12" height="6" rx="1.5" fill="#597e83"/>
  </g>
  <g className="preview-blade-impact" fill="none" stroke="#a4f1d2" strokeWidth="2">
   <circle cx="159" cy="102" r="8" strokeDasharray="3 3"/>
   <path d="M143 93 139 89 M175 93 179 89"/>
  </g>
 </>;
 case 'endless-ascent':return <>
  <path d="M0 108 52 88 89 102 131 76 183 101 228 66 320 98V128H0Z"
   fill="#17344b" opacity=".62"/>
  <g className="preview-ascent-clouds" fill="#b7d7fc" opacity=".18">
   <ellipse cx="48" cy="35" rx="42" ry="10"/><ellipse cx="276" cy="45" rx="46" ry="9"/>
  </g>
  <g className="preview-ascent-platforms" stroke="#f4d9a9" strokeWidth="1.7">
   <rect x="82" y="106" width="89" height="12" rx="5" fill="#376d78"/>
   <rect x="151" y="54" width="92" height="12" rx="5" fill="#65b8bd"/>
   <rect x="251" y="10" width="65" height="12" rx="5" fill={gradient('metal')}/>
   <path d="M92 109H159 M161 57H231 M260 13H306" stroke="#e0fff4" opacity=".65"/>
  </g>
  <path d="M122 98Q126 39 176 43" fill="none" stroke="#b9f3e6"
   strokeWidth="1.5" strokeDasharray="3 7" opacity=".33"/>
  <g className="preview-ascent-trail" stroke="#b4eaff" strokeWidth="2" opacity=".55">
   <path d="M122 101v13 M115 103v8 M129 103v8" strokeLinecap="round"/>
  </g>
  {/* Feet start at y=106. Final position x+55,y-52 = feet y=54 on upper platform. */}
  <g className="preview-ascent-player">
   <rect x="110" y="79" width="26" height="27" rx="9" fill={gradient('energy')}
    stroke="#f9dda5" strokeWidth="2"/>
   <circle cx="119" cy="91" r="1.8" fill="#18394a"/>
   <circle cx="128" cy="91" r="1.8" fill="#18394a"/>
  </g>
  <g className="preview-ascent-landing" fill="none" stroke="#a2e9de" strokeWidth="2">
   <path d="M164 52 160 46 M220 52 224 46" opacity=".7"/>
  </g>
 </>;
 default:return null;
 }
}
