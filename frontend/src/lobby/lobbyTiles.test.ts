import {describe,expect,it} from 'vitest';
import {matchesLobbyFilter,stableLobbyTiles} from './lobbyTiles';

const ids=['blackjack','roulette','kniffel','chill-lounge','casino-empire',
  'midnight-reels','neon-vault','nine-lives','midnight-manor',
  'midnight-merge','arcade-games','midnight-descent'] as const;
const modes=ids.map(id=>({id}));
const visibleIds=(filter:string)=>stableLobbyTiles(modes,filter,ids)
  .filter(tile=>tile.visible).map(tile=>tile.mode.id);

describe('lobby tile filter stability',()=>{
  it('keeps ALL original keyed cards mounted across tab switches',()=>{
    for(const filter of ['all','tables','slots','relax','empire','collections','stats']){
      const tiles=stableLobbyTiles([...modes].reverse(),filter,ids);
      expect(tiles.map(tile=>tile.mode.id)).toEqual(ids);
      expect(tiles.every(tile=>modes.includes(tile.mode))).toBe(true);
      expect(new Set(tiles.map(tile=>tile.mode.id)).size).toBe(modes.length);
    }
  });

  it('only changes visibility; returning from Slots restores all original cards',()=>{
    expect(visibleIds('slots')).toEqual([
      'midnight-reels','neon-vault','nine-lives','midnight-manor']);
    expect(visibleIds('tables')).toEqual(['blackjack','roulette','kniffel']);
    expect(visibleIds('relax')).toEqual(['chill-lounge']);
    expect(visibleIds('empire')).toEqual(['casino-empire','midnight-merge']);
    expect(visibleIds('all')).toEqual(ids);
    expect(matchesLobbyFilter('neon-vault','slots')).toBe(true);
    expect(matchesLobbyFilter('neon-vault','tables')).toBe(false);
  });

  it('does not mutate or reorder the source snapshot',()=>{
    const original=[...modes].reverse();
    const previous=original.map(mode=>mode.id);
    stableLobbyTiles(original,'slots',ids);
    expect(original.map(mode=>mode.id)).toEqual(previous);
  });
});
