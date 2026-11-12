import type {ReactNode} from 'react';

/** Source-authored scalable card art. Neither emoji fonts nor network image requests
 * affect the identity of a saved pair; the C# symbol name remains authoritative. */
export function MemoryIllustration({deck,name,accent}:{deck:'animals'|'fruits';name:string;accent:string}){
 const animal=deck==='animals';
 const is=(...names:string[])=>names.includes(name);
 const shadow='#16262a',cream='#fff0d7';
 const eyes=<><ellipse cx="40" cy="52" rx="3.2" ry="4.2" fill={shadow}/><ellipse cx="61" cy="52" rx="3.2" ry="4.2" fill={shadow}/></>;
 const animalFace=()=>{
  const long=is('Rabbit'),pointy=is('Cat','Fox','Tiger','Lion','Squirrel'),round=is('Bear','Panda','Koala','Monkey','Pig','Cow');
  return <g>
   {long?<><ellipse cx="32" cy="24" rx="10" ry="23" fill={accent} stroke={shadow} strokeWidth="2"/><ellipse cx="68" cy="24" rx="10" ry="23" fill={accent} stroke={shadow} strokeWidth="2"/><path d="M32 11v24M68 11v24" stroke="#fbd7d3" strokeWidth="4"/></>:
   <g fill={accent} stroke={shadow} strokeWidth="2.2">{pointy?<path d="M20 44L21 18L43 31M58 31L79 18L81 44"/>:
   round?<><circle cx="25" cy="34" r="15"/><circle cx="75" cy="34" r="15"/></>:
   <><ellipse cx="23" cy="34" rx="11" ry="16"/><ellipse cx="77" cy="34" rx="11" ry="16"/></>}</g>}
   <ellipse cx="50" cy="56" rx="34" ry="29" fill={accent} stroke={shadow} strokeWidth="2.5"/>
   {is('Panda')&&<><ellipse cx="38" cy="51" rx="10" ry="14" fill="#3a4244"/><ellipse cx="62" cy="51" rx="10" ry="14" fill="#3a4244"/></>}
   {is('Tiger','Cat','Fox')&&<path d="M50 28v17M30 35l7 8M70 35l-7 8M23 55l12-1M77 55l-12-1" stroke="#533f35" strokeWidth="4" strokeLinecap="round"/>}
   {is('Cow')&&<><ellipse cx="30" cy="44" rx="12" ry="8" fill="#51605b"/><ellipse cx="70" cy="42" rx="8" ry="13" fill="#51605b"/></>}
   {eyes}
   <ellipse cx="50" cy="65" rx="16" ry="12" fill={is('Pig')?'#eab2ae':cream} opacity=".94"/>
   {is('Pig')?<><ellipse cx="44" cy="63" rx="2" ry="4" fill="#985a66"/><ellipse cx="56" cy="63" rx="2" ry="4" fill="#985a66"/></>:
    <><path d="M45 63l5 4 5-4Z" fill={shadow}/><path d="M50 67v3m0 0q-5 5-9 0m9 0q5 5 9 0" stroke={shadow} strokeWidth="2" fill="none" strokeLinecap="round"/></>}
   {is('Lion')&&<ellipse cx="50" cy="55" rx="42" ry="41" stroke="#c48a42" strokeWidth="13" fill="none" strokeDasharray="6 4"/>}
   {is('Dog')&&<path d="M23 34Q4 40 22 65M77 34Q95 40 78 65" stroke="#785f52" strokeWidth="12" fill="none" strokeLinecap="round"/>}
  </g>;
 };
 const animalArt=():ReactNode=>{
  if(is('Cat','Dog','Rabbit','Fox','Bear','Panda','Koala','Tiger','Lion','Cow','Pig','Monkey','Squirrel'))return animalFace();
  if(is('Chicken','Penguin','Songbird','Owl','Duck')){
   return <g><ellipse cx="50" cy="58" rx="30" ry="35" fill={accent} stroke={shadow} strokeWidth="2.5"/>
    {is('Owl')&&<path d="M19 40L28 18L44 33M56 33L72 18L81 40" fill={accent} stroke={shadow} strokeWidth="2"/>}
    {is('Penguin')&&<ellipse cx="50" cy="66" rx="21" ry="25" fill={cream}/>}
    <ellipse cx="39" cy="49" rx={is('Owl')?11:4} ry={is('Owl')?13:5} fill={cream}/><ellipse cx="61" cy="49" rx={is('Owl')?11:4} ry={is('Owl')?13:5} fill={cream}/>
    <circle cx="39" cy="50" r="3" fill={shadow}/><circle cx="61" cy="50" r="3" fill={shadow}/>
    <path d="M44 59L56 59L50 69Z" fill="#e9ac65" stroke={shadow} strokeWidth="1.3"/>
    {is('Duck')&&<path d="M41 62Q61 70 72 60L55 75Z" fill="#eac188"/>}
    <path d="M22 63Q8 50 16 39M78 63Q92 50 84 39" stroke={accent} strokeWidth="8" strokeLinecap="round"/></g>;
  }
  if(is('Dolphin','Whale','Shark','Tropical Fish')){
   return <g><path d="M14 56Q46 19 79 46L91 32L87 57L95 82L75 66Q46 90 14 56Z" fill={accent} stroke={shadow} strokeWidth="2.5"/><path d="M48 36L54 22L67 40" fill={accent} stroke={shadow} strokeWidth="2"/><circle cx="29" cy="50" r="3.5" fill={shadow}/><path d="M17 61q14 7 28 1" stroke={cream} strokeWidth="3" fill="none"/>{is('Shark')&&<path d="M20 65l7 5 7-6 7 5" fill="none" stroke={cream} strokeWidth="3"/>}</g>;
  }
  if(is('Octopus','Crab','Snail')){
   return <g>{[0,1,2,3].map(i=><path key={i} d={`M${25+i*16} 68q-25 28 ${i%2?-4:17} 14`} fill="none" stroke={accent} strokeWidth="9" strokeLinecap="round"/>)}<ellipse cx="50" cy="46" rx="29" ry="29" fill={accent} stroke={shadow} strokeWidth="2"/>{eyes}{is('Crab')&&<><path d="M21 54L7 32M79 54L93 32" stroke={accent} strokeWidth="6"/><circle cx="6" cy="32" r="11" fill={accent} stroke={shadow}/><circle cx="94" cy="32" r="11" fill={accent} stroke={shadow}/></>}</g>;
  }
  if(is('Bee','Butterfly','Ladybird')){
   return <g>{is('Butterfly')?<><ellipse cx="27" cy="39" rx="22" ry="24" fill={accent} stroke={shadow}/><ellipse cx="73" cy="39" rx="22" ry="24" fill={accent} stroke={shadow}/><ellipse cx="27" cy="67" rx="17" ry="21" fill={accent} stroke={shadow}/><ellipse cx="73" cy="67" rx="17" ry="21" fill={accent} stroke={shadow}/></>:
    <><ellipse cx="30" cy="33" rx="19" ry="17" fill={cream} opacity=".85"/><ellipse cx="70" cy="33" rx="19" ry="17" fill={cream} opacity=".85"/></>}
    <ellipse cx="50" cy="54" rx="18" ry="31" fill={accent} stroke={shadow} strokeWidth="2.5"/>
    <path d="M34 48h32M34 62h32" stroke={is('Bee')?shadow:cream} strokeWidth="6"/><circle cx="44" cy="38" r="2.3" fill={shadow}/><circle cx="56" cy="38" r="2.3" fill={shadow}/></g>;
  }
  if(is('Snake','Lizard','Turtle','Frog')){
   return <g>{is('Snake')?<path d="M30 72Q5 25 47 33T67 72Q57 96 40 76Q30 54 65 48" fill="none" stroke={accent} strokeWidth="18" strokeLinecap="round"/>:
   is('Turtle')?<><ellipse cx="50" cy="59" rx="35" ry="25" fill="#466e53" stroke={shadow} strokeWidth="3"/><path d="M25 53L50 37L76 53L66 76H34Z" fill={accent} stroke={cream} strokeWidth="3"/></>:
   <><ellipse cx="50" cy="65" rx="35" ry="19" fill={accent}/><path d="M25 55Q4 77 14 88M77 55Q98 77 86 88" stroke={accent} strokeWidth="7" fill="none"/></>}
   <circle cx="35" cy="44" r="10" fill={accent} stroke={shadow} strokeWidth="2"/><circle cx="65" cy="44" r="10" fill={accent} stroke={shadow} strokeWidth="2"/>
   <circle cx="35" cy="44" r="3" fill={shadow}/><circle cx="65" cy="44" r="3" fill={shadow}/></g>;
  }
  return <g><ellipse cx="50" cy="58" rx="30" ry="28" fill={accent}/>{eyes}</g>;
 };
 const fruitArt=():ReactNode=>{
  const baked=is('Croissant','Pretzel','Toast','Cupcake','Cake','Cookie','Chocolate','Candy','Coffee','Tea','Milkshake','Honey','Peanuts','Popcorn','Cheese');
  if(baked){
   if(is('Coffee','Tea','Milkshake'))return <g><path d="M25 35H75L68 84H32Z" fill={cream} stroke={shadow} strokeWidth="3"/><path d="M75 43Q99 38 91 61Q85 74 72 68" fill="none" stroke={cream} strokeWidth="7"/><ellipse cx="50" cy="34" rx="25" ry="7" fill={accent}/><path d="M38 21q-8-10 0-17M55 22q-7-9 1-18" stroke={cream} strokeWidth="3" fill="none" opacity=".7"/></g>;
   if(is('Cupcake','Cake','Popcorn'))return <g><path d="M19 44H81L71 88H29Z" fill="#eadab9" stroke={shadow} strokeWidth="3"/><path d="M20 45Q14 22 32 23Q40 7 52 24Q73 9 83 30L80 45Z" fill={accent} stroke={shadow} strokeWidth="2"/><circle cx="52" cy="18" r="6" fill="#cd7267"/><path d="M37 50l4 32m13-32v34m13-34l-4 32" stroke="#c29c76" strokeWidth="3" opacity=".6"/></g>;
   if(is('Croissant','Pretzel','Toast','Cookie','Peanuts'))return <g><path d="M14 61Q15 22 50 26Q85 21 87 59Q68 92 40 86Q19 87 14 61Z" fill={accent} stroke={shadow} strokeWidth="3"/><path d="M27 34q9 19 0 44M50 25q-8 28 2 61M71 30q-14 24 3 48" stroke={cream} strokeWidth="5" opacity=".5" fill="none"/>{is('Cookie')&&[0,1,2,3,4].map((_,i)=><circle key={i} cx={31+i%3*16} cy={42+Math.floor(i/3)*20} r="5" fill="#624c47"/>)}</g>;
   if(is('Cheese'))return <g><path d="M18 76L86 25L86 78Z" fill={accent} stroke={shadow} strokeWidth="3"/><circle cx="65" cy="59" r="8" fill="#cfaa60"/><circle cx="77" cy="39" r="5" fill="#cfaa60"/><circle cx="48" cy="73" r="6" fill="#cfaa60"/></g>;
   if(is('Honey'))return <g><path d="M31 27h38l6 17-5 41H30l-5-41Z" fill="#d1a456" stroke={shadow} strokeWidth="3"/><rect x="26" y="19" width="48" height="13" rx="4" fill="#e4cca2" stroke={shadow}/><path d="M42 48q15-12 24 0l-9 17-8-7Z" fill="#fff1b9"/></g>;
   return <g><rect x="17" y="25" width="66" height="59" rx="10" fill={accent} stroke={shadow} strokeWidth="3"/><path d="M23 40h53M23 58h53" stroke={cream} opacity=".7" strokeWidth="5"/></g>;
  }
  if(is('Banana'))return <g><path d="M17 36Q45 83 91 29Q86 77 46 83Q24 79 17 36Z" fill={accent} stroke={shadow} strokeWidth="3"/><path d="M80 35l10-11" stroke={shadow} strokeWidth="4"/></g>;
  if(is('Grapes','Blueberries','Cherries'))return <g>{[[36,39],[58,38],[24,57],[47,58],[70,57],[36,77],[58,77]].map(([x,y],i)=><circle key={i} cx={x} cy={y} r={is('Blueberries')?11:13} fill={accent} stroke={shadow} strokeWidth="2"/>)}<path d="M47 30Q38 10 65 13" stroke="#5c896a" strokeWidth="5" fill="none"/></g>;
  if(is('Watermelon','Kiwi','Coconut'))return <g><path d="M15 68A42 42 0 0 1 85 68Z" fill={accent} stroke={shadow} strokeWidth="3"/><path d="M20 68Q50 89 80 68" stroke="#72a47a" strokeWidth="8" fill="none"/>{[30,45,60,75].map((x,i)=><ellipse key={i} cx={x} cy={53+(i%2)*9} rx="3" ry="5" fill={shadow}/>)}</g>;
  if(is('Pineapple'))return <g><path d="M33 36L50 16L67 36M50 18V5M40 26L25 16M60 26L75 16" stroke="#57876b" strokeWidth="6" strokeLinecap="round"/><ellipse cx="50" cy="63" rx="24" ry="33" fill={accent} stroke={shadow} strokeWidth="3"/><path d="M31 39l37 44M70 41L33 83" stroke={cream} strokeWidth="4" opacity=".6"/></g>;
  if(is('Strawberry'))return <g><path d="M24 39Q23 17 50 29Q77 17 76 39Q71 76 50 90Q28 74 24 39Z" fill={accent} stroke={shadow} strokeWidth="3"/><path d="M26 31L45 19L50 27L58 18L74 31" fill="#6f9b6b" stroke={shadow} strokeWidth="2"/>{[0,1,2,3,4,5,6].map(i=><ellipse key={i} cx={35+i%3*14} cy={44+Math.floor(i/3)*13} rx="2" ry="4" fill={cream}/>)}</g>;
  if(is('Pear'))return <g><path d="M50 17Q69 35 64 45Q90 68 69 82Q50 97 31 82Q10 68 36 45Q31 35 50 17Z" fill={accent} stroke={shadow} strokeWidth="3"/><path d="M50 20Q54 7 67 9" stroke="#5d8b60" strokeWidth="5" fill="none"/></g>;
  return <g><ellipse cx="50" cy="57" rx={is('Lemon')?38:30} ry={is('Lemon')?22:31} fill={accent} stroke={shadow} strokeWidth="3"/><path d="M50 28Q54 10 68 16" stroke="#5e8a66" strokeWidth="5" fill="none"/><path d="M30 42q9-12 20-11" stroke={cream} strokeWidth="5" opacity=".52" fill="none"/>{is('Apple','Tomato')&&<path d="M41 30q8-13 19 0" stroke={cream} strokeWidth="4" fill="none"/>}</g>;
 };
 return <svg className="memo-illustration" viewBox="0 0 100 100" role="img" aria-label={name}>
  <defs><radialGradient id="memory-glaze" cx=".3" cy=".2"><stop stopColor="#fff9e5"/><stop offset="1" stopColor="#f4e9d0" stopOpacity=".12"/></radialGradient></defs>
  <circle cx="50" cy="52" r="46" fill="url(#memory-glaze)" opacity=".45"/>
  {animal?animalArt():fruitArt()}
  <path d="M22 86Q50 98 78 86" stroke={cream} strokeWidth="2.5" fill="none" opacity=".45"/>
 </svg>;
}
