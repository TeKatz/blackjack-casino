import type {CSSProperties} from 'react';
import type {CardFace} from './bridge';
import type {CardSkin} from './cardSkins';

const rank:Record<string,string>={Ace:'A',Jack:'J',Queen:'Q',King:'K',Two:'2',Three:'3',
  Four:'4',Five:'5',Six:'6',Seven:'7',Eight:'8',Nine:'9',Ten:'10'};
const suit:Record<string,string>={Clubs:'♣',Diamonds:'♦',Hearts:'♥',Spades:'♠'};
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
 return <div className={'bj-card bj-motion-card'+(compact?' compact':'')+
    (skin.premium?' premium':'')+(skin.dark?' dark':'')+(red?' red':'')+
    (card.faceDown?' is-facedown':' is-faceup')}
    data-facedown={card.faceDown} data-card-set={variant} style={styles} role="img"
    aria-label={card.faceDown?'Face-down card':(card.rank??'')+' of '+(card.suit??'')}>
   <div className="bj-card-flip">
    <div className="bj-card-side bj-card-front">
      {skin.premium&&<span className="bj-card-skin-crest" aria-hidden="true"/>}
      <span className="bj-rank">{rank[card.rank??'']??card.rank??''}</span>
      <span className="bj-suit">{suit[card.suit??'']??''}</span>
      <span className="bj-corner-suit">{suit[card.suit??'']??''}</span>
      {skin.premium&&<span className="bj-ornament" aria-hidden="true">{skin.mark}</span>}
    </div>
    <div className="bj-card-side bj-card-back">
      <span className="bj-back-engrave" aria-hidden="true">{skin.mark}</span>
      <span className="bj-back-filigree" aria-hidden="true"/>
    </div>
   </div>
 </div>;
}
