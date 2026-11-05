import type {ReelSymbol} from './bridge';

export function ReelIcon({symbol}:{symbol:ReelSymbol}){
  if(symbol==='Bar')return <div className="symbol-bar"><strong>BAR</strong><i>THE MIDNIGHT CLUB</i></div>;
  if(symbol==='Seven')return <div className="symbol-seven">7<span>✦</span></div>;
  if(symbol==='Cherry')return <svg className="symbol-art" viewBox="0 0 100 90" aria-hidden="true">
    <path d="M43 43Q43 18 58 8M58 8Q73 27 75 42M58 8Q54 28 40 36" fill="none" stroke="#3c7047" strokeWidth="5" strokeLinecap="round"/>
    <path d="M55 10Q72-1 88 7Q74 24 55 10Z" fill="#78a95f" stroke="#417348" strokeWidth="2"/>
    <circle cx="34" cy="60" r="22" fill="#b71636" stroke="#751426" strokeWidth="3"/>
    <circle cx="71" cy="60" r="22" fill="#de2c40" stroke="#8f1730" strokeWidth="3"/>
    <path d="M24 51Q29 43 38 43M61 51Q66 44 74 45" stroke="#ffd6c0" strokeWidth="5" strokeLinecap="round" opacity=".66"/>
  </svg>;
  if(symbol==='Lemon')return <svg className="symbol-art" viewBox="0 0 100 90" aria-hidden="true">
    <path d="M18 51Q7 39 20 29Q38 6 73 24Q88 24 85 39Q89 64 63 71Q35 81 18 51Z" fill="#edd051" stroke="#b68928" strokeWidth="3"/>
    <path d="M31 30Q45 20 61 25" stroke="#fff4aa" strokeWidth="6" strokeLinecap="round" opacity=".8"/>
    <path d="M65 20Q70 5 85 10Q82 20 65 20Z" fill="#72a768" stroke="#3d7047" strokeWidth="2"/>
  </svg>;
  return <svg className="symbol-art" viewBox="0 0 100 90" aria-hidden="true">
    <path d="M49 13V8M37 75H66M21 62Q34 50 33 29Q35 16 50 16Q65 16 67 29Q65 50 80 62Q79 69 50 69Q21 69 21 62Z"
      fill="#efc36e" stroke="#9d652f" strokeWidth="3"/>
    <path d="M38 30Q38 22 45 20" fill="none" stroke="#fff1b5" strokeWidth="5" strokeLinecap="round"/>
    <circle cx="50" cy="72" r="7" fill="#a76b32"/>
  </svg>;
}
