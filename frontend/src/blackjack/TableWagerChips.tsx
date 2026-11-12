import type {CSSProperties} from 'react';

export function stackHeights(wager:string):number[]{
 const amount=Number(wager);
 if(!Number.isFinite(amount)||amount<=0)return [];
 const height=Math.max(1,Math.min(12,1+Math.floor(Math.log10(Math.max(1,amount))*2)));
 if(height<=3)return [height];
 if(height<=7)return [Math.min(height,5),height-3];
 return [Math.min(6,height-3),Math.min(5,height-5),Math.min(5,height-7)];
}

type ChipMaterial='emerald'|'ruby'|'champagne';
const metal:Record<ChipMaterial,{rim:string;edge:string;light:string}>={
 emerald:{rim:'#e9cd83',edge:'#1e5b49',light:'#a7f1cc'},
 ruby:{rim:'#f4cd88',edge:'#78283c',light:'#ffc2af'},
 champagne:{rim:'#fff0b6',edge:'#75602e',light:'#fff4c5'},
};
function ChipDisc({material,index,last,stackIndex}:{
 material:ChipMaterial;index:number;last:boolean;stackIndex:number;
}){
 const p=metal[material];
 return <g className="bj-coin-layer" transform={`translate(0 ${-index*7.1})`}
  style={{'--coin-delay':(index*45+stackIndex*80)+'ms'} as CSSProperties}>
  <path d="M-43 -1C-43 -15 43 -15 43 -1L43 10C43 24 -43 24 -43 10Z"
   fill={`url(#bj-chip-side-${material})`} stroke={p.rim} strokeWidth="1.8"/>
  <path d="M-40 9Q0 27 40 9" fill="none" stroke="#1e130e" strokeOpacity=".7" strokeWidth="3"/>
  {[-34,-26,-18,-10,5,14,23,32].map((x,i)=><path key={i}
    d={`M${x} 11l${x>0?1.5:-1.5} 6`} stroke={i%2?p.light:p.rim}
    strokeWidth="3" opacity=".68"/>)}
  <ellipse rx="43" ry="13.8" cy="-2" fill={`url(#bj-chip-face-${material})`}
   stroke={p.rim} strokeWidth="3"/>
  <ellipse rx="36" ry="11.1" cy="-2" fill="none" stroke="#070d0e"
    strokeOpacity=".85" strokeWidth="2.3"/>
  <ellipse rx="32.4" ry="9.8" cy="-2" fill="none" stroke={p.rim}
    strokeOpacity=".92" strokeWidth="1.1" strokeDasharray="3 2.4"/>
  <ellipse rx="26.4" ry="7.8" cy="-2" fill={p.edge} stroke={p.rim}
    strokeOpacity=".7" strokeWidth=".75"/>
  {last&&<g className="bj-chip-face-emblem" fill={p.rim} textAnchor="middle">
    <path d="M0-8l3.5 5-3.5 5-3.5-5Z" fill="none" stroke={p.light}
      strokeWidth=".85" opacity=".85"/>
    <text x="0" y="2.7" fontFamily="Georgia,serif" fontSize="16" fontWeight="bold"
      transform="translate(0 0) scale(1 .62)">{material==='ruby'?'♦':material==='champagne'?'♣':'♠'}</text>
   </g>}
  <path d="M-38 -5Q-24 -14 0 -14" fill="none" stroke="#fff6d8" strokeOpacity=".66"
    strokeWidth="1.3" strokeLinecap="round"/>
 </g>;
}
function Stack({x,y,height,index,material}:{x:number;y:number;height:number;
 index:number;material:ChipMaterial}){
 return <g className={'bj-wager-svg-stack bj-stack-'+index}
  transform={`translate(${x} ${y})`} style={{'--stack-delay':(index*75)+'ms'} as CSSProperties}>
  <ellipse cy="18" rx="49" ry="13" fill="#000000" opacity=".45"/>
  {Array.from({length:height},(_,i)=><ChipDisc key={i} material={material}
    index={i} last={i===height-1} stackIndex={index}/>)}
 </g>;
}
function ChipGradients(){
 return <defs>
  <linearGradient id="bj-chip-side-emerald" x1="0" x2="0" y1="0" y2="1">
   <stop stopColor="#d8a64f"/><stop offset=".26" stopColor="#0e3c2b"/>
   <stop offset=".55" stopColor="#24684d"/><stop offset=".77" stopColor="#12261f"/>
   <stop offset="1" stopColor="#c4964a"/>
  </linearGradient>
  <linearGradient id="bj-chip-side-ruby" x1="0" x2="0" y1="0" y2="1">
   <stop stopColor="#f0d3a1"/><stop offset=".26" stopColor="#85283e"/>
   <stop offset=".55" stopColor="#b04753"/><stop offset=".77" stopColor="#371721"/>
   <stop offset="1" stopColor="#c79b68"/>
  </linearGradient>
  <linearGradient id="bj-chip-side-champagne" x1="0" x2="0" y1="0" y2="1">
   <stop stopColor="#faefc8"/><stop offset=".3" stopColor="#9b713e"/>
   <stop offset=".6" stopColor="#ead49b"/><stop offset=".8" stopColor="#7a5429"/>
   <stop offset="1" stopColor="#fff2bd"/>
  </linearGradient>
  <radialGradient id="bj-chip-face-emerald" cx=".3" cy=".15" r=".95">
   <stop stopColor="#4da887"/><stop offset=".38" stopColor="#115f4b"/>
   <stop offset=".82" stopColor="#042c2b"/><stop offset="1" stopColor="#d4aa66"/>
  </radialGradient>
  <radialGradient id="bj-chip-face-ruby" cx=".3" cy=".15" r=".95">
   <stop stopColor="#e27b75"/><stop offset=".4" stopColor="#933449"/>
   <stop offset=".82" stopColor="#351723"/><stop offset="1" stopColor="#e8ba75"/>
  </radialGradient>
  <radialGradient id="bj-chip-face-champagne" cx=".3" cy=".15" r=".95">
   <stop stopColor="#fff6dc"/><stop offset=".4" stopColor="#d7bb70"/>
   <stop offset=".8" stopColor="#785825"/><stop offset="1" stopColor="#fff2bf"/>
  </radialGradient>
  <radialGradient id="bj-wager-ambient" cx=".5" cy=".5" r=".55">
   <stop stopColor="#ffd890" stopOpacity=".43"/>
   <stop offset=".55" stopColor="#d9af65" stopOpacity=".13"/>
   <stop offset="1" stopColor="#d9af65" stopOpacity="0"/>
  </radialGradient>
  <linearGradient id="bj-front-coin" x1="0" y1="0" x2="1" y2="1">
   <stop stopColor="#ffefad"/><stop offset=".23" stopColor="#97652d"/>
   <stop offset=".52" stopColor="#edc974"/><stop offset=".76" stopColor="#62401e"/>
   <stop offset="1" stopColor="#fce1a0"/>
  </linearGradient>
 </defs>;
}
function FacingCoin(){
 return <g className="bj-wager-facing-coin" transform="translate(213 170) rotate(-19)">
   <ellipse rx="39" ry="36" fill="#000" opacity=".4" transform="translate(4 8)"/>
   <circle r="40" fill="url(#bj-front-coin)" stroke="#fbe5a4" strokeWidth="2"/>
   <circle r="33.5" fill="#123e31" stroke="#182922" strokeWidth="2"/>
   <circle r="29.5" fill="none" stroke="#e8c273" strokeWidth="2.5" strokeDasharray="4 3.1"/>
   <circle r="24.5" fill="#0a312a" stroke="#d7ae63" strokeWidth="1.1"/>
   <path d="M-21 0H21M0-21V21" stroke="#c9a45c" opacity=".26"/>
   <text textAnchor="middle" dominantBaseline="central" fill="#ffedbb"
    fontSize="34" fontFamily="Georgia,serif">♠</text>
   <path d="M-31 -14A34 34 0 0 1 18 -29" stroke="#fff5ca"
    strokeWidth="2.4" fill="none" strokeLinecap="round" opacity=".8"/>
   <circle r="3" cx="0" cy="-36" fill="#fff1c2"/>
 </g>;
}
/** A scalable vector chip sculpture; never conveys bets to the game engine. */
export function TableWagerChips({wager,animationsEnabled}:{
 wager:string;animationsEnabled:boolean;
}){
 const heights=stackHeights(wager);
 if(!heights.length)return null;
 const positions=heights.length===1?[{x:142,y:155}]:
  heights.length===2?[{x:99,y:158},{x:180,y:143}]:
  [{x:80,y:158},{x:157,y:143},{x:229,y:153}];
 const materials:ChipMaterial[]=['ruby','emerald','champagne'];
 const total=heights.reduce((a,b)=>a+b,0);
 return <div className="bj-wager-display bj-wager-premium" aria-hidden="true"
   data-testid="blackjack-table-wager-chips" data-chip-count={total}
   data-animation={animationsEnabled}>
   <svg className="bj-wager-illustration" viewBox="0 0 320 238" role="presentation">
    <ChipGradients/>
    <ellipse cx="163" cy="137" rx="146" ry="92" fill="url(#bj-wager-ambient)"/>
    <ellipse cx="155" cy="196" rx="120" ry="22" fill="#000" opacity=".37"/>
    {heights.map((height,i)=><Stack key={i} x={positions[i].x} y={positions[i].y}
      index={i} height={height} material={materials[i]}/>)}
    <FacingCoin/>
    <path d="M95 216H225" stroke="#b59158" strokeWidth=".8" strokeOpacity=".8"/>
    <text x="160" y="228" textAnchor="middle" fill="#f4d49e" fontSize="10"
      fontFamily="Georgia,serif" letterSpacing="5">WAGER</text>
   </svg>
 </div>;
}
