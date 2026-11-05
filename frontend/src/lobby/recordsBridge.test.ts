import {describe,it,expect} from 'vitest';
import {filterChallenges,isChallenges,isStatistics,isAccount,isHistory,isVault,previewChallenges,previewStatistics,previewAccount,previewHistory,previewVault} from './recordsBridge';
describe('Challenges and Statistics bridge',()=>{
  it('validates native data and rejects malformed rewards',()=>{
    expect(isChallenges(previewChallenges)).toBe(true);
    expect(isStatistics(previewStatistics)).toBe(true);
    expect(isAccount(previewAccount)).toBe(true);
    expect(isHistory(previewHistory)).toBe(true);
    expect(isVault(previewVault)).toBe(true);
    expect(isVault({...previewVault,progress:1.8})).toBe(false);
    expect(isAccount({...previewAccount,portraits:[{id:'x',name:3,description:'a'}]})).toBe(false);
    expect(isHistory({total:1,entries:[{id:'x',hands:[{name:'x',cards:'x'}]}]})).toBe(false);
    expect(isChallenges({...previewChallenges,entries:[{...previewChallenges.entries[0],coinReward:-100}]})).toBe(false);
    expect(isChallenges({...previewChallenges,entries:[{...previewChallenges.entries[0],id:'../../escape'}]})).toBe(false);
    expect(isStatistics({...previewStatistics,games:[{...previewStatistics.games[0],metrics:[{label:'A',value:3}]}]})).toBe(false);
  });
  it('respects category, claim state and search together',()=>{
    expect(filterChallenges(previewChallenges.entries,'Blackjack','ready','seat').map(e=>e.id)).toEqual(['bj-50']);
    expect(filterChallenges(previewChallenges.entries,'All','progress','master').map(e=>e.id)).toEqual(['bj-1000']);
    expect(filterChallenges(previewChallenges.entries,'Midnight Manor','ready','')).toEqual([]);
  });
});
