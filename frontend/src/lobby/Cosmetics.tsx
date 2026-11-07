import type {CSSProperties,ReactNode} from 'react';
import dicePalettes from './dicePalettes.json';
import frameThemes from './frameThemes.json';
import './cosmetics.css';
const jpg=new Set(['house-guest','eclipse-raven','eclipse-lady','eclipse-moon','eclipse-sovereign','dynasty-heiress','neon-overseer','neon-emperor','neon-croupier']);
const svg=new Set(['secret-guest']);
export function portraitPath(variant:string){const id=variant.replace(/^portrait-/,'');return /^[a-z0-9-]+$/.test(id)?'/shop/portraits/'+id+'.'+(svg.has(id)?'svg':jpg.has(id)?'jpg':'png'):'';}
const titleColors:Record<string,string>={'title-violet':'#CEAAFF','title-gold':'#FFD877','title-teal':'#68E9CD','title-rose':'#FFAACD','title-ice':'#A7DEFF','title-amber':'#FFB968','title-neon-cyan':'#63F5F0','title-neon-magenta':'#FF6BC7','title-dynasty-gold':'#FFD36E','title-dynasty-prism':'#E7B6FF'};
export function titleStyle(variant=''):CSSProperties{
 const gradient=variant==='title-adventure-names'?'linear-gradient(90deg,#D7C17B,#8FD5B4)':variant==='title-adventure-bell'?'linear-gradient(90deg,#65D7CF,#F0D38A)':null;
 return gradient?{backgroundImage:gradient,backgroundClip:'text',color:'transparent'}:{color:titleColors[variant]??'#F3E6CF'};
}
export function CosmeticTitle({variant,children}:{variant?:string;children:ReactNode}){return <span className="cosmetic-title" style={titleStyle(variant)}>{children}</span>;}
const frames:Record<string,[string,string,string?]>={
 'classic':['#52695A','#52695A'],'static-slate':['#92A4B8','#92A4B8'],'static-copper':['#CF966E','#CF966E'],'static-sage':['#9BC5A5','#9BC5A5'],'static-pearl':['#E1DFD3','#E1DFD3'],
 'neon-noir':['#58F5EE','#FF4DAB'],'eclipse-thread':['#E0CDEB','#766885'],'eclipse-halo':['#FFCB75','#9A563F'],
 'aurora-drift':['#98F4E3','#7895CF'],'neon-thread':['#54F6F0','#FF55B8'],'dynasty-circuit':['#48E7E4','#F3BF58'],
 'neon-pagoda':['#FF4FB5','#50F2EA'],'dynasty-crown':['#F7CF69','#FF58BC','#54F6F0'],
 'emerald-breathe':['#68dfa4','#245e43'],'midnight-sweep':['#8cc9ee','#304e8a'],'burgundy-ember':['#e38a9b','#702941'],
 'gold-shimmer':['#f2cd7b','#946428'],'crimson-heartbeat':['#ed7091','#6c203d'],'starlight-orbit':['#d4e9ff','#697da3'],
 'royal-violet':['#bb85f3','#ebca79'],'obsidian-pulse':['#42dcdf','#141e2b'],
 'prestige-bronze':['#deac7c','#81583b'],'prestige-silver':['#edf4fc','#8296ad'],'prestige-gold':['#ffe29b','#b88734'],
 'midnight-line':['#98b1d7','#344c77'],'eclipse-crown':['#dfb3ef','#edc880']};
export function ProfileFrame({variant='classic',children,large=false}:{variant?:string;children?:ReactNode;large?:boolean}){
 const themes:Record<string,{a:string;b:string;motion:string;seconds:number}>=frameThemes;
 const theme=themes[variant];
 const distinct=['neon-noir','eclipse-thread','eclipse-halo','aurora-drift','neon-thread','dynasty-circuit','neon-pagoda','dynasty-crown'].includes(variant);
 const palette=theme&&!distinct?[theme.a,theme.b]:frames[variant]??frames.classic;
 return <span className={'cosmetic-frame '+(large?'large ':'')+(!variant.startsWith('static-')&&variant!=='classic'?'animated':'')} data-frame={variant} data-frame-motion={theme?.motion??'pulse'}
 style={{'--frame-a':palette[0],'--frame-b':palette[1],'--frame-c':palette[2]??palette[0],'--frame-duration':(theme?.seconds??4)+'s'} as CSSProperties}>{children??'♠'}</span>;
}
export function PlayerPortrait({portrait,frame,large=false}:{portrait?:string;frame?:string;large?:boolean}){
 return <ProfileFrame variant={frame} large={large}>{portrait?<img src={portraitPath(portrait)} alt=""/>:'♠'}</ProfileFrame>;
}
export function DicePreview({variant}:{variant:string}){
 const palette:Record<string,{top:string;bottom:string;pips:string;edge:string;glow?:string;engraved:boolean}>=dicePalettes;
 const p=palette[variant]??{top:'#FFFCEE',bottom:'#D7CBB2',pips:'#2D3730',edge:'#BDA36E',engraved:false};
 return <div className="cosmetic-dice" data-engraved={p.engraved} style={{'--die-top':p.top,'--die-bottom':p.bottom,'--die-pips':p.pips,'--die-edge':p.edge,'--die-glow':p.glow??'transparent'} as CSSProperties}>
 {[4,5].map((face,index)=><span key={face} className={'cosmetic-d6 d6-'+index}>{Array.from({length:9},(_,i)=><i key={i} style={{visibility:([0,2,6,8,...(face===5?[4]:[])]).includes(i)?'visible':'hidden'}}/>)}</span>)}</div>;
}
export function VaultIcon({tier}:{tier:string}){
 const colors:Record<string,string>={bronze:'#C99464',silver:'#D5E6F2',gold:'#F1D181',platinum:'#B9DAD9',diamond:'#99EFFF'};
 const color=colors[tier.toLowerCase()]??colors.bronze;
 return <svg className="premium-vault-icon" viewBox="0 0 64 64" aria-hidden="true" style={{color}}>
  <path d="M8 10H54V54H8Z" fill="#102b29" stroke="currentColor" strokeWidth="4"/>
  <path d="M14 16H48V48H14Z M10 20H16 M10 43H16" fill="none" stroke="currentColor" strokeWidth="2"/>
  <circle cx="30" cy="32" r="11" fill="#213b3c" stroke="currentColor" strokeWidth="2"/>
  <path d="M30 21V43 M19 32H41 M22 24L38 40 M22 40L38 24 M45 26V38" stroke="currentColor" strokeWidth="2"/>
  <circle cx="30" cy="32" r="3" fill="currentColor"/>
 </svg>;
}
