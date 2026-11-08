import type {CSSProperties} from 'react';
import type {DicePalette} from './bridge';

function Die({value,palette}:{value:number;palette:DicePalette}){
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
      <stop stopColor="#fff" stopOpacity=".42"/><stop offset=".5" stopColor="#fff" stopOpacity="0"/></linearGradient>
  </defs>
  <rect x="6" y="7" width="88" height="88" rx="21" fill={palette.bottom} stroke={palette.edge} strokeWidth="5"/>
  <rect x="8" y="7" width="84" height="80" rx="19" fill={palette.top} stroke="#fff" strokeOpacity=".24" strokeWidth="1.5"/>
  <rect x="10" y="8" width="80" height="82" rx="18" fill="url(#md-die-gloss)"/>
  {pips[value].map(([x,y],i)=><g key={i}>
    {palette.engraved&&<circle cx={x} cy={y+1.5} r="10" fill="#101820" opacity=".35"/>}
    <circle cx={x} cy={y} r="8.9" fill={palette.pips} stroke={palette.edge} strokeWidth=".8"/>
    <circle cx={x-2} cy={y-2} r="2.3" fill="#fff" opacity=".35"/>
  </g>)}
  {value===0&&<text x="50" y="64" textAnchor="middle" fill={palette.pips}
   fontFamily="Georgia" fontSize="43">✦</text>}
 </svg>;
}
/** Six real textured faces rotate in 3D; the authoritative result is never randomized here. */
export function DiceCube({value,palette,rolling,index,token}:{
 value:number;palette:DicePalette;rolling:boolean;index:number;token:number;
}){
 const rotations:Record<number,[number,number]>={
  1:[0,0],2:[0,-90],3:[-90,0],4:[90,0],5:[0,90],6:[0,180],
 };
 const [x,y]=rotations[value]??[0,0];
 const style={
  '--die-final-x':x+'deg','--die-final-y':y+'deg',
  '--die-delay':(index*64)+'ms','--die-sideways':(index%2===0?1:-1),
 } as CSSProperties;
 return <span className={'md-die-stage'+(rolling?' is-rolling':'')} style={style}>
  <span className="md-die-shadow" aria-hidden="true"/>
  <span className="md-die-rotor" key={token} aria-hidden="true">
   {[1,2,6,5,3,4].map((face,i)=><span key={face}
    className={'md-cube-face md-face-'+['front','right','back','left','top','bottom'][i]}>
    <Die value={face} palette={palette}/></span>)}
  </span>
  <span className="md-die-landing" aria-hidden="true"/>
  <span className="md-die-scatter" aria-hidden="true">
   {Array.from({length:6},(_,k)=><i key={k} style={{'--spark':k,'--spark-angle':(k*60)+'deg','--spark-delay':(k*16)+'ms'} as CSSProperties}/>)}
  </span>
 </span>;
}
