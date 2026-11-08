import {describe,it,expect} from 'vitest';
import {renderToStaticMarkup} from 'react-dom/server';
import {ModernSeasons,seasonTheme} from './ModernSeasons';
import type {SeasonsSnapshot} from './recordsBridge';

const base:SeasonsSnapshot={
 activeSeasonId:'season-3-neon-dynasty',
 seasons:[
  {id:'season-1-midnight-shift',name:'Midnight Shift',description:'Early season',
   tier:50,experience:5000,totalTiers:50,maxExperience:5000,archived:true,rewardsEnabled:true,
   tiers:[{tier:50,requiredExperience:5000,coins:20,cosmeticId:'title-veteran',bankrollReward:0}]},
  {id:'season-2-velvet-eclipse',name:'Velvet Eclipse',description:'Second season',
   tier:100,experience:15000,totalTiers:100,maxExperience:15000,archived:true,rewardsEnabled:true,
   tiers:[{tier:100,requiredExperience:15000,coins:40,cosmeticId:'frame-eclipse',bankrollReward:250}]},
  {id:'season-3-neon-dynasty',name:'Neon Dynasty',description:'Current',
   tier:2,experience:500,totalTiers:5,maxExperience:3000,archived:false,rewardsEnabled:true,
   tiers:[1,2,3,4,5].map(t=>({tier:t,requiredExperience:t*600,coins:40,
    cosmeticId:t===5?'title-neon':'',bankrollReward:t===4?100:0})).map(t=>({
     ...t,cosmeticId:t.cosmeticId||null}))}
 ]
};
const noop=()=>{};
describe('premium season pass themes',()=>{
 it('uses a distinct identity for each real season',()=>{
  expect(seasonTheme('season-1-midnight-shift','Midnight Shift')).toBe('midnight');
  expect(seasonTheme('season-2-velvet-eclipse','Velvet Eclipse')).toBe('eclipse');
  expect(seasonTheme('season-3-neon-dynasty','Neon Dynasty')).toBe('neon');
 });
 it('renders an accessible progression meter and keeps activation locked for the active pass',()=>{
  const html=renderToStaticMarkup(<ModernSeasons data={base} loaded language="en" pending={null}
   onBack={noop} onActivate={noop}/>);
  expect(html).toContain('data-season-theme="neon"');
  expect(html).toContain('role="progressbar"');
  expect(html).toContain('aria-valuenow="17"');
  expect(html).toContain('NEXT FEATURED REWARD');
  expect(html).toContain('role="tablist"');
  expect(html).toContain('aria-pressed="true"');
  expect(html).toMatch(/class="record-claim-all season-activate" disabled=""/);
  expect(html).toContain('Tier 3');
 });
});
