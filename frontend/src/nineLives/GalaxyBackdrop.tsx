import {memo,useId,useMemo} from 'react';

/**
 * The same moonlit cobalt canvas sits behind every reel symbol. Star positions
 * remain deterministic but no longer cause mismatched colour blocks on the grid.
 * All celestial objects stay in the side gutters.
 */
const SCENES=[
 {name:'Amethyst Rift',base:'#080b20',haze:'#a345b8',glow:'#dd80ff',light:'#ead1ff',
  left:'M0 31 Q45 0 76 28 T145 33 L151 115 Q81 100 31 167 L0 177 Z',
  right:'M420 0 L420 133 Q394 175 345 151 T279 122 Q328 74 347 41 T420 0',
  planet:{x:367,y:134,r:11,kind:'moon'}},
 {name:'Rose Satellite',base:'#120b22',haze:'#c34e96',glow:'#f798d3',light:'#ffdbe9',
  left:'M0 96 Q39 15 84 47 T157 83 L121 158 Q63 121 0 174 Z',
  right:'M420 38 Q387 24 355 65 T280 123 L297 170 Q376 129 420 171 Z',
  planet:{x:56,y:28,r:12,kind:'planet'}},
 {name:'Cobalt Spiral',base:'#070f25',haze:'#2066a6',glow:'#7ac5ff',light:'#c6ecff',
  left:'M0 0 Q69 43 123 12 T150 101 Q101 150 0 121 Z',
  right:'M420 13 Q366 48 340 105 T279 168 L420 180 Z',
  planet:{x:360,y:51,r:24,kind:'spiral'}},
 {name:'Turquoise Dawn',base:'#091221',haze:'#247e83',glow:'#8bebda',light:'#ddfff7',
  left:'M0 66 C49 26 66 128 125 73 L155 148 Q79 171 0 154 Z',
  right:'M420 13 Q380 6 343 60 Q335 112 287 152 L420 164 Z',
  planet:{x:363,y:77,r:22,kind:'crescent'}},
 {name:'Antique Constellation',base:'#18101f',haze:'#aa6946',glow:'#ffc990',light:'#ffebce',
  left:'M0 12 Q78 2 118 67 T157 155 L61 162 L0 123 Z',
  right:'M420 32 Q360 26 347 86 T270 153 L420 178 Z',
  planet:{x:344,y:126,r:20,kind:'spiral'}},
 {name:'Orchid Nebula',base:'#130b25',haze:'#924388',glow:'#ff8ece',light:'#ffe2fa',
  left:'M0 19 Q68 49 101 10 T149 96 L135 159 L0 151 Z',
  right:'M420 37 Q373 0 354 58 T280 126 Q338 172 420 159 Z',
  planet:{x:370,y:37,r:20,kind:'planet'}},
 {name:'Sapphire Rivers',base:'#081125',haze:'#365cb9',glow:'#91b9ff',light:'#e5f1ff',
  left:'M0 14 Q56 62 127 0 T143 96 L83 143 L0 157 Z',
  right:'M420 31 Q365 80 350 54 T270 143 L420 171 Z',
  planet:{x:368,y:145,r:14,kind:'moon'}},
 {name:'Amber Eclipse',base:'#160d19',haze:'#a8633b',glow:'#ffc278',light:'#fff1d4',
  left:'M0 40 Q54 8 96 58 T166 134 L100 177 L0 154 Z',
  right:'M420 0 Q373 58 319 43 T290 160 L420 142 Z',
  planet:{x:364,y:35,r:14,kind:'crescent'}},
 {name:'Violet Ringworld',base:'#110a20',haze:'#884cbd',glow:'#d0a1ff',light:'#f1dfff',
  left:'M0 11 Q61 14 107 83 T150 162 L0 180 Z',
  right:'M420 3 Q382 48 345 33 T278 124 Q345 178 420 152 Z',
  planet:{x:367,y:71,r:17,kind:'ring'}},
] as const;

type Scene=(typeof SCENES)[number];
const UNIFIED_REEL_BASE='#080f25';
const UNIFIED_REEL_HAZE='#4d76a0';
const UNIFIED_REEL_GLOW='#8ab9dc';
type Star={x:number;y:number;r:number;brightness:number;spark:boolean;delay:number;duration:number};

function starsFor(cell:number,compact:boolean):Star[]{
 let value=(0x91c08b5f ^ Math.imul(cell+1,0x9e3779b1))>>>0;
 const next=()=>{
   value^=value<<13;value^=value>>>17;value^=value<<5;
   return (value>>>0)/4294967296;
 };
 return Array.from({length:compact?11:33},(_,index)=>{
   // Side-gutter positions deliberately exclude the central symbol footprint.
   const x=next()<.5?9+next()*112:298+next()*112;
   return {x,y:9+next()*162,r:.35+next()*(index%7===0?1.5:.85),
     brightness:.25+next()*.48,spark:index%9===0,
     delay:-next()*13,duration:7+next()*8};
 });
}

/** Useful in tests: different cells are guaranteed to use independent star maps. */
export function galaxySignature(cell:number):string{
 const scene=SCENES[((cell%SCENES.length)+SCENES.length)%SCENES.length];
 const points=starsFor(cell,false).map(s=>s.x.toFixed(3)+','+s.y.toFixed(3));
 return scene.name+'|'+points.join('|');
}

function Planet({scene,id}:{scene:Scene;id:string}){
 const {x,y,r,kind}=scene.planet;
 if(kind==='spiral')return <g className="nine-galaxy-planet" opacity=".83">
   <ellipse cx={x} cy={y} rx={r*1.36} ry={r*.46} stroke={scene.glow} strokeWidth="2.1"
     fill="none" transform={`rotate(-23 ${x} ${y})`}/>
   <ellipse cx={x} cy={y} rx={r*.83} ry={r*.27} stroke={scene.light} strokeWidth="1.4"
     fill="none" transform={`rotate(-23 ${x} ${y})`}/>
   <ellipse cx={x} cy={y} rx={r*.25} ry={r*.14} fill={scene.light} opacity=".78"/>
 </g>;
 if(kind==='crescent')return <g className="nine-galaxy-planet">
   <circle cx={x} cy={y} r={r} fill={scene.light} opacity=".86"/>
   <circle cx={x+r*.47} cy={y-r*.26} r={r*.92} fill={scene.base}/>
 </g>;
 return <g className="nine-galaxy-planet">
   <circle cx={x} cy={y} r={r} fill={`url(#${id}-planet)`} opacity=".88"/>
   {kind==='ring'&&<ellipse cx={x} cy={y} rx={r*1.6} ry={r*.35}
     transform={`rotate(-24 ${x} ${y})`} stroke={scene.light}
     strokeWidth="2.2" fill="none" opacity=".68"/>}
   {kind==='moon'&&<circle cx={x-r*.24} cy={y+r*.13} r={r*.18}
     fill={scene.base} opacity=".48"/>}
 </g>;
}

function GalaxyBackdropView({cell,compact=false}:{cell:number;compact?:boolean}){
 const id=useId().replace(/:/g,'');
 const scene=SCENES[((cell%SCENES.length)+SCENES.length)%SCENES.length];
 const stars=useMemo(()=>starsFor(cell,compact),[cell,compact]);
 return <svg className={'nine-galaxy-scene'+(compact?' nine-galaxy-compact':'')}
   data-galaxy-cell={cell} data-galaxy-name={scene.name}
   viewBox="0 0 420 180" preserveAspectRatio="none" aria-hidden="true" focusable="false">
   <defs>
     <radialGradient id={id+'-haze'} cx="50%" cy="49%" r="75%">
       <stop offset="0%" stopColor={UNIFIED_REEL_HAZE} stopOpacity=".46"/>
       <stop offset="55%" stopColor={UNIFIED_REEL_HAZE} stopOpacity=".17"/>
       <stop offset="100%" stopColor={UNIFIED_REEL_HAZE} stopOpacity="0"/>
     </radialGradient>
     <radialGradient id={id+'-planet'} cx="30%" cy="25%" r="75%">
       <stop offset="0%" stopColor={scene.light}/>
       <stop offset="36%" stopColor={scene.glow}/>
       <stop offset="100%" stopColor={scene.base}/>
     </radialGradient>
   </defs>
   <rect width="420" height="180" fill={UNIFIED_REEL_BASE}/>
   <g className="nine-galaxy-clouds" opacity=".83">
     <path d={scene.left} fill={`url(#${id}-haze)`}/>
     <path d={scene.right} fill={`url(#${id}-haze)`}/>
     <path d={scene.left} stroke={UNIFIED_REEL_GLOW} strokeWidth=".65" fill="none" opacity=".20"/>
   </g>
   <g className="nine-galaxy-stars">
     {stars.map((star,index)=><g key={index} className="nine-galaxy-star"
       style={{animationDelay:star.delay+'s',animationDuration:star.duration+'s'}}
       opacity={star.brightness}>
       <circle cx={star.x} cy={star.y} r={star.r}
         fill={index%3===0?scene.light:'#f3f1ff'}/>
       {star.spark&&<path d={`M${star.x-4} ${star.y}h8 M${star.x} ${star.y-4}v8`}
         stroke={scene.light} strokeWidth=".7" opacity=".67"/>}
     </g>)}
   </g>
   {!compact&&<Planet scene={scene} id={id}/>}
 </svg>;
}
export const GalaxyBackdrop=memo(GalaxyBackdropView);
