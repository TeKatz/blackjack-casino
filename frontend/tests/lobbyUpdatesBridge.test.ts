import {describe,expect,it} from 'vitest';
import {fallbackUpdates,isLobbyUpdatesSnapshot} from '../src/lobby/updatesBridge';
describe('Lobby updater bridge',()=>{
  const state={...fallbackUpdates,installedVersion:'0.10.1-dev.0',canCheck:true};
  it('accepts native update status and rejects fake/invalid availability',()=>{
    expect(isLobbyUpdatesSnapshot(state)).toBe(true);
    expect(isLobbyUpdatesSnapshot({...state,updateAvailable:true,canInstall:true})).toBe(true);
    expect(isLobbyUpdatesSnapshot({...state,channel:'nightly'})).toBe(false);
    expect(isLobbyUpdatesSnapshot({...state,canInstall:true,updateAvailable:false})).toBe(false);
    expect(isLobbyUpdatesSnapshot({...state,busy:true,canCheck:true})).toBe(false);
    expect(isLobbyUpdatesSnapshot({...state,installedVersion:12})).toBe(false);
    expect(isLobbyUpdatesSnapshot({...state,status:'x'.repeat(2049)})).toBe(false);
  });
});
