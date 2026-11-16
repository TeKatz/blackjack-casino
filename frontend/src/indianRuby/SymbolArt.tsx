import {memo,useId} from 'react';
import type {SymbolId} from './bridge';

/** Original scalable cabinet artwork. Shared between the settled reels and motion tapes. */
function RubySymbolArtView({symbol}:{symbol:SymbolId}){
 const key=useId().replaceAll(':','');
 const gold=key+'g',shade=key+'s',gem=key+'r',ivory=key+'i';
 const letter:{[K in SymbolId]?:string}={Ace:'A',King:'K',Queen:'Q',Jack:'J',Ten:'10'};
 return <svg className={'ruby-symbol-art ruby-symbol-'+symbol.toLowerCase()}
   viewBox="0 0 120 120" role="presentation" aria-hidden="true" focusable="false">
   <defs>
    <linearGradient id={gold} x1="0" x2="1" y1="0" y2="1">
     <stop offset="0" stopColor="#fff6c3"/><stop offset=".24" stopColor="#fbd779"/>
     <stop offset=".46" stopColor="#ba7334"/><stop offset=".68" stopColor="#fff0a0"/>
     <stop offset="1" stopColor="#825023"/>
    </linearGradient>
    <linearGradient id={shade} x1=".1" x2=".9" y1="0" y2="1">
     <stop offset="0" stopColor="#c9d7d5"/><stop offset=".45" stopColor="#738893"/>
     <stop offset="1" stopColor="#29394c"/>
    </linearGradient>
    <linearGradient id={gem} x1=".15" x2=".8" y1=".05" y2=".9">
     <stop offset="0" stopColor="#ff9a96"/><stop offset=".25" stopColor="#f02855"/>
     <stop offset=".57" stopColor="#990624"/><stop offset="1" stopColor="#4f0316"/>
    </linearGradient>
    <linearGradient id={ivory} x1="0" x2="1" y1="0" y2="1">
     <stop stopColor="#ffffe8"/><stop offset="1" stopColor="#d6b68b"/>
    </linearGradient>
   </defs>
   {symbol==='Elephant'&&<>
     <circle cx="60" cy="57" r="51" fill="#371f25" opacity=".14"/>
     <path d="M14 48Q4 30 16 19Q36 12 48 35L41 85Q16 83 14 48Z" fill={`url(#${shade})`} stroke="#6f543e" strokeWidth="2"/>
     <path d="M106 48Q116 30 104 19Q84 12 72 35L79 85Q104 83 106 48Z" fill={`url(#${shade})`} stroke="#6f543e" strokeWidth="2"/>
     <path d="M19 39Q18 27 28 28Q40 34 38 65Q23 65 19 39Z" fill="#d08c80" opacity=".75"/>
     <path d="M101 39Q102 27 92 28Q80 34 82 65Q97 65 101 39Z" fill="#d08c80" opacity=".75"/>
     <path d="M36 34Q40 14 60 14Q83 14 86 40L80 79Q76 88 68 88L68 101Q69 110 80 102Q86 97 81 93Q92 97 89 106Q86 118 70 115Q54 112 55 95L51 87Q37 83 34 71Z"
        fill={`url(#${shade})`} stroke="#485269" strokeWidth="2.5"/>
     <path d="M47 79Q42 96 31 96Q38 87 39 73" fill={`url(#${ivory})`} stroke="#aa8e62" strokeWidth="1.5"/>
     <path d="M75 79Q81 96 90 93Q82 85 82 71" fill={`url(#${ivory})`} stroke="#aa8e62" strokeWidth="1.5"/>
     <ellipse cx="45" cy="61" rx="4.4" ry="3.5" fill="#101e27"/><ellipse cx="75" cy="61" rx="4.4" ry="3.5" fill="#101e27"/>
     <circle cx="46.5" cy="60" r="1.2" fill="#fff"/>
     <circle cx="76.5" cy="60" r="1.2" fill="#fff"/>
     <path d="M39 37Q61 29 81 37L80 48Q59 44 41 49Z" fill={`url(#${gold})`} stroke="#653916" strokeWidth="2"/>
     <path d="M49 31L60 9L71 31L60 39Z" fill={`url(#${gold})`} stroke="#85461f" strokeWidth="1.7"/>
     <path d="M60 23L66 31L60 38L54 31Z" fill={`url(#${gem})`} stroke="#f8d08c" strokeWidth="1.2"/>
     <circle cx="46" cy="42" r="3" fill="#fff2af"/><circle cx="60" cy="45" r="3" fill="#fff2af"/><circle cx="74" cy="42" r="3" fill="#fff2af"/>
     <path d="M59 53L60 95" stroke="#d7e0d5" strokeWidth="1.5" opacity=".45" fill="none"/>
    </>}
   {symbol==='Ruby'&&<>
     <circle cx="60" cy="60" r="48" fill="#fbc56a" opacity=".12"/>
     <path d="M60 5L64 22L60 17L56 22ZM60 115L64 99L60 103L56 99ZM5 60L22 64L17 60L22 56ZM115 60L98 64L103 60L98 56Z"
       fill={`url(#${gold})`}/>
     <path d="M60 12L77 25L94 41L99 64L60 108L20 64L26 40L44 25Z"
       fill={`url(#${gem})`} stroke={`url(#${gold})`} strokeWidth="3.5" strokeLinejoin="round"/>
     <path d="M26 40L44 25L77 25L94 41L78 48L44 48Z" fill="#ffb9ad" opacity=".8"/>
     <path d="M20 64L44 48L78 48L99 64L60 108Z" fill="#bd073b" opacity=".6"/>
     <path d="M44 48L60 108L78 48" fill="#ff6580" opacity=".68"/>
     <path d="M26 40L44 48L60 108L20 64ZM94 41L78 48L60 108L99 64Z" fill="#64051f" opacity=".48"/>
     <path d="M60 30L72 37L60 44L47 36Z" fill="#fff0e6" opacity=".65"/>
     <path d="M34 33L41 38" stroke="#fff9da" strokeWidth="2" strokeLinecap="round"/>
    </>}
   {symbol==='Necklace'&&<>
     <circle cx="60" cy="61" r="48" fill="#44250f" opacity=".13"/>
     <path d="M15 28Q19 72 57 84Q102 69 105 26" fill="none" stroke="#7a451b" strokeWidth="10" strokeLinecap="round"/>
     <path d="M15 28Q19 72 57 84Q102 69 105 26" fill="none" stroke={`url(#${gold})`} strokeWidth="7" strokeLinecap="round" strokeDasharray="2 8"/>
     <path d="M24 30Q28 71 60 79Q94 70 97 29" fill="none" stroke={`url(#${gold})`} strokeWidth="3"/>
     {Array.from({length:9},(_,i)=>{
      const theta=(.12+i*.095)*Math.PI;
      return <circle key={i} cx={60-45*Math.cos(theta)} cy={25+49*Math.sin(theta)}
        r={i===4?5:3.3} fill="#f2e1a6" stroke="#915b29" strokeWidth="1"/>;
     })}
     <path d="M60 67L75 86L60 110L45 86Z" fill={`url(#${gem})`} stroke={`url(#${gold})`} strokeWidth="4"/>
     <path d="M60 70L60 103L49 85Z" fill="#ffc9b6" opacity=".54"/>
     <path d="M15 25L21 13L27 25L21 35ZM93 25L99 13L105 25L99 35Z" fill={`url(#${gold})`}/>
    </>}
   {symbol==='Vase'&&<>
     <ellipse cx="60" cy="100" rx="37" ry="8" fill="#462518" opacity=".25"/>
     <path d="M36 20L83 20L76 32Q89 47 86 69Q83 91 72 98L47 98Q29 85 33 62Q34 42 43 32Z"
       fill={`url(#${gold})`} stroke="#8b461d" strokeWidth="3"/>
     <path d="M39 44Q59 31 81 44L83 72Q61 86 36 71Z" fill="#7b3e3a" stroke="#c6a05d" strokeWidth="2"/>
     <path d="M39 56Q59 43 81 56M37 66Q60 81 83 66" fill="none" stroke="#f6c877" strokeWidth="2" opacity=".8"/>
     <path d="M46 51Q60 40 74 52Q73 69 60 77Q47 67 46 51Z" fill="#2a766c" stroke="#ffda84" strokeWidth="2"/>
     <path d="M60 47L65 59L60 68L55 59Z" fill="#c4ead4"/>
     <path d="M36 27Q25 27 26 45Q26 55 34 60M83 27Q94 27 94 45Q93 54 86 60"
       stroke={`url(#${gold})`} strokeWidth="5" fill="none"/>
     <path d="M39 20L81 20" stroke="#fff2b0" strokeWidth="4" strokeLinecap="round"/>
    </>}
   {symbol==='Spices'&&<>
     <circle cx="60" cy="60" r="46" fill="#4b2512" opacity=".11"/>
     <path d="M60 9Q74 35 86 18Q87 44 110 45Q84 59 104 80Q76 77 72 108Q59 87 42 105Q43 79 16 79Q37 61 10 45Q39 43 39 17Q52 37 60 9Z"
       fill={`url(#${gold})`} stroke="#8b4b20" strokeWidth="2"/>
     <path d="M60 27Q80 40 89 58Q80 82 60 95Q39 80 31 58Q41 39 60 27Z"
       fill="#b65034" stroke="#f1c377" strokeWidth="3"/>
     <path d="M60 35L69 52L88 57L70 69L60 86L50 69L33 57L51 52Z" fill="#f5bc50" stroke="#ffdca2" strokeWidth="2"/>
     <circle cx="60" cy="58" r="9" fill="#8d2630" stroke="#ffe39c" strokeWidth="3"/>
     <path d="M56 55Q61 50 65 55" stroke="#ffe7b3" strokeWidth="2" fill="none"/>
    </>}
   {letter[symbol]&&<>
     <circle cx="60" cy="60" r="48" fill="#743e21" opacity=".09"/>
     <path d="M60 10L67 20L60 28L53 20Z M60 110L67 100L60 92L53 100Z"
       fill={`url(#${gold})`}/>
     <path d="M20 55Q25 31 41 26M100 55Q95 31 79 26M20 65Q25 89 41 94M100 65Q95 89 79 94"
       stroke={`url(#${gold})`} strokeWidth="2.5" fill="none"/>
     <text x="60" y="81" textAnchor="middle" fontFamily="Georgia,serif"
       fontSize={symbol==='Ten'?64:78} fontWeight="bold" letterSpacing="-5"
       fill={symbol==='Ace'||symbol==='King'?`url(#${gem})`:'#38565d'}
       stroke="#ffe6aa" strokeWidth="1.2" paintOrder="stroke">{letter[symbol]}</text>
     <path d="M37 29L41 25M83 29L79 25" stroke="#fff4cb" strokeWidth="2" strokeLinecap="round"/>
    </>}
   {symbol==='Empty'&&<circle cx="60" cy="60" r="14" fill={`url(#${gold})`} opacity=".17"/>}
 </svg>;
}
export const RubySymbolArt=memo(RubySymbolArtView);
