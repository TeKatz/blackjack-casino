import type {CSSProperties} from 'react';
import type {DicePalette} from './bridge';
import {diceAppearance} from './diceSkins';

export function Die({value,palette,variant='dice-ivory'}:{value:number;palette:DicePalette;variant?:string}){
 const appearance=diceAppearance(variant,palette);
 const pips:Record<number,number[][]>={
  0:[],1:[[50,50]],2:[[29,29],[71,71]],3:[[29,29],[50,50],[71,71]],
  4:[[29,29],[71,29],[29,71],[71,71]],5:[[29,29],[71,29],[50,50],[29,71],[71,71]],
  6:[[29,25],[71,25],[29,50],[71,50],[29,75],[71,75]],
 };
 return <svg viewBox="0 0 100 100" className="md-die-image" data-skin={variant}
   data-material={appearance.surface} aria-hidden="true">
  <defs><linearGradient id="md-die-grad" x1="0" y1="0" x2=".9" y2="1">
    <stop offset="0" stopColor="#fff" stopOpacity=".28"/><stop offset=".25" stopColor={palette.top}/>
    <stop offset="1" stopColor={palette.bottom}/></linearGradient>
    <linearGradient id="md-die-gloss" x1="0" y1="0" x2="1" y2=".8">
      <stop stopColor="#fff" stopOpacity=".36"/><stop offset=".5" stopColor="#fff" stopOpacity="0"/></linearGradient>
    <linearGradient id="md-die-bevel" x1=".1" y1="0" x2=".95" y2="1">
      <stop stopColor="#fff" stopOpacity=".70"/><stop offset=".18" stopColor={palette.top}/>
      <stop offset=".67" stopColor={palette.bottom}/><stop offset="1" stopColor={palette.edge}/></linearGradient>
    <radialGradient id="md-pip-inner" cx=".30" cy=".25" r=".85">
      <stop stopColor={palette.pips}/><stop offset=".7" stopColor={palette.pips}/>
      <stop offset="1" stopColor={palette.edge}/></radialGradient>
  </defs>
  <rect x="0.5" y="0.5" width="99" height="99" rx="17" fill="url(#md-die-bevel)"
    stroke={palette.edge} strokeWidth="1"/>
  <rect x="6" y="6" width="88" height="88" rx="14" fill={palette.bottom}
    stroke={palette.edge} strokeOpacity=".75" strokeWidth="2"/>
  <rect x="9" y="9" width="82" height="82" rx="12" fill={palette.top}
    stroke="#fff" strokeOpacity=".32" strokeWidth="1.2"/>
  <rect x="9" y="9" width="82" height="82" rx="12" fill="url(#md-die-gloss)"/>
  {appearance.translucent&&<g opacity=".27" stroke="#ffffff" strokeWidth="1.15" fill="none">
    <path d="M11 35L35 10L79 10M12 76L49 11M10 91L82 24L91 50M31 90L90 34"/>
    <path d="M9 62L35 87L88 66" stroke={palette.edge} strokeWidth="2.2"/>
  </g>}
  {appearance.surface==='porcelain'&&<path d="M16 19Q49 4 84 19M16 81Q49 96 84 81"
    fill="none" stroke={palette.pips} strokeWidth="2" opacity=".44"/>}
  {appearance.surface==='resin'&&<g opacity=".26" stroke="#dcebc5" fill="none" strokeWidth="1.4">
    <path d="M15 28Q35 15 48 36T86 47M21 77Q47 53 81 79M20 61Q45 49 60 60"/>
  </g>}
  {appearance.surface==='metal'&&<g opacity=".47" fill="none" stroke={palette.edge} strokeWidth="1.3">
    <path d="M17 19h9M17 19v9M83 19h-9M83 19v9M17 81h9M17 81v-9M83 81h-9M83 81v-9"/>
    <path d="M23 13h54M23 87h54" strokeWidth=".65"/>
  </g>}
  {appearance.surface==='velvet'&&<g opacity=".16" stroke="#fff1e1" strokeWidth=".8">
    {Array.from({length:10},(_,k)=><path key={k} d={`M${14+k*8} 13l-9 75`}/>)}
  </g>}
  {appearance.surface==='opal'&&<g opacity=".27" fill="none">
    <path d="M19 14L56 8L88 36L67 87L18 79Z" stroke="#bbfff5" strokeWidth="2"/>
    <path d="M19 14L67 87M88 36L18 79M56 8L18 79" stroke="#efdbff" strokeWidth="1.8"/>
  </g>}
  {palette.engraved&&<g stroke={palette.edge} strokeWidth="1.2" opacity=".7" fill="none">
    <path d="M17 28V17H28M72 17h11v11M17 72v11h11M83 72v11H72"/>
    <path d="M50 12l3 4-3 4-3-4Z" fill={palette.edge} stroke="none"/>
  </g>}
  <path d="M17 13H78 M13 17V72" fill="none" stroke="#fff" strokeOpacity=".34" strokeWidth="1.4"/>
  {pips[value].map(([x,y],i)=><g key={i}>
    {palette.engraved&&<circle cx={x} cy={y+1.5} r="10" fill="#101820" opacity=".35"/>}
    <circle cx={x} cy={y} r="9.5" fill={palette.edge} opacity=".55"/>
    <circle cx={x} cy={y} r="8.2" fill="url(#md-pip-inner)" stroke={palette.edge} strokeWidth=".65"/>
    <circle cx={x-2.3} cy={y-2.8} r="2.2" fill="#fff" opacity=".36"/>
  </g>)}
  {value===0&&<text x="50" y="64" textAnchor="middle" fill={palette.pips}
   fontFamily="Georgia" fontSize="43">✦</text>}
 </svg>;
}
/**
 * Idle/held/result: one exact, flat SVG face. No cuboid or side faces exist.
 * Rolling: a temporary six-faced object spins, lands face-on, then hands the
 * visual back to the same SVG. All faces/animations are cosmetic; the native
 * game supplies the value, holds and roll token.
 */
export function DiceCube({value,palette,rolling,index,token,variant='dice-ivory'}:{
 value:number;palette:DicePalette;rolling:boolean;index:number;token:number;variant?:string;
}){
 const rotations:Record<number,[number,number]>={
  1:[0,0],2:[0,-90],3:[-90,0],4:[90,0],5:[0,90],6:[0,180],
 };
 const [x,y]=rotations[value]??[0,0];
 const appearance=diceAppearance(variant,palette);
 const style={
  '--die-final-x':x+'deg','--die-final-y':y+'deg',
  '--die-delay':(index*64)+'ms',
  '--die-sideways':(index%2===0?1:-1),
  '--die-front-color':palette.top,
  '--die-side-color':palette.bottom,
  '--die-edge-color':palette.edge,
  '--die-glow-color':appearance.glow,
 } as CSSProperties;
 return <span className={'md-die-stage'+(rolling?' is-rolling':'')} style={style}
  data-skin={variant} data-material={appearance.surface}>
  <span className="md-die-flat-face" data-testid="dice-flat-face" aria-hidden="true">
   <Die value={value} palette={palette} variant={variant}/>
  </span>
  {rolling&&<span className="md-die-roll-3d" data-testid="dice-rolling-3d" aria-hidden="true">
   <span className="md-die-shadow"/>
   <span className="md-die-rotor" key={token}>
    {[1,2,6,5,3,4].map((face,i)=><span key={face}
     className={'md-cube-face md-face-'+['front','right','back','left','top','bottom'][i]}>
     <span className="md-cube-face-material"><Die value={face} palette={palette} variant={variant}/></span>
     <span className="md-cube-bevel-light"/>
    </span>)}
   </span>
   <span className="md-die-landing"/>
   <span className="md-die-scatter">
    {Array.from({length:6},(_,k)=><i key={k} style={{
     '--spark-angle':(k*60)+'deg','--spark-delay':(k*16)+'ms',
    } as CSSProperties}/>)}
   </span>
  </span>}
 </span>;
}
