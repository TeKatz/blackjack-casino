import type {ReelSymbol} from './bridge';

/** Original 5-symbol repertoire, redrawn as dimensional vector cabinet artwork.
 * Art cannot introduce symbols or affect weighted stops/paylines. */
export function ReelIcon({symbol}:{symbol:ReelSymbol}){
  if(symbol==='Bar')return <div className="symbol-bar">
    <strong>BAR</strong><i>THE MIDNIGHT CLUB</i>
  </div>;
  if(symbol==='Seven')return <div className="symbol-seven">7<span>✦</span></div>;
  if(symbol==='Cherry')return <svg className="symbol-art" viewBox="0 0 112 102" aria-hidden="true">
    <path d="M48 47C51 27 59 19 65 7M65 7C83 23 82 35 86 44M65 7C63 24 48 37 42 43" fill="none" stroke="#2d563b" strokeWidth="6" strokeLinecap="round"/>
    <path d="M65 10Q74 -2 100 8Q93 20 76 20Q70 17 65 10Z" fill="#497a44" stroke="#203c2b" strokeWidth="2"/>
    <path d="M69 11Q84 9 95 9" fill="none" stroke="#b9d687" strokeWidth="2.6" strokeLinecap="round"/>
    <ellipse cx="36" cy="67" rx="27" ry="27" fill="#651125" stroke="#5b1320" strokeWidth="3"/>
    <ellipse cx="35" cy="64" rx="24" ry="24" fill="#a31132"/>
    <ellipse cx="34" cy="58" rx="19" ry="19" fill="#d3213b" opacity=".9"/>
    <ellipse cx="31" cy="53" rx="11" ry="6" fill="#ff9b92" opacity=".69" transform="rotate(-27 31 53)"/>
    <ellipse cx="45" cy="80" rx="12" ry="6" fill="#4e1023" opacity=".3"/>
    <ellipse cx="78" cy="67" rx="27" ry="27" fill="#721025" stroke="#641221" strokeWidth="3"/>
    <ellipse cx="76" cy="63" rx="24" ry="24" fill="#bb1631"/>
    <ellipse cx="75" cy="57" rx="19" ry="18" fill="#f02e45" opacity=".9"/>
    <ellipse cx="71" cy="50" rx="11" ry="6" fill="#ffcec1" opacity=".73" transform="rotate(-28 71 50)"/>
    <ellipse cx="88" cy="80" rx="11" ry="7" fill="#540c24" opacity=".32"/>
    <path d="M42 42Q48 28 65 7Q75 17 83 43" fill="none" stroke="#83a65b" strokeWidth="1.8" opacity=".9"/>
  </svg>;
  if(symbol==='Lemon')return <svg className="symbol-art" viewBox="0 0 112 102" aria-hidden="true">
    <path d="M77 27Q86 10 101 17Q101 31 79 35Z" fill="#4d8647" stroke="#365b36" strokeWidth="2.6"/>
    <path d="M77 28Q89 23 97 20" stroke="#badb8d" strokeWidth="2" fill="none"/>
    <path d="M15 60Q7 50 19 39Q34 16 70 24Q81 22 90 32Q99 42 93 56Q92 74 68 83Q32 92 17 68Z"
      fill="#d7a936" stroke="#a07c2a" strokeWidth="3.8"/>
    <path d="M20 56Q14 48 25 39Q40 24 70 29Q80 27 88 39Q92 56 78 69Q49 87 23 65Z"
      fill="#f4d75b" stroke="#dbb644" strokeWidth="2"/>
    <path d="M23 52Q35 29 62 32Q72 30 78 38" stroke="#fff3ac" strokeWidth="5"
      opacity=".93" strokeLinecap="round" fill="none"/>
    <path d="M23 65Q39 82 65 77Q83 72 88 56" stroke="#b89235" strokeWidth="3"
      opacity=".43" fill="none"/>
    <ellipse cx="40" cy="45" rx="12" ry="6" fill="#fffbd4" opacity=".43" transform="rotate(-25 40 45)"/>
    <path d="M11 58L16 62M92 42L98 41" stroke="#d7af36" strokeWidth="4" strokeLinecap="round"/>
  </svg>;
  return <svg className="symbol-art" viewBox="0 0 112 102" aria-hidden="true">
    <path d="M55 11V5M44 85H68" stroke="#825326" strokeWidth="5" strokeLinecap="round"/>
    <path d="M27 68Q44 52 39 36Q40 23 55 22Q70 23 72 36Q68 52 86 68Q86 76 55 78Q26 76 27 68Z"
      fill="#a57433" stroke="#6d451e" strokeWidth="4"/>
    <path d="M31 65Q44 52 44 36Q43 28 55 27Q68 28 68 36Q67 50 81 65Q72 72 55 72Q38 72 31 65Z"
      fill="#ecc16c"/>
    <path d="M45 33Q49 27 57 29" fill="none" stroke="#fff1bd" strokeWidth="5" strokeLinecap="round"/>
    <path d="M37 62Q47 54 47 43" fill="none" stroke="#fff0a5" strokeWidth="3" strokeLinecap="round" opacity=".9"/>
    <path d="M69 39Q70 54 79 65" fill="none" stroke="#8b5725" strokeWidth="5" opacity=".39"/>
    <path d="M27 69Q55 82 85 69" fill="none" stroke="#f9d98c" strokeWidth="4" strokeLinecap="round"/>
    <ellipse cx="55" cy="80" rx="10" ry="8" fill="#c28a3e" stroke="#7f4f21" strokeWidth="3"/>
    <ellipse cx="53" cy="77" rx="5" ry="3" fill="#ffdf96" opacity=".77"/>
    <path d="M37 64H73" stroke="#a17038" strokeWidth="1.5" opacity=".7"/>
  </svg>;
}
