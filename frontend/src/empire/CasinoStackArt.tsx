import {memo} from 'react';

/** Layered, scalable SVG art. Each chip is a separate disc, never a stretched CSS stripe. */
function Chip({top,variant=0}:{top:boolean;variant?:number}){
  const surface=variant===1?'url(#ce-black-face)':variant===2?'url(#ce-royal-face)':'url(#ce-emerald-face)';
  return <g className="ce-chip-disc">
    <ellipse cx="0" cy="9" rx="61" ry="21" fill="#030d0c" opacity=".85"/>
    <path d="M-61 0 A61 21 0 0 0 61 0 L61 10 A61 21 0 0 1 -61 10Z"
      fill={variant===1?'url(#ce-onyx-edge)':'url(#ce-emerald-edge)'} stroke="#9b7b4b" strokeWidth="1"/>
    <g stroke="#e0c98e" strokeWidth="4.5" opacity=".85">
      <path d="M-59 1l1 8M-42 15l0 7M-22 20l0 8M22 20l0 8M42 15l0 7M59 1l-1 8"/>
    </g>
    <ellipse rx="61" ry="21" fill={surface} stroke="#ebd3a0" strokeWidth="2.2"/>
    <ellipse rx="51" ry="17" fill="none" stroke="#b99355" strokeWidth="3"/>
    <ellipse rx="43" ry="14" fill="none" stroke="#f2d99a" strokeWidth="1.5" opacity=".88"/>
    <g stroke="#e9d4a3" strokeWidth="4" strokeLinecap="round">
      <path d="M-56-7l8 3M-56 8l8-3M56-7l-8 3M56 8l-8-3M-7-19l2 6M7 19l-2-6"/>
    </g>
    {top&&<g>
      <ellipse cy="-1" rx="34" ry="10" fill="#08221f" fillOpacity=".63" stroke="#b4945c" strokeWidth="1.6"/>
      <text x="0" y="7" fontFamily="Georgia,serif" fontSize="23" fill="#f4d897"
        textAnchor="middle" stroke="#20160c" strokeWidth=".4">♠</text>
      <path d="M-26-2q8-9 17-9M26-2q-8-9-17-9" stroke="#e3c57d" strokeWidth="1.4" fill="none" opacity=".8"/>
    </g>}
  </g>;
}
type Column = {key:string;x:number;y:number;count:number;variant:number;scale:number;show:boolean};
function Column({column}:{column:Column}){
  if(!column.show)return null;
  return <g className="ce-chip-column" transform={`translate(${column.x} ${column.y}) scale(${column.scale})`}>
    <ellipse cx="0" cy="17" rx="84" ry="28" fill="#010909" opacity=".65"/>
    {Array.from({length:column.count},(_,i)=><g key={i} transform={`translate(0 ${-i*9.5})`}>
      <Chip top={i===column.count-1} variant={column.variant}/>
    </g>)}
  </g>;
}
function CasinoStackArt({levels,clicks}:{levels:number;clicks:number}){
  const tier=Math.min(4,Math.floor(levels/10));
  const growth=Math.min(8,Math.floor(levels/6)+Math.floor(Math.log10(clicks+1)/2));
  const columns:Column[]=[
    {key:'back',x:461,y:312,count:4+growth,variant:0,scale:.90,show:true},
    {key:'left',x:309,y:358,count:4+growth,variant:1,scale:.94,show:true},
    {key:'right',x:591,y:348,count:3+growth,variant:0,scale:.96,show:tier>=1},
    {key:'hero',x:444,y:381,count:6+growth*2,variant:tier>=3?2:0,scale:1.06,show:true},
    {key:'front',x:374,y:416,count:3+growth,variant:1,scale:.83,show:tier>=2},
  ];
  return <svg className="ce-stack-art" xmlns="http://www.w3.org/2000/svg"
    viewBox="0 0 900 570" preserveAspectRatio="xMidYMid meet" focusable="false" aria-hidden="true">
    <defs>
      <linearGradient id="ce-wood" x1="0" x2=".7" y1="0" y2="1">
        <stop stopColor="#0c0908"/><stop offset=".14" stopColor="#58351f"/>
        <stop offset=".36" stopColor="#b47a3f"/><stop offset=".46" stopColor="#2f1b12"/>
        <stop offset=".73" stopColor="#6a4127"/><stop offset="1" stopColor="#0b0c0b"/>
      </linearGradient>
      <linearGradient id="ce-metal" x1="0" x2=".8" y1="0" y2=".4">
        <stop stopColor="#594125"/><stop offset=".12" stopColor="#f9dc94"/>
        <stop offset=".28" stopColor="#b28242"/><stop offset=".46" stopColor="#ffe2a2"/>
        <stop offset=".7" stopColor="#845d33"/><stop offset=".87" stopColor="#f4c678"/>
        <stop offset="1" stopColor="#30271d"/>
      </linearGradient>
      <linearGradient id="ce-lip" x1="0" x2="0" y1="0" y2="1">
        <stop stopColor="#a97b44"/><stop offset=".22" stopColor="#462613"/>
        <stop offset=".55" stopColor="#1b1412"/><stop offset=".81" stopColor="#724720"/>
        <stop offset="1" stopColor="#d09c52"/>
      </linearGradient>
      <radialGradient id="ce-felt" cx=".43" cy=".3" r=".75">
        <stop stopColor="#23594a"/><stop offset=".48" stopColor="#113d32"/>
        <stop offset=".86" stopColor="#08261e"/><stop offset="1" stopColor="#061b17"/>
      </radialGradient>
      <linearGradient id="ce-emerald-face" x1="0" x2=".9" y1="0" y2="1">
        <stop stopColor="#347b61"/><stop offset=".39" stopColor="#0a302a"/>
        <stop offset=".67" stopColor="#164e41"/><stop offset="1" stopColor="#091a1d"/>
      </linearGradient>
      <linearGradient id="ce-black-face" x1="0" x2=".7" y1="0" y2="1">
        <stop stopColor="#56574e"/><stop offset=".4" stopColor="#161e1d"/>
        <stop offset=".77" stopColor="#263331"/><stop offset="1" stopColor="#0a1516"/>
      </linearGradient>
      <linearGradient id="ce-royal-face" x1="0" x2=".7" y1="0" y2="1">
        <stop stopColor="#b69b55"/><stop offset=".4" stopColor="#3b3724"/>
        <stop offset=".8" stopColor="#5f5b36"/><stop offset="1" stopColor="#181d17"/>
      </linearGradient>
      <linearGradient id="ce-emerald-edge" x1="0" x2="1" y1="0" y2=".35">
        <stop stopColor="#09231f"/><stop offset=".24" stopColor="#d4b87c"/>
        <stop offset=".46" stopColor="#255447"/><stop offset=".65" stopColor="#082c23"/>
        <stop offset=".81" stopColor="#c9a561"/><stop offset="1" stopColor="#0b211a"/>
      </linearGradient>
      <linearGradient id="ce-onyx-edge" x1="0" x2="1" y1="0" y2="0">
        <stop stopColor="#111a1a"/><stop offset=".22" stopColor="#c6ae80"/>
        <stop offset=".49" stopColor="#303e3c"/><stop offset=".72" stopColor="#0b191a"/>
        <stop offset=".91" stopColor="#c5a66e"/><stop offset="1" stopColor="#151e1d"/>
      </linearGradient>
      <radialGradient id="ce-amber-illum"><stop stopColor="#ffe1a2" stopOpacity=".62"/>
        <stop offset=".55" stopColor="#dca14e" stopOpacity=".11"/>
        <stop offset="1" stopColor="#eaa04b" stopOpacity="0"/></radialGradient>
      <filter id="ce-shadow"><feGaussianBlur stdDeviation="18"/></filter>
      <filter id="ce-glow"><feGaussianBlur stdDeviation="5"/></filter>
      <filter id="ce-grain"><feTurbulence type="fractalNoise" baseFrequency=".20" numOctaves="3" seed="2" stitchTiles="stitch"/>
        <feColorMatrix type="saturate" values="0"/>
        <feComponentTransfer><feFuncA type="linear" slope=".11"/></feComponentTransfer></filter>
      <clipPath id="ce-table-clip"><ellipse cx="448" cy="361" rx="348" ry="119"/></clipPath>
    </defs>
    <ellipse cx="453" cy="462" rx="405" ry="72" fill="#000" opacity=".6" filter="url(#ce-shadow)"/>
    <ellipse cx="449" cy="391" rx="404" ry="160" fill="#080d0c" opacity=".86"/>
    <!-- dimensioned dark wood side beneath the metal lip -->
    <path d="M48 365C48 278 220 218 449 218S852 278 852 365V408C852 493 668 525 449 525S48 487 48 408Z"
      fill="url(#ce-lip)" stroke="#0c100e" strokeWidth="9"/>
    <path d="M67 410C156 493 731 514 831 411" fill="none" stroke="#e2a85e" strokeWidth="2.5" opacity=".58"/>
    <path d="M74 428C210 500 681 518 821 429" fill="none" stroke="#07130e" strokeWidth="11" opacity=".7"/>
    <ellipse cx="449" cy="361" rx="402" ry="146" fill="url(#ce-wood)" stroke="#1c1a13" strokeWidth="9"/>
    <ellipse cx="449" cy="357" rx="383" ry="134" fill="none" stroke="url(#ce-metal)" strokeWidth="7"/>
    <ellipse cx="449" cy="357" rx="363" ry="125" fill="#091d18" stroke="#4e3923" strokeWidth="7"/>
    <ellipse cx="448" cy="358" rx="349" ry="119" fill="url(#ce-felt)" stroke="#ad8854" strokeWidth="2"/>
    <g clipPath="url(#ce-table-clip)">
      <rect width="900" height="570" filter="url(#ce-grain)" opacity=".38"/>
      <path d="M174 332Q439 240 728 321" stroke="#d3b578" strokeOpacity=".19" strokeWidth="2" fill="none"/>
      <path d="M172 391Q449 491 729 393" stroke="#e7c68d" strokeOpacity=".13" strokeWidth="2" fill="none"/>
      <ellipse cx="456" cy="330" rx="200" ry="125" fill="url(#ce-amber-illum)" opacity=".4"/>
    </g>
    <ellipse cx="445" cy="403" rx="195" ry="50" fill="#000" opacity=".48" filter="url(#ce-shadow)"/>
    <!-- individual ceramic chips with curved edges and distinct top faces -->
    {columns.map(column=><Column key={column.key} column={column}/>)}
    <!-- side halo dots highlight tangible tabletop -->
    <g fill="#ffe1a1">
      <ellipse cx="112" cy="362" rx="5" ry="3.5"/>
      <ellipse cx="780" cy="351" rx="6" ry="3.4"/>
      <ellipse cx="209" cy="448" rx="3.7" ry="2"/>
      <ellipse cx="680" cy="456" rx="4.4" ry="2.5"/>
    </g>
    <g stroke="#ffe6b2" strokeWidth="2" fill="none" opacity=".7">
      <path d="M112 346v32M98 362h28"/>
      <path d="M780 333v36M761 351h38"/>
    </g>
    <ellipse cx="449" cy="471" rx="202" ry="30" fill="url(#ce-amber-illum)" opacity=".32"/>
  </svg>;
}
export default memo(CasinoStackArt);
