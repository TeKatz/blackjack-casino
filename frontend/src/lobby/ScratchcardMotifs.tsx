/** Original vector emblems. Five identities per series; C# still owns their numeric IDs. */
const identities:Record<string,readonly string[]>={
  'lucky-sevens':['spade','diamond-suit','club','heart','seven'],
  'gold-rush':['gold-ingot','gold-nugget','mining-pick','treasure-chest','stacked-coins'],
  'platinum-vault':['platinum-ingot','vault-door','vip-chip','royal-crown','vault-key'],
  'diamond-royale':['cut-diamond','solitaire-ring','gem-crown','crystal-sceptre','prism-star'],
};
export function scratchSymbolLabel(gameId:string, index:number):string {
  return identities[gameId]?.[index]??'unknown';
}
function GoldSymbol({symbol}:{symbol:number}){
  switch(symbol){
    case 0:return <g>
      <path d="M9 28 19 17h29l8 11-6 21H15L9 28Z" fill="currentColor" fillOpacity=".17"/>
      <path d="M9 28h47M19 17l-4 11 4 21m29-32 8 11-6 21M15 49h35"/>
      <path d="m24 23 3-3h17" strokeWidth="1.4"/>
    </g>;
    case 1:return <g>
      <path d="m12 35 5-14 11-7 11 4 10-3 6 14-4 17-15 6-16-4-8-13Z"
        fill="currentColor" fillOpacity=".16"/>
      <path d="m12 35 17-8 10 7 16-5m-26-2-1-13m11 20-3 18m-16-4 19-14m10-19-10 19"/>
      <path d="m20 26 8-7m19 21 3-9" strokeWidth="1.4"/>
    </g>;
    case 2:return <g>
      <path d="m16 51 28-35 5 4-28 35-5-4Z" fill="currentColor" fillOpacity=".16"/>
      <path d="M8 20q25-21 47 0l-4 8Q30 16 13 31l-5-11Z"
        fill="currentColor" fillOpacity=".25"/>
      <path d="M44 18 20 50m5-1-4 6M13 30l-5-10m43 8 4-8"/>
    </g>;
    case 3:return <g>
      <rect x="10" y="25" width="44" height="27" rx="4" fill="currentColor" fillOpacity=".14"/>
      <path d="M10 29V20q22-19 44 0v9M10 31h44M17 25v25m30-25v25M10 44h44"/>
      <path d="M29 34h6v8h-6z" fill="currentColor" fillOpacity=".34"/>
      <path d="M20 16q12-6 24 0" strokeWidth="1.6"/>
    </g>;
    default:return <g>
      <path d="M11 36v10q0 10 21 10t21-10V36" fill="currentColor" fillOpacity=".12"/>
      <ellipse cx="32" cy="36" rx="21" ry="9" fill="currentColor" fillOpacity=".25"/>
      <path d="M11 36q0 9 21 9t21-9m-42 9q0 9 21 9t21-9M13 24v9q0 8 19 8t19-8v-9"/>
      <ellipse cx="32" cy="24" rx="19" ry="8" fill="currentColor" fillOpacity=".16"/>
      <path d="M29 20h6m-3-3v13" strokeWidth="1.8"/>
    </g>;
  }
}
function PlatinumSymbol({symbol}:{symbol:number}){
  switch(symbol){
    case 0:return <g>
      <path d="m8 38 9-16h31l8 16-8 11H16L8 38Z" fill="currentColor" fillOpacity=".14"/>
      <path d="M8 38h48M17 22l-1 16v11m32-27 8 16-8 11M20 30h24"/>
      <path d="m24 18 5-6h21l5 10" strokeWidth="1.8"/>
      <path d="M26 43h13" strokeWidth="1.6"/>
    </g>;
    case 1:return <g>
      <rect x="9" y="8" width="46" height="48" rx="7" fill="currentColor" fillOpacity=".09"/>
      <rect x="14" y="13" width="36" height="38" rx="4" strokeWidth="1.6"/>
      <circle cx="32" cy="32" r="13" fill="currentColor" fillOpacity=".13"/>
      <circle cx="32" cy="32" r="6"/>
      <path d="M32 19v7m0 12v7M19 32h7m12 0h7m-21-8 4 4m8 8 4 4m0-16-4 4m-8 8-4 4"/>
    </g>;
    case 2:return <g>
      <circle cx="32" cy="32" r="23" fill="currentColor" fillOpacity=".15"/>
      <circle cx="32" cy="32" r="16" strokeWidth="2"/>
      <path d="m21 11 5 10m12 22 5 10M11 21l10 5m22 12 10 5M43 11l-5 10M26 43l-5 10M11 43l10-5m22-12 10-5"/>
      <path d="m32 23 6 9-6 9-6-9z" fill="currentColor" fillOpacity=".25"/>
    </g>;
    case 3:return <g>
      <path d="M10 22 20 34l12-21 12 21 10-12-5 26H15l-5-26Z"
        fill="currentColor" fillOpacity=".15"/>
      <path d="M15 48h34v6H15zM20 34l12-21 12 21m-34-12 5 26h34l5-26"/>
      <circle cx="10" cy="20" r="3" fill="currentColor"/>
      <circle cx="32" cy="12" r="3" fill="currentColor"/>
      <circle cx="54" cy="20" r="3" fill="currentColor"/>
    </g>;
    default:return <g>
      <circle cx="23" cy="22" r="12" fill="currentColor" fillOpacity=".13"/>
      <circle cx="23" cy="22" r="5"/>
      <path d="M32 29 52 49l-5 5-7-7-5 5-7-7 5-5-8-9m-8-1 8-8" fill="currentColor" fillOpacity=".14"/>
      <path d="m37 41-4 4m9 1-4 4"/>
    </g>;
  }
}
function DiamondSymbol({symbol}:{symbol:number}){
  switch(symbol){
    case 0:return <g>
      <path d="m7 24 11-13h28l11 13-25 31L7 24Z" fill="currentColor" fillOpacity=".11"/>
      <path d="M7 24h50M18 11l8 13 6 31 6-31 8-13M7 24l19 0m12 0h19M26 24l6-13 6 13"/>
      <path d="m14 18 5-7m28 0 5 7" strokeWidth="1.4"/>
    </g>;
    case 1:return <g>
      <circle cx="32" cy="39" r="16" fill="currentColor" fillOpacity=".08"/>
      <path d="M16 38q-1 19 16 19t16-19M19 31q-4 15 13 15t13-15"/>
      <path d="m23 18 9-12 9 12-9 13-9-13Z" fill="currentColor" fillOpacity=".2"/>
      <path d="M23 18h18m-18 0 9 13 9-13M32 6v25M26 12h12"/>
    </g>;
    case 2:return <g>
      <path d="M9 26 19 37l13-21 13 21 10-11-6 24H15L9 26Z" fill="currentColor" fillOpacity=".1"/>
      <path d="m15 50 34 0v6H15zM19 37l13-21 13 21m-36-11 6 24h34l6-24"/>
      <path d="m27 37 5-7 5 7-5 7-5-7Z" fill="currentColor" fillOpacity=".3"/>
      <path d="m8 17 3-4 3 4-3 4-3-4Zm42 0 3-4 3 4-3 4-3-4Z"/>
    </g>;
    case 3:return <g>
      <path d="m28 19 4-14 8 14-8 11-4-11Z" fill="currentColor" fillOpacity=".18"/>
      <path d="m32 30 0 27m-6-3h12M28 36h8M26 17l6-12 10 12-10 13-6-13Zm6-12v25M26 17h16"/>
      <path d="M17 17h7m-3-4v8M47 33h7m-3-4v8" strokeWidth="1.7"/>
    </g>;
    default:return <g>
      <path d="M32 5 39 24 59 32 39 39 32 59 24 39 5 32 24 24 32 5Z"
        fill="currentColor" fillOpacity=".13"/>
      <path d="M32 5v54M5 32h54M24 24l15 15m0-15L24 39"/>
      <path d="m15 11 4 4m30 30 4 4m0-38-4 4m-30 30-4 4" strokeWidth="1.5"/>
      <path d="m32 20 12 12-12 12-12-12 12-12Z" fill="currentColor" fillOpacity=".12"/>
    </g>;
  }
}
/** The classic symbols remain intact; the premium series receive purpose-made SVG art. */
export function ScratchcardMotif({gameId,symbol}:{gameId:string;symbol:number}){
  const id=scratchSymbolLabel(gameId,symbol);
  if(gameId==='lucky-sevens'){
    const classic=['♠','♦','♣','♥','7'];
    return <span className="scratch-motif-classic" data-motif={id}>{classic[symbol]??'?'}</span>;
  }
  return <svg className="scratch-motif-svg" data-motif={id}
    viewBox="0 0 64 64" fill="none" stroke="currentColor" strokeWidth="2.4"
    strokeLinecap="round" strokeLinejoin="round" focusable="false" aria-hidden="true">
    {gameId==='gold-rush'?<GoldSymbol symbol={symbol}/>:
      gameId==='platinum-vault'?<PlatinumSymbol symbol={symbol}/>:
      <DiamondSymbol symbol={symbol}/>}
  </svg>;
}
