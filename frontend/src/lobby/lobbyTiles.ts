/**
 * Category navigation changes which tiles participate in the grid, not which
 * keyed React elements exist. PixiJS maps scenes to their art-window elements:
 * unmounting those elements on every filter change recreates the GPU art and
 * exposes a differently cropped CSS fallback before the first render tick.
 */
export function matchesLobbyFilter(id:string,filter:string):boolean{
  if(filter==='all')return true;
  if(filter==='tables')return id==='blackjack'||id==='roulette'||id==='kniffel';
  if(filter==='slots')return ['midnight-reels','neon-vault','nine-lives','midnight-manor'].includes(id);
  if(filter==='relax')return id==='chill-lounge';
  if(filter==='empire')return id==='casino-empire'||id==='midnight-merge';
  return true;
}

export function stableLobbyTiles<T extends {id:string}>(
  modes:readonly T[],filter:string,order:readonly string[],
):{mode:T;visible:boolean}[]{
  return [...modes]
    .sort((a,b)=>order.indexOf(a.id)-order.indexOf(b.id))
    .map(mode=>({mode,visible:matchesLobbyFilter(mode.id,filter)}));
}
