import type {CSSProperties} from 'react';
import type {CardFace} from './bridge';
import type {CardSkin} from './cardSkins';

const ranks:Record<string,string>={Ace:'A',Jack:'J',Queen:'Q',King:'K',Two:'2',Three:'3',
 Four:'4',Five:'5',Six:'6',Seven:'7',Eight:'8',Nine:'9',Ten:'10'};
const suits:Record<string,string>={Clubs:'♣',Diamonds:'♦',Hearts:'♥',Spades:'♠'};

/** Purely decorative engraved linework, placed inside a fixed geometric card box.
 * Neither the emblem nor the central pip participates in grid layout or hit testing.
 */
function CollectorEngraving({variant}:{variant:string}){
 const solar=variant==='card-solar-eclipse';
 if(solar)return <svg className="bj-card-engraving bj-card-engraving-solar" viewBox="0 0 100 148"
   preserveAspectRatio="none" aria-hidden="true" focusable="false">
   <g fill="none" stroke="currentColor" strokeWidth="1.05">
    <rect x="7" y="7" width="86" height="134" rx="7" opacity=".6"/>
    <rect x="11" y="11" width="78" height="126" rx="5" opacity=".28"/>
    <circle cx="50" cy="74" r="26" opacity=".65"/>
    <circle cx="50" cy="74" r="22" strokeDasharray="2 3" opacity=".6"/>
    {Array.from({length:12},(_,i)=><path key={i} d="M50 43V36"
      transform={`rotate(${i*30} 50 74)`} opacity=".67"/>)}
    <path d="M33 16 Q50 27 67 16 M33 132 Q50 121 67 132" opacity=".5"/>
   </g>
   <g fill="currentColor" opacity=".8"><path d="M50 36l2.4 5-2.4 5-2.4-5Z"/>
    <path d="M50 102l2.4 5-2.4 5-2.4-5Z"/></g>
  </svg>;
 return <svg className="bj-card-engraving bj-card-engraving-sovereign" viewBox="0 0 100 148"
   preserveAspectRatio="none" aria-hidden="true" focusable="false">
   <g fill="none" stroke="currentColor" strokeWidth="1">
    <rect x="7" y="7" width="86" height="134" rx="2" opacity=".65"/>
    <rect x="11" y="11" width="78" height="126" rx="2" opacity=".35"/>
    <path d="M50 34 L74 74 L50 114 L26 74 Z" opacity=".77"/>
    <path d="M50 42 L68 74 L50 106 L32 74 Z" opacity=".55"/>
    <path d="M18 33 Q34 31 34 16 M82 33 Q66 31 66 16
      M18 115 Q34 117 34 132 M82 115 Q66 117 66 132" opacity=".72"/>
    <path d="M30 19 L50 25 L70 19 M30 129 L50 123 L70 129" opacity=".38"/>
    <path d="M50 16L52 20L50 24L48 20Z M50 124L52 128L50 132L48 128Z"/>
   </g>
   <g fill="currentColor" opacity=".72">
    <circle cx="50" cy="34" r="1.5"/><circle cx="50" cy="114" r="1.5"/>
    <circle cx="25" cy="74" r="1.5"/><circle cx="75" cy="74" r="1.5"/>
   </g>
  </svg>;
}
export function PlayingCard({card,skin,variant,dealIndex=0,compact=false}:{
 card:CardFace;skin:CardSkin;variant?:string;dealIndex?:number;compact?:boolean;
}){
 const styles={
  '--card-face-a':skin.faceA,'--card-face-b':skin.faceB,'--card-border':skin.border,
  '--card-black':skin.black,'--card-red':skin.red,'--card-back-a':skin.backA,
  '--card-back-b':skin.backB,'--card-accent':skin.accent,
  '--card-arrival-delay':Math.min(dealIndex,9)*75+'ms',
 } as CSSProperties;
 const red=card.suit==='Hearts'||card.suit==='Diamonds';
 const rank=ranks[card.rank??'']??card.rank??'';
 const pip=suits[card.suit??'']??'';
 const special=variant==='card-midnight-sovereign'||variant==='card-solar-eclipse';
 return <div className={'bj-card bj-motion-card'+(compact?' compact':'')+
   (skin.premium?' premium':'')+(skin.dark?' dark':'')+(red?' red':'')+
   (card.faceDown?' is-facedown':' is-faceup')}
   data-facedown={card.faceDown} data-card-set={variant} style={styles} role="img"
   aria-label={card.faceDown?'Face-down card':(card.rank??'')+' of '+(card.suit??'')}>
  <div className="bj-card-flip">
   <div className="bj-card-side bj-card-front">
    {skin.premium&&<span className="bj-card-skin-crest" aria-hidden="true">
     {special&&<CollectorEngraving variant={variant}/>}
    </span>}
    <span className="bj-rank">{rank}</span>
    <span className="bj-index-pip" aria-hidden="true">{pip}</span>
    <span className="bj-suit" aria-hidden="true">{pip}</span>
    <span className="bj-corner-rank" aria-hidden="true">{rank}</span>
    <span className="bj-corner-suit" aria-hidden="true">{pip}</span>
    {skin.premium&&!special&&<span className="bj-ornament" aria-hidden="true">{skin.mark}</span>}
   </div>
   <div className="bj-card-side bj-card-back">
    <span className="bj-back-engrave" aria-hidden="true">{skin.mark}</span>
    <span className="bj-back-filigree" aria-hidden="true"/>
   </div>
  </div>
 </div>;
}
