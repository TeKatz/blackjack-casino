import type {CSSProperties} from 'react';
import type {DicePalette} from './bridge';

export function Die({value,palette}:{value:number;palette:DicePalette}){
 const pips:Record<number,number[][]>={
  0:[],1:[[50,50]],2:[[29,29],[71,71]],3:[[29,29],[50,50],[71,71]],
  4:[[29,29],[71,29],[29,71],[71,71]],5:[[29,29],[71,29],[50,50],[29,71],[71,71]],
  6:[[29,25],[71,25],[29,50],[71,50],[29,75],[71,75]],
 };
 return <svg viewBox="0 0 100 100" className="md-die-image" aria-hidden="true">
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
export function DiceCube({value,palette,rolling,index,token}:{
 value:number;palette:DicePalette;rolling:boolean;index:number;token:number;
}){
 const rotations:Record<number,[number,number]>={
  1:[0,0],2:[0,-90],3:[-90,0],4:[90,0],5:[0,90],6:[0,180],
 };
 const [x,y]=rotations[value]??[0,0];
 const style={
  '--die-final-x':x+'deg','--die-final-y':y+'deg',
  '--die-delay':(index*64)+'ms',
  '--die-sideways':(index%2===0?1:-1),
  '--die-front-color':palette.top,
  '--die-side-color':palette.bottom,
  '--die-edge-color':palette.edge,
 } as CSSProperties;
 return <span className={'md-die-stage'+(rolling?' is-rolling':'')} style={style}>
  <span className="md-die-flat-face" data-testid="dice-flat-face" aria-hidden="true">
   <Die value={value} palette={palette}/>
  </span>
  {rolling&&<span className="md-die-roll-3d" data-testid="dice-rolling-3d" aria-hidden="true">
   <span className="md-die-shadow"/>
   <span className="md-die-rotor" key={token}>
    {[1,2,6,5,3,4].map((face,i)=><span key={face}
     className={'md-cube-face md-face-'+['front','right','back','left','top','bottom'][i]}>
     <span className="md-cube-face-material"><Die value={face} palette={palette}/></span>
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
