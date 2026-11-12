/** Inventory identifiers stay native-owned; these are purely decorative pictograms. */
export function BackroomItemIcon({kind,name}:{kind:string;name:string}){
 const label=(kind+' '+name).toLowerCase();
 const is=(words:string[])=>words.some(word=>label.includes(word));
 const potion=is(['consumable','potion','elixir','tonic','salve','vial','remedy','antidote']);
 const document=is(['quest','letter','map','ledger','note','record','journal','scroll','paper','clue','contract','deed']);
 const relic=is(['relic','amulet','crystal','gem','stone','talisman','orb','charm']);
 const key=is(['key','lockpick','utility','tool','knife','torch','compass','lantern']);
 const palette=potion?'#b6e5c0':document?'#e6cea1':relic?'#cfb9ed':key?'#a5ceda':'#d7c39d';
 return <svg className="backroom-inventory-icon" viewBox="0 0 48 48" fill="none"
  aria-hidden="true" focusable="false">
  <rect x="2" y="2" width="44" height="44" rx="11" fill="#10282b" stroke={palette} strokeOpacity=".5"/>
  {potion?<><path d="M20 9h8m-6 0v12L13 35q-2 4 3 5h16q5-1 3-5L26 21V9" stroke={palette} strokeWidth="2.4" strokeLinecap="round"/>
   <path d="M18 30h12l4 7H14Z" fill="#73bda6" opacity=".7"/><circle cx="27" cy="27" r="2" fill="#faf0c0"/></>:
   document?<><path d="M13 10h18l4 6v23H13Z" fill="#d2b88c" stroke={palette} strokeWidth="2.1"/>
    <path d="M31 10v7h5M18 22h14M18 27h12M18 32h8" stroke="#634e3b" strokeWidth="2"/></>:
   relic?<><path d="M24 8L36 19L31 36L24 42L16 36L12 19Z" fill="#6b597f" stroke={palette} strokeWidth="2.5"/>
    <path d="M24 8L24 42M12 19H36M16 36L24 19L31 36" stroke="#f0deff" strokeOpacity=".7" strokeWidth="1.3"/></>:
   key?<><circle cx="18" cy="19" r="8" stroke={palette} strokeWidth="4"/><path d="M24 25l14 14m-5-6l-4 4" stroke={palette} strokeWidth="4" strokeLinecap="round"/></>:
   <><rect x="13" y="13" width="22" height="22" rx="5" stroke={palette} strokeWidth="3"/>
   <path d="M24 15v18M15 24h18" stroke={palette} strokeWidth="2"/></>}
 </svg>;
}
